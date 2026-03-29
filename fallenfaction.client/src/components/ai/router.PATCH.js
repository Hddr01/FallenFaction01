// ════════════════════════════════════════════════════════════════════════
// router/index.js  —  ADD THESE ROUTES
// ════════════════════════════════════════════════════════════════════════
// Add these imports near the top of your router file:
//
//   import MyRequestsPage from '../components/ai/MyRequestsPage.vue';
//   import VotingPage     from '../components/ai/VotingPage.vue';
//   import WalletPage     from '../components/ai/WalletPage.vue';
//   import AdminRequestsManagement from '../components/admin/AdminRequestsManagement.vue';
//
// Then add these routes to the routes array:

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
