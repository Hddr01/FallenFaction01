import apiClient from './apiClient.js'
// services/teamService.js - Updated with image upload functionality

export const teamService = {
  async getAllTeams() {
    try {
      const response = await apiClient.get('/team/');
      return { success: true, data: response.data };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to fetch teams' };
    }
  },

  async getTeamById(id) {
    try {
      const response = await apiClient.get(`/team/${id}`);
      return { success: true, data: response.data };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to fetch team details' };
    }
  },

  async createTeam(teamData) {
    try {
      const response = await apiClient.post('/team/', teamData);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create team',
        validationErrors: error.response?.data?.errors
      };
    }
  },

  async updateTeam(id, teamData) {
    try {
      await apiClient.put(`/team/${id}`, teamData);
      return { success: true };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update team',
        validationErrors: error.response?.data?.errors
      };
    }
  },

  async deleteTeam(id) {
    try {
      await apiClient.delete(`/team/${id}`);
      return { success: true };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to delete team' };
    }
  },

  async uploadAvatar(teamId, file) {
    try {
      const formData = new FormData();
      formData.append('file', file);
      const response = await apiClient.post(`/team/${teamId}/upload-avatar`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      return { success: true, data: response.data };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to upload avatar' };
    }
  },

  async uploadBackground(teamId, file) {
    try {
      const formData = new FormData();
      formData.append('file', file);
      const response = await apiClient.post(`/team/${teamId}/upload-background`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      return { success: true, data: response.data };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to upload background' };
    }
  },

  async deleteAvatar(teamId) {
    try {
      const response = await apiClient.delete(`/team/${teamId}/avatar`);
      return { success: true, message: response.data.message };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to delete avatar' };
    }
  },

  async deleteBackground(teamId) {
    try {
      const response = await apiClient.delete(`/team/${teamId}/background`);
      return { success: true, message: response.data.message };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to delete background' };
    }
  },

  async joinTeam(id) {
    try {
      await apiClient.post(`/team/${id}/join`);
      return { success: true };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to join team' };
    }
  },

  async leaveTeam(id) {
    try {
      await apiClient.delete(`/team/${id}/leave`);
      return { success: true };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to leave team' };
    }
  },

  async updateMemberRole(teamId, userId, role) {
    try {
      const roleValue = typeof role === 'string' ? parseInt(role) : role;
      if (isNaN(roleValue) || roleValue < 0 || roleValue > 2) {
        throw new Error('Invalid role value. Must be 0 (Admin), 1 (Member), or 2 (Viewer)');
      }
      const response = await apiClient.put(`/team/${teamId}/members/${userId}/role`, { role: roleValue });
      return { success: true, message: response.data?.message || 'Member role updated successfully' };
    } catch (error) {
      let errorMessage = 'Failed to update member role';
      if (error.response?.status === 400) {
        errorMessage = error.response?.data?.message || error.response?.data?.title || 'Invalid request.';
      } else if (error.response?.status === 403) {
        errorMessage = 'You do not have permission to update member roles.';
      } else if (error.response?.status === 404) {
        errorMessage = 'Team or user not found.';
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      }
      return { success: false, error: errorMessage };
    }
  },

  async getMyTeams() {
    try {
      const response = await apiClient.get('/team/my-teams');
      return { success: true, data: response.data };
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to fetch your teams' };
    }
  },

  async getUserTeams() {
    return this.getMyTeams();
  },

  async getTeamPermissions(teamId) {
    try {
      const response = await apiClient.get(`/team/${teamId}/permissions`);
      return { success: true, data: response.data };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch team permissions',
        data: { teamId, isMember: false, role: null, permissions: [] }
      };
    }
  },

  getRoleName(roleValue) {
    return { 0: 'Admin', 1: 'Member', 2: 'Viewer' }[roleValue] || 'Unknown';
  },

  getRoleValue(roleName) {
    const v = { Admin: 0, Member: 1, Viewer: 2 }[roleName];
    return v !== undefined ? v : null;
  },

  isValidRole(role) {
    const v = typeof role === 'string' ? parseInt(role) : role;
    return !isNaN(v) && v >= 0 && v <= 2;
  },

  getImageUrl(path) {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    return path;
  },

  validateImageFile(file, maxSizeMB = 5) {
    if (!file) return { valid: false, error: 'No file provided' };
    if (!file.type.startsWith('image/')) return { valid: false, error: 'File must be an image' };
    if (file.size > maxSizeMB * 1024 * 1024) return { valid: false, error: `File size must be less than ${maxSizeMB}MB` };
    return { valid: true };
  }
};
