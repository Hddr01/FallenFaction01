// services/teamService.js - FIXED VERSION with improved role update debugging
import axios from 'axios';

// Use Vite environment variables (not process.env)
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064/api';

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

    if (import.meta.env.DEV) {
      console.log(`Team API Request: ${config.method?.toUpperCase()} ${config.url}`, config.data);
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add response interceptor for error handling
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

// SINGLE EXPORT - export const (not export { teamService } at the end)
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

  // FIXED: Update member role to match backend UpdateMemberRoleDto structure
  async updateMemberRole(teamId, userId, role) {
    try {
      console.log('Updating member role:', { teamId, userId, role });

      // Ensure role is a number (TeamRole enum)
      const roleValue = typeof role === 'string' ? parseInt(role) : role;

      // Validate role value (0=Admin, 1=Member, 2=Viewer)
      if (isNaN(roleValue) || roleValue < 0 || roleValue > 2) {
        throw new Error('Invalid role value. Must be 0 (Admin), 1 (Member), or 2 (Viewer)');
      }

      // Send the correct data structure expected by UpdateMemberRoleDto
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

      // Enhanced error reporting
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

      // Handle specific error cases
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
  }
};
