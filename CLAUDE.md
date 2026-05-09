# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FallenFaction is a community platform for reading, translating, and publishing web novels. The stack is:
- **Backend**: ASP.NET Core 9.0 + Entity Framework Core + SQL Server 2022
- **Frontend**: Vue 3.5 + Vite + Pinia + Tailwind CSS 4 + Shadcn Vue
- **Infra**: Docker Compose, Nginx, GitHub Actions CI/CD, Sentry

## Commands

### Frontend (`fallenfaction.client/`)
```bash
npm install          # Install dependencies
npm run dev          # Vite dev server on https://localhost:49217
npm run build        # Production build
npm run lint         # ESLint with auto-fix
npm test             # Run all Vitest tests
npx vitest run src/utils/storage.test.js  # Run a single test file
```

### Backend (`FallenFaction.Server/`)
```bash
dotnet build         # Build the project
dotnet run           # Run the backend (port 7217)
dotnet watch run     # Run with file-watching
dotnet ef migrations add <Name>   # Add EF migration
dotnet ef database update         # Apply migrations
```

### Full Stack
```bash
docker-compose up -d   # Start all services (SQL Server, backend, frontend)
docker-compose down    # Stop all services
```

## Architecture

### Backend Structure

`Program.cs` wires up the entire application — DI registrations, EF Core, Identity, JWT, CORS, rate limiting, and the middleware pipeline are all in that single file.

**Rule**: Controllers never touch `DbContext` directly. All business logic lives in scoped services injected via interfaces (`ICommentService`, `ITrustService`, `ITokenService`, `IAuthService`, `IEmailService`). Controllers just call services and map results to HTTP responses.

**Data flow**: Request → Controller → Service (business logic) → `ApplicationDbContext` (EF Core) → SQL Server. AutoMapper handles entity↔DTO conversion via profiles in `Mappings/`.

**DTOs** are organized by feature under `DTOs/` (e.g., `DTOs/Auth/`, `DTOs/Title/`, `DTOs/Chapter/`). There are separate request/response DTOs.

**Background services** registered as `IHostedService`:
- `AutoReleaseService` — runs every 2 hours; picks highest-voted `TranslationRequest` and promotes it to a published Title with an AITranslation team
- `SilverTicketExpiryService` — expires Silver Tickets older than 3 months
- `OnlineStatusCleanupService` — clears stale online status records

**EF Core configuration**: `QuerySplittingBehavior.SplitQuery` is enabled globally. Transactions with retries must use `IExecutionStrategy` (not plain `BeginTransactionAsync`) to be compatible with the retry-on-failure SQL Server policy.

**Rate limiting**: Global 100 req/min per IP; named policies `"login"` (5/15 min), `"ticket-unlock"` (10/min), `"comment-create"` (20/min). Apply with `[EnableRateLimiting("policy-name")]`.

**Auth**: JWT Bearer is the default scheme. `AddIdentity` overrides it to cookie scheme internally, so `Program.cs` explicitly resets `DefaultAuthenticateScheme` back to `JwtBearerDefaults.AuthenticationScheme`.

**Upload size limits**: Global Kestrel limit is 10 MB. Per-endpoint limits are enforced inside action methods (avatars: 5 MB, banners: 10 MB).

### Frontend Structure

**API access**: All HTTP calls go through the singleton axios instance at `src/services/apiClient.js`. It has one request interceptor (attaches auth token) and one response interceptor (handles 401 redirect and 429 rate-limit). Never create additional axios instances or import axios directly in components.

**State**: Pinia stores in `src/stores/`. `authStore` owns JWT + user profile. `themeStore` is the single authority for theme state — do not read/write theme from `localStorage` directly; use `storage.theme` via the `themeStore` or `useTheme` composable.

**localStorage**: Access only through `src/utils/storage.js`. It defines namespaced keys: `ff-theme` (theme), `novelReader_*` (reader prefs), `smartLoading_history`. Auth tokens are managed exclusively by `authStore`, not via `storage`.

**Composables**:
- `useScrollSignal` — module singleton; one passive RAF-gated scroll listener shared across all consumers. Returns `{ scrollY: Ref<number> }`.
- `useScrollLock(isLocked)` — watches a boolean ref and toggles `document.body.style.overflow`.
- `useTheme` — thin shim over `themeStore`; exposes `{ isDark, toggleTheme }` and `initTheme()`.

**Routing**: Titles use slug-based routes like `/novel/title-name-42` (slug includes the title ID suffix). Admin panel lives under `/admin/*`.

**UI components**: Shadcn Vue components (style: `new-york`) live in `src/components/ui/`. Use Lucide Vue Next for icons.

**Vite dev proxy**: `/api`, `/auth`, and `/uploads` are proxied to the backend at `https://localhost:7217`. The frontend runs at `https://localhost:49217` with auto-generated HTTPS certs.

**Tests**: Vitest + Happy DOM. Test files are colocated next to their module (e.g., `storage.test.js` next to `storage.js`). Test globals are enabled — no need to import `describe`/`it`/`expect`.

### Domain Model Summary

See `CONTEXT.md` for the full domain model. Key concepts:

- **Title** → has Chapters, belongs to a Team, has pending/rejected variants for the content approval workflow
- **UserTrustRecord** — per-user, per-action approval counter; 5 approvals without rejection → user becomes "trusted" and their submissions are auto-approved; any rejection resets count to zero
- **Ticket economy** — Gold (never expires) and Silver (expires 3 months); `UserTicket` is the wallet; every debit/credit is recorded in `TicketTransaction`; AI chapter unlock cost: `(charCount + 500) × 0.0012`, min 1 ticket
- **TranslationRequest** lifecycle: Pending → Approved → (community votes) → AutoReleaseService picks highest-voted → Released
- **Comment** — supports nested replies via `ParentCommentId`; soft-deleted (content hidden, record kept, restorable)
- **Roles**: `Admin`, `Moderator`, `User`; team-level roles: `Admin`, `Member`, `Viewer`; fine-grained team permissions via `UserTeamRolePermission`

## Key Conventions

- **Backend nullable**: `<Nullable>enable</Nullable>` is set — all reference types are non-nullable unless explicitly marked `?`.
- **JSON serialization**: camelCase property names, null values omitted (`WhenWritingNull`), cycles ignored (`IgnoreCycles`), max depth 128.
- **Migrations**: Always add migrations from the `FallenFaction.Server/` directory with the `dotnet ef` CLI; never hand-edit migration files.
- **Seeding**: `PermissionSeeder` and `AITeamSeeder` run on every startup after `MigrateAsync()`. New seed data goes through these classes.
- **Frontend linting**: `_`-prefixed variables suppress unused-var warnings. `vue/multi-word-component-names` is off. `no-console` and `no-debugger` only warn/error in production.
- **ADRs**: Significant frontend architecture decisions are documented in `docs/adr/`. When introducing a new composable, store, or global pattern, add an ADR.
- **No Prettier**: ESLint handles all frontend code style. Do not add a Prettier config.
