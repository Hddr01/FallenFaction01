import apiClient from './apiClient.js'
// services/authorService.js

export const authorService = {
  // Get all authors
  async getAuthors() {
    try {
      const response = await apiClient.get('/author/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching authors:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch authors'
      };
    }
  },

  // Get author by ID
  async getAuthorById(id) {
    try {
      const response = await apiClient.get(`/author/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching author:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch author'
      };
    }
  },

  // Create new author (Admin only)
  async createAuthor(authorData) {
    try {
      const response = await apiClient.post('/author/', authorData);
      return {
        success: true,
        data: response.data.data,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error creating author:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create author'
      };
    }
  },

  // Update author (Admin only)
  async updateAuthor(id, authorData) {
    try {
      const response = await apiClient.put(`/author/${id}`, authorData);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error updating author:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update author'
      };
    }
  },

  // Delete author (Admin only)
  async deleteAuthor(id) {
    try {
      const response = await apiClient.delete(`/author/${id}`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error deleting author:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete author'
      };
    }
  },

  // Search authors
  async searchAuthors(query) {
    try {
      const response = await apiClient.get(`/author/search?query=${encodeURIComponent(query)}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error searching authors:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search authors'
      };
    }
  }
};
