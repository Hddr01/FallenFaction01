import { describe, it, expect, beforeEach } from 'vitest'
import { nextTick, watch } from 'vue'
import { setActivePinia, createPinia } from 'pinia'
import { initTheme, useTheme } from './useTheme.js'

describe('useTheme', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    document.documentElement.classList.remove('dark')
  })

  it('exposes isDark as a ref reflecting the saved theme (ChapterReader reads isDark.value)', () => {
    localStorage.setItem('ff-theme', 'dark')
    initTheme()
    const { isDark } = useTheme()
    expect(isDark.value).toBe(true)
  })

  it('isDark stays live after toggleTheme', () => {
    initTheme()
    const { isDark, toggleTheme } = useTheme()
    expect(isDark.value).toBe(true)
    toggleTheme()
    expect(isDark.value).toBe(false)
    expect(localStorage.getItem('ff-theme')).toBe('light')
  })

  it('isDark is watchable', async () => {
    initTheme()
    const { isDark, toggleTheme } = useTheme()
    const seen = []
    watch(isDark, v => seen.push(v))
    toggleTheme()
    await nextTick()
    expect(seen).toEqual([false])
  })
})
