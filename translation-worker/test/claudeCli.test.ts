import { describe, expect, test } from "bun:test";
import { ClaudeCliBackend, parseClaudeOutput } from "../src/backends/claudeCli";
import { RefusalError, UsageLimitError } from "../src/backends/types";

describe("ClaudeCliBackend.buildArgs", () => {
  test("isolates the run from user settings, hooks, MCP and tools", () => {
    const b = new ClaudeCliBackend({ bin: "claude", models: { translate: "opus", assist: "sonnet" }, timeoutMs: 1000 });
    const args = b.buildArgs({ system: "S", prompt: "P", tier: "assist", jsonSchema: { type: "object" } });

    // empty-string values must stay as their own argv entries
    expect(args[args.indexOf("--setting-sources") + 1]).toBe("");
    expect(args[args.indexOf("--tools") + 1]).toBe("");
    expect(args).toContain("--strict-mcp-config");
    expect(args).toContain("--no-session-persistence");
    expect(args[args.indexOf("--model") + 1]).toBe("sonnet");
    expect(args[args.indexOf("--json-schema") + 1]).toBe('{"type":"object"}');
    expect(args).not.toContain("P"); // the prompt goes through stdin, not argv
  });
});

describe("parseClaudeOutput", () => {
  test("plain text result", () => {
    const r = parseClaudeOutput(JSON.stringify({ result: "Hello", modelUsage: { "claude-opus-5": {} } }), "", 0, "opus", false);
    expect(r).toEqual({ text: "Hello", model: "claude-opus-5" });
  });

  test("structured output", () => {
    const out = JSON.stringify({ result: '{"a":1}', structured_output: { a: 1 }, modelUsage: {} });
    expect(parseClaudeOutput(out, "", 0, "sonnet", true).json).toEqual({ a: 1 });
  });

  test("usage limit in an error result → UsageLimitError with reset time", () => {
    const reset = Math.floor(Date.now() / 1000) + 3600;
    const out = JSON.stringify({ is_error: true, result: `Claude AI usage limit reached|${reset}` });
    try {
      parseClaudeOutput(out, "", 1, "opus", false);
      throw new Error("expected throw");
    } catch (e) {
      expect(e).toBeInstanceOf(UsageLimitError);
      const ms = (e as UsageLimitError).retryAfterMs!;
      expect(ms).toBeGreaterThan(3500_000);
      expect(ms).toBeLessThan(3700_000);
    }
  });

  test("429 status → UsageLimitError", () => {
    const out = JSON.stringify({ is_error: true, api_error_status: 429, result: "Too many" });
    expect(() => parseClaudeOutput(out, "", 1, "opus", false)).toThrow(UsageLimitError);
  });

  test("refusal → RefusalError", () => {
    const out = JSON.stringify({ result: "", stop_reason: "refusal" });
    expect(() => parseClaudeOutput(out, "", 0, "opus", false)).toThrow(RefusalError);
  });

  test("non-JSON crash → plain error", () => {
    expect(() => parseClaudeOutput("segfault", "boom", 1, "opus", false)).toThrow(/without JSON/);
  });
});
