<!-- components/team/TeamList.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 class="text-3xl font-bold text-[var(--color-heading)]">Teams</h1>
          <p class="mt-2 text-[var(--color-text)] opacity-75">Discover and join translation teams</p>
        </div>
        <div class="mt-4 sm:mt-0">
          <router-link to="/team/addteam"
                       class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition-all duration-200">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Create Team
          </router-link>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mb-6">
        <div class="border-b border-[var(--color-border)]">
          <nav class="-mb-px flex space-x-8">
            <button @click="activeTab = 'all'"
                    class="py-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
                    :class="{
                'border-green-500 text-green-600': activeTab === 'all',
                'border-transparent text-[var(--color-text)] opacity-75 hover:text-[var(--color-heading)] hover:border-[var(--color-border-hover)]': activeTab !== 'all'
              }">
              All Teams
            </button>
            <button @click="activeTab = 'my'"
                    class="py-2 px-1 border-b-2 font-medium text-sm transition-colors duration-200"
                    :class="{
                'border-green-500 text-green-600': activeTab === 'my',
                'border-transparent text-[var(--color-text)] opacity-75 hover:text-[var(--color-heading)] hover:border-[var(--color-border-hover)]': activeTab !== 'my'
              }">
              My Teams
            </button>
          </nav>
        </div>
      </div>

      <!-- Search -->
      <div class="mb-6">
        <div class="max-w-md">
          <div class="relative">
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <svg class="h-5 w-5 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m21 21-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
            <input v-model="searchQuery"
                   type="text"
                   placeholder="Search teams..."
                   class="block w-full pl-10 pr-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] placeholder-[var(--color-text)] placeholder-opacity-50 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading teams...</span>
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
            <h3 class="text-sm font-medium text-red-800">Error loading teams</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
            <div class="mt-4">
              <button @click="loadTeams"
                      class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                Try Again
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Teams Grid -->
      <div v-else>
        <!-- Empty State -->
        <div v-if="filteredTeams.length === 0" class="text-center py-12">
          <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-1.657-.126-3.153-.356-4.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-1.657.126-3.153.356-4.857m0 0a5.002 5.002 0 019.288 0" />
          </svg>
          <h3 class="mt-2 text-lg font-medium text-[var(--color-text)]">No teams found</h3>
          <p class="mt-1 text-[var(--color-text)] opacity-75">
            <span v-if="searchQuery">Try adjusting your search terms</span>
            <span v-else-if="activeTab === 'my'">You haven't joined any teams yet</span>
            <span v-else>No teams have been created yet</span>
          </p>
          <div v-if="activeTab === 'all'" class="mt-6">
            <router-link to="/team/addteam"
                         class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition-all duration-200">
              Create the First Team
            </router-link>
          </div>
        </div>

        <!-- Teams Grid -->
        <div v-else class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          <TeamCard v-for="team in filteredTeams"
                    :key="team.id"
                    :team="team"
                    @join="handleJoinTeam"
                    @leave="handleLeaveTeam"
                    @view="handleViewTeam" />
        </div>
      </div>

      <!-- Success/Error Messages -->
      <div v-if="message.text"
           class="fixed top-4 right-4 max-w-sm w-full bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-lg z-50 transition-all duration-300"
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
  import { ref, reactive, computed, onMounted, watch } from 'vue';
  import { useRouter } from 'vue-router';
  import { teamService } from '../../services/teamService';
  import TeamCard from './TeamCard.vue';

  export default {
    name: 'TeamList',
    components: {
      TeamCard
    },
    setup() {
      const router = useRouter();

      const teams = ref([]);
      const myTeams = ref([]);
      const loading = ref(false);
      const error = ref('');
      const activeTab = ref('all');
      const searchQuery = ref('');

      const message = reactive({
        text: '',
        type: ''
      });

      const filteredTeams = computed(() => {
        const currentTeams = activeTab.value === 'my' ? myTeams.value : teams.value;

        if (!searchQuery.value) {
          return currentTeams;
        }

        const query = searchQuery.value.toLowerCase();
        return currentTeams.filter(team =>
          team.name.toLowerCase().includes(query) ||
          team.description.toLowerCase().includes(query) ||
          team.creatorName?.toLowerCase().includes(query)
        );
      });

      const loadTeams = async () => {
        loading.value = true;
        error.value = '';

        try {
          if (activeTab.value === 'all') {
            const result = await teamService.getAllTeams();
            if (result.success) {
              teams.value = result.data;
            } else {
              error.value = result.error;
            }
          } else {
            const result = await teamService.getMyTeams();
            if (result.success) {
              myTeams.value = result.data;
            } else {
              error.value = result.error;
            }
          }
        } catch (err) {
          error.value = 'Failed to load teams';
        } finally {
          loading.value = false;
        }
      };

      const showMessage = (text, type) => {
        message.text = text;
        message.type = type;
        setTimeout(() => {
          message.text = '';
        }, type === 'success' ? 3000 : 5000);
      };

      const handleJoinTeam = async (teamId) => {
        try {
          const result = await teamService.joinTeam(teamId);
          if (result.success) {
            showMessage('Successfully joined the team!', 'success');
            loadTeams(); // Refresh the list
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to join team', 'error');
        }
      };

      const handleLeaveTeam = async (teamId) => {
        if (!confirm('Are you sure you want to leave this team?')) {
          return;
        }

        try {
          const result = await teamService.leaveTeam(teamId);
          if (result.success) {
            showMessage('Successfully left the team', 'success');
            loadTeams(); // Refresh the list
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to leave team', 'error');
        }
      };

      const handleViewTeam = (teamId) => {
        router.push(`/team/${teamId}`);
      };

      // Watch for tab changes to load appropriate data
      watch(activeTab, () => {
        loadTeams();
      });

      onMounted(() => {
        loadTeams();
      });

      return {
        teams,
        myTeams,
        loading,
        error,
        activeTab,
        searchQuery,
        message,
        filteredTeams,
        loadTeams,
        handleJoinTeam,
        handleLeaveTeam,
        handleViewTeam
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
