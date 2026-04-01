<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Admin Chapter Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Review and manage submitted chapters, grouped by title and team</p>
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
            <div class="mt-2 text-sm text-red-700"><p>{{ error }}</p></div>
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

      <!-- Grouped Chapter List -->
      <div v-else class="space-y-6">
        <div class="flex items-center justify-between mb-2">
          <h2 class="text-lg font-semibold text-[var(--color-heading)]">
            Pending Chapters ({{ pendingChapters.length }}) — {{ groupedChapters.length }} title(s)
          </h2>
        </div>

        <!-- Title Group -->
        <div v-for="titleGroup in groupedChapters" :key="titleGroup.titleKey"
             class="bg-[var(--color-background-soft)] rounded-lg border border-[var(--color-border)] shadow-sm overflow-hidden">

          <!-- Title Header (accordion toggle) -->
          <button
            class="w-full flex items-center justify-between px-5 py-4 text-left hover:bg-[var(--color-background-mute)] transition-colors duration-150 focus:outline-none"
            @click="toggleTitle(titleGroup.titleKey)"
          >
            <div class="flex items-center gap-3 min-w-0">
              <svg :class="expandedTitles.has(titleGroup.titleKey) ? 'rotate-90' : ''"
                   class="h-4 w-4 text-[var(--color-text)] opacity-60 transition-transform duration-150 flex-shrink-0"
                   fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
              <span class="font-semibold text-[var(--color-heading)] truncate">{{ titleGroup.titleName }}</span>
              <span v-if="titleGroup.isTitleApproved"
                    class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 flex-shrink-0">
                Live
              </span>
              <span v-else
                    class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800 flex-shrink-0">
                Title Pending
              </span>
            </div>
            <span class="ml-4 text-xs text-[var(--color-text)] opacity-60 flex-shrink-0">
              {{ titleGroup.totalChapters }} chapter(s)
            </span>
          </button>

          <!-- Team Groups inside Title -->
          <div v-if="expandedTitles.has(titleGroup.titleKey)" class="border-t border-[var(--color-border)]">
            <div v-for="teamGroup in titleGroup.teams" :key="teamGroup.teamName"
                 class="border-b border-[var(--color-border)] last:border-b-0">

              <!-- Team Header -->
              <div class="flex items-center justify-between px-5 py-3 bg-[var(--color-background-mute)]">
                <div class="flex items-center gap-2">
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                    {{ teamGroup.teamName }}
                  </span>
                  <span class="text-xs text-[var(--color-text)] opacity-60">{{ teamGroup.chapters.length }} chapter(s)</span>
                </div>
                <div class="flex items-center gap-2">
                  <span v-if="!titleGroup.isTitleApproved" class="text-xs text-amber-600 italic">
                    Approve the title first
                  </span>
                  <button
                    v-if="titleGroup.isTitleApproved"
                    :disabled="isProcessing || massProcessingKey === titleGroup.titleKey + teamGroup.teamName"
                    @click="massApproveTeam(titleGroup, teamGroup)"
                    class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                  >
                    <svg v-if="massProcessingKey === titleGroup.titleKey + teamGroup.teamName"
                         class="animate-spin -ml-1 mr-1 h-3 w-3 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Release All ({{ teamGroup.chapters.length }})
                  </button>
                </div>
              </div>

              <!-- Chapter Rows -->
              <table class="min-w-full divide-y divide-[var(--color-border)]">
                <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
                  <tr v-for="chapter in teamGroup.chapters" :key="chapter.id"
                      class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                    <td class="px-5 py-3 whitespace-nowrap text-xs text-[var(--color-text)] opacity-60 w-16">
                      #{{ chapter.id }}
                    </td>
                    <td class="px-5 py-3 whitespace-nowrap text-sm text-[var(--color-text)] min-w-[160px]">
                      <button @click="viewChapterDetails(chapter.id)"
                              class="text-green-600 hover:text-green-700 font-medium hover:underline focus:outline-none text-left">
                        <span class="font-medium">Vol.{{ chapter.volumeNumber }} Ch.{{ chapter.chapterNumber }}</span>
                        <span v-if="chapter.name" class="block text-xs opacity-75 truncate max-w-xs">{{ chapter.name }}</span>
                      </button>
                    </td>
                    <td class="px-5 py-3 whitespace-nowrap text-xs text-[var(--color-text)] hidden sm:table-cell">
                      {{ formatDate(chapter.createdDate) }}
                      <span class="block opacity-60">{{ chapter.updatedByUserName }}</span>
                    </td>
                    <td class="px-5 py-3 whitespace-nowrap text-xs">
                      <span v-if="chapter.originalChapterId"
                            class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">Edit</span>
                      <span v-else
                            class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">New</span>
                    </td>
                    <td class="px-5 py-3 whitespace-nowrap text-xs hidden md:table-cell">
                      <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
                        {{ chapter.wordCount ?? '—' }} words
                      </span>
                    </td>
                    <td class="px-5 py-3 whitespace-nowrap text-sm font-medium space-x-2 text-right">
                      <button @click="acceptChapter(chapter.id)"
                              :disabled="isProcessing || !titleGroup.isTitleApproved"
                              :title="!titleGroup.isTitleApproved ? 'Title must be approved first' : 'Accept chapter'"
                              class="inline-flex items-center px-2.5 py-1 border border-transparent text-xs font-medium rounded text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                        Accept
                      </button>
                      <button @click="promptReject(chapter.id)"
                              :disabled="isProcessing"
                              class="inline-flex items-center px-2.5 py-1 border border-transparent text-xs font-medium rounded text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                        Reject
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>

            </div>
          </div>

        </div>
      </div>

      <!-- Chapter Details Modal -->
      <div v-if="showDetailsModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
          <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Chapter Details</h3>
            <button @click="closeDetailsModal" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div v-if="chapterDetails" class="p-6 space-y-6">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Basic Information</h4>
                <dl class="space-y-2">
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Chapter ID</dt>
                    <dd class="text-sm text-[var(--color-text)]">#{{ chapterDetails.id }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Title</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ chapterDetails.titleName }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Title Status</dt>
                    <dd>
                      <span v-if="chapterDetails.isTitleApproved"
                            class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">Live</span>
                      <span v-else
                            class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">Title Pending</span>
                    </dd>
                  </div>
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Chapter</dt>
                    <dd class="text-sm text-[var(--color-text)]">
                      Vol.{{ chapterDetails.volumeNumber }} Ch.{{ chapterDetails.chapterNumber }}
                      <span v-if="chapterDetails.name" class="block text-xs opacity-75">{{ chapterDetails.name }}</span>
                    </dd>
                  </div>
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Team</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ chapterDetails.teamName }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Submitted By</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ chapterDetails.updatedByUserName }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Submitted Date</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ formatDate(chapterDetails.createdDate) }}</dd>
                  </div>
                </dl>
              </div>
              <div>
                <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-2">Statistics</h4>
                <dl class="space-y-2">
                  <div>
                    <dt class="text-xs opacity-60 text-[var(--color-text)]">Word Count</dt>
                    <dd class="text-sm text-[var(--color-text)]">
                      {{ chapterDetails.content ? chapterDetails.content.trim().split(/\s+/).length : 0 }}
                    </dd>
                  </div>
                </dl>
              </div>
            </div>

            <div v-if="chapterDetails.content">
              <h4 class="text-sm font-medium text-[var(--color-text)] opacity-75 mb-4">Content Preview</h4>
              <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4 max-h-64 overflow-y-auto text-sm text-[var(--color-text)] leading-relaxed whitespace-pre-wrap font-serif">
                {{ chapterDetails.content.slice(0, 1500) }}{{ chapterDetails.content.length > 1500 ? '…' : '' }}
              </div>
            </div>
          </div>

          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button @click="closeDetailsModal"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none transition-colors duration-200">
              Close
            </button>
            <button v-if="chapterDetails?.isTitleApproved"
                    @click="acceptChapter(chapterDetails.id)"
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
      <div v-if="showRejectModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-[60]">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-md w-full">
          <div class="px-6 py-4 border-b border-[var(--color-border)]">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Reject Chapter</h3>
          </div>
          <div class="p-6">
            <p class="text-sm text-[var(--color-text)] mb-4">Please provide a reason (optional):</p>
            <textarea v-model="rejectReason" rows="3" placeholder="Reason for rejection (optional)"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-red-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 resize-vertical"></textarea>
          </div>
          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button @click="showRejectModal = false; rejectReason = ''"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none transition-colors duration-200">
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
import { ref, computed, onMounted } from 'vue'
import adminApi from '../../services/adminApi.js'

const pendingChapters = ref([])
const isLoading = ref(true)
const error = ref('')
const successMessage = ref('')
const isProcessing = ref(false)
const processingId = ref(null)
const massProcessingKey = ref(null)

const showDetailsModal = ref(false)
const chapterDetails = ref(null)
const showRejectModal = ref(false)
const rejectReason = ref('')

// Track which title accordions are open
const expandedTitles = ref(new Set())

// ── Grouping ─────────────────────────────────────────────────────────────────

const groupedChapters = computed(() => {
  const map = new Map()

  for (const chapter of pendingChapters.value) {
    const titleKey = chapter.isTitleApproved
      ? `live-${chapter.titleId}`
      : `pending-${chapter.pendingTitleId}`

    if (!map.has(titleKey)) {
      map.set(titleKey, {
        titleKey,
        titleName: chapter.titleName || 'Unknown Title',
        titleId: chapter.titleId,
        pendingTitleId: chapter.pendingTitleId,
        isTitleApproved: chapter.isTitleApproved,
        teams: new Map(),
        totalChapters: 0,
      })
    }

    const titleGroup = map.get(titleKey)
    titleGroup.totalChapters++

    const teamName = chapter.teamName || 'Unknown Team'
    if (!titleGroup.teams.has(teamName)) {
      titleGroup.teams.set(teamName, { teamName, chapters: [] })
    }
    titleGroup.teams.get(teamName).chapters.push(chapter)
  }

  return Array.from(map.values()).map(tg => ({
    ...tg,
    teams: Array.from(tg.teams.values()).map(team => ({
      ...team,
      chapters: team.chapters.slice().sort((a, b) =>
        a.volumeNumber !== b.volumeNumber
          ? a.volumeNumber - b.volumeNumber
          : a.chapterNumber - b.chapterNumber
      ),
    })),
  }))
})

// ── Data loading ──────────────────────────────────────────────────────────────

const loadPendingChapters = async () => {
  isLoading.value = true
  error.value = ''

  try {
    const result = await adminApi.getPendingChapters()
    if (result.success) {
      pendingChapters.value = result.data
      // Auto-expand all title groups on load
      expandedTitles.value = new Set(
        groupedChapters.value.map(tg => tg.titleKey)
      )
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to load pending chapters'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

// ── Accordion ─────────────────────────────────────────────────────────────────

const toggleTitle = (titleKey) => {
  const next = new Set(expandedTitles.value)
  if (next.has(titleKey)) {
    next.delete(titleKey)
  } else {
    next.add(titleKey)
  }
  expandedTitles.value = next
}

// ── Individual chapter actions ────────────────────────────────────────────────

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
    console.error(err)
  }
}

const closeDetailsModal = () => {
  showDetailsModal.value = false
  chapterDetails.value = null
}

const acceptChapter = async (chapterId) => {
  if (!confirm('Accept this chapter? It will be published.')) return

  isProcessing.value = true
  processingId.value = chapterId

  try {
    const result = await adminApi.acceptChapter(chapterId)
    if (result.success) {
      successMessage.value = result.message || 'Chapter accepted!'
      pendingChapters.value = pendingChapters.value.filter(c => c.id !== chapterId)
      closeDetailsModal()
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to accept chapter'
    console.error(err)
  } finally {
    isProcessing.value = false
    processingId.value = null
  }
}

const promptReject = (chapterId) => {
  processingId.value = chapterId
  showRejectModal.value = true
}

const confirmRejectChapter = async () => {
  const chapterId = processingId.value
  if (!chapterId) return

  isProcessing.value = true

  try {
    const result = await adminApi.rejectChapter(chapterId, rejectReason.value)
    if (result.success) {
      successMessage.value = result.message || 'Chapter rejected!'
      pendingChapters.value = pendingChapters.value.filter(c => c.id !== chapterId)
      closeDetailsModal()
      showRejectModal.value = false
      rejectReason.value = ''
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to reject chapter'
    console.error(err)
  } finally {
    isProcessing.value = false
    processingId.value = null
  }
}

// ── Mass approve ──────────────────────────────────────────────────────────────

const massApproveTeam = async (titleGroup, teamGroup) => {
  const count = teamGroup.chapters.length
  if (!confirm(`Release all ${count} chapter(s) from "${teamGroup.teamName}" for "${titleGroup.titleName}"?`)) return

  const key = titleGroup.titleKey + teamGroup.teamName
  massProcessingKey.value = key
  isProcessing.value = true

  const ids = teamGroup.chapters.map(c => c.id)

  try {
    const result = await adminApi.massApproveChapters(ids)
    if (result.success) {
      successMessage.value = result.message || `${count} chapter(s) released!`
      pendingChapters.value = pendingChapters.value.filter(c => !ids.includes(c.id))
    } else {
      error.value = result.error
    }
  } catch (err) {
    error.value = 'Failed to mass approve chapters'
    console.error(err)
  } finally {
    isProcessing.value = false
    massProcessingKey.value = null
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

const formatDate = (dateString) => {
  if (!dateString) return 'Unknown'
  try {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    })
  } catch {
    return 'Invalid date'
  }
}

onMounted(loadPendingChapters)
</script>

<style scoped>
.overflow-y-auto::-webkit-scrollbar { width: 8px; }
.overflow-y-auto::-webkit-scrollbar-track { background: var(--color-background-mute); }
.overflow-y-auto::-webkit-scrollbar-thumb { background: var(--color-border); border-radius: 4px; }
.overflow-y-auto::-webkit-scrollbar-thumb:hover { background: var(--color-border-hover); }
</style>
