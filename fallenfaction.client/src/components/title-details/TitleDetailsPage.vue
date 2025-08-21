<!-- TitleDetailsPage.vue - Updated to support guest users -->
<template>
  <div class="min-h-screen bg-[var(--color-background)]">
    <!-- Loading State -->
    <div v-if="loading" class="flex items-center justify-center min-h-screen">
      <div class="text-center">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-[var(--color-accent)]" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-[var(--color-text)] text-lg">Loading title details...</span>
        </div>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="flex items-center justify-center min-h-screen px-4">
      <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)] p-6 max-w-md w-full text-center">
        <div class="text-red-500 mb-4">
          <svg class="h-16 w-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
          </svg>
        </div>
        <h2 class="text-xl font-semibold text-[var(--color-text)] mb-2">{{ getErrorTitle() }}</h2>
        <p class="text-[var(--color-text)] opacity-75 mb-4">{{ error }}</p>
        <div class="flex space-x-3 justify-center">
          <button @click="retryLoad"
                  class="px-4 py-2 bg-[var(--color-accent)] text-white rounded-md hover:bg-[var(--color-accent-hover)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] transition-colors duration-200">
            Try Again
          </button>
          <router-link to="/"
                       class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-md hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Go Home
          </router-link>
        </div>
      </div>
    </div>

    <!-- Title Details Content -->
    <div v-else-if="titleData" class="relative">
      <!-- Background Image -->
      <div v-if="titleData.backgroundImagePath"
           class="absolute inset-0 h-[60vh] bg-cover bg-center bg-no-repeat"
           :style="{ backgroundImage: `url(${getImageUrl(titleData.backgroundImagePath)})` }">
        <div class="absolute inset-0 bg-gradient-to-b from-black/30 via-black/70 to-[var(--color-background)]"></div>
      </div>

      <!-- Main Content Grid -->
      <div class="relative z-10">
        <!-- Mobile Layout -->
        <div class="lg:hidden">
          <!-- Cover Image Section -->
          <div class="flex justify-center pt-8 pb-6 px-4">
            <div class="relative">
              <div class="w-48 h-72 rounded-xl overflow-hidden shadow-2xl border border-[var(--color-border)]">
                <img :src="getImageUrl(titleData.coverImagePath)"
                     :alt="titleData.originalTitle"
                     class="w-full h-full object-cover bg-[var(--color-background-mute)]"
                     @error="onCoverImageError"
                     @load="onCoverImageLoad" />
              </div>

              <!-- Type Badge -->
              <div class="absolute top-3 right-3 bg-black/80 text-white px-2 py-1 rounded-md text-xs font-medium backdrop-blur-sm">
                {{ getMangaType(titleData.type) }}
              </div>

              <!-- Action Dropdown - Only show for authenticated users -->
              <div v-if="isAuthenticated" class="absolute top-3 left-3">
                <div class="relative" ref="actionDropdownRef">
                  <button @click="showActionDropdown = !showActionDropdown"
                          class="w-8 h-8 bg-black/80 text-white rounded-full flex items-center justify-center backdrop-blur-sm hover:bg-black/90 focus:outline-none transition-colors duration-200">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01"></path>
                    </svg>
                  </button>

                  <!-- Dropdown Menu -->
                  <div v-if="showActionDropdown"
                       class="absolute top-10 left-0 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-lg min-w-48 py-2 z-20">
                    <a :href="`/${titleData.originalTitle}/AddChapter`"
                       class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                      <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                      </svg>
                      Add Chapter
                    </a>
                    <a :href="`/Title/Edit/${titleData.id}`"
                       class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                      <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                      </svg>
                      Edit
                    </a>
                    <a href="#"
                       class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                      <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
                      </svg>
                      Report
                    </a>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Title Header -->
          <div class="text-center px-4 pb-6">
            <h1 class="text-2xl font-bold text-[var(--color-text)] mb-2 leading-tight">{{ titleData.englishTitle }}</h1>
            <h2 v-if="titleData.originalTitle !== titleData.englishTitle"
                class="text-lg text-[var(--color-text)] opacity-75 font-medium">
              {{ titleData.originalTitle }}
            </h2>
          </div>

          <!-- Rating Section -->
          <div class="mx-4 mb-6 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
            <div class="flex items-center justify-center space-x-4">
              <div class="flex items-center space-x-2">
                <svg class="w-5 h-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                </svg>
                <span class="text-2xl font-bold text-[var(--color-text)]">{{ titleData.averageRating?.toFixed(1) || '0.0' }}</span>
                <span class="text-sm text-[var(--color-text)] opacity-75">({{ titleData.ratingCount || 0 }})</span>
              </div>
              <button @click="showRatingModal = true"
                      :disabled="!isAuthenticated"
                      class="px-4 py-2 bg-[var(--color-background-mute)] border border-[var(--color-accent)] text-[var(--color-accent)] rounded-lg hover:bg-[var(--color-accent)] hover:text-white focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                <svg class="w-4 h-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"></path>
                </svg>
                {{ isAuthenticated ? 'Rate' : 'Sign in to Rate' }}
              </button>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="px-4 mb-6 space-y-3">
            <!-- Read Button -->
            <router-link v-if="titleData.chapterCount > 0"
                         :to="getFirstChapterUrl()"
                         class="w-full bg-gradient-to-r from-orange-500 to-orange-600 text-white py-3 px-4 rounded-xl font-semibold text-center transition-all duration-200 hover:from-orange-600 hover:to-orange-700 hover:shadow-lg transform hover:-translate-y-0.5 flex items-center justify-center space-x-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
              </svg>
              <span>Start Reading</span>
            </router-link>

            <!-- Continue Reading Button (only for authenticated users with bookmarks) -->
            <router-link v-if="isAuthenticated && userBookmark && userBookmark.lastReadChapter > 0"
                         :to="getContinueReadingUrl()"
                         class="w-full bg-green-600 text-white py-3 px-4 rounded-xl font-semibold text-center transition-all duration-200 hover:bg-green-700 hover:shadow-lg transform hover:-translate-y-0.5 flex items-center justify-center space-x-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"></path>
              </svg>
              <span>Continue (Ch. {{ userBookmark.lastReadChapter }})</span>
            </router-link>

            <!-- Bookmark Component - Only for authenticated users -->
            <div v-if="isAuthenticated">
              <BookmarkDropdown :title-id="titleData.id"
                                @bookmark-changed="onBookmarkChanged"
                                @bookmark-loaded="onBookmarkLoaded" />
            </div>
            <div v-else class="w-full">
              <button @click="goToLogin"
                      class="w-full bg-[var(--color-background-mute)] border border-[var(--color-border)] text-[var(--color-text)] py-3 px-4 rounded-xl font-medium text-center transition-all duration-200 hover:bg-[var(--color-background-soft)] hover:border-[var(--color-accent)] flex items-center justify-center space-x-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"></path>
                </svg>
                <span>Sign in to bookmark</span>
              </button>
            </div>
          </div>

          <!-- Sidebar Info Cards -->
          <div class="px-4 space-y-4 mb-6">
            <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
              <div class="grid grid-cols-2 gap-4">
                <div class="text-center">
                  <div class="text-xs text-[var(--color-text)] opacity-60 uppercase tracking-wide mb-1">Type</div>
                  <div class="font-medium text-[var(--color-text)]">{{ getMangaType(titleData.type) }}</div>
                </div>
                <div class="text-center">
                  <div class="text-xs text-[var(--color-text)] opacity-60 uppercase tracking-wide mb-1">Release</div>
                  <div class="font-medium text-[var(--color-text)]">{{ titleData.releaseDate || 'Unknown' }}</div>
                </div>
                <div class="text-center">
                  <div class="text-xs text-[var(--color-text)] opacity-60 uppercase tracking-wide mb-1">Chapters</div>
                  <div class="font-medium text-[var(--color-text)]">{{ titleData.chapterCount || 0 }}</div>
                </div>
                <div class="text-center">
                  <div class="text-xs text-[var(--color-text)] opacity-60 uppercase tracking-wide mb-1">Status</div>
                  <div class="font-medium text-[var(--color-text)]">{{ titleData.statusTitle || 'Unknown' }}</div>
                </div>
              </div>
            </div>

            <!-- Quick Stats -->
            <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4">
              <div class="grid grid-cols-3 gap-4 text-center">
                <div>
                  <div class="text-lg font-bold text-[var(--color-text)]">{{ formatNumber(titleData.viewCount) || 0 }}</div>
                  <div class="text-xs text-[var(--color-text)] opacity-60">Views</div>
                </div>
                <div>
                  <div class="text-lg font-bold text-[var(--color-text)]">{{ titleData.bookmarkCount || 0 }}</div>
                  <div class="text-xs text-[var(--color-text)] opacity-60">Bookmarks</div>
                </div>
                <div>
                  <div class="text-lg font-bold text-[var(--color-text)]">{{ titleData.ratingCount || 0 }}</div>
                  <div class="text-xs text-[var(--color-text)] opacity-60">Ratings</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Desktop Layout -->
        <div class="hidden lg:block">
          <div class="max-w-6xl mx-auto px-4 py-8">
            <div class="grid grid-cols-12 gap-8">
              <!-- Cover Image Column -->
              <div class="col-span-3">
                <div class="sticky top-8">
                  <div class="relative">
                    <div class="w-full h-96 rounded-xl overflow-hidden shadow-2xl border border-[var(--color-border)]">
                      <img :src="getImageUrl(titleData.coverImagePath)"
                           :alt="titleData.originalTitle"
                           class="w-full h-full object-cover bg-[var(--color-background-mute)]"
                           @error="onCoverImageError"
                           @load="onCoverImageLoad" />
                    </div>

                    <!-- Type Badge -->
                    <div class="absolute top-4 right-4 bg-black/80 text-white px-3 py-1 rounded-lg text-sm font-medium backdrop-blur-sm">
                      {{ getMangaType(titleData.type) }}
                    </div>

                    <!-- Action Dropdown - Only for authenticated users -->
                    <div v-if="isAuthenticated" class="absolute top-4 left-4">
                      <div class="relative" ref="actionDropdownRef">
                        <button @click="showActionDropdown = !showActionDropdown"
                                class="w-10 h-10 bg-black/80 text-white rounded-full flex items-center justify-center backdrop-blur-sm hover:bg-black/90 focus:outline-none transition-colors duration-200">
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01"></path>
                          </svg>
                        </button>

                        <!-- Dropdown Menu -->
                        <div v-if="showActionDropdown"
                             class="absolute top-12 left-0 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-lg min-w-48 py-2 z-20">
                          <a :href="`/${titleData.originalTitle}/AddChapter`"
                             class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                            <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                            </svg>
                            Add Chapter
                          </a>
                          <a :href="`/Title/Edit/${titleData.id}`"
                             class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                            <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                            </svg>
                            Edit
                          </a>
                          <a href="#"
                             class="flex items-center px-4 py-2 text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors duration-150">
                            <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
                            </svg>
                            Report
                          </a>
                        </div>
                      </div>
                    </div>
                  </div>

                  <!-- Action Buttons -->
                  <div class="mt-6 space-y-3">
                    <!-- Read Button -->
                    <router-link v-if="titleData.chapterCount > 0"
                                 :to="getFirstChapterUrl()"
                                 class="w-full bg-gradient-to-r from-orange-500 to-orange-600 text-white py-3 px-4 rounded-xl font-semibold text-center transition-all duration-200 hover:from-orange-600 hover:to-orange-700 hover:shadow-lg transform hover:-translate-y-0.5 flex items-center justify-center space-x-2">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
                      </svg>
                      <span>Start Reading</span>
                    </router-link>

                    <!-- Continue Reading Button (only for authenticated users with bookmarks) -->
                    <router-link v-if="isAuthenticated && userBookmark && userBookmark.lastReadChapter > 0"
                                 :to="getContinueReadingUrl()"
                                 class="w-full bg-green-600 text-white py-3 px-4 rounded-xl font-semibold text-center transition-all duration-200 hover:bg-green-700 hover:shadow-lg transform hover:-translate-y-0.5 flex items-center justify-center space-x-2">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"></path>
                      </svg>
                      <span>Continue (Ch. {{ userBookmark.lastReadChapter }})</span>
                    </router-link>

                    <!-- Bookmark Component - Only for authenticated users -->
                    <div v-if="isAuthenticated">
                      <BookmarkDropdown :title-id="titleData.id"
                                        @bookmark-changed="onBookmarkChanged"
                                        @bookmark-loaded="onBookmarkLoaded" />
                    </div>
                    <div v-else class="w-full">
                      <button @click="goToLogin"
                              class="w-full bg-[var(--color-background-mute)] border border-[var(--color-border)] text-[var(--color-text)] py-3 px-4 rounded-xl font-medium text-center transition-all duration-200 hover:bg-[var(--color-background-soft)] hover:border-[var(--color-accent)] flex items-center justify-center space-x-2">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"></path>
                        </svg>
                        <span>Sign in to bookmark</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Main Content Column -->
              <div class="col-span-6">
                <!-- Title Header -->
                <div class="mb-8 text-center lg:text-left">
                  <h1 class="text-4xl font-bold text-[var(--color-text)] mb-3 leading-tight">{{ titleData.englishTitle }}</h1>
                  <h2 v-if="titleData.originalTitle !== titleData.englishTitle"
                      class="text-2xl text-[var(--color-text)] opacity-75 font-medium">
                    {{ titleData.originalTitle }}
                  </h2>
                </div>

                <!-- Quick Stats -->
                <div class="grid grid-cols-4 gap-4 mb-8">
                  <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4 text-center">
                    <div class="flex items-center justify-center mb-2">
                      <svg class="w-5 h-5 text-yellow-400 mr-2" fill="currentColor" viewBox="0 0 20 20">
                        <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                      </svg>
                      <span class="text-lg font-bold text-[var(--color-text)]">{{ titleData.averageRating?.toFixed(1) || 'N/A' }}</span>
                    </div>
                    <div class="text-xs text-[var(--color-text)] opacity-60">{{ titleData.ratingCount || 0 }} ratings</div>
                  </div>
                  <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4 text-center">
                    <div class="flex items-center justify-center mb-2">
                      <svg class="w-5 h-5 text-blue-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"></path>
                      </svg>
                      <span class="text-lg font-bold text-[var(--color-text)]">{{ titleData.chapterCount || 0 }}</span>
                    </div>
                    <div class="text-xs text-[var(--color-text)] opacity-60">chapters</div>
                  </div>
                  <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4 text-center">
                    <div class="flex items-center justify-center mb-2">
                      <svg class="w-5 h-5 text-green-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"></path>
                      </svg>
                      <span class="text-lg font-bold text-[var(--color-text)]">{{ titleData.bookmarkCount || 0 }}</span>
                    </div>
                    <div class="text-xs text-[var(--color-text)] opacity-60">bookmarks</div>
                  </div>
                  <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4 text-center">
                    <div class="flex items-center justify-center mb-2">
                      <svg class="w-5 h-5 text-purple-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                      </svg>
                      <span class="text-lg font-bold text-[var(--color-text)]">{{ formatNumber(titleData.viewCount) || 0 }}</span>
                    </div>
                    <div class="text-xs text-[var(--color-text)] opacity-60">views</div>
                  </div>
                </div>

                <!-- Rating Section -->
                <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6 mb-8">
                  <div class="flex items-center justify-between">
                    <div class="flex items-center space-x-4">
                      <div class="flex items-center space-x-2">
                        <svg class="w-6 h-6 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                          <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                        </svg>
                        <span class="text-3xl font-bold text-[var(--color-text)]">{{ titleData.averageRating?.toFixed(1) || '0.0' }}</span>
                        <span class="text-[var(--color-text)] opacity-75">({{ titleData.ratingCount || 0 }})</span>
                      </div>
                    </div>
                    <button @click="isAuthenticated ? (showRatingModal = true) : goToLogin()"
                            class="px-6 py-3 bg-[var(--color-background-mute)] border border-[var(--color-accent)] text-[var(--color-accent)] rounded-lg hover:bg-[var(--color-accent)] hover:text-white focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] transition-all duration-200">
                      <svg class="w-5 h-5 inline mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"></path>
                      </svg>
                      {{ isAuthenticated ? 'Rate' : 'Sign in to Rate' }}
                    </button>
                  </div>
                </div>

                <!-- Tabs Content -->
                <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl overflow-hidden">
                  <TitleDetailsTabs :title-id="titleData.id"
                                    :title-data="titleData"
                                    :initial-tab="initialTab"
                                    :is-authenticated="isAuthenticated"
                                    @tab-changed="onTabChanged" />
                </div>
              </div>

              <!-- Sidebar Column -->
              <div class="col-span-3">
                <div class="sticky top-8">
                  <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-4 space-y-4">
                    <!-- Info Items -->
                    <div class="pb-3 border-b border-[var(--color-border)] last:border-b-0 last:pb-0"
                         v-for="(info, index) in sidebarInfo" :key="index">
                      <div class="text-xs text-[var(--color-text)] opacity-60 uppercase tracking-wide mb-1">{{ info.label }}</div>
                      <div class="font-medium text-[var(--color-text)]">{{ info.value }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Rating Modal - Only show for authenticated users -->
    <div v-if="showRatingModal && isAuthenticated"
         class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-4"
         @click="closeRatingModal">
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl max-w-md w-full shadow-2xl"
           @click.stop>
        <div class="p-6 border-b border-[var(--color-border)]">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold text-[var(--color-text)]">Rate {{ titleData.originalTitle }}</h3>
            <button @click="closeRatingModal"
                    class="w-8 h-8 text-[var(--color-text)] opacity-60 hover:opacity-100 rounded-lg hover:bg-[var(--color-background-mute)] flex items-center justify-center transition-all duration-200">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
              </svg>
            </button>
          </div>
        </div>
        <div class="p-6">
          <div class="text-center">
            <div class="flex justify-center space-x-2 mb-4">
              <button v-for="star in 10"
                      :key="star"
                      @click="setRating(star)"
                      @mouseover="hoverRating = star"
                      @mouseleave="hoverRating = 0"
                      class="w-8 h-8 transition-all duration-200 transform hover:scale-110"
                      :class="{
                      'text-yellow-400' : star <= (hoverRating || selectedRating),
                        'text-gray-300': star > (hoverRating || selectedRating)
                      }">
                <svg class="w-full h-full" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                </svg>
              </button>
            </div>
            <p class="text-[var(--color-text)] font-medium mb-6 min-h-6">
              {{ getRatingText(hoverRating || selectedRating) }}
            </p>
          </div>
        </div>
        <div class="p-6 border-t border-[var(--color-border)] flex space-x-3">
          <button @click="submitRating"
                  :disabled="!selectedRating || submittingRating"
                  class="flex-1 px-4 py-2 bg-[var(--color-accent)] text-white rounded-lg hover:bg-[var(--color-accent-hover)] focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-200">
            <span v-if="submittingRating" class="inline-flex items-center">
              <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Submitting...
            </span>
            <span v-else>Submit Rating</span>
          </button>
          <button @click="closeRatingModal"
                  class="px-4 py-2 bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-background-soft)] transition-colors duration-200">
            Cancel
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
  import { useRoute, useRouter } from 'vue-router'
  import { titleDetailsService } from '../../services/titleDetailsService'
  import TitleDetailsTabs from './TitleDetailsTabs.vue'
  import BookmarkDropdown from './BookmarkDropdown.vue'

  // Props
  const props = defineProps({
    titleName: {
      type: String,
      required: true
    }
  })

  // Router
  const route = useRoute()
  const router = useRouter()

  // Reactive data
  const titleData = ref(null)
  const loading = ref(true)
  const error = ref(null)
  const initialTab = ref('info')
  const userBookmark = ref(null)
  const isAuthenticated = ref(false)

  // Rating modal
  const showRatingModal = ref(false)
  const selectedRating = ref(0)
  const hoverRating = ref(0)
  const submittingRating = ref(false)

  // Action dropdown
  const showActionDropdown = ref(false)
  const actionDropdownRef = ref(null)

  // Computed properties
  const sidebarInfo = computed(() => {
    if (!titleData.value) return []

    const info = [
      { label: 'Type', value: getMangaType(titleData.value.type) },
      { label: 'Release', value: titleData.value.releaseDate || 'Unknown' },
      { label: 'Chapters', value: titleData.value.chapterCount || 0 },
      { label: 'Status', value: titleData.value.statusTitle || 'Unknown' },
      { label: 'Translation', value: titleData.value.statusTranslation || 'Unknown' }
    ]

    // Add authors if available
    if (titleData.value.authors && titleData.value.authors.length > 0) {
      info.push({
        label: 'Author',
        value: titleData.value.authors.map(a => a.name || a).join(', ')
      })
    }

    // Add artists if available
    if (titleData.value.artists && titleData.value.artists.length > 0) {
      info.push({
        label: 'Artist',
        value: titleData.value.artists.map(a => a.name || a).join(', ')
      })
    }

    return info
  })

  // Methods
  const loadTitleData = async () => {
    loading.value = true
    error.value = null

    try {
      console.log('Loading title data for:', props.titleName)

      const result = await titleDetailsService.getTitleDetails(props.titleName)

      if (result.success && result.data) {
        titleData.value = result.data

        // Update page title
        document.title = `${titleData.value.originalTitle} - FallenFaction`

        // Only load user-specific data if authenticated
        if (isAuthenticated.value) {
          await loadUserBookmark()
        }

        console.log('Title data loaded successfully:', titleData.value)
      } else {
        error.value = result.error || 'Title not found'
        console.error('Failed to load title data:', result.error)
      }
    } catch (err) {
      error.value = 'Failed to load title details'
      console.error('Error loading title data:', err)
    } finally {
      loading.value = false
    }
  }

  // FIXED: Only load bookmark data for authenticated users
  const loadUserBookmark = async () => {
    if (!isAuthenticated.value || !titleData.value) return

    try {
      console.log('Loading user bookmark for title:', titleData.value.id)
      // Implementation depends on your bookmark API - only call if user is authenticated
    } catch (err) {
      console.error('Error loading user bookmark:', err)
      // Don't let bookmark errors affect the main page load
    }
  }

  const retryLoad = async () => {
    await loadTitleData()
  }

  // FIXED: Check auth status without throwing errors
  const checkAuthStatus = () => {
    try {
      const token = localStorage.getItem('authToken')
      const user = localStorage.getItem('authUser')
      isAuthenticated.value = !!(token && user)
      console.log('Auth status check:', { isAuthenticated: isAuthenticated.value })
    } catch (err) {
      console.error('Error checking auth status:', err)
      isAuthenticated.value = false
    }
  }

  const onTabChanged = (tabKey) => {
    const url = new URL(window.location)
    url.searchParams.set('section', tabKey)
    window.history.replaceState({}, '', url)
  }

  const onBookmarkChanged = (bookmarkData) => {
    console.log('Bookmark changed:', bookmarkData)
    if (titleData.value) {
      if (bookmarkData.action === 'added') {
        titleData.value.bookmarkCount = (titleData.value.bookmarkCount || 0) + 1
      } else if (bookmarkData.action === 'removed') {
        titleData.value.bookmarkCount = Math.max((titleData.value.bookmarkCount || 1) - 1, 0)
      }
    }
  }

  const onBookmarkLoaded = (bookmarkData) => {
    userBookmark.value = bookmarkData.isBookmarked ? bookmarkData.currentBookmark : null
  }

  // Rating methods
  const setRating = (rating) => {
    selectedRating.value = rating
  }

  const submitRating = async () => {
    if (!selectedRating.value || submittingRating.value || !isAuthenticated.value) return

    submittingRating.value = true
    try {
      const result = await titleDetailsService.rateTitle(titleData.value.id, selectedRating.value)

      if (result.success) {
        showToast('Rating submitted successfully!', 'success')
        closeRatingModal()

        if (result.data) {
          titleData.value.averageRating = result.data.averageRating
          titleData.value.ratingCount = result.data.totalRatings
        }
      } else {
        showToast(result.error || 'Failed to submit rating', 'error')
      }
    } catch (err) {
      showToast('Failed to submit rating', 'error')
      console.error('Error submitting rating:', err)
    } finally {
      submittingRating.value = false
    }
  }

  const closeRatingModal = () => {
    showRatingModal.value = false
    selectedRating.value = 0
    hoverRating.value = 0
  }

  const getRatingText = (rating) => {
    const texts = [
      '', 'Terrible', 'Very Bad', 'Bad', 'Poor', 'Average',
      'Good', 'Very Good', 'Great', 'Excellent', 'Masterpiece'
    ]
    return texts[rating] || ''
  }

  // Helper methods
  const getImageUrl = (imagePath) => {
    return titleDetailsService.getImageUrl(imagePath)
  }

  const getMangaType = (type) => {
    const types = {
      0: 'Manga',
      1: 'Manhwa',
      2: 'Manhua',
      3: 'Comic',
      4: 'Novel'
    }
    return types[type] || 'Manga'
  }

  const formatNumber = (num) => {
    if (!num) return '0'
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M'
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K'
    return num.toString()
  }

  const getFirstChapterUrl = () => {
    if (!titleData.value || !titleData.value.chapterCount) return '#'
    return `/${titleData.value.originalTitle}/chapter/1`
  }

  const getContinueReadingUrl = () => {
    if (!userBookmark.value) return '#'
    return `/${titleData.value.originalTitle}/chapter/${userBookmark.value.lastReadChapter}`
  }

  const getErrorTitle = () => {
    if (error.value?.includes('not found') || error.value?.includes('404')) {
      return 'Title Not Found'
    }
    if (error.value?.includes('permission') || error.value?.includes('403')) {
      return 'Access Denied'
    }
    return 'Error Loading Title'
  }

  const goToLogin = () => {
    const returnUrl = encodeURIComponent(route.fullPath)
    router.push(`/account/login?returnUrl=${returnUrl}`)
  }

  const onCoverImageError = (event) => {
    console.error('Cover image failed to load:', event.target.src)
    event.target.src = titleDetailsService.getImageUrl('/img/default-cover.png')
  }

  const onCoverImageLoad = () => {
    console.log('Cover image loaded successfully')
  }

  // Handle click outside for dropdown
  const handleClickOutside = (event) => {
    if (showActionDropdown.value && actionDropdownRef.value && !actionDropdownRef.value.contains(event.target)) {
      showActionDropdown.value = false
    }
  }

  // Toast notification function
  const showToast = (message, type = 'info') => {
    let toastContainer = document.getElementById('toast-container')
    if (!toastContainer) {
      toastContainer = document.createElement('div')
      toastContainer.id = 'toast-container'
      toastContainer.className = 'fixed bottom-4 right-4 z-50 space-y-2'
      document.body.appendChild(toastContainer)
    }

    const toast = document.createElement('div')
    const bgColor = type === 'success' ? 'bg-green-500' : type === 'error' ? 'bg-red-500' : 'bg-blue-500'
    toast.className = `${bgColor} text-white px-4 py-3 rounded-lg shadow-lg max-w-sm transform transition-all duration-300 translate-x-full opacity-0`
    toast.textContent = message

    toastContainer.appendChild(toast)

    // Trigger animation
    nextTick(() => {
      toast.classList.remove('translate-x-full', 'opacity-0')
    })

    // Remove after 3 seconds
    setTimeout(() => {
      toast.classList.add('translate-x-full', 'opacity-0')
      setTimeout(() => {
        if (toast.parentNode) {
          toast.remove()
        }
        if (toastContainer.children.length === 0) {
          toastContainer.remove()
        }
      }, 300)
    }, 3000)
  }

  // Lifecycle hooks
  onMounted(async () => {
    // Get initial tab from URL query
    initialTab.value = route.query.section || 'info'

    // Check authentication status FIRST before loading anything else
    checkAuthStatus()

    // Load title data (this should work for both authenticated and guest users)
    await loadTitleData()

    // Add click outside listener
    document.addEventListener('click', handleClickOutside)
  })

  onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside)
  })

  // Watch for route changes
  import { watch } from 'vue'
  watch(() => props.titleName, async (newTitleName) => {
    if (newTitleName) {
      await loadTitleData()
    }
  })

  watch(() => route.query.section, (newTab) => {
    if (newTab) {
      initialTab.value = newTab
    }
  })
</script>
