<template>
  <div class="space-y-3">
    <div class="flex items-center justify-between">
      <label class="text-sm font-medium text-foreground">{{ label }}</label>
      <a v-if="createNewUrl"
         :href="createNewUrl"
         target="_blank"
         rel="noopener noreferrer"
         class="inline-flex items-center gap-1.5 text-sm font-medium text-primary hover:text-primary/80 transition-colors">
        <Plus class="size-3.5" />
        {{ createNewText }}
      </a>
    </div>

    <Popover v-model:open="isOpen">
      <PopoverTrigger as-child>
        <Button variant="outline"
                role="combobox"
                :aria-expanded="isOpen"
                class="w-full justify-between h-auto min-h-10 px-3 py-2 select-trigger-bg hover:brightness-110 border border-white/10">
          <span class="block truncate text-left font-normal">
            {{ displayText }}
          </span>
          <ChevronDown class="ml-2 size-4 shrink-0 opacity-50 transition-transform duration-200"
                       :class="{ 'rotate-180': isOpen }" />
        </Button>
      </PopoverTrigger>
      <PopoverContent class="w-[var(--radix-popover-trigger-width)] p-0 popover-bg border border-white/10" align="start">
        <Command class="bg-[#141414]">
        <CommandInput v-if="searchable"
                      v-model:search-term="searchQuery"
                      placeholder="Search..."
                      class="h-9 bg-transparent" />
        <CommandEmpty>No results found</CommandEmpty>
        <CommandList class="max-h-60 bg-[#141414]">
          <CommandGroup>
            <CommandItem v-for="option in filteredOptions"
                         :key="option.id"
                         :value="option.name"
                         @select="toggleOption(option.id)"
                         class="cursor-pointer">
              <div class="flex items-center gap-2 flex-1">
                <Checkbox :checked="modelValue.includes(option.id)"
                          class="pointer-events-none" />
                <span class="flex-1">{{ option.name }}</span>
              </div>
              <Check v-if="modelValue.includes(option.id)"
                     class="ml-auto size-4 text-primary" />
            </CommandItem>
          </CommandGroup>
        </CommandList>
        </Command>
      </PopoverContent>
    </Popover>

    <!-- Selected items display -->
    <TransitionGroup v-if="showSelectedItems && modelValue.length > 0"
                     name="badge"
                     tag="div"
                     class="flex flex-wrap gap-2">
      <Badge v-for="selectedId in modelValue"
             :key="selectedId"
             variant="secondary"
             class="gap-1.5 pr-1">
        <span class="text-xs">{{ getOptionName(selectedId) }}</span>
        <button type="button"
                @click.stop="removeOption(selectedId)"
                class="ml-0.5 rounded-sm opacity-70 hover:opacity-100 hover:bg-muted transition-all">
          <X class="size-3" />
          <span class="sr-only">Remove</span>
        </button>
      </Badge>
    </TransitionGroup>
  </div>
</template>

<script setup>
  import { ref, computed, watch } from 'vue'
  import { Popover, PopoverTrigger, PopoverContent } from '@/components/ui/popover'
  import { Command, CommandInput, CommandEmpty, CommandGroup, CommandItem, CommandList } from '@/components/ui/command'
  import { Checkbox } from '@/components/ui/checkbox'
  import { Badge } from '@/components/ui/badge'
  import { Button } from '@/components/ui/button'
  import { Plus, ChevronDown, Check, X } from 'lucide-vue-next'

  const props = defineProps({
    options: {
      type: Array,
      required: true,
      default: () => []
    },
    modelValue: {
      type: Array,
      default: () => []
    },
    placeholder: {
      type: String,
      default: 'Select options'
    },
    label: {
      type: String,
      default: ''
    },
    createNewUrl: {
      type: String,
      default: ''
    },
    createNewText: {
      type: String,
      default: 'Create new'
    },
    searchable: {
      type: Boolean,
      default: true
    },
    showSelectedItems: {
      type: Boolean,
      default: false
    },
    maxDisplayItems: {
      type: Number,
      default: 3
    }
  })

  const emit = defineEmits(['update:modelValue'])

  const isOpen = ref(false)
  const searchQuery = ref('')

  // Computed properties
  const displayText = computed(() => {
    if (props.modelValue.length === 0) {
      return props.placeholder
    }

    const selectedNames = props.options
      .filter(option => props.modelValue.includes(option.id))
      .map(option => option.name)

    if (selectedNames.length <= props.maxDisplayItems) {
      return selectedNames.join(', ')
    } else {
      return `${selectedNames.slice(0, props.maxDisplayItems).join(', ')} and ${selectedNames.length - props.maxDisplayItems} more`
    }
  })

  const filteredOptions = computed(() => {
    if (!props.searchable || !searchQuery.value.trim()) {
      return props.options
    }

    const query = searchQuery.value.toLowerCase().trim()
    return props.options.filter(option =>
      option.name.toLowerCase().includes(query)
    )
  })

  // Methods
  const toggleOption = (optionId) => {
    const newValue = props.modelValue.includes(optionId)
      ? props.modelValue.filter(id => id !== optionId)
      : [...props.modelValue, optionId]

    emit('update:modelValue', newValue)
  }

  const removeOption = (optionId) => {
    const newValue = props.modelValue.filter(id => id !== optionId)
    emit('update:modelValue', newValue)
  }

  const getOptionName = (optionId) => {
    const option = props.options.find(opt => opt.id === optionId)
    return option ? option.name : ''
  }

  // Clear search when dropdown closes
  watch(isOpen, (newValue) => {
    if (!newValue) {
      searchQuery.value = ''
    }
  })
</script>

<style scoped>
  .badge-enter-active,
  .badge-leave-active {
    transition: all 0.2s ease;
  }

  .badge-enter-from,
  .badge-leave-to {
    opacity: 0;
    transform: scale(0.8);
  }

  .badge-move {
    transition: transform 0.2s ease;
  }
  /* Match navbar background styling with visible dark gray */
  .select-trigger-bg {
    background-color: #141414;
    border-color: rgba(255, 255, 255, 0.1);
  }

    .select-trigger-bg:hover {
      background-color: #1a1a1a;
    }

  .popover-bg {
    background-color: #141414;
    backdrop-filter: blur(20px) brightness(1.05);
    -webkit-backdrop-filter: blur(20px) brightness(1.05);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
    border-color: rgba(255, 255, 255, 0.1);
  }
</style>
