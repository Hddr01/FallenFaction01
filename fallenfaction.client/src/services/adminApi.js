// services/adminApi.js - FIXED Complete admin API service with chapter management
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Accept': 'application/json',
  },
  withCredentials: true,
  timeout: 10000, // 10 second timeout
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Log requests in development
    if (import.meta.env.DEV) {
      console.log(`Admin API Request: ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`, config.data);
    }

    return config;
  },
  (error) => {
    console.error('Admin API Request Error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor to handle token expiration
api.interceptors.response.use(
  (response) => {
    // Log responses in development
    if (import.meta.env.DEV) {
      console.log(`Admin API Response: ${response.status} ${response.config.url}`, response.data);
    }
    return response;
  },
  (error) => {
    console.error('Admin API Error:', error);
    console.error('Error details:', {
      status: error.response?.status,
      message: error.response?.data?.message,
      data: error.response?.data,
      url: error.config?.url
    });

    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
      if (!window.location.pathname.includes('/account/login')) {
        window.location.href = '/account/login';
      }
    }
    return Promise.reject(error);
  }
);

const adminApi = {
  // TITLE MANAGEMENT METHODS

  // FIXED: Get pending titles for approval - use correct endpoint
  async getPendingTitles() {
    try {
      console.log('Calling: GET /api/AdminTitle/PendingTitles');
      const response = await api.get('/AdminTitle/PendingTitles');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load pending titles',
        data: []
      };
    }
  },

  // Get pending title details
  async getPendingTitleDetails(titleId) {
    try {
      console.log(`Calling: GET /api/AdminTitle/GetPendingTitleDetails?id=${titleId}`);
      const response = await api.get(`/AdminTitle/GetPendingTitleDetails?id=${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load title details'
      };
    }
  },

  // FIXED: Accept pending title - use TitleApiController endpoint
  async acceptTitle(titleId) {
    try {
      console.log(`Calling: POST /api/TitleApi/approve/${titleId}`);
      const response = await api.post(`/TitleApi/approve/${titleId}`);
      return {
        success: true,
        message: response.data.message || 'Title approved successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to approve title'
      };
    }
  },

  // Reject pending title
  async rejectTitle(titleId, reason = '') {
    try {
      console.log('Calling: POST /api/AdminTitle/RejectTitle');
      const response = await api.post('/AdminTitle/RejectTitle', {
        id: titleId,
        reason: reason
      });
      return {
        success: true,
        message: response.data.message || 'Title rejected successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to reject title'
      };
    }
  },

  // Get approved titles for management
  async getApprovedTitles() {
    try {
      console.log('Calling: GET /api/AdminTitle/AdminTitleManagement');
      const response = await api.get('/AdminTitle/AdminTitleManagement');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load approved titles',
        data: []
      };
    }
  },

  // Get title details for editing
  async getTitleDetails(titleId) {
    try {
      console.log(`Calling: GET /api/AdminTitle/GetTitleDetails/${titleId}`);
      const response = await api.get(`/AdminTitle/GetTitleDetails/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load title details'
      };
    }
  },

  // FIXED: Update title - properly handle FormData
  async updateTitle(updateData) {
    try {
      console.log('Calling: POST /api/AdminTitle/UpdateTitle');
      const formData = new FormData();

      // Add ID first (required)
      formData.append('id', updateData.id);

      // Add files if present
      if (updateData.coverImage && updateData.coverImage instanceof File) {
        formData.append('coverImage', updateData.coverImage);
      }
      if (updateData.backgroundImage && updateData.backgroundImage instanceof File) {
        formData.append('backgroundImage', updateData.backgroundImage);
      }

      // Add simple text fields
      const textFields = [
        'originalTitle', 'englishTitle', 'alternativeNames', 'releaseDate',
        'description', 'statusTitle', 'statusTranslation'
      ];

      textFields.forEach(field => {
        formData.append(field, updateData[field] || '');
      });

      // Add numeric fields
      formData.append('type', updateData.type || '1');
      formData.append('ageRestriction', updateData.ageRestriction || '0');

      // Add boolean fields
      formData.append('isAvailable', updateData.isAvailable !== undefined ? updateData.isAvailable : true);
      formData.append('areCommentsEnabled', updateData.areCommentsEnabled !== undefined ? updateData.areCommentsEnabled : true);
      formData.append('areChapterCommentsEnabled', updateData.areChapterCommentsEnabled !== undefined ? updateData.areChapterCommentsEnabled : true);

      // Add array fields (many-to-many relationships)
      const arrayFields = ['authors', 'artists', 'publishers', 'teams', 'categories', 'tags', 'formats'];

      arrayFields.forEach(field => {
        if (updateData[field] && Array.isArray(updateData[field])) {
          updateData[field].forEach(id => formData.append(field, id));
        }
      });

      // Add external links
      if (updateData.externalLinks && Array.isArray(updateData.externalLinks)) {
        updateData.externalLinks.filter(link => link && link.trim()).forEach(link => {
          formData.append('externalLinks', link);
        });
      }

      const response = await api.post('/AdminTitle/UpdateTitle', formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });

      return {
        success: true,
        message: response.data.message || 'Title updated successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update title'
      };
    }
  },

  // Delete title
  async deleteTitle(titleId) {
    try {
      console.log('Calling: POST /api/AdminTitle/DeleteTitle');
      const response = await api.post('/AdminTitle/DeleteTitle', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Title deleted successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to delete title'
      };
    }
  },

  // Toggle title availability
  async toggleTitleAvailability(titleId) {
    try {
      console.log('Calling: POST /api/AdminTitle/ToggleTitleAvailability');
      const response = await api.post('/AdminTitle/ToggleTitleAvailability', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Title availability updated successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update title availability'
      };
    }
  },

  // Toggle title comments
  async toggleTitleComments(titleId) {
    try {
      console.log('Calling: POST /api/AdminTitle/ToggleTitleComments');
      const response = await api.post('/AdminTitle/ToggleTitleComments', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Title comments updated successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update title comments'
      };
    }
  },

  // Toggle chapter comments
  async toggleChapterComments(titleId) {
    try {
      console.log('Calling: POST /api/AdminTitle/ToggleChapterComments');
      const response = await api.post('/AdminTitle/ToggleChapterComments', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Chapter comments updated successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update chapter comments'
      };
    }
  },

  // Search titles
  async searchTitles(searchString) {
    try {
      console.log(`Calling: GET /api/AdminTitle/SearchTitle?searchString=${searchString}`);
      const response = await api.get(`/AdminTitle/SearchTitle?searchString=${encodeURIComponent(searchString)}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to search titles',
        data: []
      };
    }
  },

  // CHAPTER MANAGEMENT METHODS

  // Get pending chapters for admin review
  async getPendingChapters() {
    try {
      console.log('Calling: GET /api/Titles/chapters/pending');
      const response = await api.get('/Titles/chapters/pending');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading pending chapters:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load pending chapters',
        data: []
      };
    }
  },

  // Get detailed information about a specific pending chapter
  async getPendingChapterDetails(chapterId) {
    try {
      console.log(`Calling: GET /api/Titles/chapters/pending/${chapterId}`);
      const response = await api.get(`/Titles/chapters/pending/${chapterId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading pending chapter details:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapter details'
      };
    }
  },

  // Accept/approve a pending chapter
  async acceptChapter(chapterId) {
    try {
      console.log(`Calling: POST /api/Titles/chapters/pending/${chapterId}/approve`);
      const response = await api.post(`/Titles/chapters/pending/${chapterId}/approve`);
      return {
        success: true,
        message: response.data.message || 'Chapter accepted successfully!',
        data: response.data
      };
    } catch (error) {
      console.error('Error accepting chapter:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to accept chapter'
      };
    }
  },

  // Reject a pending chapter
  async rejectChapter(chapterId, reason = '') {
    try {
      console.log(`Calling: POST /api/Titles/chapters/pending/${chapterId}/reject`);
      const response = await api.post(`/Titles/chapters/pending/${chapterId}/reject`, {
        reason: reason
      });
      return {
        success: true,
        message: response.data.message || 'Chapter rejected successfully!',
        data: response.data
      };
    } catch (error) {
      console.error('Error rejecting chapter:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to reject chapter'
      };
    }
  },

  // UTILITY METHODS

  // Test API connectivity
  async testConnection() {
    try {
      console.log('Testing admin API connection...');
      const response = await api.get('/AdminTitle/AdminTitleManagement');
      console.log('Admin API connection test successful');
      return { success: true, data: response.data };
    } catch (error) {
      console.error('Admin API connection test failed:', error);
      return { success: false, error: error.message };
    }
  },


  // Add to the adminApi object in adminApi.js

  // TITLE CHANGE MANAGEMENT METHODS

  // Get all pending title changes
  async getPendingTitleChanges() {
    try {
      console.log('Calling: GET /api/AdminTitle/PendingChanges');
      const response = await api.get('/AdminTitle/PendingChanges');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load pending changes',
        data: []
      };
    }
  },

  // Get pending changes for a specific title
  async getPendingChangesForTitle(titleId) {
    try {
      console.log(`Calling: GET /api/AdminTitle/PendingChanges/${titleId}`);
      const response = await api.get(`/AdminTitle/PendingChanges/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load title changes'
      };
    }
  },

  // Approve all pending changes for a title
  async approveTitleChanges(titleId, adminComment = '') {
    try {
      console.log(`Calling: POST /api/AdminTitle/ApproveChanges/${titleId}`);
      const response = await api.post(`/AdminTitle/ApproveChanges/${titleId}`, {
        adminComment: adminComment
      });
      return {
        success: true,
        message: response.data.message || 'Changes approved successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to approve changes'
      };
    }
  },

  // Reject all pending changes for a title
  async rejectTitleChanges(titleId, rejectionReason, adminComment = '') {
    try {
      console.log(`Calling: POST /api/AdminTitle/RejectChanges/${titleId}`);
      const response = await api.post(`/AdminTitle/RejectChanges/${titleId}`, {
        rejectionReason: rejectionReason,
        adminComment: adminComment
      });
      return {
        success: true,
        message: response.data.message || 'Changes rejected successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to reject changes'
      };
    }
  },

  // Test chapter API connectivity
  async testChapterConnection() {
    try {
      console.log('Testing chapter API connection...');
      const response = await api.get('/Titles/chapters/pending');
      console.log('Chapter API connection test successful');
      return { success: true, data: response.data };
    } catch (error) {
      console.error('Chapter API connection test failed:', error);
      return { success: false, error: error.message };
    }
  },

  // Helper method to validate data before sending
  validateTitleData(titleData) {
    const errors = [];

    if (!titleData.id) {
      errors.push('Title ID is required');
    }

    if (!titleData.englishTitle || titleData.englishTitle.trim() === '') {
      errors.push('English title is required');
    }

    return {
      isValid: errors.length === 0,
      errors: errors
    };
  },

  // Helper method to prepare form data
  prepareFormData(data) {
    const formData = new FormData();

    Object.keys(data).forEach(key => {
      const value = data[key];

      if (value === null || value === undefined) {
        return; // Skip null/undefined values
      }

      if (value instanceof File) {
        formData.append(key, value);
      } else if (Array.isArray(value)) {
        value.forEach(item => formData.append(key, item));
      } else {
        formData.append(key, String(value));
      }
    });

    return formData;
  }
};

export default adminApi;
