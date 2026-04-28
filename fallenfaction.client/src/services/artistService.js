import apiClient from './apiClient.js'
// services/artistService.js

export const artistService = {
  // Get all artists
  async getArtists() {
    try {
      const response = await apiClient.get('/artist/');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching artists:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch artists'
      };
    }
  },

  // Get artist by ID
  async getArtistById(id) {
    try {
      const response = await apiClient.get(`/artist/${id}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching artist:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to fetch artist'
      };
    }
  },

  // Create new artist (authenticated users)
  async createArtist(artistData) {
    try {
      const response = await apiClient.post('/artist/', artistData);
      return {
        success: true,
        data: response.data.data,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error creating artist:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to create artist'
      };
    }
  },

  // Update artist (authenticated users)
  async updateArtist(id, artistData) {
    try {
      const response = await apiClient.put(`/artist/${id}`, artistData);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error updating artist:', error);

      if (error.response?.status === 400) {
        return {
          success: false,
          error: error.response.data.message || 'Invalid input data',
          validationErrors: error.response.data.errors || []
        };
      }

      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update artist'
      };
    }
  },

  // Delete artist (authenticated users)
  async deleteArtist(id) {
    try {
      const response = await apiClient.delete(`/artist/${id}`);
      return {
        success: true,
        message: response.data.message
      };
    } catch (error) {
      console.error('Error deleting artist:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete artist'
      };
    }
  },

  // Search artists
  async searchArtists(query) {
    try {
      const response = await apiClient.get(`/artist/search?query=${encodeURIComponent(query)}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error searching artists:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to search artists'
      };
    }
  }
};
