// router/index.js - Updated with Content Management Routes
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
import AdminChapterManagement from '../components/admin/AdminChapterManagement.vue';

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
import AdminCommentsManagement from '../components/admin/AdminCommentsManagement.vue';
import AdminTitleChanges from '../components/admin/AdminTitleChanges.vue';

// Content Management Components
import ContentManagement from '../components/content/ContentManagement.vue';
import TitlesManagement from '../components/content/TitlesManagement.vue';
import ChaptersManagement from '../components/content/ChaptersManagement.vue';
import TeamsManagement from '../components/content/TeamsManagement.vue';
import ModerationManagement from '../components/content/ModerationManagement.vue';
import TitleChangeHistory from '../components/title-details/TitleChangeHistory.vue'


import AddChapter from '../components/title-details/AddChapter.vue'
import ChapterReader from '../components/title-details/ChapterReader.vue';

// cataloge
import Catalog from '../components/catalog/Catalog.vue';
import CommentThreadView from '../components/title-details/CommentThreadView.vue';



const routes = [
  {
    path: '/',
    name: 'Home',
    component: HomePage
  },
  {
    path: '/:titleName/chapter/:chapterName/v:volumeNumber/t:teamId',
    name: 'ChapterReader',
    component: ChapterReader,
    props: route => ({
      titleName: decodeURIComponent(route.params.titleName),
      chapterName: decodeURIComponent(route.params.chapterName),
      volumeNumber: parseInt(route.params.volumeNumber),
      teamId: parseInt(route.params.teamId)
    }),
    meta: {
      title: 'Reading Chapter',
      hideNavigation: true, // Hide main site navigation for full-screen reading
      requiresAuth: false
    }
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
  path: '/title/:titleId/change-history',
  name: 'TitleChangeHistory',
  component: TitleChangeHistory,
  props: route => ({
    titleId: parseInt(route.params.titleId)
    // Remove titleName from props - it will be loaded from the API
  }),
  meta: {
    requiresAuth: true,
    title: 'Change History'
  }
},

  // === CONTENT MANAGEMENT ROUTES ===
  // Main content management dashboard
  {
    path: '/user/content',
    name: 'Content Management',
    component: ContentManagement,
    meta: {
      requiresAuth: true,
      title: 'Content Management'
    }
  },
  // Legacy routes that redirect to main content management
  {
    path: '/user/content/index',
    redirect: '/user/content'
  },
  {
    path: '/user/content/uploads',
    redirect: '/user/content'
  },
  // Specific content management sections (can be used for direct linking)
  {
    path: '/user/content/titles',
    name: 'My Titles',
    component: ContentManagement,
    meta: {
      requiresAuth: true,
      title: 'My Titles',
      defaultTab: 'titles'
    }
  },
  {
    path: '/user/content/chapters',
    name: 'My Chapters',
    component: ContentManagement,
    meta: {
      requiresAuth: true,
      title: 'My Chapters',
      defaultTab: 'chapters'
    }
  },
  {
    path: '/user/content/teams',
    name: 'My Teams Content',
    component: ContentManagement,
    meta: {
      requiresAuth: true,
      title: 'My Teams',
      defaultTab: 'teams'
    }
  },
  // Admin moderation routes
  {
    path: '/user/content/moderation',
    name: 'Content Moderation',
    component: ContentManagement,
    meta: {
      requiresAuth: true,
      requiresAdminOrModerator: true,
      title: 'Content Moderation',
      defaultTab: 'moderation'
    }
  },
  // Legacy MVC-style routes for backward compatibility
  {
    path: '/user/content/rejected',
    name: 'Rejected Content',
    component: ContentManagement,
    meta: {
      requiresAuth: true,
      title: 'Rejected Content',
      defaultTab: 'titles'
    }
  },

  // === TITLE AND CHAPTER ROUTES ===
  {
    path: '/manga/addtitle',
    name: 'Add Title',
    component: AddTitle,
    meta: {
      requiresAuth: true,
      title: 'Add Title'
    }
  },
  {
    path: '/:titleName/AddChapter',
    name: 'Add Chapter',
    component: AddChapter,
    meta: {
      requiresAuth: true,
      title: 'Add Chapter'
    }
  },

  // === TEAM ROUTES ===
  // Public viewing
  {
    path: '/teams',
    name: 'Teams',
    component: TeamList,
    meta: {
      requiresAuth: true,
      title: 'Teams'
    }
  },
  // My teams - redirect to content management
  {
    path: '/user/content/myteam',
    redirect: '/user/content/teams'
  },
  // Create new team
  {
    path: '/team/addteam',
    name: 'Add Team',
    component: AddTeam,
    meta: {
      requiresAuth: true,
      title: 'Create Team'
    }
  },
  // View team details
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
  // Edit team (from content management)
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

  // === AUTHOR ROUTES ===
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

  // === PUBLISHER ROUTES ===
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

  // === ARTIST ROUTES ===
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


  {
    path: '/catalog',
    name: 'Catalog',
    component: Catalog,
    meta: {
      title: 'Browse Catalog',
      requiresAuth: false
    }
  },

  // Alternative route with category/tag pre-filtering
  {
    path: '/catalog/:filter',
    name: 'CatalogFiltered',
    component: Catalog,
    props: route => ({
      initialFilter: route.params.filter,
      initialValues: route.query
    }),
    meta: {
      title: 'Browse Catalog',
      requiresAuth: false
    }
  },

  // === ADMIN ROUTES ===
  // Admin author management
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
  // Admin publisher management
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
  // Admin artist management
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
    path: '/Title/Edit/:id',
    name: 'Edit Title',
    component: () => import('../components/title-details/EditTitle.vue'),
    props: route => ({
      id: parseInt(route.params.id)
    }),
    meta: {
      requiresAuth: true,
      title: 'Edit Title'
    }
  },
  // Admin title management
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
    path: '/admin/title-changes',
    name: 'Admin Title Changes',
    component: AdminTitleChanges,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Title Changes Review'
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
  // Admin chapter management
  {
    path: '/admin/chapters',
    name: 'Admin Chapter Management',
    component: AdminChapterManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Chapter Management'
    }
  },
  // Admin comments management
  {
    path: '/admin/comments',
    name: 'Admin Comments Management',
    component: AdminCommentsManagement,
    meta: {
      requiresAuth: true,
      requiresAdmin: true,
      title: 'Admin - Comments Management'
    }
  },
  // Admin user management
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
  // Admin team management
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
  {
    path: '/thread/:commentId',
    name: 'CommentThread',
    component: CommentThreadView,
    props: route => ({
      commentId: parseInt(route.params.commentId)
    }),
    meta: {
      title: 'Comment Thread',
      requiresAuth: false   // publicly accessible (CommentsComponent handles auth gating)
    }
  },

  // === ERROR PAGES ===
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

  // === TITLE DETAILS ROUTE ===
  // This should be near the end but before the catch-all 404
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

  // === 404 ROUTE ===
  // This should be the last route
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: NotFoundPage,
    meta: {
      hideNavigation: true
    }
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

  // Check if route requires admin OR moderator role
  if (to.meta.requiresAdminOrModerator && !authStore.isAdmin && !authStore.isModerator) {
    // If user is authenticated but not admin/moderator, show 403 error
    if (authStore.isAuthenticated) {
      next({
        name: 'Error',
        params: { code: '403' },
        query: {
          message: 'Access denied. Admin or Moderator privileges required.',
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
