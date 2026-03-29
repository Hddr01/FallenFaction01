<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Page Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Admin Title Management</h1>
        <p class="mt-2 text-[var(--color-text)] opacity-75">Review and manage submitted titles</p>
      </div>

      <!-- Global messages -->
      <div v-if="successMessage" class="mb-6 flex items-start gap-3 bg-green-900/30 border border-green-600/40 rounded-lg p-4">
        <svg class="h-5 w-5 text-green-400 shrink-0 mt-0.5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" /></svg>
        <p class="text-sm text-green-300 flex-1">{{ successMessage }}</p>
        <button @click="successMessage = ''" class="text-green-400 hover:text-green-200"><svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg></button>
      </div>
      <div v-if="error" class="mb-6 flex items-start gap-3 bg-red-900/30 border border-red-600/40 rounded-lg p-4">
        <svg class="h-5 w-5 text-red-400 shrink-0 mt-0.5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" /></svg>
        <p class="text-sm text-red-300 flex-1">{{ error }}</p>
        <button @click="error = ''" class="text-red-400 hover:text-red-200"><svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg></button>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="text-center py-12">
        <svg class="animate-spin h-8 w-8 text-green-500 mx-auto" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
        </svg>
        <p class="mt-3 text-[var(--color-text)] opacity-60">Loading pending titles...</p>
      </div>

      <!-- Empty -->
      <div v-else-if="pendingTitles.length === 0" class="text-center py-16 text-[var(--color-text)] opacity-50">
        <svg class="mx-auto h-12 w-12 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
        <p class="text-sm font-medium">No pending titles</p>
        <p class="text-xs mt-1">All submitted titles have been reviewed.</p>
      </div>

      <!-- Table -->
      <div v-else class="bg-[var(--color-background-soft)] rounded-xl border border-[var(--color-border)] overflow-hidden shadow">
        <div class="px-6 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
          <h2 class="text-lg font-semibold text-[var(--color-heading)]">
            Pending Titles
            <span class="ml-2 text-sm font-normal text-[var(--color-text)] opacity-50">({{ pendingTitles.length }})</span>
          </h2>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-[var(--color-border)]">
            <thead class="bg-[var(--color-background-mute)]">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-[var(--color-text)] opacity-60 uppercase tracking-wider">ID</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-[var(--color-text)] opacity-60 uppercase tracking-wider">Original Title</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-[var(--color-text)] opacity-60 uppercase tracking-wider">English Title</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-[var(--color-text)] opacity-60 uppercase tracking-wider">Type</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-[var(--color-text)] opacity-60 uppercase tracking-wider">Duplicates</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-[var(--color-text)] opacity-60 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--color-border)]">
              <tr v-for="title in pendingTitles" :key="title.id"
                  class="hover:bg-[var(--color-background-mute)] transition-colors"
                  :class="{ 'border-l-4 border-l-red-500': getSimilarityLevel(title.id) === 'exact',
                             'border-l-4 border-l-yellow-500': getSimilarityLevel(title.id) === 'similar',
                             'border-l-4 border-l-blue-500': getSimilarityLevel(title.id) === 'partial' }">
                <td class="px-4 py-3 text-sm text-[var(--color-text)] opacity-60 whitespace-nowrap">#{{ title.id }}</td>
                <td class="px-4 py-3 whitespace-nowrap">
                  <button @click="viewTitleDetails(title)" class="text-sm font-medium text-green-500 hover:text-green-400 hover:underline">
                    {{ title.originalTitle || 'N/A' }}
                  </button>
                </td>
                <td class="px-4 py-3 text-sm text-[var(--color-text)] whitespace-nowrap">{{ title.englishTitle }}</td>
                <td class="px-4 py-3 whitespace-nowrap">
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" :class="getTypeColor(title.type)">
                    {{ getTypeName(title.type) }}
                  </span>
                </td>
                <!-- Similarity badge -->
                <td class="px-4 py-3 whitespace-nowrap">
                  <template v-if="similarityCache[title.id] === undefined">
                    <span class="text-xs text-[var(--color-text)] opacity-40">checking…</span>
                  </template>
                  <template v-else-if="!similarityCache[title.id]?.length">
                    <span class="inline-flex items-center gap-1 text-xs text-green-400">
                      <svg class="h-3.5 w-3.5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" /></svg>
                      Unique
                    </span>
                  </template>
                  <template v-else>
                    <button @click="viewTitleDetails(title)" class="inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full"
                            :class="badgeClass(getSimilarityLevel(title.id))">
                      <svg class="h-3 w-3" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
                      {{ similarityCache[title.id].length }} {{ getSimilarityLevel(title.id) }}
                    </button>
                  </template>
                </td>
                <td class="px-4 py-3 whitespace-nowrap text-sm space-x-2">
                  <button @click="acceptTitle(title.id)" :disabled="isProcessing"
                          class="inline-flex items-center px-3 py-1.5 rounded text-xs font-medium text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
                    Accept
                  </button>
                  <button @click="rejectTitle(title.id)" :disabled="isProcessing"
                          class="inline-flex items-center px-3 py-1.5 rounded text-xs font-medium text-white bg-red-600 hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors">
                    Reject
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ── Title Details + Similarity Modal ──────────────────────────────── -->
    <Transition name="modal-fade">
      <div v-if="showDetailsModal && titleDetails" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
        <div class="bg-[var(--color-background-soft)] rounded-2xl shadow-2xl max-w-4xl w-full max-h-[90vh] overflow-y-auto border border-[var(--color-border)]">

          <!-- Modal header -->
          <div class="px-6 py-4 border-b border-[var(--color-border)] flex items-center justify-between sticky top-0 bg-[var(--color-background-soft)] z-10">
            <div>
              <h3 class="text-lg font-semibold text-[var(--color-heading)]">Title Review</h3>
              <p class="text-xs text-[var(--color-text)] opacity-50 mt-0.5">ID #{{ titleDetails.id }}</p>
            </div>
            <button @click="closeDetailsModal" class="text-[var(--color-text)] opacity-50 hover:opacity-100 transition-opacity">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>

          <div class="p-6 space-y-6">

            <!-- ── SIMILARITY ALERT ─────────────────────────────────────── -->
            <div v-if="currentSimilarities.length > 0" class="rounded-xl border overflow-hidden"
                 :class="currentSimilarities[0].matchLevel === 'exact' ? 'border-red-500/40 bg-red-900/20' :
                          currentSimilarities[0].matchLevel === 'similar' ? 'border-yellow-500/40 bg-yellow-900/20' :
                          'border-blue-500/40 bg-blue-900/20'">
              <div class="px-4 py-3 flex items-center gap-2 border-b"
                   :class="currentSimilarities[0].matchLevel === 'exact' ? 'border-red-500/30 bg-red-900/30' :
                            currentSimilarities[0].matchLevel === 'similar' ? 'border-yellow-500/30 bg-yellow-900/30' :
                            'border-blue-500/30 bg-blue-900/30'">
                <svg class="h-5 w-5 shrink-0" :class="currentSimilarities[0].matchLevel === 'exact' ? 'text-red-400' :
                                                        currentSimilarities[0].matchLevel === 'similar' ? 'text-yellow-400' : 'text-blue-400'"
                     fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
                </svg>
                <h4 class="font-semibold text-sm"
                    :class="currentSimilarities[0].matchLevel === 'exact' ? 'text-red-300' :
                             currentSimilarities[0].matchLevel === 'similar' ? 'text-yellow-300' : 'text-blue-300'">
                  {{
 currentSimilarities[0].matchLevel === 'exact' ? '⛔ Exact duplicate detected' :
                     currentSimilarities[0].matchLevel === 'similar' ? '⚠️ Similar titles exist' :
                     'ℹ️ Partial name overlap found'
                  }}
                </h4>
              </div>

              <!-- Matched titles list -->
              <div class="divide-y divide-white/5">
                <div v-for="match in currentSimilarities" :key="match.id" class="px-4 py-3 flex items-start justify-between gap-4">
                  <div class="min-w-0">
                    <p class="text-sm font-medium text-[var(--color-text)]">{{ match.originalTitle }}</p>
                    <p v-if="match.englishTitle && match.englishTitle !== match.originalTitle"
                       class="text-xs text-[var(--color-text)] opacity-50">{{ match.englishTitle }}</p>
                    <p v-if="match.alternativeNames" class="text-xs text-[var(--color-text)] opacity-40 mt-0.5">
                      Alt: {{ match.alternativeNames }}
                    </p>
                  </div>
                  <div class="flex items-center gap-2 shrink-0">
                    <span class="text-xs px-2 py-0.5 rounded-full font-medium" :class="badgeClass(match.matchLevel)">
                      {{ match.matchLevel }}
                    </span>
                    <a :href="`/${buildSlug(match.originalTitle, match.id)}`" target="_blank"
                       class="text-xs text-blue-400 hover:text-blue-300 underline">
                      View #{{ match.id }}
                    </a>
                  </div>
                </div>
              </div>

              <!-- Disambiguation protocol -->
              <div class="px-4 py-3 bg-[var(--color-border)] text-xs text-[var(--color-text)] opacity-60 space-y-1">
                <p class="font-semibold opacity-80">📋 Disambiguation protocol:</p>
                <ul class="list-disc list-inside space-y-0.5">
                  <li><strong>Exact duplicate</strong> — reject unless it's a different medium/adaptation. If different, ask the submitter to add a year or subtitle: e.g. "Naruto (2002)" vs "Naruto: Shippuden".</li>
                  <li><strong>Similar / Partial</strong> — review carefully. If these are distinct works, both can coexist. Ensure the titles are distinguishable so readers don't confuse them.</li>
                  <li>All approved titles get a unique URL slug <code class="bg-white/10 px-1 rounded">title-name-{id}</code>, so URL conflicts are impossible even with identical names.</li>
                </ul>
              </div>
            </div>

            <!-- ── Title info grid ─────────────────────────────────────── -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h4 class="text-xs font-semibold text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-3">Basic Information</h4>
                <dl class="space-y-2">
                  <div v-for="[label, val] in [
                    ['Original Title',    titleDetails.originalTitle || 'N/A'],
                    ['English Title',     titleDetails.englishTitle],
                    ['Alternative Names', titleDetails.alternativeNames || 'N/A'],
                    ['Release Date',      titleDetails.releaseDate || 'N/A'],
                    ['Type',              getTypeName(titleDetails.type)],
                    ['Age Restriction',   titleDetails.ageRestriction || 'None'],
                  ]" :key="label">
                    <dt class="text-xs text-[var(--color-text)] opacity-40">{{ label }}</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ val }}</dd>
                  </div>
                </dl>
              </div>
              <div>
                <h4 class="text-xs font-semibold text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-3">Status & Tags</h4>
                <dl class="space-y-2">
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-40">Title Status</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.statusTitle || 'N/A' }}</dd>
                  </div>
                  <div>
                    <dt class="text-xs text-[var(--color-text)] opacity-40">Translation Status</dt>
                    <dd class="text-sm text-[var(--color-text)]">{{ titleDetails.statusTranslation || 'N/A' }}</dd>
                  </div>
                  <div v-if="titleDetails.categories?.length">
                    <dt class="text-xs text-[var(--color-text)] opacity-40 mb-1">Categories</dt>
                    <dd class="flex flex-wrap gap-1">
                      <span v-for="c in titleDetails.categories" :key="c.id" class="text-xs px-2 py-0.5 rounded-full bg-blue-900/50 text-blue-300 border border-blue-700/40">{{ c.name }}</span>
                    </dd>
                  </div>
                  <div v-if="titleDetails.tags?.length">
                    <dt class="text-xs text-[var(--color-text)] opacity-40 mb-1">Tags</dt>
                    <dd class="flex flex-wrap gap-1">
                      <span v-for="t in titleDetails.tags" :key="t.id" class="text-xs px-2 py-0.5 rounded-full bg-purple-900/50 text-purple-300 border border-purple-700/40">{{ t.name }}</span>
                    </dd>
                  </div>
                </dl>
              </div>
            </div>

            <!-- Description -->
            <div v-if="titleDetails.description">
              <h4 class="text-xs font-semibold text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-2">Description</h4>
              <p class="text-sm text-[var(--color-text)] bg-[var(--color-background-mute)] p-3 rounded-lg border border-[var(--color-border)] leading-relaxed">
                {{ titleDetails.description }}
              </p>
            </div>

            <!-- Cover / background images -->
            <div v-if="titleDetails.coverImagePath || titleDetails.backgroundImagePath" class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div v-if="titleDetails.coverImagePath">
                <h4 class="text-xs font-semibold text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-2">Cover</h4>
                <img :src="titleDetails.coverImagePath" alt="Cover" class="max-w-full h-auto rounded-lg border border-[var(--color-border)]">
              </div>
              <div v-if="titleDetails.backgroundImagePath">
                <h4 class="text-xs font-semibold text-[var(--color-text)] opacity-50 uppercase tracking-wider mb-2">Background</h4>
                <img :src="titleDetails.backgroundImagePath" alt="Background" class="max-w-full h-auto rounded-lg border border-[var(--color-border)]">
              </div>
            </div>
          </div>

          <!-- Modal footer -->
          <div class="px-6 py-4 border-t border-[var(--color-border)] flex justify-end gap-3 sticky bottom-0 bg-[var(--color-background-soft)]">
            <button @click="closeDetailsModal" class="px-4 py-2 rounded-lg text-sm font-medium text-[var(--color-text)] border border-[var(--color-border)] bg-[var(--color-background)] hover:bg-[var(--color-background-mute)] transition-colors">
              Close
            </button>
            <button @click="rejectTitle(titleDetails.id)" :disabled="isProcessing"
                    class="px-4 py-2 rounded-lg text-sm font-medium text-white bg-red-600 hover:bg-red-700 disabled:opacity-50 transition-colors">
              Reject
            </button>
            <button @click="acceptTitle(titleDetails.id)" :disabled="isProcessing"
                    class="px-4 py-2 rounded-lg text-sm font-medium text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 transition-colors">
              Accept Title
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
  import { ref, computed, onMounted } from 'vue'
  import adminApi from '../../services/adminApi.js'
  import { titleDetailsService } from '../../services/titleDetailsService.js'
  import { buildTitleSlug } from '@/utils/titleSlug.js'

  // ── State ─────────────────────────────────────────────────────────────────────
  const pendingTitles = ref([])
  const isLoading = ref(true)
  const error = ref('')
  const successMessage = ref('')
  const isProcessing = ref(false)

  const showDetailsModal = ref(false)
  const titleDetails = ref(null)
  const currentSimilarities = ref([])

  // Cache: titleId → array of matches (or empty array when checked and clean)
  const similarityCache = ref({})

  // ── Type helpers ──────────────────────────────────────────────────────────────
  const TYPE_NAMES = { 1: 'Manga', 2: 'Manhwa', 3: 'Manhua', 4: 'Comic', 5: 'Webtoon' }
  const TYPE_COLORS = {
    1: 'bg-red-900/40 text-red-300 border border-red-700/40',
    2: 'bg-blue-900/40 text-blue-300 border border-blue-700/40',
    3: 'bg-yellow-900/40 text-yellow-300 border border-yellow-700/40',
    4: 'bg-purple-900/40 text-purple-300 border border-purple-700/40',
    5: 'bg-green-900/40 text-green-300 border border-green-700/40',
  }
  const getTypeName = (t) => TYPE_NAMES[t] || 'Unknown'
  const getTypeColor = (t) => TYPE_COLORS[t] || 'bg-gray-900/40 text-gray-300'

  // ── Similarity helpers ────────────────────────────────────────────────────────
  function getSimilarityLevel(titleId) {
    const matches = similarityCache.value[titleId]
    if (!matches?.length) return null
    if (matches.some(m => m.matchLevel === 'exact')) return 'exact'
    if (matches.some(m => m.matchLevel === 'similar')) return 'similar'
    return 'partial'
  }

  function badgeClass(level) {
    if (level === 'exact') return 'bg-red-900/50 text-red-300 border border-red-600/40'
    if (level === 'similar') return 'bg-yellow-900/50 text-yellow-300 border border-yellow-600/40'
    return 'bg-blue-900/50 text-blue-300 border border-blue-600/40'
  }

  function buildSlug(name, id) {
    return buildTitleSlug(name, id)
  }

  // ── Data loading ──────────────────────────────────────────────────────────────
  const loadPendingTitles = async () => {
    isLoading.value = true
    error.value = ''
    try {
      const result = await adminApi.getPendingTitles()
      if (result.success) {
        pendingTitles.value = result.data
        // Start similarity checks in the background for all pending titles
        result.data.forEach(t => checkSimilarityForTitle(t))
      } else {
        error.value = result.error
      }
    } catch (err) {
      error.value = 'Failed to load pending titles'
    } finally {
      isLoading.value = false
    }
  }

  // Run similarity check and populate cache
  const checkSimilarityForTitle = async (title) => {
    try {
      const result = await titleDetailsService.checkSimilarity(
        title.originalTitle,
        title.englishTitle,
        title.alternativeNames || ''
      )
      similarityCache.value = {
        ...similarityCache.value,
        [title.id]: result.success ? (result.data.matches || []) : []
      }
    } catch {
      similarityCache.value = { ...similarityCache.value, [title.id]: [] }
    }
  }

  // ── Modal ─────────────────────────────────────────────────────────────────────
  const viewTitleDetails = async (title) => {
    try {
      const result = await adminApi.getPendingTitleDetails(title.id)
      if (result.success) {
        titleDetails.value = result.data
        currentSimilarities.value = similarityCache.value[title.id] || []
        showDetailsModal.value = true
      } else {
        error.value = result.error
      }
    } catch {
      error.value = 'Failed to load title details'
    }
  }

  const closeDetailsModal = () => {
    showDetailsModal.value = false
    titleDetails.value = null
    currentSimilarities.value = []
  }

  // ── Accept / Reject ───────────────────────────────────────────────────────────
  const acceptTitle = async (titleId) => {
    if (!confirm('Accept this title? It will be published.')) return
    isProcessing.value = true
    try {
      const result = await adminApi.acceptTitle(titleId)
      if (result.success) {
        successMessage.value = result.message || 'Title accepted.'
        pendingTitles.value = pendingTitles.value.filter(t => t.id !== titleId)
        const cache = { ...similarityCache.value }; delete cache[titleId]
        similarityCache.value = cache
        closeDetailsModal()
      } else {
        error.value = result.error
      }
    } catch {
      error.value = 'Failed to accept title'
    } finally {
      isProcessing.value = false
    }
  }

  const rejectTitle = async (titleId) => {
    if (!confirm('Reject this title? It will be moved to rejected.')) return
    isProcessing.value = true
    try {
      const result = await adminApi.rejectTitle(titleId)
      if (result.success) {
        successMessage.value = result.message || 'Title rejected.'
        pendingTitles.value = pendingTitles.value.filter(t => t.id !== titleId)
        const cache = { ...similarityCache.value }; delete cache[titleId]
        similarityCache.value = cache
        closeDetailsModal()
      } else {
        error.value = result.error
      }
    } catch {
      error.value = 'Failed to reject title'
    } finally {
      isProcessing.value = false
    }
  }

  onMounted(loadPendingTitles)
</script>

<style scoped>
  .modal-fade-enter-active, .modal-fade-leave-active {
    transition: opacity 0.2s ease;
  }

  .modal-fade-enter-from, .modal-fade-leave-to {
    opacity: 0;
  }
</style>
