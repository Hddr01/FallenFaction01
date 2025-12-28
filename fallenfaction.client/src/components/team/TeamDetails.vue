<!-- components/team/TeamDetails.vue - Updated with Role Management -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Loading State -->
      <div v-if="loading" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading team details...</span>
        </div>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-md p-4">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3">
            <h3 class="text-sm font-medium text-red-800">Error Loading Team</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
            <div class="mt-4">
              <button @click="loadTeamDetails"
                      class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                Try Again
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Team Details -->
      <div v-else-if="team" class="space-y-6">
        <!-- Navigation Tabs -->
        <div class="border-b border-[var(--color-border)]">
          <nav class="-mb-px flex space-x-8">
            <button @click="activeTab = 'overview'"
                    class="py-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
                    :class="{
                      'border-green-500 text-green-600': activeTab === 'overview',
                      'border-transparent text-[var(--color-text)] opacity-75 hover:text-[var(--color-heading)] hover:border-[var(--color-border-hover)]': activeTab !== 'overview'
                    }">
              Overview
            </button>
            <button @click="activeTab = 'members'"
                    class="py-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
                    :class="{
                      'border-green-500 text-green-600': activeTab === 'members',
                      'border-transparent text-[var(--color-text)] opacity-75 hover:text-[var(--color-heading)] hover:border-[var(--color-border-hover)]': activeTab !== 'members'
                    }">
              Members ({{ team.members?.length || 0 }})
            </button>
            <button v-if="canManageRoles"
                    @click="activeTab = 'roles'"
                    class="py-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
                    :class="{
                      'border-green-500 text-green-600': activeTab === 'roles',
                      'border-transparent text-[var(--color-text)] opacity-75 hover:text-[var(--color-heading)] hover:border-[var(--color-border-hover)]': activeTab !== 'roles'
                    }">
              Role Management
            </button>
            <button v-if="isCreator || isAdmin"
                    @click="activeTab = 'settings'"
                    class="py-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
                    :class="{
                      'border-green-500 text-green-600': activeTab === 'settings',
                      'border-transparent text-[var(--color-text)] opacity-75 hover:text-[var(--color-heading)] hover:border-[var(--color-border-hover)]': activeTab !== 'settings'
                    }">
              Settings
            </button>
          </nav>
        </div>

        <!-- Tab Content -->
        <div v-if="activeTab === 'overview'">
          <!-- Header -->
          <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-sm overflow-hidden">
            <div class="px-6 py-8">
              <div class="sm:flex sm:items-center sm:justify-between">
                <div class="sm:flex sm:space-x-5">
                  <div class="flex-1">
                    <h1 class="text-2xl font-bold text-[var(--color-heading)]">{{ team.name }}</h1>
                    <p class="mt-2 text-[var(--color-text)]">{{ team.description }}</p>

                    <div class="mt-4 flex items-center space-x-6">
                      <div class="flex items-center text-sm text-[var(--color-text)] opacity-75">
                        <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-1.657-.126-3.153-.356-4.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-1.657.126-3.153.356-4.857m0 0a5.002 5.002 0 019.288 0" />
                        </svg>
                        <span>{{ team.members?.length || 0 }} member{{ team.memberCount !== 1 ? 's' : '' }}</span>
                      </div>
                      <div class="flex items-center text-sm text-[var(--color-text)] opacity-75">
                        <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
                        </svg>
                        <span>{{ team.titleCount }} title{{ team.titleCount !== 1 ? 's' : '' }}</span>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="mt-5 sm:mt-0 sm:ml-6 sm:flex-shrink-0 sm:flex sm:items-center">
                  <div class="flex space-x-3">
                    <button v-if="!isMember && !isCreator"
                            @click="joinTeam"
                            :disabled="actionLoading"
                            class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                      <svg v-if="actionLoading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      {{ actionLoading ? 'Joining...' : 'Join Team' }}
                    </button>

                    <button v-else-if="isMember && !isCreator"
                            @click="leaveTeam"
                            :disabled="actionLoading"
                            class="inline-flex items-center px-4 py-2 border border-red-300 text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                      <svg v-if="actionLoading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-red-700" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      {{ actionLoading ? 'Leaving...' : 'Leave Team' }}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Team Creator Info -->
          <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-sm overflow-hidden">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-medium text-[var(--color-heading)]">Team Creator</h3>
            </div>
            <div class="px-6 py-4">
              <div class="flex items-center">
                <div class="text-sm">
                  <div class="font-medium text-[var(--color-text)]">{{ team.creatorName }}</div>
                  <div class="text-[var(--color-text)] opacity-60">Team founder</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Members Tab -->
        <div v-if="activeTab === 'members'">
          <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-sm overflow-hidden">
            <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
              <h3 class="text-lg font-medium text-[var(--color-heading)]">Team Members ({{ team.members?.length || 0 }})</h3>
              <div class="flex items-center space-x-2">
                <span class="text-sm text-[var(--color-text)] opacity-60">Click on a role to see permissions</span>
              </div>
            </div>

            <div class="divide-y divide-[var(--color-border)]">
              <div v-for="member in team.members"
                   :key="member.userId"
                   class="px-6 py-4 flex items-center justify-between hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                <div class="flex items-center">
                  <img :src="member.profilePicturePath || '/img/default-avatar.png'"
                       :alt="member.userName"
                       class="h-10 w-10 rounded-full object-cover" />
                  <div class="ml-4">
                    <div class="text-sm font-medium text-[var(--color-text)]">{{ member.userName }}</div>
                    <div class="text-sm text-[var(--color-text)] opacity-60">{{ member.email }}</div>
                    <div class="flex items-center mt-1">
                      <button @click="showRolePermissions(member)"
                              class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium transition-colors duration-200 hover:bg-opacity-80"
                              :class="getRoleClass(member.role)">
                        {{ formatRole(member.role) }}
                        <svg class="ml-1 w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                        </svg>
                      </button>
                      <span class="ml-2 flex items-center text-xs text-[var(--color-text)] opacity-50">
                        <span class="w-2 h-2 rounded-full mr-1"
                              :class="{ 'bg-green-400': member.isOnline, 'bg-gray-400': !member.isOnline }"></span>
                        {{ member.isOnline ? 'Online' : 'Offline' }}
                      </span>
                    </div>
                  </div>
                </div>

                <div v-if="canManageRoles && member.userId !== team.creatorId" class="flex items-center">
                  <select :value="member.role"
                          @change="updateMemberRole(member.userId, $event.target.value)"
                          class="text-sm border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 transition-colors duration-200">
                    <option value="0">Admin</option>
                    <option value="1">Member</option>
                    <option value="2">Viewer</option>
                  </select>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Role Management Tab -->
        <div v-if="activeTab === 'roles' && canManageRoles">
          <TeamRoleManagement :teamId="team.id" />
        </div>

        <!-- Settings Tab -->
        <div v-if="activeTab === 'settings' && (isCreator || isAdmin)">
          <div class="space-y-6">
            <!-- Edit Team Settings -->
            <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-sm overflow-hidden">
              <div class="px-6 py-4 border-b border-[var(--color-border)]">
                <h3 class="text-lg font-medium text-[var(--color-heading)]">Team Settings</h3>
              </div>
              <div class="px-6 py-4">
                <form @submit.prevent="updateTeam" class="space-y-4">
                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Team Name</label>
                    <input v-model="editForm.name"
                           type="text"
                           class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 transition-colors duration-200"
                           maxlength="100"
                           required />
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Description</label>
                    <textarea v-model="editForm.description"
                              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 transition-colors duration-200 resize-vertical"
                              maxlength="500"
                              rows="4"
                              required></textarea>
                  </div>

                  <div class="flex justify-end space-x-3 pt-4">
                    <button type="submit"
                            :disabled="updateLoading"
                            class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                      <svg v-if="updateLoading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      {{ updateLoading ? 'Updating...' : 'Update Team' }}
                    </button>
                  </div>
                </form>
              </div>
            </div>

            <!-- Danger Zone -->
            <div v-if="isCreator" class="bg-[var(--color-background-soft)] border border-red-200 rounded-lg overflow-hidden">
              <div class="px-6 py-4 border-b border-red-200">
                <h3 class="text-lg font-medium text-red-800">Danger Zone</h3>
              </div>
              <div class="px-6 py-4">
                <div class="flex items-center justify-between">
                  <div>
                    <h4 class="text-sm font-medium text-red-800">Delete Team</h4>
                    <p class="text-sm text-red-600">Once deleted, this team and all its data will be permanently removed.</p>
                  </div>
                  <button @click="deleteTeam"
                          :disabled="actionLoading"
                          class="px-4 py-2 border border-red-300 text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    Delete Team
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Role Permissions Modal -->
      <div v-if="showPermissionsModal"
           class="fixed inset-0 bg-gray-600 bg-opacity-50 z-50 flex items-center justify-center p-4">
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-xl w-full max-w-lg mx-auto">
          <div class="p-6">
            <div class="flex items-center justify-between mb-6">
              <h3 class="text-lg font-medium text-[var(--color-heading)]">
                {{ selectedMember?.userName }} - {{ formatRole(selectedMember?.role) }} Permissions
              </h3>
              <button @click="showPermissionsModal = false" class="text-[var(--color-text)] opacity-50 hover:opacity-75 transition-opacity duration-200">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                </svg>
              </button>
            </div>

            <div class="space-y-3">
              <div v-for="permission in rolePermissions" :key="permission.name"
                   class="flex items-center p-3 rounded-lg"
                   :class="permission.hasPermission ? 'bg-green-50 border border-green-200' : 'bg-gray-50 border border-gray-200'">
                <svg class="w-5 h-5 mr-3"
                     :class="permission.hasPermission ? 'text-green-600' : 'text-gray-400'"
                     fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path v-if="permission.hasPermission"
                        stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                  <path v-else
                        stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                </svg>
                <div>
                  <div class="font-medium"
                       :class="permission.hasPermission ? 'text-green-800' : 'text-gray-600'">
                    {{ permission.displayName }}
                  </div>
                  <div class="text-sm opacity-75"
                       :class="permission.hasPermission ? 'text-green-700' : 'text-gray-500'">
                    {{ permission.description }}
                  </div>
                </div>
              </div>
            </div>

            <div class="mt-6 flex justify-end">
              <button @click="showPermissionsModal = false"
                      class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] transition-all duration-200">
                Close
              </button>
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
  import { useRoute, useRouter } from 'vue-router';
  import { useAuthStore } from '../../stores/authStore';
  import { teamService } from '../../services/teamService';
  import TeamRoleManagement from './TeamRoleManagement.vue';

  export default {
    name: 'TeamDetails',
    components: {
      TeamRoleManagement
    },
    props: {
      teamId: {
        type: Number,
        required: true
      }
    },
    setup(props) {
      const route = useRoute();
      const router = useRouter();
      const authStore = useAuthStore();

      const team = ref(null);
      const loading = ref(false);
      const error = ref('');
      const actionLoading = ref(false);
      const updateLoading = ref(false);
      const activeTab = ref('overview');
      const showPermissionsModal = ref(false);
      const selectedMember = ref(null);

      const editForm = reactive({
        name: '',
        description: ''
      });

      const message = reactive({
        text: '',
        type: ''
      });

      // Available permissions for role checking
      const availablePermissions = [
        { name: 'CanAddTitle', displayName: 'Add Titles', description: 'Create new manga titles' },
        { name: 'CanEditTitle', displayName: 'Edit Titles', description: 'Modify existing titles' },
        { name: 'CanDeleteTitle', displayName: 'Delete Titles', description: 'Remove titles from team' },
        { name: 'CanAddChapter', displayName: 'Add Chapters', description: 'Upload new chapters' },
        { name: 'CanEditChapter', displayName: 'Edit Chapters', description: 'Modify existing chapters' },
        { name: 'CanDeleteChapter', displayName: 'Delete Chapters', description: 'Remove chapters' },
        { name: 'CanAddMember', displayName: 'Add Members', description: 'Invite new team members' },
        { name: 'CanRemoveMember', displayName: 'Remove Members', description: 'Remove team members' },
        { name: 'CanViewAnalytics', displayName: 'View Analytics', description: 'Access team statistics' }
      ];

      // Role permissions based on your backend logic
      const rolePermissions = computed(() => {
        if (!selectedMember.value) return [];

        const role = selectedMember.value.role;
        let permissions = [];

        if (role === 0) { // Admin
          permissions = availablePermissions.map(p => p.name);
        } else if (role === 1) { // Member
          permissions = ['CanAddTitle', 'CanEditTitle', 'CanAddChapter', 'CanEditChapter'];
        } else { // Viewer
          permissions = [];
        }

        return availablePermissions.map(permission => ({
          ...permission,
          hasPermission: permissions.includes(permission.name)
        }));
      });

      const isCreator = computed(() => {
        return team.value && authStore.user && team.value.creatorId === authStore.user.id;
      });

      const isMember = computed(() => {
        return team.value && authStore.user &&
          team.value.members?.some(member => member.userId === authStore.user.id);
      });

      const isAdmin = computed(() => {
        if (!team.value || !authStore.user) return false;
        const userMembership = team.value.members?.find(member => member.userId === authStore.user.id);
        return userMembership && userMembership.role === 0; // Admin role
      });

      const canManageRoles = computed(() => {
        return isCreator.value || isAdmin.value;
      });

      const showMessage = (text, type) => {
        message.text = text;
        message.type = type;
        setTimeout(() => {
          message.text = '';
        }, type === 'success' ? 3000 : 5000);
      };

      const loadTeamDetails = async () => {
        loading.value = true;
        error.value = '';

        try {
          const result = await teamService.getTeamById(props.teamId);
          if (result.success) {
            team.value = result.data;
            editForm.name = result.data.name;
            editForm.description = result.data.description;
          } else {
            error.value = result.error;
          }
        } catch (err) {
          error.value = 'Failed to load team details';
        } finally {
          loading.value = false;
        }
      };

      const joinTeam = async () => {
        actionLoading.value = true;
        try {
          const result = await teamService.joinTeam(props.teamId);
          if (result.success) {
            showMessage('Successfully joined the team!', 'success');
            await loadTeamDetails(); // Refresh data
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to join team', 'error');
        } finally {
          actionLoading.value = false;
        }
      };

      const leaveTeam = async () => {
        if (!confirm('Are you sure you want to leave this team?')) {
          return;
        }

        actionLoading.value = true;
        try {
          const result = await teamService.leaveTeam(props.teamId);
          if (result.success) {
            showMessage('Successfully left the team', 'success');
            setTimeout(() => {
              router.push('/teams');
            }, 1500);
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to leave team', 'error');
        } finally {
          actionLoading.value = false;
        }
      };

      const updateTeam = async () => {
        updateLoading.value = true;
        try {
          const result = await teamService.updateTeam(props.teamId, {
            name: editForm.name,
            description: editForm.description
          });

          if (result.success) {
            showMessage('Team updated successfully!', 'success');
            await loadTeamDetails(); // Refresh data
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to update team', 'error');
        } finally {
          updateLoading.value = false;
        }
      };

      const deleteTeam = async () => {
        if (!confirm('Are you sure you want to delete this team? This action cannot be undone.')) {
          return;
        }

        actionLoading.value = true;
        try {
          const result = await teamService.deleteTeam(props.teamId);
          if (result.success) {
            showMessage('Team deleted successfully', 'success');
            setTimeout(() => {
              router.push('/teams');
            }, 1500);
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to delete team', 'error');
        } finally {
          actionLoading.value = false;
        }
      };

      const updateMemberRole = async (userId, newRole) => {
        try {
          const result = await teamService.updateMemberRole(props.teamId, userId, parseInt(newRole));
          if (result.success) {
            showMessage('Member role updated successfully', 'success');
            await loadTeamDetails(); // Refresh data
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to update member role', 'error');
        }
      };

      const showRolePermissions = (member) => {
        selectedMember.value = member;
        showPermissionsModal.value = true;
      };

      const formatRole = (role) => {
        const roles = {
          0: 'Admin',
          1: 'Member',
          2: 'Viewer'
        };
        return roles[role] || 'Unknown';
      };

      const getRoleClass = (role) => {
        const classes = {
          0: 'bg-red-100 text-red-800',
          1: 'bg-blue-100 text-blue-800',
          2: 'bg-gray-100 text-gray-800'
        };
        return classes[role] || 'bg-gray-100 text-gray-800';
      };

      onMounted(() => {
        loadTeamDetails();
      });

      return {
        team,
        loading,
        error,
        actionLoading,
        updateLoading,
        activeTab,
        showPermissionsModal,
        selectedMember,
        editForm,
        message,
        rolePermissions,
        isCreator,
        isMember,
        isAdmin,
        canManageRoles,
        loadTeamDetails,
        joinTeam,
        leaveTeam,
        updateTeam,
        deleteTeam,
        updateMemberRole,
        showRolePermissions,
        formatRole,
        getRoleClass
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
</style>
