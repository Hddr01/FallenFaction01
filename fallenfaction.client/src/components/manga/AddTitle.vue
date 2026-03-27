<template>
  <div class="min-h-screen bg-black flex items-center justify-center py-8">
    <div class="max-w-4xl w-full mx-auto px-4">
      <div class="bg-black rounded-lg shadow-md border border-[var(--color-border)]">
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
                  <p>{{ errorMessage }}</p>
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
            <ImagePlaceholder v-model="formData.coverImage"
                              label="Cover Image"
                              aspect-ratio="cover"
                              @remove="formData.coverImage = null" />
            <ImagePlaceholder v-model="formData.backgroundImage"
                              label="Background Image"
                              aspect-ratio="background"
                              @remove="formData.backgroundImage = null" />
          </div>

          <!-- Title Fields -->
          <div class="space-y-4">
            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Original Title (without hieroglyphs):
              </Label>
              <Input type="text"
                     v-model="formData.originalTitle"
                     class="form-input-bg" />
              <span v-if="errors.originalTitle" class="text-red-500 text-sm">{{ errors.originalTitle }}</span>
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                English Title <span class="text-red-500">*</span>
              </Label>
              <Input type="text"
                     v-model="formData.englishTitle"
                     class="form-input-bg" />
              <span v-if="errors.englishTitle" class="text-red-500 text-sm">{{ errors.englishTitle }}</span>
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Alternative Names:
              </Label>
              <Input type="text"
                     v-model="formData.alternativeNames"
                     placeholder="Alternative Names, separated by '/'"
                     class="form-input-bg" />
              <span v-if="errors.alternativeNames" class="text-red-500 text-sm">{{ errors.alternativeNames }}</span>
            </div>
          </div>

          <!-- Type and Release Date -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Type
              </Label>
              <Select v-model="formData.type">
                <SelectTrigger class="form-select-bg">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="select-dropdown-bg">
                  <SelectItem value="1">Manga</SelectItem>
                  <SelectItem value="2">Manhwa</SelectItem>
                  <SelectItem value="3">Manhua</SelectItem>
                  <SelectItem value="4">Comic</SelectItem>
                  <SelectItem value="5">Webtoon</SelectItem>
                </SelectContent>
              </Select>
              <span v-if="errors.type" class="text-red-500 text-sm">{{ errors.type }}</span>
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Release Year
              </Label>
              <Input type="number"
                     v-model="formData.releaseDate"
                     :min="1800"
                     :max="new Date().getFullYear() + 5"
                     class="form-input-bg" />
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

            <!-- Publishers -->
            <MultiSelect :options="publishers"
                         v-model="formData.publishers"
                         placeholder="Select publishers"
                         label="Publishers"
                         create-new-url="/publisher/Create"
                         create-new-text="Create New Publisher" />

            <!-- Teams - REQUIRED -->
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
            <TagsInputWithSuggestions :options="tags"
                                      v-model="formData.tags"
                                      placeholder="Add tags..."
                                      label="Tags" />

            <!-- Formats -->
            <MultiSelect :options="formats"
                         v-model="formData.formats"
                         placeholder="Select release formats"
                         label="Release Format" />
          </div>

          <!-- Status and Age Restriction -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Title Status
              </Label>
              <Select v-model="formData.statusTitle">
                <SelectTrigger class="form-select-bg">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="select-dropdown-bg">
                  <SelectItem value="done">Done</SelectItem>
                  <SelectItem value="inproces">In Process</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Translation Status
              </Label>
              <Select v-model="formData.statusTranslation">
                <SelectTrigger class="form-select-bg">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="select-dropdown-bg">
                  <SelectItem value="done">Done</SelectItem>
                  <SelectItem value="inproces">In Process</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Age Restriction
              </Label>
              <Select v-model="formData.ageRestriction">
                <SelectTrigger class="form-select-bg">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="select-dropdown-bg">
                  <SelectItem value="0">No Restriction</SelectItem>
                  <SelectItem value="12">12+</SelectItem>
                  <SelectItem value="16">16+</SelectItem>
                  <SelectItem value="18">18+</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <!-- External Links -->
          <div>
            <Label class="text-sm font-medium text-foreground mb-2">
              External Links
            </Label>
            <div class="space-y-3">
              <div v-for="(link, index) in formData.externalLinks"
                   :key="index"
                   class="flex gap-2">
                <Input type="url"
                       v-model="formData.externalLinks[index]"
                       placeholder="http://example.com"
                       class="flex-1 form-input-bg" />
                <Button type="button"
                        variant="outline"
                        size="icon"
                        @click="removeLink(index)"
                        :disabled="formData.externalLinks.length === 1"
                        class="border-red-300 text-red-700 hover:bg-red-50 disabled:opacity-50">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                  </svg>
                </Button>
              </div>
            </div>
            <Button type="button"
                    variant="link"
                    @click="addNewLink"
                    class="mt-3 text-green-600 hover:text-green-700">
              Add Another Link
            </Button>
            <span v-if="errors.externalLinks" class="block text-red-500 text-sm mt-1">{{ errors.externalLinks }}</span>
          </div>

          <!-- Description -->
          <div class="space-y-3">
            <Label class="text-sm font-medium text-foreground">
              Description:
            </Label>
            <Textarea v-model="formData.description"
                      :rows="5"
                      class="form-textarea-bg resize-vertical" />
            <span v-if="errors.description" class="text-red-500 text-sm">{{ errors.description }}</span>
          </div>

          <!-- Submit Buttons -->
          <div class="flex space-x-4">
            <Button type="submit"
                    :disabled="isSubmitting"
                    class="px-6 py-2">
              <span v-if="isSubmitting" class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Creating...
              </span>
              <span v-else>Create</span>
            </Button>
            <Button type="button"
                    variant="outline"
                    @click="handleCancel"
                    :disabled="isSubmitting"
                    class="px-6 py-2">
              Cancel
            </Button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, reactive, onMounted } from 'vue'
  import MultiSelect from './MultiSelect.vue'
  import ImagePlaceholder from './ImagePlaceholder.vue'
  import TagsInputWithSuggestions from './TagsInputWithSuggestions.vue'
  import { Input } from '@/components/ui/input'
  import { Label } from '@/components/ui/label'
  import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
  import { Textarea } from '@/components/ui/textarea'
  import { Button } from '@/components/ui/button'
  import titleApi from '../../services/titleApi.js'

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

  // Loading & error states
  const isLoading = ref(true)
  const isSubmitting = ref(false)
  const connectionError = ref(false)
  const errorMessage = ref('')
  const errors = reactive({})

  // External links handlers
  const addNewLink = () => {
    formData.externalLinks.push('')
  }

  const removeLink = (index) => {
    if (formData.externalLinks.length > 1) {
      formData.externalLinks.splice(index, 1)
    }
  }

  // Form validation
  const validateForm = () => {
    const newErrors = {}

    if (!formData.englishTitle.trim()) {
      newErrors.englishTitle = 'English title is required'
    } else if (formData.englishTitle.trim().length < 2) {
      newErrors.englishTitle = 'English title must be at least 2 characters long'
    }

    if (!formData.teams || formData.teams.length === 0) {
      newErrors.teams = 'At least one team must be selected'
    }

    const invalidLinks = formData.externalLinks.filter(link => {
      if (!link.trim()) return false
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

    if (formData.releaseDate && (formData.releaseDate < 1800 || formData.releaseDate > new Date().getFullYear() + 5)) {
      newErrors.releaseDate = 'Please enter a valid year'
    }

    Object.keys(errors).forEach(key => delete errors[key])
    Object.assign(errors, newErrors)

    return Object.keys(newErrors).length === 0
  }

  // Form submission
  const handleSubmit = async () => {
    if (!validateForm()) {
      return
    }

    isSubmitting.value = true

    try {
      // Pass the formData object directly - titleApi.createTitle will build FormData
      const result = await titleApi.createTitle(formData)

      if (result.success && result.data?.id) {
        window.location.href = `/manga/${result.data.id}`
      } else if (result.success) {
        alert('Title created successfully!')
        window.location.href = '/'
      } else {
        alert(result.error || 'Failed to create title. Please try again.')
      }
    } catch (error) {
      console.error('Error creating title:', error)
      alert(error.message || 'Failed to create title. Please try again.')
    } finally {
      isSubmitting.value = false
    }
  }

  const handleCancel = () => {
    if (confirm('Are you sure you want to cancel? All unsaved changes will be lost.')) {
      window.location.href = '/'
    }
  }

  // Load form data from API
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

        authors.value = data.Authors || data.authors || []
        publishers.value = data.Publishers || data.publishers || []
        teams.value = data.Teams || data.teams || []
        categories.value = data.Categories || data.categories || []
        tags.value = data.Tags || data.tags || []
        formats.value = data.Formats || data.formats || []

        console.log('Data assigned:', {
          authors: authors.value.length,
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
      errorMessage.value = error.message || 'Failed to load form data. Please check your connection.'
    } finally {
      isLoading.value = false
    }
  }

  const retryLoadData = async () => {
    await loadFormData()
  }

  onMounted(() => {
    loadFormData()
  })
</script>

<style>
  /* Global styles for Select dropdowns (not scoped because they render in portals) */
  [data-radix-popper-content-wrapper] {
    z-index: 50;
  }

  .select-dropdown-bg {
    background-color: #141414 !important;
    border: 1px solid rgba(255, 255, 255, 0.1) !important;
  }

    .select-dropdown-bg [role="option"] {
      background-color: transparent !important;
      color: white;
    }

      .select-dropdown-bg [role="option"]:hover,
      .select-dropdown-bg [role="option"][data-highlighted] {
        background-color: rgba(255, 255, 255, 0.1) !important;
      }
</style>

<style scoped>
  /* Custom styling for form inputs with #141414 background */
  .form-input-bg {
    background-color: #141414;
    border-color: rgba(255, 255, 255, 0.1);
  }

    .form-input-bg:hover {
      background-color: #1a1a1a;
    }

    .form-input-bg:focus {
      background-color: #141414;
      border-color: rgba(255, 255, 255, 0.2);
    }

  .form-select-bg {
    background-color: #141414;
    border-color: rgba(255, 255, 255, 0.1);
  }

    .form-select-bg:hover {
      background-color: #1a1a1a;
    }

  .select-dropdown-bg {
    background-color: #141414 !important;
    border-color: rgba(255, 255, 255, 0.1) !important;
  }

    /* Ensure SelectItem background */
    .select-dropdown-bg :deep([role="option"]) {
      background-color: transparent;
    }

    .select-dropdown-bg :deep([role="option"]:hover) {
      background-color: rgba(255, 255, 255, 0.1);
    }

    .select-dropdown-bg :deep([role="option"][data-highlighted]) {
      background-color: rgba(255, 255, 255, 0.1);
    }

  .form-textarea-bg {
    background-color: #141414;
    border-color: rgba(255, 255, 255, 0.1);
  }

    .form-textarea-bg:hover {
      background-color: #1a1a1a;
    }

    .form-textarea-bg:focus {
      background-color: #141414;
      border-color: rgba(255, 255, 255, 0.2);
    }
</style>
