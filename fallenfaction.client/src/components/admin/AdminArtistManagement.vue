<!-- components/admin/AdminArtistManagement.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Artist Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Add, edit, and manage artists</p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-5 gap-8">
        <!-- Left Column: Artist Form -->
        <div class="lg:col-span-2">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)]">Artist Management</h3>
            </div>

            <div class="p-6">
              <form @submit.prevent="handleSubmit" class="space-y-6">
                <!-- Hidden ID field for editing -->
                <input type="hidden" v-model="form.id" />

                <!-- Artist Name -->
                <div>
                  <label for="artistName" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                    Artist Name <span class="text-red-500">*</span>
                  </label>
                  <input id="artistName"
                         v-model="form.name"
                         @input="searchAsYouType"
                         type="text"
                         class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200"
                         :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.name }"
                         placeholder="Enter Artist Name"
                         maxlength="200"
                         required />
                  <div v-if="errors.name" class="mt-1 text-sm text-red-600">{{ errors.name }}</div>
                </div>

                <!-- Other Name -->
                <div>
                  <label for="artistOtherName" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                    Other Name
                  </label>
                  <input id="artistOtherName"
                         v-model="form.otherName"
                         type="text"
                         class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200"
                         :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.otherName }"
                         placeholder="Enter Other Name"
                         maxlength="300" />
                  <div v-if="errors.otherName" class="mt-1 text-sm text-red-600">{{ errors.otherName }}</div>
                </div>

                <!-- Description -->
                <div>
                  <label for="artistDescription" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                    Description
                  </label>
                  <textarea id="artistDescription"
                            v-model="form.description"
                            class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 resize-vertical"
                            :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.description }"
                            placeholder="Enter Description"
                            maxlength="2000"
                            rows="4"></textarea>
                  <div v-if="errors.description" class="mt-1 text-sm text-red-600">{{ errors.description }}</div>
                </div>

                <!-- Action Buttons -->
                <div class="grid grid-cols-3 gap-4">
                  <button type="submit"
                          :disabled="loading || !isFormValid"
                          class="px-4 py-2 bg-green-600 text-white text-sm font-medium rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    <svg v-if="loading && !isEditing" class="animate-spin -ml-1 mr-1 h-4 w-4 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    {{ loading && !isEditing ? 'Adding...' : 'Add' }}
                  </button>

                  <button type="button"
                          @click="updateArtist"
                          :disabled="loading || !isEditing || !isFormValid"
                          class="px-4 py-2 bg-yellow-600 text-white text-sm font-medium rounded-md hover:bg-yellow-700 focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    <svg v-if="loading && isEditing" class="animate-spin -ml-1 mr-1 h-4 w-4 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    {{ loading && isEditing ? 'Updating...' : 'Update' }}
                  </button>

                  <button type="button"
                          @click="deleteArtist"
                          :disabled="loading || !isEditing"
                          class="px-4 py-2 bg-red-600 text-white text-sm font-medium rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    Delete
                  </button>
                </div>

                <!-- Clear/Reset Button -->
                <button type="button"
                        @click="clearForm"
                        class="w-full px-4 py-2 border border-[var(--color-border)] text-sm font-medium rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-[var(--color-border-hover)] transition-all duration-200">
                  Clear Form
                </button>
              </form>
            </div>
          </div>
        </div>

        <!-- Right Column: Artist List -->
        <div class="lg:col-span-3">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4">Artist List</h3>
              <input type="text"
                     v-model="searchQuery"
                     @input="handleSearch"
                     placeholder="Search by name..."
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
            </div>

            <!-- Loading State -->
            <div v-if="isLoadingArtists" class="p-6 text-center">
              <div class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span class="text-[var(--color-text)]">Loading artists...</span>
              </div>
            </div>

            <!-- Artists Table -->
            <div v-else class="overflow-x-auto max-h-96">
              <table class="min-w-full divide-y divide-[var(--color-border)]">
                <thead class="bg-[var(--color-background-mute)] sticky top-0">
                  <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">ID</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Name</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Other Name</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Titles</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
                  <tr v-for="artist in displayedArtists"
                      :key="artist.id"
                      class="hover:bg-[var(--color-background-mute)] transition-colors duration-200"
                      :class="{ 'bg-green-50 border-l-4 border-l-green-500': selectedArtistId === artist.id }">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                      {{ artist.id }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      {{ artist.name }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      {{ artist.otherName || '-' }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      {{ artist.titleCount || 0 }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button @click="editArtist(artist)"
                              class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-green-700 bg-green-100 hover:bg-green-200 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
                        Edit
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>

              <!-- No Results -->
              <div v-if="displayedArtists.length === 0 && !isLoadingArtists" class="text-center py-8">
                <svg class="mx-auto h-8 w-8 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
                <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No artists found</h3>
                <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">{{ searchQuery ? 'Try adjusting your search terms' : 'Start by adding a new artist' }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Success/Error Messages -->
      <div v-if="message.text"
           class="fixed top-20 right-4 max-w-sm w-full bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-lg transition-all duration-300"
           style="z-index: 9999;"
           :class="{
             'border-green-500': message.type === 'success',
             'border-red-500': message.type === 'error'
           }">
        <div class="p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg v-if="message.type === 'success'" class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
              </svg>
              <svg v-else class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="ml-3 w-0 flex-1">
              <p class="text-sm font-medium text-[var(--color-text)]">
                {{ message.text }}
              </p>
            </div>
            <div class="ml-4 flex-shrink-0 flex">
              <button @click="message.text = ''"
                      class="rounded-md inline-flex text-[var(--color-text)] opacity-50 hover:opacity-75 focus:outline-none focus:ring-2 focus:ring-green-500 transition-all duration-200">
                <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { ref, reactive, computed, onMounted } from 'vue';
  import { artistService } from '../../services/artistService';

  export default {
    name: 'AdminArtistManagement',
    setup() {
      const artists = ref([]);
      const searchResults = ref([]);
      const searchQuery = ref('');
      const isLoadingArtists = ref(true);
      const loading = ref(false);
      const selectedArtistId = ref(null);

      const form = reactive({
        id: '',
        name: '',
        otherName: '',
        description: ''
      });

      const errors = reactive({
        name: '',
        otherName: '',
        description: ''
      });

      const message = reactive({
        text: '',
        type: ''
      });

      let searchTimeout = null;

      const isEditing = computed(() => !!form.id);

      const isFormValid = computed(() => {
        return form.name.trim().length > 0 &&
          form.name.length <= 200 &&
          form.otherName.length <= 300 &&
          form.description.length <= 2000;
      });

      const displayedArtists = computed(() => {
        return searchQuery.value ? searchResults.value : artists.value;
      });

      const showMessage = (text, type) => {
        message.text = text;
        message.type = type;
        setTimeout(() => {
          message.text = '';
        }, type === 'success' ? 3000 : 5000);
      };

      const validateForm = () => {
        errors.name = '';
        errors.otherName = '';
        errors.description = '';

        let isValid = true;

        if (!form.name.trim()) {
          errors.name = 'Artist name is required';
          isValid = false;
        } else if (form.name.length > 200) {
          errors.name = 'Name cannot exceed 200 characters';
          isValid = false;
        }

        if (form.otherName.length > 300) {
          errors.otherName = 'Alternative names cannot exceed 300 characters';
          isValid = false;
        }

        if (form.description.length > 2000) {
          errors.description = 'Description cannot exceed 2000 characters';
          isValid = false;
        }

        return isValid;
      };

      const loadArtists = async () => {
        isLoadingArtists.value = true;
        try {
          const result = await artistService.getArtists();
          if (result.success) {
            artists.value = result.data;
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to load artists', 'error');
        } finally {
          isLoadingArtists.value = false;
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

          try {
            const result = await artistService.searchArtists(searchQuery.value.trim());
            if (result.success) {
              searchResults.value = result.data;
            } else {
              searchResults.value = [];
            }
          } catch (error) {
            searchResults.value = [];
          }
        }, 300);
      };

      const searchAsYouType = () => {
        // This mimics the original behavior where typing in name field searches artists
        if (form.name.trim()) {
          searchQuery.value = form.name;
          handleSearch();
        }
      };

      const editArtist = (artist) => {
        form.id = artist.id;
        form.name = artist.name;
        form.otherName = artist.otherName || '';
        form.description = artist.description || '';
        selectedArtistId.value = artist.id;
      };

      const clearForm = () => {
        form.id = '';
        form.name = '';
        form.otherName = '';
        form.description = '';
        selectedArtistId.value = null;
        errors.name = '';
        errors.otherName = '';
        errors.description = '';
      };

      const handleSubmit = async () => {
        if (!validateForm()) {
          return;
        }

        loading.value = true;

        try {
          const artistData = {
            name: form.name.trim(),
            otherName: form.otherName.trim(),
            description: form.description.trim()
          };

          const result = await artistService.createArtist(artistData);

          if (result.success) {
            showMessage(result.message || 'Artist created successfully!', 'success');
            clearForm();
            await loadArtists();
          } else {
            showMessage(result.error, 'error');
            if (result.validationErrors) {
              result.validationErrors.forEach(error => {
                if (error.toLowerCase().includes('name')) {
                  errors.name = error;
                } else if (error.toLowerCase().includes('other')) {
                  errors.otherName = error;
                } else if (error.toLowerCase().includes('description')) {
                  errors.description = error;
                }
              });
            }
          }
        } catch (error) {
          showMessage('An unexpected error occurred', 'error');
        } finally {
          loading.value = false;
        }
      };

      const updateArtist = async () => {
        if (!validateForm() || !form.id) {
          return;
        }

        loading.value = true;

        try {
          const artistData = {
            name: form.name.trim(),
            otherName: form.otherName.trim(),
            description: form.description.trim()
          };

          const result = await artistService.updateArtist(form.id, artistData);

          if (result.success) {
            showMessage(result.message || 'Artist updated successfully!', 'success');
            clearForm();
            await loadArtists();
          } else {
            showMessage(result.error, 'error');
            if (result.validationErrors) {
              result.validationErrors.forEach(error => {
                if (error.toLowerCase().includes('name')) {
                  errors.name = error;
                } else if (error.toLowerCase().includes('other')) {
                  errors.otherName = error;
                } else if (error.toLowerCase().includes('description')) {
                  errors.description = error;
                }
              });
            }
          }
        } catch (error) {
          showMessage('An unexpected error occurred', 'error');
        } finally {
          loading.value = false;
        }
      };

      const deleteArtist = async () => {
        if (!form.id || !confirm(`Are you sure you want to delete "${form.name}"? This action cannot be undone.`)) {
          return;
        }

        loading.value = true;

        try {
          const result = await artistService.deleteArtist(form.id);

          if (result.success) {
            showMessage(result.message || 'Artist deleted successfully!', 'success');
            clearForm();
            await loadArtists();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to delete artist', 'error');
        } finally {
          loading.value = false;
        }
      };

      onMounted(() => {
        loadArtists();
      });

      return {
        artists,
        searchResults,
        searchQuery,
        isLoadingArtists,
        loading,
        selectedArtistId,
        form,
        errors,
        message,
        isEditing,
        isFormValid,
        displayedArtists,
        loadArtists,
        handleSearch,
        searchAsYouType,
        editArtist,
        clearForm,
        handleSubmit,
        updateArtist,
        deleteArtist
      };
    }
  };
</script>

<style scoped>
  /* Custom focus ring offset color */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }

  /* Custom scrollbar for table */
  .overflow-x-auto::-webkit-scrollbar {
    height: 8px;
  }

  .overflow-x-auto::-webkit-scrollbar-track {
    background: var(--color-background-mute);
  }

  .overflow-x-auto::-webkit-scrollbar-thumb {
    background: var(--color-border);
    border-radius: 4px;
  }

    .overflow-x-auto::-webkit-scrollbar-thumb:hover {
      background: var(--color-border-hover);
    }
</style>
