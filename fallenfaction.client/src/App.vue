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

    <!-- Enhanced Debug Info (development only) -->
    <div v-if="showDebugInfo && isDev" class="debug-info">
      <h4>🚀 Enhanced Smart Loading Debug</h4>

      <h5>Device & Network</h5>
      <p>Device Score: <strong>{{ deviceCapabilities.score }}</strong> ({{ deviceCapabilities.category }})</p>
      <p>Network Score: <strong>{{ networkInfo.score }}</strong> ({{ networkInfo.type }})</p>
      <p>Loading Strategy: <strong>{{ loadingStrategy.name }}</strong></p>
      <p>Timeout: <strong>{{ loadingStrategy.timeout }}ms</strong></p>
      <p>Actual Load Time: <strong>{{ actualLoadTime }}ms</strong></p>

      <h5>Navigation</h5>
      <p>Current Route: <strong>{{ $route.path }}</strong></p>
      <p>Hide Navigation: <strong>{{ hideNavigation ? '✅' : '❌' }}</strong></p>
      <p>Route Loading: <strong>{{ showRouteLoading ? '⏳' : '✅' }}</strong></p>
      <p v-if="showRouteLoading">Loading: <strong>{{ routeLoadingConfig.text }}</strong></p>

      <h5>Authentication Status</h5>
      <p>Auth Initialized: <strong>{{ authStore.isInitialized ? '✅' : '⏳' }}</strong></p>
      <p>User Logged In: <strong>{{ authStore.isAuthenticated ? '✅' : '❌' }}</strong></p>
      <p v-if="authStore.user">User: <strong>{{ authStore.userFullName }}</strong></p>

      <h5>Resource Loading</h5>
      <p>Router: {{ resourcesLoaded.router ? '✅' : '⏳' }}</p>
      <p>DOM: {{ resourcesLoaded.dom ? '✅' : '⏳' }}</p>
      <p>Critical: {{ resourcesLoaded.critical ? '✅' : '⏳' }}</p>
      <p>Auth: {{ resourcesLoaded.auth ? '✅' : '⏳' }}</p>

      <div v-if="analytics" class="ml-analytics">
        <h5>🤖 ML Analytics</h5>
        <p>Total Sessions: <strong>{{ analytics.totalSessions }}</strong></p>
        <p>Avg Accuracy: <strong>{{ Math.round(analytics.averageAccuracy * 100) }}%</strong></p>
        <p>Best Strategy: <strong>{{ analytics.mostSuccessfulStrategy }}</strong></p>
        <p>Learning Active: <strong>{{ analytics.learningEnabled ? '🧠' : '💤' }}</strong></p>
      </div>

      <button @click="showDebugInfo = false">Close Debug</button>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted, onUnmounted, provide, getCurrentInstance, nextTick, computed, watch } from 'vue';
  import { useRouter, useRoute } from 'vue-router';
  import { useAuthStore } from './stores/authStore';
  import NavBar from './layout/NavBar.vue';
  import Footer from './layout/Footer.vue';
  import LoadingScreen from './LoadingScreen.vue';
  import ErrorHandler from './services/ErrorHandler.js';
  import { createApiClient } from './services/ApiErrorHandler.js';
  // Import the enhanced algorithm
  import EnhancedSmartLoadingAlgorithm from './services/EnhancedSmartLoadingAlgorithm.js';

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

  // Smart Loading Algorithm Variables
  const deviceCapabilities = ref({});
  const networkInfo = ref({});
  const loadingStrategy = ref({});
  const loadingStartTime = ref(0);
  const actualLoadTime = ref(0);
  const analytics = ref(null);
  const resourcesLoaded = ref({
    router: false,
    dom: false,
    critical: false,
    auth: false
  });

  // Create error handler instance with router
  const errorHandler = new ErrorHandler(router);
  const apiClient = createApiClient(errorHandler);

  provide('apiClient', apiClient);
  provide('errorHandler', errorHandler);

  let unsubscribeFromErrors;
  let notificationId = 0;

  // Initialize enhanced smart loading with configuration
  const smartLoader = new EnhancedSmartLoadingAlgorithm({
    minTime: 250,
    maxTime: 4000,
    fastThreshold: 80,
    enableLearning: true
  });

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
    const loadingDuration = deviceCapabilities.value.score > 80 ? 300 : 600;

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
    if (smartLoader.config.LEARNING_ENABLED) {
      const realPerformanceTime = Math.min(resourceTime, 1000);

      smartLoader.recordPerformance(
        deviceCapabilities.value.score,
        networkInfo.value.score || 75,
        loadingStrategy.value.timeout,
        realPerformanceTime,
        loadingStrategy.value.name.replace(' (ML-Adjusted)', '')
      );
    }

    analytics.value = smartLoader.getAnalytics();
    showInitialLoading.value = false;
  };

  const startAppLoadingComplete = () => {
    if (loadingScreenRef.value) {
      loadingScreenRef.value.startFadeOut();
    }
  };

  // Enhanced smart loading execution with auth integration
  const executeSmartLoading = async () => {
    loadingStartTime.value = Date.now();

    deviceCapabilities.value = smartLoader.analyzeDeviceCapabilities();
    networkInfo.value = smartLoader.analyzeNetworkInfo();

    loadingStrategy.value = smartLoader.determineLoadingStrategy(
      deviceCapabilities.value.score,
      networkInfo.value.score
    );

    try {
      await smartLoader.preloadCriticalResources(loadingStrategy.value);
    } catch (error) {
      // Silently handle preloading errors
    }

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
        }, Math.random() * 200 + 100);
      })
    );

    try {
      await Promise.all(resourcePromises);

      const elapsedTime = Date.now() - loadingStartTime.value;
      const remainingTime = Math.max(0, loadingStrategy.value.timeout - elapsedTime);

      const finalTimeout = Math.max(
        smartLoader.config.MINIMUM_LOADING_TIME - elapsedTime,
        remainingTime
      );

      setTimeout(() => {
        startAppLoadingComplete();
      }, finalTimeout);

    } catch (error) {
      setTimeout(() => startAppLoadingComplete(), loadingStrategy.value.timeout);
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

  // Enhanced development helpers
  if (isDev) {
    window.smartLoader = smartLoader;
    window.authStore = authStore;
    window.showLoadingDebug = () => {
      analytics.value = smartLoader.getAnalytics();
      showDebugInfo.value = true;
    };
    window.triggerRouteLoading = () => {
      showRouteLoadingScreen();
    };
    window.getLoadingAnalytics = () => smartLoader.getAnalytics();
    window.clearLoadingHistory = () => {
      localStorage.removeItem('smartLoading_history');
      smartLoader.performanceHistory = [];
    };
    window.resetToFastLoading = () => {
      localStorage.removeItem('smartLoading_history');
      smartLoader.performanceHistory = [];
      const fastExamples = [
        { deviceScore: 100, networkScore: 95, predictedTime: 300, actualTime: 150, strategy: 'High Performance', accuracy: 0.5, timestamp: Date.now() },
        { deviceScore: 100, networkScore: 95, predictedTime: 300, actualTime: 120, strategy: 'High Performance', accuracy: 0.6, timestamp: Date.now() },
        { deviceScore: 100, networkScore: 95, predictedTime: 300, actualTime: 180, strategy: 'High Performance', accuracy: 0.4, timestamp: Date.now() }
      ];
      smartLoader.performanceHistory = fastExamples;
      smartLoader.savePerformanceHistory();
    };
    window.deviceCapabilities = deviceCapabilities;
    window.networkInfo = networkInfo;
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

