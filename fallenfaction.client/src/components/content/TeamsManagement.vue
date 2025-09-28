<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center">
      <div>
        <h3 class="text-lg font-medium text-[var(--color-heading)]">My Teams</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">Teams you are part of and manage</p>
      </div>
      <router-link to="/team/addteam"
                   class="inline-flex items-center px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
        <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
        </svg>
        Create New Team
      </router-link>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center py-12">
      <div class="inline-flex items-center">
        <svg class="animate-spin -ml-1 mr-3 h-6 w-6 text-[var(--color-accent)]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="text-[var(--color-text)]">Loading teams...</span>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="teams.length === 0" class="text-center py-12">
      <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"></path>
      </svg>
      <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No teams yet</h3>
      <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">Get started by creating your first team.</p>
      <div class="mt-6">
        <router-link to="/team/addteam"
                     class="inline-flex items-center px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
          </svg>
          Create Team
        </router-link>
      </div>
    </div>

    <!-- Teams Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div v-for="team in teams" :key="team.id" class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg overflow-hidden hover:shadow-lg transition-shadow duration-200">
        <!-- Team Header -->
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center">
              <div class="w-12 h-12 bg-gradient-to-r from-blue-500 to-purple-600 rounded-lg flex items-center justify-center text-white font-bold text-lg">
                {{ getTeamInitials(team.name) }}
              </div>
              <div class="ml-3">
                <h4 class="text-lg font-semibold text-[var(--color-heading)]">{{ team.name }}</h4>
                <p class="text-sm text-[var(--color-text)] opacity-75">{{ getTeamRole(team) }}</p>
              </div>
            </div>
            <div class="relative" ref="dropdownRefs">
              <button @click="toggleDropdown(team.id)"
                      class="p-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] rounded-lg transition-colors duration-200">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01"></path>
                </svg>
              </button>

              <!-- Dropdown Menu -->
              <div v-if="openDropdown === team.id"
                   class="absolute right-0 top-full mt-1 w-48 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-lg z-10">
                <router-link :to="`/user/content/editteam/${team.id}`"
                             @click="closeDropdown"
                             class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                  <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                  </svg>
                  Manage Team
                </router-link>
                <router-link :to="`/team/${team.id}`"
                             @click="closeDropdown"
                             class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                  <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                  </svg>
                  View Team
                </router-link>
                <button v-if="canDeleteTeam(team)"
                        @click="deleteTeam(team.id)"
                        class="flex items-center w-full px-4 py-2 text-red-600 hover:bg-red-50 transition-colors duration-150">
                  <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                  </svg>
                  Delete Team
                </button>
              </div>
            </div>
          </div>

          <p class="text-sm text-[var(--color-text)] opacity-75 mb-4 line-clamp-2">
            {{ team.description || 'No description available' }}
          </p>

          <!-- Team Stats -->
          <div class="grid grid-cols-3 gap-4 mb-4">
            <div class="text-center">
              <div class="text-lg font-semibold text-[var(--color-text)]">{{ team.memberCount || 0 }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-60">Members</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-semibold text-[var(--color-text)]">{{ team.titleCount || 0 }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-60">Titles</div>
            </div>
            <div class="text-center">
              <div class="text-lg font-semibold text-[var(--color-text)]">{{ team.chapterCount || 0 }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-60">Chapters</div>
            </div>
          </div>

          <!-- Team Members Preview -->
          <div v-if="team.members && team.members.length > 0" class="mb-4">
            <div class="text-xs text-[var(--color-text)] opacity-60 mb-2">Recent Members</div>
            <div class="flex -space-x-2 overflow-hidden">
              <img v-for="member in team.members.slice(0, 4)"
                   :key="member.id"
                   :src="getProfileImage(member.profilePicturePath)"
                   :alt="member.userName"
                   :title="member.userName"
                   class="inline-block h-8 w-8 rounded-full ring-2 ring-[var(--color-background)] object-cover" />
              <div v-if="team.members.length > 4"
                   class="inline-flex h-8 w-8 items-center justify-center rounded-full ring-2 ring-[var(--color-background)] bg-[var(--color-background-mute)] text-xs font-medium text-[var(--color-text)]">
                +{{ team.members.length - 4 }}
              </div>
            </div>
          </div>

          <!-- Team Status -->
          <div class="flex items-center justify-between">
            <span :class="[
              'inline-flex px-2 py-1 text-xs font-semibold rounded-full',
              getTeamStatusColor(team)
            ]">
              {{ getTeamStatus(team) }}
            </span>
            <span class="text-xs text-[var(--color-text)] opacity-60">
              Created {{ formatDate(team.createdAt) }}
            </span>
          </div>
        </div>

        <!-- Team Actions -->
        <div class="px-6 py-3 bg-[var(--color-background-mute)] border-t border-[var(--color-border)]">
          <div class="flex justify-between items-center">
            <router-link :to="`/team/${team.id}`"
                         class="text-sm text-[var(--color-accent)] hover:text-[var(--color-accent-hover)] font-medium">
              View Details
            </router-link>
            <router-link :to="`/user/content/editteam/${team.id}`"
                         class="inline-flex items-center px-3 py-1 bg-[var(--color-accent)] text-white text-sm rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
              <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
              </svg>
              Manage
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- Recent Activity (if available) -->
    <div v-if="teams.length > 0" class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-6">
      <h4 class="text-lg font-medium text-[var(--color-heading)] mb-4">Recent Team Activity</h4>

      <div v-if="recentActivity.length === 0" class="text-center py-8">
        <svg class="mx-auto h-8 w-8 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <p class="mt-2 text-sm text-[var(--color-text)] opacity-75">No recent activity</p>
      </div>

      <div v-else class="space-y-3">
        <div v-for="activity in recentActivity" :key="activity.id"
             class="flex items-center p-3 bg-[var(--color-background-mute)] rounded-lg">
          <div class="flex-shrink-0">
            <div :class="[
              'w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-medium',
              getActivityColor(activity.type)
            ]">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path v-if="activity.type === 'chapter'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                <path v-else-if="activity.type === 'member'" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
              </svg>
            </div>
          </div>
          <div class="ml-3 flex-1">
            <p class="text-sm text-[var(--color-text)]">{{ activity.description }}</p>
            <p class="text-xs text-[var(--color-text)] opacity-60">{{ formatDate(activity.createdAt) }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { teamService } from '../../services/teamService'

// Props
const props = defineProps({
  teams: {
    type: Array,
    default: () => []
  }
})

// Emits
const emit = defineEmits(['refresh'])

// Reactive data
const loading = ref(false)
const openDropdown = ref(null)
const recentActivity = ref([])

// Methods
const getTeamInitials = (name) => {
  return name
    .split(' ')
    .map(word => word.charAt(0).toUpperCase())
    .slice(0, 2)
    .join('')
}

const getTeamRole = (team) => {
  // This would come from the API based on user's role in the team
  if (team.isOwner) return 'Owner'
  if (team.isAdmin) return 'Admin'
  if (team.isModerator) return 'Moderator'
  return 'Member'
}

const getProfileImage = (imagePath) => {
  if (!imagePath) return '/img/logo.png'
  if (imagePath.startsWith('http')) return imagePath
  return imagePath.startsWith('/') ? imagePath : `/${imagePath}`
}

const getTeamStatus = (team) => {
  if (team.isActive === false) return 'Inactive'
  if (team.memberCount === 1) return 'Solo'
  if (team.chapterCount > 0) return 'Active'
  return 'New'
}

const getTeamStatusColor = (team) => {
  const status = getTeamStatus(team)
  switch (status) {
    case 'Active':
      return 'bg-green-100 text-green-800'
    case 'Solo':
      return 'bg-blue-100 text-blue-800'
    case 'New':
      return 'bg-yellow-100 text-yellow-800'
    case 'Inactive':
      return 'bg-red-100 text-red-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

const getActivityColor = (type) => {
  switch (type) {
    case 'chapter':
      return 'bg-blue-500'
    case 'member':
      return 'bg-green-500'
    case 'title':
      return 'bg-purple-500'
    default:
      return 'bg-gray-500'
  }
}

const formatDate = (date) => {
  if (!date) return 'Unknown'
  return new Date(date).toLocaleDateString()
}

const canDeleteTeam = (team) => {
  // Only team owners can delete teams, and only if they have no active content
  return team.isOwner && team.chapterCount === 0
}

const toggleDropdown = (teamId) => {
  openDropdown.value = openDropdown.value === teamId ? null : teamId
}

const closeDropdown = () => {
  openDropdown.value = null
}

const deleteTeam = async (teamId) => {
  if (!confirm('Are you sure you want to delete this team? This action cannot be undone.')) {
    return
  }

  try {
    const result = await teamService.deleteTeam(teamId)
    if (result.success) {
      emit('refresh')
    } else {
      alert(result.error || 'Failed to delete team')
    }
  } catch (error) {
    console.error('Error deleting team:', error)
    alert('Failed to delete team')
  }
}

const loadRecentActivity = async () => {
  try {
    // This would load recent activity for user's teams
    // For now, we'll use mock data
    recentActivity.value = [
      {
        id: 1,
        type: 'chapter',
        description: 'New chapter published for "Sample Manga"',
        createdAt: new Date().toISOString()
      },
      {
        id: 2,
        type: 'member',
        description: 'New member joined the team',
        createdAt: new Date(Date.now() - 86400000).toISOString()
      }
    ]
  } catch (error) {
    console.error('Error loading team activity:', error)
  }
}

// Handle click outside to close dropdown
const handleClickOutside = (event) => {
  if (openDropdown.value && !event.target.closest('[ref="dropdownRefs"]')) {
    closeDropdown()
  }
}

// Lifecycle
onMounted(() => {
  loadRecentActivity()
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
  .line-clamp-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
</style>
