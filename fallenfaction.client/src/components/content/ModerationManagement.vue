<template>
  <div class="space-y-6">
    <!-- Section Navigation -->
    <div class="border-b border-[var(--color-border)]">
      <div class="overflow-x-auto scrollbar-hide">
        <nav class="flex min-w-max" aria-label="Moderation sections">
          <button v-for="section in sections"
                  :key="section.id"
                  @click="activeSection = section.id"
                  :class="[
                  'whitespace-nowrap py-2 px-1 mr-8 border-b-2 font-medium text-sm transition-colors duration-200 shrink-0',
                  activeSection === section.id
                    ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
                    : 'border-transparent text-[var(--color-text)] opacity-70 hover:opacity-100 hover:border-[var(--color-border-hover)]'
                ]">
            {{ section.label }}
            <span v-if="section.count !== undefined"
                  :class="[
                  'ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
                  activeSection === section.id
                    ? 'bg-[var(--color-accent)] text-white'
                    : 'bg-[var(--color-background-mute)] text-[var(--color-text)]'
                ]">
              {{ section.count }}
            </span>
          </button>
        </nav>
      </div>
    </div>

    <!-- Pending Titles Section -->
    <div v-if="activeSection === 'titles'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Pending Titles</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">Review and approve/reject title submissions</p>
      </div>

      <div v-if="pendingContent.pendingTitles?.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No pending titles</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All title submissions have been reviewed.</p>
      </div>

      <div v-else class="space-y-4">
        <div v-for="title in pendingContent.pendingTitles" :key="title.id"
             class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-6">
          <div class="flex">
            <!-- Cover Image -->
            <div class="flex-shrink-0 mr-6">
              <img class="h-32 w-24 object-cover rounded-lg border border-[var(--color-border)]"
                   :src="getImageUrl(title.coverImagePath)"
                   :alt="title.originalTitle" />
            </div>

            <!-- Title Info -->
            <div class="flex-1">
              <div class="flex justify-between items-start mb-4">
                <div>
                  <h4 class="text-xl font-semibold text-[var(--color-heading)]">{{ title.englishTitle || title.originalTitle }}</h4>
                  <p v-if="title.originalTitle !== title.englishTitle" class="text-[var(--color-text)] opacity-75">{{ title.originalTitle }}</p>
                  <p class="text-sm text-[var(--color-text)] opacity-60 mt-1">Submitted by {{ title.submittedBy || 'Unknown User' }}</p>
                </div>
                <span class="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-yellow-100 text-yellow-800">
                  Pending Review
                </span>
              </div>

              <p class="text-[var(--color-text)] mb-4 line-clamp-3">{{ title.description || 'No description provided' }}</p>

              <!-- Title Details -->
              <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                <div>
                  <span class="text-xs text-[var(--color-text)] opacity-60">Type</span>
                  <p class="text-sm font-medium text-[var(--color-text)]">{{ getMangaType(title.type) }}</p>
                </div>
                <div>
                  <span class="text-xs text-[var(--color-text)] opacity-60">Release Date</span>
                  <p class="text-sm font-medium text-[var(--color-text)]">{{ title.releaseDate || 'Unknown' }}</p>
                </div>
                <div>
                  <span class="text-xs text-[var(--color-text)] opacity-60">Status</span>
                  <p class="text-sm font-medium text-[var(--color-text)]">{{ title.statusTitle || 'Unknown' }}</p>
                </div>
                <div>
                  <span class="text-xs text-[var(--color-text)] opacity-60">Age Rating</span>
                  <p class="text-sm font-medium text-[var(--color-text)]">{{ title.ageRestriction || 0 }}+</p>
                </div>
              </div>

              <!-- Categories and Tags -->
              <div class="space-y-2 mb-4">
                <div v-if="title.categories?.length > 0">
                  <span class="text-xs text-[var(--color-text)] opacity-60">Categories: </span>
                  <span class="text-sm text-[var(--color-text)]">{{ title.categories.map(c => c.name || c).join(', ') }}</span>
                </div>
                <div v-if="title.tags?.length > 0">
                  <span class="text-xs text-[var(--color-text)] opacity-60">Tags: </span>
                  <span class="text-sm text-[var(--color-text)]">{{ title.tags.map(t => t.name || t).join(', ') }}</span>
                </div>
                <div v-if="title.authors?.length > 0">
                  <span class="text-xs text-[var(--color-text)] opacity-60">Authors: </span>
                  <span class="text-sm text-[var(--color-text)]">{{ title.authors.map(a => a.name || a).join(', ') }}</span>
                </div>
              </div>

              <!-- Action Buttons -->
              <div class="flex space-x-3">
                <button @click="viewTitleDetails(title)"
                        class="inline-flex items-center px-3 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
                  <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                  </svg>
                  View Details
                </button>
                <button @click="approveTitle(title.id)"
                        :disabled="processing"
                        class="inline-flex items-center px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 transition-colors duration-200">
                  <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                  </svg>
                  {{ processing ? 'Processing...' : 'Approve' }}
                </button>
                <button @click="rejectTitle(title)"
                        :disabled="processing"
                        class="inline-flex items-center px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 transition-colors duration-200">
                  <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                  </svg>
                  {{ processing ? 'Processing...' : 'Reject' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Pending Chapters Section -->
    <div v-if="activeSection === 'chapters'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Pending Chapters</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">Review and approve/reject chapter submissions</p>
      </div>

      <div v-if="pendingContent.pendingChapters?.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No pending chapters</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All chapter submissions have been reviewed.</p>
      </div>

      <div v-else class="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
        <table class="min-w-full divide-y divide-[var(--color-border)]">
          <thead class="bg-[var(--color-background-mute)]">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Chapter</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Title</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Team</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Submitted</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Images</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-[var(--color-background)] divide-y divide-[var(--color-border)]">
            <tr v-for="chapter in pendingContent.pendingChapters" :key="chapter.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
              <td class="px-6 py-4 whitespace-nowrap">
                <div class="text-sm font-medium text-[var(--color-text)]">
                  Vol.{{ chapter.volumeNumber }} Ch.{{ chapter.chapterNumber }}
                </div>
                <div v-if="chapter.name" class="text-sm text-[var(--color-text)] opacity-75">
                  {{ chapter.name }}
                </div>
              </td>
              <td class="px-6 py-4">
                <div class="text-sm font-medium text-[var(--color-text)]">{{ chapter.titleName }}</div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <div class="text-sm text-[var(--color-text)]">{{ chapter.teamName }}</div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] opacity-75">
                {{ formatDate(chapter.createdDate) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                {{ chapter.imageCount || 0 }} images
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                <button @click="viewChapterDetails(chapter)"
                        class="text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]">
                  View
                </button>
                <button @click="approveChapter(chapter.id)"
                        :disabled="processing"
                        class="text-green-600 hover:text-green-700">
                  Approve
                </button>
                <button @click="rejectChapter(chapter)"
                        :disabled="processing"
                        class="text-red-600 hover:text-red-700">
                  Reject
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Title Details Modal -->
    <div v-if="showTitleModal" class="fixed inset-0 bg-[var(--color-background)] bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-4xl w-full max-h-[80vh] overflow-y-auto">
        <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
          <h3 class="text-lg font-semibold text-[var(--color-heading)]">Title Review</h3>
          <button @click="closeTitleModal" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>

        <div v-if="selectedTitle" class="p-6 space-y-6">
          <!-- Full title details would go here -->
          <div class="text-center">
            <p class="text-[var(--color-text)]">Full title review interface would be implemented here.</p>
            <p class="text-sm text-[var(--color-text)] opacity-75 mt-2">This would show all title details, images, and metadata for thorough review.</p>
          </div>
        </div>

        <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
          <button @click="closeTitleModal"
                  class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Close
          </button>
        </div>
      </div>
    </div>

    <!-- Chapter Details Modal -->
    <div v-if="showChapterModal" class="fixed inset-0 bg-[var(--color-background)] bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-6xl w-full max-h-[80vh] overflow-y-auto">
        <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
          <h3 class="text-lg font-semibold text-[var(--color-heading)]">Chapter Review</h3>
          <button @click="closeChapterModal" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>

        <div v-if="selectedChapter" class="p-6 space-y-6">
          <!-- Chapter Info -->
          <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Title</label>
              <p class="text-[var(--color-text)] font-medium">{{ selectedChapter.titleName }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Team</label>
              <p class="text-[var(--color-text)]">{{ selectedChapter.teamName }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Volume</label>
              <p class="text-[var(--color-text)]">{{ selectedChapter.volumeNumber }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Chapter</label>
              <p class="text-[var(--color-text)]">{{ selectedChapter.chapterNumber }}</p>
            </div>
          </div>

          <!-- Chapter Images Preview -->
          <div class="text-center">
            <p class="text-[var(--color-text)]">Chapter image review interface would be implemented here.</p>
            <p class="text-sm text-[var(--color-text)] opacity-75 mt-2">This would show all chapter pages for quality review.</p>
          </div>
        </div>

        <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
          <button @click="closeChapterModal"
                  class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Close
          </button>
        </div>
      </div>
    </div>

    <!-- Rejection Reason Modal -->
    <div v-if="showRejectModal" class="fixed inset-0 bg-[var(--color-background)] bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-md w-full">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h3 class="text-lg font-semibold text-[var(--color-heading)]">Rejection Reason</h3>
        </div>

        <div class="p-6">
          <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
            Please provide a reason for rejection:
          </label>
          <textarea v-model="rejectionReason"
                    rows="4"
                    class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-red-500"
                    placeholder="Enter rejection reason..."></textarea>
        </div>

        <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
          <button @click="closeRejectModal"
                  class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Cancel
          </button>
          <button @click="confirmRejection"
                  :disabled="!rejectionReason.trim() || processing"
                  class="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 transition-colors duration-200">
            {{ processing ? 'Rejecting...' : 'Reject' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import contentService from '../../services/contentService'

// Props
const props = defineProps({
  pendingContent: {
    type: Object,
    default: () => ({
      pendingTitles: [],
      pendingChapters: []
    })
  }
})

// Emits
const emit = defineEmits(['refresh'])

// Reactive data
const activeSection = ref('titles')
const processing = ref(false)
const showTitleModal = ref(false)
const showChapterModal = ref(false)
const showRejectModal = ref(false)
const selectedTitle = ref(null)
const selectedChapter = ref(null)
const rejectionReason = ref('')
const rejectionTarget = ref(null)
const rejectionType = ref('') // 'title' or 'chapter'

// Computed properties
const sections = computed(() => [
  { id: 'titles', label: 'Pending Titles', count: props.pendingContent.pendingTitles?.length || 0 },
  { id: 'chapters', label: 'Pending Chapters', count: props.pendingContent.pendingChapters?.length || 0 }
])

// Methods
const getImageUrl = (imagePath) => {
  if (!imagePath) return '/img/logo.png'
  if (imagePath.startsWith('http')) return imagePath
  return imagePath.startsWith('/') ? imagePath : `/${imagePath}`
}

const getMangaType = (type) => {
  const types = {
    1: 'Novel',
    2: 'Light Novel',
    3: 'Web Novel',
    4: 'Short Story',
    5: 'Wuxia',
    6: 'Xianxia',
    7: 'Xuanhuan',
    8: 'Classic Fiction'
  }
  return types[type] || 'Novel'
}

const formatDate = (date) => {
  if (!date) return 'Unknown'
  return new Date(date).toLocaleDateString()
}

const viewTitleDetails = (title) => {
  selectedTitle.value = title
  showTitleModal.value = true
}

const closeTitleModal = () => {
  showTitleModal.value = false
  selectedTitle.value = null
}

const viewChapterDetails = (chapter) => {
  selectedChapter.value = chapter
  showChapterModal.value = true
}

const closeChapterModal = () => {
  showChapterModal.value = false
  selectedChapter.value = null
}

const approveTitle = async (titleId) => {
  if (processing.value) return

  if (!confirm('Are you sure you want to approve this title?')) {
    return
  }

  processing.value = true
  try {
    const result = await contentService.approveTitle(titleId)
    if (result.success) {
      emit('refresh')
      // Show success message
    } else {
      alert(result.error || 'Failed to approve title')
    }
  } catch (error) {
    console.error('Error approving title:', error)
    alert('Failed to approve title')
  } finally {
    processing.value = false
  }
}

const rejectTitle = (title) => {
  rejectionTarget.value = title
  rejectionType.value = 'title'
  rejectionReason.value = ''
  showRejectModal.value = true
}

const approveChapter = async (chapterId) => {
  if (processing.value) return

  if (!confirm('Are you sure you want to approve this chapter?')) {
    return
  }

  processing.value = true
  try {
    const result = await contentService.approveChapter(chapterId)
    if (result.success) {
      emit('refresh')
      // Show success message
    } else {
      alert(result.error || 'Failed to approve chapter')
    }
  } catch (error) {
    console.error('Error approving chapter:', error)
    alert('Failed to approve chapter')
  } finally {
    processing.value = false
  }
}

const rejectChapter = (chapter) => {
  rejectionTarget.value = chapter
  rejectionType.value = 'chapter'
  rejectionReason.value = ''
  showRejectModal.value = true
}

const closeRejectModal = () => {
  showRejectModal.value = false
  rejectionTarget.value = null
  rejectionType.value = ''
  rejectionReason.value = ''
}

const confirmRejection = async () => {
  if (!rejectionReason.value.trim() || processing.value) return

  processing.value = true
  try {
    let result
    if (rejectionType.value === 'title') {
      result = await contentService.rejectTitle(rejectionTarget.value.id, rejectionReason.value)
    } else if (rejectionType.value === 'chapter') {
      result = await contentService.rejectChapter(rejectionTarget.value.id, rejectionReason.value)
    }

    if (result.success) {
      emit('refresh')
      closeRejectModal()
      // Show success message
    } else {
      alert(result.error || 'Failed to reject content')
    }
  } catch (error) {
    console.error('Error rejecting content:', error)
    alert('Failed to reject content')
  } finally {
    processing.value = false
  }
}
</script>

<style scoped>
  .line-clamp-3 {
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }

    .scrollbar-hide::-webkit-scrollbar {
      display: none;
    }
</style>
