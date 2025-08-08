// services/adminApi.js - Complete admin API service with chapter management
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064/api',
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
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token expiration
api.interceptors.response.use(
  (response) => response,
  (error) => {
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

  // Get pending titles for approval
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

  // Get pending title details
  async getPendingTitleDetails(titleId) {
    try {
      const response = await api.get(`/AdminTitle/GetPendingTitleDetails?id=${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message
      };
    }
  },

  // Accept pending title
  async acceptTitle(titleId) {
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
  },

  // Reject pending title
  async rejectTitle(titleId, reason = '') {
    try {
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
        error: error.response?.data?.message || error.message
      };
    }
  },

  // Get approved titles for management
  async getApprovedTitles() {
    try {
      const response = await api.get('/AdminTitle/AdminTitleManagement');
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

  // Get title details for editing
  async getTitleDetails(titleId) {
    try {
      const response = await api.get(`/AdminTitle/GetTitleDetails/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message
      };
    }
  },

  // Update title
  async updateTitle(updateData) {
    try {
      const formData = new FormData();

      // Add files
      if (updateData.coverImage) {
        formData.append('coverImage', updateData.coverImage);
      }
      if (updateData.backgroundImage) {
        formData.append('backgroundImage', updateData.backgroundImage);
      }

      // Add simple fields
      formData.append('id', updateData.id);
      formData.append('originalTitle', updateData.originalTitle || '');
      formData.append('englishTitle', updateData.englishTitle || '');
      formData.append('alternativeNames', updateData.alternativeNames || '');
      formData.append('releaseDate', updateData.releaseDate || '');
      formData.append('description', updateData.description || '');
      formData.append('statusTitle', updateData.statusTitle || 'inproces');
      formData.append('statusTranslation', updateData.statusTranslation || 'inproces');
      formData.append('type', updateData.type || '1');
      formData.append('ageRestriction', updateData.ageRestriction || '0');
      formData.append('isAvailable', updateData.isAvailable || false);
      formData.append('areCommentsEnabled', updateData.areCommentsEnabled || false);
      formData.append('areChapterCommentsEnabled', updateData.areChapterCommentsEnabled || false);

      // Add array fields
      if (updateData.authors) {
        updateData.authors.forEach(id => formData.append('authors', id));
      }
      if (updateData.artists) {
        updateData.artists.forEach(id => formData.append('artists', id));
      }
      if (updateData.publishers) {
        updateData.publishers.forEach(id => formData.append('publishers', id));
      }
      if (updateData.teams) {
        updateData.teams.forEach(id => formData.append('teams', id));
      }
      if (updateData.categories) {
        updateData.categories.forEach(id => formData.append('categories', id));
      }
      if (updateData.tags) {
        updateData.tags.forEach(id => formData.append('tags', id));
      }
      if (updateData.formats) {
        updateData.formats.forEach(id => formData.append('formats', id));
      }

      // Add external links
      if (updateData.externalLinks) {
        updateData.externalLinks.forEach(link => formData.append('externalLinks', link));
      }

      const response = await api.post('/AdminTitle/UpdateTitle', formData, {
        headers: {
          'Content-Type': undefined
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
        error: error.response?.data?.message || error.message
      };
    }
  },

  // Delete title
  async deleteTitle(titleId) {
    try {
      const response = await api.post('/AdminTitle/DeleteTitle', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Title deleted successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message
      };
    }
  },

  // Toggle title availability
  async toggleTitleAvailability(titleId) {
    try {
      const response = await api.post('/AdminTitle/ToggleTitleAvailability', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Title availability updated successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message
      };
    }
  },

  // Toggle title comments
  async toggleTitleComments(titleId) {
    try {
      const response = await api.post('/AdminTitle/ToggleTitleComments', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Title comments updated successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message
      };
    }
  },

  // Toggle chapter comments
  async toggleChapterComments(titleId) {
    try {
      const response = await api.post('/AdminTitle/ToggleChapterComments', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Chapter comments updated successfully!',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message
      };
    }
  },

  // CHAPTER MANAGEMENT METHODS

  // Get pending chapters for admin review
  async getPendingChapters() {
    try {
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

  // Test API connectivity for chapters
  async testChapterConnection() {
    try {
      const response = await api.get('/Titles/chapters/pending');
      return response.status === 200;
    } catch (error) {
      console.error('Chapter API connection test failed:', error);
      return false;
    }
  }
};

export default adminApi;
