// services/teamRoleService.js
import axios from 'axios';

// Create axios instance with base configuration
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Accept': 'application/json',
    'Content-Type': 'application/json'
  },
  withCredentials: true,
  timeout: 10000,
});

// Request interceptor to add auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    if (import.meta.env.DEV) {
      console.log(`Role API Request: ${config.method?.toUpperCase()} ${config.url}`, config.data);
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token expiration
api.interceptors.response.use(
  (response) => {
    if (import.meta.env.DEV) {
      console.log(`Role API Response: ${response.status} ${response.config.url}`, response.data);
    }
    return response;
  },
  (error) => {
    console.error('Role API Error:', error);
    console.error('Error details:', {
      status: error.response?.status,
      data: error.response?.data,
      url: error.config?.url
    });

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

export const teamRoleService = {

  // Get permissions overview for a team (roles, permissions, etc.)
  async getPermissionsOverview(teamId) {
    try {
      const response = await api.get(`/TeamRoleApi/${teamId}/permissions-overview`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Failed to load permissions overview',
        data: {
          AllPermissions: [],
          DefaultRoles: [],
          CustomRoles: []
        }
      };
    }
  },

  // Get specific member's permissions
  async getMemberPermissions(teamId, userId) {
    try {
      const response = await api.get(`/TeamRoleApi/${teamId}/member/${userId}/permissions`);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Failed to load member permissions',
        data: {
          Permissions: []
        }
      };
    }
  },

  // Update member's custom permissions
  async updateMemberPermissions(teamId, userId, permissionNames) {
    try {
      const response = await api.put(`/TeamRoleApi/${teamId}/member/${userId}/permissions`, {
        permissionNames: permissionNames
      });
      return {
        success: true,
        message: response.data.message || 'Member permissions updated successfully'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Failed to update member permissions'
      };
    }
  },

  // Apply a role template to a member
  async applyRoleTemplate(teamId, userId, templateName) {
    try {
      const response = await api.post(`/TeamRoleApi/${teamId}/member/${userId}/apply-template`, {
        templateName: templateName
      });
      return {
        success: true,
        message: response.data.message || 'Role template applied successfully'
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Failed to apply role template'
      };
    }
  },

  // Get available role templates
  async getRoleTemplates() {
    try {
      const response = await api.get('/TeamRoleApi/role-templates');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Failed to load role templates',
        data: []
      };
    }
  },

  // Create custom role (for future implementation)
  async createCustomRole(teamId, roleData) {
    try {
      const response = await api.post(`/TeamRoleApi/${teamId}/custom-roles`, {
        name: roleData.name,
        description: roleData.description,
        permissionNames: roleData.permissions
      });
      return {
        success: true,
        message: response.data.message || 'Custom role created successfully',
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Failed to create custom role'
      };
    }
  },

  // Helper method to get permission categories for UI
  getPermissionCategories() {
    return [
      {
        name: 'content',
        displayName: 'Content Management',
        permissions: [
          'CanAddTitle',
          'CanEditTitle',
          'CanDeleteTitle',
          'CanAddChapter',
          'CanEditChapter',
          'CanDeleteChapter'
        ]
      },
      {
        name: 'members',
        displayName: 'Member Management',
        permissions: [
          'CanAddMember',
          'CanRemoveMember'
        ]
      },
      {
        name: 'analytics',
        displayName: 'Analytics & Reports',
        permissions: [
          'CanViewAnalytics'
        ]
      }
    ];
  },

  // Helper method to get permission details
  getPermissionDetails() {
    return {
      'CanAddTitle': {
        displayName: 'Add Titles',
        description: 'Create new manga titles'
      },
      'CanEditTitle': {
        displayName: 'Edit Titles',
        description: 'Modify existing titles'
      },
      'CanDeleteTitle': {
        displayName: 'Delete Titles',
        description: 'Remove titles from team'
      },
      'CanAddChapter': {
        displayName: 'Add Chapters',
        description: 'Upload new chapters'
      },
      'CanEditChapter': {
        displayName: 'Edit Chapters',
        description: 'Modify existing chapters'
      },
      'CanDeleteChapter': {
        displayName: 'Delete Chapters',
        description: 'Remove chapters'
      },
      'CanAddMember': {
        displayName: 'Add Members',
        description: 'Invite new team members'
      },
      'CanRemoveMember': {
        displayName: 'Remove Members',
        description: 'Remove team members'
      },
      'CanViewAnalytics': {
        displayName: 'View Analytics',
        description: 'Access team statistics'
      }
    };
  },

  // Helper method to get default role configurations
  getDefaultRoles() {
    return [
      {
        name: 'Admin',
        value: 0,
        description: 'Full team management permissions',
        permissions: [
          'CanAddTitle', 'CanEditTitle', 'CanDeleteTitle',
          'CanAddChapter', 'CanEditChapter', 'CanDeleteChapter',
          'CanAddMember', 'CanRemoveMember', 'CanViewAnalytics'
        ]
      },
      {
        name: 'Member',
        value: 1,
        description: 'Can contribute content and moderate',
        permissions: ['CanAddTitle', 'CanEditTitle', 'CanAddChapter', 'CanEditChapter']
      },
      {
        name: 'Viewer',
        value: 2,
        description: 'Read-only access to team content',
        permissions: []
      }
    ];
  },

  // Helper method to format role names
  formatRole(roleValue) {
    const roles = {
      0: 'Admin',
      1: 'Member',
      2: 'Viewer'
    };
    return roles[roleValue] || 'Unknown';
  },

  // Helper method to get role class for styling
  getRoleClass(roleValue) {
    const classes = {
      0: 'bg-red-100 text-red-800',
      1: 'bg-blue-100 text-blue-800',
      2: 'bg-gray-100 text-gray-800'
    };
    return classes[roleValue] || 'bg-gray-100 text-gray-800';
  },

  // Validate if user has specific permission
  hasPermission(userPermissions, permission) {
    return Array.isArray(userPermissions) && userPermissions.includes(permission);
  },

  // Get permissions for a specific role
  getPermissionsForRole(roleValue) {
    const defaultRoles = this.getDefaultRoles();
    const role = defaultRoles.find(r => r.value === roleValue);
    return role ? role.permissions : [];
  },

  // Compare two permission arrays
  comparePermissions(permissions1, permissions2) {
    if (!Array.isArray(permissions1) || !Array.isArray(permissions2)) {
      return false;
    }

    if (permissions1.length !== permissions2.length) {
      return false;
    }

    const sorted1 = [...permissions1].sort();
    const sorted2 = [...permissions2].sort();

    return sorted1.every((perm, index) => perm === sorted2[index]);
  },

  // Get permission differences between two sets
  getPermissionDifferences(oldPermissions, newPermissions) {
    const old = Array.isArray(oldPermissions) ? oldPermissions : [];
    const newPerms = Array.isArray(newPermissions) ? newPermissions : [];

    const added = newPerms.filter(p => !old.includes(p));
    const removed = old.filter(p => !newPerms.includes(p));

    return { added, removed };
  },

  // Validate permission names
  validatePermissions(permissions) {
    const validPermissions = Object.keys(this.getPermissionDetails());
    const invalid = permissions.filter(p => !validPermissions.includes(p));

    return {
      isValid: invalid.length === 0,
      invalidPermissions: invalid,
      validPermissions: permissions.filter(p => validPermissions.includes(p))
    };
  }
};

export default teamRoleService;
