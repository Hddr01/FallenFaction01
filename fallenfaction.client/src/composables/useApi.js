import { ref, computed, onScopeDispose, getCurrentScope } from 'vue'

/**
 * Loading/error/empty/data state machine for service-backed components.
 *
 * Replaces the hand-rolled pattern across ~25 components: declare
 * loading/error/data refs, write a fetch function that toggles loading,
 * try/catch around the service call, set error from result.error or the
 * caught exception, set data on success.
 *
 * Auto-detects the standard service-result envelope ({ success, data, error })
 * vs raw data. Stale responses (a second `run` while the first is still
 * in flight) are ignored. Resolved/rejected responses after the consumer's
 * effect scope has disposed are also ignored — no cross-route writes.
 *
 * @template T
 * @param {(...args: unknown[]) => Promise<unknown>} fetcher
 *   The service call. Returns either the standard envelope or raw data.
 * @param {Object} [options]
 * @param {boolean} [options.immediate=true]
 *   Whether to invoke the fetcher on construction. Set false for paginated
 *   or user-triggered fetches.
 * @param {T | null} [options.initialValue=null]
 *   Initial value for `data` before the first fetch resolves.
 * @param {(data: T | null) => boolean} [options.isEmptyCheck]
 *   Custom predicate for the `isEmpty` flag. Defaults to: null/undefined,
 *   empty array, empty object are considered empty.
 */
export function useApi(fetcher, options = {}) {
  const {
    immediate = true,
    initialValue = null,
    isEmptyCheck = defaultIsEmpty
  } = options

  const data = ref(initialValue)
  const loading = ref(immediate)
  const error = ref('')

  let requestId = 0
  let disposed = false

  // Mark disposed so in-flight responses don't write to refs after unmount.
  // getCurrentScope guards the case where useApi is called outside a setup.
  if (getCurrentScope()) {
    onScopeDispose(() => { disposed = true })
  }

  const isEmpty = computed(() =>
    !loading.value && !error.value && isEmptyCheck(data.value)
  )

  const run = async (...args) => {
    const myId = ++requestId
    loading.value = true
    error.value = ''
    try {
      const result = await fetcher(...args)
      if (disposed || myId !== requestId) return

      if (isServiceEnvelope(result)) {
        if (result.success) {
          data.value = result.data ?? null
        } else {
          error.value = result.error || 'Request failed'
        }
      } else {
        data.value = result ?? null
      }
    } catch (err) {
      if (disposed || myId !== requestId) return
      error.value = err?.message || 'An unexpected error occurred'
    } finally {
      if (!disposed && myId === requestId) loading.value = false
    }
  }

  const refresh = () => run()

  if (immediate) run()

  return { data, loading, error, isEmpty, refresh, run }
}

function defaultIsEmpty(data) {
  if (data == null) return true
  if (Array.isArray(data)) return data.length === 0
  if (typeof data === 'object') return Object.keys(data).length === 0
  return false
}

function isServiceEnvelope(result) {
  return result != null
    && typeof result === 'object'
    && typeof result.success === 'boolean'
}
