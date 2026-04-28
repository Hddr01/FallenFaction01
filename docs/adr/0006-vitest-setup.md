# ADR-0006: Vitest + happy-dom as the test runner

## Status
Accepted

## Context
The project had no test runner. Adding one was required before composable tests could be written.

## Decision
Use **Vitest** with **happy-dom** as the DOM environment.

- `vitest.config.js` is a **separate file** from `vite.config.js`. The Vite config reads SSL certificates at load time via `fs.readFileSync` — importing it in a test environment would crash immediately.
- `happy-dom` over `jsdom`: faster, sufficient for composable tests. Use `jsdom` only if `getComputedStyle` or full CSS behaviour is needed.
- Test files are **colocated** next to the module they test (e.g. `useScrollSignal.test.js` next to `useScrollSignal.js`), not in a mirrored `__tests__/` tree.

## Test conventions
- Test observable contract (what callers see), not implementation mechanism.
- For event-based composables: test initial value, value after event, and batching behaviour.
- Skip testing `addEventListener` call counts — that couples tests to singleton internals, not the public interface.
- `vi.resetModules()` in `beforeEach` is required for module-singleton composables so each test gets a fresh import.

## Running tests
```
cd fallenfaction.client
npm test          # watch mode
npm test -- --run # single run (CI)
```

## Files
- `fallenfaction.client/vitest.config.js`
- `fallenfaction.client/package.json` (`"test": "vitest"` script)
