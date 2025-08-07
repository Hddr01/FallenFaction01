<template>
  <div class="bookmark-container" :data-title-id="titleId">
    <!-- Single combined button -->
    <button class="btn btn-outline-secondary btn-zor is-hfa variant-b3o bookmark-btn"
            type="button"
            @click="toggleDropdown"
            :disabled="loading">
      <i class="fas fa-plus fa-fw fa-sm" v-if="!loading && !isBookmarked"></i>
      <i class="fas fa-bookmark fa-fw fa-sm" v-else-if="!loading && isBookmarked"></i>
      <i class="fas fa-spinner fa-spin fa-fw fa-sm" v-else></i>
      <span class="bookmark-text">{{ bookmarkStatus }}</span>
      <i class="fas fa-chevron-down fa-sm dropdown-arrow" :class="{ 'rotated': showDropdown }"></i>
    </button>

    <!-- Dropdown menu -->
    <div class="bookmark-dropdown"
         :class="{ 'show': showDropdown }"
         ref="dropdown">
      <template v-if="loading">
        <div class="bookmark-item">
          <i class="fas fa-spinner fa-spin"></i>
          <span>Loading...</span>
        </div>
      </template>

      <template v-else-if="lastError">
        <div class="bookmark-item error-item">
          <i class="fas fa-exclamation-triangle"></i>
          <span>{{ lastError }}</span>
        </div>
        <div class="bookmark-item" @click="loadFolders">
          <i class="fas fa-redo"></i>
          <span>Retry</span>
        </div>
      </template>

      <template v-else>
        <!-- Show remove option if bookmarked -->
        <div v-if="isBookmarked" class="bookmark-item remove-item" @click="removeBookmark">
          <i class="fas fa-trash"></i>
          <span>Remove from {{ currentFolderName }}</span>
        </div>

        <!-- Show divider if bookmarked -->
        <div v-if="isBookmarked && folderOptions.length > 0" class="bookmark-divider"></div>

        <!-- Show all folders -->
        <div class="bookmark-item"
             v-for="folder in folderOptions"
             :key="folder.id"
             :class="{ 'current-folder': isBookmarked && folder.id === currentBookmark?.folderId }"
             @click="moveToFolder(folder.id)">
          <i class="fas fa-bookmark"></i>
          <span>{{ folder.name }}</span>
          <i v-if="isBookmarked && folder.id === currentBookmark?.folderId" class="fas fa-check current-check"></i>
        </div>

        <!-- Empty state -->
        <div v-if="folderOptions.length === 0" class="bookmark-item disabled">
          <i class="fas fa-folder"></i>
          <span>No folders available</span>
        </div>
      </template>
    </div>
  </div>
</template>

// BookmarkDropdown.vue - Updated to use Axios-based ApiClient
<script>
  export default {
    name: 'BookmarkDropdown',
    props: {
      titleId: {
        type: [Number, String],
        required: true
      }
    },
    emits: ['bookmark-changed', 'bookmark-loaded'],
    inject: ['apiClient'], // Inject the apiClient from App.vue
    data() {
      return {
        folderOptions: [],
        folders: [],
        currentBookmark: null,
        showDropdown: false,
        loading: true,
        lastError: null
      }
    },
    computed: {
      bookmarkStatus() {
        if (this.loading) return 'Loading...'
        if (this.isBookmarked) {
          return this.currentFolderName
        }
        return 'Add to Bookmarks'
      },
      isBookmarked() {
        return this.currentBookmark !== null
      },
      currentFolderName() {
        if (!this.currentBookmark) return ''
        const folder = this.folderOptions.find(f => f.id === this.currentBookmark.folderId)
        return folder ? folder.name : 'Bookmarked'
      }
    },
    async mounted() {
      await this.loadFolders()
      document.addEventListener('click', this.handleClickOutside)
    },
    beforeUnmount() {
      document.removeEventListener('click', this.handleClickOutside)
    },
    methods: {
      toggleDropdown() {
        this.showDropdown = !this.showDropdown
      },
      closeDropdown() {
        this.showDropdown = false
      },
      handleClickOutside(event) {
        if (this.showDropdown && this.$refs.dropdown && !this.$el.contains(event.target)) {
          this.closeDropdown()
        }
      },

      // UPDATED: Use axios-based apiClient instead of safeFetch
      async loadFolders() {
        this.loading = true
        this.lastError = null

        try {
          console.log('Loading bookmark folders for title:', this.titleId)

          // Use the injected apiClient - much cleaner!
          const response = await this.apiClient.get(`/Bookmarks/GetFolders?titleId=${this.titleId}`)

          // Axios automatically handles JSON parsing
          const data = response.data

          this.folders = data.folders || []
          this.folderOptions = this.folders.map(f => ({ id: f.id, name: f.name }))
          this.currentBookmark = data.currentBookmark

          // Update reading progress if we have a bookmark
          if (this.currentBookmark) {
            const progressElement = document.querySelector('.py_-7y7')
            if (progressElement) {
              const chapterCount = document.querySelectorAll('.chapter-row').length || 0
              progressElement.textContent = `${this.currentBookmark.lastReadChapter} / ${chapterCount}`
            }
          }

          // Force reactivity update
          this.$forceUpdate()

          // Emit event to notify parent component
          this.$emit('bookmark-loaded', {
            isBookmarked: this.isBookmarked,
            currentBookmark: this.currentBookmark,
            folderName: this.currentFolderName
          })

        } catch (error) {
          console.error('Error loading bookmark folders:', error)

          // Handle different error types
          if (error.response?.status === 401 || error.response?.status === 403) {
            this.lastError = 'Please log in to bookmark titles'
          } else {
            this.lastError = error.response?.data?.message || error.message || 'Failed to load bookmark folders'
          }

          this.showErrorToast(this.lastError)
        } finally {
          this.loading = false
        }
      },

      async moveToFolder(folderId) {
        if (this.loading) return

        // If already in this folder, do nothing
        if (this.isBookmarked && this.currentBookmark.folderId === folderId) {
          this.closeDropdown()
          return
        }

        this.loading = true
        this.lastError = null

        try {
          // Remove from current folder if already bookmarked
          if (this.isBookmarked) {
            await this.apiClient.post('/Bookmarks/RemoveBookmark', {
              bookmarkId: this.currentBookmark.id
            })
          }

          // Add to new folder
          await this.apiClient.post('/Bookmarks/AddBookmark', {
            titleId: this.titleId,
            folderId: folderId
          })

          // Reload data to get updated state
          await this.loadFolders()
          this.closeDropdown()

          const folderName = this.folderOptions.find(f => f.id === folderId)?.name || 'folder'
          this.showSuccessToast(`Moved to ${folderName}`)

          // Emit event for bookmark statistics update
          this.$emit('bookmark-changed', {
            action: this.isBookmarked ? 'moved' : 'added',
            folderId: folderId,
            folderName: folderName
          })

        } catch (error) {
          console.error('Error moving bookmark:', error)
          this.lastError = error.response?.data?.message || error.message || 'Failed to move bookmark'
          this.showErrorToast(this.lastError)
        } finally {
          this.loading = false
        }
      },

      async removeBookmark() {
        if (!this.currentBookmark || this.loading) return

        this.loading = true
        this.lastError = null

        try {
          await this.apiClient.post('/Bookmarks/RemoveBookmark', {
            bookmarkId: this.currentBookmark.id
          })

          const oldBookmark = this.currentBookmark
          const oldFolderName = this.currentFolderName

          // Reset bookmark state
          this.currentBookmark = null
          this.closeDropdown()
          this.$forceUpdate()

          this.showSuccessToast(`Removed from ${oldFolderName}`)

          // Emit event for bookmark statistics update
          this.$emit('bookmark-changed', {
            action: 'removed',
            folderId: oldBookmark.folderId,
            folderName: oldFolderName
          })

        } catch (error) {
          console.error('Error removing bookmark:', error)
          this.lastError = error.response?.data?.message || error.message || 'Failed to remove bookmark'
          this.showErrorToast(this.lastError)
        } finally {
          this.loading = false
        }
      },

      // Keep existing toast methods
      showSuccessToast(message) {
        this.showToast(message, 'success')
      },
      showErrorToast(message) {
        this.showToast(message, 'error')
      },
      showToast(message, type = 'info') {
        // Your existing toast implementation
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

        toastContainer.appendChild(toast)

        setTimeout(() => {
          if (toast.parentNode) {
            toast.remove()
          }
          if (toastContainer.children.length === 0) {
            toastContainer.remove()
          }
        }, 3000)

        setTimeout(() => {
          toast.style.opacity = '1'
        }, 100)
      }
    }
  }
</script>

<style scoped>
  .bookmark-container {
    position: relative;
    display: inline-block;
  }

  .bookmark-btn {
    display: flex;
    align-items: center;
    gap: 8px;
    width: 100%;
    min-width: 260px;
    justify-content: space-between;
    padding: 8px 12px;
  }

  .bookmark-text {
    flex-grow: 1;
    text-align: left;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .dropdown-arrow {
    transition: transform 0.2s ease;
    flex-shrink: 0;
  }

    .dropdown-arrow.rotated {
      transform: rotate(180deg);
    }

  .bookmark-dropdown {
    display: none;
    position: absolute;
    top: 100%;
    right: 0;
    margin-top: 5px;
    background-color: #1c1c1c;
    min-width: 200px;
    border-radius: 5px;
    border: 1px solid #333;
    overflow: hidden;
    z-index: 1000;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.3);
  }

    .bookmark-dropdown.show {
      display: block;
    }

  .bookmark-item {
    padding: 12px 15px;
    color: white;
    display: flex;
    align-items: center;
    gap: 10px;
    cursor: pointer;
    text-decoration: none;
    transition: background-color 0.2s ease;
    position: relative;
  }

    .bookmark-item:hover:not(.disabled):not(.error-item):not(.current-folder) {
      background-color: #2a2a2a;
    }

    .bookmark-item.current-folder {
      background-color: rgba(156, 39, 176, 0.2);
      color: #ba68c8;
      cursor: default;
    }

      .bookmark-item.current-folder:hover {
        background-color: rgba(156, 39, 176, 0.3);
      }

    .bookmark-item.remove-item {
      color: #ff6b6b;
    }

      .bookmark-item.remove-item:hover {
        background-color: rgba(255, 107, 107, 0.1);
      }

    .bookmark-item.disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .bookmark-item.error-item {
      color: #f44336;
      cursor: default;
    }

      .bookmark-item.error-item:hover {
        background-color: rgba(244, 67, 54, 0.1);
      }

    .bookmark-item i {
      font-size: 16px;
      width: 20px;
      text-align: center;
      flex-shrink: 0;
    }

  .current-check {
    margin-left: auto;
    color: #4caf50;
  }

  .bookmark-divider {
    height: 1px;
    background-color: #333;
    margin: 5px 0;
  }

  /* Mobile adjustments */
  @media (max-width: 768px) {
    .bookmark-dropdown {
      position: fixed;
      left: 0;
      right: 0;
      width: 100%;
      max-width: 100%;
      bottom: 0;
      top: auto;
      border-radius: 15px 15px 0 0;
      margin-top: 0;
    }

    .bookmark-item {
      padding: 16px 20px;
      font-size: 16px;
    }

    .bookmark-dropdown::before {
      content: '';
      display: block;
      width: 36px;
      height: 4px;
      background-color: #555;
      border-radius: 2px;
      margin: 8px auto;
    }

    .bookmark-btn {
      min-width: 200px;
      width: 100%;
    }
  }
</style>
