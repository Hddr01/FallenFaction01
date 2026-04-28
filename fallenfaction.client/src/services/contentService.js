import apiClient from './apiClient.js'
// services/contentService.js
export const contentService = {
  // Get user's content overview
  async getUserContent() {
    try {
      const response = await apiClient.get('/Titles/UserContent');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading user content:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load user content',
        data: {
          titles: [],
          pendingTitles: [],
          rejectedTitles: [],
          chapters: [],
          teams: []
        }
      };
    }
  },

  // Get user's uploaded titles
  async getUserTitles() {
    try {
      const response = await apiClient.get('/Titles/UserTitles');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading user titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load titles',
        data: []
      };
    }
  },

  // Get user's pending titles
  async getUserPendingTitles() {
    try {
      const response = await apiClient.get('/TitleApi/user-pending');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading pending titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load pending titles',
        data: []
      };
    }
  },

  // Get user's rejected titles
  async getUserRejectedTitles() {
    try {
      const response = await apiClient.get('/TitleApi/user-rejected');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading rejected titles:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load rejected titles',
        data: []
      };
    }
  },

  // Get user's chapters
  async getUserChapters() {
    try {
      const response = await apiClient.get('/Titles/UserChapters');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading user chapters:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load chapters',
        data: {
          pendingChapters: [],
          approvedChapters: [],
          rejectedChapters: []
        }
      };
    }
  },

  // Get user's teams
  async getUserTeams() {
    try {
      const response = await apiClient.get('/Team/my-teams');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading user teams:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load teams',
        data: []
      };
    }
  },

  // Get pending content for moderation (admin/moderator only)
  async getPendingContent() {
    try {
      const [titlesResponse, chaptersResponse] = await Promise.all([
        apiClient.get('/AdminTitle/PendingTitles'),
        apiClient.get('/Titles/chapters/pending')
      ]);

      return {
        success: true,
        data: {
          pendingTitles: titlesResponse.data,
          pendingChapters: chaptersResponse.data
        }
      };
    } catch (error) {
      console.error('Error loading pending content:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load pending content',
        data: {
          pendingTitles: [],
          pendingChapters: []
        }
      };
    }
  },

  // Get title details
  async getTitleDetails(titleId) {
    try {
      const response = await apiClient.get(`/Titles/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error loading title details:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to load title details'
      };
    }
  },

  // Delete user title (if allowed)
  async deleteTitle(titleId) {
    try {
      const response = await apiClient.delete(`/Titles/${titleId}`);
      return {
        success: true,
        message: response.data.message || 'Title deleted successfully'
      };
    } catch (error) {
      console.error('Error deleting title:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to delete title'
      };
    }
  },

  // Approve pending title (admin only)
  async approveTitle(titleId) {
    try {
      const response = await apiClient.post(`/TitleApi/approve/${titleId}`);
      return {
        success: true,
        message: response.data.message || 'Title approved successfully'
      };
    } catch (error) {
      console.error('Error approving title:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to approve title'
      };
    }
  },

  // Reject pending title (admin only)
  async rejectTitle(titleId, reason) {
    try {
      const response = await apiClient.post(`/TitleApi/reject/${titleId}`, {
        reason: reason
      });
      return {
        success: true,
        message: response.data.message || 'Title rejected successfully'
      };
    } catch (error) {
      console.error('Error rejecting title:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to reject title'
      };
    }
  },

  // Approve pending chapter (admin only)
  async approveChapter(chapterId) {
    try {
      const response = await apiClient.post(`/Titles/chapters/pending/${chapterId}/approve`);
      return {
        success: true,
        message: response.data.message || 'Chapter approved successfully'
      };
    } catch (error) {
      console.error('Error approving chapter:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to approve chapter'
      };
    }
  },

  // Reject pending chapter (admin only)
  async rejectChapter(chapterId, reason) {
    try {
      const response = await apiClient.post(`/Titles/chapters/pending/${chapterId}/reject`, {
        reason: reason
      });
      return {
        success: true,
        message: response.data.message || 'Chapter rejected successfully'
      };
    } catch (error) {
      console.error('Error rejecting chapter:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to reject chapter'
      };
    }
  }
};

export default contentService;
