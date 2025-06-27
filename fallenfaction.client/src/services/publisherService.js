// services/publisherService.js
import axios from 'axios';

// Use Vite environment variables (not process.env)
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api';

// Create axios instance with base configuration
const publisherApi = axios.create({
  baseURL: `${API_BASE_URL}/publisher`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000,
});

// Add request interceptor to include auth token
publisherApi.interceptors.request.use(
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

// Add response interceptor for error handling
publisherApi.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized access
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
      if (!window.location.pathname.includes('/account/login')) {
        window.location.href = '/account/login';
      }
    }
    return Promise.reject(error);
  }
);

export const publisherService = {
  // Get all publishers
  async getPublishers() {
    try {
      const response = await publisherApi.get('/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching publishers:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch publishers'
      };
    }
  },

  // Get publisher by ID
  async getPublisherById(id) {
    try {
      const response = await publisherApi.get(`/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching publisher:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch publisher'
      };
    }
  },

  // Create new publisher (Admin only)
  async createPublisher(publisherData) {
    try {
      const response = await publisherApi.post('/', publisherData);
      return {
        success: true,
        data: response.data.data,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error creating publisher:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create publisher'
      };
    }
  },

  // Update publisher (Admin only)
  async updatePublisher(id, publisherData) {
    try {
      const response = await publisherApi.put(`/${id}`, publisherData);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error updating publisher:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update publisher'
      };
    }
  },

  // Delete publisher (Admin only)
  async deletePublisher(id) {
    try {
      const response = await publisherApi.delete(`/${id}`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error deleting publisher:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete publisher'
      };
    }
  },

  // Search publishers
  async searchPublishers(query) {
    try {
      const response = await publisherApi.get(`/search?query=${encodeURIComponent(query)}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error searching publishers:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search publishers'
      };
    }
  }
};
