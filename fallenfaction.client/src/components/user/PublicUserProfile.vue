<template>
  <div class="profile-page-wrapper min-h-screen bg-[var(--color-background)]">

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center min-h-screen">
      <div class="s-spinner" />
    </div>

    <!-- Not found -->
    <div v-else-if="error" class="flex flex-col items-center justify-center min-h-screen gap-4 text-center px-4">
      <UserX class="size-16 text-muted-foreground" />
      <h1 class="text-2xl font-bold text-[var(--color-heading)]">User not found</h1>
      <p class="text-[var(--color-text)] opacity-75">This profile doesn't exist or has been deactivated.</p>
      <Button @click="$router.push('/')">Go Home</Button>
    </div>

    <!-- Profile -->
    <template v-else-if="profile">

      <!-- ─── HERO BANNER ──────────────────────────────────────── -->
      <div class="relative h-44 sm:h-56 overflow-hidden bg-[var(--color-background-soft)]">
        <img v-if="profile.banner"
             :src="profile.banner"
             alt="Profile banner"
             class="absolute inset-0 w-full h-full object-cover" />
        <div v-else class="absolute inset-0 banner-bg" />
        <div class="absolute inset-0 bg-gradient-to-b from-transparent via-black/20 to-black/80" />

        <!-- "Edit my profile" button top-right if own profile -->
        <div v-if="isOwnProfile" class="absolute top-4 right-4 z-10">
          <Button variant="outline"
                  size="sm"
                  class="gap-2 bg-black/40 border-white/20 text-white hover:bg-black/60 hover:text-white backdrop-blur-sm"
                  @click="$router.push('/profile')">
            <Pencil class="size-4" />
            <span class="hidden sm:inline">Edit Profile</span>
          </Button>
        </div>
      </div>

      <!-- Avatar row -->
      <div class="max-w-5xl mx-auto px-4 sm:px-8 -mt-10 sm:-mt-12 relative z-10">
        <div class="flex items-end gap-4 pb-4">
          <!-- Avatar -->
          <div class="relative shrink-0">
            <Avatar class="size-20 sm:size-24 ring-4 ring-[var(--color-background)] shadow-2xl">
              <AvatarImage :src="profile.avatar" :alt="profile.name" />
              <AvatarFallback class="text-2xl sm:text-3xl font-bold bg-[var(--vt-c-indigo)] text-white">
                {{ profile.name.slice(0, 2).toUpperCase() }}
              </AvatarFallback>
            </Avatar>
            <!-- Online dot -->
            <span class="absolute bottom-1 right-1 size-4 rounded-full border-2 border-[var(--color-background)] shadow"
                  :class="profile.isOnline ? 'bg-emerald-400' : 'bg-zinc-500'"
                  :title="profile.isOnline ? 'Online' : 'Offline'" />
          </div>

          <!-- Name + badges -->
          <div class="pb-1 min-w-0">
            <h1 class="text-xl sm:text-2xl font-bold text-[var(--color-heading)] leading-tight truncate">
              {{ profile.name }}
            </h1>
            <div class="flex flex-wrap items-center gap-1.5 mt-1">
              <Badge variant="outline" class="text-xs">
                @{{ profile.name }}
              </Badge>
              <Badge v-if="profile.isVerified"
                     class="text-xs bg-blue-500/20 text-blue-400 border-blue-500/30">
                ✓ Verified
              </Badge>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── MAIN CONTENT ───────────────────────────────────────── -->
      <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 pt-2 pb-16">

        <!-- Tab nav -->
        <div class="border-b border-[var(--color-border)] mb-8">
          <div class="flex gap-1 overflow-x-auto scrollbar-hide">
            <a v-for="tab in tabs"
               :key="tab.key"
               href="#"
               class="relative flex items-center gap-2 px-4 py-3 text-sm font-medium whitespace-nowrap transition-colors duration-200 cursor-pointer select-none shrink-0"
               :class="activeTab === tab.key
                ? 'text-[var(--color-heading)]'
                : 'text-[var(--color-text)] opacity-60 hover:opacity-90'"
               @click.prevent="switchTab(tab.key)">
              <component :is="tab.icon" class="size-4 shrink-0" />
              {{ tab.label }}
              <Motion v-if="activeTab === tab.key"
                      layout-id="pub-profile-tab-indicator"
                      class="absolute bottom-0 left-0 right-0 h-0.5 bg-[var(--color-accent)] rounded-full"
                      :initial="{ opacity: 0 }"
                      :animate="{ opacity: 1 }"
                      :transition="{ type: 'spring', stiffness: 400, damping: 35 }" />
            </a>
          </div>
        </div>

        <!-- Tab panels -->
        <div class="tab-content-container">

          <!-- ── OVERVIEW ─────────────────────────────────────── -->
          <div v-show="activeTab === 'overview'" class="tab-panel">
            <Motion :initial="{ opacity: 0, y: 8 }" :animate="{ opacity: 1, y: 0 }" :transition="{ duration: 0.22 }">
              <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
                <!-- Profile info card -->
                <Card class="md:col-span-2 px-6 py-0 gap-0">
                  <CardHeader class="px-0 pt-6 pb-4">
                    <CardTitle class="flex items-center gap-2 text-base">
                      <UserIcon class="size-4 text-[var(--color-accent)]" />
                      Profile Information
                    </CardTitle>
                  </CardHeader>
                  <CardContent class="px-0 pb-6">
                    <dl class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-5">
                      <div>
                        <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">Username</dt>
                        <dd class="text-sm text-[var(--color-text)]">@{{ profile.name }}</dd>
                      </div>
                      <div>
                        <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">Member Since</dt>
                        <dd class="text-sm text-[var(--color-text)]">{{ formatDate(profile.registrationDate) }}</dd>
                      </div>
                      <div>
                        <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">Level</dt>
                        <dd class="text-sm text-[var(--color-text)]">{{ profile.level }}</dd>
                      </div>
                      <div>
                        <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">XP Points</dt>
                        <dd class="text-sm text-[var(--color-text)]">{{ profile.xpPoints }}</dd>
                      </div>
                      <div>
                        <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">Status</dt>
                        <dd>
                          <Badge :class="profile.isOnline
                            ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
                            : 'bg-zinc-500/15 text-zinc-400 border-zinc-500/30'"
                                 class="text-xs">
                            {{ profile.isOnline ? 'Online' : 'Offline' }}
                          </Badge>
                          <Badge v-if="profile.isVerified" class="ml-2 text-xs bg-blue-500/20 text-blue-400 border-blue-500/30">Verified</Badge>
                        </dd>
                      </div>

                      <div v-if="profile.bio" class="sm:col-span-2">
                        <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">Bio</dt>
                        <dd class="text-sm text-[var(--color-text)] bg-[var(--color-background-mute)] rounded-lg px-3 py-2.5 border border-[var(--color-border)] leading-relaxed whitespace-pre-wrap">
                          {{ profile.bio }}
                        </dd>
                      </div>
                    </dl>
                  </CardContent>
                </Card>

                <!-- Stats card -->
                <Card class="px-6 py-0 gap-0 h-fit">
                  <CardHeader class="px-0 pt-6 pb-4">
                    <CardTitle class="flex items-center gap-2 text-base">
                      <Zap class="size-4 text-[var(--color-accent)]" />
                      Stats
                    </CardTitle>
                  </CardHeader>
                  <CardContent class="px-0 pb-6 flex flex-col gap-4">
                    <div class="flex items-center justify-between">
                      <span class="text-sm text-[var(--color-text)] opacity-60">Level</span>
                      <span class="text-sm font-semibold text-[var(--color-heading)]">{{ profile.level }}</span>
                    </div>
                    <div class="flex items-center justify-between">
                      <span class="text-sm text-[var(--color-text)] opacity-60">XP</span>
                      <span class="text-sm font-semibold text-[var(--color-heading)]">{{ profile.xpPoints }}</span>
                    </div>
                    <div class="flex items-center justify-between">
                      <span class="text-sm text-[var(--color-text)] opacity-60">Comments</span>
                      <span class="text-sm font-semibold text-[var(--color-heading)]">{{ comments.pagination.totalCount }}</span>
                    </div>
                    <Separator />
                    <div v-if="isOwnProfile">
                      <Button variant="outline" class="w-full justify-start gap-2 text-sm h-9" @click="$router.push('/profile')">
                        <Pencil class="size-4" /> Edit My Profile
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </Motion>
          </div>

          <!-- ── BOOKMARKS ──────────────────────────────────────── -->
          <div v-show="activeTab === 'bookmarks'" class="tab-panel">
            <Motion :initial="{ opacity: 0, y: 8 }" :animate="{ opacity: 1, y: 0 }" :transition="{ duration: 0.22 }">
              <div v-if="isOwnProfile">
                <!-- Redirect own user to their own profile -->
                <div class="flex flex-col items-center justify-center py-24 text-center">
                  <BookmarkIcon class="size-14 mb-5 text-[var(--color-text)] opacity-15" />
                  <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-2">Your Reading Lists</h3>
                  <p class="text-sm text-[var(--color-text)] opacity-50 max-w-xs leading-relaxed mb-4">
                    Manage all your reading lists from your personal profile page.
                  </p>
                  <Button variant="outline" size="sm" @click="$router.push('/profile')">
                    Go to My Profile
                  </Button>
                </div>
              </div>
              <div v-else class="flex flex-col items-center justify-center py-24 text-center">
                <BookmarkIcon class="size-14 mb-5 text-[var(--color-text)] opacity-15" />
                <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-2">Reading Lists are Private</h3>
                <p class="text-sm text-[var(--color-text)] opacity-50 max-w-xs leading-relaxed">
                  {{ profile.name }}'s reading lists are only visible to them.
                </p>
              </div>
            </Motion>
          </div>

          <!-- ── FOLLOWING ──────────────────────────────────────── -->
          <div v-show="activeTab === 'following'" class="tab-panel">
            <Motion :initial="{ opacity: 0, y: 8 }" :animate="{ opacity: 1, y: 0 }" :transition="{ duration: 0.22 }">
              <div class="flex flex-col items-center justify-center py-24 text-center">
                <Users class="size-14 mb-5 text-[var(--color-text)] opacity-15" />
                <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-2">Following</h3>
                <p class="text-sm text-[var(--color-text)] opacity-50 max-w-xs leading-relaxed">
                  This feature is coming soon.
                </p>
              </div>
            </Motion>
          </div>

          <!-- ── COMMENTS ───────────────────────────────────────── -->
          <div v-show="activeTab === 'comments'" class="tab-panel">
            <Motion :initial="{ opacity: 0, y: 8 }" :animate="{ opacity: 1, y: 0 }" :transition="{ duration: 0.22 }">
              <!-- Loading -->
              <div v-if="comments.loading" class="space-y-3">
                <Skeleton v-for="n in 5" :key="n" class="h-28 w-full rounded-xl" />
              </div>

              <!-- Error -->
              <div v-else-if="comments.error" class="flex flex-col items-center justify-center py-20 text-center">
                <MessageSquare class="size-12 mb-4 text-[var(--color-text)] opacity-20" />
                <p class="text-sm font-medium text-[var(--color-heading)] mb-1">Could not load comments</p>
                <Button variant="outline" size="sm" @click="loadComments">Try again</Button>
              </div>

              <!-- Empty -->
              <div v-else-if="!comments.loading && comments.items.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
                <MessageSquare class="size-12 mb-4 text-[var(--color-text)] opacity-20" />
                <h3 class="text-base font-semibold text-[var(--color-heading)] mb-1">No comments yet</h3>
                <p class="text-sm text-[var(--color-text)] opacity-50 max-w-xs leading-relaxed">
                  {{ profile.name }} hasn't commented on anything yet.
                </p>
              </div>

              <!-- List -->
              <div v-else class="space-y-2">
                <div class="flex items-center justify-between pb-4 border-b border-[var(--color-border)]">
                  <p class="text-sm font-medium text-[var(--color-heading)]">
                    Comment History
                    <span class="ml-2 text-xs font-normal text-[var(--color-text)] opacity-50">
                      {{ comments.pagination.totalCount }} total
                    </span>
                  </p>
                  <div class="flex items-center gap-2">
                    <span class="text-xs text-[var(--color-text)] opacity-50">Sort:</span>
                    <select v-model="comments.sortBy"
                            class="text-xs bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-md px-2 py-1 text-[var(--color-text)] cursor-pointer focus:outline-none focus:border-[var(--color-accent)]"
                            @change="reloadComments">
                      <option value="newest">Newest</option>
                      <option value="oldest">Oldest</option>
                      <option value="likes">Most Liked</option>
                    </select>
                  </div>
                </div>

                <div v-for="c in comments.items" :key="c.id" class="comment-history-card">
                  <!-- Context breadcrumb -->
                  <div class="comment-context">
                    <component :is="c.targetType === 1 ? BookOpenIcon : BookOpen"
                               class="size-3.5 shrink-0 opacity-60" />
                    <span class="text-[11px] text-[var(--color-text)] opacity-50">
                      {{ c.targetType === 1 ? 'Title' : 'Chapter' }}
                    </span>
                    <span class="text-[11px] opacity-30">›</span>
                    <a v-if="c.titleSlug"
                       :href="buildCommentUrl(c)"
                       class="text-[11px] font-medium text-[var(--color-accent)] hover:underline truncate max-w-[260px]">
                      {{ c.titleName || c.titleSlug }}
                    </a>
                    <span v-else class="text-[11px] text-[var(--color-text)] opacity-50 truncate">
                      {{ c.titleName || '—' }}
                    </span>
                    <span class="text-[11px] text-[var(--color-text)] opacity-35 ml-auto shrink-0">
                      {{ formatTimeAgo(c.postedDate) }}
                    </span>
                  </div>

                  <!-- Body -->
                  <div class="flex gap-3 mt-2">
                    <div class="flex flex-col items-center gap-0.5 shrink-0 pt-0.5">
                      <ChevronUp class="size-4 text-[var(--color-text)] opacity-30" />
                      <span class="text-xs font-semibold tabular-nums leading-none"
                            :class="{
                              'text-emerald-400': (c.likesCount - c.dislikesCount) > 0,
                              'text-red-400': (c.likesCount - c.dislikesCount) < 0,
                              'text-[var(--color-text)] opacity-50': (c.likesCount - c.dislikesCount) === 0
                            }">
                        {{ c.likesCount - c.dislikesCount }}
                      </span>
                      <ChevronDown class="size-4 text-[var(--color-text)] opacity-30" />
                    </div>
                    <div class="flex-1 min-w-0">
                      <p class="text-sm text-[var(--color-text)] leading-relaxed"
                         :class="{ 'line-clamp-4': !c.expanded }">
                        {{ c.content }}
                      </p>
                      <button v-if="c.content.length > 300"
                              class="text-xs text-[var(--color-accent)] hover:underline mt-1"
                              @click="c.expanded = !c.expanded">
                        {{ c.expanded ? 'Show less' : 'Show more' }}
                      </button>
                      <div class="flex items-center gap-3 mt-2">
                        <a :href="buildCommentUrl(c)" class="comment-action-btn" title="View in context">
                          <ExternalLink class="size-3" /> View in context
                        </a>
                        <span v-if="c.parentCommentId" class="text-[11px] text-[var(--color-text)] opacity-40 flex items-center gap-1">
                          <CornerDownRight class="size-3" /> Reply
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Load more -->
                <div v-if="comments.pagination.hasNext" class="pt-4 flex justify-center">
                  <Button variant="outline" size="sm" :disabled="comments.loadingMore" @click="loadMoreComments">
                    <Loader2 v-if="comments.loadingMore" class="size-4 animate-spin mr-2" />
                    Load more
                  </Button>
                </div>
              </div>
            </Motion>
          </div>

        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import axios from 'axios'
import { Motion } from 'motion-v'
import {
  UserIcon, BookmarkIcon, BookOpenIcon, BookOpen,
  Users, MessageSquare, Zap, Pencil, Loader2,
  ChevronUp, ChevronDown, ExternalLink, CornerDownRight,
  UserX,
} from 'lucide-vue-next'
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { buildTitleSlug } from '@/utils/titleSlug.js'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const profile = ref(null)
const loading = ref(true)
const error = ref(false)

const isOwnProfile = computed(() =>
  authStore.isAuthenticated && authStore.user?.id === route.params.id
)

// ── Tabs ───────────────────────────────────────────────────────
const tabs = [
  { key: 'overview',  label: 'Overview',   icon: UserIcon },
  { key: 'bookmarks', label: 'Bookmarks',  icon: BookmarkIcon },
  { key: 'following', label: 'Following',  icon: Users },
  { key: 'comments',  label: 'Comments',   icon: MessageSquare },
]
const activeTab = ref('overview')

function switchTab(key) {
  activeTab.value = key
  if (key === 'comments' && !comments.initialized) loadComments()
}

// ── Date helpers ───────────────────────────────────────────────
function formatDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
}

function formatTimeAgo(dateString) {
  const now = new Date()
  const date = new Date(dateString)
  const secs = Math.floor((now - date) / 1000)
  if (secs < 60) return 'just now'
  const mins = Math.floor(secs / 60)
  if (mins < 60) return `${mins} minute${mins !== 1 ? 's' : ''} ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs} hour${hrs !== 1 ? 's' : ''} ago`
  const days = Math.floor(hrs / 24)
  if (days < 30) return `${days} day${days !== 1 ? 's' : ''} ago`
  const months = Math.floor(days / 30)
  if (months < 12) return `${months} month${months !== 1 ? 's' : ''} ago`
  return `${Math.floor(months / 12)} year${Math.floor(months / 12) !== 1 ? 's' : ''} ago`
}

function buildCommentUrl(c) {
  if (!c.titleSlug && !c.titleName) return '#'
  const slug = c.titleId
    ? buildTitleSlug(c.titleName || c.titleSlug, c.titleId)
    : encodeURIComponent(c.titleSlug || 'title')
  if (c.targetType === 1) return `/${slug}?section=comments&comment_id=${c.id}`
  if (c.chapterName && c.volumeNumber != null && c.teamId != null) {
    const chName = encodeURIComponent(c.chapterName)
    return `/${slug}/chapter/${chName}/v${c.volumeNumber}/t${c.teamId}?viewMode=single&comment_id=${c.id}`
  }
  return `/${slug}?section=comments&comment_id=${c.id}`
}

// ── Comments state ─────────────────────────────────────────────
const COMMENTS_PAGE_SIZE = 20
const comments = reactive({
  initialized: false,
  loading: false,
  loadingMore: false,
  error: null,
  items: [],
  pagination: { totalCount: 0, page: 1, pageSize: COMMENTS_PAGE_SIZE, totalPages: 0, hasNext: false, hasPrevious: false },
  page: 1,
  sortBy: 'newest',
})

function mapComment(c) {
  return {
    id: c.id,
    content: c.content,
    postedDate: c.postedDate,
    likesCount: c.likesCount ?? 0,
    dislikesCount: c.dislikesCount ?? 0,
    parentCommentId: c.parentCommentId ?? null,
    targetType: c.targetType,
    titleId: c.titleId ?? null,
    titleName: c.titleName ?? null,
    titleSlug: c.titleSlug ?? null,
    chapterId: c.chapterId ?? null,
    chapterName: c.chapterName ?? null,
    volumeNumber: c.volumeNumber ?? null,
    teamId: c.teamId ?? null,
    expanded: false,
  }
}

async function loadComments() {
  comments.initialized = true
  comments.loading = true
  comments.error = null
  comments.page = 1
  try {
    const res = await axios.get(`/api/Comments/GetUserComments/${route.params.id}`, {
      params: { page: 1, pageSize: COMMENTS_PAGE_SIZE, sortBy: comments.sortBy }
    })
    comments.items = (res.data.comments ?? []).map(mapComment)
    comments.pagination = res.data.pagination ?? comments.pagination
  } catch (err) {
    comments.error = 'Failed to load comments.'
  } finally {
    comments.loading = false
  }
}

async function reloadComments() {
  comments.items = []
  comments.pagination = { ...comments.pagination, hasNext: false }
  await loadComments()
}

async function loadMoreComments() {
  if (!comments.pagination.hasNext || comments.loadingMore) return
  comments.loadingMore = true
  comments.page++
  try {
    const res = await axios.get(`/api/Comments/GetUserComments/${route.params.id}`, {
      params: { page: comments.page, pageSize: COMMENTS_PAGE_SIZE, sortBy: comments.sortBy }
    })
    comments.items.push(...(res.data.comments ?? []).map(mapComment))
    comments.pagination = res.data.pagination ?? comments.pagination
  } catch {
    // silently stop
  } finally {
    comments.loadingMore = false
  }
}

// ── Init ───────────────────────────────────────────────────────
onMounted(async () => {
  const id = route.params.id
  try {
    const res = await axios.get(`/api/Users/${id}/profile`)
    profile.value = res.data
    document.title = `${res.data.name} — FallenFaction`
    // Eagerly load comment count for stats card
    loadComments()
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.banner-bg {
  background: radial-gradient(ellipse 80% 60% at 20% 40%, color-mix(in srgb, var(--vt-c-indigo) 35%, transparent), transparent 70%),
              radial-gradient(ellipse 60% 80% at 80% 60%, color-mix(in srgb, var(--color-accent) 25%, transparent), transparent 70%),
              var(--color-background-soft);
}

.tab-content-container {
  display: grid;
  grid-template-columns: 1fr;
  width: 100%;
  overflow: hidden;
}

.tab-panel {
  grid-column: 1;
  grid-row: 1;
  min-height: 0;
  min-width: 0;
  width: 100%;
  overflow: hidden;
}

.scrollbar-hide {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
.scrollbar-hide::-webkit-scrollbar { display: none; }

.comment-history-card {
  padding: 12px 14px;
  border-radius: 10px;
  border: 1px solid var(--color-border);
  transition: border-color 0.15s;
}
.comment-history-card:hover {
  border-color: color-mix(in srgb, var(--color-accent) 40%, transparent);
}

.comment-context {
  display: flex;
  align-items: center;
  gap: 5px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--color-border);
}

.comment-action-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 0.6875rem;
  color: var(--color-text);
  opacity: 0.5;
  text-decoration: none;
  transition: opacity 0.15s, color 0.15s;
}
.comment-action-btn:hover {
  opacity: 1;
  color: var(--color-accent);
}

.s-spinner {
  width: 2rem; height: 2rem;
  border: 3px solid var(--color-border);
  border-top-color: var(--color-accent);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
</style>
