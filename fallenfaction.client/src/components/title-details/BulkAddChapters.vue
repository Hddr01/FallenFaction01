<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Header -->
      <div class="mb-8 flex items-center gap-4">
        <button @click="router.back()"
          class="p-2 rounded-md border border-[var(--color-border)] text-[var(--color-text)] hover:bg-[var(--color-background-mute)] transition-colors">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
        </button>
        <div>
          <h1 class="text-3xl font-bold text-[var(--color-heading)]">Bulk Upload Chapters</h1>
          <p class="text-[var(--color-text)] opacity-75">{{ titleName || 'Loading…' }}</p>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="isLoadingForm" class="text-center py-16">
        <svg class="animate-spin mx-auto h-8 w-8 text-green-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
        </svg>
        <p class="mt-3 text-[var(--color-text)] opacity-60">Loading form data…</p>
      </div>

      <!-- Permission Error -->
      <div v-else-if="initError" class="bg-red-500/10 border border-red-500/30 rounded-xl p-6 text-center">
        <p class="text-red-400 font-medium">{{ initError }}</p>
      </div>

      <template v-else>
        <!-- Format Instructions -->
        <div class="mb-6 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-5">
          <div class="flex items-center gap-2 mb-3 cursor-pointer select-none" @click="showInstructions = !showInstructions">
            <svg class="w-4 h-4 text-[var(--color-accent)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            <span class="text-sm font-semibold text-[var(--color-heading)]">How to format your input</span>
            <svg :class="['w-4 h-4 ml-auto transition-transform', showInstructions ? 'rotate-180' : '']" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
            </svg>
          </div>
          <div v-if="showInstructions" class="space-y-3 text-sm text-[var(--color-text)] opacity-80">
            <p>Separate chapters with a line containing only <code class="bg-[var(--color-background)] px-1.5 py-0.5 rounded text-[var(--color-accent)] font-mono">===</code> (three equals signs).</p>
            <p>Start each chapter block with optional metadata lines:</p>
            <pre class="bg-[var(--color-background)] rounded-lg p-3 text-xs font-mono overflow-x-auto text-[var(--color-text)]">Chapter: 1
Volume: 1
Name: The Beginning

Chapter text goes here...

===

Chapter: 2
Volume: 1

Next chapter content...</pre>
            <p class="opacity-60">If you omit <code class="font-mono">Chapter:</code> and <code class="font-mono">Volume:</code>, numbers are auto-incremented from the last chapter in the title.</p>
          </div>
        </div>

        <!-- Team + Volume start row -->
        <div class="mb-6 grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Team *</label>
            <select v-model="selectedTeamId"
              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-lg bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">Select team</option>
              <option v-for="t in userTeams" :key="t.id" :value="t.id">{{ t.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Start Volume</label>
            <input type="number" v-model.number="startVolume" min="1"
              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-lg bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-green-500"/>
          </div>
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Start Chapter №</label>
            <input type="number" v-model.number="startChapter" min="1" step="0.1"
              class="w-full px-3 py-2 border border-[var(--color-border)] rounded-lg bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-green-500"/>
          </div>
        </div>

        <!-- Paste area -->
        <div class="mb-4">
          <div class="flex items-center justify-between mb-1">
            <label class="block text-sm font-medium text-[var(--color-text)]">Chapter Content</label>
            <span class="text-xs text-[var(--color-text)] opacity-50">{{ detectedCount }} chapter{{ detectedCount !== 1 ? 's' : '' }} detected</span>
          </div>
          <textarea v-model="rawInput"
            @input="parseChapters"
            placeholder="Paste all chapters here, separated by === on its own line..."
            rows="24"
            class="w-full px-4 py-3 border border-[var(--color-border)] rounded-xl bg-[var(--color-background)] text-[var(--color-text)] focus:outline-none focus:ring-2 focus:ring-green-500 resize-y font-serif leading-relaxed text-sm"/>
        </div>

        <!-- Preview table -->
        <div v-if="parsedChapters.length > 0" class="mb-6 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl overflow-hidden">
          <div class="px-4 py-3 border-b border-[var(--color-border)] flex items-center justify-between">
            <span class="text-sm font-semibold text-[var(--color-heading)]">Preview ({{ parsedChapters.length }} chapters)</span>
            <span class="text-xs text-[var(--color-text)] opacity-50">{{ totalWords }} words total</span>
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="border-b border-[var(--color-border)]">
                <tr class="text-[var(--color-text)] opacity-60">
                  <th class="px-4 py-2 text-left font-medium w-16">Vol.</th>
                  <th class="px-4 py-2 text-left font-medium w-20">Ch.</th>
                  <th class="px-4 py-2 text-left font-medium">Name</th>
                  <th class="px-4 py-2 text-right font-medium w-24">Words</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-[var(--color-border)]">
                <tr v-for="(ch, i) in parsedChapters" :key="i" class="text-[var(--color-text)]">
                  <td class="px-4 py-2">{{ ch.volumeNumber }}</td>
                  <td class="px-4 py-2">{{ ch.chapterNumber }}</td>
                  <td class="px-4 py-2 opacity-70">{{ ch.name || '—' }}</td>
                  <td class="px-4 py-2 text-right opacity-50">{{ ch.wordCount }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Submit row -->
        <div class="flex items-center justify-between gap-4">
          <div v-if="submitProgress.total > 0" class="flex-1">
            <div class="flex items-center justify-between text-xs text-[var(--color-text)] opacity-60 mb-1">
              <span>Uploading {{ submitProgress.done }}/{{ submitProgress.total }}</span>
              <span>{{ submitProgress.failed }} failed</span>
            </div>
            <div class="w-full h-2 rounded-full bg-[var(--color-border)] overflow-hidden">
              <div class="h-2 bg-green-500 transition-all duration-300 rounded-full"
                :style="{ width: (submitProgress.done / submitProgress.total * 100) + '%' }"/>
            </div>
          </div>
          <div v-else class="text-sm text-[var(--color-text)] opacity-50">
            {{ parsedChapters.length > 0 ? `Ready to upload ${parsedChapters.length} chapters` : 'Paste chapters above to begin' }}
          </div>
          <button
            @click="submitAll"
            :disabled="isSubmitting || parsedChapters.length === 0 || !selectedTeamId"
            class="px-8 py-2.5 bg-green-600 text-white rounded-lg font-semibold text-sm hover:bg-green-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors flex items-center gap-2 shrink-0">
            <svg v-if="isSubmitting" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
            </svg>
            {{ isSubmitting ? 'Uploading…' : 'Upload All Chapters' }}
          </button>
        </div>

        <!-- Results -->
        <div v-if="results.length > 0" class="mt-6 space-y-2">
          <div v-for="(r, i) in results" :key="i"
            :class="['px-4 py-2.5 rounded-lg text-sm flex items-center gap-2',
              r.ok ? 'bg-green-500/10 border border-green-500/20 text-green-400' : 'bg-red-500/10 border border-red-500/20 text-red-400']">
            <svg v-if="r.ok" class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
            </svg>
            <svg v-else class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
            <span>Vol.{{ r.vol }} Ch.{{ r.ch }}{{ r.name ? ' — ' + r.name : '' }}: {{ r.message }}</span>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { chapterService } from '../../services/chapterService.js'
import { titleDetailsService } from '../../services/titleDetailsService.js'

const route  = useRoute()
const router = useRouter()

// State
const titleName      = ref('')
const titleId        = ref(null)
const isLoadingForm  = ref(true)
const initError      = ref('')
const userTeams      = ref([])
const selectedTeamId = ref('')
const startVolume    = ref(1)
const startChapter   = ref(1)
const rawInput       = ref('')
const parsedChapters = ref([])
const isSubmitting   = ref(false)
const showInstructions = ref(true)
const results        = ref([])
const submitProgress = ref({ total: 0, done: 0, failed: 0 })

const detectedCount = computed(() => parsedChapters.value.length)
const totalWords    = computed(() => parsedChapters.value.reduce((s, c) => s + c.wordCount, 0))

// Load form data (same pattern as AddChapter.vue)
onMounted(async () => {
  try {
    isLoadingForm.value = true
    let actualTitleId = null

    const titleIdParam = route.params.titleId
    if (titleIdParam && !isNaN(titleIdParam)) {
      actualTitleId = parseInt(titleIdParam)
    } else {
      const slugParam = route.params.titleSlug || route.params.titleName
      if (slugParam) {
        const res = await titleDetailsService.getTitleDetails(slugParam)
        if (res.success && res.data) {
          actualTitleId = res.data.id
          titleName.value = res.data.originalTitle
        } else throw new Error('Title not found.')
      }
    }
    if (!actualTitleId) throw new Error('Title not found. Access this page from a valid title.')

    const res = await chapterService.getChapterFormData(actualTitleId)
    if (!res.success) throw new Error(res.error || 'Failed to load form data.')
    if (!res.data?.hasPermission) throw new Error('You do not have permission to add chapters to this title.')

    titleId.value        = actualTitleId
    titleName.value      = res.data.titleName || titleName.value
    userTeams.value      = res.data.userTeams || []
    startVolume.value    = res.data.suggestedVolumeNumber   || 1
    startChapter.value   = res.data.suggestedChapterNumber  || 1
    if (userTeams.value.length === 1) selectedTeamId.value = userTeams.value[0].id
  } catch (err) {
    initError.value = err.message
  } finally {
    isLoadingForm.value = false
  }
})

// Re-parse when start numbers change
watch([startVolume, startChapter], parseChapters)

function parseChapters() {
  if (!rawInput.value.trim()) { parsedChapters.value = []; return }

  const blocks = rawInput.value.split(/^===\s*$/m).map(b => b.trim()).filter(Boolean)
  let vol = startVolume.value
  let ch  = startChapter.value

  parsedChapters.value = blocks.map(block => {
    const lines = block.split('\n')
    let name = ''
    let contentStart = 0
    let parsedVol = null
    let parsedCh  = null

    // Parse leading metadata lines
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i]
      const chMatch  = line.match(/^Chapter:\s*(.+)/i)
      const volMatch = line.match(/^Volume:\s*(.+)/i)
      const nameMatch = line.match(/^Name:\s*(.+)/i)

      if (chMatch)   { parsedCh  = parseFloat(chMatch[1]);  contentStart = i + 1; continue }
      if (volMatch)  { parsedVol = parseInt(volMatch[1]);    contentStart = i + 1; continue }
      if (nameMatch) { name = nameMatch[1].trim();           contentStart = i + 1; continue }
      // First non-metadata line → content starts
      if (!chMatch && !volMatch && !nameMatch) { contentStart = i; break }
    }

    const content = lines.slice(contentStart).join('\n').trim()
    const usedVol = parsedVol ?? vol
    const usedCh  = parsedCh  ?? ch

    // Auto-increment for next block
    ch = Math.round((usedCh + 1) * 10) / 10
    vol = usedVol

    return {
      volumeNumber: usedVol,
      chapterNumber: usedCh,
      name,
      content,
      wordCount: content ? content.trim().split(/\s+/).length : 0
    }
  })
}

async function submitAll() {
  if (!titleId.value || !selectedTeamId.value || parsedChapters.value.length === 0) return

  isSubmitting.value  = true
  results.value       = []
  submitProgress.value = { total: parsedChapters.value.length, done: 0, failed: 0 }

  for (const ch of parsedChapters.value) {
    try {
      const res = await chapterService.createChapter(titleId.value, {
        titleId: titleId.value,
        teamId: selectedTeamId.value,
        volumeNumber: ch.volumeNumber,
        chapterNumber: ch.chapterNumber,
        name: ch.name || '',
        content: ch.content
      })

      const ok = res.success
      results.value.push({
        ok,
        vol: ch.volumeNumber,
        ch: ch.chapterNumber,
        name: ch.name,
        message: ok ? (res.message || 'Submitted') : (res.error || 'Failed')
      })
      if (!ok) submitProgress.value.failed++
    } catch (err) {
      results.value.push({ ok: false, vol: ch.volumeNumber, ch: ch.chapterNumber, name: ch.name, message: err.message || 'Error' })
      submitProgress.value.failed++
    }
    submitProgress.value.done++
  }

  isSubmitting.value = false
  const ok = results.value.filter(r => r.ok).length
  if (ok > 0) {
    rawInput.value = ''
    parsedChapters.value = []
  }
}
</script>
