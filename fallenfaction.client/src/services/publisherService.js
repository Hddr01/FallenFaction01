import apiClient from './apiClient.js'
// services/publisherService.js

export const publisherService = {
  // Get all publishers
  async getPublishers() {
    try {
      const response = await apiClient.get('/publisher/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching publishers:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch publishers'
      };
    }
  },

  // Get publisher by ID
  async getPublisherById(id) {
    try {
      const response = await apiClient.get(`/publisher/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching publisher:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch publisher'
      };
    }
  },

  // Create new publisher (Admin only)
  async createPublisher(publisherData) {
    try {
      const response = await apiClient.post('/publisher/', publisherData);
      return {
        success: true,
        data: response.data.data,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error creating publisher:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create publisher'
      };
    }
  },

  // Update publisher (Admin only)
  async updatePublisher(id, publisherData) {
    try {
      const response = await apiClient.put(`/publisher/${id}`, publisherData);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error updating publisher:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update publisher'
      };
    }
  },

  // Delete publisher (Admin only)
  async deletePublisher(id) {
    try {
      const response = await apiClient.delete(`/publisher/${id}`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error deleting publisher:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete publisher'
      };
    }
  },

  // Search publishers
  async searchPublishers(query) {
    try {
      const response = await apiClient.get(`/publisher/search?query=${encodeURIComponent(query)}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error searching publishers:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search publishers'
      };
    }
  }
};
