// router/index.js
import { createRouter, createWebHistory } from 'vue-router';
import HomePage from '../HomePage.vue';
import NotFoundPage from '../NotFoundPage.vue';
import ErrorPage from '../ErrorPage.vue';
import Login from '../identity/auth/Login.vue';
import Register from '../identity/auth/Register.vue';
import Profile from '../identity/profile/Profile.vue';
import { useAuthStore } from '../stores/authStore';
import AddTitle from '../components/manga/AddTitle.vue';
import AdminTitleManagement from '../components/admin/AdminTitleManagement.vue';
import TitleManagement from '../components/admin/TitleManagement.vue';

// Title Details Page
import TitleDetailsPage from '../components/title-details/TitleDetailsPage.vue';

// Team Components
import TeamList from '../components/team/TeamList.vue';
import AddTeam from '../components/team/AddTeam.vue';
import TeamDetails from '../components/team/TeamDetails.vue';

// Author Components
import AddAuthor from '../components/author/AddAuthor.vue';
import AuthorList from '../components/author/AuthorList.vue';
import AdminAuthorManagement from '../components/admin/AdminAuthorManagement.vue';

// Publisher Components
import AddPublisher from '../components/publisher/AddPublisher.vue';
import PublisherList from '../components/publisher/PublisherList.vue';
import AdminPublisherManagement from '../components/admin/AdminPublisherManagement.vue';

// Artist Components
import AddArtist from '../components/artist/AddArtist.vue';
import ArtistList from '../components/artist/ArtistList.vue';
import AdminArtistManagement from '../components/admin/AdminArtistManagement.vue';

import AdminUserManagement from '../components/admin/AdminUserManagement.vue';
import AdminTeamManagement from '../components/admin/AdminTeamManagement.vue';


const routes = [
  {
    path: '/',
    name: 'Home',
    component: HomePage
  },
  // Authentication routes
  {
    path: '/account/login',
    name: 'Login',
    component: Login,
    meta: {
      requiresGuest: true,
      title: 'Login'
    }
  },
  {
    path: '/account/register',
    name: 'Register',
    component: Register,
    meta: {
      requiresGuest: true,
      title: 'Register'
    }
  },
  // Protected routes
  {
    path: '/profile',
    name: 'Profile',
    component: Profile,
    meta: {
      requiresAuth: true,
      title: 'Profile'
    }
  },
  {
    path: '/manga/addtitle',
    name: 'Add Title',
    component: AddTitle,
    meta: {
      requiresAuth: true,
      title: 'Add Title'
    }
  },

  // Author routes
  // Public viewing
  {
    path: '/authors',
    name: 'Authors',
    component: AuthorList,
    meta: {
      title: 'Authors'
    }
  },
  // User creation routes (authenticated users, not admin-only)
  {
    path: '/author/createa',
    name: 'Create Author',
    component: AddAuthor,
    meta: {
      requiresAuth: true,
      title: 'Create Author'
    }
  },
  // Admin-only creation route (keeping this for backward compatibility)
  {
    path: '/author/create',
    name: 'Add Author Admin',
    component: AddAuthor,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Add Author'
    }
  },

  // Publisher routes
  // Public viewing
  {
    path: '/publishers',
    name: 'Publishers',
    component: PublisherList,
    meta: {
      title: 'Publishers'
    }
  },
  // User creation route (authenticated users, not admin-only)
  {
    path: '/publisher/create',
    name: 'Create Publisher',
    component: AddPublisher,
    meta: {
      requiresAuth: true,
      title: 'Create Publisher'
    }
  },

  // Artist routes
  // Public viewing
  {
    path: '/artists',
    name: 'Artists',
    component: ArtistList,
    meta: {
      title: 'Artists'
    }
  },
  // User creation route (authenticated users, not admin-only)
  {
    path: '/artist/create',
    name: 'Create Artist',
    component: AddArtist,
    meta: {
      requiresAuth: true,
      title: 'Create Artist'
    }
  },

  // Team routes
  {
    path: '/teams',
    name: 'Teams',
    component: TeamList,
    meta: {
      requiresAuth: true,
      title: 'Teams'
    }
  },
  {
    path: '/user/content/myteam',
    name: 'My Teams',
    component: TeamList,
    meta: {
      requiresAuth: true,
      title: 'My Teams'
    }
  },
  {
    path: '/team/addteam',
    name: 'Add Team',
    component: AddTeam,
    meta: {
      requiresAuth: true,
      title: 'Create Team'
    }
  },
  {
    path: '/team/:id',
    name: 'Team Details',
    component: TeamDetails,
    props: route => ({
      teamId: parseInt(route.params.id)
    }),
    meta: {
      requiresAuth: true,
      title: 'Team Details'
    }
  },
  {
    path: '/user/content/editteam/:id',
    name: 'Edit Team',
    component: TeamDetails,
    props: route => ({
      teamId: parseInt(route.params.id)
    }),
    meta: {
      requiresAuth: true,
      title: 'Edit Team'
    }
  },

  // Admin routes (require admin privileges)
  {
    path: '/admin/authors',
    name: 'Admin Author Management',
    component: AdminAuthorManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Author Management'
    }
  },
  {
    path: '/admin/publishers',
    name: 'Admin Publisher Management',
    component: AdminPublisherManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Publisher Management'
    }
  },
  {
    path: '/admin/artists',
    name: 'Admin Artist Management',
    component: AdminArtistManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Artist Management'
    }
  },
  {
    path: '/admin/titles/add',
    name: 'Admin New Titles Management',
    component: AdminTitleManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - New Titles Management'
    }
  },
  {
    path: '/admin/titles',
    name: 'Title Management',
    component: TitleManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Title Management'
    }
  },
  {
    path: '/admin/users',
    name: 'Admin User Management',
    component: AdminUserManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - User Management'
    }
  },
  {
    path: '/admin/teams',
    name: 'Admin Team Management',
    component: AdminTeamManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Team Management'
    }
  },

  // Error pages
  {
    path: '/error/:code',
    name: 'Error',
    component: ErrorPage,
    props: route => ({
      statusCode: parseInt(route.params.code),
      message: route.query.message || '',
      path: route.query.path || '',
      showRetry: route.query.retry === 'true'
    })
  },

  // Title Details Route - This should be near the end but before the catch-all 404
  {
    path: '/:titleName',
    name: 'TitleDetails',
    component: TitleDetailsPage,
    props: route => ({
      titleName: decodeURIComponent(route.params.titleName)
    }),
    meta: {
      title: 'Title Details'
    },
    // Add a beforeEnter guard to validate the title name format if needed
    beforeEnter: (to, from, next) => {
      const titleName = decodeURIComponent(to.params.titleName);

      // Basic validation - reject if it looks like a system route
      const systemRoutes = ['api', 'admin', 'account', 'user', 'team', 'manga', 'author', 'artist', 'publisher', 'error'];
      const firstSegment = titleName.split('/')[0].toLowerCase();

      if (systemRoutes.includes(firstSegment)) {
        next('/404');
        return;
      }

      next();
    }
  },

  // 404 - This should be the last route
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: NotFoundPage
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

// Global navigation guards
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore();

  // Set page title
  document.title = to.meta.title ? `${to.meta.title} - FallenFaction` : 'FallenFaction';

  // Initialize auth state if not already done
  if (!authStore.isInitialized) {
    await authStore.initializeAuth();
  }

  // Check if route requires authentication
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({
      name: 'Login',
      query: { redirect: to.fullPath }
    });
    return;
  }

  // Check if route requires admin role
  if (to.meta.requiresAdmin && !authStore.isAdmin) {
    // If user is authenticated but not admin, show 403 error
    if (authStore.isAuthenticated) {
      next({
        name: 'Error',
        params: { code: '403' },
        query: {
          message: 'Access denied. Admin privileges required.',
          path: to.fullPath
        }
      });
    } else {
      // If not authenticated, redirect to login
      next({
        name: 'Login',
        query: { redirect: to.fullPath }
      });
    }
    return;
  }

  // Check if route requires guest (logged out user)
  if (to.meta.requiresGuest && authStore.isAuthenticated) {
    next({ name: 'Home' });
    return;
  }

  next();
});

// Error handling for navigation failures
router.onError((error) => {
  console.error('Router error:', error);
  router.push('/error/500?message=Navigation error occurred');
});

export default router;
