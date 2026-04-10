// router/index.js
import { createRouter, createWebHistory } from 'vue-router';
import HomePage from '../HomePage.vue';
import NotFoundPage from '../NotFoundPage.vue';
import ErrorPage from '../ErrorPage.vue';
import Login from '../identity/auth/Login.vue';
import Register from '../identity/auth/Register.vue';
import ConfirmEmail from '../identity/auth/ConfirmEmail.vue';
import TermsAcceptPage from '../identity/auth/TermsAcceptPage.vue';
import Profile from '../identity/profile/Profile.vue';
import { useAuthStore } from '../stores/authStore';
import AddTitle from '../components/manga/AddTitle.vue';

// Re-export slug helpers from the dedicated utility so any code that
// previously imported them from the router still works — but components
// should prefer importing directly from '@/utils/titleSlug.js' to avoid
// circular dependency issues.
export { buildTitleSlug, parseTitleSlug } from '../utils/titleSlug.js'
import { buildTitleSlug, parseTitleSlug } from '../utils/titleSlug.js'
import AdminTitleManagement from '../components/admin/AdminTitleManagement.vue';
import TitleManagement from '../components/admin/TitleManagement.vue';
import AdminChapterManagement from '../components/admin/AdminChapterManagement.vue';
import TitleDetailsPage from '../components/title-details/TitleDetailsPage.vue';
import TeamList from '../components/team/TeamList.vue';
import AddTeam from '../components/team/AddTeam.vue';
import TeamDetails from '../components/team/TeamDetails.vue';
import AddAuthor from '../components/author/AddAuthor.vue';
import AuthorList from '../components/author/AuthorList.vue';
import AdminAuthorManagement from '../components/admin/AdminAuthorManagement.vue';
import AddPublisher from '../components/publisher/AddPublisher.vue';
import PublisherList from '../components/publisher/PublisherList.vue';
import AdminPublisherManagement from '../components/admin/AdminPublisherManagement.vue';
import AdminReportsManagement from '../components/admin/AdminReportsManagement.vue';
import AdminNotificationsManagement from '../components/admin/AdminNotificationsManagement.vue';
import StaticPage from '../components/pages/StaticPage.vue';
import AdminUserManagement from '../components/admin/AdminUserManagement.vue';
import AdminTeamManagement from '../components/admin/AdminTeamManagement.vue';
import AdminCommentsManagement from '../components/admin/AdminCommentsManagement.vue';
import AdminTitleChanges from '../components/admin/AdminTitleChanges.vue';
import ContentManagement from '../components/content/ContentManagement.vue';
import TitleChangeHistory from '../components/title-details/TitleChangeHistory.vue';
import AddChapter from '../components/title-details/AddChapter.vue';
import BulkAddChapters from '../components/title-details/BulkAddChapters.vue';
import NovelChapterEditor from '../components/title-details/NovelChapterEditor.vue';
import ChapterReader from '../components/title-details/ChapterReader.vue';
import Catalog from '../components/catalog/Catalog.vue';
import CommentThreadView from '../components/title-details/CommentThreadView.vue';

import MyRequestsPage from '../components/ai/MyRequestsPage.vue';
import VotingPage from '../components/ai/VotingPage.vue';
import WalletPage from '../components/ai/WalletPage.vue';
import AdminRequestsManagement from '../components/admin/AdminRequestsManagement.vue';
import ArtistList from '../components/artist/ArtistList.vue';
import AddArtist from '../components/artist/AddArtist.vue';
import AdminArtistManagement from '../components/admin/AdminArtistManagement.vue';
import AdminTitleJoinRequests from '../components/admin/AdminTitleJoinRequests.vue';


// ── System route prefixes — never matched as title slugs ────────────────────
const SYSTEM_PREFIXES = [
  'api', 'admin', 'account', 'user', 'team', 'novel', 'author',
  'publisher', 'error', 'catalog', 'profile', 'thread', 'title', 'tickets', 'voting',
  'teams', 'authors', 'publishers', 'artists', 'artist', 'home', 'dmca', 'faq', 'terms',
  'about', 'contact', 'privacy', 'pages',
]

const routes = [
  {
    path: '/',
    name: 'Home',
    component: HomePage
  },

  // ── Chapter reader ─────────────────────────────────────────────────────────
  // :titleSlug is "name-{id}" e.g. "naruto-42"
  {
    path: '/:titleSlug/chapter/:chapterName/v:volumeNumber/t:teamId',
    name: 'ChapterReader',
    component: ChapterReader,
    props: route => {
      const vol = parseInt(route.params.volumeNumber)
      const team = parseInt(route.params.teamId)
      return {
        titleSlug: decodeURIComponent(route.params.titleSlug),
        chapterName: decodeURIComponent(route.params.chapterName),
        // Fall back to safe integers when the URL segment is not a valid number
        // (e.g. "null" / "undefined" from a broken URL builder).
        volumeNumber: Number.isFinite(vol) ? vol : 1,
        teamId: Number.isFinite(team) ? team : 0,
      }
    },
    meta: { title: 'Reading Chapter', hideNavigation: true, requiresAuth: false }
  },

  // ── Auth ───────────────────────────────────────────────────────────────────
  { path: '/account/login', name: 'Login', component: Login, meta: { requiresGuest: true, title: 'Login' } },
  { path: '/account/register', name: 'Register', component: Register, meta: { requiresGuest: true, title: 'Register' } },
  { path: '/account/confirm-email', name: 'ConfirmEmail', component: ConfirmEmail, meta: { title: 'Confirm Email' } },
  { path: '/terms/accept', name: 'TermsAccept', component: TermsAcceptPage, meta: { title: 'Accept Terms' } },

  // ── Profile ───────────────────────────────────────────────────────────────
  { path: '/profile', name: 'Profile', component: Profile, meta: { requiresAuth: true, title: 'Profile' } },

  // ── Change history ────────────────────────────────────────────────────────
  {
    path: '/title/:titleId/change-history',
    name: 'TitleChangeHistory',
    component: TitleChangeHistory,
    props: route => ({ titleId: parseInt(route.params.titleId) }),
    meta: { requiresAuth: true, title: 'Change History' }
  },
  // ── AI Translation: User routes ──────────────────────────────────────
  {
    path: '/profile/requests',
    name: 'MyRequests',
    component: () => import('../components/ai/MyRequestsPage.vue'),
    meta: { requiresAuth: true, title: 'My Requests' }
  },
  {
    path: '/profile/wallet',
    name: 'Wallet',
    component: () => import('../components/ai/WalletPage.vue'),
    meta: { requiresAuth: true, title: 'Ticket Wallet' }
  },

  // ── AI Translation: Public voting page ───────────────────────────────
  {
    path: '/novel/voting',
    name: 'NovelVoting',
    component: () => import('../components/ai/VotingPage.vue'),
    meta: { title: 'Novel Voting' }
  },

  // ── AI Translation: Admin queue ───────────────────────────────────────
  {
    path: '/admin/translation-requests',
    name: 'AdminTranslationRequests',
    component: () => import('../components/admin/AdminRequestsManagement.vue'),
    meta: { requiresAuth: true, requiresAdmin: true, title: 'Translation Requests' }
  },
  {
    path: '/admin/grant-tickets',
    name: 'AdminGrantTickets',
    component: () => import('../components/admin/AdminGrantTickets.vue'),
    meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Grant Tickets' }
  },

  // ── Content management ────────────────────────────────────────────────────
  { path: '/user/content', name: 'Content Management', component: ContentManagement, meta: { requiresAuth: true, title: 'Content Management' } },
  { path: '/user/content/index', redirect: '/user/content' },
  { path: '/user/content/uploads', redirect: '/user/content' },
  { path: '/user/content/titles', name: 'My Titles', component: ContentManagement, meta: { requiresAuth: true, title: 'My Titles', defaultTab: 'titles' } },
  { path: '/user/content/chapters', name: 'My Chapters', component: ContentManagement, meta: { requiresAuth: true, title: 'My Chapters', defaultTab: 'chapters' } },
  { path: '/user/content/teams', name: 'My Teams Content', component: ContentManagement, meta: { requiresAuth: true, title: 'My Teams', defaultTab: 'teams' } },
  { path: '/user/content/moderation', name: 'Content Moderation', component: ContentManagement, meta: { requiresAuth: true, requiresAdminOrModerator: true, title: 'Content Moderation', defaultTab: 'moderation' } },
  { path: '/user/content/rejected', name: 'Rejected Content', component: ContentManagement, meta: { requiresAuth: true, title: 'Rejected Content', defaultTab: 'titles' } },
  { path: '/user/content/myteam', redirect: '/user/content/teams' },
  { path: '/user/chapters', redirect: '/user/content/chapters' },
  { path: '/user/titles', redirect: '/user/content/titles' },
  { path: '/user/teams', redirect: '/user/content/teams' },

  // ── Public user profiles — must come after all specific /user/* routes ────
  { path: '/user/:id', name: 'PublicUserProfile', component: () => import('../components/user/PublicUserProfile.vue'), props: true, meta: { title: 'User Profile' } },

  // ── Titles ────────────────────────────────────────────────────────────────
  { path: '/novel/addtitle', name: 'Add Title', component: AddTitle, meta: { requiresAuth: true, title: 'Add Title' } },
  { path: '/:titleSlug/AddChapter', name: 'Add Chapter', component: AddChapter, meta: { requiresAuth: true, title: 'Add Chapter' } },
  { path: '/:titleSlug/BulkAddChapters', name: 'Bulk Add Chapters', component: BulkAddChapters, meta: { requiresAuth: true, title: 'Bulk Upload Chapters' } },
  { path: '/:titleSlug/chapters/edit', name: 'Novel Chapter Editor', component: NovelChapterEditor, props: route => ({ titleSlug: decodeURIComponent(route.params.titleSlug) }), meta: { requiresAuth: true, title: 'Chapter Editor' } },
  { path: '/Title/Edit/:id', name: 'Edit Title', component: () => import('../components/title-details/EditTitle.vue'), props: route => ({ id: parseInt(route.params.id) }), meta: { requiresAuth: true, title: 'Edit Title' } },

  // ── Teams ─────────────────────────────────────────────────────────────────
  { path: '/teams', name: 'Teams', component: TeamList, meta: { requiresAuth: true, title: 'Teams' } },
  { path: '/team/addteam', name: 'Add Team', component: AddTeam, meta: { requiresAuth: true, title: 'Create Team' } },
  { path: '/team/:id', name: 'Team Details', component: TeamDetails, props: route => ({ teamId: parseInt(route.params.id) }), meta: { requiresAuth: true, title: 'Team Details' } },
  { path: '/user/content/editteam/:id', name: 'Edit Team', component: TeamDetails, props: route => ({ teamId: parseInt(route.params.id) }), meta: { requiresAuth: true, title: 'Edit Team' } },

  // ── Authors & Publishers ──────────────────────────────────────────────────
  { path: '/authors', name: 'Authors', component: AuthorList, meta: { title: 'Authors' } },
  { path: '/author/CreateA', name: 'Create Author', component: AddAuthor, meta: { requiresAuth: true, title: 'Create Author' } },
  { path: '/author/createa', redirect: '/author/CreateA' },
  { path: '/author/create', name: 'Add Author Admin', component: AddAuthor, meta: { requiresAuth: true, requiresAdmin: true, title: 'Add Author' } },
  { path: '/publishers', name: 'Publishers', component: PublisherList, meta: { title: 'Publishers' } },
  { path: '/publisher/create', name: 'Create Publisher', component: AddPublisher, meta: { requiresAuth: true, title: 'Create Publisher' } },

  // ── Artists ───────────────────────────────────────────────────────────────
  { path: '/artists', name: 'Artists', component: ArtistList, meta: { title: 'Artists' } },
  { path: '/artist/create', name: 'Create Artist', component: AddArtist, meta: { requiresAuth: true, title: 'Create Artist' } },

  // ── Catalog ───────────────────────────────────────────────────────────────
  { path: '/catalog', name: 'Catalog', component: Catalog, meta: { title: 'Browse Catalog', requiresAuth: false } },
  { path: '/catalog/:filter', name: 'CatalogFiltered', component: Catalog, props: route => ({ initialFilter: route.params.filter, initialValues: route.query }), meta: { title: 'Browse Catalog', requiresAuth: false } },

  // ── Admin ─────────────────────────────────────────────────────────────────
  { path: '/admin/authors', name: 'Admin Author Management', component: AdminAuthorManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Author Management' } },
  { path: '/admin/publishers', name: 'Admin Publisher Management', component: AdminPublisherManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Publisher Management' } },
  { path: '/admin/artists', name: 'Admin Artist Management', component: AdminArtistManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Artist Management' } },
  { path: '/admin/title-join-requests', name: 'Admin Title Join Requests', component: AdminTitleJoinRequests, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Title Join Requests' } },
  { path: '/admin/titles/add', name: 'Admin New Titles Management', component: AdminTitleManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - New Titles Management' } },
  { path: '/admin/title-changes', name: 'Admin Title Changes', component: AdminTitleChanges, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Title Changes Review' } },
  { path: '/admin/titles', name: 'Title Management', component: TitleManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Title Management' } },
  { path: '/admin/chapters', name: 'Admin Chapter Management', component: AdminChapterManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Chapter Management' } },
  { path: '/admin/comments', name: 'Admin Comments Management', component: AdminCommentsManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Comments Management' } },
  { path: '/admin/users', name: 'Admin User Management', component: AdminUserManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - User Management' } },
  { path: '/admin/teams', name: 'Admin Team Management', component: AdminTeamManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Team Management' } },
  { path: '/admin/reports', name: 'Admin Reports Management', component: AdminReportsManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Reports Management' } },
  { path: '/admin/notifications', name: 'Admin Notifications Management', component: AdminNotificationsManagement, meta: { requiresAuth: true, requiresAdmin: true, title: 'Admin - Notifications' } },

  // ── Static / Legal Pages ──────────────────────────────────────────────────
  { path: '/dmca', name: 'DMCA', component: StaticPage, props: { page: 'dmca' }, meta: { title: 'DMCA Policy' } },
  { path: '/faq', name: 'FAQ', component: StaticPage, props: { page: 'faq' }, meta: { title: 'FAQ' } },
  { path: '/terms', name: 'Terms', component: StaticPage, props: { page: 'terms' }, meta: { title: 'Terms of Service' } },
  { path: '/about', name: 'About', component: StaticPage, props: { page: 'about' }, meta: { title: 'About' } },
  { path: '/contact', name: 'Contact', component: StaticPage, props: { page: 'contact' }, meta: { title: 'Contact Us' } },
  { path: '/privacy', name: 'Privacy', component: StaticPage, props: { page: 'privacy' }, meta: { title: 'Privacy Policy' } },

  // ── Misc ──────────────────────────────────────────────────────────────────
  { path: '/thread/:commentId', name: 'CommentThread', component: CommentThreadView, props: route => ({ commentId: parseInt(route.params.commentId) }), meta: { title: 'Comment Thread', requiresAuth: false } },
  { path: '/error/:code', name: 'Error', component: ErrorPage, props: route => ({ statusCode: parseInt(route.params.code), message: route.query.message || '', path: route.query.path || '', showRetry: route.query.retry === 'true' }) },

  // ── Title details — /:titleSlug  (e.g. /naruto-42) ────────────────────────
  // Must come just before the 404 catch-all.
  {
    path: '/:titleSlug',
    name: 'TitleDetails',
    component: TitleDetailsPage,
    props: route => ({ titleSlug: decodeURIComponent(route.params.titleSlug) }),
    meta: { title: 'Title Details' },
    beforeEnter: (to, from, next) => {
      const slug = decodeURIComponent(to.params.titleSlug)
      const first = slug.split('/')[0].toLowerCase()
      // Only block known system prefixes — let everything else through.
      // TitleDetailsPage handles the "not found" case gracefully.
      if (SYSTEM_PREFIXES.includes(first)) { next('/404'); return }
      next()
    }
  },

  // ── 404 ───────────────────────────────────────────────────────────────────
  { path: '/:pathMatch(.*)*', name: 'NotFound', component: NotFoundPage, meta: { hideNavigation: true } }
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore()
  document.title = to.meta.title ? `${to.meta.title} - FallenFaction` : 'FallenFaction'

  if (!authStore.isInitialized) await authStore.initializeAuth()

  if (to.name === 'TermsAccept' && authStore.isAuthenticated) {
    next({ name: 'Home' })
    return
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'Login', query: { redirect: to.fullPath } }); return
  }
  if (to.meta.requiresAdmin && !authStore.isAdmin) {
    next(authStore.isAuthenticated
      ? { name: 'Error', params: { code: '403' }, query: { message: 'Admin privileges required.', path: to.fullPath } }
      : { name: 'Login', query: { redirect: to.fullPath } }
    ); return
  }
  if (to.meta.requiresAdminOrModerator && !authStore.isAdmin && !authStore.isModerator) {
    next(authStore.isAuthenticated
      ? { name: 'Error', params: { code: '403' }, query: { message: 'Admin or Moderator privileges required.', path: to.fullPath } }
      : { name: 'Login', query: { redirect: to.fullPath } }
    ); return
  }
  if (to.meta.requiresGuest && authStore.isAuthenticated) { next({ name: 'Home' }); return }
  next()
})

router.onError(error => {
  console.error('Router error:', error)
  router.push('/error/500?message=Navigation error occurred')
})

export default router
