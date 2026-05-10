import apiClient from './apiClient.js'
// services/commentsService.js - Simplified for Infinite Accordion Approach
const mapCommentFromDto = (dto) => {
  if (!dto) return null;

  return {
    id: dto.id,
    content: dto.content,
    userId: dto.userId,
    userName: dto.userName,
    userHandle: dto.userHandle,
    userAvatarUrl: dto.userAvatarUrl,
    postedDate: dto.postedDate,
    likesCount: dto.likesCount || 0,
    dislikesCount: dto.dislikesCount || 0,
    currentUserLiked: dto.currentUserLiked || false,
    currentUserDisliked: dto.currentUserDisliked || false,
    isDeleted: dto.isDeleted || false,
    deletedAt: dto.deletedAt,
    deletedByUserName: dto.deletedByUserName,
    deletionReason: dto.deletionReason,
    parentCommentId: dto.parentCommentId,
    isPinned: dto.isPinned ?? false,
    pinnedAt: dto.pinnedAt || null,
    pinnedByUserName: dto.pinnedByUserName || null,
    pinnedByTeamName: dto.pinnedByTeamName || null,
    replies: dto.replies?.map(mapCommentFromDto) || []
  };
};

/**
 * Maps pagination data from API response
 */
const mapPagination = (data) => {
  const pagination = data.pagination || {};

  return {
    totalCount: pagination.totalCount || 0,
    page: pagination.page || 1,
    pageSize: pagination.pageSize || 20,
    totalPages: pagination.totalPages || 0,
    hasNext: pagination.hasNext || false,
    hasPrevious: pagination.hasPrevious || false
  };
};

// =============================================================================
// COMMENT SERVICE API
// =============================================================================

export const commentsService = {
  /**
   * Get comment statistics for a target
   */
  async getCommentStats(targetId, targetType) {
    try {
      const response = await apiClient.get('/Comments/GetCommentStats', {
        params: { targetId, targetType }
      });

      const data = response.data;
      return {
        success: true,
        data: {
          totalComments: data.totalComments || 0,
          commentsEnabled: data.commentsEnabled ?? true
        }
      };
    } catch (error) {
      console.error('Error fetching comment stats:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load comment statistics',
        data: {
          totalComments: 0,
          commentsEnabled: true
        }
      };
    }
  },

  /**
   * Get comments for a target with infinite nesting support
   * All nested replies are loaded recursively - no depth limit!
   * 
   * @param {number} targetId - The target entity ID
   * @param {number} targetType - 1 = Title, 2 = Volume, 3 = Chapter
   * @param {number} page - Page number (default: 1)
   * @param {number} pageSize - Items per page (default: 20)
   * @param {string} sortBy - Sort order: 'newest', 'oldest', 'likes' (default: 'newest')
   */
  async getComments(targetId, targetType, page = 1, pageSize = 20, sortBy = 'newest') {
    try {
      const response = await apiClient.get('/Comments/GetComments', {
        params: {
          targetId,
          targetType,
          page,
          pageSize,
          sortBy
        }
      });

      const data = response.data;

      return {
        success: true,
        data: {
          comments: (data.comments || []).map(mapCommentFromDto),
          pagination: mapPagination(data)
        }
      };
    } catch (error) {
      console.error('Error fetching comments:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load comments',
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
        }
      };
    }
  },

  /**
   * Get a specific comment thread (Reddit-style isolated view)
   * 
   * @param {number} commentId - The comment ID to get the thread for
   */
  async getCommentThread(commentId) {
    try {
      const response = await apiClient.get(`/Comments/GetCommentThread/${commentId}`);

      const data = response.data;

      return {
        success: true,
        data: {
          comment: mapCommentFromDto(data.comment),
          parentChain: (data.parentChain || []).map(parent => ({
            id: parent.id,
            userName: parent.userName,
            content: parent.content,
            isDeleted: parent.isDeleted
          })),
          targetId: data.targetId,
          targetType: data.targetType
        }
      };
    } catch (error) {
      console.error('Error fetching comment thread:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load comment thread',
        data: null
      };
    }
  },

  /**
   * Get overall comment statistics for admin dashboard
   * 
   * @returns {Object} { totalComments, deletedComments, reportedComments, ... }
   */
  async getCommentStatsForAdmin() {
    try {
      const response = await apiClient.get('/AdminComments/GetStats');

      const data = response.data;

      return {
        success: true,
        data: {
          totalComments: data.totalComments || 0,
          deletedComments: data.deletedComments || 0,
          reportedComments: data.reportedComments || 0,
          activeComments: data.activeComments || 0,
          commentsToday: data.commentsToday || 0,
          commentsThisWeek: data.commentsThisWeek || 0,
          commentsThisMonth: data.commentsThisMonth || 0
        }
      };
    } catch (error) {
      console.error('Error fetching admin comment stats:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load comment statistics',
        data: {
          totalComments: 0,
          deletedComments: 0,
          reportedComments: 0,
          activeComments: 0,
          commentsToday: 0,
          commentsThisWeek: 0,
          commentsThisMonth: 0
        }
      };
    }
  },

  /**
   * Get all comments for admin management (with filters)
   * 
   * @param {Object} params - Filter parameters
   * @param {number} params.page - Page number
   * @param {number} params.pageSize - Items per page
   * @param {string} params.sortBy - Sort order
   * @param {number} params.targetType - Filter by target type (1=Title, 2=Chapter, 3=Image)
   * @param {boolean} params.showReported - Show only reported comments
   * @param {boolean} params.showDeleted - Show deleted comments
   * @param {string} params.search - Search query
   */
  async getAllCommentsForAdmin(params = {}) {
    try {
      const response = await apiClient.get('/AdminComments/GetAllComments', {
        params: {
          page: params.page || 1,
          pageSize: params.pageSize || 20,
          sortBy: params.sortBy || 'newest',
          targetType: params.targetType,
          showReported: params.showReported || false,
          showDeleted: params.showDeleted || false,
          search: params.search || ''
        }
      });

      const data = response.data;

      return {
        success: true,
        data: {
          comments: (data.comments || []).map(mapCommentFromDto),
          pagination: mapPagination(data)
        }
      };
    } catch (error) {
      console.error('Error fetching admin comments:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load comments',
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
        }
      };
    }
  },

  /**
   * Add a new comment or reply (supports infinite nesting depth)
   * 
   * BACKWARDS COMPATIBLE: Supports both parameter orders
   * - Recommended: addComment(targetId, targetType, content, parentCommentId)
   * - Legacy: addComment(content, targetId, targetType, parentCommentId)
   * 
   * @param {number|string} param1 - targetId OR content (for backwards compat)
   * @param {number|string} param2 - targetType OR targetId
   * @param {string|number} param3 - content OR targetType
   * @param {number|null} parentCommentId - Parent comment ID for replies
   */
  async addComment(param1, param2, param3, parentCommentId = null) {
    try {
      let targetId, targetType, content;

      // Auto-detect parameter order based on types
      // If param1 is a string and param2/param3 are numbers, it's the old order
      if (typeof param1 === 'string' && typeof param2 === 'number' && typeof param3 === 'number') {
        // OLD ORDER: (content, targetId, targetType, parentCommentId)
        content = param1;
        targetId = param2;
        targetType = param3;
      } else {
        // NEW ORDER: (targetId, targetType, content, parentCommentId)
        targetId = param1;
        targetType = param2;
        content = param3;
      }

      // Validate content using validation helper
      const validation = this.validateCommentContent(content);
      if (!validation.isValid) {
        return {
          success: false,
          error: validation.error
        };
      }

      const response = await apiClient.post('/Comments/AddComment', {
        targetId: parseInt(targetId),
        targetType: parseInt(targetType),
        content: content.trim(),
        parentCommentId: parentCommentId ? parseInt(parentCommentId) : null
      });

      return {
        success: true,
        data: mapCommentFromDto(response.data),
        message: 'Comment posted successfully'
      };
    } catch (error) {
      console.error('Error adding comment:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to post comment'
      };
    }
  },

  /**
   * Validate comment content before submission
   * 
   * @param {string} content - Comment text to validate
   * @returns {Object} { isValid: boolean, error?: string }
   */
  validateCommentContent(content) {
    if (!content || typeof content !== 'string') {
      return {
        isValid: false,
        error: 'Comment content is required'
      };
    }

    const trimmed = content.trim();

    if (trimmed.length === 0) {
      return {
        isValid: false,
        error: 'Comment content cannot be empty'
      };
    }

    if (trimmed.length > 2000) {
      return {
        isValid: false,
        error: `Comment cannot exceed 2000 characters (currently ${trimmed.length})`
      };
    }

    return {
      isValid: true
    };
  },

  /**
   * React to a comment (like/dislike toggle)
   * 
   * @param {number} commentId - The comment ID
   * @param {boolean} isLike - true for like, false for dislike
   */
  async reactToComment(commentId, isLike) {
    try {
      const response = await apiClient.post(`/Comments/${commentId}/React`, {
        isLike
      });

      const data = response.data;

      return {
        success: true,
        data: {
          commentId: data.commentId,
          likesCount: data.likesCount || 0,
          dislikesCount: data.dislikesCount || 0,
          userLiked: data.userLiked || false,
          userDisliked: data.userDisliked || false
        }
      };
    } catch (error) {
      console.error('Error reacting to comment:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to update reaction'
      };
    }
  },

  /**
   * Delete a comment (owner hard delete or admin soft delete)
   * 
   * @param {number} commentId - The comment ID to delete
   */
  async deleteComment(commentId) {
    try {
      const response = await apiClient.delete(`/Comments/${commentId}`);

      return {
        success: true,
        message: response.data.message || 'Comment deleted successfully'
      };
    } catch (error) {
      console.error('Error deleting comment:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete comment'
      };
    }
  },

  /**
   * Soft delete a comment as admin (with reason)
   * 
   * @param {number} commentId - The comment ID to delete
   * @param {string} reason - Reason for deletion
   */
  async deleteCommentAsAdmin(commentId, reason = 'Deleted by administrator') {
    try {
      const response = await apiClient.delete(`/AdminComments/DeleteComment/${commentId}`, {
        params: { reason }
      });

      return {
        success: true,
        message: response.data.message || 'Comment deleted successfully'
      };
    } catch (error) {
      console.error('Error deleting comment as admin:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete comment'
      };
    }
  },

  /**
   * Restore a soft-deleted comment (admin only)
   * 
   * @param {number} commentId - The comment ID to restore
   */
  async restoreCommentAsAdmin(commentId) {
    try {
      const response = await apiClient.post(`/AdminComments/RestoreComment/${commentId}`);

      return {
        success: true,
        message: response.data.message || 'Comment restored successfully'
      };
    } catch (error) {
      console.error('Error restoring comment:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to restore comment'
      };
    }
  },

  /**
   * Permanently delete a comment (admin only - hard delete)
   * WARNING: This is irreversible!
   * 
   * @param {number} commentId - The comment ID to permanently delete
   */
  async permanentlyDeleteComment(commentId) {
    try {
      const response = await apiClient.delete(`/AdminComments/PermanentlyDeleteComment/${commentId}`);

      return {
        success: true,
        message: response.data.message || 'Comment permanently deleted'
      };
    } catch (error) {
      console.error('Error permanently deleting comment:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to permanently delete comment'
      };
    }
  },

  /**
   * Format comment date for display
   * 
   * @param {string} dateString - ISO date string
   * @returns {string} Formatted relative time
   */
  formatCommentDate(dateString) {
    if (!dateString) return '';

    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now - date) / 1000);

    // Less than a minute
    if (diffInSeconds < 60) {
      return 'just now';
    }

    // Less than an hour
    const diffInMinutes = Math.floor(diffInSeconds / 60);
    if (diffInMinutes < 60) {
      return `${diffInMinutes}m ago`;
    }

    // Less than a day
    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) {
      return `${diffInHours}h ago`;
    }

    // Less than a week
    const diffInDays = Math.floor(diffInHours / 24);
    if (diffInDays < 7) {
      return `${diffInDays}d ago`;
    }

    // Less than a month
    if (diffInDays < 30) {
      const weeks = Math.floor(diffInDays / 7);
      return `${weeks}w ago`;
    }

    // Format as date
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    });
  },

  /**
   * Count total comments including all nested replies
   * 
   * @param {Array} comments - Array of comment objects
   * @returns {number} Total count including all nested replies
   */
  countTotalComments(comments) {
    if (!comments || comments.length === 0) return 0;

    return comments.reduce((total, comment) => {
      return total + 1 + this.countTotalComments(comment.replies || []);
    }, 0);
  },

  /**
   * Find a comment by ID in a nested tree (supports infinite depth)
   * 
   * @param {Array} comments - Array of comment objects
   * @param {number} commentId - ID to search for
   * @returns {Object|null} Found comment or null
   */
  findCommentById(comments, commentId) {
    for (const comment of comments) {
      if (comment.id === commentId) {
        return comment;
      }

      if (comment.replies && comment.replies.length > 0) {
        const found = this.findCommentById(comment.replies, commentId);
        if (found) return found;
      }
    }

    return null;
  },

  /**
   * Update a comment in the nested tree (supports infinite depth)
   * 
   * @param {Array} comments - Array of comment objects
   * @param {number} commentId - ID of comment to update
   * @param {Object} updates - Properties to update
   * @returns {boolean} Whether comment was found and updated
   */
  updateCommentInTree(comments, commentId, updates) {
    for (let i = 0; i < comments.length; i++) {
      if (comments[i].id === commentId) {
        comments[i] = { ...comments[i], ...updates };
        return true;
      }

      if (comments[i].replies && comments[i].replies.length > 0) {
        const updated = this.updateCommentInTree(comments[i].replies, commentId, updates);
        if (updated) return true;
      }
    }

    return false;
  },

  /**
   * Remove a comment from the nested tree (supports infinite depth)
   * 
   * @param {Array} comments - Array of comment objects
   * @param {number} commentId - ID of comment to remove
   * @returns {boolean} Whether comment was found and removed
   */
  removeCommentFromTree(comments, commentId) {
    for (let i = 0; i < comments.length; i++) {
      if (comments[i].id === commentId) {
        comments.splice(i, 1);
        return true;
      }

      if (comments[i].replies && comments[i].replies.length > 0) {
        const removed = this.removeCommentFromTree(comments[i].replies, commentId);
        if (removed) return true;
      }
    }

    return false;
  },

  /**
   * Pin a comment (requires team permission or admin role)
   */
  async pinComment(commentId) {
    const response = await apiClient.put(`/Comments/${commentId}/pin`);
    return response.data;
  },

  /**
   * Unpin a comment
   */
  async unpinComment(commentId) {
    const response = await apiClient.put(`/Comments/${commentId}/unpin`);
    return response.data;
  }
};

export default commentsService;
