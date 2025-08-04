<template>
    <div class="comments-container">
        <!-- Comment Form -->
        <div v-if="isAuthenticated" class="comments-form">
            <div class="comment-avatar">
                <div class="avatar-placeholder">
                    <i class="fas fa-user"></i>
                </div>
            </div>
            <div class="comment-input-container">
                <textarea class="comment-input"
                          v-model="newCommentText"
                          placeholder="Write your comment here..."
                          :disabled="submittingComment"></textarea>
                <div class="comment-buttons">
                    <button class="btn-zor variant-7q9 is-11c"
                            @click="submitComment"
                            :disabled="!newCommentText.trim() || submittingComment">
                        <i v-if="submittingComment" class="fas fa-spinner fa-spin"></i>
                        {{ submittingComment ? 'Posting...' : 'Post Comment' }}
                    </button>
                </div>
            </div>
        </div>
        <div v-else class="comments-login-prompt">
            <p>Please <a href="/Identity/Account/Login">log in</a> to post comments</p>
        </div>

        <!-- Comments Header -->
        <div class="comments-sort">
            <div class="comments-count">
                <span>{{ totalComments }} Comments</span>
            </div>
            <div class="comments-sort-options">
                <a href="#"
                   :class="['btn-zor', 'link-qky', sortBy === 'newest' ? 'variant-7q9 active' : 'variant-9vn']"
                   @click.prevent="setSortBy('newest')">
                    Latest
                </a>
                <a href="#"
                   :class="['btn-zor', 'link-qky', sortBy === 'oldest' ? 'variant-7q9 active' : 'variant-9vn']"
                   @click.prevent="setSortBy('oldest')">
                    Oldest
                </a>
                <a href="#"
                   :class="['btn-zor', 'link-qky', sortBy === 'likes' ? 'variant-7q9 active' : 'variant-9vn']"
                   @click.prevent="setSortBy('likes')">
                    Most Liked
                </a>
            </div>
        </div>

        <!-- Comments List -->
        <div class="comments-list">
            <template v-if="sortedComments.length > 0">
                <div v-for="comment in sortedComments"
                     :key="comment.id"
                     class="comment-item"
                     :id="`comment-${comment.id}`">
                    <div class="comment-avatar">
                        <img v-if="comment.userAvatarUrl"
                             :src="comment.userAvatarUrl"
                             :alt="`${comment.userName} avatar`" />
                        <div v-else class="avatar-placeholder">
                            <i class="fas fa-user"></i>
                        </div>
                    </div>
                    <div class="comment-content">
                        <div class="comment-header">
                            <div class="comment-username">{{ comment.userName || 'Anonymous' }}</div>
                            <div class="comment-date">{{ formatDate(comment.postedDate) }}</div>
                        </div>
                        <div class="comment-text">{{ comment.content }}</div>
                        <div class="comment-actions">
                            <a href="#"
                               :class="['btn-zor', 'link-qky', comment.currentUserLiked ? 'variant-7q9' : 'variant-9vn', 'like-comment']"
                               @click.prevent="toggleLike(comment)">
                                <i class="fas fa-thumbs-up"></i>
                                Like (<span class="like-count">{{ comment.likesCount }}</span>)
                            </a>
                            <a href="#"
                               :class="['btn-zor', 'link-qky', comment.currentUserDisliked ? 'variant-7q9' : 'variant-9vn', 'dislike-comment']"
                               @click.prevent="toggleDislike(comment)">
                                <i class="fas fa-thumbs-down"></i>
                                Dislike (<span class="dislike-count">{{ comment.dislikesCount }}</span>)
                            </a>
                            <a href="#"
                               class="btn-zor link-qky variant-9vn reply-comment"
                               @click.prevent="toggleReplyForm(comment.id)">
                                <i class="fas fa-reply"></i> Reply
                            </a>

                            <a v-if="canDeleteComment(comment)"
                               href="#"
                               class="btn-zor link-qky variant-9vn delete-comment"
                               @click.prevent="deleteComment(comment.id)">
                                <i class="fas fa-trash"></i> Delete
                            </a>
                        </div>

                        <!-- Reply Form -->
                        <div v-if="showReplyForm === comment.id" class="reply-form-container">
                            <div class="comments-form reply-form">
                                <div class="comment-avatar">
                                    <div class="avatar-placeholder">
                                        <i class="fas fa-user"></i>
                                    </div>
                                </div>
                                <div class="comment-input-container">
                                    <textarea class="comment-input"
                                              v-model="replyText"
                                              placeholder="Write your reply here..."
                                              :disabled="submittingReply"></textarea>
                                    <div class="comment-buttons">
                                        <button class="btn-zor variant-7q9 is-11c"
                                                @click="submitReply(comment.id)"
                                                :disabled="!replyText.trim() || submittingReply">
                                            <i v-if="submittingReply" class="fas fa-spinner fa-spin"></i>
                                            {{ submittingReply ? 'Posting...' : 'Submit Reply' }}
                                        </button>
                                        <button class="btn-zor variant-b3o is-hfa"
                                                @click="cancelReply()">
                                            Cancel
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Replies -->
                        <div v-if="getReplies(comment.id).length > 0" class="comment-replies">
                            <div v-for="reply in getReplies(comment.id)"
                                 :key="reply.id"
                                 class="comment-item reply-item"
                                 :id="`comment-${reply.id}`">
                                <div class="comment-avatar">
                                    <img v-if="reply.userAvatarUrl"
                                         :src="reply.userAvatarUrl"
                                         :alt="`${reply.userName} avatar`" />
                                    <div v-else class="avatar-placeholder">
                                        <i class="fas fa-user"></i>
                                    </div>
                                </div>
                                <div class="comment-content">
                                    <div class="comment-header">
                                        <div class="comment-username">{{ reply.userName || 'Anonymous' }}</div>
                                        <div class="comment-date">{{ formatDate(reply.postedDate) }}</div>
                                    </div>
                                    <div class="comment-text">{{ reply.content }}</div>
                                    <div class="comment-actions">
                                        <a href="#"
                                           :class="['btn-zor', 'link-qky', reply.currentUserLiked ? 'variant-7q9' : 'variant-9vn', 'like-comment']"
                                           @click.prevent="toggleLike(reply)">
                                            <i class="fas fa-thumbs-up"></i>
                                            (<span class="like-count">{{ reply.likesCount }}</span>)
                                        </a>
                                        <a href="#"
                                           :class="['btn-zor', 'link-qky', reply.currentUserDisliked ? 'variant-7q9' : 'variant-9vn', 'dislike-comment']"
                                           @click.prevent="toggleDislike(reply)">
                                            <i class="fas fa-thumbs-down"></i>
                                            (<span class="dislike-count">{{ reply.dislikesCount }}</span>)
                                        </a>

                                        <a v-if="canDeleteComment(reply)"
                                           href="#"
                                           class="btn-zor link-qky variant-9vn delete-comment"
                                           @click.prevent="deleteComment(reply.id)">
                                            <i class="fas fa-trash"></i>
                                        </a>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </template>

            <div v-else class="empty-comments">
                <div class="empty-icon">
                    <i class="fas fa-comments"></i>
                </div>
                <div class="empty-text">No comments yet. Be the first to share your thoughts!</div>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'CommentsComponent',
    props: {
        comments: {
            type: Array,
            default: () => []
        },
        targetId: {
            type: [Number, String],
            required: true
        },
        targetType: {
            type: String,
            required: true
        },
        isAuthenticated: {
            type: Boolean,
            default: false
        },
        currentUser: {
            type: String,
            default: ''
        },
        isAdmin: {
            type: Boolean,
            default: false
        }
    },
    emits: ['comments-updated'],
    data() {
        return {
            localComments: [],
            sortBy: 'newest',
            newCommentText: '',
            replyText: '',
            showReplyForm: null,
            submittingComment: false,
            submittingReply: false
        }
    },
    computed: {
        topLevelComments() {
            return this.localComments.filter(comment => !comment.parentCommentId)
        },

        totalComments() {
            return this.localComments.length
        },

        sortedComments() {
            const comments = [...this.topLevelComments]

            switch (this.sortBy) {
                case 'newest':
                    return comments.sort((a, b) => new Date(b.postedDate) - new Date(a.postedDate))
                case 'oldest':
                    return comments.sort((a, b) => new Date(a.postedDate) - new Date(b.postedDate))
                case 'likes':
                    return comments.sort((a, b) => b.likesCount - a.likesCount)
                default:
                    return comments
            }
        }
    },
    watch: {
        comments: {
            handler(newComments) {
                this.localComments = [...newComments]
            },
            immediate: true
        }
    },
    methods: {
        setSortBy(sortType) {
            this.sortBy = sortType
        },

        getReplies(commentId) {
            return this.localComments
                .filter(comment => comment.parentCommentId === commentId)
                .sort((a, b) => new Date(a.postedDate) - new Date(b.postedDate))
        },

        toggleReplyForm(commentId) {
            this.showReplyForm = this.showReplyForm === commentId ? null : commentId
            this.replyText = ''
        },

        cancelReply() {
            this.showReplyForm = null
            this.replyText = ''
        },

        async submitComment() {
            if (!this.newCommentText.trim() || this.submittingComment) return

            this.submittingComment = true

            try {
                const response = await this.safeFetch('/api/Comments/AddComment', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        content: this.newCommentText,
                        targetType: parseInt(this.targetType),
                        targetId: parseInt(this.targetId),
                        parentCommentId: null
                    })
                })

                if (response.success !== false) {
                    // Add the new comment to local state
                    this.localComments.push(response)
                    this.newCommentText = ''
                    this.$emit('comments-updated', this.localComments)
                    this.showToast('Comment posted successfully!', 'success')
                } else {
                    throw new Error(response.error || 'Failed to post comment')
                }
            } catch (error) {
                console.error('Error posting comment:', error)
                this.showToast('Failed to post comment. Please try again.', 'error')
            }

            this.submittingComment = false
        },

        async submitReply(parentCommentId) {
            if (!this.replyText.trim() || this.submittingReply) return

            this.submittingReply = true

            try {
                const response = await this.safeFetch('/api/Comments/AddComment', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        content: this.replyText,
                        targetType: parseInt(this.targetType),
                        targetId: parseInt(this.targetId),
                        parentCommentId: parentCommentId
                    })
                })

                if (response.success !== false) {
                    // Add the new reply to local state
                    this.localComments.push(response)
                    this.cancelReply()
                    this.$emit('comments-updated', this.localComments)
                    this.showToast('Reply posted successfully!', 'success')
                } else {
                    throw new Error(response.error || 'Failed to post reply')
                }
            } catch (error) {
                console.error('Error posting reply:', error)
                this.showToast('Failed to post reply. Please try again.', 'error')
            }

            this.submittingReply = false
        },

        async toggleLike(comment) {
            if (!this.isAuthenticated) {
                this.showToast('Please log in to like comments.', 'error')
                return
            }

            try {
                const response = await this.safeFetch('/api/Comments/ReactToComment', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        commentId: comment.id,
                        isLike: true
                    })
                })

                if (response.success !== false) {
                    // Update comment in local state
                    const commentIndex = this.localComments.findIndex(c => c.id === comment.id)
                    if (commentIndex !== -1) {
                        this.localComments[commentIndex].likesCount = response.likesCount
                        this.localComments[commentIndex].dislikesCount = response.dislikesCount
                        this.localComments[commentIndex].currentUserLiked = response.userLiked
                        this.localComments[commentIndex].currentUserDisliked = response.userDisliked
                    }
                } else {
                    throw new Error(response.error || 'Failed to like comment')
                }
            } catch (error) {
                console.error('Error liking comment:', error)
                this.showToast('Failed to like comment. Please try again.', 'error')
            }
        },

        async toggleDislike(comment) {
            if (!this.isAuthenticated) {
                this.showToast('Please log in to dislike comments.', 'error')
                return
            }

            try {
                const response = await this.safeFetch('/api/Comments/ReactToComment', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        commentId: comment.id,
                        isLike: false
                    })
                })

                if (response.success !== false) {
                    // Update comment in local state
                    const commentIndex = this.localComments.findIndex(c => c.id === comment.id)
                    if (commentIndex !== -1) {
                        this.localComments[commentIndex].likesCount = response.likesCount
                        this.localComments[commentIndex].dislikesCount = response.dislikesCount
                        this.localComments[commentIndex].currentUserLiked = response.userLiked
                        this.localComments[commentIndex].currentUserDisliked = response.userDisliked
                    }
                } else {
                    throw new Error(response.error || 'Failed to dislike comment')
                }
            } catch (error) {
                console.error('Error disliking comment:', error)
                this.showToast('Failed to dislike comment. Please try again.', 'error')
            }
        },

        async deleteComment(commentId) {
            if (!confirm('Are you sure you want to delete this comment?')) return

            try {
                const response = await this.safeFetch(`/api/Comments/DeleteComment/${commentId}`, {
                    method: 'DELETE'
                })

                if (response.success !== false) {
                    // Remove comment and its replies from local state
                    this.localComments = this.localComments.filter(c =>
                        c.id !== commentId && c.parentCommentId !== commentId
                    )
                    this.$emit('comments-updated', this.localComments)
                    this.showToast('Comment deleted successfully!', 'success')
                } else {
                    throw new Error(response.error || 'Failed to delete comment')
                }
            } catch (error) {
                console.error('Error deleting comment:', error)
                this.showToast('Failed to delete comment. Please try again.', 'error')
            }
        },

        canDeleteComment(comment) {
            return this.isAuthenticated && (this.isAdmin || comment.userName === this.currentUser)
        },

        formatDate(dateString) {
            const date = new Date(dateString)
            return date.toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            })
        },

        async safeFetch(url, options = {}) {
            try {
                const response = await fetch(url, {
                    headers: {
                        'X-CSRF-TOKEN': this.getCsrfToken(),
                        ...options.headers
                    },
                    ...options
                })

                if (!response.ok) {
                    if (response.status === 401 || response.status === 403) {
                        return { success: false, error: 'Authentication required' }
                    }
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`)
                }

                // Handle empty responses (like DELETE operations)
                if (response.status === 204 || response.headers.get('content-length') === '0') {
                    return { success: true }
                }

                const contentType = response.headers.get('content-type')
                if (contentType && contentType.includes('application/json')) {
                    return await response.json()
                }

                return { success: true }
            } catch (error) {
                console.error('Fetch error:', error)
                return { success: false, error: error.message }
            }
        },

        getCsrfToken() {
            return document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },

        showToast(message, type = 'info') {
            // Create toast element if it doesn't exist
            let toastContainer = document.getElementById('toast-container')
            if (!toastContainer) {
                toastContainer = document.createElement('div')
                toastContainer.id = 'toast-container'
                toastContainer.style.cssText = `
                    position: fixed;
                    bottom: 20px;
                    right: 20px;
                    z-index: 1000;
                `
                document.body.appendChild(toastContainer)
            }

            // Create toast
            const toast = document.createElement('div')
            toast.className = `toast toast-${type}`
            const bgColor = type === 'success' ? '#4caf50' : type === 'error' ? '#f44336' : '#2196f3'
            toast.style.cssText = `
                min-width: 250px;
                background-color: ${bgColor};
                color: white;
                padding: 15px;
                margin-bottom: 10px;
                border-radius: 5px;
                box-shadow: 0 2px 5px rgba(0,0,0,0.2);
                animation: fadeIn 0.5s, fadeOut 0.5s 2.5s forwards;
                opacity: 0;
            `
            toast.textContent = message

            // Add to container
            toastContainer.appendChild(toast)

            // Remove after 3 seconds
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.remove()
                }
                if (toastContainer.children.length === 0) {
                    toastContainer.remove()
                }
            }, 3000)

            // Trigger animation
            setTimeout(() => {
                toast.style.opacity = '1'
            }, 100)
        }
    }
}
</script>

<style scoped>
    /* Use existing comment styles from _CommentsPartial.cshtml */
    .comments-container {
        width: 100%;
    }

    .comments-form {
        display: flex;
        gap: 15px;
        margin-bottom: 30px;
        padding: 20px;
        background-color: var(--background-elevated);
        border-radius: 8px;
    }

    .comment-avatar {
        flex-shrink: 0;
    }

        .comment-avatar img {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            object-fit: cover;
        }

    .avatar-placeholder {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        background-color: var(--background-elevated-2);
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--text-muted);
    }

    .comment-input-container {
        flex: 1;
    }

    .comment-input {
        width: 100%;
        min-height: 80px;
        padding: 10px;
        border: 1px solid var(--border-base);
        border-radius: 6px;
        background-color: var(--background);
        color: var(--text-primary);
        resize: vertical;
        font-family: inherit;
    }

        .comment-input:focus {
            outline: none;
            border-color: var(--primary-color);
        }

    .comment-buttons {
        margin-top: 10px;
        display: flex;
        gap: 10px;
    }

    .comments-login-prompt {
        padding: 20px;
        text-align: center;
        background-color: var(--background-elevated);
        border-radius: 8px;
        margin-bottom: 30px;
    }

        .comments-login-prompt a {
            color: var(--primary-color);
        }

    .comments-sort {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 20px;
        padding-bottom: 15px;
        border-bottom: 1px solid var(--border-base);
    }

    .comments-count {
        font-weight: 600;
        color: var(--text-primary);
    }

    .comments-sort-options {
        display: flex;
        gap: 15px;
    }

    .comment-item {
        margin-bottom: 24px;
        padding: 15px;
        border-radius: 8px;
        background-color: var(--background-elevated-2);
        transition: background-color 0.2s ease;
    }

        .comment-item:hover {
            background-color: var(--background-elevated-3);
        }

    .reply-item {
        margin-left: 40px;
        margin-top: 10px;
        background-color: var(--background);
    }

    .comment-replies {
        margin-top: 15px;
        padding-top: 10px;
        border-top: 1px solid var(--border-base);
    }

    .comment-content {
        display: flex;
        flex-direction: column;
        gap: 10px;
    }

    .comment-header {
        display: flex;
        align-items: center;
        gap: 10px;
    }

    .comment-username {
        font-weight: 600;
        color: var(--text-primary);
    }

    .comment-date {
        font-size: 0.9rem;
        color: var(--text-muted);
    }

    .comment-text {
        line-height: 1.5;
        color: var(--text-primary);
    }

    .comment-actions {
        display: flex;
        gap: 15px;
        flex-wrap: wrap;
    }

        .comment-actions a {
            font-size: 0.9rem;
            text-decoration: none;
            display: flex;
            align-items: center;
            gap: 5px;
        }

    .reply-form-container {
        margin-top: 15px;
    }

    .reply-form {
        background-color: var(--background-elevated);
        border-radius: 6px;
        padding: 15px;
    }

    .empty-comments {
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
    }

    /* Mobile responsive */
    @media (max-width: 768px) {
        .comments-sort {
            flex-direction: column;
            gap: 15px;
            align-items: stretch;
        }

        .comments-sort-options {
            justify-content: center;
        }

        .reply-item {
            margin-left: 20px;
        }

        .comment-actions {
            font-size: 0.8rem;
        }
    }
</style>