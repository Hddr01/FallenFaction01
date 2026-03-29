<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Header -->
      <div class="mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-3xl font-bold text-[var(--color-heading)]">My Requests</h1>
          <p class="text-[var(--color-text)] opacity-70 mt-1">Track your novel translation requests</p>
        </div>
        <button @click="showForm = true"
          class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-[var(--color-accent)] text-white font-semibold hover:opacity-90 transition">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          New Request
        </button>
      </div>

      <!-- Status tabs -->
      <div class="bg-[var(--color-background-soft)] rounded-xl border border-[var(--color-border)] shadow overflow-hidden">
        <div class="border-b border-[var(--color-border)] overflow-x-auto">
          <nav class="flex min-w-max px-4">
            <button v-for="tab in tabs" :key="tab.value"
              @click="activeTab = tab.value"
              :class="[
                'py-3 px-4 border-b-2 font-medium text-sm transition-colors whitespace-nowrap',
                activeTab === tab.value
                  ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
                  : 'border-transparent text-[var(--color-text)] opacity-60 hover:opacity-100'
              ]">
              {{ tab.label }}
              <span v-if="tab.count !== null"
                class="ml-1.5 px-1.5 py-0.5 rounded-full text-xs font-semibold"
                :class="activeTab === tab.value ? 'bg-[var(--color-accent)] text-white' : 'bg-[var(--color-border)] text-[var(--color-text)]'">
                {{ tab.count }}
              </span>
            </button>
          </nav>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="py-16 flex justify-center">
          <svg class="animate-spin h-8 w-8 text-[var(--color-accent)]" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
          </svg>
        </div>

        <!-- Empty state -->
        <div v-else-if="!requests.length" class="py-20 text-center">
          <svg class="mx-auto h-14 w-14 text-[var(--color-text)] opacity-20 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
          <p class="text-[var(--color-text)] opacity-50 text-lg">You have not made any requests yet.</p>
          <button @click="showForm = true"
            class="mt-4 text-[var(--color-accent)] hover:underline text-sm font-medium">
            Submit your first request →
          </button>
        </div>

        <!-- Request list -->
        <div v-else class="divide-y divide-[var(--color-border)]">
          <div v-for="req in requests" :key="req.id"
            class="flex items-start gap-4 p-5 hover:bg-[var(--color-background)] transition">

            <!-- Cover placeholder -->
            <div class="shrink-0 w-12 h-16 rounded overflow-hidden bg-[var(--color-border)]">
              <img v-if="req.coverImageUrl" :src="req.coverImageUrl"
                class="w-full h-full object-cover" :alt="req.proposedTitle" />
              <div v-else class="w-full h-full flex items-center justify-center text-[var(--color-text)] opacity-30">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
                </svg>
              </div>
            </div>

            <!-- Info -->
            <div class="flex-1 min-w-0">
              <div class="flex flex-wrap items-center gap-2 mb-1">
                <h3 class="font-semibold text-[var(--color-heading)] truncate">{{ req.proposedTitle }}</h3>
                <span :class="statusClass(req.status)" class="text-xs px-2 py-0.5 rounded-full font-medium">
                  {{ req.status }}
                </span>
              </div>
              <p v-if="req.originalLanguageTitle" class="text-sm text-[var(--color-text)] opacity-60 mb-1">
                {{ req.originalLanguageTitle }}
              </p>
              <div class="flex flex-wrap gap-3 text-xs text-[var(--color-text)] opacity-50">
                <span>Submitted {{ formatDate(req.createdAt) }}</span>
                <span v-if="req.voteCount > 0">● {{ req.voteCount }} vote{{ req.voteCount !== 1 ? 's' : '' }}</span>
                <span v-if="req.releasedTitleId">
                  ●
                  <router-link :to="`/title/${req.releasedTitleId}`"
                    class="text-[var(--color-accent)] hover:underline ml-0.5">
                    View released title →
                  </router-link>
                </span>
              </div>
              <p v-if="req.rejectionReason" class="mt-1 text-xs text-red-400 italic">
                Rejection reason: {{ req.rejectionReason }}
              </p>
            </div>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="p-4 border-t border-[var(--color-border)] flex justify-center gap-2">
          <button :disabled="page <= 1" @click="page--"
            class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">
            ← Prev
          </button>
          <span class="px-3 py-1.5 text-sm text-[var(--color-text)] opacity-70">
            {{ page }} / {{ totalPages }}
          </span>
          <button :disabled="page >= totalPages" @click="page++"
            class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">
            Next →
          </button>
        </div>
      </div>
    </div>

    <!-- New Request Modal -->
    <Teleport to="body">
      <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
        <div class="bg-[var(--color-background-soft)] rounded-2xl shadow-2xl w-full max-w-xl max-h-[90vh] overflow-y-auto border border-[var(--color-border)]">
          <div class="p-6">
            <div class="flex items-center justify-between mb-5">
              <h2 class="text-xl font-bold text-[var(--color-heading)]">Request Novel Translation</h2>
              <button @click="showForm = false" class="text-[var(--color-text)] opacity-40 hover:opacity-80 transition">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>

            <form @submit.prevent="submitRequest" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Source URL *</label>
                <input v-model="form.sourceUrl" type="url" required placeholder="https://www.biquge.com/..."
                  class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Proposed English Title *</label>
                <input v-model="form.proposedTitle" type="text" required placeholder="Solo Leveling"
                  class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Original Title</label>
                <input v-model="form.originalLanguageTitle" type="text" placeholder="나 혼자만 레벨업"
                  class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
              </div>
              <div>
                <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Synopsis</label>
                <textarea v-model="form.description" rows="3" placeholder="Brief description..."
                  class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] resize-none"/>
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Genres * <span class="opacity-50">(comma-sep)</span></label>
                  <input v-model="form.genres" required placeholder="Action, Fantasy"
                    class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Tags * <span class="opacity-50">(min 2)</span></label>
                  <input v-model="form.tags" required placeholder="isekai, magic system"
                    class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
                </div>
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Cover Image URL</label>
                  <input v-model="form.coverImageUrl" type="url"
                    class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Est. Chapter Count</label>
                  <input v-model.number="form.estimatedChapterCount" type="number" min="1"
                    class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
                </div>
              </div>

              <p v-if="formError" class="text-red-400 text-sm">{{ formError }}</p>

              <div class="flex gap-3 pt-2">
                <button type="button" @click="showForm = false"
                  class="flex-1 px-4 py-2 rounded-lg border border-[var(--color-border)] text-sm font-medium text-[var(--color-text)] hover:bg-[var(--color-background)] transition">
                  Cancel
                </button>
                <button type="submit" :disabled="submitting"
                  class="flex-1 px-4 py-2 rounded-lg bg-[var(--color-accent)] text-white text-sm font-semibold hover:opacity-90 transition disabled:opacity-50">
                  {{ submitting ? 'Submitting...' : 'Submit Request' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { getMyRequests, createTranslationRequest } from '@/services/aiTranslationService';

const requests  = ref([]);
const loading   = ref(false);
const total     = ref(0);
const page      = ref(1);
const pageSize  = 20;
const activeTab = ref('All');
const showForm  = ref(false);
const submitting = ref(false);
const formError  = ref('');

const tabs = [
  { label: 'All',           value: 'All',           count: null },
  { label: 'Pending',       value: 'Pending',       count: null },
  { label: 'Approved',      value: 'Approved',      count: null },
  { label: 'Released',      value: 'Released',      count: null },
  { label: 'Rejected',      value: 'Rejected',      count: null },
  { label: 'Pre-Processing', value: 'PreProcessing', count: null },
];

const totalPages = computed(() => Math.ceil(total.value / pageSize));

const form = ref({
  sourceUrl: '', proposedTitle: '', originalLanguageTitle: '',
  description: '', genres: '', tags: '', coverImageUrl: '',
  estimatedChapterCount: null,
});

async function fetchRequests() {
  loading.value = true;
  try {
    const params = { page: page.value, pageSize };
    if (activeTab.value !== 'All') params.status = activeTab.value;
    const res = await getMyRequests(params);
    requests.value = res.data;
    total.value = parseInt(res.headers['x-total-count'] ?? res.data.length);
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
}

async function submitRequest() {
  formError.value = '';
  const tagList = form.value.tags.split(',').filter(t => t.trim());
  if (tagList.length < 2) { formError.value = 'Please enter at least 2 tags.'; return; }
  if (!form.value.genres.trim()) { formError.value = 'Please enter at least one genre.'; return; }

  submitting.value = true;
  try {
    await createTranslationRequest(form.value);
    showForm.value = false;
    form.value = { sourceUrl: '', proposedTitle: '', originalLanguageTitle: '', description: '', genres: '', tags: '', coverImageUrl: '', estimatedChapterCount: null };
    await fetchRequests();
  } catch (e) {
    formError.value = e.response?.data?.message ?? e.response?.data ?? 'Submission failed. Please try again.';
  } finally {
    submitting.value = false;
  }
}

function statusClass(status) {
  const map = {
    Pending:       'bg-yellow-500/15 text-yellow-400',
    Approved:      'bg-blue-500/15 text-blue-400',
    PreProcessing: 'bg-purple-500/15 text-purple-400',
    Released:      'bg-green-500/15 text-green-400',
    Rejected:      'bg-red-500/15 text-red-400',
  };
  return map[status] ?? 'bg-gray-500/15 text-gray-400';
}

function formatDate(d) {
  return new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}

watch([activeTab, page], fetchRequests);
onMounted(fetchRequests);
</script>
