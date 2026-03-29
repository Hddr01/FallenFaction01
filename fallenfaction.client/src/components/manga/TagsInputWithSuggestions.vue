<template>
  <div class="space-y-3">
    <label v-if="label" class="block text-sm font-medium text-foreground">
      {{ label }}
    </label>

    <Popover v-model:open="open">
      <ListboxRoot v-model="selectedTags"
                   highlight-on-hover
                   multiple>
        <PopoverAnchor class="inline-flex w-full">
          <TagsInput v-slot="{ modelValue: tags }" v-model="selectedTags" class="w-full tags-input-bg border border-white/10">
            <TagsInputItem v-for="item in tags" :key="item.toString()" :value="item.toString()">
              <TagsInputItemText />
              <TagsInputItemDelete />
            </TagsInputItem>
            <ListboxFilter v-model="searchTerm" as-child>
              <TagsInputInput :placeholder="placeholder"
                              @keydown.enter.prevent
                              @keydown.down="open = true"
                              class="bg-transparent" />
            </ListboxFilter>
            <PopoverTrigger as-child>
              <Button size="icon-sm" variant="ghost" class="order-last self-start ml-auto">
                <ChevronDown class="size-3.5" />
              </Button>
            </PopoverTrigger>
          </TagsInput>
        </PopoverAnchor>
        <PopoverContent class="p-1 w-[var(--radix-popover-trigger-width)] popover-bg border border-white/10"
                        @open-auto-focus.prevent
                        align="start">
          <ListboxContent class="max-h-[200px] scroll-py-1 overflow-x-hidden overflow-y-auto empty:after:content-['No_options'] empty:p-1 empty:after:block bg-[var(--color-input-bg)]"
                          tabindex="0">
            <ListboxItem v-for="item in filteredOptions"
                         :key="item.id"
                         class="data-[highlighted]:bg-accent data-[highlighted]:text-accent-foreground relative flex cursor-default items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-hidden select-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4"
                         :value="item.name"
                         @select="() => { searchTerm = '' }">
              <span>{{ item.name }}</span>
              <ListboxItemIndicator class="ml-auto inline-flex items-center justify-center">
                <Check class="size-4" />
              </ListboxItemIndicator>
            </ListboxItem>
          </ListboxContent>
        </PopoverContent>
      </ListboxRoot>
    </Popover>
  </div>
</template>

<script setup>
  import { ref, computed, watch } from 'vue'
  import { ListboxContent, ListboxFilter, ListboxItem, ListboxItemIndicator, ListboxRoot, useFilter } from 'reka-ui'
  import { Button } from '@/components/ui/button'
  import { Popover, PopoverAnchor, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
  import { TagsInput, TagsInputInput, TagsInputItem, TagsInputItemDelete, TagsInputItemText } from '@/components/ui/tags-input'
  import { ChevronDown, Check } from 'lucide-vue-next'

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
      default: 'Add tags...'
    },
    label: {
      type: String,
      default: ''
    }
  })

  const emit = defineEmits(['update:modelValue'])

  const searchTerm = ref('')
  const open = ref(false)

  // Convert between ID array (what the form uses) and name array (what TagsInput uses)
  const selectedTags = computed({
    get() {
      return props.options
        .filter(option => props.modelValue.includes(option.id))
        .map(option => option.name)
    },
    set(newTags) {
      // Convert tag names to IDs
      const tagIds = newTags.map(tagName => {
        const existingOption = props.options.find(opt => opt.name === tagName)
        return existingOption ? existingOption.id : tagName
      })
      emit('update:modelValue', tagIds)
    }
  })

  const { contains } = useFilter({ sensitivity: 'base' })

  const filteredOptions = computed(() =>
    searchTerm.value === ''
      ? props.options
      : props.options.filter(option => contains(option.name, searchTerm.value))
  )

  watch(searchTerm, (value) => {
    if (value) {
      open.value = true
    }
  })
</script>

<style scoped>
  /* Match navbar background styling with visible dark gray */
  .tags-input-bg {
    background-color: var(--color-input-bg);
    border-color: rgba(255, 255, 255, 0.1);
  }

  .popover-bg {
    background-color: var(--color-input-bg);
    backdrop-filter: blur(20px) brightness(1.05);
    -webkit-backdrop-filter: blur(20px) brightness(1.05);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
    border-color: rgba(255, 255, 255, 0.1);
  }
</style>
