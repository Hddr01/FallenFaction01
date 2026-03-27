// services/titleDetailsService.js
import axios from 'axios';

class TitleDetailsService {
  constructor() {
    // Configure the base URL to use environment variable
    const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api';

    this.apiClient = axios.create({
      baseURL: baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      }
    });

    // Add request interceptor for debugging
    this.apiClient.interceptors.request.use(
      (config) => {
        console.log(`Title API Request: ${config.method?.toUpperCase()} ${config.baseURL}${config.url}`);

        // Add auth token if available
        const token = localStorage.getItem('authToken');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        return config;
      },
      (error) => {
        console.error('Title API Request Error:', error);
        return Promise.reject(error);
      }
    );

    // Add response interceptor for error handling
    this.apiClient.interceptors.response.use(
      (response) => {
        console.log(`Title API Response: ${response.status} ${response.config.url}`, response.data);
        return response;
      },
      (error) => {
        console.error('Title API Response Error:', error);
        console.error('Error details:', {
          message: error.message,
          status: error.response?.status,
          data: error.response?.data,
          url: error.config?.url
        });

        // Handle authentication errors
        if (error.response?.status === 401) {
          localStorage.removeItem('authToken');
          localStorage.removeItem('authUser');

          if (!window.location.pathname.includes('/account/login')) {
            window.location.href = '/account/login';
          }
        }

        // Create a standardized error response
        const errorResponse = {
          success: false,
          error: this.getErrorMessage(error),
          status: error.response?.status || 500,
          data: null
        };

        return Promise.resolve({ data: errorResponse });
      }
    );
  }

  getErrorMessage(error) {
    if (error.response?.status === 404) {
      return 'Title not found';
    }
    if (error.response?.status === 403) {
      return 'Access denied';
    }
    if (error.response?.status === 500) {
      return 'Server error occurred';
    }
    if (error.code === 'ECONNREFUSED') {
      return 'Backend server is not running';
    }
    if (error.code === 'ERR_NETWORK') {
      return 'Network error - check if backend is running';
    }
    return error.response?.data?.message || error.message || 'An error occurred';
  }

  // ── Slug helpers ─────────────────────────────────────────────────────────
  // Build a URL-safe slug: "My Hero Academia" + 42 → "my-hero-academia-42"
  buildTitleSlug(originalTitle, id) {
    const slug = (originalTitle || '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
    return `${slug || 'title'}-${id}`
  }

  // Parse slug back to id: "my-hero-academia-42" → 42
  parseTitleSlug(slug) {
    const lastDash = (slug || '').lastIndexOf('-')
    if (lastDash > 0) {
      const suffix = slug.slice(lastDash + 1)
      const id = parseInt(suffix, 10)
      if (!isNaN(id) && id > 0) return id
    }
    return null
  }

  // ── Get title details — accepts EITHER a slug ("naruto-42") or a plain name ──
  async getTitleDetails(titleSlugOrName) {
    try {
      console.log('Fetching title details for:', titleSlugOrName)

      // Try slug format first (ends with -<number>)
      const id = this.parseTitleSlug(titleSlugOrName)
      if (id !== null) {
        const response = await this.apiClient.get(
          `/Titles/BySlug/${encodeURIComponent(titleSlugOrName)}`
        )
        if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
          throw new Error('Received HTML instead of JSON')
        }
        return { success: true, data: response.data, error: null }
      }

      // Fallback — legacy plain-name endpoint (backward compat)
      const encodedTitle = encodeURIComponent(titleSlugOrName)
      const response = await this.apiClient.get(`/Titles/Details/${encodedTitle}`)
      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON')
      }
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching title details:', error)
      return { success: false, data: null, error: this.getErrorMessage(error) }
    }
  }

  // ── Check for duplicate / similar titles (admin use) ─────────────────────
  async checkSimilarity(originalTitle, englishTitle = '', alternativeNames = '') {
    try {
      const response = await this.apiClient.get('/Titles/CheckSimilarity', {
        params: {
          originalTitle: originalTitle || '',
          englishTitle: englishTitle || '',
          alternativeNames: alternativeNames || '',
        }
      })
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error checking similarity:', error)
      return { success: false, data: { matches: [] }, error: this.getErrorMessage(error) }
    }
  }

  async getReadingProgress(titleId) {
    try {
      console.log('Fetching reading progress for title ID:', titleId);
      const response = await this.apiClient.get(`/Bookmarks/GetReadingProgress?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching reading progress:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // FIXED: Get chapters for a title using correct endpoint
  async getChapters(titleId) {
    try {
      console.log('Fetching chapters for title ID:', titleId);
      // Fixed endpoint: Use the correct route from TitlesController
      const response = await this.apiClient.get(`/Titles/${titleId}/chapters`);

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching chapters:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get comments for a title - Updated to use new comments service
  async getComments(titleId, targetType = 1, options = {}) {
    try {
      console.log('Fetching comments for title ID:', titleId);

      const {
        page = 1,
        pageSize = 20,
        sortBy = 'newest'
      } = options;

      const response = await this.apiClient.get('/Comments/GetComments', {
        params: {
          targetId: titleId,
          targetType: targetType,
          page,
          pageSize,
          sortBy
        }
      });

      const totalCount = parseInt(response.headers['x-total-count'] || '0');
      const currentPage = parseInt(response.headers['x-page'] || '1');
      const currentPageSize = parseInt(response.headers['x-page-size'] || '20');

      return {
        success: true,
        data: {
          comments: Array.isArray(response.data) ? response.data : [],
          pagination: {
            totalCount,
            page: currentPage,
            pageSize: currentPageSize,
            totalPages: Math.ceil(totalCount / currentPageSize),
            hasNext: currentPage * currentPageSize < totalCount,
            hasPrevious: currentPage > 1
          }
        },
        error: null
      };
    } catch (error) {
      console.error('Error fetching comments:', error);
      return {
        success: false,
        data: {
          comments: [],
          pagination: {
            totalCount: 0,
            page: 1,
            pageSize: 20,
            totalPages: 0,
            hasNext: false,
            hasPrevious: false
          }
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get comment statistics for a title
  async getCommentStats(titleId, targetType = 1) {
    try {
      console.log('Fetching comment stats for title ID:', titleId);
      const response = await this.apiClient.get('/Comments/GetCommentStats', {
        params: {
          targetId: titleId,
          targetType: targetType
        }
      });

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching comment stats:', error);
      return {
        success: false,
        data: {
          totalComments: 0,
          topLevelComments: 0,
          replies: 0,
          lastCommentDate: null,
          commentsEnabled: true
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // =============================================================================
  // RATING METHODS - Updated with comprehensive rating functionality
  // =============================================================================

  // Submit a rating for a title (handles both add and update)
  async rateTitle(titleId, rating) {
    try {
      console.log('Submitting rating for title ID:', titleId, 'Rating:', rating);

      // First check if user already has a rating for this title
      const existingRating = await this.getUserRating(titleId);

      let response;
      if (existingRating.success && existingRating.data.hasRated) {
        // Update existing rating
        response = await this.apiClient.put(`/Ratings/UpdateRating/${existingRating.data.ratingId}`, {
          ratingId: existingRating.data.ratingId,
          value: parseInt(rating)
        });
      } else {
        // Add new rating
        response = await this.apiClient.post('/Ratings/AddRating', {
          titleId: parseInt(titleId),
          value: parseInt(rating)
        });
      }

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error submitting rating:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get current user's rating for a title
  async getUserRating(titleId) {
    try {
      console.log('Fetching user rating for title ID:', titleId);
      const response = await this.apiClient.get(`/Ratings/GetUserRating?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching user rating:', error);
      return {
        success: false,
        data: {
          ratingId: null,
          value: null,
          hasRated: false,
          ratedAt: null
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get rating statistics for a title - FIXED METHOD
  async getRatingStats(titleId) {
    try {
      console.log('Fetching rating stats for title ID:', titleId);
      const response = await this.apiClient.get(`/Ratings/GetRatingStats?titleId=${titleId}`);

      return {
        success: true,
        data: {
          average: response.data.average || 0,
          total: response.data.total || 0,
          distribution: response.data.distribution || []
        },
        error: null
      };
    } catch (error) {
      console.error('Error fetching rating stats:', error);
      return {
        success: false,
        data: {
          average: 0,
          total: 0,
          distribution: []
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get comprehensive rating summary (includes user rating if authenticated)
  async getRatingSummary(titleId) {
    try {
      console.log('Fetching rating summary for title ID:', titleId);
      const response = await this.apiClient.get(`/Ratings/GetRatingSummary?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching rating summary:', error);
      return {
        success: false,
        data: {
          titleId: titleId,
          titleName: 'Unknown',
          averageRating: 0,
          totalRatings: 0,
          userRating: null,
          distribution: []
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Delete a rating
  async deleteRating(ratingId) {
    try {
      console.log('Deleting rating ID:', ratingId);
      const response = await this.apiClient.delete(`/Ratings/DeleteRating/${ratingId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error deleting rating:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get title change log history
  async getTitleChangeLog(titleId) {
    try {
      console.log('Fetching title change log for ID:', titleId);
      const response = await this.apiClient.get(`/AdminTitle/TitleChangeLog/${titleId}`);

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching title change log:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get title change statistics
  async getTitleChangeStats(titleId) {
    try {
      console.log('Fetching title change stats for ID:', titleId);
      const response = await this.apiClient.get(`/AdminTitle/TitleChangeStats/${titleId}`);

      return {
        success: true,
        data: response.data || {
          TotalChanges: 0,
          ChangesByStatus: [],
          LastUpdate: null
        },
        error: null
      };
    } catch (error) {
      console.error('Error fetching title change stats:', error);
      return {
        success: false,
        data: {
          TotalChanges: 0,
          ChangesByStatus: [],
          LastUpdate: null
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get all ratings for a title with pagination
  async getRatings(titleId, page = 1, pageSize = 20, sortBy = 'newest') {
    try {
      console.log('Fetching ratings for title ID:', titleId);
      const response = await this.apiClient.get(`/Ratings/GetRatings?titleId=${titleId}&page=${page}&pageSize=${pageSize}&sortBy=${sortBy}`);

      return {
        success: true,
        data: {
          ratings: response.data || [],
          totalCount: parseInt(response.headers['x-total-count'] || '0'),
          page: parseInt(response.headers['x-page'] || '1'),
          pageSize: parseInt(response.headers['x-page-size'] || '20')
        },
        error: null
      };
    } catch (error) {
      console.error('Error fetching ratings:', error);
      return {
        success: false,
        data: {
          ratings: [],
          totalCount: 0,
          page: 1,
          pageSize: 20
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // =============================================================================
  // BOOKMARK METHODS
  // =============================================================================

  // Check if user has bookmarked a title
  async checkBookmark(titleId) {
    try {
      console.log('Checking bookmark status for title ID:', titleId);
      const response = await this.apiClient.get(`/Bookmarks/CheckBookmark?titleId=${titleId}`);

      return {
        success: true,
        data: {
          isBookmarked: response.data?.isBookmarked || false,
          bookmarkId: response.data?.bookmarkId || null
        },
        error: null
      };
    } catch (error) {
      console.error('Error checking bookmark:', error);
      return {
        success: false,
        data: {
          isBookmarked: false,
          bookmarkId: null
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Add bookmark for a title
  async addBookmark(titleId) {
    try {
      console.log('Adding bookmark for title ID:', titleId);
      const response = await this.apiClient.post('/Bookmarks/AddBookmark', {
        titleId: parseInt(titleId)
      });

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error adding bookmark:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // Remove bookmark for a title
  async removeBookmark(titleId) {
    try {
      console.log('Removing bookmark for title ID:', titleId);
      const response = await this.apiClient.delete(`/Bookmarks/RemoveBookmark?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error removing bookmark:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // Get user's bookmark for a title (includes reading progress)
  async getUserBookmark(titleId) {
    try {
      console.log('Fetching user bookmark for title ID:', titleId);
      const response = await this.apiClient.get(`/Bookmarks/GetUserBookmark?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching user bookmark:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // Update bookmark status (reading, completed, on-hold, plan-to-read, dropped)
  async updateBookmarkStatus(titleId, status) {
    try {
      console.log('Updating bookmark status for title ID:', titleId, 'Status:', status);
      const response = await this.apiClient.put('/Bookmarks/UpdateStatus', {
        titleId: parseInt(titleId),
        status: status
      });

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error updating bookmark status:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

  // =============================================================================
  // ALIAS METHODS FOR COMPATIBILITY
  // =============================================================================

  // Alias for rateTitle to match component usage
  async submitRating(titleId, rating) {
    return this.rateTitle(titleId, rating);
  }

  // Alias for getTitleChangeLog to match component usage
  async getChangeHistory(titleId) {
    return this.getTitleChangeLog(titleId);
  }

  // =============================================================================
  // OTHER METHODS
  // =============================================================================

  // Get bookmark statistics for a title
  async getBookmarkStats(titleId) {
    try {
      console.log('Fetching bookmark stats for title ID:', titleId);
      const response = await this.apiClient.get(`/Bookmarks/GetBookmarkStats?titleId=${titleId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching bookmark stats:', error);
      return {
        success: false,
        data: {
          totalBookmarks: 0,
          folderDistribution: []
        },
        error: this.getErrorMessage(error)
      };
    }
  }

  // Helper method to get image URLs
  getImageUrl(imagePath) {
    if (!imagePath) return '/img/default-cover.png';

    // Already absolute — return as-is
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }

    // Relative path — return as-is. Both Vite dev proxy and nginx proxy
    // /uploads → backend, so the browser resolves from the current origin.
    // Never hardcode a hostname here — it breaks mobile/Docker access.
    return imagePath.startsWith('/') ? imagePath : `/${imagePath}`;
  }

  // Get the base URL for images
  getImageBaseUrl() {
    // Kept for compatibility but no longer used for image URL construction
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api';
    return apiBaseUrl.replace('/api', '');
  }

  // Test API connectivity
  async testConnection() {
    try {
      console.log('Testing title API connection...');
      const response = await this.apiClient.get('/Titles/Debug');
      console.log('Title API connection test successful:', response.data);
      return { success: true, data: response.data };
    } catch (error) {
      console.error('Title API connection test failed:', error);
      return { success: false, error: this.getErrorMessage(error) };
    }
  }


  async getChapterByRoute(titleName, chapterName, volume, teamId, page = null) {
    try {
      console.log('Fetching chapter by route:', { titleName, chapterName, volume, teamId, page });

      // Encode the title name for URL
      const encodedTitleName = encodeURIComponent(titleName);
      const encodedChapterName = encodeURIComponent(chapterName);

      let url = `/Titles/${encodedTitleName}/chapter/${encodedChapterName}/v${volume}/t${teamId}`;

      if (page) {
        url += `?page=${page}`;
      }

      const response = await this.apiClient.get(url);

      // Check if response is HTML (indicates wrong endpoint)
      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found');
      }

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error fetching chapter by route:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }




  // Get chapters list for navigation (simplified list for popups)
  async getChaptersList(titleId) {
    try {
      console.log('Fetching chapters list for title ID:', titleId);
      const response = await this.apiClient.get(`/Titles/${titleId}/chapters/list`);

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : [],
        error: null
      };
    } catch (error) {
      console.error('Error fetching chapters list:', error);
      return {
        success: false,
        data: [],
        error: this.getErrorMessage(error)
      };
    }
  }

  // Update reading progress for a chapter
  async updateReadingProgress(titleId, chapterNumber) {
    try {
      console.log('Updating reading progress:', { titleId, chapterNumber });

      const response = await this.apiClient.post('/Titles/updateProgress', {
        titleId: parseInt(titleId),
        chapterNumber: parseInt(chapterNumber)
      });

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('Error updating reading progress:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  }

}

export const titleDetailsService = new TitleDetailsService();

// Export test function for debugging
export const testTitleApiConnection = () => titleDetailsService.testConnection();
