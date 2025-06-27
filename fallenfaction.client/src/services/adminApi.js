// services/adminApi.js - Updated with title management methods
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api',
  headers: {
    'Accept': 'application/json',
  },
  withCredentials: true,
  timeout: 30000, // Increased timeout for file uploads
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
  // Get all pending titles
  async getPendingTitles() {
    try {
      const response = await api.get('/TitleApi/pending');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching pending titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to fetch pending titles',
        data: []
      };
    }
  },

  // Get pending title details
  async getPendingTitleDetails(titleId) {
    try {
      const response = await api.get(`/AdminTitle/GetPendingTitleDetails`, {
        params: { id: titleId }
      });
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching pending title details:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to fetch title details'
      };
    }
  },

  // NEW: Get approved title details for editing
  async getTitleDetails(titleId) {
    try {
      const response = await api.get(`/AdminTitle/GetTitleDetails/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching title details:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to fetch title details'
      };
    }
  },

  // Accept a pending title
  async acceptTitle(titleId) {
    try {
      const response = await api.post('/TitleApi/approve/' + titleId);
      return {
        success: true,
        message: response.data.message || 'Title approved successfully!',
        data: response.data
      };
    } catch (error) {
      console.error('Error accepting title:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to accept title'
      };
    }
  },

  // Reject a pending title
  async rejectTitle(titleId) {
    try {
      const response = await api.post('/AdminTitle/RejectTitle', { id: titleId });
      return {
        success: true,
        message: response.data.message || 'Title rejected successfully!',
        data: response.data
      };
    } catch (error) {
      console.error('Error rejecting title:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to reject title'
      };
    }
  },

  // Get all approved titles (for main admin management)
  async getApprovedTitles() {
    try {
      const response = await api.get('/AdminTitle/AdminTitleManagement');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching approved titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to fetch approved titles',
        data: []
      };
    }
  },

  // NEW: Update an existing title
  async updateTitle(titleData) {
    try {
      console.log('Updating title with data:', titleData);

      // Create FormData for file uploads
      const formData = new FormData();

      // Add basic fields
      formData.append('id', titleData.id);
      formData.append('originalTitle', titleData.originalTitle || '');
      formData.append('englishTitle', titleData.englishTitle || '');
      formData.append('alternativeNames', titleData.alternativeNames || '');
      formData.append('releaseDate', titleData.releaseDate || '');
      formData.append('type', String(titleData.type || 1));
      formData.append('statusTitle', titleData.statusTitle || 'inproces');
      formData.append('statusTranslation', titleData.statusTranslation || 'inproces');
      formData.append('ageRestriction', String(titleData.ageRestriction || 0));
      formData.append('description', titleData.description || '');
      formData.append('isAvailable', String(titleData.isAvailable ?? true));
      formData.append('areCommentsEnabled', String(titleData.areCommentsEnabled ?? true));
      formData.append('areChapterCommentsEnabled', String(titleData.areChapterCommentsEnabled ?? true));

      // Add image files if provided
      if (titleData.coverImage && titleData.coverImage instanceof File) {
        formData.append('coverImage', titleData.coverImage);
      }
      if (titleData.backgroundImage && titleData.backgroundImage instanceof File) {
        formData.append('backgroundImage', titleData.backgroundImage);
      }

      // Add array fields
      const arrayFields = ['authors', 'artists', 'publishers', 'teams', 'categories', 'tags', 'formats'];
      arrayFields.forEach(fieldName => {
        if (titleData[fieldName] && Array.isArray(titleData[fieldName])) {
          titleData[fieldName].forEach(id => {
            if (id !== null && id !== undefined && id !== '') {
              formData.append(fieldName, String(id));
            }
          });
        }
      });

      // Add external links
      if (titleData.externalLinks && Array.isArray(titleData.externalLinks)) {
        titleData.externalLinks
          .filter(link => link && link.trim())
          .forEach(link => {
            formData.append('externalLinks', link.trim());
          });
      }

      const response = await api.post('/AdminTitle/UpdateTitle', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        timeout: 60000, // 60 second timeout for uploads
      });

      return {
        success: true,
        message: response.data.message || 'Title updated successfully!',
        data: response.data
      };
    } catch (error) {
      console.error('Error updating title:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update title'
      };
    }
  },

  // Search titles
  async searchTitles(searchString) {
    try {
      const response = await api.get('/AdminTitle/SearchTitle', {
        params: { searchString }
      });
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error searching titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to search titles',
        data: []
      };
    }
  },

  // Toggle title availability
  async toggleTitleAvailability(titleId) {
    try {
      const response = await api.post('/AdminTitle/ToggleTitleAvailability', { id: titleId });
      return {
        success: true,
        message: 'Title availability updated successfully!'
      };
    } catch (error) {
      console.error('Error toggling title availability:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update title availability'
      };
    }
  },

  // Toggle title comments
  async toggleTitleComments(titleId) {
    try {
      const response = await api.post('/AdminTitle/ToggleTitleComments', { id: titleId });
      return {
        success: true,
        message: 'Title comments updated successfully!'
      };
    } catch (error) {
      console.error('Error toggling title comments:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update title comments'
      };
    }
  },

  // Toggle chapter comments
  async toggleChapterComments(titleId) {
    try {
      const response = await api.post('/AdminTitle/ToggleChapterComments', { id: titleId });
      return {
        success: true,
        message: 'Chapter comments updated successfully!'
      };
    } catch (error) {
      console.error('Error toggling chapter comments:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update chapter comments'
      };
    }
  },

  // Delete title permanently
  async deleteTitle(titleId) {
    try {
      const response = await api.post('/AdminTitle/DeleteTitle', { id: titleId });
      return {
        success: true,
        message: 'Title deleted successfully!'
      };
    } catch (error) {
      console.error('Error deleting title:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to delete title'
      };
    }
  }
};

export default adminApi;

