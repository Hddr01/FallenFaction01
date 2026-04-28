# ADR-0003: Mobile sidebar — v-show + CSS transition instead of v-if

## Status
Accepted

## Context
`v-if="showMobileSidebar"` on the sidebar overlay destroyed and recreated ~100–150 DOM nodes on every toggle. The `slideInRight` CSS animation started mid-mount, so the first frame was always missing — visible stutter on open.

## Decision
Replace `v-if` with `v-show` (persistent DOM) and wrap the overlay in `<Transition name="sidebar-overlay">`. The transition CSS handles both enter (slide in) and leave (slide out):

```css
.sidebar-overlay-enter-from .mobile-sidebar,
.sidebar-overlay-leave-to   .mobile-sidebar { transform: translateX(100%); }

.sidebar-overlay-enter-active .mobile-sidebar,
.sidebar-overlay-leave-active .mobile-sidebar { transition: transform 0.28s ease-out; }
```

The old `@keyframes slideInRight` animation was removed from `.mobile-sidebar`.

## Consequences
- Sidebar DOM is always present; GPU compositor can pre-promote the layer.
- Smooth enter **and** leave animation — previously there was no leave animation.
- `v-show` means the sidebar subtree is always in memory; acceptable given its size.

## Files
- `fallenfaction.client/src/layout/NavBar.vue` (template + style)
