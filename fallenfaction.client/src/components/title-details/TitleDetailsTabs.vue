<template>
  <div class="space-y-6 h-auto">
    <!-- ANIMATED: Horizontal Tab Navigation with motion-v -->
    <div class="border-b border-gray-700 bg-background rounded-t-lg overflow-hidden shadow-lg">
      <div class="flex w-full overflow-x-auto whitespace-nowrap bg-transparent p-1.5 gap-1 scrollbar-hide">
        <a v-for="tab in tabs"
           :key="tab.key"
           class="flex-shrink-0 block cursor-pointer bg-transparent rounded-md transition-all duration-300 ease-out no-underline text-gray-400 relative overflow-visible hover:text-gray-100 hover:-translate-y-0.5"
           :class="{ 'text-purple-500': activeTab === tab.key }"
           href="#"
           @click.prevent="switchTab(tab.key)">
          <div class="px-4 py-2 relative block font-semibold text-sm tracking-wide z-10">
            {{ tab.title }}
            <i v-if="loading && activeTab === tab.key" class="fas fa-spinner fa-spin fa-sm ml-2"></i>

            <!-- Animated background indicator -->
            <Motion v-if="activeTab === tab.key"
                    layout-id="tab-indicator"
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

    <!-- Tab Content -->
    <div class="mt-6 tab-content-container">
      <!-- About Title Tab -->
      <div v-show="activeTab === 'info'" class="tab-content-panel">
        <Motion :initial="{ opacity: 0 }"
                :animate="{ opacity: 1 }"
                :transition="{ duration: 0.2 }">
          <template v-if="tabData.info.loaded">
            <!-- Description -->
            <div class="mb-6 px-2">
              <div class="text-base leading-relaxed">
                <div class="transition-all duration-300 ease-in-out text-gray-100 break-words"
                     :class="descriptionExpanded ? '' : 'line-clamp-3'">
                  {{ titleData.description }}
                </div>
              </div>
              <button v-if="titleData.description && titleData.description.length > 200"
                      class="mt-2 text-sm text-purple-500 hover:text-purple-400 transition-colors"
                      type="button"
                      @click="toggleDescription">
                {{ descriptionExpanded ? 'Read less...' : 'Read more...' }}
              </button>
            </div>

            <!-- Tags/Categories -->
            <div class="flex flex-wrap gap-2 mb-6 px-2">
              <!-- Age Restriction Button - Always dark red background -->
              <Button v-if="titleData.ageRestriction > 0"
                      variant="outline"
                      size="sm"
                      as-child
                      class="border-red-800 text-red-100 bg-red-900 hover:bg-red-800 hover:text-white hover:border-red-700 dark:border-red-700 dark:text-red-100 dark:bg-red-900 dark:hover:bg-red-800 dark:hover:text-white transition-all duration-300">
                <a :href="`/catalog?ageRestriction=${titleData.ageRestriction}`">
                  {{ titleData.ageRestriction }}+
                </a>
              </Button>

              <!-- Category Buttons -->
              <Button v-for="(category, index) in titleData.categories"
                      :key="`category-${index}`"
                      variant="outline"
                      size="sm"
                      as-child
                      class="transition-all duration-300 bg-[#141414] hover:-translate-y-0.5">
                <a :href="`/catalog?category=${encodeURIComponent(category)}`">
                  {{ category }}
                </a>
              </Button>

              <!-- Tag Buttons -->
              <Button v-for="(tag, index) in visibleTags"
                      :key="`tag-${index}`"
                      variant="outline"
                      size="sm"
                      as-child
                      class="transition-all duration-300 bg-[#141414] hover:-translate-y-0.5">
                <a :href="`/catalog?tag=${encodeURIComponent(tag)}`">
                  {{ tag }}
                </a>
              </Button>

              <!-- Show More/Less Button -->
              <Button v-if="titleData.tags && titleData.tags.length > 10"
                      variant="outline"
                      size="sm"
                      class="transition-all duration-300"
                      @click="showAllTags = !showAllTags">
                {{ showAllTags ? 'Show less' : `+${titleData.tags.length - 10} more` }}
              </Button>
            </div>

            <!-- Translators Section - CAROUSEL VERSION -->
            <div v-if="titleData.teams && titleData.teams.length > 0" class="px-2 mt-8">
              <div class="mb-4">
                <h2 class="text-lg font-semibold text-gray-100">Translators</h2>
              </div>

              <!-- Carousel for Translators -->
              <Carousel :opts="{
                        align: 'start' ,
                        loop: titleData.teams.length>
                4,
                dragFree: true,
                containScroll: 'trimSnaps',
                watchDrag: true,
                skipSnaps: false,
                }"
                class="w-full">
                <CarouselContent class="-ml-4">
                  <CarouselItem v-for="team in titleData.teams"
                                :key="team.id"
                                class="pl-4 basis-auto">
                    <Button variant="outline"
                            as-child
                            class="bg-[#141414] h-auto p-0 overflow-hidden transition-all duration-300 hover:-translate-y-1 hover:shadow-lg">
                      <a :href="`/team/${team.id}`"
                         class="flex flex-col items-center w-32">
                        <!-- Avatar Section -->
                        <div class="w-full aspect-square bg-muted flex items-center justify-center p-4">
                          <Avatar class="w-20 h-20">
                            <AvatarImage v-if="team.avatarImagePath"
                                         :src="getImageUrl(team.avatarImagePath)"
                                         :alt="team.name" />
                            <AvatarFallback class="text-2xl font-bold">
                              {{ team.name?.substring(0, 2).toUpperCase() || 'TE' }}
                            </AvatarFallback>
                          </Avatar>
                        </div>

                        <!-- Team Name -->
                        <div class="w-full p-3 bg-muted/50">
                          <div class="text-sm font-medium text-center truncate">
                            {{ team.name }}
                          </div>
                        </div>
                      </a>
                    </Button>
                  </CarouselItem>
                </CarouselContent>

                <!-- Optional: Navigation Arrows (uncomment if you want them) -->
                <!-- <CarouselPrevious class="left-2" />
                <CarouselNext class="right-2" /> -->
              </Carousel>
            </div>

            <!-- Statistics Section -->
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-8 px-2">
              <!-- Rating Statistics Section - FIXED: 10-grade system -->
              <div>
                <Card class="w-full">
                  <CardHeader class="border-b-0 pb-3">
                    <CardTitle class="text-lg">Rating Statistics</CardTitle>
                    <CardDescription>
                      Based on {{ ratingStatsTotalCount }} {{ ratingStatsTotalCount === 1 ? 'review' : 'reviews' }}
                    </CardDescription>
                  </CardHeader>
                  <CardContent class="space-y-2 pt-0">
                    <!-- Loading State -->
                    <div v-if="loadingRatingStats" class="flex items-center justify-center py-8">
                      <i class="fas fa-spinner fa-spin text-2xl text-muted-foreground"></i>
                    </div>

                    <!-- Rating Distribution - 10 grades -->
                    <template v-else-if="ratingStatsDistribution.length > 0">
                      <div v-for="item in ratingStatsDistribution"
                           :key="item.value"
                           class="flex items-center justify-between gap-3">
                        <div class="flex items-center gap-2.5 min-w-[60px]">
                          <span class="w-6 text-sm font-medium text-right">{{ item.value }}</span>
                          <Star class="h-4 w-4 fill-yellow-400 text-yellow-400" />
                        </div>

                        <!-- Progress bar -->
                        <div class="flex-1 h-2 bg-muted rounded-full overflow-hidden max-w-[200px]">
                          <div class="h-full bg-yellow-400 transition-all duration-300 rounded-full"
                               :style="`width: ${item.percentage}%`"></div>
                        </div>

                        <div class="flex items-center gap-1 text-sm text-muted-foreground min-w-[70px] justify-end">
                          <span>{{ item.count }}</span>
                          <span>({{ item.percentage.toFixed(1) }}%)</span>
                        </div>
                      </div>
                    </template>

                    <!-- Empty State -->
                    <div v-else class="text-center py-6 text-muted-foreground">
                      <StarIcon class="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p class="text-sm">No ratings yet</p>
                    </div>

                    <!-- Rate Button - Dialog Implementation -->
                    <div class="pt-2">
                      <Dialog v-if="isAuthenticated" v-model:open="isRatingDialogOpen">
                        <DialogTrigger as-child>
                          <Button size="sm"
                                  variant="outline"
                                  class="w-full">
                            <StarIcon class="w-4 h-4 mr-1" />
                            Rate this Title
                          </Button>
                        </DialogTrigger>
                        <DialogContent class="sm:max-w-md bg-black border-white/20">
                          <DialogHeader>
                            <DialogTitle class="text-[var(--color-white)] text-center">Rate this title</DialogTitle>
                            <DialogDescription class="text-center text-muted-foreground">
                              Share your rating with the community
                            </DialogDescription>
                          </DialogHeader>

                          <div class="p-4 space-y-6">
                            <!-- Star Rating (10 stars) -->
                            <div class="flex justify-center space-x-2">
                              <button v-for="star in 10"
                                      :key="star"
                                      type="button"
                                      @click="setRating(star)"
                                      @mouseover="hoverRating = star"
                                      @mouseleave="hoverRating = 0"
                                      class="w-8 h-8 transition-all duration-200 transform hover:scale-110"
                                      :class="{
                                      'text-yellow-400' : star <= (hoverRating || selectedRating),
                            'text-gray-600': star > (hoverRating || selectedRating)
                          }">
                    <svg class="w-full h-full" fill="currentColor" viewBox="0 0 20 20">
                      <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                    </svg>
                  </button>
                            </div>

                            <!-- Rating Text -->
                            <p class="text-white font-medium text-center min-h-6">
                              {{ getRatingText(hoverRating || selectedRating) }}
                            </p>
                          </div>

                          <DialogFooter class="sm:justify-center gap-2">
                            <Button type="button"
                                    variant="outline"
                                    class="bg-[#141414] ring-[0.3px] ring-white hover:text-white"
                                    @click="isRatingDialogOpen = false">
                              Cancel
                            </Button>
                            <Button type="button"
                                    @click="submitRating"
                                    :disabled="!selectedRating || submittingRating"
                                    class="justify-center bg-[#e6e6e6] text-black hover:bg-gray-100 hover:text-black">
                              <span v-if="submittingRating" class="inline-flex items-center">
                                <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-black" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                                </svg>
                                Submitting...
                              </span>
                              <span v-else>Submit Rating</span>
                            </Button>
                          </DialogFooter>
                        </DialogContent>
                      </Dialog>

                      <Button v-else
                              size="sm"
                              variant="outline"
                              class="w-full"
                              @click="emit('go-to-login')">
                        <StarIcon class="w-4 h-4 mr-1" />
                        Sign in to Rate
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              </div>

              <!-- Bookmarks Section -->
              <div>
                <Card class="w-full">
                  <CardHeader class="border-b-0 pb-3">
                    <CardTitle class="text-lg">Reading Lists</CardTitle>
                    <CardDescription>
                      {{ bookmarkStatsTotalCount }} {{ bookmarkStatsTotalCount === 1 ? 'person has' : 'people have' }} this in their lists
                    </CardDescription>
                  </CardHeader>
                  <CardContent class="space-y-3 pt-0">
                    <!-- Loading State -->
                    <div v-if="loadingBookmarkStats" class="flex items-center justify-center py-8">
                      <i class="fas fa-spinner fa-spin text-2xl text-muted-foreground"></i>
                    </div>

                    <!-- Bookmark Distribution -->
                    <template v-else-if="bookmarkStatsDistribution && bookmarkStatsDistribution.length > 0">
                      <div v-for="stat in bookmarkStatsDistribution"
                           :key="stat.folderId"
                           class="flex items-center justify-between gap-3">
                        <div class="flex items-center gap-2.5">
                          <component :is="getStatusIcon(stat.folderName)"
                                     class="h-4 w-4"
                                     :class="getStatusColor(stat.folderName)" />
                          <span class="text-sm font-medium">{{ stat.folderName }}</span>
                        </div>
                        <div class="flex items-center gap-2">
                          <!-- Progress bar -->
                          <div class="w-32 h-2 bg-muted rounded-full overflow-hidden">
                            <div class="h-full transition-all duration-300 rounded-full"
                                 :class="getStatusBgColor(stat.folderName)"
                                 :style="`width: ${stat.percentage}%`"></div>
                          </div>
                          <div class="flex items-center gap-0.5 text-sm text-muted-foreground min-w-[70px] justify-end">
                            <span>{{ stat.count }}</span>
                            <span>({{ stat.percentage.toFixed(1) }}%)</span>
                          </div>
                        </div>
                      </div>
                    </template>

                    <!-- Empty State -->
                    <div v-else class="text-center py-6 text-muted-foreground">
                      <BookmarkIcon class="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p class="text-sm">No one has bookmarked this yet</p>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </div>
          </template>

          <div v-else-if="tabData.info.error" class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 bg-gray-900 rounded-b-lg">
            <i class="fas fa-exclamation-triangle text-6xl mb-8 text-red-500"></i>
            <p class="text-base mb-4">{{ tabData.info.error }}</p>
            <button @click="loadTabContent('info')" class="px-4 py-2 bg-transparent border border-gray-600 rounded text-gray-300 hover:bg-gray-800 transition-colors">Retry</button>
          </div>

          <div v-else class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 bg-gray-900 rounded-b-lg">
            <i class="fas fa-spinner fa-spin text-6xl mb-8 text-purple-500"></i>
            <p class="text-base mb-4">Loading title information...</p>
          </div>
        </Motion>
      </div>

      <!-- Chapters Tab -->
      <div v-show="activeTab === 'chapters'" class="tab-content-panel">
        <Motion :initial="{ opacity: 0 }"
                :animate="{ opacity: 1 }"
                :transition="{ duration: 0.2 }">
          <template v-if="tabData.chapters.loaded">
            <div v-if="!titleData.areChapterCommentsEnabled" class="flex items-center gap-3 p-4 mb-3 rounded-lg bg-yellow-500/10 border border-yellow-500/30 text-yellow-500 font-medium">
              <i class="fas fa-comment-slash text-xl"></i>
              Comments have been disabled for chapters of this title.
            </div>

            <ChaptersComponent :chapters="tabData.chapters.data"
                               :title-name="titleData.originalTitle" />
          </template>

          <div v-else-if="tabData.chapters.error" class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 bg-gray-900 rounded-b-lg">
            <i class="fas fa-exclamation-triangle text-6xl mb-8 text-red-500"></i>
            <p class="text-base mb-4">{{ tabData.chapters.error }}</p>
            <button @click="loadTabContent('chapters')" class="px-4 py-2 bg-transparent border border-gray-600 rounded text-gray-300 hover:bg-gray-800 transition-colors">Retry</button>
          </div>

          <div v-else class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 bg-gray-900 rounded-b-lg">
            <i class="fas fa-spinner fa-spin text-6xl mb-8 text-purple-500"></i>
            <p class="text-base mb-4">Loading chapters...</p>
          </div>
        </Motion>
      </div>

      <!-- Comments Tab -->
      <div v-show="activeTab === 'comments'" class="tab-content-panel">
        <Motion :initial="{ opacity: 0 }"
                :animate="{ opacity: 1 }"
                :transition="{ duration: 0.2 }">
          <template v-if="tabData.comments.loaded">
            <div>
              <CommentsComponent :comments="tabData.comments.data"
                                 :target-id="titleId"
                                 target-type="1"
                                 :is-authenticated="isAuthenticated"
                                 @comments-updated="onCommentsUpdated" />
            </div>
          </template>

          <div v-else-if="tabData.comments.error" class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 bg-gray-900 rounded-b-lg">
            <i class="fas fa-exclamation-triangle text-6xl mb-8 text-red-500"></i>
            <p class="text-base mb-4">{{ tabData.comments.error }}</p>
            <button @click="loadTabContent('comments')" class="px-4 py-2 bg-transparent border border-gray-600 rounded text-gray-300 hover:bg-gray-800 transition-colors">Retry</button>
          </div>

          <div v-else class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 bg-gray-900 rounded-b-lg">
            <i class="fas fa-spinner fa-spin text-6xl mb-8 text-purple-500"></i>
            <p class="text-base mb-4">Loading comments...</p>
          </div>
        </Motion>
      </div>

      <!-- Art Tab -->
      <div v-show="activeTab === 'art'" class="tab-content-panel">
        <Motion :initial="{ opacity: 0 }"
                :animate="{ opacity: 1 }"
                :transition="{ duration: 0.2 }">
          <div class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 rounded-b-lg">
            <i class="fas fa-palette text-6xl mb-8 text-purple-500 opacity-70"></i>
            <h3 class="mb-4 text-gray-100 text-2xl font-semibold">Art Gallery</h3>
            <p class="text-base opacity-80 max-w-md leading-6">Fan art and official artwork will be displayed here soon!</p>
          </div>
        </Motion>
      </div>

      <!-- Related Tab -->
      <div v-show="activeTab === 'related'" class="tab-content-panel">
        <Motion :initial="{ opacity: 0 }"
                :animate="{ opacity: 1 }"
                :transition="{ duration: 0.2 }">
          <div class="flex flex-col items-center justify-center py-20 px-5 text-center text-gray-400 rounded-b-lg">
            <i class="fas fa-link text-6xl mb-8 text-purple-500 opacity-70"></i>
            <h3 class="mb-4 text-gray-100 text-2xl font-semibold">Related Titles</h3>
            <p class="text-base opacity-80 max-w-md leading-6">Discover similar manga and related series here!</p>
          </div>
        </Motion>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, watch, onMounted, nextTick } from 'vue'
  import { Motion } from 'motion-v'
  import ChaptersComponent from './ChaptersComponent.vue'
  import CommentsComponent from './CommentsComponent.vue'
  import {
    Star,
    StarIcon,
    BookmarkIcon,
    BookOpen,
    CheckCircle2,
    PauseCircle,
    Clock,
    XCircle
  } from 'lucide-vue-next'
  import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
  import { Button } from '@/components/ui/button'
  import {
    Carousel,
    CarouselContent,
    CarouselItem,
    CarouselNext,
    CarouselPrevious,
  } from '@/components/ui/carousel'
  import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
  import Autoplay from 'embla-carousel-autoplay'
  import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
  } from '@/components/ui/dialog'
  import { titleDetailsService } from '@/services/titleDetailsService'

  const props = defineProps({
    titleId: {
      type: Number,
      required: true
    },
    titleData: {
      type: Object,
      required: true
    },
    isAuthenticated: {
      type: Boolean,
      default: false
    },
    initialTab: {
      type: String,
      default: 'info'
    },
    titleCommentsDisabled: {
      type: Boolean,
      default: false
    }
  })

  const emit = defineEmits(['tab-changed', 'comments-updated', 'show-rating-modal', 'go-to-login'])

  // Helper function to get correct image URLs
  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5064'

  const getImageUrl = (path) => {
    if (!path) return ''
    if (path.startsWith('http')) return path
    // Remove /api from base URL for static files
    const baseUrl = API_BASE_URL.replace('/api', '')
    return `${baseUrl}${path}`
  }

  // Tab state
  const activeTab = ref(props.initialTab)
  const loading = ref(false)

  // Rating Dialog State
  const isRatingDialogOpen = ref(false)
  const selectedRating = ref(0)
  const hoverRating = ref(0)
  const submittingRating = ref(false)

  // Tabs configuration
  const tabs = ref([
    { key: 'info', title: 'About Title' },
    { key: 'chapters', title: 'Chapters' },
    { key: 'comments', title: 'Comments' },
    { key: 'art', title: 'Art' },
    { key: 'related', title: 'Related' }
  ])

  // Tab data state
  const tabData = ref({
    info: { loaded: false, data: null, error: null },
    chapters: { loaded: false, data: [], error: null },
    comments: { loaded: false, data: [], error: null },
    art: { loaded: false, data: [], error: null },
    related: { loaded: false, data: [], error: null }
  })

  // UI state
  const descriptionExpanded = ref(false)
  const showAllTags = ref(false)
  const showLeftTranslatorButton = ref(false)
  const showRightTranslatorButton = ref(true)
  const translatorsContainer = ref(null)

  // Carousel plugin setup
  const carouselPlugin = Autoplay({
    delay: 4000,
    stopOnInteraction: true,
    stopOnMouseEnter: true
  })

  // ============================================================================
  // RATING STATISTICS - FIXED: 10-grade system with API loading
  // ============================================================================
  const loadingRatingStats = ref(false)
  const ratingStatsData = ref(null)

  const ratingStatsTotalCount = computed(() => {
    return ratingStatsData.value?.totalRatings || 0
  })

  const ratingStatsDistribution = computed(() => {
    if (!ratingStatsData.value?.distribution) return []

    // Return distribution in descending order (10 to 1)
    return ratingStatsData.value.distribution
      .sort((a, b) => b.value - a.value)
  })

  const loadRatingStats = async () => {
    if (!props.titleId) return

    try {
      loadingRatingStats.value = true
      console.log('Loading rating stats for title ID:', props.titleId)

      const result = await titleDetailsService.getRatingStats(props.titleId)

      if (result.success && result.data) {
        ratingStatsData.value = result.data
        console.log('Rating stats loaded:', result.data)
      } else {
        console.error('Failed to load rating stats:', result.error)
        ratingStatsData.value = {
          totalRatings: 0,
          averageRating: 0,
          distribution: []
        }
      }
    } catch (error) {
      console.error('Error loading rating stats:', error)
      ratingStatsData.value = {
        totalRatings: 0,
        averageRating: 0,
        distribution: []
      }
    } finally {
      loadingRatingStats.value = false
    }
  }

  // ============================================================================
  // BOOKMARK STATISTICS - FIXED: Load from API
  // ============================================================================
  const loadingBookmarkStats = ref(false)
  const bookmarkStatsData = ref(null)

  const bookmarkStatsTotalCount = computed(() => {
    return bookmarkStatsData.value?.totalBookmarks || 0
  })

  const bookmarkStatsDistribution = computed(() => {
    if (!bookmarkStatsData.value?.folderDistribution) return []

    // Calculate percentages and sort by count
    const total = bookmarkStatsTotalCount.value
    return bookmarkStatsData.value.folderDistribution
      .map(folder => ({
        folderId: folder.folderId,
        folderName: folder.folderName,
        count: folder.count,
        percentage: total > 0 ? (folder.count / total) * 100 : 0
      }))
      .sort((a, b) => b.count - a.count)
  })

  const loadBookmarkStats = async () => {
    if (!props.titleId) return

    try {
      loadingBookmarkStats.value = true
      console.log('Loading bookmark stats for title ID:', props.titleId)

      const result = await titleDetailsService.getBookmarkStats(props.titleId)

      if (result.success && result.data) {
        bookmarkStatsData.value = result.data
        console.log('Bookmark stats loaded:', result.data)
      } else {
        console.error('Failed to load bookmark stats:', result.error)
        bookmarkStatsData.value = {
          totalBookmarks: 0,
          folderDistribution: []
        }
      }
    } catch (error) {
      console.error('Error loading bookmark stats:', error)
      bookmarkStatsData.value = {
        totalBookmarks: 0,
        folderDistribution: []
      }
    } finally {
      loadingBookmarkStats.value = false
    }
  }

  // Other Computed
  const visibleTags = computed(() => {
    if (!props.titleData.tags || !Array.isArray(props.titleData.tags)) return []
    return showAllTags.value ? props.titleData.tags : props.titleData.tags.slice(0, 10)
  })

  // Bookmark status helpers
  const getStatusIcon = (folderName) => {
    const iconMap = {
      'Reading': BookOpen,
      'Completed': CheckCircle2,
      'On Hold': PauseCircle,
      'Plan to Read': Clock,
      'Dropped': XCircle
    }
    return iconMap[folderName] || BookmarkIcon
  }

  const getStatusColor = (folderName) => {
    const colorMap = {
      'Reading': 'text-blue-500',
      'Completed': 'text-green-500',
      'On Hold': 'text-yellow-500',
      'Plan to Read': 'text-purple-500',
      'Dropped': 'text-red-500'
    }
    return colorMap[folderName] || 'text-gray-500'
  }

  const getStatusBgColor = (folderName) => {
    const bgColorMap = {
      'Reading': 'bg-blue-500',
      'Completed': 'bg-green-500',
      'On Hold': 'bg-yellow-500',
      'Plan to Read': 'bg-purple-500',
      'Dropped': 'bg-red-500'
    }
    return bgColorMap[folderName] || 'bg-gray-500'
  }

  // Methods
  const setRating = (rating) => {
    selectedRating.value = rating
  }

  const submitRating = async () => {
    if (!selectedRating.value || submittingRating.value || !props.isAuthenticated) return

    submittingRating.value = true
    try {
      const result = await titleDetailsService.rateTitle(props.titleId, selectedRating.value)

      if (result.success) {
        // Show success message (you can add toast notification here)
        console.log('Rating submitted successfully!')

        // Close dialog and reset
        isRatingDialogOpen.value = false
        selectedRating.value = 0
        hoverRating.value = 0

        // Reload rating statistics to show updated data
        await loadRatingStats()
      } else {
        console.error('Failed to submit rating:', result.error)
        alert(result.error || 'Failed to submit rating')
      }
    } catch (err) {
      console.error('Error submitting rating:', err)
      alert('Failed to submit rating')
    } finally {
      submittingRating.value = false
    }
  }

  const getRatingText = (rating) => {
    const texts = [
      '', 'Terrible', 'Very Bad', 'Bad', 'Poor', 'Average',
      'Good', 'Very Good', 'Great', 'Excellent', 'Masterpiece'
    ]
    return texts[rating] || ''
  }

  const switchTab = (tabKey) => {
    activeTab.value = tabKey
    emit('tab-changed', tabKey)
    loadTabContent(tabKey)
  }

  const loadTabContent = async (tabKey) => {
    if (tabData.value[tabKey].loaded || loading.value) return

    loading.value = true

    try {
      if (tabKey === 'info') {
        // Info is already loaded from titleData prop
        tabData.value.info.loaded = true
        tabData.value.info.data = props.titleData

        // Load statistics when info tab loads
        await Promise.all([
          loadRatingStats(),
          loadBookmarkStats()
        ])
      } else if (tabKey === 'chapters') {
        // Load chapters
        const response = await fetch(`/api/titles/${props.titleId}/chapters`)
        const result = await response.json()
        tabData.value.chapters.data = Array.isArray(result) ? result : []
        tabData.value.chapters.loaded = true
      } else if (tabKey === 'comments') {
        // CommentsComponent handles its own data loading via commentsService
        tabData.value.comments.loaded = true
        tabData.value.comments.data = []
      }
    } catch (error) {
      console.error(`Error loading ${tabKey} tab:`, error)
      tabData.value[tabKey].error = 'An error occurred while loading data'
    } finally {
      loading.value = false
    }
  }

  const toggleDescription = () => {
    descriptionExpanded.value = !descriptionExpanded.value
  }

  const scrollTranslators = (direction) => {
    if (translatorsContainer.value) {
      translatorsContainer.value.scrollLeft += direction
      nextTick(() => checkTranslatorButtons())
    }
  }

  const checkTranslatorButtons = () => {
    if (!translatorsContainer.value) return

    const container = translatorsContainer.value
    showLeftTranslatorButton.value = container.scrollLeft > 0
    showRightTranslatorButton.value = container.scrollLeft < (container.scrollWidth - container.clientWidth - 10)
  }

  const onCommentsUpdated = (comments) => {
    tabData.value.comments.data = comments
    emit('comments-updated', comments)
  }

  // Watchers
  watch(() => props.initialTab, (newTab) => {
    if (newTab) {
      activeTab.value = newTab
      loadTabContent(newTab)
    }
  })

  // Reload stats when titleId changes
  watch(() => props.titleId, async () => {
    if (activeTab.value === 'info' && tabData.value.info.loaded) {
      await Promise.all([
        loadRatingStats(),
        loadBookmarkStats()
      ])
    }
  })

  // Lifecycle
  onMounted(() => {
    loadTabContent(activeTab.value)
    nextTick(() => {
      if (translatorsContainer.value) {
        checkTranslatorButtons()
      }
    })
  })
</script>

<style scoped>
  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }

    .scrollbar-hide::-webkit-scrollbar {
      display: none;
    }

  /* Desktop - absolute positioning to prevent jumping, but allow natural height */
  @media (min-width: 1024px) {
    .tab-content-container {
      position: relative;
      min-height: 800px;
    }

    .tab-content-panel {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      width: 100%;
      height: auto;
    }
  }

  /* Mobile - natural flow */
  @media (max-width: 1023px) {
    .tab-content-container {
      position: static;
    }

    .tab-content-panel {
      position: static;
      overflow: visible;
    }
  }
</style>
