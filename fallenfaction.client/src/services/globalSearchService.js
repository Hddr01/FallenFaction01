import apiClient from './apiClient.js'
// services/globalSearchService.js
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: { 'Accept': 'application/json' },
  withCredentials: true,
  timeout: 15000,
});

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (error) => Promise.reject(error)
);

// â”€â”€â”€ helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const safe = (promise) =>
  promise.then((r) => r).catch(() => []);

// â”€â”€â”€ individual searches â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

async function searchTitles(query) {
  // Hits the new GET /api/Titles/Search?query=... endpoint we added
  const res = await apiClient.get('/Titles/Search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 20) : [];
}

async function searchTeams(query) {
  const res = await apiClient.get('/Team/search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 10) : [];
}

async function searchAuthors(query) {
  const res = await apiClient.get('/Author/search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 10) : [];
}

async function searchArtists(query) {
  const res = await apiClient.get('/Artist/search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 10) : [];
}

async function searchPublishers(query) {
  const res = await apiClient.get('/Publisher/search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 10) : [];
}

async function searchTags(query) {
  const res = await apiClient.get('/Titles/Tags/Search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 10) : [];
}

async function searchUsers(query) {
  // Hits the new GET /api/Users/search?query=... endpoint we added
  const res = await apiClient.get('/Users/search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 10) : [];
}

// â”€â”€â”€ public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

export const globalSearchService = {
  /**
   * Search every category in parallel.
   */
  async searchAll(query) {
    if (!query || query.trim().length < 2) return emptyResults();

    const [titles, teams, authors, artists, publishers, tags, users] =
      await Promise.all([
        safe(searchTitles(query)),
        safe(searchTeams(query)),
        safe(searchAuthors(query)),
        safe(searchArtists(query)),
        safe(searchPublishers(query)),
        safe(searchTags(query)),
        safe(searchUsers(query)),
      ]);

    return { titles, teams, authors, artists, publishers, tags, users };
  },

  /**
   * Search a single category by key.
   * Key must be one of: titles | teams | authors | artists | publishers | tags | users
   */
  async searchCategory(category, query) {
    if (!query || query.trim().length < 2) return emptyResults();

    const fn = {
      titles: searchTitles,
      teams: searchTeams,
      authors: searchAuthors,
      artists: searchArtists,
      publishers: searchPublishers,
      tags: searchTags,
      users: searchUsers,
    }[category];

    if (!fn) return emptyResults();

    const results = await safe(fn(query));
    const r = emptyResults();
    r[category] = results;
    return r;
  },

  // Individual exports for direct use if needed
  searchTitles,
  searchTeams,
  searchAuthors,
  searchArtists,
  searchPublishers,
  searchTags,
  searchUsers,
};

function emptyResults() {
  return {
    titles: [],
    teams: [],
    authors: [],
    artists: [],
    publishers: [],
    tags: [],
    users: [],
  };
}

export default globalSearchService;
