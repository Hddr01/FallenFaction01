import apiClient from './apiClient.js'
// services/homepageService.js
export const homepageService = new HomepageService();

// Export test function for debugging
export const testApiConnection = () => homepageService.testConnection();
