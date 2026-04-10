<template>
  <div class="comment-item">
    <!-- Comment content wrapper with vertical line for nested comments -->
    <div class="comment-wrapper" :class="{ 'has-line': depth > 0 }">
      <!-- Clickable vertical line for collapse (only shown for nested comments) -->
      <div v-if="depth > 0"
           class="vertical-line"
           :class="{ collapsed: isCollapsed }"
           @click="toggleCollapse"
           :title="isCollapsed ? 'Expand thread' : 'Collapse thread'">
      </div>

      <!-- Main comment content -->
      <div class="comment-main">
        <!-- Show collapsed indicator if collapsed -->
        <div v-if="isCollapsed" class="collapsed-indicator" @click="toggleCollapse">
          <span class="collapsed-info">
            [+] {{ comment.userName || `@${comment.userHandle}` }} ({{ getTotalRepliesCount() }} {{ getTotalRepliesCount() === 1 ? 'reply' : 'replies' }})
          </span>
        </div>

        <!-- Full comment (hidden when collapsed) -->
        <div v-show="!isCollapsed" class="comment-content">
          <!-- Comment header -->
          <div class="comment-header">
            <div class="user-info">
              <a :href="`/user/${comment.userId}`" class="avatar-link" @click.prevent="$router.push(`/user/${comment.userId}`)">
                <img :src="comment.userAvatarUrl || '/img/default-avatar.png'"
                     :alt="`@${comment.userHandle}`"
                     class="avatar" />
              </a>
              <a :href="`/user/${comment.userId}`" class="username" @click.prevent="$router.push(`/user/${comment.userId}`)">
                {{ comment.userName || `@${comment.userHandle}` }}
              </a>
              <time class="timestamp" :datetime="comment.postedDate">
                {{ formatTimeAgo(comment.postedDate) }}
              </time>
              <span v-if="comment.isDeleted" class="deleted-badge">deleted</span>
              <span v-if="comment.isPinned" class="pinned-badge" title="Pinned by team">
                <Pin :size="12" /> Pinned
                <template v-if="comment.pinnedByTeamName">
                  by {{ comment.pinnedByTeamName }}
                </template>
              </span>
            </div>

            <div class="vote-controls">
              <button @click="handleVote(true)"
                      class="vote-btn"
                      :class="{ active: comment.currentUserLiked }"
                      :disabled="isVoting || comment.isDeleted">
                <ChevronUp :size="16" />
              </button>
              <span class="vote-count" :class="voteClass">
                {{ netVotes }}
              </span>
              <button @click="handleVote(false)"
                      class="vote-btn"
                      :class="{ active: comment.currentUserDisliked }"
                      :disabled="isVoting || comment.isDeleted">
                <ChevronDown :size="16" />
              </button>
            </div>
          </div>

          <!-- Comment body -->
          <div class="comment-body">
            <div v-if="!comment.isDeleted"
                 class="comment-text"
                 :class="{ 'is-collapsed-text': isTextCollapsed }"
                 >{{ comment.content }}</div>
            <div v-else class="deleted-content">
              [This comment has been deleted]
              <span v-if="comment.deletionReason" class="deletion-reason">
                Reason: {{ comment.deletionReason }}
              </span>
            </div>

            <button v-if="isTextTooLong && !comment.isDeleted"
                    @click="isTextCollapsed = !isTextCollapsed"
                    class="expand-btn">
              {{ isTextCollapsed ? 'Show more' : 'Show less' }}
            </button>
          </div>

          <!-- Comment actions -->
          <div class="comment-actions">
            <button @click="toggleReply" class="action-btn" :disabled="comment.isDeleted || !canReply">
              <MessageSquare :size="14" />
              Reply
            </button>

            <!-- Report dropdown -->
            <div v-if="isAuthenticated && !comment.isDeleted" class="report-dropdown-wrapper" ref="reportDropdownRef">
              <button @click="showReportDropdown = !showReportDropdown" class="action-btn">
                <Flag :size="14" />
                Report
              </button>
              <div v-if="showReportDropdown" class="report-dropdown">
                <button @click="openCommentReport" class="report-dropdown-item">
                  <MessageSquare :size="13" />
                  Report Comment
                </button>
                <button @click="openUserReport" class="report-dropdown-item">
                  <UserX :size="13" />
                  Report User
                </button>
              </div>
            </div>

            <button @click="handleDelete" class="action-btn" v-if="canDelete">
              <Trash2 :size="14" />
              Delete
            </button>
            <button @click="handlePin" class="action-btn"
                    v-if="canPin"
                    :disabled="isPinning">
              <Pin :size="14" />
              {{ comment.isPinned ? 'Unpin' : 'Pin' }}
            </button>
          </div>

          <!-- Reply form -->
          <div v-if="showReplyForm" class="reply-form">
            <textarea v-model="replyContent"
                      placeholder="Write a reply..."
                      class="reply-input"
                      rows="3"
                      @keydown.ctrl.enter="submitReply"
                      @keydown.meta.enter="submitReply" />
            <div class="reply-actions">
              <button @click="submitReply" class="btn-primary" :disabled="!replyContent.trim() || submittingReply">
                {{ submittingReply ? 'Posting...' : 'Submit' }}
              </button>
              <button @click="cancelReply" class="btn-secondary">
                Cancel
              </button>
            </div>
          </div>

          <!-- Nested replies -->
          <div v-if="hasReplies && !isCollapsed" class="replies-container">
            <!-- Show "Continue thread" button if we've reached max depth -->
            <div v-if="depth >= maxDepth" class="continue-thread">
              <a :href="getContinueThreadUrl(comment.replies[0].id)"
                 class="continue-thread-btn"
                 @click.prevent="navigateToThread(comment.replies[0].id)">
                <span class="continue-arrow">→</span>
                Continue this thread ({{ getTotalRepliesCount() }} more {{ getTotalRepliesCount() === 1 ? 'reply' : 'replies' }})
              </a>
            </div>

            <!-- Regular nested replies (below max depth) -->
            <CommentItem v-else
                         v-for="reply in comment.replies"
                         :key="reply.id"
                         :comment="reply"
                         :target-id="targetId"
                         :target-type="targetType"
                         :is-authenticated="isAuthenticated"
                         :current-user-id="currentUserId"
                         :is-admin="isAdmin"
                         :can-reply="canReply"
                         :depth="depth + 1"
                         :max-depth="maxDepth"
                         :parent-active="!isCollapsed"
                         :active-sibling="openChildId"
                         @reply-added="$emit('reply-added', $event)"
                         @comment-updated="$emit('comment-updated', $event)"
                         @comment-deleted="$emit('comment-deleted', $event)"
                         @sibling-open="handleSiblingOpen" />
          </div>

          <!-- Load more replies button (deprecated - replaced by continue thread) -->
          <div v-if="comment.replies && comment.replies.length > 0 && depth >= maxDepth && false" class="load-more">
            <button class="load-more-btn">
              Continue this thread →
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>

  <!-- Report Comment Modal -->
  <ReportModal :is-open="showReportModal"
               :target-type="1"
               :target-id="comment.id"
               @close="showReportModal = false"
               @reported="showReportModal = false" />

  <!-- Report User Modal -->
  <ReportModal :is-open="showReportUserModal"
               :target-type="4"
               :target-id="comment.userId"
               @close="showReportUserModal = false"
               @reported="showReportUserModal = false" />
</template>

<script setup>
  import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
  import { ChevronUp, ChevronDown, MessageSquare, Flag, Trash2, Pin, UserX } from 'lucide-vue-next'
  import { commentsService } from '../../services/commentsService'
  import ReportModal from '../shared/ReportModal.vue'

  const props = defineProps({
    comment: {
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
    canManageTitle: {
      type: Boolean,
      default: false
    },
    canReply: {
      type: Boolean,
      default: true
    },
    depth: {
      type: Number,
      default: 0
    },
    maxDepth: {
      type: Number,
      default: 8
    },
    parentActive: {
      type: Boolean,
      default: true
    },
    activeSibling: {
      type: [String, Number, null],
      default: null
    }
  })

  const emit = defineEmits(['reply-added', 'comment-updated', 'comment-deleted', 'sibling-open'])

  // State
  const isCollapsed = ref(false)
  const showReportModal = ref(false)
  const showReportUserModal = ref(false)
  const showReportDropdown = ref(false)
  const reportDropdownRef = ref(null)
  const isTextCollapsed = ref(true)
  const showReplyForm = ref(false)
  const replyContent = ref('')
  const isVoting = ref(false)
  const submittingReply = ref(false)
  const isPinning = ref(false)
  const openChildId = ref(null) // Track which child comment is currently open

  // Computed
  const netVotes = computed(() => {
    return props.comment.likesCount - props.comment.dislikesCount
  })

  const voteClass = computed(() => {
    if (props.comment.currentUserLiked) return 'upvoted'
    if (props.comment.currentUserDisliked) return 'downvoted'
    return ''
  })

  const hasReplies = computed(() => {
    return props.comment.replies && props.comment.replies.length > 0
  })

  const isTextTooLong = computed(() => {
    return props.comment.content && props.comment.content.length > 500
  })

  const canDelete = computed(() => {
    if (!props.isAuthenticated) return false
    if (props.comment.isDeleted) return false
    // User can delete their own comment, or admin can delete any comment
    return props.comment.userId === props.currentUserId || props.isAdmin
  })

  const canPin = computed(() => {
    if (!props.isAuthenticated) return false
    if (props.comment.isDeleted || props.comment.parentCommentId) return false
    return props.isAdmin || props.canManageTitle
  })

  // Watch for parent collapse - auto collapse this comment if parent collapses
  watch(() => props.parentActive, (newVal) => {
    if (!newVal && props.depth > 0) {
      isCollapsed.value = true
      openChildId.value = null // Also close any open children
    }
  })

  // Watch for sibling activation - auto collapse if a sibling is opened
  watch(() => props.activeSibling, (newVal) => {
    if (newVal && newVal !== props.comment.id) {
      isCollapsed.value = true
      openChildId.value = null // Also close any open children
    }
  })

  // Methods
  const toggleCollapse = () => {
    const wasCollapsed = isCollapsed.value
    isCollapsed.value = !isCollapsed.value

    // If we're opening this comment, notify parent to close siblings
    if (wasCollapsed) {
      emit('sibling-open', props.comment.id)
    }

    // If we're closing, reset the open child
    if (!wasCollapsed) {
      openChildId.value = null
    }
  }

  const handleSiblingOpen = (childId) => {
    // Update which child is currently open
    openChildId.value = childId
  }

  const toggleReply = () => {
    if (!props.isAuthenticated) {
      alert('You must be logged in to reply')
      return
    }
    showReplyForm.value = !showReplyForm.value
    if (!showReplyForm.value) {
      replyContent.value = ''
    }
  }

  const handleVote = async (isLike) => {
    if (!props.isAuthenticated) {
      alert('You must be logged in to vote')
      return
    }

    if (isVoting.value || props.comment.isDeleted) return

    isVoting.value = true

    try {
      const result = await commentsService.reactToComment(props.comment.id, isLike)

      if (result.success) {
        // Update the comment with new vote counts and user reaction status
        const updatedComment = {
          ...props.comment,
          likesCount: result.data.likesCount,
          dislikesCount: result.data.dislikesCount,
          currentUserLiked: result.data.userLiked,
          currentUserDisliked: result.data.userDisliked
        }

        emit('comment-updated', updatedComment)
      } else {
        console.error('Failed to vote:', result.error)
        alert(result.error || 'Failed to vote on comment')
      }
    } catch (error) {
      console.error('Error voting:', error)
      alert('Failed to vote on comment')
    } finally {
      isVoting.value = false
    }
  }

  const submitReply = async () => {
    if (!replyContent.value.trim() || submittingReply.value) return

    if (!props.isAuthenticated) {
      alert('You must be logged in to reply')
      return
    }

    submittingReply.value = true

    try {
      const result = await commentsService.addComment(
        parseInt(props.targetId),
        parseInt(props.targetType),
        replyContent.value.trim(),
        props.comment.id // parentCommentId
      )

      if (result.success) {
        // Emit the new reply to parent
        emit('reply-added', result.data)

        // Clear form
        replyContent.value = ''
        showReplyForm.value = false
      } else {
        console.error('Failed to post reply:', result.error)
        alert(result.error || 'Failed to post reply')
      }
    } catch (error) {
      console.error('Error posting reply:', error)
      alert('Failed to post reply')
    } finally {
      submittingReply.value = false
    }
  }

  const cancelReply = () => {
    replyContent.value = ''
    showReplyForm.value = false
  }

  const openCommentReport = () => {
    showReportDropdown.value = false
    if (!props.isAuthenticated) {
      alert('You must be logged in to report comments')
      return
    }
    showReportModal.value = true
  }

  const openUserReport = () => {
    showReportDropdown.value = false
    if (!props.isAuthenticated) {
      alert('You must be logged in to report users')
      return
    }
    showReportUserModal.value = true
  }

  // Keep old handleReport for any other callers
  const handleReport = openCommentReport

  // Close report dropdown when clicking outside
  const handleOutsideClick = (e) => {
    if (reportDropdownRef.value && !reportDropdownRef.value.contains(e.target)) {
      showReportDropdown.value = false
    }
  }

  onMounted(() => document.addEventListener('click', handleOutsideClick, true))
  onUnmounted(() => document.removeEventListener('click', handleOutsideClick, true))

  const handleDelete = async () => {
    if (!canDelete.value) return

    const confirmed = confirm('Are you sure you want to delete this comment?')
    if (!confirmed) return

    try {
      const result = await commentsService.deleteComment(props.comment.id)

      if (result.success) {
        // Emit delete event
        emit('comment-deleted', props.comment.id)
      } else {
        console.error('Failed to delete comment:', result.error)
        alert(result.error || 'Failed to delete comment')
      }
    } catch (error) {
      console.error('Error deleting comment:', error)
      alert('Failed to delete comment')
    }
  }

  const handlePin = async () => {
    isPinning.value = true
    try {
      if (props.comment.isPinned) {
        await commentsService.unpinComment(props.comment.id)
        emit('comment-updated', {
          ...props.comment,
          isPinned: false,
          pinnedAt: null,
          pinnedByUserName: null,
          pinnedByTeamName: null
        })
      } else {
        await commentsService.pinComment(props.comment.id)
        emit('comment-updated', {
          ...props.comment,
          isPinned: true,
          pinnedAt: new Date().toISOString()
        })
      }
    } catch (error) {
      console.error('Error pinning/unpinning comment:', error)
      const d = error?.response?.data
      const msg = typeof d === 'string' ? d : (d?.message || error.message || 'Failed to pin/unpin comment')
      alert(msg)
    } finally {
      isPinning.value = false
    }
  }

  const getTotalRepliesCount = () => {
    const countReplies = (replies) => {
      if (!replies || replies.length === 0) return 0
      return replies.reduce((count, reply) => {
        return count + 1 + countReplies(reply.replies || [])
      }, 0)
    }
    return countReplies(props.comment.replies || [])
  }

  const formatTimeAgo = (date) => {
    const now = new Date()
    const posted = new Date(date)
    const diffMs = now - posted
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)

    if (diffMins < 1) return 'just now'
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
    if (diffDays < 30) {
      const weeks = Math.floor(diffDays / 7)
      return `${weeks} week${weeks > 1 ? 's' : ''} ago`
    }
    const months = Math.floor(diffDays / 30)
    if (months < 12) return `${months} month${months > 1 ? 's' : ''} ago`
    const years = Math.floor(months / 12)
    return `${years} year${years > 1 ? 's' : ''} ago`
  }

  const getContinueThreadUrl = (commentId) => {
    // Build URL with comment_id parameter
    const currentUrl = new URL(window.location.href)
    currentUrl.searchParams.set('comment_id', commentId)
    return currentUrl.toString()
  }

  const navigateToThread = (commentId) => {
    // Navigate to the isolated thread view
    const url = getContinueThreadUrl(commentId)
    window.location.href = url
  }
</script>

<style scoped>
  /* ... (keep all your existing CSS, it's fine) ... */
  .comment-item {
    position: relative;
  }

  .comment-wrapper {
    position: relative;
  }

    .comment-wrapper.has-line {
      padding-left: 20px;
    }

  /* Vertical collapsible line for nested comments */
  .vertical-line {
    position: absolute;
    left: 0;
    top: 0;
    bottom: 0;
    width: 20px;
    cursor: pointer;
    z-index: 1;
  }

    .vertical-line::before {
      content: '';
      position: absolute;
      left: 8px;
      top: 0;
      bottom: 0;
      width: 2px;
      background: #e5e5e5;
      transition: background 0.2s ease;
    }

    .vertical-line:hover::before {
      background: #ff6d00;
    }

    .vertical-line.collapsed::before {
      background: #e5e5e5;
      opacity: 0.5;
    }

  .comment-main {
    width: 100%;
  }

  /* Collapsed indicator */
  .collapsed-indicator {
    padding: 8px 12px;
    cursor: pointer;
    border-radius: 6px;
    transition: background 0.2s ease;
  }

    .collapsed-indicator:hover {
      background: rgba(0, 0, 0, 0.03);
    }

  .collapsed-info {
    font-size: 13px;
    color: var(--color-text-muted, #8a8a8e);
    font-weight: 500;
  }

  /* Comment content */
  .comment-content {
    width: 100%;
  }

  /* Comment header */
  .comment-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    margin-bottom: 6px;
  }

  .user-info {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 1;
    min-width: 0;
  }

  .avatar-link {
    flex-shrink: 0;
    line-height: 0;
  }

  .avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    object-fit: cover;
    background: var(--color-background-mute, #f8f9fa);
    border: 1px solid var(--color-border, #e5e5e5);
  }

  .username {
    font-weight: 600;
    color: var(--color-text, #212529);
    font-size: 13px;
    text-decoration: none;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 150px;
  }

    .username:hover {
      text-decoration: underline;
    }

  .timestamp {
    font-size: 12px;
    color: var(--color-text-muted, #8a8a8e);
    white-space: nowrap;
  }

  .deleted-badge {
    font-size: 11px;
    padding: 2px 6px;
    background: #f44336;
    color: white;
    border-radius: 3px;
    text-transform: uppercase;
    font-weight: 600;
  }

  .pinned-badge {
    display: inline-flex;
    align-items: center;
    gap: 3px;
    font-size: 11px;
    padding: 2px 8px;
    background: #f59e0b;
    color: white;
    border-radius: 3px;
    font-weight: 600;
  }

  /* Vote controls */
  .vote-controls {
    display: flex;
    align-items: center;
    gap: 4px;
    flex-shrink: 0;
  }

  .vote-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 26px;
    height: 26px;
    padding: 0;
    background: none;
    border: none;
    border-radius: 4px;
    color: var(--color-text-muted, #8a8a8e);
    cursor: pointer;
    transition: all 0.2s ease;
  }

    .vote-btn:hover:not(:disabled) {
      background: rgba(0, 0, 0, 0.05);
    }

    .vote-btn.active {
      color: #3cce7b;
    }

    .vote-btn:last-child.active {
      color: #f44336;
    }

    .vote-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .vote-count {
    min-width: 30px;
    text-align: center;
    font-size: 12px;
    font-weight: 600;
    color: var(--color-text-muted, #8a8a8e);
  }

    .vote-count.upvoted {
      color: #3cce7b;
    }

    .vote-count.downvoted {
      color: #f44336;
    }

  /* Comment body */
  .comment-body {
    margin-top: 6px;
    line-height: 1.6;
    color: var(--color-text, #212529);
    font-size: 14px;
  }

  .comment-text {
    word-wrap: break-word;
    word-break: break-word;
  }

    .comment-text.is-collapsed-text {
      display: -webkit-box;
      -webkit-line-clamp: 6;
      -webkit-box-orient: vertical;
      overflow: hidden;
      position: relative;
    }

  .deleted-content {
    color: var(--color-text-muted, #8a8a8e);
    font-style: italic;
  }

  .deletion-reason {
    display: block;
    margin-top: 4px;
    font-size: 12px;
  }

  .expand-btn {
    margin-top: 8px;
    padding: 0;
    background: none;
    border: none;
    color: var(--color-accent, #ff6d00);
    font-size: 13px;
    cursor: pointer;
    text-decoration: none;
    font-weight: 500;
  }

    .expand-btn:hover {
      text-decoration: underline;
    }

  /* Comment actions */
  .comment-actions {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-top: 8px;
  }

  /* Report dropdown */
  .report-dropdown-wrapper {
    position: relative;
  }

  .report-dropdown {
    position: absolute;
    top: calc(100% + 6px);
    left: 0;
    z-index: 1000;
    min-width: 160px;
    background: var(--color-background, #fff);
    border: 1px solid var(--color-border, #e5e5e5);
    border-radius: 8px;
    box-shadow: 0 4px 16px rgba(0,0,0,0.12);
    overflow: hidden;
    animation: dropdown-in 0.1s ease;
  }

  @keyframes dropdown-in {
    from {
      opacity: 0;
      transform: translateY(-4px);
    }

    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  .report-dropdown-item {
    display: flex;
    align-items: center;
    gap: 8px;
    width: 100%;
    padding: 9px 14px;
    background: none;
    border: none;
    color: var(--color-text, #212529);
    font-size: 13px;
    font-weight: 500;
    cursor: pointer;
    text-align: left;
    transition: background 0.15s ease;
  }

    .report-dropdown-item:hover {
      background: var(--color-background-mute, #f8f9fa);
      color: var(--color-accent, #ff6d00);
    }

  .action-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 0;
    background: none;
    border: none;
    color: var(--color-text-muted, #8a8a8e);
    font-size: 13px;
    cursor: pointer;
    transition: color 0.2s ease;
    font-weight: 500;
  }

    .action-btn:hover:not(:disabled) {
      color: var(--color-accent, #ff6d00);
    }

    .action-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  /* Reply form */
  .reply-form {
    margin-top: 12px;
    padding: 12px;
    background: var(--color-background-mute, #f8f9fa);
    border-radius: 6px;
    border: 1px solid var(--color-border, #e5e5e5);
  }

  .reply-input {
    width: 100%;
    padding: 8px 12px;
    border: 1px solid var(--color-border, #e5e5e5);
    border-radius: 4px;
    font-family: inherit;
    font-size: 14px;
    line-height: 1.4;
    resize: vertical;
    min-height: 60px;
    background: var(--color-background, white);
    color: var(--color-text, #212529);
  }

    .reply-input:focus {
      outline: none;
      border-color: var(--color-accent, #ff9100);
      box-shadow: 0 0 0 3px rgba(255, 145, 0, 0.1);
    }

  .reply-actions {
    display: flex;
    gap: 8px;
    margin-top: 8px;
  }

  .btn-primary,
  .btn-secondary {
    padding: 6px 16px;
    border: none;
    border-radius: 4px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s ease;
  }

  .btn-primary {
    background: var(--color-accent, #ff9100);
    color: white;
  }

    .btn-primary:hover:not(:disabled) {
      background: #ff9f1a;
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .btn-secondary {
    background: transparent;
    color: var(--color-text-muted, #8a8a8e);
    border: 1px solid var(--color-border, #e5e5e5);
  }

    .btn-secondary:hover {
      background: var(--color-background-mute, #f8f9fa);
    }

  /* Replies container */
  .replies-container {
    margin-top: 12px;
  }

  /* Continue thread button (Reddit-style) */
  .continue-thread {
    margin-top: 12px;
    padding-left: 8px;
  }

  .continue-thread-btn {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 8px 12px;
    background: var(--color-background-mute, #f8f9fa);
    border: 1px solid var(--color-border, #e5e5e5);
    border-radius: 6px;
    color: var(--color-accent, #ff6d00);
    font-size: 13px;
    font-weight: 600;
    text-decoration: none;
    cursor: pointer;
    transition: all 0.2s ease;
  }

    .continue-thread-btn:hover {
      background: var(--color-background-soft, #f0f0f0);
      border-color: var(--color-accent, #ff6d00);
      transform: translateX(2px);
    }

  .continue-arrow {
    font-size: 16px;
    font-weight: bold;
    transition: transform 0.2s ease;
  }

  .continue-thread-btn:hover .continue-arrow {
    transform: translateX(3px);
  }

  /* Load more */
  .load-more {
    margin-top: 12px;
  }

  .load-more-btn {
    padding: 0;
    background: none;
    border: none;
    color: var(--color-accent, #ff6d00);
    font-size: 13px;
    cursor: pointer;
    text-decoration: none;
    font-weight: 500;
  }

    .load-more-btn:hover {
      text-decoration: underline;
    }

  /* Dark mode support */
  @media (prefers-color-scheme: dark) {
    .vertical-line::before {
      background: #6b7280 /* Blue for dark mode */
    }

    .vertical-line:hover::before {
      background: #f7fee7;
    }

    .avatar {
      background: var(--color-background-mute);
    }
  }

  /* Mobile responsive */
  @media screen and (max-width: 768px) {
    .comment-wrapper.has-line {
      padding-left: 24px; /* Increased from 12px to 24px for better spacing */
    }

    .vertical-line {
      width: 24px; /* Increased from 12px to 24px to match padding */
    }

    .username {
      max-width: 100px;
    }

    .vote-controls {
      flex-direction: column;
      gap: 2px;
    }

    .vote-count {
      min-width: 20px;
    }
  }
</style>
