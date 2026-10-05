import { AnthropicApiBackend } from "./backends/anthropicApi";
import { ClaudeCliBackend } from "./backends/claudeCli";
import { CodexCliBackend } from "./backends/codexCli";
import type { LlmBackend, ModelTier } from "./backends/types";

export type BackendName = "claude-cli" | "codex-cli" | "anthropic-api";

export interface WorkerConfig {
  apiUrl: string;
  workerKey: string;
  backend: BackendName;
  models: Record<ModelTier, string>;
  pollSeconds: number;
  maxChunkChars: number;
  reviseBelow: number;
  callTimeoutMs: number;
}

const env = (name: string, fallback?: string) => process.env[name]?.trim() || fallback;

// Per-backend model defaults. "translate" does translation + revision, "assist" does terminology + review.
const DEFAULT_MODELS: Record<BackendName, Record<ModelTier, string>> = {
  "claude-cli": { translate: "opus", assist: "sonnet" },
  "anthropic-api": { translate: "claude-opus-5", assist: "claude-sonnet-5" },
  "codex-cli": { translate: "gpt-5", assist: "gpt-5" },
};

export function loadConfig(): WorkerConfig {
  const backend = (env("FF_BACKEND", "claude-cli") as BackendName);
  if (!DEFAULT_MODELS[backend]) throw new Error(`Unknown FF_BACKEND "${backend}" (claude-cli | codex-cli | anthropic-api)`);

  return {
    apiUrl: env("FF_API_URL", "https://fallenfaction.com")!,
    workerKey: env("FF_WORKER_KEY", "")!,
    backend,
    models: {
      translate: env("FF_MODEL_TRANSLATE", DEFAULT_MODELS[backend].translate)!,
      assist: env("FF_MODEL_ASSIST", DEFAULT_MODELS[backend].assist)!,
    },
    pollSeconds: Number(env("FF_POLL_SECONDS", "30")),
    maxChunkChars: Number(env("FF_MAX_CHUNK_CHARS", "8000")),
    // Quality first: by default any reported issue triggers a revision pass (lower to e.g. 8.5 to save quota)
    reviseBelow: Number(env("FF_REVISE_BELOW", "10")),
    callTimeoutMs: Number(env("FF_CALL_TIMEOUT_MINUTES", "20")) * 60_000,
  };
}

export function createBackend(cfg: WorkerConfig): LlmBackend {
  switch (cfg.backend) {
    case "claude-cli":
      return new ClaudeCliBackend({ bin: env("FF_CLAUDE_BIN", "claude")!, models: cfg.models, timeoutMs: cfg.callTimeoutMs });
    case "codex-cli":
      return new CodexCliBackend({ bin: env("FF_CODEX_BIN", "codex")!, models: cfg.models, timeoutMs: cfg.callTimeoutMs });
    case "anthropic-api":
      return new AnthropicApiBackend({ models: cfg.models, effort: (env("FF_API_EFFORT", "high") as any) });
  }
}
