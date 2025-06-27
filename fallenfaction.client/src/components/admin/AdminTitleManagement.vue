<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Admin Title Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Review and manage submitted titles</p>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading pending titles...</span>
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
            <h3 class="text-sm font-medium text-red-800">Error Loading Titles</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
            <div class="mt-4">
              <button @click="loadPendingTitles"
                      class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                Try Again
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Success Message -->
      <div v-if="successMessage" class="mb-6 bg-green-50 border border-green-200 rounded-md p-4">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3">
            <p class="text-sm font-medium text-green-800">{{ successMessage }}</p>
          </div>
          <div class="ml-auto pl-3">
            <button @click="successMessage = ''" class="text-green-400 hover:text-green-600">
              <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- No Pending Titles -->
      <div v-else-if="!isLoading && pendingTitles.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No pending titles</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All submitted titles have been reviewed.</p>
      </div>

      <!-- Pending Titles Table -->
      <div v-else class="bg-[var(--color-background-soft)] shadow-md rounded-lg border border-[var(--color-border)] overflow-hidden">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h2 class="text-xl font-semibold text-[var(--color-heading)]">Pending Titles ({{ pendingTitles.length }})</h2>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-[var(--color-border)]">
            <thead class="bg-[var(--color-background-mute)]">
              <tr>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Title ID
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Original Title
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  English Title
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Type
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
              <tr v-for="title in pendingTitles" :key="title.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                  #{{ title.id }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <button @click="viewTitleDetails(title.id)"
                          class="text-sm text-green-600 hover:text-green-700 font-medium hover:underline focus:outline-none">
                    {{ title.originalTitle || 'N/A' }}
                  </button>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                  {{ title.englishTitle }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                        :class="getTypeColor(title.type)">
                    {{ getTypeName(title.type) }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                  <button @click="acceptTitle(title.id)"
                          :disabled="isProcessing"
                          class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    <svg v-if="processingId === title.id" class="animate-spin -ml-1 mr-1 h-3 w-3 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Accept
                  </button>
                  <button @click="rejectTitle(title.id)"
                          :disabled="isProcessing"
                          class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    <svg v-if="processingId === title.id" class="animate-spin -ml-1 mr-1 h-3 w-3 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Reject
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Title Details Modal -->
      <div v-if="showDetailsModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
          <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Title Details</h3>
            <button @click="closeDetailsModal" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>

          <div v-if="titleDetails" class="p-6 space-y-6">
            <!-- Title Information -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Basic Information</h4>
                <dl class="space-y-2">
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Title ID</dt>
                    <dd class="text-sm text-[var(--color-text)]">#{{ titleDetails.id }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Original Title</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.originalTitle || 'N/A' }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">English Title</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.englishTitle }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Alternative Names</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.alternativeNames || 'N/A' }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Release Date</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.releaseDate || 'N/A' }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Type</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ getTypeName(titleDetails.type) }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Age Restriction</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.ageRestriction || 'No restriction' }}</dd>
                  </div>
                </dl>
              </div>

              <div>
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Status & Categories</h4>
                <dl class="space-y-2">
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Title Status</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.statusTitle || 'N/A' }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Translation Status</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.statusTranslation || 'N/A' }}</dd>
                  </div>
                  <div v-if="titleDetails.categories && titleDetails.categories.length">
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Categories</dt>
                    <dd class="text-sm text-[var(--color-text)]">
                      <div class="flex flex-wrap gap-1 mt-1">
                        <span v-for="category in titleDetails.categories" :key="category.id"
                              class="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-800">
                          {{ category.name }}
                        </span>
                      </div>
                    </dd>
                  </div>
                  <div v-if="titleDetails.tags && titleDetails.tags.length">
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Tags</dt>
                    <dd class="text-sm text-[var(--color-text)]">
                      <div class="flex flex-wrap gap-1 mt-1">
                        <span v-for="tag in titleDetails.tags" :key="tag.id"
                              class="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-purple-100 text-purple-800">
                          {{ tag.name }}
                        </span>
                      </div>
                    </dd>
                  </div>
                </dl>
              </div>
            </div>

            <!-- Description -->
            <div v-if="titleDetails.description">
              <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Description</h4>
              <p class="text-sm text-[var(--color-text)] bg-[var(--color-background-mute)] p-3 rounded border border-[var(--color-border)]">
                {{ titleDetails.description }}
              </p>
            </div>

            <!-- Images -->
            <div v-if="titleDetails.coverImagePath || titleDetails.backgroundImagePath" class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div v-if="titleDetails.coverImagePath">
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Cover Image</h4>
                <img :src="titleDetails.coverImagePath" alt="Cover" class="max-w-full h-auto rounded border border-[var(--color-border)]">
              </div>
              <div v-if="titleDetails.backgroundImagePath">
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Background Image</h4>
                <img :src="titleDetails.backgroundImagePath" alt="Background" class="max-w-full h-auto rounded border border-[var(--color-border)]">
              </div>
            </div>
          </div>

          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button @click="closeDetailsModal"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] transition-colors duration-200">
              Close
            </button>
            <button @click="acceptTitle(titleDetails.id)"
                    :disabled="isProcessing"
                    class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 transition-colors duration-200">
              Accept Title
            </button>
            <button @click="rejectTitle(titleDetails.id)"
                    :disabled="isProcessing"
                    class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 transition-colors duration-200">
              Reject Title
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import adminApi from '../../services/adminApi.js'

// Reactive data
const pendingTitles = ref([])
const isLoading = ref(true)
const error = ref('')
const successMessage = ref('')
const isProcessing = ref(false)
const processingId = ref(null)

// Modal state
const showDetailsModal = ref(false)
const titleDetails = ref(null)

// Type mappings
const typeNames = {
  1: 'Manga',
  2: 'Manhwa',
  3: 'Manhua',
  4: 'Comic',
  5: 'Webtoon'
}

const typeColors = {
  1: 'bg-red-100 text-red-800',
  2: 'bg-blue-100 text-blue-800',
  3: 'bg-yellow-100 text-yellow-800',
  4: 'bg-purple-100 text-purple-800',
  5: 'bg-green-100 text-green-800'
}

// Methods
const getTypeName = (type) => {
  return typeNames[type] || 'Unknown'
}

const getTypeColor = (type) => {
  return typeColors[type] || 'bg-gray-100 text-gray-800'
}

const loadPendingTitles = async () => {
  isLoading.value = true
  error.value = ''

  try {
    const result = await adminApi.getPendingTitles()

    if (result.success) {
      pendingTitles.value = result.data
      console.log('Loaded pending titles:', result.data)
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to load pending titles'
    console.error('Error loading pending titles:', err)
  } finally {
    isLoading.value = false
  }
}

const viewTitleDetails = async (titleId) => {
  try {
    const result = await adminApi.getPendingTitleDetails(titleId)

    if (result.success) {
      titleDetails.value = result.data
      showDetailsModal.value = true
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to load title details'
    console.error('Error loading title details:', err)
  }
}

const closeDetailsModal = () => {
  showDetailsModal.value = false
  titleDetails.value = null
}

const acceptTitle = async (titleId) => {
  if (!confirm('Are you sure you want to accept this title? It will be moved to the approved titles.')) {
    return
  }

  isProcessing.value = true
  processingId.value = titleId

  try {
    const result = await adminApi.acceptTitle(titleId)

    if (result.success) {
      successMessage.value = result.message
      // Remove the title from the pending list
      pendingTitles.value = pendingTitles.value.filter(title => title.id !== titleId)
      closeDetailsModal()
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to accept title'
    console.error('Error accepting title:', err)
  } finally {
    isProcessing.value = false
    processingId.value = null
  }
}

const rejectTitle = async (titleId) => {
  if (!confirm('Are you sure you want to reject this title? It will be moved to rejected titles.')) {
    return
  }

  isProcessing.value = true
  processingId.value = titleId

  try {
    const result = await adminApi.rejectTitle(titleId)

    if (result.success) {
      successMessage.value = result.message
      // Remove the title from the pending list
      pendingTitles.value = pendingTitles.value.filter(title => title.id !== titleId)
      closeDetailsModal()
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to reject title'
    console.error('Error rejecting title:', err)
  } finally {
    isProcessing.value = false
    processingId.value = null
  }
}

// Load data on mount
onMounted(async () => {
  console.log('Admin Title Management component mounted')
  await loadPendingTitles()
})
</script>

<style scoped>
  /* Custom focus and hover states using CSS variables */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }
</style>
