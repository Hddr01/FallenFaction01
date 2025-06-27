// main.js - Fixed version to prevent multiple auth store initializations
import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'

// Create app instance
const app = createApp(App);
const pinia = createPinia();

// Use plugins
app.use(pinia);
app.use(router);

// Mount the app
app.mount('#app');

// Handle auth store initialization and visibility changes AFTER mounting
let authStore = null;
let visibilityHandler = null;

// Initialize auth store after app is mounted
setTimeout(() => {
  try {
    // Import auth store after Pinia is set up
    import('./stores/authStore').then(({ useAuthStore }) => {
      authStore = useAuthStore();

      // Set up visibility change handler only once
      if (!visibilityHandler) {
        visibilityHandler = () => {
          if (authStore && authStore.isAuthenticated) {
            authStore.updateUserOnlineStatus(!document.hidden);
          }
        };

        document.addEventListener('visibilitychange', visibilityHandler);

        // Also handle page unload to set user offline
        window.addEventListener('beforeunload', () => {
          if (authStore && authStore.isAuthenticated) {
            // Try to set user offline before page unloads
            authStore.updateUserOnlineStatus(false);
          }
        });
      }
    });
  } catch (error) {
    console.error('Error setting up auth store:', error);
  }
}, 100);

// Cleanup function for development hot reload
if (import.meta.hot) {
  import.meta.hot.dispose(() => {
    if (visibilityHandler) {
      document.removeEventListener('visibilitychange', visibilityHandler);
      visibilityHandler = null;
    }
  });
}
