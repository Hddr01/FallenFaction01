<template>
  <div class="app-container">
    <!-- Initial Loading Screen with Smart Algorithm -->
    <LoadingScreen v-if="showInitialLoading"
                   ref="loadingScreenRef"
                   :loading-gif="loadingConfig.gif"
                   :loading-text="loadingConfig.text"
                   :loading-type="loadingConfig.type"
                   @loading-complete="onInitialLoadingComplete" />

    <!-- Route Loading Screen (using your custom component) -->
    <LoadingScreen v-else-if="showRouteLoading"
                   ref="routeLoadingScreenRef"
                   :loading-gif="routeLoadingConfig.gif"
                   :loading-text="routeLoadingConfig.text"
                   :loading-type="routeLoadingConfig.type"
                   @loading-complete="onRouteLoadingComplete" />

    <!-- Main App Content with Router -->
    <div v-else class="main-app">
      <!-- Navigation Bar - FIXED: Hidden when route has hideNavigation meta -->
      <NavBar v-if="!hideNavigation" />

      <!-- Router View with conditional padding for navbar -->
      <div class="router-content" :class="{ 'no-navbar': hideNavigation }">
        <router-view v-slot="{ Component, route }">
          <transition name="page" mode="out-in">
            <component :is="Component" :key="route.path" />
          </transition>
        </router-view>
      </div>

      <!-- Footer - FIXED: Hidden when route has hideNavigation meta -->
      <Footer v-if="!hideNavigation" />
    </div>

    <!-- API Error Notifications -->
    <div v-if="apiNotifications.length > 0" class="api-notifications">
      <div v-for="notification in apiNotifications"
           :key="notification.id"
           class="api-notification"
           :class="notification.type">
        <span class="notification-message">{{ notification.message }}</span>
        <button @click="dismissNotification(notification.id)" class="dismiss-btn">×</button>
      </div>
    </div>

    <!-- Auth Error Notifications -->
    <div v-if="authStore.error" class="auth-error-notification">
      <div class="api-notification error">
        <span class="notification-message">{{ authStore.error }}</span>
        <button @click="authStore.clearError()" class="dismiss-btn">×</button>
      </div>
    </div>

    <!-- Debug Info (development only) -->
    <div v-if="showDebugInfo && isDev" class="debug-info">
      <h4>Debug Info</h4>
      <h5>Navigation</h5>
      <p>Current Route: <strong>{{ $route.path }}</strong></p>
      <p>Hide Navigation: <strong>{{ hideNavigation ? '✅' : '❌' }}</strong></p>
      <p>Route Loading: <strong>{{ showRouteLoading ? '⏳' : '✅' }}</strong></p>
      <h5>Authentication Status</h5>
      <p>Auth Initialized: <strong>{{ authStore.isInitialized ? '✅' : '⏳' }}</strong></p>
      <p>User Logged In: <strong>{{ authStore.isAuthenticated ? '✅' : '❌' }}</strong></p>
      <p v-if="authStore.user">User: <strong>{{ authStore.userFullName }}</strong></p>
      <h5>Resource Loading</h5>
      <p>Router: {{ resourcesLoaded.router ? '✅' : '⏳' }}</p>
      <p>DOM: {{ resourcesLoaded.dom ? '✅' : '⏳' }}</p>
      <p>Critical: {{ resourcesLoaded.critical ? '✅' : '⏳' }}</p>
      <p>Auth: {{ resourcesLoaded.auth ? '✅' : '⏳' }}</p>
      <button @click="showDebugInfo = false">Close Debug</button>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted, onUnmounted, getCurrentInstance, nextTick, computed, watch } from 'vue';
  import { useRouter, useRoute } from 'vue-router';
  import { useAuthStore } from './stores/authStore';
  import NavBar from './layout/NavBar.vue';
  import Footer from './layout/Footer.vue';
  import LoadingScreen from './LoadingScreen.vue';
  import ErrorHandler from './services/ErrorHandler.js';

  const router = useRouter();
  const route = useRoute();
  const authStore = useAuthStore();

  // Separate initial loading from route loading
  const showInitialLoading = ref(true);
  const showRouteLoading = ref(false);

  const loadingScreenRef = ref(null);
  const routeLoadingScreenRef = ref(null);
  const apiNotifications = ref([]);
  const showDebugInfo = ref(false);

  // Development environment check
  const isDev = import.meta.env.DEV;

  // FIXED: Computed property to determine if navigation should be hidden
  const hideNavigation = computed(() => {
    return route.meta?.hideNavigation || false;
  });

  // Loading state variables
  const loadingStartTime = ref(0);
  const actualLoadTime = ref(0);
  const resourcesLoaded = ref({
    router: false,
    dom: false,
    critical: false,
    auth: false
  });

  const errorHandler = new ErrorHandler(router);

  let unsubscribeFromErrors;
  let notificationId = 0;

  // Simple static loading strategy (no image preloading needed for novels)
  const loadingStrategy = ref({ gif: '/img/happy_girl.gif', text: 'Loading...', type: 'standard' });

  // Computed loading configuration for initial loading
  const loadingConfig = computed(() => {
    const baseConfig = {
      gif: loadingStrategy.value.gif || '/img/happy_girl.gif',
      text: loadingStrategy.value.text || 'Loading...',
      type: loadingStrategy.value.type || 'standard'
    };

    if (!resourcesLoaded.value.auth && authStore.isInitialized === false) {
      baseConfig.text = 'Initializing authentication...';
    } else if (resourcesLoaded.value.auth && !resourcesLoaded.value.router) {
      baseConfig.text = 'Setting up navigation...';
    } else if (resourcesLoaded.value.router && !resourcesLoaded.value.critical) {
      baseConfig.text = 'Loading core features...';
    }

    return baseConfig;
  });

  // Computed loading configuration for route changes
  const routeLoadingConfig = computed(() => {
    const routeConfigs = {
      '/account/login': {
        gif: '/img/happy_girl.gif',
        text: 'Loading login page...',
        type: 'auth'
      },
      '/account/register': {
        gif: '/img/happy_girl.gif',
        text: 'Loading registration...',
        type: 'auth'
      },
      '/profile': {
        gif: '/img/happy_girl.gif',
        text: 'Loading your profile...',
        type: 'profile'
      },
      '/home/search': {
        gif: '/img/happy_girl.gif',
        text: 'Loading search...',
        type: 'search'
      },
      '/home/cataloge': {
        gif: '/img/happy_girl.gif',
        text: 'Loading catalog...',
        type: 'catalog'
      }
    };

    return routeConfigs[route.path] || {
      gif: '/img/happy_girl.gif',
      text: 'Loading page...',
      type: 'standard'
    };
  });

  // Watch for route changes to show loading
  watch(() => route.path, (newPath, oldPath) => {
    if (oldPath && newPath !== oldPath && !showInitialLoading.value) {
      showRouteLoadingScreen();
    }
  });

  const showRouteLoadingScreen = () => {
    showRouteLoading.value = true;

    // Determine loading duration based on device performance
    const loadingDuration = 400;

    // Auto-complete route loading after calculated duration
    setTimeout(() => {
      if (routeLoadingScreenRef.value && showRouteLoading.value) {
        routeLoadingScreenRef.value.startFadeOut();
      }
    }, loadingDuration);
  };

  const onRouteLoadingComplete = () => {
    showRouteLoading.value = false;
  };

  const onInitialLoadingComplete = () => {
    const totalTime = Date.now() - loadingStartTime.value;
    const resourceTime = actualLoadTime.value || totalTime;

    actualLoadTime.value = totalTime;

    // Record performance for machine learning
    showInitialLoading.value = false;
  };

  const startAppLoadingComplete = () => {
    if (loadingScreenRef.value) {
      loadingScreenRef.value.startFadeOut();
    }
  };

  // Simple loading execution without image preloading
  const executeSmartLoading = async () => {
    loadingStartTime.value = Date.now();

    const resourcePromises = [];

    // Authentication initialization
    resourcePromises.push(
      authStore.initializeAuth().then(() => {
        resourcesLoaded.value.auth = true;
      }).catch((error) => {
        console.error('Auth initialization failed:', error);
        resourcesLoaded.value.auth = true;
      })
    );

    // Router ready
    resourcePromises.push(
      router.isReady().then(() => {
        resourcesLoaded.value.router = true;
      })
    );

    // DOM ready
    resourcePromises.push(
      nextTick().then(() => {
        resourcesLoaded.value.dom = true;
      })
    );

    // Critical resources
    resourcePromises.push(
      new Promise(resolve => {
        setTimeout(() => {
          resourcesLoaded.value.critical = true;
          resolve();
        }, 200);
      })
    );

    try {
      await Promise.all(resourcePromises);
      const elapsedTime = Date.now() - loadingStartTime.value;
      const remainingTime = Math.max(0, 500 - elapsedTime);
      setTimeout(() => { startAppLoadingComplete(); }, remainingTime);
    } catch (error) {
      setTimeout(() => startAppLoadingComplete(), 500);
    }
  };

  // API notification functions
  const handleApiNotification = (event) => {
    const { message, status, canRetry } = event.detail;

    addNotification({
      message,
      type: status >= 500 ? 'error' : 'warning',
      canRetry,
      duration: status >= 500 ? 8000 : 5000
    });
  };

  const addNotification = (notification) => {
    const id = ++notificationId;
    const newNotification = {
      id,
      ...notification,
      timestamp: Date.now()
    };

    apiNotifications.value.push(newNotification);

    if (notification.duration) {
      setTimeout(() => {
        dismissNotification(id);
      }, notification.duration);
    }
  };

  const dismissNotification = (id) => {
    const index = apiNotifications.value.findIndex(n => n.id === id);
    if (index > -1) {
      apiNotifications.value.splice(index, 1);
    }
  };

  onMounted(async () => {
    errorHandler.setupGlobalHandlers();

    unsubscribeFromErrors = errorHandler.onErrorChange((error) => {
      if (error) {
        router.push(`/error/${error.statusCode}?message=${encodeURIComponent(error.message)}`);
      }
    });

    window.addEventListener('api-error', handleApiNotification);

    const app = getCurrentInstance()?.appContext.app;
    if (app) {
      app.config.errorHandler = (error, instance, info) => {
        errorHandler.handleVueError(error, instance, info);
      };
    }

    await executeSmartLoading();
  });

  onUnmounted(() => {
    if (unsubscribeFromErrors) {
      unsubscribeFromErrors();
    }
    window.removeEventListener('api-error', handleApiNotification);
  });

  // Development helpers
  if (isDev) {
    window.authStore = authStore;
    window.showLoadingDebug = () => { showDebugInfo.value = true; };
    window.triggerRouteLoading = () => { showRouteLoadingScreen(); };
    window.loadingStrategy = loadingStrategy;
  }
</script>

<style scoped>
  .app-container {
    min-height: 100vh;
    position: relative;
  }

  .main-app {
    min-height: 100vh;
  }

  /* FIXED: Conditional padding based on navbar visibility */
  .router-content {
    padding-top: 60px;
    min-height: calc(100vh - 60px);
  }

    .router-content.no-navbar {
      padding-top: 0;
      min-height: 100vh;
    }

  /* Page Transitions */
  .page-enter-active, .page-leave-active {
    transition: opacity 0.3s ease;
  }

  .page-enter-from, .page-leave-to {
    opacity: 0;
  }

  /* Debug Info */
  .debug-info {
    position: fixed;
    top: 20px;
    left: 20px;
    background: rgba(0, 0, 0, 0.9);
    color: white;
    padding: 16px;
    border-radius: 8px;
    z-index: 3000;
    max-width: 350px;
    font-family: 'Courier New', monospace;
    font-size: 12px;
    line-height: 1.4;
    backdrop-filter: blur(10px);
    border: 1px solid rgba(255, 255, 255, 0.2);
    max-height: 80vh;
    overflow-y: auto;
  }

    .debug-info h4 {
      margin: 0 0 12px 0;
      color: #4ecdc4;
      font-size: 14px;
    }

    .debug-info h5 {
      margin: 12px 0 8px 0;
      color: #ff6b6b;
      font-size: 12px;
    }

    .debug-info p {
      margin: 4px 0;
      color: #fff;
    }

    .debug-info button {
      margin-top: 12px;
      padding: 6px 12px;
      background: #4ecdc4;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 11px;
    }

      .debug-info button:hover {
        background: #45a8a0;
      }

  .ml-analytics {
    border-top: 1px solid rgba(255, 255, 255, 0.2);
    margin-top: 12px;
    padding-top: 12px;
  }

  .api-notifications {
    position: fixed;
    top: 80px;
    right: 20px;
    z-index: 2000;
    max-width: 400px;
  }

  .auth-error-notification {
    position: fixed;
    top: 80px;
    right: 20px;
    z-index: 2000;
    max-width: 400px;
  }

  .api-notification {
    background: rgba(0, 0, 0, 0.9);
    color: white;
    padding: 12px 16px;
    margin-bottom: 8px;
    border-radius: 8px;
    border-left: 4px solid;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
    backdrop-filter: blur(10px);
    animation: slideInRight 0.3s ease-out;
  }

    .api-notification.error {
      border-left-color: #ff6b6b;
    }

    .api-notification.warning {
      border-left-color: #ffd93d;
    }

    .api-notification.success {
      border-left-color: #6bcf7f;
    }

  .notification-message {
    flex: 1;
    font-size: 14px;
    line-height: 1.4;
  }

  .dismiss-btn {
    background: none;
    border: none;
    color: rgba(255, 255, 255, 0.7);
    font-size: 18px;
    cursor: pointer;
    padding: 0;
    margin-left: 12px;
    width: 20px;
    height: 20px;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: color 0.2s;
  }

    .dismiss-btn:hover {
      color: white;
    }

  @keyframes slideInRight {
    from {
      transform: translateX(100%);
      opacity: 0;
    }

    to {
      transform: translateX(0);
      opacity: 1;
    }
  }

  /* Responsive adjustments */
  @media (max-width: 768px) {
    .debug-info {
      left: 10px;
      right: 10px;
      max-width: none;
      font-size: 11px;
    }

    .api-notifications,
    .auth-error-notification {
      right: 10px;
      left: 10px;
      max-width: none;
    }

    .api-notification {
      padding: 10px 12px;
      font-size: 13px;
    }

    .router-content {
      padding-top: 60px;
    }

      .router-content.no-navbar {
        padding-top: 0;
      }
  }
</style>
