// services/titleDetailsService.js
import axios from 'axios';

class TitleDetailsService {
  constructor() {
    // Configure the base URL to use environment variable
    const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064/api';

    this.apiClient = axios.create({
      baseURL: baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      }
    });

    // Add request interceptor for debugging
    this.apiClient.interceptors.request.use(
      (config) => {
        console.log(`Title API Request: ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`);

        // Add auth token if available
        const token = localStorage.getItem('authToken');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        return config;
      },
      (error) => {
        console.error('Title API Request Error:', error);
        return Promise.reject(error);
      }
    );

    // Add response interceptor for error handling
    this.apiClient.interceptors.response.use(
      (response) => {
        console.log(`Title API Response: ${response.status} ${response.config.url}`, response.data);
        return response;
      },
      (error) => {
        console.error('Title API Response Error:', error);
        console.error('Error details:', {
          message: error.message,
          status: error.response?.status,
          data: error.response?.data,
          url: error.config?.url
        });

        // Handle authentication errors
        if (error.response?.status === 401) {
          localStorage.removeItem('authToken');
          localStorage.removeItem('authUser');

          if (!window.location.pathname.includes('/account/login')) {
            window.location.href = '/account/login';
          }
        }

        // Create a standardized error response
        const errorResponse = {
          success: false,
          error: this.getErrorMessage(error),
          status: error.response?.status || 500,
          data: null
        };

        return Promise.resolve({ data: errorResponse });
      }
    );
  }

  getErrorMessage(error) {
    if (error.response?.status === 404) {
      return 'Title not found';
    }
    if (error.response?.status === 403) {
      return 'Access denied';
    }
    if (error.response?.status === 500) {
      return 'Server error occurred';
    }
    if (error.code === 'ECONNREFUSED') {
      return 'Backend server is not running';
    }
    if (error.code === 'ERR_NETWORK') {
      return 'Network error - check if backend is running';
    }
    return error.response?.data?.message || error.message || 'An error occurred';
  }

  // Get title details by original title name
  async getTitleDetails(titleName) {
    try {
      console.log('Fetching title details for:', titleName);

      // Encode the title name for URL
      const encodedTitle = encodeURIComponent(titleName);
      const response = await this.apiClient.get(`/Titles/Details/${encodedTitle}`);

      // Check if response is HTML (indicates wrong endpoint)
      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found');
      }

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching title details:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get chapters for a title
  async getChapters(titleId) {
    try {
      console.log('Fetching chapters for title ID:', titleId);
      const response = await this.apiClient.get(`/Titles/GetChapters?titleId=${titleId}`);

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching chapters:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get comments for a title
  async getComments(titleId, targetType = 1) {
    try {
      console.log('Fetching comments for title ID:', titleId);
      const response = await this.apiClient.get(`/Comments/GetComments?targetId=${titleId}&targetType=${targetType}`);

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching comments:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  // Submit a rating for a title
  async rateTitle(titleId, rating) {
    try {
      console.log('Submitting rating for title ID:', titleId, 'Rating:', rating);

      const response = await this.apiClient.post('/Ratings/RateTitle', {
        titleId: parseInt(titleId),
        rating: parseInt(rating)
      });

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error submitting rating:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get rating statistics for a title
  async getRatingStats(titleId) {
    try {
      console.log('Fetching rating stats for title ID:', titleId);
      const response = await this.apiClient.get(`/Ratings/GetRatings?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching rating stats:', error);
      return {
        success: false,
        data: {
          average: 0,
          total: 0,
          distribution: []
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get bookmark statistics for a title
  async getBookmarkStats(titleId) {
    try {
      console.log('Fetching bookmark stats for title ID:', titleId);
      const response = await this.apiClient.get(`/Bookmarks/GetBookmarkStats?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching bookmark stats:', error);
      return {
        success: false,
        data: {
          totalBookmarks: 0,
          folderDistribution: []
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Helper method to get image URLs
  getImageUrl(imagePath) {
    if (!imagePath) {
      const baseUrl = this.getImageBaseUrl();
      return `${baseUrl}/img/default-cover.png`;
    }

    // Check if the path is already a full URL
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }

    // Check if it's a relative path that starts with /
    const baseUrl = this.getImageBaseUrl();
    const fullUrl = imagePath.startsWith('/')
      ? `${baseUrl}${imagePath}`
      : `${baseUrl}/${imagePath}`;

    return fullUrl;
  }

  // Get the base URL for images
  getImageBaseUrl() {
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064/api';
    return apiBaseUrl.replace('/api', ''); // Remove /api to get base server URL
  }

  // Test API connectivity
  async testConnection() {
    try {
      console.log('Testing title API connection...');
      const response = await this.apiClient.get('/Titles/Debug');
      console.log('Title API connection test successful:', response.data);
      return { success: true, data: response.data };
    } catch (error) {
      console.error('Title API connection test failed:', error);
      return { success: false, error: this.getErrorMessage(error) };
    }
  }
}

export const titleDetailsService = new TitleDetailsService();

// Export test function for debugging
export const testTitleApiConnection = () => titleDetailsService.testConnection();
