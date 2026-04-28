<template>
  <component :is="viewMode === 'grid' ? 'div' : Card"
             :class="[
      'group cursor-pointer transition-all duration-300',
      viewMode === 'grid'
        ? 'hover:scale-105 hover:shadow-xl'
        : 'hover:shadow-lg'
    ]"
             @click="navigateToTitle">
    <!-- Grid View -->
    <div v-if="viewMode === 'grid'" class="relative">
      <!-- Cover Image -->
      <div class="relative aspect-[2/3] overflow-hidden rounded-lg bg-muted">
        <img :src="coverUrl"
             :alt="title.originalTitle || title.englishTitle"
             class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-110"
             loading="lazy"
             @error="handleImageError" />

        <!-- Overlay Gradient -->
        <div class="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />

        <!-- Quick Info Overlay -->
        <div class="absolute inset-x-0 bottom-0 p-3 text-white transform translate-y-2 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-300">
          <div class="flex items-center gap-2 text-xs mb-2">
            <Badge variant="secondary" class="bg-primary/90">
              {{ typeDisplay }}
            </Badge>
            <Badge v-if="title.ageRestriction > 0" variant="destructive" class="bg-destructive/90">
              {{ ageDisplay }}
            </Badge>
          </div>

          <p class="text-xs line-clamp-3 opacity-90">
            {{ title.description || 'No description available.' }}
          </p>
        </div>

        <!-- Badges -->
        <div class="absolute top-2 left-2 flex flex-col gap-1">
          <Badge v-if="title.isFeatured" variant="default" class="shadow-lg">
            <Star class="h-3 w-3 mr-1 fill-current" />
            Featured
          </Badge>
          <Badge v-if="isNew" variant="secondary" class="shadow-lg">
            <Sparkles class="h-3 w-3 mr-1" />
            New
          </Badge>
        </div>

        <!-- Bookmark Button -->
        <Button variant="ghost"
                size="icon"
                :disabled="bookmarkLoading"
                class="absolute top-2 right-2 bg-background/80 backdrop-blur-sm hover:bg-background opacity-0 group-hover:opacity-100 transition-opacity"
                @click.stop="toggleBookmark">
          <Loader2 v-if="bookmarkLoading" class="h-4 w-4 animate-spin" />
          <Bookmark v-else :class="['h-4 w-4', isBookmarked ? 'fill-[var(--color-accent)] text-[var(--color-accent)]' : '']" />
        </Button>

        <!-- Rating -->
        <div v-if="title.averageRating"
             class="absolute bottom-2 right-2 flex items-center gap-1 bg-background/90 backdrop-blur-sm px-2 py-1 rounded-md text-xs font-medium">
          <Star class="h-3 w-3 fill-yellow-400 text-yellow-400" />
          {{ title.averageRating.toFixed(1) }}
        </div>
      </div>

      <!-- Title Info -->
      <div class="mt-3 space-y-1">
        <h3 class="font-semibold line-clamp-2 text-sm group-hover:text-primary transition-colors">
          {{ title.originalTitle || title.englishTitle }}
        </h3>

        <div class="flex items-center gap-2 text-xs text-muted-foreground">
          <span class="flex items-center gap-1">
            <BookOpen class="h-3 w-3" />
            {{ title.chapterCount || 0 }} chapters
          </span>

          <span class="flex items-center gap-1">
            <Eye class="h-3 w-3" />
            {{ formatViews(title.viewCount) }}
          </span>
        </div>

        <!-- Status Badge -->
        <div class="flex items-center gap-1">
          <Badge :variant="getStatusVariant(title.statusTitle)" class="text-xs">
            {{ statusDisplay }}
          </Badge>
          <Badge v-if="title.statusTranslation"
                 :variant="getStatusVariant(title.statusTranslation)"
                 class="text-xs">
            TL: {{ getStatusText(title.statusTranslation) }}
          </Badge>
        </div>
      </div>
    </div>

    <!-- List View -->
    <div v-else class="p-4">
      <div class="flex gap-4">
        <!-- Cover Image -->
        <div class="relative w-24 h-36 flex-shrink-0 overflow-hidden rounded-lg bg-muted">
          <img :src="coverUrl"
               :alt="title.originalTitle || title.englishTitle"
               class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-110"
               loading="lazy"
               @error="handleImageError" />

          <!-- Rating -->
          <div v-if="title.averageRating"
               class="absolute bottom-1 right-1 flex items-center gap-1 bg-background/90 backdrop-blur-sm px-1.5 py-0.5 rounded text-xs font-medium">
            <Star class="h-3 w-3 fill-yellow-400 text-yellow-400" />
            {{ title.averageRating.toFixed(1) }}
          </div>
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0 space-y-2">
          <!-- Title & Badges -->
          <div class="flex items-start justify-between gap-2">
            <div class="flex-1 min-w-0">
              <h3 class="font-semibold text-lg line-clamp-1 group-hover:text-primary transition-colors">
                {{ title.originalTitle || title.englishTitle }}
              </h3>
              <p v-if="title.englishTitle && title.originalTitle" class="text-sm text-muted-foreground line-clamp-1">
                {{ title.englishTitle }}
              </p>
            </div>

            <Button variant="ghost"
                    size="icon"
                    :disabled="bookmarkLoading"
                    @click.stop="toggleBookmark">
              <Loader2 v-if="bookmarkLoading" class="h-4 w-4 animate-spin" />
              <Bookmark v-else :class="['h-4 w-4', isBookmarked ? 'fill-[var(--color-accent)] text-[var(--color-accent)]' : '']" />
            </Button>
          </div>

          <!-- Badges -->
          <div class="flex flex-wrap items-center gap-1">
            <Badge variant="secondary">{{ typeDisplay }}</Badge>
            <Badge :variant="getStatusVariant(title.statusTitle)">
              {{ statusDisplay }}
            </Badge>
            <Badge v-if="title.statusTranslation"
                   :variant="getStatusVariant(title.statusTranslation)">
              TL: {{ getStatusText(title.statusTranslation) }}
            </Badge>
            <Badge v-if="title.ageRestriction > 0" variant="destructive">
              {{ ageDisplay }}
            </Badge>
            <Badge v-if="title.isFeatured">
              <Star class="h-3 w-3 mr-1 fill-current" />
              Featured
            </Badge>
          </div>

          <!-- Description -->
          <p class="text-sm text-muted-foreground line-clamp-2">
            {{ title.description || 'No description available.' }}
          </p>

          <!-- Meta Info -->
          <div class="flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
            <span class="flex items-center gap-1">
              <BookOpen class="h-3 w-3" />
              {{ title.chapterCount || 0 }} chapters
            </span>

            <span class="flex items-center gap-1">
              <Eye class="h-3 w-3" />
              {{ formatViews(title.viewCount) }} views
            </span>

            <span v-if="title.releaseDate" class="flex items-center gap-1">
              <Calendar class="h-3 w-3" />
              {{ formatYear(title.releaseDate) }}
            </span>

            <span v-if="title.lastUpdated" class="flex items-center gap-1">
              <Clock class="h-3 w-3" />
              Updated {{ formatRelativeTime(title.lastUpdated) }}
            </span>
          </div>

          <!-- Categories/Tags -->
          <div v-if="title.categories?.length || title.tags?.length" class="flex flex-wrap gap-1">
            <Badge v-for="category in title.categories?.slice(0, 3)"
                   :key="`cat-${category.id}`"
                   variant="outline"
                   class="text-xs">
              {{ category.name }}
            </Badge>
            <Badge v-for="tag in title.tags?.slice(0, 3)"
                   :key="`tag-${tag.id}`"
                   variant="outline"
                   class="text-xs">
              {{ tag.name }}
            </Badge>
            <Badge v-if="(title.categories?.length || 0) + (title.tags?.length || 0) > 6"
                   variant="outline"
                   class="text-xs">
              +{{ (title.categories?.length || 0) + (title.tags?.length || 0) - 6 }}
            </Badge>
          </div>
        </div>
      </div>
    </div>
  </component>
</template>

<script setup>
  import { ref, computed } from 'vue';
  import { useRouter } from 'vue-router';
  import {
    Star, Bookmark, BookOpen, Eye, Calendar, Clock, Sparkles, Loader2
  } from 'lucide-vue-next';

  import { Card } from '@/components/ui/card';
  import { Badge } from '@/components/ui/badge';
  import { Button } from '@/components/ui/button';
  import { buildTitleSlug } from '@/utils/titleSlug.js';

  import catalogService from '@/services/catalogService';
  import apiClient from '@/services/apiClient.js';

  const props = defineProps({
    title: {
      type: Object,
      required: true
    },
    viewMode: {
      type: String,
      default: 'grid',
      validator: (value) => ['grid', 'list'].includes(value)
    }
  });

  const router = useRouter();
  const isBookmarked = ref(false);
  const bookmarkLoading = ref(false);
  const currentBookmarkId = ref(null);
  const cachedFolders = ref([]);
  const imageError = ref(false);

  // Check auth from localStorage (same pattern as BookmarkDropdown)
  const isAuthenticated = computed(() => {
    try {
      return !!(localStorage.getItem('authToken') && localStorage.getItem('authUser'));
    } catch { return false; }
  });

  // Bookmark status is loaded lazily on first interaction — not on mount.
  // Loading GetFolders for every card on mount caused N+1 requests (1 per card × all visible cards).

  // Computed
  const coverUrl = computed(() => {
    if (imageError.value) return '/img/default-cover.png';
    return catalogService.getImageUrl(props.title.coverImagePath);
  });

  const typeDisplay = computed(() =>
    catalogService.getTypeDisplayName(props.title.type)
  );

  const statusDisplay = computed(() =>
    getStatusText(props.title.statusTitle)
  );

  const ageDisplay = computed(() =>
    catalogService.getAgeRestrictionDisplay(props.title.ageRestriction)
  );

  const isNew = computed(() => {
    if (!props.title.createdDate) return false;
    const daysSinceCreation = Math.floor(
      (new Date() - new Date(props.title.createdDate)) / (1000 * 60 * 60 * 24)
    );
    return daysSinceCreation <= 30;
  });

  // Methods
  function navigateToTitle() {
    // Use slug format: "title-name-{id}" e.g. "naruto-42"
    const slug = buildTitleSlug(props.title.originalTitle || props.title.englishTitle, props.title.id)
    router.push(`/${slug}`)
  }

  async function toggleBookmark(event) {
    event.stopPropagation();
    if (!isAuthenticated.value) {
      router.push(`/account/login?returnUrl=${encodeURIComponent(window.location.pathname)}`);
      return;
    }
    if (bookmarkLoading.value || !apiClient) return;
    bookmarkLoading.value = true;
    try {
      if (isBookmarked.value && currentBookmarkId.value) {
        // Remove from folder
        await apiClient.post('/Bookmarks/RemoveBookmark', { bookmarkId: currentBookmarkId.value });
        isBookmarked.value = false;
        currentBookmarkId.value = null;
      } else {
        // Use cached folders — no extra GetFolders call
        const folders = cachedFolders.value.length
          ? cachedFolders.value
          : (await apiClient.get(`/Bookmarks/GetFolders?titleId=${props.title.id}`)).data.folders ?? [];
        cachedFolders.value = folders;
        const targetFolder = folders.find(f => f.name === 'Reading') ?? folders[0];
        if (!targetFolder) return;
        // Add — response carries the new bookmark id
        const addRes = await apiClient.post('/Bookmarks/AddBookmark', {
          titleId: props.title.id,
          folderId: targetFolder.id
        });
        currentBookmarkId.value = addRes.data?.bookmarkId ?? addRes.data?.id ?? null;
        isBookmarked.value = true;
      }
    } catch (err) {
      console.error('Bookmark toggle failed:', err);
    } finally {
      bookmarkLoading.value = false;
    }
  }

  function handleImageError() {
    imageError.value = true;
  }

  function getStatusText(status) {
    const statuses = {
      'inproces': 'Ongoing',
      'completed': 'Completed',
      'frozen': 'Hiatus',
      'abandoned': 'Dropped'
    };
    return statuses[status] || status;
  }

  function getStatusVariant(status) {
    const variants = {
      'inproces': 'default',
      'completed': 'secondary',
      'frozen': 'outline',
      'abandoned': 'destructive'
    };
    return variants[status] || 'outline';
  }

  function formatViews(views) {
    if (!views) return '0';
    if (views >= 1000000) return `${(views / 1000000).toFixed(1)}M`;
    if (views >= 1000) return `${(views / 1000).toFixed(1)}K`;
    return views.toString();
  }

  function formatYear(date) {
    if (!date) return '';
    return new Date(date).getFullYear();
  }

  function formatRelativeTime(date) {
    if (!date) return '';

    const now = new Date();
    const then = new Date(date);
    const diffInSeconds = Math.floor((now - then) / 1000);

    if (diffInSeconds < 60) return 'just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)}d ago`;
    if (diffInSeconds < 31536000) return `${Math.floor(diffInSeconds / 2592000)}mo ago`;
    return `${Math.floor(diffInSeconds / 31536000)}y ago`;
  }
</script>
