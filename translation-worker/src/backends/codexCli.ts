import { mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import type { LlmBackend, LlmRequest, LlmResult, ModelTier } from "./types";
import { UsageLimitError } from "./types";

export interface CodexCliOptions {
  bin: string;
  models: Record<ModelTier, string>;
  timeoutMs: number;
}

/**
 * EXPERIMENTAL / UNTESTED — codex was not installed on the machine this was written on.
 * Runs `codex exec` headless on the machine's ChatGPT login. Verify flags against
 * `codex exec --help` for your installed version before relying on it.
 */
export class CodexCliBackend implements LlmBackend {
  readonly name = "codex-cli";
  private readonly cwd = mkdtempSync(join(tmpdir(), "ff-tl-codex-"));

  constructor(private readonly opts: CodexCliOptions) {}

  async complete(req: LlmRequest): Promise<LlmResult> {
    const lastMessageFile = join(this.cwd, `last-${crypto.randomUUID()}.txt`);
    const args = [
      this.opts.bin, "exec",
      "--skip-git-repo-check",
      "--sandbox", "read-only",
      "-m", this.opts.models[req.tier],
      "--output-last-message", lastMessageFile,
    ];
    if (req.jsonSchema) {
      const schemaFile = join(this.cwd, `schema-${crypto.randomUUID()}.json`);
      writeFileSync(schemaFile, JSON.stringify(req.jsonSchema));
      args.push("--output-schema", schemaFile);
    }
    args.push("-"); // read the prompt from stdin

    const env: Record<string, string | undefined> = { ...process.env };
    delete env.CLAUDECODE;

    const proc = Bun.spawn(args, {
      cwd: this.cwd,
      env,
      // codex has no separate system-prompt flag in exec mode; prepend it
      stdin: new TextEncoder().encode(`${req.system}\n\n---\n\n${req.prompt}`),
      stdout: "pipe",
      stderr: "pipe",
    });
    const timer = setTimeout(() => proc.kill(), this.opts.timeoutMs);
    req.signal?.addEventListener("abort", () => proc.kill(), { once: true });

    try {
      const [stdout, stderr, exitCode] = await Promise.all([
        new Response(proc.stdout).text(),
        new Response(proc.stderr).text(),
        proc.exited,
      ]);
      const blob = `${stdout}\n${stderr}`;
      if (exitCode !== 0) {
        if (/usage limit|rate limit|429/i.test(blob)) throw new UsageLimitError(blob.slice(0, 500), 15 * 60_000);
        throw new Error(`codex exited ${exitCode}: ${blob.slice(-800)}`);
      }
      const text = readFileSync(lastMessageFile, "utf8").trim();
      const model = `codex:${this.opts.models[req.tier]}`;
      return req.jsonSchema ? { text, json: JSON.parse(text), model } : { text, model };
    } finally {
      clearTimeout(timer);
    }
  }
}
