// A backend is "something that turns (system, prompt) into text or JSON".
// The pipeline never knows whether that is a Claude Code subscription, Codex, or the API.

export type ModelTier = "translate" | "assist";

export interface LlmRequest {
  /** Stable instructions. Keep it short: CLI backends pass it on the command line. */
  system: string;
  /** Everything variable (glossary, context, source text) goes here. */
  prompt: string;
  tier: ModelTier;
  /** When set, the backend must return parsed JSON matching this schema in `json`. */
  jsonSchema?: Record<string, unknown>;
  signal?: AbortSignal;
}

export interface LlmResult {
  text: string;
  json?: unknown;
  /** Concrete model that served the request, for the QA report. */
  model: string;
}

export interface LlmBackend {
  readonly name: string;
  complete(req: LlmRequest): Promise<LlmResult>;
}

/**
 * The subscription / API quota is exhausted. Not the job's fault:
 * the worker keeps its lease, waits, and retries the same job.
 */
export class UsageLimitError extends Error {
  constructor(message: string, readonly retryAfterMs?: number) {
    super(message);
    this.name = "UsageLimitError";
  }
}

/** The model declined to translate this content. Retrying the same text won't help. */
export class RefusalError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "RefusalError";
  }
}
