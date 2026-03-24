// services/authorService.js
import axios from 'axios';

// Use Vite environment variables (not process.env)
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

// Create axios instance with base configuration
const authorApi = axios.create({
  baseURL: `${API_BASE_URL}/author`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000,
});

// Add request interceptor to include auth token
authorApi.interceptors.request.use(
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
authorApi.interceptors.response.use(
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

export const authorService = {
  // Get all authors
  async getAuthors() {
    try {
      const response = await authorApi.get('/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching authors:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch authors'
      };
    }
  },

  // Get author by ID
  async getAuthorById(id) {
    try {
      const response = await authorApi.get(`/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching author:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch author'
      };
    }
  },

  // Create new author (Admin only)
  async createAuthor(authorData) {
    try {
      const response = await authorApi.post('/', authorData);
      return {
        success: true,
        data: response.data.data,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error creating author:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create author'
      };
    }
  },

  // Update author (Admin only)
  async updateAuthor(id, authorData) {
    try {
      const response = await authorApi.put(`/${id}`, authorData);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error updating author:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update author'
      };
    }
  },

  // Delete author (Admin only)
  async deleteAuthor(id) {
    try {
      const response = await authorApi.delete(`/${id}`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error deleting author:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete author'
      };
    }
  },

  // Search authors
  async searchAuthors(query) {
    try {
      const response = await authorApi.get(`/search?query=${encodeURIComponent(query)}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error searching authors:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search authors'
      };
    }
  }
};
