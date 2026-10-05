// Pure text helpers: parsing model output, chunking, and the deterministic QA validators.
import type { GlossaryTerm } from "./types";

/** Source paragraphs: scrapers usually emit one paragraph per line, sometimes with blank lines between. */
export function sourceParagraphs(text: string): string[] {
  return text.replace(/\r\n?/g, "\n").split("\n").map(l => l.trim()).filter(Boolean);
}

/** Output paragraphs: we instruct one paragraph per blank-line-separated block. */
export function outputParagraphs(text: string): string[] {
  return text.replace(/\r\n?/g, "\n").split(/\n\s*\n/).map(p => p.trim()).filter(Boolean);
}

/** Normalizes model output to the site's canonical format: paragraphs separated by one blank line. */
export function normalizeBody(text: string): string {
  return outputParagraphs(text)
    .map(p => p.split("\n").map(l => l.trim()).filter(Boolean).join("\n"))
    .join("\n\n");
}

const PREAMBLE = /^(here('s| is)|sure[,!.]|certainly[,!.]|below is)/i;

/** Splits "Title: ...\n\nbody" output. Tolerates code fences and a chatty first line. */
export function parseTranslation(raw: string): { title: string | null; body: string } {
  let text = raw.replace(/\r\n?/g, "\n").trim();
  text = text.replace(/^```[a-z]*\n/i, "").replace(/\n```$/, "").trim();

  const lines = text.split("\n");
  while (lines.length > 0 && (lines[0].trim() === "" || PREAMBLE.test(lines[0].trim()))) lines.shift();

  let title: string | null = null;
  const m = lines[0]?.match(/^\s*\**\s*title\s*\**\s*[:：]\s*(.+?)\s*\**\s*$/i);
  if (m) {
    title = stripChapterPrefix(m[1].trim().replace(/^\*+|\*+$/g, "").trim().replace(/^["“]|["”]$/g, ""));
    lines.shift();
  }
  return { title, body: normalizeBody(lines.join("\n")) };
}

/** The site already renders "Ch.N - <name>", so "Chapter 12: The Duel" must become "The Duel". */
export function stripChapterPrefix(title: string): string {
  const stripped = title.replace(/^(chapter|ch\.?)\s*\d+\s*[:：.\-–—]?\s*/i, "").trim();
  return stripped || title;
}

/** Splits a long source chapter on paragraph boundaries into parts of at most ~maxChars. */
export function chunkSource(text: string, maxChars: number): string[] {
  const paragraphs = sourceParagraphs(text);
  const chunks: string[] = [];
  let current: string[] = [];
  let size = 0;
  for (const p of paragraphs) {
    if (size > 0 && size + p.length > maxChars) {
      chunks.push(current.join("\n"));
      current = [];
      size = 0;
    }
    current.push(p);
    size += p.length + 1;
  }
  if (current.length > 0) chunks.push(current.join("\n"));
  return chunks;
}

// ── Validators ──────────────────────────────────────────────────────────────

const CJK = /[぀-ヿ㐀-䶿一-鿿가-힯豈-﫿]/g;

export function isCjkLanguage(lang: string): boolean {
  return ["zh", "ja", "ko"].includes(lang.toLowerCase());
}

export interface ValidationReport {
  /** Problems serious enough to hold the chapter for human review. */
  flags: string[];
  /** Worth recording, not worth blocking on. */
  warnings: string[];
  metrics: {
    sourceParagraphs: number;
    outputParagraphs: number;
    paragraphRatio: number;
    lengthRatio: number;
    cjkResidue: number;
    glossaryMissing: string[];
  };
}

export function validateTranslation(
  source: string,
  body: string,
  lang: string,
  glossary: GlossaryTerm[],
): ValidationReport {
  const flags: string[] = [];
  const warnings: string[] = [];

  const srcParas = sourceParagraphs(source).length;
  const outParas = outputParagraphs(body).length;
  const paragraphRatio = srcParas === 0 ? 1 : outParas / srcParas;

  const srcChars = source.replace(/\s/g, "").length;
  const outChars = body.replace(/\s/g, "").length;
  const lengthRatio = srcChars === 0 ? 1 : outChars / srcChars;

  const cjkCount = (body.match(CJK) ?? []).length;
  const cjkResidue = outChars === 0 ? 0 : cjkCount / outChars;

  const cjk = isCjkLanguage(lang);

  // Silent omissions are the #1 failure mode of LLM translation: paragraphs vanish or get summarized.
  if (paragraphRatio < 0.6) flags.push(`Only ${outParas}/${srcParas} paragraphs survived (possible omissions).`);
  else if (paragraphRatio < 0.85) warnings.push(`Paragraph count dropped: ${outParas}/${srcParas}.`);
  if (paragraphRatio > 1.8) warnings.push(`Paragraph count grew: ${outParas}/${srcParas}.`);

  // CJK → English typically lands around 2.5–4.5 English chars per source char.
  const [lowHard, lowSoft, highSoft] = cjk ? [1.2, 1.8, 6.5] : [0.45, 0.65, 1.9];
  if (lengthRatio < lowHard) flags.push(`Translation is suspiciously short (length ratio ${lengthRatio.toFixed(2)}).`);
  else if (lengthRatio < lowSoft) warnings.push(`Translation is short (length ratio ${lengthRatio.toFixed(2)}).`);
  if (lengthRatio > highSoft) warnings.push(`Translation is long (length ratio ${lengthRatio.toFixed(2)}).`);

  if (cjk) {
    if (cjkResidue > 0.01) flags.push(`Untranslated source text left (${cjkCount} CJK characters).`);
    else if (cjkCount > 0) warnings.push(`${cjkCount} CJK characters left in the translation.`);
  }

  const lowerBody = body.toLowerCase();
  const glossaryMissing = glossary
    .filter(t => source.includes(t.sourceTerm) && !lowerBody.includes(t.translation.toLowerCase()))
    .map(t => `${t.sourceTerm} -> ${t.translation}`);
  // Names legitimately turn into pronouns, so a miss is only a warning; real glossary
  // violations are caught (with judgment) by the review pass.
  if (glossaryMissing.length > 0) warnings.push(`Glossary terms not used: ${glossaryMissing.join("; ")}`);

  return {
    flags,
    warnings,
    metrics: { sourceParagraphs: srcParas, outputParagraphs: outParas, paragraphRatio, lengthRatio, cjkResidue, glossaryMissing },
  };
}
