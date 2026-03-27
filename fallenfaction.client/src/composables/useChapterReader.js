// composables/useChapterReader.js - Enhanced Vue 3 composable with scroll position memory
import { ref, reactive, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { titleDetailsService } from '@/services/titleDetailsService'
import { chapterService } from '@/services/chapterService'
import { buildTitleSlug } from '@/utils/titleSlug.js'

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
  // SCROLL POSITION MEMORY SYSTEM
  // ===================================================================

  // Navigation history for tracking scroll positions
  const navigationHistory = ref([])
  const currentHistoryIndex = ref(-1)
  const maxHistorySize = 50

  // Scroll position tracking
  const scrollPositions = ref(new Map())
  const isRestoringScroll = ref(false)
  const scrollRestoreTimeout = ref(null)
  let scrollSaveTimer = null

  // Generate unique key for current location
  const getCurrentLocationKey = () => {
    return `${props.titleName}/${props.chapterName}/v${props.volumeNumber}/t${props.teamId}/${settings.viewMode}/${currentPage.value}`
  }

  // Save current scroll position
  const saveScrollPosition = () => {
    if (isRestoringScroll.value || typeof window === 'undefined') return

    const key = getCurrentLocationKey()
    const scrollY = window.scrollY || window.pageYOffset || 0
    const scrollX = window.scrollX || window.pageXOffset || 0

    const position = {
      key,
      scrollY,
      scrollX,
      timestamp: Date.now(),
      viewMode: settings.viewMode,
      page: currentPage.value
    }

    scrollPositions.value.set(key, position)

    // Also save to localStorage for persistence
    try {
      const savedPositions = JSON.parse(localStorage.getItem('chapterScrollPositions') || '{}')
      savedPositions[key] = position

      // Keep only recent positions (last 100)
      const entries = Object.entries(savedPositions)
      if (entries.length > 100) {
        entries.sort((a, b) => b[1].timestamp - a[1].timestamp)
        const recent = Object.fromEntries(entries.slice(0, 100))
        localStorage.setItem('chapterScrollPositions', JSON.stringify(recent))
      } else {
        localStorage.setItem('chapterScrollPositions', JSON.stringify(savedPositions))
      }
    } catch (error) {
      console.warn('Failed to save scroll position to localStorage:', error)
    }

    console.log('Saved scroll position:', position)
  }

  // Restore scroll position for a given key
  const restoreScrollPosition = (key, fallbackBehavior = 'top') => {
    let position = scrollPositions.value.get(key)

    if (!position) {
      try {
        const savedPositions = JSON.parse(localStorage.getItem('chapterScrollPositions') || '{}')
        position = savedPositions[key]
        if (position) {
          scrollPositions.value.set(key, position)
        }
      } catch (error) {
        console.warn('Failed to load scroll position from localStorage:', error)
      }
    }

    if (position && typeof window !== 'undefined') {
      console.log('Restoring scroll position:', position)
      isRestoringScroll.value = true

      if (scrollRestoreTimeout.value) {
        clearTimeout(scrollRestoreTimeout.value)
      }

      nextTick(() => {
        scrollRestoreTimeout.value = setTimeout(() => {
          window.scrollTo({
            top: position.scrollY,
            left: position.scrollX,
            behavior: 'auto'
          })

          setTimeout(() => {
            isRestoringScroll.value = false
          }, 500)
        }, 100)
      })

      return true
    } else {
      console.log('No saved position found, using fallback:', fallbackBehavior)
      nextTick(() => {
        if (fallbackBehavior === 'bottom') {
          scrollToBottom()
        } else {
          scrollToTop()
        }
      })
      return false
    }
  }

  // Add to navigation history
  const addToNavigationHistory = (direction = 'forward') => {
    const currentKey = getCurrentLocationKey()
    const historyItem = {
      key: currentKey,
      direction,
      timestamp: Date.now(),
      titleName: props.titleName,
      chapterName: props.chapterName,
      volumeNumber: props.volumeNumber,
      teamId: props.teamId,
      viewMode: settings.viewMode,
      page: currentPage.value
    }

    if (currentHistoryIndex.value < navigationHistory.value.length - 1) {
      navigationHistory.value = navigationHistory.value.slice(0, currentHistoryIndex.value + 1)
    }

    navigationHistory.value.push(historyItem)
    currentHistoryIndex.value = navigationHistory.value.length - 1

    if (navigationHistory.value.length > maxHistorySize) {
      navigationHistory.value = navigationHistory.value.slice(-maxHistorySize)
      currentHistoryIndex.value = navigationHistory.value.length - 1
    }

    console.log('Added to navigation history:', historyItem)
  }

  // Check if going back in history
  const isGoingBack = (targetKey) => {
    if (currentHistoryIndex.value > 0) {
      const previous = navigationHistory.value[currentHistoryIndex.value - 1]
      return previous && previous.key === targetKey
    }
    return false
  }

  // Throttled scroll listener
  const handleScroll = () => {
    if (isRestoringScroll.value) return

    if (scrollSaveTimer) {
      clearTimeout(scrollSaveTimer)
    }

    scrollSaveTimer = setTimeout(() => {
      saveScrollPosition()
    }, 500)
  }

  // Setup scroll tracking
  const setupScrollTracking = () => {
    if (typeof window === 'undefined') return

    try {
      const savedPositions = JSON.parse(localStorage.getItem('chapterScrollPositions') || '{}')
      Object.entries(savedPositions).forEach(([key, position]) => {
        scrollPositions.value.set(key, position)
      })
      console.log('Loaded saved scroll positions:', scrollPositions.value.size)
    } catch (error) {
      console.warn('Failed to load saved scroll positions:', error)
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    window.addEventListener('beforeunload', () => {
      saveScrollPosition()
    })
  }

  const cleanupScrollTracking = () => {
    if (typeof window === 'undefined') return

    window.removeEventListener('scroll', handleScroll)
    window.removeEventListener('beforeunload', saveScrollPosition)

    if (scrollSaveTimer) {
      clearTimeout(scrollSaveTimer)
    }

    if (scrollRestoreTimeout.value) {
      clearTimeout(scrollRestoreTimeout.value)
    }
  }

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

        const urlPage = parseInt(route.query.page) || 1
        currentPage.value = Math.max(1, Math.min(urlPage, totalPages.value))

        // Handle view mode with proper priority: URL > saved preference > default
        const urlViewMode = route.query.viewMode
        const savedViewMode = getSavedPreference('viewMode', 'single')

        if (urlViewMode) {
          settings.viewMode = urlViewMode
          savePreference('viewMode', urlViewMode)
        } else {
          settings.viewMode = savedViewMode
          updateUrl({ viewMode: savedViewMode })
        }

        await loadChaptersList()

        if (settings.autoProgressTracking) {
          await updateReadingProgress()
        }

        if (orderedImages.value.length > 0) {
          await preloadImages(orderedImages.value.slice(0, 3))
        }

        updatePageTitle()

        if (typeof window !== 'undefined') {
          await nextTick()
          handleScrollBehavior()
        }

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
    if (!chapterData.value || typeof document === 'undefined') return

    const title = `${chapterData.value.titleName} - Vol.${chapterData.value.volumeNumber} Ch.${chapterData.value.chapterNumber}`
    document.title = title
  }

  const retryLoad = () => {
    loadChapter()
  }

  // ===================================================================
  // ENHANCED NAVIGATION WITH SCROLL MEMORY
  // ===================================================================

  const goToPrevPage = () => {
    const targetKey = `${props.titleName}/${props.chapterName}/v${props.volumeNumber}/t${props.teamId}/${settings.viewMode}/${currentPage.value - 1}`

    if (currentPage.value > 1) {
      saveScrollPosition()
      addToNavigationHistory('forward')

      const newPage = currentPage.value - 1
      currentPage.value = newPage
      updateUrl({ page: newPage })

      if (isGoingBack(targetKey)) {
        restoreScrollPosition(targetKey, 'top')
        currentHistoryIndex.value--
      } else {
        scrollToTop()
      }
    } else {
      gotoPrevChapter()
    }
  }

  const goToNextPage = () => {
    if (currentPage.value < totalPages.value) {
      saveScrollPosition()
      addToNavigationHistory('forward')

      const newPage = currentPage.value + 1
      currentPage.value = newPage
      updateUrl({ page: newPage })

      scrollToTop()
    } else {
      gotoNextChapter()
    }
  }

  const changePage = (page) => {
    if (typeof page === 'object' && page.target) {
      page = parseInt(page.target.value)
    }

    const newPage = Math.max(1, Math.min(page, totalPages.value))
    const oldPage = currentPage.value

    if (newPage === oldPage) return

    const targetKey = `${props.titleName}/${props.chapterName}/v${props.volumeNumber}/t${props.teamId}/${settings.viewMode}/${newPage}`

    if (newPage !== oldPage) {
      saveScrollPosition()
      if (newPage > oldPage) {
        addToNavigationHistory('forward')
      }
    }

    currentPage.value = newPage
    updateUrl({ page: newPage })

    if (settings.viewMode === 'single') {
      if (newPage < oldPage && isGoingBack(targetKey)) {
        restoreScrollPosition(targetKey, 'top')
        currentHistoryIndex.value--
      } else {
        scrollToTop()
      }
    }

    if (settings.enableAnalytics) {
      trackPageView(newPage)
    }
  }

  const gotoPrevChapter = () => {
    if (!chapterData.value?.previousChapterId) {
      goToTitleDetails()
      return
    }

    const targetKey = `${chapterData.value.titleName}/${chapterData.value.previousChapterName}/v${chapterData.value.previousChapterVolume}/t${chapterData.value.previousChapterTeamId}/${settings.viewMode}/${chapterData.value.previousChapterPageCount || 1}`

    saveScrollPosition()
    addToNavigationHistory('forward')

    const url = `/${buildTitleSlug(chapterData.value.titleName, chapterData.value.titleId)}/chapter/${chapterData.value.previousChapterName}/v${chapterData.value.previousChapterVolume}/t${chapterData.value.previousChapterTeamId}`
    const query = {
      viewMode: settings.viewMode,
      page: chapterData.value.previousChapterPageCount || 1,
      restoreScroll: isGoingBack(targetKey) ? 'true' : 'false'
    }

    router.push({ path: url, query })
  }

  const gotoNextChapter = () => {
    if (!chapterData.value?.nextChapterId) {
      goToTitleDetails()
      return
    }

    saveScrollPosition()
    addToNavigationHistory('forward')

    const url = `/${buildTitleSlug(chapterData.value.titleName, chapterData.value.titleId)}/chapter/${chapterData.value.nextChapterName}/v${chapterData.value.nextChapterVolume}/t${chapterData.value.nextChapterTeamId}`
    const query = {
      viewMode: settings.viewMode,
      page: 1,
      restoreScroll: 'false'
    }

    router.push({ path: url, query })
  }

  const goToTitleDetails = () => {
    const titleName = chapterData.value?.titleName || props.titleName
    const titleId = chapterData.value?.titleId
    const slug = titleId ? buildTitleSlug(titleName, titleId) : encodeURIComponent(titleName)
    router.push(`/${slug}`)
  }

  const goToChapter = (chapter) => {
    const url = `/${buildTitleSlug(chapterData.value.titleName, chapterData.value.titleId)}/chapter/${chapter.name || chapter.chapterNumber}/v${chapter.volumeNumber}/t${chapter.teamId}`
    const query = {
      viewMode: settings.viewMode,
      page: 1,
      restoreScroll: 'false'
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
    console.log('Hint:', message)

    // You can implement your hint display logic here
    // For example, creating a toast notification or modal
  }

  // ===================================================================
  // SETTINGS MANAGEMENT
  // ===================================================================

  const updateSetting = (key, value) => {
    const oldValue = settings[key]
    settings[key] = value
    savePreference(key, value)

    if (key === 'viewMode') {
      updateUrl({ viewMode: value })

      if (typeof window !== 'undefined') {
        setTimeout(() => scrollToTop(), 100)
      }
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
    Object.keys(settings).forEach(key => {
      const saved = getSavedPreference(key, settings[key])
      settings[key] = saved
    })

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

  const handleScrollBehavior = () => {
    const restoreScroll = route.query.restoreScroll
    const scrollTo = route.query.scrollTo
    const currentKey = getCurrentLocationKey()

    if (restoreScroll === 'true') {
      const restored = restoreScrollPosition(currentKey)
      if (restored) {
        currentHistoryIndex.value--
      }
    } else if (settings.viewMode === 'single') {
      scrollToTop()
    } else if (settings.viewMode === 'all') {
      if (scrollTo === 'bottom') {
        setTimeout(() => scrollToBottom(), 100)
      } else {
        scrollToTop()
      }
    }

    const query = { ...route.query }
    delete query.scrollTo
    delete query.restoreScroll
    if (Object.keys(query).length !== Object.keys(route.query).length) {
      router.replace({ query }).catch(() => { })
    }
  }

  const scrollToTop = () => {
    if (typeof window === 'undefined') return

    window.scrollTo({
      top: 0,
      behavior: 'auto' // Use 'auto' for instant scroll when restoring position
    })

    const container = document.getElementById('chapterContainer')
    if (container) {
      container.scrollTop = 0
    }
  }

  const scrollToBottom = () => {
    if (typeof window === 'undefined') return

    setTimeout(() => {
      window.scrollTo({
        top: document.documentElement.scrollHeight,
        behavior: 'auto'
      })
    }, 200)
  }

  const preloadImages = async (imagePaths) => {
    if (!imagePaths || imagePaths.length === 0) return

    const promises = imagePaths.slice(0, 5).map(imagePath => {
      return new Promise((resolve) => {
        const img = new Image()
        img.onload = resolve
        img.onerror = resolve
        img.src = getImageUrl(imagePath.imagePath || imagePath)
      })
    })

    try {
      await Promise.all(promises)
      console.log('Chapter images preloaded successfully')
    } catch (error) {
      console.warn('Some images failed to preload:', error)
    }
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

  // Enhanced tap zone handler with scroll memory
  const handleTapZoneClick = (zone, action) => {
    saveScrollPosition()

    if (typeof action === 'function') {
      action()
    }

    if (!settings.hideHints) {
      let hintMessage = ''
      switch (zone) {
        case 'left':
          hintMessage = settings.viewMode === 'single' ? 'Previous page' : 'Previous chapter'
          break
        case 'center':
          hintMessage = 'Toggle controls'
          break
        case 'right':
          hintMessage = settings.viewMode === 'single' ? 'Next page' : 'Next chapter'
          break
      }
      if (hintMessage) {
        const deviceType = typeof window !== 'undefined' && window.innerWidth <= 768 ? 'mobile' : 'desktop'
        const actionPrefix = deviceType === 'mobile' ? 'Tap' : 'Click'
        showHint(`${actionPrefix}: ${hintMessage}`)
      }
    }
  }

  // ===================================================================
  // WATCHERS
  // ===================================================================

  watch(() => route.query, (newQuery, oldQuery) => {
    if (newQuery.page) {
      const page = parseInt(newQuery.page)
      if (page !== currentPage.value) {
        currentPage.value = Math.max(1, Math.min(page, totalPages.value))
      }
    }

    if (newQuery.viewMode && newQuery.viewMode !== settings.viewMode) {
      settings.viewMode = newQuery.viewMode
      savePreference('viewMode', newQuery.viewMode)
    }

    if (newQuery.restoreScroll && newQuery.restoreScroll !== oldQuery?.restoreScroll) {
      if (typeof window !== 'undefined') {
        setTimeout(() => handleScrollBehavior(), 100)
      }
    }
  })

  watch(() => [props.titleName, props.chapterName, props.volumeNumber, props.teamId], () => {
    loadChapter()
  })

  watch(currentPage, (newPage, oldPage) => {
    if (newPage === totalPages.value && oldPage < totalPages.value) {
      trackChapterCompletion()
    }

    if (settings.viewMode === 'single' && newPage !== oldPage && typeof window !== 'undefined') {
      // Only scroll to top if not restoring a position
      if (!isRestoringScroll.value) {
        scrollToTop()
      }
    }
  })

  watch(chapterData, (newChapter, oldChapter) => {
    if (newChapter && oldChapter && newChapter.id !== oldChapter.id) {
      if (typeof window !== 'undefined') {
        setTimeout(() => handleScrollBehavior(), 100)
      }
    }
  }, { immediate: false })

  // ===================================================================
  // LIFECYCLE
  // ===================================================================

  onMounted(() => {
    loadPreferences()
    loadChapter()

    setupScrollTracking()

    if (typeof document !== 'undefined') {
      document.addEventListener('keydown', handleKeydown)

      document.addEventListener('dblclick', (e) => {
        if (e.target.closest('button, select, .popup__content')) return

        if (!document.fullscreenElement) {
          document.documentElement.requestFullscreen().catch(console.error)
        } else {
          document.exitFullscreen()
        }
      })
    }

    // Add current location to history after loading
    nextTick(() => {
      addToNavigationHistory('initial')
    })
  })

  onUnmounted(() => {
    if (typeof document !== 'undefined') {
      document.removeEventListener('keydown', handleKeydown)
    }

    cleanupScrollTracking()
    saveScrollPosition()

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

    // Enhanced navigation with scroll memory
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
    updateUrl,
    scrollToTop,
    scrollToBottom,
    handleScrollBehavior,
    handleTapZoneClick,

    // Scroll memory utilities
    saveScrollPosition,
    restoreScrollPosition,
    getCurrentLocationKey,
    navigationHistory,
    scrollPositions
  }
}
