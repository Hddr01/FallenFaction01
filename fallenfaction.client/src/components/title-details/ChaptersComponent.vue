<template>
  <div class="chapters-container">
    <div class="chapters-header">
      <div class="chapters-sort">
        <span class="chapters-sort-label">Sort by:</span>
        <div class="chapters-sort-options">
          <button class="btn-zor variant-b3o is-hfa size-o8d"
                  :class="{ active: sortBy === 'newest' }"
                  @click="setSortBy('newest')">
            Newest
          </button>
          <button class="btn-zor variant-b3o is-hfa size-o8d"
                  :class="{ active: sortBy === 'oldest' }"
                  @click="setSortBy('oldest')">
            Oldest
          </button>
        </div>
      </div>
      <div class="chapters-filter">
        <input type="text"
               placeholder="Search chapters..."
               class="chapters-search"
               v-model="searchQuery"
               @input="filterChapters" />
      </div>
    </div>

    <div class="chapters-list">
      <template v-if="filteredChapters.length > 0">
        <div class="chapters-table">
          <div class="chapters-table-header">
            <div class="chapter-number">№</div>
            <div class="chapter-name">Name</div>
            <div class="chapter-team">Team</div>
            <div class="chapter-date">Date</div>
          </div>
          <div class="chapters-table-body">
            <a v-for="chapter in paginatedChapters"
               :key="chapter.id"
               :href="getChapterUrl(chapter)"
               class="chapter-row">
              <div class="chapter-number">
                <span class="volume-badge">Vol. {{ chapter.volumeNumber }}</span>
              </div>
              <div class="chapter-name">{{ chapter.name }}</div>
              <div class="chapter-team">
                <span v-if="chapter.team">{{ chapter.team.name }}</span>
                <span v-else>Unknown</span>
              </div>
              <div class="chapter-date">{{ formatDate(chapter.createdDate) }}</div>
            </a>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="chapters-pagination">
          <button class="btn btn-outline-secondary btn-sm"
                  :disabled="currentPage === 1"
                  @click="goToPage(currentPage - 1)">
            <i class="fas fa-chevron-left"></i>
          </button>

          <span class="pagination-info">
            Page {{ currentPage }} of {{ totalPages }}
          </span>

          <button class="btn btn-outline-secondary btn-sm"
                  :disabled="currentPage === totalPages"
                  @click="goToPage(currentPage + 1)">
            <i class="fas fa-chevron-right"></i>
          </button>
        </div>
      </template>

      <div v-else-if="searchQuery && chapters.length > 0" class="empty-chapters">
        <div class="empty-icon">
          <i class="fas fa-search"></i>
        </div>
        <div class="empty-text">No chapters found matching "{{ searchQuery }}"</div>
        <button class="btn btn-outline-secondary btn-sm mt-2" @click="clearSearch">
          Clear Search
        </button>
      </div>

      <div v-else class="empty-chapters">
        <div class="empty-icon">
          <i class="fas fa-book"></i>
        </div>
        <div class="empty-text">No chapters available yet.</div>
      </div>
    </div>
  </div>
</template>

<script>
  export default {
    name: 'ChaptersComponent',
    props: {
      chapters: {
        type: Array,
        default: () => []
      },
      titleName: {
        type: String,
        required: true
      },
      chaptersPerPage: {
        type: Number,
        default: 20
      }
    },
    data() {
      return {
        sortBy: 'newest',
        searchQuery: '',
        filteredChapters: [],
        currentPage: 1
      }
    },
    computed: {
      sortedChapters() {
        const chapters = [...this.filteredChapters]

        if (this.sortBy === 'newest') {
          return chapters.sort((a, b) => {
            // Compare volume numbers first (descending)
            const volumeCompare = b.volumeNumber - a.volumeNumber
            if (volumeCompare !== 0) return volumeCompare

            // If volumes are equal, compare chapter numbers (descending)
            return b.chapterNumber - a.chapterNumber
          })
        } else {
          return chapters.sort((a, b) => {
            // Compare volume numbers first (ascending)
            const volumeCompare = a.volumeNumber - b.volumeNumber
            if (volumeCompare !== 0) return volumeCompare

            // If volumes are equal, compare chapter numbers (ascending)
            return a.chapterNumber - b.chapterNumber
          })
        }
      },

      totalPages() {
        return Math.ceil(this.sortedChapters.length / this.chaptersPerPage)
      },

      paginatedChapters() {
        const start = (this.currentPage - 1) * this.chaptersPerPage
        const end = start + this.chaptersPerPage
        return this.sortedChapters.slice(start, end)
      }
    },
    watch: {
      chapters: {
        handler(newChapters) {
          this.filteredChapters = [...newChapters]
          this.currentPage = 1
        },
        immediate: true
      },

      sortBy() {
        this.currentPage = 1
      }
    },
    methods: {
      setSortBy(sortType) {
        this.sortBy = sortType
      },

      filterChapters() {
        if (!this.searchQuery.trim()) {
          this.filteredChapters = [...this.chapters]
        } else {
          const query = this.searchQuery.toLowerCase()
          this.filteredChapters = this.chapters.filter(chapter =>
            chapter.name.toLowerCase().includes(query) ||
            chapter.chapterNumber.toString().includes(query) ||
            chapter.volumeNumber.toString().includes(query) ||
            (chapter.team && chapter.team.name.toLowerCase().includes(query))
          )
        }
        this.currentPage = 1
      },

      clearSearch() {
        this.searchQuery = ''
        this.filteredChapters = [...this.chapters]
        this.currentPage = 1
      },

      goToPage(page) {
        if (page >= 1 && page <= this.totalPages) {
          this.currentPage = page
        }
      },

      getChapterUrl(chapter) {
        return `/${this.titleName}/chapter/${chapter.name}/v${chapter.volumeNumber}/t${chapter.teamId || 0}`
      },

      formatDate(dateString) {
        const date = new Date(dateString)
        return date.toLocaleDateString('en-US', {
          year: 'numeric',
          month: 'short',
          day: 'numeric'
        })
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
