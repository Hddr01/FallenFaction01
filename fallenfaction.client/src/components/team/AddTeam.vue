<!-- components/team/AddTeam.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-2xl mx-auto">
      <!-- Page Header -->
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Create New Team</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Start building your team and collaborate on manga translations</p>
      </div>

      <!-- Form Container -->
      <div class="bg-[var(--color-background-soft)] overflow-hidden shadow rounded-lg border border-[var(--color-border)]">
        <div class="px-4 py-5 sm:p-6">
          <form @submit.prevent="handleSubmit" class="space-y-6">
            <!-- Team Name -->
            <div>
              <label for="name" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Team Name <span class="text-red-500">*</span>
              </label>
              <input id="name"
                     v-model="form.name"
                     type="text"
                     class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200"
                     :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.name }"
                     placeholder="Enter team name"
                     maxlength="100"
                     required />
              <div v-if="errors.name" class="mt-1 text-sm text-red-600">{{ errors.name }}</div>
              <div class="mt-1 text-xs text-[var(--color-text)] opacity-50 text-right">{{ form.name.length }}/100</div>
            </div>

            <!-- Description -->
            <div>
              <label for="description" class="block text-sm font-medium text-[var(--color-text)] mb-2">
                Description <span class="text-red-500">*</span>
              </label>
              <textarea id="description"
                        v-model="form.description"
                        class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 resize-vertical"
                        :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.description }"
                        placeholder="Describe your team's goals, preferred genres, or any other relevant information"
                        maxlength="500"
                        rows="4"
                        required></textarea>
              <div v-if="errors.description" class="mt-1 text-sm text-red-600">{{ errors.description }}</div>
              <div class="mt-1 text-xs text-[var(--color-text)] opacity-50 text-right">{{ form.description.length }}/500</div>
            </div>

            <!-- Submit Buttons -->
            <div class="flex space-x-4 pt-4">
              <button type="submit"
                      :disabled="loading || !isFormValid"
                      class="flex-1 inline-flex justify-center items-center px-6 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                {{ loading ? 'Creating...' : 'Create Team' }}
              </button>
              <button type="button"
                      @click="handleCancel"
                      :disabled="loading"
                      class="flex-1 inline-flex justify-center items-center px-6 py-2 border border-[var(--color-border)] text-sm font-medium rounded-md text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[var(--color-border-hover)] disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Success/Error Messages -->
      <div v-if="message.text"
           class="mt-6 rounded-md p-4"
           :class="{
             'bg-green-50 border border-green-200': message.type === 'success',
             'bg-red-50 border border-red-200': message.type === 'error'
           }">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg v-if="message.type === 'success'" class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
            <svg v-else class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3">
            <p class="text-sm font-medium"
               :class="{
                 'text-green-800': message.type === 'success',
                 'text-red-800': message.type === 'error'
               }">
              {{ message.text }}
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { ref, computed, reactive } from 'vue';
  import { useRouter } from 'vue-router';
  import { teamService } from '../../services/teamService';

  export default {
    name: 'AddTeam',
    setup() {
      const router = useRouter();

      const form = reactive({
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

      const loading = ref(false);

      const isFormValid = computed(() => {
        return form.name.trim().length > 0 &&
          form.description.trim().length > 0 &&
          form.name.length <= 100 &&
          form.description.length <= 500;
      });

      const validateForm = () => {
        // Clear previous errors
        errors.name = '';
        errors.description = '';

        let isValid = true;

        if (!form.name.trim()) {
          errors.name = 'Team name is required';
          isValid = false;
        } else if (form.name.length > 100) {
          errors.name = 'Team name cannot exceed 100 characters';
          isValid = false;
        }

        if (!form.description.trim()) {
          errors.description = 'Description is required';
          isValid = false;
        } else if (form.description.length > 500) {
          errors.description = 'Description cannot exceed 500 characters';
          isValid = false;
        }

        return isValid;
      };

      const handleSubmit = async () => {
        if (!validateForm()) {
          return;
        }

        loading.value = true;
        message.text = '';

        try {
          const result = await teamService.createTeam({
            name: form.name.trim(),
            description: form.description.trim()
          });

          if (result.success) {
            message.text = 'Team created successfully!';
            message.type = 'success';

            // Redirect to team page after short delay
            setTimeout(() => {
              router.push(`/teams`);
            }, 1500);
          } else {
            message.text = result.error;
            message.type = 'error';

            // Handle validation errors
            if (result.validationErrors) {
              Object.keys(result.validationErrors).forEach(field => {
                if (errors.hasOwnProperty(field.toLowerCase())) {
                  errors[field.toLowerCase()] = result.validationErrors[field][0];
                }
              });
            }
          }
        } catch (error) {
          message.text = 'An unexpected error occurred. Please try again.';
          message.type = 'error';
        } finally {
          loading.value = false;
        }
      };

      const handleCancel = () => {
        router.push('/teams');
      };

      return {
        form,
        errors,
        message,
        loading,
        isFormValid,
        handleSubmit,
        handleCancel
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
</style>
