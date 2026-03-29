<template>
  <div class="navbar">
    <div class="navbar-container">
      <!-- Desktop Layout -->
      <div class="desktop-navbar">
        <div class="navbar-left">
          <router-link to="/" class="navbar-brand">Fallen Faction</router-link>
        </div>

        <div class="navbar-center">
          <div class="nav-item">
            <router-link to="/" class="nav-link">
              <Home class="nav-icon-lucide" :size="20" />
              <span class="nav-text">Home</span>
            </router-link>
          </div>
          <div class="nav-item">
            <router-link to="/catalog" class="nav-link">
              <BookOpen class="nav-icon-lucide" :size="20" />
              <span class="nav-text">Catalog</span>
            </router-link>
          </div>
          <div class="nav-item">
            <router-link to="/novel/voting" class="nav-link">
              <Sparkles class="nav-icon-lucide" :size="20" />
              <span class="nav-text">AI Voting</span>
            </router-link>
          </div>

          <!-- More Dropdown -->
          <DropdownMenu :modal="false">
            <DropdownMenuTrigger as-child>
              <Button variant="ghost" size="sm" class="nav-link">
                <MoreHorizontal class="nav-icon-lucide" :size="20" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" class="dropdown-menu-content" :sideOffset="5">
              <DropdownMenuLabel class="dropdown-menu-label">Info</DropdownMenuLabel>
              <DropdownMenuSeparator class="dropdown-menu-separator" />
              <DropdownMenuGroup>
                <DropdownMenuItem as-child class="dropdown-menu-item">
                  <router-link to="/about" class="dropdown-item-link">
                    <Info :size="16" class="mr-2" />About
                  </router-link>
                </DropdownMenuItem>
                <DropdownMenuItem as-child class="dropdown-menu-item">
                  <router-link to="/faq" class="dropdown-item-link">
                    <HelpCircle :size="16" class="mr-2" />FAQ
                  </router-link>
                </DropdownMenuItem>
                <DropdownMenuItem as-child class="dropdown-menu-item">
                  <router-link to="/contact" class="dropdown-item-link">
                    <Mail :size="16" class="mr-2" />Contact
                  </router-link>
                </DropdownMenuItem>
              </DropdownMenuGroup>
              <DropdownMenuSeparator class="dropdown-menu-separator" />
              <DropdownMenuLabel class="dropdown-menu-label">Legal</DropdownMenuLabel>
              <DropdownMenuSeparator class="dropdown-menu-separator" />
              <DropdownMenuGroup>
                <DropdownMenuItem as-child class="dropdown-menu-item">
                  <router-link to="/privacy" class="dropdown-item-link">
                    <Shield :size="16" class="mr-2" />Privacy Policy
                  </router-link>
                </DropdownMenuItem>
                <DropdownMenuItem as-child class="dropdown-menu-item">
                  <router-link to="/terms" class="dropdown-item-link">
                    <FileText :size="16" class="mr-2" />Terms of Service
                  </router-link>
                </DropdownMenuItem>
                <DropdownMenuItem as-child class="dropdown-menu-item">
                  <router-link to="/dmca" class="dropdown-item-link">
                    <Copyright :size="16" class="mr-2" />DMCA
                  </router-link>
                </DropdownMenuItem>
              </DropdownMenuGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <div class="navbar-right">
          <!-- Theme Toggle (Desktop) -->
          <Button variant="ghost" size="icon" class="theme-toggle-btn" @click="toggleTheme" :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'">
            <Sun v-if="isDark" :size="18" />
            <Moon v-else :size="18" />
          </Button>

          <!-- Global Search -->
          <div class="nav-item">
            <GlobalSearch ref="globalSearchRef" />
          </div>

          <!-- Authenticated users -->
          <template v-if="authStore.isAuthenticated">
            <!-- Teams Dropdown -->
            <DropdownMenu :modal="false">
              <DropdownMenuTrigger as-child>
                <Button variant="ghost" size="sm" class="nav-link" @click="fetchUserTeams">
                  <Layers class="nav-icon-lucide" :size="20" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" class="dropdown-menu-content w-56" :sideOffset="5">
                <DropdownMenuLabel class="dropdown-menu-label">Content & Teams</DropdownMenuLabel>
                <DropdownMenuSeparator class="dropdown-menu-separator" />
                <DropdownMenuGroup>
                  <DropdownMenuItem as-child class="dropdown-menu-item">
                    <router-link to="/user/content" class="dropdown-item-link">
                      <FolderOpen :size="16" class="mr-2" />Content Management
                    </router-link>
                  </DropdownMenuItem>
                </DropdownMenuGroup>
                <DropdownMenuSeparator class="dropdown-menu-separator" />
                <DropdownMenuLabel class="dropdown-menu-label">My Teams</DropdownMenuLabel>
                <DropdownMenuGroup>
                  <DropdownMenuItem as-child class="dropdown-menu-item">
                    <router-link to="/user/content/myteam" class="dropdown-item-link">
                      <Users :size="16" class="mr-2" />View All Teams
                    </router-link>
                  </DropdownMenuItem>
                  <template v-if="loadingTeams">
                    <div class="px-3 py-2 text-sm" style="color: var(--sidebar-subtext)">Loading teams…</div>
                  </template>
                  <template v-else-if="teams.length > 0">
                    <DropdownMenuItem v-for="team in teams" :key="team.id" as-child class="dropdown-menu-item">
                      <router-link :to="`/user/content/editteam/${team.id}`" class="dropdown-item-link pl-8">
                        <div class="w-2 h-2 rounded-full bg-green-500 mr-2"></div>
                        {{ team.name }}
                      </router-link>
                    </DropdownMenuItem>
                  </template>
                </DropdownMenuGroup>
                <DropdownMenuSeparator class="dropdown-menu-separator" />
                <DropdownMenuItem as-child class="dropdown-menu-item">
                  <router-link to="/team/addteam" class="dropdown-item-link">
                    <PlusCircle :size="16" class="mr-2" />Create New Team
                  </router-link>
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>

            <!-- Add Content Dropdown -->
            <DropdownMenu :modal="false">
              <DropdownMenuTrigger as-child>
                <Button variant="ghost" size="sm" class="nav-link">
                  <Grid3x3 class="nav-icon-lucide" :size="20" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" class="dropdown-menu-content w-56" :sideOffset="5">
                <DropdownMenuLabel class="dropdown-menu-label">Add Content</DropdownMenuLabel>
                <DropdownMenuSeparator class="dropdown-menu-separator" />
                <DropdownMenuGroup>
                  <DropdownMenuItem as-child class="dropdown-menu-item">
                    <router-link to="/novel/addtitle" class="dropdown-item-link">
                      <BookPlus :size="16" class="mr-2 text-red-400" />Add Title
                    </router-link>
                  </DropdownMenuItem>
                  <DropdownMenuItem as-child class="dropdown-menu-item">
                    <router-link to="/author/CreateA" class="dropdown-item-link">
                      <UserPlus :size="16" class="mr-2 text-blue-400" />Add Author
                    </router-link>
                  </DropdownMenuItem>
                  <DropdownMenuItem as-child class="dropdown-menu-item">
                    <router-link to="/publisher/create" class="dropdown-item-link">
                      <Building2 :size="16" class="mr-2 text-orange-400" />Add Publisher
                    </router-link>
                  </DropdownMenuItem>
                  <DropdownMenuItem as-child class="dropdown-menu-item">
                    <router-link to="/artist/create" class="dropdown-item-link">
                      <Palette :size="16" class="mr-2 text-purple-400" />Add Artist
                    </router-link>
                  </DropdownMenuItem>
                </DropdownMenuGroup>
              </DropdownMenuContent>
            </DropdownMenu>

            <div class="nav-item"><WalletWidget /></div>
            <div class="nav-item"><NotificationDropdown /></div>

            <!-- Profile Dropdown -->
            <DropdownMenu :modal="false">
              <DropdownMenuTrigger as-child>
                <Button variant="ghost" size="icon" class="profile-btn">
                  <Avatar class="h-8 w-8">
                    <AvatarImage :src="authStore.user.profilePicturePath" :alt="authStore.userFullName" />
                    <AvatarFallback>{{ getInitials(authStore.userFullName) }}</AvatarFallback>
                  </Avatar>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" class="dropdown-menu-content w-56" :sideOffset="5">
                <DropdownMenuLabel class="dropdown-menu-label">
                  {{ authStore.userFullName || 'My Account' }}
                </DropdownMenuLabel>
                <DropdownMenuSeparator class="dropdown-menu-separator" />
                <DropdownMenuGroup>
                  <DropdownMenuItem as-child class="dropdown-menu-item">
                    <router-link to="/profile" class="dropdown-item-link">
                      <UserCircle :size="16" class="mr-2" />My Profile
                    </router-link>
                  </DropdownMenuItem>
                </DropdownMenuGroup>
                <DropdownMenuSeparator class="dropdown-menu-separator" />
                <DropdownMenuItem @click="logout" :disabled="authStore.isLoading" class="dropdown-menu-item text-red-400 focus:text-red-400">
                  <LogOut :size="16" class="mr-2" />
                  {{ authStore.isLoading ? 'Logging out…' : 'Logout' }}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </template>

          <!-- Guest users -->
          <template v-else>
            <div class="nav-item">
              <Button variant="ghost" size="sm" as-child>
                <router-link to="/account/login" class="nav-link">
                  <LogIn :size="20" class="mr-2" />Login
                </router-link>
              </Button>
            </div>
            <div class="nav-item">
              <Button variant="ghost" size="sm" as-child>
                <router-link to="/account/register" class="nav-link">
                  <UserPlus :size="20" class="mr-2" />Register
                </router-link>
              </Button>
            </div>
          </template>
        </div>
      </div>

      <!-- Mobile Layout -->
      <div class="mobile-navbar">
        <div class="mobile-nav-left">
          <Button variant="ghost" size="icon" class="mobile-search-btn" @click="openGlobalSearch">
            <Search :size="20" />
          </Button>
        </div>

        <!-- Brand centred on mobile -->
        <router-link to="/" class="mobile-brand">Fallen Faction</router-link>

        <div class="mobile-nav-right">
          <!-- Theme Toggle (Mobile) -->
          <Button variant="ghost" size="icon" class="theme-toggle-btn" @click="toggleTheme">
            <Sun v-if="isDark" :size="18" />
            <Moon v-else :size="18" />
          </Button>

          <Button variant="ghost" size="icon" @click="toggleMobileSidebar" class="mobile-menu-btn">
            <Avatar v-if="authStore.isAuthenticated" class="h-7 w-7">
              <AvatarImage :src="authStore.user.profilePicturePath" :alt="authStore.userFullName" />
              <AvatarFallback>{{ getInitials(authStore.userFullName) }}</AvatarFallback>
            </Avatar>
            <Menu v-else :size="20" />
          </Button>
        </div>
      </div>
    </div>

    <!-- Mobile Sidebar Overlay -->
    <div v-if="showMobileSidebar" class="mobile-sidebar-overlay" @click="closeMobileSidebar">
      <div class="mobile-sidebar" @click.stop>
        <!-- Sidebar Header -->
        <div class="sidebar-header">
          <div v-if="authStore.isAuthenticated" class="sidebar-user-info">
            <Avatar class="h-10 w-10">
              <AvatarImage :src="authStore.user.profilePicturePath" :alt="authStore.userFullName" />
              <AvatarFallback>{{ getInitials(authStore.userFullName) }}</AvatarFallback>
            </Avatar>
            <div class="sidebar-user-details">
              <div class="sidebar-username">{{ authStore.userFullName || 'User' }}</div>
              <router-link to="/profile" class="sidebar-profile-link" @click="closeMobileSidebar">View Profile</router-link>
            </div>
          </div>
          <div v-else class="sidebar-guest-info">
            <div class="sidebar-brand">Fallen Faction</div>
          </div>
          <Button variant="ghost" size="icon" @click="closeMobileSidebar" class="sidebar-close-btn">
            <X :size="24" />
          </Button>
        </div>

        <!-- Sidebar Content -->
        <div class="sidebar-content">

          <!-- Main Navigation -->
          <div class="sidebar-section">
            <Button variant="ghost" as-child class="sidebar-item">
              <router-link to="/" @click="closeMobileSidebar">
                <Home :size="20" class="mr-3" /><span>Home</span>
              </router-link>
            </Button>
            <Button variant="ghost" as-child class="sidebar-item">
              <router-link to="/catalog" @click="closeMobileSidebar">
                <BookOpen :size="20" class="mr-3" /><span>Catalog</span>
              </router-link>
            </Button>
            <Button variant="ghost" as-child class="sidebar-item">
              <router-link to="/novel/voting" @click="closeMobileSidebar">
                <Sparkles :size="20" class="mr-3" /><span>AI Voting</span>
              </router-link>
            </Button>

            <!-- More Collapsible -->
            <Collapsible v-model:open="showMobileMore" class="sidebar-expandable">
              <CollapsibleTrigger as-child>
                <Button variant="ghost" class="sidebar-item sidebar-toggle">
                  <div class="sidebar-toggle-content">
                    <MoreHorizontal :size="20" class="mr-3" /><span>More</span>
                  </div>
                  <ChevronRight :size="16" class="sidebar-arrow" :class="{ 'expanded': showMobileMore }" />
                </Button>
              </CollapsibleTrigger>
              <CollapsibleContent class="sidebar-submenu">
                <Button variant="ghost" as-child class="sidebar-submenu-item">
                  <router-link to="/about" @click="closeMobileSidebar"><Info :size="16" class="mr-2" />About</router-link>
                </Button>
                <Button variant="ghost" as-child class="sidebar-submenu-item">
                  <router-link to="/faq" @click="closeMobileSidebar"><HelpCircle :size="16" class="mr-2" />FAQ</router-link>
                </Button>
                <Button variant="ghost" as-child class="sidebar-submenu-item">
                  <router-link to="/contact" @click="closeMobileSidebar"><Mail :size="16" class="mr-2" />Contact</router-link>
                </Button>
                <Button variant="ghost" as-child class="sidebar-submenu-item">
                  <router-link to="/privacy" @click="closeMobileSidebar"><Shield :size="16" class="mr-2" />Privacy Policy</router-link>
                </Button>
                <Button variant="ghost" as-child class="sidebar-submenu-item">
                  <router-link to="/terms" @click="closeMobileSidebar"><FileText :size="16" class="mr-2" />Terms of Service</router-link>
                </Button>
                <Button variant="ghost" as-child class="sidebar-submenu-item">
                  <router-link to="/dmca" @click="closeMobileSidebar"><Copyright :size="16" class="mr-2" />DMCA</router-link>
                </Button>
              </CollapsibleContent>
            </Collapsible>
          </div>

          <!-- Authenticated User Content -->
          <template v-if="authStore.isAuthenticated">
            <Separator class="sidebar-divider" />

            <!-- Content & Teams Collapsible -->
            <div class="sidebar-section">
              <Collapsible v-model:open="showMobileTeams" class="sidebar-expandable">
                <CollapsibleTrigger as-child>
                  <Button variant="ghost" class="sidebar-item sidebar-toggle" @click="onTeamsToggle">
                    <div class="sidebar-toggle-content">
                      <Layers :size="20" class="mr-3" /><span>Content & Teams</span>
                    </div>
                    <ChevronRight :size="16" class="sidebar-arrow" :class="{ 'expanded': showMobileTeams }" />
                  </Button>
                </CollapsibleTrigger>
                <CollapsibleContent class="sidebar-submenu">
                  <Button variant="ghost" as-child class="sidebar-submenu-item">
                    <router-link to="/user/content" @click="closeMobileSidebar">
                      <FolderOpen :size="16" class="mr-2" />Content Management
                    </router-link>
                  </Button>
                  <Button variant="ghost" as-child class="sidebar-submenu-item">
                    <router-link to="/user/content/myteam" @click="closeMobileSidebar">
                      <Users :size="16" class="mr-2" />View All Teams
                    </router-link>
                  </Button>
                  <div v-if="loadingTeams" class="sidebar-loading">Loading teams…</div>
                  <template v-else-if="teams.length > 0">
                    <div v-for="team in teams" :key="team.id" class="sidebar-team-item">
                      <Button variant="ghost" as-child class="sidebar-submenu-item">
                        <router-link :to="`/user/content/editteam/${team.id}`" @click="closeMobileSidebar">
                          <div class="w-2 h-2 rounded-full bg-green-500 mr-2"></div>{{ team.name }}
                        </router-link>
                      </Button>
                    </div>
                  </template>
                  <Button variant="ghost" as-child class="sidebar-submenu-item create-team">
                    <router-link to="/team/addteam" @click="closeMobileSidebar">
                      <PlusCircle :size="16" class="mr-2" />Create New Team
                    </router-link>
                  </Button>
                </CollapsibleContent>
              </Collapsible>
            </div>

            <!-- Add Content Collapsible -->
            <div class="sidebar-section">
              <Collapsible v-model:open="showMobileAddContent" class="sidebar-expandable">
                <CollapsibleTrigger as-child>
                  <Button variant="ghost" class="sidebar-item sidebar-toggle">
                    <div class="sidebar-toggle-content">
                      <Grid3x3 :size="20" class="mr-3" /><span>Add Content</span>
                    </div>
                    <ChevronRight :size="16" class="sidebar-arrow" :class="{ 'expanded': showMobileAddContent }" />
                  </Button>
                </CollapsibleTrigger>
                <CollapsibleContent class="sidebar-submenu">
                  <Button variant="ghost" as-child class="sidebar-submenu-item">
                    <router-link to="/novel/addtitle" @click="closeMobileSidebar">
                      <BookPlus :size="16" class="mr-2 text-red-500" />Add Title
                    </router-link>
                  </Button>
                  <Button variant="ghost" as-child class="sidebar-submenu-item">
                    <router-link to="/author/CreateA" @click="closeMobileSidebar">
                      <UserPlus :size="16" class="mr-2 text-blue-500" />Add Author
                    </router-link>
                  </Button>
                  <Button variant="ghost" as-child class="sidebar-submenu-item">
                    <router-link to="/publisher/create" @click="closeMobileSidebar">
                      <Building2 :size="16" class="mr-2 text-orange-500" />Add Publisher
                    </router-link>
                  </Button>
                  <Button variant="ghost" as-child class="sidebar-submenu-item">
                    <router-link to="/artist/create" @click="closeMobileSidebar">
                      <Palette :size="16" class="mr-2 text-purple-500" />Add Artist
                    </router-link>
                  </Button>
                </CollapsibleContent>
              </Collapsible>
            </div>

            <!-- Notifications -->
            <div class="sidebar-section">
              <div class="sidebar-notification-wrapper">
                <NotificationDropdown :mobile="true" @close-sidebar="closeMobileSidebar" />
              </div>
            </div>

            <Separator class="sidebar-divider" />

            <!-- Logout -->
            <div class="sidebar-section">
              <Button variant="ghost" @click="logout" class="sidebar-item logout-item" :disabled="authStore.isLoading">
                <LogOut :size="20" class="mr-3" />
                <span>{{ authStore.isLoading ? 'Logging out…' : 'Logout' }}</span>
              </Button>
            </div>
          </template>

          <!-- Guest User Content -->
          <template v-else>
            <Separator class="sidebar-divider" />
            <div class="sidebar-section">
              <Button variant="ghost" as-child class="sidebar-item">
                <router-link to="/account/login" @click="closeMobileSidebar">
                  <LogIn :size="20" class="mr-3" /><span>Login</span>
                </router-link>
              </Button>
              <Button variant="ghost" as-child class="sidebar-item">
                <router-link to="/account/register" @click="closeMobileSidebar">
                  <UserPlus :size="20" class="mr-3" /><span>Register</span>
                </router-link>
              </Button>
            </div>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, onMounted, onUnmounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { useAuthStore } from '../stores/authStore'
  import { teamService } from '../services/teamService'
  import { useTheme } from '../composables/useTheme.js'
  import { Button } from '@/components/ui/button'
  import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
  import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
  import { Separator } from '@/components/ui/separator'
  import {
    DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel,
    DropdownMenuSeparator, DropdownMenuTrigger, DropdownMenuGroup,
  } from '@/components/ui/dropdown-menu'
  import {
    Home, BookOpen, Search, MoreHorizontal, Layers, Grid3x3,
    UserCircle, LogOut, LogIn, UserPlus, ChevronRight, X, Menu,
    HelpCircle, Shield, FolderOpen, Users, PlusCircle, BookPlus,
    Building2, Palette, Info, Mail, FileText, Copyright, Sparkles,
    Sun, Moon,
  } from 'lucide-vue-next'

  import GlobalSearch from '@/components/search/GlobalSearch.vue'
  import NotificationDropdown from '@/components/shared/NotificationDropdown.vue'
  import WalletWidget from '@/components/ai/WalletWidget.vue'

  const router = useRouter()
  const authStore = useAuthStore()
  const { isDark, toggleTheme } = useTheme()

  const showMobileSidebar    = ref(false)
  const showMobileMore       = ref(false)
  const showMobileTeams      = ref(false)
  const showMobileAddContent = ref(false)
  const teams                = ref([])
  const loadingTeams         = ref(false)
  const globalSearchRef      = ref(null)

  const getInitials = (name) => {
    if (!name) return 'U'
    return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2)
  }

  const openGlobalSearch = () => globalSearchRef.value?.open()

  const toggleMobileSidebar = () => {
    showMobileSidebar.value = !showMobileSidebar.value
    if (showMobileSidebar.value && authStore.isAuthenticated) fetchUserTeams()
    document.body.style.overflow = showMobileSidebar.value ? 'hidden' : ''
  }

  const closeMobileSidebar = () => {
    showMobileSidebar.value = false
    document.body.style.overflow = ''
    showMobileMore.value       = false
    showMobileTeams.value      = false
    showMobileAddContent.value = false
  }

  const onTeamsToggle = () => { if (!showMobileTeams.value) fetchUserTeams() }

  const logout = async () => {
    closeMobileSidebar()
    try { await authStore.logout(); router.push('/') }
    catch { router.push('/') }
  }

  const fetchUserTeams = async () => {
    if (teams.value.length === 0 && !loadingTeams.value && authStore.isAuthenticated) {
      loadingTeams.value = true
      try {
        const result = await teamService.getMyTeams()
        teams.value = result.success ? (result.data || []) : []
      } catch { teams.value = [] }
      finally { loadingTeams.value = false }
    }
  }

  const handleScroll = () => { if (showMobileSidebar.value) closeMobileSidebar() }

  onMounted(() => window.addEventListener('scroll', handleScroll))
  onUnmounted(() => {
    window.removeEventListener('scroll', handleScroll)
    document.body.style.overflow = ''
  })
</script>

<style>
  /* ── Global dropdown styles (override Radix) ─────────────────────────────── */
  [data-radix-popper-content-wrapper],
  [data-radix-portal] { z-index: 9999 !important; }

  .dropdown-menu-content,
  [role="menu"] {
    background-color: var(--dropdown-bg)       !important;
    border: 1px solid var(--dropdown-border)   !important;
    border-radius: 10px                         !important;
    padding: 6px                                !important;
    box-shadow: var(--dropdown-shadow)          !important;
    min-width: 220px                            !important;
    z-index: 9999                               !important;
    position: fixed                             !important;
    transition: background-color 0.2s ease      !important;
  }

  .dropdown-menu-item,
  [role="menuitem"] {
    color: var(--dropdown-text)     !important;
    padding: 8px 10px               !important;
    border-radius: 6px              !important;
    cursor: pointer                 !important;
    transition: all 0.12s ease      !important;
    font-size: 14px                 !important;
    outline: none                   !important;
    line-height: 1.5                !important;
  }

  .dropdown-menu-item:hover,
  [role="menuitem"]:hover,
  [role="menuitem"][data-highlighted] {
    background-color: var(--dropdown-hover) !important;
    color: var(--dropdown-text)             !important;
  }

  .dropdown-menu-label {
    color: var(--dropdown-text) !important;
    padding: 8px 10px 6px 10px  !important;
    font-size: 14px             !important;
    font-weight: 500            !important;
  }

  .dropdown-menu-separator,
  [role="separator"] {
    background-color: var(--dropdown-border) !important;
    margin: 6px 0                            !important;
    height: 1px                              !important;
  }
</style>

<style scoped>
  /* ── Navbar shell ────────────────────────────────────────────────────────── */
  .navbar {
    background-color: var(--navbar-bg);
    width: 100vw;
    height: 60px;
    color: var(--navbar-text);
    box-shadow: 0 2px 4px rgba(0,0,0,0.12);
    border-bottom: 1px solid var(--navbar-border);
    position: fixed;
    top: 0; left: 0;
    z-index: 1000;
    backdrop-filter: blur(20px) brightness(1.05);
    -webkit-backdrop-filter: blur(20px) brightness(1.05);
    transition: background-color 0.25s ease, border-color 0.25s ease;
  }

  .navbar-container {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0 20px;
    height: 100%;
    max-width: 1272px;
    margin: 0 auto;
    width: 100%;
  }

  .desktop-navbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
  }

  .mobile-navbar {
    display: none;
    justify-content: space-between;
    align-items: center;
    width: 100%;
  }

  .navbar-left,
  .navbar-right,
  .navbar-center {
    display: flex;
    align-items: center;
    gap: 4px;
  }

  .navbar-center {
    flex: 1;
    justify-content: center;
    margin-left: 40px;
  }

  .navbar-brand {
    font-size: 18px;
    font-weight: 600;
    margin-right: 50px;
    white-space: nowrap;
    color: var(--navbar-text);
    text-decoration: none;
    padding: 8px 12px;
    border-radius: 4px;
    transition: background-color 0.2s;
  }
  .navbar-brand:hover { background-color: var(--navbar-hover-bg); }

  .mobile-brand {
    font-size: 16px;
    font-weight: 600;
    color: var(--navbar-text);
    text-decoration: none;
    letter-spacing: 0.01em;
  }

  .nav-item { position: relative; }

  .nav-link {
    display: flex;
    align-items: center;
    color: var(--navbar-text);
    text-decoration: none;
    padding: 8px 12px;
    border-radius: 4px;
    transition: background-color 0.2s;
    white-space: nowrap;
  }
  .nav-link:hover {
    background-color: var(--navbar-hover-bg);
    text-decoration: none;
  }

  .nav-icon-lucide { margin-right: 6px; flex-shrink: 0; }
  .profile-btn { padding: 4px; border-radius: 50%; }

  .dropdown-item-link {
    display: flex;
    align-items: center;
    width: 100%;
    color: inherit;
    text-decoration: none;
  }
  .dropdown-item-link:hover { text-decoration: none; }

  /* Theme toggle */
  .theme-toggle-btn {
    color: var(--navbar-text) !important;
    border-radius: 8px;
    transition: background-color 0.2s, transform 0.2s;
  }
  .theme-toggle-btn:hover {
    background-color: var(--navbar-hover-bg) !important;
    transform: scale(1.1);
  }

  :deep(.dropdown-menu-item svg) { flex-shrink: 0 !important; }

  :deep(.dropdown-menu-content[data-state="open"]) {
    animation: slideIn 0.15s ease-out !important;
  }
  @keyframes slideIn {
    from { opacity: 0; transform: translateY(-5px); }
    to   { opacity: 1; transform: translateY(0);    }
  }

  /* ── Mobile Sidebar ───────────────────────────────────────────────────────── */
  .mobile-sidebar-overlay {
    position: fixed;
    top: 0; left: 0;
    width: 100%; height: 100vh;
    background-color: var(--sidebar-overlay);
    z-index: 2000;
    display: flex;
    justify-content: flex-end;
  }

  .mobile-sidebar {
    width: 300px;
    max-width: 85vw;
    height: 100vh;
    background-color: var(--sidebar-bg);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    box-shadow: -4px 0 20px rgba(0,0,0,0.15);
    display: flex;
    flex-direction: column;
    animation: slideInRight 0.28s ease-out;
    transition: background-color 0.25s ease;
  }

  @keyframes slideInRight {
    from { transform: translateX(100%); }
    to   { transform: translateX(0);   }
  }

  .sidebar-header {
    padding: 20px;
    border-bottom: 1px solid var(--sidebar-divider);
    display: flex;
    justify-content: space-between;
    align-items: center;
    min-height: 80px;
  }

  .sidebar-user-info  { display: flex; align-items: center; flex: 1; gap: 12px; }
  .sidebar-user-details { flex: 1; }

  .sidebar-username { font-weight: 500; color: var(--sidebar-text); margin-bottom: 4px; }

  .sidebar-profile-link {
    color: var(--sidebar-subtext);
    text-decoration: none;
    font-size: 12px;
  }
  .sidebar-profile-link:hover { color: var(--sidebar-text); }

  .sidebar-guest-info { flex: 1; }
  .sidebar-brand { font-size: 18px; font-weight: 600; color: var(--sidebar-text); }

  .sidebar-close-btn { margin-left: 10px; color: var(--sidebar-text); }

  .sidebar-content { flex: 1; overflow-y: auto; padding: 10px 0; }
  .sidebar-section  { margin-bottom: 10px; }

  .sidebar-item {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    padding: 15px 20px;
    color: var(--sidebar-text) !important;
    text-decoration: none;
    width: 100%;
    text-align: left;
    font-size: 15px;
    height: auto;
  }
  .sidebar-item:hover {
    background-color: var(--sidebar-hover) !important;
    text-decoration: none;
  }

  .sidebar-expandable { position: relative; }
  .sidebar-toggle     { justify-content: space-between; }
  .sidebar-toggle-content { display: flex; align-items: center; }

  .sidebar-arrow { transition: transform 0.2s; color: var(--sidebar-subtext); }
  .sidebar-arrow.expanded { transform: rotate(90deg); }

  .sidebar-submenu { background-color: var(--sidebar-submenu-bg); }

  .sidebar-submenu-item {
    display: flex;
    align-items: center;
    padding: 12px 20px 12px 55px;
    color: var(--sidebar-subtext) !important;
    text-decoration: none;
    font-size: 14px;
    justify-content: flex-start;
    width: 100%;
    text-align: left;
    height: auto;
  }
  .sidebar-submenu-item:hover {
    background-color: var(--sidebar-hover) !important;
    color: var(--sidebar-text) !important;
    text-decoration: none;
  }

  .sidebar-team-item { position: relative; }

  .sidebar-loading {
    padding: 12px 20px 12px 55px;
    color: var(--sidebar-subtext);
    font-size: 14px;
    font-style: italic;
  }

  .sidebar-divider {
    background-color: var(--sidebar-divider) !important;
    margin: 10px 0;
  }

  .sidebar-notification-wrapper { padding: 0 12px; }
  .sidebar-notification-wrapper :deep(.notification-trigger) {
    width: 100%;
    justify-content: flex-start;
    padding: 15px 8px;
    font-size: 15px;
    border-radius: 6px;
  }
  .sidebar-notification-wrapper :deep(.notification-dropdown) {
    position: fixed;
    top: 80px; right: 0; left: 0;
    width: 100%;
    max-height: 60vh;
    border-radius: 0;
    border-left: none; border-right: none;
  }

  .logout-item { color: #ef4444 !important; }
  .logout-item:hover { background-color: rgba(239,68,68,0.1) !important; color: #ef4444 !important; }

  /* ── Responsive ──────────────────────────────────────────────────────────── */
  @media (max-width: 1300px) {
    .navbar-container { max-width: 95%; padding: 0 15px; }
  }
  @media (max-width: 1024px) {
    .navbar-center { margin-left: 20px; }
    .navbar-brand  { margin-right: 20px; font-size: 16px; }
  }
  @media (max-width: 768px) {
    .desktop-navbar { display: none; }
    .mobile-navbar  { display: flex; }
    .navbar-container { padding: 0 15px; }
    .mobile-nav-left, .mobile-nav-right { display: flex; align-items: center; gap: 2px; }
    .mobile-sidebar { width: 280px; }
  }
  @media (max-width: 480px) {
    .navbar-container { padding: 0 10px; }
    .mobile-sidebar { width: 260px; }
  }
  @media (max-width: 375px) {
    .mobile-sidebar { width: 100%; max-width: 100%; }
  }
</style>
