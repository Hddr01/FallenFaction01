<template>
  <div class="min-h-screen bg-[var(--color-background)] p-6">
    <div class="max-w-5xl mx-auto">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-[var(--color-text)] mb-2">Notifications Management</h1>
        <p class="text-[var(--color-text)] opacity-70">Send global announcements to all users</p>
      </div>

      <!-- Send New Notification -->
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl p-6 mb-8">
        <h2 class="text-lg font-semibold text-[var(--color-text)] mb-4">Send Global Notification</h2>
        <div class="space-y-4">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Type</label>
              <select v-model="form.type"
                      class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2">
                <option :value="10">Announcement</option>
                <option :value="11">Maintenance Notice</option>
                <option :value="12">New Feature</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Title</label>
              <input v-model="form.title" type="text" placeholder="Notification title"
                     class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2" />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Message</label>
            <textarea v-model="form.message" rows="3" placeholder="Write the notification message..."
                      class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2" />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Link URL (optional)</label>
              <input v-model="form.linkUrl" type="text" placeholder="https://..."
                     class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2" />
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Schedule For (optional)</label>
              <input v-model="form.scheduledFor" type="datetime-local"
                     class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2" />
            </div>
            <div>
              <label class="block text-sm font-medium text-[var(--color-text)] mb-1">Expires At (optional)</label>
              <input v-model="form.expiresAt" type="datetime-local"
                     class="w-full bg-[var(--color-background)] border border-[var(--color-border)] text-[var(--color-text)] rounded-lg px-4 py-2" />
            </div>
          </div>
          <div class="flex justify-end">
            <button @click="sendNotification" :disabled="!form.title || !form.message || sending"
                    class="px-6 py-2 bg-[var(--color-accent)] text-white rounded-lg hover:opacity-90 disabled:opacity-50 flex items-center gap-2">
              <svg v-if="sending" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              Send Notification
            </button>
          </div>
        </div>
      </div>

      <!-- Sent Notifications -->
      <div class="bg-[var(--color-background-soft)] border border-[var(--color-border)] rounded-xl overflow-hidden">
        <div class="px-6 py-4 border-b border-[var(--color-border)]">
          <h2 class="text-lg font-semibold text-[var(--color-text)]">Sent Global Notifications</h2>
        </div>

        <div v-if="loading" class="p-8 text-center">
          <div class="animate-spin w-8 h-8 border-2 border-[var(--color-accent)] border-t-transparent rounded-full mx-auto"></div>
        </div>

        <div v-else-if="notifications.length === 0" class="p-12 text-center text-[var(--color-text)] opacity-60">
          No global notifications sent yet.
        </div>

        <div v-else class="divide-y divide-[var(--color-border)]">
          <div v-for="n in notifications" :key="n.id" class="px-6 py-4 flex items-start justify-between gap-4">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                      :class="typeBadge(n.type)">
                  {{ n.typeName }}
                </span>
                <span v-if="isExpired(n)" class="text-xs text-red-500">Expired</span>
                <span v-else-if="isScheduled(n)" class="text-xs text-amber-500">Scheduled</span>
              </div>
              <h3 class="font-medium text-[var(--color-text)]">{{ n.title }}</h3>
              <p class="text-sm text-[var(--color-text)] opacity-60 mt-1">{{ n.message }}</p>
              <p class="text-xs text-[var(--color-text)] opacity-40 mt-2">
                Sent {{ formatDate(n.createdAt) }}
                <template v-if="n.scheduledFor"> · Scheduled for {{ formatDate(n.scheduledFor) }}</template>
                <template v-if="n.expiresAt"> · Expires {{ formatDate(n.expiresAt) }}</template>
              </p>
            </div>
            <button @click="deleteNotification(n.id)"
                    class="p-2 rounded-lg hover:bg-red-100 dark:hover:bg-red-900/30 text-red-500 shrink-0" title="Delete">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import notificationsService from '../../services/notificationsService';

const notifications = ref([]);
const loading = ref(true);
const sending = ref(false);

const form = ref({
  type: 10,
  title: '',
  message: '',
  linkUrl: '',
  scheduledFor: '',
  expiresAt: ''
});

function typeBadge(type) {
  const map = {
    10: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
    11: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300',
    12: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
  };
  return map[type] || 'bg-gray-100 text-gray-800';
}

function isExpired(n) {
  return n.expiresAt && new Date(n.expiresAt) < new Date();
}
function isScheduled(n) {
  return n.scheduledFor && new Date(n.scheduledFor) > new Date();
}

function formatDate(d) {
  if (!d) return '';
  return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

async function sendNotification() {
  sending.value = true;
  try {
    const payload = {
      type: form.value.type,
      title: form.value.title,
      message: form.value.message,
      linkUrl: form.value.linkUrl || null,
      scheduledFor: form.value.scheduledFor ? new Date(form.value.scheduledFor).toISOString() : null,
      expiresAt: form.value.expiresAt ? new Date(form.value.expiresAt).toISOString() : null
    };
    await notificationsService.sendGlobal(payload);
    form.value = { type: 10, title: '', message: '', linkUrl: '', scheduledFor: '', expiresAt: '' };
    await fetchNotifications();
  } catch (err) {
    console.error('Send failed:', err);
  } finally {
    sending.value = false;
  }
}

async function deleteNotification(id) {
  if (!confirm('Delete this notification?')) return;
  try {
    await notificationsService.deleteGlobal(id);
    await fetchNotifications();
  } catch (err) {
    console.error('Delete failed:', err);
  }
}

async function fetchNotifications() {
  loading.value = true;
  try {
    notifications.value = await notificationsService.getGlobalNotifications();
  } catch (err) {
    console.error('Fetch failed:', err);
  } finally {
    loading.value = false;
  }
}

onMounted(fetchNotifications);
</script>
