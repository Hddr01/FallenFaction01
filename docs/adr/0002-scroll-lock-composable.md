# ADR-0002: useScrollLock — body scroll lock composable

## Status
Accepted

## Context
`document.body.style.overflow = 'hidden'` was set inline in three places in `NavBar.vue`: `toggleMobileSidebar`, `closeMobileSidebar`, and `onUnmounted`. Any future caller would have to remember to replicate this pattern.

## Decision
Create `src/composables/useScrollLock.js`. Accepts a `Ref<boolean>`, watches it, and manages `document.body.style.overflow` in one place. Cleans up on unmount.

```js
// usage — NavBar.vue
useScrollLock(showMobileSidebar)
// no other code needed; lock follows the ref automatically
```

## Consequences
- Scroll-lock policy lives in one file. Adding a second lockable overlay (e.g. a modal) is one line.
- `toggleMobileSidebar` and `closeMobileSidebar` no longer touch `document.body` directly.

## Files
- `fallenfaction.client/src/composables/useScrollLock.js`
- Caller: `NavBar.vue`
