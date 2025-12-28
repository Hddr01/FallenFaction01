<script setup>
  import { Loader } from 'lucide-vue-next';
  import { cn } from '@/lib/utils'

  const props = defineProps({
    class: {
      type: String,
      default: ''
    }
  })
</script>

<template>
  <Loader  role="status"
               aria-label="Loading"
               :class="cn('size-4 animate-spin', props.class)" />
</template>
