<template>
  <div class="min-h-screen bg-[var(--color-background)] py-8">
    <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">

      <div class="mb-6">
        <h1 class="text-2xl font-bold text-[var(--color-heading)]">Translation Join Requests</h1>
        <p class="text-sm text-[var(--color-text)] opacity-60 mt-1">Teams requesting to co-translate existing titles</p>
      </div>

      <!-- Tab filter -->
      <div class="flex gap-2 mb-6 flex-wrap">
        <button v-for="tab in tabs" :key="tab.value"
          @click="activeTab = tab.value; page = 1"
          :class="['px-3 py-1.5 rounded-full text-xs font-medium transition border',
            activeTab === tab.value
              ? 'bg-[var(--color-accent)] text-white border-transparent'
              : 'border-[var(--color-border)] text-[var(--color-text)] hover:bg-[var(--color-background-mute)]']">
          {{ tab.label }}
        </button>
      </div>

      <!-- Table -->
      <div class="bg-[var(--color-background-soft)] rounded-xl border border-[var(--color-border)] overflow-hidden">
        <div v-if="loading" class="py-16 text-center text-[var(--color-text)] opacity-50">Loading…</div>
        <div v-else-if="requests.length === 0" class="py-16 text-center text-[var(--color-text)] opacity-50">No requests found.</div>
        <table v-else class="w-full text-sm">
          <thead class="border-b border-[var(--color-border)]">
            <tr class="text-[var(--color-text)] opacity-60 text-left">
              <th class="px-4 py-3 font-medium">Title</th>
              <th class="px-4 py-3 font-medium">Requesting Team</th>
              <th class="px-4 py-3 font-medium">Submitted by</th>
              <th class="px-4 py-3 font-medium">Status</th>
              <th class="px-4 py-3 font-medium">Date</th>
              <th class="px-4 py-3 font-medium">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-[var(--color-border)]">
            <tr v-for="req in requests" :key="req.id" class="text-[var(--color-text)]">
              <td class="px-4 py-3">
                <a :href="`/${req.titleId}`" class="hover:underline font-medium">{{ req.titleName }}</a>
              </td>
              <td class="px-4 py-3">{{ req.requestingTeamName }}</td>
              <td class="px-4 py-3 opacity-70">{{ req.requestedByUserName }}</td>
              <td class="px-4 py-3">
                <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(req.status)]">
                  {{ req.status }}
                </span>
              </td>
              <td class="px-4 py-3 opacity-50 text-xs">{{ formatDate(req.createdAt) }}</td>
              <td class="px-4 py-3">
                <div class="flex gap-2">
                  <button v-if="req.status === 'Pending'" @click="openDetail(req)"
                    class="px-2.5 py-1 rounded bg-blue-500/15 text-blue-400 text-xs font-medium hover:bg-blue-500/25 transition">
                    Review
                  </button>
                  <button v-else @click="openDetail(req)"
                    class="px-2.5 py-1 rounded bg-[var(--color-border)] text-[var(--color-text)] opacity-60 text-xs font-medium hover:opacity-100 transition">
                    View
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="mt-4 flex justify-center gap-2">
        <button :disabled="page <= 1" @click="page--"
          class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">← Prev</button>
        <span class="px-3 py-1.5 text-sm opacity-60">{{ page }} / {{ totalPages }}</span>
        <button :disabled="page >= totalPages" @click="page++"
          class="px-3 py-1.5 rounded border border-[var(--color-border)] text-sm disabled:opacity-40">Next →</button>
      </div>
    </div>

    <!-- Detail / Review modal -->
    <Teleport to="body">
      <div v-if="detailModal.open" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
        <div class="bg-[var(--color-background-soft)] rounded-2xl shadow-xl border border-[var(--color-border)] w-full max-w-lg p-6">
          <h3 class="text-lg font-bold text-[var(--color-heading)] mb-4">Join Request — {{ detailModal.req?.requestingTeamName }}</h3>

          <div class="space-y-3 text-sm mb-6">
            <div class="flex justify-between">
              <span class="opacity-60">Title</span>
              <a :href="`/${detailModal.req?.titleId}`" class="font-medium hover:underline">{{ detailModal.req?.titleName }}</a>
            </div>
            <div class="flex justify-between">
              <span class="opacity-60">Team</span>
              <span class="font-medium">{{ detailModal.req?.requestingTeamName }}</span>
            </div>
            <div class="flex justify-between">
              <span class="opacity-60">Submitted by</span>
              <span>{{ detailModal.req?.requestedByUserName }}</span>
            </div>
            <div class="flex justify-between">
              <span class="opacity-60">Status</span>
              <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(detailModal.req?.status)]">{{ detailModal.req?.status }}</span>
            </div>
            <div v-if="detailModal.req?.message" class="pt-2 border-t border-[var(--color-border)]">
              <p class="opacity-60 mb-1">Reason from requester:</p>
              <p class="bg-[var(--color-background)] rounded-lg px-3 py-2">{{ detailModal.req.message }}</p>
            </div>
            <div v-if="detailModal.req?.autoRejectedReason" class="pt-2 border-t border-[var(--color-border)]">
              <p class="text-red-400 text-xs">Auto-rejected: {{ detailModal.req.autoRejectedReason }}</p>
            </div>
            <div v-if="detailModal.req?.rejectionReason" class="pt-2 border-t border-[var(--color-border)]">
              <p class="opacity-60 mb-1">Rejection reason:</p>
              <p class="text-red-300">{{ detailModal.req.rejectionReason }}</p>
            </div>
          </div>

          <!-- Reject reason input -->
          <div v-if="detailModal.req?.status === 'Pending' && detailModal.showReject" class="mb-4">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Rejection reason *</label>
            <textarea v-model="detailModal.rejectReason" rows="2" placeholder="Explain the rejection…"
              class="w-full px-3 py-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-background)] text-[var(--color-text)] text-sm focus:outline-none focus:ring-2 focus:ring-red-500 resize-none"/>
          </div>

          <div v-if="detailModal.error" class="mb-4 px-3 py-2 rounded-lg bg-red-500/10 border border-red-500/30 text-sm text-red-400">{{ detailModal.error }}</div>

          <div class="flex gap-3">
            <button @click="detailModal.open = false"
              class="flex-1 px-4 py-2 rounded-lg border border-[var(--color-border)] text-sm font-medium hover:bg-[var(--color-background)] transition">
              Close
            </button>
            <template v-if="detailModal.req?.status === 'Pending'">
              <button v-if="!detailModal.showReject" @click="detailModal.showReject = true"
                class="px-4 py-2 rounded-lg bg-red-500/80 text-white text-sm font-semibold hover:bg-red-500 transition">
                Reject
              </button>
              <button v-else @click="confirmReject"
                :disabled="!detailModal.rejectReason.trim() || detailModal.actioning"
                class="px-4 py-2 rounded-lg bg-red-500/80 text-white text-sm font-semibold hover:bg-red-500 transition disabled:opacity-50">
                Confirm Reject
              </button>
              <button @click="confirmApprove" :disabled="detailModal.actioning"
                class="px-4 py-2 rounded-lg bg-green-500/80 text-white text-sm font-semibold hover:bg-green-500 transition disabled:opacity-50">
                {{ detailModal.actioning ? '…' : 'Approve' }}
              </button>
            </template>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import apiClient from '@/services/apiClient.js';

const api = apiClient;

const requests  = ref([]);
const loading   = ref(false);
const total     = ref(0);
const page      = ref(1);
const PAGE_SIZE = 50;
const activeTab = ref('Pending');

const tabs = [
  { label: 'All',           value: '' },
  { label: 'Pending',       value: 'Pending' },
  { label: 'Approved',      value: 'Approved' },
  { label: 'Rejected',      value: 'RejectedByAdmin' },
  { label: 'Auto-Rejected', value: 'AutoRejected' },
];

const totalPages = computed(() => Math.ceil(total.value / PAGE_SIZE));

const detailModal = ref({ open: false, req: null, showReject: false, rejectReason: '', actioning: false, error: '' });

async function fetchRequests() {
  loading.value = true;
  try {
    const params = { page: page.value, pageSize: PAGE_SIZE };
    if (activeTab.value) params.status = activeTab.value;
    const res = await api.get('/title-join-requests/admin', { params });
    requests.value = res.data;
    total.value = parseInt(res.headers['x-total-count'] ?? res.data.length);
  } catch (e) { console.error(e); }
  finally { loading.value = false; }
}

function openDetail(req) {
  detailModal.value = { open: true, req, showReject: false, rejectReason: '', actioning: false, error: '' };
}

async function confirmApprove() {
  detailModal.value.actioning = true;
  detailModal.value.error = '';
  try {
    await api.post(`/title-join-requests/${detailModal.value.req.id}/approve`);
    detailModal.value.open = false;
    await fetchRequests();
  } catch (e) {
    detailModal.value.error = e.response?.data?.message ?? 'Approval failed.';
  } finally { detailModal.value.actioning = false; }
}

async function confirmReject() {
  if (!detailModal.value.rejectReason.trim()) return;
  detailModal.value.actioning = true;
  detailModal.value.error = '';
  try {
    await api.post(`/title-join-requests/${detailModal.value.req.id}/reject`, { reason: detailModal.value.rejectReason });
    detailModal.value.open = false;
    await fetchRequests();
  } catch (e) {
    detailModal.value.error = e.response?.data?.message ?? 'Rejection failed.';
  } finally { detailModal.value.actioning = false; }
}

function statusClass(s) {
  return {
    Pending:          'bg-yellow-500/15 text-yellow-400',
    Approved:         'bg-green-500/15 text-green-400',
    RejectedByAdmin:  'bg-red-500/15 text-red-400',
    RejectedByTeam:   'bg-orange-500/15 text-orange-400',
    AutoRejected:     'bg-red-500/15 text-red-400',
  }[s] ?? 'bg-gray-500/15 text-gray-400';
}

function formatDate(d) {
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

watch([activeTab, page], fetchRequests);
onMounted(fetchRequests);
</script>
