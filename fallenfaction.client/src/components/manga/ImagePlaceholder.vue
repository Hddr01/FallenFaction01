<template>
  <div class="space-y-3">
    <label v-if="label" class="block text-sm font-medium text-foreground">
      {{ label }}
    </label>

    <!-- Image Upload Area -->
    <div class="relative group"
         @dragover.prevent="isDragging = true"
         @dragleave.prevent="isDragging = false"
         @drop.prevent="handleDrop">
      <!-- Preview State -->
      <Transition name="fade-scale" mode="out-in">
        <div v-if="previewUrl" key="preview" class="relative">
          <div class="relative overflow-hidden rounded-lg border-2 border-border bg-muted transition-all duration-300"
               :class="imageClasses">
            <img :src="previewUrl"
                 :alt="label || 'Uploaded image'"
                 class="w-full h-full object-cover" />

            <!-- Overlay on Hover -->
            <div class="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity duration-200 flex items-center justify-center gap-2">
              <Button type="button"
                      size="icon"
                      variant="secondary"
                      @click="triggerFileInput"
                      class="h-9 w-9">
                <RefreshCw class="size-4" />
              </Button>
              <Button type="button"
                      size="icon"
                      variant="destructive"
                      @click="removeImage"
                      class="h-9 w-9">
                <Trash2 class="size-4" />
              </Button>
            </div>
          </div>
        </div>

        <!-- Upload State -->
        <div v-else key="upload" class="relative">
          <input ref="fileInput"
                 type="file"
                 accept="image/*"
                 @change="handleFileChange"
                 class="hidden" />

          <button type="button"
                  @click="triggerFileInput"
                  :class="[
              'relative w-full rounded-lg border-2 border-dashed transition-all duration-200',
              'hover:border-primary/50 hover:bg-muted/50',
              'focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2',
              isDragging ? 'border-primary bg-primary/10 scale-[1.02]' : 'border-border',
              placeholderClasses
            ]">
            <div class="flex flex-col items-center justify-center gap-3 text-center p-8">
              <div :class="[
                'rounded-full p-4 transition-transform duration-200',
                isDragging ? 'bg-primary/20 scale-110 -rotate-6' : 'bg-muted'
              ]">
                <Upload :class="[
                  'size-8 transition-colors duration-200',
                  isDragging ? 'text-primary' : 'text-muted-foreground'
                ]" />
              </div>

              <div class="space-y-1">
                <p class="text-sm font-medium text-foreground">
                  <span v-if="isDragging" class="text-primary">Drop your image here</span>
                  <span v-else>Click to upload or drag and drop</span>
                </p>
                <p class="text-xs text-muted-foreground">
                  {{ acceptedFormats }}
                </p>
              </div>
            </div>
          </button>
        </div>
      </Transition>
    </div>
  </div>
</template>

<script setup>
  import { ref, computed, watch } from 'vue'
  import { Button } from '@/components/ui/button'
  import { RefreshCw, Trash2, Upload } from 'lucide-vue-next'

  const props = defineProps({
    modelValue: {
      type: [File, String, null],
      default: null
    },
    label: {
      type: String,
      default: ''
    },
    aspectRatio: {
      type: String,
      default: 'auto', // 'cover' (2:3), 'background' (16:9), or 'auto'
    },
    maxSize: {
      type: Number,
      default: 5 * 1024 * 1024 // 5MB
    },
    acceptedFormats: {
      type: String,
      default: 'PNG, JPG, GIF, WEBP up to 5MB'
    }
  })

  const emit = defineEmits(['update:modelValue', 'remove'])

  const fileInput = ref(null)
  const isDragging = ref(false)
  const previewUrl = ref(null)

  // Initialize preview URL if modelValue is a string (existing image URL)
  if (typeof props.modelValue === 'string') {
    previewUrl.value = props.modelValue
  }

  // Computed
  const imageClasses = computed(() => {
    switch (props.aspectRatio) {
      case 'cover':
        return 'aspect-[2/3] w-44 max-h-[264px]' // ~176×264px — standard manga cover thumbnail
      case 'background':
        return 'aspect-video w-full' // Background image style
      default:
        return 'w-full h-64' // Auto/default
    }
  })

  const placeholderClasses = computed(() => {
    switch (props.aspectRatio) {
      case 'cover':
        return 'aspect-[2/3] w-44 max-h-[264px]' // match preview size
      case 'background':
        return 'aspect-video w-full' // Background image style
      default:
        return '' // Auto/default - no aspect ratio
    }
  })

  // Methods
  const triggerFileInput = () => {
    fileInput.value?.click()
  }

  const handleFileChange = (event) => {
    const file = event.target.files?.[0]
    if (file) {
      processFile(file)
    }
  }

  const handleDrop = (event) => {
    isDragging.value = false
    const file = event.dataTransfer?.files?.[0]
    if (file) {
      processFile(file)
    }
  }

  const processFile = (file) => {
    // Validate file type
    if (!file.type.startsWith('image/')) {
      alert('Please select an image file')
      return
    }

    // Validate file size
    if (file.size > props.maxSize) {
      alert(`File size must be less than ${(props.maxSize / 1024 / 1024).toFixed(0)}MB`)
      return
    }

    // Create preview URL
    const reader = new FileReader()
    reader.onload = (e) => {
      previewUrl.value = e.target?.result
    }
    reader.readAsDataURL(file)

    // Emit the file
    emit('update:modelValue', file)
  }

  const removeImage = () => {
    previewUrl.value = null
    if (fileInput.value) {
      fileInput.value.value = ''
    }
    emit('remove')
    emit('update:modelValue', null)
  }

  // Watch modelValue changes from parent
  watch(() => props.modelValue, (newValue) => {
    if (typeof newValue === 'string') {
      previewUrl.value = newValue
    } else if (newValue === null) {
      previewUrl.value = null
    }
  })
</script>

<style scoped>
  .fade-scale-enter-active,
  .fade-scale-leave-active {
    transition: all 0.3s ease;
  }

  .fade-scale-enter-from {
    opacity: 0;
    transform: scale(0.95);
  }

  .fade-scale-leave-to {
    opacity: 0;
    transform: scale(0.95);
  }
</style>
