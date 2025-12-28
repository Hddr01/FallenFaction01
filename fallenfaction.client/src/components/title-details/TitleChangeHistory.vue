<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div v-if="!loading && !error" class="mb-4 bg-blue-50 border border-blue-200 rounded-lg p-3">
        <div class="flex items-center">
          <svg class="w-5 h-5 text-blue-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
          </svg>
          <span class="text-sm text-blue-800">
            {{
 changeStats.hasFullAccess
        ? 'You have full access - viewing all changes (including pending and rejected)'
        : 'Viewing approved changes only'
            }}
          </span>
        </div>
      </div>
      <div class="mb-8">
        <div class="flex items-center gap-4 mb-4">
          <button @click="goBack"
                  class="p-2 rounded-md border border-[var(--color-border)] text-[var(--color-text)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"></path>
            </svg>
          </button>
          <div>
            <h1 class="text-3xl font-bold text-[var(--color-heading)]">Change History</h1>
            <p class="text-[var(--color-text)] opacity-75">{{ titleName || 'Loading...' }}</p>
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
          <span class="text-xl text-[var(--color-text)]">Loading change history...</span>
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
            <h3 class="text-sm font-medium text-red-800">Error Loading Change History</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
            <div class="mt-4">
              <button @click="loadChangeHistory" class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200 transition-colors duration-200">
                Try Again
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <div v-else>
        <!-- Statistics Summary -->
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
            <div class="flex items-center">
              <svg class="w-8 h-8 text-blue-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
              </svg>
              <div>
                <p class="text-2xl font-bold text-[var(--color-text)]">{{ changeStats.totalChanges || 0 }}</p>
                <p class="text-sm text-[var(--color-text)] opacity-75">Total Changes</p>
              </div>
            </div>
          </div>

          <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
            <div class="flex items-center">
              <svg class="w-8 h-8 text-green-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
              </svg>
              <div>
                <p class="text-2xl font-bold text-[var(--color-text)]">{{ getStatusCount('Approved') + getStatusCount('AutoApproved') }}</p>
                <p class="text-sm text-[var(--color-text)] opacity-75">Approved</p>
              </div>
            </div>
          </div>

          <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
            <div class="flex items-center">
              <svg class="w-8 h-8 text-yellow-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
              </svg>
              <div>
                <p class="text-2xl font-bold text-[var(--color-text)]">{{ getStatusCount('Pending') }}</p>
                <p class="text-sm text-[var(--color-text)] opacity-75">Pending</p>
              </div>
            </div>
          </div>

          <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
            <div class="flex items-center">
              <svg class="w-8 h-8 text-red-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
              </svg>
              <div>
                <p class="text-2xl font-bold text-[var(--color-text)]">{{ getStatusCount('Rejected') }}</p>
                <p class="text-sm text-[var(--color-text)] opacity-75">Rejected</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Filters -->
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4 mb-6">
          <div class="flex flex-wrap items-center gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Filter by Status:</label>
              <select v-model="selectedStatus"
                      @change="applyFilters"
                      class="px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option value="">All Statuses</option>
                <option value="Pending">Pending</option>
                <option value="Approved">Approved</option>
                <option value="AutoApproved">Auto-Approved</option>
                <option value="Rejected">Rejected</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Filter by Type:</label>
              <select v-model="selectedChangeType"
                      @change="applyFilters"
                      class="px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option value="">All Types</option>
                <option v-for="type in uniqueChangeTypes" :key="type" :value="type">
                  {{ titleChangeService.formatChangeType(type) }}
                </option>
              </select>
            </div>

            <div class="flex items-end">
              <button @click="clearFilters"
                      class="px-4 py-2 bg-gray-500 text-white rounded-md hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-gray-500 transition-colors duration-200">
                Clear Filters
              </button>
            </div>
          </div>
        </div>

        <!-- Change Log Entries -->
        <div class="space-y-4">
          <div v-if="filteredChanges.length === 0" class="text-center py-8 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl">
            <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
            </svg>
            <h3 class="text-lg font-medium text-[var(--color-text)] mb-2">No Changes Found</h3>
            <p class="text-[var(--color-text)] opacity-75">
              {{ hasFilters ? 'No changes match the current filters.' : 'No change history available for this title.' }}
            </p>
          </div>

          <div v-for="change in filteredChanges"
               :key="change.id"
               class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl overflow-hidden">
            <!-- Change Header -->
            <div class="px-6 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
              <div class="flex items-center space-x-4">
                <div class="flex-shrink-0">
                  <svg class="w-6 h-6 text-[var(--color-accent)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                  </svg>
                </div>
                <div>
                  <h3 class="text-lg font-semibold text-[var(--color-text)]">{{ titleChangeService.formatChangeType(change.changeType) }}</h3>
                  <p class="text-sm text-[var(--color-text)] opacity-75">
                    Changed by {{ change.updatedByUser?.userName || 'Unknown User' }} on {{ titleChangeService.formatDateTime(change.createdAt) }}
                  </p>
                </div>
              </div>

              <!-- Status Badge -->
              <div>
                <span :class="['px-3 py-1 rounded-full text-xs font-medium border', getStatusClass(change.status)]">
                  {{ getStatusLabel(change.status) }}
                </span>
              </div>
            </div>

            <!-- Change Details -->
            <div class="px-6 py-4">
              <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <!-- Old Value -->
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Previous Value:</label>
                  <div class="bg-red-50 border border-red-200 rounded-md p-3 min-h-16">
                    <pre class="text-sm text-red-800 whitespace-pre-wrap break-words">{{ change.oldValue || 'No previous value' }}</pre>
                  </div>
                </div>

                <!-- New Value -->
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-2">New Value:</label>
                  <div class="bg-green-50 border border-green-200 rounded-md p-3 min-h-16">
                    <pre class="text-sm text-green-800 whitespace-pre-wrap break-words">{{ change.newValue || 'No new value' }}</pre>
                  </div>
                </div>
              </div>

              <!-- Admin Comments and Review Info -->
              <div v-if="change.status !== 'Pending'" class="mt-4 pt-4 border-t border-[var(--color-border)]">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div v-if="change.reviewedByUser">
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Reviewed By:</label>
                    <p class="text-sm text-[var(--color-text)]">{{ change.reviewedByUser.userName }}</p>
                    <p class="text-xs text-[var(--color-text)] opacity-60">{{ titleChangeService.formatDateTime(change.reviewedAt) }}</p>
                  </div>

                  <div v-if="change.adminComment">
                    <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Admin Comment:</label>
                    <p class="text-sm text-[var(--color-text)] bg-[var(--color-background-mute)] p-2 rounded">{{ change.adminComment }}</p>
                  </div>
                </div>

                <div v-if="change.status === 'Rejected' && change.rejectionReason" class="mt-3">
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Rejection Reason:</label>
                  <p class="text-sm text-red-600 bg-red-50 p-2 rounded">{{ change.rejectionReason }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Pagination (if needed) -->
        <div v-if="filteredChanges.length > 0" class="mt-8 flex justify-between items-center">
          <p class="text-sm text-[var(--color-text)] opacity-75">
            Showing {{ filteredChanges.length }} of {{ changeLog.length }} changes
          </p>

          <div class="flex space-x-2">
            <button @click="goBack"
                    class="px-6 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] transition-colors duration-200">
              Back to Title
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { titleChangeService } from '../../services/titleChangeService'
  import { titleDetailsService } from '../../services/titleDetailsService'


  // Props
  const props = defineProps({
    titleId: {
      type: [Number, String],
      required: true
    }
  })

  const route = useRoute()
  const router = useRouter()
  const titleName = ref(route.query.titleName || 'Loading...')

  // Reactive data
  const loading = ref(true)
  const error = ref('')
  const changeLog = ref([])
  const changeStats = ref({
    totalChanges: 0,
    changesByStatus: [],
    lastUpdate: null
  })

  // Filters
  const selectedStatus = ref('')
  const selectedChangeType = ref('')

  // Computed properties
  const uniqueChangeTypes = computed(() => {
    const types = [...new Set(changeLog.value.map(change => change.changeType))]
    return types.sort()
  })

  const filteredChanges = computed(() => {
    let filtered = [...changeLog.value]

    if (selectedStatus.value) {
      filtered = filtered.filter(change => change.status === selectedStatus.value)
    }

    if (selectedChangeType.value) {
      filtered = filtered.filter(change => change.changeType === selectedChangeType.value)
    }

    // Sort by creation date (newest first)
    return filtered.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
  })

  const hasFilters = computed(() => {
    return selectedStatus.value || selectedChangeType.value
  })

  const loadTitleName = async () => {
    // First try to use the query parameter
    if (route.query.titleName) {
      titleName.value = route.query.titleName
      return
    }

    // Fallback: fetch using GetTitleByName with titleId
    // This requires first getting basic title info
    if (!titleName.value || titleName.value === 'Loading...') {
      try {
        // Get the title's name first using a simple query
        const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/Titles/Details/ById/${props.titleId}`)
        if (response.ok) {
          const title = await response.json()
          titleName.value = title.originalTitle || title.englishTitle || 'Unknown Title'
        } else {
          titleName.value = `Title ${props.titleId}`
        }
      } catch (err) {
        console.error('Error loading title name:', err)
        titleName.value = `Title ${props.titleId}`
      }
    }
  }


  // Methods
  const loadChangeHistory = async () => {
    loading.value = true
    error.value = ''

    try {
      // Load change log
      const changeResult = await titleChangeService.getTitleChangeLog(props.titleId)
      if (changeResult.success) {
        changeLog.value = changeResult.data || []
      } else {
        console.warn('Failed to load change log:', changeResult.error)
        changeLog.value = []
      }

      // Load change statistics
      const statsResult = await titleChangeService.getTitleChangeStats(props.titleId)
      if (statsResult.success) {
        changeStats.value = statsResult.data || {
          totalChanges: 0,
          changesByStatus: [],
          lastUpdate: null
        }
      }

      // If both failed, show error
      if (!changeResult.success && !statsResult.success) {
        error.value = changeResult.error || 'Failed to load change history'
      }

    } catch (err) {
      console.error('Error loading change history:', err)
      error.value = 'Failed to load change history'
    } finally {
      loading.value = false
    }
  }

  const getStatusCount = (status) => {
    const statusCounts = changeStats.value.changesByStatus || []
    const found = statusCounts.find(s => s.status === status)
    return found ? found.count : 0
  }

  const getStatusClass = (status) => {
    const statusInfo = titleChangeService.getStatusInfo(status)
    return statusInfo.class
  }

  const getStatusLabel = (status) => {
    const statusInfo = titleChangeService.getStatusInfo(status)
    return statusInfo.label
  }

  const applyFilters = () => {
    // Filters are automatically applied via computed property
    console.log('Filters applied:', { selectedStatus: selectedStatus.value, selectedChangeType: selectedChangeType.value })
  }

  const clearFilters = () => {
    selectedStatus.value = ''
    selectedChangeType.value = ''
  }

  const goBack = () => {
    router.back()
  }

  // Lifecycle
  onMounted(async () => {
    console.log('TitleChangeHistory component mounted for title ID:', props.titleId)
    await loadTitleName()
    await loadChangeHistory()
  })

  // Expose titleChangeService for template usage (already imported)
</script>

<style scoped>
  /* Custom scrollbar */
  .overflow-y-auto::-webkit-scrollbar {
    width: 8px;
  }

  .overflow-y-auto::-webkit-scrollbar-track {
    background: var(--color-background-mute);
  }

  .overflow-y-auto::-webkit-scrollbar-thumb {
    background: var(--color-border);
    border-radius: 4px;
  }

    .overflow-y-auto::-webkit-scrollbar-thumb:hover {
      background: var(--color-border-hover);
    }

  /* Pre-formatted text styling */
  pre {
    font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
    font-size: 0.875rem;
    line-height: 1.4;
  }

  /* Transition animations */
  .transition-colors {
    transition-property: background-color, border-color, color;
    transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
  }

  /* Focus styles */
  select:focus,
  button:focus {
    outline: 2px solid transparent;
    outline-offset: 2px;
  }

  /* Status badge animations */
  .px-3.py-1 {
    transition: all 0.2s ease-in-out;
  }
</style>
