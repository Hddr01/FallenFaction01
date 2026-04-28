import apiClient from './apiClient.js'
// services/userService.js

export const userService = {
  // Get all users
  async getUsers() {
    try {
      const response = await apiClient.get('/AdminUsers/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch users'
      };
    }
  },

  // Search users
  async searchUsers(searchString) {
    try {
      const response = await apiClient.get('/AdminUsers/SearchUser', {
        params: { searchString }
      });
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search users'
      };
    }
  },

  // Get user by ID
  async getUserById(id) {
    try {
      const response = await apiClient.get(`/AdminUsers/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch user details'
      };
    }
  },

  // Ban user from site
  async banUserFromSite(userId) {
    try {
      const response = await apiClient.post('/AdminUsers/BanUser', {
        userId: userId,
        banType: 'site'
      });
      return {
        success: true,
        message: response.data.message || 'User banned from site successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to ban user from site'
      };
    }
  },

  // Ban user from comments
  async banUserFromComments(userId) {
    try {
      const response = await apiClient.post('/AdminUsers/BanUser', {
        userId: userId,
        banType: 'comments'
      });
      return {
        success: true,
        message: response.data.message || 'User banned from comments successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to ban user from comments'
      };
    }
  },

  // Unban user from site
  async unbanUserFromSite(userId) {
    try {
      const response = await apiClient.post('/AdminUsers/UnbanUser', {
        userId: userId,
        banType: 'site'
      });
      return {
        success: true,
        message: response.data.message || 'User unbanned from site successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to unban user from site'
      };
    }
  },

  // Unban user from comments
  async unbanUserFromComments(userId) {
    try {
      const response = await apiClient.post('/AdminUsers/UnbanUser', {
        userId: userId,
        banType: 'comments'
      });
      return {
        success: true,
        message: response.data.message || 'User unbanned from comments successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to unban user from comments'
      };
    }
  },

  // Change user role
  async changeUserRole(userId, role) {
    try {
      const response = await apiClient.post('/AdminUsers/ChangeUserRole', {
        userId: userId,
        role: role
      });
      return {
        success: true,
        message: response.data.message || 'User role changed successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to change user role'
      };
    }
  },

  // Delete user
  async deleteUser(userId) {
    try {
      const response = await apiClient.delete(`/AdminUsers/${userId}`);
      return {
        success: true,
        message: response.data.message || 'User deleted successfully!'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete user'
      };
    }
  },

  // Get available roles
  async getRoles() {
    try {
      const response = await apiClient.get('/AdminUsers/Roles');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch roles',
        data: []
      };
    }
  }
};
