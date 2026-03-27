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
          <button class="icon-btn" @click="toggleSettings">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" /><circle cx="12" cy="12" r="3" />
            </svg>
          </button>
          <button class="icon-btn" @click="toggleChapterList">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/>
              <line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/>
            </svg>
          </button>
        </div>
      </div>
    </div>

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
        <!-- Chapter Header -->
        <div class="chapter-header">
          <p class="chapter-meta">Vol.{{ chapterData.volumeNumber }} · Ch.{{ chapterData.chapterNumber }}</p>
          <h2 class="chapter-title-display" v-if="chapterData.name">{{ chapterData.name }}</h2>
          <p class="chapter-team" v-if="chapterData.team?.name">Translated by {{ chapterData.team.name }}</p>
        </div>

        <hr class="chapter-divider" />

        <!-- Text Body -->
        <div class="chapter-text" v-if="chapterData.content && chapterData.content.trim()" v-html="formattedContent"></div>
        <div v-else class="no-content">
          <p>This chapter has no content yet.</p>
          <button @click="goToTitleDetails" class="px-4 py-2 mt-4 bg-[var(--color-accent)] text-white rounded-md">Back to Title</button>
        </div>

        <!-- Bottom Navigation -->
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

        <!-- Comments -->
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
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
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
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
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
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { titleDetailsService } from '../../services/titleDetailsService'
import { chapterService } from '../../services/chapterService'
import CommentsComponent from '../title-details/CommentsComponent.vue'

const props = defineProps({
  titleName: { type: String, required: true },
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
const currentUserId = ref('')
const isAdmin = ref(false)

// UI State
const uiVisible = ref(true)
const showSettings = ref(false)
const showChapterList = ref(false)

// Reading preferences
const currentTheme = ref('dark')
const fontSize = ref(18)
const lineHeight = ref(1.8)
const contentWidth = ref(75)
const fontFamily = ref('serif')

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
  } catch (err) {
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
  // Preserve paragraph breaks — convert double newlines to <p> tags
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

    const result = await titleDetailsService.getChapterByRoute(
      props.titleName,
      props.chapterName,
      props.volumeNumber,
      props.teamId
    )

    if (result.success && result.data) {
      chapterData.value = result.data
      document.title = `${result.data.titleName} - ${result.data.name || `Ch.${result.data.chapterNumber}`}`
      await loadChaptersList()
      await updateReadingProgress()
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
  } catch (err) { /* non-critical */ }
}

const updateReadingProgress = async () => {
  if (!chapterData.value) return
  try {
    await chapterService.updateReadingProgress(
      chapterData.value.titleId,
      chapterData.value.chapterNumber
    )
  } catch (err) { /* non-critical */ }
}

const retryLoad = () => loadChapter()

// =============================================
// NAVIGATION
// =============================================
const goToTitleDetails = () => {
  const titleName = (chapterData.value?.titleName || props.titleName).trim()
  router.push({ path: `/${encodeURIComponent(titleName)}`, query: { section: 'chapters' } })
}

const buildChapterUrl = (titleName, chapterName, volume, teamId) => {
  return `/${encodeURIComponent(titleName.trim())}/chapter/${encodeURIComponent(chapterName)}/v${volume}/t${teamId}`
}

const gotoNextChapter = () => {
  if (!chapterData.value?.nextChapterId) { goToTitleDetails(); return }
  const url = buildChapterUrl(
    chapterData.value.titleName,
    chapterData.value.nextChapterName,
    chapterData.value.nextChapterVolume,
    chapterData.value.nextChapterTeamId
  )
  router.push(url)
}

const gotoPrevChapter = () => {
  if (!chapterData.value?.previousChapterId) { goToTitleDetails(); return }
  const url = buildChapterUrl(
    chapterData.value.titleName,
    chapterData.value.previousChapterName,
    chapterData.value.previousChapterVolume,
    chapterData.value.previousChapterTeamId
  )
  router.push(url)
}

const goToChapter = (chapter) => {
  const url = buildChapterUrl(
    chapterData.value.titleName.trim(),
    chapter.name || chapter.chapterNumber,
    chapter.volumeNumber,
    chapter.teamId
  )
  router.push(url)
  toggleChapterList()
}

// =============================================
// UI
// =============================================
const toggleSettings = () => { showSettings.value = !showSettings.value; showChapterList.value = false }
const toggleChapterList = () => { showChapterList.value = !showChapterList.value; showSettings.value = false }
const handleBackdropClick = (e) => {
  if (e.target === e.currentTarget) { showSettings.value = false; showChapterList.value = false }
}

const setTheme = (t) => { currentTheme.value = t; savePref('theme', t) }
const setFont = (f) => { fontFamily.value = f; savePref('fontFamily', f) }

const savePref = (key, value) => {
  localStorage.setItem(`novelReader_${key}`, JSON.stringify(value))
}
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
}

const handleKeydown = (e) => {
  if (showSettings.value || showChapterList.value) {
    if (e.key === 'Escape') { showSettings.value = false; showChapterList.value = false }
    return
  }
  if (e.key === 'ArrowLeft') { e.preventDefault(); gotoPrevChapter() }
  if (e.key === 'ArrowRight') { e.preventDefault(); gotoNextChapter() }
}

// =============================================
// LIFECYCLE
// =============================================
onMounted(async () => {
  checkAuthStatus()
  loadPreferences()
  await loadChapter()
  document.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<style scoped>
.chapter-container {
  min-height: 100vh;
  background-color: var(--color-background);
  color: var(--color-text);
}

.chapter-container.dark {
  --reader-bg: #111;
  --reader-text: #e8e8e8;
  --reader-navbar: rgba(0,0,0,0.95);
  --reader-border: #2a2a2a;
}
.chapter-container.light {
  --reader-bg: #fafafa;
  --reader-text: #1a1a1a;
  --reader-navbar: rgba(255,255,255,0.95);
  --reader-border: #ddd;
}
.chapter-container.sepia {
  --reader-bg: #f4ecd8;
  --reader-text: #3b2f2f;
  --reader-navbar: rgba(244,236,216,0.95);
  --reader-border: #c9b99a;
}

/* Navbar */
.reader-navbar {
  position: fixed;
  top: 0; left: 0; right: 0;
  z-index: 1000;
  background: var(--reader-navbar);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--reader-border);
  transition: transform 0.3s ease;
}
.reader-navbar.hidden { transform: translateY(-100%); }

.navbar-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem 1rem;
  max-width: 1200px;
  margin: 0 auto;
  min-height: 60px;
}
.navbar-left { display: flex; align-items: center; gap: 1rem; }
.navbar-right { display: flex; align-items: center; gap: 0.5rem; }

.back-button {
  display: flex; align-items: center; gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: transparent;
  border: 1px solid var(--reader-border);
  border-radius: 0.375rem;
  color: var(--reader-text);
  cursor: pointer;
  transition: all 0.2s;
}
.back-button:hover { background: var(--color-background-mute); }

.chapter-info { display: flex; flex-direction: column; gap: 0.25rem; }
.title-name { font-size: 1.1rem; font-weight: 600; cursor: pointer; margin: 0; color: var(--reader-text); }
.chapter-nav { display: flex; align-items: center; gap: 0.5rem; }
.chapter-nav-btn {
  padding: 0.25rem; background: transparent; border: none;
  color: var(--reader-text); cursor: pointer; border-radius: 0.25rem;
}
.chapter-nav-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.chapter-name { font-size: 0.95rem; font-weight: 500; margin: 0; color: var(--reader-text); }

.icon-btn {
  padding: 0.5rem;
  background: transparent;
  border: 1px solid var(--reader-border);
  border-radius: 0.375rem;
  color: var(--reader-text);
  cursor: pointer;
  transition: all 0.2s;
}
.icon-btn:hover { background: var(--color-background-mute); }

/* States */
.state-container {
  display: flex; justify-content: center; align-items: center;
  min-height: 100vh; padding: 2rem;
}
.state-inner {
  display: flex; flex-direction: column;
  align-items: center; text-align: center; gap: 0.75rem;
  max-width: 400px; color: var(--reader-text);
}

/* Reader body */
.reader-body {
  padding-top: 80px;
  padding-bottom: 4rem;
  min-height: 100vh;
  background-color: var(--reader-bg);
}
.text-content-wrapper {
  margin: 0 auto;
  padding: 2rem 1.5rem;
  color: var(--reader-text);
}

/* Chapter header */
.chapter-header { text-align: center; margin-bottom: 1.5rem; }
.chapter-meta { font-size: 0.85rem; opacity: 0.6; margin: 0 0 0.5rem; }
.chapter-title-display { font-size: 1.6rem; font-weight: 700; margin: 0 0 0.5rem; }
.chapter-team { font-size: 0.85rem; opacity: 0.55; margin: 0; }
.chapter-divider { border: none; border-top: 1px solid var(--reader-border); margin: 1.5rem 0 2rem; }

/* Chapter text */
.chapter-text :deep(p) {
  margin: 0 0 1.5em;
  text-indent: 2em;
}
.chapter-text :deep(p:first-child) { text-indent: 0; }

.no-content {
  text-align: center; padding: 4rem 0; opacity: 0.6;
}

/* Bottom nav */
.bottom-nav {
  display: flex; justify-content: space-between; align-items: center;
  gap: 1rem; margin: 3rem 0 2rem; flex-wrap: wrap;
}
.nav-btn {
  display: flex; align-items: center; gap: 0.5rem;
  padding: 0.75rem 1.5rem; border-radius: 0.5rem;
  font-weight: 500; cursor: pointer; transition: all 0.2s;
  border: 1px solid var(--reader-border);
  background: var(--color-background-mute);
  color: var(--reader-text);
}
.nav-btn:hover:not(:disabled) { border-color: var(--color-accent); }
.nav-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.back-btn { background: var(--color-accent); color: #fff; border-color: var(--color-accent); }
.back-btn:hover { background: var(--color-accent-hover); }

/* Comments */
.comments-section { margin-top: 3rem; border-top: 1px solid var(--reader-border); padding-top: 2rem; }

/* Popups */
.popup {
  position: fixed; top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.7); backdrop-filter: blur(4px);
  z-index: 2000; display: flex; justify-content: center;
  align-items: center; padding: 1rem;
}
.popup__content {
  background: var(--color-background-soft);
  border: 1px solid var(--color-border);
  border-radius: 1rem;
  max-width: 480px; width: 100%; max-height: 80vh;
  display: flex; flex-direction: column;
}
.popup__content.scrollable { overflow-y: auto; }
.popup__header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 1.25rem 1.5rem; border-bottom: 1px solid var(--color-border);
}
.popup__header h3 { margin: 0; font-size: 1.2rem; font-weight: 600; color: var(--color-text); }
.close-btn { padding: 0.4rem; background: transparent; border: none; color: var(--color-text); cursor: pointer; border-radius: 0.25rem; }
.close-btn:hover { background: var(--color-background-mute); }

/* Chapter list */
.chapter-list { padding: 0; }
.chapter-item {
  padding: 0.9rem 1.5rem; cursor: pointer;
  border-bottom: 1px solid var(--color-border);
  transition: background 0.15s;
}
.chapter-item:hover { background: var(--color-background-mute); }
.chapter-item.active { background: var(--color-accent); color: #fff; }
.chapter-item:last-child { border-bottom: none; }
.chapter-title { font-weight: 500; font-size: 0.9rem; }
.chapter-team-tag { font-size: 0.8rem; opacity: 0.65; margin-top: 0.2rem; }

/* Settings */
.settings-content { padding: 1.5rem; display: flex; flex-direction: column; gap: 1.5rem; }
.settings-section { display: flex; flex-direction: column; gap: 0.6rem; }
.settings-label { font-weight: 500; font-size: 0.9rem; color: var(--color-text); }
.settings-options { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.opt-btn {
  padding: 0.45rem 1rem;
  background: var(--color-background-mute);
  border: 1px solid var(--color-border);
  border-radius: 0.4rem; color: var(--color-text);
  cursor: pointer; font-size: 0.875rem; transition: all 0.2s;
}
.opt-btn:hover { border-color: var(--color-accent); }
.opt-btn.active { background: var(--color-accent); color: #fff; border-color: var(--color-accent); }
.settings-slider { width: 100%; }
.slider {
  width: 100%; height: 4px;
  background: var(--color-background-mute);
  border-radius: 2px; outline: none; -webkit-appearance: none;
}
.slider::-webkit-slider-thumb {
  -webkit-appearance: none; width: 16px; height: 16px;
  background: var(--color-accent); border-radius: 50%; cursor: pointer;
}

@media (max-width: 768px) {
  .back-text { display: none; }
  .text-content-wrapper { max-width: 100% !important; padding: 1rem; }
  .bottom-nav { flex-direction: column; }
  .nav-btn { width: 100%; justify-content: center; }
}
</style>
