import { readdirSync, readFileSync, statSync } from "node:fs";
import { basename, extname, join } from "node:path";

export interface RawChapter {
  volumeNumber: number;
  chapterNumber: number;
  sourceChapterTitle?: string;
  sourceText: string;
}

/**
 * A folder of .txt files, one chapter per file (any scraper can produce this).
 * Chapter number = first number in the filename ("0012.txt", "chapter-12.txt", "c12_title.txt").
 * First line = chapter title, unless `firstLineIsTitle` is false.
 */
export function readTxtFolder(dir: string, opts: { volume: number; firstLineIsTitle: boolean }): RawChapter[] {
  const files = readdirSync(dir)
    .filter(f => extname(f).toLowerCase() === ".txt" && statSync(join(dir, f)).isFile())
    .sort((a, b) => a.localeCompare(b, undefined, { numeric: true }));

  const seen = new Set<number>();
  return files.map((file, index) => {
    const num = basename(file).match(/\d+/);
    const chapterNumber = num ? Number(num[0]) : index + 1;
    if (seen.has(chapterNumber)) throw new Error(`Two files map to chapter ${chapterNumber} (latest: ${file})`);
    seen.add(chapterNumber);

    const text = readFileSync(join(dir, file), "utf8").replace(/^﻿/, "").replace(/\r\n?/g, "\n");
    const lines = text.split("\n");
    let title: string | undefined;
    if (opts.firstLineIsTitle) {
      while (lines.length && !lines[0].trim()) lines.shift();
      const first = lines[0]?.trim();
      if (first && first.length <= 120 && lines.length > 1) {
        title = first;
        lines.shift();
      }
    }
    return { volumeNumber: opts.volume, chapterNumber, sourceChapterTitle: title, sourceText: lines.join("\n").trim() };
  });
}

/** JSON array: [{ "chapter": 1, "volume"?: 1, "title"?: "...", "text": "..." }] */
export function readJsonFile(path: string, defaultVolume: number): RawChapter[] {
  const data = JSON.parse(readFileSync(path, "utf8").replace(/^﻿/, ""));
  if (!Array.isArray(data)) throw new Error("JSON input must be an array of chapters");
  return data.map((c: any, i: number) => {
    const chapterNumber = Number(c.chapter ?? c.chapterNumber ?? i + 1);
    const text = String(c.text ?? c.body ?? c.content ?? "");
    if (!text.trim()) throw new Error(`Chapter ${chapterNumber} has no text`);
    return {
      volumeNumber: Number(c.volume ?? c.volumeNumber ?? defaultVolume),
      chapterNumber,
      sourceChapterTitle: c.title ?? undefined,
      sourceText: stripHtml(text),
    };
  });
}

/**
 * Splits chapters into enqueue requests: at most 50 chapters and ~4 MB of JSON each
 * (the server caps request bodies at 10 MB; CJK text is ~3 bytes per character).
 */
export function batchChapters(chapters: RawChapter[], maxCount = 50, maxBytes = 4 * 1024 * 1024): RawChapter[][] {
  const batches: RawChapter[][] = [];
  let current: RawChapter[] = [];
  let bytes = 0;
  for (const c of chapters) {
    const size = Buffer.byteLength(JSON.stringify(c), "utf8");
    if (size > maxBytes) throw new Error(`Chapter ${c.chapterNumber} alone is ${(size / 1048576).toFixed(1)} MB; split it first.`);
    if (current.length > 0 && (current.length >= maxCount || bytes + size > maxBytes)) {
      batches.push(current);
      current = [];
      bytes = 0;
    }
    current.push(c);
    bytes += size;
  }
  if (current.length > 0) batches.push(current);
  return batches;
}

// Scrapers often hand back HTML paragraphs; the pipeline wants one paragraph per line.
export function stripHtml(text: string): string {
  if (!/<[a-z][\s\S]*>/i.test(text)) return text;
  return text
    .replace(/<br\s*\/?>/gi, "\n")
    .replace(/<\/p>/gi, "\n")
    .replace(/<[^>]+>/g, "")
    .replace(/&nbsp;/g, " ")
    .replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&#39;/g, "'")
    .replace(/&amp;/g, "&")
    .split("\n").map(l => l.trim()).filter(Boolean).join("\n");
}
