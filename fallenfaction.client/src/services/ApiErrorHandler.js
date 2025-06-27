// src/services/ApiErrorHandler.js
import ErrorHandler from './ErrorHandler.js';

export class ApiErrorHandler {
    constructor(errorHandler) {
        this.errorHandler = errorHandler;
        this.setupFetchInterceptor();
    }

    setupFetchInterceptor() {
        const originalFetch = window.fetch;
        
        window.fetch = async function(...args) {
            try {
                const response = await originalFetch.apply(this, args);
                
                // Check if this is an API call
                const url = args[0];
                const isApiCall = typeof url === 'string' && url.includes('/api/');
                
                if (!response.ok) {
                    const error = new Error(`HTTP ${response.status}: ${response.statusText}`);
                    error.response = response;
                    error.status = response.status;
                    error.url = url;
                    
                    if (isApiCall) {
                        // For API calls, mark as handled and reject
                        error.handled = true;
                        console.log(`Fetch API call ${url} failed with status ${response.status} - not redirecting`);
                        throw error;
                    } else if (response.status >= 500) {
                        // For page navigation, only redirect on server errors
                        this.errorHandler?.handleError(error, true);
                    }
                    
                    throw error;
                }
                
                return response;
            } catch (error) {
                const url = args[0];
                const isApiCall = typeof url === 'string' && url.includes('/api/');
                
                if (isApiCall) {
                    error.handled = true;
                    console.log(`Fetch API call ${url} failed with network error - not redirecting`);
                } else {
                    this.errorHandler?.handleError(error, true);
                }
                
                throw error;
            }
        }.bind(this);
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
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), timeout);
                
                const response = await apiCall(controller.signal);
                clearTimeout(timeoutId);
                
                if (!response.ok) {
                    const error = await this.createErrorFromResponse(response);
                    this.logApiError(error, attempt, retryCount);
                    
                    // Don't retry on client errors (4xx)
                    if (response.status >= 400 && response.status < 500) {
                        throw error;
                    }
                    
                    // Retry on server errors (5xx) if retries are configured
                    if (attempt < retryCount) {
                        await this.delay(retryDelay * Math.pow(2, attempt)); // Exponential backoff
                        attempt++;
                        continue;
                    }
                    
                    throw error;
                }
                
                return response;
            } catch (error) {
                // Mark as handled to prevent global error handler from showing error page
                error.handled = true;
                
                if (error.name === 'AbortError') {
                    const timeoutError = new Error('Request timeout');
                    timeoutError.handled = true;
                    timeoutError.status = 408;
                    this.logApiError(timeoutError, attempt, retryCount);
                    throw timeoutError;
                }
                
                this.logApiError(error, attempt, retryCount);
                
                // Don't retry on network errors unless explicitly configured
                if (attempt < retryCount && this.isRetryableError(error)) {
                    await this.delay(retryDelay * Math.pow(2, attempt));
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

    async createErrorFromResponse(response) {
        let errorMessage = response.statusText;
        let errorData = null;
        
        try {
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                errorData = await response.json();
                errorMessage = errorData.message || errorData.error || errorMessage;
            } else {
                const text = await response.text();
                if (text) {
                    errorMessage = text;
                }
            }
        } catch (parseError) {
            console.warn('Failed to parse error response:', parseError);
        }
        
        const error = new Error(errorMessage);
        error.response = response;
        error.status = response.status;
        error.data = errorData;
        error.handled = true;
        
        return error;
    }

    isRetryableError(error) {
        // Retry on network errors and 5xx server errors
        return !error.status || error.status >= 500 || error.name === 'TypeError';
    }

    logApiError(error, attempt, maxRetries) {
        const retryInfo = maxRetries > 0 ? ` (attempt ${attempt + 1}/${maxRetries + 1})` : '';
        console.error(`API Error${retryInfo}:`, {
            message: error.message,
            status: error.status,
            url: error.url || error.config?.url,
            data: error.data
        });
    }

    showErrorNotification(error) {
        // This could integrate with a toast notification system
        console.warn('API Error Notification:', error.message);
        
        // Example: You could dispatch a custom event that your notification system listens to
        window.dispatchEvent(new CustomEvent('api-error', {
            detail: {
                message: this.getUserFriendlyMessage(error),
                status: error.status,
                canRetry: this.isRetryableError(error)
            }
        }));
    }

    getUserFriendlyMessage(error) {
        const status = error.status || 0;
        
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

    // Wrapper methods for common HTTP methods
    async get(url, options = {}) {
        return this.handleApiCall(
            (signal) => fetch(url, { ...options, signal }),
            options
        );
    }

    async post(url, data, options = {}) {
        return this.handleApiCall(
            (signal) => fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                body: JSON.stringify(data),
                signal,
                ...options
            }),
            options
        );
    }

    async put(url, data, options = {}) {
        return this.handleApiCall(
            (signal) => fetch(url, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                body: JSON.stringify(data),
                signal,
                ...options
            }),
            options
        );
    }

    async patch(url, data, options = {}) {
        return this.handleApiCall(
            (signal) => fetch(url, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                body: JSON.stringify(data),
                signal,
                ...options
            }),
            options
        );
    }

    async delete(url, options = {}) {
        return this.handleApiCall(
            (signal) => fetch(url, {
                method: 'DELETE',
                signal,
                ...options
            }),
            options
        );
    }
}

// Convenience functions for API calls
export const createApiClient = (errorHandler) => {
    const apiHandler = new ApiErrorHandler(errorHandler);
    
    return {
        // Basic methods
        get: (url, options) => apiHandler.get(url, options),
        post: (url, data, options) => apiHandler.post(url, data, options),
        put: (url, data, options) => apiHandler.put(url, data, options),
        patch: (url, data, options) => apiHandler.patch(url, data, options),
        delete: (url, options) => apiHandler.delete(url, options),
        
        // Methods with built-in error handling
        getWithNotification: (url, options = {}) => 
            apiHandler.get(url, { ...options, showErrorNotification: true }),
        
        postWithRetry: (url, data, options = {}) => 
            apiHandler.post(url, data, { ...options, retryCount: 2, showErrorNotification: true }),
        
        // Method to get JSON directly
        getJson: async (url, options = {}) => {
            const response = await apiHandler.get(url, options);
            return response ? await response.json() : null;
        },
        
        postJson: async (url, data, options = {}) => {
            const response = await apiHandler.post(url, data, options);
            return response ? await response.json() : null;
        }
    };
};

export default ApiErrorHandler;
