// composables/useTheme.js
import { useThemeStore } from '../stores/themeStore.js'

export function initTheme() {
  const store = useThemeStore()
  store.init()
}

export function useTheme() {
  const store = useThemeStore()

  function toggleTheme() {
    store.applyTheme(store.isDark ? 'light' : 'dark')
  }

  return { isDark: store.isDark, toggleTheme }
}
