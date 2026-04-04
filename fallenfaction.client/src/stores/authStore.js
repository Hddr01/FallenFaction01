// stores/authStore.js - IMPROVED VERSION with better online status handling
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import authApi from '../services/authApi';

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref(null);
  const token = ref(null);
  const isLoading = ref(false);
  const isInitialized = ref(false);
  const error = ref(null);

  // Online status management
  const heartbeatInterval = ref(null);
  const visibilityChangeHandler = ref(null);
  const beforeUnloadHandler = ref(null);
  const activityHandlers = ref([]);

  // Concurrency control with logout flag
  const isUpdatingStatus = ref(false);
  const pendingStatusUpdate = ref(null);
  const statusUpdateTimeout = ref(null);
  const isLoggingOut = ref(false);

  // Getters
  const isAuthenticated = computed(() => !!token.value && !!user.value && !isLoggingOut.value);
  const userName = computed(() => user.value?.userName || '');
  const userFullName = computed(() => {
    if (user.value?.profileName) return user.value.profileName;
    if (user.value?.firstName && user.value?.lastName) {
      return `${user.value.firstName} ${user.value.lastName}`;
    }
    return user.value?.userName || user.value?.email || '';
  });
  const userRoles = computed(() => user.value?.roles || []);
  const isAdmin = computed(() => userRoles.value.includes('Admin'));
  const isModerator = computed(() => userRoles.value.includes('Moderator') || isAdmin.value);

  // Actions
  const initializeAuth = async () => {
    if (isInitialized.value) return;

    const storedToken = localStorage.getItem('authToken');
    const storedUser = localStorage.getItem('authUser');

    if (storedToken && storedUser) {
      try {
        token.value = storedToken;
        user.value = JSON.parse(storedUser);

        await getUserProfile();
        startOnlineStatusManagement();
      } catch (error) {
        console.error('Token validation failed:', error);
        // Only logout on explicit 401 — not on network errors
        if (error?.response?.status === 401) {
          await logout();
        } else {
          // Network error — keep user logged in, start status management anyway
          startOnlineStatusManagement();
        }
      }
    }

    isInitialized.value = true;
  };

  const PENDING_TERMS_KEY = 'ff-pending-terms';

  const login = async (credentials) => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await authApi.login(credentials);

      if (response.requiresTermsAcceptance) {
        sessionStorage.setItem(
          PENDING_TERMS_KEY,
          JSON.stringify({
            email: credentials.email,
            password: credentials.password,
            termsVersion: response.termsVersion || null
          })
        );
        return {
          success: true,
          requiresTermsAcceptance: true,
          termsVersion: response.termsVersion
        };
      }

      if (response.success && response.token && response.user) {
        sessionStorage.removeItem(PENDING_TERMS_KEY);
        token.value = response.token;
        user.value = response.user;

        localStorage.setItem('authToken', response.token);
        localStorage.setItem('authUser', JSON.stringify(response.user));

        startOnlineStatusManagement();

        return { success: true };
      } else {
        error.value = response.message || 'Login failed';
        return { success: false, message: response.message, errors: response.errors };
      }
    } catch (err) {
      const errorMessage = err.response?.data?.message || 'Network error occurred';
      error.value = errorMessage;
      return { success: false, message: errorMessage };
    } finally {
      isLoading.value = false;
    }
  };

  const acceptTermsAndLogin = async () => {
    isLoading.value = true;
    error.value = null;

    try {
      const raw = sessionStorage.getItem(PENDING_TERMS_KEY);
      if (!raw) {
        error.value = 'Session expired. Please sign in again.';
        return { success: false, message: error.value };
      }

      let pending;
      try {
        pending = JSON.parse(raw);
      } catch {
        sessionStorage.removeItem(PENDING_TERMS_KEY);
        error.value = 'Session expired. Please sign in again.';
        return { success: false, message: error.value };
      }

      const { email, password } = pending;

      const response = await authApi.acceptTerms({ email, password });

      if (response.success && response.token && response.user) {
        sessionStorage.removeItem(PENDING_TERMS_KEY);
        token.value = response.token;
        user.value = response.user;

        localStorage.setItem('authToken', response.token);
        localStorage.setItem('authUser', JSON.stringify(response.user));

        startOnlineStatusManagement();

        return { success: true };
      }

      error.value = response.message || 'Could not accept terms';
      return { success: false, message: response.message, errors: response.errors };
    } catch (err) {
      const errorMessage = err.response?.data?.message || 'Network error occurred';
      error.value = errorMessage;
      return { success: false, message: errorMessage };
    } finally {
      isLoading.value = false;
    }
  };

  const register = async (userData) => {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await authApi.register(userData);

      if (response.success) {
        token.value = response.token;
        user.value = response.user;

        localStorage.setItem('authToken', response.token);
        localStorage.setItem('authUser', JSON.stringify(response.user));

        startOnlineStatusManagement();

        return { success: true };
      } else {
        error.value = response.message || 'Registration failed';
        return { success: false, message: response.message, errors: response.errors };
      }
    } catch (err) {
      const errorMessage = err.response?.data?.message || 'Network error occurred';
      error.value = errorMessage;
      return { success: false, message: errorMessage };
    } finally {
      isLoading.value = false;
    }
  };

  // IMPROVED: Logout with better error handling and immediate status update
  const logout = async () => {
    console.log('Logout initiated...');

    if (isLoggingOut.value) {
      console.log('Logout already in progress, skipping...');
      return;
    }

    isLoggingOut.value = true;
    isLoading.value = true;

    try {
      // Store user info before clearing anything
      const currentUserId = user.value?.id;
      const wasAuthenticated = !!token.value;

      // STEP 1: Stop online status management FIRST
      console.log('Stopping online status management...');
      stopOnlineStatusManagement();

      // STEP 2: Send immediate offline status (don't wait for API logout)
      if (wasAuthenticated && currentUserId && token.value) {
        try {
          console.log('Setting user offline immediately...');
          await updateUserOnlineStatus(false, true); // Force immediate update
        } catch (error) {
          console.warn('Failed to set user offline before logout:', error.message);
        }
      }

      // STEP 3: Call API logout
      if (wasAuthenticated && currentUserId && token.value) {
        try {
          console.log('Attempting API logout...');
          await authApi.logout();
          console.log('API logout successful');
        } catch (error) {
          console.warn('API logout failed, but continuing with local logout:', error.message);
        }
      }

      // STEP 4: Clear local state
      console.log('Clearing local authentication state...');

      token.value = null;
      user.value = null;
      error.value = null;
      isUpdatingStatus.value = false;
      pendingStatusUpdate.value = null;

      if (statusUpdateTimeout.value) {
        clearTimeout(statusUpdateTimeout.value);
        statusUpdateTimeout.value = null;
      }

      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
      sessionStorage.removeItem(PENDING_TERMS_KEY);

      console.log('Local state cleared');

    } catch (error) {
      console.error('Error during logout process:', error);
    } finally {
      isLoading.value = false;
      isLoggingOut.value = false;
      console.log('Logout process completed');
    }
  };

  const getUserProfile = async () => {
    try {
      const profile = await authApi.getUserProfile();
      if (profile) {
        user.value = profile;
        localStorage.setItem('authUser', JSON.stringify(profile));
      }
    } catch (error) {
      console.error('Failed to fetch user profile:', error);
      throw error;
    }
  };

  const clearError = () => {
    error.value = null;
  };

  const updateUserOnlineStatus = async (isOnline, force = false) => {
    if (!user.value || isLoggingOut.value) {
      console.log('Skipping online status update - user null or logging out');
      return;
    }

    if (isUpdatingStatus.value && !force) {
      pendingStatusUpdate.value = isOnline;

      if (statusUpdateTimeout.value) {
        clearTimeout(statusUpdateTimeout.value);
      }

      statusUpdateTimeout.value = setTimeout(() => {
        if (pendingStatusUpdate.value !== null && !isLoggingOut.value) {
          const pendingStatus = pendingStatusUpdate.value;
          pendingStatusUpdate.value = null;
          updateUserOnlineStatus(pendingStatus, true);
        }
      }, 500);

      return;
    }

    if (isUpdatingStatus.value && !force) {
      console.log('Status update already in progress, skipping...');
      return;
    }

    if (!user.value) {
      console.log('User became null during status update, aborting...');
      return;
    }

    isUpdatingStatus.value = true;

    try {
      const previousOnlineState = user.value.isOnline;
      const previousLastActive = user.value.lastActive;

      user.value.isOnline = isOnline;
      user.value.lastActive = new Date().toISOString();
      localStorage.setItem('authUser', JSON.stringify(user.value));

      const result = await retryApiCall(() => authApi.updateOnlineStatus(isOnline), 3);

      if (!result || result.success === false) {
        console.warn('Failed to update online status on server, reverting local state');

        if (user.value) {
          user.value.isOnline = previousOnlineState;
          user.value.lastActive = previousLastActive;
          localStorage.setItem('authUser', JSON.stringify(user.value));
        }
      }
    } catch (error) {
      console.error('Failed to update online status:', error);

      if (user.value) {
        user.value.isOnline = !isOnline;
        localStorage.setItem('authUser', JSON.stringify(user.value));
      }
    } finally {
      isUpdatingStatus.value = false;

      if (pendingStatusUpdate.value !== null && !isLoggingOut.value) {
        const pendingStatus = pendingStatusUpdate.value;
        pendingStatusUpdate.value = null;
        setTimeout(() => {
          if (!isLoggingOut.value) {
            updateUserOnlineStatus(pendingStatus, true);
          }
        }, 100);
      }
    }
  };

  const retryApiCall = async (apiCall, maxRetries = 3, delay = 1000) => {
    for (let attempt = 0; attempt < maxRetries; attempt++) {
      if (isLoggingOut.value) {
        console.log('Aborting API retry due to logout');
        throw new Error('Logout in progress');
      }

      try {
        const result = await apiCall();
        return result;
      } catch (error) {
        console.warn(`API call attempt ${attempt + 1} failed:`, error.message);

        if (attempt === maxRetries - 1 || isLoggingOut.value) {
          throw error;
        }

        await new Promise(resolve => setTimeout(resolve, delay * Math.pow(2, attempt)));
      }
    }
  };

  const startOnlineStatusManagement = () => {
    if (!user.value || isLoggingOut.value) return;

    console.log('Starting online status management...');

    // Set user as online
    updateUserOnlineStatus(true);

    // Start heartbeat - ping server every 60 seconds
    heartbeatInterval.value = setInterval(async () => {
      if (user.value &&
        !isLoggingOut.value &&
        document.visibilityState === 'visible' &&
        !isUpdatingStatus.value) {
        try {
          await retryApiCall(() => authApi.heartbeat(), 2);

          if (user.value && !isLoggingOut.value) {
            user.value.lastActive = new Date().toISOString();
            user.value.isOnline = true;
            localStorage.setItem('authUser', JSON.stringify(user.value));
          }
        } catch (error) {
          console.error('Heartbeat failed:', error);
        }
      }
    }, 60000);

    // Handle page visibility changes
    let visibilityTimeout;
    visibilityChangeHandler.value = () => {
      if (isLoggingOut.value) return;

      if (visibilityTimeout) {
        clearTimeout(visibilityTimeout);
      }

      visibilityTimeout = setTimeout(() => {
        if (!isLoggingOut.value) {
          if (document.visibilityState === 'visible') {
            updateUserOnlineStatus(true);
          } else {
            updateUserOnlineStatus(false);
          }
        }
      }, 250);
    };
    document.addEventListener('visibilitychange', visibilityChangeHandler.value);

    // IMPROVED: Handle page unload with better error handling
    beforeUnloadHandler.value = () => {
      if (user.value && !isLoggingOut.value && navigator.sendBeacon) {
        try {
          const data = JSON.stringify({ isOnline: false });
          const apiUrl = import.meta.env.VITE_API_BASE_URL;
          const fullUrl = `${apiUrl}/Auth/online-status`;

          // Use sendBeacon for reliable delivery during page unload
          const success = navigator.sendBeacon(fullUrl, new Blob([data], {
            type: 'application/json'
          }));

          if (!success) {
            console.warn('SendBeacon failed, trying synchronous request');
            // Fallback to synchronous request (last resort)
            try {
              fetch(fullUrl, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                  'Authorization': `Bearer ${token.value}`
                },
                body: data,
                keepalive: true
              });
            } catch (error) {
              console.warn('Fallback request also failed:', error);
            }
          }
        } catch (error) {
          console.warn('Error in beforeUnload handler:', error);
        }
      }
    };
    window.addEventListener('beforeunload', beforeUnloadHandler.value);

    // Handle user activity (throttled)
    const activityEvents = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart', 'click'];
    let lastActivityUpdate = 0;
    const ACTIVITY_THROTTLE = 30000;

    const handleActivity = () => {
      if (isLoggingOut.value || !user.value) {
        return;
      }

      const now = Date.now();

      if (user.value) {
        user.value.lastActive = new Date().toISOString();
        localStorage.setItem('authUser', JSON.stringify(user.value));

        if (now - lastActivityUpdate > ACTIVITY_THROTTLE) {
          lastActivityUpdate = now;
          updateUserOnlineStatus(true);
        }
      }
    };

    // Store handlers for proper cleanup
    activityEvents.forEach(event => {
      document.addEventListener(event, handleActivity, { passive: true });
      activityHandlers.value.push({ event, handler: handleActivity });
    });
  };

  const stopOnlineStatusManagement = () => {
    console.log('Stopping online status management...');

    // Set logging out flag immediately
    isLoggingOut.value = true;

    // Clear heartbeat interval
    if (heartbeatInterval.value) {
      clearInterval(heartbeatInterval.value);
      heartbeatInterval.value = null;
      console.log('Heartbeat interval cleared');
    }

    // Clear pending status updates
    if (statusUpdateTimeout.value) {
      clearTimeout(statusUpdateTimeout.value);
      statusUpdateTimeout.value = null;
      console.log('Status update timeout cleared');
    }

    isUpdatingStatus.value = false;
    pendingStatusUpdate.value = null;

    // Remove visibility change handler
    if (visibilityChangeHandler.value) {
      document.removeEventListener('visibilitychange', visibilityChangeHandler.value);
      visibilityChangeHandler.value = null;
      console.log('Visibility change handler removed');
    }

    // Remove beforeunload handler
    if (beforeUnloadHandler.value) {
      window.removeEventListener('beforeunload', beforeUnloadHandler.value);
      beforeUnloadHandler.value = null;
      console.log('Before unload handler removed');
    }

    // Remove activity handlers properly
    activityHandlers.value.forEach(({ event, handler }) => {
      document.removeEventListener(event, handler, { passive: true });
    });
    activityHandlers.value = [];
    console.log('Activity handlers removed');

    console.log('Online status management stopped');
  };

  const refreshUserProfile = async () => {
    if (!isAuthenticated.value || isLoggingOut.value) return;

    try {
      await getUserProfile();
    } catch (error) {
      console.error('Failed to refresh user profile:', error);
    }
  };

  return {
    // State
    user,
    token,
    isLoading,
    isInitialized,
    error,

    // Getters
    isAuthenticated,
    userName,
    userFullName,
    userRoles,
    isAdmin,
    isModerator,

    // Actions
    initializeAuth,
    login,
    acceptTermsAndLogin,
    register,
    logout,
    getUserProfile,
    clearError,
    updateUserOnlineStatus,
    refreshUserProfile,
    startOnlineStatusManagement,
    stopOnlineStatusManagement
  };
});
