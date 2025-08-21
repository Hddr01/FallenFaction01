<!-- Enhanced Comments Component with Improved Error Handling -->
<template>
  <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl overflow-hidden">
    <!-- Comments Header -->
    <div class="p-6 border-b border-[var(--color-border)]">
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-xl font-semibold text-[var(--color-text)]">
          Comments ({{ totalComments }})
        </h3>

        <!-- Sort Options -->
        <div class="flex items-center space-x-2">
          <span class="text-sm text-[var(--color-text)] opacity-60">Sort by:</span>
          <select v-model="currentSort"
                  @change="handleSortChange"
                  class="bg-[var(--color-background-mute)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-3 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-transparent">
            <option value="newest">Newest</option>
            <option value="oldest">Oldest</option>
            <option value="likes">Most Liked</option>
          </select>
        </div>
      </div>

      <!-- Comments Disabled Notice -->
      <div v-if="commentsDisabled"
           class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4 mb-4">
        <div class="flex items-center">
          <svg class="w-5 h-5 text-amber-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
          </svg>
          <span class="text-amber-800 dark:text-amber-200 text-sm font-medium">
            Comments have been disabled for this content.
          </span>
        </div>
      </div>

      <!-- Debug Info (only show in development) -->
      <div v-if="showDebugInfo && (debugMode || error)"
           class="bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg p-3 mb-4 text-xs">
        <details>
          <summary class="cursor-pointer font-medium text-gray-700 dark:text-gray-300">Debug Info</summary>
          <div class="mt-2 space-y-1 text-gray-600 dark:text-gray-400">
            <div><strong>Target ID:</strong> {{ targetId }}</div>
            <div><strong>Target Type:</strong> {{ targetType }}</div>
            <div><strong>Comments Enabled:</strong> {{ commentsEnabled }}</div>
            <div><strong>Comments Disabled:</strong> {{ commentsDisabled }}</div>
            <div><strong>API Call Made:</strong> {{ apiCallMade }}</div>
            <div><strong>API Error:</strong> {{ apiError || 'None' }}</div>
            <div><strong>Stats Loaded:</strong> {{ statsLoaded }}</div>
          </div>
        </details>
      </div>

      <!-- Comment Form -->
      <div v-if="!commentsDisabled" class="space-y-4">
        <!-- Auth Check -->
        <div v-if="!isAuthenticated"
             class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
          <div class="flex items-center justify-between">
            <div class="flex items-center">
              <svg class="w-5 h-5 text-blue-500 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
              </svg>
              <span class="text-blue-800 dark:text-blue-200 text-sm">
                Sign in to join the conversation
              </span>
            </div>
            <button @click="goToLogin"
                    class="bg-[var(--color-accent)] text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-[var(--color-accent-hover)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] transition-colors duration-200">
              Sign In
            </button>
          </div>
        </div>

        <!-- Comment Input -->
        <div v-else class="space-y-3">
          <div class="flex space-x-3">
            <!-- User Avatar -->
            <div class="flex-shrink-0">
              <div class="w-10 h-10 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-full flex items-center justify-center">
                <svg class="w-5 h-5 text-[var(--color-text)] opacity-60" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                </svg>
              </div>
            </div>

            <!-- Comment Input -->
            <div class="flex-1">
              <textarea v-model="newCommentText"
                        :disabled="submittingComment"
                        placeholder="Share your thoughts..."
                        rows="3"
                        maxlength="2000"
                        class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-3 focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-transparent resize-none transition-colors duration-200"
                        @keydown.ctrl.enter="submitComment"
                        @keydown.meta.enter="submitComment"></textarea>

              <!-- Character Count & Actions -->
              <div class="flex items-center justify-between mt-2">
                <span class="text-xs text-[var(--color-text)] opacity-60">
                  {{ newCommentText.length }}/2000 characters
                </span>
                <div class="flex items-center space-x-2">
                  <span class="text-xs text-[var(--color-text)] opacity-60">
                    Ctrl+Enter to post
                  </span>
                  <button @click="submitComment"
                          :disabled="!newCommentText.trim() || submittingComment"
                          class="bg-[var(--color-accent)] text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-[var(--color-accent-hover)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    <svg v-if="submittingComment" class="animate-spin w-4 h-4 mr-2 inline" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    {{ submittingComment ? 'Posting...' : 'Post Comment' }}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Comments List -->
    <div class="p-6">
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

      <!-- Error State -->
      <div v-else-if="error" class="text-center py-12">
        <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-6 max-w-md mx-auto">
          <svg class="w-12 h-12 text-red-500 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
          </svg>
          <h3 class="text-lg font-medium text-red-800 dark:text-red-200 mb-2">Error Loading Comments</h3>
          <p class="text-red-700 dark:text-red-300 text-sm mb-4">{{ error }}</p>
          <button @click="retryLoad"
                  class="bg-red-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
            Try Again
          </button>
        </div>
      </div>

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
      <!-- Fixed template section for CommentsComponent.vue -->
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
                     :reply-depth="0"
                     :max-reply-depth="3"
                     @reply-added="onReplyAdded"
                     @comment-updated="onCommentUpdated"
                     @comment-deleted="onCommentDeleted" />

        <!-- Load More Button -->
        <div v-if="hasMoreComments" class="text-center pt-6">
          <button @click="loadMoreComments"
                  :disabled="loadingMore"
                  class="bg-[var(--color-background-mute)] border border-[var(--color-border)] text-[var(--color-text)] px-6 py-3 rounded-lg font-medium hover:bg-[var(--color-background-soft)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
            <svg v-if="loadingMore" class="animate-spin w-4 h-4 mr-2 inline" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            {{ loadingMore ? 'Loading...' : `Load More Comments (${remainingComments} remaining)` }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { commentsService } from '../../services/commentsService'
  import CommentItem from './CommentItem.vue'

  export default {
    name: 'CommentsSection',
    components: {
      CommentItem
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
      }
    },
    emits: ['comments-loaded', 'comment-added', 'comments-updated'],
    data() {
      return {
        comments: [],
        loading: true,
        error: null,
        commentsEnabled: true, // Default to true
        totalComments: 0,
        currentSort: 'newest',
        currentPage: 1,
        hasMoreComments: false,
        loadingMore: false,

        // Comment form
        newCommentText: '',
        submittingComment: false,

        // Pagination info
        pagination: {
          totalCount: 0,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false
        },

        // Debug info
        debugMode: process.env.NODE_ENV === 'development',
        showDebugInfo: true,
        apiCallMade: false,
        apiError: null,
        statsLoaded: false
      }
    },
    computed: {
      commentsDisabled() {
        // Comments are disabled if explicitly set to false
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
        await this.loadCommentStats()
        await this.loadComments()
      },

      async loadCommentStats() {
        try {
          this.apiCallMade = true
          this.apiError = null

          console.log('Loading comment stats for target:', this.targetId, 'type:', this.targetType)

          const result = await commentsService.getCommentStats(
            parseInt(this.targetId),
            parseInt(this.targetType)
          )

          console.log('Comment stats result:', result)

          if (result.success) {
            this.commentsEnabled = result.data.commentsEnabled !== false // Default to true if undefined
            this.totalComments = result.data.totalComments || 0
            this.statsLoaded = true

            console.log('Comments enabled:', this.commentsEnabled)
            console.log('Total comments:', this.totalComments)
          } else {
            console.error('Failed to load comment stats:', result.error)
            this.apiError = result.error
            // Don't disable comments on API failure - default to enabled
            this.commentsEnabled = true
          }
        } catch (error) {
          console.error('Error loading comment stats:', error)
          this.apiError = error.message
          // Don't disable comments on exception - default to enabled
          this.commentsEnabled = true
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

          console.log('Loading comments for target:', this.targetId, 'type:', this.targetType)

          const result = await commentsService.getComments(
            parseInt(this.targetId),
            parseInt(this.targetType),
            {
              page: this.currentPage,
              sortBy: this.currentSort,
              pageSize: 20
            }
          )

          console.log('Comments result:', result)

          if (result.success) {
            if (reset) {
              this.comments = result.data.comments
            } else {
              this.comments = [...this.comments, ...result.data.comments]
            }

            this.pagination = result.data.pagination
            this.hasMoreComments = result.data.pagination.hasNext
            this.totalComments = result.data.pagination.totalCount

            this.$emit('comments-loaded', {
              comments: this.comments,
              totalCount: this.totalComments
            })
          } else {
            this.error = result.error
            console.error('Failed to load comments:', result.error)
          }
        } catch (error) {
          console.error('Error loading comments:', error)
          this.error = 'Failed to load comments'
        } finally {
          this.loading = false
          this.loadingMore = false
        }
      },

      async loadMoreComments() {
        if (this.loadingMore || !this.hasMoreComments) return

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

        // Validate content
        const validation = commentsService.validateCommentContent(this.newCommentText)
        if (!validation.isValid) {
          this.showToast(validation.error, 'error')
          return
        }

        this.submittingComment = true

        try {
          const result = await commentsService.addComment(
            this.newCommentText,
            parseInt(this.targetId),
            parseInt(this.targetType)
          )

          if (result.success) {
            // Add new comment to the beginning of the list
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
        // Find the parent comment and add the reply
        const parentComment = this.findCommentById(reply.parentCommentId)
        if (parentComment) {
          if (!parentComment.replies) {
            parentComment.replies = []
          }
          parentComment.replies.push(reply)
          this.totalComments += 1

          this.$emit('comments-updated', {
            comments: this.comments,
            totalCount: this.totalComments
          })
        }
      },

      onCommentUpdated(updatedComment) {
        // Update the comment in the list
        const comment = this.findCommentById(updatedComment.id)
        if (comment) {
          Object.assign(comment, updatedComment)
        }
      },

      onCommentDeleted(deletedCommentId) {
        // Remove comment from list
        this.removeCommentById(deletedCommentId)
        this.totalComments = Math.max(0, this.totalComments - 1)

        this.$emit('comments-updated', {
          comments: this.comments,
          totalCount: this.totalComments
        })
      },

      findCommentById(commentId) {
        for (const comment of this.comments) {
          if (comment.id === commentId) {
            return comment
          }
          if (comment.replies) {
            const reply = comment.replies.find(r => r.id === commentId)
            if (reply) return reply
          }
        }
        return null
      },

      removeCommentById(commentId) {
        // Remove from top-level comments
        const index = this.comments.findIndex(c => c.id === commentId)
        if (index !== -1) {
          const removedComment = this.comments.splice(index, 1)[0]
          // Count total removed (comment + replies)
          const totalRemoved = 1 + (removedComment.replies?.length || 0)
          this.totalComments = Math.max(0, this.totalComments - totalRemoved)
          return
        }

        // Remove from replies
        for (const comment of this.comments) {
          if (comment.replies) {
            const replyIndex = comment.replies.findIndex(r => r.id === commentId)
            if (replyIndex !== -1) {
              comment.replies.splice(replyIndex, 1)
              this.totalComments = Math.max(0, this.totalComments - 1)
              return
            }
          }
        }
      },

      goToLogin() {
        const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
        window.location.href = `/account/login?returnUrl=${returnUrl}`
      },

      showToast(message, type = 'info') {
        // Create toast notification
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

        // Trigger animation
        setTimeout(() => {
          toast.classList.remove('translate-x-full', 'opacity-0')
        }, 100)

        // Auto remove after 5 seconds
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
