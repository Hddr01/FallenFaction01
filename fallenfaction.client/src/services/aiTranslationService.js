import apiClient from './apiClient.js'
// services/aiTranslationService.js
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// â”€â”€ Wallet â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export const getWallet = () => apiClient.get('/tickets/wallet');
export const getTransactions = (page = 1, pageSize = 20) =>
  apiClient.get('/tickets/transactions', { params: { page, pageSize } });

// â”€â”€ Unlock â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export const getUnlockCost = (chapterId) => apiClient.get(`/tickets/unlock-cost/${chapterId}`);
export const unlockChapter = (chapterId) => apiClient.post('/tickets/unlock', { chapterId });

// â”€â”€ Admin: Tickets â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export const adminGrantTickets = (dto) => apiClient.post('/tickets/admin/grant', dto);

// â”€â”€ Translation Requests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export const getTranslationRequests = (params = {}) =>
  apiClient.get('/translation-requests', { params });

export const getMyRequests = (params = {}) =>
  apiClient.get('/translation-requests/my', { params });

export const createTranslationRequest = (dto) =>
  apiClient.post('/translation-requests', dto);

export const voteOnRequest = (id) =>
  apiClient.post(`/translation-requests/${id}/vote`);

// â”€â”€ Admin: Requests â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export const adminGetRequestQueue = (params = {}) =>
  apiClient.get('/translation-requests/admin/queue', { params });

export const adminReviewRequest = (dto) =>
  apiClient.post('/translation-requests/admin/review', dto);

export const adminReleaseRequest = (dto) =>
  apiClient.post('/translation-requests/admin/release', dto);

export const adminSearchAiTitles = (q = '') =>
  apiClient.get('/translation-requests/admin/search-titles', { params: { q } });

// Search all titles in the system â€” used in the request form to warn about duplicates
export const searchExistingTitles = (q = '') =>
  apiClient.get('/Titles/Search', { params: { query: q, limit: 5 } });

export default api;
