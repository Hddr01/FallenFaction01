// services/titleApi.js - Axios-based API service (similar to authApi.js)
import axios from 'axios';

// Create axios instance with base configuration (same pattern as authApi.js)
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064/api',
  headers: {
    'Accept': 'application/json',
  },
  withCredentials: true,
  timeout: 10000, // 10 second timeout
});

// Request interceptor to add auth token (same as authApi.js)
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

// Response interceptor to handle token expiration (same as authApi.js)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Handle different error scenarios
    if (error.response?.status === 401) {
      // Token expired or invalid
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');

      // Only redirect if we're not already on the login page
      if (!window.location.pathname.includes('/account/login')) {
        window.location.href = '/account/login';
      }
    }

    return Promise.reject(error);
  }
);

const titleApi = {
  // Get form data (authors, artists, etc.)
  async getFormData() {
    try {
      const response = await api.get('/TitleApi/form-data');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading form data:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load form data',
        data: {
          Authors: [],
          Artists: [],
          Publishers: [],
          Teams: [],
          Categories: [],
          Tags: [],
          Formats: []
        }
      };
    }
  },

  // Create title with file uploads
  async createTitle(formData) {
    try {
      // Create FormData for file uploads
      const submitData = new FormData();

      // Add files
      if (formData.coverImage) {
        submitData.append('coverImage', formData.coverImage);
      }
      if (formData.backgroundImage) {
        submitData.append('backgroundImage', formData.backgroundImage);
      }

      // Add simple fields
      submitData.append('englishTitle', formData.englishTitle || '');
      submitData.append('originalTitle', formData.originalTitle || '');
      submitData.append('alternativeNames', formData.alternativeNames || '');
      submitData.append('type', formData.type || '1');
      submitData.append('releaseDate', formData.releaseDate || '');
      submitData.append('description', formData.description || '');
      submitData.append('statusTitle', formData.statusTitle || 'inproces');
      submitData.append('statusTranslation', formData.statusTranslation || 'inproces');
      submitData.append('ageRestriction', formData.ageRestriction || '0');

      // Add array fields
      if (formData.authors) {
        formData.authors.forEach(id => submitData.append('authors', id));
      }
      if (formData.artists) {
        formData.artists.forEach(id => submitData.append('artists', id));
      }
      if (formData.publishers) {
        formData.publishers.forEach(id => submitData.append('publishers', id));
      }
      if (formData.teams) {
        formData.teams.forEach(id => submitData.append('teams', id));
      }
      if (formData.categories) {
        formData.categories.forEach(id => submitData.append('categories', id));
      }
      if (formData.tags) {
        formData.tags.forEach(id => submitData.append('tags', id));
      }
      if (formData.formats) {
        formData.formats.forEach(id => submitData.append('formats', id));
      }

      // Add external links (filter out empty ones)
      if (formData.externalLinks) {
        const validLinks = formData.externalLinks.filter(link => link && link.trim());
        validLinks.forEach(link => submitData.append('externalLinks', link));
      }

      // Make the request with FormData
      // Note: Don't set Content-Type header for FormData - axios will set it automatically with boundary
      const response = await api.post('/TitleApi/create', submitData, {
        headers: {
          // Remove Content-Type to let axios set it automatically for FormData
          'Content-Type': undefined
        }
      });

      return {
        success: true,
        message: response.data.message || 'Title created successfully!',
        data: response.data
      };
    } catch (error) {
      console.error('Error creating title:', error);

      const errorMessage = error.response?.data?.message ||
        error.response?.data?.error ||
        error.message ||
        'Failed to create title';

      return {
        success: false,
        error: errorMessage,
        message: errorMessage
      };
    }
  },

  // Test API connectivity
  async testConnection() {
    try {
      const response = await api.get('/TitleApi/form-data');
      return response.status === 200;
    } catch (error) {
      console.error('API connection test failed:', error);
      return false;
    }
  },

  // Get pending titles (admin only)
  async getPendingTitles() {
    try {
      const response = await api.get('/TitleApi/pending');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message,
        data: []
      };
    }
  },

  // Approve pending title (admin only)
  async approvePendingTitle(titleId) {
    try {
      const response = await api.post(`/TitleApi/approve/${titleId}`);
      return {
        success: true,
        message: response.data.message || 'Title approved successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message
      };
    }
  }
};

export default titleApi;
