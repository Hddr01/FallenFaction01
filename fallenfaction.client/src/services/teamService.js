import apiClient from './apiClient.js'
// services/teamService.js - Updated with image upload functionality
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

const teamApi = axios.create({
  baseURL: `${API_BASE_URL}/team`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000,
});

teamApi.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    if (import.meta.env.DEV) {
      console.log(`Team API Request: ${config.method?.toUpperCase()} ${config.url}`, config.data);
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

teamApi.interceptors.response.use(
  (response) => {
    if (import.meta.env.DEV) {
      console.log(`Team API Response: ${response.status} ${response.config.url}`, response.data);
    }
    return response;
  },
  (error) => {
    console.error('Team API Error:', error);
    console.error('Error details:', {
      status: error.response?.status,
      data: error.response?.data,
      url: error.config?.url,
      requestData: error.config?.data
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

export const teamService = {
  // Get all teams
  async getAllTeams() {
    try {
      const response = await teamApi.get('/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch teams'
      };
    }
  },

  // Get team by ID
  async getTeamById(id) {
    try {
      const response = await teamApi.get(`/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch team details'
      };
    }
  },

  // Create new team
  async createTeam(teamData) {
    try {
      const response = await teamApi.post('/', teamData);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create team',
        validationErrors: error.response?.data?.errors
      };
    }
  },

  // Update team
  async updateTeam(id, teamData) {
    try {
      await teamApi.put(`/${id}`, teamData);
      return {
        success: true
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update team',
        validationErrors: error.response?.data?.errors
      };
    }
  },

  // Delete team
  async deleteTeam(id) {
    try {
      await teamApi.delete(`/${id}`);
      return {
        success: true
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete team'
      };
    }
  },

  // Upload avatar
  async uploadAvatar(teamId, file) {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await teamApi.post(`/${teamId}/upload-avatar`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });

      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to upload avatar'
      };
    }
  },

  // Upload background
  async uploadBackground(teamId, file) {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await teamApi.post(`/${teamId}/upload-background`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });

      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to upload background'
      };
    }
  },

  // Delete avatar
  async deleteAvatar(teamId) {
    try {
      const response = await teamApi.delete(`/${teamId}/avatar`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete avatar'
      };
    }
  },

  // Delete background
  async deleteBackground(teamId) {
    try {
      const response = await teamApi.delete(`/${teamId}/background`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete background'
      };
    }
  },

  // Join team
  async joinTeam(id) {
    try {
      await teamApi.post(`/${id}/join`);
      return {
        success: true
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to join team'
      };
    }
  },

  // Leave team
  async leaveTeam(id) {
    try {
      await teamApi.delete(`/${id}/leave`);
      return {
        success: true
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to leave team'
      };
    }
  },

  // Update member role
  async updateMemberRole(teamId, userId, role) {
    try {
      console.log('Updating member role:', { teamId, userId, role });

      const roleValue = typeof role === 'string' ? parseInt(role) : role;

      if (isNaN(roleValue) || roleValue < 0 || roleValue > 2) {
        throw new Error('Invalid role value. Must be 0 (Admin), 1 (Member), or 2 (Viewer)');
      }

      const payload = {
        role: roleValue
      };

      console.log('Sending payload:', payload);

      const response = await teamApi.put(`/${teamId}/members/${userId}/role`, payload);

      console.log('Role update successful:', response.data);

      return {
        success: true,
        message: response.data?.message || 'Member role updated successfully'
      };

    } catch (error) {
      console.error('Error updating member role:', error);

      const errorDetails = {
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
        url: error.config?.url,
        method: error.config?.method,
        requestData: error.config?.data,
        teamId,
        userId,
        role
      };

      console.error('Detailed error information:', errorDetails);

      let errorMessage = 'Failed to update member role';

      if (error.response?.status === 400) {
        errorMessage = error.response?.data?.message ||
          error.response?.data?.title ||
          'Invalid request. Please check the role value and try again.';
      } else if (error.response?.status === 403) {
        errorMessage = 'You do not have permission to update member roles.';
      } else if (error.response?.status === 404) {
        errorMessage = 'Team or user not found.';
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      }

      return {
        success: false,
        error: errorMessage,
        details: errorDetails
      };
    }
  },

  // Get my teams
  async getMyTeams() {
    try {
      const response = await teamApi.get('/my-teams');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch your teams'
      };
    }
  },

  // Get user teams (alias for compatibility)
  async getUserTeams() {
    return this.getMyTeams();
  },

  // Get team permissions for current user
  async getTeamPermissions(teamId) {
    try {
      const response = await teamApi.get(`/${teamId}/permissions`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch team permissions',
        data: {
          teamId: teamId,
          isMember: false,
          role: null,
          permissions: []
        }
      };
    }
  },

  // Helper function to get role name from enum value
  getRoleName(roleValue) {
    const roles = {
      0: 'Admin',
      1: 'Member',
      2: 'Viewer'
    };
    return roles[roleValue] || 'Unknown';
  },

  // Helper function to get role value from name
  getRoleValue(roleName) {
    const roles = {
      'Admin': 0,
      'Member': 1,
      'Viewer': 2
    };
    return roles[roleName] !== undefined ? roles[roleName] : null;
  },

  // Validate team role value
  isValidRole(role) {
    const roleValue = typeof role === 'string' ? parseInt(role) : role;
    return !isNaN(roleValue) && roleValue >= 0 && roleValue <= 2;
  },

  // Helper to get full image URL
  getImageUrl(path) {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    const baseUrl = API_BASE_URL.replace('/api', '');
    return `${baseUrl}${path}`;
  },

  // Validate image file
  validateImageFile(file, maxSizeMB = 5) {
    if (!file) {
      return { valid: false, error: 'No file provided' };
    }

    if (!file.type.startsWith('image/')) {
      return { valid: false, error: 'File must be an image' };
    }

    const maxSize = maxSizeMB * 1024 * 1024;
    if (file.size > maxSize) {
      return { valid: false, error: `File size must be less than ${maxSizeMB}MB` };
    }

    return { valid: true };
  }
};
