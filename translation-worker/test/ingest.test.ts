import { describe, expect, test } from "bun:test";
import { mkdtempSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { batchChapters, readJsonFile, readTxtFolder, stripHtml } from "../src/ingest";

describe("readTxtFolder", () => {
  test("numbers chapters from filenames, natural sort, first line as title", () => {
    const dir = mkdtempSync(join(tmpdir(), "ff-ingest-"));
    writeFileSync(join(dir, "chapter-10.txt"), "第十章 终\n\n结束了。");
    writeFileSync(join(dir, "chapter-2.txt"), "﻿第二章 起\r\n他来了。\r\n她走了。");
    writeFileSync(join(dir, "notes.md"), "ignored");

    const chapters = readTxtFolder(dir, { volume: 1, firstLineIsTitle: true });
    expect(chapters.map(c => c.chapterNumber)).toEqual([2, 10]);
    expect(chapters[0].sourceChapterTitle).toBe("第二章 起");
    expect(chapters[0].sourceText).toBe("他来了。\n她走了。");
    expect(chapters[1].sourceText).toBe("结束了。");
  });

  test("rejects two files claiming the same chapter", () => {
    const dir = mkdtempSync(join(tmpdir(), "ff-ingest-"));
    writeFileSync(join(dir, "1.txt"), "a\nb");
    writeFileSync(join(dir, "001.txt"), "a\nb");
    expect(() => readTxtFolder(dir, { volume: 1, firstLineIsTitle: false })).toThrow(/chapter 1/);
  });
});

describe("readJsonFile", () => {
  test("accepts chapter/title/text and strips HTML bodies", () => {
    const dir = mkdtempSync(join(tmpdir(), "ff-ingest-"));
    const file = join(dir, "c.json");
    writeFileSync(file, JSON.stringify([{ chapter: 5, title: "五", text: "<p>甲&amp;乙</p><p>丙</p>" }]));
    expect(readJsonFile(file, 2)).toEqual([
      { volumeNumber: 2, chapterNumber: 5, sourceChapterTitle: "五", sourceText: "甲&乙\n丙" },
    ]);
  });
});

test("stripHtml leaves plain text alone", () => {
  expect(stripHtml("a < b\nc")).toBe("a < b\nc");
});

describe("batchChapters", () => {
  const ch = (n: number, chars: number) => ({ volumeNumber: 1, chapterNumber: n, sourceText: "字".repeat(chars) });

  test("splits by count", () => {
    const batches = batchChapters(Array.from({ length: 120 }, (_, i) => ch(i + 1, 10)));
    expect(batches.map(b => b.length)).toEqual([50, 50, 20]);
  });

  test("splits by UTF-8 bytes, not characters", () => {
    // 3 chapters × 1000 CJK chars ≈ 3 KB each; a 7 KB cap fits two per batch
    const batches = batchChapters([ch(1, 1000), ch(2, 1000), ch(3, 1000)], 50, 7000);
    expect(batches.map(b => b.length)).toEqual([2, 1]);
  });

  test("rejects a single oversized chapter", () => {
    expect(() => batchChapters([ch(1, 5000)], 50, 1000)).toThrow(/split it first/);
  });
});
