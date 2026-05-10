import { describe, it, expect, vi } from 'vitest'
import { effectScope, nextTick } from 'vue'
import { useApi } from './useApi.js'

const flush = async () => {
  await nextTick()
  await Promise.resolve()
}

const deferred = () => {
  let resolve, reject
  const promise = new Promise((res, rej) => { resolve = res; reject = rej })
  return { promise, resolve, reject }
}

describe('useApi', () => {
  it('auto-fetches on construction when immediate=true (default)', async () => {
    const fetcher = vi.fn(async () => ({ success: true, data: [1, 2, 3] }))
    const { data, loading, error, isEmpty } = useApi(fetcher)
    expect(loading.value).toBe(true)
    await flush()
    expect(fetcher).toHaveBeenCalledOnce()
    expect(loading.value).toBe(false)
    expect(error.value).toBe('')
    expect(data.value).toEqual([1, 2, 3])
    expect(isEmpty.value).toBe(false)
  })

  it('does not fetch when immediate=false', async () => {
    const fetcher = vi.fn(async () => ({ success: true, data: 'x' }))
    const { data, loading, run } = useApi(fetcher, { immediate: false })
    expect(loading.value).toBe(false)
    expect(fetcher).not.toHaveBeenCalled()
    await run()
    expect(fetcher).toHaveBeenCalledOnce()
    expect(data.value).toBe('x')
  })

  it('exposes the service-error message via error ref', async () => {
    const { error, data } = useApi(async () => ({ success: false, error: 'Forbidden' }))
    await flush()
    expect(error.value).toBe('Forbidden')
    expect(data.value).toBeNull()
  })

  it('exposes thrown-exception message via error ref', async () => {
    const { error, data, loading } = useApi(async () => { throw new Error('boom') })
    await flush()
    expect(error.value).toBe('boom')
    expect(loading.value).toBe(false)
    expect(data.value).toBeNull()
  })

  it('handles raw data (non-envelope)', async () => {
    const { data } = useApi(async () => ({ id: 1, name: 'A' }))
    await flush()
    expect(data.value).toEqual({ id: 1, name: 'A' })
  })

  it('isEmpty is true for null / [] / {} after a successful fetch', async () => {
    const cases = [
      { fetcher: async () => ({ success: true, data: null }) },
      { fetcher: async () => ({ success: true, data: [] }) },
      { fetcher: async () => ({ success: true, data: {} }) }
    ]
    for (const { fetcher } of cases) {
      const { isEmpty } = useApi(fetcher)
      await flush()
      expect(isEmpty.value).toBe(true)
    }
  })

  it('isEmpty respects a custom isEmptyCheck', async () => {
    const { isEmpty } = useApi(
      async () => ({ success: true, data: { items: [] } }),
      { isEmptyCheck: d => !d || (d.items?.length ?? 0) === 0 }
    )
    await flush()
    expect(isEmpty.value).toBe(true)
  })

  it('refresh() retriggers the fetcher and clears the prior error', async () => {
    let attempt = 0
    const fetcher = vi.fn(async () => {
      attempt++
      return attempt === 1
        ? { success: false, error: 'first failure' }
        : { success: true, data: 'ok' }
    })
    const { data, error, refresh } = useApi(fetcher)
    await flush()
    expect(error.value).toBe('first failure')
    await refresh()
    expect(error.value).toBe('')
    expect(data.value).toBe('ok')
    expect(fetcher).toHaveBeenCalledTimes(2)
  })

  it('ignores stale responses when run is called twice in a row', async () => {
    const slow = deferred()
    const fast = deferred()
    let call = 0
    const fetcher = vi.fn(async () => {
      call++
      return call === 1 ? slow.promise : fast.promise
    })
    const { data, run } = useApi(fetcher, { immediate: false })
    const slowRun = run()
    const fastRun = run()
    fast.resolve({ success: true, data: 'fast' })
    await fastRun
    slow.resolve({ success: true, data: 'slow' })
    await slowRun
    expect(data.value).toBe('fast')
  })

  it('does not write to refs after the effect scope is disposed', async () => {
    const d = deferred()
    let api
    const scope = effectScope()
    scope.run(() => {
      api = useApi(() => d.promise, { immediate: false })
      api.run()
    })
    scope.stop()
    d.resolve({ success: true, data: 'late' })
    await flush()
    // data was never written because the scope was stopped before resolution
    expect(api.data.value).toBeNull()
  })

  it('can pass arguments through run(...args) to the fetcher', async () => {
    const fetcher = vi.fn(async (page, size) => ({ success: true, data: { page, size } }))
    const { data, run } = useApi(fetcher, { immediate: false })
    await run(2, 25)
    expect(fetcher).toHaveBeenCalledWith(2, 25)
    expect(data.value).toEqual({ page: 2, size: 25 })
  })
})
