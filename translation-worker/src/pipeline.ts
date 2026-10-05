import type { LlmBackend } from "./backends/types";
import {
  continuationSystem, reviewPrompt, reviewSchema, reviewSystem, revisePrompt, terminologyPrompt,
  terminologySchema, terminologySystem, translatePrompt, translatorSystem,
} from "./prompts";
import { chunkSource, parseTranslation, validateTranslation, type ValidationReport } from "./text";
import type { ClaimedJob, CompleteJobPayload, GlossaryTerm, ReviewResult } from "./types";

export interface PipelineOptions {
  /** Source characters per translation call; longer chapters are split on paragraph boundaries. */
  maxChunkChars: number;
  /** Revise when the first review scores below this, or reports any major/critical issue. */
  reviseBelow: number;
  log?: (msg: string) => void;
  signal?: AbortSignal;
}

interface Draft {
  title: string;
  body: string;
}

/**
 * Multi-pass translation, the main lever for quality:
 *  1. terminology  — extend the title glossary with this chapter's new names/terms
 *  2. translate    — full context: synopsis, style guide, story so far, glossary
 *  3. review       — editor pass against the source: omissions, mistranslations, glossary drift
 *  4. revise       — only when the review finds real problems; then re-review and keep the better draft
 *  5. validators   — deterministic checks (paragraph/length ratios, leftover source script)
 */
export async function translateJob(job: ClaimedJob, backend: LlmBackend, opts: PipelineOptions): Promise<CompleteJobPayload> {
  const log = opts.log ?? (() => {});
  const lang = job.sourceLanguage;
  const models = new Set<string>();
  const started = Date.now();

  // ── 1. Terminology ─────────────────────────────────────────────────────────
  let newTerms: GlossaryTerm[] = [];
  try {
    const res = await backend.complete({
      system: terminologySystem(lang),
      prompt: terminologyPrompt(job),
      tier: "assist",
      jsonSchema: terminologySchema as unknown as Record<string, unknown>,
      signal: opts.signal,
    });
    models.add(res.model);
    const known = new Set(job.glossary.map(g => g.sourceTerm));
    newTerms = ((res.json as { terms?: GlossaryTerm[] })?.terms ?? [])
      .map(t => ({ ...t, sourceTerm: t.sourceTerm.trim(), translation: t.translation.trim() }))
      .filter(t => t.sourceTerm && t.translation && !known.has(t.sourceTerm) && job.sourceText.includes(t.sourceTerm));
    log(`terminology: ${newTerms.length} new term(s)`);
  } catch (error) {
    if (isFatal(error)) throw error;
    log(`terminology pass failed, continuing with existing glossary: ${(error as Error).message}`);
  }

  const glossary: GlossaryTerm[] = [...job.glossary, ...newTerms.map(t => ({ ...t, isLocked: false }))];

  // ── 2. Translate ───────────────────────────────────────────────────────────
  const chunks = chunkSource(job.sourceText, opts.maxChunkChars);
  let title: string | null = null;
  const bodies: string[] = [];
  for (let i = 0; i < chunks.length; i++) {
    const previousTail = i > 0 ? bodies[i - 1].slice(-1500) : undefined;
    const res = await backend.complete({
      system: i === 0 ? translatorSystem(lang) : continuationSystem(lang),
      prompt: translatePrompt(job, glossary, { text: chunks[i], index: i, total: chunks.length, previousTail }),
      tier: "translate",
      signal: opts.signal,
    });
    models.add(res.model);
    const parsed = parseTranslation(res.text);
    if (i === 0) title = parsed.title;
    if (!parsed.body) throw new Error(`Empty translation for part ${i + 1}/${chunks.length}`);
    bodies.push(parsed.body);
  }
  const draft: Draft = { title: title ?? `Chapter ${job.chapterNumber}`, body: bodies.join("\n\n") };
  log(`translate: ${chunks.length} part(s), ${draft.body.length} chars`);

  // ── 3. Review ──────────────────────────────────────────────────────────────
  const firstReview = await review(job, glossary, draft, backend, opts, models);
  log(`review: score ${firstReview.score}, ${firstReview.issues.length} issue(s)`);

  let best = draft;
  let bestReview = firstReview;
  let revised = false;

  // ── 4. Revise (only when it's worth it) ────────────────────────────────────
  const serious = firstReview.issues.filter(i => i.severity !== "minor");
  if (firstReview.score < opts.reviseBelow || serious.length > 0) {
    const res = await backend.complete({
      system: translatorSystem(lang),
      prompt: revisePrompt(job, glossary, draft.title, draft.body, firstReview.issues),
      tier: "translate",
      signal: opts.signal,
    });
    models.add(res.model);
    const parsed = parseTranslation(res.text);
    if (parsed.body) {
      const candidate: Draft = { title: parsed.title ?? draft.title, body: parsed.body };
      const secondReview = await review(job, glossary, candidate, backend, opts, models);
      log(`revise: re-review score ${secondReview.score} (was ${firstReview.score})`);
      if (secondReview.score >= firstReview.score) {
        best = candidate;
        bestReview = secondReview;
        revised = true;
      }
    }
  }

  // ── 5. Deterministic validators ────────────────────────────────────────────
  const validation: ValidationReport = validateTranslation(job.sourceText, best.body, lang, glossary);
  const remainingCritical = bestReview.issues.filter(i => i.severity === "critical");
  const qaFlagged = validation.flags.length > 0 || remainingCritical.length > 0;
  log(`validate: ${validation.flags.length} flag(s), ${validation.warnings.length} warning(s)${qaFlagged ? " → needs review" : ""}`);

  const modelInfo = `${backend.name}:${[...models].join(",")}`.slice(0, 200);

  return {
    translatedTitle: best.title.slice(0, 255),
    content: best.body,
    // The summary describes source events, so the first review's is as good as any; prefer it unless empty.
    summary: (firstReview.summary || bestReview.summary || "").slice(0, 4000) || undefined,
    newGlossaryTerms: newTerms,
    qaScore: clampScore(bestReview.score),
    qaFlagged,
    modelInfo,
    qaReport: JSON.stringify({
      revised,
      firstScore: firstReview.score,
      finalScore: bestReview.score,
      issues: bestReview.issues,
      firstIssues: revised ? firstReview.issues : undefined,
      validation,
      chunks: chunks.length,
      durationMs: Date.now() - started,
      attempt: job.attempt,
    }),
  };
}

async function review(
  job: ClaimedJob,
  glossary: GlossaryTerm[],
  draft: Draft,
  backend: LlmBackend,
  opts: PipelineOptions,
  models: Set<string>,
): Promise<ReviewResult> {
  const res = await backend.complete({
    system: reviewSystem(job.sourceLanguage),
    prompt: reviewPrompt(job, glossary, draft.title, draft.body),
    tier: "assist",
    jsonSchema: reviewSchema as unknown as Record<string, unknown>,
    signal: opts.signal,
  });
  models.add(res.model);
  const json = res.json as Partial<ReviewResult> | undefined;
  return {
    score: typeof json?.score === "number" ? json.score : 0,
    issues: Array.isArray(json?.issues) ? json!.issues : [],
    summary: typeof json?.summary === "string" ? json.summary.trim() : "",
  };
}

function clampScore(score: number): number {
  return Math.round(Math.min(10, Math.max(0, score)) * 100) / 100;
}

// Quota and refusal errors must bubble up to the worker loop, not be swallowed by a pass.
function isFatal(error: unknown): boolean {
  const name = (error as Error)?.name;
  return name === "UsageLimitError" || name === "RefusalError" || (error as Error)?.message === "Aborted";
}
