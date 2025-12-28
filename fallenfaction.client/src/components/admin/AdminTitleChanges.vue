<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Pending Title Changes</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Review and approve user-submitted title edits</p>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading pending changes...</span>
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
            <h3 class="text-sm font-medium text-red-800">Error Loading Changes</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
            <div class="mt-4">
              <button @click="loadPendingChanges"
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

      <!-- No Pending Changes -->
      <div v-else-if="!isLoading && pendingChanges.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No pending changes</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All title edits have been reviewed.</p>
      </div>

      <!-- Pending Changes List -->
      <div v-else class="space-y-6">
        <div v-for="change in pendingChanges" :key="change.titleId"
             class="bg-[var(--color-background-soft)] shadow-md rounded-lg border border-[var(--color-border)] overflow-hidden">
          <!-- Title Header -->
          <div class="px-6 py-4 border-b border-[var(--color-border)] bg-[var(--color-background-mute)]">
            <div class="flex justify-between items-center">
              <div>
                <h2 class="text-lg font-semibold text-[var(--color-heading)]">
                  {{ change.titleEnglishName || change.titleName }}
                </h2>
                <p class="text-sm text-[var(--color-text)] opacity-75 mt-1">
                  Submitted by {{ change.submittedBy }} • {{ formatDate(change.submittedAt) }}
                </p>
              </div>
              <span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-yellow-100 text-yellow-800">
                {{ change.changeCount }} changes
              </span>
            </div>
          </div>

          <!-- Changes List -->
          <div class="px-6 py-4">
            <div class="space-y-3">
              <div v-for="item in change.changes" :key="item.id"
                   class="p-4 bg-[var(--color-background)] border border-[var(--color-border)] rounded-md">
                <div class="flex justify-between items-start mb-2">
                  <h3 class="text-sm font-medium text-[var(--color-heading)]">{{ item.changeType }}</h3>
                </div>
                <div class="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p class="text-[var(--color-text)] opacity-60 mb-1">Old Value:</p>
                    <p class="text-[var(--color-text)] bg-red-50 border border-red-200 rounded px-2 py-1 break-words">
                      {{ item.oldValue || '(empty)' }}
                    </p>
                  </div>
                  <div>
                    <p class="text-[var(--color-text)] opacity-60 mb-1">New Value:</p>
                    <p class="text-[var(--color-text)] bg-green-50 border border-green-200 rounded px-2 py-1 break-words">
                      {{ item.newValue || '(empty)' }}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="px-6 py-4 border-t border-[var(--color-border)] bg-[var(--color-background-mute)] flex justify-end space-x-3">
            <button @click="openRejectDialog(change)"
                    :disabled="isProcessing"
                    class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
              <svg v-if="processingId === change.titleId" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Reject All
            </button>
            <button @click="openApproveDialog(change)"
                    :disabled="isProcessing"
                    class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
              <svg v-if="processingId === change.titleId" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Approve All
            </button>
          </div>
        </div>
      </div>

      <!-- Approve Dialog -->
      <div v-if="showApproveDialog" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-md w-full">
          <div class="px-6 py-4 border-b border-[var(--color-border)]">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Approve Changes</h3>
          </div>
          <div class="p-6">
            <p class="text-[var(--color-text)] mb-4">
              Are you sure you want to approve {{ selectedChange?.changeCount }} changes for "{{ selectedChange?.titleEnglishName || selectedChange?.titleName }}"?
            </p>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Admin Comment (Optional):</label>
            <textarea v-model="adminComment"
                      rows="3"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 resize-vertical"
                      placeholder="Add any notes about this approval..."></textarea>
          </div>
          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button @click="closeApproveDialog"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none transition-colors duration-200">
              Cancel
            </button>
            <button @click="approveChanges"
                    class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
              Approve Changes
            </button>
          </div>
        </div>
      </div>

      <!-- Reject Dialog -->
      <div v-if="showRejectDialog" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-md w-full">
          <div class="px-6 py-4 border-b border-[var(--color-border)]">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Reject Changes</h3>
          </div>
          <div class="p-6">
            <p class="text-[var(--color-text)] mb-4">
              Are you sure you want to reject {{ selectedChange?.changeCount }} changes for "{{ selectedChange?.titleEnglishName || selectedChange?.titleName }}"?
            </p>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Rejection Reason *:</label>
            <textarea v-model="rejectionReason"
                      required
                      rows="3"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-red-500 resize-vertical"
                      placeholder="Please provide a reason for rejection..."></textarea>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2 mt-4">Admin Comment (Optional):</label>
            <textarea v-model="adminComment"
                      rows="2"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-red-500 resize-vertical"
                      placeholder="Add any additional notes..."></textarea>
          </div>
          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button @click="closeRejectDialog"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none transition-colors duration-200">
              Cancel
            </button>
            <button @click="rejectChanges"
                    :disabled="!rejectionReason.trim()"
                    class="px-4 py-2 border border-transparent rounded-md text-sm font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
              Reject Changes
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
  const pendingChanges = ref([])
  const isLoading = ref(true)
  const error = ref('')
  const successMessage = ref('')
  const isProcessing = ref(false)
  const processingId = ref(null)

  // Dialog state
  const showApproveDialog = ref(false)
  const showRejectDialog = ref(false)
  const selectedChange = ref(null)
  const adminComment = ref('')
  const rejectionReason = ref('')

  // Methods
  const loadPendingChanges = async () => {
    isLoading.value = true
    error.value = ''

    try {
      const result = await adminApi.getPendingTitleChanges()

      if (result.success) {
        pendingChanges.value = result.data
        console.log('Loaded pending changes:', result.data)
      } else {
        error.value = result.error
      }
    } catch (err) {
      error.value = 'Failed to load pending changes'
      console.error('Error loading pending changes:', err)
    } finally {
      isLoading.value = false
    }
  }

  const openApproveDialog = (change) => {
    selectedChange.value = change
    adminComment.value = ''
    showApproveDialog.value = true
  }

  const closeApproveDialog = () => {
    showApproveDialog.value = false
    selectedChange.value = null
    adminComment.value = ''
  }

  const openRejectDialog = (change) => {
    selectedChange.value = change
    adminComment.value = ''
    rejectionReason.value = ''
    showRejectDialog.value = true
  }

  const closeRejectDialog = () => {
    showRejectDialog.value = false
    selectedChange.value = null
    adminComment.value = ''
    rejectionReason.value = ''
  }

  const approveChanges = async () => {
    if (!selectedChange.value) return

    isProcessing.value = true
    processingId.value = selectedChange.value.titleId

    try {
      const result = await adminApi.approveTitleChanges(
        selectedChange.value.titleId,
        adminComment.value
      )

      if (result.success) {
        successMessage.value = result.message
        pendingChanges.value = pendingChanges.value.filter(
          change => change.titleId !== selectedChange.value.titleId
        )
        closeApproveDialog()
      } else {
        error.value = result.error
      }
    } catch (err) {
      error.value = 'Failed to approve changes'
      console.error('Error approving changes:', err)
    } finally {
      isProcessing.value = false
      processingId.value = null
    }
  }

  const rejectChanges = async () => {
    if (!selectedChange.value || !rejectionReason.value.trim()) return

    isProcessing.value = true
    processingId.value = selectedChange.value.titleId

    try {
      const result = await adminApi.rejectTitleChanges(
        selectedChange.value.titleId,
        rejectionReason.value,
        adminComment.value
      )

      if (result.success) {
        successMessage.value = result.message
        pendingChanges.value = pendingChanges.value.filter(
          change => change.titleId !== selectedChange.value.titleId
        )
        closeRejectDialog()
      } else {
        error.value = result.error
      }
    } catch (err) {
      error.value = 'Failed to reject changes'
      console.error('Error rejecting changes:', err)
    } finally {
      isProcessing.value = false
      processingId.value = null
    }
  }

  const formatDate = (dateString) => {
    if (!dateString) return 'Unknown date'
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  // Load data on mount
  onMounted(async () => {
    console.log('AdminTitleChanges component mounted')
    await loadPendingChanges()
  })
</script>

<style scoped>
  /* Custom focus and hover states using CSS variables */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }
</style>
