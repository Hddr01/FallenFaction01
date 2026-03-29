<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Header -->
      <div class="mb-6">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Ticket Wallet</h1>
        <p class="text-[var(--color-text)] opacity-70 mt-1">
          Manage your Gold and Silver tickets for AI chapter unlocks
        </p>
      </div>

      <!-- Balance cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <!-- Gold -->
        <div class="rounded-xl bg-gradient-to-br from-yellow-500/10 to-yellow-500/5 border border-yellow-500/20 p-5">
          <div class="flex items-center gap-2 mb-3">
            <div class="w-8 h-8 rounded-full bg-yellow-400 flex items-center justify-center font-bold text-black text-sm">G</div>
            <span class="text-sm font-medium text-yellow-400">Gold Tickets</span>
          </div>
          <div class="text-3xl font-bold text-yellow-400">
            {{ loading ? '–' : formatBal(wallet?.goldBalance) }}
          </div>
          <p class="text-xs text-yellow-400/50 mt-1">Never expire · From Patreon</p>
        </div>

        <!-- Silver -->
        <div class="rounded-xl bg-gradient-to-br from-slate-500/10 to-slate-500/5 border border-slate-500/20 p-5">
          <div class="flex items-center gap-2 mb-3">
            <div class="w-8 h-8 rounded-full bg-slate-300 flex items-center justify-center font-bold text-black text-sm">S</div>
            <span class="text-sm font-medium text-slate-300">Silver Tickets</span>
          </div>
          <div class="text-3xl font-bold text-slate-300">
            {{ loading ? '–' : formatBal(wallet?.silverBalance) }}
          </div>
          <p class="text-xs text-slate-400/50 mt-1">Expire after 3 months</p>
        </div>

        <!-- Level + XP -->
        <div class="rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] p-5">
          <div class="flex items-center justify-between mb-3">
            <span class="text-sm font-medium text-[var(--color-text)] opacity-70">Your Level</span>
            <span class="text-xs px-2 py-0.5 rounded-full font-semibold"
              :class="wallet?.canVote ? 'bg-green-500/15 text-green-400' : 'bg-yellow-500/15 text-yellow-400'">
              Lv {{ wallet?.userLevel ?? 1 }}
            </span>
          </div>
          <div class="text-2xl font-bold text-[var(--color-heading)] mb-2">
            {{ levelName(wallet?.userLevel ?? 1) }}
          </div>
          <div class="h-2 rounded-full bg-[var(--color-border)] overflow-hidden mb-1">
            <div class="h-full rounded-full bg-[var(--color-accent)] transition-all"
              :style="{ width: xpProgress + '%' }"/>
          </div>
          <div class="flex justify-between text-xs text-[var(--color-text)] opacity-40">
            <span>{{ wallet?.xpPoints ?? 0 }} XP</span>
            <span>{{ nextLevelXp }} XP</span>
          </div>
          <p v-if="!wallet?.canVote" class="text-xs text-yellow-400/70 mt-2">
            Reach Level 2 to vote on novel requests
          </p>
          <p v-else class="text-xs text-green-400/80 mt-2">✓ Voting unlocked</p>
        </div>
      </div>

      <!-- How to earn -->
      <div class="mb-6 rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] p-4">
        <h3 class="font-semibold text-[var(--color-heading)] mb-3 text-sm">How to get tickets</h3>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div class="flex items-start gap-3">
            <div class="w-8 h-8 rounded-lg bg-yellow-500/15 flex items-center justify-center shrink-0">
              <span class="text-yellow-400 text-xs font-bold">G</span>
            </div>
            <div>
              <div class="text-sm font-medium text-[var(--color-heading)]">Gold Tickets</div>
              <p class="text-xs text-[var(--color-text)] opacity-50 mt-0.5">
                Support on Patreon — your tier grants Gold tickets each month automatically.
              </p>
            </div>
          </div>
          <div class="flex items-start gap-3">
            <div class="w-8 h-8 rounded-lg bg-slate-500/15 flex items-center justify-center shrink-0">
              <span class="text-slate-300 text-xs font-bold">S</span>
            </div>
            <div>
              <div class="text-sm font-medium text-[var(--color-heading)]">Silver Tickets</div>
              <p class="text-xs text-[var(--color-text)] opacity-50 mt-0.5">
                Earned through contributions. Expire after 3 months.
                Admin can also grant Silver for community work.
              </p>
            </div>
          </div>
        </div>
        <a href="https://www.patreon.com" target="_blank" rel="noopener"
          class="mt-3 inline-flex items-center gap-2 text-sm text-orange-400 font-medium hover:underline">
          <svg class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
            <path d="M14.82 2.41C18.78 2.41 22 5.65 22 9.62c0 3.96-3.22 7.19-7.18 7.19-3.95 0-7.17-3.23-7.17-7.19 0-3.97 3.22-7.21 7.17-7.21M2 21.6h3.5V2.41H2V21.6z"/>
          </svg>
          Support on Patreon →
        </a>
      </div>

      <!-- Transaction history -->
      <div class="rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] overflow-hidden">
        <div class="px-5 py-4 border-b border-[var(--color-border)] flex items-center justify-between">
          <h2 class="font-semibold text-[var(--color-heading)]">Transaction History</h2>
          <span class="text-xs text-[var(--color-text)] opacity-40">{{ totalTransactions }} records</span>
        </div>

        <div v-if="txLoading" class="py-12 flex justify-center">
          <svg class="animate-spin h-6 w-6 text-[var(--color-accent)]" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
          </svg>
        </div>

        <div v-else-if="!transactions.length" class="py-16 text-center">
          <p class="text-[var(--color-text)] opacity-40">No transactions yet.</p>
        </div>

        <div v-else class="divide-y divide-[var(--color-border)]">
          <div v-for="tx in transactions" :key="tx.id"
            class="flex items-center gap-4 px-5 py-3 hover:bg-[var(--color-background)] transition">

            <!-- Icon -->
            <div class="shrink-0 w-9 h-9 rounded-full flex items-center justify-center"
              :class="tx.amount > 0 ? 'bg-green-500/15' : 'bg-red-500/15'">
              <span class="text-base" :class="tx.amount > 0 ? 'text-green-400' : 'text-red-400'">
                {{ tx.amount > 0 ? '↓' : '↑' }}
              </span>
            </div>

            <!-- Description -->
            <div class="flex-1 min-w-0">
              <div class="text-sm text-[var(--color-heading)] truncate">{{ tx.description }}</div>
              <div class="text-xs text-[var(--color-text)] opacity-40 mt-0.5">
                {{ formatDateTime(tx.createdAt) }}
                <span v-if="tx.expiresAt" class="ml-2 text-yellow-400/60">
                  · expires {{ formatDate(tx.expiresAt) }}
                </span>
              </div>
            </div>

            <!-- Amount + type -->
            <div class="shrink-0 text-right">
              <div class="text-sm font-semibold"
                :class="tx.amount > 0 ? 'text-green-400' : 'text-red-400'">
                {{ tx.amount > 0 ? '+' : '' }}{{ formatBal(tx.amount) }}
              </div>
              <div class="text-xs mt-0.5"
                :class="tx.ticketType === 'Gold' ? 'text-yellow-400/60' : 'text-slate-400/60'">
                {{ tx.ticketType }}
              </div>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="txTotalPages > 1" class="p-4 border-t border-[var(--color-border)] flex justify-center gap-2">
          <button :disabled="txPage <= 1" @click="txPage--"
            class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">← Prev</button>
          <span class="px-3 py-1.5 text-sm opacity-60">{{ txPage }} / {{ txTotalPages }}</span>
          <button :disabled="txPage >= txTotalPages" @click="txPage++"
            class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">Next →</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { getWallet, getTransactions } from '@/services/aiTranslationService';

const wallet   = ref(null);
const loading  = ref(false);

const transactions     = ref([]);
const txLoading        = ref(false);
const totalTransactions = ref(0);
const txPage           = ref(1);
const TX_PAGE_SIZE     = 20;

const XP_THRESHOLDS  = [0, 100, 300, 700, 1500];
const LEVEL_NAMES    = ['', 'Newcomer', 'Reader', 'Regular', 'Veteran', 'Champion'];

const txTotalPages = computed(() => Math.ceil(totalTransactions.value / TX_PAGE_SIZE));

const nextLevelXp = computed(() => {
  const lv = wallet.value?.userLevel ?? 1;
  return XP_THRESHOLDS[Math.min(lv, XP_THRESHOLDS.length - 1)] ?? 9999;
});

const xpProgress = computed(() => {
  if (!wallet.value) return 0;
  const lv = wallet.value.userLevel;
  const cur = XP_THRESHOLDS[lv - 1] ?? 0;
  const nxt = nextLevelXp.value;
  if (nxt <= cur) return 100;
  return Math.min(100, ((wallet.value.xpPoints - cur) / (nxt - cur)) * 100);
});

function levelName(lv) { return LEVEL_NAMES[lv] ?? 'Unknown'; }
function formatBal(n)   { return n == null ? '0' : Number(n).toFixed(Number(n) % 1 === 0 ? 0 : 2); }
function formatDate(d)  { return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }); }
function formatDateTime(d) {
  return new Date(d).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

async function fetchWallet() {
  loading.value = true;
  try {
    const res = await getWallet();
    wallet.value = res.data;
  } finally {
    loading.value = false;
  }
}

async function fetchTransactions() {
  txLoading.value = true;
  try {
    const res = await getTransactions(txPage.value, TX_PAGE_SIZE);
    transactions.value = res.data;
    totalTransactions.value = parseInt(res.headers['x-total-count'] ?? res.data.length);
  } finally {
    txLoading.value = false;
  }
}

watch(txPage, fetchTransactions);
onMounted(() => { fetchWallet(); fetchTransactions(); });
</script>
