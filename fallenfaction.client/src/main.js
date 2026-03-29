// main.js
import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'

// ── Apply saved theme BEFORE mount to prevent a flash of wrong theme ─────────
import { initTheme } from './composables/useTheme.js'
initTheme()

// Create app instance
const app = createApp(App)
const pinia = createPinia()

// Use plugins
app.use(pinia)
app.use(router)

// Mount the app
app.mount('#app')

// Handle auth store initialisation and visibility changes AFTER mounting
let authStore = null
let visibilityHandler = null

setTimeout(() => {
  try {
    import('./stores/authStore').then(({ useAuthStore }) => {
      authStore = useAuthStore()

      if (!visibilityHandler) {
        visibilityHandler = () => {
          if (authStore && authStore.isAuthenticated) {
            authStore.updateUserOnlineStatus(!document.hidden)
          }
        }

        document.addEventListener('visibilitychange', visibilityHandler)

        window.addEventListener('beforeunload', () => {
          if (authStore && authStore.isAuthenticated) {
            authStore.updateUserOnlineStatus(false)
          }
        })
      }
    })
  } catch (error) {
    console.error('Error setting up auth store:', error)
  }
}, 100)

// Cleanup for development hot reload
if (import.meta.hot) {
  import.meta.hot.dispose(() => {
    if (visibilityHandler) {
      document.removeEventListener('visibilitychange', visibilityHandler)
      visibilityHandler = null
    }
  })
}
