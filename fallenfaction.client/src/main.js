// main.js
import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import * as Sentry from '@sentry/vue'

// ── Apply saved theme BEFORE mount to prevent a flash of wrong theme ─────────
import { initTheme } from './composables/useTheme.js'
initTheme()

// Create app instance
const app = createApp(App)
const pinia = createPinia()

// ── Sentry ────────────────────────────────────────────────────────────────────
Sentry.init({
  app,
  dsn: 'REDACTED_SENTRY_DSN',
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
