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
            <div class="space-y-3">
              <Label for="name" class="text-sm font-medium text-foreground">
                Team Name <span class="text-red-500">*</span>
              </Label>
              <Input id="name"
                     v-model="form.name"
                     type="text"
                     class="form-input-bg"
                     :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.name }"
                     placeholder="Enter team name"
                     maxlength="100"
                     required />
              <div v-if="errors.name" class="text-sm text-red-600">{{ errors.name }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-50 text-right">{{ form.name.length }}/100</div>
            </div>

            <!-- Description -->
            <div class="space-y-3">
              <Label for="description" class="text-sm font-medium text-foreground">
                Description <span class="text-red-500">*</span>
              </Label>
              <Textarea id="description"
                        v-model="form.description"
                        class="form-textarea-bg resize-vertical"
                        :class="{ 'border-red-500 focus:ring-red-500 focus:border-red-500': errors.description }"
                        placeholder="Describe your team's goals, preferred genres, or any other relevant information"
                        maxlength="500"
                        rows="4"
                        required />
              <div v-if="errors.description" class="text-sm text-red-600">{{ errors.description }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-50 text-right">{{ form.description.length }}/500</div>
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
                {{ loading ? 'Creating...' : 'Create Team' }}
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
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import { useRouter } from 'vue-router';
  import { teamService } from '../../services/teamService';
  import { useForm } from '@/composables/useForm';
  import { Input } from '@/components/ui/input';
  import { Label } from '@/components/ui/label';
  import { Textarea } from '@/components/ui/textarea';
  import { Button } from '@/components/ui/button';

  export default {
    name: 'AddTeam',
    components: {
      Input,
      Label,
      Textarea,
      Button
    },
    setup() {
      const router = useRouter();

      const { form, errors, message, loading, isValid, handleSubmit } = useForm({
        initialValues: { name: '', description: '' },
        validate: f => {
          const out = {};
          if (!f.name.trim()) out.name = 'Team name is required';
          else if (f.name.length > 100) out.name = 'Team name cannot exceed 100 characters';
          if (!f.description.trim()) out.description = 'Description is required';
          else if (f.description.length > 500) out.description = 'Description cannot exceed 500 characters';
          return out;
        },
        submit: f => teamService.createTeam({
          name: f.name.trim(),
          description: f.description.trim()
        }),
        onSuccess: result => {
          message.text = result.message || 'Team created successfully!';
          setTimeout(() => router.push('/teams'), 1500);
        }
      });

      const handleCancel = () => router.push('/teams');

      return {
        form,
        errors,
        message,
        loading,
        isFormValid: isValid,
        handleSubmit,
        handleCancel
      };
    }
  };
</script>

<style scoped>
  /* Custom styling for form inputs — uses --color-input-bg */
  .form-input-bg {
    background-color: var(--color-input-bg);
    border-color: rgba(255, 255, 255, 0.1);
  }

    .form-input-bg:hover {
      background-color: var(--color-input-bg-hover);
    }

    .form-input-bg:focus {
      background-color: var(--color-input-bg);
      border-color: rgba(255, 255, 255, 0.2);
    }

  .form-textarea-bg {
    background-color: var(--color-input-bg);
    border-color: rgba(255, 255, 255, 0.1);
  }

    .form-textarea-bg:hover {
      background-color: var(--color-input-bg-hover);
    }

    .form-textarea-bg:focus {
      background-color: var(--color-input-bg);
      border-color: rgba(255, 255, 255, 0.2);
    }
</style>
