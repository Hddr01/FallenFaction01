<template>
  <Dialog :open="isOpen" @update:open="handleOpenChange">
    <DialogContent class="sm:max-w-[480px] bg-[var(--color-background)] border-[var(--color-border)] overflow-y-auto max-h-[min(90vh,880px)] overflow-x-visible isolate">
      <DialogHeader>
        <DialogTitle class="text-[var(--color-heading)] flex items-center gap-2">
          <Flag class="w-5 h-5 text-red-500" />
          Report {{ targetLabel }}
        </DialogTitle>
        <DialogDescription class="text-[var(--color-text)] opacity-70">
          Help us keep the community safe. All reports are reviewed by our admin team.
        </DialogDescription>
      </DialogHeader>

      <div class="space-y-4 py-2">
        <!-- Reason select -->
        <div class="space-y-2 relative z-20">
          <label class="text-sm font-medium text-[var(--color-text)]">
            Reason <span class="text-red-500">*</span>
          </label>
          <select
            v-model="form.reason"
            class="relative z-20 w-full px-3 py-2 rounded-md border border-[var(--color-border)] bg-[var(--color-background-soft)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--vt-c-indigo)] focus:border-transparent transition-colors"
          >
            <option value="" disabled>Select a reason...</option>
            <option value="1">Spam</option>
            <option value="2">Harassment or Bullying</option>
            <option value="3">Inappropriate Content</option>
            <option value="4">Spoiler (unmarked)</option>
            <option value="5">Copyright Violation</option>
            <option value="6">Misinformation / Fake</option>
            <option value="7">Hate Speech</option>
            <option value="99">Other</option>
          </select>
          <p v-if="validationErrors.reason" class="text-xs text-red-500">{{ validationErrors.reason }}</p>
        </div>

        <!-- Description textarea -->
        <div class="space-y-2">
          <label class="text-sm font-medium text-[var(--color-text)]">
            Additional Details
            <span class="text-[var(--color-text)] opacity-50 font-normal">(optional)</span>
          </label>
          <textarea
            v-model="form.description"
            placeholder="Describe the issue in more detail..."
            rows="4"
            maxlength="1000"
            class="w-full px-3 py-2 rounded-md border border-[var(--color-border)] bg-[var(--color-background-soft)] text-[var(--color-text)] text-sm resize-none focus:outline-none focus:ring-2 focus:ring-[var(--vt-c-indigo)] focus:border-transparent transition-colors placeholder:opacity-40"
          />
          <p class="text-xs text-[var(--color-text)] opacity-40 text-right">
            {{ form.description.length }}/1000
          </p>
        </div>

        <!-- Success message -->
        <div v-if="successMessage" class="flex items-center gap-2 px-3 py-2 rounded-md bg-green-500/10 border border-green-500/30 text-green-600 dark:text-green-400 text-sm">
          <CheckCircle class="w-4 h-4 flex-shrink-0" />
          {{ successMessage }}
        </div>

        <!-- Error message -->
        <div v-if="errorMessage" class="flex items-center gap-2 px-3 py-2 rounded-md bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-400 text-sm">
          <AlertCircle class="w-4 h-4 flex-shrink-0" />
          {{ errorMessage }}
        </div>
      </div>

      <DialogFooter class="gap-2">
        <button
          @click="handleClose"
          :disabled="submitting"
          class="px-4 py-2 rounded-md text-sm font-medium border border-[var(--color-border)] bg-transparent text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          @click="submitReport"
          :disabled="submitting || !form.reason || !!successMessage"
          class="px-4 py-2 rounded-md text-sm font-medium bg-red-600 hover:bg-red-700 text-white transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <span v-if="submitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
          {{ submitting ? 'Submitting...' : 'Submit Report' }}
        </button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { Flag, CheckCircle, AlertCircle } from 'lucide-vue-next'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { reportsService } from '@/services/reportsService'

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false
  },
  // 1=Comment, 2=Title, 3=Chapter, 4=User
  targetType: {
    type: Number,
    required: true
  },
  // The ID of the thing being reported
  targetId: {
    type: [Number, String],
    required: true
  }
})

const emit = defineEmits(['close', 'reported'])

const TARGET_LABELS = {
  1: 'Comment',
  2: 'Title',
  3: 'Chapter',
  4: 'User'
}

const targetLabel = computed(() => TARGET_LABELS[props.targetType] || 'Content')

const form = ref({ reason: '', description: '' })
const submitting = ref(false)
const successMessage = ref('')
const errorMessage = ref('')
const validationErrors = ref({})

// Reset form when modal opens
watch(() => props.isOpen, (open) => {
  if (open) {
    form.value = { reason: '', description: '' }
    submitting.value = false
    successMessage.value = ''
    errorMessage.value = ''
    validationErrors.value = {}
  }
})

const validate = () => {
  const errors = {}
  if (!form.value.reason) errors.reason = 'Please select a reason.'
  validationErrors.value = errors
  return Object.keys(errors).length === 0
}

const submitReport = async () => {
  if (!validate() || submitting.value) return

  submitting.value = true
  errorMessage.value = ''

  // Build the payload based on target type
  const payload = {
    targetType: props.targetType,
    reason: parseInt(form.value.reason),
    description: form.value.description.trim() || undefined
  }

  if (props.targetType === 1) payload.targetCommentId = parseInt(props.targetId)
  else if (props.targetType === 2) payload.targetTitleId = parseInt(props.targetId)
  else if (props.targetType === 3) payload.targetChapterId = parseInt(props.targetId)
  else if (props.targetType === 4) payload.targetUserId = String(props.targetId)

  try {
    await reportsService.createReport(payload)
    successMessage.value = 'Your report has been submitted. Thank you for helping keep the community safe!'
    emit('reported')
    // Auto-close after showing success
    setTimeout(() => {
      handleClose()
    }, 2000)
  } catch (err) {
    const msg = err.message || ''
    if (msg.includes('already reported') || msg.includes('duplicate') || msg.toLowerCase().includes('conflict')) {
      errorMessage.value = 'You have already reported this content. Our team is reviewing it.'
    } else if (msg.includes('401') || msg.includes('Unauthorized')) {
      errorMessage.value = 'You must be logged in to submit a report.'
    } else {
      errorMessage.value = 'Failed to submit report. Please try again.'
    }
  } finally {
    submitting.value = false
  }
}

const handleClose = () => {
  if (!submitting.value) emit('close')
}

const handleOpenChange = (open) => {
  if (!open) handleClose()
}
</script>
