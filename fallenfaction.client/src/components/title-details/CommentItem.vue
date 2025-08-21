<!-- Enhanced CommentItem.vue with Arrow Up/Down buttons instead of Like/Dislike -->
<template>
  <article class="comment-item"
           :class="{ 'is-reply': isReply, 'is-deleted': comment.isDeleted }"
           :aria-label="`Comment by ${comment.userName}`"
           role="article"
           :aria-describedby="comment.isDeleted ? `deleted-notice-${comment.id}` : undefined">

    <!-- Loading Screen for Delete Operation -->
    <LoadingScreen v-if="deletingComment"
                   loading-text="Deleting comment..."
                   @loading-complete="onDeleteComplete" />

    <!-- Loading Screen for Restore Operation -->
    <LoadingScreen v-else-if="restoringComment"
                   loading-text="Restoring comment..."
                   @loading-complete="onRestoreComplete" />

    <!-- Main Comment Content -->
    <div v-else class="flex space-x-3">
      <!-- Avatar -->
      <div class="flex-shrink-0">
        <div class="w-10 h-10 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-full flex items-center justify-center overflow-hidden"
             :class="{ 'opacity-50': comment.isDeleted }"
             :aria-label="`${comment.userName}'s avatar`">
          <img v-if="comment.userAvatarUrl && !comment.isDeleted"
               :src="comment.userAvatarUrl"
               :alt="`${comment.userName}'s avatar`"
               class="w-full h-full object-cover"
               loading="lazy" />
          <svg v-else
               class="w-5 h-5 text-[var(--color-text)] opacity-60"
               fill="none"
               stroke="currentColor"
               viewBox="0 0 24 24"
               aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
          </svg>
        </div>
      </div>

      <!-- Comment Content -->
      <div class="flex-1 min-w-0">
        <!-- Header -->
        <header class="flex items-center space-x-2 mb-2">
          <h4 class="font-medium text-[var(--color-text)] text-sm"
              :class="{ 'opacity-50': comment.isDeleted }">
            {{ comment.isDeleted && !showDeletedContent ? '[Deleted]' : comment.userName }}
          </h4>
          <time :datetime="comment.postedDate"
                class="text-xs text-[var(--color-text)] opacity-60"
                :title="formatFullDate(comment.postedDate)">
            {{ formatDate(comment.postedDate) }}
          </time>

          <!-- Status Badges -->
          <span v-if="comment.isDeleted"
                :id="`deleted-notice-${comment.id}`"
                class="bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300 text-xs px-2 py-0.5 rounded-full font-medium"
                role="status"
                aria-label="This comment has been deleted">
            Deleted
          </span>
          <span v-if="isUserAdmin(comment.userId) && !comment.isDeleted"
                class="bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200 text-xs px-2 py-0.5 rounded-full font-medium"
                role="status"
                aria-label="Administrator">
            Admin
          </span>
        </header>

        <!-- Comment Text -->
        <div class="prose prose-sm max-w-none text-[var(--color-text)] mb-3">
          <!-- Deleted Comment Display -->
          <div v-if="comment.isDeleted" role="region" aria-labelledby="`deleted-notice-${comment.id}`">
            <!-- Default deleted message -->
            <p v-if="!showDeletedContent" class="italic text-gray-500 dark:text-gray-400">
              [This comment has been deleted]
            </p>

            <!-- Show original content for admins or original author -->
            <div v-else class="space-y-2">
              <details class="bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg p-3">
                <summary class="text-xs text-gray-600 dark:text-gray-400 mb-2 cursor-pointer">
                  Original Content
                  <span v-if="canSeeDeletedDetails">
                    - Deleted {{ formatDate(comment.deletedAt) }}
                    <span v-if="comment.deletedByUserName">by {{ comment.deletedByUserName }}</span>
                    <span v-if="comment.deletionReason" class="block mt-1">Reason: {{ comment.deletionReason }}</span>
                  </span>
                </summary>
                <p class="whitespace-pre-wrap break-words opacity-70">{{ comment.content }}</p>
              </details>
            </div>
          </div>

          <!-- Regular Comment Display -->
          <p v-else class="whitespace-pre-wrap break-words">{{ comment.content }}</p>
        </div>

        <!-- Actions -->
        <div v-if="!comment.isDeleted || isAdmin"
             class="flex items-center space-x-4"
             role="group"
             :aria-label="`Actions for comment by ${comment.userName}`">

          <!-- Upvote Button (Arrow Up - Green) -->
          <button @click="toggleLike"
                  :disabled="!isAuthenticated || reactingToComment || comment.isDeleted"
                  :aria-label="`${comment.currentUserLiked ? 'Remove upvote' : 'Upvote'} comment. Current upvotes: ${comment.likesCount}`"
                  :class="upvoteButtonClasses"
                  type="button">
            <svg class="w-4 h-4"
                 fill="currentColor"
                 viewBox="0 0 24 24"
                 aria-hidden="true">
              <path d="M7 14l5-5 5 5z" />
            </svg>
            <span>{{ comment.likesCount || 0 }}</span>
          </button>

          <!-- Downvote Button (Arrow Down - Red) -->
          <button @click="toggleDislike"
                  :disabled="!isAuthenticated || reactingToComment || comment.isDeleted"
                  :aria-label="`${comment.currentUserDisliked ? 'Remove downvote' : 'Downvote'} comment. Current downvotes: ${comment.dislikesCount}`"
                  :class="downvoteButtonClasses"
                  type="button">
            <svg class="w-4 h-4"
                 fill="currentColor"
                 viewBox="0 0 24 24"
                 aria-hidden="true">
              <path d="M7 10l5 5 5-5z" />
            </svg>
            <span>{{ comment.dislikesCount || 0 }}</span>
          </button>

          <!-- Reply Button -->
          <button v-if="canReply && !comment.isDeleted && replyDepth < maxReplyDepth"
                  @click="toggleReplyForm"
                  :disabled="!isAuthenticated"
                  :aria-label="showReplyForm ? 'Cancel reply' : 'Reply to comment'"
                  :aria-expanded="showReplyForm"
                  class="flex items-center space-x-1 text-sm font-medium text-[var(--color-text)] opacity-60 hover:opacity-100 disabled:cursor-not-allowed transition-opacity duration-200"
                  type="button">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"></path>
            </svg>
            <span>Reply</span>
          </button>

          <!-- Delete Button -->
          <button v-if="canDelete && !comment.isDeleted"
                  @click="deleteComment"
                  :disabled="deletingComment"
                  :aria-label="`Delete ${isAdmin ? '(soft delete)' : ''} comment`"
                  class="flex items-center space-x-1 text-sm font-medium text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                  type="button">
            <!-- Simple loading indicator -->
            <svg v-if="deletingComment" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
            </svg>
            <span>{{ deletingComment ? 'Deleting...' : 'Delete' }}</span>
          </button>

          <!-- Restore Button -->
          <button v-if="isAdmin && comment.isDeleted"
                  @click="restoreComment"
                  :disabled="restoringComment"
                  :aria-label="restoringComment ? 'Restoring comment...' : 'Restore deleted comment'"
                  class="flex items-center space-x-1 text-sm font-medium text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200"
                  type="button">
            <!-- Simple loading indicator -->
            <svg v-if="restoringComment" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
            </svg>
            <span>{{ restoringComment ? 'Restoring...' : 'Restore' }}</span>
          </button>
        </div>

        <!-- Reply Form -->
        <ReplyForm v-if="showReplyForm && !comment.isDeleted"
                   :target-id="targetId"
                   :target-type="targetType"
                   :parent-comment-id="comment.id"
                   :submitting="submittingReply"
                   @reply-submitted="onReplySubmitted"
                   @reply-cancelled="cancelReply"
                   class="mt-4" />

        <!-- Replies -->
        <div v-if="comment.replies && comment.replies.length > 0" class="mt-4 space-y-4" role="region" aria-label="Replies">
          <CommentItem v-for="reply in comment.replies"
                       :key="reply.id"
                       :comment="reply"
                       :target-id="targetId"
                       :target-type="targetType"
                       :is-reply="true"
                       :is-authenticated="isAuthenticated"
                       :current-user-id="currentUserId"
                       :is-admin="isAdmin"
                       :can-reply="canReply"
                       :reply-depth="replyDepth + 1"
                       :max-reply-depth="maxReplyDepth"
                       @comment-updated="$emit('comment-updated', $event)"
                       @comment-deleted="$emit('comment-deleted', $event)"
                       @comment-restored="$emit('comment-restored', $event)" />
        </div>
      </div>
    </div>
  </article>
</template>

<script>
  import { commentsService } from '../../services/commentsService'
  import { useToast } from '../../utils/toastService'
  import LoadingScreen from '../../LoadingScreen.vue'
  import ReplyForm from './ReplyForm.vue'

  export default {
    name: 'CommentItem',
    components: {
      LoadingScreen,
      ReplyForm
    },
    props: {
      comment: {
        type: Object,
        required: true
      },
      isReply: {
        type: Boolean,
        default: false
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
      canReply: {
        type: Boolean,
        default: true
      },
      targetId: {
        type: [Number, String],
        required: true
      },
      targetType: {
        type: [Number, String],
        required: true
      },
      replyDepth: {
        type: Number,
        default: 0
      },
      maxReplyDepth: {
        type: Number,
        default: 3
      }
    },
    emits: ['reply-added', 'comment-updated', 'comment-deleted', 'comment-restored'],
    setup() {
      const { success, error } = useToast();
      return { showSuccessToast: success, showErrorToast: error };
    },
    data() {
      return {
        showReplyForm: false,
        submittingReply: false,
        reactingToComment: false,
        deletingComment: false,
        restoringComment: false
      }
    },
    computed: {
      canDelete() {
        if (!this.isAuthenticated || this.comment.isDeleted) return false
        return this.comment.userId === this.currentUserId || this.isAdmin
      },

      showDeletedContent() {
        if (!this.comment.isDeleted) return false
        return this.isAdmin || this.comment.userId === this.currentUserId
      },

      canSeeDeletedDetails() {
        return this.isAdmin
      },

      upvoteButtonClasses() {
        const baseClasses = 'flex items-center space-x-1 text-sm font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors duration-200'

        if (!this.isAuthenticated || this.comment.isDeleted) {
          return `${baseClasses} cursor-not-allowed opacity-50 text-gray-400`
        }

        if (this.comment.currentUserLiked) {
          return `${baseClasses} text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300 focus:ring-green-500`
        }

        return `${baseClasses} text-gray-500 dark:text-gray-400 hover:text-green-600 dark:hover:text-green-400 focus:ring-green-500`
      },

      downvoteButtonClasses() {
        const baseClasses = 'flex items-center space-x-1 text-sm font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors duration-200'

        if (!this.isAuthenticated || this.comment.isDeleted) {
          return `${baseClasses} cursor-not-allowed opacity-50 text-gray-400`
        }

        if (this.comment.currentUserDisliked) {
          return `${baseClasses} text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 focus:ring-red-500`
        }

        return `${baseClasses} text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 focus:ring-red-500`
      }
    },
    methods: {
      async toggleLike() {
        if (!this.isAuthenticated || this.reactingToComment || this.comment.isDeleted) return
        await this.reactToComment(true)
      },

      async toggleDislike() {
        if (!this.isAuthenticated || this.reactingToComment || this.comment.isDeleted) return
        await this.reactToComment(false)
      },

      async reactToComment(isLike) {
        if (this.reactingToComment) return

        this.reactingToComment = true

        try {
          const result = await commentsService.reactToComment(this.comment.id, isLike)

          if (result.success) {
            this.comment.likesCount = result.data.likesCount
            this.comment.dislikesCount = result.data.dislikesCount
            this.comment.currentUserLiked = result.data.userLiked
            this.comment.currentUserDisliked = result.data.userDisliked

            this.$emit('comment-updated', this.comment)
          } else {
            this.showErrorToast(result.error)
          }
        } catch (error) {
          console.error('Error reacting to comment:', error)
          this.showErrorToast('Failed to update reaction')
        } finally {
          this.reactingToComment = false
        }
      },

      toggleReplyForm() {
        if (!this.isAuthenticated || this.comment.isDeleted) return
        this.showReplyForm = !this.showReplyForm
      },

      async onReplySubmitted(replyData) {
        this.$emit('reply-added', replyData)
        this.showReplyForm = false
        this.showSuccessToast('Reply posted successfully!')
      },

      cancelReply() {
        this.showReplyForm = false
      },

      async deleteComment() {
        if (!this.canDelete || this.deletingComment) return

        const confirmMessage = this.isAdmin
          ? 'Are you sure you want to soft-delete this comment? It can be restored later.'
          : 'Are you sure you want to delete this comment? This action cannot be undone.'

        if (!confirm(confirmMessage)) return

        this.deletingComment = true

        try {
          const result = this.isAdmin
            ? await commentsService.deleteCommentAsAdmin(this.comment.id, 'Deleted by administrator')
            : await commentsService.deleteComment(this.comment.id)

          if (result.success) {
            this.comment.isDeleted = true
            this.comment.deletedAt = new Date().toISOString()

            this.$emit('comment-deleted', this.comment.id)
            this.showSuccessToast(result.message)
          } else {
            this.showErrorToast(result.error)
          }
        } catch (error) {
          console.error('Error deleting comment:', error)
          this.showErrorToast('Failed to delete comment')
        } finally {
          this.deletingComment = false
        }
      },

      async restoreComment() {
        if (!this.isAdmin || this.restoringComment) return

        if (!confirm('Are you sure you want to restore this comment?')) return

        this.restoringComment = true

        try {
          const result = await commentsService.restoreCommentAsAdmin(this.comment.id)

          if (result.success) {
            this.comment.isDeleted = false
            this.comment.deletedAt = null
            this.comment.deletedByUserName = null
            this.comment.deletionReason = null

            this.$emit('comment-restored', this.comment.id)
            this.showSuccessToast('Comment restored successfully')
          } else {
            this.showErrorToast(result.error)
          }
        } catch (error) {
          console.error('Error restoring comment:', error)
          this.showErrorToast('Failed to restore comment')
        } finally {
          this.restoringComment = false
        }
      },

      onDeleteComplete() {
        this.deletingComment = false
      },

      onRestoreComplete() {
        this.restoringComment = false
      },

      formatDate(dateString) {
        if (!dateString) return ''
        return commentsService.formatCommentDate(dateString)
      },

      formatFullDate(dateString) {
        if (!dateString) return ''
        return new Date(dateString).toLocaleString()
      },

      isUserAdmin(userId) {
        return this.isAdmin && userId === this.currentUserId
      }
    }
  }
</script>

<style scoped>
  .comment-item.is-reply {
    margin-left: 1.5rem;
    padding-left: 1rem;
    border-left: 2px solid var(--color-border);
  }

    /* Reduce indentation for deeply nested replies to prevent excessive narrowing */
    .comment-item.is-reply .comment-item.is-reply {
      margin-left: 1rem;
      padding-left: 0.75rem;
      border-left: 1px solid var(--color-border);
    }

      .comment-item.is-reply .comment-item.is-reply .comment-item.is-reply {
        margin-left: 0.75rem;
        padding-left: 0.5rem;
        border-left: 1px dashed var(--color-border);
      }

  .comment-item.is-deleted {
    opacity: 0.8;
  }

  .prose p {
    margin-bottom: 0;
    line-height: 1.625;
  }

  button:focus {
    outline: none;
  }

  @media (prefers-contrast: high) {
    .comment-item {
      border: 1px solid var(--color-border);
    }
  }

  /* Mobile responsive - reduce nesting on small screens */
  @media (max-width: 768px) {
    .comment-item.is-reply {
      margin-left: 1rem;
      padding-left: 0.75rem;
    }

      .comment-item.is-reply .comment-item.is-reply {
        margin-left: 0.5rem;
        padding-left: 0.5rem;
      }

        .comment-item.is-reply .comment-item.is-reply .comment-item.is-reply {
          margin-left: 0.25rem;
          padding-left: 0.25rem;
        }
  }
</style>
