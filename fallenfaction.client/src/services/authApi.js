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
    // Handle different error scenarios
    if (error.response?.status === 401) {
      // Only redirect to login if this isn't a logout request
      const isLogoutRequest = error.config?.url?.includes('/auth/logout');

      if (!isLogoutRequest) {
        // Token expired or invalid for non-logout requests
        localStorage.removeItem('authToken');
        localStorage.removeItem('authUser');

        // Only redirect if we're not already on the login page
        if (!window.location.pathname.includes('/account/login')) {
          window.location.href = '/account/login';
        }
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

  // IMPROVED: Update online status with better method handling
  async updateOnlineStatus(isOnline) {
    try {
      // Try PATCH first (preferred method)
      const response = await api.patch('/auth/online-status', { isOnline }, {
        timeout: 5000, // 5 second timeout
        validateStatus: function (status) {
          // Accept 200-299 status codes as success
          return status >= 200 && status < 300;
        }
      });
      return response.data;
    } catch (error) {
      // If PATCH fails with 405 (Method Not Allowed), try POST
      if (error.response?.status === 405) {
        try {
          console.log('PATCH not allowed, trying POST for online status update');
          const response = await api.post('/auth/online-status', { isOnline }, {
            timeout: 5000,
            validateStatus: function (status) {
              return status >= 200 && status < 300;
            }
          });
          return response.data;
        } catch (postError) {
          console.warn('Both PATCH and POST failed for online status:', postError.message);
        }
      }

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
