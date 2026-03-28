<template>
  <div class="min-h-screen bg-[var(--color-background)] p-6">
    <div class="max-w-7xl mx-auto">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-text)] mb-2">Reports Management</h1>
        <p class="text-[var(--color-text)] opacity-70">Review and resolve user reports</p>
      </div>

      <!-- Stats Cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <div v-for="stat in statCards" :key="stat.label"
             class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-5 cursor-pointer hover:border-[var(--color-accent)] transition-colors"
             :class="{ 'ring-2 ring-[var(--color-accent)]': selectedStatus === stat.filterValue }"
             @click="filterByStatus(stat.filterValue)">
          <p class="text-sm font-medium text-[var(--color-text)] opacity-60">{{ stat.label }}</p>
          <p class="text-2xl font-bold mt-1" :class="stat.color">{{ stat.count }}</p>
        </div>
      </div>

      <!-- Filters -->
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6 mb-6">
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div class="md:col-span-2">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Search</label>
            <input v-model="searchQuery" @input="debouncedSearch" type="text"
                   placeholder="Search by user, description..."
                   class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)]" />
          </div>
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Target Type</label>
            <select v-model="selectedTargetType" @change="fetchReports"
                    class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2">
              <option value="">All Types</option>
              <option value="1">Comment</option>
              <option value="2">Title</option>
              <option value="3">Chapter</option>
              <option value="4">User</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Reason</label>
            <select v-model="selectedReason" @change="fetchReports"
                    class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2">
              <option value="">All Reasons</option>
              <option v-for="r in reasons" :key="r.value" :value="r.value">{{ r.label }}</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Bulk Actions -->
      <div v-if="selectedReports.length > 0" class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl p-4 mb-6 flex items-center justify-between">
        <span class="text-sm text-blue-800 dark:text-blue-300">{{ selectedReports.length }} report(s) selected</span>
        <div class="flex gap-2">
          <button @click="bulkAction('Resolved')" class="px-4 py-2 bg-green-600 text-white rounded-lg text-sm hover:bg-green-700">Resolve All</button>
          <button @click="bulkAction('Dismissed')" class="px-4 py-2 bg-gray-600 text-white rounded-lg text-sm hover:bg-gray-700">Dismiss All</button>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="text-center py-12">
        <div class="animate-spin w-8 h-8 border-2 border-[var(--color-accent)] border-t-transparent rounded-full mx-auto"></div>
        <p class="mt-4 text-[var(--color-text)] opacity-60">Loading reports...</p>
      </div>

      <!-- Reports Table -->
      <div v-else-if="reports.length > 0" class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl overflow-hidden">
        <table class="w-full">
          <thead>
            <tr class="border-b border-[var(--color-border)]">
              <th class="px-4 py-3 text-left">
                <input type="checkbox" @change="toggleSelectAll" :checked="allSelected"
                       class="rounded border-[var(--color-border)]" />
              </th>
              <th class="px-4 py-3 text-left text-sm font-medium text-[var(--color-text)] opacity-60">Reporter</th>
              <th class="px-4 py-3 text-left text-sm font-medium text-[var(--color-text)] opacity-60">Target</th>
              <th class="px-4 py-3 text-left text-sm font-medium text-[var(--color-text)] opacity-60">Reason</th>
              <th class="px-4 py-3 text-left text-sm font-medium text-[var(--color-text)] opacity-60">Status</th>
              <th class="px-4 py-3 text-left text-sm font-medium text-[var(--color-text)] opacity-60">Date</th>
              <th class="px-4 py-3 text-left text-sm font-medium text-[var(--color-text)] opacity-60">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="report in reports" :key="report.id"
                class="border-b border-[var(--color-border)] hover:bg-[var(--color-background)] transition-colors">
              <td class="px-4 py-3">
                <input type="checkbox" :value="report.id" v-model="selectedReports"
                       class="rounded border-[var(--color-border)]" />
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <img :src="report.reporterAvatar || '/img/default-avatar.png'" class="w-7 h-7 rounded-full" />
                  <span class="text-sm text-[var(--color-text)]">{{ report.reporterUserName || 'Unknown' }}</span>
                </div>
              </td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                      :class="targetBadgeClass(report.targetType)">
                  {{ report.targetTypeName }}
                </span>
                <p class="text-xs text-[var(--color-text)] opacity-60 mt-1 max-w-[200px] truncate">
                  {{ report.targetPreview || report.targetUserName || '—' }}
                </p>
              </td>
              <td class="px-4 py-3">
                <span class="text-sm text-[var(--color-text)]">{{ report.reasonName }}</span>
              </td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center px-2 py-1 rounded text-xs font-medium"
                      :class="statusBadgeClass(report.statusName)">
                  {{ report.statusName }}
                </span>
              </td>
              <td class="px-4 py-3 text-sm text-[var(--color-text)] opacity-60">
                {{ formatDate(report.createdAt) }}
              </td>
              <td class="px-4 py-3">
                <div class="flex gap-1">
                  <button @click="openReviewModal(report)" title="Review"
                          class="p-2 rounded-lg hover:bg-[var(--color-background)] text-[var(--color-text)] opacity-60 hover:opacity-100">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  </button>
                  <button v-if="report.statusName === 'Pending'" @click="quickResolve(report.id, 'Resolved')" title="Resolve"
                          class="p-2 rounded-lg hover:bg-green-100 dark:hover:bg-green-900/30 text-green-600">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                    </svg>
                  </button>
                  <button v-if="report.statusName === 'Pending'" @click="quickResolve(report.id, 'Dismissed')" title="Dismiss"
                          class="p-2 rounded-lg hover:bg-red-100 dark:hover:bg-red-900/30 text-red-600">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Empty State -->
      <div v-else class="text-center py-16 bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl">
        <svg class="w-16 h-16 mx-auto text-[var(--color-text)] opacity-20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="mt-4 text-lg text-[var(--color-text)] opacity-60">No reports found</p>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex justify-center items-center gap-2 mt-6">
        <button @click="changePage(page - 1)" :disabled="page <= 1"
                class="px-4 py-2 rounded-lg border border-[var(--color-border)] text-[var(--color-text)] disabled:opacity-30">
          Prev
        </button>
        <span class="text-sm text-[var(--color-text)]">Page {{ page }} of {{ totalPages }}</span>
        <button @click="changePage(page + 1)" :disabled="page >= totalPages"
                class="px-4 py-2 rounded-lg border border-[var(--color-border)] text-[var(--color-text)] disabled:opacity-30">
          Next
        </button>
      </div>

      <!-- Review Modal -->
      <div v-if="reviewModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" @click.self="reviewModal = null">
        <div class="bg-[var(--color-background)] border border-[var(--color-border)] rounded-2xl p-6 max-w-lg w-full max-h-[80vh] overflow-y-auto">
          <h2 class="text-xl font-bold text-[var(--color-text)] mb-4">Review Report #{{ reviewModal.id }}</h2>

          <div class="space-y-3 mb-6">
            <div><span class="text-sm opacity-60">Reporter:</span> <span class="text-sm font-medium">{{ reviewModal.reporterUserName }}</span></div>
            <div><span class="text-sm opacity-60">Target:</span> <span class="text-sm font-medium">{{ reviewModal.targetTypeName }}</span></div>
            <div v-if="reviewModal.targetPreview">
              <span class="text-sm opacity-60">Preview:</span>
              <p class="text-sm mt-1 p-3 bg-[var(--color-background-soft)] rounded-lg">{{ reviewModal.targetPreview }}</p>
            </div>
            <div><span class="text-sm opacity-60">Reason:</span> <span class="text-sm font-medium">{{ reviewModal.reasonName }}</span></div>
            <div v-if="reviewModal.description">
              <span class="text-sm opacity-60">Description:</span>
              <p class="text-sm mt-1">{{ reviewModal.description }}</p>
            </div>
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Status</label>
            <select v-model="reviewForm.status"
                    class="w-full bg-[var(--color-background-soft)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2">
              <option value="1">Reviewed</option>
              <option value="2">Resolved</option>
              <option value="3">Dismissed</option>
            </select>
          </div>

          <div class="mb-6">
            <label class="block text-sm font-medium text-[var(--color-text)] mb-2">Admin Note</label>
            <textarea v-model="reviewForm.adminNote" rows="3"
                      class="w-full bg-[var(--color-background-soft)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2"
                      placeholder="Optional note about the resolution..." />
          </div>

          <div class="flex gap-3 justify-end">
            <button @click="reviewModal = null" class="px-4 py-2 rounded-lg border border-[var(--color-border)] text-[var(--color-text)]">Cancel</button>
            <button @click="submitReview" class="px-4 py-2 rounded-lg bg-[var(--color-accent)] text-white">Submit Review</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import reportsService from '../../services/reportsService';

const reports = ref([]);
const loading = ref(true);
const page = ref(1);
const totalPages = ref(1);
const totalCount = ref(0);
const searchQuery = ref('');
const selectedStatus = ref('0'); // Pending by default
const selectedTargetType = ref('');
const selectedReason = ref('');
const selectedReports = ref([]);
const reviewModal = ref(null);
const reviewForm = ref({ status: 2, adminNote: '' });
const counts = ref({ pending: 0, reviewed: 0, resolved: 0, dismissed: 0, total: 0 });

let searchTimeout = null;

const reasons = [
  { value: 1, label: 'Spam' },
  { value: 2, label: 'Harassment' },
  { value: 3, label: 'Inappropriate Content' },
  { value: 4, label: 'Spoiler' },
  { value: 5, label: 'Copyright Violation' },
  { value: 6, label: 'Misinformation' },
  { value: 7, label: 'Hate Speech' },
  { value: 99, label: 'Other' },
];

const statCards = computed(() => [
  { label: 'Pending', count: counts.value.pending, color: 'text-amber-500', filterValue: '0' },
  { label: 'Reviewed', count: counts.value.reviewed, color: 'text-blue-500', filterValue: '1' },
  { label: 'Resolved', count: counts.value.resolved, color: 'text-green-500', filterValue: '2' },
  { label: 'Dismissed', count: counts.value.dismissed, color: 'text-gray-500', filterValue: '3' },
]);

const allSelected = computed(() =>
  reports.value.length > 0 && selectedReports.value.length === reports.value.length
);

function targetBadgeClass(type) {
  const map = { 1: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
                 2: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300',
                 3: 'bg-teal-100 text-teal-800 dark:bg-teal-900/30 dark:text-teal-300',
                 4: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300' };
  return map[type] || 'bg-gray-100 text-gray-800';
}

function statusBadgeClass(status) {
  const map = { Pending: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300',
                Reviewed: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
                Resolved: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
                Dismissed: 'bg-gray-100 text-gray-800 dark:bg-gray-700/30 dark:text-gray-300' };
  return map[status] || '';
}

function formatDate(d) {
  if (!d) return '';
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function filterByStatus(val) {
  selectedStatus.value = selectedStatus.value === val ? '' : val;
  page.value = 1;
  fetchReports();
}

function debouncedSearch() {
  clearTimeout(searchTimeout);
  searchTimeout = setTimeout(() => { page.value = 1; fetchReports(); }, 400);
}

function changePage(p) {
  page.value = p;
  fetchReports();
}

function toggleSelectAll(e) {
  selectedReports.value = e.target.checked ? reports.value.map(r => r.id) : [];
}

function openReviewModal(report) {
  reviewModal.value = report;
  reviewForm.value = { status: 2, adminNote: '' };
}

async function submitReview() {
  try {
    await reportsService.reviewReport(reviewModal.value.id, reviewForm.value);
    reviewModal.value = null;
    await fetchReports();
    await fetchCounts();
  } catch (err) {
    console.error('Review failed:', err);
  }
}

async function quickResolve(id, statusName) {
  const statusMap = { Reviewed: 1, Resolved: 2, Dismissed: 3 };
  try {
    await reportsService.reviewReport(id, { status: statusMap[statusName] });
    await fetchReports();
    await fetchCounts();
  } catch (err) {
    console.error('Quick resolve failed:', err);
  }
}

async function bulkAction(statusName) {
  const statusMap = { Reviewed: 1, Resolved: 2, Dismissed: 3 };
  try {
    await reportsService.bulkReviewReports({
      reportIds: selectedReports.value,
      status: statusMap[statusName]
    });
    selectedReports.value = [];
    await fetchReports();
    await fetchCounts();
  } catch (err) {
    console.error('Bulk action failed:', err);
  }
}

async function fetchReports() {
  loading.value = true;
  try {
    const data = await reportsService.getAdminReports({
      status: selectedStatus.value,
      targetType: selectedTargetType.value,
      reason: selectedReason.value,
      searchQuery: searchQuery.value,
      page: page.value,
      pageSize: 20
    });
    reports.value = data.reports || [];
    totalCount.value = data.totalCount || 0;
    totalPages.value = data.totalPages || 1;
  } catch (err) {
    console.error('Failed to fetch reports:', err);
  } finally {
    loading.value = false;
  }
}

async function fetchCounts() {
  try {
    counts.value = await reportsService.getReportCounts();
  } catch (err) {
    console.error('Failed to fetch counts:', err);
  }
}

onMounted(async () => {
  await Promise.all([fetchReports(), fetchCounts()]);
});
</script>
