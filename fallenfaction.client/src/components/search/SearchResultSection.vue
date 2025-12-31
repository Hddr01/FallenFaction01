<template>
  <div class="space-y-2">
    <!-- Section Header -->
    <div class="flex items-center gap-2 text-xs font-medium text-muted-foreground px-2">
      <component :is="icon" class="h-3.5 w-3.5" />
      <span>{{ title }}</span>
      <span class="ml-auto">{{ results.length }}</span>
    </div>

    <!-- Results List -->
    <div class="space-y-1">
      <button v-for="(result, index) in displayedResults"
              :key="result.id || index"
              @click="$emit('select', result)"
              class="w-full flex items-center gap-3 p-3 rounded-lg hover:bg-secondary/50 transition-colors text-left">
        <slot name="result" :result="result">
          <!-- Default slot content -->
          <div class="flex-1">
            <p class="font-medium">{{ result.name || result.title }}</p>
          </div>
        </slot>
      </button>

      <!-- Show More Button -->
      <Button v-if="results.length > maxDisplay"
              variant="ghost"
              size="sm"
              @click="toggleShowAll"
              class="w-full text-xs text-muted-foreground">
        {{ showAll ? 'Show Less' : `Show ${results.length - maxDisplay} More` }}
      </Button>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed } from 'vue';
  import { Button } from '@/components/ui/button';

  const props = defineProps({
    title: {
      type: String,
      required: true
    },
    icon: {
      type: Object,
      required: true
    },
    results: {
      type: Array,
      required: true
    },
    maxDisplay: {
      type: Number,
      default: 5
    }
  });

  defineEmits(['select']);

  const showAll = ref(false);

  const displayedResults = computed(() => {
    if (showAll.value) {
      return props.results;
    }
    return props.results.slice(0, props.maxDisplay);
  });

  const toggleShowAll = () => {
    showAll.value = !showAll.value;
  };
</script>
