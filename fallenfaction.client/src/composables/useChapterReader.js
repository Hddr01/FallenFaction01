// composables/useChapterReader.js - Vue 3 composable for chapter reading functionality
import { ref, reactive, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { titleDetailsService } from '@/services/titleDetailsService'
import { chapterService } from '@/services/chapterService'

export function useChapterReader(props) {
  const router = useRouter()
  const route = useRoute()

  // ===================================================================
  // STATE MANAGEMENT
  // ===================================================================

  // Loading and error states
  const loading = ref(true)
  const error = ref('')

  // Chapter data
  const chapterData = ref(null)
  const chaptersList = ref([])

  // Reading state
  const currentPage = ref(1)
  const startTime = ref(null)
  const readingTime = ref(0)

  // UI state
  const uiVisible = ref(true)
  const showSettings = ref(false)
  const showChapterList = ref(false)

  // Settings with reactive defaults
  const settings = reactive({
    viewMode: 'single', // 'single' or 'all'
    theme: 'dark', // 'dark', 'light', 'system'
    readingDirection: 'horizontal', // 'horizontal', 'vertical'
    imageSize: 'width', // 'width', 'height'
    brightness: 100, // 50-150
    imageGap: 13, // 0-50px
    containerWidth: 100, // 50-100%
    hidePageNumbers: false,
    hideHints: false,
    enableAnalytics: true,
    autoProgressTracking: true
  })

  // ===================================================================
  // COMPUTED PROPERTIES
  // ===================================================================

  const isMobile = computed(() => {
    if (typeof window === 'undefined') return false
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

  const canGoToPrevious = computed(() => {
    if (settings.viewMode === 'single') {
      return currentPage.value > 1 || !!chapterData.value?.previousChapterId
    }
    return !!chapterData.value?.previousChapterId
  })

  const canGoToNext = computed(() => {
    if (settings.viewMode === 'single') {
      return currentPage.value < totalPages.value || !!chapterData.value?.nextChapterId
    }
    return !!chapterData.value?.nextChapterId
  })

  const readingProgress = computed(() => {
    if (totalPages.value === 0) return 0
    return (currentPage.value / totalPages.value) * 100
  })

  const imageStyles = computed(() => ({
    filter: `brightness(${settings.brightness}%)`,
    maxWidth: settings.imageSize === 'width' ? '100%' : 'none',
    maxHeight: settings.imageSize === 'height' ? 'calc(100vh - 60px)' : 'none'
  }))

  const allPagesImageStyles = computed(() => ({
    filter: `brightness(${settings.brightness}%)`,
    maxWidth: `${settings.containerWidth}%`
  }))

  // ===================================================================
  // CORE FUNCTIONALITY
  // ===================================================================

  const loadChapter = async () => {
    try {
      loading.value = true
      error.value = ''
      startTime.value = Date.now()

      console.log('Loading chapter:', props)

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

        // Set view mode from URL or saved preference
        settings.viewMode = route.query.viewMode || getSavedPreference('viewMode', 'single')

        // Load chapter list for navigation
        await loadChaptersList()

        // Update reading progress if enabled
        if (settings.autoProgressTracking) {
          await updateReadingProgress()
        }

        // Preload images for better experience
        if (orderedImages.value.length > 0) {
          chapterService.preloadImages(orderedImages.value.slice(0, 3))
        }

        // Update page title
        updatePageTitle()

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
    if (!chapterData.value || !settings.autoProgressTracking) return

    try {
      await chapterService.updateReadingProgress(
        chapterData.value.titleId,
        chapterData.value.chapterNumber
      )
    } catch (err) {
      console.error('Error updating reading progress:', err)
    }
  }

  const updatePageTitle = () => {
    if (!chapterData.value) return

    const title = `${chapterData.value.titleName} - Vol.${chapterData.value.volumeNumber} Ch.${chapterData.value.chapterNumber}`
    document.title = title
  }

  const retryLoad = () => {
    loadChapter()
  }

  // ===================================================================
  // NAVIGATION
  // ===================================================================

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
    if (typeof page === 'object' && page.target) {
      page = parseInt(page.target.value)
    }

    const newPage = Math.max(1, Math.min(page, totalPages.value))
    currentPage.value = newPage

    // Update URL without causing a route change
    updateUrl({ page: newPage })

    // Track analytics if enabled
    if (settings.enableAnalytics) {
      trackPageView(newPage)
    }
  }

  const gotoPrevChapter = () => {
    if (!chapterData.value?.previousChapterId) {
      goToTitleDetails()
      return
    }

    const url = `/${chapterData.value.titleName}/chapter/${chapterData.value.previousChapterName}/v${chapterData.value.previousChapterVolume}/t${chapterData.value.previousChapterTeamId}`
    const query = {
      viewMode: settings.viewMode,
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
      viewMode: settings.viewMode,
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
      viewMode: settings.viewMode,
      page: 1
    }

    router.push({ path: url, query })
  }

  // ===================================================================
  // UI CONTROLS
  // ===================================================================

  const toggleUI = () => {
    uiVisible.value = !uiVisible.value
    savePreference('uiVisible', uiVisible.value)

    if (!settings.hideHints) {
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

  const showHint = (message, duration = 2000) => {
    if (settings.hideHints) return

    // Implementation would depend on your hint display system
    console.log('Hint:', message)
  }

  // ===================================================================
  // SETTINGS MANAGEMENT
  // ===================================================================

  const updateSetting = (key, value) => {
    settings[key] = value
    savePreference(key, value)

    // Handle special cases
    if (key === 'viewMode') {
      updateUrl({ viewMode: value })
    }
  }

  const savePreference = (key, value) => {
    try {
      localStorage.setItem(`chapterReader_${key}`, JSON.stringify(value))
    } catch (err) {
      console.warn('Failed to save preference:', key, err)
    }
  }

  const getSavedPreference = (key, defaultValue) => {
    try {
      const saved = localStorage.getItem(`chapterReader_${key}`)
      return saved ? JSON.parse(saved) : defaultValue
    } catch {
      return defaultValue
    }
  }

  const loadPreferences = () => {
    // Load all saved preferences
    Object.keys(settings).forEach(key => {
      const saved = getSavedPreference(key, settings[key])
      settings[key] = saved
    })

    // Load UI state
    uiVisible.value = getSavedPreference('uiVisible', true)
  }

  const resetPreferences = () => {
    Object.keys(settings).forEach(key => {
      localStorage.removeItem(`chapterReader_${key}`)
    })

    // Reset to defaults
    settings.viewMode = 'single'
    settings.theme = 'dark'
    settings.readingDirection = 'horizontal'
    settings.imageSize = 'width'
    settings.brightness = 100
    settings.imageGap = 13
    settings.containerWidth = 100
    settings.hidePageNumbers = false
    settings.hideHints = false
  }

  // ===================================================================
  // ANALYTICS & TRACKING
  // ===================================================================

  const trackPageView = async (page) => {
    if (!settings.enableAnalytics || !chapterData.value) return

    try {
      await chapterService.trackChapterView(
        chapterData.value.id,
        totalPages.value,
        Math.floor((Date.now() - startTime.value) / 1000)
      )
    } catch (err) {
      // Silently fail - analytics shouldn't break reading experience
    }
  }

  const trackChapterCompletion = async () => {
    if (!settings.enableAnalytics || !chapterData.value) return

    const timeSpent = Math.floor((Date.now() - startTime.value) / 1000)
    readingTime.value = timeSpent

    try {
      await chapterService.trackChapterView(
        chapterData.value.id,
        totalPages.value,
        timeSpent
      )
    } catch (err) {
      // Silently fail
    }
  }

  // ===================================================================
  // UTILITIES
  // ===================================================================

  const updateUrl = (params) => {
    const query = { ...route.query, ...params }
    router.replace({ query }).catch(() => { }) // Ignore navigation errors
  }

  const getImageUrl = (imagePath) => {
    return chapterService.getImageUrl(imagePath)
  }

  const handleImageError = (event) => {
    console.error('Image failed to load:', event.target.src)
    event.target.src = chapterService.getImageUrl('/img/default-chapter.png')
  }

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
        if (settings.viewMode === 'single') {
          goToPrevPage()
        } else {
          gotoPrevChapter()
        }
        break
      case 'ArrowRight':
        e.preventDefault()
        if (settings.viewMode === 'single') {
          goToNextPage()
        } else {
          gotoNextChapter()
        }
        break
      case 'Escape':
        e.preventDefault()
        toggleUI()
        break
    }
  }

  // ===================================================================
  // WATCHERS
  // ===================================================================

  watch(() => route.query, (newQuery) => {
    if (newQuery.page) {
      const page = parseInt(newQuery.page)
      if (page !== currentPage.value) {
        currentPage.value = Math.max(1, Math.min(page, totalPages.value))
      }
    }
    if (newQuery.viewMode && newQuery.viewMode !== settings.viewMode) {
      settings.viewMode = newQuery.viewMode
    }
  })

  watch(() => [props.titleName, props.chapterName, props.volumeNumber, props.teamId], () => {
    loadChapter()
  })

  // Track reading completion when reaching last page
  watch(currentPage, (newPage, oldPage) => {
    if (newPage === totalPages.value && oldPage < totalPages.value) {
      trackChapterCompletion()
    }
  })

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  onMounted(() => {
    loadPreferences()
    loadChapter()

    // Add keyboard listener
    document.addEventListener('keydown', handleKeydown)

    // Add fullscreen on double-click
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

    // Track session end
    if (settings.enableAnalytics && startTime.value) {
      trackChapterCompletion()
    }
  })

  // ===================================================================
  // RETURN API
  // ===================================================================

  return {
    // State
    loading,
    error,
    chapterData,
    chaptersList,
    currentPage,
    settings,
    uiVisible,
    showSettings,
    showChapterList,

    // Computed
    isMobile,
    orderedImages,
    totalPages,
    currentImage,
    canGoToPrevious,
    canGoToNext,
    readingProgress,
    imageStyles,
    allPagesImageStyles,

    // Core functions
    loadChapter,
    retryLoad,

    // Navigation
    goToPrevPage,
    goToNextPage,
    changePage,
    gotoPrevChapter,
    gotoNextChapter,
    goToTitleDetails,
    goToChapter,

    // UI controls
    toggleUI,
    toggleSettings,
    toggleChapterList,
    showHint,

    // Settings
    updateSetting,
    resetPreferences,

    // Utilities
    getImageUrl,
    handleImageError,
    updateUrl
  }
}
