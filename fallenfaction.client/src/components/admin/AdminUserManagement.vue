<!-- components/admin/AdminUserManagement.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">User Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Manage users, roles, and permissions</p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-5 gap-8">
        <!-- Left Column: User Actions -->
        <div class="lg:col-span-2">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)]">User Actions</h3>
            </div>

            <div class="p-6 space-y-6">
              <!-- Search by User ID -->
              <div>
                <label for="userIdSearch" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                  Search by User ID
                </label>
                <div class="flex gap-2">
                  <input id="userIdSearch"
                         v-model="userIdSearch"
                         type="text"
                         class="flex-1 px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200"
                         placeholder="Enter User ID" />
                  <button @click="searchUserById"
                          :disabled="!userIdSearch.trim() || isLoadingUserDetails"
                          class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    <span v-if="isLoadingUserDetails">Loading...</span>
                    <span v-else>Search</span>
                  </button>
                </div>
              </div>

              <!-- Selected User Info -->
              <div v-if="selectedUser" class="bg-[var(--color-background-mute)] p-4 rounded-md border border-[var(--color-border)]">
                <h4 class="text-sm font-medium text-[var(--color-text)] mb-3">Selected User</h4>
                <div class="space-y-2 text-sm">
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">ID:</span>
                    <span class="text-[var(--color-text)]">{{ selectedUser.id }}</span>
                  </div>
                  <div class="flex justify-between" v-if="selectedUser.displayName && selectedUser.displayName !== selectedUser.userName">
                    <span class="text-[var(--color-text)] opacity-75">Display Name:</span>
                    <span class="text-[var(--color-text)]">{{ selectedUser.displayName }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Username:</span>
                    <span class="text-[var(--color-text)]">@{{ selectedUser.userName }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Email:</span>
                    <span class="text-[var(--color-text)]">{{ selectedUser.email }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Roles:</span>
                    <span class="text-[var(--color-text)]">{{ selectedUser.roles.join(', ') || 'User' }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Status:</span>
                    <div class="flex gap-1">
                      <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                            :class="selectedUser.isBanned ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800'">
                        {{ selectedUser.isBanned ? 'Site Banned' : 'Active' }}
                      </span>
                      <span v-if="selectedUser.isBannedFromComments"
                            class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
                        Comments Banned
                      </span>
                      <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                            :class="selectedUser.isOnline ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'">
                        {{ selectedUser.isOnline ? 'Online' : 'Offline' }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Ban Actions -->
              <div class="space-y-3">
                <h4 class="text-sm font-medium text-[var(--color-text)]">Ban Actions</h4>
                <div class="grid grid-cols-2 gap-3">
                  <button @click="banUserFromSite"
                          :disabled="!selectedUser || loading || selectedUser.isBanned"
                          class="px-3 py-2 bg-red-600 text-white text-sm font-medium rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    Ban from Site
                  </button>
                  <button @click="banUserFromComments"
                          :disabled="!selectedUser || loading || selectedUser.isBannedFromComments"
                          class="px-3 py-2 bg-orange-600 text-white text-sm font-medium rounded-md hover:bg-orange-700 focus:outline-none focus:ring-2 focus:ring-orange-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    Ban from Comments
                  </button>
                </div>
              </div>

              <!-- Unban Actions -->
              <div class="space-y-3">
                <h4 class="text-sm font-medium text-[var(--color-text)]">Unban Actions</h4>
                <div class="grid grid-cols-2 gap-3">
                  <button @click="unbanUserFromSite"
                          :disabled="!selectedUser || loading || !selectedUser.isBanned"
                          class="px-3 py-2 bg-green-600 text-white text-sm font-medium rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    Unban from Site
                  </button>
                  <button @click="unbanUserFromComments"
                          :disabled="!selectedUser || loading || !selectedUser.isBannedFromComments"
                          class="px-3 py-2 bg-yellow-600 text-white text-sm font-medium rounded-md hover:bg-yellow-700 focus:outline-none focus:ring-2 focus:ring-yellow-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    Unban from Comments
                  </button>
                </div>
              </div>

              <!-- Role Management -->
              <div class="space-y-3">
                <h4 class="text-sm font-medium text-[var(--color-text)]">Role Management</h4>
                <div class="flex gap-2">
                  <select v-model="selectedRole"
                          :disabled="!selectedUser || loading"
                          class="flex-1 px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 disabled:opacity-50">
                    <option value="">Select Role</option>
                    <option v-for="role in availableRoles" :key="role" :value="role">{{ role }}</option>
                  </select>
                  <button @click="changeUserRole"
                          :disabled="!selectedUser || !selectedRole || loading"
                          class="px-4 py-2 bg-purple-600 text-white text-sm font-medium rounded-md hover:bg-purple-700 focus:outline-none focus:ring-2 focus:ring-purple-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    Change Role
                  </button>
                </div>
              </div>

              <!-- Delete User -->
              <div class="space-y-3">
                <h4 class="text-sm font-medium text-[var(--color-text)] text-red-600">Danger Zone</h4>
                <button @click="deleteUser"
                        :disabled="!selectedUser || loading"
                        class="w-full px-4 py-2 bg-red-600 text-white text-sm font-medium rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                  Delete User Permanently
                </button>
              </div>

              <!-- Clear Selection -->
              <button @click="clearSelection"
                      class="w-full px-4 py-2 border border-[var(--color-border)] text-sm font-medium rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] transition-all duration-200">
                Clear Selection
              </button>
            </div>
          </div>
        </div>

        <!-- Right Column: User List -->
        <div class="lg:col-span-3">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4">User List</h3>
              <input type="text"
                     v-model="searchQuery"
                     @input="handleSearch"
                     placeholder="Search by username or email..."
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
            </div>

            <!-- Loading State -->
            <div v-if="isLoadingUsers" class="p-6 text-center">
              <div class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span class="text-[var(--color-text)]">Loading users...</span>
              </div>
            </div>

            <!-- Users Table -->
            <div v-else class="overflow-x-auto max-h-96">
              <table class="min-w-full divide-y divide-[var(--color-border)]">
                <thead class="bg-[var(--color-background-mute)] sticky top-0">
                  <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">User ID</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Username</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Email</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Roles</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Status</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
                  <tr v-for="user in displayedUsers"
                      :key="user.id"
                      class="hover:bg-[var(--color-background-mute)] transition-colors duration-200"
                      :class="{ 'bg-blue-50 border-l-4 border-l-blue-500': selectedUser?.id === user.id }">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                      {{ user.id.substring(0, 8) }}...
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      <div>{{ user.displayName || user.userName }}</div>
                      <div class="text-xs opacity-50">@{{ user.userName }}</div>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      {{ user.email }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      <div class="flex flex-wrap gap-1">
                        <span v-for="role in user.roles" :key="role"
                              class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                          {{ role }}
                        </span>
                        <span v-if="user.roles.length === 0"
                              class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                          User
                        </span>
                      </div>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                      <div class="flex flex-wrap gap-1">
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                              :class="user.isBanned ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800'">
                          {{ user.isBanned ? 'Banned' : 'Active' }}
                        </span>
                        <span v-if="user.isBannedFromComments"
                              class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
                          No Comments
                        </span>
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                              :class="user.isOnline ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'">
                          {{ user.isOnline ? 'Online' : 'Offline' }}
                        </span>
                      </div>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button @click="selectUser(user)"
                              class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-blue-700 bg-blue-100 hover:bg-blue-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors duration-200">
                        Select
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>

              <!-- No Results -->
              <div v-if="displayedUsers.length === 0 && !isLoadingUsers" class="text-center py-8">
                <svg class="mx-auto h-8 w-8 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
                </svg>
                <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No users found</h3>
                <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">{{ searchQuery ? 'Try adjusting your search terms' : 'No users available' }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Success/Error Messages -->
      <div v-if="message.text"
           class="fixed top-20 right-4 max-w-sm w-full bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-lg transition-all duration-300"
           style="z-index: 9999;"
           :class="{
             'border-green-500': message.type === 'success',
             'border-red-500': message.type === 'error'
           }">
        <div class="p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg v-if="message.type === 'success'" class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
              </svg>
              <svg v-else class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="ml-3 w-0 flex-1">
              <p class="text-sm font-medium text-[var(--color-text)]">
                {{ message.text }}
              </p>
            </div>
            <div class="ml-4 flex-shrink-0 flex">
              <button @click="message.text = ''"
                      class="rounded-md inline-flex text-[var(--color-text)] opacity-50 hover:opacity-75 focus:outline-none focus:ring-2 focus:ring-green-500 transition-all duration-200">
                <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { ref, reactive, computed, onMounted } from 'vue';
  import { userService } from '../../services/userService';

  export default {
    name: 'AdminUserManagement',
    setup() {
      const users = ref([]);
      const searchResults = ref([]);
      const searchQuery = ref('');
      const userIdSearch = ref('');
      const selectedUser = ref(null);
      const selectedRole = ref('');
      const availableRoles = ref([]);
      const isLoadingUsers = ref(true);
      const isLoadingUserDetails = ref(false);
      const loading = ref(false);

      const message = reactive({
        text: '',
        type: ''
      });

      let searchTimeout = null;

      const displayedUsers = computed(() => {
        return searchQuery.value ? searchResults.value : users.value;
      });

      const showMessage = (text, type) => {
        message.text = text;
        message.type = type;
        setTimeout(() => {
          message.text = '';
        }, type === 'success' ? 3000 : 5000);
      };

      const loadUsers = async () => {
        isLoadingUsers.value = true;
        try {
          const result = await userService.getUsers();
          if (result.success) {
            users.value = result.data;
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to load users', 'error');
        } finally {
          isLoadingUsers.value = false;
        }
      };

      const loadRoles = async () => {
        try {
          const result = await userService.getRoles();
          if (result.success) {
            availableRoles.value = result.data;
          }
        } catch (error) {
          console.error('Failed to load roles:', error);
        }
      };

      const handleSearch = () => {
        if (searchTimeout) {
          clearTimeout(searchTimeout);
        }

        searchTimeout = setTimeout(async () => {
          if (!searchQuery.value.trim()) {
            searchResults.value = [];
            return;
          }

          try {
            const result = await userService.searchUsers(searchQuery.value.trim());
            if (result.success) {
              searchResults.value = result.data;
            } else {
              searchResults.value = [];
            }
          } catch (error) {
            searchResults.value = [];
          }
        }, 300);
      };

      const searchUserById = async () => {
        if (!userIdSearch.value.trim()) return;

        isLoadingUserDetails.value = true;
        try {
          const result = await userService.getUserById(userIdSearch.value.trim());
          if (result.success) {
            selectedUser.value = result.data;
            selectedRole.value = '';
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to fetch user details', 'error');
        } finally {
          isLoadingUserDetails.value = false;
        }
      };

      const selectUser = (user) => {
        selectedUser.value = user;
        userIdSearch.value = user.id;
        selectedRole.value = '';
      };

      const clearSelection = () => {
        selectedUser.value = null;
        userIdSearch.value = '';
        selectedRole.value = '';
      };

      const banUserFromSite = async () => {
        if (!selectedUser.value || !confirm(`Are you sure you want to ban "${selectedUser.value.userName}" from the site?`)) {
          return;
        }

        loading.value = true;
        try {
          const result = await userService.banUserFromSite(selectedUser.value.id);
          if (result.success) {
            showMessage(result.message, 'success');
            selectedUser.value.isBanned = true;
            await loadUsers();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to ban user from site', 'error');
        } finally {
          loading.value = false;
        }
      };

      const banUserFromComments = async () => {
        if (!selectedUser.value || !confirm(`Are you sure you want to ban "${selectedUser.value.userName}" from comments?`)) {
          return;
        }

        loading.value = true;
        try {
          const result = await userService.banUserFromComments(selectedUser.value.id);
          if (result.success) {
            showMessage(result.message, 'success');
            selectedUser.value.isBannedFromComments = true;
            await loadUsers();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to ban user from comments', 'error');
        } finally {
          loading.value = false;
        }
      };

      const unbanUserFromSite = async () => {
        if (!selectedUser.value || !confirm(`Are you sure you want to unban "${selectedUser.value.userName}" from the site?`)) {
          return;
        }

        loading.value = true;
        try {
          const result = await userService.unbanUserFromSite(selectedUser.value.id);
          if (result.success) {
            showMessage(result.message, 'success');
            selectedUser.value.isBanned = false;
            await loadUsers();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to unban user from site', 'error');
        } finally {
          loading.value = false;
        }
      };

      const unbanUserFromComments = async () => {
        if (!selectedUser.value || !confirm(`Are you sure you want to unban "${selectedUser.value.userName}" from comments?`)) {
          return;
        }

        loading.value = true;
        try {
          const result = await userService.unbanUserFromComments(selectedUser.value.id);
          if (result.success) {
            showMessage(result.message, 'success');
            selectedUser.value.isBannedFromComments = false;
            await loadUsers();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to unban user from comments', 'error');
        } finally {
          loading.value = false;
        }
      };

      const changeUserRole = async () => {
        if (!selectedUser.value || !selectedRole.value || !confirm(`Are you sure you want to change "${selectedUser.value.userName}" role to "${selectedRole.value}"?`)) {
          return;
        }

        loading.value = true;
        try {
          const result = await userService.changeUserRole(selectedUser.value.id, selectedRole.value);
          if (result.success) {
            showMessage(result.message, 'success');
            selectedUser.value.roles = selectedRole.value === 'User' ? [] : [selectedRole.value];
            selectedRole.value = '';
            await loadUsers();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to change user role', 'error');
        } finally {
          loading.value = false;
        }
      };

      const deleteUser = async () => {
        if (!selectedUser.value || !confirm(`Are you sure you want to permanently delete user "${selectedUser.value.userName}"? This action cannot be undone.`)) {
          return;
        }

        loading.value = true;
        try {
          const result = await userService.deleteUser(selectedUser.value.id);
          if (result.success) {
            showMessage(result.message, 'success');
            clearSelection();
            await loadUsers();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to delete user', 'error');
        } finally {
          loading.value = false;
        }
      };

      onMounted(() => {
        loadUsers();
        loadRoles();
      });

      return {
        users,
        searchResults,
        searchQuery,
        userIdSearch,
        selectedUser,
        selectedRole,
        availableRoles,
        isLoadingUsers,
        isLoadingUserDetails,
        loading,
        message,
        displayedUsers,
        loadUsers,
        handleSearch,
        searchUserById,
        selectUser,
        clearSelection,
        banUserFromSite,
        banUserFromComments,
        unbanUserFromSite,
        unbanUserFromComments,
        changeUserRole,
        deleteUser
      };
    }
  };
</script>

<style scoped>
  /* Custom focus ring offset color */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }

  /* Custom scrollbar for table */
  .overflow-x-auto::-webkit-scrollbar {
    height: 8px;
  }

  .overflow-x-auto::-webkit-scrollbar-track {
    background: var(--color-background-mute);
  }

  .overflow-x-auto::-webkit-scrollbar-thumb {
    background: var(--color-border);
    border-radius: 4px;
  }

    .overflow-x-auto::-webkit-scrollbar-thumb:hover {
      background: var(--color-border-hover);
    }
</style>
