import apiClient from './apiClient.js'

const authApi = {
  async login(credentials) {
    try {
      const response = await apiClient.post('/auth/login', credentials);
      return response.data;
    } catch (error) {
      if (error.response?.data) return error.response.data;
      throw error;
    }
  },

  async acceptTerms(payload) {
    try {
      const response = await apiClient.post('/auth/accept-terms', payload);
      return response.data;
    } catch (error) {
      if (error.response?.data) return error.response.data;
      throw error;
    }
  },

  async register(userData) {
    try {
      const response = await apiClient.post('/auth/register', userData);
      return response.data;
    } catch (error) {
      if (error.response?.data) return error.response.data;
      throw error;
    }
  },

  async logout() {
    try {
      const response = await apiClient.post('/auth/logout', {}, {
        timeout: 3000,
        validateStatus: (status) => status < 500
      });
      return response.data || { success: true, message: 'Logout successful' };
    } catch (error) {
      console.warn('Logout API call encountered an issue:', error.message);
      return {
        success: true,
        message: 'Local logout completed',
        note: 'Server logout may have failed but local state has been cleared'
      };
    }
  },

  async getUserProfile() {
    const response = await apiClient.get('/auth/profile');
    return response.data;
  },

  async checkUserExists(email) {
    const response = await apiClient.get(`/auth/user-exists?email=${encodeURIComponent(email)}`);
    return response.data;
  },

  async refreshToken(refreshToken) {
    const response = await apiClient.post('/auth/refresh-token', { refreshToken });
    return response.data;
  },

  async validateToken() {
    const response = await apiClient.get('/auth/validate-token');
    return response.data;
  },

  async updateOnlineStatus(isOnline) {
    try {
      const response = await apiClient.patch('/auth/online-status', null, {
        params: { isOnline },
        timeout: 5000,
        validateStatus: (status) => status >= 200 && status < 300
      });
      return response.data;
    } catch (error) {
      console.warn('Failed to update online status:', error.message);
      return { success: false, message: 'Status update failed', error: error.message };
    }
  },

  async heartbeat() {
    try {
      const response = await apiClient.post('/auth/heartbeat', {}, {
        timeout: 5000,
        validateStatus: (status) => status >= 200 && status < 300
      });
      return response.data;
    } catch (error) {
      console.warn('Heartbeat failed:', error.message);
      return { success: false, message: 'Heartbeat failed', error: error.message };
    }
  },

  async healthCheck() {
    try {
      const response = await apiClient.get('/auth/health', { timeout: 3000 });
      return response.data;
    } catch (error) {
      console.warn('Health check failed:', error.message);
      return { status: 'unhealthy', error: error.message };
    }
  },

  async confirmEmail(userId, token) {
    try {
      const response = await apiClient.get('/auth/confirm-email', { params: { userId, token } });
      return response.data;
    } catch (error) {
      return error.response?.data ?? { success: false, message: 'Confirmation failed.' };
    }
  },

  async resendConfirmation(email) {
    try {
      const response = await apiClient.post('/auth/resend-confirmation', { email });
      return response.data;
    } catch (error) {
      return error.response?.data ?? { success: false, message: 'Failed to resend confirmation.' };
    }
  },

  async submitContact(formData) {
    try {
      const response = await apiClient.post('/contact', formData);
      return response.data;
    } catch (error) {
      return error.response?.data ?? { success: false, message: 'Failed to send message.' };
    }
  },

  async testConnection() {
    try {
      await this.healthCheck();
      return true;
    } catch (error) {
      return false;
    }
  }
};

export default authApi;
