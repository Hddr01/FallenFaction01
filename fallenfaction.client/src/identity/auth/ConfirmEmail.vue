<template>
  <div class="min-h-screen flex items-center justify-center bg-[var(--color-background)] py-12 px-4">
    <div class="max-w-md w-full text-center space-y-6">

      <!-- Loading -->
      <div v-if="state === 'loading'" class="space-y-4">
        <div class="mx-auto h-16 w-16 flex items-center justify-center rounded-full bg-[var(--color-background-soft)]">
          <svg class="animate-spin h-8 w-8 text-[var(--vt-c-indigo)]" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
        </div>
        <p class="text-[var(--color-text)] opacity-70">Confirming your email…</p>
      </div>

      <!-- Success -->
      <div v-else-if="state === 'success'" class="space-y-6">
        <div class="mx-auto h-16 w-16 flex items-center justify-center rounded-full bg-green-100">
          <svg class="h-8 w-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-[var(--color-heading)]">Email confirmed!</h1>
        <p class="text-[var(--color-text)] opacity-80">{{ message }}</p>
        <router-link to="/account/login"
                     class="inline-block px-6 py-2 bg-[var(--vt-c-indigo)] text-white rounded-lg hover:opacity-90 font-medium">
          Log in
        </router-link>
      </div>

      <!-- Error -->
      <div v-else-if="state === 'error'" class="space-y-6">
        <div class="mx-auto h-16 w-16 flex items-center justify-center rounded-full bg-red-100">
          <svg class="h-8 w-8 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-[var(--color-heading)]">Confirmation failed</h1>
        <p class="text-[var(--color-text)] opacity-80">{{ message }}</p>
        <div class="space-y-3">
          <button @click="handleResend" :disabled="resendCooldown > 0 || resendLoading"
                  class="block w-full px-6 py-2 bg-[var(--vt-c-indigo)] text-white rounded-lg hover:opacity-90 font-medium disabled:opacity-50">
            {{ resendLoading ? 'Sending…' : resendCooldown > 0 ? `Resend (${resendCooldown}s)` : 'Request a new link' }}
          </button>
          <router-link to="/account/login"
                       class="block text-sm text-[var(--color-text)] opacity-60 hover:opacity-100">
            ← Back to login
          </router-link>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import authApi from '../../services/authApi';

const route = useRoute();

const state = ref('loading'); // 'loading' | 'success' | 'error'
const message = ref('');
const resendCooldown = ref(0);
const resendLoading = ref(false);
let cooldownTimer = null;

onUnmounted(() => { if (cooldownTimer) clearInterval(cooldownTimer); });

const startCooldown = () => {
  resendCooldown.value = 60;
  cooldownTimer = setInterval(() => {
    resendCooldown.value--;
    if (resendCooldown.value <= 0) clearInterval(cooldownTimer);
  }, 1000);
};

const handleResend = async () => {
  const email = route.query.email;
  if (!email || resendCooldown.value > 0) return;
  resendLoading.value = true;
  try {
    await authApi.resendConfirmation(email);
    startCooldown();
  } finally {
    resendLoading.value = false;
  }
};

onMounted(async () => {
  const { userId, token } = route.query;
  if (!userId || !token) {
    state.value = 'error';
    message.value = 'Invalid confirmation link. Please check the link in your email.';
    return;
  }

  const result = await authApi.confirmEmail(userId, token);
  if (result.success) {
    state.value = 'success';
    message.value = result.message || 'Your email has been confirmed. You can now log in.';
  } else {
    state.value = 'error';
    message.value = result.message || 'This link is invalid or has expired.';
  }
});
</script>
