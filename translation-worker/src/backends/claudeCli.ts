import { mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import type { LlmBackend, LlmRequest, LlmResult, ModelTier } from "./types";
import { RefusalError, UsageLimitError } from "./types";

export interface ClaudeCliOptions {
  bin: string;
  models: Record<ModelTier, string>;
  timeoutMs: number;
}

const DEFAULT_LIMIT_BACKOFF_MS = 15 * 60_000;

/**
 * Runs `claude -p` headless on this machine's Claude Code login (Pro/Max subscription).
 *
 * Isolation matters: without these flags every call would load the user's CLAUDE.md,
 * hooks, plugins and MCP servers, and the model could reach for tools.
 *  --setting-sources ""   no user/project/local settings → no hooks, no CLAUDE.md
 *  --strict-mcp-config    no MCP servers
 *  --tools ""             no tools at all — pure text in, text out
 * (--bare would be cleaner but it disables OAuth, i.e. the subscription.)
 */
export class ClaudeCliBackend implements LlmBackend {
  readonly name = "claude-cli";
  private readonly cwd = mkdtempSync(join(tmpdir(), "ff-tl-claude-"));

  constructor(private readonly opts: ClaudeCliOptions) {}

  buildArgs(req: LlmRequest): string[] {
    const args = [
      this.opts.bin,
      "-p",
      "--setting-sources", "",
      "--strict-mcp-config",
      "--tools", "",
      "--no-session-persistence",
      "--model", this.opts.models[req.tier],
      "--system-prompt", req.system,
      "--output-format", "json",
    ];
    if (req.jsonSchema) args.push("--json-schema", JSON.stringify(req.jsonSchema));
    return args;
  }

  async complete(req: LlmRequest): Promise<LlmResult> {
    const env: Record<string, string | undefined> = { ...process.env };
    // Behave the same whether the worker was started from a terminal or from inside a Claude session
    delete env.CLAUDECODE;
    delete env.CLAUDE_CODE_ENTRYPOINT;

    const proc = Bun.spawn(this.buildArgs(req), {
      cwd: this.cwd,
      env,
      stdin: new TextEncoder().encode(req.prompt),
      stdout: "pipe",
      stderr: "pipe",
    });

    const timer = setTimeout(() => proc.kill(), this.opts.timeoutMs);
    const onAbort = () => proc.kill();
    req.signal?.addEventListener("abort", onAbort, { once: true });

    try {
      const [stdout, stderr, exitCode] = await Promise.all([
        new Response(proc.stdout).text(),
        new Response(proc.stderr).text(),
        proc.exited,
      ]);
      if (req.signal?.aborted) throw new Error("Aborted");

      return parseClaudeOutput(stdout, stderr, exitCode, this.opts.models[req.tier], !!req.jsonSchema);
    } finally {
      clearTimeout(timer);
      req.signal?.removeEventListener("abort", onAbort);
    }
  }
}

const LIMIT_PATTERN = /usage limit|limit reached|hit your limit|rate.?limit|too many requests|\b429\b/i;

export function parseClaudeOutput(
  stdout: string,
  stderr: string,
  exitCode: number,
  requestedModel: string,
  expectJson: boolean,
): LlmResult {
  let parsed: any;
  try {
    parsed = JSON.parse(stdout);
  } catch {
    const blob = `${stdout}\n${stderr}`.trim();
    if (LIMIT_PATTERN.test(blob)) throw new UsageLimitError(blob.slice(0, 500), parseResetMs(blob));
    throw new Error(`claude exited ${exitCode} without JSON output: ${blob.slice(0, 800)}`);
  }

  const resultText: string = typeof parsed.result === "string" ? parsed.result : "";
  if (parsed.is_error) {
    const blob = `${resultText} ${parsed.api_error_status ?? ""} ${stderr}`;
    if (parsed.api_error_status === 429 || LIMIT_PATTERN.test(blob)) {
      throw new UsageLimitError(resultText || "Claude usage limit reached", parseResetMs(blob));
    }
    throw new Error(`claude returned an error: ${blob.trim().slice(0, 800)}`);
  }
  if (parsed.stop_reason === "refusal") throw new RefusalError(resultText || "Model refused the request");

  const model = Object.keys(parsed.modelUsage ?? {})[0] ?? requestedModel;

  if (expectJson) {
    let json = parsed.structured_output;
    if (json === undefined) {
      try { json = JSON.parse(resultText); } catch { /* handled below */ }
    }
    if (json === undefined || json === null) throw new Error("claude returned no structured output");
    return { text: resultText, json, model };
  }

  return { text: resultText, model };
}

// Claude Code has printed resets as "...limit reached|1735689600" (epoch seconds); fall back to a fixed backoff.
function parseResetMs(blob: string): number {
  const epoch = blob.match(/\|(\d{10})\b/);
  if (epoch) {
    const ms = Number(epoch[1]) * 1000 - Date.now();
    if (ms > 0 && ms < 24 * 3600_000) return ms + 30_000;
  }
  return DEFAULT_LIMIT_BACKOFF_MS;
}
