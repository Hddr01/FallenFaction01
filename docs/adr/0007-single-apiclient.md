# ADR-0007: Single apiClient singleton for all HTTP requests

## Status
Accepted

## Context
The codebase had 21+ service files each calling `axios.create()` with hand-rolled auth-token injection and partial 401/429 handling. `ApiErrorHandler.js` held a second axios factory that `App.vue` instantiated and passed to components via `provide('apiClient')`. Three components (`BookmarkDropdown`, `TitleCard`, `Profile`) consumed it via `inject('apiClient')`. The result: multiple independent interceptor stacks with divergent behaviour, no single place to change auth logic, and components coupled to Vue's provide/inject mechanism for something that isn't a tree-scoped concern.

## Decision
Create `src/services/apiClient.js` — a module-level singleton `axios` instance:

- One `axios.create()` with `VITE_API_BASE_URL ?? '/api'` base URL, 15 s timeout, `withCredentials: true`.
- **Request interceptor**: reads `authToken` from `localStorage`, injects `Authorization: Bearer …`.
- **Response interceptor**:
  - 401 → clears `authToken`/`authUser`, redirects to `/account/login` after 100 ms (skips `/auth/logout` and `/auth/accept-terms`).
  - 429 → redirects to `/error/429` with `retry-after` message (skips background endpoints: heartbeat, online-status, health).
- All 21 service files import `apiClient` directly; no axios instance creation of their own.
- `ApiErrorHandler.js` deleted. `App.vue` no longer provides `apiClient`; components import directly.
- Dead `setupAxiosInterceptors` / `setupFetchInterceptor` exports removed from `ErrorHandler.js`.

## Consequences
- Auth logic lives in one place. A future token-refresh or PKCE flow changes one file.
- Components no longer depend on the provide/inject chain for HTTP; they can be tested in isolation.
- `ErrorHandler.js` is now exclusively a Vue/global error page handler, not an HTTP concern.
