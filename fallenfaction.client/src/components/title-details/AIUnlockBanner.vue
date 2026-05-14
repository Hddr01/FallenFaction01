<template>
  <div class="rounded-xl border border-[var(--color-border)] bg-gradient-to-r from-purple-500/5 to-blue-500/5 p-4">

    <!-- Header row -->
    <div class="flex flex-wrap items-center justify-between gap-3 mb-3">
      <div class="flex items-center gap-2">
        <div class="w-7 h-7 rounded-lg bg-purple-500/20 flex items-center justify-center">
          <svg class="w-4 h-4 text-purple-400" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/>
          </svg>
        </div>
        <span class="font-semibold text-[var(--color-heading)] text-sm">AI-Unlock Progress</span>
      </div>

      <!-- Stats -->
      <div class="flex items-center gap-3 text-sm">
        <span class="text-green-400 font-medium">{{ unlockedCount }} unlocked</span>
        <span class="text-[var(--color-text)] opacity-40">·</span>
        <span class="text-red-400 font-medium">{{ lockedCount }} locked</span>
        <span class="text-[var(--color-text)] opacity-40">·</span>
        <span class="text-[var(--color-text)] opacity-60">{{ totalCount }} total</span>
      </div>
    </div>

    <!-- Progress bar -->
    <div class="h-2 rounded-full bg-[var(--color-border)] overflow-hidden mb-3">
      <div class="h-full rounded-full bg-gradient-to-r from-purple-500 to-blue-500 transition-all duration-700"
        :style="{ width: progressPct + '%' }" />
    </div>

    <!-- Info + action row -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <p class="text-xs text-[var(--color-text)] opacity-50">
        Once a chapter is unlocked by anyone, it's free for everyone permanently.
        First 50 chapters are always free.
      </p>

      <!-- Batch unlock button -->
      <div v-if="lockedCount > 0" class="flex items-center gap-2">
        <div v-if="wallet" class="text-xs text-[var(--color-text)] opacity-50">
          Balance:
          <span class="text-slate-300 font-medium">{{ formatBal(wallet.silverBalance) }}S</span>
        </div>

        <button @click="openBatchModal"
          :disabled="!authStore.isAuthenticated"
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-purple-500/20 border border-purple-500/30 text-purple-300 text-xs font-semibold hover:bg-purple-500/30 transition disabled:opacity-40 disabled:cursor-not-allowed">
          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M8 11V7a4 4 0 118 0m-4 8v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2z"/>
          </svg>
          Batch Unlock
        </button>
      </div>

      <div v-else class="text-xs text-green-400 font-medium flex items-center gap-1">
        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
        </svg>
        All chapters unlocked!
      </div>
    </div>
  </div>

  <!-- Batch Unlock Modal -->
  <Teleport to="body">
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div class="bg-[var(--color-background-soft)] rounded-2xl shadow-2xl border border-[var(--color-border)] w-full max-w-md">

        <!-- Header -->
        <div class="p-5 border-b border-[var(--color-border)] flex items-center justify-between">
          <div>
            <h3 class="font-bold text-[var(--color-heading)]">Batch Unlock Chapters</h3>
            <p class="text-xs text-[var(--color-text)] opacity-50 mt-0.5">
              {{ lockedCount }} chapters still locked
            </p>
          </div>
          <button @click="showModal = false" class="text-[var(--color-text)] opacity-40 hover:opacity-80 transition">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Loading locked chapters -->
        <div v-if="loadingCosts" class="p-8 flex justify-center">
          <svg class="animate-spin h-6 w-6 text-purple-400" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
          </svg>
        </div>

        <div v-else class="p-5 space-y-4">
          <!-- How many to unlock slider -->
          <div>
            <div class="flex justify-between text-sm mb-2">
              <label class="font-medium text-[var(--color-heading)]">Chapters to unlock</label>
              <span class="text-purple-400 font-semibold">{{ batchCount }}</span>
            </div>
            <input type="range" v-model.number="batchCount"
              :min="1" :max="lockedCount" step="1"
              class="w-full accent-purple-500" />
            <div class="flex justify-between text-xs text-[var(--color-text)] opacity-40 mt-1">
              <span>1</span>
              <span>{{ lockedCount }}</span>
            </div>
          </div>

          <!-- Cost summary -->
          <div class="rounded-xl bg-[var(--color-background)] border border-[var(--color-border)] p-4 space-y-2">
            <div class="flex justify-between text-sm">
              <span class="text-[var(--color-text)] opacity-60">Est. total cost</span>
              <span class="font-bold text-purple-400">≈ {{ formatBal(estimatedCost) }} tickets</span>
            </div>
            <div class="flex justify-between text-sm">
              <span class="text-[var(--color-text)] opacity-60">Silver balance</span>
              <span :class="wallet && wallet.silverBalance >= estimatedCost ? 'text-green-400' : 'text-[var(--color-text)]'">
                {{ formatBal(wallet?.silverBalance ?? 0) }}
              </span>
            </div>
            <div class="border-t border-[var(--color-border)] pt-2 flex justify-between text-sm font-medium">
              <span class="text-[var(--color-text)] opacity-70">After unlock</span>
              <span :class="hasEnough ? 'text-green-400' : 'text-red-400'">
                {{ hasEnough ? 'Sufficient balance ✓' : 'Insufficient balance ✗' }}
              </span>
            </div>
          </div>

          <p class="text-xs text-[var(--color-text)] opacity-40 leading-relaxed">
            Cost per chapter = (CharacterCount + 500) × 0.0012, minimum 1 ticket.
            Unlocks happen one by one — if balance runs out, remaining chapters stay locked.
          </p>

          <!-- Error -->
          <p v-if="batchError" class="text-red-400 text-sm">{{ batchError }}</p>

          <!-- Progress during batch -->
          <div v-if="batchRunning" class="space-y-2">
            <div class="flex justify-between text-xs text-[var(--color-text)] opacity-60">
              <span>Unlocking {{ batchDone }} / {{ batchCount }}...</span>
              <span>{{ Math.round((batchDone / batchCount) * 100) }}%</span>
            </div>
            <div class="h-1.5 rounded-full bg-[var(--color-border)] overflow-hidden">
              <div class="h-full rounded-full bg-purple-500 transition-all"
                :style="{ width: (batchDone / batchCount * 100) + '%' }" />
            </div>
          </div>

          <!-- Actions -->
          <div class="flex gap-3 pt-1">
            <button @click="showModal = false" :disabled="batchRunning"
              class="flex-1 px-4 py-2 rounded-lg border border-[var(--color-border)] text-sm font-medium hover:bg-[var(--color-background)] transition disabled:opacity-40">
              Cancel
            </button>
            <button @click="runBatchUnlock"
              :disabled="!hasEnough || batchRunning || batchCount < 1"
              class="flex-1 px-4 py-2 rounded-lg bg-purple-500/80 hover:bg-purple-500 text-white text-sm font-semibold transition disabled:opacity-40">
              {{ batchRunning ? `Unlocking (${batchDone}/${batchCount})…` : `Unlock ${batchCount} chapter${batchCount !== 1 ? 's' : ''}` }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { getWallet, unlockChapter, getUnlockCost } from '@/services/aiTranslationService'
import { useAuthStore } from '@/stores/authStore'

const props = defineProps({
  titleId:       { type: Number, required: true },
  lockedCount:   { type: Number, default: 0 },
  unlockedCount: { type: Number, default: 0 },
  totalCount:    { type: Number, default: 0 },
})

const emit = defineEmits(['unlocked'])

const authStore   = useAuthStore()
const wallet      = ref(null)
const showModal   = ref(false)
const loadingCosts = ref(false)
const batchCount  = ref(1)
const batchError  = ref('')
const batchRunning = ref(false)
const batchDone   = ref(0)
const lockedChapterIds = ref([])   // array of {id, cost} loaded when modal opens

const progressPct = computed(() =>
  props.totalCount > 0
    ? Math.round((props.unlockedCount / props.totalCount) * 100)
    : 0
)

const estimatedCost = computed(() => {
  const sample = lockedChapterIds.value.slice(0, batchCount.value)
  if (sample.length === 0) return batchCount.value * 1.5  // rough estimate before load
  return sample.reduce((s, c) => s + c.cost, 0)
})

const hasEnough = computed(() =>
  (wallet.value?.silverBalance ?? 0) >= estimatedCost.value
)

function formatBal(n) {
  return n == null ? '0' : Number(n).toFixed(Number(n) % 1 === 0 ? 0 : 2)
}

async function fetchWallet() {
  if (!authStore.isAuthenticated) return
  try {
    const res = await getWallet()
    wallet.value = res.data
  } catch {}
}

async function openBatchModal() {
  if (!authStore.isAuthenticated) return
  showModal.value = true
  batchCount.value = Math.min(props.lockedCount, 10)
  batchError.value = ''
  batchDone.value  = 0

  // Fetch locked chapters + their costs from the API
  await loadLockedChapterCosts()
}

async function loadLockedChapterCosts() {
  loadingCosts.value = true
  try {
    // Get chapter list for this title
    const res = await fetch(
      `${import.meta.env.VITE_API_BASE_URL ?? '/api'}/Titles/${props.titleId}/chapters`,
      { headers: { Authorization: `Bearer ${localStorage.getItem('authToken')}` } }
    )
    const chapters = await res.json()
    const locked = chapters.filter(c => c.isAILocked)

    // Compute costs client-side using the same formula as backend
    lockedChapterIds.value = locked.map(c => ({
      id: c.id,
      cost: Math.max(1, Math.round((c.characterCount + 500) * 0.0012 * 100) / 100),
      name: c.name,
    }))
  } catch (e) {
    batchError.value = 'Could not load chapter data. Try again.'
  } finally {
    loadingCosts.value = false
  }
}

async function runBatchUnlock() {
  if (batchRunning.value) return
  batchRunning.value = true
  batchError.value   = ''
  batchDone.value    = 0

  const toUnlock = lockedChapterIds.value.slice(0, batchCount.value)

  for (const ch of toUnlock) {
    try {
      const res = await unlockChapter(ch.id)
      if (res.data.success) {
        batchDone.value++
        // Update local wallet
        if (wallet.value) {
          wallet.value.silverBalance = res.data.newSilverBalance
        }
        emit('unlocked', ch.id)
      } else {
        batchError.value = res.data.message ?? 'Unlock failed.'
        break
      }
    } catch (e) {
      batchError.value = e.response?.data ?? 'Insufficient tickets or error.'
      break
    }
  }

  batchRunning.value = false

  if (batchDone.value > 0) {
    // Small delay so user sees 100% before close
    setTimeout(() => {
      showModal.value = false
      emit('unlocked')
    }, 800)
  }
}

onMounted(fetchWallet)
watch(() => authStore.isAuthenticated, fetchWallet)
</script>
