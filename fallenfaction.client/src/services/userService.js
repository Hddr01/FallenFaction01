// services/userService.js
import axios from 'axios';

// Use Vite environment variables
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

// Create axios instance with base configuration
const userApi = axios.create({
  baseURL: `${API_BASE_URL}/AdminUsers`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
  timeout: 10000,
});

// Add request interceptor to include auth token
userApi.interceptors.request.use(
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
userApi.interceptors.response.use(
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

export const userService = {
  // Get all users
  async getUsers() {
    try {
      const response = await userApi.get('/');
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
      const response = await userApi.get('/SearchUser', {
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
      const response = await userApi.get(`/${id}`);
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
      const response = await userApi.post('/BanUser', {
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
      const response = await userApi.post('/BanUser', {
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
      const response = await userApi.post('/UnbanUser', {
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
      const response = await userApi.post('/UnbanUser', {
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
      const response = await userApi.post('/ChangeUserRole', {
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
      const response = await userApi.delete(`/${userId}`);
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
      const response = await userApi.get('/Roles');
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
