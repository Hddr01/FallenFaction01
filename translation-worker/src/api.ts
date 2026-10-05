import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join } from "node:path";
import type { ClaimedJob, CompleteJobPayload } from "./types";

export class ApiError extends Error {
  constructor(message: string, readonly status: number, readonly body: string) {
    super(message);
    this.name = "ApiError";
  }
}

const sleep = (ms: number) => new Promise(r => setTimeout(r, ms));

async function request(baseUrl: string, path: string, init: RequestInit & { retries?: number } = {}): Promise<Response> {
  const retries = init.retries ?? 4;
  for (let attempt = 0; ; attempt++) {
    let res: Response;
    try {
      res = await fetch(`${baseUrl.replace(/\/$/, "")}${path}`, init);
    } catch (error) {
      if (attempt >= retries) throw error;
      await sleep(2000 * 2 ** attempt); // network blip / server restarting during a deploy
      continue;
    }
    if ((res.status === 429 || res.status >= 502) && attempt < retries) {
      const retryAfter = Number(res.headers.get("retry-after"));
      await sleep(Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter * 1000 : 2000 * 2 ** attempt);
      continue;
    }
    if (!res.ok) {
      const body = await res.text();
      throw new ApiError(`${init.method ?? "GET"} ${path} → ${res.status}: ${body.slice(0, 500)}`, res.status, body);
    }
    return res;
  }
}

/** Calls made by a worker, authenticated with its X-Worker-Key. */
export class WorkerApi {
  constructor(private readonly baseUrl: string, private readonly key: string) {}

  private post(path: string, body?: unknown) {
    return request(this.baseUrl, path, {
      method: "POST",
      headers: { "X-Worker-Key": this.key, "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  }

  async claim(): Promise<ClaimedJob | null> {
    const res = await this.post("/api/translation-worker/claim");
    return res.status === 204 ? null : ((await res.json()) as ClaimedJob);
  }

  /** Returns false when the lease was lost (someone else owns the job now). */
  async heartbeat(jobId: number): Promise<boolean> {
    try {
      await this.post(`/api/translation-worker/jobs/${jobId}/heartbeat`);
      return true;
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) return false;
      throw error;
    }
  }

  async complete(jobId: number, payload: CompleteJobPayload): Promise<{ status: string; chapterId?: number; glossaryTermsAdded: number }> {
    const res = await this.post(`/api/translation-worker/jobs/${jobId}/complete`, payload);
    return (await res.json()) as any;
  }

  async fail(jobId: number, error: string, countAttempt: boolean): Promise<void> {
    await this.post(`/api/translation-worker/jobs/${jobId}/fail`, { error: error.slice(0, 2000), countAttempt });
  }
}

/** Admin calls, authenticated with a user JWT (FF_ADMIN_TOKEN, or email + password login). */
export class AdminApi {
  private token: string | null;
  private readonly credentials: { email: string; password: string } | null;

  constructor(
    private readonly baseUrl: string,
    opts: { token?: string; email?: string; password?: string },
  ) {
    this.token = opts.token ?? null;
    this.credentials = opts.email && opts.password ? { email: opts.email, password: opts.password } : null;
  }

  // The site allows only 5 logins per 15 min per IP, so the JWT is cached between CLI runs.
  private get cacheKey() {
    return `${this.baseUrl}|${this.credentials?.email ?? ""}`;
  }

  private async auth(): Promise<string> {
    if (this.token) return this.token;
    if (!this.credentials) throw new Error("Set FF_ADMIN_TOKEN, or FF_ADMIN_EMAIL and FF_ADMIN_PASSWORD.");

    const cached = readTokenCache()[this.cacheKey];
    if (cached && new Date(cached.expiresAt).getTime() > Date.now() + 5 * 60_000) {
      this.token = cached.token;
      return cached.token;
    }

    let res: Response;
    try {
      res = await request(this.baseUrl, "/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(this.credentials),
        retries: 0,
      });
    } catch (error) {
      if (error instanceof ApiError && error.status === 429) {
        throw new Error("Login rate limit hit (5 per 15 min). Wait, or set FF_ADMIN_TOKEN to a JWT from the site.");
      }
      if (error instanceof ApiError && error.status === 401) {
        const msg = JSON.parse(error.body || "{}").message;
        throw new Error(`Login failed: ${msg ?? "unauthorized"}`);
      }
      throw error;
    }
    const body = (await res.json()) as { token?: string; tokenExpiration?: string; message?: string };
    if (!body.token) throw new Error(`Login failed: ${body.message ?? "no token returned"}`);

    this.token = body.token;
    const cache = readTokenCache();
    cache[this.cacheKey] = { token: body.token, expiresAt: body.tokenExpiration ?? new Date(Date.now() + 3600_000).toISOString() };
    writeTokenCache(cache);
    return body.token;
  }

  async call<T = unknown>(method: string, path: string, body?: unknown): Promise<T> {
    const send = async () => request(this.baseUrl, `/api/admin/translation-pipeline${path}`, {
      method,
      headers: { Authorization: `Bearer ${await this.auth()}`, "Content-Type": "application/json" },
      body: body === undefined ? undefined : JSON.stringify(body),
    });

    let res: Response;
    try {
      res = await send();
    } catch (error) {
      // Cached token revoked or expired early: log in again once
      if (!(error instanceof ApiError && error.status === 401) || !this.credentials) throw error;
      const cache = readTokenCache();
      delete cache[this.cacheKey];
      writeTokenCache(cache);
      this.token = null;
      res = await send();
    }
    const text = await res.text();
    if (!text) return undefined as T;
    try {
      return JSON.parse(text) as T;
    } catch {
      return text as T;
    }
  }
}

type TokenCache = Record<string, { token: string; expiresAt: string }>;
const TOKEN_CACHE = join(homedir(), ".fallenfaction", "admin-token.json");

function readTokenCache(): TokenCache {
  try {
    return JSON.parse(readFileSync(TOKEN_CACHE, "utf8"));
  } catch {
    return {};
  }
}

function writeTokenCache(cache: TokenCache) {
  try {
    mkdirSync(dirname(TOKEN_CACHE), { recursive: true });
    writeFileSync(TOKEN_CACHE, JSON.stringify(cache), { mode: 0o600 });
  } catch {
    // caching is best-effort
  }
}
