<template>
  <div class="notification-wrapper" ref="wrapperRef">
    <!-- Bell trigger button (used in desktop navbar + mobile sidebar) -->
    <button class="notification-trigger nav-link"
            :class="{ 'notification-trigger--mobile': mobile }"
            @click="toggleDropdown"
            :aria-label="`Notifications${unreadCount > 0 ? `, ${unreadCount} unread` : ''}`">
      <Bell class="nav-icon-lucide" :size="20" />
      <span v-if="mobile" class="nd-mobile-label">Notifications</span>
      <span v-if="unreadCount > 0" class="unread-badge" :class="{ 'unread-badge--mobile': mobile }">
        {{ unreadCount > 99 ? '99+' : unreadCount }}
      </span>
    </button>

    <!-- ═══════════════════════════════════════════════════════
         MOBILE: full-screen overlay teleported to <body>
         (matches the mobile search full-screen pattern)
    ═══════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="nd-fullscreen">
        <div v-if="isOpen && isMobileViewport" class="nd-fullscreen-overlay" role="dialog" aria-modal="true" aria-label="Notifications">

          <!-- Header -->
          <div class="nd-fs-header">
            <span class="nd-fs-title">Notifications</span>
            <div class="nd-fs-header-right">
              <button v-if="unreadCount > 0"
                      class="nd-mark-all"
                      @click="handleMarkAllAsRead"
                      :disabled="markingAll">
                {{ markingAll ? 'Marking...' : 'Mark all read' }}
              </button>
              <button class="nd-fs-close" @click="closeDropdown" aria-label="Close">
                <X :size="22" />
              </button>
            </div>
          </div>

          <!-- Notification list / states -->
          <div class="nd-fs-body">
            <div v-if="loading" class="nd-loading">
              <div class="nd-spinner"></div>
            </div>

            <div v-else-if="notifications.length === 0" class="nd-empty">
              <BellOff :size="44" class="nd-empty-icon" />
              <p>No notifications yet</p>
            </div>

            <div v-else>
              <div v-for="n in notifications"
                   :key="n.id"
                   class="nd-item"
                   :class="{ 'nd-item--unread': !n.isRead }"
                   @click="handleNotificationClick(n)">
                <div class="nd-item-icon" :class="typeIconClass(n.type)">
                  <component :is="typeIcon(n.type)" :size="16" />
                </div>
                <div class="nd-item-body">
                  <div class="nd-item-title">{{ n.title }}</div>
                  <div class="nd-item-message">{{ n.message }}</div>
                  <div class="nd-item-time">{{ timeAgo(n.createdAt) }}</div>
                </div>
                <button class="nd-dismiss" @click.stop="handleDismiss(n)" title="Dismiss">
                  <X :size="14" />
                </button>
              </div>
            </div>

            <div v-if="!loading && totalCount > notifications.length" class="nd-footer">
              <button class="nd-load-more" @click="loadMore" :disabled="loadingMore">
                {{ loadingMore ? 'Loading...' : `Load more (${totalCount - notifications.length} remaining)` }}
              </button>
            </div>
          </div>

        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════
         DESKTOP: dropdown anchored to the bell button
    ═══════════════════════════════════════════════════════ -->
    <Transition name="dropdown">
      <div v-if="isOpen && !isMobileViewport" class="notification-dropdown">

        <!-- Header -->
        <div class="nd-header">
          <span class="nd-title">Notifications</span>
          <button v-if="unreadCount > 0"
                  class="nd-mark-all"
                  @click="handleMarkAllAsRead"
                  :disabled="markingAll">
            {{ markingAll ? 'Marking...' : 'Mark all read' }}
          </button>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="nd-loading">
          <div class="nd-spinner"></div>
        </div>

        <!-- Empty -->
        <div v-else-if="notifications.length === 0" class="nd-empty">
          <BellOff :size="32" class="nd-empty-icon" />
          <p>No notifications yet</p>
        </div>

        <!-- List -->
        <div v-else class="nd-list">
          <div v-for="n in notifications"
               :key="n.id"
               class="nd-item"
               :class="{ 'nd-item--unread': !n.isRead }"
               @click="handleNotificationClick(n)">
            <div class="nd-item-icon" :class="typeIconClass(n.type)">
              <component :is="typeIcon(n.type)" :size="14" />
            </div>
            <div class="nd-item-body">
              <div class="nd-item-title">{{ n.title }}</div>
              <div class="nd-item-message">{{ n.message }}</div>
              <div class="nd-item-time">{{ timeAgo(n.createdAt) }}</div>
            </div>
            <button class="nd-dismiss" @click.stop="handleDismiss(n)" title="Dismiss">
              <X :size="12" />
            </button>
          </div>
        </div>

        <!-- Load more -->
        <div v-if="!loading && totalCount > notifications.length" class="nd-footer">
          <button class="nd-load-more" @click="loadMore" :disabled="loadingMore">
            {{ loadingMore ? 'Loading...' : `Load more (${totalCount - notifications.length} remaining)` }}
          </button>
        </div>

      </div>
    </Transition>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted, onUnmounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { Bell, BellOff, X, Megaphone, Wrench, Sparkles, MessageSquare, BookOpen } from 'lucide-vue-next';
  import { notificationsService } from '../../services/notificationsService';

  // ── Props / emits ────────────────────────────────────────────
  const props = defineProps({
    mobile: { type: Boolean, default: false }
  });
  const emit = defineEmits(['close-sidebar']);

  // ── State ────────────────────────────────────────────────────
  const router = useRouter();
  const wrapperRef = ref(null);
  const isOpen = ref(false);
  const loading = ref(false);
  const loadingMore = ref(false);
  const markingAll = ref(false);
  const unreadCount = ref(0);
  const notifications = ref([]);
  const totalCount = ref(0);
  const currentPage = ref(1);
  const PAGE_SIZE = 15;

  // ── Viewport detection ───────────────────────────────────────
  const isMobileViewport = ref(window.innerWidth <= 768);
  const onResize = () => { isMobileViewport.value = window.innerWidth <= 768; };

  // ── Icon helpers ─────────────────────────────────────────────
  const typeIcon = (type) => {
    const map = { 1: BookOpen, 10: Megaphone, 11: Wrench, 12: Sparkles, 20: MessageSquare, 21: MessageSquare };
    return map[type] ?? Bell;
  };
  const typeIconClass = (type) => {
    const map = { 1: 'nd-icon--blue', 10: 'nd-icon--purple', 11: 'nd-icon--amber', 12: 'nd-icon--green', 20: 'nd-icon--teal', 21: 'nd-icon--teal' };
    return map[type] ?? 'nd-icon--gray';
  };

  // ── Time formatting ──────────────────────────────────────────
  const timeAgo = (d) => {
    if (!d) return '';
    const diff = Date.now() - new Date(d).getTime();
    const m = Math.floor(diff / 60000);
    if (m < 1) return 'just now';
    if (m < 60) return `${m}m ago`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h}h ago`;
    const days = Math.floor(h / 24);
    if (days < 7) return `${days}d ago`;
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  };

  // ── Data fetching ────────────────────────────────────────────
  const fetchUnreadCount = async () => {
    try {
      const c = await notificationsService.getUnreadCount();
      unreadCount.value = typeof c === 'number' ? c : (c?.count ?? 0);
    } catch { /* silent */ }
  };

  const fetchNotifications = async (reset = true) => {
    if (reset) { loading.value = true; currentPage.value = 1; notifications.value = []; }
    else { loadingMore.value = true; }
    try {
      const res = await notificationsService.getNotifications({ page: currentPage.value, pageSize: PAGE_SIZE });
      const items = res?.notifications ?? res ?? [];
      if (reset) notifications.value = items;
      else notifications.value.push(...items);
      totalCount.value = res?.totalCount ?? items.length;
      unreadCount.value = res?.unreadCount ?? unreadCount.value;
    } catch { /* silent */ } finally {
      loading.value = false;
      loadingMore.value = false;
    }
  };

  const loadMore = async () => { currentPage.value++; await fetchNotifications(false); };

  // ── Toggle ───────────────────────────────────────────────────
  const toggleDropdown = () => {
    isOpen.value = !isOpen.value;
    if (isOpen.value) {
      fetchNotifications(true);
      if (isMobileViewport.value) document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  };

  const closeDropdown = () => {
    isOpen.value = false;
    document.body.style.overflow = '';
  };

  // ── Actions ──────────────────────────────────────────────────
  const handleNotificationClick = async (n) => {
    if (!n.isRead) {
      n.isRead = true;
      if (unreadCount.value > 0) unreadCount.value--;
      try { await notificationsService.markAsRead(n.id); } catch { /* silent */ }
    }
    if (n.linkUrl) {
      closeDropdown();
      emit('close-sidebar');
      n.linkUrl.startsWith('/')
        ? router.push(n.linkUrl)
        : window.open(n.linkUrl, '_blank', 'noopener');
    }
  };

  const handleDismiss = async (n) => {
    notifications.value = notifications.value.filter(x => x.id !== n.id);
    totalCount.value = Math.max(0, totalCount.value - 1);
    if (!n.isRead && unreadCount.value > 0) unreadCount.value--;
    try { await notificationsService.dismiss(n.id); } catch { /* silent */ }
  };

  const handleMarkAllAsRead = async () => {
    markingAll.value = true;
    try {
      await notificationsService.markAllAsRead();
      notifications.value = notifications.value.map(n => ({ ...n, isRead: true }));
      unreadCount.value = 0;
    } catch { /* silent */ } finally { markingAll.value = false; }
  };

  // ── Click-outside (desktop only) ────────────────────────────
  const handleClickOutside = (e) => {
    if (!isMobileViewport.value && wrapperRef.value && !wrapperRef.value.contains(e.target)) {
      closeDropdown();
    }
  };

  // ── Lifecycle ────────────────────────────────────────────────
  let pollInterval = null;
  onMounted(() => {
    fetchUnreadCount();
    pollInterval = setInterval(fetchUnreadCount, 60000);
    document.addEventListener('click', handleClickOutside, true);
    window.addEventListener('resize', onResize);
  });
  onUnmounted(() => {
    if (pollInterval) clearInterval(pollInterval);
    document.removeEventListener('click', handleClickOutside, true);
    window.removeEventListener('resize', onResize);
    document.body.style.overflow = '';
  });
</script>

<style scoped>
    /* ── Wrapper ── */
    .notification-wrapper {
      position: relative;
    }

    /* ── Trigger button ── */
    .notification-trigger {
      position: relative;
      display: flex;
      align-items: center;
      background: none;
      border: none;
      cursor: pointer;
      padding: 8px 12px;
      border-radius: 4px;
      color: white;
      transition: background-color 0.2s;
    }

      .notification-trigger:hover {
        background-color: rgba(255,255,255,0.1);
      }

    .notification-trigger--mobile {
      width: 100%;
      justify-content: flex-start;
      padding: 15px 12px;
      border-radius: 6px;
    }

    /* ── Unread badge ── */
    .unread-badge {
      position: absolute;
      top: 4px;
      right: 6px;
      background: #ef4444;
      color: white;
      font-size: 10px;
      font-weight: 600;
      min-width: 16px;
      height: 16px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0 3px;
      pointer-events: none;
      border: 1.5px solid rgba(0,0,0,0.6);
    }

    .unread-badge--mobile {
      position: static;
      margin-left: auto;
      border: none;
    }

    .nd-mobile-label {
      margin-left: 12px;
      font-size: 15px;
      color: white;
    }

    /* ══════════════════════════════════════════════
     MOBILE FULL-SCREEN OVERLAY
  ══════════════════════════════════════════════ */
    .nd-fullscreen-overlay {
      position: fixed;
      inset: 0;
      z-index: 9999;
      background: var(--color-input-bg);
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .nd-fs-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px 20px;
      border-bottom: 1px solid rgba(255,255,255,0.08);
      min-height: 60px;
      flex-shrink: 0;
    }

    .nd-fs-title {
      font-size: 18px;
      font-weight: 500;
      color: rgba(255,255,255,0.95);
    }

    .nd-fs-header-right {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .nd-fs-close {
      background: none;
      border: none;
      cursor: pointer;
      color: rgba(255,255,255,0.7);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 4px;
      border-radius: 50%;
      transition: background-color 0.15s, color 0.15s;
    }

      .nd-fs-close:hover {
        background-color: rgba(255,255,255,0.1);
        color: white;
      }

    .nd-fs-body {
      flex: 1;
      overflow-y: auto;
      -webkit-overflow-scrolling: touch;
    }

    /* Fullscreen transition */
    .nd-fullscreen-enter-active {
      transition: opacity 0.2s ease, transform 0.2s ease;
    }

    .nd-fullscreen-leave-active {
      transition: opacity 0.18s ease, transform 0.18s ease;
    }

    .nd-fullscreen-enter-from {
      opacity: 0;
      transform: translateY(20px);
    }

    .nd-fullscreen-leave-to {
      opacity: 0;
      transform: translateY(20px);
    }

    /* ══════════════════════════════════════════════
     DESKTOP DROPDOWN
  ══════════════════════════════════════════════ */
    .notification-dropdown {
      position: absolute;
      top: calc(100% + 8px);
      right: 0;
      width: 360px;
      max-height: 480px;
      background: var(--color-background-soft);
      border: 1px solid rgba(255,255,255,0.08);
      border-radius: 10px;
      box-shadow: 0 10px 38px -10px rgba(0,0,0,0.6), 0 10px 20px -15px rgba(0,0,0,0.5);
      z-index: 9999;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .nd-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 14px 16px 10px;
      border-bottom: 1px solid rgba(255,255,255,0.06);
      flex-shrink: 0;
    }

    .nd-title {
      font-size: 14px;
      font-weight: 500;
      color: rgba(255,255,255,0.95);
    }

    /* ── Shared list styles ── */
    .nd-mark-all {
      background: none;
      border: none;
      cursor: pointer;
      font-size: 12px;
      color: #6b8cff;
      padding: 2px 0;
      transition: opacity 0.15s;
    }

      .nd-mark-all:hover {
        opacity: 0.8;
      }

      .nd-mark-all:disabled {
        opacity: 0.5;
        cursor: default;
      }

    .nd-loading {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 40px;
    }

    .nd-spinner {
      width: 26px;
      height: 26px;
      border: 2px solid rgba(255,255,255,0.1);
      border-top-color: rgba(255,255,255,0.6);
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }

    @keyframes spin {
      to {
        transform: rotate(360deg);
      }
    }

    .nd-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 56px 20px;
      gap: 12px;
      color: rgba(255,255,255,0.3);
      font-size: 14px;
    }

    .nd-empty-icon {
      opacity: 0.4;
    }

    .nd-list {
      overflow-y: auto;
      flex: 1;
    }

    .nd-item {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 14px 16px;
      cursor: pointer;
      transition: background-color 0.12s;
      border-bottom: 1px solid rgba(255,255,255,0.04);
      position: relative;
    }

      .nd-item:hover {
        background-color: rgba(255,255,255,0.04);
      }

    .nd-item--unread {
      background-color: rgba(107,140,255,0.06);
    }

      .nd-item--unread::before {
        content: '';
        position: absolute;
        left: 5px;
        top: 50%;
        transform: translateY(-50%);
        width: 5px;
        height: 5px;
        border-radius: 50%;
        background: #6b8cff;
      }

    .nd-item-icon {
      flex-shrink: 0;
      width: 30px;
      height: 30px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-top: 1px;
    }

    .nd-icon--blue {
      background: rgba(55,138,221,0.15);
      color: #66aaee;
    }

    .nd-icon--purple {
      background: rgba(127,119,221,0.15);
      color: #a89de8;
    }

    .nd-icon--amber {
      background: rgba(239,159,39,0.15);
      color: #f0b848;
    }

    .nd-icon--green {
      background: rgba(99,153,34,0.15);
      color: #88cc44;
    }

    .nd-icon--teal {
      background: rgba(29,158,117,0.15);
      color: #3dbf92;
    }

    .nd-icon--gray {
      background: rgba(136,135,128,0.15);
      color: #aaa;
    }

    .nd-item-body {
      flex: 1;
      min-width: 0;
    }

    .nd-item-title {
      font-size: 13px;
      font-weight: 500;
      color: rgba(255,255,255,0.9);
      margin-bottom: 3px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .nd-item-message {
      font-size: 12px;
      color: rgba(255,255,255,0.5);
      line-height: 1.4;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .nd-item-time {
      font-size: 11px;
      color: rgba(255,255,255,0.3);
      margin-top: 4px;
    }

    .nd-dismiss {
      flex-shrink: 0;
      background: none;
      border: none;
      cursor: pointer;
      color: rgba(255,255,255,0.25);
      padding: 2px;
      border-radius: 3px;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: color 0.12s, background-color 0.12s;
      margin-top: 2px;
    }

      .nd-dismiss:hover {
        color: rgba(255,255,255,0.7);
        background-color: rgba(255,255,255,0.08);
      }

    .nd-footer {
      border-top: 1px solid rgba(255,255,255,0.06);
      padding: 10px 16px;
      flex-shrink: 0;
    }

    .nd-load-more {
      background: none;
      border: none;
      cursor: pointer;
      font-size: 12px;
      color: #6b8cff;
      width: 100%;
      text-align: center;
      padding: 4px 0;
      transition: opacity 0.15s;
    }

      .nd-load-more:hover {
        opacity: 0.8;
      }

      .nd-load-more:disabled {
        opacity: 0.5;
        cursor: default;
      }

    /* Desktop dropdown transition */
    .dropdown-enter-active, .dropdown-leave-active {
      transition: opacity 0.15s ease, transform 0.15s ease;
    }

    .dropdown-enter-from, .dropdown-leave-to {
      opacity: 0;
      transform: translateY(-6px);
    }
</style>
