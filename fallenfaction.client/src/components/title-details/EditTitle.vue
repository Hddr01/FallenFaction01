<template>
  <div class="min-h-screen bg-black flex items-center justify-center py-8">
    <div class="max-w-4xl w-full mx-auto px-4">
      <div class="bg-black rounded-lg shadow-md border border-[var(--color-border)]">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h3 class="text-lg font-semibold text-[var(--color-heading)]">Edit Title</h3>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="p-6 text-center">
          <div class="inline-flex items-center">
            <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <span class="text-[var(--color-text)]">Loading title data...</span>
          </div>
        </div>

        <!-- Connection Error State -->
        <div v-else-if="error" class="p-6">
          <div class="bg-red-50 border border-red-200 rounded-md p-4">
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
                <div class="mt-4">
                  <button @click="loadTitleData"
                          class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                    Try Again
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Main Form -->
        <form v-else-if="titleData" @submit.prevent="submitChanges" class="p-6 space-y-6">
          <!-- Cover and Background Images -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <ImagePlaceholder v-model="form.coverImage"
                              label="Cover Image"
                              aspect-ratio="cover"
                              :existing-image="titleData.coverImagePath"
                              @remove="form.coverImage = null" />
            <ImagePlaceholder v-model="form.backgroundImage"
                              label="Background Image"
                              aspect-ratio="background"
                              :existing-image="titleData.backgroundImagePath"
                              @remove="form.backgroundImage = null" />
          </div>

          <!-- Title Fields -->
          <div class="space-y-4">
            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Original Title (without hieroglyphs):
              </Label>
              <Input type="text"
                     v-model="form.originalTitle"
                     class="form-input-bg" />
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                English Title <span class="text-red-500">*</span>
              </Label>
              <Input type="text"
                     v-model="form.englishTitle"
                     class="form-input-bg"
                     required />
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Alternative Names:
              </Label>
              <Input type="text"
                     v-model="form.alternativeNames"
                     placeholder="Alternative Names, separated by '/'"
                     class="form-input-bg" />
            </div>
          </div>

          <!-- Type and Release Date -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Type
              </Label>
              <Select v-model="form.type">
                <SelectTrigger class="form-select-bg">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="select-dropdown-bg">
                  <SelectItem :value="1">Manga</SelectItem>
                  <SelectItem :value="2">Manhwa</SelectItem>
                  <SelectItem :value="3">Manhua</SelectItem>
                  <SelectItem :value="4">Comic</SelectItem>
                  <SelectItem :value="5">Webtoon</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Release Year
              </Label>
              <Input type="number"
                     v-model="form.releaseDate"
                     :min="1800"
                     :max="new Date().getFullYear() + 5"
                     class="form-input-bg" />
            </div>
          </div>

          <!-- Multi-select fields -->
          <div class="space-y-6">
            <!-- Authors -->
            <MultiSelect :options="formData.authors"
                         v-model="form.authors"
                         placeholder="Select authors"
                         label="Authors"
                         create-new-url="/author/CreateA"
                         create-new-text="Create New Author" />

            <!-- Publishers -->
            <MultiSelect :options="formData.publishers"
                         v-model="form.publishers"
                         placeholder="Select publishers"
                         label="Publishers"
                         create-new-url="/publisher/Create"
                         create-new-text="Create New Publisher" />

            <!-- Teams - REQUIRED -->
            <div>
              <MultiSelect :options="formData.teams"
                           v-model="form.teams"
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
            </div>

            <!-- Categories -->
            <MultiSelect :options="formData.categories"
                         v-model="form.categories"
                         placeholder="Select genres"
                         label="Genres" />

            <!-- Tags -->
            <TagsInputWithSuggestions :options="formData.tags"
                                      v-model="form.tags"
                                      placeholder="Add tags..."
                                      label="Tags" />

            <!-- Formats -->
            <MultiSelect :options="formData.formats"
                         v-model="form.formats"
                         placeholder="Select release formats"
                         label="Release Format" />
          </div>

          <!-- Status and Age Restriction -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Title Status
              </Label>
              <Select v-model="form.statusTitle">
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
              <Select v-model="form.statusTranslation">
                <SelectTrigger class="form-select-bg">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="select-dropdown-bg">
                  <SelectItem value="done">Done</SelectItem>
                  <SelectItem value="inproces">In Process</SelectItem>
                  <SelectItem value="dropped">Dropped</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div class="space-y-3">
              <Label class="text-sm font-medium text-foreground">
                Age Restriction
              </Label>
              <Select v-model="form.ageRestriction">
                <SelectTrigger class="form-select-bg">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent class="select-dropdown-bg">
                  <SelectItem :value="0">No Restriction</SelectItem>
                  <SelectItem :value="13">13+</SelectItem>
                  <SelectItem :value="16">16+</SelectItem>
                  <SelectItem :value="18">18+</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <!-- External Links -->
          <div class="space-y-3">
            <Label class="text-sm font-medium text-foreground">
              External Links:
            </Label>
            <div class="space-y-2">
              <div v-for="(link, index) in form.externalLinks" :key="index" class="flex gap-2">
                <Input type="url"
                       v-model="form.externalLinks[index]"
                       placeholder="http://example.com"
                       class="flex-1 form-input-bg" />
                <Button type="button"
                        variant="outline"
                        size="icon"
                        @click="removeExternalLink(index)"
                        :disabled="form.externalLinks.length === 1"
                        class="border-red-300 text-red-700 hover:bg-red-50 disabled:opacity-50">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                  </svg>
                </Button>
              </div>
            </div>
            <Button type="button"
                    variant="link"
                    @click="addExternalLink"
                    class="mt-3 text-green-600 hover:text-green-700">
              Add Another Link
            </Button>
          </div>

          <!-- Description -->
          <div class="space-y-3">
            <Label class="text-sm font-medium text-foreground">
              Description:
            </Label>
            <Textarea v-model="form.description"
                      :rows="5"
                      class="form-textarea-bg resize-vertical" />
          </div>

          <!-- Submit Buttons -->
          <div class="flex space-x-4">
            <Button type="submit"
                    :disabled="submitting"
                    class="px-6 py-2">
              <span v-if="submitting" class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Submitting...
              </span>
              <span v-else>Submit for Approval</span>
            </Button>
            <Button type="button"
                    variant="outline"
                    @click="handleCancel"
                    :disabled="submitting"
                    class="px-6 py-2">
              Cancel
            </Button>
          </div>
        </form>
      </div>
    </div>
  </div>

  <!-- Success Message -->
  <div v-if="successMessage" class="fixed bottom-4 right-4 bg-green-600 text-white px-6 py-3 rounded-lg shadow-lg z-50">
    {{ successMessage }}
  </div>
</template>

<script setup>
  import { ref, onMounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import titleApi from '../../services/titleApi'
  import adminApi from '../../services/adminApi'
  import MultiSelect from '../manga/MultiSelect.vue'
  import ImagePlaceholder from '../manga/ImagePlaceholder.vue'
  import TagsInputWithSuggestions from '../manga/TagsInputWithSuggestions.vue'
  import { Input } from '@/components/ui/input'
  import { Label } from '@/components/ui/label'
  import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
  import { Textarea } from '@/components/ui/textarea'
  import { Button } from '@/components/ui/button'

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
        authors: Array.isArray(titleData.value.authors) ? titleData.value.authors : [],
        publishers: Array.isArray(titleData.value.publishers) ? titleData.value.publishers : [],
        teams: Array.isArray(titleData.value.teams) ? titleData.value.teams : [],
        categories: Array.isArray(titleData.value.categories) ? titleData.value.categories : [],
        tags: Array.isArray(titleData.value.tags) ? titleData.value.tags : [],
        formats: Array.isArray(titleData.value.formats) ? titleData.value.formats : [],
        // FIXED: Parse externalLinks properly - can be string, array, or null
        externalLinks: (() => {
          if (Array.isArray(titleData.value.externalLinks)) {
            return titleData.value.externalLinks.length > 0 ? titleData.value.externalLinks : ['']
          }
          if (typeof titleData.value.externalLinks === 'string' && titleData.value.externalLinks.trim()) {
            // If it's a semicolon-separated string, split it
            const links = titleData.value.externalLinks.split(';').map(link => link.trim()).filter(link => link)
            return links.length > 0 ? links : ['']
          }
          return ['']
        })()
      }
    } catch (err) {
      console.error('Error loading title data:', err)
      error.value = err.message || 'Failed to load title data'
    } finally {
      loading.value = false
    }
  }

  const addExternalLink = () => {
    if (!Array.isArray(form.value.externalLinks)) {
      form.value.externalLinks = ['']
    }
    form.value.externalLinks.push('')
  }

  const removeExternalLink = (index) => {
    if (Array.isArray(form.value.externalLinks) && form.value.externalLinks.length > 1) {
      form.value.externalLinks.splice(index, 1)
    }
  }

  const handleCancel = () => {
    router.push('/user/content')
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
