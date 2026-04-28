import apiClient from './apiClient.js'
// services/titleDetailsService.js
export const titleDetailsService = new TitleDetailsService();

// Export test function for debugging
export const testTitleApiConnection = () => titleDetailsService.testConnection();
