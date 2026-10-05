import Anthropic from "@anthropic-ai/sdk";
import type { LlmBackend, LlmRequest, LlmResult, ModelTier } from "./types";
import { RefusalError, UsageLimitError } from "./types";

export interface AnthropicApiOptions {
  models: Record<ModelTier, string>;
  effort: "low" | "medium" | "high" | "xhigh" | "max";
}

/**
 * Claude API backend — the production path once volume grows.
 * Streams (chapters can produce long outputs), caches the system prompt,
 * and opts into server-side refusal fallbacks.
 */
export class AnthropicApiBackend implements LlmBackend {
  readonly name = "anthropic-api";
  // Resolves ANTHROPIC_API_KEY / ANTHROPIC_AUTH_TOKEN / `ant auth login` profile from the environment.
  private readonly client = new Anthropic({ maxRetries: 4 });

  constructor(private readonly opts: AnthropicApiOptions) {}

  async complete(req: LlmRequest): Promise<LlmResult> {
    const model = this.opts.models[req.tier];
    const outputConfig: Record<string, unknown> = { effort: this.opts.effort };
    if (req.jsonSchema) outputConfig.format = { type: "json_schema", schema: req.jsonSchema };

    let message: any;
    try {
      const stream = this.client.beta.messages.stream(
        {
          model,
          max_tokens: 64000,
          system: [{ type: "text", text: req.system, cache_control: { type: "ephemeral" } }],
          messages: [{ role: "user", content: req.prompt }],
          output_config: outputConfig,
          betas: ["server-side-fallback-2026-07-01"],
          fallbacks: "default",
        } as any,
        { signal: req.signal },
      );
      message = await stream.finalMessage();
    } catch (error) {
      if (error instanceof Anthropic.RateLimitError) {
        const retryAfter = Number(error.headers?.get?.("retry-after"));
        throw new UsageLimitError(error.message, Number.isFinite(retryAfter) ? retryAfter * 1000 : 60_000);
      }
      if (error instanceof Anthropic.APIError && error.status === 529) {
        throw new UsageLimitError("Anthropic API overloaded", 60_000);
      }
      throw error;
    }

    if (message.stop_reason === "refusal") {
      throw new RefusalError(message.stop_details?.explanation ?? "Model refused the request");
    }
    if (message.stop_reason === "max_tokens") {
      throw new Error("Output hit max_tokens — chapter chunk too large");
    }

    const text = message.content
      .filter((b: any) => b.type === "text")
      .map((b: any) => b.text)
      .join("");

    if (req.jsonSchema) {
      return { text, json: JSON.parse(text), model: message.model ?? model };
    }
    return { text, model: message.model ?? model };
  }
}
