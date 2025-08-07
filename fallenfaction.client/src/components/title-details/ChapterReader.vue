<template>
  <div class="chapter-container" :class="[currentTheme, readingDirection, { 'debug-mode': debugMode }]" id="chapterContainer">
    <!-- Manga Navbar -->
    <div class="manga-navbar" :class="{ 'hidden': !uiVisible && isMobile }" id="mangaNavbar">
      <div class="navbar-content">
        <div class="navbar-left">
          <button class="back-button" @click="goToTitleDetails">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="m15 18-6-6 6-6" />
            </svg>
            <span class="back-text">Back to Chapters</span>
          </button>

          <div class="chapter-info">
            <h1 class="title-name" @click="goToTitleDetails">{{ chapterData?.titleName || 'Loading...' }}</h1>
            <div class="chapter-nav">
              <button class="chapter-nav-btn" @click="gotoPrevChapter" :disabled="!chapterData?.previousChapterId">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="m15 18-6-6 6-6" />
                </svg>
              </button>
              <h2 class="chapter-name">
                Vol.{{ chapterData?.volumeNumber }} Ch.{{ chapterData?.chapterNumber }}
                <span v-if="chapterData?.name">: {{ chapterData.name }}</span>
              </h2>
              <button class="chapter-nav-btn" @click="gotoNextChapter" :disabled="!chapterData?.nextChapterId">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="m9 18 6-6-6-6" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <div class="navbar-right">
          <button class="settings-btn" @click="toggleSettings">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
              <circle cx="12" cy="12" r="3" />
            </svg>
          </button>

          <button class="chapter-list-btn" @click="toggleChapterList">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="8" y1="6" x2="21" y2="6" />
              <line x1="8" y1="12" x2="21" y2="12" />
              <line x1="8" y1="18" x2="21" y2="18" />
              <line x1="3" y1="6" x2="3.01" y2="6" />
              <line x1="3" y1="12" x2="3.01" y2="12" />
              <line x1="3" y1="18" x2="3.01" y2="18" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner">
        <svg class="animate-spin h-8 w-8 text-[var(--color-accent)]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="text-[var(--color-text)]">Loading chapter...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <div class="error-content">
        <svg class="w-16 h-16 text-red-500 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="text-xl font-semibold text-[var(--color-text)] mb-2">Chapter Not Found</h3>
        <p class="text-[var(--color-text)] opacity-75 mb-4">{{ error }}</p>
        <div class="flex space-x-3 justify-center">
          <button @click="retryLoad" class="px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
            Try Again
          </button>
          <button @click="goToTitleDetails" class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Back to Title
          </button>
        </div>
      </div>
    </div>

    <!-- Chapter Content -->
    <div v-else-if="chapterData && orderedImages.length > 0" class="chapter-content">
      <!-- Single Page View -->
      <div v-if="viewMode === 'single'" class="single-page-view">
        <!-- Content Area -->
        <div class="content-area" ref="singlePageContainer">
          <div class="manga-image-container">
            <img :src="getImageUrl(currentImage?.imagePath)"
                 :alt="`Page ${currentPage}`"
                 class="manga-image"
                 :style="imageStyles"
                 @error="handleImageError"
                 @load="handleImageLoad" />
          </div>
        </div>

        <!-- Touch Zones for Mobile - Cover entire viewport -->
        <div v-if="isMobile" class="tap-zones-fullscreen">
          <div class="tap-zone tap-zone-left" @click="goToPrevPage" data-zone="Previous"></div>
          <div class="tap-zone tap-zone-center" @click="toggleUI" data-zone="Toggle UI"></div>
          <div class="tap-zone tap-zone-right" @click="goToNextPage" data-zone="Next"></div>
        </div>

        <!-- Page Indicator -->
        <div class="page-indicator" :class="{ 'hidden': !uiVisible && isMobile }">
          <select v-model="currentPage"
                  @change="changePage"
                  class="page-selector">
            <option v-for="page in totalPages" :key="page" :value="page">
              {{ page }} / {{ totalPages }}
            </option>
          </select>
        </div>
      </div>

      <!-- All Pages View -->
      <div v-else class="all-pages-view">
        <div class="all-pages-container" ref="allPagesContainer">
          <div class="manga-content-wrapper">
            <div class="manga-pages-wrapper" :style="{ gap: `${imageGap}px` }">
              <div v-for="(image, index) in orderedImages"
                   :key="image.id"
                   class="manga-page-wrapper"
                   :style="{ marginBottom: `${imageGap}px` }">
                <div v-if="!hidePageNumbers" class="page-number-indicator">
                  Page {{ index + 1 }}
                </div>
                <img :src="getImageUrl(image.imagePath)"
                     :alt="`Page ${index + 1}`"
                     class="manga-image"
                     :style="allPagesImageStyles"
                     @error="handleImageError" />
              </div>
            </div>

            <!-- Navigation Controls -->
            <div class="static-navigation">
              <div class="chapter-navigation-controls">
                <button @click="gotoPrevChapter"
                        :disabled="!chapterData.previousChapterId"
                        class="nav-btn prev-chapter">
                  Previous Chapter
                </button>
                <button @click="goToTitleDetails" class="nav-btn back-to-title">
                  Back to Title
                </button>
                <button @click="gotoNextChapter"
                        :disabled="!chapterData.nextChapterId"
                        class="nav-btn next-chapter">
                  Next Chapter
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Touch Zones for All Pages View - Cover the entire content area -->
        <div v-if="isMobile" class="tap-zones-all-pages">
          <div class="tap-zone tap-zone-left" @click="gotoPrevChapter" data-zone="Previous Chapter"></div>
          <div class="tap-zone tap-zone-center" @click="toggleUI" data-zone="Toggle UI"></div>
          <div class="tap-zone tap-zone-right" @click="gotoNextChapter" data-zone="Next Chapter"></div>
        </div>
      </div>
    </div>

    <!-- No Images State -->
    <div v-else-if="chapterData && orderedImages.length === 0" class="no-images-container">
      <div class="no-images-content">
        <svg class="w-16 h-16 text-[var(--color-text)] opacity-50 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
        </svg>
        <h3 class="text-xl font-semibold text-[var(--color-text)] mb-2">No Images Available</h3>
        <p class="text-[var(--color-text)] opacity-75 mb-4">This chapter doesn't have any images yet.</p>
        <button @click="goToTitleDetails" class="px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
          Back to Title
        </button>
      </div>
    </div>

    <!-- Chapter List Popup -->
    <div v-if="showChapterList" class="popup chapter-list-popup" @click="handleBackdropClick">
      <div class="popup__content scrollable" @click.stop>
        <div class="popup__header">
          <h3>Chapter List</h3>
          <button class="close-btn" @click="toggleChapterList">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
        <div class="chapter-list">
          <div v-for="chapter in chaptersList"
               :key="chapter.id"
               class="chapter-item"
               :class="{ 'active': chapter.chapterNumber === chapterData?.chapterNumber }"
               @click="goToChapter(chapter)">
            <div class="chapter-item-left">
              <div class="chapter-title">
                Vol.{{ chapter.volumeNumber }} Ch.{{ chapter.chapterNumber }}
                <span v-if="chapter.name">: {{ chapter.name }}</span>
              </div>
              <div class="chapter-team">{{ chapter.teamName }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Settings Popup -->
    <div v-if="showSettings" class="popup settings-popup" @click="handleBackdropClick">
      <div class="popup__content scrollable" @click.stop>
        <div class="popup__header">
          <h3>Reading Settings</h3>
          <button class="close-btn" @click="toggleSettings">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        <div class="settings-content">
          <!-- View Mode -->
          <div class="settings-section">
            <div class="settings-label">Reading Mode</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': viewMode === 'single' }"
                      @click="setViewMode('single')">
                Single Page
              </button>
              <button class="settings-btn"
                      :class="{ 'active': viewMode === 'all' }"
                      @click="setViewMode('all')">
                All Pages
              </button>
            </div>
          </div>

          <!-- Reading Direction -->
          <div class="settings-section">
            <div class="settings-label">Reading Direction</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': readingDirection === 'horizontal' }"
                      @click="setReadingDirection('horizontal')">
                Horizontal
              </button>
              <button class="settings-btn"
                      :class="{ 'active': readingDirection === 'vertical' }"
                      @click="setReadingDirection('vertical')">
                Vertical
              </button>
            </div>
          </div>

          <!-- Theme -->
          <div class="settings-section">
            <div class="settings-label">Reading Theme</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': currentTheme === 'dark' }"
                      @click="setTheme('dark')">
                Dark
              </button>
              <button class="settings-btn"
                      :class="{ 'active': currentTheme === 'light' }"
                      @click="setTheme('light')">
                Light
              </button>
              <button class="settings-btn"
                      :class="{ 'active': currentTheme === 'system' }"
                      @click="setTheme('system')">
                System
              </button>
            </div>
          </div>

          <!-- Image Fitting -->
          <div class="settings-section">
            <div class="settings-label">Fit Images</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': imageSize === 'width' }"
                      @click="setImageSize('width')">
                By Width
              </button>
              <button class="settings-btn"
                      :class="{ 'active': imageSize === 'height' }"
                      @click="setImageSize('height')">
                By Height
              </button>
            </div>
          </div>

          <!-- Brightness -->
          <div class="settings-section">
            <div class="settings-label">
              <span>Brightness {{ brightness }}%</span>
            </div>
            <div class="settings-slider">
              <input type="range"
                     min="50"
                     max="150"
                     v-model="brightness"
                     @input="setBrightness"
                     class="slider" />
            </div>
          </div>

          <!-- Image Gap -->
          <div class="settings-section">
            <div class="settings-label">
              <span>Image Gap {{ imageGap }}px</span>
            </div>
            <div class="settings-slider">
              <input type="range"
                     min="0"
                     max="50"
                     v-model="imageGap"
                     @input="setImageGap"
                     class="slider" />
            </div>
          </div>

          <!-- Container Width -->
          <div class="settings-section">
            <div class="settings-label">
              <span>Container Width {{ containerWidth }}%</span>
            </div>
            <div class="settings-slider">
              <input type="range"
                     min="50"
                     max="100"
                     v-model="containerWidth"
                     @input="setContainerWidth"
                     class="slider" />
            </div>
          </div>

          <!-- Hide Page Numbers Toggle -->
          <div class="settings-section toggle-section">
            <div class="settings-label">
              <span>Hide Page Numbers</span>
            </div>
            <div class="toggle-switch">
              <input type="checkbox"
                     id="hidePageNumbers"
                     v-model="hidePageNumbers"
                     @change="setHidePageNumbers" />
              <label for="hidePageNumbers"></label>
            </div>
          </div>

          <!-- Hide Hints Toggle -->
          <div class="settings-section toggle-section">
            <div class="settings-label">
              <span>Hide Hints</span>
            </div>
            <div class="toggle-switch">
              <input type="checkbox"
                     id="hideHints"
                     v-model="hideHints"
                     @change="setHideHints" />
              <label for="hideHints"></label>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Hint Display -->
    <div v-if="currentHint && !hideHints" class="manga-reader-hint" :style="{ opacity: hintOpacity }">
      {{ currentHint }}
    </div>
  </div>
</template>

<script setup>
  import { ref, reactive, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { titleDetailsService } from '../../services/titleDetailsService'
  import { chapterService } from '../../services/chapterService'

  // Props
  const props = defineProps({
    titleName: {
      type: String,
      required: true
    },
    chapterName: {
      type: String,
      required: true
    },
    volumeNumber: {
      type: [Number, String],
      required: true
    },
    teamId: {
      type: [Number, String],
      required: true
    }
  })

  // Router
  const route = useRoute()
  const router = useRouter()

  // State
  const loading = ref(true)
  const error = ref('')
  const chapterData = ref(null)
  const chaptersList = ref([])
  const currentPage = ref(1)
  const debugMode = ref(false)

  // UI State
  const uiVisible = ref(true)
  const showSettings = ref(false)
  const showChapterList = ref(false)
  const currentHint = ref('')
  const hintOpacity = ref(0)

  // Settings State
  const viewMode = ref('single') // 'single' or 'all'
  const currentTheme = ref('dark')
  const readingDirection = ref('horizontal')
  const imageSize = ref('width') // 'width' or 'height'
  const brightness = ref(100)
  const imageGap = ref(13)
  const containerWidth = ref(100)
  const hidePageNumbers = ref(false)
  const hideHints = ref(false)

  // Computed
  const isMobile = computed(() => {
    return window.innerWidth <= 768
  })

  const orderedImages = computed(() => {
    if (!chapterData.value?.imagePaths) return []
    return [...chapterData.value.imagePaths].sort((a, b) => a.orderIndex - b.orderIndex)
  })

  const totalPages = computed(() => orderedImages.value.length)

  const currentImage = computed(() => {
    if (orderedImages.value.length === 0) return null
    return orderedImages.value[currentPage.value - 1] || null
  })

  const imageStyles = computed(() => ({
    filter: `brightness(${brightness.value}%)`,
    maxWidth: imageSize.value === 'width' ? '100%' : 'none',
    maxHeight: imageSize.value === 'height' ? 'calc(100vh - 60px)' : 'none'
  }))

  const allPagesImageStyles = computed(() => ({
    filter: `brightness(${brightness.value}%)`,
    maxWidth: `${containerWidth.value}%`
  }))

  // Methods
  const loadChapter = async () => {
    try {
      loading.value = true
      error.value = ''

      console.log('Loading chapter:', props)

      // Use the route pattern from your backend: /titleName/chapter/chapterName/vvolume/tteamId
      const result = await titleDetailsService.getChapterByRoute(
        props.titleName,
        props.chapterName,
        props.volumeNumber,
        props.teamId,
        route.query.page
      )

      if (result.success && result.data) {
        chapterData.value = result.data

        // Set current page from URL or default to 1
        const urlPage = parseInt(route.query.page) || 1
        currentPage.value = Math.max(1, Math.min(urlPage, totalPages.value))

        // Set view mode from URL
        viewMode.value = route.query.viewMode || 'single'

        // Load chapter list for navigation
        await loadChaptersList()

        // Update reading progress if user is authenticated
        await updateReadingProgress()

        // Update page title
        document.title = `${chapterData.value.titleName} - ${chapterData.value.name || `Ch.${chapterData.value.chapterNumber}`}`

      } else {
        error.value = result.error || 'Chapter not found'
      }
    } catch (err) {
      console.error('Error loading chapter:', err)
      error.value = err.message || 'Failed to load chapter'
    } finally {
      loading.value = false
    }
  }

  const loadChaptersList = async () => {
    if (!chapterData.value?.titleId) return

    try {
      const result = await titleDetailsService.getChapters(chapterData.value.titleId)
      if (result.success) {
        chaptersList.value = result.data || []
      }
    } catch (err) {
      console.error('Error loading chapters list:', err)
    }
  }

  const updateReadingProgress = async () => {
    if (!chapterData.value) return

    try {
      await chapterService.updateReadingProgress(
        chapterData.value.titleId,
        chapterData.value.chapterNumber
      )
    } catch (err) {
      console.error('Error updating reading progress:', err)
    }
  }

  const retryLoad = () => {
    loadChapter()
  }

  // Navigation Methods
  const goToPrevPage = () => {
    if (currentPage.value > 1) {
      changePage(currentPage.value - 1)
    } else {
      gotoPrevChapter()
    }
  }

  const goToNextPage = () => {
    if (currentPage.value < totalPages.value) {
      changePage(currentPage.value + 1)
    } else {
      gotoNextChapter()
    }
  }

  const changePage = (page) => {
    if (typeof page === 'object') {
      page = parseInt(page.target.value)
    }

    const newPage = Math.max(1, Math.min(page, totalPages.value))
    currentPage.value = newPage

    // Update URL
    updateUrl({ page: newPage })
  }

  const gotoPrevChapter = () => {
    if (!chapterData.value?.previousChapterId) {
      goToTitleDetails()
      return
    }

    const url = `/${chapterData.value.titleName}/chapter/${chapterData.value.previousChapterName}/v${chapterData.value.previousChapterVolume}/t${chapterData.value.previousChapterTeamId}`
    const query = {
      viewMode: viewMode.value,
      page: chapterData.value.previousChapterPageCount || 1
    }

    router.push({ path: url, query })
  }

  const gotoNextChapter = () => {
    if (!chapterData.value?.nextChapterId) {
      goToTitleDetails()
      return
    }

    const url = `/${chapterData.value.titleName}/chapter/${chapterData.value.nextChapterName}/v${chapterData.value.nextChapterVolume}/t${chapterData.value.nextChapterTeamId}`
    const query = {
      viewMode: viewMode.value,
      page: 1
    }

    router.push({ path: url, query })
  }

  // FIXED: Navigation to title details page - use correct router path
  const goToTitleDetails = () => {
    const titleName = chapterData.value?.titleName || props.titleName
    router.push(`/${encodeURIComponent(titleName)}`)
  }

  const goToChapter = (chapter) => {
    const url = `/${chapterData.value.titleName}/chapter/${chapter.name || chapter.chapterNumber}/v${chapter.volumeNumber}/t${chapter.teamId}`
    const query = {
      viewMode: viewMode.value,
      page: 1
    }

    router.push({ path: url, query })
    toggleChapterList()
  }

  // UI Methods
  const toggleUI = () => {
    uiVisible.value = !uiVisible.value
    savePreference('uiVisible', uiVisible.value)

    if (!hideHints.value) {
      showHint('Tap center to toggle controls')
    }
  }

  const toggleSettings = () => {
    showSettings.value = !showSettings.value
    showChapterList.value = false
  }

  const toggleChapterList = () => {
    showChapterList.value = !showChapterList.value
    showSettings.value = false
  }

  const handleBackdropClick = (e) => {
    if (e.target === e.currentTarget) {
      showSettings.value = false
      showChapterList.value = false
    }
  }

  const showHint = (message) => {
    if (hideHints.value) return

    currentHint.value = message
    hintOpacity.value = 1

    setTimeout(() => {
      hintOpacity.value = 0
      setTimeout(() => {
        currentHint.value = ''
      }, 300)
    }, 2000)
  }

  // Settings Methods
  const setViewMode = (mode) => {
    viewMode.value = mode
    updateUrl({ viewMode: mode })
    savePreference('viewMode', mode)
  }

  const setTheme = (theme) => {
    currentTheme.value = theme
    savePreference('theme', theme)
  }

  const setReadingDirection = (direction) => {
    readingDirection.value = direction
    savePreference('readingDirection', direction)
  }

  const setImageSize = (size) => {
    imageSize.value = size
    savePreference('imageSize', size)
  }

  const setBrightness = () => {
    savePreference('brightness', brightness.value)
  }

  const setImageGap = () => {
    savePreference('imageGap', imageGap.value)
  }

  const setContainerWidth = () => {
    savePreference('containerWidth', containerWidth.value)
  }

  const setHidePageNumbers = () => {
    savePreference('hidePageNumbers', hidePageNumbers.value)
  }

  const setHideHints = () => {
    savePreference('hideHints', hideHints.value)
  }

  // Helper Methods
  const getImageUrl = (imagePath) => {
    return titleDetailsService.getImageUrl(imagePath)
  }

  const handleImageError = (event) => {
    console.error('Image failed to load:', event.target.src)
    event.target.src = titleDetailsService.getImageUrl('/img/default-cover.png')
  }

  const handleImageLoad = () => {
    // Image loaded successfully
  }

  const updateUrl = (params) => {
    const query = { ...route.query, ...params }
    router.replace({ query })
  }

  const savePreference = (key, value) => {
    localStorage.setItem(`chapterReader_${key}`, JSON.stringify(value))
  }

  const loadPreference = (key, defaultValue) => {
    try {
      const saved = localStorage.getItem(`chapterReader_${key}`)
      return saved ? JSON.parse(saved) : defaultValue
    } catch {
      return defaultValue
    }
  }

  const loadPreferences = () => {
    viewMode.value = route.query.viewMode || loadPreference('viewMode', 'single')
    currentTheme.value = loadPreference('theme', 'dark')
    readingDirection.value = loadPreference('readingDirection', 'horizontal')
    imageSize.value = loadPreference('imageSize', 'width')
    brightness.value = loadPreference('brightness', 100)
    imageGap.value = loadPreference('imageGap', 13)
    containerWidth.value = loadPreference('containerWidth', 100)
    hidePageNumbers.value = loadPreference('hidePageNumbers', false)
    hideHints.value = loadPreference('hideHints', false)
    uiVisible.value = loadPreference('uiVisible', true)
  }

  // Keyboard Navigation
  const handleKeydown = (e) => {
    if (showSettings.value || showChapterList.value) {
      if (e.key === 'Escape') {
        showSettings.value = false
        showChapterList.value = false
      }
      return
    }

    switch (e.key) {
      case 'ArrowLeft':
        e.preventDefault()
        if (viewMode.value === 'single') {
          goToPrevPage()
        } else {
          gotoPrevChapter()
        }
        break
      case 'ArrowRight':
        e.preventDefault()
        if (viewMode.value === 'single') {
          goToNextPage()
        } else {
          gotoNextChapter()
        }
        break
      case 'Escape':
        e.preventDefault()
        toggleUI()
        break
      case 'd':
        if (e.ctrlKey) {
          e.preventDefault()
          debugMode.value = !debugMode.value
        }
        break
    }
  }

  // Lifecycle
  onMounted(async () => {
    loadPreferences()
    await loadChapter()

    // Add event listeners
    document.addEventListener('keydown', handleKeydown)

    // Handle fullscreen on double click
    document.addEventListener('dblclick', (e) => {
      if (e.target.closest('button, select, .popup__content')) return

      if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen().catch(console.error)
      } else {
        document.exitFullscreen()
      }
    })
  })

  onUnmounted(() => {
    document.removeEventListener('keydown', handleKeydown)
  })

  // Watch for route changes
  watch(() => route.query, (newQuery) => {
    if (newQuery.page) {
      const page = parseInt(newQuery.page)
      if (page !== currentPage.value) {
        currentPage.value = Math.max(1, Math.min(page, totalPages.value))
      }
    }
    if (newQuery.viewMode && newQuery.viewMode !== viewMode.value) {
      viewMode.value = newQuery.viewMode
    }
  })

  // Watch for prop changes
  watch(() => [props.titleName, props.chapterName, props.volumeNumber, props.teamId], () => {
    loadChapter()
  })
</script>

<style scoped>
  /* Chapter Container */
  .chapter-container {
    min-height: 100vh;
    background-color: var(--color-background);
    color: var(--color-text);
    position: relative;
    overflow-x: hidden;
  }

    .chapter-container.dark {
      --manga-bg: #0a0a0a;
      --manga-text: #ffffff;
      --manga-navbar-bg: rgba(0, 0, 0, 0.95);
      --manga-border: #2a2a2a;
    }

    .chapter-container.light {
      --manga-bg: #ffffff;
      --manga-text: #000000;
      --manga-navbar-bg: rgba(255, 255, 255, 0.95);
      --manga-border: #e0e0e0;
    }

  /* Manga Navbar */
  .manga-navbar {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 1000;
    background: var(--manga-navbar-bg);
    backdrop-filter: blur(10px);
    border-bottom: 1px solid var(--manga-border);
    transition: transform 0.3s ease;
  }

    .manga-navbar.hidden {
      transform: translateY(-100%);
    }

  .navbar-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    max-width: 1200px;
    margin: 0 auto;
  }

  .navbar-left {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .back-button {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    background: transparent;
    border: 1px solid var(--manga-border);
    border-radius: 0.375rem;
    color: var(--manga-text);
    cursor: pointer;
    transition: all 0.2s ease;
  }

    .back-button:hover {
      background: var(--color-background-mute);
    }

  .chapter-info {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .title-name {
    font-size: 1.125rem;
    font-weight: 600;
    cursor: pointer;
    margin: 0;
  }

  .chapter-nav {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .chapter-nav-btn {
    padding: 0.25rem;
    background: transparent;
    border: none;
    color: var(--manga-text);
    cursor: pointer;
    border-radius: 0.25rem;
    transition: background-color 0.2s ease;
  }

    .chapter-nav-btn:hover:not(:disabled) {
      background: var(--color-background-mute);
    }

    .chapter-nav-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .chapter-name {
    font-size: 1rem;
    font-weight: 500;
    margin: 0;
  }

  .navbar-right {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .settings-btn,
  .chapter-list-btn {
    padding: 0.5rem;
    background: transparent;
    border: 1px solid var(--manga-border);
    border-radius: 0.375rem;
    color: var(--manga-text);
    cursor: pointer;
    transition: all 0.2s ease;
  }

    .settings-btn:hover,
    .chapter-list-btn:hover {
      background: var(--color-background-mute);
    }

  /* Chapter Content Wrapper */
  .chapter-content {
    position: relative;
    width: 100%;
    height: 100%;
  }

  /* Content Areas */
  .content-area {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 4rem 1rem 1rem;
    position: relative;
  }

  .manga-image-container {
    position: relative;
    max-width: 100%;
    max-height: calc(100vh - 120px);
    display: flex;
    justify-content: center;
    align-items: center;
  }

  .manga-image {
    max-width: 100%;
    height: auto;
    object-fit: contain;
    display: block;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  }

  /* All Pages View */
  .all-pages-container {
    padding-top: 4rem;
    min-height: 100vh;
    position: relative;
  }

  .manga-content-wrapper {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 1rem;
  }

  .manga-pages-wrapper {
    display: flex;
    flex-direction: column;
    align-items: center;
  }

  .manga-page-wrapper {
    position: relative;
    width: 100%;
    display: flex;
    flex-direction: column;
    align-items: center;
  }

  .page-number-indicator {
    background: rgba(0, 0, 0, 0.7);
    color: white;
    padding: 0.25rem 0.75rem;
    border-radius: 1rem;
    font-size: 0.875rem;
    margin-bottom: 0.5rem;
    font-weight: 500;
  }

  /* Page Indicator */
  .page-indicator {
    position: fixed;
    bottom: 1rem;
    left: 50%;
    transform: translateX(-50%);
    z-index: 100;
    transition: opacity 0.3s ease;
  }

    .page-indicator.hidden {
      opacity: 0;
      pointer-events: none;
    }

  .page-selector {
    background: var(--color-background-soft);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    padding: 0.5rem 1rem;
    color: var(--color-text);
    font-size: 0.875rem;
    cursor: pointer;
  }

  /* FIXED: Enhanced Touch Zones - Full Screen Coverage */
  .tap-zones-fullscreen {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    pointer-events: none;
    z-index: 50;
  }

  .tap-zones-all-pages {
    position: absolute;
    top: 4rem; /* Start after navbar */
    left: 0;
    right: 0;
    bottom: 0;
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    pointer-events: none;
    z-index: 50;
  }

  .tap-zone {
    pointer-events: auto;
    cursor: pointer;
    background: transparent;
    border: none;
    padding: 0;
    margin: 0;
    transition: background-color 0.1s ease;
  }

    .tap-zone:active {
      background: rgba(255, 255, 255, 0.05);
    }

  /* Debug Mode Visualization */
  .chapter-container.debug-mode .tap-zone {
    border: 2px dashed rgba(255, 255, 255, 0.5);
    position: relative;
  }

  .chapter-container.debug-mode .tap-zone-left {
    background: rgba(255, 255, 0, 0.1);
  }

  .chapter-container.debug-mode .tap-zone-center {
    background: rgba(0, 255, 0, 0.1);
  }

  .chapter-container.debug-mode .tap-zone-right {
    background: rgba(0, 0, 255, 0.1);
  }

  .chapter-container.debug-mode .tap-zone::after {
    content: attr(data-zone);
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    color: white;
    font-weight: bold;
    font-size: 14px;
    text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.8);
    pointer-events: none;
    background: rgba(0, 0, 0, 0.7);
    padding: 0.5rem;
    border-radius: 0.25rem;
    white-space: nowrap;
  }

  /* Static Navigation */
  .static-navigation {
    margin-top: 3rem;
    padding: 2rem 0;
  }

  .chapter-navigation-controls {
    display: flex;
    justify-content: center;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .nav-btn {
    padding: 0.75rem 1.5rem;
    border-radius: 0.5rem;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s ease;
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
  }

    .nav-btn.prev-chapter,
    .nav-btn.next-chapter {
      background: var(--color-background-mute);
      color: var(--color-text);
      border: 1px solid var(--color-border);
    }

      .nav-btn.prev-chapter:hover,
      .nav-btn.next-chapter:hover {
        background: var(--color-background-soft);
        border-color: var(--color-accent);
      }

      .nav-btn.prev-chapter:disabled,
      .nav-btn.next-chapter:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

    .nav-btn.back-to-title {
      background: var(--color-accent);
      color: white;
      border: 1px solid var(--color-accent);
    }

      .nav-btn.back-to-title:hover {
        background: var(--color-accent-hover);
      }

  /* Loading and Error States */
  .loading-container,
  .error-container,
  .no-images-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 2rem;
  }

  .loading-spinner,
  .error-content,
  .no-images-content {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    max-width: 400px;
  }

  /* Popups */
  .popup {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    backdrop-filter: blur(4px);
    z-index: 2000;
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 1rem;
  }

  .popup__content {
    background: var(--color-background-soft);
    border: 1px solid var(--color-border);
    border-radius: 1rem;
    max-width: 500px;
    width: 100%;
    max-height: 80vh;
    display: flex;
    flex-direction: column;
  }

    .popup__content.scrollable {
      overflow-y: auto;
    }

  .popup__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    border-bottom: 1px solid var(--color-border);
  }

    .popup__header h3 {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--color-text);
    }

  .close-btn {
    padding: 0.5rem;
    background: transparent;
    border: none;
    color: var(--color-text);
    cursor: pointer;
    border-radius: 0.375rem;
    transition: background-color 0.2s ease;
  }

    .close-btn:hover {
      background: var(--color-background-mute);
    }

  /* Chapter List */
  .chapter-list {
    padding: 0;
    max-height: 60vh;
    overflow-y: auto;
  }

  .chapter-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    cursor: pointer;
    border-bottom: 1px solid var(--color-border);
    transition: background-color 0.2s ease;
  }

    .chapter-item:hover {
      background: var(--color-background-mute);
    }

    .chapter-item.active {
      background: var(--color-accent);
      color: white;
    }

    .chapter-item:last-child {
      border-bottom: none;
    }

  .chapter-title {
    font-weight: 500;
    margin-bottom: 0.25rem;
  }

  .chapter-team {
    font-size: 0.875rem;
    opacity: 0.75;
  }

  /* Settings Content */
  .settings-content {
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
  }

  .settings-section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

    .settings-section.toggle-section {
      flex-direction: row;
      justify-content: space-between;
      align-items: center;
    }

  .settings-label {
    font-weight: 500;
    color: var(--color-text);
    font-size: 0.9rem;
  }

  .settings-options {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
  }

  .settings-btn {
    padding: 0.5rem 1rem;
    background: var(--color-background-mute);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    color: var(--color-text);
    cursor: pointer;
    transition: all 0.2s ease;
    font-size: 0.875rem;
  }

    .settings-btn:hover {
      background: var(--color-background-soft);
      border-color: var(--color-accent);
    }

    .settings-btn.active {
      background: var(--color-accent);
      color: white;
      border-color: var(--color-accent);
    }

  .settings-slider {
    width: 100%;
  }

  .slider {
    width: 100%;
    height: 4px;
    background: var(--color-background-mute);
    border-radius: 2px;
    outline: none;
    -webkit-appearance: none;
  }

    .slider::-webkit-slider-thumb {
      -webkit-appearance: none;
      width: 16px;
      height: 16px;
      background: var(--color-accent);
      border-radius: 50%;
      cursor: pointer;
    }

    .slider::-moz-range-thumb {
      width: 16px;
      height: 16px;
      background: var(--color-accent);
      border-radius: 50%;
      cursor: pointer;
      border: none;
    }

  /* Toggle Switch */
  .toggle-switch {
    position: relative;
    width: 44px;
    height: 24px;
  }

    .toggle-switch input {
      opacity: 0;
      width: 0;
      height: 0;
    }

    .toggle-switch label {
      position: absolute;
      cursor: pointer;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: var(--color-background-mute);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      transition: all 0.3s ease;
    }

      .toggle-switch label::before {
        position: absolute;
        content: "";
        height: 16px;
        width: 16px;
        left: 3px;
        top: 3px;
        background: white;
        border-radius: 50%;
        transition: all 0.3s ease;
      }

    .toggle-switch input:checked + label {
      background: var(--color-accent);
      border-color: var(--color-accent);
    }

      .toggle-switch input:checked + label::before {
        transform: translateX(20px);
      }

  /* Hint Display */
  .manga-reader-hint {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: rgba(0, 0, 0, 0.8);
    color: white;
    padding: 0.75rem 1.5rem;
    border-radius: 0.5rem;
    font-size: 0.875rem;
    font-weight: 500;
    z-index: 1500;
    transition: opacity 0.3s ease;
    pointer-events: none;
  }

  /* Mobile Responsive */
  @media (max-width: 768px) {
    .navbar-content {
      padding: 0.5rem;
      flex-wrap: wrap;
      gap: 0.5rem;
    }

    .back-text {
      display: none;
    }

    .chapter-info {
      order: 3;
      width: 100%;
      text-align: center;
    }

    .title-name {
      font-size: 1rem;
    }

    .chapter-name {
      font-size: 0.875rem;
    }

    .content-area {
      padding: 3rem 0.5rem 1rem;
    }

    .popup__content {
      margin: 0.5rem;
      max-height: 90vh;
    }

    .chapter-navigation-controls {
      flex-direction: column;
      align-items: stretch;
    }

    .nav-btn {
      width: 100%;
    }

    .settings-options {
      flex-direction: column;
    }

    .settings-btn {
      width: 100%;
      text-align: center;
    }
  }

  @media (max-width: 480px) {
    .navbar-content {
      padding: 0.25rem;
    }

    .chapter-nav {
      gap: 0.25rem;
    }

    .settings-section.toggle-section {
      flex-direction: column;
      gap: 0.5rem;
      align-items: flex-start;
    }
  }
</style>
