<template>
  <!-- ─── Trigger button ─────────────────────────────────────── -->
  <button class="search-trigger" @click="open" aria-label="Open search">
    <Search class="h-4 w-4 text-muted-foreground" />
    <span class="search-trigger-label">Search everything...</span>
    <kbd class="search-trigger-kbd"><span class="text-xs">⌘</span>K</kbd>
  </button>

  <!-- ─── Full-screen overlay ────────────────────────────────── -->
  <Teleport to="body">
    <Transition name="sf">
      <div v-if="isOpen" class="s-overlay" @click.self="close">
        <div class="s-modal" role="dialog" aria-modal="true" aria-label="Search">

          <!-- Header -->
          <div class="s-header">
            <Search class="s-header-icon" />
            <input ref="inputRef"
                   v-model="query"
                   type="text"
                   placeholder="Search titles, teams, people, tags..."
                   class="s-input"
                   autocomplete="off"
                   autocorrect="off"
                   autocapitalize="off"
                   spellcheck="false"
                   @input="onInput" />
            <button v-if="query" class="s-icon-btn" @click="clearQuery" aria-label="Clear">
              <X class="h-4 w-4" />
            </button>
            <button class="s-icon-btn s-close-mobile" @click="close" aria-label="Close">
              <ArrowLeft class="h-5 w-5" />
            </button>
          </div>

          <!-- ─── Category tabs — ALWAYS visible ────────────────── -->
          <div class="s-tabs-bar">
            <div class="s-tabs-scroll" ref="tabsRef">
              <button v-for="tab in TABS"
                      :key="tab.key"
                      class="s-tab"
                      :class="{ 's-tab-active': activeTab === tab.key }"
                      @click="selectTab(tab.key)">
                <component :is="tab.icon" class="s-tab-icon" />
                <span>{{ tab.label }}</span>
                <span v-if="results[tab.key]?.length" class="s-tab-count">
                  {{ results[tab.key].length }}
                </span>
                <span v-if="activeTab === tab.key" class="s-tab-line" />
              </button>
            </div>
          </div>

          <!-- ─── Body ─────────────────────────────────────────── -->
          <div class="s-body">

            <!-- Loading -->
            <div v-if="isLoading" class="s-state">
              <div class="s-spinner" />
              <p class="s-state-text">Searching...</p>
            </div>

            <!-- Empty query -->
            <div v-else-if="!query.trim()" class="s-state">
              <Sparkles class="s-state-icon" />
              <p class="s-state-text">Start typing to search</p>
              <div class="s-quick">
                <p class="s-quick-label">Quick Actions</p>
                <button class="s-quick-btn" @click="nav('/home/catalog')">
                  <BookOpen class="h-4 w-4" /> Browse Catalog
                </button>
                <button class="s-quick-btn" @click="nav('/home/teams')">
                  <Users class="h-4 w-4" /> Browse Teams
                </button>
              </div>
            </div>

            <!-- Query too short -->
            <div v-else-if="query.trim().length < 2" class="s-state">
              <p class="s-state-text">Keep typing…</p>
            </div>

            <!-- No results in active tab -->
            <div v-else-if="!isLoading && activeTabResults.length === 0" class="s-state">
              <SearchX class="s-state-icon" />
              <p class="s-state-text">No {{ activeTabLabel }} found for "{{ query }}"</p>
              <p class="s-state-sub">Try different keywords</p>
            </div>

            <!-- Results -->
            <div v-else class="s-results">

              <!-- TITLES → TitleCard grid -->
              <div v-if="activeTab === 'titles'" class="titles-grid">
                <TitleCard v-for="t in results.titles"
                           :key="t.id"
                           :title="t"
                           view-mode="grid"
                           @click="close" />
              </div>

              <!-- PEOPLE (teams / authors / artists / publishers / users) -->
              <div v-else-if="['teams','authors','artists','publishers','users'].includes(activeTab)"
                   class="people-list">
                <button v-for="item in activeTabResults"
                        :key="item.id"
                        class="people-item"
                        @click="handleSelect(activeTab, item)">
                  <img v-if="activeTab === 'users' && item.avatar"
                       :src="item.avatar"
                       :alt="item.name"
                       class="people-img" />
                  <div v-else class="people-avatar">
                    <component :is="activeTabIcon" class="h-5 w-5 opacity-50" />
                  </div>
                  <div class="people-info">
                    <p class="people-name">{{ item.name }}</p>
                    <p v-if="item.description" class="people-sub">{{ item.description }}</p>
                    <p v-else-if="item.titleCount != null" class="people-sub">{{ item.titleCount }} titles</p>
                    <p v-else-if="item.level != null" class="people-sub">Level {{ item.level }}</p>
                  </div>
                </button>
              </div>

              <!-- TAGS → pill cloud -->
              <div v-else-if="activeTab === 'tags'" class="tags-cloud">
                <button v-for="item in results.tags"
                        :key="item.id"
                        class="tag-pill"
                        @click="handleSelect('tags', item)">
                  <Tag class="h-3.5 w-3.5" />
                  {{ item.name }}
                </button>
              </div>

            </div>
          </div>

          <!-- Footer (desktop) -->
          <div v-if="query.trim().length >= 2 && totalResults > 0 && !isLoading" class="s-footer">
            <span>{{ totalResults }} result{{ totalResults !== 1 ? 's' : '' }}</span>
            <span class="s-footer-hint"><kbd>ESC</kbd> to close</span>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
  import { ref, computed, nextTick, onMounted, onUnmounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { debounce } from 'lodash-es'
  import {
    Search, X, ArrowLeft, BookOpen, Users, Pen, Palette,
    Building, Tag, UserCircle, SearchX, Sparkles
  } from 'lucide-vue-next'

  import TitleCard from '@/components/catalog/TitleCard.vue'
  import { globalSearchService } from '@/services/globalSearchService'

  const router = useRouter()
  const isOpen = ref(false)
  const query = ref('')
  const isLoading = ref(false)
  const activeTab = ref('titles')
  const inputRef = ref(null)
  const tabsRef = ref(null)

  const EMPTY = () => ({
    titles: [], teams: [], authors: [],
    artists: [], publishers: [], tags: [], users: [],
  })
  const results = ref(EMPTY())

  // ─── tabs ─────────────────────────────────────────────────────────
  const TABS = [
    { key: 'titles', label: 'Titles', icon: BookOpen },
    { key: 'teams', label: 'Teams', icon: Users },
    { key: 'authors', label: 'Authors', icon: Pen },
    { key: 'artists', label: 'Artists', icon: Palette },
    { key: 'publishers', label: 'Publishers', icon: Building },
    { key: 'tags', label: 'Tags', icon: Tag },
    { key: 'users', label: 'Users', icon: UserCircle },
  ]

  const activeTabResults = computed(() => results.value[activeTab.value] ?? [])
  const activeTabLabel = computed(() => TABS.find(t => t.key === activeTab.value)?.label ?? '')
  const activeTabIcon = computed(() => TABS.find(t => t.key === activeTab.value)?.icon ?? null)
  const totalResults = computed(() => Object.values(results.value).reduce((s, a) => s + a.length, 0))

  // ─── tab selection ─────────────────────────────────────────────────
  function selectTab(key) {
    activeTab.value = key
    nextTick(() => {
      tabsRef.value?.querySelector('.s-tab-active')
        ?.scrollIntoView({ inline: 'nearest', block: 'nearest', behavior: 'smooth' })
    })
    // Re-search in the new category immediately if query is ready
    if (query.value.trim().length >= 2) runSearch(query.value)
  }

  // ─── search ────────────────────────────────────────────────────────
  // Only search the active category — tabs are the filter
  const runSearch = debounce(async (q) => {
    const trimmed = q.trim()
    if (!trimmed || trimmed.length < 2) {
      results.value = EMPTY()
      isLoading.value = false
      return
    }
    isLoading.value = true
    try {
      results.value = await globalSearchService.searchCategory(activeTab.value, trimmed)
    } catch {
      results.value = EMPTY()
    } finally {
      isLoading.value = false
    }
  }, 300)

  function onInput() { runSearch(query.value) }

  function clearQuery() {
    query.value = ''
    results.value = EMPTY()
    inputRef.value?.focus()
  }

  // ─── open / close ──────────────────────────────────────────────────
  function open() {
    isOpen.value = true
    document.body.style.overflow = 'hidden'
    nextTick(() => inputRef.value?.focus())
  }

  function close() {
    isOpen.value = false
    query.value = ''
    results.value = EMPTY()
    document.body.style.overflow = ''
  }

  // ─── navigation ────────────────────────────────────────────────────
  function nav(path) { close(); router.push(path) }

  function handleSelect(type, item) {
    close()
    const routes = {
      teams: `/team/${item.id}`,
      authors: `/home/catalog?authors=${item.id}`,
      artists: `/home/catalog?artists=${item.id}`,
      publishers: `/home/catalog?publishers=${item.id}`,
      tags: `/home/catalog?tags=${item.id}`,
      users: `/user/${item.id}`,
    }
    if (routes[type]) router.push(routes[type])
  }

  // ─── keyboard ──────────────────────────────────────────────────────
  function onKeyDown(e) {
    if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
      e.preventDefault()
      isOpen.value ? close() : open()
    }
    if (e.key === 'Escape' && isOpen.value) close()
  }

  onMounted(() => window.addEventListener('keydown', onKeyDown))
  onUnmounted(() => {
    window.removeEventListener('keydown', onKeyDown)
    document.body.style.overflow = ''
  })

  defineExpose({ open })
</script>

<style scoped>
  /* ── Trigger ────────────────────────────────────────────────── */
  .search-trigger {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.4rem 0.75rem;
    border-radius: 0.5rem;
    border: 1px solid var(--color-border);
    background: var(--color-background-soft);
    color: var(--color-text);
    cursor: pointer;
    font-size: 0.875rem;
    transition: border-color 0.15s, background 0.15s;
    width: 100%;
    max-width: 16rem;
    white-space: nowrap;
  }

    .search-trigger:hover {
      border-color: var(--color-accent);
      background: var(--color-background-mute);
    }

  .search-trigger-label {
    flex: 1;
    text-align: left;
    opacity: 0.45;
  }

  .search-trigger-kbd {
    margin-left: auto;
    display: inline-flex;
    align-items: center;
    gap: 1px;
    height: 1.25rem;
    padding: 0 0.375rem;
    border-radius: 0.25rem;
    border: 1px solid var(--color-border);
    background: var(--color-background-mute);
    font-size: 0.625rem;
    font-family: monospace;
    opacity: 0.7;
  }

  /* ── Transition ─────────────────────────────────────────────── */
  .sf-enter-active, .sf-leave-active {
    transition: opacity 0.18s ease;
  }

  .sf-enter-from, .sf-leave-to {
    opacity: 0;
  }

  /* ── Overlay ────────────────────────────────────────────────── */
  .s-overlay {
    position: fixed;
    inset: 0;
    z-index: 9999;
    background: rgba(0,0,0,0.55);
    backdrop-filter: blur(3px);
    display: flex;
    align-items: flex-start;
    justify-content: center;
    padding: 5vh 1rem 1rem;
  }

  /* ── Modal — desktop ────────────────────────────────────────── */
  .s-modal {
    width: 100%;
    max-width: 44rem;
    max-height: 85vh;
    display: flex;
    flex-direction: column;
    background: var(--color-background-soft);
    border: 1px solid var(--color-border);
    border-radius: 1rem;
    overflow: hidden;
    box-shadow: 0 24px 64px rgba(0,0,0,0.5);
  }

  /* ── Mobile full-screen ─────────────────────────────────────── */
  @media (max-width: 640px) {
    .s-overlay {
      padding: 0;
      align-items: stretch;
    }

    .s-modal {
      max-width: 100%;
      max-height: 100%;
      height: 100%;
      border-radius: 0;
      border: none;
    }
  }

  /* ── Header ─────────────────────────────────────────────────── */
  .s-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.875rem 1rem;
    border-bottom: 1px solid var(--color-border);
    flex-shrink: 0;
  }

  .s-header-icon {
    width: 1.1rem;
    height: 1.1rem;
    color: var(--color-text);
    opacity: 0.4;
    flex-shrink: 0;
  }

  .s-input {
    flex: 1;
    background: transparent;
    border: none;
    outline: none;
    font-size: 1rem;
    color: var(--color-text);
    caret-color: var(--color-accent);
  }

    .s-input::placeholder {
      color: var(--color-text);
      opacity: 0.35;
    }

  .s-icon-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0.3rem;
    background: transparent;
    border: none;
    color: var(--color-text);
    opacity: 0.5;
    cursor: pointer;
    border-radius: 0.25rem;
    transition: opacity 0.15s;
    flex-shrink: 0;
  }

    .s-icon-btn:hover {
      opacity: 1;
    }

  .s-close-mobile {
    display: none;
    border: 1px solid var(--color-border);
    padding: 0.35rem;
    border-radius: 0.375rem;
  }

  @media (max-width: 640px) {
    .s-close-mobile {
      display: flex;
    }
  }

  /* ── Tabs ────────────────────────────────────────────────────── */
  .s-tabs-bar {
    border-bottom: 1px solid var(--color-border);
    flex-shrink: 0;
    background: var(--color-background-soft);
  }

  .s-tabs-scroll {
    display: flex;
    overflow-x: auto;
    scrollbar-width: none;
    -ms-overflow-style: none;
  }

    .s-tabs-scroll::-webkit-scrollbar {
      display: none;
    }

  .s-tab {
    position: relative;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 10px 14px 11px;
    font-size: 0.8125rem;
    font-weight: 500;
    color: var(--color-text);
    opacity: 0.55;
    white-space: nowrap;
    cursor: pointer;
    background: transparent;
    border: none;
    transition: opacity 0.15s;
    flex-shrink: 0;
    user-select: none;
  }

    .s-tab:hover {
      opacity: 0.85;
    }

  .s-tab-active {
    color: var(--color-text);
    opacity: 1;
    font-weight: 600;
  }

  .s-tab-icon {
    width: 0.875rem;
    height: 0.875rem;
  }

  .s-tab-count {
    font-size: 0.6875rem;
    font-weight: 500;
    line-height: 1.4;
    background: var(--color-accent);
    color: #fff;
    padding: 0 5px;
    border-radius: 99px;
  }

  .s-tab-line {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 2px;
    background: var(--color-accent);
    border-radius: 99px;
  }

  /* ── Body ────────────────────────────────────────────────────── */
  .s-body {
    flex: 1;
    overflow-y: auto;
    overscroll-behavior: contain;
  }

  /* ── States ──────────────────────────────────────────────────── */
  .s-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem 1.5rem;
    text-align: center;
    gap: 0.5rem;
  }

  .s-state-icon {
    width: 2.75rem;
    height: 2.75rem;
    color: var(--color-text);
    opacity: 0.2;
    margin-bottom: 0.5rem;
  }

  .s-state-text {
    font-size: 0.9rem;
    color: var(--color-text);
    opacity: 0.6;
  }

  .s-state-sub {
    font-size: 0.8rem;
    color: var(--color-text);
    opacity: 0.4;
  }

  .s-spinner {
    width: 2rem;
    height: 2rem;
    border-radius: 50%;
    border: 2px solid var(--color-border);
    border-top-color: var(--color-accent);
    animation: spin 0.7s linear infinite;
    margin-bottom: 0.5rem;
  }

  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }

  .s-quick {
    margin-top: 0.75rem;
    width: 100%;
    max-width: 18rem;
    text-align: left;
  }

  .s-quick-label {
    font-size: 0.75rem;
    font-weight: 500;
    color: var(--color-text);
    opacity: 0.45;
    margin-bottom: 0.5rem;
  }

  .s-quick-btn {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    padding: 0.6rem 0.75rem;
    border-radius: 0.5rem;
    background: transparent;
    border: none;
    color: var(--color-text);
    font-size: 0.875rem;
    cursor: pointer;
    transition: background 0.15s;
    margin-bottom: 0.25rem;
  }

    .s-quick-btn:hover {
      background: var(--color-background-mute);
    }

  /* ── Results ─────────────────────────────────────────────────── */
  .s-results {
    padding: 1rem;
  }

  .titles-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
    gap: 1rem;
  }

  @media (min-width: 480px) {
    .titles-grid {
      grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
    }
  }

  @media (min-width: 640px) {
    .titles-grid {
      grid-template-columns: repeat(auto-fill, minmax(155px, 1fr));
    }
  }

  .people-list {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .people-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.625rem 0.75rem;
    border-radius: 0.5rem;
    background: transparent;
    border: none;
    cursor: pointer;
    text-align: left;
    transition: background 0.15s;
    width: 100%;
  }

    .people-item:hover {
      background: var(--color-background-mute);
    }

  .people-avatar {
    width: 2.5rem;
    height: 2.5rem;
    border-radius: 50%;
    background: var(--color-background-mute);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    color: var(--color-text);
  }

  .people-img {
    width: 2.5rem;
    height: 2.5rem;
    border-radius: 50%;
    object-fit: cover;
    flex-shrink: 0;
  }

  .people-info {
    flex: 1;
    min-width: 0;
  }

  .people-name {
    font-size: 0.9rem;
    font-weight: 500;
    color: var(--color-text);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .people-sub {
    font-size: 0.775rem;
    color: var(--color-text);
    opacity: 0.5;
    margin-top: 1px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .tags-cloud {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    padding: 0.25rem 0;
  }

  .tag-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    padding: 0.4rem 0.85rem;
    border-radius: 99px;
    background: var(--color-background-mute);
    border: 1px solid var(--color-border);
    color: var(--color-text);
    font-size: 0.8125rem;
    cursor: pointer;
    transition: border-color 0.15s;
  }

    .tag-pill:hover {
      border-color: var(--color-accent);
    }

  /* ── Footer ──────────────────────────────────────────────────── */
  .s-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0.5rem 1rem;
    border-top: 1px solid var(--color-border);
    font-size: 0.75rem;
    color: var(--color-text);
    opacity: 0.5;
    flex-shrink: 0;
  }

  .s-footer-hint {
    display: flex;
    align-items: center;
    gap: 0.375rem;
  }

    .s-footer-hint kbd {
      display: inline-flex;
      align-items: center;
      height: 1.25rem;
      padding: 0 0.375rem;
      border-radius: 0.25rem;
      border: 1px solid currentColor;
      font-size: 0.6875rem;
      font-family: monospace;
      opacity: 0.7;
    }

  @media (max-width: 640px) {
    .s-footer {
      display: none;
    }
  }
</style>
