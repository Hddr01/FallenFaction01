# ADR-0004: Remove brightness() from navbar backdrop-filter

## Status
Accepted

## Context
The fixed navbar had `backdrop-filter: blur(20px) brightness(1.05)`. Combining `brightness()` with `blur()` forces software rasterization on most mobile GPUs — the fixed element repaints every scroll frame, blocking the main thread.

## Decision
Drop `brightness(1.05)`, keep only `blur(20px)`. Add `transform: translateZ(0)` to force GPU layer promotion before the first scroll, not reactively.

```css
.navbar {
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  transform: translateZ(0);
}
```

The `.mobile-sidebar` already used `blur(20px)` without brightness and was not changed.

## Consequences
- Navbar scroll is now compositor-only — zero main thread cost per scroll frame.
- Slight visual difference: no brightness boost over content. The frosted-glass effect is preserved via blur alone.

## Files
- `fallenfaction.client/src/layout/NavBar.vue` (style)
