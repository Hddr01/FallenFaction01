import apiClient from './apiClient.js'

function getErrorMessage(error) {
  if (error.response?.status === 404) return 'Title not found'
  if (error.response?.status === 403) return 'Access denied'
  if (error.response?.status === 500) return 'Server error occurred'
  if (error.code === 'ECONNREFUSED') return 'Backend server is not running'
  if (error.code === 'ERR_NETWORK') return 'Network error - check if backend is running'
  return error.response?.data?.message || error.message || 'An error occurred'
}

export const titleDetailsService = {
  buildTitleSlug(originalTitle, id) {
    const slug = (originalTitle || '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
    return `${slug || 'title'}-${id}`
  },

  parseTitleSlug(slug) {
    const lastDash = (slug || '').lastIndexOf('-')
    if (lastDash > 0) {
      const id = parseInt(slug.slice(lastDash + 1), 10)
      if (!isNaN(id) && id > 0) return id
    }
    return null
  },

  async getTitleDetails(titleSlugOrName) {
    try {
      const id = this.parseTitleSlug(titleSlugOrName)
      if (id !== null) {
        const response = await apiClient.get(`/Titles/BySlug/${encodeURIComponent(titleSlugOrName)}`)
        if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
          throw new Error('Received HTML instead of JSON')
        }
        return { success: true, data: response.data, error: null }
      }
      const response = await apiClient.get(`/Titles/Details/${encodeURIComponent(titleSlugOrName)}`)
      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON')
      }
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching title details:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async checkSimilarity(originalTitle, englishTitle = '', alternativeNames = '') {
    try {
      const response = await apiClient.get('/Titles/CheckSimilarity', {
        params: { originalTitle: originalTitle || '', englishTitle: englishTitle || '', alternativeNames: alternativeNames || '' }
      })
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error checking similarity:', error)
      return { success: false, data: { matches: [] }, error: getErrorMessage(error) }
    }
  },

  async getReadingProgress(titleId) {
    try {
      const response = await apiClient.get(`/Bookmarks/GetReadingProgress?titleId=${titleId}`)
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching reading progress:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async getChapters(titleId) {
    try {
      const response = await apiClient.get(`/Titles/${titleId}/chapters`)
      return { success: true, data: Array.isArray(response.data) ? response.data : [], error: null }
    } catch (error) {
      console.error('Error fetching chapters:', error)
      return { success: false, data: [], error: getErrorMessage(error) }
    }
  },

  async getComments(titleId, targetType = 1, options = {}) {
    try {
      const { page = 1, pageSize = 20, sortBy = 'newest' } = options
      const response = await apiClient.get('/Comments/GetComments', {
        params: { targetId: titleId, targetType, page, pageSize, sortBy }
      })
      const totalCount = parseInt(response.headers['x-total-count'] || '0')
      const currentPage = parseInt(response.headers['x-page'] || '1')
      const currentPageSize = parseInt(response.headers['x-page-size'] || '20')
      return {
        success: true,
        data: {
          comments: Array.isArray(response.data) ? response.data : [],
          pagination: {
            totalCount, page: currentPage, pageSize: currentPageSize,
            totalPages: Math.ceil(totalCount / currentPageSize),
            hasNext: currentPage * currentPageSize < totalCount,
            hasPrevious: currentPage > 1
          }
        },
        error: null
      }
    } catch (error) {
      console.error('Error fetching comments:', error)
      return {
        success: false,
        data: { comments: [], pagination: { totalCount: 0, page: 1, pageSize: 20, totalPages: 0, hasNext: false, hasPrevious: false } },
        error: getErrorMessage(error)
      }
    }
  },

  async getCommentStats(titleId, targetType = 1) {
    try {
      const response = await apiClient.get('/Comments/GetCommentStats', { params: { targetId: titleId, targetType } })
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching comment stats:', error)
      return { success: false, data: { totalComments: 0, topLevelComments: 0, replies: 0, lastCommentDate: null, commentsEnabled: true }, error: getErrorMessage(error) }
    }
  },

  async rateTitle(titleId, rating) {
    try {
      const existingRating = await this.getUserRating(titleId)
      let response
      if (existingRating.success && existingRating.data.hasRated) {
        response = await apiClient.put(`/Ratings/UpdateRating/${existingRating.data.ratingId}`, {
          ratingId: existingRating.data.ratingId, value: parseInt(rating)
        })
      } else {
        response = await apiClient.post('/Ratings/AddRating', { titleId: parseInt(titleId), value: parseInt(rating) })
      }
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error submitting rating:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async getUserRating(titleId) {
    try {
      const response = await apiClient.get(`/Ratings/GetUserRating?titleId=${titleId}`)
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching user rating:', error)
      return { success: false, data: { ratingId: null, value: null, hasRated: false, ratedAt: null }, error: getErrorMessage(error) }
    }
  },

  async getRatingStats(titleId) {
    try {
      const response = await apiClient.get(`/Ratings/GetRatingStats?titleId=${titleId}`)
      return { success: true, data: { average: response.data.average || 0, total: response.data.total || 0, distribution: response.data.distribution || [] }, error: null }
    } catch (error) {
      console.error('Error fetching rating stats:', error)
      return { success: false, data: { average: 0, total: 0, distribution: [] }, error: getErrorMessage(error) }
    }
  },

  async getRatingSummary(titleId) {
    try {
      const response = await apiClient.get(`/Ratings/GetRatingSummary?titleId=${titleId}`)
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching rating summary:', error)
      return { success: false, data: { titleId, titleName: 'Unknown', averageRating: 0, totalRatings: 0, userRating: null, distribution: [] }, error: getErrorMessage(error) }
    }
  },

  async deleteRating(ratingId) {
    try {
      const response = await apiClient.delete(`/Ratings/DeleteRating/${ratingId}`)
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error deleting rating:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async getTitleChangeLog(titleId) {
    try {
      const response = await apiClient.get(`/AdminTitle/TitleChangeLog/${titleId}`)
      return { success: true, data: Array.isArray(response.data) ? response.data : [], error: null }
    } catch (error) {
      console.error('Error fetching title change log:', error)
      return { success: false, data: [], error: getErrorMessage(error) }
    }
  },

  async getTitleChangeStats(titleId) {
    try {
      const response = await apiClient.get(`/AdminTitle/TitleChangeStats/${titleId}`)
      return { success: true, data: response.data || { TotalChanges: 0, ChangesByStatus: [], LastUpdate: null }, error: null }
    } catch (error) {
      console.error('Error fetching title change stats:', error)
      return { success: false, data: { TotalChanges: 0, ChangesByStatus: [], LastUpdate: null }, error: getErrorMessage(error) }
    }
  },

  async getRatings(titleId, page = 1, pageSize = 20, sortBy = 'newest') {
    try {
      const response = await apiClient.get(`/Ratings/GetRatings?titleId=${titleId}&page=${page}&pageSize=${pageSize}&sortBy=${sortBy}`)
      return {
        success: true,
        data: {
          ratings: response.data || [],
          totalCount: parseInt(response.headers['x-total-count'] || '0'),
          page: parseInt(response.headers['x-page'] || '1'),
          pageSize: parseInt(response.headers['x-page-size'] || '20')
        },
        error: null
      }
    } catch (error) {
      console.error('Error fetching ratings:', error)
      return { success: false, data: { ratings: [], totalCount: 0, page: 1, pageSize: 20 }, error: getErrorMessage(error) }
    }
  },

  async checkBookmark(titleId) {
    try {
      const response = await apiClient.get(`/Bookmarks/CheckBookmark?titleId=${titleId}`)
      return { success: true, data: { isBookmarked: response.data?.isBookmarked || false, bookmarkId: response.data?.bookmarkId || null }, error: null }
    } catch (error) {
      console.error('Error checking bookmark:', error)
      return { success: false, data: { isBookmarked: false, bookmarkId: null }, error: getErrorMessage(error) }
    }
  },

  async addBookmark(titleId) {
    try {
      const response = await apiClient.post('/Bookmarks/AddBookmark', { titleId: parseInt(titleId) })
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error adding bookmark:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async removeBookmark(titleId) {
    try {
      const response = await apiClient.delete(`/Bookmarks/RemoveBookmark?titleId=${titleId}`)
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error removing bookmark:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async getUserBookmark(titleId) {
    try {
      const response = await apiClient.get(`/Bookmarks/GetUserBookmark?titleId=${titleId}`)
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching user bookmark:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async updateBookmarkStatus(titleId, status) {
    try {
      const response = await apiClient.put('/Bookmarks/UpdateStatus', { titleId: parseInt(titleId), status })
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error updating bookmark status:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async getBookmarkStats(titleId) {
    try {
      const response = await apiClient.get(`/Bookmarks/GetBookmarkStats?titleId=${titleId}`)
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching bookmark stats:', error)
      return { success: false, data: { totalBookmarks: 0, folderDistribution: [] }, error: getErrorMessage(error) }
    }
  },

  async getChapterByRoute(titleName, chapterName, volume, teamId, page = null, cid = null) {
    try {
      let url = `/Titles/${encodeURIComponent(titleName)}/chapter/${encodeURIComponent(chapterName)}/v${volume}/t${teamId}`
      const params = []
      if (page) params.push(`page=${page}`)
      if (cid) params.push(`cid=${cid}`)
      if (params.length) url += `?${params.join('&')}`
      const response = await apiClient.get(url)
      if (typeof response.data === 'string' && response.data.includes('<!DOCTYPE html>')) {
        throw new Error('Received HTML instead of JSON - API endpoint not found')
      }
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error fetching chapter by route:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async getChaptersList(titleId) {
    try {
      const response = await apiClient.get(`/Titles/${titleId}/chapters/list`)
      return { success: true, data: Array.isArray(response.data) ? response.data : [], error: null }
    } catch (error) {
      console.error('Error fetching chapters list:', error)
      return { success: false, data: [], error: getErrorMessage(error) }
    }
  },

  async updateReadingProgress(titleId, chapterNumber) {
    try {
      const response = await apiClient.post('/Titles/updateProgress', { titleId: parseInt(titleId), chapterNumber: parseInt(chapterNumber) })
      return { success: true, data: response.data, error: null }
    } catch (error) {
      console.error('Error updating reading progress:', error)
      return { success: false, data: null, error: getErrorMessage(error) }
    }
  },

  async testConnection() {
    try {
      const response = await apiClient.get('/Titles/Debug')
      return { success: true, data: response.data }
    } catch (error) {
      console.error('Title API connection test failed:', error)
      return { success: false, error: getErrorMessage(error) }
    }
  },

  // Aliases for compatibility
  async submitRating(titleId, rating) { return this.rateTitle(titleId, rating) },
  async getChangeHistory(titleId) { return this.getTitleChangeLog(titleId) },

  getImageUrl(imagePath) {
    if (!imagePath) return '/img/default-cover.png'
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) return imagePath
    return imagePath.startsWith('/') ? imagePath : `/${imagePath}`
  },

  getImageBaseUrl() {
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'
    return apiBaseUrl.replace('/api', '')
  },
}

export const testTitleApiConnection = () => titleDetailsService.testConnection()
