import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useThemeStore } from './themeStore.js'

describe('themeStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    document.documentElement.classList.remove('dark')
  })

  it('defaults to dark theme', () => {
    const store = useThemeStore()
    store.init()
    expect(store.isDark).toBe(true)
    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })

  it('reads saved light theme from storage', () => {
    localStorage.setItem('ff-theme', 'light')
    const store = useThemeStore()
    store.init()
    expect(store.isDark).toBe(false)
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('applyTheme(dark) sets class and persists', () => {
    const store = useThemeStore()
    store.applyTheme('dark')
    expect(store.isDark).toBe(true)
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem('ff-theme')).toBe('dark')
  })

  it('applyTheme(light) removes class and persists', () => {
    const store = useThemeStore()
    store.applyTheme('dark')
    store.applyTheme('light')
    expect(store.isDark).toBe(false)
    expect(document.documentElement.classList.contains('dark')).toBe(false)
    expect(localStorage.getItem('ff-theme')).toBe('light')
  })
})
