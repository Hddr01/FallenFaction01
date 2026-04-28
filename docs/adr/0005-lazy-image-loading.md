# ADR-0005: loading="lazy" on off-screen images in HomePage

## Status
Accepted

## Context
All images in `HomePage.vue` were eagerly fetched at mount. `decoding="async"` was present on carousel covers but `loading="lazy"` was missing everywhere. On slow mobile connections, 15–30 cover images downloaded simultaneously, delaying LCP and competing with first-render.

## Decision
Add `loading="lazy"` to all four image groups in `HomePage.vue`:
1. Carousel cover images (already had `decoding="async"`)
2. Top Users avatars
3. Top Teams avatars
4. Last Updates cover images

No JS changes needed — the browser handles the viewport-proximity threshold automatically.

## Consequences
- Images load as they approach the viewport, not all at mount.
- LCP improves on slow connections.
- The `TitleCard` component (used in Weekly Featured, Top Titles sections) renders images via its own template — if it lacks `loading="lazy"`, add it there separately.

## Files
- `fallenfaction.client/src/HomePage.vue`
