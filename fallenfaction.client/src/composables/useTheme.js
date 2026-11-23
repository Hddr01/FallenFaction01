// composables/useTheme.js
import { computed } from 'vue'
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

  // A live ref, not `store.isDark` (Pinia unwraps that to a plain boolean snapshot):
  // ChapterReader reads isDark.value and watches it.
  const isDark = computed(() => store.isDark)

  return { isDark, toggleTheme }
}
