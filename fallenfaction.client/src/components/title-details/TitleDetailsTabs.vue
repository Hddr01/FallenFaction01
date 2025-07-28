<template>
    <div class="content-z51 pap-473">
        <!-- FIXED: Horizontal Tab Navigation -->
        <div class="tabs-k5y _bo-2yz">
            <div class="menu-7a9">
                <a v-for="tab in tabs"
                   :key="tab.key"
                   class="item-qx3 fa-ic3"
                   :class="{ 'is-chy': activeTab === tab.key }"
                   href="#"
                   @click.prevent="switchTab(tab.key)">
                    <div class="item-9y5">
                        {{ tab.title }}
                        <i v-if="loading && activeTab === tab.key" class="fas fa-spinner fa-spin fa-sm ml-2"></i>
                    </div>
                </a>
            </div>
        </div>

        <!-- Tab Content -->
        <div class="section-pfr">
            <!-- About Title Tab -->
            <div v-show="activeTab === 'info'" class="tab-content-panel">
                <template v-if="tabData.info.loaded">
                    <!-- Description -->
                    <div class="t6_-pzq">
                        <div class="text-wqk col-l5f style-W8hJO" id="style-W8hJO">
                            <div class="t6_-5dv" :class="{ 'expanded': descriptionExpanded }">
                                {{ titleData.description }}
                            </div>
                        </div>
                        <button v-if="titleData.description && titleData.description.length > 300"
                                class="btn-zor link-qky variant-7q9 button-t3d"
                                type="button"
                                @click="toggleDescription">
                            {{ descriptionExpanded ? 'Read less...' : 'Read more...' }}
                        </button>
                    </div>

                    <!-- Tags/Categories -->
                    <div class="xn_-5zv t5_-lfm">
                        <a v-if="titleData.ageRestriction > 0"
                           :href="`/catalog?ageRestriction=${titleData.ageRestriction}`"
                           class="c1_-wkm c1_-me4 c1_-k2o"
                           data-type="restriction">
                            <span>{{ titleData.ageRestriction }}+</span>
                        </a>

                        <a v-for="category in titleData.categories"
                           :key="category.id"
                           :href="`/catalog?category=${category.id}`"
                           class="c1_-wkm c1_-me4 c1_-c5l"
                           data-type="genre">
                            <span>{{ category.name }}</span>
                        </a>

                        <a v-for="(tag, index) in visibleTags"
                           :key="tag.id"
                           :href="`/catalog?tag=${tag.id}`"
                           class="c1_-wkm c1_-me4 c1_-c5l"
                           data-type="tag">
                            <span>{{ tag.name }}</span>
                        </a>

                        <div v-if="titleData.tags && titleData.tags.length > 10"
                             class="c1_-wkm c1_-me4 c1_-c5l"
                             @click="showAllTags = !showAllTags"
                             style="cursor: pointer;">
                            <span>{{ showAllTags ? 'Show less' : `+${titleData.tags.length - 10} more` }}</span>
                        </div>
                    </div>

                    <!-- Translators Section - NOW INSIDE INFO TAB -->
                    <div v-if="titleData.teams && titleData.teams.length > 0" class="section-pfr pr_-o6a" style="margin-top: 30px;">
                        <div>
                            <div class="section-6th size-o8d">Translators</div>
                        </div>
                        <div class="cu_-vhr cu_-nvs" data-scroll-container="">
                            <div class="cu_-n68 cu_-4dv cu_-aog cu_-7s8"
                                 @click="scrollTranslators(-200)"
                                 :style="{ display: showLeftTranslatorButton ? 'flex' : 'none' }">
                                <div class="cu_-rij"><i class="fas fa-chevron-left"></i></div>
                            </div>
                            <div class="cu_-n68 cu_-83k cu_-aog cu_-7s8"
                                 @click="scrollTranslators(200)"
                                 :style="{ display: showRightTranslatorButton ? 'flex' : 'none' }">
                                <div class="cu_-rij"><i class="fas fa-chevron-right"></i></div>
                            </div>
                            <div class="pr_-j5q cu_-s9p"
                                 data-scroll-content=""
                                 ref="translatorsContainer"
                                 @scroll="checkTranslatorButtons">
                                <a v-for="team in titleData.teams"
                                   :key="team.id"
                                   :href="`/team/${team.id}`"
                                   class="item-nja size-v8y sha-mdq">
                                    <div class="item-3im">
                                        <span class="cov-oga item-lnl">
                                            <div class="cov-oj9 _ratio-qka">
                                                <img src="/img/logo.png"
                                                     class="cov-6id _lo-m2q"
                                                     :alt="team.name">
                                            </div>
                                        </span>
                                    </div>
                                    <div class="item-2ke">
                                        <div class="item-y18"><span>{{ team.name }}</span></div>
                                    </div>
                                </a>
                            </div>
                        </div>
                    </div>

                    <!-- Statistics Section - NOW INSIDE INFO TAB -->
                    <div class="xq_-rz4" style="margin-top: 30px;">
                        <!-- Ratings Section -->
                        <div class="section-pfr pr_-o6a" data-stats="rating">
                            <div>
                                <div class="section-6th size-o8d">
                                    <span>User Ratings</span>
                                    <div class="rat-dtk _ml-ext">
                                        <div class="info-4wf">
                                            <i class="fas fa-star rating-info__star"></i>
                                            <span class="info-csc" id="stats-rating-value">{{ ratingStats.averageRating.toFixed(2) }}</span>
                                            <span class="info-mj1" id="stats-rating-count">({{ ratingStats.totalRatings }})</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="ac5-97d" id="rating-stats">
                                <template v-if="ratingStats.totalRatings > 0">
                                    <div v-for="stat in ratingStats.distribution" :key="stat.value" class="ac5-cfp">
                                        <div class="ac5-ine">
                                            <div class="ac5-khr">
                                                <span>{{ stat.value }}</span>
                                                <i class="fas fa-star"></i>
                                            </div>
                                        </div>
                                        <div class="ac5-ine ac5-eo9">
                                            <div class="progress-g8a ac5-mga" :data-stats-id="stat.value">
                                                <div class="progress-dwj _ho-2oh" :style="`width: ${stat.percentage}%`"></div>
                                            </div>
                                        </div>
                                        <div class="ac5-ine ac5-k9y">{{ stat.percentage.toFixed(1) }}%</div>
                                        <div class="ac5-ine ac5-o5z">{{ stat.count }}</div>
                                    </div>
                                </template>
                                <div v-else class="empty-stats">No ratings yet</div>
                            </div>
                        </div>

                        <!-- Bookmarks Section -->
                        <div class="section-pfr pr_-o6a" data-stats="bookmarks">
                            <div>
                                <div class="section-6th size-o8d">
                                    In Lists: <span id="bookmark-count">{{ bookmarkStats.totalBookmarks }}</span> people
                                </div>
                            </div>
                            <div class="ac5-97d" id="bookmark-stats">
                                <template v-if="bookmarkStats.folderDistribution && bookmarkStats.folderDistribution.length > 0">
                                    <div v-for="stat in bookmarkStats.folderDistribution" :key="stat.folderName" class="ac5-cfp">
                                        <div class="ac5-ine">
                                            <div class="ac5-khr">{{ stat.folderName }}</div>
                                        </div>
                                        <div class="ac5-ine ac5-eo9">
                                            <div class="progress-g8a ac5-mga" :data-stats-id="stat.folderName.toLowerCase().replace(/\s+/g, '-')">
                                                <div class="progress-dwj _ho-2oh" :style="`width: ${stat.percentage}%`"></div>
                                            </div>
                                        </div>
                                        <div class="ac5-ine ac5-k9y">{{ stat.percentage.toFixed(1) }}%</div>
                                        <div class="ac5-ine ac5-o5z">{{ stat.count }}</div>
                                    </div>
                                </template>
                                <div v-else class="empty-stats">No bookmarks yet</div>
                            </div>
                        </div>
                    </div>
                </template>

                <div v-else-if="tabData.info.error" class="error-state">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>{{ tabData.info.error }}</p>
                    <button @click="loadTabContent('info')" class="btn btn-outline-secondary">Retry</button>
                </div>

                <div v-else class="loading-state">
                    <i class="fas fa-spinner fa-spin"></i>
                    <p>Loading title information...</p>
                </div>
            </div>

            <!-- Chapters Tab -->
            <div v-show="activeTab === 'chapters'" class="tab-content-panel">
                <template v-if="tabData.chapters.loaded">
                    <div v-if="!titleData.areChapterCommentsEnabled" class="alert alert-warning mb-3">
                        <i class="fas fa-comment-slash"></i>
                        Comments have been disabled for chapters of this title.
                    </div>

                    <ChaptersComponent :chapters="tabData.chapters.data" :title-name="titleData.originalTitle" />
                </template>

                <div v-else-if="tabData.chapters.error" class="error-state">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>{{ tabData.chapters.error }}</p>
                    <button @click="loadTabContent('chapters')" class="btn btn-outline-secondary">Retry</button>
                </div>

                <div v-else class="loading-state">
                    <i class="fas fa-spinner fa-spin"></i>
                    <p>Loading chapters...</p>
                </div>
            </div>

            <!-- Comments Tab -->
            <div v-show="activeTab === 'comments'" class="tab-content-panel">
                <template v-if="tabData.comments.loaded">
                    <div v-if="!titleData.areCommentsEnabled" class="alert alert-warning mb-3">
                        <i class="fas fa-comment-slash"></i>
                        Comments have been disabled for this title.
                    </div>
                    <div v-else>
                        <CommentsComponent :comments="tabData.comments.data"
                                           :target-id="titleId"
                                           target-type="1"
                                           @comments-updated="onCommentsUpdated" />
                    </div>
                </template>

                <div v-else-if="tabData.comments.error" class="error-state">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>{{ tabData.comments.error }}</p>
                    <button @click="loadTabContent('comments')" class="btn btn-outline-secondary">Retry</button>
                </div>

                <div v-else class="loading-state">
                    <i class="fas fa-spinner fa-spin"></i>
                    <p>Loading comments...</p>
                </div>
            </div>

            <!-- Reviews Tab -->
            <div v-show="activeTab === 'reviews'" class="tab-content-panel">
                <div class="coming-soon">
                    <i class="fas fa-star"></i>
                    <h3>Reviews Coming Soon</h3>
                    <p>User reviews and ratings will be available in a future update.</p>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
    // FIXED: Corrected import paths - remove the extra './title-details/' since we're already in that folder
    import ChaptersComponent from './ChaptersComponent.vue'
    import CommentsComponent from './CommentsComponent.vue'

    export default {
        name: 'TitleDetailsTabs',
        components: {
            ChaptersComponent,
            CommentsComponent
        },
        props: {
            titleId: {
                type: [Number, String],
                required: true
            },
            titleData: {
                type: Object,
                required: true
            },
            initialTab: {
                type: String,
                default: 'info'
            }
        },
        emits: ['tab-changed'],
        data() {
            return {
                activeTab: this.initialTab,
                loading: false,
                descriptionExpanded: false,
                showAllTags: false,
                tabs: [
                    { key: 'info', title: 'About Title' },
                    { key: 'chapters', title: 'Chapters' },
                    { key: 'comments', title: 'Comments' },
                    { key: 'reviews', title: 'Reviews' }
                ],
                tabData: {
                    info: { loaded: false, data: null, error: null },
                    chapters: { loaded: false, data: [], error: null },
                    comments: { loaded: false, data: [], error: null },
                    reviews: { loaded: false, data: [], error: null }
                },
                // NEW: Add data for ratings and bookmarks statistics
                ratingStats: {
                    averageRating: 0,
                    totalRatings: 0,
                    distribution: []
                },
                bookmarkStats: {
                    totalBookmarks: 0,
                    folderDistribution: []
                },
                // NEW: Add data for translator scroll buttons
                showLeftTranslatorButton: false,
                showRightTranslatorButton: false
            }
        },
        computed: {
            visibleTags() {
                if (!this.titleData.tags) return []
                return this.showAllTags ? this.titleData.tags : this.titleData.tags.slice(0, 10)
            }
        },
        watch: {
            titleData: {
                handler() {
                    this.$nextTick(() => {
                        this.checkTranslatorButtons()
                    })
                },
                deep: true
            }
        },
        async mounted() {
            // Load the initial tab content
            await this.loadTabContent(this.activeTab)

            // Update URL without page reload
            this.updateURL()

            // Load statistics data
            await this.loadRatingStats()
            await this.loadBookmarkStats()

            // Listen for bookmark changes to refresh stats
            document.addEventListener('bookmark-stats-refresh', this.handleBookmarkStatsRefresh)
            document.addEventListener('rating-stats-refresh', this.handleRatingStatsRefresh)

            // Check translator scroll buttons after DOM is updated
            this.$nextTick(() => {
                this.checkTranslatorButtons()
            })
        },

        beforeUnmount() {
            // Clean up event listeners
            document.removeEventListener('bookmark-stats-refresh', this.handleBookmarkStatsRefresh)
            document.removeEventListener('rating-stats-refresh', this.handleRatingStatsRefresh)
        },
        methods: {
            async switchTab(tabKey) {
                if (this.activeTab === tabKey) return

                this.activeTab = tabKey
                this.updateURL()
                this.$emit('tab-changed', tabKey)

                // Load content if not already loaded
                if (!this.tabData[tabKey].loaded) {
                    await this.loadTabContent(tabKey)
                }
            },

            async loadTabContent(tabKey) {
                this.loading = true
                this.tabData[tabKey].error = null

                try {
                    switch (tabKey) {
                        case 'info':
                            await this.loadInfoTab()
                            break
                        case 'chapters':
                            await this.loadChaptersTab()
                            break
                        case 'comments':
                            await this.loadCommentsTab()
                            break
                        case 'reviews':
                            await this.loadReviewsTab()
                            break
                    }
                } catch (error) {
                    console.error(`Error loading ${tabKey} tab:`, error)
                    this.tabData[tabKey].error = `Failed to load ${tabKey}. Please try again.`
                }

                this.loading = false
            },

            async loadInfoTab() {
                // Info tab data comes from props, so just mark as loaded
                this.tabData.info.loaded = true
                this.tabData.info.data = this.titleData
            },

            async loadChaptersTab() {
                const response = await this.safeFetch(`/api/Titles/GetChapters?titleId=${this.titleId}`)
                if (response.success === false) {
                    throw new Error(response.error)
                }

                this.tabData.chapters.data = response
                this.tabData.chapters.loaded = true
            },

            async loadCommentsTab() {
                const response = await this.safeFetch(`/api/Comments/GetComments?targetId=${this.titleId}&targetType=1`)
                if (response.success === false) {
                    throw new Error(response.error)
                }

                this.tabData.comments.data = response
                this.tabData.comments.loaded = true
            },

            async loadReviewsTab() {
                // Placeholder for future reviews functionality
                this.tabData.reviews.loaded = true
                this.tabData.reviews.data = []
            },

            // NEW: Load rating statistics
            async loadRatingStats() {
                try {
                    const response = await this.safeFetch(`/api/Ratings/GetRatings?titleId=${this.titleId}`)
                    if (response.success !== false) {
                        this.ratingStats.averageRating = response.average || 0
                        this.ratingStats.totalRatings = response.total || 0
                        this.ratingStats.distribution = response.distribution || []
                    }
                } catch (error) {
                    console.error('Error loading rating stats:', error)
                }
            },

            // NEW: Load bookmark statistics
            async loadBookmarkStats() {
                try {
                    const response = await this.safeFetch(`/api/Bookmarks/GetBookmarkStats?titleId=${this.titleId}`)
                    if (response.success !== false) {
                        this.bookmarkStats.totalBookmarks = response.totalBookmarks || 0
                        this.bookmarkStats.folderDistribution = response.folderDistribution || []
                    }
                } catch (error) {
                    console.error('Error loading bookmark stats:', error)
                }
            },

            // NEW: Event handler for bookmark stats refresh
            handleBookmarkStatsRefresh(event) {
                if (event.detail && event.detail.titleId == this.titleId) {
                    this.loadBookmarkStats()
                }
            },

            // NEW: Event handler for rating stats refresh
            handleRatingStatsRefresh(event) {
                if (event.detail && event.detail.titleId == this.titleId) {
                    this.loadRatingStats()
                }
            },

            // NEW: Translator scroll methods
            scrollTranslators(amount) {
                const container = this.$refs.translatorsContainer
                if (container) {
                    container.scrollBy({ left: amount, behavior: 'smooth' })
                }
            },

            checkTranslatorButtons() {
                const container = this.$refs.translatorsContainer
                if (!container) return

                this.showLeftTranslatorButton = container.scrollLeft > 0
                this.showRightTranslatorButton = container.scrollLeft + container.clientWidth < container.scrollWidth
            },

            toggleDescription() {
                this.descriptionExpanded = !this.descriptionExpanded
            },

            updateURL() {
                const url = new URL(window.location)
                url.searchParams.set('section', this.activeTab)
                window.history.replaceState({}, '', url)
            },

            onCommentsUpdated(comments) {
                this.tabData.comments.data = comments
            },

            async safeFetch(url, options = {}) {
                try {
                    const response = await fetch(url, {
                        headers: {
                            'Content-Type': 'application/json',
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

                    const contentType = response.headers.get('content-type')
                    if (contentType && contentType.includes('application/json')) {
                        return await response.json()
                    }

                    return { success: false, error: 'Invalid response format' }
                } catch (error) {
                    console.error('Fetch error:', error)
                    return { success: false, error: error.message }
                }
            }
        }
    }
</script>

<style scoped>
    /* UPDATED: Enhanced tab content panel styling */
    .tab-content-panel {
        min-height: 200px;
        background-color: var(--foreground, #1a1a1a);
        border-radius: 0 0 8px 8px;
    }

    /* UPDATED: Enhanced loading and error states */
    .loading-state, .error-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 60px 20px;
        text-align: center;
        color: var(--text-secondary, #a0a0a5);
        background-color: var(--foreground, #1a1a1a);
        border-radius: 0 0 8px 8px;
    }

        .loading-state i, .error-state i {
            font-size: 2.5rem;
            margin-bottom: 1.5rem;
            opacity: 0.7;
        }

        .loading-state p, .error-state p {
            font-size: 1.1rem;
            margin: 0;
        }

    .error-state {
        color: var(--red, #f44336);
    }

        .error-state i {
            color: var(--red, #f44336);
        }

        .error-state button {
            margin-top: 1.5rem;
            background-color: var(--foreground, #1a1a1a);
            border: 1px solid var(--border-base, #3a3a3a);
            color: var(--text-primary, #e0e0e0);
            padding: 10px 20px;
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.3s ease;
        }

            .error-state button:hover {
                background-color: var(--background-elevated-2, #2a2a2a);
                border-color: var(--primary, #9c27b0);
            }

    /* UPDATED: Enhanced coming soon styling */
    .coming-soon {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 80px 20px;
        text-align: center;
        color: var(--text-secondary, #a0a0a5);
        background-color: var(--foreground, #1a1a1a);
        border-radius: 0 0 8px 8px;
    }

        .coming-soon i {
            font-size: 4rem;
            margin-bottom: 2rem;
            color: var(--primary, #9c27b0);
            opacity: 0.7;
        }

        .coming-soon h3 {
            margin-bottom: 1rem;
            color: var(--text-primary, #e0e0e0);
            font-size: 1.5rem;
            font-weight: 600;
        }

        .coming-soon p {
            font-size: 1rem;
            opacity: 0.8;
            max-width: 400px;
            line-height: 1.5;
        }

    /* UPDATED: Enhanced description expansion styles */
    .t6_-5dv {
        max-height: 4.5em;
        overflow: hidden;
        line-height: 1.6;
        transition: max-height 0.4s ease;
        color: var(--text-primary, #e0e0e0);
    }

        .t6_-5dv.expanded {
            max-height: none;
        }

    /* UPDATED: Enhanced tag styles */
    .c1_-wkm {
        cursor: pointer;
        transition: all 0.3s ease;
        background-color: rgba(156, 39, 176, 0.1);
        border-color: var(--primary, #9c27b0);
    }

        .c1_-wkm:hover {
            background-color: rgba(156, 39, 176, 0.2);
            transform: translateY(-1px);
            box-shadow: 0 2px 8px rgba(156, 39, 176, 0.3);
        }

    /* UPDATED: Enhanced alert styles */
    .alert {
        padding: 16px 20px;
        border-radius: 8px;
        display: flex;
        align-items: center;
        gap: 12px;
        margin-bottom: 20px;
        font-weight: 500;
    }

    .alert-warning {
        background-color: rgba(255, 193, 7, 0.1);
        border: 1px solid rgba(255, 193, 7, 0.3);
        color: #ffc107;
    }

        .alert-warning i {
            font-size: 1.2rem;
        }

    /* UPDATED: Horizontal tabs container styling with spacing */
    .tabs-k5y {
        border-bottom: 1px solid var(--tab-border, #3a3a3a);
        background-color: var(--tab-background, #1a1a1a);
        border-radius: 8px 8px 0 0;
        overflow: hidden;
        margin-bottom: 0;
    }

    .menu-7a9 {
        display: flex;
        width: 100%;
        overflow-x: auto;
        white-space: nowrap;
        background-color: var(--tab-background, #1a1a1a);
        padding: 8px;
        margin: 0;
        gap: 8px;
    }

        .menu-7a9::-webkit-scrollbar {
            display: none;
        }





    .item-qx3.fa-ic3 {
        background-color: transparent !important;
        box-shadow: none !important;
    }

    /* UPDATED: Individual tab styling with spacing */
    .item-qx3 {
        flex-shrink: 0;
        display: block;
        cursor: pointer;
        background-color: var(--tab-background, #1a1a1a);
        border-radius: 6px;
        transition: all 0.3s ease;
        text-decoration: none;
        color: var(--text-secondary, #a0a0a5);
        position: relative;
        overflow: hidden;
    }

        .item-qx3:hover {
            background-color: rgba(156, 39, 176, 0.1);
            color: var(--text-primary, #e0e0e0);
            text-decoration: none;
            transform: translateY(-1px);
            box-shadow: 0 2px 8px rgba(156, 39, 176, 0.2);
        }

        .item-qx3.is-chy {
            color: var(--primary, #9c27b0);
            background-color: rgba(156, 39, 176, 0.15);
            box-shadow: 0 2px 8px rgba(156, 39, 176, 0.3);
        }

    /* UPDATED: Enhanced item styling */
    .item-9y5 {
        padding: 16px 24px;
        position: relative;
        display: block;
        font-weight: 500;
        font-size: 14px;
    }

        .item-9y5::after {
            content: "";
            position: absolute;
            bottom: 0;
            left: 0;
            right: 0;
            height: 3px;
            background: var(--primary, #9c27b0);
            transition: transform 0.3s ease-out;
            transform: translateY(4px) scale(0);
            border-radius: 3px 3px 0 0;
        }

    .nav-link.active .item-9y5::after {
        transform: translateY(0) scale(1);
    }

    /* UPDATED: Enhanced statistics section styles */
    .xq_-rz4 {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 30px;
        margin-top: 30px;
        background-color: var(--foreground, #1a1a1a);
        padding: 20px;
        border-radius: 8px;
        border: 1px solid var(--border-base, #3a3a3a);
    }

    .empty-stats {
        text-align: center;
        color: var(--text-secondary, #a0a0a5);
        padding: 30px;
        font-style: italic;
        background-color: var(--background-elevated-2, #2a2a2a);
        border-radius: 6px;
        border: 1px dashed var(--border-base, #3a3a3a);
    }

    /* UPDATED: Enhanced progress bar styles */
    .progress-g8a {
        position: relative;
        height: 8px;
        border-radius: 8px;
        background: var(--background-fill-3, rgba(118, 118, 128, .2));
        color: var(--primary-lighten, #ba68c8);
        overflow: hidden;
        box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.2);
    }

    .progress-dwj {
        position: absolute;
        left: 0;
        border-radius: inherit;
        background: currentColor;
        transition: width 0.5s ease;
        height: 100%;
        top: 0;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
    }

    /* Responsive adjustments */
    @media (max-width: 768px) {
        .nav-tabs {
            overflow-x: auto;
            white-space: nowrap;
            -webkit-overflow-scrolling: touch;
            scrollbar-width: none;
            -ms-overflow-style: none;
        }

            .nav-tabs::-webkit-scrollbar {
                display: none;
            }

            .nav-tabs .nav-item {
                flex-shrink: 0;
                display: inline-block;
            }

        .tab-content-panel {
            padding: 0;
            margin: 0;
        }

        .xq_-rz4 {
            grid-template-columns: 1fr;
            gap: 20px;
            padding: 15px;
        }

        .item-9y5 {
            padding: 14px 20px;
            font-size: 13px;
        }

        .loading-state, .error-state, .coming-soon {
            padding: 40px 15px;
        }
    }

    @media (max-width: 480px) {
        .item-9y5 {
            padding: 12px 16px;
            font-size: 12px;
        }

        .xq_-rz4 {
            padding: 12px;
            gap: 15px;
        }
    }
</style>