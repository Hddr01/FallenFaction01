import apiClient from './apiClient.js'
// services/titleChangeService.js
export const titleChangeService = {
  /**
   * Get complete change log history for a title
   * GET: api/AdminTitle/TitleChangeLog/{titleId}
   */
  async getTitleChangeLog(titleId) {
    try {
      console.log('Fetching title change log for ID:', titleId);
      const response = await apiClient.get(`/AdminTitle/TitleChangeLog/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching title change log:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load change log',
        data: []
      };
    }
  },

  /**
   * Get change statistics for a title
   * GET: api/AdminTitle/TitleChangeStats/{titleId}
   */
  async getTitleChangeStats(titleId) {
    try {
      console.log('Fetching title change stats for ID:', titleId);
      const response = await apiClient.get(`/AdminTitle/TitleChangeStats/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching title change stats:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load change statistics',
        data: {
          TotalChanges: 0,
          ChangesByStatus: [],
          LastUpdate: null
        }
      };
    }
  },

  /**
   * Get pending changes for a title (for admins)
   * GET: api/AdminTitle/PendingChanges/{titleId}
   */
  async getPendingChangesForTitle(titleId) {
    try {
      console.log('Fetching pending changes for title ID:', titleId);
      const response = await apiClient.get(`/AdminTitle/PendingChanges/${titleId}`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      console.error('Error fetching pending changes:', error);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Failed to load pending changes',
        data: {
          TitleId: titleId,
          TitleName: 'Unknown',
          Changes: []
        }
      };
    }
  },

  /**
   * Helper method to format change type for display
   */
  formatChangeType(changeType) {
    const typeMap = {
      'Original Title': 'Original Title',
      'English Title': 'English Title',
      'Description': 'Description',
      'Alternative Names': 'Alternative Names',
      'Release Date': 'Release Date',
      'Status': 'Title Status',
      'Translation Status': 'Translation Status',
      'Type': 'Content Type',
      'Age Restriction': 'Age Rating',
      'Cover Image': 'Cover Image',
      'Background Image': 'Background Image',
      'Authors': 'Authors',
      'Artists': 'Artists',
      'Publishers': 'Publishers',
      'Teams': 'Teams',
      'Categories': 'Categories',
      'Tags': 'Tags',
      'Formats': 'Formats',
      'External Links': 'External Links',
      'Availability Status': 'Availability',
      'Comments Status': 'Comments',
      'Chapter Comments Status': 'Chapter Comments'
    };
    return typeMap[changeType] || changeType;
  },

  /**
   * Helper method to get status display info
   */
  getStatusInfo(status) {
    const statusMap = {
      'Pending': {
        label: 'Pending Review',
        class: 'bg-yellow-100 text-yellow-800 border-yellow-200',
        icon: 'clock'
      },
      'Approved': {
        label: 'Approved',
        class: 'bg-green-100 text-green-800 border-green-200',
        icon: 'check-circle'
      },
      'AutoApproved': {
        label: 'Auto-Approved',
        class: 'bg-blue-100 text-blue-800 border-blue-200',
        icon: 'zap'
      },
      'Rejected': {
        label: 'Rejected',
        class: 'bg-red-100 text-red-800 border-red-200',
        icon: 'x-circle'
      }
    };
    return statusMap[status] || {
      label: status,
      class: 'bg-gray-100 text-gray-800 border-gray-200',
      icon: 'help-circle'
    };
  },

  /**
   * Helper method to format datetime for display
   */
  formatDateTime(dateString) {
    if (!dateString) return 'Not set';

    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now - date) / 1000);

    // Show relative time for recent dates
    if (diffInSeconds < 60) return 'Just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} minutes ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} hours ago`;
    if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)} days ago`;

    // Show absolute date for older dates
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  },

  /**
   * Helper method to truncate long values for display
   */
  truncateValue(value, maxLength = 100) {
    if (!value || typeof value !== 'string') return value || 'N/A';
    if (value.length <= maxLength) return value;
    return value.substring(0, maxLength) + '...';
  },

  /**
   * Test API connectivity
   */
  async testConnection() {
    try {
      console.log('Testing title change API connection...');
      // Test with a dummy title ID - the endpoint should return 404 but connection works
      const response = await apiClient.get('/AdminTitle/TitleChangeLog/1');
      console.log('Title change API connection test successful');
      return { success: true };
    } catch (error) {
      if (error.response?.status === 404) {
        // 404 is expected for non-existent title, connection works
        console.log('Title change API connection test successful (404 expected)');
        return { success: true };
      }
      console.error('Title change API connection test failed:', error);
      return { success: false, error: error.message };
    }
  }
};

export default titleChangeService;
