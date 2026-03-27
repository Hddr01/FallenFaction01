<template>
  <div class="profile-page-wrapper">
    <!-- ─── HERO HEADER ─────────────────────────────────────────── -->
    <div class="relative h-44 sm:h-56 overflow-hidden bg-[var(--color-background-soft)] group/banner cursor-pointer"
         @click="triggerBannerUpload">
      <!-- Actual banner image or gradient fallback -->
      <img v-if="bannerUrl"
           :src="bannerUrl"
           alt="Profile banner"
           class="absolute inset-0 w-full h-full object-cover" />
      <div v-else class="absolute inset-0 banner-bg" />
      <div class="absolute inset-0 bg-gradient-to-b from-transparent via-black/20 to-black/80" />

      <!-- Banner upload hover overlay -->
      <div class="absolute inset-0 bg-black/40 opacity-0 group-hover/banner:opacity-100 transition-opacity flex items-center justify-center gap-2 pointer-events-none">
        <Camera class="size-5 text-white" />
        <span class="text-white text-sm font-medium">Change banner</span>
      </div>
      <input ref="bannerInputRef" type="file" accept="image/*" class="hidden" @change="onBannerSelected" />

      <!-- Logout top-right -->
      <div class="absolute top-4 right-4 z-10" @click.stop>
        <Button variant="outline"
                size="sm"
                :disabled="authStore.isLoading"
                class="gap-2 bg-black/40 border-white/20 text-white hover:bg-black/60 hover:text-white backdrop-blur-sm"
                @click="handleLogout">
          <LogOut class="size-4" />
          <span class="hidden sm:inline">{{ authStore.isLoading ? 'Signing out…' : 'Sign out' }}</span>
        </Button>
      </div>
    </div>

    <!-- Avatar row: sits OUTSIDE the banner so overflow-hidden never clips it. -->
    <div class="max-w-5xl mx-auto px-4 sm:px-8 -mt-10 sm:-mt-12 relative z-10">
      <div class="flex items-end gap-4 pb-4">
        <!-- Avatar with upload overlay -->
        <div class="relative shrink-0 group/avatar cursor-pointer" @click="triggerAvatarUpload">
          <Avatar class="size-20 sm:size-24 ring-4 ring-[var(--color-background)] shadow-2xl">
            <AvatarImage v-if="authStore.user?.profilePicturePath"
                         :src="authStore.user.profilePicturePath"
                         :alt="authStore.userFullName" />
            <AvatarFallback class="text-2xl sm:text-3xl font-bold bg-[var(--vt-c-indigo)] text-white">
              {{ userInitials }}
            </AvatarFallback>
          </Avatar>
          <!-- Upload overlay -->
          <div class="absolute inset-0 rounded-full bg-black/50 opacity-0 group-hover/avatar:opacity-100 transition-opacity flex items-center justify-center">
            <Camera class="size-5 text-white" />
          </div>
          <!-- Online dot -->
          <span class="absolute bottom-1 right-1 size-4 rounded-full border-2 border-[var(--color-background)] shadow"
                :class="authStore.user?.isOnline ? 'bg-emerald-400' : 'bg-zinc-500'" />
          <input ref="avatarInputRef" type="file" accept="image/*" class="hidden" @change="onAvatarSelected" />
        </div>

        <!-- Name + badges -->
        <div class="pb-1 min-w-0">
          <h1 class="text-xl sm:text-2xl font-bold text-[var(--color-heading)] leading-tight truncate">
            {{ authStore.userFullName }}
          </h1>
          <div class="flex flex-wrap items-center gap-1.5 mt-1">
            <Badge variant="outline" class="text-xs">
              @{{ authStore.user?.userName }}
            </Badge>
            <Badge v-for="role in authStore.userRoles"
                   :key="role"
                   class="text-xs bg-[var(--vt-c-indigo)]/80 text-white border-0">
              {{ role }}
            </Badge>
          </div>
        </div>
      </div>
    </div>

    <!-- ─── MAIN CONTENT ─────────────────────────────────────────── -->
    <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 pt-2 pb-16">

      <!-- ─── ANIMATED TAB NAV ──────────────────────────────────── -->
      <div class="border-b border-[var(--color-border)] mb-8">
        <div class="flex gap-1 overflow-x-auto scrollbar-hide">
          <a v-for="tab in tabs"
             :key="tab.key"
             href="#"
             class="relative flex items-center gap-2 px-4 py-3 text-sm font-medium whitespace-nowrap transition-colors duration-200 cursor-pointer select-none shrink-0"
             :class="activeTab === tab.key
              ? 'text-[var(--color-heading)]'
              : 'text-[var(--color-text)] opacity-60 hover:opacity-90'"
             @click.prevent="switchTab(tab.key)">
            <component :is="tab.icon" class="size-4 shrink-0" />
            {{ tab.label }}

            <!-- Motion-v sliding underline indicator -->
            <Motion v-if="activeTab === tab.key"
                    layout-id="profile-tab-indicator"
                    class="absolute bottom-0 left-0 right-0 h-0.5 bg-[var(--color-accent)] rounded-full"
                    :initial="{ opacity: 0 }"
                    :animate="{ opacity: 1 }"
                    :transition="{ type: 'spring', stiffness: 400, damping: 35 }" />
          </a>
        </div>
      </div>

      <!-- ─── TAB PANELS ────────────────────────────────────────── -->
      <div class="tab-content-container">

        <!-- ── OVERVIEW ────────────────────────────────────────── -->
        <div v-show="activeTab === 'overview'" class="tab-panel">
          <Motion :initial="{ opacity: 0, y: 8 }"
                  :animate="{ opacity: 1, y: 0 }"
                  :transition="{ duration: 0.22 }">
            <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
              <!-- Profile info card -->
              <Card class="md:col-span-2 px-6 py-0 gap-0">
                <CardHeader class="px-0 pt-6 pb-4">
                  <CardTitle class="flex items-center gap-2 text-base">
                    <UserIcon class="size-4 text-[var(--color-accent)]" />
                    Profile Information
                  </CardTitle>
                </CardHeader>
                <CardContent class="px-0 pb-6">
                  <dl class="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-5">
                    <div v-for="field in profileFields" :key="field.label">
                      <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">
                        {{ field.label }}
                      </dt>
                      <dd class="text-sm text-[var(--color-text)]">
                        <template v-if="field.component === 'badge'">
                          <Badge :class="field.badgeClass" class="text-xs">{{ field.value }}</Badge>
                          <Badge v-if="field.verified" class="ml-2 text-xs bg-blue-500/20 text-blue-400 border-blue-500/30">Verified</Badge>
                        </template>
                        <template v-else>
                          {{ field.value || '—' }}
                        </template>
                      </dd>
                    </div>

                    <div v-if="authStore.user?.bio" class="sm:col-span-2">
                      <dt class="text-xs font-medium text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-1">Bio</dt>
                      <dd class="text-sm text-[var(--color-text)] bg-[var(--color-background-mute)] rounded-lg px-3 py-2.5 border border-[var(--color-border)] leading-relaxed">
                        {{ authStore.user.bio }}
                      </dd>
                    </div>
                  </dl>
                </CardContent>
              </Card>

              <!-- Quick actions card -->
              <Card class="px-6 py-0 gap-0 h-fit">
                <CardHeader class="px-0 pt-6 pb-4">
                  <CardTitle class="flex items-center gap-2 text-base">
                    <Zap class="size-4 text-[var(--color-accent)]" />
                    Quick Actions
                  </CardTitle>
                </CardHeader>
                <CardContent class="px-0 pb-6 flex flex-col gap-2">
                  <Button variant="outline" class="w-full justify-start gap-2 text-sm h-9" @click="editProfileOpen = true">
                    <Pencil class="size-4" /> Edit Profile
                  </Button>
                  <Button variant="outline" class="w-full justify-start gap-2 text-sm h-9" @click="changePasswordOpen = true">
                    <KeyRound class="size-4" /> Change Password
                  </Button>
                  <Button variant="outline" class="w-full justify-start gap-2 text-sm h-9" @click="accountSettingsOpen = true">
                    <Settings2 class="size-4" /> Account Settings
                  </Button>
                  <Separator class="my-1" />
                  <Button variant="outline"
                          class="w-full justify-start gap-2 text-sm h-9 text-destructive hover:text-destructive border-destructive/30 hover:bg-destructive/10"
                          :disabled="authStore.isLoading"
                          @click="handleLogout">
                    <LogOut class="size-4" />
                    {{ authStore.isLoading ? 'Signing out…' : 'Sign out' }}
                  </Button>
                </CardContent>
              </Card>
            </div>
          </Motion>
        </div>

        <!-- ── BOOKMARKS ─────────────────────────────────────────── -->
        <div v-show="activeTab === 'bookmarks'" class="tab-panel">
          <Motion :initial="{ opacity: 0, y: 8 }"
                  :animate="{ opacity: 1, y: 0 }"
                  :transition="{ duration: 0.22 }"
                  class="w-full overflow-hidden block">
            <!-- Loading state -->
            <div v-if="bookmarks.loadingFolders" class="space-y-4 sm:space-y-0 sm:flex sm:gap-6">
              <div class="flex gap-2 overflow-x-auto pb-1 sm:hidden">
                <Skeleton v-for="n in 5" :key="n" class="h-8 w-24 rounded-full shrink-0" />
              </div>
              <div class="hidden sm:block w-48 shrink-0 space-y-2">
                <Skeleton v-for="n in 6" :key="n" class="h-9 w-full rounded-lg" />
              </div>
              <div class="flex-1 grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4">
                <div v-for="n in 8" :key="n" class="space-y-2">
                  <Skeleton class="aspect-[2/3] w-full rounded-lg" />
                  <Skeleton class="h-4 w-3/4" />
                  <Skeleton class="h-3 w-1/2" />
                </div>
              </div>
            </div>

            <div v-else class="flex flex-col sm:flex-row sm:gap-6">

              <!-- ── MOBILE: Folder tabs carousel (same pattern as main nav) ─── -->
              <div class="sm:hidden mb-4">
                <div class="flex items-center justify-between mb-1">
                  <span class="text-xs font-semibold uppercase tracking-wider text-[var(--color-text)] opacity-50">Folders</span>
                  <Dialog v-model:open="bookmarks.createFolderOpen">
                    <DialogTrigger as-child>
                      <Button variant="ghost" size="icon-sm" class="size-6">
                        <Plus class="size-3.5" />
                      </Button>
                    </DialogTrigger>
                    <DialogContent class="sm:max-w-sm bg-[var(--color-background)] border border-[var(--color-border)]">
                      <DialogHeader>
                        <DialogTitle>New Folder</DialogTitle>
                        <DialogDescription>Create a custom reading list folder.</DialogDescription>
                      </DialogHeader>
                      <div class="py-2">
                        <Input v-model="bookmarks.newFolderName" placeholder="Folder name…" class="w-full" @keydown.enter="createFolder" />
                      </div>
                      <DialogFooter>
                        <Button variant="outline" @click="bookmarks.createFolderOpen = false">Cancel</Button>
                        <Button :disabled="!bookmarks.newFolderName.trim() || bookmarks.creating" @click="createFolder">
                          <Loader2 v-if="bookmarks.creating" class="size-4 animate-spin" />
                          <span v-else>Create</span>
                        </Button>
                      </DialogFooter>
                    </DialogContent>
                  </Dialog>
                </div>

                <!-- Sliding tab row — same underline-indicator pattern as main nav -->
                <div class="border-b border-[var(--color-border)]">
                  <div class="flex overflow-x-auto scrollbar-hide gap-0">
                    <!-- All Bookmarks tab -->
                    <a href="#"
                       class="folder-tab shrink-0"
                       :class="bookmarks.activeFolderId === null ? 'folder-tab-active' : ''"
                       @click.prevent="selectFolder(null)">
                      <BookmarkIcon class="size-3.5 shrink-0" />
                      <span>All</span>
                      <span class="folder-tab-count">{{ totalBookmarkCount }}</span>
                      <Motion v-if="bookmarks.activeFolderId === null"
                              layout-id="folder-tab-indicator"
                              class="folder-tab-indicator"
                              :transition="{ type: 'spring', stiffness: 400, damping: 30 }" />
                    </a>

                    <a v-for="folder in bookmarks.folders"
                       :key="folder.id"
                       href="#"
                       class="folder-tab shrink-0"
                       :class="bookmarks.activeFolderId === folder.id ? 'folder-tab-active' : ''"
                       @click.prevent="selectFolder(folder.id)">
                      <component :is="getFolderIcon(folder.name)" class="size-3.5 shrink-0" :class="getFolderColor(folder.name)" />
                      <span>{{ folder.name }}</span>
                      <span class="folder-tab-count">{{ folder.count }}</span>
                      <Motion v-if="bookmarks.activeFolderId === folder.id"
                              layout-id="folder-tab-indicator"
                              class="folder-tab-indicator"
                              :transition="{ type: 'spring', stiffness: 400, damping: 30 }" />
                    </a>
                  </div>
                </div>
              </div>

              <!-- ── DESKTOP: Vertical sidebar ─── -->
              <div class="hidden sm:block w-48 shrink-0 space-y-1">
                <div class="flex items-center justify-between mb-3">
                  <span class="text-xs font-semibold uppercase tracking-wider text-[var(--color-text)] opacity-50">Folders</span>
                  <Dialog v-model:open="bookmarks.createFolderOpen">
                    <DialogTrigger as-child>
                      <Button variant="ghost" size="icon-sm" class="size-6">
                        <Plus class="size-3.5" />
                      </Button>
                    </DialogTrigger>
                    <DialogContent class="sm:max-w-sm">
                      <DialogHeader>
                        <DialogTitle>New Folder</DialogTitle>
                        <DialogDescription>Create a custom reading list folder.</DialogDescription>
                      </DialogHeader>
                      <div class="py-2">
                        <Input v-model="bookmarks.newFolderName" placeholder="Folder name…" class="w-full" @keydown.enter="createFolder" />
                      </div>
                      <DialogFooter>
                        <Button variant="outline" @click="bookmarks.createFolderOpen = false">Cancel</Button>
                        <Button :disabled="!bookmarks.newFolderName.trim() || bookmarks.creating" @click="createFolder">
                          <Loader2 v-if="bookmarks.creating" class="size-4 animate-spin" />
                          <span v-else>Create</span>
                        </Button>
                      </DialogFooter>
                    </DialogContent>
                  </Dialog>
                </div>

                <button class="folder-btn w-full" :class="bookmarks.activeFolderId === null ? 'folder-btn-active' : ''" @click="selectFolder(null)">
                  <BookmarkIcon class="size-4 shrink-0" />
                  <span class="truncate flex-1 text-left">All Bookmarks</span>
                  <span class="folder-count">{{ totalBookmarkCount }}</span>
                </button>

                <button v-for="folder in bookmarks.folders"
                        :key="folder.id"
                        class="folder-btn w-full group"
                        :class="bookmarks.activeFolderId === folder.id ? 'folder-btn-active' : ''"
                        @click="selectFolder(folder.id)">
                  <component :is="getFolderIcon(folder.name)" class="size-4 shrink-0" :class="getFolderColor(folder.name)" />
                  <span class="truncate flex-1 text-left">{{ folder.name }}</span>
                  <span class="folder-count">{{ folder.count }}</span>
                  <span v-if="!folder.isDefault" class="hidden group-hover:flex items-center gap-0.5 ml-1" @click.stop>
                    <button class="p-0.5 rounded hover:bg-[var(--color-background-mute)] text-[var(--color-text)] opacity-60 hover:opacity-100" @click.stop="openRenameFolder(folder)">
                      <Pencil class="size-3" />
                    </button>
                    <button class="p-0.5 rounded hover:bg-red-500/10 text-[var(--color-text)] opacity-60 hover:text-red-400" @click.stop="openDeleteFolder(folder)">
                      <Trash2 class="size-3" />
                    </button>
                  </span>
                </button>
              </div>

              <!-- ── Main area ─── -->
              <div class="flex-1 min-w-0 overflow-hidden">
                <!-- Toolbar: folder name + count + view toggle -->
                <div class="flex items-center justify-between mb-5">
                  <div>
                    <h2 class="font-semibold text-[var(--color-heading)]">{{ activeFolderName }}</h2>
                    <p class="text-xs text-[var(--color-text)] opacity-50 mt-0.5">
                      {{ activeBookmarks.length }} {{ activeBookmarks.length === 1 ? 'title' : 'titles' }}
                    </p>
                  </div>

                  <!-- View Toggle (matches catalog) -->
                  <div class="flex items-center gap-1 p-1 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg">
                    <Button :variant="bookmarks.viewMode === 'grid' ? 'default' : 'ghost'"
                            size="icon-sm"
                            class="size-7"
                            @click="bookmarks.viewMode = 'grid'">
                      <LayoutGrid class="size-3.5" />
                    </Button>
                    <Button :variant="bookmarks.viewMode === 'list' ? 'default' : 'ghost'"
                            size="icon-sm"
                            class="size-7"
                            @click="bookmarks.viewMode = 'list'">
                      <LayoutList class="size-3.5" />
                    </Button>
                  </div>
                </div>

                <!-- Loading items -->
                <div v-if="bookmarks.loadingItems"
                     :class="bookmarks.viewMode === 'grid'
                       ? 'grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-4'
                       : 'flex flex-col gap-3'">
                  <div v-for="n in 6" :key="n" class="space-y-2">
                    <Skeleton :class="bookmarks.viewMode === 'grid' ? 'aspect-[2/3] w-full rounded-lg' : 'h-28 w-full rounded-lg'" />
                    <Skeleton class="h-4 w-3/4" />
                  </div>
                </div>

                <!-- Empty state -->
                <div v-else-if="activeBookmarks.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
                  <BookmarkX class="size-12 mb-4 text-[var(--color-text)] opacity-20" />
                  <p class="text-sm text-[var(--color-text)] opacity-50">No titles in this list yet.</p>
                  <p class="text-xs text-[var(--color-text)] opacity-35 mt-1">Browse the catalog and add titles to your reading lists.</p>
                  <Button variant="outline" size="sm" class="mt-4" as-child>
                    <a href="/catalog">Browse Catalog</a>
                  </Button>
                </div>

                <!-- ── GRID VIEW ── (matching TitleCard grid mode) -->
                <div v-else-if="bookmarks.viewMode === 'grid'"
                     class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-2 sm:gap-4 w-full">
                  <div v-for="bm in activeBookmarks"
                       :key="bm.id"
                       class="group cursor-pointer transition-all duration-300 min-w-0 sm:hover:scale-105 sm:hover:shadow-xl"
                       @click="navigateToTitle(bm)">
                    <div class="relative aspect-[2/3] overflow-hidden rounded-lg bg-muted">
                      <img :src="getImageUrl(bm.coverImage)"
                           :alt="bm.titleName"
                           class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-110"
                           loading="lazy"
                           @error="$event.target.src='/img/no-cover.png'" />

                      <!-- Overlay gradient -->
                      <div class="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />

                      <!-- Quick actions overlay -->
                      <div class="absolute inset-x-0 bottom-0 p-2 transform translate-y-2 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-300 flex gap-1.5 justify-center">
                        <Button size="sm"
                                variant="outline"
                                class="gap-1 bg-black/60 border-white/20 text-white hover:bg-black/80 hover:text-white text-xs h-7 px-2 backdrop-blur-sm"
                                @click.stop="navigateToTitle(bm)">
                          <Eye class="size-3" /> View
                        </Button>
                        <Button size="sm"
                                variant="outline"
                                class="gap-1 bg-black/60 border-red-400/40 text-red-300 hover:bg-red-500/20 hover:text-red-200 text-xs h-7 px-2 backdrop-blur-sm"
                                @click.stop="openRemoveBookmark(bm)">
                          <Trash2 class="size-3" /> Remove
                        </Button>
                      </div>

                      <!-- Folder badge -->
                      <div class="absolute top-2 left-2">
                        <Badge class="text-[10px] px-1.5 py-0 bg-black/60 border-white/10 text-white/90 backdrop-blur-sm shadow">
                          <component :is="getFolderIcon(bm.folderName)" class="size-2.5 mr-1" :class="getFolderColor(bm.folderName)" />
                          {{ bm.folderName }}
                        </Badge>
                      </div>

                      <!-- Chapter progress badge -->
                      <div v-if="bm.lastReadChapter" class="absolute bottom-2 right-2 bg-black/70 backdrop-blur-sm px-1.5 py-0.5 rounded text-[10px] text-white/80 font-medium">
                        Ch. {{ bm.lastReadChapter }}
                      </div>
                    </div>

                    <div class="mt-2 space-y-0.5">
                      <h3 class="font-semibold line-clamp-2 text-sm group-hover:text-primary transition-colors">
                        {{ bm.titleName }}
                      </h3>
                      <p v-if="bm.addedDate" class="text-[11px] text-muted-foreground">
                        Added {{ formatTimeAgo(bm.addedDate) }}
                      </p>
                    </div>
                  </div>
                </div>

                <!-- ── LIST VIEW ── (matching TitleCard list mode) -->
                <div v-else class="flex flex-col gap-3">
                  <div v-for="bm in activeBookmarks"
                       :key="bm.id"
                       class="group cursor-pointer"
                       @click="navigateToTitle(bm)">
                    <div class="flex gap-4 p-3 rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] hover:border-[var(--color-accent)]/40 hover:shadow-lg transition-all duration-200">
                      <!-- Cover thumbnail -->
                      <div class="relative w-16 h-24 shrink-0 overflow-hidden rounded-lg bg-muted">
                        <img :src="getImageUrl(bm.coverImage)"
                             :alt="bm.titleName"
                             class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-110"
                             loading="lazy"
                             @error="$event.target.src='/img/no-cover.png'" />
                      </div>

                      <!-- Info -->
                      <div class="flex-1 min-w-0 flex flex-col justify-between py-0.5">
                        <div>
                          <div class="flex items-start justify-between gap-2 mb-1.5">
                            <h3 class="font-semibold text-sm line-clamp-2 group-hover:text-primary transition-colors">
                              {{ bm.titleName }}
                            </h3>
                            <Badge class="shrink-0 text-[10px] px-1.5 py-0">
                              <component :is="getFolderIcon(bm.folderName)" class="size-2.5 mr-1" :class="getFolderColor(bm.folderName)" />
                              {{ bm.folderName }}
                            </Badge>
                          </div>
                          <p v-if="bm.lastReadChapter" class="text-xs text-muted-foreground flex items-center gap-1">
                            <BookOpenIcon class="size-3" />
                            Reading from Ch. {{ bm.lastReadChapter }}
                          </p>
                        </div>
                        <div class="flex items-center gap-3 mt-2">
                          <span class="text-[11px] text-muted-foreground">Added {{ formatTimeAgo(bm.addedDate) }}</span>
                          <Button size="sm"
                                  variant="ghost"
                                  class="h-6 px-2 text-[11px] text-red-400 hover:text-red-300 hover:bg-red-500/10 ml-auto"
                                  @click.stop="openRemoveBookmark(bm)">
                            <Trash2 class="size-3 mr-1" /> Remove
                          </Button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </Motion>
        </div>

        <!-- ── FOLLOWING ─────────────────────────────────────────── -->
        <div v-show="activeTab === 'following'" class="tab-panel">
          <Motion :initial="{ opacity: 0, y: 8 }"
                  :animate="{ opacity: 1, y: 0 }"
                  :transition="{ duration: 0.22 }">
            <div class="flex flex-col items-center justify-center py-24 text-center">
              <Users class="size-14 mb-5 text-[var(--color-text)] opacity-15" />
              <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-2">Following</h3>
              <p class="text-sm text-[var(--color-text)] opacity-50 max-w-xs leading-relaxed">
                Users and titles you follow will appear here. This feature is coming soon.
              </p>
            </div>
          </Motion>
        </div>

        <!-- ── COMMENTS ─────────────────────────────────────────── -->
        <div v-show="activeTab === 'comments'" class="tab-panel">
          <Motion :initial="{ opacity: 0, y: 8 }"
                  :animate="{ opacity: 1, y: 0 }"
                  :transition="{ duration: 0.22 }">
            <!-- Loading skeletons -->
            <div v-if="comments.loading" class="space-y-3">
              <Skeleton v-for="n in 5" :key="n" class="h-28 w-full rounded-xl" />
            </div>

            <!-- Error state -->
            <div v-else-if="comments.error" class="flex flex-col items-center justify-center py-20 text-center">
              <MessageSquare class="size-12 mb-4 text-[var(--color-text)] opacity-20" />
              <p class="text-sm font-medium text-[var(--color-heading)] mb-1">Could not load comments</p>
              <p class="text-xs text-[var(--color-text)] opacity-50 mb-4">{{ comments.error }}</p>
              <Button variant="outline" size="sm" @click="loadComments">Try again</Button>
            </div>

            <!-- Empty state -->
            <div v-else-if="!comments.loading && comments.items.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
              <MessageSquare class="size-12 mb-4 text-[var(--color-text)] opacity-20" />
              <h3 class="text-base font-semibold text-[var(--color-heading)] mb-1">No comments yet</h3>
              <p class="text-sm text-[var(--color-text)] opacity-50 max-w-xs leading-relaxed">
                Your comments on titles and chapters will appear here.
              </p>
            </div>

            <!-- Comment list -->
            <div v-else class="space-y-2">
              <!-- Header bar -->
              <div class="flex items-center justify-between pb-4 border-b border-[var(--color-border)]">
                <p class="text-sm font-medium text-[var(--color-heading)]">
                  Comment History
                  <span class="ml-2 text-xs font-normal text-[var(--color-text)] opacity-50">
                    {{ comments.pagination.totalCount }} total
                  </span>
                </p>
                <!-- Sort selector -->
                <div class="flex items-center gap-2">
                  <span class="text-xs text-[var(--color-text)] opacity-50">Sort:</span>
                  <select v-model="comments.sortBy"
                          class="text-xs bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-md px-2 py-1 text-[var(--color-text)] cursor-pointer focus:outline-none focus:border-[var(--color-accent)]"
                          @change="reloadComments">
                    <option value="newest">Newest</option>
                    <option value="oldest">Oldest</option>
                    <option value="likes">Most Liked</option>
                  </select>
                </div>
              </div>

              <!-- Individual comment cards — Reddit-style matching CommentItem -->
              <div v-for="c in comments.items"
                   :key="c.id"
                   class="comment-history-card">
                <!-- Context breadcrumb: where this comment was posted -->
                <div class="comment-context">
                  <component :is="c.targetType === 1 ? BookOpenIcon : c.targetType === 2 ? BookOpen : ImageIcon"
                             class="size-3.5 shrink-0 opacity-60" />
                  <span class="text-[11px] text-[var(--color-text)] opacity-50">
                    {{ c.targetType === 1 ? 'Title' : c.targetType === 2 ? 'Chapter' : 'Page' }}
                  </span>
                  <span class="text-[11px] opacity-30">›</span>
                  <!-- Breadcrumb title link: navigates to the exact location of this comment -->
                  <a v-if="c.titleSlug"
                     :href="buildCommentUrl(c)"
                     class="text-[11px] font-medium text-[var(--color-accent)] hover:underline truncate max-w-[260px]">
                    {{ c.titleName || c.titleSlug }}
                  </a>
                  <span v-else class="text-[11px] text-[var(--color-text)] opacity-50 truncate">
                    {{ c.titleName || '—' }}
                  </span>
                  <span class="text-[11px] text-[var(--color-text)] opacity-35 ml-auto shrink-0">
                    {{ formatTimeAgo(c.postedDate) }}
                  </span>
                </div>

                <!-- Comment body row: vote column + content -->
                <div class="flex gap-3 mt-2">
                  <!-- Vote score pill (matches CommentItem's vote-controls) -->
                  <div class="flex flex-col items-center gap-0.5 shrink-0 pt-0.5">
                    <ChevronUp class="size-4 text-[var(--color-text)] opacity-30" />
                    <span class="text-xs font-semibold tabular-nums leading-none"
                          :class="{
                          'text-emerald-400' : (c.likesCount - c.dislikesCount)>
                      0,
                      'text-red-400':    (c.likesCount - c.dislikesCount) < 0,
                      'text-[var(--color-text)] opacity-50': (c.likesCount - c.dislikesCount) === 0
                      }"
                      >
                      {{ c.likesCount - c.dislikesCount }}
                    </span>
                    <ChevronDown class="size-4 text-[var(--color-text)] opacity-30" />
                  </div>

                  <!-- Text + actions -->
                  <div class="flex-1 min-w-0">
                    <!-- Comment text -->
                    <p class="text-sm text-[var(--color-text)] leading-relaxed"
                       :class="{ 'line-clamp-4': !c.expanded }">
                      {{ c.content }}
                    </p>
                    <button v-if="c.content.length > 300"
                            class="text-xs text-[var(--color-accent)] hover:underline mt-1"
                            @click="c.expanded = !c.expanded">
                      {{ c.expanded ? 'Show less' : 'Show more' }}
                    </button>

                    <!-- Actions row — matching CommentItem's comment-actions -->
                    <div class="flex items-center gap-3 mt-2">
                      <!-- View in context — opens the thread on the title page -->
                      <a :href="buildCommentUrl(c)"
                         class="comment-action-btn"
                         title="View this comment in context">
                        <ExternalLink class="size-3" />
                        View in context
                      </a>
                      <!-- Reply badge if it is a reply itself -->
                      <span v-if="c.parentCommentId"
                            class="text-[11px] text-[var(--color-text)] opacity-40 flex items-center gap-1">
                        <CornerDownRight class="size-3" /> Reply
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Load more -->
              <div v-if="comments.pagination.hasNext" class="pt-4 flex justify-center">
                <Button variant="outline"
                        size="sm"
                        :disabled="comments.loadingMore"
                        @click="loadMoreComments">
                  <Loader2 v-if="comments.loadingMore" class="size-4 animate-spin mr-2" />
                  Load more
                </Button>
              </div>
            </div>
          </Motion>
        </div>

      </div>
    </div>

    <!-- ─── RENAME FOLDER DIALOG ─────────────────────────────────── -->
    <Dialog v-model:open="bookmarks.renameFolderOpen">
      <DialogContent class="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Rename Folder</DialogTitle>
          <DialogDescription>Enter a new name for "{{ bookmarks.renamingFolder?.name }}".</DialogDescription>
        </DialogHeader>
        <div class="py-2">
          <Input v-model="bookmarks.renameFolderName"
                 placeholder="New folder name…"
                 class="w-full"
                 @keydown.enter="renameFolder" />
        </div>
        <DialogFooter>
          <Button variant="outline" @click="bookmarks.renameFolderOpen = false">Cancel</Button>
          <Button :disabled="!bookmarks.renameFolderName.trim() || bookmarks.renaming" @click="renameFolder">
            <Loader2 v-if="bookmarks.renaming" class="size-4 animate-spin" />
            <span v-else>Save</span>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>

    <!-- ─── DELETE FOLDER CONFIRM ────────────────────────────────── -->
    <AlertDialog v-model:open="bookmarks.deleteFolderOpen">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Delete "{{ bookmarks.deletingFolder?.name }}"?</AlertDialogTitle>
          <AlertDialogDescription>
            Bookmarks inside will be moved to your default folder. This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction class="bg-destructive hover:bg-destructive/90 text-white"
                             :disabled="bookmarks.deleting"
                             @click="deleteFolder">
            <Loader2 v-if="bookmarks.deleting" class="size-4 animate-spin mr-2" />
            Delete
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>

    <!-- ─── REMOVE BOOKMARK CONFIRM ──────────────────────────────── -->
    <AlertDialog v-model:open="bookmarks.removeBookmarkOpen">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Remove bookmark?</AlertDialogTitle>
          <AlertDialogDescription>
            "{{ bookmarks.removingBookmark?.titleName }}" will be removed from your reading lists.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction class="bg-destructive hover:bg-destructive/90 text-white"
                             :disabled="bookmarks.removingBm"
                             @click="removeBookmark">
            <Loader2 v-if="bookmarks.removingBm" class="size-4 animate-spin mr-2" />
            Remove
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>

    <!-- ─── EDIT PROFILE DIALOG ─────────────────────────────────── -->
    <Dialog v-model:open="editProfileOpen">
      <DialogContent class="sm:max-w-md bg-[var(--color-background)] border border-[var(--color-border)]">
        <DialogHeader>
          <DialogTitle class="flex items-center gap-2">
            <Pencil class="size-4" /> Edit Profile
          </DialogTitle>
          <DialogDescription>Update your personal information.</DialogDescription>
        </DialogHeader>
        <div class="space-y-4 py-2">
          <div class="grid grid-cols-2 gap-3">
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-[var(--color-text)] opacity-70">First Name</label>
              <Input v-model="editForm.firstName" placeholder="First name" />
            </div>
            <div class="space-y-1.5">
              <label class="text-xs font-medium text-[var(--color-text)] opacity-70">Last Name</label>
              <Input v-model="editForm.lastName" placeholder="Last name" />
            </div>
          </div>
          <div class="space-y-1.5">
            <label class="text-xs font-medium text-[var(--color-text)] opacity-70">Date of Birth</label>
            <Input v-model="editForm.dateOfBirth" type="date" />
          </div>
          <div class="space-y-1.5">
            <label class="text-xs font-medium text-[var(--color-text)] opacity-70">Bio</label>
            <textarea v-model="editForm.bio"
                      placeholder="Tell something about yourself…"
                      maxlength="500"
                      rows="3"
                      class="w-full rounded-md border border-[var(--color-border)] bg-[var(--color-background-soft)] px-3 py-2 text-sm text-[var(--color-text)] resize-none focus:outline-none focus:border-[var(--color-accent)] placeholder:text-muted-foreground" />
            <p class="text-[11px] text-[var(--color-text)] opacity-40 text-right">{{ (editForm.bio || '').length }}/500</p>
          </div>
          <p v-if="editForm.error" class="text-xs text-destructive">{{ editForm.error }}</p>
          <p v-if="editForm.success" class="text-xs text-emerald-400">{{ editForm.success }}</p>
        </div>
        <DialogFooter>
          <Button variant="outline" @click="editProfileOpen = false">Cancel</Button>
          <Button :disabled="editForm.saving" @click="saveProfile">
            <Loader2 v-if="editForm.saving" class="size-4 animate-spin mr-2" />
            Save Changes
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>

    <!-- ─── CHANGE PASSWORD DIALOG ───────────────────────────────── -->
    <Dialog v-model:open="changePasswordOpen">
      <DialogContent class="sm:max-w-sm bg-[var(--color-background)] border border-[var(--color-border)]">
        <DialogHeader>
          <DialogTitle class="flex items-center gap-2">
            <KeyRound class="size-4" /> Change Password
          </DialogTitle>
          <DialogDescription>Enter your current password and choose a new one.</DialogDescription>
        </DialogHeader>
        <div class="space-y-3 py-2">
          <div class="space-y-1.5">
            <label class="text-xs font-medium text-[var(--color-text)] opacity-70">Current Password</label>
            <Input v-model="pwForm.current" type="password" placeholder="Current password" autocomplete="current-password" />
          </div>
          <div class="space-y-1.5">
            <label class="text-xs font-medium text-[var(--color-text)] opacity-70">New Password</label>
            <Input v-model="pwForm.next" type="password" placeholder="Min. 6 characters" autocomplete="new-password" />
          </div>
          <div class="space-y-1.5">
            <label class="text-xs font-medium text-[var(--color-text)] opacity-70">Confirm New Password</label>
            <Input v-model="pwForm.confirm" type="password" placeholder="Repeat new password" autocomplete="new-password" />
          </div>
          <p v-if="pwForm.error" class="text-xs text-destructive">{{ pwForm.error }}</p>
          <p v-if="pwForm.success" class="text-xs text-emerald-400">{{ pwForm.success }}</p>
        </div>
        <DialogFooter>
          <Button variant="outline" @click="changePasswordOpen = false">Cancel</Button>
          <Button :disabled="pwForm.saving" @click="savePassword">
            <Loader2 v-if="pwForm.saving" class="size-4 animate-spin mr-2" />
            Update Password
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>

    <!-- ─── ACCOUNT SETTINGS DIALOG ─────────────────────────────── -->
    <Dialog v-model:open="accountSettingsOpen">
      <DialogContent class="sm:max-w-sm bg-[var(--color-background)] border border-[var(--color-border)]">
        <DialogHeader>
          <DialogTitle class="flex items-center gap-2">
            <Settings2 class="size-4" /> Account Settings
          </DialogTitle>
          <DialogDescription>Manage your account images and preferences.</DialogDescription>
        </DialogHeader>
        <div class="space-y-5 py-2">
          <!-- Avatar -->
          <div class="space-y-2">
            <p class="text-xs font-medium text-[var(--color-text)] opacity-70">Profile Picture</p>
            <div class="flex items-center gap-4">
              <Avatar class="size-16 shrink-0">
                <AvatarImage v-if="authStore.user?.profilePicturePath"
                             :src="getImageUrl(authStore.user.profilePicturePath)" />
                <AvatarFallback class="text-lg font-bold bg-[var(--vt-c-indigo)] text-white">{{ userInitials }}</AvatarFallback>
              </Avatar>
              <div class="flex-1 space-y-2">
                <Button variant="outline" size="sm" class="w-full gap-2" :disabled="avatarUploading" @click="triggerAvatarUpload">
                  <Loader2 v-if="avatarUploading" class="size-3.5 animate-spin" />
                  <Upload v-else class="size-3.5" />
                  {{ avatarUploading ? 'Uploading…' : 'Upload new avatar' }}
                </Button>
                <p class="text-[10px] text-[var(--color-text)] opacity-40">JPG, PNG, WebP, GIF — max 5 MB</p>
              </div>
            </div>
          </div>

          <Separator />

          <!-- Banner -->
          <div class="space-y-2">
            <p class="text-xs font-medium text-[var(--color-text)] opacity-70">Profile Banner</p>
            <div class="relative h-20 rounded-lg overflow-hidden bg-[var(--color-background-mute)] border border-[var(--color-border)]">
              <img v-if="bannerUrl" :src="bannerUrl" class="w-full h-full object-cover" alt="Banner preview" />
              <div v-else class="absolute inset-0 banner-bg" />
              <div v-if="!bannerUrl" class="absolute inset-0 flex items-center justify-center">
                <p class="text-xs text-[var(--color-text)] opacity-40">No banner set</p>
              </div>
            </div>
            <Button variant="outline" size="sm" class="w-full gap-2" :disabled="bannerUploading" @click="triggerBannerUpload">
              <Loader2 v-if="bannerUploading" class="size-3.5 animate-spin" />
              <Upload v-else class="size-3.5" />
              {{ bannerUploading ? 'Uploading…' : 'Upload new banner' }}
            </Button>
            <p class="text-[10px] text-[var(--color-text)] opacity-40">JPG, PNG, WebP — max 10 MB</p>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" class="w-full" @click="accountSettingsOpen = false">Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>

  </div>
</template>

<script setup>
  import { ref, reactive, computed, inject, onMounted, watch } from 'vue'
  import { useRouter } from 'vue-router'
  import { useAuthStore } from '../../stores/authStore'
  import { Motion } from 'motion-v'
  import {
    UserIcon, BookmarkIcon, BookmarkX, BookOpenIcon,
    Users, MessageSquare, Zap, Pencil, KeyRound,
    Settings2, LogOut, Plus, Trash2, Eye, Loader2,
    BookOpen, CheckCircle2, PauseCircle, Clock, XCircle,
    ChevronUp, ChevronDown, ExternalLink, CornerDownRight, ImageIcon,
    LayoutGrid, LayoutList, Camera, Upload,
  } from 'lucide-vue-next'

  // UI components
  import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
  import { Badge } from '@/components/ui/badge'
  import { Button } from '@/components/ui/button'
  import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
  import { Separator } from '@/components/ui/separator'
  import { Skeleton } from '@/components/ui/skeleton'
  import { Input } from '@/components/ui/input'
  import {
    Dialog, DialogContent, DialogHeader, DialogTitle,
    DialogDescription, DialogFooter, DialogTrigger,
  } from '@/components/ui/dialog'
  import {
    AlertDialog, AlertDialogContent, AlertDialogHeader, AlertDialogTitle,
    AlertDialogDescription, AlertDialogFooter, AlertDialogCancel, AlertDialogAction,
  } from '@/components/ui/alert-dialog'

  // ─── setup ────────────────────────────────────────────────────
  const router = useRouter()
  const authStore = useAuthStore()
  const apiClient = inject('apiClient')
  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

  const getImageUrl = (path) => {
    if (!path) return '/img/no-cover.png'
    // Already absolute — return as-is (external CDN etc.)
    if (path.startsWith('http')) return path
    // Relative path (e.g. /uploads/avatars/...) — let the browser resolve it
    // against the current origin. Both Vite (dev) and nginx (Docker) proxy
    // /uploads → backend, so this works from localhost AND from 192.168.x.x.
    // Never prepend a hardcoded hostname — that breaks mobile/Docker access.
    return path
  }

  // ─── modal open flags ─────────────────────────────────────────
  const editProfileOpen = ref(false)
  const changePasswordOpen = ref(false)
  const accountSettingsOpen = ref(false)

  // ─── banner image ─────────────────────────────────────────────
  const bannerUrl = computed(() => {
    const path = authStore.user?.bannerImagePath
    return path ? getImageUrl(path) : null
  })

  // ─── avatar upload ────────────────────────────────────────────
  const avatarInputRef = ref(null)
  const avatarUploading = ref(false)

  function triggerAvatarUpload() {
    avatarInputRef.value?.click()
  }

  async function onAvatarSelected(e) {
    const file = e.target.files?.[0]
    if (!file) return
    avatarUploading.value = true
    try {
      const fd = new FormData()
      fd.append('file', file)
      const res = await apiClient.post('/UserProfile/UploadAvatar', fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      // Update authStore user so avatar reflects immediately everywhere
      if (authStore.user) {
        authStore.user.profilePicturePath = res.data.profilePicturePath
        localStorage.setItem('authUser', JSON.stringify(authStore.user))
      }
    } catch (err) {
      console.error('Avatar upload failed', err)
    } finally {
      avatarUploading.value = false
      e.target.value = ''
    }
  }

  // ─── banner upload ────────────────────────────────────────────
  const bannerInputRef = ref(null)
  const bannerUploading = ref(false)

  function triggerBannerUpload() {
    bannerInputRef.value?.click()
  }

  async function onBannerSelected(e) {
    const file = e.target.files?.[0]
    if (!file) return
    bannerUploading.value = true
    try {
      const fd = new FormData()
      fd.append('file', file)
      const res = await apiClient.post('/UserProfile/UploadBanner', fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      if (authStore.user) {
        authStore.user.bannerImagePath = res.data.bannerImagePath
        localStorage.setItem('authUser', JSON.stringify(authStore.user))
      }
    } catch (err) {
      console.error('Banner upload failed', err)
    } finally {
      bannerUploading.value = false
      e.target.value = ''
    }
  }

  // ─── edit profile form ────────────────────────────────────────
  const editForm = reactive({
    firstName: '',
    lastName: '',
    bio: '',
    dateOfBirth: '',
    saving: false,
    error: '',
    success: '',
  })

  // Pre-fill form when dialog opens
  watch(editProfileOpen, (open) => {
    if (!open) return
    const u = authStore.user
    editForm.firstName = u?.firstName ?? ''
    editForm.lastName = u?.lastName ?? ''
    editForm.bio = u?.bio ?? ''
    editForm.dateOfBirth = u?.dateOfBirth
      ? new Date(u.dateOfBirth).toISOString().split('T')[0]
      : ''
    editForm.error = ''
    editForm.success = ''
  })

  async function saveProfile() {
    editForm.error = ''
    editForm.success = ''
    editForm.saving = true
    try {
      const res = await apiClient.put('/UserProfile/UpdateProfile', {
        firstName: editForm.firstName || null,
        lastName: editForm.lastName || null,
        bio: editForm.bio || null,
        dateOfBirth: editForm.dateOfBirth || null,
      })
      // Merge updated fields into store
      if (authStore.user) {
        Object.assign(authStore.user, res.data)
        localStorage.setItem('authUser', JSON.stringify(authStore.user))
      }
      editForm.success = 'Profile updated successfully.'
      setTimeout(() => { editProfileOpen.value = false }, 1200)
    } catch (err) {
      editForm.error = err?.response?.data?.message ?? 'Failed to save. Please try again.'
    } finally {
      editForm.saving = false
    }
  }

  // ─── change password form ─────────────────────────────────────
  const pwForm = reactive({
    current: '',
    next: '',
    confirm: '',
    saving: false,
    error: '',
    success: '',
  })

  watch(changePasswordOpen, (open) => {
    if (!open) return
    pwForm.current = ''
    pwForm.next = ''
    pwForm.confirm = ''
    pwForm.error = ''
    pwForm.success = ''
  })

  async function savePassword() {
    pwForm.error = ''
    pwForm.success = ''
    if (pwForm.next !== pwForm.confirm) {
      pwForm.error = 'New passwords do not match.'
      return
    }
    if (pwForm.next.length < 6) {
      pwForm.error = 'New password must be at least 6 characters.'
      return
    }
    pwForm.saving = true
    try {
      await apiClient.post('/UserProfile/ChangePassword', {
        currentPassword: pwForm.current,
        newPassword: pwForm.next,
        confirmPassword: pwForm.confirm,
      })
      pwForm.success = 'Password changed successfully.'
      setTimeout(() => { changePasswordOpen.value = false }, 1200)
    } catch (err) {
      pwForm.error = err?.response?.data?.message ?? 'Failed to change password.'
    } finally {
      pwForm.saving = false
    }
  }

  // ─── tabs ─────────────────────────────────────────────────────
  const tabs = [
    { key: 'overview', label: 'Overview', icon: UserIcon },
    { key: 'bookmarks', label: 'Bookmarks', icon: BookmarkIcon },
    { key: 'following', label: 'Following', icon: Users },
    { key: 'comments', label: 'Comments', icon: MessageSquare },
  ]
  const activeTab = ref('overview')

  function switchTab(key) {
    activeTab.value = key
    if (key === 'bookmarks' && !bookmarks.initialized) {
      loadFolders()
    }
    if (key === 'comments' && !comments.initialized) {
      loadComments()
    }
  }

  // ─── computed: user display ───────────────────────────────────
  const userInitials = computed(() => {
    const u = authStore.user
    if (u?.firstName && u?.lastName) return `${u.firstName[0]}${u.lastName[0]}`.toUpperCase()
    return (u?.userName || u?.email || 'U').slice(0, 2).toUpperCase()
  })

  const profileFields = computed(() => {
    const u = authStore.user
    if (!u) return []
    return [
      { label: 'First Name', value: u.firstName },
      { label: 'Last Name', value: u.lastName },
      { label: 'Username', value: u.userName ? `@${u.userName}` : null },
      { label: 'Email', value: u.email },
      { label: 'Date of Birth', value: u.dateOfBirth ? formatDate(u.dateOfBirth) : null },
      { label: 'Member Since', value: u.registrationDate ? formatDate(u.registrationDate) : null },
      { label: 'Last Active', value: u.lastLoginDate ? formatDateTime(u.lastLoginDate) : null },
      {
        label: 'Account Status',
        component: 'badge',
        value: u.isActive ? 'Active' : 'Inactive',
        badgeClass: u.isActive
          ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'
          : 'bg-red-500/15 text-red-400 border-red-500/30',
        verified: u.isVerified,
      },
    ]
  })

  // ─── date helpers ─────────────────────────────────────────────
  const formatDate = (d) => new Date(d).toLocaleDateString()
  const formatDateTime = (d) => new Date(d).toLocaleString()

  // ─── logout ───────────────────────────────────────────────────
  async function handleLogout() {
    await authStore.logout()
    router.push('/')
  }

  // ─── BOOKMARKS STATE ──────────────────────────────────────────
  const bookmarks = reactive({
    initialized: false,
    loadingFolders: false,
    loadingItems: false,
    folders: [],
    activeFolderId: null,
    itemsByFolder: {},
    allItems: [],
    viewMode: 'grid',   // 'grid' | 'list'

    // Create folder dialog
    createFolderOpen: false,
    newFolderName: '',
    creating: false,

    // Rename folder dialog
    renameFolderOpen: false,
    renamingFolder: null,
    renameFolderName: '',
    renaming: false,

    // Delete folder dialog
    deleteFolderOpen: false,
    deletingFolder: null,
    deleting: false,

    // Remove bookmark
    removeBookmarkOpen: false,
    removingBookmark: null,
    removingBm: false,
  })

  const totalBookmarkCount = computed(() =>
    bookmarks.folders.reduce((s, f) => s + (f.count || 0), 0)
  )

  const activeFolderName = computed(() => {
    if (bookmarks.activeFolderId === null) return 'All Bookmarks'
    return bookmarks.folders.find(f => f.id === bookmarks.activeFolderId)?.name ?? 'Folder'
  })

  const activeBookmarks = computed(() => {
    if (bookmarks.activeFolderId === null) return bookmarks.allItems
    return bookmarks.itemsByFolder[bookmarks.activeFolderId] ?? []
  })

  // ─── Folder icons/colors (matches TitleDetailsTabs palette) ───
  const folderIconMap = {
    'Reading': BookOpen,
    'Completed': CheckCircle2,
    'On Hold': PauseCircle,
    'Plan to Read': Clock,
    'Dropped': XCircle,
    'Favorites': BookmarkIcon,
    'Others': BookmarkIcon,
  }
  const folderColorMap = {
    'Reading': 'text-blue-400',
    'Completed': 'text-emerald-400',
    'On Hold': 'text-yellow-400',
    'Plan to Read': 'text-purple-400',
    'Dropped': 'text-red-400',
    'Favorites': 'text-pink-400',
    'Others': 'text-gray-400',
  }
  const getFolderIcon = (name) => folderIconMap[name] ?? BookmarkIcon
  const getFolderColor = (name) => folderColorMap[name] ?? 'text-[var(--color-text)]'

  // Navigate to title using originalTitle (the Vue router slug)
  function navigateToTitle(bm) {
    const slug = bm.originalTitle || bm.titleName
    if (slug) router.push(`/${encodeURIComponent(slug)}`)
  }

  // ─── Load folders (+ all bookmarks) ──────────────────────────
  async function loadFolders() {
    bookmarks.loadingFolders = true
    bookmarks.initialized = true
    try {
      const res = await apiClient.get('/Bookmarks/GetFolders')
      bookmarks.folders = res.data.folders ?? []
      // Eagerly load items for "All" view
      await loadAllBookmarks()
    } catch (err) {
      console.error('Failed to load bookmark folders', err)
    } finally {
      bookmarks.loadingFolders = false
    }
  }

  async function loadAllBookmarks() {
    if (!bookmarks.folders.length) return
    const all = []
    for (const folder of bookmarks.folders) {
      try {
        const r = await apiClient.get(`/Bookmarks/GetBookmarksByFolder/${folder.id}`)
        const items = (r.data ?? []).map(bm => ({ ...bm, folderName: folder.name }))
        bookmarks.itemsByFolder[folder.id] = items
        all.push(...items)
      } catch {/* skip */ }
    }
    bookmarks.allItems = all.sort((a, b) => new Date(b.addedDate) - new Date(a.addedDate))
  }

  async function loadFolderItems(folderId) {
    if (bookmarks.itemsByFolder[folderId] !== undefined) return
    bookmarks.loadingItems = true
    try {
      const folder = bookmarks.folders.find(f => f.id === folderId)
      const r = await apiClient.get(`/Bookmarks/GetBookmarksByFolder/${folderId}`)
      bookmarks.itemsByFolder[folderId] = (r.data ?? []).map(bm => ({ ...bm, folderName: folder?.name ?? '' }))
    } catch (err) {
      console.error('Failed to load folder items', err)
    } finally {
      bookmarks.loadingItems = false
    }
  }

  function selectFolder(folderId) {
    bookmarks.activeFolderId = folderId
    if (folderId !== null) loadFolderItems(folderId)
  }

  // ─── Create folder ────────────────────────────────────────────
  async function createFolder() {
    const name = bookmarks.newFolderName.trim()
    if (!name) return
    bookmarks.creating = true
    try {
      const r = await apiClient.post('/Bookmarks/CreateFolder', { name })
      bookmarks.folders.push({ ...r.data, count: 0 })
      bookmarks.itemsByFolder[r.data.id] = []
      bookmarks.newFolderName = ''
      bookmarks.createFolderOpen = false
    } catch (err) {
      console.error('Create folder failed', err)
    } finally {
      bookmarks.creating = false
    }
  }

  // ─── Rename folder ────────────────────────────────────────────
  function openRenameFolder(folder) {
    bookmarks.renamingFolder = folder
    bookmarks.renameFolderName = folder.name
    bookmarks.renameFolderOpen = true
  }

  async function renameFolder() {
    const name = bookmarks.renameFolderName.trim()
    if (!name || !bookmarks.renamingFolder) return
    bookmarks.renaming = true
    try {
      await apiClient.put(`/Bookmarks/UpdateFolder/${bookmarks.renamingFolder.id}`, { name })
      const idx = bookmarks.folders.findIndex(f => f.id === bookmarks.renamingFolder.id)
      if (idx !== -1) bookmarks.folders[idx].name = name
      // Update cached items
      const items = bookmarks.itemsByFolder[bookmarks.renamingFolder.id]
      if (items) items.forEach(i => { i.folderName = name })
      bookmarks.allItems.forEach(i => { if (i.folderId === bookmarks.renamingFolder.id) i.folderName = name })
      bookmarks.renameFolderOpen = false
    } catch (err) {
      console.error('Rename folder failed', err)
    } finally {
      bookmarks.renaming = false
    }
  }

  // ─── Delete folder ────────────────────────────────────────────
  function openDeleteFolder(folder) {
    bookmarks.deletingFolder = folder
    bookmarks.deleteFolderOpen = true
  }

  async function deleteFolder() {
    if (!bookmarks.deletingFolder) return
    bookmarks.deleting = true
    try {
      await apiClient.delete(`/Bookmarks/DeleteFolder/${bookmarks.deletingFolder.id}`)
      const deletedId = bookmarks.deletingFolder.id
      bookmarks.folders = bookmarks.folders.filter(f => f.id !== deletedId)
      delete bookmarks.itemsByFolder[deletedId]
      // Remove from allItems and reload default folder cache
      const defaultFolder = bookmarks.folders.find(f => f.isDefault)
      if (defaultFolder) {
        delete bookmarks.itemsByFolder[defaultFolder.id] // force refetch
      }
      await loadAllBookmarks()
      if (bookmarks.activeFolderId === deletedId) bookmarks.activeFolderId = null
      bookmarks.deleteFolderOpen = false
    } catch (err) {
      console.error('Delete folder failed', err)
    } finally {
      bookmarks.deleting = false
    }
  }

  // ─── Remove bookmark ──────────────────────────────────────────
  function openRemoveBookmark(bm) {
    bookmarks.removingBookmark = bm
    bookmarks.removeBookmarkOpen = true
  }

  async function removeBookmark() {
    const bm = bookmarks.removingBookmark
    if (!bm) return
    bookmarks.removingBm = true
    try {
      await apiClient.post('/Bookmarks/RemoveBookmark', { bookmarkId: bm.id })
      // Remove from caches
      bookmarks.allItems = bookmarks.allItems.filter(i => i.id !== bm.id)
      if (bookmarks.itemsByFolder[bm.folderId]) {
        bookmarks.itemsByFolder[bm.folderId] = bookmarks.itemsByFolder[bm.folderId].filter(i => i.id !== bm.id)
      }
      // Decrement folder count
      const folder = bookmarks.folders.find(f => f.id === bm.folderId)
      if (folder && folder.count > 0) folder.count--
      bookmarks.removeBookmarkOpen = false
    } catch (err) {
      console.error('Remove bookmark failed', err)
    } finally {
      bookmarks.removingBm = false
    }
  }

  // ─── COMMENTS STATE ──────────────────────────────────────────
  const COMMENTS_PAGE_SIZE = 20

  const comments = reactive({
    initialized: false,
    loading: false,
    loadingMore: false,
    error: null,
    items: [],       // UserCommentDto[] (each has .expanded for show-more)
    pagination: { totalCount: 0, page: 1, pageSize: COMMENTS_PAGE_SIZE, totalPages: 0, hasNext: false, hasPrevious: false },
    page: 1,
    sortBy: 'newest',
  })

  // Map the backend UserCommentDto, attaching reactive .expanded flag
  function mapUserComment(c) {
    return {
      id: c.id,
      content: c.content,
      postedDate: c.postedDate,
      likesCount: c.likesCount ?? 0,
      dislikesCount: c.dislikesCount ?? 0,
      parentCommentId: c.parentCommentId ?? null,
      targetType: c.targetType,
      titleId: c.titleId ?? null,
      titleName: c.titleName ?? null,
      titleSlug: c.titleSlug ?? null,
      chapterId: c.chapterId ?? null,
      chapterName: c.chapterName ?? null,
      volumeNumber: c.volumeNumber ?? null,
      teamId: c.teamId ?? null,
      expanded: false,
    }
  }

  // Build the "View in context" URL — points directly to where the comment lives:
  //   targetType 1 (Title)   → /{titleSlug}?section=comments&comment_id={id}
  //   targetType 2 (Chapter) → /{titleSlug}/chapter/{name}/v{vol}/t{team}?viewMode=single&comment_id={id}
  //   targetType 3 (Page)    → same as chapter (comment lives in the chapter reader's image section)
  function buildCommentUrl(c) {
    if (!c.titleSlug) return '#'
    const slug = encodeURIComponent(c.titleSlug)

    if (c.targetType === 1) {
      // Title comment — open title page on comments tab at this thread
      return `/${slug}?section=comments&comment_id=${c.id}`
    }

    // Chapter or Page comment — build full chapter reader URL
    if (c.chapterName && c.volumeNumber != null && c.teamId != null) {
      const chName = encodeURIComponent(c.chapterName)
      return `/${slug}/chapter/${chName}/v${c.volumeNumber}/t${c.teamId}?viewMode=single&comment_id=${c.id}`
    }

    // Fallback: open title comments tab with the thread
    return `/${slug}?section=comments&comment_id=${c.id}`
  }

  // formatTimeAgo — matches CommentItem's relative timestamp style
  function formatTimeAgo(dateString) {
    const now = new Date()
    const date = new Date(dateString)
    const secs = Math.floor((now - date) / 1000)
    if (secs < 60) return 'just now'
    const mins = Math.floor(secs / 60)
    if (mins < 60) return `${mins} minute${mins !== 1 ? 's' : ''} ago`
    const hrs = Math.floor(mins / 60)
    if (hrs < 24) return `${hrs} hour${hrs !== 1 ? 's' : ''} ago`
    const days = Math.floor(hrs / 24)
    if (days < 30) return `${days} day${days !== 1 ? 's' : ''} ago`
    const months = Math.floor(days / 30)
    if (months < 12) return `${months} month${months !== 1 ? 's' : ''} ago`
    return `${Math.floor(months / 12)} year${Math.floor(months / 12) !== 1 ? 's' : ''} ago`
  }

  async function loadComments() {
    comments.initialized = true
    comments.loading = true
    comments.error = null
    comments.page = 1

    try {
      const res = await apiClient.get('/Comments/GetMyComments', {
        params: { page: 1, pageSize: COMMENTS_PAGE_SIZE, sortBy: comments.sortBy }
      })
      const data = res.data
      comments.items = (data.comments ?? []).map(mapUserComment)
      comments.pagination = data.pagination ?? comments.pagination
    } catch (err) {
      comments.error = err?.response?.data?.message ?? err.message ?? 'Failed to load comments.'
    } finally {
      comments.loading = false
    }
  }

  async function reloadComments() {
    comments.items = []
    comments.pagination = { ...comments.pagination, hasNext: false }
    await loadComments()
  }

  async function loadMoreComments() {
    if (!comments.pagination.hasNext || comments.loadingMore) return
    comments.loadingMore = true
    comments.page++
    try {
      const res = await apiClient.get('/Comments/GetMyComments', {
        params: { page: comments.page, pageSize: COMMENTS_PAGE_SIZE, sortBy: comments.sortBy }
      })
      const data = res.data
      comments.items.push(...(data.comments ?? []).map(mapUserComment))
      comments.pagination = data.pagination ?? comments.pagination
    } catch {
      // silently stop
    } finally {
      comments.loadingMore = false
    }
  }

  // ─── init ─────────────────────────────────────────────────────
  onMounted(() => {
    // Nothing eager – tabs load on demand
  })
</script>

<style scoped>
  /* ── Banner ───────────────────────────────────────────────── */
  .banner-bg {
    background: radial-gradient(ellipse 80% 60% at 20% 40%, color-mix(in srgb, var(--vt-c-indigo) 35%, transparent), transparent 70%), radial-gradient(ellipse 60% 80% at 80% 60%, color-mix(in srgb, var(--color-accent) 25%, transparent), transparent 70%), var(--color-background-soft);
  }

  /* ── Tab content layout (prevent height jumps on desktop) ── */
  .tab-content-container {
    display: grid;
    grid-template-columns: 1fr;
    width: 100%;
    overflow: hidden;
  }

  .tab-panel {
    grid-column: 1;
    grid-row: 1;
    min-height: 0;
    min-width: 0;
    width: 100%;
    overflow: hidden;
  }

  /* ── Scrollbar hide ──────────────────────────────────────── */
  .scrollbar-hide {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }

    .scrollbar-hide::-webkit-scrollbar {
      display: none;
    }

  /* ── Folder tab carousel (mobile) — mirrors main nav tabs ── */
  .folder-tab {
    position: relative;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 9px 14px 10px;
    font-size: 0.8125rem;
    font-weight: 500;
    color: var(--color-text);
    opacity: 0.6;
    white-space: nowrap;
    cursor: pointer;
    text-decoration: none;
    transition: opacity 0.15s, color 0.15s;
    user-select: none;
  }

    .folder-tab:hover {
      opacity: 0.9;
    }

  .folder-tab-active {
    color: var(--color-heading);
    opacity: 1;
    font-weight: 600;
  }

  .folder-tab-count {
    font-size: 0.6875rem;
    opacity: 0.5;
    font-weight: 400;
  }

  .folder-tab-indicator {
    position: absolute;
    bottom: 0;
    left: 0;
    right: 0;
    height: 2px;
    background: var(--color-accent);
    border-radius: 9999px;
  }

  /* ── Folder sidebar button ───────────────────────────────── */
  .folder-btn {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 7px 10px;
    border-radius: 8px;
    font-size: 0.8125rem;
    color: var(--color-text);
    transition: background 0.15s, color 0.15s;
    cursor: pointer;
  }

    .folder-btn:hover:not(.folder-btn-active) {
      background: var(--color-background-mute);
    }

  .folder-btn-active {
    background: color-mix(in srgb, var(--color-accent) 12%, transparent);
    color: var(--color-accent);
    font-weight: 600;
  }

  .folder-count {
    font-size: 0.7rem;
    color: var(--color-text);
    opacity: 0.4;
    margin-left: auto;
    flex-shrink: 0;
  }

  /* ── Comment history card (profile tab) ─────────────────── */
  .comment-history-card {
    padding: 12px 14px;
    border-radius: 10px;
    border: 1px solid var(--color-border);
    transition: border-color 0.15s;
  }

    .comment-history-card:hover {
      border-color: color-mix(in srgb, var(--color-accent) 40%, transparent);
    }

  .comment-context {
    display: flex;
    align-items: center;
    gap: 5px;
    padding-bottom: 8px;
    border-bottom: 1px solid var(--color-border);
  }

  .comment-action-btn {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    font-size: 0.6875rem;
    color: var(--color-text);
    opacity: 0.55;
    transition: opacity 0.15s, color 0.15s;
    cursor: pointer;
    text-decoration: none;
  }

    .comment-action-btn:hover {
      opacity: 1;
      color: var(--color-accent);
    }
</style>
