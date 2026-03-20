<!-- AdminCommentsManagement.vue - Complete Admin Interface with Soft Delete Features -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] p-6">
    <div class="max-w-7xl mx-auto">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-text)] mb-2">Comment Management</h1>
        <p class="text-[var(--color-text)] opacity-70">Monitor and moderate user comments across the platform</p>
      </div>

      <!-- Stats Cards -->
      <div class="grid grid-cols-1 md:grid-cols-5 gap-6 mb-8">
        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6">
          <div class="flex items-center">
            <div class="p-3 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
              <svg class="w-6 h-6 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
              </svg>
            </div>
            <div class="ml-4">
              <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Active Comments</p>
              <p class="text-2xl font-bold text-[var(--color-text)]">{{ (stats.totalComments || 0).toLocaleString() }}</p>
            </div>
          </div>
        </div>

        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6">
          <div class="flex items-center">
            <div class="p-3 bg-gray-100 dark:bg-gray-900/30 rounded-lg">
              <svg class="w-6 h-6 text-gray-600 dark:text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
              </svg>
            </div>
            <div class="ml-4">
              <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Deleted Comments</p>
              <p class="text-2xl font-bold text-[var(--color-text)]">{{ (stats.deletedComments || 0).toLocaleString() }}</p>
            </div>
          </div>
        </div>

        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6">
          <div class="flex items-center">
            <div class="p-3 bg-green-100 dark:bg-green-900/30 rounded-lg">
              <svg class="w-6 h-6 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
              </svg>
            </div>
            <div class="ml-4">
              <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Today's Comments</p>
              <p class="text-2xl font-bold text-[var(--color-text)]">{{ (stats.commentsToday || 0).toLocaleString() }}</p>
            </div>
          </div>
        </div>

        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6">
          <div class="flex items-center">
            <div class="p-3 bg-amber-100 dark:bg-amber-900/30 rounded-lg">
              <svg class="w-6 h-6 text-amber-600 dark:text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
              </svg>
            </div>
            <div class="ml-4">
              <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Reported</p>
              <p class="text-2xl font-bold text-[var(--color-text)]">{{ (stats.reportedComments || 0).toLocaleString() }}</p>
            </div>
          </div>
        </div>

        <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6">
          <div class="flex items-center">
            <div class="p-3 bg-purple-100 dark:bg-purple-900/30 rounded-lg">
              <svg class="w-6 h-6 text-purple-600 dark:text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
              </svg>
            </div>
            <div class="ml-4">
              <p class="text-sm font-medium text-[var(--color-text)] opacity-60">Active Users</p>
              <p class="text-2xl font-bold text-[var(--color-text)]">{{ (stats.activeComments || 0).toLocaleString() }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Filters and Search -->
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6 mb-8">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
          <!-- Search -->
          <div class="md:col-span-2">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Search Comments</label>
            <div class="relative">
              <input v-model="searchQuery"
                     @input="debouncedSearch"
                     type="text"
                     placeholder="Search by content, user, or title..."
                     class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2 pl-10 focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-transparent">
              <svg class="absolute left-3 top-3 w-4 h-4 text-[var(--color-text)] opacity-40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
              </svg>
            </div>
          </div>

          <!-- Filter by Type -->
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Content Type</label>
            <select v-model="filters.targetType" @change="loadComments"
                    class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-transparent">
              <option value="">All Types</option>
              <option value="1">Title Comments</option>
              <option value="2">Chapter Comments</option>
              <option value="3">Image Comments</option>
            </select>
          </div>

          <!-- Sort By -->
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Sort By</label>
            <select v-model="filters.sortBy" @change="loadComments"
                    class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:border-transparent">
              <option value="newest">Newest First</option>
              <option value="oldest">Oldest First</option>
              <option value="most_liked">Most Liked</option>
              <option value="most_reported">Most Reported</option>
              <option value="recently_deleted">Recently Deleted</option>
            </select>
          </div>
        </div>

        <!-- Advanced Filters -->
        <div class="flex justify-between items-center mt-4">
          <div class="flex items-center space-x-4">
            <label class="flex items-center">
              <input v-model="filters.showDeleted" @change="loadComments" type="checkbox"
                     class="rounded border-[var(--color-border)] text-[var(--color-accent)] focus:ring-[var(--color-accent)]">
              <span class="ml-2 text-sm text-[var(--color-text)]">Show deleted comments</span>
            </label>
            <label class="flex items-center">
              <input v-model="filters.showReported" @change="loadComments" type="checkbox"
                     class="rounded border-[var(--color-border)] text-[var(--color-accent)] focus:ring-[var(--color-accent)]">
              <span class="ml-2 text-sm text-[var(--color-text)]">Show only reported</span>
            </label>
          </div>

          <div class="flex items-center space-x-2">
            <!-- Bulk Actions -->
            <div v-if="selectedComments.length > 0" class="flex items-center space-x-2">
              <span class="text-sm text-[var(--color-text)] opacity-60">
                {{ selectedComments.length }} selected
              </span>
              <button @click="bulkDeleteComments"
                      class="bg-red-600 text-white px-3 py-2 rounded-lg text-sm font-medium hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                Delete Selected
              </button>
              <button @click="clearSelection"
                      class="border border-[var(--color-border)] text-[var(--color-text)] px-3 py-2 rounded-lg text-sm font-medium hover:bg-[var(--color-background-mute)] transition-colors duration-200">
                Clear
              </button>
            </div>

            <button @click="loadComments" :disabled="loading"
                    class="bg-[var(--color-accent)] text-white px-4 py-2 rounded-lg font-medium hover:bg-[var(--color-accent-hover)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] disabled:opacity-50 transition-colors duration-200">
              <svg v-if="loading" class="animate-spin w-4 h-4 mr-2 inline" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              {{ loading ? 'Loading...' : 'Refresh' }}
            </button>
          </div>
        </div>
      </div>

      <!-- Comments List -->
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl overflow-hidden">
        <!-- Loading State -->
        <div v-if="loading" class="p-12 text-center">
          <svg class="animate-spin w-8 h-8 text-[var(--color-accent)] mx-auto mb-4" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p class="text-[var(--color-text)] opacity-70">Loading comments...</p>
        </div>

        <!-- Empty State -->
        <div v-else-if="comments.length === 0" class="p-12 text-center">
          <svg class="w-16 h-16 text-[var(--color-text)] opacity-30 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"></path>
          </svg>
          <h3 class="text-lg font-medium text-[var(--color-text)] mb-2">No comments found</h3>
          <p class="text-[var(--color-text)] opacity-60">Try adjusting your search criteria or filters.</p>
        </div>

        <!-- Comments Table -->
        <div v-else class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-[var(--color-background-mute)] border-b border-[var(--color-border)]">
              <tr>
                <th class="text-left py-4 px-6 font-medium text-[var(--color-text)]">
                  <input type="checkbox"
                         :checked="allCommentsSelected"
                         @change="toggleSelectAll"
                         class="rounded border-[var(--color-border)] text-[var(--color-accent)] focus:ring-[var(--color-accent)]">
                </th>
                <th class="text-left py-4 px-6 font-medium text-[var(--color-text)]">User</th>
                <th class="text-left py-4 px-6 font-medium text-[var(--color-text)]">Comment</th>
                <th class="text-left py-4 px-6 font-medium text-[var(--color-text)]">Target</th>
                <th class="text-left py-4 px-6 font-medium text-[var(--color-text)]">Status</th>
                <th class="text-left py-4 px-6 font-medium text-[var(--color-text)]">Date</th>
                <th class="text-left py-4 px-6 font-medium text-[var(--color-text)]">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--color-border)]">
              <tr v-for="comment in comments" :key="comment.id"
                  class="hover:bg-[var(--color-background)] transition-colors duration-150"
                  :class="{
                    'bg-red-50 dark:bg-red-900/10': comment.isReported && !comment.isDeleted,
                    'bg-gray-50 dark:bg-gray-900/10': comment.isDeleted
                  }">
                <!-- Checkbox -->
                <td class="py-4 px-6">
                  <input type="checkbox"
                         :checked="selectedComments.includes(comment.id)"
                         @change="toggleCommentSelection(comment.id)"
                         class="rounded border-[var(--color-border)] text-[var(--color-accent)] focus:ring-[var(--color-accent)]">
                </td>

                <!-- User -->
                <td class="py-4 px-6">
                  <div class="flex items-center">
                    <div class="w-8 h-8 bg-[var(--color-background-mute)] border border-[var(--color-border)] rounded-full flex items-center justify-center mr-3">
                      <span class="text-xs font-medium text-[var(--color-text)]">{{ comment.userName.charAt(0).toUpperCase() }}</span>
                    </div>
                    <div>
                      <p class="font-medium text-[var(--color-text)]">{{ comment.userName }}</p>
                      <p class="text-xs text-[var(--color-text)] opacity-60">{{ comment.userId.substring(0, 8) }}...</p>
                    </div>
                  </div>
                </td>

                <!-- Comment Content -->
                <td class="py-4 px-6 max-w-md">
                  <div class="space-y-2">
                    <p v-if="!comment.isDeleted" class="text-sm text-[var(--color-text)] line-clamp-3">
                      {{ comment.content }}
                    </p>
                    <div v-else class="space-y-1">
                      <p class="text-sm text-gray-500 dark:text-gray-400 italic">[Deleted Comment]</p>
                      <details class="cursor-pointer">
                        <summary class="text-xs text-[var(--color-text)] opacity-60 hover:opacity-100">Show original</summary>
                        <p class="text-sm text-[var(--color-text)] opacity-70 mt-1">{{ comment.content }}</p>
                      </details>
                    </div>
                    <div v-if="comment.parentCommentId" class="text-xs text-[var(--color-text)] opacity-60 bg-[var(--color-background-mute)] px-2 py-1 rounded">
                      Reply to comment #{{ comment.parentCommentId }}
                    </div>
                  </div>
                </td>

                <!-- Target -->
                <td class="py-4 px-6">
                  <div class="text-sm">
                    <p class="font-medium text-[var(--color-text)]">{{ getTargetTypeName(comment) }}</p>
                    <p class="text-[var(--color-text)] opacity-60 truncate max-w-32">{{ comment.targetTitle || 'Unknown' }}</p>
                  </div>
                </td>

                <!-- Status -->
                <td class="py-4 px-6">
                  <div class="space-y-2">
                    <!-- Deletion Status -->
                    <div v-if="comment.isDeleted" class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300">
                      <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                      </svg>
                      Deleted
                    </div>

                    <!-- Reported Status -->
                    <div v-if="comment.isReported && !comment.isDeleted" class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300">
                      <svg class="w-3 h-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
                      </svg>
                      Reported
                    </div>

                    <!-- Reactions -->
                    <div class="flex items-center space-x-2 text-xs">
                      <span class="text-green-600 dark:text-green-400">👍 {{ comment.likesCount }}</span>
                      <span class="text-red-600 dark:text-red-400">👎 {{ comment.dislikesCount }}</span>
                    </div>
                  </div>
                </td>

                <!-- Date -->
                <td class="py-4 px-6">
                  <div class="text-sm space-y-1">
                    <p class="text-[var(--color-text)]">{{ formatDate(comment.postedDate) }}</p>
                    <p class="text-[var(--color-text)] opacity-60">{{ formatTimeAgo(comment.postedDate) }}</p>
                    <div v-if="comment.isDeleted && comment.deletedAt" class="text-xs">
                      <p class="text-red-600 dark:text-red-400">Deleted: {{ formatTimeAgo(comment.deletedAt) }}</p>
                      <p v-if="comment.deletedByUserName" class="text-[var(--color-text)] opacity-60">by {{ comment.deletedByUserName }}</p>
                    </div>
                  </div>
                </td>

                <!-- Actions -->
                <td class="py-4 px-6">
                  <div class="flex items-center space-x-2">
                    <button @click="viewComment(comment)"
                            title="View Details"
                            class="text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 transition-colors duration-200">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                      </svg>
                    </button>

                    <!-- Restore Button for Deleted Comments -->
                    <button v-if="comment.isDeleted"
                            @click="restoreComment(comment)"
                            title="Restore Comment"
                            class="text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300 transition-colors duration-200">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
                      </svg>
                    </button>

                    <!-- Delete/Permanent Delete Button -->
                    <button v-if="!comment.isDeleted"
                            @click="deleteComment(comment)"
                            title="Soft Delete"
                            class="text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 transition-colors duration-200">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                      </svg>
                    </button>
                    <button v-else
                            @click="permanentlyDeleteComment(comment)"
                            title="Permanently Delete"
                            class="text-red-800 dark:text-red-600 hover:text-red-900 dark:hover:text-red-500 transition-colors duration-200">
                      <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                        <path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z" />
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="pagination.totalPages > 1" class="p-6 border-t border-[var(--color-border)]">
          <div class="flex items-center justify-between">
            <div class="text-sm text-[var(--color-text)] opacity-70">
              Showing {{ ((pagination.page - 1) * pagination.pageSize) + 1 }} to
              {{ Math.min(pagination.page * pagination.pageSize, pagination.totalCount) }} of
              {{ pagination.totalCount }} comments
            </div>

            <div class="flex items-center space-x-2">
              <button @click="goToPage(pagination.page - 1)"
                      :disabled="!pagination.hasPrevious"
                      class="px-3 py-2 text-sm border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-mute)] disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                Previous
              </button>

              <div class="flex items-center space-x-1">
                <button v-for="page in getVisiblePages()" :key="page"
                        @click="goToPage(page)"
                        :disabled="page === '...'"
                        :class="[
                          'px-3 py-2 text-sm rounded-lg transition-colors duration-200',
                          page === pagination.page
                            ? 'bg-[var(--color-accent)] text-white'
                            : page === '...'
                            ? 'cursor-default'
                            : 'border border-[var(--color-border)] hover:bg-[var(--color-background-mute)]'
                        ]">
                  {{ page }}
                </button>
              </div>

              <button @click="goToPage(pagination.page + 1)"
                      :disabled="!pagination.hasNext"
                      class="px-3 py-2 text-sm border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-mute)] disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
                Next
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Comment Detail Modal -->
    <div v-if="selectedComment"
         class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-4"
         @click="closeCommentModal">
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl max-w-4xl w-full max-h-[90vh] overflow-hidden"
           @click.stop>
        <div class="p-6 border-b border-[var(--color-border)]">
          <div class="flex items-center justify-between">
            <h3 class="text-xl font-semibold text-[var(--color-text)]">Comment Details</h3>
            <button @click="closeCommentModal"
                    class="w-8 h-8 text-[var(--color-text)] opacity-60 hover:opacity-100 rounded-lg hover:bg-[var(--color-background-mute)] flex items-center justify-center transition-all duration-200">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>
        </div>

        <div class="p-6 overflow-y-auto max-h-[70vh]">
          <div class="space-y-6">
            <!-- Status Badges -->
            <div v-if="selectedComment.isDeleted || selectedComment.isReported" class="flex items-center space-x-2">
              <div v-if="selectedComment.isDeleted" class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7"></path>
                </svg>
                Deleted
              </div>
              <div v-if="selectedComment.isReported" class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01"></path>
                </svg>
                Reported
              </div>
            </div>

            <!-- Comment Info -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h4 class="font-medium text-[var(--color-text)] mb-2">User Information</h4>
                <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4">
                  <p><span class="font-medium">Username:</span> {{ selectedComment.userName }}</p>
                  <p><span class="font-medium">User ID:</span> {{ selectedComment.userId }}</p>
                  <p><span class="font-medium">Posted:</span> {{ formatDate(selectedComment.postedDate) }}</p>
                </div>
              </div>

              <div>
                <h4 class="font-medium text-[var(--color-text)] mb-2">Content Information</h4>
                <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4">
                  <p><span class="font-medium">Target:</span> {{ getTargetTypeName(selectedComment) }}</p>
                  <p><span class="font-medium">Likes:</span> {{ selectedComment.likesCount || 0 }}</p>
                  <p><span class="font-medium">Dislikes:</span> {{ selectedComment.dislikesCount || 0 }}</p>
                </div>
              </div>
            </div>

            <!-- Deletion Info -->
            <div v-if="selectedComment.isDeleted">
              <h4 class="font-medium text-[var(--color-text)] mb-2">Deletion Information</h4>
              <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
                <p><span class="font-medium">Deleted At:</span> {{ formatDate(selectedComment.deletedAt) }}</p>
                <p><span class="font-medium">Deleted By:</span> {{ selectedComment.deletedByUserName || 'System' }}</p>
                <p v-if="selectedComment.deletionReason"><span class="font-medium">Reason:</span> {{ selectedComment.deletionReason }}</p>
              </div>
            </div>

            <!-- Comment Content -->
            <div>
              <h4 class="font-medium text-[var(--color-text)] mb-2">Comment Content</h4>
              <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-lg p-4">
                <p class="whitespace-pre-wrap">{{ selectedComment.content }}</p>
              </div>
            </div>

            <!-- Actions -->
            <div class="flex justify-end space-x-3">
              <button @click="closeCommentModal"
                      class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-soft)] transition-colors duration-200">
                Close
              </button>
              <button v-if="selectedComment.isDeleted"
                      @click="restoreComment(selectedComment)"
                      class="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
                Restore Comment
              </button>
              <button v-else
                      @click="deleteComment(selectedComment)"
                      class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                Delete Comment
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { commentsService } from '../../services/commentsService'

  export default {
    name: 'AdminCommentsManagement',
    data() {
      return {
        loading: false,
        comments: [],
        selectedComments: [],
        stats: {
          totalComments: 0,
          deletedComments: 0,
          commentsToday: 0,
          reportedComments: 0,
          activeComments: 0,
          commentsThisWeek: 0,
          commentsThisMonth: 0
        },
        pagination: {
          page: 1,
          pageSize: 20,
          totalCount: 0,
          totalPages: 0,
          hasNext: false,
          hasPrevious: false
        },
        filters: {
          targetType: '',
          sortBy: 'newest',
          showReported: false,
          showDeleted: false
        },
        searchQuery: '',
        selectedComment: null,
        searchTimeout: null
      }
    },
    computed: {
      allCommentsSelected() {
        return this.comments.length > 0 && this.selectedComments.length === this.comments.length
      }
    },
    async mounted() {
      await this.loadStats()
      await this.loadComments()
    },
    methods: {
      async loadStats() {
        try {
          const result = await commentsService.getCommentStatsForAdmin()
          if (result.success) {
            this.stats = result.data
          } else {
            this.showToast('Failed to load statistics', 'error')
          }
        } catch (error) {
          console.error('Error loading stats:', error)
          this.showToast('Error loading statistics', 'error')
        }
      },

      async loadComments() {
        this.loading = true
        try {
          const result = await commentsService.getAllCommentsForAdmin({
            page: this.pagination.page,
            pageSize: this.pagination.pageSize,
            sortBy: this.filters.sortBy,
            targetType: this.filters.targetType || null,
            showReported: this.filters.showReported,
            showDeleted: this.filters.showDeleted,
            search: this.searchQuery
          })

          if (result.success) {
            this.comments = result.data.comments || []
            this.pagination = result.data.pagination || this.pagination
          } else {
            this.showToast(result.error || 'Failed to load comments', 'error')
            this.comments = []
          }
        } catch (error) {
          console.error('Error loading comments:', error)
          this.showToast('Failed to load comments', 'error')
          this.comments = []
        } finally {
          this.loading = false
        }
      },

      debouncedSearch() {
        clearTimeout(this.searchTimeout)
        this.searchTimeout = setTimeout(() => {
          this.pagination.page = 1
          this.loadComments()
        }, 500)
      },

      async deleteComment(comment) {
        const message = comment.isDeleted
          ? 'This comment is already soft-deleted. Do you want to view permanent deletion options?'
          : `Are you sure you want to soft-delete this comment by ${comment.userName}? The comment will be marked as deleted but can be restored.`

        if (!confirm(message)) return

        try {
          const result = await commentsService.deleteCommentAsAdmin(comment.id, 'Deleted by administrator')
          if (result.success) {
            this.showToast('Comment soft-deleted successfully', 'success')
            await this.loadComments()
            await this.loadStats()
            this.closeCommentModal()
          } else {
            this.showToast(result.error, 'error')
          }
        } catch (error) {
          console.error('Error deleting comment:', error)
          this.showToast('Failed to delete comment', 'error')
        }
      },

      async restoreComment(comment) {
        if (!confirm(`Are you sure you want to restore this comment by ${comment.userName}?`)) return

        try {
          const result = await commentsService.restoreCommentAsAdmin(comment.id)
          if (result.success) {
            this.showToast('Comment restored successfully', 'success')
            await this.loadComments()
            await this.loadStats()
            this.closeCommentModal()
          } else {
            this.showToast(result.error, 'error')
          }
        } catch (error) {
          console.error('Error restoring comment:', error)
          this.showToast('Failed to restore comment', 'error')
        }
      },

      async permanentlyDeleteComment(comment) {
        const confirmed = confirm(
          `⚠️ WARNING: This will PERMANENTLY delete this comment. This action cannot be undone!\n\n` +
          `Comment by: ${comment.userName}\n` +
          `Content: "${comment.content.substring(0, 100)}..."\n\n` +
          `Are you absolutely sure?`
        )

        if (!confirmed) return

        try {
          const result = await commentsService.permanentlyDeleteComment(comment.id)
          if (result.success) {
            this.showToast('Comment permanently deleted', 'success')
            await this.loadComments()
            await this.loadStats()
            this.closeCommentModal()
          } else {
            this.showToast(result.error, 'error')
          }
        } catch (error) {
          console.error('Error permanently deleting comment:', error)
          this.showToast('Failed to permanently delete comment', 'error')
        }
      },

      async bulkDeleteComments() {
        if (this.selectedComments.length === 0) return

        const message = `Are you sure you want to soft-delete ${this.selectedComments.length} selected comment(s)?`
        if (!confirm(message)) return

        try {
          const result = await commentsService.bulkDeleteComments(this.selectedComments, 'Bulk deleted by administrator')
          if (result.success) {
            this.showToast(result.message, 'success')
            this.selectedComments = []
            await this.loadComments()
            await this.loadStats()
          } else {
            this.showToast(result.error, 'error')
          }
        } catch (error) {
          console.error('Error bulk deleting comments:', error)
          this.showToast('Failed to delete comments', 'error')
        }
      },

      toggleCommentSelection(commentId) {
        const index = this.selectedComments.indexOf(commentId)
        if (index > -1) {
          this.selectedComments.splice(index, 1)
        } else {
          this.selectedComments.push(commentId)
        }
      },

      toggleSelectAll() {
        if (this.allCommentsSelected) {
          this.selectedComments = []
        } else {
          this.selectedComments = this.comments.map(c => c.id)
        }
      },

      clearSelection() {
        this.selectedComments = []
      },

      viewComment(comment) {
        this.selectedComment = comment
      },

      closeCommentModal() {
        this.selectedComment = null
      },

      getTargetTypeName(comment) {
        if (comment.titleId) return 'Title'
        if (comment.chapterId) return 'Chapter'
        if (comment.chapterImageId) return 'Image'
        return 'Unknown'
      },

      formatDate(dateString) {
        if (!dateString) return 'N/A'
        return new Date(dateString).toLocaleDateString('en-US', {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit'
        })
      },

      formatTimeAgo(dateString) {
        if (!dateString) return 'N/A'
        return commentsService.formatCommentDate(dateString)
      },

      goToPage(page) {
        if (page >= 1 && page <= this.pagination.totalPages) {
          this.pagination.page = page
          this.loadComments()
        }
      },

      getVisiblePages() {
        const current = this.pagination.page
        const total = this.pagination.totalPages
        const delta = 2
        const range = []
        const rangeWithDots = []

        for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
          range.push(i)
        }

        if (current - delta > 2) {
          rangeWithDots.push(1, '...')
        } else {
          rangeWithDots.push(1)
        }

        rangeWithDots.push(...range)

        if (current + delta < total - 1) {
          rangeWithDots.push('...', total)
        } else if (total > 1) {
          rangeWithDots.push(total)
        }

        return rangeWithDots
      },

      showToast(message, type = 'info') {
        let toastContainer = document.getElementById('toast-container')
        if (!toastContainer) {
          toastContainer = document.createElement('div')
          toastContainer.id = 'toast-container'
          toastContainer.className = 'fixed bottom-4 right-4 z-50 space-y-2'
          document.body.appendChild(toastContainer)
        }

        const toast = document.createElement('div')
        const bgColor = type === 'success' ? 'bg-green-500' : type === 'error' ? 'bg-red-500' : 'bg-blue-500'

        toast.className = `${bgColor} text-white px-6 py-4 rounded-lg shadow-lg max-w-sm transform transition-all duration-300 translate-x-full opacity-0`
        toast.textContent = message

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
      }
    }
  }
</script>
