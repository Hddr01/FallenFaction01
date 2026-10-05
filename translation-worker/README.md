# FallenFaction translation worker

Turns raw chapters (Chinese / Japanese / Korean / any language) into published English chapters on FallenFaction.
The server holds the queue and the translation memory. Workers are plain CLI processes on any PC; they only make
outbound HTTPS calls, so there is no VPN, open port, or Tailscale to set up.

```
 raw chapters ──ingest──▶ FallenFaction server ◀──claim / complete──  worker PC (Claude Code login)
                          • job queue (lease, in chapter order)        • terminology pass
                          • per-title glossary + chapter summaries     • translate (full context)
                          • QA gate → publish or NeedsReview           • review → revise → re-review
                                                                       • deterministic validators
```

## Quality: what each chapter goes through

1. **Terminology**: new names, sects, techniques and ranks in this chapter are added to the title's glossary.
   The first rendering of a term sticks; admin-edited terms are locked and mandatory.
2. **Translate**: the prompt carries the synopsis, the title's style guide, summaries of the previous 5 chapters,
   and every glossary term that appears in the chapter. Long chapters are split on paragraph boundaries.
3. **Review**: an editor pass against the source checks for omissions, mistranslations, glossary drift,
   untranslated text, additions, fluency and formatting, and scores 1–10. It also writes the chapter summary
   that later chapters use as context.
4. **Revise**: if anything was reported, the draft is revised and re-reviewed. The better-scoring version wins.
5. **Validators**: paragraph count, length ratio and leftover source script are checked to catch silent omissions.

A chapter is **published automatically** only if it passes the title's `minQaScore` (default 7), has no critical
issues left and no validator flags. Otherwise it waits in **NeedsReview** for an admin.
AI titles keep the site rule: the first 50 chapters are free, later chapters are ticket-locked.
Chapters of one title are translated strictly in order, so each chapter sees the context of the previous ones.

## Setup

Requires [Bun](https://bun.sh) and, for the default backend, [Claude Code](https://claude.com/claude-code)
logged in on that PC (`claude` → `/login`).

```sh
cd translation-worker
bun install
```

### 1. Admin: create a worker key (once per PC)

```sh
export FF_API_URL=https://fallenfaction.com
export FF_ADMIN_EMAIL=you@example.com FF_ADMIN_PASSWORD=...   # or FF_ADMIN_TOKEN=<jwt>
bun run cli create-worker my-desktop          # prints the key once
```

The site allows 5 logins per 15 min per IP, so the CLI caches the admin JWT in `~/.fallenfaction/admin-token.json`.

### 2. Admin: set up a title

```sh
bun run cli profile 42 --lang zh --style-file style.md --synopsis-file synopsis.txt
bun run cli glossary 42 --import names.json     # optional: [{ "sourceTerm": "林动", "translation": "Lin Dong", "category": "character" }]
bun run cli ingest 42 --dir ./raw/novel-42      # one .txt per chapter; first line = chapter title
bun run cli ingest 42 --json chapters.json      # or [{ "chapter": 1, "title": "...", "text": "..." }]
```

The title must already exist on the site (an AI-translation title under the AI/TL team).

### 3. Run the worker

```sh
export FF_API_URL=https://fallenfaction.com FF_WORKER_KEY=ffw_...
bun run worker            # runs until Ctrl+C (the current chapter goes back to the queue)
bun run worker --once     # translate one chapter and exit
```

### 4. Admin: monitor and review

```sh
bun run cli status 42                         # counts per status + which failed chapter is blocking the title
bun run cli jobs --status NeedsReview
bun run cli job 17                            # source, translation, QA report
bun run cli publish 17                        # publish a reviewed chapter as-is
bun run cli retry 17                          # re-translate (a published chapter is updated in place)
bun run cli revoke-worker 3
```

## Configuration

| Variable | Default | |
|---|---|---|
| `FF_BACKEND` | `claude-cli` | `claude-cli`, `anthropic-api` (needs `ANTHROPIC_API_KEY`), `codex-cli` (**experimental, untested**) |
| `FF_MODEL_TRANSLATE` | `opus` / `claude-opus-5` | translation and revision |
| `FF_MODEL_ASSIST` | `sonnet` / `claude-sonnet-5` | terminology and review |
| `FF_REVISE_BELOW` | `10` | revise whenever the review is below this score or reports a major issue; `8.5` saves quota |
| `FF_MAX_CHUNK_CHARS` | `8000` | source characters per translation call |
| `FF_POLL_SECONDS` | `30` | idle polling interval |
| `FF_CALL_TIMEOUT_MINUTES` | `20` | per model call |

The `claude-cli` backend runs `claude -p` isolated from your own setup: no settings, hooks, CLAUDE.md, MCP servers,
or tools are loaded, so your personal Claude Code configuration never leaks into translations.
When the subscription hits its usage limit, the worker keeps the job's lease and waits for the reset
instead of burning retries.

## Subscriptions vs. the API

Claude Code (Pro/Max) and ChatGPT subscriptions are licensed for an individual's own use. Running *your own*
subscription for your own site at a modest pace is a grey area, and you will hit weekly limits. Pooling
**other people's** subscriptions to serve a site that sells chapter unlocks is account sharing under both
Anthropic's and OpenAI's consumer terms, and gets accounts banned. For real volume, switch the same worker to
`FF_BACKEND=anthropic-api`. Nothing else changes, and API calls get prompt caching (the system prompts are stable)
and server-side refusal fallbacks.

## Development

```sh
bun test          # unit tests (pipeline runs against a scripted fake backend)
bun run typecheck
```
