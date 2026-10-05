import type { ClaimedJob, GlossaryTerm, ReviewIssue } from "./types";

// System prompts depend only on the source language, so they stay byte-identical across
// calls (prompt-cache friendly) and short (CLI backends pass them as a command-line argument).
// Everything per-chapter goes into the user prompt.

export function languageName(code: string): string {
  switch (code.toLowerCase()) {
    case "zh": return "Chinese";
    case "ja": return "Japanese";
    case "ko": return "Korean";
    default: return code;
  }
}

export function translatorSystem(lang: string): string {
  const L = languageName(lang);
  return `You are a senior literary translator who turns ${L} web novels into polished, natural English for an English-speaking readership.

Fidelity rules (non-negotiable):
- Translate every sentence. Never skip, summarize, condense, or add content. Repetitive passages, status windows, lists and in-chapter author notes are translated too.
- Keep the paragraph structure: each source paragraph becomes exactly one English paragraph, in the same order, separated by a blank line.
- Use the glossary renderings exactly for every listed term. Entries marked LOCKED are mandatory with no variation.
- Stay consistent with the story so far (names, relationships, who knows what).

Style:
- Fluent, idiomatic English prose a native reader enjoys, not a literal gloss. Restructure sentences when source syntax reads awkwardly, but keep meaning, tone and voice.
- Dialogue uses English double quotes; convert 「」『』“” and similar marks accordingly. Keep 【】 system text as [ ].
- Preserve each speaker's register (arrogant young master, formal elder, crude bandit...).
- Render idioms by meaning, not word for word, unless the literal image matters to the scene.
- No translator notes, footnotes, or commentary of any kind.

Output format, exactly:
Title: <English chapter title, without any "Chapter N" prefix>

<chapter body>

Nothing before the Title line and nothing after the body.`;
}

export function continuationSystem(lang: string): string {
  return translatorSystem(lang).replace(
    /Output format, exactly:[\s\S]*$/,
    "Output format: only the English text of the given part, paragraphs separated by blank lines. No title line, nothing before or after.",
  );
}

export function terminologySystem(lang: string): string {
  const L = languageName(lang);
  return `You maintain the terminology glossary for a ${L}-to-English web novel translation. Consistency across hundreds of chapters depends on you.

Find proper nouns and recurring special terms in the chapter that are NOT already in the provided glossary: character names, titles and epithets, places, sects/clans/organizations, cultivation realms and ranks, techniques and skills, artifacts and items, unique in-world concepts.

Rendering conventions:
- Chinese personal names: pinyin without tone marks, family name first, given-name syllables joined (林动 -> Lin Dong, 萧炎 -> Xiao Yan).
- Japanese names: Hepburn romanization, source order. Korean names: Revised Romanization, family name first.
- Sects, places, techniques, realms, items: translate the meaning into evocative English that follows genre conventions (青阳镇 -> Qingyang Town, 天元宗 -> Heavenly Origin Sect, 筑基 -> Foundation Establishment).
- Match the style of the existing glossary entries.

Skip common words, generic nouns, pronouns and one-off descriptions. Copy each source term exactly as it appears in the chapter. Return an empty list when nothing qualifies.`;
}

export function reviewSystem(lang: string): string {
  const L = languageName(lang);
  return `You are an exacting editor reviewing an English translation of a ${L} web novel chapter against its source.

Check, in priority order:
1. omission: a source sentence or paragraph missing, condensed or summarized in the translation
2. mistranslation: meaning changed, wrong subject or object, reversed negation, wrong numbers
3. glossary: a glossary term rendered differently from its glossary entry
4. untranslated: source-language text left in the translation
5. addition: content that is not in the source
6. fluency: stiff, unnatural or ungrammatical English
7. formatting: paragraphs merged or split wrongly, broken dialogue punctuation

Severity: critical = readers would notice lost or wrong meaning (missing paragraph, wrong speaker); major = a clear error within a sentence; minor = polish.
Score 1-10: 10 = publishable as-is by a professional; 8 = minor polish only; 6 = several major issues; 4 or below = critical problems.
Quote short excerpts so each issue can be located. Report at most 25 issues, most severe first.

Also write a 3-6 sentence English summary of what happens in this chapter (events, who appears, what changed), using the glossary names. It is shown to the translator of later chapters as context.`;
}

// ── User prompts ─────────────────────────────────────────────────────────────

function formatGlossary(terms: GlossaryTerm[]): string {
  if (terms.length === 0) return "(empty)";
  return terms
    .map(t => {
      const notes = t.notes ? ` (${t.notes})` : "";
      return `${t.sourceTerm} -> ${t.translation} [${t.category}]${notes}${t.isLocked ? " LOCKED" : ""}`;
    })
    .join("\n");
}

function seriesContext(job: ClaimedJob): string {
  const parts: string[] = [];
  parts.push(`# Series\nTitle: ${job.titleName}${job.originalTitle && job.originalTitle !== job.titleName ? ` (original: ${job.originalTitle})` : ""}`);
  if (job.synopsis) parts.push(`Synopsis: ${job.synopsis}`);
  if (job.styleGuide) parts.push(`\n# Series style guide (follow it)\n${job.styleGuide}`);
  if (job.previousChapters.length > 0) {
    parts.push(
      "\n# Story so far (most recent chapters)\n" +
        job.previousChapters
          .map(p => `- Ch. ${p.chapterNumber}${p.title ? ` "${p.title}"` : ""}: ${p.summary}`)
          .join("\n"),
    );
  }
  return parts.join("\n");
}

function chapterHeader(job: ClaimedJob): string {
  return `Volume ${job.volumeNumber}, Chapter ${job.chapterNumber}` +
    (job.sourceChapterTitle ? `\nSource chapter title: ${job.sourceChapterTitle}` : "");
}

export function terminologyPrompt(job: ClaimedJob): string {
  return `# Existing glossary (do not repeat these)
${formatGlossary(job.glossary)}

# Chapter
${chapterHeader(job)}

<source>
${job.sourceText}
</source>`;
}

export function translatePrompt(
  job: ClaimedJob,
  glossary: GlossaryTerm[],
  chunk: { text: string; index: number; total: number; previousTail?: string },
): string {
  const parts = [
    seriesContext(job),
    `\n# Glossary (mandatory renderings)\n${formatGlossary(glossary)}`,
    `\n# Chapter to translate\n${chapterHeader(job)}`,
  ];
  if (chunk.total > 1) {
    parts.push(`\nThis chapter is split into ${chunk.total} parts. You are translating part ${chunk.index + 1}.`);
    if (chunk.previousTail) {
      parts.push(`\n# End of the previous part (already translated, for continuity only, do not repeat)\n${chunk.previousTail}`);
    }
  }
  parts.push(`\n<source>\n${chunk.text}\n</source>`);
  return parts.join("\n");
}

export function reviewPrompt(job: ClaimedJob, glossary: GlossaryTerm[], title: string, translation: string): string {
  return `${seriesContext(job)}

# Glossary
${formatGlossary(glossary)}

# Source chapter
${chapterHeader(job)}

<source>
${job.sourceText}
</source>

# Translation to review
<translation>
Title: ${title}

${translation}
</translation>`;
}

export function revisePrompt(
  job: ClaimedJob,
  glossary: GlossaryTerm[],
  title: string,
  draft: string,
  issues: ReviewIssue[],
): string {
  const issueList = issues
    .map((i, n) =>
      `${n + 1}. [${i.severity}/${i.type}] source: "${i.sourceExcerpt}" | draft: "${i.translationExcerpt}" | fix: ${i.suggestion}`)
    .join("\n");

  return `${seriesContext(job)}

# Glossary (mandatory renderings)
${formatGlossary(glossary)}

# Source chapter
${chapterHeader(job)}

<source>
${job.sourceText}
</source>

# Your draft translation
<draft>
Title: ${title}

${draft}
</draft>

# Editor's review: fix every issue below
${issueList}

Return the complete corrected translation in the required output format. Fix every listed issue, re-check the whole chapter for omissions, and keep everything that was already correct.`;
}

// ── JSON schemas (structured output) ─────────────────────────────────────────

export const terminologySchema = {
  type: "object",
  additionalProperties: false,
  required: ["terms"],
  properties: {
    terms: {
      type: "array",
      items: {
        type: "object",
        additionalProperties: false,
        required: ["sourceTerm", "translation", "category", "notes"],
        properties: {
          sourceTerm: { type: "string" },
          translation: { type: "string" },
          category: {
            type: "string",
            enum: ["character", "place", "organization", "technique", "item", "rank", "term", "other"],
          },
          notes: { type: "string" },
        },
      },
    },
  },
} as const;

export const reviewSchema = {
  type: "object",
  additionalProperties: false,
  required: ["score", "issues", "summary"],
  properties: {
    score: { type: "number" },
    summary: { type: "string" },
    issues: {
      type: "array",
      items: {
        type: "object",
        additionalProperties: false,
        required: ["type", "severity", "sourceExcerpt", "translationExcerpt", "suggestion"],
        properties: {
          type: {
            type: "string",
            enum: ["omission", "mistranslation", "glossary", "untranslated", "addition", "fluency", "formatting"],
          },
          severity: { type: "string", enum: ["critical", "major", "minor"] },
          sourceExcerpt: { type: "string" },
          translationExcerpt: { type: "string" },
          suggestion: { type: "string" },
        },
      },
    },
  },
} as const;
