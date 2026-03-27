<template>
  <div class="space-y-6">
    <!-- Section Navigation -->
    <div class="border-b border-[var(--color-border)]">
      <div class="overflow-x-auto scrollbar-hide">
        <nav class="flex min-w-max" aria-label="Chapter sections">
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

    <!-- Approved Chapters Section -->
    <div v-if="activeSection === 'approved'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Published Chapters</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">Your chapters that have been approved and published</p>
      </div>

      <div v-if="chapters.approvedChapters?.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No published chapters</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">Your approved chapters will appear here.</p>
      </div>

      <div v-else class="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
        <table class="min-w-full divide-y divide-[var(--color-border)]">
          <thead class="bg-[var(--color-background-mute)]">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Chapter</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Title</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Team</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Published</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-[var(--color-background)] divide-y divide-[var(--color-border)]">
            <tr v-for="chapter in chapters.approvedChapters" :key="chapter.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
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
                {{ formatDate(chapter.releaseDate || chapter.approvedDate) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                <button @click="viewChapter(chapter)"
                        class="text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]">
                  View
                </button>
                <button @click="deleteChapter(chapter.id)"
                        class="text-red-600 hover:text-red-700">
                  Delete
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Pending Chapters Section -->
    <div v-if="activeSection === 'pending'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Pending Review</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">These chapters are waiting for admin approval</p>
      </div>

      <div v-if="chapters.pendingChapters?.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No pending chapters</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All your chapters have been reviewed.</p>
      </div>

      <div v-else class="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
        <table class="min-w-full divide-y divide-[var(--color-border)]">
          <thead class="bg-[var(--color-background-mute)]">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Chapter</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Title</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Team</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Submitted</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Status</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-[var(--color-background)] divide-y divide-[var(--color-border)]">
            <tr v-for="chapter in chapters.pendingChapters" :key="chapter.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
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
                {{ formatDate(chapter.createdAt || chapter.createdDate) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-yellow-100 text-yellow-800">
                  Pending Review
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <button @click="viewChapterDetails(chapter)"
                        class="text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]">
                  View Details
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Rejected Chapters Section -->
    <div v-if="activeSection === 'rejected'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Rejected Chapters</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">These chapters were rejected during review</p>
      </div>

      <div v-if="chapters.rejectedChapters?.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No rejected chapters</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All your chapter submissions have been approved.</p>
      </div>

      <div v-else class="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
        <table class="min-w-full divide-y divide-[var(--color-border)]">
          <thead class="bg-[var(--color-background-mute)]">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Chapter</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Title</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Team</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Rejected</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Reason</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-[var(--color-background)] divide-y divide-[var(--color-border)]">
            <tr v-for="chapter in chapters.rejectedChapters" :key="chapter.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
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
                {{ formatDate(chapter.rejectedAt || chapter.lastUpdatedAt) }}
              </td>
              <td class="px-6 py-4 text-sm text-[var(--color-text)] opacity-75">
                {{ chapter.rejectionReason || 'No reason provided' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <button @click="viewChapterDetails(chapter)"
                        class="text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]">
                  View Details
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Chapter Details Modal -->
    <div v-if="showDetailsModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-4xl w-full max-h-[80vh] overflow-y-auto">
        <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
          <h3 class="text-lg font-semibold text-[var(--color-heading)]">Chapter Details</h3>
          <button @click="closeDetailsModal" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>

        <div v-if="selectedChapter" class="p-6 space-y-6">
          <!-- Chapter Info -->
          <div class="grid grid-cols-2 gap-4">
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

          <div v-if="selectedChapter.name">
            <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Chapter Name</label>
            <p class="text-[var(--color-text)]">{{ selectedChapter.name }}</p>
          </div>

          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Submitted Date</label>
            <p class="text-[var(--color-text)]">{{ formatDate(selectedChapter.createdAt || selectedChapter.createdDate) }}</p>
          </div>

          <div v-if="selectedChapter.rejectionReason">
            <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Rejection Reason</label>
            <p class="text-red-600">{{ selectedChapter.rejectionReason }}</p>
          </div>

          <!-- Chapter Images -->
          <div v-if="selectedChapter.images?.length > 0">
            <label class="block text-sm font-medium text-[var(--color-text)] opacity-60 mb-2">Chapter Images ({{ selectedChapter.images.length }})</label>
            <div class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-3 max-h-64 overflow-y-auto border border-[var(--color-border)] rounded p-3">
              <div v-for="(image, index) in selectedChapter.images" :key="image.id" class="relative">
                <img :src="getImageUrl(image.imagePath)"
                     :alt="`Page ${index + 1}`"
                     class="w-full h-20 object-cover rounded border border-[var(--color-border)]"
                     @error="handleImageError" />
                <div class="absolute bottom-0 left-0 right-0 bg-black bg-opacity-75 text-white text-xs px-1 py-0.5 rounded-b text-center">
                  {{ index + 1 }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
          <button @click="closeDetailsModal"
                  class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Close
          </button>
          <button v-if="selectedChapter.titleName && selectedChapter.chapterNumber"
                  @click="goToChapter"
                  class="px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
            View Chapter
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import contentService from '../../services/contentService'

// Props
const props = defineProps({
  chapters: {
    type: Object,
    default: () => ({
      pendingChapters: [],
      approvedChapters: [],
      rejectedChapters: []
    })
  }
})

// Emits
const emit = defineEmits(['refresh'])

// Router
const router = useRouter()

// Reactive data
const activeSection = ref('approved')
const showDetailsModal = ref(false)
const selectedChapter = ref(null)

// Computed properties
const sections = computed(() => [
  { id: 'approved', label: 'Published', count: props.chapters.approvedChapters?.length || 0 },
  { id: 'pending', label: 'Pending Review', count: props.chapters.pendingChapters?.length || 0 },
  { id: 'rejected', label: 'Rejected', count: props.chapters.rejectedChapters?.length || 0 }
])

// Methods
const formatDate = (date) => {
  if (!date) return 'Unknown'
  return new Date(date).toLocaleDateString()
}

const getImageUrl = (imagePath) => {
  if (!imagePath) return '/img/logo.png'
  if (imagePath.startsWith('http')) return imagePath
  return imagePath.startsWith('/') ? imagePath : `/${imagePath}`
}

const handleImageError = (event) => {
  event.target.src = '/img/logo.png'
}

const viewChapter = (chapter) => {
  if (chapter.titleName && chapter.chapterNumber) {
    const chapterName = chapter.name || chapter.chapterNumber.toString()
    router.push(`/${encodeURIComponent(chapter.titleName)}/chapter/${encodeURIComponent(chapterName)}/v${chapter.volumeNumber}/t${chapter.teamId || 0}`)
  }
}

const viewChapterDetails = async (chapter) => {
  selectedChapter.value = chapter
  showDetailsModal.value = true

  // Try to load detailed chapter information if we have an ID
  if (chapter.id) {
    try {
      const result = await contentService.getChapterDetails(chapter.id)
      if (result.success) {
        selectedChapter.value = {
          ...chapter,
          ...result.data
        }
      }
    } catch (error) {
      console.error('Error loading chapter details:', error)
    }
  }
}

const closeDetailsModal = () => {
  showDetailsModal.value = false
  selectedChapter.value = null
}

const goToChapter = () => {
  if (selectedChapter.value) {
    viewChapter(selectedChapter.value)
    closeDetailsModal()
  }
}

const deleteChapter = async (chapterId) => {
  if (!confirm('Are you sure you want to delete this chapter? This action cannot be undone.')) {
    return
  }

  try {
    const result = await contentService.deleteChapter(chapterId)
    if (result.success) {
      emit('refresh')
    } else {
      alert(result.error || 'Failed to delete chapter')
    }
  } catch (error) {
    console.error('Error deleting chapter:', error)
    alert('Failed to delete chapter')
  }
}
</script>

<style scoped>
  /* Add any component-specific styles here */

  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }

    .scrollbar-hide::-webkit-scrollbar {
      display: none;
    }
</style>
