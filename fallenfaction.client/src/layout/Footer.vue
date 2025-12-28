<template>
  <footer class="bg-[var(--color-background)] text-[var(--color-text)] mt-auto border-t border-[var(--color-border)]">
    <!-- Main Footer Links Section -->
    <div class="bg-[var(--color-background-soft)] py-12 md:py-16">
      <div class="max-w-7xl mx-auto px-5">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">

          <!-- General Links Section -->
          <div class="space-y-4">
            <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4 pb-2 border-b-2 border-[var(--vt-c-indigo)] inline-block">
              General
            </h3>
            <ul class="space-y-3">
              <li>
                <router-link to="/home/privacy"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Privacy Policy
                </router-link>
              </li>
              <li>
                <router-link to="/home/fqa"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  FAQ
                </router-link>
              </li>
              <li>
                <router-link to="/home/terms"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Terms of Usage
                </router-link>
              </li>
              <li>
                <router-link to="/home/dmca"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  DMCA
                </router-link>
              </li>
              <li>
                <router-link to="/home/book-issuing"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Book Issuing
                </router-link>
              </li>
            </ul>
          </div>

          <!-- Browse & Discover Section -->
          <div class="space-y-4">
            <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4 pb-2 border-b-2 border-[var(--vt-c-indigo)] inline-block">
              Browse
            </h3>
            <ul class="space-y-3">
              <li>
                <router-link to="/authors"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Authors
                </router-link>
              </li>
              <li>
                <router-link to="/artists"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Artists
                </router-link>
              </li>
              <li>
                <router-link to="/publishers"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Publishers
                </router-link>
              </li>
              <li>
                <router-link to="/teams"
                             class="text-[var(--color-text)] hover:text-[var(--vt-c-indigo)] hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Teams
                </router-link>
              </li>
            </ul>
          </div>

          <!-- Content Creation Section (Authenticated Users Only) -->
          <div v-if="authStore.isAuthenticated" class="space-y-4">
            <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4 pb-2 border-b-2 border-green-500 inline-block">
              Create Content
            </h3>
            <ul class="space-y-3">
              <li>
                <router-link to="/manga/addtitle"
                             class="text-[var(--color-text)] hover:text-green-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Add Title
                </router-link>
              </li>
              <li>
                <router-link to="/author/createa"
                             class="text-[var(--color-text)] hover:text-green-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Add Author
                </router-link>
              </li>
              <li>
                <router-link to="/publisher/create"
                             class="text-[var(--color-text)] hover:text-green-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Add Publisher
                </router-link>
              </li>
              <li>
                <router-link to="/artist/create"
                             class="text-[var(--color-text)] hover:text-green-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Add Artist
                </router-link>
              </li>
              <li>
                <router-link to="/team/addteam"
                             class="text-[var(--color-text)] hover:text-green-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Create Team
                </router-link>
              </li>
            </ul>
          </div>

          <!-- Admin Management Section (Admin Users Only) -->
          <div v-if="authStore.isAdmin" class="space-y-4">
            <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4 pb-2 border-b-2 border-red-500 inline-block">
              Admin Panel
            </h3>
            <ul class="space-y-3">
              <li>
                <router-link to="/admin/titles/add"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Review Titles
                </router-link>
              </li>
              <li>
                <router-link to="/admin/chapters"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Review Chapters
                </router-link>
              </li>
              <li>
                <router-link to="/admin/title-changes"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Review Title Changes
                </router-link>
              </li>
              <li>
                <router-link to="/admin/comments"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Manage Comments
                </router-link>
              </li>
              <li>
                <router-link to="/admin/titles"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Manage Titles
                </router-link>
              </li>
              <li>
                <router-link to="/admin/users"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Manage Users
                </router-link>
              </li>
              <li>
                <router-link to="/admin/teams"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Manage Teams
                </router-link>
              </li>
              <li>
                <router-link to="/admin/authors"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Manage Authors
                </router-link>
              </li>
              <li>
                <router-link to="/admin/artists"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Manage Artists
                </router-link>
              </li>
              <li>
                <router-link to="/admin/publishers"
                             class="text-[var(--color-text)] hover:text-red-500 hover:pl-2 transition-all duration-200 text-sm block py-1">
                  Manage Publishers
                </router-link>
              </li>
            </ul>
          </div>

        </div>
      </div>
    </div>

    <!-- Copyright Section -->
    <div class="bg-[var(--color-background)] py-6 border-t border-[var(--color-border)]">
      <div class="max-w-7xl mx-auto px-5">
        <div class="flex flex-col md:flex-row justify-between items-center gap-4">
          <p class="text-[var(--color-text)] opacity-75 text-sm m-0">
            &copy; {{ currentYear }} All rights reserved.
            <a href="https://fallenfaction.com"
               target="_blank"
               class="text-[var(--vt-c-indigo)] hover:opacity-80 font-medium transition-all duration-200 no-underline">
              Fallen Faction
            </a>
          </p>
          <div class="flex items-center gap-2 text-[var(--color-text)] opacity-60 text-sm">
            <span class="bg-[var(--color-background-mute)] px-2 py-1 rounded-full text-xs">v2.0</span>
            <span class="opacity-50">•</span>
            <router-link to="/home/privacy"
                         class="text-[var(--color-text)] opacity-75 hover:text-[var(--vt-c-indigo)] transition-colors duration-200 no-underline">
              Privacy
            </router-link>
            <span class="opacity-50">•</span>
            <router-link to="/home/terms"
                         class="text-[var(--color-text)] opacity-75 hover:text-[var(--vt-c-indigo)] transition-colors duration-200 no-underline">
              Terms
            </router-link>
          </div>
        </div>
      </div>
    </div>
  </footer>
</template>

<script setup>
  import { computed } from 'vue';
  import { useAuthStore } from '../stores/authStore';

  const authStore = useAuthStore();

  // Get current year for copyright
  const currentYear = computed(() => new Date().getFullYear());
</script>

<style scoped>
  /* Custom styles for router-link-active state */
  .router-link-active {
    color: var(--vt-c-indigo);
  }

  /* Focus states for accessibility */
  a:focus {
    outline: 2px solid var(--vt-c-indigo);
    outline-offset: 2px;
    border-radius: 4px;
  }

  /* Reduced motion support */
  @media (prefers-reduced-motion: reduce) {
    * {
      transition: none !important;
    }
  }
</style>
