<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">

      <!-- Header -->
      <div class="mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 class="text-3xl font-bold text-[var(--color-heading)]">Translation Requests</h1>
          <p class="text-[var(--color-text)] opacity-60 mt-1">Review and manage community novel requests</p>
        </div>
        <div class="flex gap-2 text-sm">
          <span v-for="s in statusCounts" :key="s.label"
            class="px-3 py-1.5 rounded-lg border border-[var(--color-border)] bg-[var(--color-background-soft)]">
            <span :class="s.color">{{ s.label }}</span>
            <span class="ml-1.5 font-semibold text-[var(--color-heading)]">{{ s.count }}</span>
          </span>
        </div>
      </div>

      <!-- Tab filters -->
      <div class="bg-[var(--color-background-soft)] rounded-xl border border-[var(--color-border)] overflow-hidden">
        <div class="border-b border-[var(--color-border)] overflow-x-auto">
          <nav class="flex min-w-max px-4">
            <button v-for="tab in tabs" :key="tab.value"
              @click="activeTab = tab.value; page = 1"
              :class="[
                'py-3 px-4 border-b-2 font-medium text-sm transition whitespace-nowrap',
                activeTab === tab.value
                  ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
                  : 'border-transparent text-[var(--color-text)] opacity-60 hover:opacity-100'
              ]">
              {{ tab.label }}
            </button>
          </nav>
        </div>

        <!-- Loading -->
        <div v-if="loading" class="py-16 flex justify-center">
          <svg class="animate-spin h-7 w-7 text-[var(--color-accent)]" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
          </svg>
        </div>

        <!-- Empty -->
        <div v-else-if="!requests.length" class="py-16 text-center text-[var(--color-text)] opacity-40">
          No requests in this category.
        </div>

        <!-- Table -->
        <div v-else class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-[var(--color-border)] text-[var(--color-text)] opacity-50">
                <th class="px-4 py-3 text-left font-medium w-8">#</th>
                <th class="px-4 py-3 text-left font-medium">Title</th>
                <th class="px-4 py-3 text-left font-medium">Submitted by</th>
                <th class="px-4 py-3 text-center font-medium">Votes</th>
                <th class="px-4 py-3 text-left font-medium">Status</th>
                <th class="px-4 py-3 text-left font-medium">Date</th>
                <th class="px-4 py-3 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--color-border)]">
              <tr v-for="req in requests" :key="req.id"
                class="hover:bg-[var(--color-background)] transition">
                <td class="px-4 py-3 text-[var(--color-text)] opacity-40">{{ req.id }}</td>

                <!-- Title cell -->
                <td class="px-4 py-3">
                  <div class="flex items-center gap-3">
                    <div class="shrink-0 w-8 h-10 rounded overflow-hidden bg-[var(--color-border)]">
                      <img v-if="req.coverImageUrl" :src="req.coverImageUrl" class="w-full h-full object-cover"/>
                    </div>
                    <div class="min-w-0">
                      <div class="font-medium text-[var(--color-heading)] truncate max-w-[180px]">
                        {{ req.proposedTitle }}
                      </div>
                      <div v-if="req.originalLanguageTitle" class="text-xs text-[var(--color-text)] opacity-40 truncate max-w-[180px]">
                        {{ req.originalLanguageTitle }}
                      </div>
                      <a :href="req.sourceUrl" target="_blank" rel="noopener"
                        class="text-xs text-[var(--color-accent)] hover:underline">
                        View source ↗
                      </a>
                    </div>
                  </div>
                </td>

                <td class="px-4 py-3 text-[var(--color-text)] opacity-70">{{ req.requestedByUserName }}</td>

                <td class="px-4 py-3 text-center">
                  <span class="font-semibold text-[var(--color-heading)]">{{ req.voteCount }}</span>
                </td>

                <td class="px-4 py-3">
                  <span :class="statusClass(req.status)"
                    class="text-xs px-2 py-0.5 rounded-full font-medium">
                    {{ req.status }}
                  </span>
                </td>

                <td class="px-4 py-3 text-xs text-[var(--color-text)] opacity-50">
                  {{ formatDate(req.createdAt) }}
                </td>

                <!-- Actions -->
                <td class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-1.5">

                    <!-- Approve (from Pending) -->
                    <button v-if="req.status === 'Pending'"
                      @click="review(req, 'Approve')"
                      :disabled="actioningId === req.id"
                      class="px-2.5 py-1 rounded bg-blue-500/15 text-blue-400 text-xs font-medium hover:bg-blue-500/25 transition disabled:opacity-40">
                      Approve
                    </button>

                    <!-- Reject -->
                    <button v-if="req.status === 'Pending' || req.status === 'Approved'"
                      @click="openReject(req)"
                      :disabled="actioningId === req.id"
                      class="px-2.5 py-1 rounded bg-red-500/15 text-red-400 text-xs font-medium hover:bg-red-500/25 transition disabled:opacity-40">
                      Reject
                    </button>

                    <!-- PreProcessing -->
                    <button v-if="req.status === 'Approved'"
                      @click="review(req, 'PreProcessing')"
                      :disabled="actioningId === req.id"
                      class="px-2.5 py-1 rounded bg-purple-500/15 text-purple-400 text-xs font-medium hover:bg-purple-500/25 transition disabled:opacity-40">
                      Start
                    </button>

                    <!-- Release -->
                    <button v-if="req.status === 'PreProcessing'"
                      @click="openRelease(req)"
                      :disabled="actioningId === req.id"
                      class="px-2.5 py-1 rounded bg-green-500/15 text-green-400 text-xs font-medium hover:bg-green-500/25 transition disabled:opacity-40">
                      Release
                    </button>

                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="p-4 border-t border-[var(--color-border)] flex justify-center gap-2">
          <button :disabled="page <= 1" @click="page--"
            class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">← Prev</button>
          <span class="px-3 py-1.5 text-sm opacity-60">{{ page }} / {{ totalPages }}</span>
          <button :disabled="page >= totalPages" @click="page++"
            class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">Next →</button>
        </div>
      </div>
    </div>

    <!-- Reject modal -->
    <Teleport to="body">
      <div v-if="rejectModal.open" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
        <div class="bg-[var(--color-background-soft)] rounded-2xl shadow-xl border border-[var(--color-border)] w-full max-w-md p-6">
          <h3 class="text-lg font-bold text-[var(--color-heading)] mb-1">Reject Request</h3>
          <p class="text-sm text-[var(--color-text)] opacity-60 mb-4">
            "{{ rejectModal.req?.proposedTitle }}" — provide a reason for the requester.
          </p>
          <textarea v-model="rejectModal.reason" rows="3" placeholder="Reason for rejection..."
            class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-red-500 resize-none mb-4"/>
          <div class="flex gap-3">
            <button @click="rejectModal.open = false"
              class="flex-1 px-4 py-2 rounded-lg border border-[var(--color-border)] text-sm font-medium hover:bg-[var(--color-background)] transition">
              Cancel
            </button>
            <button @click="confirmReject" :disabled="!rejectModal.reason.trim() || actioningId"
              class="flex-1 px-4 py-2 rounded-lg bg-red-500/80 text-white text-sm font-semibold hover:bg-red-500 transition disabled:opacity-50">
              Confirm Reject
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Release modal -->
    <Teleport to="body">
      <div v-if="releaseModal.open" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
        <div class="bg-[var(--color-background-soft)] rounded-2xl shadow-xl border border-[var(--color-border)] w-full max-w-md p-6">
          <h3 class="text-lg font-bold text-[var(--color-heading)] mb-1">Release Novel</h3>
          <p class="text-sm text-[var(--color-text)] opacity-60 mb-4">
            Enter the Title ID that was created in the system for "{{ releaseModal.req?.proposedTitle }}".
          </p>
          <div class="mb-4">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Title ID *</label>
            <input v-model.number="releaseModal.titleId" type="number" min="1" placeholder="e.g. 42"
              class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]"/>
          </div>
          <div class="flex gap-3">
            <button @click="releaseModal.open = false"
              class="flex-1 px-4 py-2 rounded-lg border border-[var(--color-border)] text-sm font-medium hover:bg-[var(--color-background)] transition">
              Cancel
            </button>
            <button @click="confirmRelease" :disabled="!releaseModal.titleId || actioningId"
              class="flex-1 px-4 py-2 rounded-lg bg-green-500/80 text-white text-sm font-semibold hover:bg-green-500 transition disabled:opacity-50">
              Confirm Release
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import {
  adminGetRequestQueue, adminReviewRequest, adminReleaseRequest
} from '@/services/aiTranslationService';

const requests    = ref([]);
const loading     = ref(false);
const total       = ref(0);
const page        = ref(1);
const PAGE_SIZE   = 50;
const activeTab   = ref('All');
const actioningId = ref(null);

const tabs = [
  { label: 'All',           value: 'All' },
  { label: 'Pending',       value: 'Pending' },
  { label: 'Approved',      value: 'Approved' },
  { label: 'Pre-Processing', value: 'PreProcessing' },
  { label: 'Released',      value: 'Released' },
  { label: 'Rejected',      value: 'Rejected' },
];

const statusCounts = ref([
  { label: 'Pending',  color: 'text-yellow-400', count: 0 },
  { label: 'Approved', color: 'text-blue-400',   count: 0 },
  { label: 'Released', color: 'text-green-400',  count: 0 },
]);

const totalPages = computed(() => Math.ceil(total.value / PAGE_SIZE));

// Modals
const rejectModal  = ref({ open: false, req: null, reason: '' });
const releaseModal = ref({ open: false, req: null, titleId: null });

async function fetchRequests() {
  loading.value = true;
  try {
    const params = { page: page.value, pageSize: PAGE_SIZE, orderBy: 'votes' };
    if (activeTab.value !== 'All') params.status = activeTab.value;
    const res = await adminGetRequestQueue(params);
    requests.value = res.data;
    total.value = parseInt(res.headers['x-total-count'] ?? res.data.length);
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
}

async function review(req, action) {
  actioningId.value = req.id;
  try {
    await adminReviewRequest({ requestId: req.id, action });
    await fetchRequests();
  } catch (e) {
    alert(e.response?.data?.message ?? 'Action failed.');
  } finally {
    actioningId.value = null;
  }
}

function openReject(req)  { rejectModal.value  = { open: true, req, reason: '' }; }
function openRelease(req) { releaseModal.value = { open: true, req, titleId: null }; }

async function confirmReject() {
  const req = rejectModal.value.req;
  actioningId.value = req.id;
  try {
    await adminReviewRequest({ requestId: req.id, action: 'Reject', rejectionReason: rejectModal.value.reason });
    rejectModal.value.open = false;
    await fetchRequests();
  } catch (e) {
    alert(e.response?.data?.message ?? 'Rejection failed.');
  } finally {
    actioningId.value = null;
  }
}

async function confirmRelease() {
  const req = releaseModal.value.req;
  actioningId.value = req.id;
  try {
    await adminReleaseRequest({ requestId: req.id, titleId: releaseModal.value.titleId });
    releaseModal.value.open = false;
    await fetchRequests();
  } catch (e) {
    alert(e.response?.data?.message ?? 'Release failed.');
  } finally {
    actioningId.value = null;
  }
}

function statusClass(s) {
  return {
    Pending:       'bg-yellow-500/15 text-yellow-400',
    Approved:      'bg-blue-500/15 text-blue-400',
    PreProcessing: 'bg-purple-500/15 text-purple-400',
    Released:      'bg-green-500/15 text-green-400',
    Rejected:      'bg-red-500/15 text-red-400',
  }[s] ?? 'bg-gray-500/15 text-gray-400';
}

function formatDate(d) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

watch([activeTab, page], fetchRequests);
onMounted(fetchRequests);
</script>
