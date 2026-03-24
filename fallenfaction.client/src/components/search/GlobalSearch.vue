<template>
  <Dialog v-model:open="isOpen">
    <DialogTrigger as-child>
      <Button variant="secondary"
              class="w-full md:w-64 justify-start text-left font-normal gap-2">
        <Search class="h-4 w-4 text-muted-foreground" />
        <span class="text-muted-foreground">Search everything...</span>
        <kbd class="ml-auto pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground opacity-100">
          <span class="text-xs">⌘</span>K
        </kbd>
      </Button>
    </DialogTrigger>

    <DialogContent class="max-w-2xl p-0 gap-0" style="background-color: hsl(20 14.3% 4.1%) !important;">
      <!-- Search Input -->
      <div class="flex items-center border-b px-4 py-3" style="border-color: hsl(240 3.7% 15.9%);">
        <Search class="h-4 w-4 text-muted-foreground mr-3" />
        <input ref="searchInputRef"
               v-model="searchQuery"
               @input="handleSearch"
               type="text"
               placeholder="Search titles, teams, people, tags..."
               class="flex-1 bg-transparent outline-none text-sm placeholder:text-muted-foreground" />
        <Button v-if="searchQuery"
                variant="ghost"
                size="sm"
                @click="clearSearch"
                class="h-6 w-6 p-0 clear-search-btn ml-2">
          <X class="h-3 w-3" />
        </Button>
      </div>

      <!-- Results -->
      <div class="max-h-[60vh] overflow-y-auto">
        <!-- Loading State -->
        <div v-if="isLoading" class="p-8 text-center">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
          <p class="text-sm text-muted-foreground mt-2">Searching...</p>
        </div>

        <!-- Empty State - No Query -->
        <div v-else-if="!searchQuery" class="p-8 text-center space-y-4">
          <div class="text-muted-foreground">
            <Sparkles class="h-12 w-12 mx-auto mb-3 opacity-50" />
            <p class="text-sm">Start typing to search across everything</p>
          </div>

          <!-- Quick Links -->
          <div class="text-left max-w-md mx-auto space-y-2">
            <p class="text-xs text-muted-foreground font-medium mb-2">Quick Actions</p>
            <Button variant="ghost" class="w-full justify-start gap-2" size="sm" @click="navigateTo('/home/catalog')">
              <BookOpen class="h-4 w-4" />
              Browse Catalog
            </Button>
            <Button variant="ghost" class="w-full justify-start gap-2" size="sm" @click="navigateTo('/home/teams')">
              <Users class="h-4 w-4" />
              Browse Teams
            </Button>
          </div>
        </div>

        <!-- Empty State - No Results -->
        <div v-else-if="!hasResults" class="p-8 text-center">
          <SearchX class="h-12 w-12 text-muted-foreground mx-auto mb-3 opacity-50" />
          <p class="text-sm text-muted-foreground">No results found for "{{ searchQuery }}"</p>
          <p class="text-xs text-muted-foreground mt-1">Try different keywords</p>
        </div>

        <!-- Results by Category -->
        <div v-else class="p-4 space-y-4">
          <!-- Titles -->
          <SearchResultSection v-if="results.titles.length > 0"
                               title="Titles"
                               :icon="BookOpen"
                               :results="results.titles"
                               @select="handleTitleSelect">
            <template #result="{ result }">
              <div class="flex items-center gap-3 flex-1 min-w-0">
                <img :src="getImageUrl(result.coverImagePath)"
                     :alt="result.englishTitle"
                     class="w-10 h-14 object-cover rounded" />
                <div class="flex-1 min-w-0">
                  <p class="font-medium truncate">{{ result.englishTitle }}</p>
                  <p class="text-xs text-muted-foreground truncate">{{ result.originalTitle }}</p>
                </div>
                <Badge variant="secondary" class="shrink-0">{{ getTypeDisplay(result.type) }}</Badge>
              </div>
            </template>
          </SearchResultSection>

          <!-- Teams -->
          <SearchResultSection v-if="results.teams.length > 0"
                               title="Teams"
                               :icon="Users"
                               :results="results.teams"
                               @select="handleTeamSelect">
            <template #result="{ result }">
              <div class="flex items-center gap-3 flex-1 min-w-0">
                <div class="w-10 h-10 rounded-full bg-secondary flex items-center justify-center shrink-0">
                  <Users class="h-5 w-5 text-muted-foreground" />
                </div>
                <div class="flex-1 min-w-0">
                  <p class="font-medium truncate">{{ result.name }}</p>
                  <p class="text-xs text-muted-foreground truncate">{{ result.description }}</p>
                </div>
              </div>
            </template>
          </SearchResultSection>

          <!-- Authors -->
          <SearchResultSection v-if="results.authors.length > 0"
                               title="Authors"
                               :icon="Pen"
                               :results="results.authors"
                               @select="handleAuthorSelect">
            <template #result="{ result }">
              <div class="flex items-center gap-3 flex-1 min-w-0">
                <div class="w-10 h-10 rounded-full bg-secondary flex items-center justify-center shrink-0">
                  <Pen class="h-5 w-5 text-muted-foreground" />
                </div>
                <div class="flex-1 min-w-0">
                  <p class="font-medium truncate">{{ result.name }}</p>
                  <p class="text-xs text-muted-foreground">{{ result.titleCount }} titles</p>
                </div>
              </div>
            </template>
          </SearchResultSection>

          <!-- Artists -->
          <SearchResultSection v-if="results.artists.length > 0"
                               title="Artists"
                               :icon="Palette"
                               :results="results.artists"
                               @select="handleArtistSelect">
            <template #result="{ result }">
              <div class="flex items-center gap-3 flex-1 min-w-0">
                <div class="w-10 h-10 rounded-full bg-secondary flex items-center justify-center shrink-0">
                  <Palette class="h-5 w-5 text-muted-foreground" />
                </div>
                <div class="flex-1 min-w-0">
                  <p class="font-medium truncate">{{ result.name }}</p>
                  <p class="text-xs text-muted-foreground">{{ result.titleCount }} titles</p>
                </div>
              </div>
            </template>
          </SearchResultSection>

          <!-- Publishers -->
          <SearchResultSection v-if="results.publishers.length > 0"
                               title="Publishers"
                               :icon="Building"
                               :results="results.publishers"
                               @select="handlePublisherSelect">
            <template #result="{ result }">
              <div class="flex items-center gap-3 flex-1 min-w-0">
                <div class="w-10 h-10 rounded-full bg-secondary flex items-center justify-center shrink-0">
                  <Building class="h-5 w-5 text-muted-foreground" />
                </div>
                <div class="flex-1 min-w-0">
                  <p class="font-medium truncate">{{ result.name }}</p>
                  <p class="text-xs text-muted-foreground">{{ result.titleCount }} titles</p>
                </div>
              </div>
            </template>
          </SearchResultSection>

          <!-- Tags -->
          <SearchResultSection v-if="results.tags.length > 0"
                               title="Tags"
                               :icon="Tag"
                               :results="results.tags"
                               @select="handleTagSelect">
            <template #result="{ result }">
              <div class="flex items-center gap-3 flex-1 min-w-0">
                <Tag class="h-4 w-4 text-muted-foreground shrink-0" />
                <p class="font-medium truncate">{{ result.name }}</p>
              </div>
            </template>
          </SearchResultSection>

          <!-- Users -->
          <SearchResultSection v-if="results.users.length > 0"
                               title="Users"
                               :icon="UserCircle"
                               :results="results.users"
                               @select="handleUserSelect">
            <template #result="{ result }">
              <div class="flex items-center gap-3 flex-1 min-w-0">
                <img :src="result.avatar"
                     :alt="result.name"
                     class="w-10 h-10 rounded-full object-cover" />
                <div class="flex-1 min-w-0">
                  <p class="font-medium truncate">{{ result.name }}</p>
                  <p class="text-xs text-muted-foreground">Level {{ result.level }}</p>
                </div>
              </div>
            </template>
          </SearchResultSection>
        </div>
      </div>

      <!-- Footer -->
      <div v-if="hasResults"
           class="border-t px-4 py-2 flex items-center justify-between text-xs text-muted-foreground"
           style="border-color: hsl(240 3.7% 15.9%);">
        <div class="flex items-center gap-4">
          <span>{{ totalResults }} results</span>
        </div>
        <div class="flex items-center gap-2">
          <kbd class="pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium">
            ESC
          </kbd>
          <span>to close</span>
        </div>
      </div>
    </DialogContent>
  </Dialog>
</template>

<script setup>
  import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { debounce } from 'lodash-es';
  import {
    Search, X, BookOpen, Users, Pen, Palette, Building, Tag as TagIcon,
    UserCircle, SearchX, Sparkles
  } from 'lucide-vue-next';

  import { Dialog, DialogContent, DialogTrigger } from '@/components/ui/dialog';
  import { Button } from '@/components/ui/button';
  import { Badge } from '@/components/ui/badge';
  import SearchResultSection from './SearchResultSection.vue';
  import { globalSearchService } from '@/services/globalSearchService';

  const router = useRouter();
  const isOpen = ref(false);
  const searchQuery = ref('');
  const searchInputRef = ref(null);
  const isLoading = ref(false);

  const results = ref({
    titles: [],
    teams: [],
    authors: [],
    artists: [],
    publishers: [],
    tags: [],
    users: []
  });

  const hasResults = computed(() => {
    return Object.values(results.value).some(arr => arr.length > 0);
  });

  const totalResults = computed(() => {
    return Object.values(results.value).reduce((sum, arr) => sum + arr.length, 0);
  });

  // Keyboard shortcut (Cmd+K / Ctrl+K)
  const handleKeyDown = (e) => {
    if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
      e.preventDefault();
      isOpen.value = !isOpen.value;
    }
    if (e.key === 'Escape' && isOpen.value) {
      isOpen.value = false;
    }
  };

  onMounted(() => {
    window.addEventListener('keydown', handleKeyDown);
  });

  onUnmounted(() => {
    window.removeEventListener('keydown', handleKeyDown);
  });

  // Focus input when dialog opens
  watch(isOpen, (newValue) => {
    if (newValue) {
      setTimeout(() => {
        searchInputRef.value?.focus();
      }, 100);
    } else {
      // Clear search when closing
      searchQuery.value = '';
      results.value = {
        titles: [],
        teams: [],
        authors: [],
        artists: [],
        publishers: [],
        tags: [],
        users: []
      };
    }
  });

  // Debounced search
  const performSearch = debounce(async (query) => {
    if (!query || query.trim().length < 2) {
      results.value = {
        titles: [],
        teams: [],
        authors: [],
        artists: [],
        publishers: [],
        tags: [],
        users: []
      };
      isLoading.value = false;
      return;
    }

    isLoading.value = true;

    try {
      const searchResults = await globalSearchService.searchAll(query);
      results.value = searchResults;
    } catch (error) {
      console.error('Search error:', error);
      results.value = {
        titles: [],
        teams: [],
        authors: [],
        artists: [],
        publishers: [],
        tags: [],
        users: []
      };
    } finally {
      isLoading.value = false;
    }
  }, 300);

  const handleSearch = () => {
    performSearch(searchQuery.value);
  };

  const clearSearch = () => {
    searchQuery.value = '';
    results.value = {
      titles: [],
      teams: [],
      authors: [],
      artists: [],
      publishers: [],
      tags: [],
      users: []
    };
  };

  // Navigation handlers
  const handleTitleSelect = (title) => {
    isOpen.value = false;
    router.push(`/title/${title.id}`);
  };

  const handleTeamSelect = (team) => {
    isOpen.value = false;
    router.push(`/team/${team.id}`);
  };

  const handleAuthorSelect = (author) => {
    isOpen.value = false;
    router.push(`/home/catalog?authors=${author.id}`);
  };

  const handleArtistSelect = (artist) => {
    isOpen.value = false;
    router.push(`/home/catalog?artists=${artist.id}`);
  };

  const handlePublisherSelect = (publisher) => {
    isOpen.value = false;
    router.push(`/home/catalog?publishers=${publisher.id}`);
  };

  const handleTagSelect = (tag) => {
    isOpen.value = false;
    router.push(`/home/catalog?tags=${tag.id}`);
  };

  const handleUserSelect = (user) => {
    isOpen.value = false;
    router.push(`/user/${user.id}`);
  };

  const navigateTo = (path) => {
    isOpen.value = false;
    router.push(path);
  };

  // Helper functions
  const getImageUrl = (path) => {
    if (!path) return '/img/no-cover.png';
    if (path.startsWith('http')) return path;
    const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace('/api', '');
    return `${baseUrl}${path}`;
  };

  const getTypeDisplay = (type) => {
    const types = {
      1: 'Manga',
      2: 'Manhwa',
      3: 'Manhua',
      4: 'Comic',
      5: 'Webtoon'
    };
    return types[type] || 'Unknown';
  };

  // Expose method to open dialog programmatically (for mobile button)
  defineExpose({
    open: () => {
      isOpen.value = true;
    }
  });
</script>

<style scoped>
  /* Ensure dialog backdrop has blur */
  :deep(.DialogOverlay) {
    backdrop-filter: blur(4px);
    background-color: rgba(0, 0, 0, 0.5);
  }

  /* Hide the default Dialog close button to prevent overlap with our clear button */
  :deep([data-radix-dialog-close]) {
    display: none !important;
  }

  /* Ensure our clear button is properly positioned */
  .clear-search-btn {
    flex-shrink: 0;
  }
</style>
