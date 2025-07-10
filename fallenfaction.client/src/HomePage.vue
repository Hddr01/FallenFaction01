<template>
  <div class="home-page">
    <!-- Carousel Section -->
    <section class="content-section carousel-section">
      <div class="carousel-container"
           @mouseenter="showArrows = true"
           @mouseleave="showArrows = false">
        <!-- Left Arrow -->
        <div class="carousel-arrow carousel-arrow-left"
             :class="{ 'show': showArrows }"
             @click="scrollCarousel(-1)">
          <svg viewBox="0 0 320 512">
            <path fill="currentColor" d="M9.4 233.4c-12.5 12.5-12.5 32.8 0 45.3l192 192c12.5 12.5 32.8 12.5 45.3 0s12.5-32.8 0-45.3L77.3 256 246.6 86.6c12.5-12.5 12.5-32.8 0-45.3s-32.8-12.5-45.3 0l-192 192z"></path>
          </svg>
        </div>

        <!-- Right Arrow -->
        <div class="carousel-arrow carousel-arrow-right"
             :class="{ 'show': showArrows }"
             @click="scrollCarousel(1)">
          <svg viewBox="0 0 320 512">
            <path fill="currentColor" d="M310.6 233.4c12.5 12.5 12.5 32.8 0 45.3l-192 192c-12.5 12.5-32.8 12.5-45.3 0s-12.5-32.8 0-45.3L242.7 256 73.4 86.6c-12.5-12.5-12.5-32.8 0-45.3s32.8-12.5 45.3 0l192 192z"></path>
          </svg>
        </div>

        <!-- Scrollable Container -->
        <div class="carousel-scroll-container"
             ref="carouselContainer"
             @touchstart="handleTouchStart"
             @touchmove="handleTouchMove"
             @touchend="handleTouchEnd">
          <div v-for="manga in topTitles" :key="'carousel-' + manga.id" class="manga-carousel-card">
            <router-link :to="getTitleUrl(manga.originalTitle)" class="manga-link">
              <div class="manga-cover">
                <img :src="getImageUrl(manga.coverImagePath)"
                     :alt="manga.originalTitle"
                     @load="onImageLoad(manga.originalTitle, manga.coverImagePath)"
                     @error="onImageError(manga.originalTitle, manga.coverImagePath)" />
                <div v-if="manga.latestChapter" class="chapter-badge">
                  Chapter {{ manga.latestChapter }}
                </div>
              </div>
            </router-link>
            <router-link :to="getTitleUrl(manga.originalTitle)" class="manga-caption">
              <div class="manga-title">{{ manga.originalTitle }}</div>
              <div class="manga-type">{{ getMangaType(manga.type) }}</div>
            </router-link>
          </div>
        </div>
      </div>
    </section>

    <!-- Weekly Featured Section -->
    <h2 class="section-title">Weekly Featured</h2>
    <section class="content-section">
      <div v-if="loading.featured" class="loading-container">
        <div class="loading-spinner"></div>
        <span>Loading featured titles...</span>
      </div>
      <div v-else-if="error.featured" class="error-container">
        <p>{{ error.featured }}</p>
        <button @click="fetchFeaturedManga" class="retry-button">Try Again</button>
      </div>
      <div v-else class="manga-grid">
        <div v-for="manga in featuredManga" :key="'featured-' + manga.id" class="manga-card">
          <router-link :to="getTitleUrl(manga.originalTitle)" class="manga-link">
            <div class="manga-cover">
              <img :src="getImageUrl(manga.coverImagePath)"
                   :alt="manga.originalTitle"
                   @load="onImageLoad(manga.originalTitle, manga.coverImagePath)"
                   @error="onImageError(manga.originalTitle, manga.coverImagePath)" />
              <div v-if="manga.latestChapter" class="chapter-badge">
                {{ manga.latestChapter }}
              </div>
            </div>
            <div class="manga-info-below">
              <h3 class="manga-title">{{ manga.originalTitle }}</h3>
              <div class="manga-type">{{ getMangaType(manga.type) }}</div>
            </div>
          </router-link>
        </div>
      </div>
    </section>

    <!-- Top Users and Top Teams Row -->
    <div class="dual-section-container">
      <div class="dual-section-row">
        <!-- Top Users Section -->
        <div class="section-wrapper">
          <h2 class="section-title">Top Users</h2>
          <section class="content-section users-section">
            <div v-if="loading.users" class="loading-container-small">
              <div class="loading-spinner-small"></div>
              <span>Loading users...</span>
            </div>
            <div v-else-if="error.users" class="error-container-small">
              <p>{{ error.users }}</p>
              <button @click="fetchTopUsers" class="retry-button-small">Retry</button>
            </div>
            <div v-else class="users-grid">
              <div v-for="user in topUsers" :key="user.id" class="user-card">
                <div class="user-avatar">
                  <img :src="getImageUrl(user.avatar)"
                       :alt="user.name"
                       @load="onImageLoad(user.name, user.avatar)"
                       @error="onImageError(user.name, user.avatar)" />
                </div>
                <div class="user-info">
                  <div class="user-name">{{ user.name }}</div>
                  <div class="user-level">lvl {{ user.level }}</div>
                </div>
                <div class="user-score">{{ user.score }}</div>
              </div>
            </div>
          </section>
        </div>

        <!-- Top Teams Section -->
        <div class="section-wrapper">
          <h2 class="section-title">Top Teams</h2>
          <section class="content-section teams-section">
            <div v-if="loading.teams" class="loading-container-small">
              <div class="loading-spinner-small"></div>
              <span>Loading teams...</span>
            </div>
            <div v-else-if="error.teams" class="error-container-small">
              <p>{{ error.teams }}</p>
              <button @click="fetchTopTeams" class="retry-button-small">Retry</button>
            </div>
            <div v-else class="teams-grid">
              <div v-for="team in topTeams" :key="team.id" class="team-card">
                <div class="team-rank">#{{ team.id }}</div>
                <div class="team-avatar">
                  <img :src="getImageUrl(team.avatar)"
                       :alt="team.name"
                       @load="onImageLoad(team.name, team.avatar)"
                       @error="onImageError(team.name, team.avatar)" />
                </div>
                <div class="team-info">
                  <div class="team-name">{{ team.name }}</div>
                  <div class="team-level">lvl {{ team.level }}</div>
                </div>
                <div class="team-progress-container">
                  <div class="team-progress">
                    <div class="progress-bar" :style="{ width: team.progress + '%' }"></div>
                  </div>
                </div>
                <div class="team-score">{{ team.score }}</div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>

    <!-- Top Titles Section -->
    <h2 class="section-title">Top Titles</h2>
    <section class="content-section">
      <div v-if="loading.topTitles" class="loading-container">
        <div class="loading-spinner"></div>
        <span>Loading top titles...</span>
      </div>
      <div v-else-if="error.topTitles" class="error-container">
        <p>{{ error.topTitles }}</p>
        <button @click="fetchTopTitles" class="retry-button">Try Again</button>
      </div>
      <div v-else class="manga-grid large-grid">
        <div v-for="manga in topTitles" :key="'top-' + manga.id" class="manga-card">
          <router-link :to="getTitleUrl(manga.originalTitle)" class="manga-link">
            <div class="manga-cover">
              <img :src="getImageUrl(manga.coverImagePath)"
                   :alt="manga.originalTitle"
                   @load="onImageLoad(manga.originalTitle, manga.coverImagePath)"
                   @error="onImageError(manga.originalTitle, manga.coverImagePath)" />
              <div v-if="manga.latestChapter" class="chapter-badge">
                {{ manga.latestChapter }}
              </div>
            </div>
            <div class="manga-info-below">
              <h3 class="manga-title">{{ manga.originalTitle }}</h3>
              <div class="manga-type">{{ getMangaType(manga.type) }}</div>
            </div>
          </router-link>
        </div>
      </div>
    </section>

    <!-- Last Updates Section -->
    <h2 class="section-title">Last Updates</h2>
    <section class="content-section">
      <div v-if="loading.updates" class="loading-container">
        <div class="loading-spinner"></div>
        <span>Loading recent updates...</span>
      </div>
      <div v-else-if="error.updates" class="error-container">
        <p>{{ error.updates }}</p>
        <button @click="fetchLastUpdates" class="retry-button">Try Again</button>
      </div>
      <div v-else class="updates-list">
        <div v-for="update in lastUpdates" :key="update.id" class="update-card">
          <div class="update-cover">
            <img :src="getImageUrl(update.coverImagePath)"
                 :alt="update.originalTitle"
                 @load="onImageLoad(update.originalTitle, update.coverImagePath)"
                 @error="onImageError(update.originalTitle, update.coverImagePath)" />
          </div>
          <div class="update-info">
            <h4 class="update-title">{{ update.originalTitle }}</h4>
            <p class="update-description">{{ update.description }}</p>
            <div class="update-meta">
              <span class="update-team">{{ update.teamName }}</span>
            </div>
          </div>
          <div class="update-time">{{ update.timeAgo }}</div>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
  import { ref, reactive, computed, onMounted, nextTick, onUnmounted } from 'vue';
  import { homepageService } from './services/homepageService';

  export default {
    name: 'HomePage',
    setup() {
      // Reactive data
      const featuredManga = ref([]);
      const topUsers = ref([]);
      const lastUpdates = ref([]);
      const topTeams = ref([]);
      const topTitles = ref([]);

      // Loading states
      const loading = reactive({
        featured: false,
        users: false,
        teams: false,
        topTitles: false,
        updates: false
      });

      // Error states
      const error = reactive({
        featured: '',
        users: '',
        teams: '',
        topTitles: '',
        updates: ''
      });

      // Carousel state
      const showArrows = ref(false);
      const carouselContainer = ref(null);
      const isScrolling = ref(false);
      const isManualScrolling = ref(false);
      const scrollTimeout = ref(null);
      const screenWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200);

      // Carousel configuration
      const carouselConfig = {
        itemsPerSlide: 7,
        tabletItemsPerSlide: 4,
        mobileItemsPerSlide: 4,
        autoplayInterval: 5000,
        enableLoop: true,
        enableAutoplay: false
      };

      // Computed properties
      const currentItemsPerSlide = computed(() => {
        if (screenWidth.value <= 480) {
          return 2;
        } else if (screenWidth.value <= 768) {
          return carouselConfig.mobileItemsPerSlide;
        } else if (screenWidth.value <= 1200) {
          return carouselConfig.tabletItemsPerSlide;
        } else {
          return carouselConfig.itemsPerSlide;
        }
      });

      // Image URL helper methods
      const getImageBaseUrl = () => {
        const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7217/api';
        return apiBaseUrl.replace('/api', ''); // Remove /api to get base server URL
      };


      const getImageUrl = (imagePath) => {
        if (!imagePath) {
          console.log('No image path provided, using fallback');
          return `${getImageBaseUrl()}/img/default-avatar.png`;
        }

        // Check if the path is already a full URL
        if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
          console.log('🖼️ Using full URL as-is:', imagePath);
          return imagePath;
        }

        // Check if it's a relative path that starts with /
        const baseUrl = getImageBaseUrl();
        const fullUrl = imagePath.startsWith('/')
          ? `${baseUrl}${imagePath}`
          : `${baseUrl}/${imagePath}`;

        console.log('🖼️ Constructed image URL:', fullUrl);
        return fullUrl;
      };

      // Debug image loading events
      const onImageLoad = (title, path) => {
        console.log('✅ Image loaded successfully:', title);
      };

      const onImageError = (title, path) => {
        console.error('❌ Image failed to load:', title);
        console.error('Failed path:', path);
        console.error('Full URL that failed:', getImageUrl(path));
      };

      // API methods
      const fetchFeaturedManga = async () => {
        loading.featured = true;
        error.featured = '';
        try {
          console.log('🚀 Fetching featured manga...');
          const result = await homepageService.getFeaturedManga();

          if (result.success) {
            featuredManga.value = result.data;

            // Enhanced debugging
            console.log('=== FEATURED MANGA DEBUG ===');
            console.log('✅ API Base URL:', import.meta.env.VITE_API_BASE_URL);
            console.log('✅ Image Base URL:', getImageBaseUrl());
            console.log('✅ Items received:', result.data.length);

            if (result.data.length > 0) {
              const firstManga = result.data[0];
              console.log('📖 First manga:', firstManga.originalTitle || firstManga.englishTitle);
              console.log('🖼️ Cover path:', firstManga.coverImagePath);
              console.log('🌐 Full image URL:', getImageUrl(firstManga.coverImagePath));

              // Image accessibility will be verified by the img onLoad/onError events
            }

            console.log('============================');
          } else {
            error.featured = result.error;
            console.error('❌ API error:', result.error);
          }
        } catch (err) {
          error.featured = 'Failed to load featured manga';
          console.error('❌ Fetch error:', err);
        } finally {
          loading.featured = false;
        }
      };

      const fetchTopUsers = async () => {
        loading.users = true;
        error.users = '';
        try {
          const result = await homepageService.getTopUsers();
          if (result.success) {
            topUsers.value = result.data;
          } else {
            error.users = result.error;
          }
        } catch (err) {
          error.users = 'Failed to load top users';
        } finally {
          loading.users = false;
        }
      };

      const fetchLastUpdates = async () => {
        loading.updates = true;
        error.updates = '';
        try {
          const result = await homepageService.getRecentUpdates();
          if (result.success) {
            lastUpdates.value = result.data;
          } else {
            error.updates = result.error;
          }
        } catch (err) {
          error.updates = 'Failed to load recent updates';
        } finally {
          loading.updates = false;
        }
      };

      const fetchTopTeams = async () => {
        loading.teams = true;
        error.teams = '';
        try {
          const result = await homepageService.getTopTeams();
          if (result.success) {
            topTeams.value = result.data;
          } else {
            error.teams = result.error;
          }
        } catch (err) {
          error.teams = 'Failed to load top teams';
        } finally {
          loading.teams = false;
        }
      };

      const fetchTopTitles = async () => {
        loading.topTitles = true;
        error.topTitles = '';
        try {
          const result = await homepageService.getPopularTitles();
          if (result.success) {
            topTitles.value = result.data;
          } else {
            error.topTitles = result.error;
          }
        } catch (err) {
          error.topTitles = 'Failed to load top titles';
        } finally {
          loading.topTitles = false;
        }
      };

      // Carousel methods
      const handleResize = () => {
        if (typeof window !== 'undefined') {
          screenWidth.value = window.innerWidth;
        }
      };

      const scrollCarousel = (direction) => {
        const container = carouselContainer.value;
        if (!container) return;

        isManualScrolling.value = true;
        if (scrollTimeout.value) {
          clearTimeout(scrollTimeout.value);
        }

        const cardWidth = 151;
        const scrollAmount = cardWidth * 3;

        if (direction === -1) {
          const currentScroll = container.scrollLeft;
          if (currentScroll <= scrollAmount) {
            container.scrollTo({ left: 0, behavior: 'smooth' });
          } else {
            container.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
          }
        } else {
          const maxScroll = container.scrollWidth - container.clientWidth;
          const currentScroll = container.scrollLeft;
          if (currentScroll + scrollAmount >= maxScroll) {
            container.scrollTo({ left: maxScroll, behavior: 'smooth' });
          } else {
            container.scrollBy({ left: scrollAmount, behavior: 'smooth' });
          }
        }

        setTimeout(() => {
          isManualScrolling.value = false;
        }, 500);
      };

      const handleTouchStart = () => {
        isScrolling.value = true;
        isManualScrolling.value = false;
        if (scrollTimeout.value) {
          clearTimeout(scrollTimeout.value);
        }
      };

      const handleTouchMove = () => {
        isScrolling.value = true;
        if (scrollTimeout.value) {
          clearTimeout(scrollTimeout.value);
        }
      };

      const handleTouchEnd = () => {
        scrollTimeout.value = setTimeout(() => {
          isScrolling.value = false;
          if (!isManualScrolling.value) {
            snapToNearestCard();
          }
        }, 150);
      };

      const handleScroll = () => {
        if (isManualScrolling.value) return;

        if (scrollTimeout.value) {
          clearTimeout(scrollTimeout.value);
        }

        scrollTimeout.value = setTimeout(() => {
          if (!isScrolling.value && !isManualScrolling.value) {
            snapToNearestCard();
          }
        }, 150);
      };

      const snapToNearestCard = () => {
        const container = carouselContainer.value;
        if (!container) return;

        const cardWidth = screenWidth.value <= 480 ? 132 : 151;
        const scrollLeft = container.scrollLeft;
        const containerWidth = container.clientWidth;
        const maxScroll = container.scrollWidth - containerWidth;

        if (scrollLeft <= 5) {
          container.scrollTo({ left: 0, behavior: 'smooth' });
          return;
        }

        if (scrollLeft >= maxScroll - 5) {
          container.scrollTo({ left: maxScroll, behavior: 'smooth' });
          return;
        }

        const visibleCardIndex = Math.round(scrollLeft / cardWidth);
        const targetScrollLeft = visibleCardIndex * cardWidth;

        if (Math.abs(scrollLeft - targetScrollLeft) > 10) {
          container.scrollTo({
            left: Math.max(0, Math.min(targetScrollLeft, maxScroll)),
            behavior: 'smooth'
          });
        }
      };

      // Helper methods
      const getMangaType = (type) => {
        const types = {
          0: 'Manga',
          1: 'Manhwa',
          2: 'Manhua',
          3: 'Comic',
          4: 'Novel'
        };
        return types[type] || 'Manga';
      };

      const getTitleUrl = (originalTitle) => {
        return `/${encodeURIComponent(originalTitle)}`;
      };

      // Lifecycle hooks
      onMounted(async () => {
        // Add resize listener
        if (typeof window !== 'undefined') {
          window.addEventListener('resize', handleResize);
        }

        // Add scroll listener
        await nextTick();
        const container = carouselContainer.value;
        if (container) {
          container.addEventListener('scroll', handleScroll, { passive: true });
        }

        // Fetch all data
        await Promise.all([
          fetchFeaturedManga(),
          fetchTopUsers(),
          fetchLastUpdates(),
          fetchTopTeams(),
          fetchTopTitles()
        ]);
      });

      onUnmounted(() => {
        if (typeof window !== 'undefined') {
          window.removeEventListener('resize', handleResize);
        }

        const container = carouselContainer.value;
        if (container) {
          container.removeEventListener('scroll', handleScroll);
        }

        if (scrollTimeout.value) {
          clearTimeout(scrollTimeout.value);
        }
      });

      return {
        // Data
        featuredManga,
        topUsers,
        lastUpdates,
        topTeams,
        topTitles,
        loading,
        error,

        // Carousel
        showArrows,
        carouselContainer,
        currentItemsPerSlide,

        // Methods
        fetchFeaturedManga,
        fetchTopUsers,
        fetchLastUpdates,
        fetchTopTeams,
        fetchTopTitles,
        scrollCarousel,
        handleTouchStart,
        handleTouchMove,
        handleTouchEnd,
        getMangaType,
        getTitleUrl,

        // Image methods
        getImageUrl,
        onImageLoad,
        onImageError
      };
    }
  };
</script>

<style scoped>
  .home-page {
    background-color: var(--color-background);
    color: var(--color-text);
    padding: 2rem 0 0 0;
    min-height: 100vh;
    outline: none;
    border: none;
  }

  /* Loading and Error States */
  .loading-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    color: var(--color-text);
  }

  .loading-container-small {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    color: var(--color-text);
  }

  .loading-spinner {
    width: 40px;
    height: 40px;
    border: 4px solid var(--color-border);
    border-top: 4px solid var(--color-text);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 1rem;
  }

  .loading-spinner-small {
    width: 24px;
    height: 24px;
    border: 3px solid var(--color-border);
    border-top: 3px solid var(--color-text);
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 0.5rem;
  }

  @keyframes spin {
    0% {
      transform: rotate(0deg);
    }

    100% {
      transform: rotate(360deg);
    }
  }

  .error-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    color: var(--color-text);
    text-align: center;
  }

  .error-container-small {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    color: var(--color-text);
    text-align: center;
  }

  .retry-button,
  .retry-button-small {
    margin-top: 1rem;
    padding: 0.5rem 1rem;
    background-color: var(--color-accent, #8865fc);
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    transition: background-color 0.2s;
  }

    .retry-button:hover,
    .retry-button-small:hover {
      background-color: var(--color-accent-hover, #7c59d9);
    }

  /* Carousel Styling */
  .carousel-section {
    margin-bottom: 0;
  }

  .carousel-container {
    position: relative;
    background-color: var(--color-background-soft);
    user-select: none;
    min-height: 273px;
  }

  .carousel-scroll-container {
    display: flex;
    gap: 16px;
    padding: 12px 16px;
    overflow-x: scroll;
    overflow-y: hidden;
    scroll-behavior: smooth;
    scrollbar-width: none;
    -ms-overflow-style: none;
    -webkit-overflow-scrolling: touch;
    will-change: scroll-position;
    touch-action: pan-x;
    overscroll-behavior-x: contain;
    scroll-snap-type: x proximity;
  }

    .carousel-scroll-container::-webkit-scrollbar {
      display: none;
    }

  .carousel-arrow {
    position: absolute;
    top: 50%;
    margin-top: -21px;
    width: 42px;
    height: 42px;
    border-radius: 50%;
    background: var(--color-background-soft);
    box-shadow: rgba(0, 0, 0, 0.12) 0 1px 3px;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    z-index: 10;
    opacity: 0;
    transition: all 0.3s ease;
    color: var(--color-text);
  }

    .carousel-arrow.show {
      opacity: 1;
    }

    .carousel-arrow:hover {
      box-shadow: rgba(0, 0, 0, 0.16) 0 3px 8px;
    }

  .carousel-arrow-left {
    left: 15px;
  }

    .carousel-arrow-left:hover {
      transform: translateX(-3px);
    }

  .carousel-arrow-right {
    right: 15px;
  }

    .carousel-arrow-right:hover {
      transform: translateX(3px);
    }

  .carousel-arrow svg {
    width: 16px;
    height: 16px;
  }

  .manga-carousel-card {
    min-width: 135px;
    width: 135px;
    flex-shrink: 0;
    display: block;
    scroll-snap-align: center;
    transition: transform 0.2s ease;
  }

    .manga-carousel-card .manga-cover {
      position: relative;
      padding-top: 140%;
      max-width: 100%;
      border-radius: 6px;
      overflow: hidden;
      box-shadow: rgba(0, 0, 0, 0.12) 0 1px 3px;
      background-color: var(--color-background-mute);
    }

      .manga-carousel-card .manga-cover img {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        object-fit: cover;
        border-radius: inherit;
        opacity: 0;
        transition: opacity 0.15s ease-in;
      }

        .manga-carousel-card .manga-cover img[src] {
          opacity: 1;
        }

  .chapter-badge {
    position: absolute;
    bottom: 6px;
    left: 6px;
    z-index: 3;
    background: rgba(0, 0, 0, 0.7);
    color: white;
    padding: 3px 6px;
    border-radius: 4px;
    font-size: 12px;
    white-space: nowrap;
    width: auto;
    display: inline-block;
  }

  .manga-caption {
    display: block;
    padding-top: 6px;
    max-height: 60px;
    overflow: hidden;
    color: var(--color-text);
    text-decoration: none;
  }

    .manga-caption:hover {
      text-decoration: none;
    }

    .manga-caption .manga-title {
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
      word-break: break-word;
      hyphens: auto;
      line-height: 18px;
      font-weight: 600;
      font-size: 0.9rem;
      margin: 0 0 2px 0;
      color: var(--color-text);
    }

    .manga-caption .manga-type {
      font-size: 13px;
      color: #999;
      display: flex;
      gap: 10px;
    }

  .manga-link {
    text-decoration: none;
    color: inherit;
    display: block;
  }

  .manga-info-below {
    padding-top: 6px;
    max-height: 60px;
    overflow: hidden;
    color: var(--color-text);
  }

    .manga-info-below .manga-title {
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
      word-break: break-word;
      hyphens: auto;
      line-height: 18px;
      font-weight: 600;
      font-size: 0.9rem;
      margin: 0 0 2px 0;
      color: var(--color-text);
    }

    .manga-info-below .manga-type {
      font-size: 13px;
      color: #999;
      display: flex;
      gap: 10px;
    }

  /* Section Styling */
  .content-section {
    background-color: var(--color-background-soft);
    border-radius: 12px;
    margin-bottom: 2rem;
    overflow: hidden;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    border: 1px solid var(--color-border);
  }

  .section-title {
    font-size: 1.5rem;
    font-weight: 600;
    color: var(--color-heading);
    margin: 0 0 1rem 0;
  }

  /* Dual Section Container and Row */
  .dual-section-container {
    margin-bottom: 2rem;
  }

  .dual-section-row {
    display: grid;
    grid-template-columns: 1fr 2fr;
    gap: 1rem;
  }

  .section-wrapper {
    display: flex;
    flex-direction: column;
  }

    .section-wrapper .section-title {
      margin-bottom: 1rem;
    }

    .section-wrapper .content-section {
      margin-bottom: 0;
      flex: 1;
    }

  /* Manga Grid */
  .manga-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, 135px);
    gap: 1.5rem;
    padding: 2rem;
    justify-content: center;
  }

  .large-grid {
    grid-template-columns: repeat(auto-fill, 135px);
    gap: 2rem;
  }

  .manga-card {
    background: transparent;
    border-radius: 12px;
    overflow: visible;
    transition: all 0.3s ease;
    width: 135px;
  }

    .manga-card:hover {
      transform: translateY(-8px);
    }

    .manga-card .manga-cover {
      position: relative;
      padding-top: 140%;
      width: 135px;
      overflow: hidden;
      border-radius: 6px;
      box-shadow: rgba(0, 0, 0, 0.12) 0 1px 3px;
      background-color: var(--color-background-mute);
    }

      .manga-card .manga-cover img {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        object-fit: cover;
        border-radius: inherit;
        opacity: 0;
        transition: opacity 0.15s ease-in, transform 0.3s ease;
      }

        .manga-card .manga-cover img[src] {
          opacity: 1;
        }

    .manga-card:hover .manga-cover img {
      transform: scale(1.05);
    }

    .manga-card:hover .manga-cover {
      box-shadow: 0 8px 25px rgba(0, 0, 0, 0.2);
    }

  /* Users Section */
  .users-section {
    max-height: 600px;
  }

  .users-grid {
    padding: 1rem;
    max-height: 500px;
    overflow-y: auto;
  }

  .user-card {
    display: flex;
    align-items: center;
    padding: 0.75rem;
    margin-bottom: 0.5rem;
    background-color: var(--color-background-mute);
    border-radius: 8px;
    transition: background-color 0.2s;
  }

    .user-card:hover {
      background-color: var(--color-background);
    }

  .user-avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    overflow: hidden;
    margin-right: 1rem;
    flex-shrink: 0;
  }

    .user-avatar img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

  .user-info {
    flex: 1;
  }

  .user-name {
    font-size: 0.9rem;
    font-weight: 500;
    color: var(--color-text);
    margin-bottom: 0.1rem;
  }

  .user-level {
    font-size: 0.8rem;
    color: #999;
  }

  .user-score {
    font-size: 0.85rem;
    color: var(--color-text);
    font-weight: 500;
  }

  /* Teams Section */
  .teams-section {
    max-height: 600px;
  }

  .teams-grid {
    padding: 1rem;
    max-height: 500px;
    overflow-y: auto;
  }

  .team-card {
    display: flex;
    align-items: center;
    padding: 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-background-mute);
    border-radius: 10px;
    transition: background-color 0.2s;
  }

    .team-card:hover {
      background-color: var(--color-background);
    }

  .team-rank {
    font-size: 1.2rem;
    font-weight: bold;
    color: var(--color-text);
    margin-right: 1rem;
    min-width: 30px;
  }

  .team-avatar {
    width: 45px;
    height: 45px;
    border-radius: 50%;
    overflow: hidden;
    margin-right: 1rem;
    flex-shrink: 0;
  }

    .team-avatar img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

  .team-info {
    margin-right: 1rem;
  }

  .team-name {
    font-size: 1rem;
    font-weight: 500;
    color: var(--color-text);
    margin-bottom: 0.1rem;
  }

  .team-level {
    font-size: 0.8rem;
    color: #999;
  }

  .team-progress-container {
    flex: 1;
    margin: 0 1rem;
  }

  .team-progress {
    height: 8px;
    background-color: var(--color-border);
    border-radius: 4px;
    overflow: hidden;
  }

  .progress-bar {
    height: 100%;
    background: linear-gradient(90deg, #8865fc, #a78bfa);
    border-radius: 4px;
    transition: width 0.3s ease;
  }

  .team-score {
    font-size: 0.9rem;
    color: var(--color-text);
    font-weight: 500;
    min-width: 70px;
    text-align: right;
  }

  /* Updates Section */
  .updates-list {
    padding: 2rem;
    max-height: none;
    overflow-y: visible;
  }

  .update-card {
    display: flex;
    padding: 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-background-mute);
    border-radius: 10px;
    transition: background-color 0.2s;
  }

    .update-card:hover {
      background-color: var(--color-background);
    }

  .update-cover {
    width: 135px;
    height: 189px;
    border-radius: 6px;
    overflow: hidden;
    margin-right: 1rem;
    flex-shrink: 0;
  }

    .update-cover img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

  .update-info {
    flex: 1;
    display: flex;
    flex-direction: column;
  }

  .update-title {
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-text);
    margin: 0 0 0.5rem 0;
  }

  .update-description {
    font-size: 0.85rem;
    color: #bbb;
    line-height: 1.4;
    margin: 0 0 0.5rem 0;
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .update-meta {
    margin-top: auto;
  }

  .update-team {
    font-size: 0.8rem;
    color: #8865fc;
    font-weight: 500;
  }

  .update-time {
    font-size: 0.8rem;
    color: #999;
    text-align: right;
    width: 80px;
    flex-shrink: 0;
    align-self: flex-start;
  }

  /* Responsive Design */
  @media (max-width: 1200px) {
    .dual-section-row {
      grid-template-columns: 1fr;
      gap: 1rem;
    }

    .manga-grid {
      grid-template-columns: repeat(auto-fill, 135px);
      gap: 1rem;
    }

    .large-grid {
      grid-template-columns: repeat(auto-fill, 135px);
      gap: 1.5rem;
    }
  }

  @media (max-width: 768px) {
    .home-page {
      padding: 1rem 0.5rem 0 0.5rem;
    }

    .manga-grid, .large-grid {
      grid-template-columns: repeat(auto-fill, 135px);
      gap: 1rem;
      padding: 1rem;
      justify-content: space-around;
    }

    .users-grid, .updates-list, .teams-grid {
      padding: 1rem;
    }

    .dual-section-row {
      gap: 0.5rem;
    }

    .update-cover {
      width: 120px;
      height: 168px;
    }

    .carousel-arrow-left {
      left: 10px;
    }

    .carousel-arrow-right {
      right: 10px;
    }
  }

  @media (max-width: 480px) {
    .manga-grid, .large-grid {
      grid-template-columns: repeat(auto-fill, 120px);
      gap: 0.75rem;
    }

    .manga-card {
      width: 120px;
    }

      .manga-card .manga-cover {
        width: 120px;
        padding-top: 140%;
      }

    .update-cover {
      width: 110px;
      height: 154px;
    }

    .carousel-arrow-left {
      left: 5px;
    }

    .carousel-arrow-right {
      right: 5px;
    }
  }
</style>
