<template>
  <div class="relative" ref="dropdownRef">
    <!-- Navbar badge trigger -->
    <button @click="open = !open"
      class="flex items-center gap-1.5 px-3 py-1.5 rounded-full border border-[var(--color-border)] hover:border-[var(--color-accent)] transition bg-[var(--color-background-soft)]"
      title="Your ticket wallet">
      <!-- Gold -->
      <span class="flex items-center gap-1 text-sm font-semibold text-yellow-400">
        <svg class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
          <circle cx="12" cy="12" r="10"/>
          <text x="12" y="16" text-anchor="middle" font-size="11" fill="var(--color-heading)" font-family="serif" font-weight="bold">G</text>
        </svg>
        {{ wallet ? formatBal(wallet.goldBalance) : '–' }}
      </span>
      <span class="text-[var(--color-border)]">|</span>
      <!-- Silver -->
      <span class="flex items-center gap-1 text-sm font-semibold text-slate-300">
        <svg class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
          <circle cx="12" cy="12" r="10"/>
          <text x="12" y="16" text-anchor="middle" font-size="11" fill="var(--color-heading)" font-family="serif" font-weight="bold">S</text>
        </svg>
        {{ wallet ? formatBal(wallet.silverBalance) : '–' }}
      </span>
    </button>

    <!-- Dropdown panel -->
    <Transition name="fade-slide">
      <div v-if="open"
        class="absolute right-0 mt-2 w-72 rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] shadow-xl z-50 overflow-hidden">

        <!-- Header -->
        <div class="px-4 py-3 border-b border-[var(--color-border)]">
          <div class="flex items-center justify-between mb-2">
            <span class="font-semibold text-[var(--color-heading)] text-sm">Ticket Wallet</span>
            <span class="text-xs px-2 py-0.5 rounded-full font-medium"
              :class="wallet?.canVote ? 'bg-green-500/15 text-green-400' : 'bg-gray-500/15 text-gray-400'">
              Level {{ wallet?.userLevel ?? 1 }}
            </span>
          </div>
          <!-- Balances -->
          <div class="grid grid-cols-2 gap-2">
            <div class="rounded-lg bg-yellow-500/10 border border-yellow-500/20 p-2.5 text-center">
              <div class="text-yellow-400 font-bold text-lg">{{ wallet ? formatBal(wallet.goldBalance) : '–' }}</div>
              <div class="text-yellow-400/70 text-xs mt-0.5">Gold Tickets</div>
            </div>
            <div class="rounded-lg bg-slate-500/10 border border-slate-500/20 p-2.5 text-center">
              <div class="text-slate-300 font-bold text-lg">{{ wallet ? formatBal(wallet.silverBalance) : '–' }}</div>
              <div class="text-slate-400/70 text-xs mt-0.5">Silver Tickets</div>
            </div>
          </div>
        </div>

        <!-- XP progress -->
        <div v-if="wallet" class="px-4 py-3 border-b border-[var(--color-border)]">
          <div class="flex justify-between text-xs text-[var(--color-text)] opacity-60 mb-1.5">
            <span>Level {{ wallet.userLevel }} · {{ wallet.xpPoints }} XP</span>
            <span>→ L{{ wallet.userLevel + 1 }}: {{ nextLevelXp(wallet.userLevel) }} XP</span>
          </div>
          <div class="h-1.5 rounded-full bg-[var(--color-border)] overflow-hidden">
            <div class="h-full rounded-full bg-[var(--color-accent)] transition-all"
              :style="{ width: xpProgress(wallet) + '%' }"/>
          </div>
          <p v-if="!wallet.canVote" class="text-xs text-[var(--color-text)] opacity-50 mt-1.5">
            Reach Level 2 or link Patreon to vote on novel requests
          </p>
          <p v-else class="text-xs text-green-400 mt-1.5">✓ You can vote on translation requests</p>
        </div>

        <!-- Actions -->
        <div class="p-3 flex flex-col gap-1.5">
          <router-link to="/profile/requests"
            class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-[var(--color-background)] transition text-sm text-[var(--color-text)]"
            @click="open = false">
            <svg class="w-4 h-4 opacity-60" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
            </svg>
            My Requests
          </router-link>
          <router-link to="/profile/wallet"
            class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-[var(--color-background)] transition text-sm text-[var(--color-text)]"
            @click="open = false">
            <svg class="w-4 h-4 opacity-60" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"/>
            </svg>
            Transaction History
          </router-link>
          <a href="https://www.patreon.com" target="_blank" rel="noopener"
            class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-[var(--color-background)] transition text-sm text-orange-400 font-medium">
            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
              <path d="M14.82 2.41C18.78 2.41 22 5.65 22 9.62c0 3.96-3.22 7.19-7.18 7.19-3.95 0-7.17-3.23-7.17-7.19 0-3.97 3.22-7.21 7.17-7.21M2 21.6h3.5V2.41H2V21.6z"/>
            </svg>
            Get Tickets via Patreon
          </a>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import { getWallet } from '@/services/aiTranslationService';

const wallet      = ref(null);
const open        = ref(false);
const dropdownRef = ref(null);

const XP_THRESHOLDS = [0, 100, 300, 700, 1500];

function nextLevelXp(level) {
  return XP_THRESHOLDS[Math.min(level, XP_THRESHOLDS.length - 1)] ?? 9999;
}

function xpProgress(w) {
  const currentThreshold = XP_THRESHOLDS[w.userLevel - 1] ?? 0;
  const nextThreshold    = nextLevelXp(w.userLevel);
  if (nextThreshold <= currentThreshold) return 100;
  const pct = ((w.xpPoints - currentThreshold) / (nextThreshold - currentThreshold)) * 100;
  return Math.min(100, Math.max(0, pct));
}

function formatBal(n) {
  return Number(n).toFixed(Number(n) % 1 === 0 ? 0 : 2);
}

function handleOutsideClick(e) {
  if (dropdownRef.value && !dropdownRef.value.contains(e.target)) open.value = false;
}

onMounted(async () => {
  try {
    const res = await getWallet();
    wallet.value = res.data;
  } catch {}
  document.addEventListener('click', handleOutsideClick);
});

onUnmounted(() => document.removeEventListener('click', handleOutsideClick));
</script>

<style scoped>
.fade-slide-enter-active, .fade-slide-leave-active { transition: opacity 0.15s, transform 0.15s; }
.fade-slide-enter-from, .fade-slide-leave-to      { opacity: 0; transform: translateY(-6px); }
</style>
