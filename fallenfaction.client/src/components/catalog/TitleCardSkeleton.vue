<template>
  <component
    :is="viewMode === 'grid' ? 'div' : Card"
    :class="[
      'animate-pulse',
      viewMode === 'list' ? 'p-4' : ''
    ]"
  >
    <!-- Grid View Skeleton -->
    <div v-if="viewMode === 'grid'" class="space-y-3">
      <!-- Cover Image Skeleton -->
      <Skeleton class="aspect-[2/3] w-full rounded-lg" />
      
      <!-- Title Skeleton -->
      <div class="space-y-2">
        <Skeleton class="h-4 w-3/4" />
        <Skeleton class="h-4 w-1/2" />
      </div>
      
      <!-- Meta Info Skeleton -->
      <div class="flex items-center gap-2">
        <Skeleton class="h-3 w-20" />
        <Skeleton class="h-3 w-16" />
      </div>
      
      <!-- Badges Skeleton -->
      <div class="flex gap-1">
        <Skeleton class="h-5 w-16 rounded-full" />
        <Skeleton class="h-5 w-20 rounded-full" />
      </div>
    </div>

    <!-- List View Skeleton -->
    <div v-else class="flex gap-4">
      <!-- Cover Image Skeleton -->
      <Skeleton class="w-24 h-36 flex-shrink-0 rounded-lg" />
      
      <!-- Content Skeleton -->
      <div class="flex-1 space-y-3">
        <!-- Title -->
        <div class="space-y-2">
          <Skeleton class="h-5 w-2/3" />
          <Skeleton class="h-4 w-1/2" />
        </div>
        
        <!-- Badges -->
        <div class="flex gap-1">
          <Skeleton class="h-5 w-16 rounded-full" />
          <Skeleton class="h-5 w-20 rounded-full" />
          <Skeleton class="h-5 w-16 rounded-full" />
        </div>
        
        <!-- Description -->
        <div class="space-y-2">
          <Skeleton class="h-3 w-full" />
          <Skeleton class="h-3 w-4/5" />
        </div>
        
        <!-- Meta Info -->
        <div class="flex gap-4">
          <Skeleton class="h-3 w-20" />
          <Skeleton class="h-3 w-24" />
          <Skeleton class="h-3 w-20" />
        </div>
      </div>
    </div>
  </component>
</template>

<script setup>
import { Card } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';

defineProps({
  viewMode: {
    type: String,
    default: 'grid',
    validator: (value) => ['grid', 'list'].includes(value)
  }
});
</script>
