<!-- ThreadComment.vue - Individual comment within thread modal -->
<template>
  <article class="transition-all duration-200 hover:bg-gray-50 dark:hover:bg-gray-800/50 rounded-lg"
           :class="[
             { 'opacity-70': comment.isDeleted },
             indentClasses
           ]">

    <div class="flex gap-3 p-4">
      <!-- Connection Line for Indentation -->
      <div v-if="indentLevel > 0" class="flex items-start pt-2">
        <div class="flex items-center">
          <!-- Indent indicators -->
          <div v-for="level in indentLevel"
               :key="level"
               class="w-4 h-px bg-gray-300 dark:bg-gray-600 mr-1"></div>
          <div class="w-2 h-2 rounded-full bg-gray-300 dark:bg-gray-600 flex-shrink-0"></div>
        </div>
      </div>

      <!-- Avatar -->
      <div class="flex-shrink-0">
        <div class="w-9 h-9 bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-full flex items-center justify-center overflow-hidden"
             :class="{ 'opacity-50': comment.isDeleted }">
          <img v-if="comment.userAvatarUrl && !comment.isDeleted"
               :src="comment.userAvatarUrl"
               :alt="`${comment.userName}'s avatar`"
               class="w-full h-full object-cover" />
          <svg v-else class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
          </svg>
        </div>
      </div>

      <!-- Comment Content -->
      <div class="flex-1 min-w-0">
        <!-- Header -->
        <header class="flex items-center gap-2 mb-2">
          <h4 class="font-medium text-gray-900 dark:text-gray-100 text-sm"
              :class="{ 'opacity-50': comment.isDeleted }">
            {{ comment.isDeleted && !showDeletedContent ? '[Deleted]' : comment.userName }}
          </h4>
          <time :datetime="comment.postedDate"
                class="text-xs text-gray-500 dark:text-gray-400"
                :title="formatFullDate(comment.postedDate)">
            {{ formatDate(comment.postedDate) }}
          </time>

          <!-- Status Badges -->
          <span v-if="comment.isDeleted"
                class="bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 text-xs px-2 py-0.5 rounded-full font-medium">
            Deleted
          </span>
          <span v-if="isUserAdmin(comment.userId) && !comment.isDeleted"
                class="bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200 text-xs px-2 py-0.5 rounded-full font-medium">
            Admin
          </span>

          <!-- Depth indicator -->
          <span v-if="indentLevel > 2"
                class="bg-indigo-100 dark:bg-indigo-900 text-indigo-800 dark:text-indigo-200 text-xs px-2 py-0.5 rounded-full font-medium">
            Level {{ indentLevel + 1 }}
          </span>
        </header>

        <!-- Comment Text -->
        <div class="prose prose-sm max-w-none text-gray-900 dark:text-gray-100 mb-3">
          <!-- Deleted Comment Display -->
          <div v-if="comment.isDeleted">
            <p v-if="!showDeletedContent" class="italic text-gray-500 dark:text-gray-400">
              [This comment has been deleted]
            </p>
            <details v-else class="bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg p-3">
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

          <!-- Regular Comment -->
          <p v-else class="whitespace-pre-wrap break-words">{{ comment.content }}</p>
        </div>

        <!-- Actions -->
        <div class="flex items-center gap-4 mb-3">
          <!-- Upvote Button -->
          <button @click="toggleLike"
                  :disabled="!isAuthenticated || reactingToComment || comment.isDeleted"
                  class="flex items-center gap-1 text-sm font-medium transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                  :class="comment.currentUserLiked
              ? 'text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300'
              : 'text-gray-500 dark:text-gray-400 hover:text-green-600 dark:hover:text-green-400'">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
              <path d="M7 14l5-5 5 5z" />
            </svg>
            <span>{{ comment.likesCount || 0 }}</span>
          </button>

          <!-- Downvote Button -->
          <button @click="toggleDislike"
                  :disabled="!isAuthenticated || reactingToComment || comment.isDeleted"
                  class="flex items-center gap-1 text-sm font-medium transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
                  :class="comment.currentUserDisliked
              ? 'text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300'
              : 'text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400'">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
              <path d="M7 10l5 5 5-5z" />
            </svg>
            <span>{{ comment.dislikesCount || 0 }}</span>
          </button>

          <!-- Reply Button -->
          <button v-if="canReply && !comment.isDeleted"
                  @click="toggleReplyForm"
                  :disabled="!isAuthenticated"
                  class="flex items-center gap-1 text-sm font-medium text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 disabled:cursor-not-allowed transition-colors duration-200">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"></path>
            </svg>
            <span>Reply</span>
          </button>

          <!-- Delete Button -->
          <button v-if="canDelete && !comment.isDeleted"
                  @click="deleteComment"
                  :disabled="deletingComment"
                  class="flex items-center gap-1 text-sm font-medium text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
            <svg v-if="deletingComment" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
            </svg>
            <span>{{ deletingComment ? 'Deleting...' : 'Delete' }}</span>
          </button>

          <!-- Restore Button -->
          <button v-if="isAdmin && comment.isDeleted"
                  @click="restoreComment"
                  :disabled="restoringComment"
                  class="flex items-center gap-1 text-sm font-medium text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
            <svg v-if="restoringComment" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
                   class="mt-4 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border-l-4 border-blue-300 dark:border-blue-700" />
      </div>
    </div>
  </article>
</template>

<script>
  import { commentsService } from '../../services/commentsService'
  import { useToast } from '../../utils/toastService'
  import ReplyForm from './ReplyForm.vue'

  export default {
    name: 'ThreadComment',
    components: {
      ReplyForm
    },
    props: {
      comment: {
        type: Object,
        required: true
      },
      indentLevel: {
        type: Number,
        default: 0
      },
      targetId: {
        type: [Number, String],
        required: true
      },
      targetType: {
        type: [Number, String],
        required: true
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
      }
    },
    emits: ['comment-updated', 'comment-deleted', 'comment-restored', 'reply-added'],
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

      indentClasses() {
        // Add left border for deeply nested comments
        if (this.indentLevel > 2) {
          return 'border-l-2 border-indigo-200 dark:border-indigo-700 ml-2 pl-2'
        } else if (this.indentLevel > 0) {
          return 'border-l border-gray-200 dark:border-gray-700 ml-1 pl-1'
        }
        return ''
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
        this.showReplyForm = false
        this.$emit('reply-added', replyData)
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
        }
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
  /* Smooth transitions for hover effects */
  .transition-all {
    transition-property: all;
    transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
    transition-duration: 200ms;
  }
</style>
