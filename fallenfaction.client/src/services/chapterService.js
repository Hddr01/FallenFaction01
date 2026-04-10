// services/chapterService.js - Enhanced chapter service for reading and management
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Accept': 'application/json',
  },
  withCredentials: true,
  timeout: 30000, // Longer timeout for file uploads
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token expiration
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
      if (!window.location.pathname.includes('/account/login')) {
        window.location.href = '/account/login';
      }
    }
    return Promise.reject(error);
  }
);

export const chapterService = {
  /**
   * Get chapter by route parameters (for reading)
   * @param {string} titleName - The title name (URL encoded)
   * @param {string} chapterName - The chapter name
   * @param {number} volumeNumber - The volume number
   * @param {number} teamId - The team ID
   * @param {number} page - Optional page number
   * @returns {Object} - Chapter details for reading
   */
  async getChapterByRoute(titleName, chapterName, volumeNumber, teamId, page = null, cid = null) {
    try {
      const encodedTitleName = encodeURIComponent(titleName);
      const encodedChapterName = encodeURIComponent(chapterName);

      let url = `/Titles/${encodedTitleName}/chapter/${encodedChapterName}/v${volumeNumber}/t${teamId}`;

      const params = []
      if (page) params.push(`page=${page}`)
      if (cid) params.push(`cid=${cid}`)
      if (params.length) url += `?${params.join('&')}`

      const response = await api.get(url);

      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading chapter by route:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapter',
        data: null
      };
    }
  },

  /**
   * Get chapter creation form data for a specific title
   * This checks if user has permission to add chapters to this title
   * @param {number|string} titleId - The title ID
   * @returns {Object} - Form data including user teams and suggested chapter numbers
   */
  async getChapterFormData(titleId) {
    try {
      const response = await api.get(`/Titles/${titleId}/chapters/create`);

      return {
        success: true,
        data: {
          titleId: response.data.titleId,
          titleName: response.data.titleName,
          userTeams: response.data.userTeams || [], // Teams user belongs to for this title
          suggestedVolumeNumber: response.data.suggestedVolumeNumber || 1,
          suggestedChapterNumber: response.data.suggestedChapterNumber || 1,
          hasPermission: response.data.userTeams && response.data.userTeams.length > 0
        }
      };
    } catch (error) {
      console.error('Error loading chapter form data:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapter form data',
        data: null
      };
    }
  },

  /**
   * Create a new chapter (submitted for review)
   * @param {number} titleId - The title ID
   * @param {Object} chapterData - Chapter information
   * @returns {Object} - Result of chapter creation
   */
  async createChapter(titleId, chapterData) {
    try {
      if (!chapterData.content || !chapterData.content.trim()) {
        throw new Error('Chapter content cannot be empty');
      }

      const response = await api.post(`/Titles/${titleId}/chapters`, {
        volumeNumber: chapterData.volumeNumber,
        chapterNumber: chapterData.chapterNumber,
        name: chapterData.name || '',
        teamId: chapterData.teamId,
        content: chapterData.content
      });

      return {
        success: true,
        message: 'Chapter submitted for review successfully!',
        data: {
          id: response.data.id,
          name: response.data.name,
          volumeNumber: response.data.volumeNumber,
          chapterNumber: response.data.chapterNumber,
          titleName: response.data.titleName,
          teamName: response.data.teamName,
          createdDate: response.data.createdDate,
          wordCount: response.data.wordCount
        }
      };
    } catch (error) {
      console.error('Error creating chapter:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to create chapter'
      };
    }
  },

  /**
   * Get chapters for a title (public access)
   * @param {number} titleId - The title ID
   * @returns {Object} - Chapters data
   */
  async getChapters(titleId) {
    try {
      const response = await api.get(`/Titles/${titleId}/chapters`);

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : []
      };
    } catch (error) {
      console.error('Error fetching chapters:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapters',
        data: []
      };
    }
  },

  /**
   * Get chapters list for navigation (simplified for popups)
   * @param {number} titleId - The title ID
   * @returns {Object} - Simplified chapters list
   */
  async getChaptersList(titleId) {
    try {
      const response = await api.get(`/Titles/${titleId}/chapters/list`);

      return {
        success: true,
        data: Array.isArray(response.data) ? response.data : []
      };
    } catch (error) {
      console.error('Error fetching chapters list:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapters list',
        data: []
      };
    }
  },

  /**
   * Get chapter details for reading
   * @param {number} titleId - The title ID
   * @param {number} chapterNumber - The chapter number
   * @param {number} volumeNumber - The volume number (optional)
   * @param {number} teamId - The team ID (optional)
   * @returns {Object} - Chapter details
   */
  async getChapterForReading(titleId, chapterNumber, volumeNumber = null, teamId = null) {
    try {
      let url = `/Titles/${titleId}/chapters/${chapterNumber}`;
      const params = new URLSearchParams();

      if (volumeNumber) params.append('volumeNumber', volumeNumber);
      if (teamId) params.append('teamId', teamId);

      if (params.toString()) {
        url += `?${params.toString()}`;
      }

      const response = await api.get(url);

      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading chapter for reading:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapter',
        data: null
      };
    }
  },

  /**
   * Update chapter reading progress (for bookmarks)
   * @param {number} titleId - The title ID
   * @param {number} chapterNumber - The chapter number just read
   * @returns {Object} - Update result
   */
  async updateReadingProgress(titleId, chapterNumber) {
    try {
      const response = await api.post('/Titles/updateProgress', {
        titleId: parseInt(titleId),
        chapterNumber: parseInt(chapterNumber)
      });

      return {
        success: true,
        data: response.data,
        message: 'Reading progress updated successfully'
      };
    } catch (error) {
      console.error('Error updating reading progress:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update progress'
      };
    }
  },

  /**
   * Get user's submitted chapters (pending review)
   * @returns {Object} - User's pending chapters
   */
  async getUserPendingChapters() {
    try {
      const response = await api.get('/Chapters/user/pending');

      return {
        success: true,
        data: response.data || []
      };
    } catch (error) {
      console.error('Error loading user pending chapters:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load pending chapters',
        data: []
      };
    }
  },

  /**
   * Get user's published chapters
   * @returns {Object} - User's published chapters
   */
  async getUserPublishedChapters() {
    try {
      const response = await api.get('/Chapters/user/published');

      return {
        success: true,
        data: response.data || []
      };
    } catch (error) {
      console.error('Error loading user published chapters:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load published chapters',
        data: []
      };
    }
  },

  /**
   * Check if user can add chapters to a specific title
   * @param {number} titleId - The title ID
   * @returns {Object} - Permission check result
   */
  async checkChapterPermission(titleId) {
    try {
      const response = await api.get(`/Titles/${titleId}/chapters/permission`);

      return {
        success: true,
        data: {
          canAddChapters: response.data.canAddChapters || false,
          userTeams: response.data.userTeams || [],
          titleName: response.data.titleName,
          reason: response.data.reason || ''
        }
      };
    } catch (error) {
      console.error('Error checking chapter permission:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Permission check failed',
        data: {
          canAddChapters: false,
          userTeams: [],
          titleName: '',
          reason: 'Permission check failed'
        }
      };
    }
  },

  /**
   * Get titles that user can add chapters to
   * @returns {Object} - Titles with chapter permission
   */
  async getTitlesWithChapterPermission() {
    try {
      const response = await api.get('/Titles/user/chapter-permission');

      return {
        success: true,
        data: response.data || []
      };
    } catch (error) {
      console.error('Error loading titles with chapter permission:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load permitted titles',
        data: []
      };
    }
  },

  /**
   * Delete a pending chapter (only if user created it)
   * @param {number} chapterId - The pending chapter ID
   * @returns {Object} - Deletion result
   */
  async deletePendingChapter(chapterId) {
    try {
      await api.delete(`/Chapters/pending/${chapterId}`);

      return {
        success: true,
        message: 'Chapter deleted successfully'
      };
    } catch (error) {
      console.error('Error deleting pending chapter:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to delete chapter'
      };
    }
  },

  /**
   * Get chapter navigation info (previous/next chapters)
   * @param {number} titleId - The title ID
   * @param {number} currentChapterNumber - Current chapter number
   * @param {number} volumeNumber - Current volume number
   * @returns {Object} - Navigation info
   */
  async getChapterNavigation(titleId, currentChapterNumber, volumeNumber) {
    try {
      const response = await api.get(`/Titles/${titleId}/chapters/navigation`, {
        params: {
          chapterNumber: currentChapterNumber,
          volumeNumber: volumeNumber
        }
      });

      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error getting chapter navigation:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load navigation',
        data: {
          previousChapter: null,
          nextChapter: null
        }
      };
    }
  },

  /**
   * Report a chapter for inappropriate content
   * @param {number} chapterId - The chapter ID
   * @param {string} reason - Report reason
   * @param {string} details - Additional details
   * @returns {Object} - Report result
   */
  async reportChapter(chapterId, reason, details = '') {
    try {
      const response = await api.post(`/Chapters/${chapterId}/report`, {
        reason: reason,
        details: details
      });

      return {
        success: true,
        message: response.data.message || 'Chapter reported successfully'
      };
    } catch (error) {
      console.error('Error reporting chapter:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to report chapter'
      };
    }
  },

  /**
   * Get chapter reading statistics
   * @param {number} chapterId - The chapter ID
   * @returns {Object} - Chapter statistics
   */
  async getChapterStats(chapterId) {
    try {
      const response = await api.get(`/Chapters/${chapterId}/stats`);

      return {
        success: true,
        data: {
          viewCount: response.data.viewCount || 0,
          uniqueReaders: response.data.uniqueReaders || 0,
          averageReadingTime: response.data.averageReadingTime || 0,
          completionRate: response.data.completionRate || 0
        }
      };
    } catch (error) {
      console.error('Error getting chapter stats:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapter stats',
        data: {
          viewCount: 0,
          uniqueReaders: 0,
          averageReadingTime: 0,
          completionRate: 0
        }
      };
    }
  },

  /**
   * Mark chapter as viewed (for analytics)
   * @param {number} chapterId - The chapter ID
   * @param {number} pageCount - Total pages in chapter
   * @param {number} timeSpent - Time spent reading (seconds)
   * @returns {Object} - View tracking result
   */
  async trackChapterView(chapterId, pageCount = 0, timeSpent = 0) {
    try {
      const response = await api.post(`/Chapters/${chapterId}/view`, {
        pageCount: pageCount,
        timeSpent: timeSpent,
        timestamp: new Date().toISOString()
      });

      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      // Don't log this error as it's not critical
      return {
        success: false,
        error: 'Failed to track view'
      };
    }
  },

  /**
   * Get user's reading history
   * @param {number} limit - Number of recent chapters to get
   * @returns {Object} - Reading history
   */
  async getReadingHistory(limit = 20) {
    try {
      const response = await api.get('/User/reading-history', {
        params: { limit }
      });

      return {
        success: true,
        data: response.data || []
      };
    } catch (error) {
      console.error('Error getting reading history:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load reading history',
        data: []
      };
    }
  },

  /**
   * Get all published chapters for a title with edit permission info.
   * @param {number} titleId
   */
  async getChaptersForManagement(titleId) {
    try {
      const response = await api.get(`/Titles/${titleId}/chapters/manage`)
      return { success: true, data: response.data }
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapters for management',
        data: null
      }
    }
  },

  /**
   * Get a single published chapter's full content for editing.
   * @param {number} titleId
   * @param {number} chapterId
   */
  async getChapterForEdit(titleId, chapterId) {
    try {
      const response = await api.get(`/Titles/${titleId}/chapters/${chapterId}/edit`)
      return { success: true, data: response.data }
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load chapter for editing',
        data: null
      }
    }
  },

  /**
   * Submit an edit for a published chapter.
   * @param {number} titleId
   * @param {number} chapterId
   * @param {{ name: string, volumeNumber: number, chapterNumber: number, teamId: number, content: string }} chapterData
   */
  async updateChapter(titleId, chapterId, chapterData) {
    try {
      const response = await api.put(`/Titles/${titleId}/chapters/${chapterId}`, {
        name: chapterData.name,
        volumeNumber: chapterData.volumeNumber,
        chapterNumber: chapterData.chapterNumber,
        teamId: chapterData.teamId,
        content: chapterData.content
      })
      return {
        success: true,
        message: response.data.message,
        autoApproved: response.data.autoApproved,
        data: response.data
      }
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to update chapter'
      }
    }
  },

  /**
   * Permanently delete a published chapter.
   * Requires CanDeleteChapter permission in the chapter's team (or admin / title creator).
   * @param {number} titleId
   * @param {number} chapterId
   */
  async deleteChapter(titleId, chapterId) {
    try {
      const response = await api.delete(`/Titles/${titleId}/chapters/${chapterId}`)
      return {
        success: true,
        message: response.data.message || 'Chapter deleted successfully.'
      }
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to delete chapter'
      }
    }
  },

  /**
   * Test API connectivity
   * @returns {boolean} - Connection status
   */
  async testConnection() {
    try {
      await api.get('/Titles/Debug');
      console.log('Chapter service connection successful');
      return true;
    } catch (error) {
      console.error('Chapter service connection failed:', error);
      return false;
    }
  },

};
