// services/titleApi.js - Axios-based API service (similar to authApi.js)
import axios from 'axios';

// Create axios instance with base configuration (same pattern as authApi.js)
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Accept': 'application/json',
  },
  withCredentials: true,
  timeout: 10000, // 10 second timeout
});

// Request interceptor to add auth token (same as authApi.js)
api.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('authToken') || localStorage.getItem('authToken');
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
      sessionStorage.removeItem('authToken');
      sessionStorage.removeItem('authUser');

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
      const response = await api.post('/TitleApi/create', submitData, {
        headers: {
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

  // Edit title (submit changes for approval)
  async editTitle(titleId, updateData) {
    try {
      // Create FormData for file uploads
      const formData = new FormData();

      // Add ID
      formData.append('id', titleId);

      // Add files if present (only if they're File objects)
      if (updateData.coverImage instanceof File) {
        formData.append('coverImage', updateData.coverImage);
      }
      if (updateData.backgroundImage instanceof File) {
        formData.append('backgroundImage', updateData.backgroundImage);
      }

      // Add simple text fields
      formData.append('englishTitle', updateData.englishTitle || '');
      formData.append('originalTitle', updateData.originalTitle || '');
      formData.append('alternativeNames', updateData.alternativeNames || '');
      formData.append('type', updateData.type || '1');
      formData.append('releaseDate', updateData.releaseDate || '');
      formData.append('description', updateData.description || '');
      formData.append('statusTitle', updateData.statusTitle || 'inproces');
      formData.append('statusTranslation', updateData.statusTranslation || 'inproces');
      formData.append('ageRestriction', updateData.ageRestriction || '0');

      // Add array fields
      if (updateData.authors && Array.isArray(updateData.authors)) {
        updateData.authors.forEach(id => formData.append('authors', id));
      }
      if (updateData.artists && Array.isArray(updateData.artists)) {
        updateData.artists.forEach(id => formData.append('artists', id));
      }
      if (updateData.publishers && Array.isArray(updateData.publishers)) {
        updateData.publishers.forEach(id => formData.append('publishers', id));
      }
      if (updateData.teams && Array.isArray(updateData.teams)) {
        updateData.teams.forEach(id => formData.append('teams', id));
      }
      if (updateData.categories && Array.isArray(updateData.categories)) {
        updateData.categories.forEach(id => formData.append('categories', id));
      }
      if (updateData.tags && Array.isArray(updateData.tags)) {
        updateData.tags.forEach(id => formData.append('tags', id));
      }
      if (updateData.formats && Array.isArray(updateData.formats)) {
        updateData.formats.forEach(id => formData.append('formats', id));
      }

      // Add external links (filter out empty ones)
      if (updateData.externalLinks && Array.isArray(updateData.externalLinks)) {
        const validLinks = updateData.externalLinks.filter(link => link && link.trim());
        validLinks.forEach(link => formData.append('externalLinks', link));
      }

      const response = await api.post(`/TitleApi/edit/${titleId}`, formData, {
        headers: {
          'Content-Type': undefined
        }
      });

      return {
        success: true,
        message: response.data.message || 'Changes submitted for approval!'
      };
    } catch (error) {
      console.error('Error submitting title changes:', error);

      const errorMessage = error.response?.data?.message ||
        error.response?.data?.error ||
        error.message ||
        'Failed to submit changes';

      return {
        success: false,
        error: errorMessage
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
