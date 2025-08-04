// services/adminTeamService.js
import axios from 'axios';

// Use Vite environment variables
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api';

// Create axios instance with base configuration
const adminTeamApi = axios.create({
  baseURL: `${API_BASE_URL}/AdminTeam`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000,
});

// Add request interceptor to include auth token
adminTeamApi.interceptors.request.use(
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
adminTeamApi.interceptors.response.use(
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

export const adminTeamService = {
  // Get all teams (admin)
  async getAllTeams() {
    try {
      const response = await adminTeamApi.get('/');
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

  // Search teams
  async searchTeams(searchString) {
    try {
      const response = await adminTeamApi.get('/SearchTeam', {
        params: { searchString }
      });
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search teams'
      };
    }
  },

  // Get team by ID (admin)
  async getTeamById(id) {
    try {
      const response = await adminTeamApi.get(`/${id}`);
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

  // Update team (admin)
  async updateTeam(id, teamData) {
    try {
      const response = await adminTeamApi.put(`/${id}`, teamData);
      return {
        success: true,
        message: response.data.message || 'Team updated successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update team',
        validationErrors: error.response?.data?.errors
      };
    }
  },

  // Delete team (admin)
  async deleteTeam(id) {
    try {
      const response = await adminTeamApi.delete(`/${id}`);
      return {
        success: true,
        message: response.data.message || 'Team deleted successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete team'
      };
    }
  },

  // Remove member from team (admin)
  async removeMember(teamId, userId) {
    try {
      const response = await adminTeamApi.delete(`/${teamId}/members/${userId}`);
      return {
        success: true,
        message: response.data.message || 'Member removed successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to remove member'
      };
    }
  },

  // Update member role (admin)
  async updateMemberRole(teamId, userId, role) {
    try {
      const response = await adminTeamApi.put(`/${teamId}/members/${userId}/role`, { role });
      return {
        success: true,
        message: response.data.message || 'Member role updated successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update member role'
      };
    }
  },

  // Get team statistics (admin)
  async getTeamStatistics() {
    try {
      const response = await adminTeamApi.get('/Statistics');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch statistics',
        data: {}
      };
    }
  }
};
