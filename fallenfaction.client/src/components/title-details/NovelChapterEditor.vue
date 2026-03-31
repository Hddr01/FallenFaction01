<template>
  <div class="min-h-screen bg-[var(--color-background)]">

    <!-- Loading -->
    <div v-if="isLoadingList" class="flex items-center justify-center min-h-screen">
      <svg class="animate-spin h-8 w-8 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
      </svg>
    </div>

    <div v-else-if="loadError" class="flex items-center justify-center min-h-screen px-4">
      <Card class="max-w-md w-full text-center p-8">
        <p class="text-red-500 text-lg font-medium mb-2">Failed to load</p>
        <p class="text-muted-foreground mb-4">{{ loadError }}</p>
        <Button variant="outline" @click="$router.back()">Go Back</Button>
      </Card>
    </div>

    <template v-else>
      <!-- Top bar -->
      <div class="border-b border-border bg-[var(--color-background-soft)] px-4 py-3 flex items-center gap-3">
        <Button variant="ghost" size="icon" @click="$router.back()">
          <ChevronLeftIcon class="h-5 w-5" />
        </Button>
        <div class="flex-1 min-w-0">
          <h1 class="font-semibold text-foreground truncate">{{ titleName }}</h1>
          <p class="text-xs text-muted-foreground">Chapter Editor</p>
        </div>
        <!-- Tab: Chapters only — Narration omitted (no model) -->
        <div class="flex gap-1">
          <Button size="sm" variant="default" class="pointer-events-none">
            Chapters
          </Button>
        </div>
        <Button variant="ghost" size="icon" as-child>
          <a :href="`/${titleSlug}`">
            <ExternalLinkIcon class="h-4 w-4" />
          </a>
        </Button>
      </div>

      <!-- Two-column layout -->
      <div class="flex h-[calc(100vh-57px)]">

        <!-- ── LEFT SIDEBAR: chapter list ── -->
        <aside class="w-64 shrink-0 border-r border-border bg-[var(--color-background-soft)] flex flex-col overflow-hidden">
          <div class="p-3 border-b border-border">
            <Input v-model="search" placeholder="Search chapters..." class="h-8 text-sm" />
          </div>

          <div class="flex-1 overflow-y-auto">
            <div v-if="filteredChapters.length === 0" class="p-4 text-sm text-muted-foreground text-center">
              No chapters found.
            </div>

            <button
              v-for="ch in filteredChapters"
              :key="ch.id"
              @click="selectChapter(ch)"
              :class="[
                'w-full text-left px-3 py-2.5 border-b border-border/50 transition-colors hover:bg-muted/50 cursor-pointer',
                selectedChapterId === ch.id ? 'bg-green-600/20 border-l-2 border-l-green-500' : '',
              ]"
            >
              <div class="flex items-center justify-between gap-2">
                <span class="text-xs font-medium text-foreground truncate">
                  Vol. {{ ch.volumeNumber }} Ch. {{ ch.chapterNumber }}
                </span>
                <div class="flex gap-1 shrink-0">
                  <Badge v-if="ch.hasPendingEdit" variant="outline" class="text-[10px] px-1 py-0 text-amber-500 border-amber-500">
                    Pending
                  </Badge>
                </div>
              </div>
              <p v-if="ch.name" class="text-[11px] text-muted-foreground truncate mt-0.5">{{ ch.name }}</p>
              <p class="text-[10px] text-muted-foreground/60 mt-0.5">{{ ch.teamName }}</p>
            </button>
          </div>
        </aside>

        <!-- ── RIGHT EDITOR PANEL ── -->
        <main class="flex-1 overflow-y-auto">

          <!-- No chapter selected -->
          <div v-if="!selectedChapterId" class="flex flex-col items-center justify-center h-full text-center px-8">
            <BookOpenIcon class="h-16 w-16 text-muted-foreground/30 mb-4" />
            <p class="text-lg font-medium text-muted-foreground mb-1">Select a chapter to edit</p>
            <p class="text-sm text-muted-foreground/70">Choose a chapter from the list on the left.</p>
          </div>

          <!-- Loading chapter content -->
          <div v-else-if="isLoadingChapter" class="flex items-center justify-center h-full">
            <svg class="animate-spin h-6 w-6 text-green-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
          </div>

          <!-- Editor form -->
          <div v-else-if="editForm" class="max-w-4xl mx-auto px-6 py-6 space-y-5">

            <!-- Pending edit banner -->
            <div v-if="editForm.hasPendingEdit" class="flex items-start gap-3 rounded-lg border border-amber-400/50 bg-amber-500/10 px-4 py-3">
              <ClockIcon class="h-5 w-5 text-amber-400 mt-0.5 shrink-0" />
              <div>
                <p class="text-sm font-medium text-amber-400">Edit pending admin review</p>
                <p class="text-xs text-muted-foreground mt-0.5">
                  A previous edit is still awaiting approval. Saving now will replace that pending edit.
                </p>
              </div>
            </div>

            <!-- Metadata row -->
            <div class="grid grid-cols-1 sm:grid-cols-4 gap-3">
              <!-- Volume -->
              <div class="space-y-1.5">
                <Label class="text-xs font-medium text-muted-foreground uppercase tracking-wider">Volume</Label>
                <Input
                  v-model.number="editForm.volumeNumber"
                  type="number"
                  min="1"
                  class="bg-[var(--color-background)]"
                />
              </div>

              <!-- Chapter number -->
              <div class="space-y-1.5">
                <Label class="text-xs font-medium text-muted-foreground uppercase tracking-wider">Chapter No.</Label>
                <Input
                  v-model.number="editForm.chapterNumber"
                  type="number"
                  min="1"
                  step="0.1"
                  class="bg-[var(--color-background)]"
                />
              </div>

              <!-- Chapter name -->
              <div class="sm:col-span-2 space-y-1.5">
                <Label class="text-xs font-medium text-muted-foreground uppercase tracking-wider">Chapter Name</Label>
                <Input
                  v-model="editForm.name"
                  placeholder="Optional chapter name"
                  class="bg-[var(--color-background)]"
                />
              </div>
            </div>

            <!-- Team selector — Translation titles only -->
            <div v-if="isTranslationTitle" class="space-y-1.5">
              <Label class="text-xs font-medium text-muted-foreground uppercase tracking-wider">Team</Label>
              <Select v-model="editForm.teamId">
                <SelectTrigger class="bg-[var(--color-background)]">
                  <SelectValue placeholder="Select team" />
                </SelectTrigger>
                <SelectContent class="bg-background" style="background-color: hsl(20 14.3% 4.1%) !important;">
                  <SelectItem v-for="team in userTeams" :key="team.id" :value="team.id">
                    {{ team.name }}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <!-- Word count bar -->
            <div class="flex items-center justify-between">
              <Label class="text-xs font-medium text-muted-foreground uppercase tracking-wider">Chapter Content</Label>
              <span class="text-xs text-muted-foreground">{{ wordCount }} words</span>
            </div>

            <!-- Content textarea -->
            <Textarea
              v-model="editForm.content"
              placeholder="Paste or write the chapter text here. Use blank lines to separate paragraphs."
              class="min-h-[480px] resize-y font-serif leading-relaxed text-base bg-[var(--color-background)]"
            />

            <!-- Action bar -->
            <div class="flex items-center justify-between pt-2 border-t border-border">

              <!-- Left: Reset / Delete -->
              <div class="flex items-center gap-2">
                <Button variant="outline" @click="resetForm" :disabled="isSaving || isDeleting">
                  Reset changes
                </Button>

                <!-- Normal delete button -->
                <Button
                  v-if="confirmDelete === null"
                  variant="outline"
                  @click="confirmDelete = 'pending'"
                  :disabled="isSaving || isDeleting"
                  class="text-red-500 border-red-500/40 hover:bg-red-500/10 hover:text-red-400"
                >
                  <Trash2Icon class="h-4 w-4 mr-1.5" />
                  Delete
                </Button>

                <!-- Inline confirm row -->
                <template v-else>
                  <span class="text-sm text-red-400 font-medium">Delete this chapter?</span>
                  <Button
                    @click="deleteChapter"
                    :disabled="isDeleting"
                    class="bg-red-600 hover:bg-red-700 text-white"
                  >
                    <svg v-if="isDeleting" class="animate-spin h-4 w-4 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                    </svg>
                    {{ isDeleting ? 'Deleting...' : 'Yes, delete' }}
                  </Button>
                  <Button variant="outline" @click="confirmDelete = null" :disabled="isDeleting">
                    Cancel
                  </Button>
                </template>
              </div>

              <!-- Right: Save -->
              <Button
                @click="submitEdit"
                :disabled="isSaving || isDeleting || !editForm.content.trim()"
                class="bg-green-600 hover:bg-green-700 text-white min-w-[120px]"
              >
                <svg v-if="isSaving" class="animate-spin h-4 w-4 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                </svg>
                {{ isSaving ? 'Saving...' : 'Save' }}
              </Button>
            </div>

          </div>
        </main>
      </div>
    </template>

    <!-- Toast -->
    <Transition name="toast">
      <div v-if="toast.show"
           :class="[
             'fixed bottom-5 right-5 z-50 flex items-start gap-3 rounded-lg border px-4 py-3 shadow-lg max-w-sm',
             toast.type === 'success'
               ? 'border-green-500/30 bg-green-500/10 text-green-400'
               : 'border-red-500/30 bg-red-500/10 text-red-400'
           ]">
        <CheckCircleIcon v-if="toast.type === 'success'" class="h-5 w-5 mt-0.5 shrink-0" />
        <XCircleIcon v-else class="h-5 w-5 mt-0.5 shrink-0" />
        <p class="text-sm font-medium">{{ toast.message }}</p>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { chapterService } from '../../services/chapterService.js'
import { titleDetailsService } from '../../services/titleDetailsService.js'
import { parseTitleSlug } from '../../utils/titleSlug.js'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue
} from '@/components/ui/select'

import {
  ChevronLeftIcon,
  BookOpenIcon,
  ExternalLinkIcon,
  ClockIcon,
  CheckCircleIcon,
  XCircleIcon,
  Trash2Icon
} from 'lucide-vue-next'

const props = defineProps({
  titleSlug: { type: String, required: true }
})

const route = useRoute()
const router = useRouter()

// ── State ──────────────────────────────────────────────────────────────────────
const titleId = ref(null)
const titleName = ref('')
const chapters = ref([])
const userTeams = ref([])
const isLoadingList = ref(true)
const loadError = ref('')
const search = ref('')

const titleCategory = ref(1) // 1=Translation, 2=Original, 3=Fanfic, 4=AITranslation
const selectedChapterId = ref(null)
const isLoadingChapter = ref(false)
const editForm = ref(null)   // populated when a chapter is selected
const originalSnapshot = ref(null) // for reset

const isSaving = ref(false)
const isDeleting = ref(false)
// confirmDelete: null = idle, 'pending' = waiting for confirmation
const confirmDelete = ref(null)
const toast = reactive({ show: false, type: 'success', message: '' })

// ── Computed ───────────────────────────────────────────────────────────────────
const filteredChapters = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return chapters.value
  return chapters.value.filter(c =>
    c.name?.toLowerCase().includes(q) ||
    String(c.chapterNumber).includes(q) ||
    String(c.volumeNumber).includes(q) ||
    c.teamName?.toLowerCase().includes(q)
  )
})

// Only Translation titles (category 1) use a team selector.
// Original / Fanfic: single creator, no team.
// AI/TL: admin-only, no per-team selector.
const isTranslationTitle = computed(() => titleCategory.value === 1)

const wordCount = computed(() => {
  if (!editForm.value?.content) return 0
  return editForm.value.content.trim()
    ? editForm.value.content.trim().split(/\s+/).length
    : 0
})

// ── Load chapter list ──────────────────────────────────────────────────────────
const loadChapterList = async () => {
  isLoadingList.value = true
  loadError.value = ''
  try {
    // Resolve titleId from slug
    const titleResult = await titleDetailsService.getTitleDetails(props.titleSlug)
    if (!titleResult.success || !titleResult.data) {
      loadError.value = 'Title not found.'
      return
    }
    titleId.value = titleResult.data.id
    titleName.value = titleResult.data.originalTitle

    const result = await chapterService.getChaptersForManagement(titleId.value)
    if (!result.success) {
      loadError.value = result.error || 'Failed to load chapters.'
      return
    }
    chapters.value = result.data.chapters || []
    userTeams.value = result.data.userTeams || []
    titleCategory.value = result.data.titleCategory ?? 1
  } catch (err) {
    loadError.value = err.message || 'Unexpected error.'
  } finally {
    isLoadingList.value = false
  }
}

// ── Select chapter for editing ─────────────────────────────────────────────────
const selectChapter = async (ch) => {
  if (selectedChapterId.value === ch.id) return

  selectedChapterId.value = ch.id
  isLoadingChapter.value = true
  editForm.value = null

  try {
    const result = await chapterService.getChapterForEdit(titleId.value, ch.id)
    if (!result.success) {
      showToast('error', result.error || 'Failed to load chapter.')
      selectedChapterId.value = null
      return
    }

    const d = result.data
    // If there's a pending edit, pre-fill the editor with the pending values
    const src = d.hasPendingEdit && d.pendingEdit ? d.pendingEdit : d

    editForm.value = {
      chapterId: d.id,
      name: src.name ?? '',
      volumeNumber: src.volumeNumber,
      chapterNumber: src.chapterNumber,
      teamId: src.teamId,
      content: src.content ?? '',
      hasPendingEdit: d.hasPendingEdit
    }
    originalSnapshot.value = { ...editForm.value }
  } finally {
    isLoadingChapter.value = false
  }
}

// ── Submit edit ────────────────────────────────────────────────────────────────
const submitEdit = async () => {
  if (!editForm.value) return
  isSaving.value = true

  try {
    const result = await chapterService.updateChapter(titleId.value, editForm.value.chapterId, {
      name: editForm.value.name,
      volumeNumber: editForm.value.volumeNumber,
      chapterNumber: editForm.value.chapterNumber,
      teamId: editForm.value.teamId,
      content: editForm.value.content
    })

    if (result.success) {
      showToast(
        'success',
        result.autoApproved
          ? 'Chapter updated successfully!'
          : 'Edit submitted for admin review.'
      )
      // Refresh list to reflect pending state changes
      const listItem = chapters.value.find(c => c.id === editForm.value.chapterId)
      if (listItem) {
        listItem.hasPendingEdit = !result.autoApproved
        if (result.autoApproved) {
          listItem.name = editForm.value.name
          listItem.volumeNumber = editForm.value.volumeNumber
          listItem.chapterNumber = editForm.value.chapterNumber
        }
      }
      originalSnapshot.value = { ...editForm.value }
      if (editForm.value) editForm.value.hasPendingEdit = !result.autoApproved
    } else {
      showToast('error', result.error || 'Failed to save.')
    }
  } finally {
    isSaving.value = false
  }
}

// ── Reset form ─────────────────────────────────────────────────────────────────
const resetForm = () => {
  if (originalSnapshot.value) {
    editForm.value = { ...originalSnapshot.value }
  }
  confirmDelete.value = null
}

// ── Delete chapter ──────────────────────────────────────────────────────────────
const deleteChapter = async () => {
  if (!editForm.value) return
  isDeleting.value = true

  try {
    const result = await chapterService.deleteChapter(titleId.value, editForm.value.chapterId)

    if (result.success) {
      // Remove from sidebar list
      chapters.value = chapters.value.filter(c => c.id !== editForm.value.chapterId)
      // Clear the editor panel
      selectedChapterId.value = null
      editForm.value = null
      originalSnapshot.value = null
      confirmDelete.value = null
      showToast('success', 'Chapter deleted.')
    } else {
      showToast('error', result.error || 'Failed to delete chapter.')
      confirmDelete.value = null
    }
  } finally {
    isDeleting.value = false
  }
}

// ── Toast helper ───────────────────────────────────────────────────────────────
const showToast = (type, message) => {
  toast.type = type
  toast.message = message
  toast.show = true
  setTimeout(() => { toast.show = false }, 4000)
}

onMounted(loadChapterList)
</script>

<style scoped>
.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}
.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(1rem);
}
</style>
