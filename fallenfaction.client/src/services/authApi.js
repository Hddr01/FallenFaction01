// authApi.js - IMPROVED VERSION with better error handling
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000, // 10 second default timeout
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token expiration and other global errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const requestUrl = error.config?.url || '';

    // Handle different error scenarios
    if (error.response?.status === 401) {
      // Only redirect to login if this isn't a logout request
      const isLogoutRequest = requestUrl.includes('/auth/logout');
      const isAcceptTermsRequest = requestUrl.includes('/auth/accept-terms');

      if (!isLogoutRequest && !isAcceptTermsRequest) {
        // Token expired or invalid for non-logout requests
        localStorage.removeItem('authToken');
        localStorage.removeItem('authUser');

        // Only redirect if we're not already on the login page
        if (!window.location.pathname.includes('/account/login')) {
          window.location.href = '/account/login';
        }
      }
    }

    if (error.response?.status === 429) {
      // Silent background calls should never navigate away from the current page
      const backgroundEndpoints = ['/auth/heartbeat', '/auth/online-status', '/auth/health'];
      const isBackgroundRequest = backgroundEndpoints.some(ep => requestUrl.includes(ep));

      if (!isBackgroundRequest && !window.location.pathname.startsWith('/error/')) {
        const retryAfter = error.response.headers['retry-after'];
        const message = retryAfter
          ? `Too many requests. Please try again in ${retryAfter} seconds.`
          : 'Too many requests. Please slow down and try again.';

        window.location.href = `/error/429?message=${encodeURIComponent(message)}&retry=true`;
      }
    }

    return Promise.reject(error);
  }
);

const authApi = {
  // Login user
  async login(credentials) {
    try {
      const response = await api.post('/auth/login', credentials);
      return response.data;
    } catch (error) {
      if (error.response?.data) {
        return error.response.data;
      }
      throw error;
    }
  },

  async acceptTerms(payload) {
    try {
      const response = await api.post('/auth/accept-terms', payload);
      return response.data;
    } catch (error) {
      if (error.response?.data) {
        return error.response.data;
      }
      throw error;
    }
  },

  // Register user
  async register(userData) {
    try {
      const response = await api.post('/auth/register', userData);
      return response.data;
    } catch (error) {
      if (error.response?.data) {
        return error.response.data;
      }
      throw error;
    }
  },

  // Logout user - IMPROVED with better error handling
  async logout() {
    try {
      // Use a shorter timeout specifically for logout requests
      const response = await api.post('/auth/logout', {}, {
        timeout: 3000, // 3 second timeout for logout
        validateStatus: function (status) {
          // Accept any status code for logout - we want to succeed locally
          return status < 500; // Only fail on server errors
        }
      });

      return response.data || { success: true, message: 'Logout successful' };
    } catch (error) {
      console.warn('Logout API call encountered an issue:', error.message);

      // Handle different error scenarios gracefully for logout
      if (error.code === 'ECONNABORTED' || error.name === 'TimeoutError') {
        console.warn('Logout request timed out - continuing with local cleanup');
      } else if (error.response?.status === 401) {
        console.warn('Already logged out on server - continuing with local cleanup');
      } else if (error.response?.status === 400) {
        console.warn('Bad request during logout - server may have already cleared session');
      } else if (error.response?.status >= 500) {
        console.warn('Server error during logout:', error.message);
      } else {
        console.warn('Network error during logout:', error.message);
      }

      // For logout, we ALWAYS want to succeed locally even if the server call fails
      return {
        success: true,
        message: 'Local logout completed',
        note: 'Server logout may have failed but local state has been cleared'
      };
    }
  },

  // Get user profile (no userId parameter needed - backend gets it from JWT)
  async getUserProfile() {
    try {
      const response = await api.get('/auth/profile');
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Check if user exists
  async checkUserExists(email) {
    try {
      const response = await api.get(`/auth/user-exists?email=${encodeURIComponent(email)}`);
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Refresh token
  async refreshToken(refreshToken) {
    try {
      const response = await api.post('/auth/refresh-token', { refreshToken });
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  // Validate token
  async validateToken() {
    try {
      const response = await api.get('/auth/validate-token');
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  async updateOnlineStatus(isOnline) {
    try {
      const response = await api.patch('/auth/online-status', null, {
        params: { isOnline },
        timeout: 5000,
        validateStatus: (status) => status >= 200 && status < 300
      });
      return response.data;
    } catch (error) {

      console.warn('Failed to update online status:', error.message);

      // Return a failure response but don't throw - this shouldn't break the app
      return {
        success: false,
        message: 'Status update failed',
        error: error.message
      };
    }
  },

  // Heartbeat to maintain online status - IMPROVED with better error handling
  async heartbeat() {
    try {
      const response = await api.post('/auth/heartbeat', {}, {
        timeout: 5000, // 5 second timeout for heartbeat
        validateStatus: function (status) {
          return status >= 200 && status < 300;
        }
      });
      return response.data;
    } catch (error) {
      console.warn('Heartbeat failed:', error.message);

      // Return a failure response but don't throw
      return {
        success: false,
        message: 'Heartbeat failed',
        error: error.message
      };
    }
  },

  // NEW: Health check method
  async healthCheck() {
    try {
      const response = await api.get('/auth/health', {
        timeout: 3000
      });
      return response.data;
    } catch (error) {
      console.warn('Health check failed:', error.message);
      return {
        status: 'unhealthy',
        error: error.message
      };
    }
  },

  async confirmEmail(userId, token) {
    try {
      const response = await api.get('/auth/confirm-email', { params: { userId, token } });
      return response.data;
    } catch (error) {
      return error.response?.data ?? { success: false, message: 'Confirmation failed.' };
    }
  },

  async resendConfirmation(email) {
    try {
      const response = await api.post('/auth/resend-confirmation', { email });
      return response.data;
    } catch (error) {
      return error.response?.data ?? { success: false, message: 'Failed to resend confirmation.' };
    }
  },

  async submitContact(formData) {
    try {
      const response = await api.post('/contact', formData);
      return response.data;
    } catch (error) {
      return error.response?.data ?? { success: false, message: 'Failed to send message.' };
    }
  },

  // NEW: Utility method to test if API is reachable
  async testConnection() {
    try {
      await this.healthCheck();
      return true;
    } catch (error) {
      return false;
    }
  }
};

export default authApi;
