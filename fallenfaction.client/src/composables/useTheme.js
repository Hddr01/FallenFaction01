// composables/useTheme.js
// Singleton reactive theme state - shared across all components
import { ref } from 'vue'

const isDark = ref(true) // default = dark

function applyTheme(dark) {
  if (dark) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
}

/**
 * Call once before app mount to read localStorage and apply immediately,
 * preventing a flash of the wrong theme.
 */
export function initTheme() {
  const saved = localStorage.getItem('ff-theme')
  // Default is dark unless user has explicitly chosen light
  isDark.value = saved !== null ? saved === 'dark' : true
  applyTheme(isDark.value)
}

/**
 * Use in any component that needs to read or toggle the theme.
 */
export function useTheme() {
  function toggleTheme() {
    isDark.value = !isDark.value
    localStorage.setItem('ff-theme', isDark.value ? 'dark' : 'light')
    applyTheme(isDark.value)
  }

  return { isDark, toggleTheme }
}
