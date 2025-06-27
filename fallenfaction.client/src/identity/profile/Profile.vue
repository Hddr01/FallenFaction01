<!-- Profile.vue - UPDATED VERSION using CSS variables -->
<template>
  <!-- FIXED: Single root element for proper transition animation -->
  <div class="profile-page-wrapper">
    <div class="min-h-screen bg-[var(--color-background)] py-12 px-4 sm:px-6 lg:px-8">
      <div class="max-w-3xl mx-auto">
        <!-- Header -->
        <div class="bg-[var(--color-background-soft)] overflow-hidden shadow rounded-lg border border-[var(--color-border)]">
          <div class="px-4 py-5 sm:p-6">
            <div class="flex items-center">
              <div class="flex-shrink-0">
                <div class="h-20 w-20 rounded-full overflow-hidden bg-[var(--color-background-mute)] flex items-center justify-center border-2 border-[var(--color-border)]">
                  <img v-if="authStore.user?.profilePicturePath"
                       :src="authStore.user.profilePicturePath"
                       :alt="authStore.userFullName"
                       class="h-full w-full object-cover"
                       @error="onImageError" />
                  <svg v-else
                       class="h-12 w-12 text-[var(--vt-c-indigo)]"
                       fill="currentColor"
                       viewBox="0 0 24 24">
                    <path d="M24 20.993V24H0v-2.996A14.977 14.977 0 0112.004 15c4.904 0 9.26 2.354 11.996 5.993zM16.002 8.999a4 4 0 11-8 0 4 4 0 018 0z" />
                  </svg>
                </div>
              </div>
              <div class="ml-5 flex-1">
                <h1 class="text-2xl font-bold text-[var(--color-heading)]">{{ authStore.userFullName }}</h1>
                <p class="text-sm text-[var(--color-text)] opacity-75">{{ authStore.user?.email }}</p>
                <div class="mt-1 flex items-center space-x-2">
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                        :class="authStore.user?.isOnline ? 'bg-green-100 text-green-800' : 'bg-[var(--color-background-mute)] text-[var(--color-text)] border border-[var(--color-border)]'">
                    <span class="w-1.5 h-1.5 rounded-full mr-1.5"
                          :class="authStore.user?.isOnline ? 'bg-green-400' : 'bg-[var(--color-text)] opacity-50'"></span>
                    {{ authStore.user?.isOnline ? 'Online' : 'Offline' }}
                  </span>
                  <span v-for="role in authStore.userRoles"
                        :key="role"
                        class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-[var(--vt-c-indigo)] text-white">
                    {{ role }}
                  </span>
                </div>
              </div>
              <div class="flex flex-col space-y-2">
                <button @click="handleLogout"
                        :disabled="authStore.isLoading"
                        class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 transition-colors duration-200">
                  <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                  </svg>
                  {{ authStore.isLoading ? 'Logging out...' : 'Logout' }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Profile Information -->
        <div class="mt-8 bg-[var(--color-background-soft)] overflow-hidden shadow rounded-lg border border-[var(--color-border)]">
          <div class="px-4 py-5 sm:p-6">
            <h2 class="text-lg font-medium text-[var(--color-heading)] mb-6">Profile Information</h2>

            <dl class="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">First Name</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">{{ authStore.user?.firstName || 'N/A' }}</dd>
              </div>

              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Last Name</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">{{ authStore.user?.lastName || 'N/A' }}</dd>
              </div>

              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Email Address</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">{{ authStore.user?.email || 'N/A' }}</dd>
              </div>

              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Username</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">{{ authStore.user?.userName || 'N/A' }}</dd>
              </div>

              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Date of Birth</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">
                  {{ authStore.user?.dateOfBirth ? formatDate(authStore.user.dateOfBirth) : 'N/A' }}
                </dd>
              </div>

              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Registration Date</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">
                  {{ authStore.user?.registrationDate ? formatDate(authStore.user.registrationDate) : 'N/A' }}
                </dd>
              </div>

              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Last Login</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">
                  {{ authStore.user?.lastLoginDate ? formatDateTime(authStore.user.lastLoginDate) : 'N/A' }}
                </dd>
              </div>

              <div>
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Account Status</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)]">
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                        :class="authStore.user?.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
                    {{ authStore.user?.isActive ? 'Active' : 'Inactive' }}
                  </span>
                  <span v-if="authStore.user?.isVerified"
                        class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 ml-2">
                    Verified
                  </span>
                </dd>
              </div>

              <div v-if="authStore.user?.bio" class="sm:col-span-2">
                <dt class="text-sm font-medium text-[var(--color-text)] opacity-75">Bio</dt>
                <dd class="mt-1 text-sm text-[var(--color-text)] bg-[var(--color-background-mute)] p-3 rounded-md border border-[var(--color-border)]">{{ authStore.user.bio }}</dd>
              </div>
            </dl>
          </div>
        </div>

        <!-- Account Actions -->
        <div class="mt-8 bg-[var(--color-background-soft)] overflow-hidden shadow rounded-lg border border-[var(--color-border)]">
          <div class="px-4 py-5 sm:p-6">
            <h2 class="text-lg font-medium text-[var(--color-heading)] mb-6">Account Actions</h2>

            <div class="space-y-4">
              <button class="inline-flex items-center px-4 py-2 border border-[var(--color-border)] shadow-sm text-sm font-medium rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--vt-c-indigo)] transition-colors duration-200">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                Edit Profile
              </button>

              <button class="inline-flex items-center px-4 py-2 border border-[var(--color-border)] shadow-sm text-sm font-medium rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--vt-c-indigo)] transition-colors duration-200">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
                Change Password
              </button>

              <button class="inline-flex items-center px-4 py-2 border border-[var(--color-border)] shadow-sm text-sm font-medium rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--vt-c-indigo)] transition-colors duration-200">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                Account Settings
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { useRouter } from 'vue-router';
  import { useAuthStore } from '../../stores/authStore';
  import { ref, computed } from 'vue';

  const router = useRouter();
  const authStore = useAuthStore();

  // FIXED: Control debug component updates to prevent excessive API calls
  const debugKey = ref(0);
  const isDevelopment = computed(() => import.meta.env.DEV);
  let lastDebugAction = 0;

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString();
  };

  const formatDateTime = (dateString) => {
    return new Date(dateString).toLocaleString();
  };

  const handleLogout = async () => {
    await authStore.logout();
    router.push('/');
  };

  const onImageError = (event) => {
    // Handle image loading errors
    event.target.style.display = 'none';
  };

  // FIXED: Throttle debug actions to prevent API spam
  const handleDebugAction = (action) => {
    const now = Date.now();
    if (now - lastDebugAction < 2000) { // 2 second throttle
      console.warn('Debug action throttled, please wait...');
      return;
    }

    lastDebugAction = now;

    // Refresh debug component after action
    setTimeout(() => {
      debugKey.value++;
    }, 1000);
  };
</script>

<style scoped>
  .profile-page-wrapper {
    /* Ensure smooth transitions */
    transition: opacity 0.3s ease-in-out, transform 0.3s ease-in-out;
  }

    /* Optional: Add enter/leave transition styles */
    .profile-page-wrapper.v-enter-active,
    .profile-page-wrapper.v-leave-active {
      transition: opacity 0.3s ease-in-out, transform 0.3s ease-in-out;
    }

    .profile-page-wrapper.v-enter-from,
    .profile-page-wrapper.v-leave-to {
      opacity: 0;
      transform: translateY(10px);
    }

  /* Custom focus ring offset color */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }
</style>
