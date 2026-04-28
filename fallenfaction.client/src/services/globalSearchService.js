import apiClient from './apiClient.js'
// services/globalSearchService.js

// ─── helpers ──────────────────────────────────────────────────────────────────
const safe = (promise) =>
  promise.then((r) => r).catch(() => []);

// ─── individual searches ──────────────────────────────────────────────────────

async function searchTitles(query) {
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
  const res = await apiClient.get('/Users/search', { params: { query } });
  return Array.isArray(res.data) ? res.data.slice(0, 10) : [];
}

// ─── public API ───────────────────────────────────────────────────────────────

export const globalSearchService = {
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

  searchTitles,
  searchTeams,
  searchAuthors,
  searchArtists,
  searchPublishers,
  searchTags,
  searchUsers,
};

function emptyResults() {
  return { titles: [], teams: [], authors: [], artists: [], publishers: [], tags: [], users: [] };
}

export default globalSearchService;
