<!-- components/publisher/PublisherList.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Header -->
      <div class="sm:flex sm:items-center sm:justify-between mb-8">
        <div>
          <h1 class="text-3xl font-bold text-[var(--color-heading)]">Publishers</h1>
          <p class="mt-2 text-[var(--color-text)] opacity-75">Browse publishers in the database</p>
        </div>
        <div v-if="authStore.isAuthenticated" class="mt-4 sm:mt-0 sm:ml-16 sm:flex-none flex gap-2">
          <!-- Regular users can create publishers -->
          <button @click="navigateToAddPublisher"
                  class="inline-flex items-center justify-center rounded-md border border-transparent bg-green-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 transition-all duration-200">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Publisher
          </button>
          <!-- Admin management button -->
          <button v-if="authStore.isAdmin"
                  @click="navigateToAdminManagement"
                  class="inline-flex items-center justify-center rounded-md border border-transparent bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-all duration-200">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            Manage
          </button>
        </div>
      </div>

      <!-- Search Bar -->
      <div class="mb-6">
        <div class="max-w-md">
          <div class="relative">
            <input v-model="searchQuery"
                   @input="handleSearch"
                   type="text"
                   class="w-full px-4 py-2 pl-10 pr-4 border border-[var(--color-border)] rounded-md bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 transition-colors duration-200"
                   placeholder="Search publishers..." />
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <svg class="h-5 w-5 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </div>
            <div v-if="searchLoading" class="absolute inset-y-0 right-0 pr-3 flex items-center">
              <svg class="animate-spin h-4 w-4 text-green-600" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            </div>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="text-center py-12">
        <div class="inline-flex items-center">
          <svg class="animate-spin -ml-1 mr-3 h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span class="text-xl text-[var(--color-text)]">Loading publishers...</span>
        </div>
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-md p-4">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3">
            <h3 class="text-sm font-medium text-red-800">Error Loading Publishers</h3>
            <div class="mt-2 text-sm text-red-700">
              <p>{{ error }}</p>
            </div>
            <div class="mt-4">
              <button @click="loadPublishers"
                      class="bg-red-100 px-3 py-2 rounded-md text-sm font-medium text-red-800 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-red-500 transition-colors duration-200">
                Try Again
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else-if="displayedPublishers.length === 0 && !loading" class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-[var(--color-text)] opacity-25" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
        </svg>
        <h3 class="mt-2 text-sm font-medium text-[var(--color-text)] opacity-75">{{ searchQuery ? 'No publishers found' : 'No publishers' }}</h3>
        <p class="mt-1 text-sm text-[var(--color-text)] opacity-50">
          {{ searchQuery ? 'Try adjusting your search terms.' : 'Get started by creating a new publisher.' }}
        </p>
        <div v-if="!searchQuery && authStore.isAuthenticated" class="mt-6">
          <button @click="navigateToAddPublisher"
                  class="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition-all duration-200">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            Add Publisher
          </button>
        </div>
      </div>

      <!-- Publishers Grid -->
      <div v-else class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <div v-for="publisher in displayedPublishers"
             :key="publisher.id"
             class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-sm hover:shadow-md transition-shadow duration-200 overflow-hidden">
          <div class="p-6">
            <div class="flex items-center justify-between">
              <div class="flex-1 min-w-0">
                <h3 class="text-lg font-medium text-[var(--color-heading)] truncate">{{ publisher.name }}</h3>
              </div>
            </div>

            <div v-if="publisher.description" class="mt-3">
              <p class="text-sm text-[var(--color-text)] opacity-75 line-clamp-3">{{ publisher.description }}</p>
            </div>

            <div class="mt-4 flex items-center justify-between text-xs text-[var(--color-text)] opacity-50">
              <span>{{ publisher.titleCount }} title{{ publisher.titleCount !== 1 ? 's' : '' }}</span>
              <span>ID: {{ publisher.id }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { ref, reactive, computed, onMounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { useAuthStore } from '../../stores/authStore';
  import { publisherService } from '../../services/publisherService';

  export default {
    name: 'PublisherList',
    setup() {
      const router = useRouter();
      const authStore = useAuthStore();

      const publishers = ref([]);
      const searchResults = ref([]);
      const searchQuery = ref('');
      const loading = ref(false);
      const searchLoading = ref(false);
      const error = ref('');

      let searchTimeout = null;

      const displayedPublishers = computed(() => {
        return searchQuery.value ? searchResults.value : publishers.value;
      });

      const loadPublishers = async () => {
        loading.value = true;
        error.value = '';

        try {
          const result = await publisherService.getPublishers();
          if (result.success) {
            publishers.value = result.data;
          } else {
            error.value = result.error;
          }
        } catch (err) {
          error.value = 'Failed to load publishers';
        } finally {
          loading.value = false;
        }
      };

      const handleSearch = () => {
        if (searchTimeout) {
          clearTimeout(searchTimeout);
        }

        searchTimeout = setTimeout(async () => {
          if (!searchQuery.value.trim()) {
            searchResults.value = [];
            return;
          }

          searchLoading.value = true;
          try {
            const result = await publisherService.searchPublishers(searchQuery.value.trim());
            if (result.success) {
              searchResults.value = result.data;
            } else {
              console.error('Search error:', result.error);
              searchResults.value = [];
            }
          } catch (error) {
            console.error('Search error:', error);
            searchResults.value = [];
          } finally {
            searchLoading.value = false;
          }
        }, 300);
      };

      const navigateToAddPublisher = () => {
        // Regular users go to the user creation route
        router.push('/publisher/create');
      };

      const navigateToAdminManagement = () => {
        // Admins go to the admin management page
        router.push('/admin/publishers');
      };

      onMounted(() => {
        loadPublishers();
      });

      return {
        authStore,
        publishers,
        searchResults,
        searchQuery,
        loading,
        searchLoading,
        error,
        displayedPublishers,
        loadPublishers,
        handleSearch,
        navigateToAddPublisher,
        navigateToAdminManagement
      };
    }
  };
</script>

<style scoped>
  .line-clamp-3 {
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
</style>
