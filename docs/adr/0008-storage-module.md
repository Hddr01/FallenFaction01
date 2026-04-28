# ADR-0008: storage module for typed localStorage access

## Status
Accepted

## Context
After centralising HTTP auth into `apiClient`, the remaining raw `localStorage` calls were scattered across four locations with different key-string literals and inconsistent JSON handling:

| Caller | Key |
|--------|-----|
| `useTheme.js` | `ff-theme` |
| `ChapterReader.vue` | `novelReader_${key}` |
| `EnhancedSmartLoadingAlgorithm.js` | `smartLoading_history` |
| `authStore.js` | `authToken`, `authUser` |

Auth keys stay in `authStore` — they are identity concerns, not general storage. The other three are application preferences and caches that have no natural owner.

## Decision
Create `src/utils/storage.js` with named namespaces:

```js
storage.theme.get() / .set(v) / .remove()
storage.readerPrefs.get(key) / .set(key, v) / .remove(key)
storage.smartLoadingHistory.get() / .set(v)
```

`readerPrefs` handles JSON serialisation/deserialisation internally. `storage.theme` is consumed by `useTheme.js` (and now `themeStore`). Raw `localStorage` calls are gone from all three callers.

## Consequences
- Key strings are defined once; a rename is a one-line change.
- JSON parse errors are caught at the boundary, not scattered.
- The module has no imports → trivially unit-testable with `localStorage.clear()`.
- `authToken`/`authUser` remain in `authStore` intentionally — they are not covered by this module.
