<!-- components/publisher/AddPublisher.vue -->
<template>
  <div class="min-h-screen bg-[var(--color-background)] py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-2xl mx-auto">
      <!-- Page Header -->
      <div class="text-center mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Add New Publisher</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Add a new publisher to the database</p>
      </div>

      <!-- Form Container -->
      <div class="bg-[var(--color-background-soft)] overflow-hidden shadow rounded-lg border border-[var(--color-border)]">
        <div class="px-4 py-5 sm:p-6">
          <form @submit.prevent="handleSubmit" class="space-y-6">
            <!-- Publisher Name -->
            <div class="space-y-3">
              <Label for="name" class="text-sm font-medium text-foreground">
                Publisher Name <span class="text-red-500">*</span>
              </Label>
              <Input id="name"
                     v-model="form.name"
                     type="text"
                     class="form-input-bg"
                     :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.name }"
                     placeholder="Enter publisher name"
                     maxlength="200"
                     required />
              <div v-if="errors.name" class="text-sm text-red-600">{{ errors.name }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-50 text-right">{{ form.name.length }}/200</div>
            </div>

            <!-- Description -->
            <div class="space-y-3">
              <Label for="description" class="text-sm font-medium text-foreground">
                Description
              </Label>
              <Textarea id="description"
                        v-model="form.description"
                        class="form-textarea-bg resize-vertical"
                        :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.description }"
                        placeholder="Brief description about the publisher (optional)"
                        maxlength="2000"
                        rows="5" />
              <div v-if="errors.description" class="text-sm text-red-600">{{ errors.description }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-50 text-right">{{ form.description.length }}/2000</div>
            </div>

            <!-- Submit Buttons -->
            <div class="flex space-x-4 pt-4">
              <Button type="submit"
                      :disabled="loading || !isFormValid"
                      class="flex-1">
                <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                {{ loading ? 'Creating...' : 'Create Publisher' }}
              </Button>
              <Button type="button"
                      @click="handleCancel"
                      :disabled="loading"
                      variant="outline"
                      class="flex-1">
                Cancel
              </Button>
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
            <div v-if="message.type === 'error' && validationErrors.length > 0" class="mt-2">
              <ul class="text-sm list-disc list-inside"
                  :class="{ 'text-red-700': message.type === 'error' }">
                <li v-for="error in validationErrors" :key="error">{{ error }}</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { ref, computed, reactive } from 'vue';
  import { useRouter } from 'vue-router';
  import { useAuthStore } from '../../stores/authStore';
  import { publisherService } from '../../services/publisherService';
  import { Input } from '@/components/ui/input';
  import { Label } from '@/components/ui/label';
  import { Textarea } from '@/components/ui/textarea';
  import { Button } from '@/components/ui/button';

  export default {
    name: 'AddPublisher',
    components: {
      Input,
      Label,
      Textarea,
      Button
    },
    setup() {
      const router = useRouter();
      const authStore = useAuthStore();

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
      const validationErrors = ref([]);

      const isFormValid = computed(() => {
        return form.name.trim().length > 0 &&
          form.name.length <= 200 &&
          form.description.length <= 2000;
      });

      const validateForm = () => {
        // Clear previous errors
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

      const handleSubmit = async () => {
        if (!validateForm()) {
          return;
        }

        loading.value = true;
        message.text = '';
        validationErrors.value = [];

        try {
          const result = await publisherService.createPublisher({
            name: form.name.trim(),
            description: form.description.trim()
          });

          if (result.success) {
            message.text = result.message || 'Publisher created successfully!';
            message.type = 'success';

            // Clear form
            form.name = '';
            form.description = '';

            // Redirect based on user role after short delay
            setTimeout(() => {
              if (authStore.isAdmin) {
                router.push('/admin/publishers');
              } else {
                router.push('/publishers');
              }
            }, 1500);
          } else {
            message.text = result.error;
            message.type = 'error';
            validationErrors.value = result.validationErrors || [];

            // Handle specific field validation errors
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
          console.error('Error creating publisher:', error);
          message.text = 'An unexpected error occurred. Please try again.';
          message.type = 'error';
        } finally {
          loading.value = false;
        }
      };

      const handleCancel = () => {
        // Redirect based on user role
        if (authStore.isAdmin) {
          router.push('/admin/publishers');
        } else {
          router.push('/publishers');
        }
      };

      return {
        form,
        errors,
        message,
        loading,
        validationErrors,
        isFormValid,
        handleSubmit,
        handleCancel
      };
    }
  };
</script>

<style scoped>
  /* Custom styling for form inputs with #141414 background */
  .form-input-bg {
    background-color: #141414;
    border-color: rgba(255, 255, 255, 0.1);
  }

    .form-input-bg:hover {
      background-color: #1a1a1a;
    }

    .form-input-bg:focus {
      background-color: #141414;
      border-color: rgba(255, 255, 255, 0.2);
    }

  .form-textarea-bg {
    background-color: #141414;
    border-color: rgba(255, 255, 255, 0.1);
  }

    .form-textarea-bg:hover {
      background-color: #1a1a1a;
    }

    .form-textarea-bg:focus {
      background-color: #141414;
      border-color: rgba(255, 255, 255, 0.2);
    }
</style>
