import apiClient from './apiClient.js'
// services/aiTranslationService.js

// ── Wallet ────────────────────────────────────────────────────────────────────
export const getWallet = () => apiClient.get('/tickets/wallet');
export const getTransactions = (page = 1, pageSize = 20) =>
  apiClient.get('/tickets/transactions', { params: { page, pageSize } });

// ── Unlock ────────────────────────────────────────────────────────────────────
export const getUnlockCost = (chapterId) => apiClient.get(`/tickets/unlock-cost/${chapterId}`);
export const unlockChapter = (chapterId) => apiClient.post('/tickets/unlock', { chapterId });

// ── Admin: Tickets ────────────────────────────────────────────────────────────
export const adminGrantTickets = (dto) => apiClient.post('/tickets/admin/grant', dto);

// ── Translation Requests ──────────────────────────────────────────────────────
export const getTranslationRequests = (params = {}) =>
  apiClient.get('/translation-requests', { params });

export const getMyRequests = (params = {}) =>
  apiClient.get('/translation-requests/my', { params });

export const createTranslationRequest = (dto) =>
  apiClient.post('/translation-requests', dto);

export const voteOnRequest = (id) =>
  apiClient.post(`/translation-requests/${id}/vote`);

// ── Admin: Requests ───────────────────────────────────────────────────────────
export const adminGetRequestQueue = (params = {}) =>
  apiClient.get('/translation-requests/admin/queue', { params });

export const adminReviewRequest = (dto) =>
  apiClient.post('/translation-requests/admin/review', dto);

export const adminReleaseRequest = (dto) =>
  apiClient.post('/translation-requests/admin/release', dto);

export const adminSearchAiTitles = (q = '') =>
  apiClient.get('/translation-requests/admin/search-titles', { params: { q } });

// Search all titles in the system — used in the request form to warn about duplicates
export const searchExistingTitles = (q = '') =>
  apiClient.get('/Titles/Search', { params: { query: q, limit: 5 } });
