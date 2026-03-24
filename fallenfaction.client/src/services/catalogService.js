// services/catalogService.js - Updated to match backend DTOs
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

// Response interceptor to handle errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
      sessionStorage.removeItem('authToken');
      sessionStorage.removeItem('authUser');
      if (!window.location.pathname.includes('/account/login')) {
        window.location.href = '/account/login';
      }
    }
    return Promise.reject(error);
  }
);

// MangaType enum (matches backend)
export const MangaType = {
  Manga: 1,
  Manhwa: 2,
  Manhua: 3,
  WesternComic: 4,
  RussianComic: 5,
  IndonesianComic: 6
};

export const catalogService = {
  /**
   * Get catalog titles with filters, sorting, and pagination
   * GET: api/Titles/Catalog
   */
  async getCatalogTitles(params = {}) {
    try {
      const queryParams = new URLSearchParams();

      // Pagination
      if (params.page) queryParams.append('page', params.page);
      if (params.pageSize) queryParams.append('pageSize', params.pageSize);

      // Search
      if (params.search) queryParams.append('search', params.search);

      // Filters
      if (params.type) queryParams.append('type', params.type);
      if (params.status) queryParams.append('status', params.status);
      if (params.translationStatus) queryParams.append('translationStatus', params.translationStatus);
      if (params.ageRestriction !== null && params.ageRestriction !== undefined) {
        queryParams.append('ageRestriction', params.ageRestriction);
      }

      // Multiple select filters
      if (params.categories?.length) {
        params.categories.forEach(id => queryParams.append('categories', id));
      }
      if (params.tags?.length) {
        params.tags.forEach(id => queryParams.append('tags', id));
      }
      if (params.formats?.length) {
        params.formats.forEach(id => queryParams.append('formats', id));
      }
      if (params.authors?.length) {
        params.authors.forEach(id => queryParams.append('authors', id));
      }
      if (params.artists?.length) {
        params.artists.forEach(id => queryParams.append('artists', id));
      }
      if (params.publishers?.length) {
        params.publishers.forEach(id => queryParams.append('publishers', id));
      }
      if (params.teams?.length) {
        params.teams.forEach(id => queryParams.append('teams', id));
      }

      // Sorting
      if (params.sortBy) queryParams.append('sortBy', params.sortBy);
      if (params.sortOrder) queryParams.append('sortOrder', params.sortOrder);

      // Release year range
      if (params.yearFrom) queryParams.append('yearFrom', params.yearFrom);
      if (params.yearTo) queryParams.append('yearTo', params.yearTo);

      const response = await api.get(`/Titles/Catalog?${queryParams.toString()}`);

      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching catalog titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load catalog',
        data: {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 24,
          totalPages: 0
        }
      };
    }
  },

  /**
   * Get filter options with counts
   * GET: api/Titles/Catalog/FilterOptions
   */
  async getFilterOptions() {
    try {
      const response = await api.get('/Titles/Catalog/FilterOptions');
      return {
        success: true,
        data: {
          Authors: response.data.authors || [],
          Artists: response.data.artists || [],
          Publishers: response.data.publishers || [],
          Teams: response.data.teams || [],
          Categories: response.data.categories || [],
          Tags: response.data.tags || [],
          Formats: response.data.formats || []
        }
      };
    } catch (error) {
      console.error('Error loading filter options:', error);

      // Fallback to TitleApi form-data endpoint if new endpoint doesn't exist yet
      try {
        const fallbackResponse = await api.get('/TitleApi/form-data');
        return {
          success: true,
          data: fallbackResponse.data
        };
      } catch (fallbackError) {
        return {
          success: false,
          error: error.response?.data?.message || 'Failed to load filter options',
          data: {
            Authors: [],
            Artists: [],
            Publishers: [],
            Teams: [],
            Categories: [],
            Tags: [],
            Formats: []
          }
        };
      }
    }
  },

  /**
   * Get featured titles
   * GET: api/Titles/Featured
   */
  async getFeaturedTitles() {
    try {
      const response = await api.get('/Titles/Featured');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading featured titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load featured titles',
        data: []
      };
    }
  },

  /**
   * Get popular titles
   * GET: api/Titles/Popular
   */
  async getPopularTitles() {
    try {
      const response = await api.get('/Titles/Popular');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading popular titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load popular titles',
        data: []
      };
    }
  },

  /**
   * Get recently updated titles
   * GET: api/Titles/RecentUpdates
   */
  async getRecentUpdates() {
    try {
      const response = await api.get('/Titles/RecentUpdates');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading recent updates:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load recent updates',
        data: []
      };
    }
  },

  /**
   * Get trending titles
   * GET: api/Titles/Trending
   */
  async getTrendingTitles() {
    try {
      const response = await api.get('/Titles/Trending');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading trending titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load trending titles',
        data: []
      };
    }
  },

  /**
   * Helper: Get type display name
   */
  getTypeDisplayName(type) {
    const types = {
      1: 'Manga',
      2: 'Manhwa',
      3: 'Manhua',
      4: 'Western Comic',
      5: 'Russian Comic',
      6: 'Indonesian Comic'
    };
    return types[type] || 'Unknown';
  },

  /**
   * Helper: Get status display name
   */
  getStatusDisplayName(status) {
    const statuses = {
      'inproces': 'Ongoing',
      'completed': 'Completed',
      'frozen': 'On Hiatus',
      'abandoned': 'Dropped'
    };
    return statuses[status] || status;
  },

  /**
   * Helper: Get age restriction display
   */
  getAgeRestrictionDisplay(ageRestriction) {
    const ratings = {
      0: 'All Ages',
      12: '12+',
      16: '16+',
      18: '18+'
    };
    return ratings[ageRestriction] || `${ageRestriction}+`;
  },

  /**
   * Helper: Build image URL
   */
  getImageUrl(path) {
    if (!path) return '/img/no-cover.png';
    if (path.startsWith('http')) return path;
    const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace('/api', '');
    return `${baseUrl}${path}`;
  },

  /**
   * Helper: Format view count for display
   */
  formatViewCount(count) {
    if (!count) return '0';
    if (count >= 1000000) return `${(count / 1000000).toFixed(1)}M`;
    if (count >= 1000) return `${(count / 1000).toFixed(1)}K`;
    return count.toString();
  },

  /**
   * Helper: Format relative time
   */
  formatRelativeTime(date) {
    if (!date) return '';

    const now = new Date();
    const then = new Date(date);
    const diffInSeconds = Math.floor((now - then) / 1000);

    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)}d ago`;
    if (diffInSeconds < 31536000) return `${Math.floor(diffInSeconds / 2592000)}mo ago`;
    return `${Math.floor(diffInSeconds / 31536000)}y ago`;
  }
};

export default catalogService;
