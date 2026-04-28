<template>
  <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-background-soft)] overflow-hidden">

    <!-- Header -->
    <div class="px-5 py-4 border-b border-[var(--color-border)] flex items-center gap-3">
      <div class="w-8 h-8 rounded-lg bg-orange-500/15 flex items-center justify-center">
        <svg class="w-4 h-4 text-orange-400" viewBox="0 0 24 24" fill="currentColor">
          <path d="M14.82 2.41C18.78 2.41 22 5.65 22 9.62c0 3.96-3.22 7.19-7.18 7.19-3.95 0-7.17-3.23-7.17-7.19 0-3.97 3.22-7.21 7.17-7.21M2 21.6h3.5V2.41H2V21.6z"/>
        </svg>
      </div>
      <div>
        <h3 class="font-semibold text-[var(--color-heading)]">Patreon</h3>
        <p class="text-xs text-[var(--color-text)] opacity-50">
          Link your Patreon account to get Gold Tickets every month
        </p>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="p-5 flex justify-center">
      <svg class="animate-spin h-5 w-5 text-orange-400" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
      </svg>
    </div>

    <div v-else class="p-5">

      <!-- Linked state -->
      <div v-if="status?.linked">
        <div class="flex items-start justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-1">
              <span class="w-2 h-2 rounded-full bg-green-400 inline-block"></span>
              <span class="text-sm font-medium text-green-400">Connected</span>
            </div>
            <p class="text-sm text-[var(--color-text)] opacity-70">
              Tier: <span class="font-semibold text-orange-400">{{ status.tierName ?? 'Unknown' }}</span>
            </p>
            <p class="text-xs text-[var(--color-text)] opacity-40 mt-0.5">
              Linked {{ formatDate(status.linkedAt) }}
            </p>
          </div>

          <button @click="unlink" :disabled="unlinking"
            class="shrink-0 px-3 py-1.5 rounded-lg border border-[var(--color-border)] text-xs font-medium text-[var(--color-text)] opacity-60 hover:opacity-100 hover:border-red-500/50 hover:text-red-400 transition disabled:opacity-30">
            {{ unlinking ? 'Unlinking…' : 'Unlink' }}
          </button>
        </div>

        <!-- Tier benefits -->
        <div class="mt-4 rounded-lg bg-orange-500/5 border border-orange-500/15 p-3">
          <p class="text-xs text-orange-300/80 font-medium mb-2">Monthly Gold ticket grants by tier:</p>
          <div class="grid grid-cols-3 gap-2">
            <div v-for="tier in tierList" :key="tier.name"
              :class="['rounded-lg p-2 text-center border transition', isCurrentTier(tier.name) ? 'border-orange-400/50 bg-orange-500/15' : 'border-[var(--color-border)] opacity-50']">
              <div class="text-xs font-medium text-[var(--color-heading)]">{{ tier.name }}</div>
              <div class="text-sm font-bold text-yellow-400 mt-0.5">{{ tier.gold }}G</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Unlinked state -->
      <div v-else class="space-y-4">
        <p class="text-sm text-[var(--color-text)] opacity-60 leading-relaxed">
          Support the platform on Patreon and your Gold Tickets are credited automatically each month.
          Gold Tickets never expire and can be used to unlock AI-translated chapters.
        </p>

        <!-- Tier table -->
        <div class="grid grid-cols-3 gap-2">
          <div v-for="tier in tierList" :key="tier.name"
            class="rounded-lg border border-[var(--color-border)] p-3 text-center">
            <div class="text-xs font-medium text-[var(--color-heading)] mb-1">{{ tier.name }}</div>
            <div class="text-lg font-bold text-yellow-400">{{ tier.gold }}<span class="text-xs text-yellow-400/60">G</span></div>
            <div class="text-xs text-[var(--color-text)] opacity-40 mt-0.5">/month</div>
          </div>
        </div>

        <button @click="startLink" :disabled="linking"
          class="w-full flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-orange-500/15 border border-orange-500/30 text-orange-400 font-semibold hover:bg-orange-500/25 transition disabled:opacity-40">
          <svg class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
            <path d="M14.82 2.41C18.78 2.41 22 5.65 22 9.62c0 3.96-3.22 7.19-7.18 7.19-3.95 0-7.17-3.23-7.17-7.19 0-3.97 3.22-7.21 7.17-7.21M2 21.6h3.5V2.41H2V21.6z"/>
          </svg>
          {{ linking ? 'Redirecting to Patreon…' : 'Link Patreon Account' }}
        </button>
      </div>

      <p v-if="error" class="mt-3 text-sm text-red-400">{{ error }}</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/apiClient.js'

const status   = ref(null)
const loading  = ref(false)
const linking  = ref(false)
const unlinking = ref(false)
const error    = ref('')

// Mirror your appsettings.json Patreon:TierMapping here for display
const tierList = [
  { name: 'Supporter', gold: 5  },
  { name: 'Champion',  gold: 15 },
  { name: 'Patron',    gold: 30 },
]

async function fetchStatus() {
  loading.value = true
  try {
    const res = await api.get('/patreon/status')
    status.value = res.data
  } catch { /* not linked yet */ }
  finally { loading.value = false }
}

async function startLink() {
  linking.value = true
  error.value = ''
  try {
    const res = await api.get('/patreon/link')
    window.location.href = res.data.url
  } catch (e) {
    error.value = 'Could not start Patreon link. Try again.'
    linking.value = false
  }
}

async function unlink() {
  unlinking.value = true
  error.value = ''
  try {
    await api.delete('/patreon/unlink')
    status.value = { linked: false }
  } catch (e) {
    error.value = 'Failed to unlink. Try again.'
  } finally {
    unlinking.value = false
  }
}

function isCurrentTier(name) {
  return status.value?.tierName?.includes(name)
}

function formatDate(d) {
  if (!d) return ''
  return new Date(d).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })
}

onMounted(fetchStatus)

// Handle redirect back from Patreon
const params = new URLSearchParams(window.location.search)
if (params.get('patreon') === 'linked') {
  fetchStatus()
  window.history.replaceState({}, '', window.location.pathname)
}
</script>
