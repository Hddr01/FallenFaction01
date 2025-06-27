<template>
  <div>
    <div class="flex justify-between items-center mb-2">
      <label class="block text-sm font-medium text-[var(--color-text)]">{{ label }}</label>
      <a v-if="createNewUrl"
         :href="createNewUrl"
         target="_blank"
         rel="noopener noreferrer"
         class="text-green-600 hover:text-green-700 text-sm font-medium transition-colors duration-200">
        {{ createNewText }}
      </a>
    </div>

    <div class="relative">
      <button type="button"
              @click="toggleDropdown"
              @blur="handleBlur"
              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-[var(--color-text)] text-left focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200">
        <span class="block truncate">
          {{ displayText }}
        </span>
        <span class="absolute inset-y-0 right-0 flex items-center pr-2">
          <svg class="h-5 w-5 text-[var(--color-text)] opacity-50 transition-transform duration-200"
               :class="{ 'rotate-180': isOpen }"
               viewBox="0 0 20 20"
               fill="currentColor">
            <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </span>
      </button>

      <Transition enter-active-class="transition duration-200 ease-out"
                  enter-from-class="opacity-0 scale-95"
                  enter-to-class="opacity-100 scale-100"
                  leave-active-class="transition duration-150 ease-in"
                  leave-from-class="opacity-100 scale-100"
                  leave-to-class="opacity-0 scale-95">
        <div v-if="isOpen"
             class="absolute z-10 mt-1 w-full bg-[var(--color-background-soft)] shadow-lg max-h-60 rounded-md py-1 text-base ring-1 ring-[var(--color-border)] overflow-auto focus:outline-none border border-[var(--color-border)]">
          <!-- Search input -->
          <div v-if="searchable" class="p-2 border-b border-[var(--color-border)]">
            <input ref="searchInput"
                   v-model="searchQuery"
                   type="text"
                   placeholder="Search..."
                   class="w-full px-3 py-1 border border-[var(--color-border)] rounded-md text-sm bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-1 focus:ring-green-500 focus:border-green-500 hover:border-[var(--color-border-hover)] transition-colors duration-200 placeholder:text-[var(--color-text)] placeholder:opacity-50"
                   @click.stop />
          </div>

          <!-- No results message -->
          <div v-if="filteredOptions.length === 0"
               class="px-3 py-2 text-[var(--color-text)] opacity-75 text-sm">
            No results found
          </div>

          <!-- Options list -->
          <div v-for="option in filteredOptions"
               :key="option.id"
               @click="toggleOption(option.id)"
               class="cursor-pointer select-none relative py-2 pl-10 pr-4 hover:bg-[var(--color-background-mute)] transition-colors duration-150"
               :class="{
              'bg-green-50 text-green-900': modelValue.includes(option.id),
              'text-[var(--color-text)]': !modelValue.includes(option.id)
            }">
            <!-- Checkbox indicator -->
            <span class="absolute inset-y-0 left-0 flex items-center pl-3">
              <div class="flex items-center justify-center w-4 h-4 border-2 rounded transition-colors duration-200"
                   :class="{
                    'border-green-500 bg-green-500': modelValue.includes(option.id),
                    'border-[var(--color-border)] bg-[var(--color-background)]': !modelValue.includes(option.id)
                   }">
                <svg v-if="modelValue.includes(option.id)"
                     class="w-3 h-3 text-white"
                     viewBox="0 0 20 20"
                     fill="currentColor">
                  <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
                </svg>
              </div>
            </span>
            <span class="block truncate">{{ option.name }}</span>
          </div>
        </div>
      </Transition>
    </div>

    <!-- Selected items display -->
    <div v-if="showSelectedItems && modelValue.length > 0" class="mt-2">
      <div class="flex flex-wrap gap-2">
        <span v-for="selectedId in modelValue"
              :key="selectedId"
              class="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-green-100 text-green-800 border border-green-200">
          {{ getOptionName(selectedId) }}
          <button type="button"
                  @click="removeOption(selectedId)"
                  class="ml-1 inline-flex items-center justify-center w-4 h-4 rounded-full hover:bg-green-200 focus:outline-none transition-colors duration-200">
            <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </span>
      </div>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, nextTick, watch } from 'vue'

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
  const searchInput = ref(null)

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
  const toggleDropdown = async () => {
    isOpen.value = !isOpen.value

    if (isOpen.value && props.searchable) {
      await nextTick()
      searchInput.value?.focus()
    }
  }

  const handleBlur = (event) => {
    // Don't close if clicking inside the dropdown
    if (event.relatedTarget && event.relatedTarget.closest('.absolute')) {
      return
    }

    setTimeout(() => {
      isOpen.value = false
      searchQuery.value = ''
    }, 150)
  }

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

  // Close dropdown when clicking outside
  const handleClickOutside = (event) => {
    if (!event.target.closest('.relative')) {
      isOpen.value = false
      searchQuery.value = ''
    }
  }

  // Keyboard navigation
  const handleKeydown = (event) => {
    if (!isOpen.value) return

    switch (event.key) {
      case 'Escape':
        isOpen.value = false
        searchQuery.value = ''
        break
      case 'Enter':
        event.preventDefault()
        if (filteredOptions.value.length > 0) {
          toggleOption(filteredOptions.value[0].id)
        }
        break
    }
  }

  // Watch for dropdown state changes
  watch(isOpen, (newValue) => {
    if (newValue) {
      document.addEventListener('click', handleClickOutside)
      document.addEventListener('keydown', handleKeydown)
    } else {
      document.removeEventListener('click', handleClickOutside)
      document.removeEventListener('keydown', handleKeydown)
    }
  })
</script>

<style scoped>
  /* Custom focus ring offset color */
  .focus\:ring-offset-2:focus {
    --tw-ring-offset-width: 2px;
    --tw-ring-offset-color: var(--color-background);
  }
</style>
