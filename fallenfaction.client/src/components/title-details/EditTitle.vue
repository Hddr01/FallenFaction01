<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Edit Title</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">
          Submit changes for admin approval
        </p>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading title data...</span>
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
            <h3 class="text-sm font-medium text-red-800">{{ error }}</h3>
            <div class="mt-4">
              <button @click="loadTitleData" class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200">
                Try Again
              </button>
              <router-link to="/user/content" class="ml-3 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-100">
                Back to My Content
              </router-link>
            </div>
          </div>
        </div>
      </div>

      <!-- Edit Form -->
      <form v-else-if="titleData" @submit.prevent="submitChanges" class="space-y-6">
        <!-- Basic Information -->
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-6">
          <h2 class="text-xl font-semibold text-[var(--color-heading)] mb-4">Basic Information</h2>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Original Title *</label>
              <input v-model="form.originalTitle"
                     type="text"
                     required
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500"
                     placeholder="Original language title" />
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">English Title *</label>
              <input v-model="form.englishTitle"
                     type="text"
                     required
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500"
                     placeholder="English title" />
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Alternative Names</label>
              <input v-model="form.alternativeNames"
                     type="text"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500"
                     placeholder="Separate multiple names with semicolons" />
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Type *</label>
              <select v-model="form.type"
                      required
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option :value="1">Manga</option>
                <option :value="2">Manhwa</option>
                <option :value="3">Manhua</option>
                <option :value="4">Comic</option>
                <option :value="5">Novel</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Release Date</label>
              <input v-model="form.releaseDate"
                     type="text"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500"
                     placeholder="e.g., 2020, March 2021" />
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Status *</label>
              <select v-model="form.statusTitle"
                      required
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option value="inproces">In Progress</option>
                <option value="completed">Completed</option>
                <option value="hiatus">Hiatus</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Translation Status *</label>
              <select v-model="form.statusTranslation"
                      required
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option value="inproces">In Progress</option>
                <option value="completed">Completed</option>
                <option value="dropped">Dropped</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Age Restriction</label>
              <select v-model.number="form.ageRestriction"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option :value="0">No Restriction</option>
                <option :value="13">13+</option>
                <option :value="16">16+</option>
                <option :value="18">18+</option>
              </select>
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Description *</label>
              <textarea v-model="form.description"
                        required
                        rows="6"
                        class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 resize-vertical"
                        placeholder="Enter title description..."></textarea>
            </div>
          </div>
        </div>

        <!-- Images -->
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-6">
          <h2 class="text-xl font-semibold text-[var(--color-heading)] mb-4">Images</h2>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Cover Image</label>
              <div v-if="titleData.coverImagePath" class="mb-2">
                <img :src="titleData.coverImagePath" alt="Current cover" class="h-48 w-auto rounded border border-[var(--color-border)]" />
                <p class="text-xs text-[var(--color-text)] opacity-60 mt-1">Current cover</p>
              </div>
              <input type="file"
                     @change="handleCoverImageChange"
                     accept="image/*"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500" />
              <p class="text-xs text-[var(--color-text)] opacity-60 mt-1">Leave empty to keep current image</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Background Image</label>
              <div v-if="titleData.backgroundImagePath" class="mb-2">
                <img :src="titleData.backgroundImagePath" alt="Current background" class="h-48 w-auto rounded border border-[var(--color-border)]" />
                <p class="text-xs text-[var(--color-text)] opacity-60 mt-1">Current background</p>
              </div>
              <input type="file"
                     @change="handleBackgroundImageChange"
                     accept="image/*"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500" />
              <p class="text-xs text-[var(--color-text)] opacity-60 mt-1">Leave empty to keep current image</p>
            </div>
          </div>
        </div>

        <!-- Relationships -->
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-6">
          <h2 class="text-xl font-semibold text-[var(--color-heading)] mb-4">Relationships</h2>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Authors</label>
              <select v-model="form.authors"
                      multiple
                      size="5"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option v-for="author in formData.authors" :key="author.id" :value="author.id">
                  {{ author.name }}
                </option>
              </select>
              <p class="text-xs text-[var(--color-text)] opacity-60 mt-1">Hold Ctrl/Cmd to select multiple</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Artists</label>
              <select v-model="form.artists"
                      multiple
                      size="5"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option v-for="artist in formData.artists" :key="artist.id" :value="artist.id">
                  {{ artist.name }}
                </option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Publishers</label>
              <select v-model="form.publishers"
                      multiple
                      size="5"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option v-for="publisher in formData.publishers" :key="publisher.id" :value="publisher.id">
                  {{ publisher.name }}
                </option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Teams *</label>
              <select v-model="form.teams"
                      multiple
                      size="5"
                      required
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option v-for="team in formData.teams" :key="team.id" :value="team.id">
                  {{ team.name }}
                </option>
              </select>
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Categories</label>
              <select v-model="form.categories"
                      multiple
                      size="5"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option v-for="category in formData.categories" :key="category.id" :value="category.id">
                  {{ category.name }}
                </option>
              </select>
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Tags</label>
              <select v-model="form.tags"
                      multiple
                      size="5"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option v-for="tag in formData.tags" :key="tag.id" :value="tag.id">
                  {{ tag.name }}
                </option>
              </select>
            </div>

            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Formats</label>
              <select v-model="form.formats"
                      multiple
                      size="5"
                      class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500">
                <option v-for="format in formData.formats" :key="format.id" :value="format.id">
                  {{ format.name }}
                </option>
              </select>
            </div>
          </div>
        </div>

        <!-- External Links -->
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg p-6">
          <h2 class="text-xl font-semibold text-[var(--color-heading)] mb-4">External Links</h2>

          <div class="space-y-2">
            <div v-for="(link, index) in form.externalLinks" :key="index" class="flex gap-2">
              <input v-model="form.externalLinks[index]"
                     type="url"
                     class="flex-1 px-3 py-2 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500"
                     placeholder="https://example.com" />
              <button type="button"
                      @click="removeExternalLink(index)"
                      class="px-3 py-2 bg-red-600 text-white rounded-md hover:bg-red-700">
                Remove
              </button>
            </div>
            <button type="button"
                    @click="addExternalLink"
                    class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700">
              Add Link
            </button>
          </div>
        </div>

        <!-- Submit Buttons -->
        <div class="flex justify-end space-x-4">
          <router-link to="/user/content"
                       class="px-6 py-3 border border-[var(--color-border)] rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] transition-colors">
            Cancel
          </router-link>
          <button type="submit"
                  :disabled="submitting"
                  class="px-6 py-3 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
            <span v-if="submitting" class="inline-flex items-center">
              <svg class="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Submitting...
            </span>
            <span v-else>Submit for Approval</span>
          </button>
        </div>
      </form>

      <!-- Success Message -->
      <div v-if="successMessage" class="fixed bottom-4 right-4 bg-green-600 text-white px-6 py-3 rounded-lg shadow-lg z-50">
        {{ successMessage }}
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import titleApi from '../../services/titleApi'
  import adminApi from '../../services/adminApi'

  const route = useRoute()
  const router = useRouter()

  const titleId = ref(parseInt(route.params.id))
  const loading = ref(true)
  const error = ref('')
  const submitting = ref(false)
  const successMessage = ref('')

  const titleData = ref(null)
  const formData = ref({
    authors: [],
    artists: [],
    publishers: [],
    teams: [],
    categories: [],
    tags: [],
    formats: []
  })

  const form = ref({
    originalTitle: '',
    englishTitle: '',
    alternativeNames: '',
    type: 1,
    releaseDate: '',
    description: '',
    statusTitle: 'inproces',
    statusTranslation: 'inproces',
    ageRestriction: 0,
    coverImage: null,
    backgroundImage: null,
    authors: [],
    artists: [],
    publishers: [],
    teams: [],
    categories: [],
    tags: [],
    formats: [],
    externalLinks: ['']
  })

  const loadTitleData = async () => {
    loading.value = true
    error.value = ''

    try {
      // Load form data using titleApi service
      const formDataResult = await titleApi.getFormData()
      if (formDataResult.success) {
        formData.value = formDataResult.data
      } else {
        throw new Error(formDataResult.error)
      }

      // Load title data using adminApi service
      const titleResult = await adminApi.getTitleDetails(titleId.value)
      if (titleResult.success) {
        titleData.value = titleResult.data
      } else {
        throw new Error(titleResult.error)
      }

      // Populate form
      form.value = {
        originalTitle: titleData.value.originalTitle || '',
        englishTitle: titleData.value.englishTitle || '',
        alternativeNames: titleData.value.alternativeNames || '',
        type: titleData.value.type || 1,
        releaseDate: titleData.value.releaseDate || '',
        description: titleData.value.description || '',
        statusTitle: titleData.value.statusTitle || 'inproces',
        statusTranslation: titleData.value.statusTranslation || 'inproces',
        ageRestriction: titleData.value.ageRestriction || 0,
        coverImage: null,
        backgroundImage: null,
        authors: titleData.value.authors || [],
        artists: titleData.value.artists || [],
        publishers: titleData.value.publishers || [],
        teams: titleData.value.teams || [],
        categories: titleData.value.categories || [],
        tags: titleData.value.tags || [],
        formats: titleData.value.formats || [],
        externalLinks: titleData.value.externalLinks?.length > 0 ? titleData.value.externalLinks : ['']
      }
    } catch (err) {
      console.error('Error loading title data:', err)
      error.value = err.message || 'Failed to load title data'
    } finally {
      loading.value = false
    }
  }

  const handleCoverImageChange = (event) => {
    form.value.coverImage = event.target.files[0]
  }

  const handleBackgroundImageChange = (event) => {
    form.value.backgroundImage = event.target.files[0]
  }

  const addExternalLink = () => {
    form.value.externalLinks.push('')
  }

  const removeExternalLink = (index) => {
    form.value.externalLinks.splice(index, 1)
  }

  const submitChanges = async () => {
    submitting.value = true
    error.value = ''

    try {
      // Use titleApi.editTitle instead of adminApi.updateTitle
      // This submits changes for approval rather than directly updating
      const result = await titleApi.editTitle(titleId.value, {
        originalTitle: form.value.originalTitle,
        englishTitle: form.value.englishTitle,
        alternativeNames: form.value.alternativeNames,
        type: form.value.type,
        releaseDate: form.value.releaseDate,
        description: form.value.description,
        statusTitle: form.value.statusTitle,
        statusTranslation: form.value.statusTranslation,
        ageRestriction: form.value.ageRestriction,
        coverImage: form.value.coverImage,
        backgroundImage: form.value.backgroundImage,
        authors: form.value.authors,
        artists: form.value.artists,
        publishers: form.value.publishers,
        teams: form.value.teams,
        categories: form.value.categories,
        tags: form.value.tags,
        formats: form.value.formats,
        externalLinks: form.value.externalLinks.filter(link => link.trim())
      })

      if (result.success) {
        successMessage.value = result.message || 'Changes submitted for admin approval!'
        setTimeout(() => {
          router.push('/user/content')
        }, 2000)
      } else {
        throw new Error(result.error)
      }
    } catch (err) {
      console.error('Error submitting changes:', err)
      error.value = err.message || 'Failed to submit changes'
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } finally {
      submitting.value = false
    }
  }

  onMounted(() => {
    loadTitleData()
  })
</script>
