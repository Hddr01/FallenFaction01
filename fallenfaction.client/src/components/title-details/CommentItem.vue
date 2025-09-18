<!-- Enhanced CommentItem.vue with Thread Integration after 3 levels -->
<template>
  <article class="relative"
           :class="{ 'comment-deleted': comment.isDeleted }"
           :aria-label="`Comment by ${comment.userName}`"
           role="article">

    <!-- Loading Screen for Delete Operation -->
    <LoadingScreen v-if="deletingComment"
                   loading-text="Deleting comment..."
                   @loading-complete="deletingComment = false" />

    <!-- Loading Screen for Restore Operation -->
    <LoadingScreen v-else-if="restoringComment"
                   loading-text="Restoring comment..."
                   @loading-complete="restoringComment = false" />

    <!-- Main Comment Content -->
    <div v-else class="flex gap-3">
      <!-- Avatar -->
      <div class="flex-shrink-0">
        <div class="comment-avatar"
             :class="{ 'comment-avatar-deleted': comment.isDeleted }">
          <img v-if="comment.userAvatarUrl && !comment.isDeleted"
               :src="comment.userAvatarUrl"
               :alt="`${comment.userName}'s avatar`"
               class="w-full h-full object-cover" />
          <svg v-else
               class="w-5 h-5 text-gray-400"
               fill="none"
               stroke="currentColor"
               viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
          </svg>
        </div>
      </div>

      <!-- Comment Content -->
      <div class="flex-1 min-w-0">
        <!-- Header -->
        <header class="flex items-center gap-2 mb-2">
          <h4 class="comment-username"
              :class="{ 'comment-username-deleted': comment.isDeleted }">
            {{ comment.isDeleted && !showDeletedContent ? '[Deleted]' : comment.userName }}
          </h4>
          <time :datetime="comment.postedDate"
                class="comment-timestamp"
                :title="formatFullDate(comment.postedDate)">
            {{ formatDate(comment.postedDate) }}
          </time>

          <!-- Status Badges -->
          <span v-if="comment.isDeleted" class="status-badge status-badge-deleted">
            Deleted
          </span>
          <span v-if="isUserAdmin(comment.userId) && !comment.isDeleted"
                class="status-badge status-badge-admin">
            Admin
          </span>
        </header>

        <!-- Comment Text -->
        <div class="comment-content">
          <!-- Deleted Comment Display -->
          <div v-if="comment.isDeleted">
            <p v-if="!showDeletedContent" class="deleted-message">
              [This comment has been deleted]
            </p>
            <details v-else class="deleted-details">
              <summary class="deleted-summary">
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
        <div v-if="!comment.isDeleted || isAdmin"
             class="flex items-center gap-4 mb-3">

          <!-- Upvote Button -->
          <button @click="toggleLike"
                  :disabled="!isAuthenticated || reactingToComment || comment.isDeleted"
                  class="reaction-button"
                  :class="[
                    'reaction-upvote',
                    comment.currentUserLiked ? 'reaction-upvote-active' : ''
                  ]">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
              <path d="M7 14l5-5 5 5z" />
            </svg>
            <span>{{ comment.likesCount || 0 }}</span>
          </button>

          <!-- Downvote Button -->
          <button @click="toggleDislike"
                  :disabled="!isAuthenticated || reactingToComment || comment.isDeleted"
                  class="reaction-button"
                  :class="[
                    'reaction-downvote',
                    comment.currentUserDisliked ? 'reaction-downvote-active' : ''
                  ]">
            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
              <path d="M7 10l5 5 5-5z" />
            </svg>
            <span>{{ comment.dislikesCount || 0 }}</span>
          </button>

          <!-- Reply Button -->
          <button v-if="canReply && !comment.isDeleted"
                  @click="toggleReplyForm"
                  :disabled="!isAuthenticated"
                  class="action-button action-button-reply">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"></path>
            </svg>
            <span>Reply</span>
          </button>

          <!-- Delete Button -->
          <button v-if="canDelete && !comment.isDeleted"
                  @click="deleteComment"
                  :disabled="deletingComment"
                  class="action-button action-button-delete">
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
                  class="action-button action-button-restore">
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
                   class="reply-form" />

        <!-- Collapsed State Message -->
        <div v-if="repliesCollapsed && comment.replies?.length > 0" class="mb-4">
          <button @click="toggleRepliesCollapsed"
                  class="collapsed-replies-button">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
            </svg>
            <span>Show {{ comment.replies.length }} hidden {{ comment.replies.length === 1 ? 'reply' : 'replies' }}</span>
          </button>
        </div>

        <!-- Replies Section -->
        <div v-if="!repliesCollapsed && comment.replies?.length > 0" class="space-y-4">

          <!-- Thread System: After depth 2 (3rd level), show thread button instead of nested comments -->
          <div v-if="depth >= 2" class="thread-transition">
            <button @click="openThreadModal"
                    class="thread-button">
              <div class="thread-button-content">
                <div class="flex items-center gap-2">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
                  </svg>
                  <span class="font-medium">Continue in Thread</span>
                </div>
                <div class="thread-stats">
                  <span>{{ getTotalNestedRepliesCount }} • {{ uniqueParticipantsCount }} {{ uniqueParticipantsCount === 1 ? 'participant' : 'participants' }}</span>
                </div>
              </div>
              <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
              </svg>
            </button>
          </div>

          <!-- Regular nested replies (depth < 2) -->
          <div v-else class="relative">

            <!-- MAIN THREAD (depth === 0) -->
            <div v-if="hasNestedReplies && depth === 0">
              <!-- Expanded: Show unified thread line -->
              <button v-if="!mainThreadCollapsed"
                      @click="toggleMainThreadCollapse"
                      :title="getMainThreadTitle"
                      class="thread-line-button thread-line-main"
                      :style="{ height: mainThreadHeight + 'px' }">
                <div class="thread-line thread-line-main-visual"></div>
              </button>

              <!-- Collapsed: Show "See hidden replies" button -->
              <div v-else class="mb-4 pb-2">
                <button @click="toggleMainThreadCollapse"
                        class="expand-button"
                        :title="'Click to expand ' + getTotalNestedRepliesCount">
                  <span>See {{ getTotalNestedRepliesCount }}</span>
                </button>
              </div>
            </div>

            <!-- INDIVIDUAL THREADS (depth > 0) -->
            <div v-else-if="depth > 0">
              <!-- Expanded: Show individual thread line -->
              <button v-if="!isThreadCollapsed(comment.id)"
                      @click="toggleThreadCollapse(comment.id)"
                      :title="getThreadLineTitle(comment.id)"
                      class="thread-line-button thread-line-individual"
                      :style="{ left: getThreadLineButtonOffset(depth) }">
                <div class="thread-line thread-line-individual-visual"></div>
              </button>

              <!-- Collapsed: Show "See hidden replies" button -->
              <div v-else class="mb-4 pb-2 flex">
                <div :class="getCollapsedButtonContainerClasses(depth)">
                  <button @click="toggleThreadCollapse(comment.id)"
                          class="expand-button"
                          :title="'Click to expand ' + getThreadReplyCount(comment)">
                    <span>See {{ getThreadReplyCount(comment) }}</span>
                  </button>
                </div>
              </div>
            </div>

            <!-- Reply Content Container - Only show when not collapsed -->
            <div v-if="!mainThreadCollapsed && !isThreadCollapsed(comment.id)"
                 :class="getReplyContainerClasses"
                 :style="getReplyContainerStyles">

              <div v-for="(reply, index) in comment.replies"
                   :key="reply.id"
                   :class="getReplyClasses(depth)">

                <div class="transition-all duration-300">
                  <CommentItem :comment="reply"
                               :target-id="targetId"
                               :target-type="targetType"
                               :is-authenticated="isAuthenticated"
                               :current-user-id="currentUserId"
                               :is-admin="isAdmin"
                               :can-reply="canReply"
                               :depth="depth + 1"
                               @comment-updated="$emit('comment-updated', $event)"
                               @comment-deleted="$emit('comment-deleted', $event)"
                               @comment-restored="$emit('comment-restored', $event)"
                               @reply-added="$emit('reply-added', $event)" />
                </div>

              </div>
            </div>

          </div>
        </div>
      </div>
    </div>

    <!-- Thread View Modal -->
    <ThreadViewModal v-if="showThreadModal"
                     :root-comment="comment"
                     :target-id="targetId"
                     :target-type="targetType"
                     :is-authenticated="isAuthenticated"
                     :current-user-id="currentUserId"
                     :is-admin="isAdmin"
                     :can-reply="canReply"
                     @close="closeThreadModal"
                     @comment-updated="$emit('comment-updated', $event)"
                     @comment-deleted="$emit('comment-deleted', $event)"
                     @comment-restored="$emit('comment-restored', $event)"
                     @reply-added="$emit('reply-added', $event)" />
  </article>
</template>

<script>
  import { commentsService } from '../../services/commentsService'
  import { useToast } from '../../utils/toastService'
  import LoadingScreen from '../../LoadingScreen.vue'
  import ReplyForm from './ReplyForm.vue'
  import ThreadViewModal from './ThreadViewModal.vue'

  export default {
    name: 'CommentItem',
    components: {
      LoadingScreen,
      ReplyForm,
      ThreadViewModal
    },
    props: {
      comment: {
        type: Object,
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
      },
      targetId: {
        type: [Number, String],
        required: true
      },
      targetType: {
        type: [Number, String],
        required: true
      },
      depth: {
        type: Number,
        default: 0
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
        restoringComment: false,
        repliesCollapsed: false,
        collapsedThreads: new Set(),
        mainThreadCollapsed: false,
        mainThreadHeight: 0,
        showThreadModal: false
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

      hasNestedReplies() {
        return this.comment.replies && this.comment.replies.length > 0
      },

      getMainThreadTitle() {
        return this.mainThreadCollapsed ? 'Click to expand entire thread' : 'Click to collapse entire thread'
      },

      getTotalNestedRepliesCount() {
        const count = this.countAllNestedReplies(this.comment)
        return count === 1 ? '1 reply' : `${count} replies`
      },

      uniqueParticipantsCount() {
        const participants = new Set()
        participants.add(this.comment.userName)

        const addParticipants = (replies) => {
          replies.forEach(reply => {
            participants.add(reply.userName)
            if (reply.replies && reply.replies.length > 0) {
              addParticipants(reply.replies)
            }
          })
        }

        if (this.comment.replies) {
          addParticipants(this.comment.replies)
        }

        return participants.size
      },

      getReplyContainerClasses() {
        if (this.depth === 0) return 'relative'
        return this.getReplyClasses(this.depth)
      },

      getReplyContainerStyles() {
        if (this.depth === 0 && this.hasNestedReplies) {
          return { marginLeft: '20px' }
        }
        return {}
      }
    },
    mounted() {
      this.calculateMainThreadHeight()
    },
    updated() {
      this.calculateMainThreadHeight()
    },
    methods: {
      openThreadModal() {
        this.showThreadModal = true
        // Prevent body scroll when modal is open
        document.body.style.overflow = 'hidden'
      },

      closeThreadModal() {
        this.showThreadModal = false
        // Restore body scroll
        document.body.style.overflow = ''
      },

      calculateMainThreadHeight() {
        if (this.depth === 0 && this.hasNestedReplies && !this.mainThreadCollapsed) {
          this.$nextTick(() => {
            const repliesContainer = this.$el.querySelector('.space-y-4')
            if (repliesContainer) {
              this.mainThreadHeight = repliesContainer.offsetHeight
            }
          })
        }
      },

      countAllNestedReplies(comment) {
        let count = 0
        if (comment.replies && comment.replies.length > 0) {
          count += comment.replies.length
          comment.replies.forEach(reply => {
            count += this.countAllNestedReplies(reply)
          })
        }
        return count
      },

      toggleMainThreadCollapse() {
        this.mainThreadCollapsed = !this.mainThreadCollapsed
      },

      getReplyClasses(depth) {
        const baseMargin = 'ml-6'
        if (depth === 0) return baseMargin
        if (depth === 1) return `${baseMargin} pl-4`
        if (depth === 2) return `${baseMargin} pl-2`
        return `${baseMargin} pl-1`
      },

      getThreadLineButtonOffset(depth) {
        if (depth === 1) return '-12px'
        if (depth === 2) return '-12px'
        return '-10px'
      },

      getCollapsedButtonContainerClasses(depth) {
        if (depth === 1) return 'ml-2'
        if (depth === 2) return 'ml-4'
        return 'ml-6'
      },

      getThreadLineTitle(replyId) {
        const isCollapsed = this.isThreadCollapsed(replyId)
        return isCollapsed ? 'Click to expand this thread' : 'Click to collapse this thread'
      },

      toggleThreadCollapse(replyId) {
        if (this.collapsedThreads.has(replyId)) {
          this.collapsedThreads.delete(replyId)
        } else {
          this.collapsedThreads.add(replyId)
        }
        this.collapsedThreads = new Set(this.collapsedThreads)
      },

      isThreadCollapsed(replyId) {
        return this.collapsedThreads.has(replyId)
      },

      getThreadReplyCount(reply) {
        const count = reply.replies?.length || 0
        return count === 1 ? '1 reply' : `${count} replies`
      },

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

      toggleRepliesCollapsed() {
        this.repliesCollapsed = !this.repliesCollapsed
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
  /* Comment Styling with CSS Variables */
  .comment-deleted {
    opacity: 0.8;
  }

  .comment-avatar {
    width: 2.5rem;
    height: 2.5rem;
    background: var(--color-background-soft);
    border: 1px solid var(--color-border);
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
  }

  .comment-avatar-deleted {
    opacity: 0.5;
  }

  .comment-username {
    font-weight: 500;
    color: var(--color-heading);
    font-size: 0.875rem;
  }

  .comment-username-deleted {
    opacity: 0.5;
  }

  .comment-timestamp {
    font-size: 0.75rem;
    color: var(--vt-c-text-light-2);
  }

  @media (prefers-color-scheme: dark) {
    .comment-timestamp {
      color: var(--vt-c-text-dark-2);
    }
  }

  .comment-content {
    color: var(--color-text);
    margin-bottom: 0.75rem;
    max-width: none;
  }

  .status-badge {
    font-size: 0.75rem;
    padding: 0.125rem 0.5rem;
    border-radius: 1rem;
    font-weight: 500;
  }

  .status-badge-deleted {
    background: var(--color-background-mute);
    color: var(--vt-c-text-light-2);
  }

  @media (prefers-color-scheme: dark) {
    .status-badge-deleted {
      background: var(--vt-c-black-mute);
      color: var(--vt-c-text-dark-2);
    }
  }

  .status-badge-admin {
    background: rgba(239, 68, 68, 0.1);
    color: rgb(185, 28, 28);
  }

  @media (prefers-color-scheme: dark) {
    .status-badge-admin {
      background: rgba(127, 29, 29, 0.3);
      color: rgb(248, 113, 113);
    }
  }

  .deleted-message {
    font-style: italic;
    color: var(--vt-c-text-light-2);
  }

  @media (prefers-color-scheme: dark) {
    .deleted-message {
      color: var(--vt-c-text-dark-2);
    }
  }

  .deleted-details {
    background: var(--color-background-mute);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    padding: 0.75rem;
  }

  .deleted-summary {
    font-size: 0.75rem;
    color: var(--vt-c-text-light-2);
    margin-bottom: 0.5rem;
    cursor: pointer;
  }

  @media (prefers-color-scheme: dark) {
    .deleted-summary {
      color: var(--vt-c-text-dark-2);
    }
  }

  /* Action Buttons */
  .reaction-button {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.875rem;
    font-weight: 500;
    transition: colors 0.2s;
    cursor: pointer;
    border: none;
    background: transparent;
  }

    .reaction-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .reaction-upvote {
    color: var(--vt-c-text-light-2);
  }

    .reaction-upvote:hover:not(:disabled) {
      color: #059669;
    }

  .reaction-upvote-active {
    color: #059669;
  }

  .reaction-downvote {
    color: var(--vt-c-text-light-2);
  }

    .reaction-downvote:hover:not(:disabled) {
      color: #dc2626;
    }

  .reaction-downvote-active {
    color: #dc2626;
  }

  @media (prefers-color-scheme: dark) {
    .reaction-upvote {
      color: var(--vt-c-text-dark-2);
    }

      .reaction-upvote:hover:not(:disabled) {
        color: #10b981;
      }

    .reaction-upvote-active {
      color: #10b981;
    }

    .reaction-downvote {
      color: var(--vt-c-text-dark-2);
    }

      .reaction-downvote:hover:not(:disabled) {
        color: #f87171;
      }

    .reaction-downvote-active {
      color: #f87171;
    }
  }

  .action-button {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.875rem;
    font-weight: 500;
    transition: colors 0.2s;
    cursor: pointer;
    border: none;
    background: transparent;
  }

    .action-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .action-button-reply {
    color: var(--vt-c-text-light-2);
  }

    .action-button-reply:hover:not(:disabled) {
      color: var(--color-heading);
    }

  .action-button-delete {
    color: #dc2626;
  }

    .action-button-delete:hover:not(:disabled) {
      color: #b91c1c;
    }

  .action-button-restore {
    color: #059669;
  }

    .action-button-restore:hover:not(:disabled) {
      color: #047857;
    }

  @media (prefers-color-scheme: dark) {
    .action-button-reply {
      color: var(--vt-c-text-dark-2);
    }

      .action-button-reply:hover:not(:disabled) {
        color: var(--color-heading);
      }

    .action-button-delete {
      color: #f87171;
    }

      .action-button-delete:hover:not(:disabled) {
        color: #ef4444;
      }

    .action-button-restore {
      color: #10b981;
    }

      .action-button-restore:hover:not(:disabled) {
        color: #059669;
      }
  }

  .reply-form {
    margin-bottom: 1rem;
  }

  /* Collapsed Replies */
  .collapsed-replies-button {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.75rem;
    background: var(--color-background-soft);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--color-heading);
    cursor: pointer;
    transition: all 0.2s;
  }

    .collapsed-replies-button:hover {
      background: var(--color-background-mute);
      border-color: var(--color-border-hover);
    }

  .expand-button {
    color: var(--color-heading);
    font-weight: 500;
    cursor: pointer;
    border: none;
    background: transparent;
    padding: 0.25rem 0.5rem;
    border-radius: 0.25rem;
    transition: background-color 0.2s;
  }

    .expand-button:hover {
      background: var(--color-background-soft);
    }

  /* Thread System */
  .thread-transition {
    margin: 1rem 0;
    padding: 0.5rem 0;
    border-top: 1px solid var(--color-border);
  }

  .thread-button {
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1rem;
    background: var(--color-background-soft);
    border: 2px solid var(--color-border);
    border-radius: 0.75rem;
    cursor: pointer;
    transition: all 0.2s;
    color: var(--color-text);
  }

    .thread-button:hover {
      background: var(--color-background-mute);
      border-color: var(--vt-c-indigo);
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

  @media (prefers-color-scheme: dark) {
    .thread-button:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
    }
  }

  .thread-button-content {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }

  .thread-stats {
    font-size: 0.75rem;
    color: var(--vt-c-text-light-2);
  }

  @media (prefers-color-scheme: dark) {
    .thread-stats {
      color: var(--vt-c-text-dark-2);
    }
  }

  /* Thread Lines */
  .thread-line-button {
    position: absolute;
    top: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    z-index: 10;
    border: none;
    background: transparent;
    outline: none;
  }

  .thread-line-main {
    left: 0;
    width: 1rem;
  }

  .thread-line-individual {
    width: 0.75rem;
  }

  .thread-line {
    transition: all 0.2s;
  }

  .thread-line-main-visual {
    position: absolute;
    left: 0.5rem;
    top: 0;
    width: 1px;
    height: 100%;
    background: var(--color-border);
  }

  .thread-line-button:hover .thread-line-main-visual {
    background: var(--vt-c-indigo);
    opacity: 0.8;
  }

  .thread-line-individual-visual {
    width: 1px;
    height: 100%;
    background: var(--color-border);
  }

  .thread-line-button:hover .thread-line-individual-visual {
    background: var(--vt-c-indigo);
    opacity: 0.8;
  }
</style>
