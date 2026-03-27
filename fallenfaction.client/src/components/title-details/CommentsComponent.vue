<!-- Enhanced Comments Component with shadcn-vue UI Components -->
<template>
  <div :class="threadMode ? '' : 'border border-[var(--color-border)] rounded-xl overflow-hidden'">
    <!-- Comments Header — hidden when displaying a single thread (threadMode) -->
    <div v-if="!threadMode" class="p-6 border-b border-[var(--color-border)]">
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-xl font-semibold text-[var(--color-text)]">
          Comments ({{ totalComments }})
        </h3>

        <!-- Sort Options with shadcn Select -->
        <div class="flex items-center space-x-2">
          <span class="text-sm text-[var(--color-text)] opacity-60">Sort by:</span>
          <Select v-model="currentSort" @update:model-value="handleSortChange">
            <SelectTrigger class="w-[140px]">
              <SelectValue placeholder="Sort by" />
            </SelectTrigger>
            <SelectContent>
              <SelectGroup>
                <SelectItem value="newest">Newest</SelectItem>
                <SelectItem value="oldest">Oldest</SelectItem>
                <SelectItem value="likes">Most Liked</SelectItem>
              </SelectGroup>
            </SelectContent>
          </Select>
        </div>
      </div>

      <!-- Comments Disabled Notice with shadcn Alert -->
      <Alert v-if="commentsDisabled" variant="warning" class="mb-4">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
        </svg>
        <AlertTitle>Comments Disabled</AlertTitle>
        <AlertDescription>
          Comments have been disabled for this content.
        </AlertDescription>
      </Alert>

      <!-- Debug Info with shadcn Alert -->
      <Alert v-if="showDebugInfo && (debugMode || error)" variant="default" class="mb-4">
        <details>
          <summary class="cursor-pointer font-medium">Debug Info</summary>
          <div class="mt-2 space-y-1 text-xs">
            <div><strong>Target ID:</strong> {{ targetId }}</div>
            <div><strong>Target Type:</strong> {{ targetType }}</div>
            <div><strong>Comments Enabled:</strong> {{ commentsEnabled }}</div>
            <div><strong>Comments Disabled:</strong> {{ commentsDisabled }}</div>
            <div><strong>API Call Made:</strong> {{ apiCallMade }}</div>
            <div><strong>API Error:</strong> {{ apiError || 'None' }}</div>
            <div><strong>Stats Loaded:</strong> {{ statsLoaded }}</div>
          </div>
        </details>
      </Alert>

      <!-- Comment Form -->
      <div v-if="!commentsDisabled" class="space-y-4">
        <!-- Auth Check with shadcn Alert and Button -->
        <Alert v-if="!isAuthenticated" variant="default" class="border-blue-200 dark:border-blue-800">
          <div class="flex items-center justify-between">
            <div class="flex items-center">
              <svg class="w-5 h-5 text-blue-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
              </svg>
              <AlertDescription class="text-blue-800 dark:text-blue-200">
                Sign in to join the conversation
              </AlertDescription>
            </div>
            <Button size="sm" @click="goToLogin">
              Sign In
            </Button>
          </div>
        </Alert>

        <!-- Comment Input with shadcn components -->
        <div v-else class="space-y-3">
          <div class="flex space-x-3">
            <!-- User Avatar with shadcn Avatar -->
            <div class="flex-shrink-0">
              <Avatar>
                <AvatarImage src="" alt="User" />
                <AvatarFallback>
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                  </svg>
                </AvatarFallback>
              </Avatar>
            </div>

            <!-- Comment Input with shadcn Textarea -->
            <div class="flex-1">
              <Textarea v-model="newCommentText"
                        :disabled="submittingComment"
                        placeholder="Share your thoughts..."
                        :rows="3"
                        maxlength="2000"
                        class="resize-none"
                        @keydown.ctrl.enter="submitComment"
                        @keydown.meta.enter="submitComment" />

              <!-- Character Count & Actions -->
              <div class="flex items-center justify-between mt-2">
                <span class="text-xs text-[var(--color-text)] opacity-60">
                  {{ newCommentText.length }}/2000 characters
                </span>
                <div class="flex items-center space-x-2">
                  <span class="text-xs text-[var(--color-text)] opacity-60">
                    Ctrl+Enter to post
                  </span>
                  <Button @click="submitComment"
                          :disabled="!newCommentText.trim() || submittingComment">
                    <svg v-if="submittingComment" class="animate-spin w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    {{ submittingComment ? 'Posting...' : 'Post Comment' }}
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Comments List -->
    <div :class="threadMode ? 'p-4' : 'p-6'">
      <!-- Thread Mode Breadcrumb (Reddit-style) -->
      <div v-if="threadMode && parentChain.length > 0" class="mb-4 p-4 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg">
        <div class="flex items-center gap-2 mb-3 pb-3 border-b border-[var(--color-border)]">
          <svg class="w-4 h-4 text-[var(--color-accent)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16l-4-4m0 0l4-4m-4 4h18"></path>
          </svg>
          <a :href="getBackToFullDiscussionUrl()" class="text-[var(--color-accent)] font-semibold text-sm hover:underline">
            Back to full discussion
          </a>
        </div>

        <div class="flex flex-col gap-2">
          <span class="text-xs font-semibold text-[var(--color-text)] opacity-60 uppercase">Parent comments:</span>
          <div class="flex flex-wrap items-center gap-2">
            <a v-for="(parent, index) in parentChain"
               :key="parent.id"
               :href="getCommentUrl(parent.id)"
               :title="parent.content"
               class="inline-flex items-center gap-2 px-3 py-1.5 bg-[var(--color-background)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] hover:border-[var(--color-accent)] transition-all">
              <Badge variant="secondary">{{ parent.userName }}</Badge>
              <span class="text-xs text-[var(--color-text)] opacity-70 max-w-[200px] truncate">{{ parent.content }}</span>
              <svg v-if="index < parentChain.length - 1" class="w-3.5 h-3.5 text-[var(--color-text)] opacity-60" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
              </svg>
            </a>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="flex items-center justify-center py-12">
        <div class="text-center">
          <svg class="animate-spin w-8 h-8 text-[var(--color-accent)] mx-auto mb-3" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p class="text-[var(--color-text)] opacity-70">Loading comments...</p>
        </div>
      </div>

      <!-- Error State with shadcn Alert and Button -->
      <Alert v-else-if="error" variant="destructive" class="max-w-md mx-auto">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
        </svg>
        <AlertTitle>Error Loading Comments</AlertTitle>
        <AlertDescription>
          {{ error }}
        </AlertDescription>
        <div class="mt-4">
          <Button variant="destructive" @click="retryLoad">
            Try Again
          </Button>
        </div>
      </Alert>

      <!-- Empty State -->
      <div v-else-if="comments.length === 0" class="text-center py-16">
        <svg class="w-16 h-16 text-[var(--color-text)] opacity-30 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
        </svg>
        <h3 class="text-lg font-medium text-[var(--color-text)] mb-2">No comments yet</h3>
        <p class="text-[var(--color-text)] opacity-60">
          {{ commentsDisabled ? 'Comments are disabled for this content.' : 'Be the first to share your thoughts!' }}
        </p>
      </div>

      <!-- Comments -->
      <div v-else class="space-y-6">
        <CommentItem v-for="comment in comments"
                     :key="comment.id"
                     :comment="comment"
                     :target-id="targetId"
                     :target-type="targetType"
                     :is-authenticated="isAuthenticated"
                     :current-user-id="currentUserId"
                     :is-admin="isAdmin"
                     :can-reply="!commentsDisabled"
                     :depth="0"
                     :max-depth="8"
                     @reply-added="onReplyAdded"
                     @comment-updated="onCommentUpdated"
                     @comment-deleted="onCommentDeleted" />

        <!-- Load More Button -->
        <div v-if="hasMore" class="flex justify-center pt-4">
          <Button variant="outline" :disabled="loadingMore" @click="loadMore">
            <svg v-if="loadingMore" class="animate-spin w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            {{ loadingMore ? 'Loading...' : 'Load More Comments' }}
          </Button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { commentsService } from '../../services/commentsService'
  import CommentItem from './CommentItem.vue'
  import { Button } from '@/components/ui/button'
  import { Textarea } from '@/components/ui/textarea'
  import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
  import {
    Select,
    SelectContent,
    SelectGroup,
    SelectItem,
    SelectTrigger,
    SelectValue,
  } from '@/components/ui/select'
  import {
    Alert,
    AlertDescription,
    AlertTitle,
  } from '@/components/ui/alert'
  import { Badge } from '@/components/ui/badge'

  export default {
    name: 'CommentsSection',
    components: {
      CommentItem,
      Button,
      Textarea,
      Avatar,
      AvatarFallback,
      AvatarImage,
      Select,
      SelectContent,
      SelectGroup,
      SelectItem,
      SelectTrigger,
      SelectValue,
      Alert,
      AlertDescription,
      AlertTitle,
      Badge
    },
    props: {
      targetId: {
        type: [Number, String],
        required: true
      },
      targetType: {
        type: [Number, String],
        required: true,
        validator: (value) => [1, 2, 3].includes(parseInt(value))
      },
      isAuthenticated: {
        type: Boolean,
        default: false
      },
      currentUserId: {
        type: String,
        default: ''
      },
      isAdmin: {
        type: Boolean,
        default: false
      },
      // When set (e.g. from CommentThreadView), the "Back to full discussion" link
      // uses this URL instead of stripping comment_id from the current URL.
      fullDiscussionUrl: {
        type: String,
        default: null
      }
    },
    emits: ['comments-loaded', 'comment-added', 'comments-updated'],
    data() {
      return {
        comments: [],
        loading: true,
        error: null,
        commentsEnabled: true,
        totalComments: 0,
        currentSort: 'newest',
        currentPage: 1,
        hasMore: false,
        loadingMore: false,
        newCommentText: '',
        submittingComment: false,
        threadMode: false,
        focusedCommentId: null,
        parentChain: [],
        pagination: {
          totalCount: 0,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false
        },
        debugMode: process.env.NODE_ENV === 'development',
        showDebugInfo: true,
        apiCallMade: false,
        apiError: null,
        statsLoaded: false
      }
    },
    computed: {
      commentsDisabled() {
        return this.commentsEnabled === false
      },
      remainingComments() {
        return Math.max(0, this.pagination.totalCount - this.comments.length)
      }
    },
    async mounted() {
      await this.initializeComments()
    },
    methods: {
      async initializeComments() {
        console.log('Initializing comments for target:', this.targetId, 'type:', this.targetType)

        const urlParams = new URLSearchParams(window.location.search)
        const commentId = urlParams.get('comment_id')

        if (commentId) {
          await this.loadCommentThread(parseInt(commentId))
        } else {
          await this.loadCommentStats()
          await this.loadComments()
        }
      },

      async loadCommentStats() {
        try {
          this.apiCallMade = true
          this.apiError = null

          const result = await commentsService.getCommentStats(
            parseInt(this.targetId),
            parseInt(this.targetType)
          )

          if (result.success) {
            this.commentsEnabled = result.data.commentsEnabled !== false
            this.totalComments = result.data.totalComments || 0
            this.statsLoaded = true
          } else {
            this.apiError = result.error
            this.commentsEnabled = true
          }
        } catch (error) {
          console.error('Error loading comment stats:', error)
          this.apiError = error.message
          this.commentsEnabled = true
        }
      },

      async loadCommentThread(commentId) {
        try {
          this.loading = true
          this.threadMode = true
          this.focusedCommentId = commentId
          this.error = null

          const result = await commentsService.getCommentThread(commentId)

          if (result.success) {
            this.comments = [result.data.comment]
            this.parentChain = result.data.parentChain || []
            this.commentsEnabled = true

            this.$emit('comments-loaded', {
              comments: this.comments,
              totalCount: 1
            })
          } else {
            this.error = result.error
          }
        } catch (error) {
          console.error('Error loading comment thread:', error)
          this.error = 'Failed to load comment thread'
        } finally {
          this.loading = false
        }
      },

      async loadComments(reset = true) {
        try {
          if (reset) {
            this.loading = true
            this.currentPage = 1
          } else {
            this.loadingMore = true
          }

          this.error = null

          const result = await commentsService.getComments(
            parseInt(this.targetId),
            parseInt(this.targetType),
            this.currentPage,
            20,
            this.currentSort
          )

          if (result.success) {
            if (reset) {
              this.comments = result.data.comments
            } else {
              this.comments = [...this.comments, ...result.data.comments]
            }

            this.pagination = result.data.pagination
            this.hasMore = result.data.pagination.hasNext
            this.totalComments = result.data.pagination.totalCount

            this.$emit('comments-loaded', {
              comments: this.comments,
              totalCount: this.totalComments
            })
          } else {
            this.error = result.error
          }
        } catch (error) {
          console.error('Error loading comments:', error)
          this.error = 'Failed to load comments'
        } finally {
          this.loading = false
          this.loadingMore = false
        }
      },

      async loadMore() {
        if (this.loadingMore || !this.hasMore) return
        this.currentPage += 1
        await this.loadComments(false)
      },

      async handleSortChange() {
        await this.loadComments(true)
      },

      async retryLoad() {
        this.error = null
        this.apiError = null
        this.apiCallMade = false
        await this.initializeComments()
      },

      async submitComment() {
        if (!this.newCommentText.trim() || this.submittingComment || !this.isAuthenticated) {
          return
        }

        const validation = commentsService.validateCommentContent(this.newCommentText)
        if (!validation.isValid) {
          this.showToast(validation.error, 'error')
          return
        }

        this.submittingComment = true

        try {
          const result = await commentsService.addComment(
            parseInt(this.targetId),
            parseInt(this.targetType),
            this.newCommentText.trim(),
            null
          )

          if (result.success) {
            this.comments.unshift(result.data)
            this.totalComments += 1
            this.newCommentText = ''

            this.showToast(result.message, 'success')

            this.$emit('comment-added', result.data)
            this.$emit('comments-updated', {
              comments: this.comments,
              totalCount: this.totalComments
            })
          } else {
            this.showToast(result.error, 'error')
          }
        } catch (error) {
          console.error('Error submitting comment:', error)
          this.showToast('Failed to post comment', 'error')
        } finally {
          this.submittingComment = false
        }
      },

      onReplyAdded(reply) {
        const parentComment = commentsService.findCommentById(this.comments, reply.parentCommentId)

        if (parentComment) {
          if (!parentComment.replies) {
            parentComment.replies = []
          }
          parentComment.replies.push(reply)
          this.totalComments += 1
          this.comments = [...this.comments]

          this.$emit('comments-updated', {
            comments: this.comments,
            totalCount: this.totalComments
          })
        }
      },

      onCommentUpdated(updatedComment) {
        const updated = commentsService.updateCommentInTree(this.comments, updatedComment.id, updatedComment)
        if (updated) {
          this.comments = [...this.comments]
        }
      },

      onCommentDeleted(deletedCommentId) {
        const removed = commentsService.removeCommentFromTree(this.comments, deletedCommentId)
        if (removed) {
          this.totalComments = Math.max(0, this.totalComments - 1)
          this.comments = [...this.comments]

          this.$emit('comments-updated', {
            comments: this.comments,
            totalCount: this.totalComments
          })
        }
      },

      goToLogin() {
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
        window.location.href = `/account/login?returnUrl=${returnUrl}`
      },

      getBackToFullDiscussionUrl() {
        // Prefer the explicitly-passed URL (set by CommentThreadView)
        if (this.fullDiscussionUrl) return this.fullDiscussionUrl
        // Fallback: strip comment_id from the current URL
        const currentUrl = new URL(window.location.href)
        currentUrl.searchParams.delete('comment_id')
        return currentUrl.toString()
      },

      getCommentUrl(commentId) {
        const currentUrl = new URL(window.location.href)
        currentUrl.searchParams.set('comment_id', commentId)
        return currentUrl.toString()
      },

      showToast(message, type = 'info') {
        const toastContainer = document.getElementById('toast-container') || this.createToastContainer()
        const toast = document.createElement('div')
        const bgColor = type === 'success' ? 'bg-green-500' : type === 'error' ? 'bg-red-500' : 'bg-blue-500'

        toast.className = `${bgColor} text-white px-6 py-4 rounded-lg shadow-lg max-w-sm transform transition-all duration-300 translate-x-full opacity-0 mb-2`
        toast.innerHTML = `
        <div class="flex items-center">
          <span class="flex-1">${message}</span>
          <button onclick="this.parentElement.parentElement.remove()" class="ml-3 text-white hover:text-gray-200">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>
      `

        toastContainer.appendChild(toast)

        setTimeout(() => {
          toast.classList.remove('translate-x-full', 'opacity-0')
        }, 100)

        setTimeout(() => {
          toast.classList.add('translate-x-full', 'opacity-0')
          setTimeout(() => {
            if (toast.parentNode) {
              toast.remove()
            }
          }, 300)
        }, 5000)
      },

      createToastContainer() {
        const container = document.createElement('div')
        container.id = 'toast-container'
        container.className = 'fixed bottom-4 right-4 z-50 space-y-2'
        document.body.appendChild(container)
        return container
      }
    }
  }
</script>

<style scoped>
  /* Thread Mode Breadcrumb Navigation (Reddit-style) */
  .thread-breadcrumb {
    margin-bottom: 16px;
    padding: 16px;
    background: rgba(0, 0, 0, 0.1);
    border: 1px solid var(--color-border, #e5e5e5);
    border-radius: 8px;
  }

  .breadcrumb-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 12px;
    padding-bottom: 12px;
    border-bottom: 1px solid var(--color-border, #e5e5e5);
  }

    .breadcrumb-header svg {
      color: var(--color-accent, #ff6d00);
    }

  .back-link {
    color: var(--color-accent, #ff6d00);
    font-weight: 600;
    font-size: 14px;
    text-decoration: none;
    transition: color 0.2s ease;
  }

    .back-link:hover {
      color: var(--color-accent-hover, #ff9100);
      text-decoration: underline;
    }

  .breadcrumb-chain {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .breadcrumb-label {
    font-size: 12px;
    font-weight: 600;
    color: var(--color-text-muted, #8a8a8e);
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  .breadcrumb-items {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
  }

  .breadcrumb-item {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 6px 12px;
    background: var(--color-background, white);
    border: 1px solid var(--color-border, #e5e5e5);
    border-radius: 6px;
    text-decoration: none;
    transition: all 0.2s ease;
    max-width: 300px;
  }

    .breadcrumb-item:hover {
      background: var(--color-background-soft, #f0f0f0);
      border-color: var(--color-accent, #ff6d00);
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

  .breadcrumb-user {
    font-size: 13px;
    font-weight: 600;
    color: var(--color-text, #212529);
    white-space: nowrap;
  }

  .breadcrumb-preview {
    font-size: 12px;
    color: var(--color-text-muted, #8a8a8e);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 200px;
  }

  .breadcrumb-arrow {
    width: 14px;
    height: 14px;
    color: var(--color-text-muted, #8a8a8e);
    flex-shrink: 0;
  }
</style>

<style scoped>
  .chapters-container {
    width: 100%;
  }

  .chapters-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    gap: 20px;
  }

  .chapters-sort {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .chapters-sort-label {
    font-weight: 500;
    color: var(--text-primary);
  }

  .chapters-sort-options {
    display: flex;
    gap: 5px;
  }

    .chapters-sort-options button.active {
      background-color: var(--primary-color);
      color: white;
    }

  .chapters-filter {
    flex-shrink: 0;
  }

  .chapters-search {
    padding: 8px 12px;
    border: 1px solid var(--border-base);
    border-radius: 4px;
    background-color: var(--background-elevated);
    color: var(--text-primary);
    width: 200px;
  }

    .chapters-search:focus {
      outline: none;
      border-color: var(--primary-color);
    }

  .chapters-table {
    background-color: var(--background-elevated);
    border-radius: 8px;
    overflow: hidden;
  }

  .chapters-table-header {
    display: grid;
    grid-template-columns: 120px 1fr 150px 120px;
    gap: 15px;
    padding: 15px 20px;
    background-color: var(--background-elevated-2);
    font-weight: 600;
    color: var(--text-muted);
    font-size: 0.9rem;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  .chapters-table-body {
    display: flex;
    flex-direction: column;
  }

  .chapter-row {
    display: grid;
    grid-template-columns: 120px 1fr 150px 120px;
    gap: 15px;
    padding: 15px 20px;
    color: var(--text-primary);
    text-decoration: none;
    border-bottom: 1px solid var(--border-base);
    transition: background-color 0.2s ease;
  }

    .chapter-row:hover {
      background-color: var(--background-elevated-2);
    }

    .chapter-row:last-child {
      border-bottom: none;
    }

  .chapter-number {
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .volume-badge, .chapter-badge {
    font-size: 0.8rem;
    padding: 2px 6px;
    border-radius: 4px;
    font-weight: 500;
  }

  .volume-badge {
    background-color: rgba(156, 39, 176, 0.2);
    color: #ba68c8;
  }

  .chapter-badge {
    background-color: rgba(33, 150, 243, 0.2);
    color: #64b5f6;
  }

  .chapter-name {
    font-weight: 500;
    line-height: 1.4;
  }

  .chapter-team {
    color: var(--text-muted);
    font-size: 0.9rem;
  }

  .chapter-date {
    color: var(--text-muted);
    font-size: 0.9rem;
  }

  .empty-chapters {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 60px 20px;
    text-align: center;
    color: var(--text-muted);
  }

  .empty-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
    opacity: 0.5;
  }

  .empty-text {
    font-size: 1.1rem;
    margin-bottom: 0.5rem;
  }

  .chapters-pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 15px;
    margin-top: 20px;
    padding: 20px;
  }

  .pagination-info {
    font-size: 0.9rem;
    color: var(--text-muted);
  }

  /* Mobile responsive */
  @media (max-width: 768px) {
    .chapters-header {
      flex-direction: column;
      align-items: stretch;
      gap: 15px;
    }

    .chapters-search {
      width: 100%;
    }

    .chapters-table-header,
    .chapter-row {
      grid-template-columns: 1fr 120px 100px;
      gap: 10px;
    }

    .chapter-team {
      display: none;
    }

    .chapter-number {
      flex-direction: row;
      gap: 8px;
    }

    .volume-badge,
    .chapter-badge {
      font-size: 0.7rem;
    }
  }

  @media (max-width: 480px) {
    .chapters-table-header,
    .chapter-row {
      grid-template-columns: 1fr 80px;
      gap: 10px;
    }

    .chapter-date {
      display: none;
    }

    .chapters-sort-options {
      flex-direction: column;
      gap: 5px;
    }

      .chapters-sort-options button {
        font-size: 0.8rem;
        padding: 6px 12px;
      }
  }
</style>
