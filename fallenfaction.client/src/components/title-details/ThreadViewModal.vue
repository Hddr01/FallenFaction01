<!-- ThreadViewModal.vue - Modal for viewing thread conversations -->
<template>
  <!-- Modal Backdrop -->
  <div class="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4"
       @click="handleBackdropClick">

    <!-- Modal Container -->
    <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] flex flex-col"
         @click.stop>

      <!-- Modal Header -->
      <header class="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
        <div class="flex items-center gap-4">
          <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100">
            Thread Conversation
          </h2>

          <!-- Thread stats -->
          <div class="flex items-center gap-4 text-sm text-gray-500 dark:text-gray-400">
            <span class="flex items-center gap-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
              </svg>
              {{ totalCommentsCount }} {{ totalCommentsCount === 1 ? 'comment' : 'comments' }}
            </span>
            <span class="flex items-center gap-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z"></path>
              </svg>
              {{ uniqueParticipants.length }} {{ uniqueParticipants.length === 1 ? 'participant' : 'participants' }}
            </span>
          </div>
        </div>

        <!-- Close Button -->
        <button @click="$emit('close')"
                class="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors duration-200 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800">
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
          </svg>
        </button>
      </header>

      <!-- Modal Content -->
      <div class="flex-1 overflow-hidden flex flex-col">

        <!-- Thread Breadcrumb -->
        <div class="px-6 py-3 bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
          <div class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
            </svg>
            <span>Viewing conversation thread started by <strong>{{ rootComment.userName }}</strong></span>
            <time class="text-xs">{{ formatDate(rootComment.postedDate) }}</time>
          </div>
        </div>

        <!-- Scrollable Thread Content -->
        <div class="flex-1 overflow-y-auto p-6 space-y-6">

          <!-- Root Comment Display -->
          <article class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
            <header class="flex items-center gap-3 mb-3">
              <div class="w-10 h-10 bg-white dark:bg-gray-800 border border-blue-200 dark:border-blue-700 rounded-full flex items-center justify-center overflow-hidden">
                <img v-if="rootComment.userAvatarUrl"
                     :src="rootComment.userAvatarUrl"
                     :alt="`${rootComment.userName}'s avatar`"
                     class="w-full h-full object-cover" />
                <svg v-else class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                </svg>
              </div>

              <div class="flex-1">
                <div class="flex items-center gap-2 mb-1">
                  <h3 class="font-semibold text-gray-900 dark:text-gray-100">{{ rootComment.userName }}</h3>
                  <time class="text-sm text-gray-500 dark:text-gray-400">{{ formatDate(rootComment.postedDate) }}</time>
                  <span class="bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs px-2 py-1 rounded-full font-medium">
                    Thread Start
                  </span>
                </div>
              </div>

              <!-- Root comment actions -->
              <div class="flex items-center gap-2">
                <button @click="toggleLike(rootComment)"
                        :disabled="!isAuthenticated"
                        class="flex items-center gap-1 text-sm font-medium transition-colors duration-200 disabled:opacity-50"
                        :class="rootComment.currentUserLiked
                    ? 'text-green-600 dark:text-green-400 hover:text-green-700'
                    : 'text-gray-500 dark:text-gray-400 hover:text-green-600'">
                  <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M7 14l5-5 5 5z" />
                  </svg>
                  <span>{{ rootComment.likesCount || 0 }}</span>
                </button>

                <button @click="showRootReplyForm = !showRootReplyForm"
                        :disabled="!isAuthenticated"
                        class="flex items-center gap-1 text-sm font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 disabled:opacity-50 transition-colors duration-200">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6"></path>
                  </svg>
                  <span>Reply</span>
                </button>
              </div>
            </header>

            <div class="prose prose-sm max-w-none text-gray-900 dark:text-gray-100 mb-3">
              <p class="whitespace-pre-wrap break-words">{{ rootComment.content }}</p>
            </div>
          </article>

          <!-- Reply Form for Root Comment -->
          <ReplyForm v-if="showRootReplyForm"
                     :target-id="targetId"
                     :target-type="targetType"
                     :parent-comment-id="rootComment.id"
                     :submitting="submittingReply"
                     @reply-submitted="onReplySubmitted"
                     @reply-cancelled="showRootReplyForm = false"
                     class="border-l-4 border-blue-300 dark:border-blue-700 pl-4 ml-4" />

          <!-- Thread Conversation -->
          <div v-if="rootComment.replies?.length > 0" class="space-y-4">
            <h4 class="text-lg font-medium text-gray-900 dark:text-gray-100 border-b border-gray-200 dark:border-gray-700 pb-2">
              Conversation
            </h4>

            <!-- Render all replies in a clean, linear fashion -->
            <div class="space-y-4">
              <ThreadComment v-for="reply in flattenedReplies"
                             :key="reply.id"
                             :comment="reply"
                             :target-id="targetId"
                             :target-type="targetType"
                             :is-authenticated="isAuthenticated"
                             :current-user-id="currentUserId"
                             :is-admin="isAdmin"
                             :can-reply="canReply"
                             :indent-level="reply._indentLevel"
                             @comment-updated="$emit('comment-updated', $event)"
                             @comment-deleted="$emit('comment-deleted', $event)"
                             @comment-restored="$emit('comment-restored', $event)"
                             @reply-added="onReplyAdded" />
            </div>
          </div>

          <!-- Empty state -->
          <div v-else class="text-center py-8 text-gray-500 dark:text-gray-400">
            <svg class="w-12 h-12 mx-auto mb-3 opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
            </svg>
            <p>No replies in this thread yet.</p>
            <p class="text-sm mt-1">Be the first to continue the conversation!</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { commentsService } from '../../services/commentsService'
  import { useToast } from '../../utils/toastService'
  import ReplyForm from './ReplyForm.vue'
  import ThreadComment from './ThreadComment.vue'

  export default {
    name: 'ThreadViewModal',
    components: {
      ReplyForm,
      ThreadComment
    },
    props: {
      rootComment: {
        type: Object,
        required: true
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
    emits: ['close', 'comment-updated', 'comment-deleted', 'comment-restored', 'reply-added'],
    setup() {
      const { success, error } = useToast();
      return { showSuccessToast: success, showErrorToast: error };
    },
    data() {
      return {
        showRootReplyForm: false,
        submittingReply: false
      }
    },
    computed: {
      flattenedReplies() {
        const flattened = []

        const flattenRecursive = (replies, indentLevel = 0) => {
          replies.forEach(reply => {
            flattened.push({
              ...reply,
              _indentLevel: indentLevel
            })

            if (reply.replies && reply.replies.length > 0) {
              flattenRecursive(reply.replies, indentLevel + 1)
            }
          })
        }

        if (this.rootComment.replies) {
          flattenRecursive(this.rootComment.replies)
        }

        return flattened
      },

      totalCommentsCount() {
        return 1 + this.countAllReplies(this.rootComment)
      },

      uniqueParticipants() {
        const participants = new Set()
        participants.add(this.rootComment.userName)

        const addParticipants = (replies) => {
          replies.forEach(reply => {
            participants.add(reply.userName)
            if (reply.replies && reply.replies.length > 0) {
              addParticipants(reply.replies)
            }
          })
        }

        if (this.rootComment.replies) {
          addParticipants(this.rootComment.replies)
        }

        return Array.from(participants)
      }
    },
    methods: {
      handleBackdropClick() {
        this.$emit('close')
      },

      countAllReplies(comment) {
        let count = 0
        if (comment.replies && comment.replies.length > 0) {
          count += comment.replies.length
          comment.replies.forEach(reply => {
            count += this.countAllReplies(reply)
          })
        }
        return count
      },

      async toggleLike(comment) {
        if (!this.isAuthenticated) return

        try {
          const result = await commentsService.reactToComment(comment.id, true)

          if (result.success) {
            comment.likesCount = result.data.likesCount
            comment.dislikesCount = result.data.dislikesCount
            comment.currentUserLiked = result.data.userLiked
            comment.currentUserDisliked = result.data.userDisliked

            this.$emit('comment-updated', comment)
          } else {
            this.showErrorToast(result.error)
          }
        } catch (error) {
          console.error('Error reacting to comment:', error)
          this.showErrorToast('Failed to update reaction')
        }
      },

      async onReplySubmitted(replyData) {
        this.showRootReplyForm = false
        this.showSuccessToast('Reply posted successfully!')
        this.$emit('reply-added', replyData)
      },

      onReplyAdded(replyData) {
        this.$emit('reply-added', replyData)
        this.showSuccessToast('Reply posted successfully!')
      },

      formatDate(dateString) {
        if (!dateString) return ''
        return commentsService.formatCommentDate(dateString)
      },

      handleEscapeKey(event) {
        if (event.key === 'Escape') {
          this.$emit('close')
        }
      }
    },

    // Handle escape key to close modal
    mounted() {
      document.addEventListener('keydown', this.handleEscapeKey)
    },

    beforeUnmount() {
      document.removeEventListener('keydown', this.handleEscapeKey)
    }
  }
</script>

<style scoped>
  /* Ensure modal appears above everything */
  .fixed {
    z-index: 1000;
  }
</style>
