import { describe, expect, test } from "bun:test";
import { chunkSource, normalizeBody, parseTranslation, validateTranslation } from "../src/text";

describe("parseTranslation", () => {
  test("splits title line from body and normalizes paragraphs", () => {
    const r = parseTranslation("Title: The Lin Clan\n\nFirst paragraph.\n\n\n\nSecond paragraph.  \n");
    expect(r.title).toBe("The Lin Clan");
    expect(r.body).toBe("First paragraph.\n\nSecond paragraph.");
  });

  test("drops a chatty preamble and code fences", () => {
    const r = parseTranslation("```\nHere is the translation:\nTitle: **Awakening**\n\nHe woke up.\n```");
    expect(r.title).toBe("Awakening");
    expect(r.body).toBe("He woke up.");
  });

  test("strips a redundant 'Chapter N' prefix but keeps a title that is only that", () => {
    expect(parseTranslation("Title: Chapter 1: The Youth of Qingyang Town\n\nx").title).toBe("The Youth of Qingyang Town");
    expect(parseTranslation("Title: Ch. 12 - The Duel\n\nx").title).toBe("The Duel");
    expect(parseTranslation("Title: Chapter 3\n\nx").title).toBe("Chapter 3");
  });

  test("no title line → null title, full body kept", () => {
    const r = parseTranslation("He woke up.\n\nShe left.");
    expect(r.title).toBeNull();
    expect(r.body).toBe("He woke up.\n\nShe left.");
  });
});

describe("chunkSource", () => {
  test("keeps short chapters whole", () => {
    expect(chunkSource("一\n二\n三", 100)).toHaveLength(1);
  });

  test("splits on paragraph boundaries without losing text", () => {
    const paras = Array.from({ length: 10 }, (_, i) => `段落${i}`.repeat(10));
    const chunks = chunkSource(paras.join("\n"), 120);
    expect(chunks.length).toBeGreaterThan(1);
    expect(chunks.join("\n").split("\n")).toEqual(paras);
  });
});

describe("validateTranslation", () => {
  const source = ["林动看着天空。", "他笑了。", "“走吧。”", "他们离开了青阳镇。"].join("\n");
  const glossary = [
    { sourceTerm: "林动", translation: "Lin Dong", category: "character" },
    { sourceTerm: "青阳镇", translation: "Qingyang Town", category: "place" },
  ];

  test("a faithful translation passes", () => {
    const body = normalizeBody(
      "Lin Dong looked up at the sky.\n\nHe smiled.\n\n\"Let's go.\"\n\nThey left Qingyang Town.",
    );
    const r = validateTranslation(source, body, "zh", glossary);
    expect(r.flags).toEqual([]);
    expect(r.metrics.paragraphRatio).toBe(1);
  });

  test("flags dropped paragraphs", () => {
    const r = validateTranslation(source, "Lin Dong looked up at the sky and they left Qingyang Town.", "zh", glossary);
    expect(r.flags.some(f => f.includes("paragraphs survived"))).toBe(true);
  });

  test("flags leftover source script", () => {
    const body = "Lin Dong looked up.\n\n他笑了。他笑了。他笑了。\n\n\"Let's go.\"\n\nThey left Qingyang Town.";
    const r = validateTranslation(source, body, "zh", glossary);
    expect(r.flags.some(f => f.includes("Untranslated"))).toBe(true);
  });

  test("glossary misses are warnings, not flags", () => {
    const body = "He looked up at the sky.\n\nHe smiled.\n\n\"Let's go.\"\n\nThey left the little town behind.";
    const r = validateTranslation(source, body, "zh", glossary);
    expect(r.flags).toEqual([]);
    expect(r.warnings.some(w => w.includes("Lin Dong"))).toBe(true);
  });
});
