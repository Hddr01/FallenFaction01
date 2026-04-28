# ADR-0009: themeStore Pinia store as single theme authority

## Status
Accepted

## Context
`useTheme.js` was a module-level singleton that managed theme state as a plain `ref`. It read/wrote `localStorage` directly and imperatively manipulated `document.documentElement.classList`. Any component that needed to react to theme changes had to import the composable; the state was invisible to Vue DevTools and untestable without a real DOM.

Reader-local themes (dark/light/sepia in `ChapterReader.vue`) are not the app theme — they are reader preferences stored under `novelReader_theme`. These stay local in `ChapterReader.vue`.

## Decision
Create `src/stores/themeStore.js` (Pinia):

- State: `currentTheme: ref('dark')`
- Computed: `isDark: computed(() => currentTheme.value === 'dark')`
- Action: `applyTheme(name)` — sets state, calls `storage.theme.set(name)`, manages `document.documentElement.classList.add/remove('dark')`
- `init()` — reads from `storage.theme` on startup, defaults to `'dark'`

`useTheme.js` is kept as a thin shim: `initTheme()` calls `store.init()`, `useTheme()` returns `{ isDark: store.isDark, toggleTheme }`. The public interface is unchanged — no callers needed updating.

## Consequences
- Theme state is visible in Vue DevTools Pinia panel.
- `applyTheme` is testable: set up `createPinia()`, call `applyTheme`, assert `classList` and `localStorage`.
- `useTheme.js` shim can be removed later if callers are ever migrated to `useThemeStore()` directly.
- Reader themes remain local to `ChapterReader.vue` — they are not the app theme and must not be centralised here.
