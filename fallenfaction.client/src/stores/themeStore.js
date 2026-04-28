import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { storage } from '../utils/storage.js'

export const useThemeStore = defineStore('theme', () => {
  const currentTheme = ref('dark')

  const isDark = computed(() => currentTheme.value === 'dark')

  function applyTheme(name) {
    currentTheme.value = name
    storage.theme.set(name)
    if (name === 'dark') {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  function init() {
    const saved = storage.theme.get()
    applyTheme(saved !== null ? saved : 'dark')
  }

  return { currentTheme, isDark, applyTheme, init }
})
