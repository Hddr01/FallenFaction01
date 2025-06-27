<template>
  <div class="min-h-screen bg-[var(--color-background)] flex items-center justify-center py-8">
    <div class="max-w-4xl w-full mx-auto px-4">
      <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h3 class="text-lg font-semibold text-[var(--color-heading)]">Create Title</h3>
        </div>

        <!-- Loading State -->
        <div v-if="isLoading" class="p-6 text-center">
          <div class="inline-flex items-center">
            <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <span class="text-[var(--color-text)]">Loading form data...</span>
          </div>
        </div>

        <!-- Connection Error State -->
        <div v-else-if="connectionError" class="p-6">
          <div class="bg-red-50 border border-red-200 rounded-md p-4">
            <div class="flex">
              <div class="flex-shrink-0">
                <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
                </svg>
              </div>
              <div class="ml-3">
                <h3 class="text-sm font-medium text-red-800">Connection Error</h3>
                <div class="mt-2 text-sm text-red-700">
                  <p>Unable to connect to the server. Please check:</p>
                  <ul class="mt-1 ml-4 list-disc">
                    <li>Is the server running?</li>
                    <li>Are you logged in?</li>
                    <li>Check your network connection</li>
                  </ul>
                </div>
                <div class="mt-4">
                  <button @click="retryLoadData"
                          class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                    Try Again
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Main Form -->
        <form v-else @submit.prevent="handleSubmit" class="p-6 space-y-6">
          <!-- Cover and Background Images -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Cover Image:
              </label>
              <input ref="coverImageInput"
                     type="file"
                     accept="image/*"
                     @change="(e) => handleFileChange('coverImage', e)"
                     class="block w-full text-sm text-[var(--color-text)] file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-[var(--color-background-mute)] file:text-green-600 hover:file:bg-[var(--color-background-soft)] hover:file:text-green-700 transition-colors duration-200" />

              <!-- Cover Image Preview -->
              <div v-if="imagePreview.coverImage" class="mt-3">
                <div class="relative inline-block">
                  <img :src="imagePreview.coverImage"
                       alt="Cover Image Preview"
                       class="w-32 h-48 object-cover rounded-lg border-2 border-[var(--color-border)] shadow-sm" />
                  <button type="button"
                          @click="removeImage('coverImage')"
                          class="absolute -top-2 -right-2 w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600 transition-colors duration-200 shadow-md">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                  </button>
                </div>
                <p class="text-xs text-[var(--color-text)] opacity-75 mt-1">Cover Image Preview</p>
              </div>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Background Image:
              </label>
              <input ref="backgroundImageInput"
                     type="file"
                     accept="image/*"
                     @change="(e) => handleFileChange('backgroundImage', e)"
                     class="block w-full text-sm text-[var(--color-text)] file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm font-semibold file:bg-[var(--color-background-mute)] file:text-green-600 hover:file:bg-[var(--color-background-soft)] hover:file:text-green-700 transition-colors duration-200" />

              <!-- Background Image Preview -->
              <div v-if="imagePreview.backgroundImage" class="mt-3">
                <div class="relative inline-block">
                  <img :src="imagePreview.backgroundImage"
                       alt="Background Image Preview"
                       class="w-48 h-32 object-cover rounded-lg border-2 border-[var(--color-border)] shadow-sm" />
                  <button type="button"
                          @click="removeImage('backgroundImage')"
                          class="absolute -top-2 -right-2 w-6 h-6 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600 transition-colors duration-200 shadow-md">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                    </svg>
                  </button>
                </div>
                <p class="text-xs text-[var(--color-text)] opacity-75 mt-1">Background Image Preview</p>
              </div>
            </div>
          </div>

          <!-- Title Fields -->
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Original Title (without hieroglyphs):
              </label>
              <input type="text"
                     v-model="formData.originalTitle"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
              <span v-if="errors.originalTitle" class="text-red-500 text-sm">{{ errors.originalTitle }}</span>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                English Title <span class="text-red-500">*</span>:
              </label>
              <input type="text"
                     v-model="formData.englishTitle"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
              <span v-if="errors.englishTitle" class="text-red-500 text-sm">{{ errors.englishTitle }}</span>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Alternative Names:
              </label>
              <input type="text"
                     v-model="formData.alternativeNames"
                     placeholder="Alternative Names, separated by '/'"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 placeholder:text-[var(--color-text)] placeholder:opacity-50" />
              <span v-if="errors.alternativeNames" class="text-red-500 text-sm">{{ errors.alternativeNames }}</span>
            </div>
          </div>

          <!-- Type and Release Date -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Type
              </label>
              <select v-model="formData.type"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                <option value="1">Manga</option>
                <option value="2">Manhwa</option>
                <option value="3">Manhua</option>
                <option value="4">Comic</option>
                <option value="5">Webtoon</option>
              </select>
              <span v-if="errors.type" class="text-red-500 text-sm">{{ errors.type }}</span>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Release Year
              </label>
              <input type="number"
                     v-model="formData.releaseDate"
                     min="1800"
                     :max="new Date().getFullYear() + 5"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
              <span v-if="errors.releaseDate" class="text-red-500 text-sm">{{ errors.releaseDate }}</span>
            </div>
          </div>

          <!-- Multi-select fields -->
          <div class="space-y-6">
            <!-- Authors -->
            <MultiSelect :options="authors"
                         v-model="formData.authors"
                         placeholder="Select authors"
                         label="Authors"
                         create-new-url="/author/CreateA"
                         create-new-text="Create New Author" />

            <!-- Artists -->
            <MultiSelect :options="artists"
                         v-model="formData.artists"
                         placeholder="Select artists"
                         label="Artists"
                         create-new-url="/people/Create"
                         create-new-text="Create New Artist" />

            <!-- Publishers -->
            <MultiSelect :options="publishers"
                         v-model="formData.publishers"
                         placeholder="Select publishers"
                         label="Publishers"
                         create-new-url="/publisher/Create"
                         create-new-text="Create New Publisher" />

            <!-- Teams - NOW REQUIRED -->
            <div>
              <MultiSelect :options="teams"
                           v-model="formData.teams"
                           placeholder="Select teams"
                           label="Teams"
                           required="true"
                           create-new-url="/team/Addteam"
                           create-new-text="Create New Team">
                <template #label>
                  Teams
                  <span class="text-red-500">*</span>
                </template>
              </MultiSelect>
              <span v-if="errors.teams" class="text-red-500 text-sm">{{ errors.teams }}</span>
            </div>

            <!-- Categories -->
            <MultiSelect :options="categories"
                         v-model="formData.categories"
                         placeholder="Select genres"
                         label="Genres" />

            <!-- Tags -->
            <MultiSelect :options="tags"
                         v-model="formData.tags"
                         placeholder="Select tags"
                         label="Tags" />

            <!-- Formats -->
            <MultiSelect :options="formats"
                         v-model="formData.formats"
                         placeholder="Select release formats"
                         label="Release Format" />
          </div>

          <!-- Status and Age Restriction -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Title Status
              </label>
              <select v-model="formData.statusTitle"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                <option value="done">Done</option>
                <option value="inproces">In Process</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Translation Status
              </label>
              <select v-model="formData.statusTranslation"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                <option value="done">Done</option>
                <option value="inproces">In Process</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Age Restriction
              </label>
              <select v-model="formData.ageRestriction"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
                <option value="0">No Restriction</option>
                <option value="12">12+</option>
                <option value="16">16+</option>
                <option value="18">18+</option>
              </select>
            </div>
          </div>

          <!-- External Links -->
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
              External Links
            </label>
            <div class="space-y-3">
              <div v-for="(link, index) in formData.externalLinks"
                   :key="index"
                   class="flex gap-2">
                <input type="url"
                       v-model="formData.externalLinks[index]"
                       placeholder="http://example.com"
                       class="flex-1 px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 placeholder:text-[var(--color-text)] placeholder:opacity-50" />
                <button type="button"
                        @click="removeLink(index)"
                        :disabled="formData.externalLinks.length === 1"
                        class="px-3 py-2 border border-red-300 text-red-700 rounded-md hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                  </svg>
                </button>
              </div>
            </div>
            <button type="button"
                    @click="addNewLink"
                    class="mt-3 text-green-600 hover:text-green-700 text-sm font-medium transition-colors duration-200">
              Add Another Link
            </button>
            <span v-if="errors.externalLinks" class="block text-red-500 text-sm mt-1">{{ errors.externalLinks }}</span>
          </div>

          <!-- Description -->
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">
              Description:
            </label>
            <textarea v-model="formData.description"
                      rows="5"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 resize-vertical"></textarea>
            <span v-if="errors.description" class="text-red-500 text-sm">{{ errors.description }}</span>
          </div>

          <!-- Submit Buttons -->
          <div class="flex space-x-4">
            <button type="submit"
                    :disabled="isSubmitting"
                    class="px-6 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
              <span v-if="isSubmitting" class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Creating...
              </span>
              <span v-else>Create</span>
            </button>
            <button type="button"
                    @click="handleCancel"
                    :disabled="isSubmitting"
                    class="px-6 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, reactive, onMounted, onUnmounted } from 'vue'
  import MultiSelect from './MultiSelect.vue'
  import titleApi from '../../services/titleApi.js' // Import the new axios-based API

  // Form data
  const formData = reactive({
    coverImage: null,
    backgroundImage: null,
    originalTitle: '',
    englishTitle: '',
    alternativeNames: '',
    type: '1',
    releaseDate: '',
    authors: [],
    artists: [],
    publishers: [],
    teams: [],
    categories: [],
    tags: [],
    formats: [],
    statusTitle: 'inproces',
    statusTranslation: 'inproces',
    ageRestriction: '0',
    externalLinks: [''],
    description: ''
  })

  // API data
  const authors = ref([])
  const artists = ref([])
  const publishers = ref([])
  const teams = ref([])
  const categories = ref([])
  const tags = ref([])
  const formats = ref([])

  const errors = reactive({})
  const isSubmitting = ref(false)
  const isLoading = ref(true)
  const connectionError = ref(false)

  // File input refs
  const coverImageInput = ref(null)
  const backgroundImageInput = ref(null)

  // Image preview URLs
  const imagePreview = reactive({
    coverImage: null,
    backgroundImage: null
  })

  // Load form data from API using axios-based service
  const loadFormData = async () => {
    try {
      isLoading.value = true
      connectionError.value = false

      console.log('Testing API connection...')
      const isConnected = await titleApi.testConnection()

      if (!isConnected) {
        throw new Error('Cannot connect to API server. Please check if the server is running.')
      }

      console.log('Loading form data...')
      const result = await titleApi.getFormData()

      if (result.success) {
        const data = result.data
        console.log('Form data loaded successfully:', data)

        // Handle both uppercase and lowercase property names from API
        authors.value = data.Authors || data.authors || []
        artists.value = data.Artists || data.artists || []
        publishers.value = data.Publishers || data.publishers || []
        teams.value = data.Teams || data.teams || []
        categories.value = data.Categories || data.categories || []
        tags.value = data.Tags || data.tags || []
        formats.value = data.Formats || data.formats || []

        console.log('Data assigned to reactive refs:', {
          authors: authors.value.length,
          artists: artists.value.length,
          publishers: publishers.value.length,
          teams: teams.value.length,
          categories: categories.value.length,
          tags: tags.value.length,
          formats: formats.value.length
        })
      } else {
        throw new Error(result.error || 'Failed to load form data')
      }

    } catch (error) {
      console.error('Error loading form data:', error)
      connectionError.value = true

      // More detailed error message for user
      const errorMessage = `Failed to load form data: ${error.message}\n\nTroubleshooting:\n• Is the server running?\n• Are you logged in?\n• Check browser console for details`
      alert(errorMessage)
    } finally {
      isLoading.value = false
    }
  }

  // Retry loading data
  const retryLoadData = async () => {
    await loadFormData()
  }

  // File input handler with validation
  const handleFileChange = (field, event) => {
    const file = event.target.files[0]

    if (file) {
      // Validate file type
      if (!file.type.startsWith('image/')) {
        alert('Please select a valid image file')
        event.target.value = '' // Clear the input
        return
      }

      // Validate file size (5MB limit)
      if (file.size > 5 * 1024 * 1024) {
        alert('File size must be less than 5MB')
        event.target.value = '' // Clear the input
        return
      }

      // Clean up previous preview URL
      if (imagePreview[field]) {
        URL.revokeObjectURL(imagePreview[field])
      }

      // Create new preview URL
      imagePreview[field] = URL.createObjectURL(file)
      formData[field] = file
    }
  }

  // Remove image and cleanup
  const removeImage = (field) => {
    if (imagePreview[field]) {
      URL.revokeObjectURL(imagePreview[field])
      imagePreview[field] = null
    }
    formData[field] = null

    // Clear the specific file input
    if (field === 'coverImage' && coverImageInput.value) {
      coverImageInput.value.value = ''
    } else if (field === 'backgroundImage' && backgroundImageInput.value) {
      backgroundImageInput.value.value = ''
    }
  }

  // External links handlers
  const addNewLink = () => {
    formData.externalLinks.push('')
  }

  const removeLink = (index) => {
    if (formData.externalLinks.length > 1) {
      formData.externalLinks.splice(index, 1)
    }
  }

  // Enhanced form validation
  const validateForm = () => {
    const newErrors = {}

    // English title validation
    if (!formData.englishTitle.trim()) {
      newErrors.englishTitle = 'English title is required'
    } else if (formData.englishTitle.trim().length < 2) {
      newErrors.englishTitle = 'English title must be at least 2 characters long'
    }

    // Teams validation - NOW REQUIRED
    if (!formData.teams || formData.teams.length === 0) {
      newErrors.teams = 'At least one team must be selected'
    }

    // Validate external links
    const invalidLinks = formData.externalLinks.filter(link => {
      if (!link.trim()) return false // Empty links are OK
      try {
        new URL(link)
        return false
      } catch {
        return true
      }
    })

    if (invalidLinks.length > 0) {
      newErrors.externalLinks = 'Please enter valid URLs for external links'
    }

    // Release date validation
    if (formData.releaseDate && (formData.releaseDate < 1800 || formData.releaseDate > new Date().getFullYear() + 5)) {
      newErrors.releaseDate = 'Please enter a valid year'
    }

    // Clear previous errors
    Object.keys(errors).forEach(key => delete errors[key])
    Object.assign(errors, newErrors)

    return Object.keys(newErrors).length === 0
  }

  // Form submission using axios-based API
  const handleSubmit = async () => {
    if (!validateForm()) {
      return
    }

    if (connectionError.value) {
      alert('Cannot submit form: No connection to server. Please refresh the page and try again.')
      return
    }

    isSubmitting.value = true

    try {
      console.log('Preparing form submission...')

      // Log form data for debugging
      console.log('Form data:', {
        englishTitle: formData.englishTitle,
        coverImage: formData.coverImage?.name,
        backgroundImage: formData.backgroundImage?.name,
        authors: formData.authors.length,
        artists: formData.artists.length,
        teams: formData.teams.length, // Log teams length
        // ... other fields
      })

      // Submit using axios-based API service
      const result = await titleApi.createTitle(formData)

      if (result.success) {
        console.log('Title created successfully:', result)
        alert(result.message || 'Title created successfully!')

        // Reset form
        resetForm()

        // Optionally redirect to another page
        // window.location.href = '/titles'
      } else {
        throw new Error(result.error || 'Failed to create title')
      }

    } catch (error) {
      console.error('Error submitting form:', error)
      alert(`Error creating title: ${error.message}`)
    } finally {
      isSubmitting.value = false
    }
  }

  const resetForm = () => {
    // Clean up image previews
    if (imagePreview.coverImage) {
      URL.revokeObjectURL(imagePreview.coverImage)
      imagePreview.coverImage = null
    }
    if (imagePreview.backgroundImage) {
      URL.revokeObjectURL(imagePreview.backgroundImage)
      imagePreview.backgroundImage = null
    }

    // Reset form data
    Object.assign(formData, {
      coverImage: null,
      backgroundImage: null,
      originalTitle: '',
      englishTitle: '',
      alternativeNames: '',
      type: '1',
      releaseDate: '',
      authors: [],
      artists: [],
      publishers: [],
      teams: [], // Reset teams array
      categories: [],
      tags: [],
      formats: [],
      statusTitle: 'inproces',
      statusTranslation: 'inproces',
      ageRestriction: '0',
      externalLinks: [''],
      description: ''
    })

    // Clear file inputs
    if (coverImageInput.value) coverImageInput.value.value = ''
    if (backgroundImageInput.value) backgroundImageInput.value.value = ''

    // Clear errors
    Object.keys(errors).forEach(key => delete errors[key])
  }

  const handleCancel = () => {
    if (confirm('Are you sure you want to cancel creating the title?')) {
      window.history.back()
    }
  }

  // Load data on component mount
  onMounted(async () => {
    console.log('AddTitle component mounted')
    console.log('Using axios-based API service')

    await loadFormData()
  })

  // Cleanup image preview URLs on component unmount
  onUnmounted(() => {
    if (imagePreview.coverImage) {
      URL.revokeObjectURL(imagePreview.coverImage)
    }
    if (imagePreview.backgroundImage) {
      URL.revokeObjectURL(imagePreview.backgroundImage)
    }
  })
</script>

<style scoped>
  /* Custom focus and hover states using CSS variables */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }
</style>
