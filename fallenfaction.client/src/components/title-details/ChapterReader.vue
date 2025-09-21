<template>
  <div class="chapter-container" :class="[currentTheme, readingDirection, { 'debug-mode': debugMode }]" id="chapterContainer">
    <!-- Manga Navbar -->
    <div class="manga-navbar" :class="{ 'hidden': !uiVisible }" id="mangaNavbar">
      <div class="navbar-content">
        <div class="navbar-left">
          <button class="back-button" @click="goToTitleDetails">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="m15 18-6-6 6-6" />
            </svg>
            <span class="back-text">Back to Chapters</span>
          </button>

          <div class="chapter-info">
            <h1 class="title-name" @click="goToTitleDetails">{{ chapterData?.titleName || 'Loading...' }}</h1>
            <div class="chapter-nav">
              <button class="chapter-nav-btn" @click="enhancedGotoPrevChapter" :disabled="!chapterData?.previousChapterId">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="m15 18-6-6 6-6" />
                </svg>
              </button>
              <h2 class="chapter-name">
                Vol.{{ chapterData?.volumeNumber }} Ch.{{ chapterData?.chapterNumber }}
                <span v-if="chapterData?.name">: {{ chapterData.name }}</span>
              </h2>
              <button class="chapter-nav-btn" @click="enhancedGotoNextChapter" :disabled="!chapterData?.nextChapterId">
                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="m9 18 6-6-6-6" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        <div class="navbar-right">
          <button class="settings-btn" @click="toggleSettings">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
              <circle cx="12" cy="12" r="3" />
            </svg>
          </button>

          <button class="chapter-list-btn" @click="toggleChapterList">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="8" y1="6" x2="21" y2="6" />
              <line x1="8" y1="12" x2="21" y2="12" />
              <line x1="8" y1="18" x2="21" y2="18" />
              <line x1="3" y1="6" x2="3.01" y2="6" />
              <line x1="3" y1="12" x2="3.01" y2="12" />
              <line x1="3" y1="18" x2="3.01" y2="18" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner">
        <svg class="animate-spin h-8 w-8 text-[var(--color-accent)]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span class="text-[var(--color-text)]">Loading chapter...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="error-container">
      <div class="error-content">
        <svg class="w-16 h-16 text-red-500 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
        </svg>
        <h3 class="text-xl font-semibold text-[var(--color-text)] mb-2">Chapter Not Found</h3>
        <p class="text-[var(--color-text)] opacity-75 mb-4">{{ error }}</p>
        <div class="flex space-x-3 justify-center">
          <button @click="retryLoad" class="px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
            Try Again
          </button>
          <button @click="goToTitleDetails" class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Back to Title
          </button>
        </div>
      </div>
    </div>

    <!-- Chapter Content -->
    <div v-else-if="chapterData && orderedImages.length > 0" class="chapter-content">
      <!-- Single Page View -->
      <!-- Single Page View -->
      <div v-if="viewMode === 'single'" class="single-page-view">
        <!-- Content Area -->
        <div class="content-area" ref="singlePageContainer">
          <div class="manga-image-container" ref="imageContainerRef">
            <img :src="getImageUrl(currentImage?.imagePath)"
                 :alt="`Page ${currentPage}`"
                 class="manga-image"
                 :style="imageStyles"
                 ref="currentImageRef"
                 @error="handleImageError"
                 @load="handleImageLoad" />

            <!-- Touch/Click Zones - Positioned on the image -->
            <div class="tap-zones-on-image" ref="tapZonesRef">
              <div class="tap-zone tap-zone-left"
                   @click="enhancedHandleTapZoneClick('left', enhancedGoToPrevPage)"
                   data-zone="Previous">
              </div>
              <div class="tap-zone tap-zone-center"
                   @click="enhancedHandleTapZoneClick('center', toggleUI)"
                   data-zone="Toggle UI">
              </div>
              <div class="tap-zone tap-zone-right"
                   @click="enhancedHandleTapZoneClick('right', enhancedGoToNextPage)"
                   data-zone="Next">
              </div>
            </div>
          </div>
        </div>

        <!-- Navigation Buttons for Single Page View (Always visible) -->
        <div v-if="viewMode === 'single'" class="single-page-navigation" :class="{ 'hidden': !uiVisible }">
          <div class="max-w-4xl mx-auto px-4 py-6">
            <!-- Desktop Navigation -->
            <div class="hidden md:flex justify-between items-center mb-6">
              <!-- Left Navigation Button -->
              <button @click="enhancedGoToPrevPage"
                      :disabled="currentPage === 1 && !chapterData?.previousChapterId"
                      class="flex items-center gap-2 px-4 py-3 bg-black/80 backdrop-blur-lg border border-white/20 rounded-xl text-white cursor-pointer transition-all duration-300 font-medium text-sm min-w-[140px] hover:bg-black/90 hover:border-[var(--color-accent)] hover:-translate-y-0.5 hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none">
                <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path>
                </svg>
                <span class="whitespace-nowrap">
                  {{ currentPage === 1 ? 'Prev Chapter' : 'Prev Page' }}
                </span>
              </button>

              <!-- Center Back to Title Button -->
              <button @click="goToTitleDetails"
                      class="flex items-center gap-2 px-4 py-3 bg-[var(--color-accent)] border border-[var(--color-accent)] rounded-xl text-white cursor-pointer transition-all duration-300 font-medium text-sm min-w-[140px] justify-center hover:bg-[var(--color-accent-hover)] hover:-translate-y-0.5 hover:shadow-lg">
                <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2H5a2 2 0 00-2 2z"></path>
                </svg>
                <span class="whitespace-nowrap">Back to Title</span>
              </button>

              <!-- Right Navigation Button -->
              <button @click="enhancedGoToNextPage"
                      :disabled="currentPage === totalPages && !chapterData?.nextChapterId"
                      class="flex items-center gap-2 px-4 py-3 bg-black/80 backdrop-blur-lg border border-white/20 rounded-xl text-white cursor-pointer transition-all duration-300 font-medium text-sm min-w-[140px] justify-end hover:bg-black/90 hover:border-[var(--color-accent)] hover:-translate-y-0.5 hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none">
                <span class="whitespace-nowrap">
                  {{ currentPage === totalPages ? 'Next Chapter' : 'Next Page' }}
                </span>
                <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                </svg>
              </button>
            </div>

            <!-- Mobile Navigation -->
            <div class="md:hidden flex flex-col gap-3 mb-6">
              <!-- Mobile Left Button -->
              <button @click="enhancedGoToPrevPage"
                      :disabled="currentPage === 1 && !chapterData?.previousChapterId"
                      class="flex items-center justify-center gap-2 w-full px-4 py-3 bg-black/80 backdrop-blur-lg border border-white/20 rounded-xl text-white cursor-pointer transition-all duration-300 font-medium text-sm hover:bg-black/90 hover:border-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed">
                <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"></path>
                </svg>
                <span>{{ currentPage === 1 ? 'Previous Chapter' : 'Previous Page' }}</span>
              </button>

              <!-- Mobile Center Button -->
              <button @click="goToTitleDetails"
                      class="flex items-center justify-center gap-2 w-full px-4 py-3 bg-[var(--color-accent)] border border-[var(--color-accent)] rounded-xl text-white cursor-pointer transition-all duration-300 font-medium text-sm hover:bg-[var(--color-accent-hover)]">
                <svg class="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2H5a2 2 0 00-2 2z"></path>
                </svg>
                <span>Back to Title</span>
              </button>

              <!-- Mobile Right Button -->
              <button @click="enhancedGoToNextPage"
                      :disabled="currentPage === totalPages && !chapterData?.nextChapterId"
                      class="flex items-center justify-center gap-2 w-full px-4 py-3 bg-black/80 backdrop-blur-lg border border-white/20 rounded-xl text-white cursor-pointer transition-all duration-300 font-medium text-sm hover:bg-black/90 hover:border-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed">
                <span>{{ currentPage === totalPages ? 'Next Chapter' : 'Next Page' }}</span>
                <svg class="w-5 h-5 flex-shrink-0 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
                </svg>
              </button>
            </div>
          </div>
        </div>

        <!-- Image Comments Section (separate from navigation) -->
        <div v-if="showImageComments && currentImage && viewMode === 'single'" class="image-comments-section">
          <div class="max-w-4xl mx-auto px-4 py-6">
            <div class="flex items-center justify-between mb-4">
              <h4 class="text-lg font-semibold text-[var(--color-text)] flex items-center gap-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z"></path>
                </svg>
                Comments for Page {{ currentPage }}
              </h4>
              <button @click="imageCommentsVisible = !imageCommentsVisible"
                      class="p-2 text-[var(--color-text)] opacity-60 hover:opacity-100 rounded-lg hover:bg-[var(--color-background-mute)] transition-all duration-200">
                <svg v-if="imageCommentsVisible" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7"></path>
                </svg>
                <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
                </svg>
              </button>
            </div>

            <!-- Image Comments Container -->
            <div v-if="imageCommentsVisible" class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
              <CommentsComponent v-if="chapterData.id"
                                 :key="`image-comments-${chapterData.id}-page-${currentPage}`"
                                 :target-id="getImageCommentTargetId()"
                                 :target-type="3"
                                 :is-authenticated="isAuthenticated"
                                 :current-user-id="currentUserId"
                                 :is-admin="isAdmin"
                                 @comments-loaded="onImageCommentsLoaded"
                                 @comment-added="onImageCommentAdded"
                                 @comments-updated="onImageCommentsUpdated" />
            </div>
          </div>
        </div>

        <!-- Page Indicator -->
        <div class="page-indicator" :class="{ 'hidden': !uiVisible }">
          <select v-model="currentPage"
                  @change="enhancedChangePage"
                  class="page-selector">
            <option v-for="page in totalPages" :key="page" :value="page">
              {{ page }} / {{ totalPages }}
            </option>
          </select>
        </div>


      </div>

      <!-- All Pages View -->
      <div v-else class="all-pages-view">
        <div class="all-pages-container" ref="allPagesContainer">
          <div class="manga-content-wrapper">
            <div class="manga-pages-wrapper" :style="[{ gap: `${imageGap}px` }, mangaPagesWrapperStyles]">
              <div v-for="(image, index) in orderedImages"
                   :key="image.id"
                   class="manga-page-wrapper"
                   :style="{ marginBottom: `${imageGap}px` }">
                <div v-if="!hidePageNumbers" class="page-number-indicator">
                  Page {{ index + 1 }}
                </div>
                <div class="manga-image-container">
                  <img :src="getImageUrl(image.imagePath)"
                       :alt="`Page ${index + 1}`"
                       class="manga-image"
                       :style="allPagesImageStyles"
                       @error="handleImageError"
                       @load="updateTapZonesDimensions" />

                  <!-- Touch/Click Zones - On each image in all pages view -->
                  <div class="tap-zones-on-image tap-zones-all-pages">
                    <div class="tap-zone tap-zone-left"
                         @click="enhancedHandleTapZoneClick('left', enhancedGotoPrevChapter)"
                         data-zone="Previous Chapter">
                    </div>
                    <div class="tap-zone tap-zone-center"
                         @click="enhancedHandleTapZoneClick('center', toggleUI)"
                         data-zone="Toggle UI">
                    </div>
                    <div class="tap-zone tap-zone-right"
                         @click="enhancedHandleTapZoneClick('right', enhancedGotoNextChapter)"
                         data-zone="Next Chapter">
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Navigation Controls -->
            <div class="static-navigation">
              <div class="chapter-navigation-controls">
                <button @click="enhancedGotoPrevChapter"
                        :disabled="!chapterData.previousChapterId"
                        class="nav-btn prev-chapter">
                  Previous Chapter
                </button>
                <button @click="goToTitleDetails" class="nav-btn back-to-title">
                  Back to Title
                </button>
                <button @click="enhancedGotoNextChapter"
                        :disabled="!chapterData.nextChapterId"
                        class="nav-btn next-chapter">
                  Next Chapter
                </button>
              </div>
            </div>

            <!-- Comments Section for All Pages View -->
            <div class="all-pages-comments-section">
              <div class="max-w-4xl mx-auto px-4 py-8">
                <CommentsComponent v-if="chapterData.id"
                                   :target-id="chapterData.id"
                                   :target-type="2"
                                   :is-authenticated="isAuthenticated"
                                   :current-user-id="currentUserId"
                                   :is-admin="isAdmin"
                                   @comments-loaded="onCommentsLoaded"
                                   @comment-added="onCommentAdded"
                                   @comments-updated="onCommentsUpdated" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- No Images State -->
    <div v-else-if="chapterData && orderedImages.length === 0" class="no-images-container">
      <div class="no-images-content">
        <svg class="w-16 h-16 text-[var(--color-text)] opacity-50 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
        </svg>
        <h3 class="text-xl font-semibold text-[var(--color-text)] mb-2">No Images Available</h3>
        <p class="text-[var(--color-text)] opacity-75 mb-4">This chapter doesn't have any images yet.</p>
        <button @click="goToTitleDetails" class="px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] transition-colors duration-200">
          Back to Title
        </button>
      </div>
    </div>

    <!-- Chapter List Popup -->
    <div v-if="showChapterList" class="popup chapter-list-popup" @click="handleBackdropClick">
      <div class="popup__content scrollable" @click.stop>
        <div class="popup__header">
          <h3>Chapter List</h3>
          <button class="close-btn" @click="toggleChapterList">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>
        <div class="chapter-list">
          <div v-for="chapter in chaptersList"
               :key="chapter.id"
               class="chapter-item"
               :class="{ 'active': chapter.chapterNumber === chapterData?.chapterNumber }"
               @click="goToChapter(chapter)">
            <div class="chapter-item-left">
              <div class="chapter-title">
                Vol.{{ chapter.volumeNumber }} Ch.{{ chapter.chapterNumber }}
                <span v-if="chapter.name">: {{ chapter.name }}</span>
              </div>
              <div class="chapter-team">{{ chapter.teamName }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Settings Popup -->
    <div v-if="showSettings" class="popup settings-popup" @click="handleBackdropClick">
      <div class="popup__content scrollable" @click.stop>
        <div class="popup__header">
          <h3>Reading Settings</h3>
          <button class="close-btn" @click="toggleSettings">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18" />
              <line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          </button>
        </div>

        <div class="settings-content">
          <!-- View Mode -->
          <div class="settings-section">
            <div class="settings-label">Reading Mode</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': viewMode === 'single' }"
                      @click="setViewMode('single')">
                Single Page
              </button>
              <button class="settings-btn"
                      :class="{ 'active': viewMode === 'all' }"
                      @click="setViewMode('all')">
                All Pages
              </button>
            </div>
          </div>

          <!-- Reading Direction -->
          <div class="settings-section">
            <div class="settings-label">Reading Direction</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': readingDirection === 'horizontal' }"
                      @click="setReadingDirection('horizontal')">
                Horizontal
              </button>
              <button class="settings-btn"
                      :class="{ 'active': readingDirection === 'vertical' }"
                      @click="setReadingDirection('vertical')">
                Vertical
              </button>
            </div>
          </div>

          <!-- Theme -->
          <div class="settings-section">
            <div class="settings-label">Reading Theme</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': currentTheme === 'dark' }"
                      @click="setTheme('dark')">
                Dark
              </button>
              <button class="settings-btn"
                      :class="{ 'active': currentTheme === 'light' }"
                      @click="setTheme('light')">
                Light
              </button>
              <button class="settings-btn"
                      :class="{ 'active': currentTheme === 'system' }"
                      @click="setTheme('system')">
                System
              </button>
            </div>
          </div>

          <!-- Image Fitting -->
          <div class="settings-section">
            <div class="settings-label">Fit Images</div>
            <div class="settings-options">
              <button class="settings-btn"
                      :class="{ 'active': imageSize === 'width' }"
                      @click="setImageSize('width')">
                By Width
              </button>
              <button class="settings-btn"
                      :class="{ 'active': imageSize === 'height' }"
                      @click="setImageSize('height')">
                By Height
              </button>
            </div>
          </div>

          <!-- Brightness -->
          <div class="settings-section">
            <div class="settings-label">
              <span>Brightness {{ brightness }}%</span>
            </div>
            <div class="settings-slider">
              <input type="range"
                     min="50"
                     max="150"
                     v-model="brightness"
                     @input="setBrightness"
                     class="slider" />
            </div>
          </div>

          <!-- Image Gap -->
          <div class="settings-section">
            <div class="settings-label">
              <span>Image Gap {{ imageGap }}px</span>
            </div>
            <div class="settings-slider">
              <input type="range"
                     min="0"
                     max="50"
                     v-model="imageGap"
                     @input="setImageGap"
                     class="slider" />
            </div>
          </div>

          <!-- Container Width -->
          <div class="settings-section">
            <div class="settings-label">
              <span>Container Width {{ containerWidth }}%</span>
            </div>
            <div class="settings-slider">
              <input type="range"
                     min="50"
                     max="100"
                     v-model="containerWidth"
                     @input="setContainerWidth"
                     class="slider" />
            </div>
          </div>

          <!-- Hide Page Numbers Toggle -->
          <div class="settings-section toggle-section">
            <div class="settings-label">
              <span>Hide Page Numbers</span>
            </div>
            <div class="toggle-switch">
              <input type="checkbox"
                     id="hidePageNumbers"
                     v-model="hidePageNumbers"
                     @change="setHidePageNumbers" />
              <label for="hidePageNumbers"></label>
            </div>
          </div>

          <!-- Hide Hints Toggle -->
          <div class="settings-section toggle-section">
            <div class="settings-label">
              <span>Hide Hints</span>
            </div>
            <div class="toggle-switch">
              <input type="checkbox"
                     id="hideHints"
                     v-model="hideHints"
                     @change="setHideHints" />
              <label for="hideHints"></label>
            </div>
          </div>
          <!-- Image Comments Toggle -->
          <div class="settings-section toggle-section">
            <div class="settings-label">
              <span>Show Image Comments</span>
            </div>
            <div class="toggle-switch">
              <input type="checkbox"
                     id="showImageComments"
                     v-model="showImageComments"
                     @change="setShowImageComments" />
              <label for="showImageComments"></label>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Hint Display -->
    <div v-if="currentHint && !hideHints" class="manga-reader-hint" :style="{ opacity: hintOpacity }">
      {{ currentHint }}
    </div>
  </div>
</template>

<script setup>
  import { ref, reactive, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { titleDetailsService } from '../../services/titleDetailsService'
  import { chapterService } from '../../services/chapterService'
  import CommentsComponent from '../title-details/CommentsComponent.vue'

  // Props
  const props = defineProps({
    titleName: {
      type: String,
      required: true
    },
    chapterName: {
      type: String,
      required: true
    },
    volumeNumber: {
      type: [Number, String],
      required: true
    },
    teamId: {
      type: [Number, String],
      required: true
    }
  })

  // Router
  const route = useRoute()
  const router = useRouter()

  // State
  const loading = ref(true)
  const error = ref('')
  const chapterData = ref(null)
  const chaptersList = ref([])
  const currentPage = ref(1)
  const debugMode = ref(false)

  const showImageComments = ref(false)
  const imageCommentsVisible = ref(true)

  // Authentication state
  const isAuthenticated = ref(false)
  const currentUserId = ref('')
  const isAdmin = ref(false)

  // Refs for image handling
  const currentImageRef = ref(null)
  const tapZonesRef = ref(null)
  const imageContainerRef = ref(null)

  // UI State
  const uiVisible = ref(true)
  const showSettings = ref(false)
  const showChapterList = ref(false)
  const currentHint = ref('')
  const hintOpacity = ref(0)

  // Dynamic navbar height tracking
  const navbarHeight = ref(0)

  // Settings State
  const viewMode = ref('single') // 'single' or 'all'
  const currentTheme = ref('dark')
  const readingDirection = ref('horizontal')
  const imageSize = ref('width') // 'width' or 'height'
  const brightness = ref(100)
  const imageGap = ref(13)
  const containerWidth = ref(100)
  const hidePageNumbers = ref(false)
  const hideHints = ref(false)

  // ===================================================================
  // AUTHENTICATION CHECK
  // ===================================================================

  const checkAuthStatus = () => {
    try {
      const token = localStorage.getItem('authToken')
      const user = localStorage.getItem('authUser')

      if (token && user) {
        const userData = JSON.parse(user)
        isAuthenticated.value = true
        currentUserId.value = userData.id || userData.userId || ''
        isAdmin.value = userData.role === 'Admin' || userData.roles?.includes('Admin') || false
      }

      console.log('ChapterReader auth status:', {
        isAuthenticated: isAuthenticated.value,
        currentUserId: currentUserId.value,
        isAdmin: isAdmin.value
      })
    } catch (err) {
      console.error('Error checking auth status in ChapterReader:', err)
      isAuthenticated.value = false
      currentUserId.value = ''
      isAdmin.value = false
    }
  }

  // ===================================================================
  // SCROLL POSITION MEMORY SYSTEM
  // ===================================================================

  // Navigation history for tracking scroll positions with proper bidirectional support
  const navigationHistory = ref([])
  const currentHistoryIndex = ref(-1)
  const maxHistorySize = 50

  // Scroll position tracking with enhanced keys
  const scrollPositions = ref(new Map())
  const isRestoringScroll = ref(false)
  const scrollRestoreTimeout = ref(null)
  let scrollSaveTimer = null

  // Generate unique key for current location
  const getCurrentLocationKey = () => {
    return `${props.titleName}/${props.chapterName}/v${props.volumeNumber}/t${props.teamId}/${viewMode.value}/${currentPage.value}`
  }

  // Enhanced save scroll position with better tracking
  const saveScrollPosition = () => {
    if (isRestoringScroll.value) return

    const key = getCurrentLocationKey()
    const scrollY = window.scrollY || window.pageYOffset || 0
    const scrollX = window.scrollX || window.pageXOffset || 0

    const position = {
      key,
      scrollY,
      scrollX,
      timestamp: Date.now(),
      viewMode: viewMode.value,
      page: currentPage.value,
      titleName: props.titleName,
      chapterName: props.chapterName,
      volumeNumber: props.volumeNumber,
      teamId: props.teamId
    }

    scrollPositions.value.set(key, position)

    // Also save to localStorage for persistence
    try {
      const savedPositions = JSON.parse(localStorage.getItem('chapterScrollPositions') || '{}')
      savedPositions[key] = position

      // Keep only recent positions (last 200 for better forward navigation)
      const entries = Object.entries(savedPositions)
      if (entries.length > 200) {
        entries.sort((a, b) => b[1].timestamp - a[1].timestamp)
        const recent = Object.fromEntries(entries.slice(0, 200))
        localStorage.setItem('chapterScrollPositions', JSON.stringify(recent))
      } else {
        localStorage.setItem('chapterScrollPositions', JSON.stringify(savedPositions))
      }
    } catch (error) {
      console.warn('Failed to save scroll position to localStorage:', error)
    }

    console.log('ðŸ“ Saved scroll position:', { key, scrollY, scrollX })
  }

  // Enhanced restore scroll position with better fallback handling
  const restoreScrollPosition = (key, fallbackBehavior = 'top') => {
    let position = scrollPositions.value.get(key)

    // Try to load from localStorage if not in memory
    if (!position) {
      try {
        const savedPositions = JSON.parse(localStorage.getItem('chapterScrollPositions') || '{}')
        position = savedPositions[key]
        if (position) {
          scrollPositions.value.set(key, position)
        }
      } catch (error) {
        console.warn('Failed to load scroll position from localStorage:', error)
      }
    }

    if (position) {
      console.log('ðŸ”„ Restoring scroll position:', { key, position })
      isRestoringScroll.value = true

      if (scrollRestoreTimeout.value) {
        clearTimeout(scrollRestoreTimeout.value)
      }

      nextTick(() => {
        scrollRestoreTimeout.value = setTimeout(() => {
          window.scrollTo({
            top: position.scrollY,
            left: position.scrollX,
            behavior: 'auto'
          })

          setTimeout(() => {
            isRestoringScroll.value = false
          }, 500)
        }, 100)
      })

      return true
    } else {
      console.log('ðŸ†• No saved position found, using fallback:', fallbackBehavior)
      nextTick(() => {
        if (fallbackBehavior === 'bottom') {
          scrollToBottom()
        } else {
          scrollToTop()
        }
      })
      return false
    }
  }

  // Enhanced navigation history tracking with proper direction support
  const addToNavigationHistory = (direction = 'forward', targetKey = null) => {
    const currentKey = getCurrentLocationKey()

    const historyItem = {
      key: currentKey,
      direction,
      timestamp: Date.now(),
      titleName: props.titleName,
      chapterName: props.chapterName,
      volumeNumber: props.volumeNumber,
      teamId: props.teamId,
      viewMode: viewMode.value,
      page: currentPage.value,
      targetKey // The key we're navigating to
    }

    // Handle different navigation scenarios
    if (direction === 'forward') {
      // For forward navigation, we might be going to a new location or revisiting
      // Truncate future history if we're in the middle of the history stack
      if (currentHistoryIndex.value < navigationHistory.value.length - 1) {
        navigationHistory.value = navigationHistory.value.slice(0, currentHistoryIndex.value + 1)
      }

      navigationHistory.value.push(historyItem)
      currentHistoryIndex.value = navigationHistory.value.length - 1
    } else if (direction === 'backward') {
      // For backward navigation, we don't add to history, just move the index
      if (currentHistoryIndex.value > 0) {
        currentHistoryIndex.value--
      }
    }

    // Keep history size manageable
    if (navigationHistory.value.length > maxHistorySize) {
      const removeCount = navigationHistory.value.length - maxHistorySize
      navigationHistory.value = navigationHistory.value.slice(removeCount)
      currentHistoryIndex.value = Math.max(0, currentHistoryIndex.value - removeCount)
    }

    console.log('ðŸ“š Navigation history updated:', {
      direction,
      currentIndex: currentHistoryIndex.value,
      historyLength: navigationHistory.value.length,
      currentKey,
      targetKey
    })
  }

  // Enhanced check for whether we're revisiting a previous location
  const isRevisitingLocation = (targetKey) => {
    // Check if we have a saved scroll position for this exact location
    return scrollPositions.value.has(targetKey)
  }

  // Enhanced check for going back in navigation history
  const isGoingBackInHistory = (targetKey) => {
    // Check if the target key exists in our recent navigation history
    if (currentHistoryIndex.value > 0) {
      // Look back through recent history to see if we're returning to a previous location
      for (let i = currentHistoryIndex.value - 1; i >= Math.max(0, currentHistoryIndex.value - 5); i--) {
        const historyItem = navigationHistory.value[i]
        if (historyItem && historyItem.key === targetKey) {
          return true
        }
      }
    }
    return false
  }

  // Throttled scroll listener
  const handleScroll = () => {
    if (isRestoringScroll.value) return

    if (scrollSaveTimer) {
      clearTimeout(scrollSaveTimer)
    }

    scrollSaveTimer = setTimeout(() => {
      saveScrollPosition()
    }, 500)
  }

  // Setup scroll tracking
  const setupScrollTracking = () => {
    try {
      const savedPositions = JSON.parse(localStorage.getItem('chapterScrollPositions') || '{}')
      Object.entries(savedPositions).forEach(([key, position]) => {
        scrollPositions.value.set(key, position)
      })
      console.log('ðŸ“‹ Loaded saved scroll positions:', scrollPositions.value.size)
    } catch (error) {
      console.warn('Failed to load saved scroll positions:', error)
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    window.addEventListener('beforeunload', () => {
      saveScrollPosition()
    })
  }

  const cleanupScrollTracking = () => {
    window.removeEventListener('scroll', handleScroll)
    window.removeEventListener('beforeunload', saveScrollPosition)

    if (scrollSaveTimer) {
      clearTimeout(scrollSaveTimer)
    }

    if (scrollRestoreTimeout.value) {
      clearTimeout(scrollRestoreTimeout.value)
    }
  }

  // ===================================================================
  // COMPUTED PROPERTIES
  // ===================================================================

  const isMobile = computed(() => {
    return window.innerWidth <= 768
  })

  const orderedImages = computed(() => {
    if (!chapterData.value?.imagePaths) return []
    return [...chapterData.value.imagePaths].sort((a, b) => a.orderIndex - b.orderIndex)
  })

  const totalPages = computed(() => orderedImages.value.length)

  const currentImage = computed(() => {
    if (orderedImages.value.length === 0) return null
    return orderedImages.value[currentPage.value - 1] || null
  })

  // FIXED: Improved image styles calculations
  const imageStyles = computed(() => {
    const availableHeight = `calc(100vh - ${navbarHeight.value}px - 2rem)` // Account for navbar and padding

    return {
      filter: `brightness(${brightness.value}%)`,
      maxWidth: imageSize.value === 'width' ? '100%' : 'none',
      maxHeight: imageSize.value === 'height' ? availableHeight : 'none',
      height: 'auto',
      objectFit: 'contain'
    }
  })

  const mangaPagesWrapperStyles = computed(() => ({
    maxWidth: `${containerWidth.value}%`,
    margin: '0 auto'
  }))

  // FIXED: All pages image styles with proper height calculations
  const allPagesImageStyles = computed(() => {
    return {
      filter: `brightness(${brightness.value}%)`,
      width: '100%',
      height: 'auto',
      maxWidth: '100%',
      display: 'block',
      objectFit: 'contain'
    }
  })

  // FIXED: Function to calculate and update navbar height
  const updateNavbarHeight = () => {
    nextTick(() => {
      const navbar = document.getElementById('mangaNavbar')
      if (navbar) {
        navbarHeight.value = navbar.offsetHeight
      }
    })
  }

  // ===================================================================
  // ENHANCED NAVIGATION WITH SCROLL MEMORY
  // ===================================================================

  // Enhanced goToNextPage with scroll restoration
  const enhancedGoToNextPage = () => {
    if (currentPage.value < totalPages.value) {
      const newPage = currentPage.value + 1
      const targetKey = `${props.titleName}/${props.chapterName}/v${props.volumeNumber}/t${props.teamId}/${viewMode.value}/${newPage}`

      // Always save current position before navigating
      saveScrollPosition()

      // Add to navigation history
      addToNavigationHistory('forward', targetKey)

      // Update current page and URL
      currentPage.value = newPage
      updateUrl({ page: newPage })

      // Check if we're revisiting this page and restore position if available
      if (isRevisitingLocation(targetKey)) {
        console.log('ðŸ”„ Revisiting next page, restoring scroll position')
        restoreScrollPosition(targetKey, 'top')
      } else {
        console.log('ðŸ†• New next page, scrolling to top')
        scrollToTop()
      }
    } else {
      enhancedGotoNextChapter()
    }
  }

  // Enhanced goToPrevPage (improved version)
  const enhancedGoToPrevPage = () => {
    if (currentPage.value > 1) {
      const newPage = currentPage.value - 1
      const targetKey = `${props.titleName}/${props.chapterName}/v${props.volumeNumber}/t${props.teamId}/${viewMode.value}/${newPage}`

      // Always save current position before navigating
      saveScrollPosition()

      // For backward navigation, we move back in history rather than adding
      addToNavigationHistory('backward', targetKey)

      // Update current page and URL
      currentPage.value = newPage
      updateUrl({ page: newPage })

      // Always try to restore position for previous pages
      if (isRevisitingLocation(targetKey)) {
        console.log('ðŸ”„ Going back to previous page, restoring scroll position')
        restoreScrollPosition(targetKey, 'top')
      } else {
        console.log('ðŸ†• New previous page, scrolling to top')
        scrollToTop()
      }
    } else {
      enhancedGotoPrevChapter()
    }
  }

  // Enhanced chapter navigation with better scroll restoration
  const enhancedGotoNextChapter = () => {
    if (!chapterData.value?.nextChapterId) {
      goToTitleDetails()
      return
    }

    const targetKey = `${chapterData.value.titleName}/${chapterData.value.nextChapterName}/v${chapterData.value.nextChapterVolume}/t${chapterData.value.nextChapterTeamId}/${viewMode.value}/1`

    // Save current position before navigating
    saveScrollPosition()

    // Add to navigation history
    addToNavigationHistory('forward', targetKey)

    const url = buildChapterUrl(
      chapterData.value.titleName,
      chapterData.value.nextChapterName,
      chapterData.value.nextChapterVolume,
      chapterData.value.nextChapterTeamId,
      {
        viewMode: viewMode.value,
        page: 1,
        restoreScroll: isRevisitingLocation(targetKey) ? 'true' : 'false',
        scrollTo: isRevisitingLocation(targetKey) ? 'restore' : 'top'
      }
    )

    console.log('âž¡ï¸ Navigating to next chapter:', {
      targetKey,
      hasPosition: isRevisitingLocation(targetKey)
    })

    router.push(url)
  }

  const enhancedGotoPrevChapter = () => {
    if (!chapterData.value?.previousChapterId) {
      goToTitleDetails()
      return
    }

    const targetKey = `${chapterData.value.titleName}/${chapterData.value.previousChapterName}/v${chapterData.value.previousChapterVolume}/t${chapterData.value.previousChapterTeamId}/${viewMode.value}/${chapterData.value.previousChapterPageCount || 1}`

    // Save current position before navigating
    saveScrollPosition()

    // Add to navigation history as backward movement
    addToNavigationHistory('backward', targetKey)

    const url = buildChapterUrl(
      chapterData.value.titleName,
      chapterData.value.previousChapterName,
      chapterData.value.previousChapterVolume,
      chapterData.value.previousChapterTeamId,
      {
        viewMode: viewMode.value,
        page: chapterData.value.previousChapterPageCount || 1,
        restoreScroll: isRevisitingLocation(targetKey) ? 'true' : 'false',
        scrollTo: isRevisitingLocation(targetKey) ? 'restore' : 'bottom'
      }
    )

    console.log('â¬…ï¸ Navigating to previous chapter:', {
      targetKey,
      hasPosition: isRevisitingLocation(targetKey)
    })

    router.push(url)
  }

  // Enhanced changePage with better scroll restoration
  const enhancedChangePage = (page) => {
    if (typeof page === 'object') {
      page = parseInt(page.target.value)
    }

    const newPage = Math.max(1, Math.min(page, totalPages.value))
    const oldPage = currentPage.value

    if (newPage === oldPage) return

    const targetKey = `${props.titleName}/${props.chapterName}/v${props.volumeNumber}/t${props.teamId}/${viewMode.value}/${newPage}`

    // Save current position before changing page
    saveScrollPosition()

    // Determine direction and add to history appropriately
    const direction = newPage > oldPage ? 'forward' : 'backward'
    addToNavigationHistory(direction, targetKey)

    currentPage.value = newPage
    updateUrl({ page: newPage })

    if (viewMode.value === 'single') {
      // For single page view, try to restore position if available
      if (isRevisitingLocation(targetKey)) {
        console.log('ðŸ”„ Revisiting page via selector, restoring scroll position')
        restoreScrollPosition(targetKey, 'top')
      } else {
        console.log('ðŸ†• New page via selector, scrolling to top')
        scrollToTop()
      }
    }
  }

  // Enhanced tap zone click handler with scroll memory
  const enhancedHandleTapZoneClick = (zone, action) => {
    // Save position before any navigation action
    saveScrollPosition()

    if (typeof action === 'function') {
      action()
    }

    // Show hints if enabled
    if (!hideHints.value) {
      let hintMessage = ''
      switch (zone) {
        case 'left':
          hintMessage = viewMode.value === 'single' ? 'Previous page' : 'Previous chapter'
          break
        case 'center':
          hintMessage = 'Toggle controls'
          break
        case 'right':
          hintMessage = viewMode.value === 'single' ? 'Next page' : 'Next chapter'
          break
      }
      if (hintMessage) {
        const deviceType = window.innerWidth <= 768 ? 'mobile' : 'desktop'
        const actionPrefix = deviceType === 'mobile' ? 'Tap' : 'Click'
        showHint(`${actionPrefix}: ${hintMessage}`)
      }
    }
  }

  // ===================================================================
  // COMMENTS SINGLE PAGE VIEW
  // ===================================================================

  const getImageCommentTargetId = () => {
    // Use the actual image ID from the database instead of synthetic ID
    if (!currentImage.value || !currentImage.value.id) {
      console.warn('No current image or image ID available for comments')
      return 0
    }

    return currentImage.value.id
  }

  const setShowImageComments = () => {
    savePreference('showImageComments', showImageComments.value)
  }

  const onImageCommentsLoaded = (data) => {
    console.log('Image comments loaded for page', currentPage.value, ':', data)
  }

  const onImageCommentAdded = (comment) => {
    console.log('New image comment added for page', currentPage.value, ':', comment)
  }

  const onImageCommentsUpdated = (data) => {
    console.log('Image comments updated for page', currentPage.value, ':', data)
  }

  // ===================================================================
  // COMMENTS EVENT HANDLERS
  // ===================================================================

  const onCommentsLoaded = (data) => {
    console.log('Chapter comments loaded:', data)
  }

  const onCommentAdded = (comment) => {
    console.log('New comment added to chapter:', comment)
  }

  const onCommentsUpdated = (data) => {
    console.log('Chapter comments updated:', data)
  }

  // ===================================================================
  // CORE FUNCTIONALITY
  // ===================================================================

  // FIXED: Enhanced loadChapter with proper scroll handling
  const loadChapter = async () => {
    try {
      loading.value = true
      error.value = ''

      console.log('Loading chapter:', props)

      // Use the route pattern from your backend: /titleName/chapter/chapterName/vvolume/tteamId
      const result = await titleDetailsService.getChapterByRoute(
        props.titleName,
        props.chapterName,
        props.volumeNumber,
        props.teamId,
        route.query.page
      )

      if (result.success && result.data) {
        chapterData.value = result.data

        // Set current page from URL or default to 1
        const urlPage = parseInt(route.query.page) || 1
        currentPage.value = Math.max(1, Math.min(urlPage, totalPages.value))

        // FIXED: Proper preference priority - URL param > saved preference > default
        const urlViewMode = route.query.viewMode
        const savedViewMode = loadPreference('viewMode', 'single')

        if (urlViewMode) {
          // URL has explicit viewMode, use it and save it
          viewMode.value = urlViewMode
          savePreference('viewMode', urlViewMode)
        } else {
          // No URL viewMode, use saved preference and update URL
          viewMode.value = savedViewMode
          updateUrl({ viewMode: savedViewMode })
        }

        // Load chapter list for navigation
        await loadChaptersList()

        // Update reading progress if user is authenticated
        await updateReadingProgress()

        // Update page title
        document.title = `${chapterData.value.titleName} - ${chapterData.value.name || `Ch.${chapterData.value.chapterNumber}`}`

        // FIXED: Handle scroll behavior after chapter loads
        await nextTick()
        enhancedHandleScrollBehavior()

        // Update navbar height after content loads
        updateNavbarHeight()

      } else {
        error.value = result.error || 'Chapter not found'
      }
    } catch (err) {
      console.error('Error loading chapter:', err)
      error.value = err.message || 'Failed to load chapter'
    } finally {
      loading.value = false
    }
  }

  const loadChaptersList = async () => {
    if (!chapterData.value?.titleId) return

    try {
      const result = await titleDetailsService.getChapters(chapterData.value.titleId)
      if (result.success) {
        chaptersList.value = result.data || []
      }
    } catch (err) {
      console.error('Error loading chapters list:', err)
    }
  }

  const updateReadingProgress = async () => {
    if (!chapterData.value) return

    try {
      await chapterService.updateReadingProgress(
        chapterData.value.titleId,
        chapterData.value.chapterNumber
      )
    } catch (err) {
      console.error('Error updating reading progress:', err)
    }
  }

  const retryLoad = () => {
    loadChapter()
  }

  // FIXED: Navigation to title details page - use correct router path
  const goToTitleDetails = () => {
    const titleName = chapterData.value?.titleName || props.titleName
    router.push(`/${encodeURIComponent(titleName)}`)
  }

  const goToChapter = (chapter) => {
    const url = buildChapterUrl(
      chapterData.value.titleName,
      chapter.name || chapter.chapterNumber,
      chapter.volumeNumber,
      chapter.teamId,
      {
        viewMode: viewMode.value,
        page: 1,
        restoreScroll: 'false'
      }
    )

    router.push(url)
    toggleChapterList()
  }

  // UI Methods
  const toggleUI = () => {
    uiVisible.value = !uiVisible.value
    savePreference('uiVisible', uiVisible.value)

    if (!hideHints.value) {
      const deviceType = window.innerWidth <= 768 ? 'Tap' : 'Click'
      showHint(`${deviceType} center to toggle UI`)
    }
  }

  const toggleSettings = () => {
    showSettings.value = !showSettings.value
    showChapterList.value = false
  }

  const toggleChapterList = () => {
    showChapterList.value = !showChapterList.value
    showSettings.value = false
  }

  const handleBackdropClick = (e) => {
    if (e.target === e.currentTarget) {
      showSettings.value = false
      showChapterList.value = false
    }
  }

  const showHint = (message) => {
    if (hideHints.value) return

    currentHint.value = message
    hintOpacity.value = 1

    setTimeout(() => {
      hintOpacity.value = 0
      setTimeout(() => {
        currentHint.value = ''
      }, 300)
    }, 2000)
  }

  // FIXED: Enhanced setViewMode to handle scroll position
  const setViewMode = (mode) => {
    viewMode.value = mode
    // Always update URL when view mode changes
    updateUrl({ viewMode: mode })
    savePreference('viewMode', mode)

    // FIXED: Reset scroll position when changing view modes
    nextTick(() => {
      scrollToTop()

      // Update tap zones only when switching to single page view
      if (mode === 'single') {
        updateTapZonesDimensions()
      }
    })
  }

  // FIXED: Enhanced URL builder with scroll behavior
  const buildChapterUrl = (titleName, chapterName, volume, teamId, options = {}) => {
    const baseUrl = `/${encodeURIComponent(titleName)}/chapter/${chapterName}/v${volume}/t${teamId}`
    const params = new URLSearchParams()

    // Include current user preferences
    params.set('viewMode', options.viewMode || viewMode.value)

    if (options.page) {
      params.set('page', options.page)
    }

    // FIXED: Add scroll behavior to URL for proper handling
    if (options.scrollTo) {
      params.set('scrollTo', options.scrollTo)
    }

    if (options.restoreScroll) {
      params.set('restoreScroll', options.restoreScroll)
    }

    return `${baseUrl}?${params.toString()}`
  }

  const setTheme = (theme) => {
    currentTheme.value = theme
    savePreference('theme', theme)
  }

  const setReadingDirection = (direction) => {
    readingDirection.value = direction
    savePreference('readingDirection', direction)
  }

  const setImageSize = (size) => {
    imageSize.value = size
    savePreference('imageSize', size)

    // Update tap zones only for single page view
    if (viewMode.value === 'single') {
      nextTick(() => {
        updateTapZonesDimensions()
      })
    }
  }

  const setBrightness = () => {
    savePreference('brightness', brightness.value)
  }

  const setImageGap = () => {
    savePreference('imageGap', imageGap.value)
  }

  const setContainerWidth = () => {
    savePreference('containerWidth', containerWidth.value)

    // Update tap zones only for single page view
    if (viewMode.value === 'single') {
      nextTick(() => {
        updateTapZonesDimensions()
      })
    }
  }

  const setHidePageNumbers = () => {
    savePreference('hidePageNumbers', hidePageNumbers.value)
  }

  const setHideHints = () => {
    savePreference('hideHints', hideHints.value)
  }

  // Helper Methods
  const getImageUrl = (imagePath) => {
    return titleDetailsService.getImageUrl(imagePath)
  }

  const handleImageError = (event) => {
    console.error('Image failed to load:', event.target.src)
    event.target.src = titleDetailsService.getImageUrl('/img/default-cover.png')
  }

  const handleImageLoad = () => {
    // Image loaded successfully
    updateTapZonesDimensions()
  }

  // FIXED: Improved tap zones positioning for all devices
  const updateTapZonesDimensions = () => {
    // Only handle single page view - all pages view uses full-area tap zones
    if (viewMode.value !== 'single') return

    if (!currentImageRef.value || !tapZonesRef.value) return

    nextTick(() => {
      const img = currentImageRef.value
      const tapZones = tapZonesRef.value

      if (img && tapZones && img.complete) {
        const imgRect = img.getBoundingClientRect()
        const containerRect = img.parentElement.getBoundingClientRect()

        // Calculate the position relative to the container
        const left = imgRect.left - containerRect.left
        const top = imgRect.top - containerRect.top

        // Apply the exact image dimensions and position to tap zones
        tapZones.style.left = `${left}px`
        tapZones.style.top = `${top}px`
        tapZones.style.width = `${imgRect.width}px`
        tapZones.style.height = `${imgRect.height}px`
        tapZones.style.right = 'auto'
        tapZones.style.bottom = 'auto'
      }
    })
  }

  // Enhanced method to handle scroll behavior based on navigation context
  const enhancedHandleScrollBehavior = () => {
    const restoreScroll = route.query.restoreScroll
    const scrollTo = route.query.scrollTo
    const currentKey = getCurrentLocationKey()

    console.log('ðŸŽ¯ Handling scroll behavior:', { restoreScroll, scrollTo, currentKey })

    if (restoreScroll === 'true' || scrollTo === 'restore') {
      // Try to restore the exact position for this location
      const restored = restoreScrollPosition(currentKey)
      if (!restored && scrollTo) {
        // Fallback to specified scroll behavior
        if (scrollTo === 'bottom') {
          setTimeout(() => scrollToBottom(), 100)
        } else {
          scrollToTop()
        }
      }
    } else if (viewMode.value === 'single') {
      scrollToTop()
    } else if (viewMode.value === 'all') {
      if (scrollTo === 'bottom') {
        setTimeout(() => scrollToBottom(), 100)
      } else {
        scrollToTop()
      }
    }

    // Clean up the scroll parameters from URL after handling
    const query = { ...route.query }
    delete query.scrollTo
    delete query.restoreScroll
    if (Object.keys(query).length !== Object.keys(route.query).length) {
      router.replace({ query }).catch(() => { })
    }
  }

  // FIXED: Utility methods for scroll management
  const scrollToTop = () => {
    // Instant scroll to top (no animation)
    window.scrollTo({
      top: 0,
      behavior: 'auto' // Changed from 'smooth' to 'auto' for instant scroll
    })

    // Also ensure container scroll is reset
    const container = document.getElementById('chapterContainer')
    if (container) {
      container.scrollTop = 0
    }
  }

  const scrollToBottom = () => {
    // Scroll to bottom for all-pages view when coming from previous chapter
    setTimeout(() => {
      window.scrollTo({
        top: document.documentElement.scrollHeight,
        behavior: 'auto'
      })
    }, 200) // Delay to ensure all images are rendered
  }

  const updateUrl = (params) => {
    const query = { ...route.query, ...params }
    router.replace({ query })
  }

  const savePreference = (key, value) => {
    localStorage.setItem(`chapterReader_${key}`, JSON.stringify(value))
  }

  const loadPreference = (key, defaultValue) => {
    try {
      const saved = localStorage.getItem(`chapterReader_${key}`)
      return saved ? JSON.parse(saved) : defaultValue
    } catch {
      return defaultValue
    }
  }

  const loadPreferences = () => {
    // Load UI state first
    uiVisible.value = loadPreference('uiVisible', true)

    // Load all settings preferences
    currentTheme.value = loadPreference('theme', 'dark')
    readingDirection.value = loadPreference('readingDirection', 'horizontal')
    imageSize.value = loadPreference('imageSize', 'width')
    brightness.value = loadPreference('brightness', 100)
    imageGap.value = loadPreference('imageGap', 13)
    containerWidth.value = loadPreference('containerWidth', 100)
    hidePageNumbers.value = loadPreference('hidePageNumbers', false)
    hideHints.value = loadPreference('hideHints', false)
    showImageComments.value = loadPreference('showImageComments', false)

    // FIXED: Don't set viewMode here - let loadChapter handle it with proper priority
    // The viewMode will be set in loadChapter with proper URL > saved preference > default logic
  }

  // Enhanced keyboard navigation with scroll memory
  const handleKeydown = (e) => {
    if (showSettings.value || showChapterList.value) {
      if (e.key === 'Escape') {
        showSettings.value = false
        showChapterList.value = false
      }
      return
    }

    switch (e.key) {
      case 'ArrowLeft':
        e.preventDefault()
        if (viewMode.value === 'single') {
          enhancedGoToPrevPage()
        } else {
          enhancedGotoPrevChapter()
        }
        break
      case 'ArrowRight':
        e.preventDefault()
        if (viewMode.value === 'single') {
          enhancedGoToNextPage()
        } else {
          enhancedGotoNextChapter()
        }
        break
      case 'Escape':
        e.preventDefault()
        toggleUI()
        break
      case 'd':
        if (e.ctrlKey) {
          e.preventDefault()
          debugMode.value = !debugMode.value
        }
        break
    }
  }

  // Lifecycle
  onMounted(async () => {
    // Check authentication status first
    checkAuthStatus()

    loadPreferences()
    await loadChapter()

    // Setup scroll position tracking
    setupScrollTracking()

    // Add event listeners
    document.addEventListener('keydown', handleKeydown)

    // Add window resize listener to update navbar height and tap zones
    window.addEventListener('resize', () => {
      updateNavbarHeight()
      updateTapZonesDimensions()
    })

    // Handle fullscreen on double click
    document.addEventListener('dblclick', (e) => {
      if (e.target.closest('button, select, .popup__content')) return

      if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen().catch(console.error)
      } else {
        document.exitFullscreen()
      }
    })

    // Initial navbar height calculation
    updateNavbarHeight()

    // Add current location to history after loading
    await nextTick()
    addToNavigationHistory('initial')
  })

  onUnmounted(() => {
    document.removeEventListener('keydown', handleKeydown)
    window.removeEventListener('resize', () => {
      updateNavbarHeight()
      updateTapZonesDimensions()
    })

    // Cleanup scroll tracking
    cleanupScrollTracking()
    saveScrollPosition()
  })

  // FIXED: Enhanced route watcher to handle scroll behavior
  watch(() => route.query, (newQuery, oldQuery) => {
    // Handle page changes
    if (newQuery.page) {
      const page = parseInt(newQuery.page)
      if (page !== currentPage.value) {
        currentPage.value = Math.max(1, Math.min(page, totalPages.value))
      }
    }

    // Handle view mode changes
    if (newQuery.viewMode && newQuery.viewMode !== viewMode.value) {
      viewMode.value = newQuery.viewMode
      savePreference('viewMode', newQuery.viewMode)
    }

    // FIXED: Handle scroll behavior when route changes (chapter navigation)
    if (newQuery.restoreScroll && newQuery.restoreScroll !== oldQuery?.restoreScroll) {
      nextTick(() => {
        enhancedHandleScrollBehavior()
      })
    }
  })

  // FIXED: Watch for page changes to ensure proper scroll behavior
  watch(currentPage, (newPage, oldPage) => {
    // Only scroll to top for single page view when page changes within same chapter
    if (viewMode.value === 'single' && newPage !== oldPage && !isRestoringScroll.value) {
      nextTick(() => {
        scrollToTop()
        updateTapZonesDimensions()
      })
    }
  }, { immediate: false })

  // FIXED: Watch for chapter data changes to reset scroll
  watch(chapterData, (newChapter, oldChapter) => {
    if (newChapter && oldChapter && newChapter.id !== oldChapter.id) {
      // Chapter changed - handle scroll behavior
      nextTick(() => {
        enhancedHandleScrollBehavior()
      })
    }
  }, { immediate: false })

  // Watch for image size changes to update tap zones (only for single page view)
  watch([imageSize, brightness, containerWidth], () => {
    if (viewMode.value === 'single') {
      nextTick(() => {
        updateTapZonesDimensions()
      })
    }
  })

  // Watch for UI visibility changes to update navbar height
  watch(uiVisible, () => {
    nextTick(() => {
      updateNavbarHeight()
    })
  })
</script>

<style scoped>
  /* Chapter Container */
  .chapter-container {
    min-height: 100vh;
    background-color: var(--color-background);
    color: var(--color-text);
    position: relative;
    overflow-x: hidden;
  }

  /* Mobile: Ensure proper viewport handling */
  @media (max-width: 768px) {
    .chapter-container {
      width: 100vw;
      overflow-x: hidden;
    }
  }

  .chapter-container.dark {
    --manga-bg: #0a0a0a;
    --manga-text: #ffffff;
    --manga-navbar-bg: rgba(0, 0, 0, 0.95);
    --manga-border: #2a2a2a;
  }

  .chapter-container.light {
    --manga-bg: #ffffff;
    --manga-text: #000000;
    --manga-navbar-bg: rgba(255, 255, 255, 0.95);
    --manga-border: #e0e0e0;
  }

  /* FIXED: Manga Navbar with proper height management */
  .manga-navbar {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 1000;
    background: var(--manga-navbar-bg);
    backdrop-filter: blur(10px);
    border-bottom: 1px solid var(--manga-border);
    transition: transform 0.3s ease;
  }

    .manga-navbar.hidden {
      transform: translateY(-100%);
    }

  .navbar-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.75rem 1rem;
    max-width: 1200px;
    margin: 0 auto;
    min-height: 60px; /* Ensure consistent navbar height */
  }

  .navbar-left {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .back-button {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    background: transparent;
    border: 1px solid var(--manga-border);
    border-radius: 0.375rem;
    color: var(--manga-text);
    cursor: pointer;
    transition: all 0.2s ease;
  }

    .back-button:hover {
      background: var(--color-background-mute);
    }

  .chapter-info {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .title-name {
    font-size: 1.125rem;
    font-weight: 600;
    cursor: pointer;
    margin: 0;
  }

  .chapter-nav {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .chapter-nav-btn {
    padding: 0.25rem;
    background: transparent;
    border: none;
    color: var(--manga-text);
    cursor: pointer;
    border-radius: 0.25rem;
    transition: background-color 0.2s ease;
  }

    .chapter-nav-btn:hover:not(:disabled) {
      background: var(--color-background-mute);
    }

    .chapter-nav-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .chapter-name {
    font-size: 1rem;
    font-weight: 500;
    margin: 0;
  }

  .navbar-right {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }

  .settings-btn,
  .chapter-list-btn {
    padding: 0.5rem;
    background: transparent;
    border: 1px solid var(--manga-border);
    border-radius: 0.375rem;
    color: var(--manga-text);
    cursor: pointer;
    transition: all 0.2s ease;
  }

    .settings-btn:hover,
    .chapter-list-btn:hover {
      background: var(--color-background-mute);
    }

  /* Chapter Content Wrapper */
  .chapter-content {
    position: relative;
    width: 100%;
    height: 100%;
  }

  /* Mobile: Ensure chapter content uses full viewport */
  @media (max-width: 768px) {
    .chapter-content {
      width: 100vw;
    }

    .single-page-view {
      width: 100vw;
      max-width: 100vw;
    }
  }

  /* FIXED: Content Areas with dynamic spacing */
  .content-area {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding-top: 80px; /* Fixed padding for navbar */
    padding-bottom: 1rem;
    padding-left: 1rem;
    padding-right: 1rem;
    position: relative;
  }

  /* Mobile: Remove horizontal padding for edge-to-edge display */
  @media (max-width: 768px) {
    .content-area {
      padding-top: 60px; /* Adjusted for mobile navbar */
      padding-left: 0;
      padding-right: 0;
    }
  }

  /* FIXED: Manga image container with better centering */
  .manga-image-container {
    position: relative;
    display: flex;
    justify-content: center;
    align-items: flex-start; /* Changed from center to flex-start for proper top alignment */
    width: 100%;
    max-width: 100%;
  }

  /* Mobile: Ensure image containers use full viewport width */
  @media (max-width: 768px) {
    .all-pages-view .manga-image-container {
      width: 100vw;
      max-width: 100vw;
    }

    .single-page-view .manga-image-container {
      width: 100vw;
      max-width: 100vw;
    }
  }

  /* FIXED: Manga image with better sizing */
  .manga-image {
    display: block;
    max-width: 100%;
    width: auto;
    height: auto;
    object-fit: contain;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  }

  /* Mobile: Ensure images can use full viewport width */
  @media (max-width: 768px) {
    .single-page-view .manga-image {
      max-width: 100vw;
      box-shadow: none; /* Remove shadow for cleaner edge-to-edge look */
    }

    .all-pages-view .manga-image {
      box-shadow: none; /* Remove shadow for cleaner edge-to-edge look */
    }
  }

  /* FIXED: All Pages View with proper spacing */
  .all-pages-container {
    padding-top: 80px; /* Fixed padding for navbar */
    padding-bottom: 2rem;
    min-height: 100vh;
    position: relative;
  }

  /* Mobile: Remove all horizontal padding for true edge-to-edge display */
  @media (max-width: 768px) {
    .all-pages-container {
      padding-top: 60px; /* Adjusted for mobile navbar */
      padding-left: 0;
      padding-right: 0;
      padding-bottom: 2rem;
    }
  }

  .manga-content-wrapper {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 1rem;
  }

  /* Mobile: Remove horizontal padding for full-width images */
  @media (max-width: 768px) {
    .manga-content-wrapper {
      padding: 0;
      max-width: 100%;
    }
  }

  .manga-pages-wrapper {
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 100%;
  }

  /* Mobile: Override container width setting for full-width images */
  @media (max-width: 768px) {
    .manga-pages-wrapper {
      max-width: 100% !important;
      margin: 0 !important;
    }
  }

  .manga-page-wrapper {
    position: relative;
    width: 100%;
    display: flex;
    flex-direction: column;
    align-items: center;
  }

  /* Mobile: Ensure page wrappers don't add any constraints */
  @media (max-width: 768px) {
    .manga-page-wrapper {
      width: 100vw; /* Use viewport width for true edge-to-edge */
    }
  }

  .page-number-indicator {
    background: rgba(0, 0, 0, 0.7);
    color: white;
    padding: 0.25rem 0.75rem;
    border-radius: 1rem;
    font-size: 0.875rem;
    margin-bottom: 0.5rem;
    font-weight: 500;
  }

  /* Page Indicator */
  .page-indicator {
    position: fixed;
    bottom: 1rem;
    left: 50%;
    transform: translateX(-50%);
    z-index: 100;
    transition: opacity 0.3s ease;
  }

    .page-indicator.hidden {
      opacity: 0;
      pointer-events: none;
    }

  .page-selector {
    background: var(--color-background-soft);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    padding: 0.5rem 1rem;
    color: var(--color-text);
    font-size: 0.875rem;
    cursor: pointer;
  }

  /* Comments Sections */
  .single-page-comments-section {
    position: relative;
    background: var(--color-background);
    border-top: 1px solid var(--color-border);
    margin-top: 2rem;
  }

  .all-pages-comments-section {
    position: relative;
    background: var(--color-background);
    border-top: 1px solid var(--color-border);
    margin-top: 2rem;
  }

  .chapter-end-navigation {
    border-bottom: 1px solid var(--color-border);
    background: var(--color-background-soft);
    padding: 2rem 0;
  }

  /* Touch Zones - Completely invisible */
  .tap-zones-on-image {
    position: absolute;
    /* Dynamic positioning via JavaScript - will be set to exact image bounds */
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    pointer-events: none;
    z-index: 50;
  }

  /* All pages view tap zones - cover the full image */
  .tap-zones-all-pages {
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    position: absolute !important; /* Override dynamic positioning */
  }

  .tap-zone {
    pointer-events: auto;
    cursor: pointer;
    background: transparent;
    border: none;
    padding: 0;
    margin: 0;
    position: relative;
  }

  /* Debug Mode Visualization */
  .chapter-container.debug-mode .tap-zone {
    border: 2px dashed rgba(255, 255, 255, 0.5);
    position: relative;
  }

  .chapter-container.debug-mode .tap-zone-left {
    background: rgba(255, 255, 0, 0.15);
  }

  .chapter-container.debug-mode .tap-zone-center {
    background: rgba(0, 255, 0, 0.15);
  }

  .chapter-container.debug-mode .tap-zone-right {
    background: rgba(0, 0, 255, 0.15);
  }

  .chapter-container.debug-mode .tap-zone::after {
    content: attr(data-zone);
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    color: white;
    font-weight: bold;
    font-size: 14px;
    text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.8);
    pointer-events: none;
    background: rgba(0, 0, 0, 0.7);
    padding: 0.5rem;
    border-radius: 0.25rem;
    white-space: nowrap;
    z-index: 10;
  }

  /* Static Navigation */
  .static-navigation {
    margin-top: 3rem;
    padding: 2rem 0;
  }

  .chapter-navigation-controls {
    display: flex;
    justify-content: center;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .nav-btn {
    padding: 0.75rem 1.5rem;
    border-radius: 0.5rem;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s ease;
    text-decoration: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border: none;
  }

    .nav-btn.prev-chapter,
    .nav-btn.next-chapter {
      background: var(--color-background-mute);
      color: var(--color-text);
      border: 1px solid var(--color-border);
    }

      .nav-btn.prev-chapter:hover,
      .nav-btn.next-chapter:hover {
        background: var(--color-background-soft);
        border-color: var(--color-accent);
      }

      .nav-btn.prev-chapter:disabled,
      .nav-btn.next-chapter:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

    .nav-btn.back-to-title {
      background: var(--color-accent);
      color: white;
      border: 1px solid var(--color-accent);
    }

      .nav-btn.back-to-title:hover {
        background: var(--color-accent-hover);
      }

  /* Loading and Error States */
  .loading-container,
  .error-container,
  .no-images-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 2rem;
  }

  .loading-spinner,
  .error-content,
  .no-images-content {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    max-width: 400px;
  }

  /* Popups */
  .popup {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    backdrop-filter: blur(4px);
    z-index: 2000;
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 1rem;
  }

  .popup__content {
    background: var(--color-background-soft);
    border: 1px solid var(--color-border);
    border-radius: 1rem;
    max-width: 500px;
    width: 100%;
    max-height: 80vh;
    display: flex;
    flex-direction: column;
  }

    .popup__content.scrollable {
      overflow-y: auto;
    }

  .popup__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1.5rem;
    border-bottom: 1px solid var(--color-border);
  }

    .popup__header h3 {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--color-text);
    }

  .close-btn {
    padding: 0.5rem;
    background: transparent;
    border: none;
    color: var(--color-text);
    cursor: pointer;
    border-radius: 0.375rem;
    transition: background-color 0.2s ease;
  }

    .close-btn:hover {
      background: var(--color-background-mute);
    }

  /* Chapter List */
  .chapter-list {
    padding: 0;
    max-height: 60vh;
    overflow-y: auto;
  }

  .chapter-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem 1.5rem;
    cursor: pointer;
    border-bottom: 1px solid var(--color-border);
    transition: background-color 0.2s ease;
  }

    .chapter-item:hover {
      background: var(--color-background-mute);
    }

    .chapter-item.active {
      background: var(--color-accent);
      color: white;
    }

    .chapter-item:last-child {
      border-bottom: none;
    }

  .chapter-title {
    font-weight: 500;
    margin-bottom: 0.25rem;
  }

  .chapter-team {
    font-size: 0.875rem;
    opacity: 0.75;
  }

  /* Settings Content */
  .settings-content {
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
  }

  .settings-section {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

    .settings-section.toggle-section {
      flex-direction: row;
      justify-content: space-between;
      align-items: center;
    }

  .settings-label {
    font-weight: 500;
    color: var(--color-text);
    font-size: 0.9rem;
  }

  .settings-options {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
  }

  .settings-btn {
    padding: 0.5rem 1rem;
    background: var(--color-background-mute);
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    color: var(--color-text);
    cursor: pointer;
    transition: all 0.2s ease;
    font-size: 0.875rem;
  }

    .settings-btn:hover {
      background: var(--color-background-soft);
      border-color: var(--color-accent);
    }

    .settings-btn.active {
      background: var(--color-accent);
      color: white;
      border-color: var(--color-accent);
    }

  .settings-slider {
    width: 100%;
  }

  .slider {
    width: 100%;
    height: 4px;
    background: var(--color-background-mute);
    border-radius: 2px;
    outline: none;
    -webkit-appearance: none;
  }

    .slider::-webkit-slider-thumb {
      -webkit-appearance: none;
      width: 16px;
      height: 16px;
      background: var(--color-accent);
      border-radius: 50%;
      cursor: pointer;
    }

    .slider::-moz-range-thumb {
      width: 16px;
      height: 16px;
      background: var(--color-accent);
      border-radius: 50%;
      cursor: pointer;
      border: none;
    }

  /* Toggle Switch */
  .toggle-switch {
    position: relative;
    width: 44px;
    height: 24px;
  }

    .toggle-switch input {
      opacity: 0;
      width: 0;
      height: 0;
    }

    .toggle-switch label {
      position: absolute;
      cursor: pointer;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: var(--color-background-mute);
      border: 1px solid var(--color-border);
      border-radius: 12px;
      transition: all 0.3s ease;
    }

      .toggle-switch label::before {
        position: absolute;
        content: "";
        height: 16px;
        width: 16px;
        left: 3px;
        top: 3px;
        background: white;
        border-radius: 50%;
        transition: all 0.3s ease;
      }

    .toggle-switch input:checked + label {
      background: var(--color-accent);
      border-color: var(--color-accent);
    }

      .toggle-switch input:checked + label::before {
        transform: translateX(20px);
      }

  /* Hint Display */
  .manga-reader-hint {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: rgba(0, 0, 0, 0.8);
    color: white;
    padding: 0.75rem 1.5rem;
    border-radius: 0.5rem;
    font-size: 0.875rem;
    font-weight: 500;
    z-index: 1500;
    transition: opacity 0.3s ease;
    pointer-events: none;
  }

  /* FIXED: Mobile Responsive with proper spacing */
  @media (max-width: 768px) {
    .navbar-content {
      padding: 0.5rem;
      flex-wrap: wrap;
      gap: 0.5rem;
      min-height: 50px; /* Smaller navbar on mobile */
    }

    .back-text {
      display: none;
    }

    .chapter-info {
      order: 3;
      width: 100%;
      text-align: center;
    }

    .title-name {
      font-size: 1rem;
    }

    .chapter-name {
      font-size: 0.875rem;
    }

    /* FIXED: Mobile content area spacing */
    .content-area {
      padding-top: 60px; /* Adjusted for mobile navbar */
      padding-left: 0;
      padding-right: 0;
    }

    .all-pages-container {
      padding-top: 60px; /* Adjusted for mobile navbar */
      padding-left: 0;
      padding-right: 0;
    }

    .popup__content {
      margin: 0.5rem;
      max-height: 90vh;
    }

    .chapter-navigation-controls {
      flex-direction: column;
      align-items: stretch;
    }

    .nav-btn {
      width: 100%;
    }

    .settings-options {
      flex-direction: column;
    }

    .settings-btn {
      width: 100%;
      text-align: center;
    }
  }

  @media (max-width: 480px) {
    .navbar-content {
      padding: 0.25rem;
    }
  }

</style>
