<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Content Management</h1>
        <p class="text-[var(--color-text)] opacity-75 mt-2">Manage your uploads, chapters, and teams</p>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="flex items-center justify-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-[var(--color-accent)]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading content...</span>
        </div>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-md p-4 mb-6">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3">
            <h3 class="text-sm font-medium text-red-800">Error</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Content Tabs -->
      <div v-else class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
        <!-- Tab Navigation -->
        <div class="border-b border-[var(--color-border)]">
          <div class="overflow-x-auto scrollbar-hide">
            <nav class="flex min-w-max px-6" aria-label="Tabs">
              <button v-for="tab in availableTabs"
                      :key="tab.id"
                      @click="activeTab = tab.id"
                      :class="[
                        'whitespace-nowrap py-4 px-1 mr-8 border-b-2 font-medium text-sm transition-colors duration-200 shrink-0',
                        activeTab === tab.id
                          ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
                          : 'border-transparent text-[var(--color-text)] opacity-70 hover:opacity-100 hover:border-[var(--color-border-hover)]'
                      ]">
                {{ tab.label }}
                <span v-if="tab.count !== undefined"
                      :class="[
                        'ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                        activeTab === tab.id
                          ? 'bg-[var(--color-accent)] text-white'
                          : 'bg-[var(--color-background-mute)] text-[var(--color-text)]'
                      ]">
                  {{ tab.count }}
                </span>
              </button>
            </nav>
          </div>
        </div>

        <!-- Tab Content -->
        <div class="p-6">
          <!-- Overview Tab -->
          <div v-if="activeTab === 'overview'" class="space-y-6">
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              <!-- Stats Cards -->
              <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4">
                <div class="flex items-center">
                  <div class="flex-shrink-0">
                    <svg class="h-8 w-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
                    </svg>
                  </div>
                  <div class="ml-4">
                    <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Total Titles</p>
                    <p class="text-2xl font-semibold text-[var(--color-text)]">{{ stats.totalTitles }}</p>
                  </div>
                </div>
              </div>

              <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4">
                <div class="flex items-center">
                  <div class="flex-shrink-0">
                    <svg class="h-8 w-8 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                  </div>
                  <div class="ml-4">
                    <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Pending Review</p>
                    <p class="text-2xl font-semibold text-[var(--color-text)]">{{ stats.pendingTitles }}</p>
                  </div>
                </div>
              </div>

              <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4">
                <div class="flex items-center">
                  <div class="flex-shrink-0">
                    <svg class="h-8 w-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                    </svg>
                  </div>
                  <div class="ml-4">
                    <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Total Chapters</p>
                    <p class="text-2xl font-semibold text-[var(--color-text)]">{{ stats.totalChapters }}</p>
                  </div>
                </div>
              </div>

              <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4">
                <div class="flex items-center">
                  <div class="flex-shrink-0">
                    <svg class="h-8 w-8 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"></path>
                    </svg>
                  </div>
                  <div class="ml-4">
                    <p class="text-sm font-medium text-[var(--color-text)] opacity-60">My Teams</p>
                    <p class="text-2xl font-semibold text-[var(--color-text)]">{{ stats.totalTeams }}</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Quick Actions -->
            <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-6">
              <h3 class="text-lg font-medium text-[var(--color-heading)] mb-4">Quick Actions</h3>
              <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <router-link to="/manga/addtitle"
                             class="flex items-center p-4 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-soft)] transition-colors duration-200">
                  <svg class="h-6 w-6 text-[var(--color-accent)] mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                  </svg>
                  <span class="text-[var(--color-text)] font-medium">Add Title</span>
                </router-link>

                <router-link to="/team/addteam"
                             class="flex items-center p-4 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-soft)] transition-colors duration-200">
                  <svg class="h-6 w-6 text-[var(--color-accent)] mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"></path>
                  </svg>
                  <span class="text-[var(--color-text)] font-medium">Create Team</span>
                </router-link>

                <router-link to="/author/createa"
                             class="flex items-center p-4 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-soft)] transition-colors duration-200">
                  <svg class="h-6 w-6 text-[var(--color-accent)] mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                  </svg>
                  <span class="text-[var(--color-text)] font-medium">Add Author</span>
                </router-link>

                <router-link to="/publisher/create"
                             class="flex items-center p-4 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-soft)] transition-colors duration-200">
                  <svg class="h-6 w-6 text-[var(--color-accent)] mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"></path>
                  </svg>
                  <span class="text-[var(--color-text)] font-medium">Add Publisher</span>
                </router-link>
              </div>
            </div>
          </div>

          <!-- Titles Tab -->
          <TitlesManagement v-else-if="activeTab === 'titles'"
                            :titles="userTitles"
                            :pendingTitles="pendingTitles"
                            :rejectedTitles="rejectedTitles"
                            @refresh="loadUserContent" />

          <!-- Chapters Tab -->
          <ChaptersManagement v-else-if="activeTab === 'chapters'"
                              :chapters="userChapters"
                              @refresh="loadUserContent" />

          <!-- Teams Tab -->
          <TeamsManagement v-else-if="activeTab === 'teams'"
                           :teams="userTeams"
                           @refresh="loadUserContent" />

          <!-- Moderation Tab (Admin/Moderator only) -->
          <ModerationManagement v-else-if="activeTab === 'moderation'"
                                :pendingContent="pendingContent"
                                @refresh="loadPendingContent" />
        </div>
      </div>

      <!-- Success/Error Messages -->
      <div v-if="successMessage" class="fixed bottom-4 right-4 bg-green-50 border border-green-200 rounded-md p-4 shadow-lg z-50">
        <div class="flex">
          <svg class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
          </svg>
          <div class="ml-3">
            <p class="text-sm font-medium text-green-800">{{ successMessage }}</p>
          </div>
          <button @click="successMessage = ''" class="ml-auto text-green-400 hover:text-green-600">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useAuthStore } from '../../stores/authStore'
import contentService from '../../services/contentService'

// Import child components
import TitlesManagement from './TitlesManagement.vue'
import ChaptersManagement from './ChaptersManagement.vue'
import TeamsManagement from './TeamsManagement.vue'
import ModerationManagement from './ModerationManagement.vue'

// Auth store
const authStore = useAuthStore()

// Reactive data
const loading = ref(true)
const error = ref('')
const successMessage = ref('')
const activeTab = ref('overview')

// Content data
const userTitles = ref([])
const pendingTitles = ref([])
const rejectedTitles = ref([])
const userChapters = ref({})
const userTeams = ref([])
const pendingContent = ref({})

// Computed properties
const stats = computed(() => ({
  totalTitles: userTitles.value.length,
  pendingTitles: pendingTitles.value.length,
  totalChapters: (userChapters.value.pendingChapters?.length || 0) +
                (userChapters.value.approvedChapters?.length || 0),
  totalTeams: userTeams.value.length
}))

const availableTabs = computed(() => {
  const baseTabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'titles', label: 'My Titles', count: stats.value.totalTitles },
    { id: 'chapters', label: 'My Chapters', count: stats.value.totalChapters },
    { id: 'teams', label: 'My Teams', count: stats.value.totalTeams }
  ]

  // Add moderation tab for admins/moderators
  if (authStore.isAdmin || authStore.isModerator) {
    baseTabs.push({
      id: 'moderation',
      label: 'Moderation',
      count: (pendingContent.value.pendingTitles?.length || 0) +
             (pendingContent.value.pendingChapters?.length || 0)
    })
  }

  return baseTabs
})

// Methods
const loadUserContent = async () => {
  loading.value = true
  error.value = ''

  try {
    const [titlesResult, pendingResult, rejectedResult, chaptersResult, teamsResult] = await Promise.all([
      contentService.getUserTitles(),
      contentService.getUserPendingTitles(),
      contentService.getUserRejectedTitles(),
      contentService.getUserChapters(),
      contentService.getUserTeams()
    ])

    if (titlesResult.success) {
      userTitles.value = titlesResult.data
    }

    if (pendingResult.success) {
      pendingTitles.value = pendingResult.data
    }

    if (rejectedResult.success) {
      rejectedTitles.value = rejectedResult.data
    }

    if (chaptersResult.success) {
      userChapters.value = chaptersResult.data
    }

    if (teamsResult.success) {
      userTeams.value = teamsResult.data
    }

    // Load pending content for admins/moderators
    if (authStore.isAdmin || authStore.isModerator) {
      await loadPendingContent()
    }

  } catch (err) {
    console.error('Error loading user content:', err)
    error.value = 'Failed to load content. Please try again.'
  } finally {
    loading.value = false
  }
}

const loadPendingContent = async () => {
  if (!authStore.isAdmin && !authStore.isModerator) return

  try {
    const result = await contentService.getPendingContent()
    if (result.success) {
      pendingContent.value = result.data
    }
  } catch (err) {
    console.error('Error loading pending content:', err)
  }
}

const showSuccess = (message) => {
  successMessage.value = message
  setTimeout(() => {
    successMessage.value = ''
  }, 5000)
}

// Lifecycle
onMounted(async () => {
  await loadUserContent()
})

// Expose methods for child components
defineExpose({
  showSuccess,
  loadUserContent,
  loadPendingContent
})
</script>

<style scoped>
  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }

    .scrollbar-hide::-webkit-scrollbar {
      display: none;
    }
</style>
