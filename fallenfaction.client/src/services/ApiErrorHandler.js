// src/services/ApiErrorHandler.js - Axios-based version
import axios from 'axios';

export class ApiErrorHandler {
  constructor(errorHandler) {
    this.errorHandler = errorHandler;
    this.createAxiosInstance();
  }

  createAxiosInstance() {
    // Create centralized axios instance
    this.api = axios.create({
      baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      },
      withCredentials: true,
      timeout: 10000
    });

    // Setup interceptors
    this.setupRequestInterceptor();
    this.setupResponseInterceptor();
  }

  setupRequestInterceptor() {
    this.api.interceptors.request.use(
      (config) => {
        // Add auth token
        const token = localStorage.getItem('authToken');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        // Add CSRF token
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (csrfToken) {
          config.headers['X-CSRF-TOKEN'] = csrfToken;
        }

        console.log(`API Request: ${config.method?.toUpperCase()} ${config.url}`);
        return config;
      },
      (error) => {
        console.error('Request interceptor error:', error);
        return Promise.reject(error);
      }
    );
  }

  setupResponseInterceptor() {
    this.api.interceptors.response.use(
      (response) => {
        console.log(`API Response: ${response.status} ${response.config.url}`);
        return response;
      },
      (error) => {
        console.error('API Response Error:', error);

        // Handle authentication errors
        if (error.response?.status === 401 || error.response?.status === 403) {
          this.handleAuthError(error);
        }

        // Mark as handled to prevent global error handler interference
        error.handled = true;

        // Log the error details
        this.logApiError(error);

        return Promise.reject(error);
      }
    );
  }

  handleAuthError(error) {
    const url = error.config?.url || '';
    console.log(`API authentication error on ${url} - clearing auth state`);

    // Clear auth tokens
    localStorage.removeItem('authToken');
    localStorage.removeItem('authUser');

    // Only redirect if not already on login page and not during logout
    const isLogoutRequest = url.includes('/auth/logout');
    const isOnLoginPage = window.location.pathname.includes('/account/login');

    if (!isLogoutRequest && !isOnLoginPage) {
      // Small delay to prevent race conditions with multiple failed requests
      setTimeout(() => {
        if (!window.location.pathname.includes('/account/login')) {
          window.location.href = '/account/login';
        }
      }, 100);
    }
  }

  async handleApiCall(apiCall, options = {}) {
    const {
      showErrorNotification = false,
      retryCount = 0,
      retryDelay = 1000,
      timeout = 10000
    } = options;

    let attempt = 0;

    while (attempt <= retryCount) {
      try {
        // Create abort controller for timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), timeout);

        // Execute the API call with abort signal
        const response = await apiCall(controller.signal);
        clearTimeout(timeoutId);

        return response;
      } catch (error) {
        this.logApiError(error, attempt, retryCount);

        if (error.name === 'AbortError') {
          const timeoutError = new Error('Request timeout');
          timeoutError.handled = true;
          timeoutError.status = 408;
          throw timeoutError;
        }

        // Don't retry on client errors (4xx) except 408 (timeout)
        if (error.response?.status >= 400 && error.response?.status < 500 && error.response?.status !== 408) {
          throw error;
        }

        // Retry on server errors (5xx) and network errors if retries are configured
        if (attempt < retryCount && this.isRetryableError(error)) {
          await this.delay(retryDelay * Math.pow(2, attempt)); // Exponential backoff
          attempt++;
          continue;
        }

        if (showErrorNotification) {
          this.showErrorNotification(error);
        }

        throw error;
      }
    }
  }

  isRetryableError(error) {
    // Retry on network errors and 5xx server errors
    return !error.response?.status ||
      error.response?.status >= 500 ||
      error.code === 'ECONNABORTED' ||
      error.code === 'ERR_NETWORK' ||
      error.name === 'TimeoutError';
  }

  logApiError(error, attempt = 0, maxRetries = 0) {
    const retryInfo = maxRetries > 0 ? ` (attempt ${attempt + 1}/${maxRetries + 1})` : '';
    console.error(`API Error${retryInfo}:`, {
      message: error.message,
      status: error.response?.status,
      url: error.config?.url,
      data: error.response?.data
    });
  }

  showErrorNotification(error) {
    const message = this.getUserFriendlyMessage(error);

    // Dispatch custom event for notification system
    window.dispatchEvent(new CustomEvent('api-error', {
      detail: {
        message,
        status: error.response?.status || error.status,
        canRetry: this.isRetryableError(error)
      }
    }));
  }

  getUserFriendlyMessage(error) {
    const status = error.response?.status || error.status || 0;

    switch (status) {
      case 400:
        return 'Invalid request. Please check your input and try again.';
      case 401:
        return 'Please log in to continue.';
      case 403:
        return 'You don\'t have permission to perform this action.';
      case 404:
        return 'The requested resource was not found.';
      case 408:
        return 'Request timed out. Please try again.';
      case 409:
        return 'This action conflicts with the current state. Please refresh and try again.';
      case 422:
        return 'The data provided could not be processed. Please check your input.';
      case 429:
        return 'Too many requests. Please wait a moment and try again.';
      case 500:
        return 'Server error. Our team has been notified.';
      case 502:
        return 'Service temporarily unavailable. Please try again in a moment.';
      case 503:
        return 'Service temporarily unavailable. Please try again later.';
      case 504:
        return 'Request timed out. Please try again.';
      default:
        if (status >= 500) {
          return 'A server error occurred. Please try again later.';
        } else if (status >= 400) {
          return 'There was a problem with your request. Please try again.';
        } else {
          return 'A network error occurred. Please check your connection and try again.';
        }
    }
  }

  delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  // HTTP method wrappers
  async get(url, options = {}) {
    return this.handleApiCall(
      (signal) => this.api.get(url, { ...options, signal }),
      options
    );
  }

  async post(url, data, options = {}) {
    return this.handleApiCall(
      (signal) => this.api.post(url, data, { ...options, signal }),
      options
    );
  }

  async put(url, data, options = {}) {
    return this.handleApiCall(
      (signal) => this.api.put(url, data, { ...options, signal }),
      options
    );
  }

  async patch(url, data, options = {}) {
    return this.handleApiCall(
      (signal) => this.api.patch(url, data, { ...options, signal }),
      options
    );
  }

  async delete(url, options = {}) {
    return this.handleApiCall(
      (signal) => this.api.delete(url, { ...options, signal }),
      options
    );
  }

  // Get the underlying axios instance for direct access
  getAxiosInstance() {
    return this.api;
  }
}

// Factory function for creating API clients
export const createApiClient = (errorHandler) => {
  const apiHandler = new ApiErrorHandler(errorHandler);
  const axiosInstance = apiHandler.getAxiosInstance();

  return {
    // Basic HTTP methods
    get: (url, options = {}) => apiHandler.get(url, options),
    post: (url, data, options = {}) => apiHandler.post(url, data, options),
    put: (url, data, options = {}) => apiHandler.put(url, data, options),
    patch: (url, data, options = {}) => apiHandler.patch(url, data, options),
    delete: (url, options = {}) => apiHandler.delete(url, options),

    // Enhanced methods with built-in features
    getWithNotification: (url, options = {}) =>
      apiHandler.get(url, { ...options, showErrorNotification: true }),

    postWithRetry: (url, data, options = {}) =>
      apiHandler.post(url, data, { ...options, retryCount: 2, showErrorNotification: true }),

    // Direct JSON methods (axios already handles JSON)
    getJson: async (url, options = {}) => {
      const response = await apiHandler.get(url, options);
      return response.data;
    },

    postJson: async (url, data, options = {}) => {
      const response = await apiHandler.post(url, data, options);
      return response.data;
    },

    // Upload method for FormData
    upload: async (url, formData, options = {}) => {
      return apiHandler.post(url, formData, {
        ...options,
        headers: {
          'Content-Type': 'multipart/form-data',
          ...options.headers
        },
        timeout: 60000 // Longer timeout for uploads
      });
    },

    // Access to underlying axios instance
    axios: axiosInstance,

    // Access to the handler for advanced usage
    handler: apiHandler
  };
};

// Export default handler class
export default ApiErrorHandler;
