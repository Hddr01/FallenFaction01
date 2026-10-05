import { WorkerApi } from "./api";
import { RefusalError, UsageLimitError, type LlmBackend } from "./backends/types";
import type { WorkerConfig } from "./config";
import { translateJob } from "./pipeline";

const HEARTBEAT_MS = 5 * 60_000; // server lease is 20 min
const sleep = (ms: number, signal?: AbortSignal) =>
  new Promise<void>(resolve => {
    const t = setTimeout(resolve, ms);
    signal?.addEventListener("abort", () => { clearTimeout(t); resolve(); }, { once: true });
  });

export function log(msg: string) {
  console.log(`[${new Date().toISOString().slice(0, 19).replace("T", " ")}] ${msg}`);
}

/**
 * Claim → translate → submit, one chapter at a time (subscriptions don't like parallelism).
 * Usage limits keep the lease and wait; refusals and errors go back to the server.
 */
export async function runWorker(cfg: WorkerConfig, backend: LlmBackend, opts: { once?: boolean } = {}) {
  if (!cfg.workerKey) throw new Error("FF_WORKER_KEY is not set. Create one with: bun run cli create-worker <name>");
  const api = new WorkerApi(cfg.apiUrl, cfg.workerKey);
  const stop = new AbortController();
  let currentJob: number | null = null;

  const shutdown = () => {
    if (stop.signal.aborted) process.exit(1); // second Ctrl+C: hard exit
    log("Stopping after giving the current job back… (Ctrl+C again to force)");
    stop.abort();
  };
  process.on("SIGINT", shutdown);
  process.on("SIGTERM", shutdown);

  log(`Worker started: backend=${backend.name}, models=${JSON.stringify(cfg.models)}, server=${cfg.apiUrl}`);

  while (!stop.signal.aborted) {
    let job;
    try {
      job = await api.claim();
    } catch (error) {
      log(`Claim failed: ${(error as Error).message}`);
      await sleep(cfg.pollSeconds * 1000, stop.signal);
      continue;
    }

    if (!job) {
      if (opts.once) { log("Queue is empty."); break; }
      await sleep(cfg.pollSeconds * 1000, stop.signal);
      continue;
    }

    currentJob = job.jobId;
    const label = `job ${job.jobId} "${job.titleName}" v${job.volumeNumber} ch${job.chapterNumber} (attempt ${job.attempt})`;
    log(`Claimed ${label}: ${job.sourceText.length} source chars, ${job.glossary.length} glossary terms, ${job.previousChapters.length} prior summaries`);

    // Lease keeper: also detects that an admin revoked us / someone else took the job
    const jobAbort = new AbortController();
    stop.signal.addEventListener("abort", () => jobAbort.abort(), { once: true });
    const heartbeat = setInterval(async () => {
      try {
        if (!(await api.heartbeat(job!.jobId))) {
          log(`Lease lost for ${label}; abandoning it.`);
          jobAbort.abort();
        }
      } catch (error) {
        log(`Heartbeat failed (will retry): ${(error as Error).message}`);
      }
    }, HEARTBEAT_MS);

    try {
      for (;;) {
        try {
          const payload = await translateJob(job, backend, {
            maxChunkChars: cfg.maxChunkChars,
            reviseBelow: cfg.reviseBelow,
            signal: jobAbort.signal,
            log: m => log(`  ${m}`),
          });
          if (jobAbort.signal.aborted) throw new Error("Aborted");
          const result = await api.complete(job.jobId, payload);
          log(`Done ${label} → ${result.status}${result.chapterId ? ` (chapter ${result.chapterId})` : ""}, QA ${payload.qaScore}, +${result.glossaryTermsAdded} glossary terms`);
          break;
        } catch (error) {
          if (error instanceof UsageLimitError && !jobAbort.signal.aborted) {
            const waitMs = error.retryAfterMs ?? 15 * 60_000;
            log(`Usage limit hit (${error.message.slice(0, 120)}). Keeping the lease, retrying in ${Math.round(waitMs / 60_000)} min.`);
            await sleep(waitMs, jobAbort.signal);
            if (jobAbort.signal.aborted) throw new Error("Aborted");
            continue;
          }
          throw error;
        }
      }
    } catch (error) {
      const message = (error as Error).message ?? String(error);
      if (stop.signal.aborted || message === "Aborted") {
        // Shutdown or lost lease — not the chapter's fault
        await api.fail(job.jobId, "Worker stopped before finishing", false).catch(() => {});
        log(`Returned ${label} to the queue.`);
      } else {
        const kind = error instanceof RefusalError ? "Refused" : "Failed";
        log(`${kind} ${label}: ${message}`);
        await api.fail(job.jobId, `${kind}: ${message}`, true).catch(e => log(`Could not report failure: ${e.message}`));
      }
    } finally {
      clearInterval(heartbeat);
      currentJob = null;
    }

    if (opts.once) break;
  }

  if (currentJob === null) log("Worker stopped.");
}
