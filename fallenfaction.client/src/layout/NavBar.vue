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
                            <i class="nav-icon home-icon"></i>
                            <span class="nav-text">Home</span>
                        </router-link>
                    </div>
                    <div class="nav-item">
                        <router-link to="/home/cataloge" class="nav-link">
                            <i class="nav-icon catalog-icon"></i>
                            <span class="nav-text">Catalog</span>
                        </router-link>
                    </div>
                    <div class="nav-item">
                        <router-link to="/home/search" class="nav-link">
                            <i class="nav-icon search-icon"></i>
                            <span class="nav-text">Search</span>
                        </router-link>
                    </div>
                    <div class="nav-item">
                        <button class="nav-link more-btn" @click="toggleMoreMenu">
                            <i class="nav-icon more-icon"></i>
                        </button>
                        <div v-if="showMoreMenu" class="more-menu">
                            <router-link to="/home/fqa" class="more-menu-item">FAQ</router-link>
                            <router-link to="/home/privacy" class="more-menu-item">Privacy</router-link>
                        </div>
                    </div>
                </div>

                <div class="navbar-right">
                    <!-- For authenticated users -->
                    <template v-if="authStore.isAuthenticated">
                        <!-- Teams dropdown -->
                        <div class="nav-item">
                            <button class="nav-link content-btn" @click="toggleTeamsMenu">
                                <i class="nav-icon content-icon"></i>
                            </button>
                            <div v-if="showTeamsMenu" class="teams-menu">
                                <div class="teams-header">
                                    <span>Content & Teams</span>
                                </div>

                                <!-- Content managment section -->
                                <div class="teams-section">
                                    <router-link to="/user/content" class="teams-menu-item">
                                        <i class="nav-icon content-icon"></i>
                                        Content Management
                                    </router-link>
                                </div>

                                <!-- Teams section -->
                                <div class="teams-section">
                                    <div class="section-header">My Teams</div>
                                    <router-link to="/user/content/myteam" class="teams-menu-item">
                                        <i class="circle-icon"></i>
                                        View All Teams
                                    </router-link>

                                    <!-- Loading indicator -->
                                    <div v-if="loadingTeams" class="loading-teams">
                                        <span>Loading teams...</span>
                                    </div>

                                    <!-- Teams list from API -->
                                    <template v-else-if="teams.length > 0">
                                        <router-link v-for="team in teams"
                                           :key="team.id"
                                           :to="`/user/content/editteam/${team.id}`"
                                           class="teams-menu-item team-item">
                                            <i class="circle-icon team-circle"></i>
                                            {{ team.name }}
                                        </router-link>
                                    </template>

                                    <router-link to="/team/addteam" class="teams-menu-item create-team">
                                        <i class="add-icon"></i> Create New Team
                                    </router-link>
                                </div>
                            </div>
                        </div>

                        <!-- Create Content dropdown -->
                        <div class="nav-item">
                            <button class="nav-link grid-btn" @click="toggleGridMenu">
                                <i class="nav-icon grid-icon"></i>
                            </button>
                            <div v-if="showGridMenu" class="grid-menu">
                                <div class="grid-header">
                                    <span>Add Content</span>
                                </div>
                                <div class="grid-section">
                                    <router-link to="/manga/addtitle" class="grid-menu-item">
                                        <i class="circle-icon title-circle"></i>
                                        Add Title
                                    </router-link>
                                    <router-link to="/author/createa" class="grid-menu-item">
                                        <i class="circle-icon author-circle"></i>
                                        Add Author
                                    </router-link>
                                    <router-link to="/publisher/create" class="grid-menu-item">
                                        <i class="circle-icon publisher-circle"></i>
                                        Add Publisher
                                    </router-link>
                                    <router-link to="/artist/create" class="grid-menu-item">
                                        <i class="circle-icon artist-circle"></i>
                                        Add Artist
                                    </router-link>
                                </div>
                            </div>
                        </div>

                        <div class="nav-item">
                            <router-link to="/home/notification" class="nav-link">
                                <i class="nav-icon notification-icon"></i>
                            </router-link>
                        </div>

                        <!-- Profile Avatar Dropdown -->
                        <div class="nav-item profile-dropdown">
                            <button class="nav-link profile-btn" @click="toggleProfileMenu">
                                <img class="profile-img" :src="authStore.user.profilePicturePath" alt="Profile" />
                            </button>
                            <div v-if="showProfileMenu" class="profile-menu">
                                <router-link to="/profile" class="profile-menu-item">
                                    <i class="circle-icon profile-circle"></i>
                                    My Profile
                                </router-link>
                                <div class="profile-menu-item user-name-item">
                                    <i class="circle-icon user-circle"></i>
                                    {{ authStore.userFullName || 'User' }}
                                </div>
                                <div class="profile-divider"></div>
                                <button @click="logout" class="profile-menu-item logout-btn" :disabled="authStore.isLoading">
                                    <i class="circle-icon logout-circle"></i>
                                    {{ authStore.isLoading ? 'Logging out...' : 'Logout' }}
                                </button>
                            </div>
                        </div>
                    </template>
                    <!-- For non-authenticated users -->
                    <template v-else>
                        <div class="nav-item">
                            <router-link to="/account/login" class="nav-link login-link">Login</router-link>
                        </div>
                        <div class="nav-item">
                            <router-link to="/account/register" class="nav-link register-link">Register</router-link>
                        </div>
                    </template>
                </div>
            </div>

            <!-- Mobile Layout -->
            <div class="mobile-navbar">
                <div class="mobile-nav-left">
                    <!-- Search Button -->
                    <div class="nav-item">
                        <router-link to="/home/search" class="nav-link mobile-search-btn">
                            <i class="nav-icon search-icon"></i>
                        </router-link>
                    </div>
                </div>

                <div class="mobile-nav-right">
                    <!-- Mobile Avatar/Menu Button -->
                    <div class="nav-item">
                        <button class="nav-link mobile-menu-btn" @click="toggleMobileSidebar">
                            <img v-if="authStore.isAuthenticated" class="profile-img mobile-profile" :src="profileImage" alt="Profile" />
                            <i v-else class="nav-icon menu-burger-icon"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Mobile Sidebar Overlay -->
        <div v-if="showMobileSidebar" class="mobile-sidebar-overlay" @click="closeMobileSidebar">
            <div class="mobile-sidebar" @click.stop>
                <!-- Sidebar Header -->
                <div class="sidebar-header">
                    <div v-if="authStore.isAuthenticated" class="sidebar-user-info">
                        <img class="sidebar-profile-img" :src="profileImage" alt="Profile" />
                        <div class="sidebar-user-details">
                            <div class="sidebar-username">{{ authStore.userFullName || 'User' }}</div>
                            <router-link to="/profile" class="sidebar-profile-link">View Profile</router-link>
                        </div>
                    </div>
                    <div v-else class="sidebar-guest-info">
                        <div class="sidebar-brand">Fallen Faction</div>
                    </div>
                    <button class="sidebar-close-btn" @click="closeMobileSidebar">
                        <i class="close-icon">×</i>
                    </button>
                </div>

                <!-- Sidebar Content -->
                <div class="sidebar-content">
                    <!-- Main Navigation -->
                    <div class="sidebar-section">
                        <router-link to="/" class="sidebar-item" @click="closeMobileSidebar">
                            <i class="sidebar-icon home-icon"></i>
                            <span>Home</span>
                        </router-link>
                        <router-link to="/home/cataloge" class="sidebar-item" @click="closeMobileSidebar">
                            <i class="sidebar-icon catalog-icon"></i>
                            <span>Catalog</span>
                        </router-link>
                        <router-link to="/home/search" class="sidebar-item" @click="closeMobileSidebar">
                            <i class="sidebar-icon search-icon"></i>
                            <span>Search</span>
                        </router-link>

                        <!-- More Section -->
                        <div class="sidebar-expandable">
                            <button class="sidebar-item sidebar-toggle" @click="toggleMobileMore">
                                <div class="sidebar-toggle-content">
                                    <i class="sidebar-icon more-icon"></i>
                                    <span>More</span>
                                </div>
                                <i class="sidebar-arrow" :class="{ 'expanded': showMobileMore }">›</i>
                            </button>
                            <div v-if="showMobileMore" class="sidebar-submenu">
                                <router-link to="/home/fqa" class="sidebar-submenu-item" @click="closeMobileSidebar">FAQ</router-link>
                                <router-link to="/home/privacy" class="sidebar-submenu-item" @click="closeMobileSidebar">Privacy</router-link>
                            </div>
                        </div>
                    </div>

                    <!-- Authenticated User Content -->
                    <template v-if="authStore.isAuthenticated">
                        <div class="sidebar-divider"></div>

                        <!-- Content & Teams Section -->
                        <div class="sidebar-section">
                            <div class="sidebar-expandable">
                                <button class="sidebar-item sidebar-toggle" @click="toggleMobileTeams">
                                    <div class="sidebar-toggle-content">
                                        <i class="sidebar-icon content-icon"></i>
                                        <span>Content & Teams</span>
                                    </div>
                                    <i class="sidebar-arrow" :class="{ 'expanded': showMobileTeams }">›</i>
                                </button>
                                <div v-if="showMobileTeams" class="sidebar-submenu">
                                    <router-link to="/user/content" class="sidebar-submenu-item" @click="closeMobileSidebar">
                                        Content Management
                                    </router-link>
                                    <router-link to="/user/content/myteam" class="sidebar-submenu-item" @click="closeMobileSidebar">
                                        View All Teams
                                    </router-link>

                                    <!-- Teams List -->
                                    <div v-if="loadingTeams" class="sidebar-loading">
                                        Loading teams...
                                    </div>
                                    <template v-else-if="teams.length > 0">
                                        <div v-for="team in teams" :key="team.id" class="sidebar-team-item">
                                            <router-link :to="`/user/content/editteam/${team.id}`"
                                               class="sidebar-submenu-item"
                                               @click="closeMobileSidebar">
                                                <i class="team-dot"></i>
                                                {{ team.name }}
                                            </router-link>
                                        </div>
                                    </template>

                                    <router-link to="/team/addteam" class="sidebar-submenu-item create-team" @click="closeMobileSidebar">
                                        <i class="add-icon-small">+</i>
                                        Create New Team
                                    </router-link>
                                </div>
                            </div>
                        </div>

                        <!-- Add Content Section -->
                        <div class="sidebar-section">
                            <div class="sidebar-expandable">
                                <button class="sidebar-item sidebar-toggle" @click="toggleMobileAddContent">
                                    <div class="sidebar-toggle-content">
                                        <i class="sidebar-icon grid-icon"></i>
                                        <span>Add Content</span>
                                    </div>
                                    <i class="sidebar-arrow" :class="{ 'expanded': showMobileAddContent }">›</i>
                                </button>
                                <div v-if="showMobileAddContent" class="sidebar-submenu">
                                    <router-link to="/manga/addtitle" class="sidebar-submenu-item" @click="closeMobileSidebar">
                                        <i class="content-dot title-dot"></i>
                                        Add Title
                                    </router-link>
                                    <router-link to="/author/createa" class="sidebar-submenu-item" @click="closeMobileSidebar">
                                        <i class="content-dot author-dot"></i>
                                        Add Author
                                    </router-link>
                                    <router-link to="/publisher/create" class="sidebar-submenu-item" @click="closeMobileSidebar">
                                        <i class="content-dot publisher-dot"></i>
                                        Add Publisher
                                    </router-link>
                                    <router-link to="/people/create" class="sidebar-submenu-item" @click="closeMobileSidebar">
                                        <i class="content-dot artist-dot"></i>
                                        Add Artist
                                    </router-link>
                                </div>
                            </div>
                        </div>

                        <!-- Notifications -->
                        <div class="sidebar-section">
                            <router-link to="/home/notification" class="sidebar-item" @click="closeMobileSidebar">
                                <i class="sidebar-icon notification-icon"></i>
                                <span>Notifications</span>
                            </router-link>
                        </div>

                        <div class="sidebar-divider"></div>

                        <!-- logout -->
                        <div class="sidebar-section">
                            <button @click="logout" class="sidebar-item logout-item" :disabled="authStore.isLoading">
                                <i class="sidebar-icon logout-icon"></i>
                                <span>{{ authStore.isLoading ? 'Logging out...' : 'Logout' }}</span>
                            </button>
                        </div>
                    </template>

                    <!-- Non-authenticated User Content -->
                    <template v-else>
                        <div class="sidebar-divider"></div>
                        <div class="sidebar-section">
                            <router-link to="/account/login" class="sidebar-item" @click="closeMobileSidebar">
                                <i class="sidebar-icon login-icon"></i>
                                <span>Login</span>
                            </router-link>
                            <router-link to="/account/register" class="sidebar-item" @click="closeMobileSidebar">
                                <i class="sidebar-icon register-icon"></i>
                                <span>Register</span>
                            </router-link>
                        </div>
                    </template>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { teamService } from '../services/teamService';


const router = useRouter();
const authStore = useAuthStore();

// Reactive state
const profileImage = ref('/img/logo.png');
const showMoreMenu = ref(false);
const showTeamsMenu = ref(false);
const showGridMenu = ref(false);
const showProfileMenu = ref(false);
const showMobileSidebar = ref(false);
const showMobileMore = ref(false);
const showMobileTeams = ref(false);
const showMobileAddContent = ref(false);
const teams = ref([]);
const loadingTeams = ref(false);

// Methods
const toggleProfileMenu = () => {
    showProfileMenu.value = !showProfileMenu.value;
    if (showProfileMenu.value) {
        showMoreMenu.value = false;
        showTeamsMenu.value = false;
        showGridMenu.value = false;
    }
};

const toggleMoreMenu = () => {
    showMoreMenu.value = !showMoreMenu.value;
    if (showMoreMenu.value) {
        showProfileMenu.value = false;
        showTeamsMenu.value = false;
        showGridMenu.value = false;
    }
};

const toggleTeamsMenu = () => {
    showTeamsMenu.value = !showTeamsMenu.value;
    if (showTeamsMenu.value) {
        showProfileMenu.value = false;
        showMoreMenu.value = false;
        showGridMenu.value = false;
        fetchUserTeams();
    }
};

const toggleGridMenu = () => {
    showGridMenu.value = !showGridMenu.value;
    if (showGridMenu.value) {
        showProfileMenu.value = false;
        showMoreMenu.value = false;
        showTeamsMenu.value = false;
    }
};

const toggleMobileSidebar = () => {
    showMobileSidebar.value = !showMobileSidebar.value;
    if (showMobileSidebar.value && authStore.isAuthenticated) {
        fetchUserTeams();
    }
    // Prevent body scroll when sidebar is open
    document.body.style.overflow = showMobileSidebar.value ? 'hidden' : '';
};

const closeMobileSidebar = () => {
    showMobileSidebar.value = false;
    document.body.style.overflow = '';
    // Reset mobile submenu states
    showMobileMore.value = false;
    showMobileTeams.value = false;
    showMobileAddContent.value = false;
};

const toggleMobileMore = () => {
    showMobileMore.value = !showMobileMore.value;
};

const toggleMobileTeams = () => {
    showMobileTeams.value = !showMobileTeams.value;
    if (showMobileTeams.value) {
        fetchUserTeams();
    }
};

const toggleMobileAddContent = () => {
    showMobileAddContent.value = !showMobileAddContent.value;
};

const logout = async () => {
    closeMobileSidebar();
    
    try {
        await authStore.logout();
        // Redirect to home after logout
        router.push('/');
    } catch (error) {
        console.error('Logout error:', error);
        // Fallback: redirect to home page
        router.push('/');
    }
};

  const fetchUserTeams = async () => {
    if ((teams.value.length === 0) && !loadingTeams.value && authStore.isAuthenticated) {
      loadingTeams.value = true;

      try {
        const result = await teamService.getMyTeams();
        if (result.success) {
          teams.value = result.data || [];
        } else {
          console.error('Error fetching teams:', result.error);
          teams.value = [];
        }
      } catch (error) {
        console.error('Error fetching teams:', error);
        teams.value = [];
      } finally {
        loadingTeams.value = false;
      }
    }
  };

const handleClickOutside = (event) => {
    if (showProfileMenu.value && !event.target.closest('.profile-btn') && !event.target.closest('.profile-menu')) {
        showProfileMenu.value = false;
    }

    if (showMoreMenu.value && !event.target.closest('.more-btn') && !event.target.closest('.more-menu')) {
        showMoreMenu.value = false;
    }

    if (showTeamsMenu.value && !event.target.closest('.content-btn') && !event.target.closest('.teams-menu')) {
        showTeamsMenu.value = false;
    }

    if (showGridMenu.value && !event.target.closest('.grid-btn') && !event.target.closest('.grid-menu')) {
        showGridMenu.value = false;
    }
};

const handleScroll = () => {
    if (showMobileSidebar.value) {
        closeMobileSidebar();
    }
};

// Lifecycle
onMounted(() => {
    // Close menus when clicking outside (desktop)
    document.addEventListener('click', handleClickOutside);
    // Handle scroll events to close mobile sidebar
    window.addEventListener('scroll', handleScroll);
});

onUnmounted(() => {
    // Clean up event listeners
    document.removeEventListener('click', handleClickOutside);
    window.removeEventListener('scroll', handleScroll);
    document.body.style.overflow = '';
});
</script>

<style scoped>
/* Keep all your existing CSS styles exactly as they are */
.navbar {
    background-color: #212121;
    width: 100vw;
    height: 60px;
    color: white;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
    position: fixed;
    top: 0;
    left: 0;
    z-index: 1000;
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

/* Desktop Navbar Styles */
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

.navbar-left, .navbar-right, .navbar-center {
    display: flex;
    align-items: center;
}

.navbar-center {
    flex: 1;
    justify-content: center;
    margin-left: 40px;
}

.navbar-brand {
    font-size: 18px;
    font-weight: 500;
    margin-right: 50px;
    white-space: nowrap;
    color: white;
    text-decoration: none;
    padding: 8px 12px;
    border-radius: 4px;
    transition: background-color 0.2s, color 0.2s;
}

    .navbar-brand:hover {
        background-color: rgba(255, 255, 255, 0.1);
        color: white;
        text-decoration: none;
    }

.profile-dropdown {
    position: relative;
}

.profile-btn {
    background: none;
    border: none;
    cursor: pointer;
    padding: 4px;
    border-radius: 50%;
    transition: background-color 0.2s;
}

    .profile-btn:hover {
        background-color: rgba(255, 255, 255, 0.1);
    }

.profile-menu {
    position: absolute;
    top: 100%;
    right: 0;
    background-color: #333;
    border-radius: 4px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
    z-index: 1000;
    min-width: 200px;
    overflow: hidden;
    margin-top: 5px;
}

.profile-menu-item {
    display: flex;
    align-items: center;
    color: white;
    text-decoration: none;
    padding: 12px 15px;
    transition: background-color 0.2s;
    border: none;
    background: none;
    width: 100%;
    text-align: left;
    cursor: pointer;
    font-size: 14px;
}

    .profile-menu-item:hover {
        background-color: rgba(255, 255, 255, 0.1);
    }

.user-name-item {
    color: #aaa;
    cursor: default;
    font-weight: 500;
}

    .user-name-item:hover {
        background-color: transparent;
    }

.logout-btn {
    color: #ff6b6b;
}

    .logout-btn:hover {
        background-color: rgba(255, 107, 107, 0.1);
    }

.profile-divider {
    height: 1px;
    background-color: rgba(255, 255, 255, 0.1);
    margin: 5px 0;
}

.profile-circle {
    background-color: #4caf50;
    opacity: 0.7;
}

.user-circle {
    background-color: #2196f3;
    opacity: 0.7;
}

.logout-circle {
    background-color: #ff6b6b;
    opacity: 0.7;
}

.nav-item {
    position: relative;
    margin: 0 5px;
}

.nav-link {
    display: flex;
    align-items: center;
    color: white;
    text-decoration: none;
    padding: 8px 12px;
    border-radius: 4px;
    transition: background-color 0.2s;
    white-space: nowrap;
}

    .nav-link:hover {
        background-color: rgba(255, 255, 255, 0.1);
    }

.nav-icon {
    width: 20px;
    height: 20px;
    margin-right: 5px;
    background-size: contain;
    background-repeat: no-repeat;
    background-position: center;
    flex-shrink: 0;
}

.home-icon {
    background-image: url('../navicons/home.svg');
}

.catalog-icon {
    background-image: url('../navicons/catalog.svg');
}

.search-icon {
    background-image: url('../navicons/search.svg');
}

.more-icon {
    background-image: url('../navicons/more.svg');
}

.content-icon {
    background-image: url('../navicons/content.svg');
}

.grid-icon {
    background-image: url('../navicons/addtab.svg');
}

.notification-icon {
    background-image: url('../navicons/notification.svg');
}

.more-btn, .content-btn, .grid-btn {
    background: none;
    border: none;
    color: white;
    cursor: pointer;
}

.more-menu, .teams-menu, .grid-menu {
    position: absolute;
    top: 100%;
    left: 0;
    background-color: #333;
    border-radius: 4px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
    z-index: 1000;
    min-width: 220px;
    overflow: hidden;
}

.teams-menu, .grid-menu {
    right: 0;
    left: auto;
}

.teams-header, .grid-header {
    padding: 10px 15px;
    background-color: #444;
    font-weight: 500;
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.teams-section, .grid-section {
    padding-bottom: 10px;
}

    .teams-section:not(:last-child) {
        border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    }

.section-header {
    padding: 8px 15px;
    font-size: 0.9rem;
    color: #aaa;
    font-weight: 500;
}

/* Create simple CSS-based icons instead of using SVG files */
.circle-icon {
    display: inline-block;
    width: 16px;
    height: 16px;
    border-radius: 50%;
    background-color: white;
    opacity: 0.7;
    margin-right: 8px;
    flex-shrink: 0;
}

.team-circle {
    opacity: 0.5;
    background-color: #4caf50;
}

.title-circle {
    background-color: #f44336;
    opacity: 0.5;
}

.author-circle {
    background-color: #2196f3;
    opacity: 0.5;
}

.publisher-circle {
    background-color: #ff9800;
    opacity: 0.5;
}

.artist-circle {
    background-color: #9c27b0;
    opacity: 0.5;
}

.loading-teams {
    padding: 5px 15px;
    color: #aaa;
    font-size: 0.9rem;
    font-style: italic;
}

.team-item {
    padding-left: 25px;
}

.more-menu-item, .teams-menu-item, .grid-menu-item {
    display: flex;
    align-items: center;
    color: white;
    text-decoration: none;
    padding: 10px 15px;
    transition: background-color 0.2s;
}

    .more-menu-item:hover, .teams-menu-item:hover, .grid-menu-item:hover {
        background-color: rgba(255, 255, 255, 0.1);
    }

.create-team {
    display: flex;
    align-items: center;
    margin-top: 5px;
}

.add-icon {
    display: inline-block;
    width: 14px;
    height: 14px;
    position: relative;
    margin-right: 8px;
    flex-shrink: 0;
}

    .add-icon:before, .add-icon:after {
        content: '';
        position: absolute;
        background-color: white;
    }

    .add-icon:before {
        width: 14px;
        height: 2px;
        top: 6px;
        left: 0;
    }

    .add-icon:after {
        width: 2px;
        height: 14px;
        top: 0;
        left: 6px;
    }

.profile-img {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    object-fit: cover;
    flex-shrink: 0;
}

.profile-link {
    padding: 0;
    margin-left: 10px;
}

button.nav-link {
    background: none;
    border: none;
    cursor: pointer;
    display: flex;
    align-items: center;
}

.navbar-right .nav-icon {
    margin-right: 0;
}

/* Mobile Sidebar Styles */
.mobile-sidebar-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100vh;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 2000;
    display: flex;
    justify-content: flex-end;
}

.mobile-sidebar {
    width: 300px;
    max-width: 85vw;
    height: 100vh;
    background-color: #2a2a2a;
    box-shadow: -2px 0 10px rgba(0, 0, 0, 0.3);
    display: flex;
    flex-direction: column;
    animation: slideInRight 0.3s ease-out;
}

@keyframes slideInRight {
    from {
        transform: translateX(100%);
    }

    to {
        transform: translateX(0);
    }
}

.sidebar-header {
    padding: 20px;
    border-bottom: 1px solid #3a3a3a;
    display: flex;
    justify-content: space-between;
    align-items: center;
    min-height: 80px;
}

.sidebar-user-info {
    display: flex;
    align-items: center;
    flex: 1;
}

.sidebar-profile-img {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    object-fit: cover;
    margin-right: 12px;
}

.sidebar-user-details {
    flex: 1;
}

.sidebar-username {
    font-weight: 500;
    color: white;
    margin-bottom: 4px;
}

.sidebar-profile-link {
    color: #aaa;
    text-decoration: none;
    font-size: 12px;
}

    .sidebar-profile-link:hover {
        color: white;
    }

.sidebar-guest-info {
    flex: 1;
}

.sidebar-brand {
    font-size: 18px;
    font-weight: 500;
    color: white;
}

.sidebar-close-btn {
    background: none;
    border: none;
    color: #aaa;
    font-size: 24px;
    cursor: pointer;
    padding: 5px;
    margin-left: 10px;
    transition: color 0.2s;
}

    .sidebar-close-btn:hover {
        color: white;
    }

.close-icon {
    font-style: normal;
}

.sidebar-content {
    flex: 1;
    overflow-y: auto;
    padding: 10px 0;
}

.sidebar-section {
    margin-bottom: 10px;
}

.sidebar-item {
    display: flex;
    align-items: center;
    padding: 15px 20px;
    color: white;
    text-decoration: none;
    transition: background-color 0.2s;
    border: none;
    background: none;
    width: 100%;
    text-align: left;
    cursor: pointer;
    font-size: 15px;
}

    .sidebar-item:hover {
        background-color: rgba(255, 255, 255, 0.1);
        color: white;
        text-decoration: none;
    }

.sidebar-icon {
    width: 20px;
    height: 20px;
    margin-right: 15px;
    background-size: contain;
    background-repeat: no-repeat;
    background-position: center;
    flex-shrink: 0;
}

.sidebar-expandable {
    position: relative;
}

.sidebar-toggle {
    justify-content: space-between;
}

.sidebar-toggle-content {
    display: flex;
    align-items: center;
}

.sidebar-arrow {
    font-size: 16px;
    transition: transform 0.2s;
    color: #aaa;
}

    .sidebar-arrow.expanded {
        transform: rotate(90deg);
    }

.sidebar-submenu {
    background-color: rgba(0, 0, 0, 0.2);
    animation: slideDown 0.2s ease-out;
}

@keyframes slideDown {
    from {
        opacity: 0;
        max-height: 0;
    }

    to {
        opacity: 1;
        max-height: 500px;
    }
}

.sidebar-submenu-item {
    display: flex;
    align-items: center;
    padding: 12px 20px 12px 55px;
    color: #ccc;
    text-decoration: none;
    font-size: 14px;
    transition: background-color 0.2s;
}

    .sidebar-submenu-item:hover {
        background-color: rgba(255, 255, 255, 0.1);
        color: white;
        text-decoration: none;
    }

.sidebar-team-item {
    position: relative;
}

.team-dot {
    width: 6px;
    height: 6px;
    background-color: #4caf50;
    border-radius: 50%;
    margin-right: 10px;
    opacity: 0.7;
}

.content-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    margin-right: 10px;
    opacity: 0.7;
}

.title-dot {
    background-color: #f44336;
}

.author-dot {
    background-color: #2196f3;
}

.publisher-dot {
    background-color: #ff9800;
}

.artist-dot {
    background-color: #9c27b0;
}

.add-icon-small {
    width: 16px;
    height: 16px;
    background-color: #4caf50;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    margin-right: 10px;
    font-size: 12px;
    font-weight: bold;
}

.sidebar-loading {
    padding: 12px 20px 12px 55px;
    color: #888;
    font-size: 14px;
    font-style: italic;
}

.sidebar-divider {
    height: 1px;
    background-color: #3a3a3a;
    margin: 10px 0;
}

.logout-item {
    color: #ff6b6b;
}

    .logout-item:hover {
        background-color: rgba(255, 107, 107, 0.1);
        color: #ff6b6b;
    }

.logout-icon {
    background-color: #ff6b6b;
    mask: radial-gradient(circle, transparent 30%, #ff6b6b 30%);
    -webkit-mask: radial-gradient(circle, transparent 30%, #ff6b6b 30%);
}

.login-icon,
.register-icon {
    background-color: #4caf50;
    mask: radial-gradient(circle, transparent 30%, #4caf50 30%);
    -webkit-mask: radial-gradient(circle, transparent 30%, #4caf50 30%);
}

.mobile-search-btn,
.mobile-menu-btn {
    padding: 8px;
    border-radius: 50%;
}

.mobile-profile {
    width: 28px;
    height: 28px;
}

.menu-burger-icon {
    width: 20px;
    height: 20px;
    background-image: linear-gradient(to bottom, white 2px, transparent 2px, transparent 6px, white 6px, white 8px, transparent 8px, transparent 12px, white 12px, white 14px, transparent 14px);
    margin-right: 0;
}

/* Responsive Design */
@media (max-width: 1300px) {
    .navbar-container {
        max-width: 95%;
        padding: 0 15px;
    }
}

@media (max-width: 1024px) {
    .navbar-center {
        margin-left: 20px;
    }

    .navbar-brand {
        margin-right: 20px;
        font-size: 16px;
        padding: 6px 10px;
    }

    .nav-item {
        margin: 0 3px;
    }

    .nav-link {
        padding: 6px 8px;
        font-size: 14px;
    }
}

@media (max-width: 768px) {
    /* Hide desktop navbar, show mobile */
    .desktop-navbar {
        display: none;
    }

    .mobile-navbar {
        display: flex;
    }

    .navbar-container {
        padding: 0 15px;
    }

    .mobile-nav-left,
    .mobile-nav-right {
        display: flex;
        align-items: center;
    }

    .mobile-sidebar {
        width: 280px;
    }

    .sidebar-submenu-item {
        padding-left: 50px;
    }
}

@media (max-width: 480px) {
    .navbar-container {
        padding: 0 10px;
    }

    .mobile-sidebar {
        width: 260px;
    }
}

@media (max-width: 375px) {
    .mobile-sidebar {
        width: 100%;
        max-width: 100%;
    }
}
</style>
