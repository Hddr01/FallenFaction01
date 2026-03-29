// services/aiTranslationService.js
import axios from 'axios';

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

// ── Wallet ────────────────────────────────────────────────────────────────────
export const getWallet = () => api.get('/tickets/wallet');
export const getTransactions = (page = 1, pageSize = 20) =>
  api.get('/tickets/transactions', { params: { page, pageSize } });

// ── Unlock ────────────────────────────────────────────────────────────────────
export const getUnlockCost = (chapterId) => api.get(`/tickets/unlock-cost/${chapterId}`);
export const unlockChapter = (chapterId) => api.post('/tickets/unlock', { chapterId });

// ── Admin: Tickets ────────────────────────────────────────────────────────────
export const adminGrantTickets = (dto) => api.post('/tickets/admin/grant', dto);

// ── Translation Requests ──────────────────────────────────────────────────────
export const getTranslationRequests = (params = {}) =>
  api.get('/translation-requests', { params });

export const getMyRequests = (params = {}) =>
  api.get('/translation-requests/my', { params });

export const createTranslationRequest = (dto) =>
  api.post('/translation-requests', dto);

export const voteOnRequest = (id) =>
  api.post(`/translation-requests/${id}/vote`);

// ── Admin: Requests ───────────────────────────────────────────────────────────
export const adminGetRequestQueue = (params = {}) =>
  api.get('/translation-requests/admin/queue', { params });

export const adminReviewRequest = (dto) =>
  api.post('/translation-requests/admin/review', dto);

export const adminReleaseRequest = (dto) =>
  api.post('/translation-requests/admin/release', dto);

export const adminSearchAiTitles = (q = '') =>
  api.get('/translation-requests/admin/search-titles', { params: { q } });

// Search all titles in the system — used in the request form to warn about duplicates
export const searchExistingTitles = (q = '') =>
  api.get('/Titles/Search', { params: { query: q, limit: 5 } });

export default api;
