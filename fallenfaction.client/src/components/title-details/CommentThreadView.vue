<template>
  <div class="min-h-screen bg-[var(--color-background)]">

    <!-- ── Loading ──────────────────────────────────────────── -->
    <div v-if="state.loading"
         class="flex flex-col items-center justify-center min-h-[60vh] gap-4">
      <svg class="animate-spin w-8 h-8 text-[var(--color-accent)]" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
        <path class="opacity-75" fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
      </svg>
      <p class="text-sm text-[var(--color-text)] opacity-60">Loading thread…</p>
    </div>

    <!-- ── Error ─────────────────────────────────────────────── -->
    <div v-else-if="state.error"
         class="flex flex-col items-center justify-center min-h-[60vh] gap-4 px-4 text-center">
      <svg class="w-12 h-12 text-[var(--color-text)] opacity-20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z" />
      </svg>
      <p class="text-sm font-medium text-[var(--color-heading)]">Could not load thread</p>
      <p class="text-xs text-[var(--color-text)] opacity-50">{{ state.error }}</p>
      <button class="text-xs text-[var(--color-accent)] hover:underline mt-2" @click="router.back()">
        ← Go back
      </button>
    </div>

    <!-- ── Thread ─────────────────────────────────────────────── -->
    <div v-else class="max-w-3xl mx-auto px-4 py-6">
      <!-- CommentsComponent renders its own "Back to full discussion" breadcrumb
           in threadMode — no duplicate header here.
           fullDiscussionUrl drives where that link points. -->
      <CommentsComponent :key="`thread-${commentId}`"
                         :target-id="state.targetId"
                         :target-type="state.targetType"
                         :full-discussion-url="state.fullDiscussionUrl"
                         :is-authenticated="isAuthenticated"
                         :current-user-id="currentUserId"
                         :is-admin="isAdmin" />
    </div>
  </div>
</template>

<script setup>
  import { reactive, computed, onMounted } from 'vue'
  import { useRouter, useRoute } from 'vue-router'
  import { commentsService } from '../../services/commentsService'
  import { useAuthStore } from '../../stores/authStore'
  import CommentsComponent from './CommentsComponent.vue'

  const router = useRouter()
  const route = useRoute()
  const authStore = useAuthStore()

  const commentId = computed(() => parseInt(route.params.commentId))

  const isAuthenticated = computed(() => authStore.isAuthenticated)
  const currentUserId = computed(() => authStore.user?.id ?? '')
  const isAdmin = computed(() => authStore.isAdmin)

  const state = reactive({
    loading: true,
    error: null,
    targetId: null,
    targetType: null,
    fullDiscussionUrl: null,   // passed to CommentsComponent for the "Back to full discussion" href
  })

  onMounted(async () => {
    if (!commentId.value || isNaN(commentId.value)) {
      state.error = 'Invalid comment ID.'
      state.loading = false
      return
    }

    // Ensure comment_id is in the query so CommentsComponent reads it on mount
    if (!route.query.comment_id) {
      await router.replace({
        path: route.path,
        query: { ...route.query, comment_id: commentId.value }
      })
    }

    const result = await commentsService.getCommentThread(commentId.value)

    if (!result.success) {
      state.error = result.error ?? 'Could not load thread.'
      state.loading = false
      return
    }

    state.targetId = result.data.targetId
    state.targetType = result.data.targetType

    // Resolve the title's originalTitle (used as the URL slug in the router).
    // We call our lightweight GetTitleSlug endpoint added alongside GetMyComments.
    const titleId = result.data.comment?.titleId ?? null
    if (titleId) {
      try {
        const apiBase = import.meta.env.VITE_API_BASE_URL ?? '/api'
        const token = localStorage.getItem('authToken')
        const res = await fetch(`${apiBase}/Comments/GetCommentTitleSlug/${titleId}`, {
          headers: token ? { Authorization: `Bearer ${token}` } : {}
        })
        if (res.ok) {
          const data = await res.json()
          const slug = data.originalTitle ?? data.englishTitle ?? null
          if (slug) {
            state.fullDiscussionUrl = `/${encodeURIComponent(slug)}?section=comments`
          }
        }
      } catch {
        // Non-critical — CommentsComponent falls back to router.back() behavior
      }
    }

    state.loading = false
  })
</script>
