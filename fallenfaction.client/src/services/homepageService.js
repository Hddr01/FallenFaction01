// services/homepageService.js
import axios from 'axios';

class HomepageService {
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
        console.log(`API Request: ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`);
        return config;
      },
      (error) => {
        console.error('API Request Error:', error);
        return Promise.reject(error);
      }
    );

    // Add response interceptor for error handling
    this.apiClient.interceptors.response.use(
      (response) => {
        console.log(`API Response: ${response.status} ${response.config.url}`, response.data);
        return response;
      },
      (error) => {
        console.error('API Response Error:', error);
        console.error('Error details:', {
          message: error.message,
          status: error.response?.status,
          data: error.response?.data,
          url: error.config?.url
        });

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
      return 'API endpoint not found';
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

  async testConnection() {
    try {
      console.log('Testing API connection...');
      const response = await this.apiClient.get('/Titles/Debug');
      console.log('API connection test successful:', response.data);
      return { success: true, data: response.data };
    } catch (error) {
      console.error('API connection test failed:', error);
      return { success: false, error: this.getErrorMessage(error) };
    }
  }

  async getFeaturedManga() {
    try {
      console.log('Fetching featured manga...');
      const response = await this.apiClient.get('/Titles/Featured');

      // Check if response is HTML (indicates wrong endpoint)
      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found');
      }

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching featured manga:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  async getPopularTitles() {
    try {
      console.log('Fetching popular titles...');
      const response = await this.apiClient.get('/Titles/Popular');

      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found');
      }

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching popular titles:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  async getRecentUpdates() {
    try {
      console.log('Fetching recent updates...');
      const response = await this.apiClient.get('/Titles/RecentUpdates');

      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found');
      }

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching recent updates:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  async getTopUsers() {
    try {
      console.log('Fetching top users...');
      const response = await this.apiClient.get('/Users/TopUsers');

      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found');
      }

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching top users:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  async getTopTeams() {
    try {
      console.log('Fetching top teams...');
      const response = await this.apiClient.get('/Team/TopTeams');

      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found');
      }

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching top teams:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  async getTitleDetails(encodedTitle) {
    try {
      console.log('Fetching title details for:', encodedTitle);
      const response = await this.apiClient.get(`/Titles/Details/${encodedTitle}`);

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

  // Add method to seed sample data if needed
  async seedSampleData() {
    try {
      console.log('Seeding sample data...');
      const response = await this.apiClient.post('/Titles/SeedSampleData');
      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error seeding sample data:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }
}

export const homepageService = new HomepageService();

// Export test function for debugging
export const testApiConnection = () => homepageService.testConnection();
