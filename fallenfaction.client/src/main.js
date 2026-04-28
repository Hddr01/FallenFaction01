// main.js
import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import * as Sentry from '@sentry/vue'

// Create app instance
const app = createApp(App)
const pinia = createPinia()

// Pinia must be active before any store is used (including themeStore via initTheme)
app.use(pinia)
app.use(router)

// ── Apply saved theme BEFORE mount to prevent a flash of wrong theme ─────────
// initTheme calls useThemeStore(), which requires an active Pinia instance above
import { initTheme } from './composables/useTheme.js'
initTheme()

// ── Sentry ────────────────────────────────────────────────────────────────────
Sentry.init({
  app,
  dsn: import.meta.env.VITE_SENTRY_DSN || undefined,
  sendDefaultPii: true,
  integrations: [
    Sentry.browserTracingIntegration({ router }),
    Sentry.replayIntegration(),
  ],
  tracesSampleRate: 1.0,
  tracePropagationTargets: [
    'localhost',
    /^https:\/\/fallenfaction\.com\/api/,
  ],
  replaysSessionSampleRate: 0.1,
  replaysOnErrorSampleRate: 1.0,
  enableLogs: true,
  // Only send events in production builds — no noise during local dev
  enabled: import.meta.env.PROD,
})

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
