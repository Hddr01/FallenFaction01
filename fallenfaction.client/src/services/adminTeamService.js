import apiClient from './apiClient.js'
// services/adminTeamService.js

export const adminTeamService = {
  // Get all teams (admin)
  async getAllTeams() {
    try {
      const response = await apiClient.get('/AdminTeam/');
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
      const response = await apiClient.get('/AdminTeam/SearchTeam', {
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
      const response = await apiClient.get(`/AdminTeam/${id}`);
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
      const response = await apiClient.put(`/AdminTeam/${id}`, teamData);
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
      const response = await apiClient.delete(`/AdminTeam/${id}`);
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
      const response = await apiClient.delete(`/AdminTeam/${teamId}/members/${userId}`);
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
      const response = await apiClient.put(`/AdminTeam/${teamId}/members/${userId}/role`, { role });
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
      const response = await apiClient.get('/AdminTeam/Statistics');
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
