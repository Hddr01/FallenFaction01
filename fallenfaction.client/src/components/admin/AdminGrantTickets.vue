<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 space-y-6">

      <!-- Header -->
      <div>
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Grant Tickets</h1>
        <p class="text-[var(--color-text)] opacity-60 mt-1">Manually award Gold or Silver tickets to users</p>
      </div>

      <!-- Grant form -->
      <div class="rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] p-6">
        <h2 class="font-semibold text-[var(--color-heading)] mb-5">New Grant</h2>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
          <!-- User search -->
          <div class="sm:col-span-2">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">User *</label>
            <div class="relative">
              <input v-model="userSearch" @input="searchUsers" type="text"
                placeholder="Search by username or email…"
                class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
              <!-- Dropdown -->
              <div v-if="userResults.length && !selectedUser"
                class="absolute top-full left-0 right-0 z-20 mt-1 rounded-lg border border-[var(--color-border)] bg-[var(--color-background-soft)] shadow-xl overflow-hidden">
                <button v-for="u in userResults" :key="u.id"
                  @click="selectUser(u)"
                  class="w-full flex items-center gap-3 px-4 py-2.5 hover:bg-[var(--color-background)] transition text-left">
                  <div class="w-7 h-7 rounded-full bg-[var(--color-accent)]/20 flex items-center justify-center text-xs font-bold text-[var(--color-accent)] shrink-0">
                    {{ u.userName?.charAt(0).toUpperCase() }}
                  </div>
                  <div>
                    <div class="text-sm font-medium text-[var(--color-heading)]">{{ u.userName }}</div>
                    <div class="text-xs text-[var(--color-text)] opacity-50">{{ u.email }}</div>
                  </div>
                </button>
              </div>
            </div>
            <!-- Selected user chip -->
            <div v-if="selectedUser" class="mt-2 inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-[var(--color-accent)]/10 border border-[var(--color-accent)]/20 text-sm">
              <span class="font-medium text-[var(--color-accent)]">{{ selectedUser.userName }}</span>
              <span class="text-[var(--color-text)] opacity-50">{{ selectedUser.email }}</span>
              <button @click="clearUser" class="ml-1 text-[var(--color-text)] opacity-40 hover:opacity-80">
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>
          </div>

          <!-- Ticket type -->
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Ticket Type *</label>
            <div class="flex gap-2">
              <button v-for="t in ['Gold', 'Silver']" :key="t"
                @click="form.ticketType = t"
                :class="[
                  'flex-1 py-2 rounded-lg border text-sm font-semibold transition',
                  form.ticketType === t
                    ? t === 'Gold' ? 'bg-yellow-500/20 border-yellow-500/40 text-yellow-400' : 'bg-slate-500/20 border-slate-500/40 text-slate-300'
                    : 'border-[var(--color-border)] text-[var(--color-text)] opacity-50 hover:opacity-80'
                ]">
                {{ t }}
              </button>
            </div>
          </div>

          <!-- Amount -->
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Amount *</label>
            <input v-model.number="form.amount" type="number" min="0.5" step="0.5" placeholder="e.g. 10"
              class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
          </div>

          <!-- Expiry (Silver only) -->
          <div v-if="form.ticketType === 'Silver'">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Expires in (months)</label>
            <input v-model.number="form.expiryMonths" type="number" min="1" max="12" placeholder="3"
              class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
          </div>

          <!-- Description -->
          <div :class="form.ticketType === 'Silver' ? '' : 'sm:col-span-2'">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Reason / Description *</label>
            <input v-model="form.description" type="text" placeholder="e.g. Community contribution reward"
              class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
          </div>
        </div>

        <p v-if="formError" class="text-red-400 text-sm mb-3">{{ formError }}</p>
        <p v-if="successMsg" class="text-green-400 text-sm mb-3">{{ successMsg }}</p>

        <button @click="submitGrant" :disabled="submitting || !canSubmit"
          class="px-6 py-2.5 rounded-lg bg-[var(--color-accent)] text-white font-semibold text-sm hover:opacity-90 transition disabled:opacity-40">
          {{ submitting ? 'Granting…' : `Grant ${form.amount || 0} ${form.ticketType} to ${selectedUser?.userName ?? 'user'}` }}
        </button>
      </div>

      <!-- Recent grants log -->
      <div class="rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] overflow-hidden">
        <div class="px-5 py-4 border-b border-[var(--color-border)]">
          <h2 class="font-semibold text-[var(--color-heading)]">Recent Admin Grants</h2>
        </div>

        <div v-if="logLoading" class="py-10 flex justify-center">
          <svg class="animate-spin h-6 w-6 text-[var(--color-accent)]" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
          </svg>
        </div>

        <div v-else-if="!grantLog.length" class="py-10 text-center text-[var(--color-text)] opacity-40 text-sm">
          No grants yet.
        </div>

        <div v-else class="divide-y divide-[var(--color-border)]">
          <div v-for="tx in grantLog" :key="tx.id" class="flex items-center gap-4 px-5 py-3">
            <div class="shrink-0 w-8 h-8 rounded-full flex items-center justify-center"
              :class="tx.ticketType === 'Gold' ? 'bg-yellow-500/15' : 'bg-slate-500/15'">
              <span class="text-xs font-bold" :class="tx.ticketType === 'Gold' ? 'text-yellow-400' : 'text-slate-300'">
                {{ tx.ticketType === 'Gold' ? 'G' : 'S' }}
              </span>
            </div>
            <div class="flex-1 min-w-0">
              <div class="text-sm text-[var(--color-heading)] truncate">{{ tx.description }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-40">{{ formatDateTime(tx.createdAt) }}</div>
            </div>
            <div class="text-sm font-semibold text-green-400">+{{ tx.amount }}</div>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { adminGrantTickets } from '@/services/aiTranslationService'
import api from '@/services/aiTranslationService'

const userSearch   = ref('')
const userResults  = ref([])
const selectedUser = ref(null)
const searching    = ref(false)
let searchTimeout  = null

const form = ref({ ticketType: 'Gold', amount: 10, description: '', expiryMonths: 3 })
const formError  = ref('')
const successMsg = ref('')
const submitting = ref(false)

const grantLog  = ref([])
const logLoading = ref(false)

const canSubmit = computed(() =>
  selectedUser.value && form.value.amount > 0 && form.value.description.trim()
)

async function searchUsers() {
  clearTimeout(searchTimeout)
  if (userSearch.value.length < 2) { userResults.value = []; return }
  searchTimeout = setTimeout(async () => {
    searching.value = true
    try {
      const res = await api.get('/tickets/admin/search', { params: { q: userSearch.value, limit: 8 } })
      userResults.value = res.data ?? []
    } catch {
      userResults.value = []
    } finally {
      searching.value = false
    }
  }, 300)
}

function selectUser(u) {
  selectedUser.value = u
  userSearch.value = u.userName
  userResults.value = []
}

function clearUser() {
  selectedUser.value = null
  userSearch.value = ''
}

async function submitGrant() {
  formError.value = ''
  successMsg.value = ''
  if (!canSubmit.value) return

  submitting.value = true
  try {
    await adminGrantTickets({
      userId: selectedUser.value.id,
      ticketType: form.value.ticketType,
      amount: form.value.amount,
      description: form.value.description,
      expiryMonths: form.value.ticketType === 'Silver' ? form.value.expiryMonths : null
    })
    successMsg.value = `✓ Granted ${form.value.amount} ${form.value.ticketType} tickets to ${selectedUser.value.userName}.`
    form.value.amount = 10
    form.value.description = ''
    await loadGrantLog()
  } catch (e) {
    formError.value = e.response?.data?.message ?? 'Grant failed. Try again.'
  } finally {
    submitting.value = false
  }
}

async function loadGrantLog() {
  logLoading.value = true
  try {
    const res = await api.get('/tickets/admin/grant-log', { params: { limit: 20 } })
    grantLog.value = res.data ?? []
  } catch {
    grantLog.value = []
  } finally {
    logLoading.value = false
  }
}

function formatDateTime(d) {
  return new Date(d).toLocaleString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

onMounted(loadGrantLog)
</script>
