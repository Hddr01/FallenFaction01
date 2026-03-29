<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Admin Chapter Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Review and manage submitted chapters</p>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading pending chapters...</span>
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
            <h3 class="text-sm font-medium text-red-800">Error Loading Chapters</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
            <div class="mt-4">
              <button @click="loadPendingChapters"
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

      <!-- No Pending Chapters -->
      <div v-else-if="!isLoading && pendingChapters.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No pending chapters</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All submitted chapters have been reviewed.</p>
      </div>

      <!-- Pending Chapters Table -->
      <div v-else class="bg-[var(--color-background-soft)] shadow-md rounded-lg border border-[var(--color-border)] overflow-hidden">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h2 class="text-xl font-semibold text-[var(--color-heading)]">Pending Chapters ({{ pendingChapters.length }})</h2>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-[var(--color-border)]">
            <thead class="bg-[var(--color-background-mute)]">
              <tr>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Chapter ID
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Title
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Chapter
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Team
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Submitted
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Images
                </th>
                <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
              <tr v-for="chapter in pendingChapters" :key="chapter.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                  #{{ chapter.id }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <button @click="viewChapterDetails(chapter.id)"
                          class="text-sm text-green-600 hover:text-green-700 font-medium hover:underline focus:outline-none max-w-xs truncate block">
                    {{ chapter.titleName || 'N/A' }}
                  </button>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                  <div class="flex flex-col">
                    <span class="font-medium">Vol.{{ chapter.volumeNumber }} Ch.{{ chapter.chapterNumber }}</span>
                    <span class="text-xs opacity-75 max-w-xs truncate">{{ chapter.name || 'Untitled' }}</span>
                  </div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                    {{ chapter.teamName }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                  <div class="flex flex-col">
                    <span>{{ formatDate(chapter.createdDate) }}</span>
                    <span class="text-xs opacity-75">by {{ chapter.updatedByUserName }}</span>
                  </div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
                    {{ chapter.wordCount ?? '—' }} words
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                  <button @click="acceptChapter(chapter.id)"
                          :disabled="isProcessing"
                          class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    <svg v-if="processingId === chapter.id" class="animate-spin -ml-1 mr-1 h-3 w-3 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Accept
                  </button>
                  <button @click="rejectChapter(chapter.id)"
                          :disabled="isProcessing"
                          class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                    <svg v-if="processingId === chapter.id" class="animate-spin -ml-1 mr-1 h-3 w-3 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
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

      <!-- Chapter Details Modal -->
      <div v-if="showDetailsModal" class="fixed inset-0 bg-[var(--color-background)] bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-6xl w-full max-h-[90vh] overflow-y-auto">
          <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Chapter Details</h3>
            <button @click="closeDetailsModal" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>

          <div v-if="chapterDetails" class="p-6 space-y-6">
            <!-- Chapter Information -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Basic Information</h4>
                <dl class="space-y-2">
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Chapter ID</dt>
                    <dd class="text-sm text-[var(--color-text)]">#{{ chapterDetails.id }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Title</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ chapterDetails.titleName }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Chapter</dt>
                    <dd class="text-sm text-[var(--color-text)]">
                      Vol.{{ chapterDetails.volumeNumber }} Ch.{{ chapterDetails.chapterNumber }}
                      <span v-if="chapterDetails.name" class="block text-xs opacity-75">{{ chapterDetails.name }}</span>
                    </dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Team</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ chapterDetails.teamName }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Submitted By</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ chapterDetails.updatedByUserName }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Submitted Date</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ formatDate(chapterDetails.createdDate) }}</dd>
                  </div>
                </dl>
              </div>

              <div>
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Chapter Statistics</h4>
                <dl class="space-y-2">
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Word Count</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ chapterDetails.content ? chapterDetails.content.trim().split(/\s+/).length : 0 }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Title ID</dt>
                    <dd class="text-sm text-[var(--color-text)]">#{{ chapterDetails.titleId }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-60">Team ID</dt>
                    <dd class="text-sm text-[var(--color-text)]">#{{ chapterDetails.teamId }}</dd>
                  </div>
                </dl>
              </div>
            </div>

            <!-- Chapter Content Preview -->
            <div v-if="chapterDetails.content">
              <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-4">Content Preview</h4>
              <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4 max-h-64 overflow-y-auto text-sm text-[var(--color-text)] leading-relaxed whitespace-pre-wrap font-serif">
                {{ chapterDetails.content.slice(0, 1500) }}{{ chapterDetails.content.length > 1500 ? '…' : '' }}
              </div>
            </div>
          </div>

          <!-- Modal Actions -->
          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button @click="closeDetailsModal"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] transition-colors duration-200">
              Close
            </button>
            <button @click="acceptChapter(chapterDetails.id)"
                    :disabled="isProcessing"
                    class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 transition-colors duration-200">
              Accept Chapter
            </button>
            <button @click="showRejectModal = true"
                    :disabled="isProcessing"
                    class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 transition-colors duration-200">
              Reject Chapter
            </button>
          </div>
        </div>
      </div>

      <!-- Reject Modal -->
      <div v-if="showRejectModal" class="fixed inset-0 bg-[var(--color-background)] bg-opacity-50 flex items-center justify-center p-4 z-60">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-md w-full">
          <div class="px-6 py-4 border-b border-[var(--color-border)]">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Reject Chapter</h3>
          </div>
          <div class="p-6">
            <p class="text-sm text-[var(--color-text)] mb-4">
              Are you sure you want to reject this chapter? Please provide a reason (optional):
            </p>
            <textarea v-model="rejectReason"
                      rows="3"
                      placeholder="Reason for rejection (optional)"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-red-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 resize-vertical"></textarea>
          </div>
          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button @click="showRejectModal = false; rejectReason = ''"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] transition-colors duration-200">
              Cancel
            </button>
            <button @click="confirmRejectChapter"
                    :disabled="isProcessing"
                    class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 transition-colors duration-200">
              Reject Chapter
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
const pendingChapters = ref([])
const isLoading = ref(true)
const error = ref('')
const successMessage = ref('')
const isProcessing = ref(false)
const processingId = ref(null)

// Modal states
const showDetailsModal = ref(false)
const chapterDetails = ref(null)
const showRejectModal = ref(false)
const rejectReason = ref('')

// Methods
const loadPendingChapters = async () => {
  isLoading.value = true
  error.value = ''

  try {
    const result = await adminApi.getPendingChapters()

    if (result.success) {
      pendingChapters.value = result.data
      console.log('Loaded pending chapters:', result.data)
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to load pending chapters'
    console.error('Error loading pending chapters:', err)
  } finally {
    isLoading.value = false
  }
}

const viewChapterDetails = async (chapterId) => {
  try {
    const result = await adminApi.getPendingChapterDetails(chapterId)

    if (result.success) {
      chapterDetails.value = result.data
      showDetailsModal.value = true
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to load chapter details'
    console.error('Error loading chapter details:', err)
  }
}

const closeDetailsModal = () => {
  showDetailsModal.value = false
  chapterDetails.value = null
}

const acceptChapter = async (chapterId) => {
  if (!confirm('Are you sure you want to accept this chapter? It will be published and available to readers.')) {
    return
  }

  isProcessing.value = true
  processingId.value = chapterId

  try {
    const result = await adminApi.acceptChapter(chapterId)

    if (result.success) {
      successMessage.value = result.message || 'Chapter accepted successfully!'
      // Remove the chapter from the pending list
      pendingChapters.value = pendingChapters.value.filter(chapter => chapter.id !== chapterId)
      closeDetailsModal()
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to accept chapter'
    console.error('Error accepting chapter:', err)
  } finally {
    isProcessing.value = false
    processingId.value = null
  }
}

const rejectChapter = async (chapterId) => {
  showRejectModal.value = true
  processingId.value = chapterId
}

const confirmRejectChapter = async () => {
  const chapterId = processingId.value

  if (!chapterId) return

  isProcessing.value = true

  try {
    const result = await adminApi.rejectChapter(chapterId, rejectReason.value)

    if (result.success) {
      successMessage.value = result.message || 'Chapter rejected successfully!'
      // Remove the chapter from the pending list
      pendingChapters.value = pendingChapters.value.filter(chapter => chapter.id !== chapterId)
      closeDetailsModal()
      showRejectModal.value = false
      rejectReason.value = ''
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to reject chapter'
    console.error('Error rejecting chapter:', err)
  } finally {
    isProcessing.value = false
    processingId.value = null
  }
}


const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'

  try {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch (err) {
    return 'Invalid date'
  }
}

// Load data on mount
onMounted(async () => {
  console.log('Admin Chapter Management component mounted')
  await loadPendingChapters()
})
</script>

<style scoped>
  /* Custom focus and hover states using CSS variables */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }

  /* Custom scrollbar for image grid */
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
</style>
