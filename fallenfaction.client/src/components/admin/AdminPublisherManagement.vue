<!-- components/admin/AdminPublisherManagement.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Publisher Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Add, edit, and manage publishers</p>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-5 gap-8">
        <!-- Left Column: Publisher Form -->
        <div class="lg:col-span-2">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)]">Publisher Management</h3>
            </div>

            <div class="p-6">
              <form @submit.prevent="handleSubmit" class="space-y-6">
                <!-- Hidden ID field for editing -->
                <input type="hidden" v-model="form.id" />

                <!-- Publisher Name -->
                <div>
                  <label for="publisherName" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                    Publisher Name <span class="text-red-500">*</span>
                  </label>
                  <input id="publisherName"
                         v-model="form.name"
                         @input="searchAsYouType"
                         type="text"
                         class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200"
                         :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.name }"
                         placeholder="Enter Publisher Name"
                         maxlength="200"
                         required />
                  <div v-if="errors.name" class="mt-1 text-sm text-red-600">{{ errors.name }}</div>
                </div>

                <!-- Description -->
                <div>
                  <label for="publisherDescription" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                    Description
                  </label>
                  <textarea id="publisherDescription"
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
                          @click="updatePublisher"
                          :disabled="loading || !isEditing || !isFormValid"
                          class="px-4 py-2 bg-yellow-600 text-white text-sm font-medium rounded-md hover:bg-yellow-700 focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                    <svg v-if="loading && isEditing" class="animate-spin -ml-1 mr-1 h-4 w-4 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    {{ loading && isEditing ? 'Updating...' : 'Update' }}
                  </button>

                  <button type="button"
                          @click="deletePublisher"
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

        <!-- Right Column: Publisher List -->
        <div class="lg:col-span-3">
          <div class="bg-[var(--color-background-soft)] rounded-lg shadow-md border border-[var(--color-border)]">
            <div class="px-6 py-4 border-b border-[var(--color-border)]">
              <h3 class="text-lg font-semibold text-[var(--color-heading)] mb-4">Publisher List</h3>
              <input type="text"
                     v-model="searchQuery"
                     @input="handleSearch"
                     placeholder="Search by name..."
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200" />
            </div>

            <!-- Loading State -->
            <div v-if="isLoadingPublishers" class="p-6 text-center">
              <div class="inline-flex items-center">
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span class="text-[var(--color-text)]">Loading publishers...</span>
              </div>
            </div>

            <!-- Publishers Table -->
            <div v-else class="overflow-x-auto max-h-96">
              <table class="min-w-full divide-y divide-[var(--color-border)]">
                <thead class="bg-[var(--color-background-mute)] sticky top-0">
                  <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">ID</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Name</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Description</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Titles</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-[var(--color-text)] uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody class="bg-[var(--color-background-soft)] divide-y divide-[var(--color-border)]">
                  <tr v-for="publisher in displayedPublishers"
                      :key="publisher.id"
                      class="hover:bg-[var(--color-background-mute)] transition-colors duration-200"
                      :class="{ 'bg-green-50 border-l-4 border-l-green-500': selectedPublisherId === publisher.id }">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-[var(--color-text)]">
                      {{ publisher.id }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)] max-w-xs truncate">
                      {{ publisher.name }}
                    </td>
                    <td class="px-6 py-4 text-sm text-[var(--color-text)] max-w-xs">
                      <div class="line-clamp-2">{{ publisher.description || '-' }}</div>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-[var(--color-text)]">
                      {{ publisher.titleCount || 0 }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button @click="editPublisher(publisher)"
                              class="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded text-green-700 bg-green-100 hover:bg-green-200 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors duration-200">
                        Edit
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>

              <!-- No Results -->
              <div v-if="displayedPublishers.length === 0 && !isLoadingPublishers" class="text-center py-8">
                <svg class="mx-auto h-8 w-8 text-[var(--color-text)] opacity-50" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                </svg>
                <h3 class="mt-2 text-sm font-medium text-[var(--color-text)]">No publishers found</h3>
                <p class="mt-1 text-sm text-[var(--color-text)] opacity-75">{{ searchQuery ? 'Try adjusting your search terms' : 'Start by adding a new publisher' }}</p>
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
  import { publisherService } from '../../services/publisherService';

  export default {
    name: 'AdminPublisherManagement',
    setup() {
      const publishers = ref([]);
      const searchResults = ref([]);
      const searchQuery = ref('');
      const isLoadingPublishers = ref(true);
      const loading = ref(false);
      const selectedPublisherId = ref(null);

      const form = reactive({
        id: '',
        name: '',
        description: ''
      });

      const errors = reactive({
        name: '',
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
          form.description.length <= 2000;
      });

      const displayedPublishers = computed(() => {
        return searchQuery.value ? searchResults.value : publishers.value;
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
        errors.description = '';

        let isValid = true;

        if (!form.name.trim()) {
          errors.name = 'Publisher name is required';
          isValid = false;
        } else if (form.name.length > 200) {
          errors.name = 'Name cannot exceed 200 characters';
          isValid = false;
        }

        if (form.description.length > 2000) {
          errors.description = 'Description cannot exceed 2000 characters';
          isValid = false;
        }

        return isValid;
      };

      const loadPublishers = async () => {
        isLoadingPublishers.value = true;
        try {
          const result = await publisherService.getPublishers();
          if (result.success) {
            publishers.value = result.data;
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to load publishers', 'error');
        } finally {
          isLoadingPublishers.value = false;
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
            const result = await publisherService.searchPublishers(searchQuery.value.trim());
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
        // This mimics the original behavior where typing in name field searches publishers
        if (form.name.trim()) {
          searchQuery.value = form.name;
          handleSearch();
        }
      };

      const editPublisher = (publisher) => {
        form.id = publisher.id;
        form.name = publisher.name;
        form.description = publisher.description || '';
        selectedPublisherId.value = publisher.id;
      };

      const clearForm = () => {
        form.id = '';
        form.name = '';
        form.description = '';
        selectedPublisherId.value = null;
        errors.name = '';
        errors.description = '';
      };

      const handleSubmit = async () => {
        if (!validateForm()) {
          return;
        }

        loading.value = true;

        try {
          const publisherData = {
            name: form.name.trim(),
            description: form.description.trim()
          };

          const result = await publisherService.createPublisher(publisherData);

          if (result.success) {
            showMessage(result.message || 'Publisher created successfully!', 'success');
            clearForm();
            await loadPublishers();
          } else {
            showMessage(result.error, 'error');
            if (result.validationErrors) {
              result.validationErrors.forEach(error => {
                if (error.toLowerCase().includes('name')) {
                  errors.name = error;
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

      const updatePublisher = async () => {
        if (!validateForm() || !form.id) {
          return;
        }

        loading.value = true;

        try {
          const publisherData = {
            name: form.name.trim(),
            description: form.description.trim()
          };

          const result = await publisherService.updatePublisher(form.id, publisherData);

          if (result.success) {
            showMessage(result.message || 'Publisher updated successfully!', 'success');
            clearForm();
            await loadPublishers();
          } else {
            showMessage(result.error, 'error');
            if (result.validationErrors) {
              result.validationErrors.forEach(error => {
                if (error.toLowerCase().includes('name')) {
                  errors.name = error;
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

      const deletePublisher = async () => {
        if (!form.id || !confirm(`Are you sure you want to delete "${form.name}"? This action cannot be undone.`)) {
          return;
        }

        loading.value = true;

        try {
          const result = await publisherService.deletePublisher(form.id);

          if (result.success) {
            showMessage(result.message || 'Publisher deleted successfully!', 'success');
            clearForm();
            await loadPublishers();
          } else {
            showMessage(result.error, 'error');
          }
        } catch (error) {
          showMessage('Failed to delete publisher', 'error');
        } finally {
          loading.value = false;
        }
      };

      onMounted(() => {
        loadPublishers();
      });

      return {
        publishers,
        searchResults,
        searchQuery,
        isLoadingPublishers,
        loading,
        selectedPublisherId,
        form,
        errors,
        message,
        isEditing,
        isFormValid,
        displayedPublishers,
        loadPublishers,
        handleSearch,
        searchAsYouType,
        editPublisher,
        clearForm,
        handleSubmit,
        updatePublisher,
        deletePublisher
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

  .line-clamp-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
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
