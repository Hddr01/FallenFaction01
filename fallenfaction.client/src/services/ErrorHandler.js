// src/services/ErrorHandler.js
export class ErrorHandler {
    constructor(router = null) {
        this.router = router;
        this.isHandlingError = false;
        this.currentError = null;
        this.errorCallbacks = [];
    }

    setupGlobalHandlers() {
        // Handle unhandled promise rejections
        window.addEventListener('unhandledrejection', (event) => {
            console.error('Unhandled promise rejection:', event.reason);

            // Check if this is an API error that should be handled locally
            if (this.isApiError(event.reason)) {
                console.log('API error detected - not redirecting to error page');
                event.preventDefault();
                return;
            }

            // Don't redirect for API authentication errors
            if (this.isAuthenticationError(event.reason)) {
                console.log('Authentication required - not redirecting to error page');
                event.preventDefault();
                return;
            }

            // Don't redirect for handled errors
            if (event.reason && event.reason.handled) {
                event.preventDefault();
                return;
            }

            // Only show error page for serious unhandled errors
            if (this.isSeriousError(event.reason)) {
                this.handleError(event.reason);
            }

            event.preventDefault();
        });

        // Handle global errors
        window.addEventListener('error', (event) => {
            console.error('Global error:', event.error);

            // Don't redirect for resource loading errors
            if (event.target !== window) {
                console.log('Resource loading error - not redirecting');
                event.preventDefault();
                return;
            }

            // Only show error page for serious errors
            if (this.isSeriousError(event.error)) {
                this.handleError(event.error);
            }

            event.preventDefault();
        });
    }

    isApiError(error) {
        if (!error) return false;

        // Check if error has a config object with URL (axios error)
        if (error.config && error.config.url) {
            return error.config.url.includes('/api/');
        }

        // Check if error message mentions API
        if (error.message && error.message.includes('/api/')) {
            return true;
        }

        // Check response URL
        if (error.response && error.response.config && error.response.config.url) {
            return error.response.config.url.includes('/api/');
        }

        // Check for fetch API errors
        if (error.url && error.url.includes('/api/')) {
            return true;
        }

        return false;
    }

    isSeriousError(error) {
        if (!error) return false;

        // Don't treat API errors as serious (page-breaking) errors
        if (this.isApiError(error)) return false;

        // Don't treat auth errors as serious errors
        if (this.isAuthenticationError(error)) return false;

        // Don't treat handled errors as serious
        if (error.handled) return false;

        // Don't treat network errors on API calls as serious
        if (error.code === 'ERR_NETWORK' && this.isApiError(error)) return false;

        // These are serious errors that should show error page
        if (error.message && (
            error.message.includes('Cannot read properties of undefined') ||
            error.message.includes('Cannot read property') ||
            error.message.includes('is not defined') ||
            error.message.includes('Syntax error') ||
            error.message.includes('Script error') ||
            error.message.includes('ReferenceError') ||
            error.message.includes('TypeError')
        )) {
            return true;
        }

        return false;
    }

    isAuthenticationError(error) {
        // Check if this is an authentication-related error
        if (error?.response?.status === 401 || error?.response?.status === 403) {
            return true;
        }

        // Check HTTP status codes
        if (error?.status === 401 || error?.status === 403) {
            return true;
        }

        // Check if error message indicates authentication issue
        if (error?.message && (
            error.message.includes('401') ||
            error.message.includes('403') ||
            error.message.includes('Unauthorized') ||
            error.message.includes('authentication') ||
            error.message.includes('login')
        )) {
            return true;
        }

        return false;
    }

    handleError(error, showErrorPage = true) {
        // Prevent recursive error handling
        if (this.isHandlingError) {
            console.warn('Already handling an error, preventing recursion');
            return;
        }

        this.isHandlingError = true;

        try {
            console.error('ErrorHandler processing:', error);

            // Don't show error page for authentication errors
            if (this.isAuthenticationError(error)) {
                console.log('Authentication error detected - not showing error page');
                return;
            }

            // Extract error details
            const errorDetails = this.extractErrorDetails(error);

            // Only show error page for serious errors
            if (showErrorPage && this.shouldShowErrorPage(errorDetails)) {
                this.showErrorPage(errorDetails);
            }
        } catch (e) {
            console.error('Error in error handler:', e);
        } finally {
            // Reset flag after a delay
            setTimeout(() => {
                this.isHandlingError = false;
            }, 1000);
        }
    }

    shouldShowErrorPage(errorDetails) {
        // Don't show error page for certain status codes
        const ignoredStatusCodes = [401, 403, 404];
        if (ignoredStatusCodes.includes(errorDetails.statusCode)) {
            return false;
        }

        // Don't show error page for API errors unless they're 500s
        if (errorDetails.path && errorDetails.path.includes('/api/')) {
            return errorDetails.statusCode >= 500;
        }

        return true;
    }

    extractErrorDetails(error) {
        let statusCode = 500;
        let message = 'An unexpected error occurred';
        let path = window.location.pathname;

        if (error?.response) {
            // Axios error
            statusCode = error.response.status || 500;
            message = error.response.data?.message || error.response.statusText || message;
            path = error.config?.url || path;
        } else if (error?.status) {
            // Fetch error
            statusCode = error.status;
            message = error.statusText || message;
        } else if (error?.message) {
            message = error.message;
        }

        return {
            statusCode,
            message,
            path,
            timestamp: new Date().toISOString(),
            requestId: `req_${Math.random().toString(36).substr(2, 9)}`,
            showRetry: statusCode >= 500,
            showDetails: true
        };
    }

    showErrorPage(details) {
        this.currentError = details;
        this.notifyErrorCallbacks(details);

        // If router is available, use it for navigation
        if (this.router) {
            this.router.push({
                name: 'Error',
                params: { statusCode: details.statusCode },
                query: {
                    message: details.message,
                    path: details.path,
                    requestId: details.requestId,
                    timestamp: details.timestamp,
                    showRetry: details.showRetry.toString(),
                    showDetails: details.showDetails.toString()
                }
            });
        }
    }

    handleVueError(error, instance, info) {
        console.error('Vue Error:', error);
        console.error('Component:', instance);
        console.error('Error Info:', info);

        // Don't redirect for authentication errors
        if (this.isAuthenticationError(error)) {
            console.log('Vue authentication error - not redirecting');
            return;
        }

        this.handleError(error);
    }

    // Error state management
    setError(statusCode, message = '', path = '', showRetry = false) {
        if (this.isHandlingError) return;

        this.currentError = {
            statusCode,
            message,
            path: path || window.location.pathname,
            requestId: this.generateRequestId(),
            timestamp: new Date().toISOString(),
            showRetry,
            showDetails: true
        };
        
        this.notifyErrorCallbacks(this.currentError);
    }

    clearError() {
        this.currentError = null;
        this.notifyErrorCallbacks(null);
    }

    onErrorChange(callback) {
        this.errorCallbacks.push(callback);
        
        // Return unsubscribe function
        return () => {
            const index = this.errorCallbacks.indexOf(callback);
            if (index > -1) {
                this.errorCallbacks.splice(index, 1);
            }
        };
    }

    notifyErrorCallbacks(error) {
        this.errorCallbacks.forEach(callback => callback(error));
    }

    generateRequestId() {
        return 'req_' + Math.random().toString(36).substr(2, 9);
    }

    // Convenience methods for common errors
    handle404(path = '') {
        this.setError(404, '', path, false);
    }

    handle500(message = 'Internal server error') {
        this.setError(500, message, '', true);
    }

    handle403(message = 'Access denied') {
        this.setError(403, message, '', false);
    }

    handleNetworkError() {
        this.setError(503, 'Network connection error. Please check your internet connection.', '', true);
    }

    handleTimeout() {
        this.setError(408, 'Request timed out. Please try again.', '', true);
    }
}

// Axios interceptors setup
export function setupAxiosInterceptors(axios, errorHandler) {
    // Request interceptor
    axios.interceptors.request.use(
        config => {
            // Add auth token if available
            const token = localStorage.getItem('authToken');
            if (token) {
                config.headers.Authorization = `Bearer ${token}`;
            }
            return config;
        },
        error => {
            return Promise.reject(error);
        }
    );

    // Response interceptor
    axios.interceptors.response.use(
        response => response,
        error => {
            // Don't handle errors if they've been marked as handled
            if (error.handled) {
                return Promise.reject(error);
            }

            // Handle specific error cases
            if (error.response) {
                const status = error.response.status;
                const url = error.config?.url || '';
                const isApiCall = url.startsWith('/api/') || url.includes('/api/');

                console.log(`Axios interceptor: ${error.config?.method || 'GET'} ${url} returned ${status}`);

                // For API calls, NEVER redirect the page
                if (isApiCall) {
                    // For auth errors, just log and reject
                    if (status === 401 || status === 403) {
                        console.log('API authentication error - not redirecting page');

                        // Check if this is a redirect response to login page
                        if (error.response.data && typeof error.response.data === 'string' &&
                            error.response.data.includes('Login') &&
                            error.response.data.includes('<!DOCTYPE')) {
                            console.log('API returned HTML login page instead of JSON - treating as 401');
                            error.response.status = 401;
                            error.response.data = { message: 'Authentication required' };
                        }
                    } else if (status === 404) {
                        console.log('API endpoint not found - not redirecting page');
                    } else if (status >= 500) {
                        console.error('API server error - not redirecting page');
                    }

                    // Mark as handled to prevent global handler from catching it
                    error.handled = true;

                    // Always reject for API calls - let the calling code handle it
                    return Promise.reject(error);
                }

                // For non-API navigation requests, only redirect for serious errors
                if (!isApiCall && status >= 500) {
                    errorHandler.handleError(error, true);
                }
            } else if (error.request) {
                // Network error
                console.error('Network error:', error);

                // Only show error page for non-API requests
                const url = error.config?.url || '';
                if (!url.includes('/api/')) {
                    errorHandler.handleError(new Error('Network error occurred'));
                }
            }

            return Promise.reject(error);
        }
    );
}

// Fetch wrapper with error handling
export function setupFetchInterceptor(errorHandler) {
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
                    errorHandler.handleError(error, true);
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
                errorHandler.handleError(error, true);
            }
            
            throw error;
        }
    };
}

export default ErrorHandler;