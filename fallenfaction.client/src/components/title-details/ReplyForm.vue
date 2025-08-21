<!-- ReplyForm.vue - Extracted reply form component -->
<template>
  <form @submit.prevent="submitReply"
        class="p-4 bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg"
        aria-label="Reply to comment">

    <div class="flex space-x-3">
      <!-- User Avatar -->
      <div class="flex-shrink-0">
        <div class="w-8 h-8 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-full flex items-center justify-center">
          <svg class="w-4 h-4 text-[var(--color-text)] opacity-60" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
          </svg>
        </div>
      </div>

      <!-- Reply Input -->
      <div class="flex-1">
        <label for="reply-textarea" class="sr-only">Write your reply</label>
        <textarea id="reply-textarea"
                  ref="replyTextarea"
                  v-model="replyText"
                  :disabled="submitting"
                  placeholder="Write your reply..."
                  rows="2"
                  maxlength="2000"
                  class="w-full bg-[var(--color-background-soft)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-transparent resize-none transition-colors duration-200"
                  @keydown.ctrl.enter="submitReply"
                  @keydown.meta.enter="submitReply"
                  @keydown.esc="cancelReply"
                  required></textarea>

        <!-- Reply Actions -->
        <div class="flex items-center justify-between mt-2">
          <div class="flex items-center space-x-2 text-xs text-[var(--color-text)] opacity-60">
            <span>{{ replyText.length }}/2000 characters</span>
            <span class="hidden sm:inline">• Press Ctrl+Enter to submit • Esc to cancel</span>
          </div>

          <div class="flex items-center space-x-2">
            <button type="button"
                    @click="cancelReply"
                    :disabled="submitting"
                    class="text-sm text-[var(--color-text)] opacity-60 hover:opacity-100 disabled:opacity-50 transition-opacity duration-200 focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] rounded px-2 py-1">
              Cancel
            </button>

            <button type="submit"
                    :disabled="!isValidReply || submitting"
                    class="bg-[var(--color-accent)] text-white px-3 py-1 rounded text-sm font-medium hover:bg-[var(--color-accent-hover)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
              <LoadingSpinner v-if="submitting" class="w-3 h-3 mr-1" />
              {{ submitting ? 'Posting...' : 'Reply' }}
            </button>
          </div>
        </div>

        <!-- Character limit warning -->
        <div v-if="replyText.length > 1800"
             class="mt-1 text-xs text-amber-600 dark:text-amber-400"
             role="status"
             aria-live="polite">
          {{ 2000 - replyText.length }} characters remaining
        </div>
      </div>
    </div>
  </form>
</template>

<script>
import { commentsService } from '../../services/commentsService'
import { useToast } from '../../utils/toastService'
import LoadingSpinner from '../ui/LoadingSpinner.vue'

export default {
  name: 'ReplyForm',
  components: {
    LoadingSpinner
  },
  props: {
    targetId: {
      type: [Number, String],
      required: true
    },
    targetType: {
      type: [Number, String],
      required: true
    },
    parentCommentId: {
      type: [Number, String],
      required: true
    },
    submitting: {
      type: Boolean,
      default: false
    }
  },
  emits: ['reply-submitted', 'reply-cancelled'],
  setup() {
    const { error: showErrorToast } = useToast();
    return { showErrorToast };
  },
  data() {
    return {
      replyText: ''
    }
  },
  computed: {
    isValidReply() {
      return this.replyText.trim().length > 0 && this.replyText.length <= 2000
    }
  },
  mounted() {
    // Auto-focus the textarea when the form is mounted
    this.$nextTick(() => {
      this.$refs.replyTextarea?.focus()
    })
  },
  methods: {
    async submitReply() {
      if (!this.isValidReply || this.submitting) return

      // Validate content
      const validation = commentsService.validateCommentContent(this.replyText)
      if (!validation.isValid) {
        this.showErrorToast(validation.error)
        return
      }

      try {
        const targetId = parseInt(this.targetId)
        const targetType = parseInt(this.targetType)

        if (isNaN(targetId) || isNaN(targetType)) {
          throw new Error(`Invalid target values: targetId=${this.targetId}, targetType=${this.targetType}`)
        }

        const result = await commentsService.addComment(
          this.replyText,
          targetId,
          targetType,
          parseInt(this.parentCommentId)
        )

        if (result.success) {
          this.$emit('reply-submitted', result.data)
          this.replyText = ''
        } else {
          this.showErrorToast(result.error)
        }
      } catch (error) {
        console.error('Error submitting reply:', error)
        this.showErrorToast('Failed to post reply')
      }
    },

    cancelReply() {
      this.replyText = ''
      this.$emit('reply-cancelled')
    }
  }
}
</script>

<style scoped>
  /* Custom focus styles for better accessibility */
  textarea:focus {
    box-shadow: 0 0 0 3px rgba(var(--color-accent-rgb), 0.1);
  }

  /* Smooth transitions */
  .transition-all {
    transition-property: all;
    transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
    transition-duration: 200ms;
  }
</style>
