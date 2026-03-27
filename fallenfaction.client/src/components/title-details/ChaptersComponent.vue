<template>
  <div class="w-full space-y-5">
    <!-- Header with Sort and Search -->
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4  text-white">
      <!-- Sort Controls -->
      <div class="flex items-center gap-3">
        <span class="text-sm font-medium text-foreground">Sort by:</span>
        <Select v-model="sortBy">
          <SelectTrigger class="w-[140px]">
            <SelectValue placeholder="Select sort order" />
          </SelectTrigger>
          <SelectContent class="bg-background" style="background-color: hsl(20 14.3% 4.1%) !important;">
            <SelectItem value="newest">
              Newest
            </SelectItem>
            <SelectItem value="oldest">
              Oldest
            </SelectItem>
          </SelectContent>
        </Select>
      </div>

      <!-- Search Input -->
      <div class="w-full sm:w-auto">
        <Input v-model="searchQuery"
               type="text"
               placeholder="Search chapters..."
               class="w-full sm:w-[250px]"
               @input="filterChapters" />
      </div>
    </div>

    <!-- Chapters List -->
    <div v-if="filteredChapters.length > 0" class="space-y-5">
      <!-- Chapters Table -->
      <Card>
        <CardContent class="p-0">
          <!-- Table Header -->
          <div class="chapters-table-header">
            <div class="chapter-col-number text-white">№</div>
            <div class="chapter-col-name text-white">Name</div>
            <div class="chapter-col-team text-white">Team</div>
            <div class="chapter-col-date text-white">Date</div>
          </div>

          <!-- Table Body -->
          <div class="divide-y divide-border ">
            <a v-for="chapter in paginatedChapters"
               :key="chapter.id"
               :href="getChapterUrl(chapter)"
               class="chapter-row">
              <div class="chapter-col-number ">
                <Badge variant="secondary" class=" text-white">
                  Vol. {{ chapter.volumeNumber }}
                </Badge>
              </div>
              <div class="chapter-col-name font-medium text-white">
                {{ chapter.name }}
              </div>
              <div class="chapter-col-team">
                <Button variant="outline"
                        size="sm"
                        class="text-white"
                        as-child>
                  <span class="flex items-center gap-2">
                    <span v-if="chapter.team" class="flex items-center gap-2">
                      <Avatar class="h-6 w-6">
                        <AvatarImage v-if="chapter.team.avatarImagePath"
                                     :src="getImageUrl(chapter.team.avatarImagePath)"
                                     :alt="chapter.team.name" />
                        <AvatarFallback>{{ chapter.team.name.slice(0, 2).toUpperCase() }}</AvatarFallback>
                      </Avatar>
                      <span>{{ chapter.team.name }}</span>
                    </span>
                    <span v-else>Unknown</span>
                  </span>
                </Button>
              </div>
              <div class="chapter-col-date text-sm text-muted-foreground text-white">
                {{ formatDate(chapter.createdDate) }}
              </div>
            </a>
          </div>
        </CardContent>
      </Card>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex justify-center items-center gap-4">
        <Button variant="outline"
                size="sm"
                :disabled="currentPage === 1"
                @click="goToPage(currentPage - 1)">
          <ChevronLeft class="h-4 w-4" />
        </Button>

        <span class="text-sm text-muted-foreground">
          Page {{ currentPage }} of {{ totalPages }}
        </span>

        <Button variant="outline"
                size="sm"
                :disabled="currentPage === totalPages"
                @click="goToPage(currentPage + 1)">
          <ChevronRight class="h-4 w-4" />
        </Button>
      </div>
    </div>

    <!-- Empty State - No Search Results -->
    <Card v-else-if="searchQuery && chapters.length > 0">
      <CardContent class="flex flex-col items-center justify-center py-16 text-center">
        <Search class="h-12 w-12 text-muted-foreground/50 mb-4" />
        <p class="text-lg text-muted-foreground mb-4">
          No chapters found matching "{{ searchQuery }}"
        </p>
        <Button variant="outline" size="sm" @click="clearSearch">
          Clear Search
        </Button>
      </CardContent>
    </Card>

    <!-- Empty State - No Chapters -->
    <Card v-else>
      <CardContent class="flex flex-col items-center justify-center py-16 text-center">
        <BookOpen class="h-12 w-12 text-muted-foreground/50 mb-4" />
        <p class="text-lg text-muted-foreground">
          No chapters available yet...
        </p>
      </CardContent>
    </Card>
  </div>
</template>

<script setup>
  import { ref, computed, watch } from 'vue'
  import { Button } from '@/components/ui/button'
  import { Input } from '@/components/ui/input'
  import { Card, CardContent } from '@/components/ui/card'
  import { Badge } from '@/components/ui/badge'
  import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
  import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
  } from '@/components/ui/select'
  import { ChevronLeft, ChevronRight, Search, BookOpen } from 'lucide-vue-next'

  const props = defineProps({
    chapters: {
      type: Array,
      default: () => []
    },
    titleSlug: {
      type: String,
      required: true
    },
    chaptersPerPage: {
      type: Number,
      default: 20
    }
  })

  const sortBy = ref('newest')
  const searchQuery = ref('')
  const filteredChapters = ref([])
  const currentPage = ref(1)

  // Helper to get full image URL
  const getImageUrl = (path) => {
    if (!path) return ''
    if (path.startsWith('http')) return path
    const baseUrl = import.meta.env.VITE_API_BASE_URL?.replace('/api', '') ?? ''
    return `${baseUrl}${path}`
  }

  const sortedChapters = computed(() => {
    const chapters = [...filteredChapters.value]

    if (sortBy.value === 'newest') {
      return chapters.sort((a, b) => {
        const volumeCompare = b.volumeNumber - a.volumeNumber
        if (volumeCompare !== 0) return volumeCompare
        return b.chapterNumber - a.chapterNumber
      })
    } else {
      return chapters.sort((a, b) => {
        const volumeCompare = a.volumeNumber - b.volumeNumber
        if (volumeCompare !== 0) return volumeCompare
        return a.chapterNumber - b.chapterNumber
      })
    }
  })

  const totalPages = computed(() => {
    return Math.ceil(sortedChapters.value.length / props.chaptersPerPage)
  })

  const paginatedChapters = computed(() => {
    const start = (currentPage.value - 1) * props.chaptersPerPage
    const end = start + props.chaptersPerPage
    return sortedChapters.value.slice(start, end)
  })

  watch(
    () => props.chapters,
    (newChapters) => {
      filteredChapters.value = [...newChapters]
      currentPage.value = 1
    },
    { immediate: true }
  )

  watch(sortBy, () => {
    currentPage.value = 1
  })

  const filterChapters = () => {
    if (!searchQuery.value.trim()) {
      filteredChapters.value = [...props.chapters]
    } else {
      const query = searchQuery.value.toLowerCase()
      filteredChapters.value = props.chapters.filter(
        (chapter) =>
          chapter.name.toLowerCase().includes(query) ||
          chapter.chapterNumber.toString().includes(query) ||
          chapter.volumeNumber.toString().includes(query) ||
          (chapter.team && chapter.team.name.toLowerCase().includes(query))
      )
    }
    currentPage.value = 1
  }

  const clearSearch = () => {
    searchQuery.value = ''
    filteredChapters.value = [...props.chapters]
    currentPage.value = 1
  }

  const goToPage = (page) => {
    if (page >= 1 && page <= totalPages.value) {
      currentPage.value = page
    }
  }

  const getChapterUrl = (chapter) => {
    return `/${props.titleSlug}/chapter/${chapter.name}/v${chapter.volumeNumber}/t${chapter.teamId || 0}`
  }

  const formatDate = (dateString) => {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }
</script>

<style scoped>
  /* Table Layout */
  .chapters-table-header {
    display: grid;
    grid-template-columns: 120px 1fr 150px 120px;
    gap: 1rem;
    padding: 1rem 1.25rem;
    font-weight: 600;
    font-size: 0.875rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: hsl(var(--muted-foreground));
  }

  .chapter-row {
    display: grid;
    grid-template-columns: 120px 1fr 150px 120px;
    gap: 1rem;
    padding: 1rem 1.25rem;
    text-decoration: none;
    color: hsl(var(--foreground));
    transition: background-color 0.2s ease;
  }

    .chapter-row:hover {
      background-color: hsl(var(--muted) / 0.1);
    }

  /* Mobile Responsive */
  @media (max-width: 768px) {
    .chapters-table-header,
    .chapter-row {
      grid-template-columns: 1fr 120px 100px;
      gap: 0.75rem;
    }

    .chapter-col-team {
      display: none;
    }
  }

  @media (max-width: 480px) {
    .chapters-table-header,
    .chapter-row {
      grid-template-columns: 1fr 80px;
      gap: 0.625rem;
      padding: 0.875rem 1rem;
    }

    .chapter-col-date {
      display: none;
    }
  }
</style>
