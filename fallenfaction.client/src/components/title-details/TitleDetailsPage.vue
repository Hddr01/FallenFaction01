<!-- TitleDetailsPage.vue - Complete with shadcn-vue buttons -->
<template>
  <div class="min-h-screen bg-[var(--color-background)]">
    <!-- Error State -->
    <div v-if="!loading && error" class="flex items-center justify-center min-h-screen px-4">
      <div class="bg-[var(--color-background)] rounded-lg shadow-md border border-[var(--color-border)] p-6 max-w-md w-full text-center">
        <div class="text-red-500 mb-4">
          <svg class="h-16 w-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
          </svg>
        </div>
        <h2 class="text-xl font-semibold text-[var(--color-text)] mb-2">{{ getErrorTitle() }}</h2>
        <p class="text-[var(--color-text)] opacity-75 mb-4">{{ error }}</p>
        <div class="flex space-x-3 justify-center">
          <Button size="sm" @click="retryLoad">
            Try Again
          </Button>
          <Button size="sm" variant="outline" @click="$router.push('/')">
            Go Home
          </Button>
        </div>
      </div>
    </div>

    <!-- Title Details Content -->
    <div v-if="!loading && !error && titleData" class="relative">
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
          <div class="flex justify-center pt-8">
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
                <DropdownMenu>
                  <DropdownMenuTrigger as-child>
                    <Button size="icon" variant="outline" class="w-8 h-8 bg-black/80 text-white border-none backdrop-blur-sm hover:bg-black/90">
                      <MoreHorizontalIcon class="w-4 h-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" class="w-48">
                    <DropdownMenuItem as-child>
                      <a :href="`/${titleData.originalTitle}/AddChapter`" class="flex items-center cursor-pointer">
                        <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                        </svg>
                        Add Chapter
                      </a>
                    </DropdownMenuItem>
                    <DropdownMenuItem as-child>
                      <a :href="`/Title/Edit/${titleData.id}`" class="flex items-center cursor-pointer">
                        <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                        </svg>
                        Edit
                      </a>
                    </DropdownMenuItem>
                    <DropdownMenuItem @click="viewChangeHistory">
                      <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                      </svg>
                      Change History
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem variant="destructive">
                      <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
                      </svg>
                      Report
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>
          </div>

          <!-- Title Header -->
          <div class="text-center">
            <!-- English Title with Rating Button -->
            <div class="flex items-center justify-center gap-2">
              <h1 class="text-2xl font-bold text-gray-200 leading-tight">{{ titleData.englishTitle }}</h1>

              <!-- Rating Button -->
              <button v-if="isAuthenticated"
                      @click="isRatingDialogOpen = true"
                      class="inline-flex items-center justify-center w-8 h-8 rounded-full bg-yellow-400/10 hover:bg-yellow-400/20 transition-colors"
                      title="Rate this title">
                <svg class="w-5 h-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                </svg>
              </button>
              <button v-else
                      @click="goToLogin"
                      class="inline-flex items-center justify-center w-8 h-8 rounded-full bg-yellow-400/10 hover:bg-yellow-400/20 transition-colors"
                      title="Sign in to rate">
                <svg class="w-5 h-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                </svg>
              </button>
              <div class="flex flex-col">
                <span class="text-2xl font-bold text-[var(--color-text)]">
                  {{ titleData.averageRating?.toFixed(1) || '0.0' }}
                </span>
                <span class="text-xs text-[var(--color-text)] opacity-75">
                  ({{ titleData.ratingCount || 0 }})
                </span>
              </div>
            </div>
            <!-- Original Title-->
            <h2 v-if="titleData.originalTitle !== titleData.englishTitle"
                class="text-lg text-[var(--color-text)] opacity-75 font-medium mb-3">
              {{ titleData.originalTitle }}
            </h2>
          </div>

          <!-- Action Buttons -->
          <div class="">
            <!-- Start Reading Button -->
            <Button v-if="canStartReading"
                    size="sm"
                    class="w-full bg-black hover:bg-gray-900 text-white reading-action-button"
                    @click="$router.push(getFirstChapterUrl())">
              <BookOpenIcon class="w-4 h-4 mr-2" />
              Start Reading
            </Button>


            <!-- Continue Reading Button -->
            <Button v-if="canContinueReading"
                    size="sm"
                    class="w-full bg-black hover:bg-gray-900 text-white reading-action-button"
                    @click="$router.push(getContinueReadingUrl())">
              <PlayIcon class="w-4 h-4 mr-2" />
              Continue (Ch. {{ userBookmark?.lastReadChapter || readingProgress?.lastReadChapter }})
            </Button>

            <!-- Bookmark ButtonGroup -->
            <ButtonGroup v-if="isAuthenticated" class="w-full bookmark-action-buttons">
              <Button size="sm"
                      variant="outline"
                      :class="[bookmarkButtonClass, 'border-0']"
                      class="flex-1"
                      @click="toggleBookmark">
                <BookmarkIcon v-if="!userBookmark" class="w-4 h-4 mr-2" />
                <BookmarkCheckIcon v-else class="w-4 h-4 mr-2" />
                {{ bookmarkStatusText }}
              </Button>

              <Drawer>
                <DrawerTrigger as-child>
                  <Button size="sm"
                          variant="outline"
                          :class="[bookmarkButtonClass, 'border-0']"
                          class="px-2">
                    <MoreHorizontalIcon class="w-4 h-4" />
                  </Button>
                </DrawerTrigger>

                <DrawerContent class="bg-black">
                  <div class="mx-auto w-full max-w-sm ">
                    <DrawerHeader>
                      <DrawerTitle class="text-[var(--color-white)] text-center">Bookmark Status</DrawerTitle>
                      <DrawerDescription class="text-center text-muted-foreground">
                        Change your reading status for this title.
                      </DrawerDescription>
                    </DrawerHeader>

                    <div class="text-center p-4 pb-0 space-y-2">
                      <Button variant="destructive"
                              class="w-full justify-center bg-[#141414] hover:text-white"
                              @click="changeBookmarkStatus('reading')">
                        Reading
                      </Button>

                      <Button variant="destructive"
                              class="w-full justify-center bg-[#141414] hover:text-white"
                              @click="changeBookmarkStatus('completed')">
                        Completed
                      </Button>

                      <Button variant="destructive"
                              class="w-full justify-center bg-[#141414] hover:text-white"
                              @click="changeBookmarkStatus('on-hold')">
                        On Hold
                      </Button>

                      <Button variant="destructive"
                              class="w-full justify-center bg-[#141414] hover:text-white"
                              @click="changeBookmarkStatus('plan-to-read')">
                        Plan to Read
                      </Button>

                      <Button variant="destructive"
                              class="w-full justify-center bg-[#141414] hover:text-white"
                              @click="changeBookmarkStatus('dropped')">
                        Dropped
                      </Button>

                      <!-- Custom (user-created) folders -->
                      <template v-if="userCustomFolders.length > 0">
                        <div class="border-t border-white/10 my-2"></div>
                        <p class="text-xs text-muted-foreground text-left px-1 pb-1">My Lists</p>
                        <Button v-for="folder in userCustomFolders"
                                :key="folder.id"
                                variant="destructive"
                                class="w-full justify-center bg-[#141414] hover:text-white"
                                @click="moveToCustomFolder(folder.id, folder.name)">
                          {{ folder.name }}
                        </Button>
                      </template>
                    </div>

                    <DrawerFooter>
                      <DrawerClose as-child>
                        <Button variant="destructive"
                                class=" w-full justify-center bg-[#e6e6e6] text-black hover:bg-gray-100 hover:text-black"
                                @click="removeBookmark">
                          Remove Bookmark
                        </Button>
                        <Button variant="destructive" class=" w-full justify-center bg-[#e6e6e6] text-black hover:bg-gray-100 hover:text-black">
                          Cancel
                        </Button>
                      </DrawerClose>
                    </DrawerFooter>
                  </div>
                </DrawerContent>
              </Drawer>
            </ButtonGroup>
            <Button v-else size="sm" variant="outline" class="w-full bg-black hover:bg-gray-900 text-white border-0" @click="goToLogin">
              <BookmarkIcon class="w-4 h-4 mr-2" />
              Sign in to bookmark
            </Button>
          </div>

          <!-- Sidebar Info Cards -->
          <!--<div class="px-4 space-y-4 mb-6">
                  <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-xl p-4">
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
                   Quick Stats
                  <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-xl p-4">
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
                </div>-->
          <div class="">
            <div class="bg-[var(--color-background)] overflow-hidden">
              <TitleDetailsTabs :title-id="titleData.id"
                                :title-data="titleData"
                                :initial-tab="initialTab"
                                :is-authenticated="isAuthenticated"
                                @tab-changed="onTabChanged" />
            </div>
          </div>
        </div>

        <!-- Desktop Layout -->
        <div class="hidden lg:block">
          <div class="flex justify-center items-start min-h-screen py-8">
            <div class="w-full max-w-7xl px-4">
              <!-- New Grid Layout: 5 columns, auto rows -->
              <div class="grid grid-cols-5 auto-rows-min gap-4">

                <!-- Block 9: Quick Actions / Stats (Top Left) -->
                <div class="row-span-1 col-start-1 row-start-1">
                </div>

                <!-- Block 1: Cover Image & Action Buttons -->
                <div class="col-start-1 row-start-2">
                  <div class="sticky top-8 space-y-3">
                    <div class="relative">
                      <div class="w-full rounded-xl overflow-hidden shadow-2xl border border-[var(--color-border)]">
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

                      <!-- Action Dropdown -->
                      <div v-if="isAuthenticated" class="absolute top-4 left-4">
                        <DropdownMenu>
                          <DropdownMenuTrigger as-child>
                            <Button size="icon" variant="outline" class="w-10 h-10 bg-black/80 text-white border-none backdrop-blur-sm hover:bg-black/90">
                              <MoreHorizontalIcon class="w-5 h-5" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="start" class="w-48">
                            <DropdownMenuItem as-child>
                              <a :href="`/${titleData.originalTitle}/AddChapter`" class="flex items-center cursor-pointer">
                                <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                                </svg>
                                Add Chapter
                              </a>
                            </DropdownMenuItem>
                            <DropdownMenuItem as-child>
                              <a :href="`/Title/Edit/${titleData.id}`" class="flex items-center cursor-pointer">
                                <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                                </svg>
                                Edit
                              </a>
                            </DropdownMenuItem>
                            <DropdownMenuItem @click="viewChangeHistory">
                              <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                              </svg>
                              Change History
                            </DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem variant="destructive">
                              <svg class="w-4 h-4 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L4.732 15.5c-.77.833.192 2.5 1.732 2.5z"></path>
                              </svg>
                              Report
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </div>

                    <!-- Start Reading Button -->
                    <Button v-if="canStartReading"
                            size="sm"
                            class="w-full bg-black hover:bg-gray-900 text-white border-0"
                            @click="$router.push(getFirstChapterUrl())">
                      <BookOpenIcon class="w-4 h-4 mr-2" />
                      Start Reading
                    </Button>

                    <!-- Continue Reading Button -->
                    <Button v-if="canContinueReading"
                            size="sm"
                            class="w-full bg-black hover:bg-gray-900 text-white border-0"
                            @click="$router.push(getContinueReadingUrl())">
                      <PlayIcon class="w-4 h-4 mr-2" />
                      Continue (Ch. {{ userBookmark?.lastReadChapter || readingProgress?.lastReadChapter }})
                    </Button>

                    <!-- Bookmark ButtonGroup -->
                    <ButtonGroup v-if="isAuthenticated" class="w-full">
                      <Button size="sm"
                              variant="outline"
                              :class="[bookmarkButtonClass, 'border-0']"
                              class="flex-1"
                              @click="toggleBookmark">
                        <BookmarkIcon v-if="!userBookmark" class="w-4 h-4 mr-2" />
                        <BookmarkCheckIcon v-else class="w-4 h-4 mr-2" />
                        {{ bookmarkStatusText }}
                      </Button>
                      <DropdownMenu>
                        <DropdownMenuTrigger as-child>
                          <Button size="sm" variant="outline" :class="[bookmarkButtonClass, 'border-0']" class="px-2">
                            <MoreHorizontalIcon class="w-4 h-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end" class="w-52">
                          <DropdownMenuItem @click="changeBookmarkStatus('reading')">
                            Reading
                          </DropdownMenuItem>
                          <DropdownMenuItem @click="changeBookmarkStatus('completed')">
                            Completed
                          </DropdownMenuItem>
                          <DropdownMenuItem @click="changeBookmarkStatus('on-hold')">
                            On Hold
                          </DropdownMenuItem>
                          <DropdownMenuItem @click="changeBookmarkStatus('plan-to-read')">
                            Plan to Read
                          </DropdownMenuItem>
                          <DropdownMenuItem @click="changeBookmarkStatus('dropped')">
                            Dropped
                          </DropdownMenuItem>
                          <!-- Custom (user-created) folders -->
                          <template v-if="userCustomFolders.length > 0">
                            <DropdownMenuSeparator />
                            <DropdownMenuItem v-for="folder in userCustomFolders"
                                              :key="folder.id"
                                              @click="moveToCustomFolder(folder.id, folder.name)">
                              {{ folder.name }}
                            </DropdownMenuItem>
                          </template>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem variant="destructive" @click="removeBookmark">
                            Remove Bookmark
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </ButtonGroup>
                    <Button v-else size="sm" variant="outline" class="w-full bg-black hover:bg-gray-900 text-white border-0" @click="goToLogin">
                      <BookmarkIcon class="w-4 h-4 mr-2" />
                      Sign in to bookmark
                    </Button>
                  </div>
                </div>

                <!-- Block 10: Reading Buttons -->
                <div class="row-span-2 row-start-9 space-y-3">

                </div>

                <!-- Block 2: Title Header & Description -->
                <div class="col-span-3 row-span-1 col-start-2 row-start-1">
                </div>

                <!-- Block 4: Rating Section with nested grid -->
                <div class="col-span-3 col-start-2 row-start-2 flex items-end">
                  <!-- Nested grid: 2 columns -->
                  <div class="grid grid-cols-7 gap-6 w-full">
                    <!-- Left side: Titles (3 columns) -->
                    <div class="col-span-3">
                      <h1 class="text-4xl font-bold text-[var(--color-text)] mb-2 text-gray-300">
                        {{ titleData.englishTitle }}
                      </h1>
                      <h2 v-if="titleData.originalTitle !== titleData.englishTitle"
                          class="text-2xl text-[var(--color-text)] opacity-75 font-medium">
                        {{ titleData.originalTitle }}
                      </h2>
                    </div>

                    <!-- Right side: Rating and Button (1 column) -->
                    <div class="col-span-1 col-start-7 flex-col justify-end space-y-1 space-x-1">
                      <!-- Rating display -->
                      <Dialog v-if="isAuthenticated" v-model:open="isRatingDialogOpen">
                        <DialogTrigger as-child>
                          <button class="flex items-center justify-end cursor-pointer hover:opacity-80 transition-opacity">
                            <div class="flex items-center space-x-2">
                              <svg class="w-6 h-6 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                                <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                              </svg>
                              <div class="flex flex-col">
                                <span class="text-2xl font-bold text-[var(--color-text)]">
                                  {{ titleData.averageRating?.toFixed(1) || '0.0' }}
                                </span>
                                <span class="text-xs text-[var(--color-text)] opacity-75">
                                  ({{ titleData.ratingCount || 0 }})
                                </span>
                              </div>
                            </div>
                          </button>
                        </DialogTrigger>

                        <DialogContent class="w-[calc(100vw-2rem)] max-w-xs sm:max-w-sm bg-black border-white/10 p-0 overflow-hidden">
                          <DialogHeader class="px-5 pt-5 pb-3">
                            <DialogTitle class="text-[var(--color-white)] text-center text-base">Rate this title</DialogTitle>
                            <DialogDescription class="text-center text-muted-foreground text-xs">
                              Share your rating with the community
                            </DialogDescription>
                          </DialogHeader>

                          <!-- Loading existing rating spinner -->
                          <div v-if="loadingExistingRating" class="px-5 pb-4 flex justify-center py-6">
                            <svg class="animate-spin w-6 h-6 text-yellow-400" fill="none" viewBox="0 0 24 24">
                              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                            </svg>
                          </div>

                          <div v-else class="px-5 pb-4 space-y-4">
                            <!-- Star Rating — two rows of 5 on tiny screens, one row on sm+ -->
                            <div class="flex flex-wrap justify-center gap-1.5">
                              <button v-for="star in 10"
                                      :key="star"
                                      type="button"
                                      @click="setRating(star)"
                                      @mouseover="hoverRating = star"
                                      @mouseleave="hoverRating = 0"
                                      class="w-9 h-9 sm:w-8 sm:h-8 transition-all duration-150 transform hover:scale-110 active:scale-95"
                                      :class="star <= (hoverRating || selectedRating) ? 'text-yellow-400' : 'text-gray-600'">
                                <svg class="w-full h-full" fill="currentColor" viewBox="0 0 20 20">
                                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                                </svg>
                              </button>
                            </div>

                            <!-- Rating label -->
                            <p class="text-white font-medium text-center text-sm min-h-5">
                              {{ getRatingText(hoverRating || selectedRating) }}
                            </p>
                          </div>

                          <DialogFooter class="px-5 pb-5 flex-row gap-2 sm:justify-center">
                            <Button type="button"
                                    variant="outline"
                                    size="sm"
                                    class="flex-1 bg-[#141414] border-white/10 text-white hover:bg-white/10 hover:text-white"
                                    @click="isRatingDialogOpen = false">
                              Cancel
                            </Button>
                            <Button type="button"
                                    size="sm"
                                    :disabled="!selectedRating || submittingRating"
                                    class="flex-1 justify-center bg-[#e6e6e6] text-black hover:bg-gray-100 hover:text-black"
                                    @click="submitRating">
                              <svg v-if="submittingRating" class="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-black" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                              </svg>
                              {{ submittingRating ? 'Submitting…' : 'Submit Rating' }}
                            </Button>
                          </DialogFooter>
                        </DialogContent>
                      </Dialog>

                      <!-- Non-authenticated state -->
                      <button v-else
                              @click="goToLogin()"
                              class="flex items-center justify-end cursor-pointer hover:opacity-80 transition-opacity">
                        <div class="flex items-center space-x-2">
                          <svg class="w-6 h-6 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                            <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"></path>
                          </svg>
                          <div class="flex flex-col">
                            <span class="text-2xl font-bold text-[var(--color-text)]">
                              {{ titleData.averageRating?.toFixed(1) || '0.0' }}
                            </span>
                            <span class="text-xs text-[var(--color-text)] opacity-75">
                              ({{ titleData.ratingCount || 0 }})
                            </span>
                          </div>
                        </div>
                      </button>
                    </div>
                    <!-- Block 5: Main Tabs Content -->
                    <div class="col-span-7 col-start-0 row-start-2 bg-[var(--color-background)] border border-[var(--color-border)] rounded-xl overflow-hidden items-end">
                      <TitleDetailsTabs :title-id="titleData.id"
                                        :title-data="titleData"
                                        :initial-tab="initialTab"
                                        :is-authenticated="isAuthenticated"
                                        @tab-changed="onTabChanged" />
                    </div>
                  </div>
                </div>



                <!-- Block 3: Stats Overview (Top Right) -->
                <div class="row-span-1 col-start-5 row-start-1">
                </div>

                <!-- Block 8: Information Sidebar (aligned with cover image) -->
                <div class="col-start-5 row-start-2">
                  <div class="sticky top-8">
                    <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-xl p-4">
                      <div class="text-sm font-semibold text-[var(--color-text)] mb-4">Information</div>
                      <div class="space-y-3">
                        <div class="pb-3 border-b border-[var(--color-border)] last:border-b-0 last:pb-0"
                             v-for="(info, index) in sidebarInfo" :key="index">
                          <div class="text-xs text-[var(--color-text)] opacity-60 uppercase tracking-wide mb-1">{{ info.label }}</div>
                          <div class="font-medium text-[var(--color-text)] text-sm">{{ info.value }}</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
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
  import TitleChangeHistory from './TitleChangeHistory.vue'
  import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
  import { Spinner } from '@/components/ui/spinner'
  import { Button } from '@/components/ui/button'
  import { ButtonGroup } from '@/components/ui/button-group'
  import {
    Drawer,
    DrawerClose,
    DrawerContent,
    DrawerDescription,
    DrawerFooter,
    DrawerHeader,
    DrawerTitle,
    DrawerTrigger,
  } from '@/components/ui/drawer'
  import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
  } from '@/components/ui/dialog'
  import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuTrigger,
    DropdownMenuItem,
    DropdownMenuSeparator
  } from '@/components/ui/dropdown-menu'
  import { Star, StarIcon, BookmarkIcon, BookmarkCheckIcon, MoreHorizontalIcon, BookOpenIcon, PlayIcon } from 'lucide-vue-next'


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
  const bookmarkStatus = ref(null) // Current bookmark status: 'reading', 'completed', etc.
  const userCustomFolders = ref([]) // User-created folders beyond the 5 standard ones
  const isAuthenticated = ref(false)
  const chaptersData = ref([])
  const loadingChapters = ref(false)

  // Rating modal
  const selectedRating = ref(0)
  const hoverRating = ref(0)
  const submittingRating = ref(false)
  const isRatingDialogOpen = ref(false)
  const loadingExistingRating = ref(false)

  // Pre-fill dialog with user's existing rating each time it opens
  watch(isRatingDialogOpen, async (open) => {
    if (!open || !isAuthenticated.value || !titleData.value?.id) return
    loadingExistingRating.value = true
    try {
      const result = await titleDetailsService.getUserRating(titleData.value.id)
      selectedRating.value = result.success && result.data?.value ? result.data.value : 0
    } catch {
      selectedRating.value = 0
    } finally {
      loadingExistingRating.value = false
    }
  })

  // Computed properties
  const canStartReading = computed(() => {
    return titleData.value?.chapterCount > 0 && chaptersData.value.length > 0 && !loadingChapters.value
  })

  const canContinueReading = computed(() => {
    if (!isAuthenticated.value) return false
    if (!chaptersData.value || chaptersData.value.length === 0) return false

    // Check reading progress - either from bookmark OR from separate progress
    let lastChapter = 0

    // Check bookmark progress first
    if (userBookmark.value?.lastReadChapter > 0) {
      lastChapter = userBookmark.value.lastReadChapter
    }
    // Then check separate progress (for unbookmarked titles)
    else if (readingProgress.value?.lastReadChapter > 0) {
      lastChapter = readingProgress.value.lastReadChapter
    }

    return lastChapter > 0
  })

  const readingProgress = ref(null)
  const bookmarkStats = ref(null)

  const loadReadingProgress = async () => {
    if (!titleData.value?.id || !isAuthenticated.value) return

    try {
      const result = await titleDetailsService.getReadingProgress(titleData.value.id)

      if (result.success && result.data) {
        readingProgress.value = result.data
        console.log('Reading progress loaded:', readingProgress.value)
      }
    } catch (err) {
      console.error('Error loading reading progress:', err)
    }
  }

  // Rating Statistics Computed Properties
  const totalReviews = computed(() => {
    if (!titleData.value?.reviews) return 0
    return titleData.value.reviews.length
  })



  const loadUserRating = async () => {
    if (!titleData.value?.id || !isAuthenticated.value) return

    try {
      const result = await titleDetailsService.getUserRating(titleData.value.id)

      if (result.success && result.data && result.data.hasRated) {
        selectedRating.value = result.data.value
        console.log('User rating loaded:', result.data.value)
      }
    } catch (err) {
      console.error('Error loading user rating:', err)
    }
  }

  const loadBookmarkStats = async () => {
    if (!titleData.value?.id) return

    try {
      const result = await titleDetailsService.getBookmarkStats(titleData.value.id)

      if (result.success && result.data) {
        bookmarkStats.value = result.data
        console.log('Bookmark stats loaded:', result.data)
      }
    } catch (err) {
      console.error('Error loading bookmark stats:', err)
    }
  }

  const sidebarInfo = computed(() => {
    if (!titleData.value) return []

    const info = [
      { label: 'Type', value: getMangaType(titleData.value.type) },
      { label: 'Release', value: titleData.value.releaseDate || 'Unknown' },
      { label: 'Chapters', value: titleData.value.chapterCount || 0 },
      { label: 'Status', value: titleData.value.statusTitle || 'Unknown' },
      { label: 'Translation', value: titleData.value.statusTranslation || 'Unknown' }
    ]

    if (titleData.value.authors && titleData.value.authors.length > 0) {
      info.push({
        label: 'Author',
        value: titleData.value.authors.map(a => a.name || a).join(', ')
      })
    }

    if (titleData.value.artists && titleData.value.artists.length > 0) {
      info.push({
        label: 'Artist',
        value: titleData.value.artists.map(a => a.name || a).join(', ')
      })
    }

    return info
  })

  const bookmarkStatusText = computed(() => {
    if (!userBookmark.value || !bookmarkStatus.value) return 'Bookmark'

    const statusMap = {
      'reading': 'Reading',
      'completed': 'Completed',
      'on-hold': 'On Hold',
      'plan-to-read': 'Plan to Read',
      'dropped': 'Dropped'
    }

    return statusMap[bookmarkStatus.value] || 'Bookmarked'
  })

  const bookmarkButtonClass = computed(() => {
    if (!userBookmark.value || !bookmarkStatus.value) {
      return 'bg-[var(--color-background)] hover:bg-[var(--color-background)]'
    }

    const statusColors = {
      'reading': 'bg-blue-600 hover:bg-blue-700 text-white border-blue-600',
      'completed': 'bg-green-600 hover:bg-green-700 text-white border-green-600',
      'on-hold': 'bg-yellow-600 hover:bg-yellow-700 text-white border-yellow-600',
      'plan-to-read': 'bg-purple-600 hover:bg-purple-700 text-white border-purple-600',
      'dropped': 'bg-red-600 hover:bg-red-700 text-white border-red-600'
    }

    return statusColors[bookmarkStatus.value] || 'bg-[var(--color-background)] hover:bg-[var(--color-background)]'
  })


  // ==================== BOOKMARK PERSISTENCE HELPERS ====================

  /**
   * Get bookmark cache key for current title
   */
  const getBookmarkCacheKey = () => {
    return `bookmark_${titleData.value?.id || props.titleName}`
  }

  /**
   * Save bookmark status to localStorage
   */
  const saveBookmarkToCache = (bookmark, status) => {
    try {
      const cacheKey = getBookmarkCacheKey()
      const cacheData = {
        bookmark,
        status,
        timestamp: Date.now()
      }
      localStorage.setItem(cacheKey, JSON.stringify(cacheData))
      console.log('Bookmark cached:', cacheData)
    } catch (err) {
      console.error('Error caching bookmark:', err)
    }
  }

  /**
   * Load bookmark status from localStorage
   */
  const loadBookmarkFromCache = () => {
    try {
      const cacheKey = getBookmarkCacheKey()
      const cached = localStorage.getItem(cacheKey)

      if (cached) {
        const cacheData = JSON.parse(cached)
        // Cache is valid for 24 hours
        const cacheAge = Date.now() - cacheData.timestamp
        const maxAge = 24 * 60 * 60 * 1000 // 24 hours

        if (cacheAge < maxAge) {
          console.log('Loading bookmark from cache:', cacheData)
          return cacheData
        } else {
          console.log('Cache expired, removing...')
          localStorage.removeItem(cacheKey)
        }
      }
    } catch (err) {
      console.error('Error loading bookmark from cache:', err)
    }
    return null
  }

  /**
   * Clear bookmark cache
   */
  const clearBookmarkCache = () => {
    try {
      const cacheKey = getBookmarkCacheKey()
      localStorage.removeItem(cacheKey)
      console.log('Bookmark cache cleared')
    } catch (err) {
      console.error('Error clearing bookmark cache:', err)
    }
  }



  // Methods
  // MODIFY loadTitleData to also load reading progress:
  const loadTitleData = async () => {
    if (!props.titleName) {
      error.value = 'No title name provided'
      return
    }

    try {
      loading.value = true
      error.value = null

      const result = await titleDetailsService.getTitleDetails(props.titleName)

      if (!result.success || !result.data) {
        error.value = result.error || 'Failed to load title details'
        return
      }

      titleData.value = result.data
      console.log('Title data loaded:', titleData.value)

      // Load chapters for reading buttons
      await loadChaptersData()

      // Load user-specific data if authenticated
      if (isAuthenticated.value) {
        await loadUserBookmark()
        await loadReadingProgress() // <-- ADD THIS LINE
        await loadUserRating()
      }

      // Load bookmark stats
      await loadBookmarkStats()

    } catch (err) {
      error.value = 'An unexpected error occurred'
      console.error('Error loading title data:', err)
    } finally {
      loading.value = false
    }
  }

  const loadUserBookmark = async () => {
    if (!isAuthenticated.value || !titleData.value) return

    try {
      console.log('Loading user bookmark for title:', titleData.value.id)

      // First, try to load from cache to show immediately
      const cached = loadBookmarkFromCache()
      if (cached) {
        userBookmark.value = cached.bookmark
        bookmarkStatus.value = cached.status
        console.log('Bookmark loaded from cache (will verify with API)')
      }

      // Then fetch from API to ensure it's up-to-date
      const result = await titleDetailsService.getUserBookmark(titleData.value.id)

      if (result.success && result.data) {
        // ✅ FIX: The API returns {success: true, data: {bookmark}}
        // So we need to extract the actual bookmark from result.data.data
        const bookmarkData = result.data.data || result.data
        const status = bookmarkData.status

        console.log('Extracted bookmark data:', bookmarkData)
        console.log('Extracted status:', status)

        if (bookmarkData && status) {
          userBookmark.value = bookmarkData
          bookmarkStatus.value = status

          // Save to cache
          saveBookmarkToCache(bookmarkData, status)

          console.log('Bookmark loaded from API with status:', status)
        } else {
          // No valid bookmark found - clear state and cache
          userBookmark.value = null
          bookmarkStatus.value = null
          clearBookmarkCache()
          console.log('No valid bookmark data found')
        }
      } else {
        // No bookmark found - clear state and cache
        userBookmark.value = null
        bookmarkStatus.value = null
        clearBookmarkCache()
        console.log('No bookmark found for this title')
      }
    } catch (err) {
      console.error('Error loading user bookmark:', err)

      // If API fails but we have cache, keep using cache
      if (!userBookmark.value) {
        const cached = loadBookmarkFromCache()
        if (cached) {
          userBookmark.value = cached.bookmark
          bookmarkStatus.value = cached.status
          console.log('Using cached bookmark due to API error')
        }
      }
    }
  }

  const toggleBookmark = async () => {
    if (!isAuthenticated.value) {
      goToLogin()
      return
    }

    try {
      if (userBookmark.value) {
        // Remove bookmark
        const result = await titleDetailsService.removeBookmark(titleData.value.id)
        if (result.success) {
          userBookmark.value = null
          bookmarkStatus.value = null
          clearBookmarkCache() // ← NEW: Clear cache
          showToast('Bookmark removed', 'success')

          if (titleData.value) {
            titleData.value.bookmarkCount = Math.max((titleData.value.bookmarkCount || 1) - 1, 0)
          }
        } else {
          showToast(result.error || 'Failed to remove bookmark', 'error')
        }
      } else {
        // Add bookmark with default status 'reading'.
        // Use UpdateStatus instead of AddBookmark — UpdateStatus resolves the
        // target folder by name server-side, so no folderId is needed here.
        const result = await titleDetailsService.updateBookmarkStatus(titleData.value.id, 'reading')
        if (result.success) {
          const responseData = result.data?.data || result.data || {}
          const newBookmark = {
            titleId: titleData.value.id,
            status: 'reading',
            folderId: responseData.folderId ?? null,
            folderName: responseData.folderName ?? 'Reading',
            ...responseData
          }

          userBookmark.value = newBookmark
          bookmarkStatus.value = 'reading'

          saveBookmarkToCache(newBookmark, 'reading')

          showToast('Added to Reading list', 'success')

          if (titleData.value) {
            titleData.value.bookmarkCount = (titleData.value.bookmarkCount || 0) + 1
          }
        } else {
          showToast(result.error || 'Failed to add bookmark', 'error')
        }
      }
    } catch (err) {
      console.error('Error toggling bookmark:', err)
      showToast('Failed to update bookmark', 'error')
    }
  }

  const changeBookmarkStatus = async (status) => {
    if (!isAuthenticated.value) {
      goToLogin()
      return
    }

    try {
      // UpdateStatus handles upsert server-side — no need to pre-create the bookmark.
      // It will find or create the bookmark and move it to the matching folder.
      const result = await titleDetailsService.updateBookmarkStatus(titleData.value.id, status)

      if (result.success) {
        const responseData = result.data?.data || {}
        bookmarkStatus.value = status
        userBookmark.value = {
          ...userBookmark.value,
          status,
          folderId: responseData.folderId ?? userBookmark.value?.folderId,
          folderName: responseData.folderName ?? userBookmark.value?.folderName,
        }

        // Also bump the bookmark count if this was a fresh add
        if (!userBookmark.value?.id && responseData.bookmarkId) {
          userBookmark.value.id = responseData.bookmarkId
          if (titleData.value) {
            titleData.value.bookmarkCount = (titleData.value.bookmarkCount || 0) + 1
          }
        }

        saveBookmarkToCache(userBookmark.value, status)
        await loadBookmarkStats()

        const statusMap = {
          'reading': 'Reading',
          'completed': 'Completed',
          'on-hold': 'On Hold',
          'plan-to-read': 'Plan to Read',
          'dropped': 'Dropped'
        }
        showToast(`Changed to ${statusMap[status] ?? status}`, 'success')
      } else {
        showToast(result.error || 'Failed to update status', 'error')
      }
    } catch (err) {
      console.error('Error changing bookmark status:', err)
      showToast('Failed to update status', 'error')
    }
  }

  const removeBookmark = async () => {
    if (!isAuthenticated.value) {
      goToLogin()
      return
    }

    try {
      const result = await titleDetailsService.removeBookmark(titleData.value.id)
      if (result.success) {
        userBookmark.value = null
        bookmarkStatus.value = null
        clearBookmarkCache() // ← NEW: Clear cache
        showToast('Bookmark removed', 'success')

        if (titleData.value) {
          titleData.value.bookmarkCount = Math.max((titleData.value.bookmarkCount || 1) - 1, 0)
        }
      } else {
        showToast(result.error || 'Failed to remove bookmark', 'error')
      }
    } catch (err) {
      console.error('Error removing bookmark:', err)
      showToast('Failed to remove bookmark', 'error')
    }
  }

  const retryLoad = async () => {
    await loadTitleData()
  }

  const checkAuthStatus = () => {
    try {
      const token = localStorage.getItem('authToken')
      const user = localStorage.getItem('authUser')
      isAuthenticated.value = !!(token && user)
      console.log('Auth status check:', { isAuthenticated: isAuthenticated.value })
      if (isAuthenticated.value) {
        loadCustomFolders()
      }
    } catch (err) {
      console.error('Error checking auth status:', err)
      isAuthenticated.value = false
    }
  }

  // The 5 standard folders that always appear in the fixed status dropdown
  const STANDARD_FOLDER_NAMES = new Set(['Reading', 'Completed', 'On Hold', 'Plan to Read', 'Dropped'])

  // Fetch user folders and keep only custom (non-standard) ones for the dropdown
  const loadCustomFolders = async () => {
    try {
      const res = await titleDetailsService.apiClient.get('/Bookmarks/GetFolders')
      const folders = res.data?.folders ?? []
      userCustomFolders.value = folders.filter(f => !STANDARD_FOLDER_NAMES.has(f.name))
    } catch (err) {
      console.error('Error loading custom folders:', err)
    }
  }

  // Move an existing bookmark into a custom (user-created) folder directly via AddBookmark
  const moveToCustomFolder = async (folderId, folderName) => {
    if (!isAuthenticated.value || !titleData.value) return
    try {
      // If no bookmark exists yet, AddBookmark creates one in the given folder
      // If it already exists, AddBookmark moves it (server handles upsert)
      const res = await titleDetailsService.apiClient.post('/Bookmarks/AddBookmark', {
        titleId: titleData.value.id,
        folderId: folderId
      })
      if (res.data) {
        userBookmark.value = { ...userBookmark.value, folderId, folderName, status: folderName.toLowerCase() }
        bookmarkStatus.value = folderName
        saveBookmarkToCache(userBookmark.value, folderName)
        showToast(`Moved to "${folderName}"`, 'success')
      }
    } catch (err) {
      console.error('Error moving to custom folder:', err)
      showToast('Failed to move bookmark', 'error')
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

  const viewChangeHistory = () => {
    // Navigate to change history route
    router.push({
      name: 'TitleChangeHistory',
      params: {
        titleId: titleData.value.id
      },
      query: {
        titleName: titleData.value.originalTitle
      }
    })
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

        // Close dialog and reset
        isRatingDialogOpen.value = false
        selectedRating.value = 0
        hoverRating.value = 0

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

  const loadChaptersData = async () => {
    if (!titleData.value?.id) return

    try {
      loadingChapters.value = true
      const result = await titleDetailsService.getChapters(titleData.value.id)

      if (result.success && result.data) {
        chaptersData.value = result.data
        console.log('Chapters loaded for reading buttons:', chaptersData.value.length)
      }
    } catch (err) {
      console.error('Error loading chapters for reading buttons:', err)
    } finally {
      loadingChapters.value = false
    }
  }

  const getFirstChapterUrl = () => {
    if (!titleData.value || !chaptersData.value.length) return '#'

    const sortedChapters = [...chaptersData.value].sort((a, b) => {
      const volumeCompare = a.volumeNumber - b.volumeNumber
      if (volumeCompare !== 0) return volumeCompare
      return a.chapterNumber - b.chapterNumber
    })

    const firstChapter = sortedChapters[0]
    if (!firstChapter) return '#'

    const chapterName = firstChapter.name || firstChapter.chapterNumber.toString()
    return `/${encodeURIComponent(titleData.value.originalTitle)}/chapter/${encodeURIComponent(chapterName)}/v${firstChapter.volumeNumber}/t${firstChapter.teamId || firstChapter.team?.id || 0}?viewMode=single`
  }

  const getContinueReadingUrl = () => {
    if (!chaptersData.value || chaptersData.value.length === 0) return '#'

    // Get last read chapter from either bookmark or progress
    let lastReadChapter = 0
    if (userBookmark.value?.lastReadChapter > 0) {
      lastReadChapter = userBookmark.value.lastReadChapter
    } else if (readingProgress.value?.lastReadChapter > 0) {
      lastReadChapter = readingProgress.value.lastReadChapter
    }

    if (lastReadChapter === 0) return '#'

    const continueChapter = chaptersData.value.find(chapter =>
      chapter.chapterNumber === lastReadChapter
    )

    if (!continueChapter) {
      const sortedChapters = [...chaptersData.value]
        .filter(c => c.chapterNumber <= lastReadChapter)
        .sort((a, b) => b.chapterNumber - a.chapterNumber)

      if (sortedChapters.length > 0) {
        const chapter = sortedChapters[0]
        const chapterName = chapter.name || chapter.chapterNumber.toString()
        return `/${encodeURIComponent(titleData.value.originalTitle)}/chapter/${encodeURIComponent(chapterName)}/v${chapter.volumeNumber}/t${chapter.teamId || chapter.team?.id || 0}?viewMode=single`
      }
      return '#'
    }

    const chapterName = continueChapter.name || continueChapter.chapterNumber.toString()
    return `/${encodeURIComponent(titleData.value.originalTitle)}/chapter/${encodeURIComponent(chapterName)}/v${continueChapter.volumeNumber}/t${continueChapter.teamId || continueChapter.team?.id || 0}?viewMode=single`
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

    nextTick(() => {
      toast.classList.remove('translate-x-full', 'opacity-0')
    })

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
    initialTab.value = route.query.section || 'info'
    checkAuthStatus()
    await loadTitleData()
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
<style scoped>
  @media (max-width: 1024px) {
    :deep(.reading-action-button) {
      border-radius: 0 !important;
    }

    :deep(.bookmark-action-buttons button) {
      border-radius: 0 !important;
    }
  }
</style>
