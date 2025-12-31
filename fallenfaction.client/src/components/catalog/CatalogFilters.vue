<template>
  <div class="space-y-6">
    <!-- Type Filter -->
    <div class="space-y-3">
      <Label class="text-sm font-medium">Type</Label>
      <Select v-model="localFilters.type" @update:modelValue="emitUpdate">
        <SelectTrigger>
          <SelectValue placeholder="All Types" />
        </SelectTrigger>
        <SelectContent class="bg-background" style="background-color: hsl(20 14.3% 4.1%) !important;">
          <SelectItem :value="null">All Types</SelectItem>
          <SelectItem value="1">Manga</SelectItem>
          <SelectItem value="2">Manhwa</SelectItem>
          <SelectItem value="3">Manhua</SelectItem>
          <SelectItem value="4">Western Comic</SelectItem>
          <SelectItem value="5">Russian Comic</SelectItem>
          <SelectItem value="6">Indonesian Comic</SelectItem>
        </SelectContent>
      </Select>
    </div>

    <Separator />

    <!-- Status Filters -->
    <div class="space-y-3">
      <Label class="text-sm font-medium">Publication Status</Label>
      <RadioGroup v-model="localFilters.status" @update:modelValue="emitUpdate">
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="all" id="status-all" />
          <Label for="status-all" class="font-normal cursor-pointer">All</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="inproces" id="status-ongoing" />
          <Label for="status-ongoing" class="font-normal cursor-pointer">Ongoing</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="completed" id="status-completed" />
          <Label for="status-completed" class="font-normal cursor-pointer">Completed</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="frozen" id="status-hiatus" />
          <Label for="status-hiatus" class="font-normal cursor-pointer">On Hiatus</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="abandoned" id="status-dropped" />
          <Label for="status-dropped" class="font-normal cursor-pointer">Dropped</Label>
        </div>
      </RadioGroup>
    </div>

    <Separator />

    <!-- Translation Status -->
    <div class="space-y-3">
      <Label class="text-sm font-medium">Translation Status</Label>
      <RadioGroup v-model="localFilters.translationStatus" @update:modelValue="emitUpdate">
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="all" id="trans-all" />
          <Label for="trans-all" class="font-normal cursor-pointer">All</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="inproces" id="trans-ongoing" />
          <Label for="trans-ongoing" class="font-normal cursor-pointer">Ongoing</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="completed" id="trans-completed" />
          <Label for="trans-completed" class="font-normal cursor-pointer">Completed</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="frozen" id="trans-hiatus" />
          <Label for="trans-hiatus" class="font-normal cursor-pointer">On Hiatus</Label>
        </div>
        <div class="flex items-center space-x-2">
          <RadioGroupItem value="abandoned" id="trans-dropped" />
          <Label for="trans-dropped" class="font-normal cursor-pointer">Dropped</Label>
        </div>
      </RadioGroup>
    </div>

    <Separator />

    <!-- Age Rating -->
    <div class="space-y-3">
      <Label class="text-sm font-medium">Age Rating</Label>
      <Select v-model="localFilters.ageRestriction" @update:modelValue="emitUpdate">
        <SelectTrigger>
          <SelectValue placeholder="All Ages" />
        </SelectTrigger>
        <SelectContent class="bg-background" style="background-color: hsl(20 14.3% 4.1%) !important;">
          <SelectItem :value="null">All Ages</SelectItem>
          <SelectItem value="0">All Ages</SelectItem>
          <SelectItem value="12">12+</SelectItem>
          <SelectItem value="16">16+</SelectItem>
          <SelectItem value="18">18+</SelectItem>
        </SelectContent>
      </Select>
    </div>

    <Separator />

    <!-- Categories -->
    <div class="space-y-3">
      <div class="flex items-center justify-between">
        <Label class="text-sm font-medium">Categories</Label>
        <Button v-if="localFilters.categories.length > 0"
                variant="ghost"
                size="sm"
                @click="clearCategories">
          Clear
        </Button>
      </div>

      <Popover v-model:open="categoriesOpen">
        <PopoverTrigger as-child>
          <Button variant="secondary" class="w-full justify-between">
            <span v-if="localFilters.categories.length === 0" class="text-muted-foreground">
              Select categories
            </span>
            <span v-else class="truncate">
              {{ selectedCategoriesText }}
            </span>
            <ChevronDown class="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent class="w-80 p-0" align="start" style="background-color: hsl(20 14.3% 4.1%) !important;">
          <Command style="background-color: hsl(20 14.3% 4.1%) !important;">
          <CommandInput placeholder="Search categories..." style="background-color: hsl(20 14.3% 4.1%) !important;" />
          <CommandEmpty>No category found.</CommandEmpty>
          <CommandList style="background-color: hsl(20 14.3% 4.1%) !important;">
            <CommandGroup>
              <CommandItem v-for="category in filterOptions.Categories"
                           :key="category.id"
                           :value="category.name"
                           @select="toggleCategory(category.id)">
                <Check :class="[
                      'mr-2 h-4 w-4',
                      localFilters.categories.includes(category.id) ? 'opacity-100' : 'opacity-0'
                    ]" />
                {{ category.name }}
              </CommandItem>
            </CommandGroup>
          </CommandList>
          </Command>
        </PopoverContent>
      </Popover>

      <!-- Selected Categories -->
      <div v-if="localFilters.categories.length > 0" class="flex flex-wrap gap-1">
        <Badge v-for="id in localFilters.categories"
               :key="id"
               variant="secondary"
               class="gap-1 pl-2 pr-1">
          {{ getCategoryName(id) }}
          <Button variant="ghost"
                  size="icon"
                  class="h-3 w-3 p-0 hover:bg-transparent"
                  @click="removeCategory(id)">
            <X class="h-2.5 w-2.5" />
          </Button>
        </Badge>
      </div>
    </div>

    <Separator />

    <!-- Tags -->
    <div class="space-y-3">
      <div class="flex items-center justify-between">
        <Label class="text-sm font-medium">Tags</Label>
        <Button v-if="localFilters.tags.length > 0"
                variant="ghost"
                size="sm"
                @click="clearTags">
          Clear
        </Button>
      </div>

      <Popover v-model:open="tagsOpen">
        <PopoverTrigger as-child>
          <Button variant="secondary" class="w-full justify-between">
            <span v-if="localFilters.tags.length === 0" class="text-muted-foreground">
              Select tags
            </span>
            <span v-else class="truncate">
              {{ selectedTagsText }}
            </span>
            <ChevronDown class="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent class="w-80 p-0" align="start" style="background-color: hsl(20 14.3% 4.1%) !important;">
          <Command style="background-color: hsl(20 14.3% 4.1%) !important;">
          <CommandInput placeholder="Search tags..." style="background-color: hsl(20 14.3% 4.1%) !important;" />
          <CommandEmpty>No tag found.</CommandEmpty>
          <CommandList style="background-color: hsl(20 14.3% 4.1%) !important;">
            <CommandGroup>
              <CommandItem v-for="tag in filterOptions.Tags"
                           :key="tag.id"
                           :value="tag.name"
                           @select="toggleTag(tag.id)">
                <Check :class="[
                      'mr-2 h-4 w-4',
                      localFilters.tags.includes(tag.id) ? 'opacity-100' : 'opacity-0'
                    ]" />
                {{ tag.name }}
              </CommandItem>
            </CommandGroup>
          </CommandList>
          </Command>
        </PopoverContent>
      </Popover>

      <!-- Selected Tags -->
      <div v-if="localFilters.tags.length > 0" class="flex flex-wrap gap-1">
        <Badge v-for="id in localFilters.tags"
               :key="id"
               variant="secondary"
               class="gap-1 pl-2 pr-1">
          {{ getTagName(id) }}
          <Button variant="ghost"
                  size="icon"
                  class="h-3 w-3 p-0 hover:bg-transparent"
                  @click="removeTag(id)">
            <X class="h-2.5 w-2.5" />
          </Button>
        </Badge>
      </div>
    </div>

    <Separator />

    <!-- Formats -->
    <div class="space-y-3">
      <Label class="text-sm font-medium">Format</Label>
      <div class="space-y-2">
        <div v-for="format in filterOptions.Formats"
             :key="format.id"
             class="flex items-center space-x-2">
          <Checkbox :id="`format-${format.id}`"
                    :checked="localFilters.formats.includes(format.id)"
                    @update:checked="toggleFormat(format.id)" />
          <Label :for="`format-${format.id}`"
                 class="font-normal cursor-pointer">
            {{ format.name }}
          </Label>
        </div>
      </div>
    </div>

    <Separator />

    <!-- Year Range -->
    <div class="space-y-3">
      <Label class="text-sm font-medium">Release Year</Label>
      <div class="grid grid-cols-2 gap-2">
        <div class="space-y-1">
          <Label for="year-from" class="text-xs text-muted-foreground">From</Label>
          <Input id="year-from"
                 v-model.number="localFilters.yearFrom"
                 type="number"
                 placeholder="2000"
                 min="1900"
                 :max="currentYear"
                 @input="emitUpdate" />
        </div>
        <div class="space-y-1">
          <Label for="year-to" class="text-xs text-muted-foreground">To</Label>
          <Input id="year-to"
                 v-model.number="localFilters.yearTo"
                 type="number"
                 placeholder="2024"
                 min="1900"
                 :max="currentYear"
                 @input="emitUpdate" />
        </div>
      </div>
    </div>

    <!-- Reset Button -->
    <Button variant="secondary" class="w-full" @click="$emit('reset')">
      <RotateCcw class="mr-2 h-4 w-4" />
      Reset All Filters
    </Button>
  </div>
</template>

<script setup>
  import { ref, computed, watch } from 'vue';
  import { Check, ChevronDown, X, RotateCcw } from 'lucide-vue-next';

  import { Label } from '@/components/ui/label';
  import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
  import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
  import { Checkbox } from '@/components/ui/checkbox';
  import { Button } from '@/components/ui/button';
  import { Input } from '@/components/ui/input';
  import { Separator } from '@/components/ui/separator';
  import { Badge } from '@/components/ui/badge';
  import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
  import {
    Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList
  } from '@/components/ui/command';

  const props = defineProps({
    modelValue: {
      type: Object,
      required: true
    },
    filterOptions: {
      type: Object,
      required: true
    }
  });

  const emit = defineEmits(['update:modelValue', 'reset']);

  const localFilters = ref({ ...props.modelValue });
  const categoriesOpen = ref(false);
  const tagsOpen = ref(false);

  const currentYear = new Date().getFullYear();

  // Computed
  const selectedCategoriesText = computed(() => {
    const selected = localFilters.value.categories
      .map(id => getCategoryName(id))
      .slice(0, 2);
    const extra = localFilters.value.categories.length - 2;
    return extra > 0 ? `${selected.join(', ')} +${extra}` : selected.join(', ');
  });

  const selectedTagsText = computed(() => {
    const selected = localFilters.value.tags
      .map(id => getTagName(id))
      .slice(0, 2);
    const extra = localFilters.value.tags.length - 2;
    return extra > 0 ? `${selected.join(', ')} +${extra}` : selected.join(', ');
  });

  // Methods
  function getCategoryName(id) {
    return props.filterOptions.Categories.find(c => c.id === id)?.name || id;
  }

  function getTagName(id) {
    return props.filterOptions.Tags.find(t => t.id === id)?.name || id;
  }

  function toggleCategory(id) {
    const index = localFilters.value.categories.indexOf(id);
    if (index > -1) {
      localFilters.value.categories.splice(index, 1);
    } else {
      localFilters.value.categories.push(id);
    }
    emitUpdate();
  }

  function removeCategory(id) {
    localFilters.value.categories = localFilters.value.categories.filter(c => c !== id);
    emitUpdate();
  }

  function clearCategories() {
    localFilters.value.categories = [];
    emitUpdate();
  }

  function toggleTag(id) {
    const index = localFilters.value.tags.indexOf(id);
    if (index > -1) {
      localFilters.value.tags.splice(index, 1);
    } else {
      localFilters.value.tags.push(id);
    }
    emitUpdate();
  }

  function removeTag(id) {
    localFilters.value.tags = localFilters.value.tags.filter(t => t !== id);
    emitUpdate();
  }

  function clearTags() {
    localFilters.value.tags = [];
    emitUpdate();
  }

  function toggleFormat(id) {
    const index = localFilters.value.formats.indexOf(id);
    if (index > -1) {
      localFilters.value.formats.splice(index, 1);
    } else {
      localFilters.value.formats.push(id);
    }
    emitUpdate();
  }

  function emitUpdate() {
    // Clean up null/undefined values
    const cleanFilters = { ...localFilters.value };
    if (cleanFilters.status === 'all') cleanFilters.status = null;
    if (cleanFilters.translationStatus === 'all') cleanFilters.translationStatus = null;

    emit('update:modelValue', cleanFilters);
  }

  // Watch for external changes
  watch(() => props.modelValue, (newValue) => {
    localFilters.value = { ...newValue };
  }, { deep: true });
</script>
