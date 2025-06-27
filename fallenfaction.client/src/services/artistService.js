// services/artistService.js
import axios from 'axios';

// Use Vite environment variables (not process.env)
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api';

// Create axios instance with base configuration
const artistApi = axios.create({
  baseURL: `${API_BASE_URL}/artist`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000,
});

// Add request interceptor to include auth token
artistApi.interceptors.request.use(
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
artistApi.interceptors.response.use(
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

export const artistService = {
  // Get all artists
  async getArtists() {
    try {
      const response = await artistApi.get('/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching artists:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch artists'
      };
    }
  },

  // Get artist by ID
  async getArtistById(id) {
    try {
      const response = await artistApi.get(`/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching artist:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch artist'
      };
    }
  },

  // Create new artist (authenticated users)
  async createArtist(artistData) {
    try {
      const response = await artistApi.post('/', artistData);
      return {
        success: true,
        data: response.data.data,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error creating artist:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create artist'
      };
    }
  },

  // Update artist (authenticated users)
  async updateArtist(id, artistData) {
    try {
      const response = await artistApi.put(`/${id}`, artistData);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error updating artist:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update artist'
      };
    }
  },

  // Delete artist (authenticated users)
  async deleteArtist(id) {
    try {
      const response = await artistApi.delete(`/${id}`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error deleting artist:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete artist'
      };
    }
  },

  // Search artists
  async searchArtists(query) {
    try {
      const response = await artistApi.get(`/search?query=${encodeURIComponent(query)}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error searching artists:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search artists'
      };
    }
  }
};
