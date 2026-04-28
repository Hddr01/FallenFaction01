import apiClient from './apiClient.js'

export const homepageService = {
  async getFeaturedManga() {
    try {
      const response = await apiClient.get('/Titles/Featured')
      return { success: true, data: response.data }
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to load featured manga' }
    }
  },

  async getPopularTitles() {
    try {
      const response = await apiClient.get('/Titles/Popular')
      return { success: true, data: response.data }
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to load popular titles' }
    }
  },

  async getRecentUpdates() {
    try {
      const response = await apiClient.get('/Titles/RecentUpdates')
      return { success: true, data: response.data }
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to load recent updates' }
    }
  },

  async getTopUsers() {
    try {
      const response = await apiClient.get('/Users/TopUsers')
      return { success: true, data: response.data }
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to load top users' }
    }
  },

  async getTopTeams() {
    try {
      const response = await apiClient.get('/Team/TopTeams')
      return { success: true, data: response.data }
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Failed to load top teams' }
    }
  },

  async testConnection() {
    try {
      const response = await apiClient.get('/Titles/Debug')
      return { success: true, data: response.data }
    } catch (error) {
      return { success: false, error: error.response?.data?.message || 'Connection test failed' }
    }
  },
}

export const testApiConnection = () => homepageService.testConnection()
