import { describe, it, expect, beforeEach, vi } from 'vitest'

vi.stubGlobal('requestAnimationFrame', cb => cb())

describe('useScrollSignal', () => {
  beforeEach(() => {
    vi.resetModules()
    window.scrollY = 0
  })

  it('scrollY initialises to 0', async () => {
    const { useScrollSignal } = await import('./useScrollSignal.js')
    const { scrollY } = useScrollSignal()
    expect(scrollY.value).toBe(0)
  })

  it('scrollY updates when a scroll event fires', async () => {
    const { useScrollSignal } = await import('./useScrollSignal.js')
    const { scrollY } = useScrollSignal()

    window.scrollY = 300
    window.dispatchEvent(new Event('scroll'))

    expect(scrollY.value).toBe(300)
  })

  it('rapid scroll events produce one update per RAF frame', async () => {
    let rafCallback = null
    vi.stubGlobal('requestAnimationFrame', cb => { rafCallback = cb })

    const { useScrollSignal } = await import('./useScrollSignal.js')
    const { scrollY } = useScrollSignal()

    window.scrollY = 100
    window.dispatchEvent(new Event('scroll'))
    window.scrollY = 200
    window.dispatchEvent(new Event('scroll'))

    expect(scrollY.value).toBe(0)

    rafCallback()
    expect(scrollY.value).toBe(200)

    window.scrollY = 300
    window.dispatchEvent(new Event('scroll'))
    rafCallback()
    expect(scrollY.value).toBe(300)
  })
})
