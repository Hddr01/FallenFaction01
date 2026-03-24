// services/globalSearchService.js
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Accept': 'application/json',
  },
  withCredentials: true,
  timeout: 15000,
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

export const globalSearchService = {
  /**
   * Search across all entities
   * @param {string} query - Search query
   * @returns {Object} - Categorized search results
   */
  async searchAll(query) {
    if (!query || query.trim().length < 2) {
      return {
        titles: [],
        teams: [],
        authors: [],
        artists: [],
        publishers: [],
        tags: [],
        users: []
      };
    }

    const trimmedQuery = query.trim();

    try {
      // Execute all searches in parallel
      const [
        titlesResult,
        teamsResult,
        authorsResult,
        artistsResult,
        publishersResult,
        tagsResult,
        usersResult
      ] = await Promise.allSettled([
        this.searchTitles(trimmedQuery),
        this.searchTeams(trimmedQuery),
        this.searchAuthors(trimmedQuery),
        this.searchArtists(trimmedQuery),
        this.searchPublishers(trimmedQuery),
        this.searchTags(trimmedQuery),
        this.searchUsers(trimmedQuery)
      ]);

      return {
        titles: titlesResult.status === 'fulfilled' ? titlesResult.value : [],
        teams: teamsResult.status === 'fulfilled' ? teamsResult.value : [],
        authors: authorsResult.status === 'fulfilled' ? authorsResult.value : [],
        artists: artistsResult.status === 'fulfilled' ? artistsResult.value : [],
        publishers: publishersResult.status === 'fulfilled' ? publishersResult.value : [],
        tags: tagsResult.status === 'fulfilled' ? tagsResult.value : [],
        users: usersResult.status === 'fulfilled' ? usersResult.value : []
      };
    } catch (error) {
      console.error('Global search error:', error);
      return {
        titles: [],
        teams: [],
        authors: [],
        artists: [],
        publishers: [],
        tags: [],
        users: []
      };
    }
  },

  /**
   * Search titles
   */
  async searchTitles(query) {
    try {
      const response = await api.get('/Titles/Search', {
        params: { query }
      });
      return response.data.slice(0, 10); // Limit to 10 results
    } catch (error) {
      console.error('Error searching titles:', error);
      return [];
    }
  },

  /**
   * Search teams
   */
  async searchTeams(query) {
    try {
      const response = await api.get('/Team/search', {
        params: { query }
      });
      return response.data.slice(0, 10);
    } catch (error) {
      console.error('Error searching teams:', error);
      return [];
    }
  },

  /**
   * Search authors
   */
  async searchAuthors(query) {
    try {
      const response = await api.get('/Author/search', {
        params: { query }
      });
      return response.data.slice(0, 10);
    } catch (error) {
      console.error('Error searching authors:', error);
      return [];
    }
  },

  /**
   * Search artists
   */
  async searchArtists(query) {
    try {
      const response = await api.get('/Artist/search', {
        params: { query }
      });
      return response.data.slice(0, 10);
    } catch (error) {
      console.error('Error searching artists:', error);
      return [];
    }
  },

  /**
   * Search publishers
   */
  async searchPublishers(query) {
    try {
      const response = await api.get('/Publisher/search', {
        params: { query }
      });
      return response.data.slice(0, 10);
    } catch (error) {
      console.error('Error searching publishers:', error);
      return [];
    }
  },

  /**
   * Search tags
   */
  async searchTags(query) {
    try {
      // Fetch all tags and filter client-side since there's no search endpoint
      const response = await api.get('/TitleApi/form-data');
      const allTags = response.data.Tags || [];

      return allTags
        .filter(tag => tag.name.toLowerCase().includes(query.toLowerCase()))
        .slice(0, 10);
    } catch (error) {
      console.error('Error searching tags:', error);
      return [];
    }
  },

  /**
   * Search users
   */
  async searchUsers(query) {
    try {
      // Since there's no user search endpoint, we'll fetch top users and filter
      // In production, you'd want a dedicated user search endpoint
      const response = await api.get('/Users/TopUsers');
      const allUsers = response.data || [];

      return allUsers
        .filter(user =>
          user.name?.toLowerCase().includes(query.toLowerCase())
        )
        .slice(0, 10);
    } catch (error) {
      console.error('Error searching users:', error);
      return [];
    }
  }
};

export default globalSearchService;
