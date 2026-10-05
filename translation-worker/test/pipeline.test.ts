import { describe, expect, test } from "bun:test";
import type { LlmBackend, LlmRequest, LlmResult } from "../src/backends/types";
import { UsageLimitError } from "../src/backends/types";
import { translateJob } from "../src/pipeline";
import type { ClaimedJob } from "../src/types";

const job: ClaimedJob = {
  jobId: 1, titleId: 7, titleName: "Martial Universe", originalTitle: "武动乾坤",
  volumeNumber: 1, chapterNumber: 3, sourceChapterTitle: "第三章 青阳镇",
  sourceText: "林动看着天空。\n他笑了。\n“走吧。”\n他们离开了青阳镇。",
  sourceLanguage: "zh", styleGuide: "Keep cultivation terms in English.", synopsis: "A youth rises.",
  leaseExpiresAt: new Date().toISOString(), attempt: 1,
  glossary: [{ sourceTerm: "林动", translation: "Lin Dong", category: "character", isLocked: true }],
  previousChapters: [{ volumeNumber: 1, chapterNumber: 2, title: "Stone", summary: "Lin Dong found a stone." }],
};

const GOOD = "Title: Qingyang Town\n\nLin Dong looked up at the sky.\n\nHe smiled.\n\n\"Let's go.\"\n\nThey left Qingyang Town.";

/** Scripted backend: answers by inspecting which pass is calling. */
function fakeBackend(scores: number[], opts: { draft?: string; revised?: string } = {}) {
  const calls: string[] = [];
  const prompts: LlmRequest[] = [];
  const backend: LlmBackend = {
    name: "fake",
    async complete(req: LlmRequest): Promise<LlmResult> {
      prompts.push(req);
      if (req.system.includes("terminology glossary")) {
        calls.push("terms");
        return { text: "", model: "m-assist", json: { terms: [
          { sourceTerm: "青阳镇", translation: "Qingyang Town", category: "place", notes: "" },
          { sourceTerm: "林动", translation: "Lin Dong", category: "character", notes: "" }, // already known
          { sourceTerm: "不存在", translation: "Ghost", category: "term", notes: "" },       // not in the text
        ] } };
      }
      if (req.system.includes("exacting editor")) {
        calls.push("review");
        const score = scores.shift() ?? 9;
        const issues = score < 8 ? [{ type: "omission", severity: "major", sourceExcerpt: "他笑了", translationExcerpt: "", suggestion: "add it" }] : [];
        return { text: "", model: "m-assist", json: { score, issues, summary: "Lin Dong leaves town." } };
      }
      if (req.prompt.includes("Editor's review")) {
        calls.push("revise");
        return { text: opts.revised ?? GOOD, model: "m-translate" };
      }
      calls.push("translate");
      return { text: opts.draft ?? GOOD, model: "m-translate" };
    },
  };
  return { backend, calls, prompts };
}

const opts = { maxChunkChars: 8000, reviseBelow: 8.5 };

describe("translateJob", () => {
  test("good draft: terms → translate → review, no revision, publishable", async () => {
    const { backend, calls, prompts } = fakeBackend([9.2]);
    const r = await translateJob(job, backend, opts);

    expect(calls).toEqual(["terms", "translate", "review"]);
    expect(r.translatedTitle).toBe("Qingyang Town");
    expect(r.qaScore).toBe(9.2);
    expect(r.qaFlagged).toBe(false);
    expect(r.summary).toBe("Lin Dong leaves town.");
    // only the genuinely new term that occurs in the text survives
    expect(r.newGlossaryTerms.map(t => t.sourceTerm)).toEqual(["青阳镇"]);
    expect(r.modelInfo).toContain("fake:");

    // the translate prompt carries the whole context
    const translate = prompts[1].prompt;
    expect(translate).toContain("林动 -> Lin Dong");
    expect(translate).toContain("LOCKED");
    expect(translate).toContain("Qingyang Town");             // new term already applied
    expect(translate).toContain("Lin Dong found a stone.");   // story so far
    expect(translate).toContain("Keep cultivation terms");    // style guide
    expect(prompts[1].tier).toBe("translate");
    expect(prompts[2].tier).toBe("assist");
  });

  test("weak draft triggers revise + re-review and keeps the better version", async () => {
    const weak = "Title: Town\n\nLin Dong looked up at the sky.\n\n\"Let's go.\"\n\nThey left Qingyang Town.";
    const { backend, calls } = fakeBackend([6, 9], { draft: weak });
    const r = await translateJob(job, backend, opts);

    expect(calls).toEqual(["terms", "translate", "review", "revise", "review"]);
    expect(r.qaScore).toBe(9);
    expect(r.content).toContain("He smiled.");
    expect(JSON.parse(r.qaReport!).revised).toBe(true);
  });

  test("revision that scores worse is discarded", async () => {
    const { backend } = fakeBackend([7, 5], { revised: "Title: X\n\nworse" });
    const r = await translateJob(job, backend, opts);
    expect(r.qaScore).toBe(7);
    expect(r.translatedTitle).toBe("Qingyang Town");
    expect(JSON.parse(r.qaReport!).revised).toBe(false);
  });

  test("validator problems flag the chapter for human review", async () => {
    const lossy = "Title: T\n\nLin Dong looked up and they all left Qingyang Town together, smiling.";
    const { backend } = fakeBackend([9.5], { draft: lossy });
    const r = await translateJob(job, backend, opts);
    expect(r.qaFlagged).toBe(true);
    expect(JSON.parse(r.qaReport!).validation.flags.length).toBeGreaterThan(0);
  });

  test("usage limits propagate instead of being swallowed by the terminology pass", async () => {
    const backend: LlmBackend = {
      name: "limited",
      async complete() { throw new UsageLimitError("limit", 1000); },
    };
    await expect(translateJob(job, backend, opts)).rejects.toBeInstanceOf(UsageLimitError);
  });
});
