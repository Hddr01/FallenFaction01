<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Header -->
      <div class="mb-6">
        <h1 class="text-3xl font-bold text-[var(--color-heading)]">Novel Voting</h1>
        <p class="text-[var(--color-text)] opacity-70 mt-1">
          Vote for which novel should be translated next. Highest votes wins!
        </p>
      </div>

      <!-- Stats bar -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <!-- Last released -->
        <div class="rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] p-4">
          <div class="text-xs text-[var(--color-text)] opacity-50 mb-1">Last Released Novel</div>
          <div class="font-semibold text-[var(--color-heading)] text-sm truncate">
            {{ lastReleased?.proposedTitle ?? '—' }}
          </div>
        </div>

        <!-- Countdown -->
        <div class="rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] p-4 text-center">
          <div class="text-xs text-[var(--color-text)] opacity-50 mb-1">Highest voted releases in</div>
          <div class="font-mono text-2xl font-bold text-[var(--color-accent)]">{{ countdown }}</div>
        </div>

        <!-- Your votes -->
        <div class="rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] p-4">
          <div class="text-xs text-[var(--color-text)] opacity-50 mb-1">Voting eligibility</div>
          <div class="flex items-center gap-2">
            <span v-if="wallet?.canVote" class="text-green-400 text-sm font-medium">✓ You can vote</span>
            <span v-else-if="authStore.isAuthenticated" class="text-yellow-400 text-sm">
              Need Level 2 or Patreon
            </span>
            <span v-else class="text-[var(--color-text)] opacity-50 text-sm">Login to vote</span>
          </div>
          <div v-if="!wallet?.canVote && authStore.isAuthenticated" class="mt-1 text-xs text-[var(--color-text)] opacity-40">
            Level {{ wallet?.userLevel ?? 1 }} · {{ wallet?.xpPoints ?? 0 }} / 100 XP
          </div>
        </div>
      </div>

      <!-- Sort & filter bar -->
      <div class="flex flex-wrap items-center justify-between gap-3 mb-4">
        <div class="flex gap-2">
          <button v-for="s in sorts" :key="s.value"
            @click="sortBy = s.value"
            :class="[
              'px-3 py-1.5 rounded-lg text-sm font-medium transition',
              sortBy === s.value
                ? 'bg-[var(--color-accent)] text-white'
                : 'bg-[var(--color-background-soft)] text-[var(--color-text)] border border-[var(--color-border)] hover:border-[var(--color-accent)]'
            ]">
            {{ s.label }}
          </button>
        </div>
        <span class="text-sm text-[var(--color-text)] opacity-50">{{ total }} novels</span>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="py-20 flex justify-center">
        <svg class="animate-spin h-8 w-8 text-[var(--color-accent)]" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
        </svg>
      </div>

      <!-- Empty -->
      <div v-else-if="!requests.length" class="py-20 text-center">
        <p class="text-[var(--color-text)] opacity-40 text-lg">No novels available for voting yet.</p>
        <router-link to="/profile/requests"
          class="mt-3 inline-block text-[var(--color-accent)] hover:underline text-sm font-medium">
          Request a novel to be added →
        </router-link>
      </div>

      <!-- Cards -->
      <div v-else class="space-y-3">
        <div v-for="(req, idx) in requests" :key="req.id"
          class="flex items-start gap-4 rounded-xl bg-[var(--color-background-soft)] border border-[var(--color-border)] p-4 hover:border-[var(--color-accent)]/40 transition group">

          <!-- Rank -->
          <div class="shrink-0 w-8 text-center">
            <span :class="[
              'text-lg font-bold',
              idx === 0 ? 'text-yellow-400' : idx === 1 ? 'text-slate-300' : idx === 2 ? 'text-orange-400' : 'text-[var(--color-text)] opacity-30'
            ]">{{ idx + 1 }}</span>
          </div>

          <!-- Cover -->
          <div class="shrink-0 w-12 h-16 rounded overflow-hidden bg-[var(--color-border)]">
            <img v-if="req.coverImageUrl" :src="req.coverImageUrl" :alt="req.proposedTitle"
              class="w-full h-full object-cover" />
            <div v-else class="w-full h-full flex items-center justify-center">
              <svg class="w-5 h-5 text-[var(--color-text)] opacity-20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
              </svg>
            </div>
          </div>

          <!-- Info -->
          <div class="flex-1 min-w-0">
            <div class="flex flex-wrap items-start justify-between gap-2">
              <div class="min-w-0">
                <h3 class="font-semibold text-[var(--color-heading)] truncate">{{ req.proposedTitle }}</h3>
                <p v-if="req.originalLanguageTitle" class="text-xs text-[var(--color-text)] opacity-50 mt-0.5">
                  {{ req.originalLanguageTitle }}
                </p>
              </div>

              <!-- Vote button -->
              <button @click="handleVote(req)"
                :disabled="!authStore.isAuthenticated || !wallet?.canVote || votingId === req.id"
                :class="[
                  'shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-semibold transition',
                  req.hasUserVoted
                    ? 'bg-[var(--color-accent)] text-white shadow-sm'
                    : 'bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] hover:border-[var(--color-accent)] hover:text-[var(--color-accent)]',
                  (!authStore.isAuthenticated || !wallet?.canVote) && 'opacity-50 cursor-not-allowed'
                ]">
                <svg class="w-3.5 h-3.5" :class="votingId === req.id ? 'animate-spin' : ''"
                  fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path v-if="votingId !== req.id" stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5"
                    d="M5 15l7-7 7 7"/>
                  <path v-else stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
                </svg>
                <span>{{ req.voteCount }}</span>
              </button>
            </div>

            <!-- Tags & genres -->
            <div class="flex flex-wrap gap-1 mt-2">
              <span v-for="genre in req.genres.split(',').slice(0,3)" :key="genre"
                class="text-xs px-2 py-0.5 rounded-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] opacity-70">
                {{ genre.trim() }}
              </span>
            </div>

            <!-- Meta -->
            <div class="flex flex-wrap gap-3 mt-2 text-xs text-[var(--color-text)] opacity-40">
              <span>by {{ req.requestedByUserName }}</span>
              <span v-if="req.estimatedChapterCount">{{ req.estimatedChapterCount }} chapters</span>
              <span>{{ formatDate(req.createdAt) }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="mt-6 flex justify-center gap-2">
        <button :disabled="page <= 1" @click="page--"
          class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">← Prev</button>
        <span class="px-3 py-1.5 text-sm opacity-60">{{ page }} / {{ totalPages }}</span>
        <button :disabled="page >= totalPages" @click="page++"
          class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">Next →</button>
      </div>

      <!-- Login nudge for non-auth -->
      <div v-if="!authStore.isAuthenticated" class="mt-6 rounded-xl border border-[var(--color-border)] p-5 text-center bg-[var(--color-background-soft)]">
        <p class="text-[var(--color-text)] opacity-70 mb-3">
          <router-link to="/account/login" class="text-[var(--color-accent)] hover:underline font-medium">Login</router-link>
          and reach Level 2 to vote on novel requests.
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import { getTranslationRequests, voteOnRequest, getWallet } from '@/services/aiTranslationService';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
const requests  = ref([]);
const loading   = ref(false);
const total     = ref(0);
const page      = ref(1);
const pageSize  = 20;
const sortBy    = ref('votes');
const wallet    = ref(null);
const votingId  = ref(null);
const lastReleased = ref(null);

const sorts = [
  { label: 'Most Voted',  value: 'votes' },
  { label: 'Newest',      value: 'newest' },
];

const totalPages = computed(() => Math.ceil(total.value / pageSize));

// ── Countdown to next 2h release ────────────────────────────────────────────
const countdown = ref('');
let countdownTimer = null;

function startCountdown() {
  function tick() {
    const now   = new Date();
    const next  = new Date(now);
    // Next even 2h mark from midnight UTC
    const totalMinutes = now.getUTCHours() * 60 + now.getUTCMinutes();
    const minutesToNext = 120 - (totalMinutes % 120);
    next.setUTCMinutes(now.getUTCMinutes() + minutesToNext, 0, 0);
    const diff = Math.max(0, next - now);
    const h    = String(Math.floor(diff / 3600000)).padStart(2, '0');
    const m    = String(Math.floor((diff % 3600000) / 60000)).padStart(2, '0');
    const s    = String(Math.floor((diff % 60000) / 1000)).padStart(2, '0');
    countdown.value = `${h}:${m}:${s}`;
  }
  tick();
  countdownTimer = setInterval(tick, 1000);
}

// ── Data ─────────────────────────────────────────────────────────────────────
async function fetchRequests() {
  loading.value = true;
  try {
    const res = await getTranslationRequests({
      status: 'Approved', orderBy: sortBy.value, page: page.value, pageSize
    });
    requests.value = res.data;
    total.value = parseInt(res.headers['x-total-count'] ?? res.data.length);
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
}

async function fetchLastReleased() {
  try {
    const res = await getTranslationRequests({ status: 'Released', orderBy: 'newest', page: 1, pageSize: 1 });
    lastReleased.value = res.data[0] ?? null;
  } catch {}
}

async function fetchWallet() {
  if (!authStore.isAuthenticated) return;
  try {
    const res = await getWallet();
    wallet.value = res.data;
  } catch {}
}

async function handleVote(req) {
  if (!authStore.isAuthenticated || !wallet.value?.canVote || votingId.value) return;
  votingId.value = req.id;
  try {
    const res = await voteOnRequest(req.id);
    req.hasUserVoted = res.data.voted;
    req.voteCount    = res.data.voteCount;
  } catch (e) {
    console.error(e);
  } finally {
    votingId.value = null;
  }
}

function formatDate(d) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

watch([sortBy, page], fetchRequests);

onMounted(() => {
  fetchRequests();
  fetchLastReleased();
  fetchWallet();
  startCountdown();
});

onUnmounted(() => {
  if (countdownTimer) clearInterval(countdownTimer);
});
</script>
