<template>
  <!-- Single root so carousel + body share one themed background (avoids seam / dark line) -->
  <div class="home-page-root">
  <!-- Carousel Section -->
  <section class="carousel-section">
    <Carousel v-if="topTitles && topTitles.length > 0"
              :opts="{
            align: 'start',
            loop: true,
            dragFree: true,
            containScroll: false,
            watchDrag: true,
            skipSnaps: true,
            friction: 0.3,
            dragThreshold: 15,
          }"
              class="w-full">
      <CarouselContent class="-ml-4">
        <CarouselItem v-for="manga in topTitles"
                      :key="'carousel-' + manga.id"
                      class="pl-4 basis-2/5 sm:basis-1/3 md:basis-1/4 lg:basis-1/5 xl:basis-1/11">
          <router-link :to="getTitleUrl(manga.originalTitle, manga.id)" class="manga-link">
            <div class="manga-cover-container">
              <img :src="getImageUrl(manga.coverImagePath)"
                   :alt="manga.originalTitle"
                   class="manga-cover-img"
                   @load="onImageLoad(manga.originalTitle, manga.coverImagePath)"
                   @error="onImageError($event, manga.originalTitle, manga.coverImagePath)" />
              <div v-if="chapterBadgeText(manga)" class="chapter-badge">
                {{ chapterBadgeText(manga) }}
              </div>
            </div>
            <div class="carousel-manga-info">
              <div class="carousel-manga-title">{{ manga.englishTitle || manga.originalTitle }}</div>
              <div class="carousel-manga-type">{{ getMangaType(manga.type) }}</div>
              <div v-if="manga.lastUpdated" class="carousel-manga-updated">
                {{ formatTimeAgo(manga.lastUpdated) }}
              </div>
            </div>
          </router-link>
        </CarouselItem>
      </CarouselContent>
    </Carousel>
    <div v-else class="loading-container">
      <Spinner class="size-10 text-primary" />
    </div>
  </section>
  <div class="home-page">

    <div class="main-container">


      <!-- Weekly Featured Section -->
      <section class="content-section">
        <h2 class="section-title">Weekly Featured</h2>
        <div v-if="loading.featured" class="loading-container">
          <Spinner class="size-10 text-primary" />
        </div>
        <div v-else-if="error.featured" class="error-container">
          <p>{{ error.featured }}</p>
          <button @click="fetchFeaturedManga" class="retry-button">Try Again</button>
        </div>
        <div v-else class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-7 2xl:grid-cols-9 gap-4">
          <TitleCard v-for="manga in featuredManga"
                     :key="'featured-' + manga.id"
                     :title="manga"
                     view-mode="grid" />
        </div>
      </section>

      <!-- Top Users and Top Teams Row -->
      <div class="dual-section-container">
        <div class="dual-section-row">
          <!-- Top Users Section -->
          <div class="section-wrapper">
            <h2 class="section-title">Top Users</h2>
            <section class="sub-section users-section">
              <div v-if="loading.users" class="loading-container-small">
                <Spinner class="size-6 text-primary" />
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
                         @error="onImageError($event, user.name, user.avatar)" />
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
            <section class="sub-section teams-section">
              <div v-if="loading.teams" class="loading-container-small">
                <Spinner class="size-6 text-primary" />
              </div>
              <div v-else-if="error.teams" class="error-container-small">
                <p>{{ error.teams }}</p>
                <button @click="fetchTopTeams" class="retry-button-small">Retry</button>
              </div>
              <div v-else-if="topTeams && topTeams.length > 0" class="teams-grid">
                <div v-for="(team, teamIndex) in topTeams" :key="team.id" class="team-card">
                  <div class="team-rank">#{{ teamIndex + 1 }}</div>
                  <div class="team-avatar">
                    <img :src="getImageUrl(team.avatar)"
                         :alt="team.name"
                         @load="onImageLoad(team.name, team.avatar)"
                         @error="onImageError($event, team.name, team.avatar)" />
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
              <div v-else class="empty-state">
                <p>No teams yet</p>
              </div>
            </section>
          </div>
        </div>
      </div>

      <!-- Top Titles Section -->
      <section class="content-section">
        <h2 class="section-title">Top Titles</h2>
        <div v-if="loading.topTitles" class="loading-container">
          <Spinner class="size-10 text-primary" />
        </div>
        <div v-else-if="error.topTitles" class="error-container">
          <p>{{ error.topTitles }}</p>
          <button @click="fetchTopTitles" class="retry-button">Try Again</button>
        </div>
        <div v-else class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-7 2xl:grid-cols-9 gap-4">
          <TitleCard v-for="manga in topTitles"
                     :key="'top-' + manga.id"
                     :title="manga"
                     view-mode="grid" />
        </div>
      </section>

      <!-- Last Updates Section -->
      <section class="content-section">
        <h2 class="section-title">Last Updates</h2>
        <div v-if="loading.updates" class="loading-container">
          <Spinner class="size-10 text-primary" />
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
                   @error="onImageError($event, update.originalTitle, update.coverImagePath)" />
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
  </div>
  </div>
</template>

<script>
  import { ref, reactive, onMounted } from 'vue';
  import { homepageService } from './services/homepageService';
  import {
    Carousel,
    CarouselContent,
    CarouselItem,
    CarouselNext,
    CarouselPrevious,
  } from '@/components/ui/carousel';

  import { Spinner } from '@/components/ui/spinner';
  import TitleCard from '@/components/catalog/TitleCard.vue';
  import { buildTitleSlug } from '@/utils/titleSlug.js';

  export default {
    name: 'HomePage',
    components: {
      Carousel,
      CarouselContent,
      CarouselItem,
      CarouselNext,
      CarouselPrevious,
      Spinner,
      TitleCard,
    },
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

      // Helper functions
      const getMangaType = (type) => {
        const types = {
          0: 'Manga',
          1: 'Manhwa',
          2: 'Manhua',
          3: 'Comic',
          4: 'Novel'
        };
        return types[type] || 'Unknown';
      };

      const formatTimeAgo = (dateStr) => {
        if (!dateStr) return '';
        const diff = Date.now() - new Date(dateStr).getTime();
        const mins = Math.floor(diff / 60000);
        if (mins < 1) return 'just now';
        if (mins < 60) return `${mins}m ago`;
        const h = Math.floor(mins / 60);
        if (h < 24) return `${h}h ago`;
        const d = Math.floor(h / 24);
        if (d < 7) return `${d}d ago`;
        return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      };

      const getImageUrl = (path) => {
        if (!path) return '/img/default-cover.png';
        if (path.startsWith('http://') || path.startsWith('https://')) {
          return path;
        }
        return path.startsWith('/') ? path : `/${path}`;
      };

      const getTitleUrl = (titleName, id) => {
        if (id) return `/${buildTitleSlug(titleName, id)}`;
        return `/${encodeURIComponent(titleName)}`;
      };

      /** Carousel badge: use latestChapterNumber first (authoritative), then latestChapter string. */
      const chapterBadgeText = (manga) => {
        const n = manga?.latestChapterNumber;
        if (n != null && n !== '' && !Number.isNaN(Number(n)) && Number(n) > 0) {
          const num = Number(n);
          return Number.isInteger(num) || Math.abs(num - Math.round(num)) < 1e-6
            ? `Ch. ${Math.round(num)}`
            : `Ch. ${num}`;
        }
        const s = manga?.latestChapter;
        if (s != null && String(s).trim() !== '' && String(s) !== 'No chapters') {
          return String(s);
        }
        return '';
      };

      const onImageLoad = (title, path) => {
        console.log(`Image loaded successfully: ${title}`);
      };

      const onImageError = (event, title, path) => {
        console.error(`Failed to load image for: ${title}`, path);
        const target = event.target;
        // Avoid infinite loop if the fallback itself fails
        if (!target.src.includes("default-cover.png") && !target.src.includes("default-avatar.png")) {
          const isCover = !path || path.includes("/covers/") || path.includes("coverImage");
          target.src = isCover ? "/img/default-cover.png" : "/img/default-avatar.png";
        }
      };

      // Data fetching functions
      const fetchFeaturedManga = async () => {
        loading.featured = true;
        error.featured = '';
        try {
          const response = await homepageService.getFeaturedManga();
          if (response.success) {
            featuredManga.value = response.data || [];
          } else {
            error.featured = response.error || 'Failed to load featured manga';
          }
        } catch (err) {
          console.error('Error fetching featured manga:', err);
          error.featured = 'Failed to load featured manga. Please try again.';
        } finally {
          loading.featured = false;
        }
      };

      const fetchTopUsers = async () => {
        loading.users = true;
        error.users = '';
        try {
          const response = await homepageService.getTopUsers();
          if (response.success) {
            topUsers.value = response.data || [];
          } else {
            error.users = response.error || 'Failed to load top users';
          }
        } catch (err) {
          console.error('Error fetching top users:', err);
          error.users = 'Failed to load top users.';
        } finally {
          loading.users = false;
        }
      };

      const fetchTopTeams = async () => {
        loading.teams = true;
        error.teams = '';
        try {
          const response = await homepageService.getTopTeams();
          if (response.success) {
            topTeams.value = response.data || [];
          } else {
            error.teams = response.error || 'Failed to load top teams';
          }
        } catch (err) {
          console.error('Error fetching top teams:', err);
          error.teams = 'Failed to load top teams.';
          topTeams.value = [];
        } finally {
          loading.teams = false;
        }
      };

      const fetchTopTitles = async () => {
        loading.topTitles = true;
        error.topTitles = '';
        try {
          const response = await homepageService.getPopularTitles();
          if (response.success) {
            topTitles.value = response.data || [];
          } else {
            error.topTitles = response.error || 'Failed to load top titles';
          }
        } catch (err) {
          console.error('Error fetching top titles:', err);
          error.topTitles = 'Failed to load top titles.';
        } finally {
          loading.topTitles = false;
        }
      };

      const fetchLastUpdates = async () => {
        loading.updates = true;
        error.updates = '';
        try {
          const response = await homepageService.getRecentUpdates();
          if (response.success) {
            lastUpdates.value = response.data || [];
          } else {
            error.updates = response.error || 'Failed to load recent updates';
          }
        } catch (err) {
          console.error('Error fetching last updates:', err);
          error.updates = 'Failed to load recent updates.';
        } finally {
          loading.updates = false;
        }
      };

      // Initialize on mount
      onMounted(async () => {
        await Promise.all([
          fetchFeaturedManga(),
          fetchTopUsers(),
          fetchTopTeams(),
          fetchTopTitles(),
          fetchLastUpdates()
        ]);
      });

      return {
        featuredManga,
        topUsers,
        lastUpdates,
        topTeams,
        topTitles,
        loading,
        error,
        getMangaType,
        formatTimeAgo,
        getImageUrl,
        getTitleUrl,
        chapterBadgeText,
        onImageLoad,
        onImageError,
        fetchFeaturedManga,
        fetchTopUsers,
        fetchTopTeams,
        fetchTopTitles,
        fetchLastUpdates
      };
    }
  };
</script>

<style scoped>
  .home-page-root {
    width: 100%;
    max-width: 100vw;
    background-color: var(--color-background);
    overflow-x: hidden;
  }

  .home-page {
    width: 100%;
    max-width: 100vw;
    padding: 0.5rem 0 0 0;
    background-color: transparent;
    overflow-x: hidden;
  }

  /* Main container card */
  .main-container {
    border-color: transparent;
  }

  /* Content sections inside the main card */
  .content-section {
    margin-bottom: 1.5rem;
    padding-bottom: 1.5rem;
    border-bottom: 1px solid var(--color-border);
  }

    .content-section:last-child {
      border-bottom: none;
      margin-bottom: 0;
      padding-bottom: 0;
    }

  /* Sub-sections for users/teams */
  .sub-section {
    padding: 0;
    border: none;
    background: transparent;
  }

  .section-title {
    font-size: 1.5rem;
    font-weight: 600;
    margin: 0 0 1rem 0;
    color: var(--color-heading);
  }

  .carousel-section {
    margin-bottom: 0;
    padding: 1rem 0 0.75rem 0;
    overflow: visible;
    background-color: transparent;
    border-bottom: none;
    box-shadow: none;
  }

  .manga-cover-container {
    position: relative;
    width: 100%;
    aspect-ratio: 3/4;
    overflow: hidden;
    border-radius: 8px;
  }

  .manga-cover-img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 0.3s ease;
  }

  .manga-link:hover .manga-cover-img {
    transform: scale(1.05);
  }

  .carousel-manga-info {
    margin-top: 0.5rem;
  }

  .carousel-manga-title {
    font-size: 0.85rem;
    font-weight: 500;
    color: var(--color-text);
    margin-bottom: 0.25rem;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .carousel-manga-updated {
    font-size: 11px;
    color: var(--color-text);
    opacity: 0.55;
    margin-top: 2px;
  }

  .carousel-manga-type {
    font-size: 0.75rem;
    color: var(--color-text);
    opacity: 0.55;
    text-transform: uppercase;
  }

  .chapter-badge {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    background: linear-gradient(to top, color-mix(in srgb, var(--color-heading) 88%, transparent), transparent);
    color: #fff;
    padding: 0.5rem 0.35rem;
    font-size: 0.75rem;
    font-weight: 600;
    text-align: center;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  /* Loading States */
  .loading-container, .error-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 3rem;
    gap: 1rem;
  }

  .loading-container-small, .error-container-small {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    gap: 0.5rem;
  }

  .retry-button {
    padding: 0.5rem 1rem;
    background-color: hsl(142.1 70.6% 45.3%);
    color: white;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    font-weight: 500;
    transition: background-color 0.2s;
  }

    .retry-button:hover {
      background-color: hsl(142.1 76.2% 36.3%);
    }

  .retry-button-small {
    padding: 0.4rem 0.8rem;
    background-color: hsl(142.1 70.6% 45.3%);
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.85rem;
    transition: background-color 0.2s;
  }

    .retry-button-small:hover {
      background-color: hsl(142.1 76.2% 36.3%);
    }

  .empty-state {
    padding: 2rem;
    text-align: center;
    color: var(--color-text);
    opacity: 0.55;
  }

  /* Manga Grid */
  .manga-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, 135px);
    gap: 1.5rem;
    justify-content: center;
  }

  .large-grid {
    gap: 2rem;
  }

  .manga-card {
    width: 135px;
    transition: transform 0.2s;
  }

    .manga-card:hover {
      transform: translateY(-4px);
    }

  .manga-link {
    text-decoration: none;
    color: inherit;
    display: block;
  }

  .manga-cover {
    position: relative;
    width: 135px;
    height: 189px;
    border-radius: 6px;
    overflow: hidden;
  }

    .manga-cover img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.3s ease;
    }

  .manga-link:hover .manga-cover img {
    transform: scale(1.05);
  }

  .manga-info-below {
    margin-top: 0.5rem;
  }

  .manga-title {
    font-size: 0.85rem;
    font-weight: 500;
    color: var(--color-text);
    margin-bottom: 0.25rem;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .manga-type {
    font-size: 0.75rem;
    color: var(--color-text);
    opacity: 0.55;
    text-transform: uppercase;
  }

  /* Dual Section Container */
  .dual-section-container {
    width: 100%;
    margin-bottom: 1.5rem;
    padding-bottom: 1.5rem;
    border-bottom: 1px solid var(--color-border);
  }

  .dual-section-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
  }

  .section-wrapper {
    display: flex;
    flex-direction: column;
  }

  /* Users & Teams Sections */
  .users-section, .teams-section {
    max-height: 600px;
  }

  .users-grid, .teams-grid {
    max-height: 500px;
    overflow-y: auto;
  }

  /* User Card */
  .user-card {
    display: flex;
    align-items: center;
    padding: 0.75rem;
    margin-bottom: 0.75rem;
    background-color: var(--color-input-bg);
    border: 1px solid var(--color-border);
    border-radius: 8px;
    transition: background-color 0.2s;
  }

    .user-card:hover {
      background-color: var(--color-input-bg-hover);
    }

  .user-avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    overflow: hidden;
    margin-right: 0.75rem;
    flex-shrink: 0;
  }

    .user-avatar img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

  .user-info {
    flex: 1;
    margin-right: 0.5rem;
  }

  .user-name {
    font-size: 0.9rem;
    font-weight: 500;
    color: var(--color-text);
  }

  .user-level {
    font-size: 0.8rem;
    color: var(--color-text);
    opacity: 0.55;
  }

  .user-score {
    font-size: 0.85rem;
    color: var(--color-text);
    font-weight: 500;
  }

  /* Team Card */
  .team-card {
    display: flex;
    align-items: center;
    padding: 1rem;
    margin-bottom: 1rem;
    background-color: var(--color-input-bg);
    border: 1px solid var(--color-border);
    border-radius: 10px;
    transition: background-color 0.2s;
  }

    .team-card:hover {
      background-color: var(--color-input-bg-hover);
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
  }

  .team-level {
    font-size: 0.8rem;
    color: var(--color-text);
    opacity: 0.55;
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
    background: linear-gradient(90deg, hsl(142.1 70.6% 45.3%), hsl(142.1 76.2% 36.3%));
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
    padding: 0;
  }

  .update-card {
    display: flex;
    padding: 0.75rem 0;
    margin-bottom: 0.5rem;
    border-bottom: 1px solid var(--color-border);
    transition: background-color 0.2s;
  }

    .update-card:hover {
      background-color: transparent;
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
    color: var(--color-text);
    opacity: 0.75;
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
    color: hsl(142.1 70.6% 45.3%);
    font-weight: 500;
  }

  .update-time {
    font-size: 0.8rem;
    color: var(--color-text);
    opacity: 0.55;
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
      gap: 1rem;
    }

    .large-grid {
      gap: 1.5rem;
    }
  }

  @media (max-width: 768px) {
    .home-page {
      padding: 1rem 0.5rem 0 0.5rem;
    }

    .manga-grid, .large-grid {
      gap: 1rem;
      justify-content: space-around;
    }

    .dual-section-row {
      gap: 0.5rem;
    }

    .update-cover {
      width: 120px;
      height: 168px;
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

    .manga-cover {
      width: 120px;
      height: 168px;
    }

    .update-cover {
      width: 110px;
      height: 154px;
    }
  }
</style>
