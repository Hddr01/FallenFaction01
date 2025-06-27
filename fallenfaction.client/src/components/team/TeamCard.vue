<!-- components/team/TeamCard.vue -->
<template>
  <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-lg shadow-sm hover:shadow-md hover:border-[var(--color-border-hover)] transition-all duration-200 overflow-hidden">
    <!-- Card Header -->
    <div class="p-6 pb-4">
      <div class="flex items-start justify-between">
        <div class="flex-1 min-w-0">
          <h3 class="text-lg font-semibold text-[var(--color-heading)] truncate">{{ team.name }}</h3>
          <div class="mt-2 flex items-center space-x-4 text-sm text-[var(--color-text)] opacity-75">
            <div class="flex items-center">
              <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-1.657-.126-3.153-.356-4.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-1.657.126-3.153.356-4.857m0 0a5.002 5.002 0 019.288 0" />
              </svg>
              <span>{{ team.memberCount }} member {{ team.memberCount !== 1 ? 's' : '' }}</span>
            </div>
            <div class="flex items-center">
              <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.746 0 3.332.477 4.5 1.253v13C19.832 18.477 18.246 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
              </svg>
              <span>{{ team.titleCount }} title{{ team.titleCount !== 1 ? 's' : '' }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Card Body -->
    <div class="px-6 pb-4">
      <p class="text-[var(--color-text)] text-sm line-clamp-3">{{ truncatedDescription }}</p>

      <div class="mt-4 flex items-center justify-between text-xs text-[var(--color-text)] opacity-60">
        <div class="flex items-center">
          <span class="font-medium">Created by:</span>
          <span class="ml-1 text-[var(--vt-c-indigo)]">{{ team.creatorName }}</span>
        </div>
        <span>{{ formatDate(team.createdDate) }}</span>
      </div>
    </div>

    <!-- Card Actions -->
    <div class="px-6 py-4 bg-[var(--color-background-mute)] border-t border-[var(--color-border)] flex items-center justify-between">
      <button @click="$emit('view', team.id)"
              class="text-sm font-medium text-green-600 hover:text-green-700 transition-all duration-200">
        View Details
      </button>

      <div class="flex items-center space-x-2">
        <!-- Join/Leave/Member Status -->
        <button v-if="!isMember"
                @click="$emit('join', team.id)"
                :disabled="joining"
                class="inline-flex items-center px-3 py-1.5 border border-transparent text-sm font-medium rounded-md text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
          <svg v-if="joining" class="animate-spin -ml-1 mr-1 h-3 w-3 text-white" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          {{ joining ? 'Joining...' : 'Join' }}
        </button>

        <button v-else-if="canLeave"
                @click="$emit('leave', team.id)"
                :disabled="leaving"
                class="inline-flex items-center px-3 py-1.5 border border-red-300 text-sm font-medium rounded-md text-red-700 bg-white hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200">
          <svg v-if="leaving" class="animate-spin -ml-1 mr-1 h-3 w-3 text-red-700" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          {{ leaving ? 'Leaving...' : 'Leave' }}
        </button>

        <span v-else class="inline-flex items-center px-3 py-1.5 rounded-md text-sm font-medium text-green-800 bg-green-100">
          <svg class="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
          </svg>
          Member
        </span>
      </div>
    </div>
  </div>
</template>

<script>
  import { ref, computed } from 'vue';
  import { useAuthStore } from '../../stores/authStore';

  export default {
    name: 'TeamCard',
    props: {
      team: {
        type: Object,
        required: true
      }
    },
    emits: ['join', 'leave', 'view'],
    setup(props) {
      const authStore = useAuthStore();
      const joining = ref(false);
      const leaving = ref(false);

      const truncatedDescription = computed(() => {
        if (!props.team.description) return '';
        if (props.team.description.length <= 120) {
          return props.team.description;
        }
        return props.team.description.substring(0, 120) + '...';
      });

      // Note: This would need to be determined based on user's team membership
      // For now, returning false - this should be populated from API or store
      const isMember = computed(() => {
        // This should check if current user is in team.members or through a separate API call
        return false;
      });

      const canLeave = computed(() => {
        // Creator cannot leave their own team
        return isMember.value && props.team.creatorId !== authStore.user?.id;
      });

      const formatDate = (dateString) => {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
          year: 'numeric',
          month: 'short',
          day: 'numeric'
        });
      };

      return {
        joining,
        leaving,
        truncatedDescription,
        isMember,
        canLeave,
        formatDate
      };
    }
  };
</script>

<style scoped>
  /* Line clamp utility for description */
  .line-clamp-3 {
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  /* Custom focus ring offset color */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }
</style>
