// services/teamService.js
import axios from 'axios';

// Use Vite environment variables (not process.env)
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api';

// Create axios instance with base configuration
const teamApi = axios.create({
  baseURL: `${API_BASE_URL}/team`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000,
});

// Add request interceptor to include auth token
teamApi.interceptors.request.use(
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
teamApi.interceptors.response.use(
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
      await teamApi.put(`/${teamId}/members/${userId}/role`, { role });
      return {
        success: true
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update member role'
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
  }
};
