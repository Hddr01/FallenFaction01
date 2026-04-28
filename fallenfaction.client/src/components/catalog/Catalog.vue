<template>
  <div class="min-h-screen bg-[var(--color-background)] text-[var(--color-text)]">
    <!-- Hero Section with Search -->
    <div class="relative border-b border-[var(--color-border)] bg-[var(--color-background-soft)]">
      <div class="container mx-auto px-4 py-12">
        <div class="max-w-4xl mx-auto text-center space-y-6">
          <h1 class="text-4xl md:text-5xl font-bold tracking-tight">
            Discover Your Next Read
          </h1>
          <p class="text-lg text-muted-foreground">
            Explore thousands of manga, manhwa, and comics
          </p>

          <!-- Search Bar -->
          <div class="relative max-w-2xl mx-auto">
            <Search class="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-muted-foreground" />
            <Input v-model="searchQuery"
                   @input="debouncedSearch"
                   placeholder="Search titles, authors, artists..."
                   class="pl-10 pr-10 h-12 text-lg" />
            <Button v-if="searchQuery"
                    variant="ghost"
                    size="sm"
                    class="absolute right-2 top-1/2 -translate-y-1/2"
                    @click="clearSearch">
              <X class="h-4 w-4" />
            </Button>
          </div>

          <!-- Quick Filters -->
          <div class="border border-[var(--color-border)] bg-[var(--color-background-soft)] rounded-lg overflow-hidden shadow-lg max-w-2xl mx-auto">
            <div class="flex w-full overflow-x-auto whitespace-nowrap bg-transparent p-1.5 gap-1 scrollbar-hide">
              <a v-for="quickFilter in quickFilters"
                 :key="quickFilter.value"
                 class="flex-shrink-0 flex items-center gap-2 cursor-pointer bg-transparent rounded-md transition-all duration-300 ease-out no-underline text-[var(--color-text)] opacity-60 relative overflow-visible hover:opacity-100 hover:text-[var(--color-heading)] hover:-translate-y-0.5"
                 :class="{ 'text-purple-500': activeQuickFilter === quickFilter.value }"
                 href="#"
                 @click.prevent="applyQuickFilter(quickFilter.value)">
                <div class="px-4 py-2 relative flex items-center gap-2 font-semibold text-sm tracking-wide z-10">
                  <component :is="quickFilter.icon" class="h-4 w-4" />
                  {{ quickFilter.label }}

                  <!-- Animated background indicator -->
                  <Motion v-if="activeQuickFilter === quickFilter.value"
                          layout-id="quick-filter-indicator"
                          class="absolute inset-0 -z-10 bg-gradient-to-br from-white/10 to-white/5 border border-white/20 rounded-md shadow-[0_4px_12px_rgba(255,255,255,0.15),inset_0_1px_0_rgba(255,255,255,0.1)]"
                          :initial="{ opacity: 0 }"
                          :animate="{ opacity: 1 }"
                          :transition="{
                            type: 'spring',
                            stiffness: 300,
                            damping: 30
                          }" />
                </div>
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Content -->
    <div class="container mx-auto px-4 py-8">
      <div class="flex flex-col lg:flex-row gap-6">
        <!-- Sidebar Filters -->
        <aside class="lg:w-80 space-y-4 relative z-10">
          <!-- Mobile Filter Toggle -->
          <Sheet v-model:open="mobileFiltersOpen">
            <SheetTrigger as-child class="lg:hidden">
              <Button variant="secondary" class="w-full gap-2">
                <SlidersHorizontal class="h-4 w-4" />
                Filters
                <Badge v-if="activeFiltersCount > 0" variant="secondary" class="ml-auto">
                  {{ activeFiltersCount }}
                </Badge>
              </Button>
            </SheetTrigger>
            <SheetContent side="left" class="w-80 overflow-y-auto filter-sheet" style="background-color: var(--color-background) !important;">
              <SheetHeader>
                <SheetTitle>Filters</SheetTitle>
                <SheetDescription>
                  Refine your search results
                </SheetDescription>
              </SheetHeader>
              <div class="mt-6">
                <CatalogFilters v-model="filters"
                                :filter-options="filterOptions"
                                @update:modelValue="handleFiltersChange"
                                @reset="resetFilters" />
              </div>
            </SheetContent>
          </Sheet>

          <!-- Desktop Filters -->
          <Card class="hidden lg:block bg-card z-10">
            <CardHeader class="pb-3">
              <div class="flex items-center justify-between">
                <CardTitle class="text-lg">Filters</CardTitle>
                <Button v-if="activeFiltersCount > 0"
                        variant="ghost"
                        size="sm"
                        @click="resetFilters"
                        class="gap-2">
                  <RotateCcw class="h-4 w-4" />
                  Reset
                </Button>
              </div>
              <CardDescription>
                {{ totalCount }} titles found
              </CardDescription>
            </CardHeader>
            <CardContent class="bg-card">
              <CatalogFilters v-model="filters"
                              :filter-options="filterOptions"
                              @update:modelValue="handleFiltersChange"
                              @reset="resetFilters" />
            </CardContent>
          </Card>
        </aside>

        <!-- Main Content Area -->
        <main class="flex-1 space-y-6 relative z-0">
          <!-- Toolbar -->
          <div class="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
            <!-- Results Info -->
            <div class="flex items-center gap-2">
              <h2 class="text-lg font-semibold">
                {{ totalCount }} Results
              </h2>
              <Badge v-if="activeFiltersCount > 0" variant="secondary">
                {{ activeFiltersCount }} filter{{ activeFiltersCount > 1 ? 's' : '' }} applied
              </Badge>
            </div>

            <!-- Sort & View Options -->
            <div class="flex items-center gap-2">
              <!-- Sort By -->
              <Select v-model="sortBy">
                <SelectTrigger class="w-[180px]">
                  <SelectValue placeholder="Sort by" />
                </SelectTrigger>
                <SelectContent class="!bg-[var(--color-background-soft)] border-[var(--color-border)]">
                  <SelectItem value="updated">Recently Updated</SelectItem>
                  <SelectItem value="rating">Highest Rated</SelectItem>
                  <SelectItem value="popular">Most Popular</SelectItem>
                  <SelectItem value="title">Title (A-Z)</SelectItem>
                  <SelectItem value="releaseDate">Release Date</SelectItem>
                  <SelectItem value="chapters">Most Chapters</SelectItem>
                </SelectContent>
              </Select>

              <!-- View Toggle -->
              <div class="flex border rounded-lg">
                <Button :variant="viewMode === 'grid' ? 'default' : 'ghost'"
                        size="sm"
                        @click="viewMode = 'grid'"
                        class="rounded-r-none">
                  <LayoutGrid class="h-4 w-4" />
                </Button>
                <Button :variant="viewMode === 'list' ? 'default' : 'ghost'"
                        size="sm"
                        @click="viewMode = 'list'"
                        class="rounded-l-none">
                  <List class="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>

          <!-- Active Filters Tags -->
          <div v-if="activeFilterTags.length > 0" class="flex flex-wrap gap-2">
            <Badge v-for="tag in activeFilterTags"
                   :key="tag.key"
                   variant="secondary"
                   class="gap-1.5 pl-2 pr-1">
              {{ tag.label }}
              <Button variant="ghost"
                      size="icon"
                      class="h-4 w-4 p-0 hover:bg-transparent"
                      @click="removeFilter(tag.key, tag.value)">
                <X class="h-3 w-3" />
              </Button>
            </Badge>
          </div>

          <!-- Loading State -->
          <div v-if="loading" class="space-y-4">
            <div :class="viewMode === 'grid'
              ? 'grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4'
              : 'space-y-4'">
              <TitleCardSkeleton v-for="i in 12" :key="i" :view-mode="viewMode" />
            </div>
          </div>

          <!-- Error State -->
          <Alert v-else-if="error" variant="destructive">
            <AlertCircle class="h-4 w-4" />
            <AlertTitle>Error</AlertTitle>
            <AlertDescription>
              {{ error }}
              <Button variant="secondary" size="sm" @click="loadCatalog" class="mt-2">
                Try Again
              </Button>
            </AlertDescription>
          </Alert>

          <!-- Empty State -->
          <Card v-else-if="titles.length === 0" class="p-12">
            <div class="text-center space-y-4">
              <div class="flex justify-center">
                <SearchX class="h-16 w-16 text-muted-foreground" />
              </div>
              <div class="space-y-2">
                <h3 class="text-lg font-semibold">No titles found</h3>
                <p class="text-muted-foreground">
                  Try adjusting your filters or search query
                </p>
              </div>
              <Button variant="secondary" @click="resetAllFilters">
                Clear All Filters
              </Button>
            </div>
          </Card>

          <!-- Titles Grid/List -->
          <TransitionGroup v-else
                           :name="viewMode === 'grid' ? 'grid' : 'list'"
                           tag="div"
                           :class="viewMode === 'grid'
              ? 'grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4'
              : 'space-y-4'">
            <TitleCard v-for="title in titles"
                       :key="title.id"
                       :title="title"
                       :view-mode="viewMode" />
          </TransitionGroup>

          <!-- Pagination -->
          <div v-if="totalPages > 1" class="flex justify-center pt-8">
            <Pagination v-slot="{ page }"
                        :total="totalCount"
                        :items-per-page="pageSize"
                        :default-page="currentPage"
                        @update:page="handlePageChange">
              <PaginationContent v-slot="{ items }">
                <PaginationItem>
                  <PaginationPrevious @click="goToPage(currentPage - 1)" />
                </PaginationItem>

                <template v-for="(item, index) in items" :key="index">
                  <PaginationItem v-if="item.type === 'page'">
                    <Button :variant="item.value === page ? 'default' : 'secondary'"
                            size="sm"
                            @click="goToPage(item.value)">
                      {{ item.value }}
                    </Button>
                  </PaginationItem>
                  <PaginationItem v-else>
                    <PaginationEllipsis :index="index" />
                  </PaginationItem>
                </template>

                <PaginationItem>
                  <PaginationNext @click="goToPage(currentPage + 1)" />
                </PaginationItem>
              </PaginationContent>
            </Pagination>
          </div>

          <!-- Scroll to Top Button -->
          <Transition name="fade">
            <Button v-if="showScrollTop"
                    variant="secondary"
                    size="icon"
                    class="fixed bottom-8 right-8 shadow-lg"
                    @click="scrollToTop">
              <ArrowUp class="h-4 w-4" />
            </Button>
          </Transition>
        </main>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted, watch } from 'vue';
  import { useScrollSignal } from '@/composables/useScrollSignal.js';
  import { useRouter, useRoute } from 'vue-router';
  import { debounce } from 'lodash-es';
  import { Motion } from 'motion-v';
  import {
    Search, X, SlidersHorizontal, LayoutGrid, List, RotateCcw,
    TrendingUp, Clock, Star, Flame, ArrowUp, AlertCircle, SearchX
  } from 'lucide-vue-next';

  // shadcn-vue components
  import { Button } from '@/components/ui/button';
  import { Input } from '@/components/ui/input';
  import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
  import { Badge } from '@/components/ui/badge';
  import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
  import {
    Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, SheetTrigger
  } from '@/components/ui/sheet';
  import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
  import {
    Pagination,
    PaginationContent,
    PaginationEllipsis,
    PaginationItem,
    PaginationNext,
    PaginationPrevious,
  } from '@/components/ui/pagination';

  // Custom components
  import CatalogFilters from './CatalogFilters.vue';
  import TitleCard from './TitleCard.vue';
  import TitleCardSkeleton from './TitleCardSkeleton.vue';

  // Services
  import catalogService from '@/services/catalogService';

  // State
  const router = useRouter();
  const route = useRoute();

  const loading = ref(false);
  const error = ref(null);
  const titles = ref([]);
  const filterOptions = ref({
    Authors: [],
    Artists: [],
    Publishers: [],
    Teams: [],
    Categories: [],
    Tags: [],
    Formats: []
  });

  // Search & Filters
  const searchQuery = ref('');
  const filters = ref({
    type: null,
    status: null,
    translationStatus: null,
    ageRestriction: null,
    categories: [],
    tags: [],
    formats: [],
    authors: [],
    artists: [],
    publishers: [],
    teams: [],
    yearFrom: null,
    yearTo: null
  });

  // View & Sort
  const viewMode = ref('grid'); // 'grid' or 'list'
  const sortBy = ref('updated');
  const sortOrder = ref('desc');

  // Pagination
  const currentPage = ref(1);
  const pageSize = ref(24);
  const totalCount = ref(0);
  const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value));

  // UI State
  const mobileFiltersOpen = ref(false);
  const { scrollY } = useScrollSignal();
  const showScrollTop = computed(() => scrollY.value > 500);
  const activeQuickFilter = ref(null);

  // Quick Filters
  const quickFilters = [
    { label: 'Trending', value: 'trending', icon: TrendingUp },
    { label: 'Popular', value: 'popular', icon: Flame },
    { label: 'New', value: 'new', icon: Star },
    { label: 'Updated', value: 'updated', icon: Clock }
  ];

  // Computed
  const activeFiltersCount = computed(() => {
    let count = 0;
    if (filters.value.type) count++;
    if (filters.value.status) count++;
    if (filters.value.translationStatus) count++;
    if (filters.value.ageRestriction) count++;
    count += filters.value.categories.length;
    count += filters.value.tags.length;
    count += filters.value.formats.length;
    count += filters.value.authors.length;
    count += filters.value.artists.length;
    count += filters.value.publishers.length;
    count += filters.value.teams.length;
    if (filters.value.yearFrom) count++;
    if (filters.value.yearTo) count++;
    return count;
  });

  const activeFilterTags = computed(() => {
    const tags = [];

    // Helper to find name by ID
    const findName = (list, id) => list.find(item => item.id === id)?.name || id;

    if (filters.value.type) {
      tags.push({
        key: 'type',
        value: filters.value.type,
        label: `Type: ${catalogService.getTypeDisplayName(filters.value.type)}`
      });
    }

    if (filters.value.status) {
      tags.push({
        key: 'status',
        value: filters.value.status,
        label: `Status: ${catalogService.getStatusDisplayName(filters.value.status)}`
      });
    }

    // Categories
    filters.value.categories.forEach(id => {
      tags.push({
        key: 'categories',
        value: id,
        label: findName(filterOptions.value.Categories, id)
      });
    });

    // Tags
    filters.value.tags.forEach(id => {
      tags.push({
        key: 'tags',
        value: id,
        label: findName(filterOptions.value.Tags, id)
      });
    });

    return tags;
  });

  // Methods
  async function loadCatalog() {
    loading.value = true;
    error.value = null;

    try {
      const params = {
        page: currentPage.value,
        pageSize: pageSize.value,
        search: searchQuery.value,
        sortBy: sortBy.value,
        sortOrder: sortOrder.value,
        ...filters.value
      };

      const result = await catalogService.getCatalogTitles(params);

      if (result.success) {
        titles.value = result.data.items || [];
        totalCount.value = result.data.totalCount || 0;
      } else {
        error.value = result.error;
        titles.value = [];
        totalCount.value = 0;
      }
    } catch (err) {
      console.error('Error loading catalog:', err);
      error.value = 'An unexpected error occurred';
    } finally {
      loading.value = false;
    }
  }

  async function loadFilterOptions() {
    const result = await catalogService.getFilterOptions();
    if (result.success) {
      filterOptions.value = result.data;
    }
  }

  const debouncedSearch = debounce(() => {
    currentPage.value = 1;
    updateURLParams();
    loadCatalog();
  }, 500);

  function clearSearch() {
    searchQuery.value = '';
    currentPage.value = 1;
    updateURLParams();
    loadCatalog();
  }

  function handleFiltersChange() {
    currentPage.value = 1;
    updateURLParams();
    loadCatalog();
  }

  function removeFilter(key, value) {
    if (Array.isArray(filters.value[key])) {
      filters.value[key] = filters.value[key].filter(v => v !== value);
    } else {
      filters.value[key] = null;
    }
    handleFiltersChange();
  }

  function resetFilters() {
    filters.value = {
      type: null,
      status: null,
      translationStatus: null,
      ageRestriction: null,
      categories: [],
      tags: [],
      formats: [],
      authors: [],
      artists: [],
      publishers: [],
      teams: [],
      yearFrom: null,
      yearTo: null
    };
    handleFiltersChange();
  }

  function resetAllFilters() {
    searchQuery.value = '';
    resetFilters();
  }

  function applyQuickFilter(filterType) {
    if (activeQuickFilter.value === filterType) {
      activeQuickFilter.value = null;
      sortBy.value = 'updated';
    } else {
      activeQuickFilter.value = filterType;

      switch (filterType) {
        case 'trending':
          sortBy.value = 'popular';
          break;
        case 'popular':
          sortBy.value = 'rating';
          break;
        case 'new':
          sortBy.value = 'releaseDate';
          break;
        case 'updated':
          sortBy.value = 'updated';
          break;
      }
    }

    currentPage.value = 1;
    updateURLParams();
    loadCatalog();
  }

  function goToPage(page) {
    if (page < 1 || page > totalPages.value) return;
    currentPage.value = page;
    updateURLParams();
    loadCatalog();
    scrollToTop();
  }

  function handlePageChange(page) {
    goToPage(page);
  }

  function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function updateURLParams() {
    const query = {};

    if (searchQuery.value) query.search = searchQuery.value;
    if (currentPage.value > 1) query.page = currentPage.value;
    if (sortBy.value !== 'updated') query.sort = sortBy.value;

    // Add active filters to URL
    Object.entries(filters.value).forEach(([key, value]) => {
      if (value && (Array.isArray(value) ? value.length > 0 : true)) {
        query[key] = Array.isArray(value) ? value.join(',') : value;
      }
    });

    router.replace({ query });
  }

  function loadFromURLParams() {
    const query = route.query;

    if (query.search) searchQuery.value = query.search;
    if (query.page) currentPage.value = parseInt(query.page);
    if (query.sort) sortBy.value = query.sort;

    // Load filters from URL
    Object.keys(filters.value).forEach(key => {
      if (query[key]) {
        const value = query[key];
        filters.value[key] = Array.isArray(filters.value[key])
          ? value.split(',').map(v => parseInt(v) || v)
          : value;
      }
    });
  }

  // Resolves name-based URL params (sent by TitleDetails tags/categories)
  // e.g. ?category=Adventure&tag=Action&ageRestriction=18
  // Must be called AFTER filterOptions are loaded so names can be resolved to IDs.
  function resolveNameBasedURLParams() {
    const query = route.query;

    // ?category=Name  (singular, name-based) → resolve to ID → push into categories[]
    if (query.category) {
      const names = String(query.category).split(',').map(n => decodeURIComponent(n).trim().toLowerCase());
      const ids = names
        .map(name => filterOptions.value.Categories.find(c => c.name.toLowerCase() === name)?.id)
        .filter(id => id != null);
      if (ids.length) filters.value.categories = [...new Set([...filters.value.categories, ...ids])];
    }

    // ?tag=Name  (singular, name-based) → resolve to ID → push into tags[]
    if (query.tag) {
      const names = String(query.tag).split(',').map(n => decodeURIComponent(n).trim().toLowerCase());
      const ids = names
        .map(name => filterOptions.value.Tags.find(t => t.name.toLowerCase() === name)?.id)
        .filter(id => id != null);
      if (ids.length) filters.value.tags = [...new Set([...filters.value.tags, ...ids])];
    }

    // ?ageRestriction=18 — comes in as string, coerce to number
    if (query.ageRestriction && !filters.value.ageRestriction) {
      filters.value.ageRestriction = parseInt(query.ageRestriction) || null;
    }
  }

  // Lifecycle
  onMounted(async () => {
    loadFromURLParams();
    await loadFilterOptions();
    resolveNameBasedURLParams(); // must run after filterOptions — resolves ?category=Name → ID
    await loadCatalog();

  });

  // Watchers
  watch(sortBy, () => {
    currentPage.value = 1;
    updateURLParams();
    loadCatalog();
  });
</script>

<style>
  /* Global styles for portaled dropdown components from filter sheet */
  /* These dropdowns are rendered outside the sheet component, so need global styles */

  /* Sheet overlay should be below sheet content */
  [data-radix-dialog-overlay] {
    z-index: 999 !important;
  }

  /* Sheet content (filter panel) */
  [data-radix-dialog-content].filter-sheet {
    z-index: 1000 !important;
    background-color: var(--color-background) !important;
  }

  /* All portaled Radix UI dropdowns should be above the sheet */
  [data-radix-popper-content-wrapper],
  [data-radix-portal] {
    z-index: 1100 !important;
  }

  /* Select dropdown content */
  [data-radix-select-viewport],
  [data-radix-select-content],
  [data-radix-select-trigger] ~ [role="presentation"],
  div[style*="pointer-events"][data-radix-popper-content-wrapper] {
    z-index: 1100 !important;
  }

  /* Ensure dropdown backgrounds follow theme */
  [data-radix-select-content],
  [data-radix-select-viewport] {
    background-color: var(--color-background-soft) !important;
  }

  /* Popover content */
  [data-radix-popover-content] {
    background-color: var(--color-background-soft) !important;
    z-index: 1100 !important;
  }

    /* Command palette in popover */
    [data-radix-popover-content] [cmdk-root] {
      background-color: var(--color-background-soft) !important;
    }
</style>

<style scoped>
  /* Grid Transition */
  .grid-move,
  .grid-enter-active,
  .grid-leave-active {
    transition: all 0.3s ease;
  }

  .grid-enter-from {
    opacity: 0;
    transform: scale(0.95);
  }

  .grid-leave-to {
    opacity: 0;
    transform: scale(0.95);
  }

  .grid-leave-active {
    position: absolute;
  }

  /* List Transition */
  .list-move,
  .list-enter-active,
  .list-leave-active {
    transition: all 0.3s ease;
  }

  .list-enter-from {
    opacity: 0;
    transform: translateX(-20px);
  }

  .list-leave-to {
    opacity: 0;
    transform: translateX(20px);
  }

  /* Fade Transition */
  .fade-enter-active,
  .fade-leave-active {
    transition: opacity 0.3s ease;
  }

  .fade-enter-from,
  .fade-leave-to {
    opacity: 0;
  }

  /* Mobile Filter Button - Match Navbar Style */
  .mobile-filter-btn {
    display: flex;
    align-items: center;
    padding: 15px 20px;
    color: white;
    font-size: 15px;
    height: auto;
  }

    .mobile-filter-btn:hover {
      background-color: rgba(255, 255, 255, 0.1);
      color: white;
    }

  /* Filter Sheet - Black Background */
  :deep(.filter-sheet) {
    background-color: var(--color-background) !important;
    backdrop-filter: blur(20px) brightness(1.05);
    -webkit-backdrop-filter: blur(20px) brightness(1.05);
    box-shadow: -2px 0 10px rgba(0, 0, 0, 0.3);
  }

  /* Ensure dropdowns and popovers are visible above the sheet */
  :deep([role="dialog"]),
  :deep([data-radix-popper-content-wrapper]) {
    z-index: 1100 !important;
  }

  /* Keep dropdown content visible with proper background */
  :deep(.filter-sheet [role="listbox"]),
  :deep(.filter-sheet [role="menu"]),
  :deep([data-radix-select-content]),
  :deep([data-radix-popover-content]) {
    background-color: var(--color-background-soft) !important;
    z-index: 1100 !important;
  }

  /* Hide scrollbar for horizontal scrolling tabs */
  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }

    .scrollbar-hide::-webkit-scrollbar {
      display: none;
    }
</style>
