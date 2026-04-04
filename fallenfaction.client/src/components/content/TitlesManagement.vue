<template>
  <div class="space-y-6">
    <!-- Section Navigation -->
    <div class="border-b border-[var(--color-border)]">
      <div class="overflow-x-auto scrollbar-hide">
        <nav class="flex min-w-max" aria-label="Title sections">
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

    <!-- Approved Titles Section -->
    <div v-if="activeSection === 'approved'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">My Published Titles</h3>
        <router-link to="/manga/addtitle"
                     class="inline-flex items-center px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
          </svg>
          Add New Title
        </router-link>
      </div>

      <div v-if="titles.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No published titles</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">Get started by creating your first title.</p>
        <div class="mt-6">
          <router-link to="/manga/addtitle"
                       class="inline-flex items-center px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
            </svg>
            Add Title
          </router-link>
        </div>
      </div>

      <div v-else class="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
        <table class="min-w-full divide-y divide-[var(--color-border)]">
          <thead class="bg-[var(--color-background-mute)]">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Title</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Status</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Chapters</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Last Updated</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-[var(--color-background)] divide-y divide-[var(--color-border)]">
            <tr v-for="title in titles" :key="title.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
              <td class="px-6 py-4">
                <div class="flex items-center">
                  <div class="h-12 w-12 flex-shrink-0">
                    <img class="h-12 w-12 rounded object-cover" :src="getImageUrl(title.coverImagePath)" :alt="title.originalTitle" />
                  </div>
                  <div class="ml-4">
                    <div class="text-sm font-medium text-[var(--color-text)]">{{ title.englishTitle || title.originalTitle }}</div>
                    <div v-if="title.originalTitle !== title.englishTitle" class="text-sm text-[var(--color-text)] opacity-75">{{ title.originalTitle }}</div>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span :class="[
                  'inline-flex px-2 py-1 text-xs font-semibold rounded-full',
                  getStatusColor(title.statusTitle)
                ]">
                  {{ title.statusTitle }}
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                {{ title.chapterCount || 0 }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] opacity-75">
                {{ formatDate(title.lastUpdated) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                <router-link :to="`/${buildTitleSlug(title.originalTitle, title.id)}`"
                             class="text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]">
                  View
                </router-link>
                <router-link :to="`/${buildTitleSlug(title.originalTitle, title.id)}/AddChapter`"
                             class="text-blue-600 hover:text-blue-700">
                  Add Chapter
                </router-link>
                <button @click="deleteTitle(title.id)"
                        class="text-red-600 hover:text-red-700">
                  Delete
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Pending Titles Section -->
    <div v-if="activeSection === 'pending'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Pending Review</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">These titles are waiting for admin approval</p>
      </div>

      <div v-if="pendingTitles.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No pending titles</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All your titles have been reviewed.</p>
      </div>

      <div v-else class="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
        <table class="min-w-full divide-y divide-[var(--color-border)]">
          <thead class="bg-[var(--color-background-mute)]">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Title</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Submitted</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Status</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-[var(--color-background)] divide-y divide-[var(--color-border)]">
            <tr v-for="title in pendingTitles" :key="title.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
              <td class="px-6 py-4">
                <div class="flex items-center">
                  <div class="h-12 w-12 flex-shrink-0">
                    <img class="h-12 w-12 rounded object-cover" :src="getImageUrl(title.coverImagePath)" :alt="title.originalTitle" />
                  </div>
                  <div class="ml-4">
                    <div class="text-sm font-medium text-[var(--color-text)]">{{ title.englishTitle || title.originalTitle }}</div>
                    <div v-if="title.originalTitle !== title.englishTitle" class="text-sm text-[var(--color-text)] opacity-75">{{ title.originalTitle }}</div>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] opacity-75">
                {{ formatDate(title.createdAt) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-yellow-100 text-yellow-800">
                  Pending Review
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <button @click="viewTitleDetails(title)"
                        class="text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]">
                  View Details
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Rejected Titles Section -->
    <div v-if="activeSection === 'rejected'">
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-medium text-[var(--color-heading)]">Rejected Titles</h3>
        <p class="text-sm text-[var(--color-text)] opacity-75">These titles were rejected during review</p>
      </div>

      <div v-if="rejectedTitles.length === 0" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No rejected titles</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">All your submissions have been approved.</p>
      </div>

      <div v-else class="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
        <table class="min-w-full divide-y divide-[var(--color-border)]">
          <thead class="bg-[var(--color-background-mute)]">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Title</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Rejected Date</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Reason</th>
              <th class="px-6 py-3 text-right text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-[var(--color-background)] divide-y divide-[var(--color-border)]">
            <tr v-for="title in rejectedTitles" :key="title.id" class="hover:bg-[var(--color-background-mute)] transition-colors duration-150">
              <td class="px-6 py-4">
                <div class="flex items-center">
                  <div class="h-12 w-12 flex-shrink-0">
                    <img class="h-12 w-12 rounded object-cover" :src="getImageUrl(title.coverImagePath)" :alt="title.originalTitle" />
                  </div>
                  <div class="ml-4">
                    <div class="text-sm font-medium text-[var(--color-text)]">{{ title.englishTitle || title.originalTitle }}</div>
                    <div v-if="title.originalTitle !== title.englishTitle" class="text-sm text-[var(--color-text)] opacity-75">{{ title.originalTitle }}</div>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] opacity-75">
                {{ formatDate(title.rejectedAt) }}
              </td>
              <td class="px-6 py-4 text-sm text-[var(--color-text)] opacity-75">
                {{ title.rejectionReason || 'No reason provided' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                <button @click="viewTitleDetails(title)"
                        class="text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]">
                  View Details
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Title Details Modal -->
    <div v-if="showDetailsModal" class="fixed inset-0 bg-[var(--color-background)] bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-2xl w-full max-h-[80vh] overflow-y-auto">
        <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
          <h3 class="text-lg font-semibold text-[var(--color-heading)]">Title Details</h3>
          <button @click="closeDetailsModal" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>

        <div v-if="selectedTitle" class="p-6 space-y-4">
          <div class="flex">
            <img class="h-32 w-24 object-cover rounded" :src="getImageUrl(selectedTitle.coverImagePath)" :alt="selectedTitle.originalTitle" />
            <div class="ml-4 flex-1">
              <h4 class="text-xl font-semibold text-[var(--color-heading)]">{{ selectedTitle.englishTitle || selectedTitle.originalTitle }}</h4>
              <p v-if="selectedTitle.originalTitle !== selectedTitle.englishTitle" class="text-[var(--color-text)] opacity-75">{{ selectedTitle.originalTitle }}</p>
              <p class="text-sm text-[var(--color-text)] mt-2">{{ selectedTitle.description }}</p>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Type</label>
              <p class="text-[var(--color-text)]">{{ getMangaType(selectedTitle.type) }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Release Date</label>
              <p class="text-[var(--color-text)]">{{ selectedTitle.releaseDate || 'Unknown' }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Status</label>
              <p class="text-[var(--color-text)]">{{ selectedTitle.statusTitle || 'Unknown' }}</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Age Restriction</label>
              <p class="text-[var(--color-text)]">{{ selectedTitle.ageRestriction || 0 }}+</p>
            </div>
          </div>

          <div v-if="selectedTitle.authors?.length > 0">
            <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Authors</label>
            <p class="text-[var(--color-text)]">{{ selectedTitle.authors.map(a => a.name || a).join(', ') }}</p>
          </div>

          <div v-if="selectedTitle.categories?.length > 0">
            <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Categories</label>
            <div class="flex flex-wrap gap-1 mt-1">
              <span v-for="category in selectedTitle.categories" :key="category.id"
                    class="inline-flex px-2 py-1 text-xs font-medium bg-[var(--color-background-mute)] text-[var(--color-text)] rounded">
                {{ category.name || category }}
              </span>
            </div>
          </div>

          <div v-if="selectedTitle.tags?.length > 0">
            <label class="block text-sm font-medium text-[var(--color-text)] opacity-60">Tags</label>
            <div class="flex flex-wrap gap-1 mt-1">
              <span v-for="tag in selectedTitle.tags" :key="tag.id"
                    class="inline-flex px-2 py-1 text-xs font-medium bg-[var(--color-background-mute)] text-[var(--color-text)] rounded">
                {{ tag.name || tag }}
              </span>
            </div>
          </div>
        </div>

        <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end">
          <button @click="closeDetailsModal"
                  class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Close
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed } from 'vue'
  import contentService from '../../services/contentService'
  import { buildTitleSlug } from '@/utils/titleSlug.js'

  // Props
  const props = defineProps({
    titles: {
      type: Array,
      default: () => []
    },
    pendingTitles: {
      type: Array,
      default: () => []
    },
    rejectedTitles: {
      type: Array,
      default: () => []
    }
  })

  // Emits
  const emit = defineEmits(['refresh'])

  // Reactive data
  const activeSection = ref('approved')
  const showDetailsModal = ref(false)
  const selectedTitle = ref(null)

  // Computed properties
  const sections = computed(() => [
    { id: 'approved', label: 'Published', count: props.titles.length },
    { id: 'pending', label: 'Pending Review', count: props.pendingTitles.length },
    { id: 'rejected', label: 'Rejected', count: props.rejectedTitles.length }
  ])

  // Methods
  const getImageUrl = (imagePath) => {
    if (!imagePath) return '/img/logo.png'
    if (imagePath.startsWith('http')) return imagePath
    return imagePath.startsWith('/') ? imagePath : `/${imagePath}`
  }

  const getStatusColor = (status) => {
    switch (status?.toLowerCase()) {
      case 'completed':
        return 'bg-green-100 text-green-800'
      case 'ongoing':
        return 'bg-blue-100 text-blue-800'
      case 'hiatus':
        return 'bg-yellow-100 text-yellow-800'
      case 'cancelled':
        return 'bg-red-100 text-red-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getMangaType = (type) => {
    const types = {
      1: 'Novel',
      2: 'Light Novel',
      3: 'Web Novel',
      4: 'Short Story'
    }
    return types[type] || 'Novel'
  }

  const formatDate = (date) => {
    if (!date) return 'Unknown'
    return new Date(date).toLocaleDateString()
  }

  const viewTitleDetails = (title) => {
    selectedTitle.value = title
    showDetailsModal.value = true
  }

  const closeDetailsModal = () => {
    showDetailsModal.value = false
    selectedTitle.value = null
  }

  const deleteTitle = async (titleId) => {
    if (!confirm('Are you sure you want to delete this title? This action cannot be undone.')) {
      return
    }

    try {
      const result = await contentService.deleteTitle(titleId)
      if (result.success) {
        emit('refresh')
      } else {
        alert(result.error || 'Failed to delete title')
      }
    } catch (error) {
      console.error('Error deleting title:', error)
      alert('Failed to delete title')
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
