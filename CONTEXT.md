# FallenFaction — Domain Context

A community platform for reading, translating, and publishing web novels. Users read content, teams translate it, and an AI translation system with a ticket economy handles automated translation.

---

## Core Concepts

**Title** — The top-level content entity. A novel, light novel, web novel, or short story. Has a type (Novel, LightNovel, WebNovel, ShortStory, Wuxia, Xianxia, Xuanhuan, ClassicFiction) and a category (Translation, Original, Fanfic, AITranslation). A Fanfic references its source Title via `SourceTitleId`. A Title is either published (live) or pending approval.

**Chapter** — A single episode/chapter belonging to a Title. Has a volume number, chapter number, language (`en` by default; the same number can exist once per language, e.g. an AI-translated `pl` copy) and full text content. Chapters can be AI-locked, meaning users must spend Tickets to read them. Character count drives ticket cost.

**Team** — A group that owns and publishes Titles and Chapters. Four types: Personal (auto-created per user, hidden), Translation (public, human translators), Creator (public, original authors), AITranslation (system-owned, admin-managed). Each team has members with roles.

**AppUser** — A registered user. Has XP, level (L1–L5), Patreon integration, a Ticket wallet, and can belong to multiple Teams. Trust status tracks how many of their submissions have been approved.

---

## Content Lifecycle

**PendingTitle / PendingChapter** — Submissions awaiting admin review. Same shape as Title/Chapter but held separately until approved or rejected.

**RejectedTitle / RejectedChapter** — Archive of rejected submissions for audit purposes.

**TitleChangeLog** — Tracks proposed metadata edits to a published Title. Goes through Pending → Approved or Rejected states, reviewed by admins.

**UserTrustRecord** — Per-user, per-action (AddTitle, AddChapter, EditTitle, EditChapter) count of admin approvals. When a user reaches 5 approvals without a rejection, they become "trusted" and their future submissions are auto-approved. A single rejection resets the count to zero.

---

## AI Translation System

**TranslationRequest** — A user-submitted request to have a novel AI-translated. Requires a source URL, proposed title, genres (min 1), and tags (min 2). Goes through: Pending → Approved → PreProcessing → Released (or Rejected).

**TranslationRequestVote** — Community vote on an Approved request. Only users at level L2+ or Patreon supporters can vote. One vote per user per request.

**AutoReleaseService** — Background job that runs every 2 hours. Picks the highest-voted Approved request, creates a Title + AITranslation Team, and marks the request as Released.

**AIChapterUnlock** — Record that a specific user has unlocked a specific AI-locked Chapter by spending Tickets.

---

## Translation Pipeline (external workers)

**TranslationJob** — One raw chapter queued for machine translation. Queued → Claimed (worker holds a 20-min lease, renewed by heartbeat) → Published, or NeedsReview (QA gate failed), or Failed (3 attempts used). Jobs of one Title are claimed strictly in (Volume, Chapter) order; a Failed job blocks the later chapters of its Title until an admin retries it. NeedsReview does not block.

**TranslationWorker** — A machine allowed to claim jobs. Authenticates with the `X-Worker-Key` header (own auth scheme `WorkerKey`, never a user JWT); only the key's SHA-256 hash is stored.

**TranslationGlossaryEntry** — Per-Title term memory (names, places, sects, techniques, ranks). Worker-proposed terms never overwrite existing ones; admin-edited terms are Locked and mandatory. A claimed job receives only the terms that occur in its source text.

**TranslationTitleProfile** — Per-Title source language, target language (default `en`; the pipeline publishes into chapters of that language and never overwrites another language), style guide, synopsis, `RequireReview`, and `MinQaScore` (default 7).

**Publishing** — `TranslationPipelineService` writes the Chapter directly (not through PendingChapter, whose approval drops `IsAILocked`/`CharacterCount`). It applies the AI rule (first 50 chapters free), writes a TitleChangeLog, and updates an existing chapter in place on re-translation, keeping its lock state.

**translation-worker/** — Bun CLI that claims jobs and runs terminology → translate → review → revise → validators through a pluggable backend (`claude-cli` headless Claude Code, `anthropic-api`, experimental `codex-cli`). See its README.

---

## Ticket Economy

**Ticket** — The platform currency. Two types:
- **Gold** — Purchased or granted via Patreon. Never expires.
- **Silver** — Earned from contributions. Expires after 3 months.

**UserTicket** — The wallet: one per user, holds Gold and Silver balances.

**TicketTransaction** — Immutable ledger entry. Every credit and debit is recorded with type, amount, balance-after, and context (which Title/Chapter/Request it relates to). Transaction types: PatreonGrant, AdminGrant, Contribution, Refund, ChapterUnlock, NovelRelease, Expiry, Adjustment.

**Ticket cost formula** — `(CharacterCount + 500) × 0.0012`, minimum 1 ticket. Applied when unlocking an AI Chapter.

---

## User Progression

**XP & Level** — Users earn XP from platform activity. Five levels:
- L1 Newcomer: 0 XP
- L2 Reader: 100 XP (unlocks Translation Request voting)
- L3 Regular: 300 XP
- L4 Veteran: 700 XP
- L5 Champion: 1500 XP

**Patreon supporter** — A user linked to an active Patreon tier. Bypasses level requirement for Translation Request voting.

---

## Team & Permissions

**TeamRole** — Admin, Member, or Viewer. Controls what a user can do within a team.

**UserTeamRolePermission** — Fine-grained permission (e.g., CanAddTitle) layered on top of role. A team admin can grant specific permissions to members beyond their role defaults.

**TitleTeamJoinRequest** — A team applies to take over translation of a Title. Goes through: Pending → Approved, RejectedByAdmin, RejectedByTeam, or AutoRejected.

---

## Community

**Comment** — Posted on a Title or Chapter (not both). Supports nested replies via `ParentCommentId`. Soft-deleted (content hidden but record kept, restorable). Can be pinned by team moderators or admins.

**CommentReaction** — Like or dislike on a Comment. One reaction per user per comment.

**Rating** — A 1–10 score a user gives to a Title. One rating per user per Title.

**Bookmark** — A user saves a Title to a BookmarkFolder for later. Tracks last-read chapter.

**BookmarkFolder** — Named collection of Bookmarks owned by a user.

**ChapterView** — Analytics record. One entry per user per Chapter read.

**ReadingProgress** — Tracks the last chapter a user read for a given Title.

---

## Moderation

**Report** — Filed against a Comment, Title, Chapter, or User. Reason is one of: Spam, Harassment, InappropriateContent, Spoiler, CopyrightViolation, MisinformationOrFake, HateSpeech, Other. Status: Pending → Reviewed → Resolved or Dismissed.

**Notification** — System or user-triggered message. Can be global (all users) or targeted. Supports scheduling and expiry. Types include: NewChapter, CommentReply, TeamInvite, ReportResolved, TitleJoinRequest, GlobalAnnouncement.

---

## Architecture

**Backend** — ASP.NET Core 8, Entity Framework Core (Code-First), SQL Server. Clean MVC with service interfaces. JWT authentication, ASP.NET Identity for user management.

**Frontend** — Vue 3 + Vite, Pinia for state, Vue Router. Slug-based title routing (`/novel/title-name-42`). Admin panel at `/admin/*`. Test runner: Vitest + happy-dom (`npm test` in `fallenfaction.client/`). Test files colocated next to their module.

**Frontend composables** (`fallenfaction.client/src/composables/`):
- `useScrollSignal` — module singleton, one passive RAF-gated scroll listener, exposes `{ scrollY: Ref<number> }`. See ADR-0001.
- `useScrollLock(isLocked)` — watches a boolean ref and sets `document.body.style.overflow`. See ADR-0002.
- `useTheme` — thin shim: `initTheme()` and `useTheme()` → `{ isDark, toggleTheme }`. Delegates to `themeStore`.

**Frontend stores** (`fallenfaction.client/src/stores/`):
- `authStore` — authentication state, JWT token, user profile.
- `themeStore` — single theme authority. State: `currentTheme`. Action: `applyTheme(name)`. Persists via `storage.theme`. See ADR-0009.

**Frontend services** (`fallenfaction.client/src/services/`):
- `apiClient` — singleton axios instance. One request interceptor (auth token), one response interceptor (401 redirect, 429 rate-limit). All service files import this directly; no other axios instances exist. See ADR-0007.
- `ErrorHandler` — Vue/global unhandled-error page handler. Not an HTTP concern (HTTP is owned by `apiClient`).

**Frontend utilities** (`fallenfaction.client/src/utils/`):
- `storage` — typed localStorage accessors. Namespaces: `theme` (`ff-theme`), `readerPrefs` (`novelReader_*`), `smartLoadingHistory` (`smartLoading_history`). Auth keys (`authToken`/`authUser`) stay in `authStore`. See ADR-0008.

**Background jobs** — AutoReleaseService (every 2h), SilverTicketExpiryService (periodic), OnlineStatusCleanupService.

**External integrations** — Patreon (OAuth + tier grants), Resend (email), Sentry (errors).

**Key seams** — Services are injected via interfaces (IAuthService, ICommentService, ITrustService, ITokenService, IEmailService). Controllers never touch DbContext directly.

---

## Module Map

```
AppUser ──────┬──── UserTicket (wallet)
              ├──── TicketTransaction[] (ledger)
              ├──── UserTeamRole[] → Team
              ├──── Bookmark[] → BookmarkFolder
              ├──── Rating[] → Title
              ├──── Comment[] → Title | Chapter
              ├──── TranslationRequest[]
              └──── UserTrustRecord[]

Title ────────┬──── Chapter[]
              ├──── Team (owner)
              ├──── Category[], Tag[], Format[]
              ├──── Author[], Artist[], Publisher[]
              ├──── Comment[], Rating[], Bookmark[]
              ├──── TitleChangeLog[]
              └──── FanficDerivatives[] → Title (self-ref)

Team ─────────┬──── UserTeamRole[] → AppUser
              ├──── Title[]
              └──── Chapter[]

TranslationRequest ──┬── TranslationRequestVote[] → AppUser
                     └── Title (after release)

Chapter ─────────────┬── AIChapterUnlock[]
                     └── ChapterView[]
```
