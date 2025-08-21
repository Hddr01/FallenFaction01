// services/commentsService.js - Enhanced with DTO mapping utilities and comprehensive functionality
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api',
  headers: {
    'Accept': 'application/json',
    'Content-Type': 'application/json'
  },
  withCredentials: true,
  timeout: 15000
});

// =============================================================================
// DTO MAPPING UTILITIES - Handles different API response formats
// =============================================================================

/**
 * Maps comment DTO from API response to consistent format
 * Handles both PascalCase (C#) and camelCase (JS) property names
 */
const mapCommentFromDto = (dto) => {
  if (!dto) return null;

  return {
    id: dto.id || dto.Id,
    content: dto.content || dto.Content,
    userId: dto.userId || dto.UserId,
    userName: dto.userName || dto.UserName,
    userAvatarUrl: dto.userAvatarUrl || dto.UserAvatarUrl,
    postedDate: dto.postedDate || dto.PostedDate,
    likesCount: dto.likesCount || dto.LikesCount || 0,
    dislikesCount: dto.dislikesCount || dto.DislikesCount || 0,
    currentUserLiked: dto.currentUserLiked || dto.CurrentUserLiked || false,
    currentUserDisliked: dto.currentUserDisliked || dto.CurrentUserDisliked || false,
    isDeleted: dto.isDeleted || dto.IsDeleted || false,
    deletedAt: dto.deletedAt || dto.DeletedAt,
    deletedByUserName: dto.deletedByUserName || dto.DeletedByUserName,
    deletionReason: dto.deletionReason || dto.DeletionReason,
    parentCommentId: dto.parentCommentId || dto.ParentCommentId,
    replies: dto.replies?.map(mapCommentFromDto) || dto.Replies?.map(mapCommentFromDto) || [],
    targetId: dto.targetId || dto.TargetId,
    targetType: dto.targetType || dto.TargetType,
    targetTitle: dto.targetTitle || dto.TargetTitle,
    isReported: dto.isReported || dto.IsReported || false
  };
};

/**
 * Maps comments response with pagination data
 */
const mapCommentsResponse = (response) => {
  const data = response.data;

  return {
    comments: (data.comments || data.Comments || []).map(mapCommentFromDto),
    pagination: {
      totalCount: parseInt(response.headers['x-total-count'] || data.pagination?.totalCount || data.Pagination?.TotalCount || '0'),
      page: parseInt(response.headers['x-page'] || data.pagination?.page || data.Pagination?.Page || '1'),
      pageSize: parseInt(response.headers['x-page-size'] || data.pagination?.pageSize || data.Pagination?.PageSize || '20'),
      totalPages: data.pagination?.totalPages || data.Pagination?.TotalPages || 0,
      hasNext: data.pagination?.hasNext || data.Pagination?.HasNext || false,
      hasPrevious: data.pagination?.hasPrevious || data.Pagination?.HasPrevious || false
    }
  };
};

/**
 * Maps reaction response to consistent format
 */
const mapReactionResponse = (data) => {
  return {
    commentId: data.commentId || data.CommentId,
    likesCount: data.likesCount || data.LikesCount || 0,
    dislikesCount: data.dislikesCount || data.DislikesCount || 0,
    userLiked: data.userLiked || data.UserLiked || false,
    userDisliked: data.userDisliked || data.UserDisliked || false
  };
};

// =============================================================================
// AXIOS INTERCEPTORS - Request/Response handling
// =============================================================================

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Log requests in development
    if (import.meta.env.DEV) {
      console.log(`📤 API Request: ${config.method?.toUpperCase()} ${config.url}`, {
        params: config.params,
        data: config.data,
        headers: config.headers
      });
    }

    return config;
  },
  (error) => {
    console.error('❌ Request interceptor error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor to handle token expiration and errors
api.interceptors.response.use(
  (response) => {
    if (import.meta.env.DEV) {
      console.log(`✅ API Response: ${response.status} ${response.config.url}`, response.data);
    }
    return response;
  },
  (error) => {
    if (import.meta.env.DEV) {
      console.error(`❌ API Error: ${error.response?.status} ${error.config?.url}`, error.response?.data);
    }

    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('authUser');
      if (!window.location.pathname.includes('/account/login')) {
        console.warn('🔐 Session expired, redirecting to login...');
        window.location.href = '/account/login';
      }
    }
    return Promise.reject(error);
  }
);

// =============================================================================
// ENHANCED COMMENTS SERVICE
// =============================================================================

export const commentsService = {
  // =============================================================================
  // ADMIN METHODS - For admin comment management
  // =============================================================================

  /**
   * Test admin comments connection
   * @returns {Object} - Connection test result
   */
  async testAdminCommentsConnection() {
    try {
      console.log('🧪 Testing AdminComments connection...');
      const response = await api.get('/AdminComments/test');
      console.log('✅ AdminComments connection successful:', response.data);
      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('❌ AdminComments connection failed:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  },

  /**
   * Get comment statistics for admin dashboard
   * @returns {Object} - Comment statistics
   */
  async getCommentStatsForAdmin() {
    try {
      console.log('📊 Loading comment statistics for admin...');

      // First test the connection
      const connectionTest = await this.testAdminCommentsConnection();
      if (!connectionTest.success) {
        console.error('🔴 Connection test failed, skipping stats request');
        throw new Error(`Connection test failed: ${connectionTest.error}`);
      }

      const response = await api.get('/AdminComments/GetStats');

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('❌ Error loading comment statistics for admin:', error);

      // Provide more specific error messages
      let errorMessage = 'Failed to load statistics';
      if (error.response?.status === 404) {
        errorMessage = 'AdminComments endpoint not found. Check controller registration.';
      } else if (error.response?.status === 401) {
        errorMessage = 'Authentication required. Please log in as admin.';
      } else if (error.response?.status === 403) {
        errorMessage = 'Access denied. Admin privileges required.';
      } else if (error.message) {
        errorMessage = error.message;
      }

      return {
        success: false,
        data: {
          totalComments: 0,
          todayComments: 0,
          reportedComments: 0,
          activeCommenters: 0
        },
        error: errorMessage
      };
    }
  },

  /**
   * Get all comments for admin management with filtering and pagination
   * @param {Object} options - Filter and pagination options
   * @returns {Object} - Comments data with pagination info
   */
  async getAllCommentsForAdmin(options = {}) {
    try {
      const {
        page = 1,
        pageSize = 20,
        sortBy = 'newest',
        targetType = null,
        showReported = false,
        showDeleted = false,
        search = ''
      } = options;

      console.log('🔍 Loading all comments for admin with options:', options);

      const response = await api.get('/AdminComments/GetAllComments', {
        params: {
          page,
          pageSize,
          sortBy,
          targetType,
          showReported,
          showDeleted,
          search
        }
      });

      // Use DTO mapping for consistent data structure
      const mappedResponse = mapCommentsResponse(response);

      return {
        success: true,
        data: mappedResponse,
        error: null
      };
    } catch (error) {
      console.error('❌ Error loading all comments for admin:', error);

      let errorMessage = 'Failed to load comments';
      if (error.response?.status === 404) {
        errorMessage = 'AdminComments endpoint not found. Check controller registration.';
      } else if (error.response?.status === 401) {
        errorMessage = 'Authentication required. Please log in as admin.';
      } else if (error.response?.status === 403) {
        errorMessage = 'Access denied. Admin privileges required.';
      }

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
        error: errorMessage
      };
    }
  },

  /**
   * Get detailed comment information for admin
   * @param {number} commentId - Comment ID
   * @returns {Object} - Detailed comment data
   */
  async getCommentForAdmin(commentId) {
    try {
      console.log(`🔍 Loading comment ${commentId} for admin`);

      const response = await api.get(`/AdminComments/GetComment/${commentId}`);

      return {
        success: true,
        data: mapCommentFromDto(response.data),
        error: null
      };
    } catch (error) {
      console.error('❌ Error loading comment for admin:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  },

  /**
   * Get comment deletion info (how many replies will be affected)
   * @param {number} commentId - Comment ID
   * @returns {Object} - Deletion info
   */
  async getCommentDeletionInfo(commentId) {
    try {
      console.log(`📋 Getting deletion info for comment ${commentId}`);

      const response = await api.get(`/AdminComments/GetDeletionInfo/${commentId}`);

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('❌ Error getting deletion info:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  },

  /**
   * Soft delete a comment (admin) with optional reason
   * @param {number} commentId - Comment ID to delete
   * @param {string} reason - Optional reason for deletion
   * @returns {Object} - Deletion result
   */
  async deleteCommentAsAdmin(commentId, reason = null) {
    try {
      console.log(`🗑️ Admin soft-deleting comment ${commentId}`);

      const params = reason ? `?reason=${encodeURIComponent(reason)}` : '';
      await api.delete(`/AdminComments/DeleteComment/${commentId}${params}`);

      return {
        success: true,
        message: 'Comment deleted successfully! (Can be restored)',
        error: null
      };
    } catch (error) {
      console.error('❌ Error deleting comment as admin:', error);

      let errorMessage = this.getErrorMessage(error);

      // Handle specific error cases
      if (error.response?.status === 404) {
        errorMessage = 'Comment not found or already deleted';
      } else if (error.response?.status === 403) {
        errorMessage = 'Access denied. Admin privileges required.';
      } else if (error.response?.status === 400 && error.response?.data?.message?.includes('already deleted')) {
        errorMessage = 'Comment is already deleted';
      }

      return {
        success: false,
        message: null,
        error: errorMessage
      };
    }
  },

  /**
   * Restore a soft-deleted comment (admin)
   * @param {number} commentId - Comment ID to restore
   * @returns {Object} - Restoration result
   */
  async restoreCommentAsAdmin(commentId) {
    try {
      console.log(`♻️ Admin restoring comment ${commentId}`);

      await api.post(`/AdminComments/RestoreComment/${commentId}`);

      return {
        success: true,
        message: 'Comment restored successfully!',
        error: null
      };
    } catch (error) {
      console.error('❌ Error restoring comment as admin:', error);

      let errorMessage = this.getErrorMessage(error);

      if (error.response?.status === 404) {
        errorMessage = 'Comment not found';
      } else if (error.response?.status === 400 && error.response?.data?.message?.includes('not deleted')) {
        errorMessage = 'Comment is not deleted';
      }

      return {
        success: false,
        message: null,
        error: errorMessage
      };
    }
  },

  /**
   * Permanently delete a comment (admin only, cannot be undone)
   * @param {number} commentId - Comment ID to permanently delete
   * @returns {Object} - Deletion result
   */
  async permanentlyDeleteComment(commentId) {
    try {
      console.log(`💀 Admin permanently deleting comment ${commentId}`);

      await api.delete(`/AdminComments/PermanentlyDeleteComment/${commentId}?confirmed=true`);

      return {
        success: true,
        message: 'Comment permanently deleted (cannot be undone)',
        error: null
      };
    } catch (error) {
      console.error('❌ Error permanently deleting comment:', error);

      return {
        success: false,
        message: null,
        error: this.getErrorMessage(error)
      };
    }
  },

  /**
   * Bulk delete multiple comments (admin)
   * @param {Array} commentIds - Array of comment IDs to delete
   * @param {string} reason - Optional reason for bulk deletion
   * @returns {Object} - Bulk deletion result
   */
  async bulkDeleteComments(commentIds, reason = null) {
    try {
      console.log(`🗑️ Admin bulk deleting ${commentIds.length} comments`);

      const response = await api.post('/AdminComments/BulkDeleteComments', {
        commentIds,
        reason
      });

      return {
        success: true,
        message: response.data.message || 'Comments deleted successfully',
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('❌ Error bulk deleting comments:', error);

      return {
        success: false,
        message: null,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  },

  // =============================================================================
  // REGULAR USER COMMENT METHODS - Enhanced with DTO mapping
  // =============================================================================

  /**
   * Get comments for a target (title, chapter, or chapter image)
   * @param {number} targetId - ID of the target
   * @param {number} targetType - Type: 1=Title, 2=Chapter, 3=ChapterImage
   * @param {Object} options - Optional parameters
   * @returns {Object} - Comments data with pagination info
   */
  async getComments(targetId, targetType, options = {}) {
    try {
      const { page = 1, pageSize = 20, sortBy = 'newest' } = options;

      console.log(`📖 Loading comments for target ${targetType}:${targetId}`);

      const response = await api.get('/Comments/GetComments', {
        params: { targetId, targetType, page, pageSize, sortBy }
      });

      // Use enhanced DTO mapping
      const mappedResponse = mapCommentsResponse(response);

      return {
        success: true,
        data: mappedResponse,
        error: null
      };
    } catch (error) {
      console.error('❌ Error loading comments:', error);
      return {
        success: false,
        data: {
          comments: [],
          pagination: {
            totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
            hasNext: false, hasPrevious: false
          }
        },
        error: this.getErrorMessage(error)
      };
    }
  },

  /**
   * Add a new comment with enhanced validation
   * @param {string} content - Comment content
   * @param {number} targetId - ID of the target
   * @param {number} targetType - Type: 1=Title, 2=Chapter, 3=ChapterImage
   * @param {number|null} parentCommentId - Parent comment ID for replies
   * @returns {Object} - Created comment data
   */
  async addComment(content, targetId, targetType, parentCommentId = null) {
    try {
      // Enhanced validation
      const validation = this.validateCommentContent(content);
      if (!validation.isValid) {
        throw new Error(validation.error);
      }

      console.log(`➕ Adding comment to target ${targetType}:${targetId}`);

      const response = await api.post('/Comments/AddComment', {
        content: content.trim(),
        targetId,
        targetType,
        parentCommentId
      });

      return {
        success: true,
        data: mapCommentFromDto(response.data),
        message: parentCommentId ? 'Reply posted successfully!' : 'Comment posted successfully!',
        error: null
      };
    } catch (error) {
      console.error('❌ Error adding comment:', error);
      return {
        success: false,
        data: null,
        message: null,
        error: this.getErrorMessage(error)
      };
    }
  },

  /**
   * React to a comment (like or dislike) with enhanced response mapping
   * @param {number} commentId - Comment ID
   * @param {boolean} isLike - true for like, false for dislike
   * @returns {Object} - Updated reaction data
   */
  async reactToComment(commentId, isLike) {
    try {
      console.log(`👍 Reacting to comment ${commentId} with ${isLike ? 'like' : 'dislike'}`);

      const response = await api.post('/Comments/ReactToComment', {
        commentId,
        isLike
      });

      // Use enhanced DTO mapping for reaction response
      return {
        success: true,
        data: mapReactionResponse(response.data),
        error: null
      };
    } catch (error) {
      console.error('❌ Error reacting to comment:', error);
      return {
        success: false,
        data: null,
        error: this.getErrorMessage(error)
      };
    }
  },

  /**
   * Delete a comment (regular user)
   * @param {number} commentId - Comment ID to delete
   * @returns {Object} - Deletion result
   */
  async deleteComment(commentId) {
    try {
      console.log(`🗑️ Deleting comment ${commentId}`);

      await api.delete(`/Comments/DeleteComment/${commentId}`);

      return {
        success: true,
        message: 'Comment deleted successfully!',
        error: null
      };
    } catch (error) {
      console.error('❌ Error deleting comment:', error);

      let errorMessage = this.getErrorMessage(error);

      // Handle specific error cases
      if (error.response?.status === 400 && error.response?.data?.hasReplies) {
        errorMessage = 'Cannot delete comment with replies. Contact an administrator if you need this comment removed.';
      } else if (error.response?.status === 403) {
        errorMessage = 'You can only delete your own comments.';
      } else if (error.response?.status === 404) {
        errorMessage = 'Comment not found or already deleted.';
      }

      return {
        success: false,
        message: null,
        error: errorMessage
      };
    }
  },

  /**
   * Get comment statistics for a target
   * @param {number} targetId - ID of the target
   * @param {number} targetType - Type: 1=Title, 2=Chapter, 3=ChapterImage
   * @returns {Object} - Comment statistics
   */
  async getCommentStats(targetId, targetType) {
    try {
      console.log(`📊 Loading comment stats for target ${targetType}:${targetId}`);

      const response = await api.get('/Comments/GetCommentStats', {
        params: {
          targetId,
          targetType
        }
      });

      return {
        success: true,
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('❌ Error loading comment stats:', error);
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
  },

  // =============================================================================
  // REPORTING METHODS
  // =============================================================================

  /**
   * Report a comment for inappropriate content
   * @param {number} commentId - Comment ID to report
   * @param {string} reason - Reason for reporting
   * @returns {Object} - Report result
   */
  async reportComment(commentId, reason) {
    try {
      console.log(`🚨 Reporting comment ${commentId} for: ${reason}`);

      const response = await api.post('/Comments/ReportComment', {
        commentId,
        reason
      });

      return {
        success: true,
        message: 'Comment reported successfully. Thank you for helping keep our community safe.',
        data: response.data,
        error: null
      };
    } catch (error) {
      console.error('❌ Error reporting comment:', error);

      let errorMessage = this.getErrorMessage(error);

      if (error.response?.status === 400 && error.response?.data?.message?.includes('already reported')) {
        errorMessage = 'You have already reported this comment.';
      } else if (error.response?.status === 404) {
        errorMessage = 'Comment not found.';
      }

      return {
        success: false,
        message: null,
        data: null,
        error: errorMessage
      };
    }
  },

  // =============================================================================
  // UTILITY METHODS - Enhanced with better validation and formatting
  // =============================================================================

  /**
   * Extract error message from error object with enhanced error handling
   * @param {Error} error - Error object
   * @returns {string} - Human readable error message
   */
  getErrorMessage(error) {
    // Check for structured error response
    if (error.response?.data?.message) {
      return error.response.data.message;
    }
    if (error.response?.data?.error) {
      return error.response.data.error;
    }
    if (error.response?.data?.errors) {
      // Handle validation errors array
      const errors = error.response.data.errors;
      if (Array.isArray(errors)) {
        return errors.join(', ');
      }
      if (typeof errors === 'object') {
        return Object.values(errors).flat().join(', ');
      }
    }

    // Network/connection errors
    if (error.code === 'ECONNABORTED') {
      return 'Request timed out. Please try again.';
    }
    if (error.code === 'ERR_NETWORK') {
      return 'Network error. Please check your connection.';
    }

    // General error message
    if (error.message) {
      return error.message;
    }

    return 'An unexpected error occurred';
  },

  /**
   * Enhanced validation for comment content
   * @param {string} content - Content to validate
   * @returns {Object} - Validation result with detailed feedback
   */
  validateCommentContent(content) {
    if (!content || typeof content !== 'string') {
      return {
        isValid: false,
        error: 'Comment content is required'
      };
    }

    const trimmedContent = content.trim();

    if (trimmedContent.length === 0) {
      return {
        isValid: false,
        error: 'Comment cannot be empty'
      };
    }

    if (trimmedContent.length < 3) {
      return {
        isValid: false,
        error: 'Comment must be at least 3 characters long'
      };
    }

    if (trimmedContent.length > 2000) {
      return {
        isValid: false,
        error: `Comment is too long (${trimmedContent.length}/2000 characters)`
      };
    }

    // Check for potentially harmful content patterns
    const suspiciousPatterns = [
      /(.)\1{20,}/g, // Excessive character repetition
      /^[^a-zA-Z0-9]*$/g, // Only special characters
    ];

    for (const pattern of suspiciousPatterns) {
      if (pattern.test(trimmedContent)) {
        return {
          isValid: false,
          error: 'Comment contains invalid content patterns'
        };
      }
    }

    return {
      isValid: true,
      error: null,
      stats: {
        length: trimmedContent.length,
        remainingChars: 2000 - trimmedContent.length
      }
    };
  },

  /**
   * Enhanced date formatting with more options
   * @param {string|Date} date - Date to format
   * @param {Object} options - Formatting options
   * @returns {string} - Formatted date
   */
  formatCommentDate(date, options = {}) {
    try {
      const {
        relative = true,
        includeTime = false,
        shortFormat = false
      } = options;

      const commentDate = new Date(date);
      const now = new Date();

      if (!relative) {
        return commentDate.toLocaleDateString('en-US', {
          year: 'numeric',
          month: shortFormat ? 'short' : 'long',
          day: 'numeric',
          ...(includeTime && {
            hour: '2-digit',
            minute: '2-digit'
          })
        });
      }

      const diffInSeconds = Math.floor((now - commentDate) / 1000);

      if (diffInSeconds < 30) {
        return 'Just now';
      }

      if (diffInSeconds < 60) {
        return `${diffInSeconds} seconds ago`;
      }

      const diffInMinutes = Math.floor(diffInSeconds / 60);
      if (diffInMinutes < 60) {
        return `${diffInMinutes} minute${diffInMinutes !== 1 ? 's' : ''} ago`;
      }

      const diffInHours = Math.floor(diffInMinutes / 60);
      if (diffInHours < 24) {
        return `${diffInHours} hour${diffInHours !== 1 ? 's' : ''} ago`;
      }

      const diffInDays = Math.floor(diffInHours / 24);
      if (diffInDays < 7) {
        return `${diffInDays} day${diffInDays !== 1 ? 's' : ''} ago`;
      }

      const diffInWeeks = Math.floor(diffInDays / 7);
      if (diffInWeeks < 4) {
        return `${diffInWeeks} week${diffInWeeks !== 1 ? 's' : ''} ago`;
      }

      return commentDate.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });
    } catch (error) {
      console.error('❌ Error formatting date:', error);
      return 'Unknown date';
    }
  },

  /**
   * Test API connectivity with enhanced diagnostics
   * @returns {Object} - Detailed connection status
   */
  async testConnection() {
    try {
      const startTime = Date.now();

      await api.get('/Comments/GetCommentStats', {
        params: { targetId: 1, targetType: 1 }
      });

      const responseTime = Date.now() - startTime;

      console.log('✅ Comments service connection successful');
      return {
        success: true,
        responseTime,
        status: 'Connected'
      };
    } catch (error) {
      console.error('❌ Comments service connection failed:', error);
      return {
        success: false,
        error: this.getErrorMessage(error),
        status: 'Disconnected'
      };
    }
  },

  /**
   * Sanitize comment content for display
   * @param {string} content - Raw comment content
   * @returns {string} - Sanitized content
   */
  sanitizeContent(content) {
    if (!content) return '';

    return content
      .trim()
      .replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '') // Remove scripts
      .replace(/<iframe\b[^<]*(?:(?!<\/iframe>)<[^<]*)*<\/iframe>/gi, '') // Remove iframes
      .replace(/javascript:/gi, '') // Remove javascript protocols
      .replace(/on\w+\s*=/gi, ''); // Remove event handlers
  },

  /**
   * Get comment thread depth
   * @param {Object} comment - Comment object
   * @returns {number} - Thread depth
   */
  getCommentDepth(comment) {
    let depth = 0;
    let current = comment;

    while (current.parentCommentId) {
      depth++;
      // In a real implementation, you'd need to look up the parent
      // This is a simplified version
      break;
    }

    return depth;
  },

  /**
   * Calculate reading time for comment
   * @param {string} content - Comment content
   * @returns {number} - Reading time in seconds
   */
  calculateReadingTime(content) {
    if (!content) return 0;

    const wordsPerMinute = 200;
    const wordCount = content.trim().split(/\s+/).length;
    const readingTimeMinutes = wordCount / wordsPerMinute;

    return Math.max(Math.ceil(readingTimeMinutes * 60), 5); // Minimum 5 seconds
  }
};

export default commentsService;
