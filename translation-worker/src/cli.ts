import { readFileSync } from "node:fs";
import { parseArgs } from "node:util";
import { AdminApi } from "./api";
import { createBackend, loadConfig } from "./config";
import { batchChapters, readJsonFile, readTxtFolder, type RawChapter } from "./ingest";
import { runWorker } from "./worker";

const USAGE = `FallenFaction translation worker

Worker (runs on any PC with Claude Code logged in):
  worker [--once]                      claim and translate chapters until stopped

Admin (needs FF_ADMIN_TOKEN, or FF_ADMIN_EMAIL + FF_ADMIN_PASSWORD):
  create-worker <name>                 create a worker and print its key (shown once)
  workers                              list workers
  revoke-worker <id>                   revoke a key; its in-flight job returns to the queue
  profile <titleId> --lang zh [--style-file f] [--synopsis-file f] [--require-review] [--min-score 7]
  ingest <titleId> (--dir <folder> | --json <file>) [--volume 1] [--no-title-line]
  glossary <titleId> [--import terms.json]   show, or import [{sourceTerm, translation, category, notes}]
  status <titleId>                     job counts and what blocks the title
  jobs [--title <id>] [--status NeedsReview]
  job <jobId>                          full job incl. source, translation and QA report
  retry <jobId>                        re-queue a Failed / NeedsReview / Published job
  publish <jobId>                      publish a NeedsReview job as-is

Environment: FF_API_URL, FF_WORKER_KEY, FF_BACKEND (claude-cli | anthropic-api | codex-cli),
FF_MODEL_TRANSLATE, FF_MODEL_ASSIST, FF_POLL_SECONDS, FF_MAX_CHUNK_CHARS, FF_REVISE_BELOW`;

const { positionals, values } = parseArgs({
  allowPositionals: true,
  options: {
    once: { type: "boolean" },
    lang: { type: "string" },
    "style-file": { type: "string" },
    "synopsis-file": { type: "string" },
    "require-review": { type: "boolean" },
    "min-score": { type: "string" },
    dir: { type: "string" },
    json: { type: "string" },
    volume: { type: "string" },
    "no-title-line": { type: "boolean" },
    import: { type: "string" },
    title: { type: "string" },
    status: { type: "string" },
    help: { type: "boolean", short: "h" },
  },
});

const [command, arg] = positionals;
const print = (v: unknown) => console.log(JSON.stringify(v, null, 2));
const need = (v: string | undefined, what: string) => {
  if (!v) { console.error(`Missing ${what}.\n\n${USAGE}`); process.exit(2); }
  return v;
};

const cfg = loadConfig();
const admin = () => new AdminApi(cfg.apiUrl, {
  token: process.env.FF_ADMIN_TOKEN,
  email: process.env.FF_ADMIN_EMAIL,
  password: process.env.FF_ADMIN_PASSWORD,
});

switch (command) {
  case "worker":
    await runWorker(cfg, createBackend(cfg), { once: values.once });
    break;

  case "create-worker": {
    const created = await admin().call<{ id: number; apiKey: string }>("POST", "/workers", { name: need(arg, "worker name") });
    print(created);
    console.error("\nStore this key now — it is not shown again. Put it in FF_WORKER_KEY on the worker PC.");
    break;
  }
  case "workers":
    print(await admin().call("GET", "/workers"));
    break;
  case "revoke-worker":
    await admin().call("DELETE", `/workers/${need(arg, "worker id")}`);
    console.log("Revoked.");
    break;

  case "profile": {
    const titleId = need(arg, "titleId");
    print(await admin().call("PUT", `/titles/${titleId}/profile`, {
      sourceLanguage: values.lang ?? "zh",
      styleGuide: values["style-file"] ? readFileSync(values["style-file"], "utf8") : null,
      synopsis: values["synopsis-file"] ? readFileSync(values["synopsis-file"], "utf8") : null,
      requireReview: !!values["require-review"],
      minQaScore: Number(values["min-score"] ?? 7),
    }));
    break;
  }

  case "ingest": {
    const titleId = need(arg, "titleId");
    const volume = Number(values.volume ?? 1);
    let chapters: RawChapter[];
    if (values.dir) chapters = readTxtFolder(values.dir, { volume, firstLineIsTitle: !values["no-title-line"] });
    else if (values.json) chapters = readJsonFile(values.json, volume);
    else { console.error(`ingest needs --dir or --json\n\n${USAGE}`); process.exit(2); }

    console.log(`Read ${chapters.length} chapter(s). Enqueueing…`);
    const api = admin();
    let enqueued = 0, skipped = 0;
    for (const batch of batchChapters(chapters)) {
      const r = await api.call<{ enqueued: number; skippedExisting: number }>(
        "POST", `/titles/${titleId}/jobs`, { chapters: batch });
      enqueued += r.enqueued;
      skipped += r.skippedExisting;
    }
    console.log(`Enqueued ${enqueued}, skipped ${skipped} already queued.`);
    break;
  }

  case "glossary": {
    const titleId = need(arg, "titleId");
    if (values.import) {
      const terms = JSON.parse(readFileSync(values.import, "utf8"));
      print({ updated: await admin().call("PUT", `/titles/${titleId}/glossary`, terms) });
    } else {
      print(await admin().call("GET", `/titles/${titleId}/glossary`));
    }
    break;
  }

  case "status":
    print(await admin().call("GET", `/titles/${need(arg, "titleId")}/status`));
    break;
  case "jobs": {
    const q = new URLSearchParams();
    if (values.title) q.set("titleId", values.title);
    if (values.status) q.set("status", values.status);
    print(await admin().call("GET", `/jobs?${q}`));
    break;
  }
  case "job": {
    const job = await admin().call<any>("GET", `/jobs/${need(arg, "jobId")}`);
    if (job?.qaReport) job.qaReport = JSON.parse(job.qaReport);
    print(job);
    break;
  }
  case "retry":
    print(await admin().call("POST", `/jobs/${need(arg, "jobId")}/retry`));
    break;
  case "publish":
    print(await admin().call("POST", `/jobs/${need(arg, "jobId")}/publish`, {}));
    break;

  default:
    console.log(USAGE);
    process.exit(command && !values.help ? 2 : 0);
}
