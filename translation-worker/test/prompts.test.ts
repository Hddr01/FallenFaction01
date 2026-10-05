import { describe, expect, test } from "bun:test";
import { continuationSystem, reviewSystem, terminologySystem, translatorSystem } from "../src/prompts";

describe("target language", () => {
  test("defaults to English", () => {
    expect(translatorSystem("zh")).toBe(translatorSystem("zh", "en"));
    expect(translatorSystem("zh")).toContain("polished, natural English for an English-speaking readership");
    expect(translatorSystem("zh")).not.toContain("inflected");
  });

  test("Polish target rewrites every prompt", () => {
    const prompts = [translatorSystem("en", "pl"), continuationSystem("en", "pl"), terminologySystem("en", "pl"), reviewSystem("en", "pl")];
    for (const p of prompts) expect(p).toContain("Polish");
    expect(prompts[0]).toContain("„…”");
    expect(prompts[0]).toContain("Title: <Polish chapter title");
    expect(prompts[0]).not.toContain("English double quotes");
    expect(prompts[1]).toContain("only the Polish text");
    expect(prompts[2]).toContain("English-to-Polish");
    expect(prompts[2]).toContain("keep them exactly as written");
    expect(prompts[2]).not.toContain("pinyin");
  });
});
