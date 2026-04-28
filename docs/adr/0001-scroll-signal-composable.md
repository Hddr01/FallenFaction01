# ADR-0001: useScrollSignal — single passive scroll listener

## Status
Accepted

## Context
`NavBar.vue` and `Catalog.vue` each registered their own `window.addEventListener('scroll', ...)` without a passive flag and without RAF gating. Every scroll pixel fired synchronously on the main thread, twice.

## Decision
Create `src/composables/useScrollSignal.js` as a **module-level singleton**: one passive, RAF-gated scroll listener registered once at module import time. Exposes a single `scrollY: Ref<number>`.

```js
// usage
const { scrollY } = useScrollSignal()
const showScrollTop = computed(() => scrollY.value > 500)
```

Key choices:
- **Module singleton, not reference-counted** — simpler, no lifecycle edge cases. A scroll listener on `window` is cheap to leave alive.
- **`scrollY` only, no `isScrolled(threshold)` helper** — callers derive their own computeds. Smaller surface, easier to test.
- **Ticking-flag RAF pattern** — fires at most once per animation frame, not once per pixel.

## Consequences
- All future scroll-reading code gets correct, cheap scroll reads by calling `useScrollSignal()`.
- Tests use `vi.stubGlobal('requestAnimationFrame', cb => cb())` to make RAF synchronous.
- `vi.resetModules()` is required between tests because module-level state persists across imports.

## Files
- `fallenfaction.client/src/composables/useScrollSignal.js`
- `fallenfaction.client/src/composables/useScrollSignal.test.js`
- Callers: `NavBar.vue` (watch), `Catalog.vue` (computed)
