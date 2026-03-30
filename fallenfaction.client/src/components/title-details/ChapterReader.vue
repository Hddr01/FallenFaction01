<template>
  <div class="chapter-container" :class="[currentTheme]" id="chapterContainer">
    <!-- Navbar -->
    <div class="reader-navbar" :class="{ 'hidden': !uiVisible }" id="readerNavbar">
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
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m15 18-6-6 6-6" /></svg>
              </button>
              <h2 class="chapter-name">
                Vol.{{ chapterData?.volumeNumber }} Ch.{{ chapterData?.chapterNumber }}
                <span v-if="chapterData?.name">: {{ chapterData.name }}</span>
              </h2>
              <button class="chapter-nav-btn" @click="gotoNextChapter" :disabled="!chapterData?.nextChapterId">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m9 18 6-6-6-6" /></svg>
              </button>
            </div>
          </div>
        </div>
        <div class="navbar-right">
          <button v-if="isAuthenticated && chapterData?.id" class="icon-btn" @click="showReportModal = true" title="Report this chapter">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z" />A
              <line x1="4" y1="22" x2="4" y2="15" />
            </svg>
          </button>
          <button class="icon-btn" @click="toggleSettings">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
              <circle cx="12" cy="12" r="3" />
            </svg>
          </button>
          <button class="icon-btn" @click="toggleChapterList">
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

    <!-- Tap hint -->
    <transition name="hint-fade">
      <div v-if="currentHint" class="tap-hint">{{ currentHint }}</div>
    </transition>

    <!-- Loading -->
    <div v-if="loading" class="state-container">
      <div class="state-inner">
        <svg class="animate-spin h-8 w-8 text-[var(--color-accent)]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span>Loading chapter...</span>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="state-container">
      <div class="state-inner">
        <svg class="w-16 h-16 text-red-500 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3>Chapter Not Found</h3>
        <p class="opacity-75 mb-4">{{ error }}</p>
        <div class="flex gap-3 justify-center">
          <button @click="retryLoad" class="px-4 py-2 bg-[var(--color-accent)] text-white rounded-md">Try Again</button>
          <button @click="goToTitleDetails" class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md">Back to Title</button>
        </div>
      </div>
    </div>

    <!-- Chapter Text Content -->
    <div v-else-if="chapterData" class="reader-body">
      <div class="text-content-wrapper" :style="contentStyles">
        <!-- ============================================================ -->
        <!-- TAP ZONES OVERLAY — covers only text area, not buttons/comments -->
        <!-- ============================================================ -->
        <div v-if="!showSettings && !showChapterList"
             class="tap-zones-overlay"
             @touchstart.passive="onTouchStart"
             @touchend="onTouchEnd"
             @mousedown="onMouseDown"
             @mouseup="onMouseUp">
          <div class="tap-zone tap-zone-left" data-zone="prev"></div>
          <div class="tap-zone tap-zone-center" data-zone="toggle"></div>
          <div class="tap-zone tap-zone-right" data-zone="next"></div>
        </div>

        <!-- Chapter Header -->
        <div class="chapter-header">
          <p class="chapter-meta">Vol.{{ chapterData.volumeNumber }} · Ch.{{ chapterData.chapterNumber }}</p>
          <h2 class="chapter-title-display" v-if="chapterData.name">{{ chapterData.name }}</h2>
          <p class="chapter-team" v-if="chapterData.team?.name">Translated by {{ chapterData.team.name }}</p>
        </div>

        <hr class="chapter-divider" />

        <!-- Text Body -->
        <div v-if="chapterData.isAILocked" class="locked-gate">
          <div class="locked-gate-inner">
            <div class="locked-icon">
              <svg class="w-12 h-12 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                      d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
              </svg>
            </div>
            <h3 class="locked-title">AI Chapter Locked</h3>
            <p class="locked-sub">
              This AI-translated chapter hasn't been unlocked yet.<br />
              Once unlocked by anyone, it becomes free for everyone permanently.
            </p>

            <div v-if="unlockCost !== null" class="locked-cost">
              Unlock cost: <span class="cost-amount">{{ unlockCost }} tickets</span>
            </div>

            <div v-if="isAuthenticated" class="locked-actions">
              <button @click="handleUnlock" :disabled="unlocking || walletBalance < unlockCost"
                      class="unlock-btn" :class="{ 'disabled': unlocking || walletBalance < unlockCost }">
                <svg v-if="unlocking" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                {{ unlocking ? 'Unlocking…' : `Unlock for ${unlockCost} tickets` }}
              </button>

              <div v-if="walletBalance < unlockCost" class="balance-warning">
                Your balance ({{ walletBalance }}) is too low.
                <router-link to="/profile/wallet" class="wallet-link">Get tickets →</router-link>
              </div>
              <div v-else class="balance-info">
                Balance: <span class="gold">{{ wallet?.goldBalance ?? 0 }}G</span>
                + <span class="silver">{{ wallet?.silverBalance ?? 0 }}S</span>
              </div>

              <p v-if="unlockError" class="unlock-error">{{ unlockError }}</p>
            </div>

            <div v-else class="locked-actions">
              <router-link to="/account/login" class="unlock-btn">
                Login to unlock
              </router-link>
            </div>
          </div>
        </div>

        <div class="chapter-text" v-else-if="chapterData.content && chapterData.content.trim()" v-html="formattedContent"></div>
        <div v-else class="no-content">
          <p>This chapter has no content yet.</p>
          <button @click="goToTitleDetails" class="px-4 py-2 mt-4 bg-[var(--color-accent)] text-white rounded-md">Back to Title</button>
        </div>

        <!-- Bottom Navigation — flows naturally after text content -->
        <div class="bottom-nav">
          <button @click="gotoPrevChapter" :disabled="!chapterData.previousChapterId" class="nav-btn prev-btn">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path></svg>
            Previous Chapter
          </button>
          <button @click="goToTitleDetails" class="nav-btn back-btn">Back to Title</button>
          <button @click="gotoNextChapter" :disabled="!chapterData.nextChapterId" class="nav-btn next-btn">
            Next Chapter
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path></svg>
          </button>
        </div>

        <!-- Comments — flows naturally after navigation -->
        <div class="comments-section">
          <CommentsComponent v-if="chapterData.id"
                             :key="`chapter-comments-${chapterData.id}`"
                             :target-id="chapterData.id"
                             :target-type="2"
                             :is-authenticated="isAuthenticated"
                             :current-user-id="currentUserId"
                             :is-admin="isAdmin" />
        </div>
      </div>
    </div>

    <!-- Chapter List Popup -->
    <div v-if="showChapterList" class="popup" @click="handleBackdropClick">
      <div class="popup__content scrollable" @click.stop>
        <div class="popup__header">
          <h3>Chapter List</h3>
          <button class="close-btn" @click="toggleChapterList">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></svg>
          </button>
        </div>
        <div class="chapter-list">
          <div v-for="chapter in chaptersList" :key="chapter.id" class="chapter-item"
               :class="{ 'active': chapter.chapterNumber === chapterData?.chapterNumber }"
               @click="goToChapter(chapter)">
            <div class="chapter-title">
              Vol.{{ chapter.volumeNumber }} Ch.{{ chapter.chapterNumber }}
              <span v-if="chapter.name">: {{ chapter.name }}</span>
            </div>
            <div class="chapter-team-tag">{{ chapter.teamName }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Settings Popup -->
    <div v-if="showSettings" class="popup" @click="handleBackdropClick">
      <div class="popup__content" @click.stop>
        <div class="popup__header">
          <h3>Reading Settings</h3>
          <button class="close-btn" @click="toggleSettings">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></svg>
          </button>
        </div>
        <div class="settings-content">
          <!-- Theme -->
          <div class="settings-section">
            <div class="settings-label">Theme</div>
            <div class="settings-options">
              <button class="opt-btn" :class="{ active: currentTheme === 'dark' }" @click="setTheme('dark')">Dark</button>
              <button class="opt-btn" :class="{ active: currentTheme === 'light' }" @click="setTheme('light')">Light</button>
              <button class="opt-btn" :class="{ active: currentTheme === 'sepia' }" @click="setTheme('sepia')">Sepia</button>
            </div>
          </div>
          <!-- Font Size -->
          <div class="settings-section">
            <div class="settings-label">Font Size: {{ fontSize }}px</div>
            <div class="settings-slider">
              <input type="range" min="14" max="28" v-model="fontSize" @input="savePref('fontSize', fontSize)" class="slider" />
            </div>
          </div>
          <!-- Line Height -->
          <div class="settings-section">
            <div class="settings-label">Line Spacing: {{ lineHeight }}</div>
            <div class="settings-slider">
              <input type="range" min="1.4" max="2.4" step="0.1" v-model="lineHeight" @input="savePref('lineHeight', lineHeight)" class="slider" />
            </div>
          </div>
          <!-- Content Width -->
          <div class="settings-section">
            <div class="settings-label">Content Width: {{ contentWidth }}%</div>
            <div class="settings-slider">
              <input type="range" min="50" max="100" v-model="contentWidth" @input="savePref('contentWidth', contentWidth)" class="slider" />
            </div>
          </div>
          <!-- Font Family -->
          <div class="settings-section">
            <div class="settings-label">Font</div>
            <div class="settings-options">
              <button class="opt-btn" :class="{ active: fontFamily === 'serif' }" @click="setFont('serif')">Serif</button>
              <button class="opt-btn" :class="{ active: fontFamily === 'sans-serif' }" @click="setFont('sans-serif')">Sans</button>
              <button class="opt-btn" :class="{ active: fontFamily === 'monospace' }" @click="setFont('monospace')">Mono</button>
            </div>
          </div>
          <!-- Tap Zones Toggle -->
          <div class="settings-section toggle-section">
            <div class="settings-label">Tap Zones</div>
            <div class="toggle-switch">
              <input type="checkbox" id="tapZonesEnabled" v-model="tapZonesEnabled" @change="savePref('tapZonesEnabled', tapZonesEnabled)" />
              <label for="tapZonesEnabled"></label>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <!-- Report Chapter Modal -->
  <ReportModal v-if="chapterData?.id && isAuthenticated"
               :is-open="showReportModal"
               :target-type="3"
               :target-id="chapterData.id"
               @close="showReportModal = false"
               @reported="showReportModal = false" />
</template>

<script setup>
  import { ref, computed, onMounted, onUnmounted } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { titleDetailsService } from '../../services/titleDetailsService'
  import { chapterService } from '../../services/chapterService'
  import { getWallet, getUnlockCost, unlockChapter } from '@/services/aiTranslationService'
  import CommentsComponent from '../title-details/CommentsComponent.vue'
  import ReportModal from '../shared/ReportModal.vue'
  // buildTitleSlug import removed — URLs now use props.titleSlug directly

  const props = defineProps({
    titleSlug: { type: String, required: true },  // "title-name-{id}" format
    chapterName: { type: String, required: true },
    volumeNumber: { type: [Number, String], required: true },
    teamId: { type: [Number, String], required: true }
  })

  const route = useRoute()
  const router = useRouter()

  // State
  const loading = ref(true)
  const error = ref('')
  const chapterData = ref(null)
  const chaptersList = ref([])
  const isAuthenticated = ref(false)
  const showReportModal = ref(false)
  const currentUserId = ref('')
  const isAdmin = ref(false)

  // ── AI Chapter unlock state ──────────────────────────────────────────
  const wallet = ref(null)
  const unlockCost = ref(null)
  const unlocking = ref(false)
  const unlockError = ref('')
  const walletBalance = computed(() =>
    (wallet.value?.goldBalance ?? 0) + (wallet.value?.silverBalance ?? 0)
  )

  // UI State
  const uiVisible = ref(true)
  const showSettings = ref(false)
  const showChapterList = ref(false)
  const currentHint = ref('')
  const tapZonesEnabled = ref(true)

  // Reading preferences
  const currentTheme = ref('dark')
  const fontSize = ref(18)
  const lineHeight = ref(1.8)
  const contentWidth = ref(75)
  const fontFamily = ref('serif')

  // =============================================
  // ZOOM PREVENTION
  // Disables pinch-zoom while reader is mounted,
  // restores the original meta content on unmount.
  // =============================================
  let originalViewportContent = ''

  const disableZoom = () => {
    let meta = document.querySelector('meta[name="viewport"]')
    if (!meta) {
      meta = document.createElement('meta')
      meta.name = 'viewport'
      document.head.appendChild(meta)
    }
    originalViewportContent = meta.getAttribute('content') || ''
    meta.setAttribute(
      'content',
      'width=device-width, initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0, user-scalable=no'
    )
  }

  const restoreZoom = () => {
    const meta = document.querySelector('meta[name="viewport"]')
    if (meta && originalViewportContent) {
      meta.setAttribute('content', originalViewportContent)
    } else if (meta) {
      meta.setAttribute('content', 'width=device-width, initial-scale=1.0')
    }
  }

  // =============================================
  // TAP ZONE LOGIC
  // Detects short taps vs. scrolls.
  // Touch: track start coords; fire only if
  //   movement < TAP_THRESHOLD and duration < TAP_MAX_MS.
  // Mouse: similarly track mousedown/mouseup.
  // =============================================
  const TAP_THRESHOLD = 12   // px — max movement to count as a tap
  const TAP_MAX_MS = 400   // ms — max duration to count as a tap

  let touchStartX = 0
  let touchStartY = 0
  let touchStartTime = 0
  let mouseStartX = 0
  let mouseStartY = 0
  let mouseStartTime = 0

  const fireTapAction = (clientX) => {
    if (!tapZonesEnabled.value) return
    if (showSettings.value || showChapterList.value) return

    const w = window.innerWidth
    const third = w / 3

    if (clientX < third) {
      // Left — previous chapter
      showHint('← Previous chapter')
      gotoPrevChapter()
    } else if (clientX > w - third) {
      // Right — next chapter
      showHint('Next chapter →')
      gotoNextChapter()
    } else {
      // Center — toggle navbar
      toggleUI()
    }
  }

  const onTouchStart = (e) => {
    if (e.touches.length !== 1) return
    touchStartX = e.touches[0].clientX
    touchStartY = e.touches[0].clientY
    touchStartTime = Date.now()
  }

  const onTouchEnd = (e) => {
    if (e.changedTouches.length !== 1) return
    const dx = Math.abs(e.changedTouches[0].clientX - touchStartX)
    const dy = Math.abs(e.changedTouches[0].clientY - touchStartY)
    const dt = Date.now() - touchStartTime

    if (dx < TAP_THRESHOLD && dy < TAP_THRESHOLD && dt < TAP_MAX_MS) {
      e.preventDefault()
      fireTapAction(touchStartX)
    }
  }

  const onMouseDown = (e) => {
    mouseStartX = e.clientX
    mouseStartY = e.clientY
    mouseStartTime = Date.now()
  }

  const onMouseUp = (e) => {
    const dx = Math.abs(e.clientX - mouseStartX)
    const dy = Math.abs(e.clientY - mouseStartY)
    const dt = Date.now() - mouseStartTime

    if (dx < TAP_THRESHOLD && dy < TAP_THRESHOLD && dt < TAP_MAX_MS) {
      fireTapAction(mouseStartX)
    }
  }

  // =============================================
  // HINT
  // =============================================
  let hintTimer = null

  const showHint = (msg) => {
    currentHint.value = msg
    clearTimeout(hintTimer)
    hintTimer = setTimeout(() => { currentHint.value = '' }, 1600)
  }

  // =============================================
  // AUTH
  // =============================================
  const checkAuthStatus = () => {
    try {
      const token = localStorage.getItem('authToken')
      const user = localStorage.getItem('authUser')
      if (token && user) {
        const userData = JSON.parse(user)
        isAuthenticated.value = true
        currentUserId.value = userData.id || userData.userId || ''
        isAdmin.value = userData.role === 'Admin' || userData.roles?.includes('Admin') || false
      }
    } catch {
      isAuthenticated.value = false
    }
  }

  // =============================================
  // COMPUTED
  // =============================================
  const contentStyles = computed(() => ({
    fontSize: `${fontSize.value}px`,
    lineHeight: `${lineHeight.value}`,
    fontFamily: fontFamily.value,
    maxWidth: `${contentWidth.value}%`,
  }))

  const formattedContent = computed(() => {
    if (!chapterData.value?.content) return ''
    return chapterData.value.content
      .split(/\n\n+/)
      .filter(p => p.trim())
      .map(p => `<p>${p.replace(/\n/g, '<br>')}</p>`)
      .join('')
  })

  // =============================================
  // DATA LOADING
  // =============================================
  const loadChapter = async () => {
    try {
      loading.value = true
      error.value = ''

      // titleSlug is "title-name-{id}"; pass it as the "titleName" param —
      // the backend now resolves both slug and plain-name formats.
      const result = await titleDetailsService.getChapterByRoute(
        props.titleSlug,
        props.chapterName,
        props.volumeNumber,
        props.teamId
      )

      if (result.success && result.data) {
        chapterData.value = result.data
        document.title = `${result.data.titleName} - ${result.data.name || `Ch.${result.data.chapterNumber}`}`
        await loadChaptersList()
        await updateReadingProgress()
        await loadUnlockData()
        window.scrollTo({ top: 0, behavior: 'auto' })
      } else {
        error.value = result.error || 'Chapter not found'
      }
    } catch (err) {
      error.value = err.message || 'Failed to load chapter'
    } finally {
      loading.value = false
    }
  }

  const loadChaptersList = async () => {
    if (!chapterData.value?.titleId) return
    try {
      const result = await titleDetailsService.getChapters(chapterData.value.titleId)
      if (result.success) chaptersList.value = result.data || []
    } catch { /* non-critical */ }
  }

  const updateReadingProgress = async () => {
    if (!chapterData.value) return
    try {
      await chapterService.updateReadingProgress(
        chapterData.value.titleId,
        chapterData.value.chapterNumber
      )
    } catch { /* non-critical */ }
  }

  const retryLoad = () => loadChapter()

  // ── AI unlock helpers ────────────────────────────────────────────────
  const loadUnlockData = async () => {
    if (!chapterData.value?.isAILocked) return
    try {
      const [walletRes, costRes] = await Promise.all([
        getWallet(),
        getUnlockCost(chapterData.value.id)
      ])
      wallet.value = walletRes.data
      unlockCost.value = costRes.data.cost
    } catch { }
  }

  const handleUnlock = async () => {
    if (unlocking.value || !chapterData.value) return
    unlocking.value = true
    unlockError.value = ''
    try {
      const res = await unlockChapter(chapterData.value.id)
      if (res.data.success) {
        // Reload the chapter — it's now unlocked
        await loadChapter()
        wallet.value = { goldBalance: res.data.newGoldBalance, silverBalance: res.data.newSilverBalance }
      } else {
        unlockError.value = res.data.message ?? 'Unlock failed.'
      }
    } catch (e) {
      unlockError.value = e.response?.data ?? 'Insufficient tickets.'
    } finally {
      unlocking.value = false
    }
  }

  // =============================================
  // NAVIGATION
  // =============================================
  const goToTitleDetails = () => {
    // Use props.titleSlug directly — never try to rebuild the slug from
    // chapterData.titleName, which is empty when the title's OriginalTitle
    // field is "" on the backend (C# ?? won't fall through on an empty string).
    router.push({ path: `/${props.titleSlug}`, query: { section: 'chapters' } })
  }

  // Chapter URLs: /{titleSlug}/chapter/{chapterSeg}/v{vol}/t{teamId}
  // When a chapter has no name the API resolves by chapterNumber instead.
  // Produces an encoded segment: name (if non-empty) OR stringified number.
  const chapterPathSegment = (name, chapterNumber) => {
    const trimmed = name != null && String(name).trim() !== '' ? String(name).trim() : ''
    if (trimmed) return encodeURIComponent(trimmed)
    // Numeric fallback — Number(0) is valid, guard only against null/undefined/NaN.
    const num = Number(chapterNumber)
    if (chapterNumber != null && !Number.isNaN(num)) return encodeURIComponent(String(num))
    return encodeURIComponent('0')
  }

  const buildChapterUrl = (chapterName, volume, teamId, chapterNumber) => {
    // Always use the slug from props — it is the canonical "name-{id}" value
    // set by the router and is immune to an empty titleName in the chapter DTO.
    const seg = chapterPathSegment(chapterName, chapterNumber)
    const vol = (volume != null && !Number.isNaN(Number(volume))) ? Number(volume) : 1
    const team = (teamId != null && !Number.isNaN(Number(teamId))) ? Number(teamId) : 0
    return `/${props.titleSlug}/chapter/${seg}/v${vol}/t${team}`
  }

  const gotoNextChapter = () => {
    if (!chapterData.value?.nextChapterId) { goToTitleDetails(); return }
    router.push(buildChapterUrl(
      chapterData.value.nextChapterName,
      chapterData.value.nextChapterVolume,
      chapterData.value.nextChapterTeamId,
      chapterData.value.nextChapterNumber
    ))
  }

  const gotoPrevChapter = () => {
    if (!chapterData.value?.previousChapterId) { goToTitleDetails(); return }
    router.push(buildChapterUrl(
      chapterData.value.previousChapterName,
      chapterData.value.previousChapterVolume,
      chapterData.value.previousChapterTeamId,
      chapterData.value.previousChapterNumber
    ))
  }

  const goToChapter = (chapter) => {
    router.push(buildChapterUrl(
      chapter.name,
      chapter.volumeNumber,
      chapter.teamId,
      chapter.chapterNumber
    ))
    toggleChapterList()
  }

  // =============================================
  // UI
  // =============================================
  const toggleUI = () => {
    uiVisible.value = !uiVisible.value
    savePref('uiVisible', uiVisible.value)
  }

  const toggleSettings = () => { showSettings.value = !showSettings.value; showChapterList.value = false }
  const toggleChapterList = () => { showChapterList.value = !showChapterList.value; showSettings.value = false }

  const handleBackdropClick = (e) => {
    if (e.target === e.currentTarget) { showSettings.value = false; showChapterList.value = false }
  }

  const setTheme = (t) => { currentTheme.value = t; savePref('theme', t) }
  const setFont = (f) => { fontFamily.value = f; savePref('fontFamily', f) }

  const savePref = (key, value) => localStorage.setItem(`novelReader_${key}`, JSON.stringify(value))
  const loadPref = (key, def) => {
    try {
      const v = localStorage.getItem(`novelReader_${key}`)
      return v !== null ? JSON.parse(v) : def
    } catch { return def }
  }

  const loadPreferences = () => {
    currentTheme.value = loadPref('theme', 'dark')
    fontSize.value = loadPref('fontSize', 18)
    lineHeight.value = loadPref('lineHeight', 1.8)
    contentWidth.value = loadPref('contentWidth', 75)
    fontFamily.value = loadPref('fontFamily', 'serif')
    uiVisible.value = loadPref('uiVisible', true)
    tapZonesEnabled.value = loadPref('tapZonesEnabled', true)
  }

  // =============================================
  // KEYBOARD
  // =============================================
  const handleKeydown = (e) => {
    if (showSettings.value || showChapterList.value) {
      if (e.key === 'Escape') { showSettings.value = false; showChapterList.value = false }
      return
    }
    if (e.key === 'ArrowLeft') { e.preventDefault(); gotoPrevChapter() }
    if (e.key === 'ArrowRight') { e.preventDefault(); gotoNextChapter() }
    if (e.key === 'Escape') { e.preventDefault(); toggleUI() }
  }

  // =============================================
  // LIFECYCLE
  // =============================================
  onMounted(async () => {
    disableZoom()
    checkAuthStatus()
    loadPreferences()
    await loadChapter()
    document.addEventListener('keydown', handleKeydown)
  })

  onUnmounted(() => {
    restoreZoom()
    document.removeEventListener('keydown', handleKeydown)
    clearTimeout(hintTimer)
  })
</script>

<style scoped>
  /* =========================================================
   Base container
   ========================================================= */
  .chapter-container {
    min-height: 100vh;
    background-color: var(--color-background);
    color: var(--color-text);
    /* Prevent rubber-band zoom on iOS while allowing vertical scroll */
    touch-action: pan-y;
    overscroll-behavior: none;
  }

    .chapter-container.dark {
      --reader-bg: #111;
      --reader-text: #e8e8e8;
      --reader-navbar: rgba(0,0,0,0.95);
      --reader-border: #2a2a2a;
    }

    .chapter-container.light {
      --reader-bg: #fafafa;
      --reader-text: var(--color-text);
      --reader-navbar: rgba(255,255,255,0.95);
      --reader-border: #ddd;
    }

    .chapter-container.sepia {
      --reader-bg: #f4ecd8;
      --reader-text: #3b2f2f;
      --reader-navbar: rgba(244,236,216,0.95);
      --reader-border: #c9b99a;
    }

  /* =========================================================
   TAP ZONES OVERLAY
   Positioned absolutely within text-content-wrapper.
   Only covers the text area, NOT buttons or comments below.
   Pointer-events only on the overlay itself; the text below
   is still selectable because the overlay has no background.
   ========================================================= */
  .tap-zones-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: auto;
    z-index: 500;
    display: grid;
    grid-template-columns: 30% 40% 30%;
    pointer-events: auto;
    /* Transparent — only catches taps, never blocks visual content */
    background: transparent;
    /* Will dynamically size based on text content */
  }

  .tap-zone {
    /* Purely structural; actions fire on the parent overlay */
    pointer-events: none;
  }

  /* =========================================================
   Tap hint
   ========================================================= */
  .tap-hint {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: rgba(0, 0, 0, 0.75);
    color: #fff;
    padding: 0.6rem 1.4rem;
    border-radius: 2rem;
    font-size: 0.9rem;
    font-weight: 500;
    z-index: 1200;
    pointer-events: none;
    white-space: nowrap;
  }

  .hint-fade-enter-active,
  .hint-fade-leave-active {
    transition: opacity 0.25s ease;
  }

  .hint-fade-enter-from,
  .hint-fade-leave-to {
    opacity: 0;
  }

  /* =========================================================
   Navbar
   ========================================================= */
  .reader-navbar {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 1000;
    background: var(--reader-navbar);
    backdrop-filter: blur(10px);
    border-bottom: 1px solid var(--reader-border);
    transition: transform 0.3s ease;
  }

    .reader-navbar.hidden {
      transform: translateY(-100%);
    }

  .navbar-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    max-width: 1200px;
    margin: 0 auto;
    min-height: 60px;
  }

  .navbar-left {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .navbar-right {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .back-button {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    background: transparent;
    border: 1px solid var(--reader-border);
    border-radius: 0.375rem;
    color: var(--reader-text);
    cursor: pointer;
    transition: all 0.2s;
    /* Make sure the button itself is above the tap overlay */
    position: relative;
    z-index: 1001;
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
    font-size: 1.1rem;
    font-weight: 600;
    cursor: pointer;
    margin: 0;
    color: var(--reader-text);
    position: relative;
    z-index: 1001;
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
    color: var(--reader-text);
    cursor: pointer;
    border-radius: 0.25rem;
    position: relative;
    z-index: 1001;
  }

    .chapter-nav-btn:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }

  .chapter-name {
    font-size: 0.95rem;
    font-weight: 500;
    margin: 0;
    color: var(--reader-text);
  }

  .icon-btn {
    padding: 0.5rem;
    background: transparent;
    border: 1px solid var(--reader-border);
    border-radius: 0.375rem;
    color: var(--reader-text);
    cursor: pointer;
    transition: all 0.2s;
    position: relative;
    z-index: 1001;
  }

    .icon-btn:hover {
      background: var(--color-background-mute);
    }

  /* =========================================================
   States
   ========================================================= */
  .state-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 2rem;
  }

  .state-inner {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    gap: 0.75rem;
    max-width: 400px;
    color: var(--reader-text);
  }

  /* =========================================================
   Reader body
   ========================================================= */
  .reader-body {
    padding-top: 80px;
    padding-bottom: 4rem;
    min-height: 100vh;
    background-color: var(--reader-bg);
    /* Text is above the tap overlay so it stays visible & selectable */
    position: relative;
    z-index: 0;
  }

  .text-content-wrapper {
    margin: 0 auto;
    padding: 2rem 1.5rem;
    color: var(--reader-text);
    position: relative;
    /* Allows tap-zones-overlay to be positioned absolutely within this element */
  }

  /* Chapter header */
  .chapter-header {
    text-align: center;
    margin-bottom: 1.5rem;
  }

  .chapter-meta {
    font-size: 0.85rem;
    opacity: 0.6;
    margin: 0 0 0.5rem;
  }

  .chapter-title-display {
    font-size: 1.6rem;
    font-weight: 700;
    margin: 0 0 0.5rem;
  }

  .chapter-team {
    font-size: 0.85rem;
    opacity: 0.55;
    margin: 0;
  }

  .chapter-divider {
    border: none;
    border-top: 1px solid var(--reader-border);
    margin: 1.5rem 0 2rem;
  }

  /* Chapter text — pointer-events: auto ensures text selection still works */
  .chapter-text {
    pointer-events: auto;
    position: relative;
    z-index: 501; /* above the tap overlay so selection works */
    user-select: text;
    -webkit-user-select: text;
  }

    .chapter-text :deep(p) {
      margin: 0 0 1.5em;
      text-indent: 2em;
    }

    .chapter-text :deep(p:first-child) {
      text-indent: 0;
    }

  /* ── AI Lock Gate ─────────────────────────── */
  .locked-gate {
    display: flex;
    justify-content: center;
    padding: 4rem 1rem;
  }

  .locked-gate-inner {
    max-width: 440px;
    width: 100%;
    text-align: center;
    padding: 2.5rem 2rem;
    border-radius: 1.25rem;
    background: rgba(139, 92, 246, 0.05);
    border: 1px solid rgba(139, 92, 246, 0.2);
  }

  .locked-icon {
    display: flex;
    justify-content: center;
    margin-bottom: 1rem;
  }

  .locked-title {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--color-heading);
    margin-bottom: 0.5rem;
  }

  .locked-sub {
    font-size: 0.875rem;
    opacity: 0.6;
    line-height: 1.6;
    margin-bottom: 1.25rem;
    color: var(--color-text);
  }

  .locked-cost {
    font-size: 0.9rem;
    color: var(--color-text);
    opacity: 0.7;
    margin-bottom: 1.25rem;
  }

  .cost-amount {
    font-weight: 700;
    color: #a78bfa;
  }

  .locked-actions {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
  }

  .unlock-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
    padding: 0.75rem 1.75rem;
    border-radius: 0.75rem;
    background: rgba(139, 92, 246, 0.2);
    border: 1px solid rgba(139, 92, 246, 0.4);
    color: #c4b5fd;
    font-weight: 600;
    font-size: 0.9rem;
    cursor: pointer;
    transition: all 0.15s;
    text-decoration: none;
  }

    .unlock-btn:hover:not(.disabled) {
      background: rgba(139, 92, 246, 0.35);
      color: #ede9fe;
    }

    .unlock-btn.disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }

  .balance-info {
    font-size: 0.8rem;
    opacity: 0.55;
    color: var(--color-text);
  }

  .balance-warning {
    font-size: 0.8rem;
    color: #f87171;
  }

  .wallet-link {
    color: #a78bfa;
    text-decoration: underline;
    margin-left: 0.25rem;
  }

  .gold {
    color: #facc15;
    font-weight: 600;
  }

  .silver {
    color: #cbd5e1;
    font-weight: 600;
  }

  .unlock-error {
    color: #f87171;
    font-size: 0.8rem;
  }

  .no-content {
    text-align: center;
    padding: 4rem 0;
    opacity: 0.6;
  }

  /* Bottom nav — flows naturally after text content */
  .bottom-nav {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    margin: 3rem 0 2rem;
    flex-wrap: wrap;
    width: 100%;
  }

  .nav-btn {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1.5rem;
    border-radius: 0.5rem;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;
    border: 1px solid var(--reader-border);
    background: var(--color-background-mute);
    color: var(--reader-text);
  }

    .nav-btn:hover:not(:disabled) {
      border-color: var(--color-accent);
    }

    .nav-btn:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }

  .back-btn {
    background: var(--color-accent);
    color: #fff;
    border-color: var(--color-accent);
  }

    .back-btn:hover {
      background: var(--color-accent-hover);
    }

  /* Comments section — flows naturally after navigation */
  .comments-section {
    margin-top: 3rem;
    border-top: 1px solid var(--reader-border);
    padding-top: 2rem;
  }

  /* =========================================================
   Popups — always on top
   ========================================================= */
  .popup {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0,0,0,0.7);
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
    max-width: 480px;
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
    padding: 1.25rem 1.5rem;
    border-bottom: 1px solid var(--color-border);
  }

    .popup__header h3 {
      margin: 0;
      font-size: 1.2rem;
      font-weight: 600;
      color: var(--color-text);
    }

  .close-btn {
    padding: 0.4rem;
    background: transparent;
    border: none;
    color: var(--color-text);
    cursor: pointer;
    border-radius: 0.25rem;
  }

    .close-btn:hover {
      background: var(--color-background-mute);
    }

  /* Chapter list */
  .chapter-list {
    padding: 0;
  }

  .chapter-item {
    padding: 0.9rem 1.5rem;
    cursor: pointer;
    border-bottom: 1px solid var(--color-border);
    transition: background 0.15s;
  }

    .chapter-item:hover {
      background: var(--color-background-mute);
    }

    .chapter-item.active {
      background: var(--color-accent);
      color: #fff;
    }

    .chapter-item:last-child {
      border-bottom: none;
    }

  .chapter-title {
    font-weight: 500;
    font-size: 0.9rem;
  }

  .chapter-team-tag {
    font-size: 0.8rem;
    opacity: 0.65;
    margin-top: 0.2rem;
  }

  /* Settings */
  .settings-content {
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
  }

  .settings-section {
    display: flex;
    flex-direction: column;
    gap: 0.6rem;
  }

    .settings-section.toggle-section {
      flex-direction: row;
      justify-content: space-between;
      align-items: center;
    }

  .settings-label {
    font-weight: 500;
    font-size: 0.9rem;
    color: var(--color-text);
  }

  .settings-options {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
  }

  .opt-btn {
    padding: 0.45rem 1rem;
    background: var(--color-background-mute);
    border: 1px solid var(--color-border);
    border-radius: 0.4rem;
    color: var(--color-text);
    cursor: pointer;
    font-size: 0.875rem;
    transition: all 0.2s;
  }

    .opt-btn:hover {
      border-color: var(--color-accent);
    }

    .opt-btn.active {
      background: var(--color-accent);
      color: #fff;
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

  /* Toggle switch */
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
      transition: all 0.3s;
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
        transition: all 0.3s;
      }

    .toggle-switch input:checked + label {
      background: var(--color-accent);
      border-color: var(--color-accent);
    }

      .toggle-switch input:checked + label::before {
        transform: translateX(20px);
      }

  /* =========================================================
   Mobile
   ========================================================= */
  @media (max-width: 768px) {
    .back-text {
      display: none;
    }

    .text-content-wrapper {
      max-width: 100% !important;
      padding: 1rem;
    }

    .bottom-nav {
      flex-direction: column;
      gap: 0.75rem;
      margin: 2rem 0 1.5rem;
    }

    .nav-btn {
      width: 100%;
      justify-content: center;
    }

    .comments-section {
      margin-top: 2rem;
      padding-top: 1.5rem;
    }
  }
</style>


