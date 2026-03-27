<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Header -->
      <div class="mb-8">
        <div class="flex items-center gap-4 mb-4">
          <button @click="goBack"
                  class="p-2 rounded-md border border-[var(--color-border)] text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </button>
          <div>
            <h1 class="text-3xl font-bold text-[var(--color-heading)]">Add Chapter</h1>
            <p class="text-[var(--color-text)] opacity-75">{{ titleName || 'Loading...' }}</p>
          </div>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="isLoadingForm" class="text-center py-12">
        <svg class="animate-spin mx-auto h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="mt-2 text-[var(--color-text)]">Loading chapter form...</p>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-md p-4">
        <p class="text-sm font-medium text-red-800">Error</p>
        <p class="mt-1 text-sm text-red-700">{{ error }}</p>
      </div>

      <!-- Form -->
      <div v-else class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h2 class="text-xl font-semibold text-[var(--color-heading)]">Chapter Information</h2>
        </div>

        <form @submit.prevent="submitChapter" class="p-6 space-y-6">
          <!-- Metadata row -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Volume Number</label>
              <input type="number" v-model.number="chapterData.volumeNumber" required min="1"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500" />
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Chapter Number</label>
              <input type="number" v-model.number="chapterData.chapterNumber" required min="1" step="0.1"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500" />
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Team</label>
              <select v-model="chapterData.teamId" required
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option value="">Select Team</option>
                <option v-for="team in userTeams" :key="team.id" :value="team.id">{{ team.name }}</option>
              </select>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Chapter Name (Optional)</label>
            <input type="text" v-model="chapterData.name" placeholder="Enter chapter name"
                   class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500" />
          </div>

          <!-- Text content area -->
          <div class="space-y-2">
            <div class="flex items-center justify-between">
              <label class="block text-sm font-medium text-[var(--color-text)]">Chapter Content</label>
              <span class="text-xs text-[var(--color-text)] opacity-60">{{ wordCount }} words</span>
            </div>
            <textarea v-model="chapterData.content"
                      placeholder="Paste or write the chapter text here. Use blank lines to separate paragraphs."
                      rows="30"
                      required
                      class="w-full px-4 py-3 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 resize-y font-serif leading-relaxed text-base"></textarea>
            <p class="text-xs text-[var(--color-text)] opacity-50">
              Separate paragraphs with a blank line. The text will be formatted automatically for readers.
            </p>
          </div>

          <!-- Actions -->
          <div class="flex justify-end gap-3">
            <button type="button" @click="goBack"
                    class="px-5 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] transition-colors">
              Cancel
            </button>
            <button type="submit"
                    :disabled="isSubmitting || !chapterData.content.trim()"
                    class="px-6 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-2">
              <svg v-if="isSubmitting" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              {{ isSubmitting ? 'Submitting...' : 'Submit Chapter' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Success toast -->
      <div v-if="successMessage" class="fixed bottom-4 right-4 bg-green-50 border border-green-200 rounded-md p-4 shadow-lg z-50 flex items-start gap-3">
        <svg class="h-5 w-5 text-green-400 mt-0.5" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
        </svg>
        <p class="text-sm font-medium text-green-800">{{ successMessage }}</p>
        <button @click="successMessage = ''" class="ml-auto text-green-400 hover:text-green-600">
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, reactive, computed, onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { chapterService } from '../../services/chapterService.js'
  import { titleDetailsService } from '../../services/titleDetailsService.js'

  const route = useRoute()
  const router = useRouter()

  const titleName = ref('')
  const isLoadingForm = ref(true)
  const isSubmitting = ref(false)
  const error = ref('')
  const successMessage = ref('')

  const chapterData = reactive({
    titleId: null,
    volumeNumber: 1,
    chapterNumber: 1,
    name: '',
    teamId: '',
    content: ''
  })

  const userTeams = ref([])

  const wordCount = computed(() => {
    return chapterData.content.trim()
      ? chapterData.content.trim().split(/\s+/).length
      : 0
  })

  const loadChapterFormData = async () => {
    try {
      isLoadingForm.value = true
      error.value = ''

      let actualTitleId = null
      const titleIdParam = route.params.titleId
      if (titleIdParam && !isNaN(titleIdParam)) {
        actualTitleId = parseInt(titleIdParam)
      } else {
        // titleSlug is "title-name-{id}" from the new router
        const titleSlugParam = route.params.titleSlug || route.params.titleName
        if (titleSlugParam) {
          const titleResult = await titleDetailsService.getTitleDetails(titleSlugParam)
          if (titleResult.success && titleResult.data) {
            actualTitleId = titleResult.data.id
            titleName.value = titleResult.data.originalTitle
          } else {
            throw new Error(`Title not found.`)
          }
        }
      }

      if (!actualTitleId) throw new Error('Title not found. Please access this page from a valid title.')

      const result = await chapterService.getChapterFormData(actualTitleId)
      if (result.success && result.data) {
        if (!result.data.hasPermission) {
          throw new Error('You do not have permission to add chapters to this title.')
        }
        chapterData.titleId = actualTitleId
        chapterData.volumeNumber = result.data.suggestedVolumeNumber
        chapterData.chapterNumber = result.data.suggestedChapterNumber
        titleName.value = result.data.titleName || titleName.value
        userTeams.value = result.data.userTeams
        if (userTeams.value.length === 1) chapterData.teamId = userTeams.value[0].id
      } else {
        throw new Error(result.error || 'Failed to load chapter form data')
      }
    } catch (err) {
      error.value = err.message
    } finally {
      isLoadingForm.value = false
    }
  }

  const submitChapter = async () => {
    if (!chapterData.content.trim()) { alert('Please enter chapter content.'); return }
    if (!chapterData.teamId) { alert('Please select a team.'); return }

    try {
      isSubmitting.value = true
      const result = await chapterService.createChapter(chapterData.titleId, chapterData)

      if (result.success) {
        successMessage.value = result.message || 'Chapter submitted for review.'
        chapterData.name = ''
        chapterData.chapterNumber = chapterData.chapterNumber + 1
        chapterData.content = ''
        setTimeout(() => router.push('/user/chapters'), 2000)
      } else {
        error.value = result.error
      }
    } catch (err) {
      error.value = err.message || 'Failed to submit chapter'
    } finally {
      isSubmitting.value = false
    }
  }

  const goBack = () => router.back()

  onMounted(loadChapterFormData)
</script>
