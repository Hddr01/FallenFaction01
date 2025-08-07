<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <div class="flex items-center gap-4 mb-4">
          <button @click="goBack"
                  class="p-2 rounded-md border border-[var(--color-border)] text-[var(--color-text)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"></path>
            </svg>
          </button>
          <div>
            <h1 class="text-3xl font-bold text-[var(--color-heading)]">Add Chapter</h1>
            <p class="text-[var(--color-text)] opacity-75">{{ titleName || 'Loading...' }}</p>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="isLoadingForm" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading chapter form...</span>
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
            <h3 class="text-sm font-medium text-red-800">Error</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Chapter Form -->
      <div v-else class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h2 class="text-xl font-semibold text-[var(--color-heading)]">Chapter Information</h2>
        </div>

        <form @submit.prevent="submitChapter" class="p-6 space-y-6">
          <!-- Chapter Details -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Volume Number:</label>
              <input type="number"
                     v-model.number="chapterData.volumeNumber"
                     required
                     min="1"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Chapter Number:</label>
              <input type="number"
                     v-model.number="chapterData.chapterNumber"
                     required
                     min="1"
                     step="0.1"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Team:</label>
              <select v-model="chapterData.teamId"
                      required
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                <option value="">Select Team</option>
                <option v-for="team in userTeams" :key="team.id" :value="team.id">
                  {{ team.name }}
                </option>
              </select>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Chapter Name (Optional):</label>
            <input type="text"
                   v-model="chapterData.name"
                   placeholder="Enter chapter name"
                   class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
          </div>

          <!-- Image Upload Section -->
          <div class="space-y-4">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Chapter Images</h3>

            <!-- Upload Buttons -->
            <div class="flex gap-3">
              <button type="button"
                      @click="showIndividualUpload = true"
                      class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors duration-200">
                <svg class="w-4 h-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
                </svg>
                Add Individual Images
              </button>

              <button type="button"
                      @click="showMassUpload = true"
                      class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
                <svg class="w-4 h-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
                </svg>
                Mass Upload Images
              </button>
            </div>

            <!-- Main Preview Area -->
            <div class="border-2 border-dashed border-[var(--color-border)] rounded-lg p-6 min-h-32">
              <div v-if="sortedImages.length === 0" class="text-center py-8">
                <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
                </svg>
                <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No images added</h3>
                <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">Use the buttons above to add chapter images</p>
              </div>

              <div v-else
                   ref="sortableContainer"
                   class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
                <div v-for="(image, index) in sortedImages"
                     :key="image.id"
                     :data-id="image.id"
                     class="relative group bg-[var(--color-background-mute)] rounded-lg p-2 cursor-move">
                  <img :src="image.preview"
                       :alt="`Page ${index + 1}`"
                       class="w-full h-24 object-cover rounded border border-[var(--color-border)]"
                       @error="handleImageError">

                  <!-- Order Badge -->
                  <div class="absolute top-1 left-1 bg-black bg-opacity-75 text-white text-xs px-2 py-1 rounded">
                    {{ index + 1 }}
                  </div>

                  <!-- Remove Button -->
                  <button type="button"
                          @click="removeImage(image.id)"
                          class="absolute top-1 right-1 w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600 opacity-0 group-hover:opacity-100 transition-all duration-200">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                  </button>
                </div>
              </div>

              <p v-if="sortedImages.length > 0" class="text-xs text-[var(--color-text)] opacity-60 mt-4">
                Drag and drop to reorder images. The order will be preserved in the final chapter.
              </p>
            </div>
          </div>

          <!-- Submit Button -->
          <div class="flex justify-end space-x-4">
            <button type="button"
                    @click="goBack"
                    class="px-6 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] transition-colors duration-200">
              Cancel
            </button>
            <button type="submit"
                    :disabled="isSubmitting || sortedImages.length === 0"
                    class="px-6 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
              <svg v-if="isSubmitting" class="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              {{ isSubmitting ? 'Creating Chapter...' : 'Create Chapter' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Individual Upload Modal -->
      <div v-if="showIndividualUpload" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-2xl w-full max-h-[80vh] overflow-y-auto">
          <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Add Individual Images</h3>
            <button @click="closeIndividualUpload" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>

          <div class="p-6 space-y-4">
            <div v-for="(input, index) in individualInputs" :key="input.id" class="flex items-center gap-3 p-3 bg-[var(--color-background-mute)] rounded-md">
              <span class="text-sm font-medium text-[var(--color-text)] min-w-20">Image #{{ index + 1 }}</span>

              <div class="flex-1">
                <input :ref="el => setFileInputRef(el, input.id)"
                       type="file"
                       accept="image/*"
                       @change="handleIndividualImageChange($event, input.id)"
                       class="hidden" />
                <button type="button"
                        @click="triggerFileInput(input.id)"
                        class="px-3 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors duration-200">
                  Choose File
                </button>
                <span class="ml-2 text-sm text-[var(--color-text)]">{{ input.fileName || 'No file chosen' }}</span>
              </div>

              <img v-if="input.preview"
                   :src="input.preview"
                   alt="Preview"
                   class="w-12 h-12 object-cover rounded border border-[var(--color-border)]" />

              <button type="button"
                      @click="removeIndividualInput(input.id)"
                      class="p-2 text-red-600 hover:text-red-700 hover:bg-red-50 rounded-md transition-colors duration-200">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                </svg>
              </button>
            </div>

            <button type="button"
                    @click="addIndividualInput"
                    class="w-full py-2 border-2 border-dashed border-[var(--color-border)] text-[var(--color-text)] rounded-md hover:border-green-500 hover:text-green-600 transition-colors duration-200">
              <svg class="w-4 h-4 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
              </svg>
              Add More Images
            </button>
          </div>

          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button type="button"
                    @click="closeIndividualUpload"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] transition-colors duration-200">
              Cancel
            </button>
            <button type="button"
                    @click="applyIndividualUpload"
                    class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors duration-200">
              Apply
            </button>
          </div>
        </div>
      </div>

      <!-- Mass Upload Modal -->
      <div v-if="showMassUpload" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-[var(--color-background-soft)] rounded-lg shadow-xl max-w-4xl w-full max-h-[80vh] overflow-y-auto">
          <div class="px-6 py-4 border-b border-[var(--color-border)] flex justify-between items-center">
            <h3 class="text-lg font-semibold text-[var(--color-heading)]">Mass Upload Images</h3>
            <button @click="closeMassUpload" class="text-[var(--color-text)] hover:text-[var(--color-heading)] focus:outline-none">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>

          <div class="p-6 space-y-4">
            <!-- File Input -->
            <div class="border-2 border-dashed border-[var(--color-border)] rounded-lg p-6 text-center">
              <input ref="massUploadInput"
                     type="file"
                     multiple
                     accept="image/*"
                     @change="handleMassUpload"
                     class="hidden" />
              <button type="button"
                      @click="$refs.massUploadInput.click()"
                      class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
                <svg class="w-5 h-5 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"></path>
                </svg>
                Choose Multiple Files
              </button>
              <p class="text-sm text-[var(--color-text)] opacity-75 mt-2">
                {{ massUploadFiles.length > 0 ? `${massUploadFiles.length} files selected` : 'No files chosen' }}
              </p>
            </div>

            <!-- Mass Upload Preview -->
            <div v-if="massUploadPreviews.length > 0" class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-3 max-h-64 overflow-y-auto">
              <div v-for="(preview, index) in massUploadPreviews" :key="index" class="relative">
                <img :src="preview"
                     alt="`Preview ${index + 1}`"
                     class="w-full h-20 object-cover rounded border border-[var(--color-border)]" />
                <div class="absolute bottom-0 left-0 right-0 bg-black bg-opacity-75 text-white text-xs px-1 py-0.5 rounded-b text-center">
                  {{ index + 1 }}
                </div>
              </div>
            </div>
          </div>

          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end space-x-3">
            <button type="button"
                    @click="closeMassUpload"
                    class="px-4 py-2 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] transition-colors duration-200">
              Cancel
            </button>
            <button type="button"
                    @click="applyMassUpload"
                    :disabled="massUploadFiles.length === 0"
                    class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:opacity-50 transition-colors duration-200">
              Apply {{ massUploadFiles.length }} Images
            </button>
          </div>
        </div>
      </div>

      <!-- Success Message -->
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
  import { ref, reactive, onMounted, computed, nextTick, onUnmounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { chapterService } from '../../services/chapterService.js'
  import { titleDetailsService } from '../../services/titleDetailsService.js'
  import Sortable from 'sortablejs'

  // Rest of your component code stays the same...

  // Router and route
  const route = useRoute()
  const router = useRouter()
  const titleName = ref('')

  // State
  const isLoadingForm = ref(true)
  const isSubmitting = ref(false)
  const error = ref('')
  const successMessage = ref('')

  // Form data
  const chapterData = reactive({
    titleId: null,
    volumeNumber: 1,
    chapterNumber: 1,
    name: '',
    teamId: ''
  })

  const userTeams = ref([])

  // Image handling
  const images = ref([])
  const sortedImages = ref([])
  let imageIdCounter = 0
  let sortableInstance = null

  // Modal states
  const showIndividualUpload = ref(false)
  const showMassUpload = ref(false)
  const individualInputs = ref([])
  const massUploadFiles = ref([])
  const massUploadPreviews = ref([])

  // File input refs
  const fileInputRefs = ref({})


  const loadChapterFormData = async () => {
    try {
      isLoadingForm.value = true
      error.value = ''

      let actualTitleId = null

      // Check if we have a direct numeric titleId parameter
      const titleIdParam = route.params.titleId
      if (titleIdParam && !isNaN(titleIdParam)) {
        actualTitleId = parseInt(titleIdParam)
      } else {
        // We have a titleName parameter, need to get the title ID first
        const titleNameParam = route.params.titleName
        if (titleNameParam) {
          console.log('Getting title ID for title name:', titleNameParam)

          // Use titleDetailsService to get title details by name
          const titleResult = await titleDetailsService.getTitleDetails(titleNameParam)

          if (titleResult.success && titleResult.data) {
            actualTitleId = titleResult.data.id
            titleName.value = titleResult.data.originalTitle
            console.log('Found title ID:', actualTitleId)
          } else {
            throw new Error(`Title "${titleNameParam}" not found. ${titleResult.error || ''}`)
          }
        }
      }

      if (!actualTitleId) {
        throw new Error('Title ID not found. Please ensure you access this page from a valid title.')
      }

      // Now get the chapter creation form data
      const result = await chapterService.getChapterFormData(actualTitleId)

      if (result.success && result.data) {
        if (!result.data.hasPermission) {
          throw new Error('You do not have permission to add chapters to this title. Make sure you are part of a team that has access to this title.')
        }

        chapterData.titleId = actualTitleId
        chapterData.volumeNumber = result.data.suggestedVolumeNumber
        chapterData.chapterNumber = result.data.suggestedChapterNumber
        titleName.value = result.data.titleName || titleName.value
        userTeams.value = result.data.userTeams

        if (userTeams.value.length === 1) {
          chapterData.teamId = userTeams.value[0].id
        }
      } else {
        throw new Error(result.error || 'Failed to load chapter form data')
      }
    } catch (err) {
      console.error('Error loading chapter form data:', err)
      error.value = err.message || 'Failed to load chapter form data'
    } finally {
      isLoadingForm.value = false
    }
  }

  // Also update the titleId computed property to be simpler:
  const titleId = computed(() => {
    return chapterData.titleId || null
  })

  const initializeSortable = async () => {
    await nextTick()
    const container = document.querySelector('[ref="sortableContainer"]')
    if (container && !sortableInstance) {
      sortableInstance = new Sortable(container, {
        animation: 150,
        ghostClass: 'opacity-50',
        onEnd: (evt) => {
          const oldIndex = evt.oldIndex
          const newIndex = evt.newIndex

          if (oldIndex !== newIndex) {
            const movedImage = sortedImages.value.splice(oldIndex, 1)[0]
            sortedImages.value.splice(newIndex, 0, movedImage)
          }
        }
      })
    }
  }

  const destroySortable = () => {
    if (sortableInstance) {
      sortableInstance.destroy()
      sortableInstance = null
    }
  }

  // Individual upload methods
  const addIndividualInput = () => {
    individualInputs.value.push({
      id: `input_${Date.now()}_${Math.random()}`,
      fileName: '',
      preview: null,
      file: null
    })
  }

  const removeIndividualInput = (inputId) => {
    const index = individualInputs.value.findIndex(input => input.id === inputId)
    if (index > -1) {
      const input = individualInputs.value[index]
      if (input.preview) {
        URL.revokeObjectURL(input.preview)
      }
      individualInputs.value.splice(index, 1)
    }
  }

  const setFileInputRef = (el, inputId) => {
    if (el) {
      fileInputRefs.value[inputId] = el
    }
  }

  const triggerFileInput = (inputId) => {
    const input = fileInputRefs.value[inputId]
    if (input) {
      input.click()
    }
  }

  const handleIndividualImageChange = (event, inputId) => {
    const file = event.target.files[0]
    if (!file) return

    const input = individualInputs.value.find(input => input.id === inputId)
    if (!input) return

    // Validate file
    if (!file.type.startsWith('image/')) {
      alert('Please select a valid image file')
      return
    }

    if (file.size > 10 * 1024 * 1024) { // 10MB limit
      alert('File size must be less than 10MB')
      return
    }

    // Clear previous preview
    if (input.preview) {
      URL.revokeObjectURL(input.preview)
    }

    input.fileName = file.name
    input.file = file
    input.preview = URL.createObjectURL(file)
  }

  const applyIndividualUpload = () => {
    const validInputs = individualInputs.value.filter(input => input.file)

    validInputs.forEach(input => {
      const imageData = {
        id: `img_${++imageIdCounter}`,
        file: input.file,
        preview: input.preview,
        fileName: input.fileName
      }
      images.value.push(imageData)
      sortedImages.value.push(imageData)
    })

    closeIndividualUpload()
    nextTick(() => {
      initializeSortable()
    })
  }

  const closeIndividualUpload = () => {
    showIndividualUpload.value = false
    // Clear individual inputs
    individualInputs.value.forEach(input => {
      if (input.preview) {
        URL.revokeObjectURL(input.preview)
      }
    })
    individualInputs.value = []
    fileInputRefs.value = {}
  }

  // Mass upload methods
  const handleMassUpload = (event) => {
    const files = Array.from(event.target.files)
    massUploadFiles.value = files
    massUploadPreviews.value = []

    files.forEach(file => {
      if (file.type.startsWith('image/')) {
        const reader = new FileReader()
        reader.onload = (e) => {
          massUploadPreviews.value.push(e.target.result)
        }
        reader.readAsDataURL(file)
      }
    })
  }

  const applyMassUpload = () => {
    massUploadFiles.value.forEach(file => {
      const imageData = {
        id: `img_${++imageIdCounter}`,
        file: file,
        preview: URL.createObjectURL(file),
        fileName: file.name
      }
      images.value.push(imageData)
      sortedImages.value.push(imageData)
    })

    closeMassUpload()
    nextTick(() => {
      initializeSortable()
    })
  }

  const closeMassUpload = () => {
    showMassUpload.value = false
    massUploadFiles.value = []
    massUploadPreviews.value = []
    if (document.querySelector('[ref="massUploadInput"]')) {
      document.querySelector('[ref="massUploadInput"]').value = ''
    }
  }

  // Image management
  const removeImage = (imageId) => {
    const imageIndex = sortedImages.value.findIndex(img => img.id === imageId)
    if (imageIndex > -1) {
      const image = sortedImages.value[imageIndex]
      if (image.preview) {
        URL.revokeObjectURL(image.preview)
      }
      sortedImages.value.splice(imageIndex, 1)
      images.value = images.value.filter(img => img.id !== imageId)
    }
  }

  const handleImageError = (event) => {
    event.target.src = '/img/logo.png' // Fallback image
  }

  // Form submission
  const submitChapter = async () => {
    if (sortedImages.value.length === 0) {
      alert('Please add at least one image before submitting.')
      return
    }

    if (!chapterData.teamId) {
      alert('Please select a team.')
      return
    }

    try {
      isSubmitting.value = true

      // Use chapterService for submission
      const result = await chapterService.createChapter(
        chapterData.titleId,
        chapterData,
        sortedImages.value
      )

      if (result.success) {
        successMessage.value = result.message

        // Clear form
        chapterData.name = ''
        chapterData.chapterNumber = chapterData.chapterNumber + 1
        clearAllImages()

        // Redirect after delay
        setTimeout(() => {
          router.push(`/user/chapters`) // Or wherever user's chapters are managed
        }, 2000)
      } else {
        error.value = result.error
      }
    } catch (err) {
      console.error('Error submitting chapter:', err)
      error.value = err.message || 'Failed to submit chapter'
    } finally {
      isSubmitting.value = false
    }
  }

  const clearAllImages = () => {
    sortedImages.value.forEach(image => {
      if (image.preview) {
        URL.revokeObjectURL(image.preview)
      }
    })
    sortedImages.value = []
    images.value = []
    destroySortable()
  }

  const goBack = () => {
    router.back()
  }

  // Initialize individual inputs with one empty input
  const initializeIndividualInputs = () => {
    individualInputs.value = [{
      id: `input_${Date.now()}`,
      fileName: '',
      preview: null,
      file: null
    }]
  }

  // Lifecycle
  onMounted(async () => {
    console.log('AddChapter component mounted')
    await loadChapterFormData()
    initializeIndividualInputs()
  })

  onUnmounted(() => {
    clearAllImages()
    destroySortable()

    // Clean up individual upload previews
    individualInputs.value.forEach(input => {
      if (input.preview) {
        URL.revokeObjectURL(input.preview)
      }
    })
  })
</script>

<style scoped>
  /* Drag and drop styles */
  .sortable-ghost {
    opacity: 0.5;
  }

  .sortable-chosen {
    transform: scale(1.05);
  }

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

  /* File input styling */
  input[type="file"] {
    width: 0.1px;
    height: 0.1px;
    opacity: 0;
    overflow: hidden;
    position: absolute;
    z-index: -1;
  }
</style>
