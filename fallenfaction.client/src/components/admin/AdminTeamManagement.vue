<!-- components/admin/AdminTeamManagement.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Team Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Manage teams, members, and permissions</p>
      </div>

      <!-- Statistics Cards -->
      <div v-if="statistics" class="grid grid-cols-1 md:grid-cols-5 gap-4 mb-8">
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-4">
          <div class="text-2xl font-bold text-[var(--color-heading)]">{{ statistics.totalTeams }}</div>
          <div class="text-sm text-[var(--color-text)] opacity-75">Total Teams</div>
        </div>
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-4">
          <div class="text-2xl font-bold text-[var(--color-heading)]">{{ statistics.totalMembers }}</div>
          <div class="text-sm text-[var(--color-text)] opacity-75">Total Members</div>
        </div>
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-4">
          <div class="text-2xl font-bold text-[var(--color-heading)]">{{ statistics.averageMembersPerTeam }}</div>
          <div class="text-sm text-[var(--color-text)] opacity-75">Avg Members/Team</div>
        </div>
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-4">
          <div class="text-2xl font-bold text-green-600">{{ statistics.teamsWithTitles }}</div>
          <div class="text-sm text-[var(--color-text)] opacity-75">Active Teams</div>
        </div>
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-4">
          <div class="text-2xl font-bold text-orange-600">{{ statistics.teamsWithoutTitles }}</div>
          <div class="text-sm text-[var(--color-text)] opacity-75">Inactive Teams</div>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-5 gap-8">
        <!-- Left Column: Team Actions -->
        <div class="lg:col-span-2">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)]">Team Actions</h3>
            </div>

            <div class="p-6 space-y-6">
              <!-- Search by Team ID -->
              <div>
                <label for="teamIdSearch" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                  Search by Team ID
                </label>
                <div class="flex gap-2">
                  <input id="teamIdSearch"
                         v-model="teamIdSearch"
                         type="number"
                         class="flex-1 px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200"
                         placeholder="Enter Team ID" />
                  <button @click="searchTeamById"
                          :disabled="!teamIdSearch || isLoadingTeamDetails"
                          class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    <span v-if="isLoadingTeamDetails">Loading...</span>
                    <span v-else>Search</span>
                  </button>
                </div>
              </div>

              <!-- Selected Team Info -->
              <div v-if="selectedTeam" class="bg-[var(--color-background-mute)] p-4 rounded-md border border-[var(--color-border)]">
                <h4 class="text-sm font-medium text-[var(--color-text)] mb-3">Selected Team</h4>
                <div class="space-y-2 text-sm">
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">ID:</span>
                    <span class="text-[var(--color-text)]">{{ selectedTeam.id }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Name:</span>
                    <span class="text-[var(--color-text)]">{{ selectedTeam.name }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Creator:</span>
                    <span class="text-[var(--color-text)]">{{ selectedTeam.creatorName }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Members:</span>
                    <span class="text-[var(--color-text)]">{{ selectedTeam.memberCount }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-[var(--color-text)] opacity-75">Titles:</span>
                    <span class="text-[var(--color-text)]">{{ selectedTeam.titleCount }}</span>
                  </div>
                </div>
              </div>

              <!-- Edit Team Form -->
              <div v-if="selectedTeam" class="space-y-4">
                <h4 class="text-sm font-medium text-[var(--color-text)]">Edit Team</h4>
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Team Name</label>
                  <input v-model="editForm.name"
                         type="text"
                         class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200"
                         maxlength="100" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Description</label>
                  <textarea v-model="editForm.description"
                            class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 resize-vertical"
                            maxlength="500"
                            rows="3"></textarea>
                </div>
                <div class="flex space-x-2">
                  <button @click="updateTeam"
                          :disabled="loading || !editForm.name.trim()"
                          class="flex-1 px-3 py-2 bg-green-600 text-white text-sm font-medium rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    Update Team
                  </button>
                  <button @click="clearSelection"
                          class="flex-1 px-3 py-2 border border-[var(--color-border)] text-sm font-medium rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] transition-colors duration-200">
                    Clear
                  </button>
                </div>
              </div>

              <!-- Danger Zone -->
              <div v-if="selectedTeam" class="space-y-3">
                <h4 class="text-sm font-medium text-red-600">Danger Zone</h4>
                <button @click="deleteTeam"
                        :disabled="loading"
                        class="w-full px-4 py-2 bg-red-600 text-white text-sm font-medium rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                  Delete Team Permanently
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Column: Team List -->
        <div class="lg:col-span-3">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4">Team List</h3>
              <input type="text"
                     v-model="searchQuery"
                     @input="handleSearch"
                     placeholder="Search by team name or description..."
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
            </div>

            <!-- Loading State -->
            <div v-if="isLoadingTeams" class="p-6 text-center">
              <div class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span class="text-[var(--color-text)]">Loading teams...</span>
              </div>
            </div>

            <!-- Teams Table -->
            <div v-else class="overflow-x-auto max-h-96">
              <table class="min-w-full divide-y divide-[var(--color-border)]">
                <thead class="bg-[var(--color-background-mute)] sticky top-0">
                  <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">ID</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Name</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Creator</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Members</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Titles</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
                  <tr v-for="team in displayedTeams"
                      :key="team.id"
                      class="hover:bg-[var(--color-background-mute)] transition-colors duration-200"
                      :class="{ 'bg-blue-50 border-l-4 border-l-blue-500': selectedTeam?.id === team.id }">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                      {{ team.id }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      {{ team.name }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      {{ team.creatorName }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      {{ team.memberCount }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                            :class="team.titleCount > 0 ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'">
                        {{ team.titleCount }}
                      </span>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <div class="flex space-x-2">
                        <button @click="selectTeam(team)"
                                class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-blue-700 bg-blue-100 hover:bg-blue-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors duration-200">
                          Select
                        </button>
                        <button @click="viewMembers(team)"
                                class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-green-700 bg-green-100 hover:bg-green-200 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
                          Members
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>

              <!-- No Results -->
              <div v-if="displayedTeams.length === 0 && !isLoadingTeams" class="text-center py-8">
                <svg class="mx-auto h-8 w-8 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-1.657-.126-3.153-.356-4.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-1.657.126-3.153.356-4.857m0 0a5.002 5.002 0 019.288 0" />
                </svg>
                <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No teams found</h3>
                <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">{{ searchQuery ? 'Try adjusting your search terms' : 'No teams available' }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Members Modal -->
      <div v-if="showMembersModal" class="fixed inset-0 bg-[var(--color-background)] bg-opacity-50 z-50 flex items-center justify-center p-4">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
          <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Team Members - {{ membersTeam?.name }}</h3>
            <button @click="closeMembersModal" class="text-[var(--color-text)] opacity-50 hover:opacity-75 transition-opacity duration-200">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>

          <div class="p-6">
            <div class="overflow-x-auto">
              <table class="min-w-full divide-y divide-[var(--color-border)]">
                <thead class="bg-[var(--color-background-mute)]">
                  <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">User</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Email</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Role</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Status</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
                  <tr v-for="member in membersTeam?.members || []" :key="member.userId">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                      {{ member.userName }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      {{ member.email }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      <select v-if="member.userId !== membersTeam.creatorId"
                              :value="member.role"
                              @change="updateMemberRole(member.userId, parseInt($event.target.value))"
                              class="text-sm border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 transition-colors duration-200">
                        <option value="0">Admin</option>
                        <option value="1">Member</option>
                        <option value="2">Viewer</option>
                      </select>
                      <span v-else class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
                        Creator
                      </span>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                      <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                            :class="member.isOnline ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'">
                        {{ member.isOnline ? 'Online' : 'Offline' }}
                      </span>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button v-if="member.userId !== membersTeam.creatorId"
                              @click="removeMember(member.userId, member.userName)"
                              class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-red-700 bg-red-100 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                        Remove
                      </button>
                      <span v-else class="text-xs text-[var(--color-text)] opacity-50">Creator</span>
                    </td>
                  </tr>
                </tbody>
              </table>
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
  import { adminTeamService } from '../../services/adminTeamService';

  export default {
    name: 'AdminTeamManagement',
    setup() {
      const teams = ref([]);
      const searchResults = ref([]);
      const searchQuery = ref('');
      const teamIdSearch = ref('');
      const selectedTeam = ref(null);
      const statistics = ref(null);
      const isLoadingTeams = ref(true);
      const isLoadingTeamDetails = ref(false);
      const loading = ref(false);

      // Members modal
      const showMembersModal = ref(false);
      const membersTeam = ref(null);

      const editForm = reactive({
        name: '',
        description: ''
      });

      const message = reactive({
        text: '',
        type: ''
      });

      let searchTimeout = null;

      const displayedTeams = computed(() => {
        return searchQuery.value ? searchResults.value : teams.value;
      });

      const showMessage = (text, type) => {
        message.text = text;
        message.type = type;
        setTimeout(() => {
          message.text = '';
        }, type === 'success' ? 3000 : 5000);
      };

      const loadTeams = async () => {
        isLoadingTeams.value = true;
        try {
          const result = await adminTeamService.getAllTeams();
          if (result.success) {
            teams.value = result.data;
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to load teams', 'error');
        } finally {
          isLoadingTeams.value = false;
        }
      };

      const loadStatistics = async () => {
        try {
          const result = await adminTeamService.getTeamStatistics();
          if (result.success) {
            statistics.value = result.data;
          }
        } catch (error) {
          console.error('Failed to load statistics:', error);
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
            const result = await adminTeamService.searchTeams(searchQuery.value.trim());
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

      const searchTeamById = async () => {
        if (!teamIdSearch.value) return;

        isLoadingTeamDetails.value = true;
        try {
          const result = await adminTeamService.getTeamById(teamIdSearch.value);
          if (result.success) {
            selectedTeam.value = result.data;
            editForm.name = result.data.name;
            editForm.description = result.data.description;
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to fetch team details', 'error');
        } finally {
          isLoadingTeamDetails.value = false;
        }
      };

      const selectTeam = (team) => {
        selectedTeam.value = team;
        teamIdSearch.value = team.id;
        editForm.name = team.name;
        editForm.description = team.description;
      };

      const clearSelection = () => {
        selectedTeam.value = null;
        teamIdSearch.value = '';
        editForm.name = '';
        editForm.description = '';
      };

      const updateTeam = async () => {
        if (!selectedTeam.value || !editForm.name.trim()) return;

        loading.value = true;
        try {
          const result = await adminTeamService.updateTeam(selectedTeam.value.id, {
            name: editForm.name.trim(),
            description: editForm.description.trim()
          });

          if (result.success) {
            showMessage(result.message, 'success');
            selectedTeam.value.name = editForm.name;
            selectedTeam.value.description = editForm.description;
            await loadTeams();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to update team', 'error');
        } finally {
          loading.value = false;
        }
      };

      const deleteTeam = async () => {
        if (!selectedTeam.value || !confirm(`Are you sure you want to permanently delete team "${selectedTeam.value.name}"? This action cannot be undone.`)) {
          return;
        }

        loading.value = true;
        try {
          const result = await adminTeamService.deleteTeam(selectedTeam.value.id);
          if (result.success) {
            showMessage(result.message, 'success');
            clearSelection();
            await loadTeams();
            await loadStatistics();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to delete team', 'error');
        } finally {
          loading.value = false;
        }
      };

      const viewMembers = async (team) => {
        try {
          const result = await adminTeamService.getTeamById(team.id);
          if (result.success) {
            membersTeam.value = result.data;
            showMembersModal.value = true;
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to load team members', 'error');
        }
      };

      const closeMembersModal = () => {
        showMembersModal.value = false;
        membersTeam.value = null;
      };

      const updateMemberRole = async (userId, newRole) => {
        if (!membersTeam.value) return;

        try {
          const result = await adminTeamService.updateMemberRole(membersTeam.value.id, userId, newRole);
          if (result.success) {
            showMessage(result.message, 'success');
            // Update the member role in the local data
            const member = membersTeam.value.members.find(m => m.userId === userId);
            if (member) {
              member.role = newRole;
            }
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to update member role', 'error');
        }
      };

      const removeMember = async (userId, userName) => {
        if (!membersTeam.value || !confirm(`Are you sure you want to remove "${userName}" from this team?`)) {
          return;
        }

        try {
          const result = await adminTeamService.removeMember(membersTeam.value.id, userId);
          if (result.success) {
            showMessage(result.message, 'success');
            // Remove the member from local data
            membersTeam.value.members = membersTeam.value.members.filter(m => m.userId !== userId);
            membersTeam.value.memberCount--;
            await loadTeams();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to remove member', 'error');
        }
      };

      onMounted(() => {
        loadTeams();
        loadStatistics();
      });

      return {
        teams,
        searchResults,
        searchQuery,
        teamIdSearch,
        selectedTeam,
        statistics,
        isLoadingTeams,
        isLoadingTeamDetails,
        loading,
        showMembersModal,
        membersTeam,
        editForm,
        message,
        displayedTeams,
        loadTeams,
        handleSearch,
        searchTeamById,
        selectTeam,
        clearSelection,
        updateTeam,
        deleteTeam,
        viewMembers,
        closeMembersModal,
        updateMemberRole,
        removeMember
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
