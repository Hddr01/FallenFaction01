<template>
  <div class="min-h-screen flex items-center justify-center bg-[var(--color-background)] py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-lg w-full space-y-8">
      <div>
        <h1 class="text-center text-3xl font-extrabold text-[var(--color-heading)]">
          Terms and Conditions
        </h1>
        <p class="mt-2 text-center text-sm text-[var(--color-text)]">
          You need to accept the current terms before you can use your account.
          <span v-if="termsVersion" class="block mt-1 text-xs opacity-75">Version: {{ termsVersion }}</span>
        </p>
      </div>

      <div class="rounded-lg border border-[var(--color-border)] bg-[var(--color-background-soft)] p-4 text-sm text-[var(--color-text)] space-y-2">
        <p>
          By continuing, you confirm that you have read and understood our
          <router-link to="/terms" class="font-medium text-[var(--vt-c-indigo)] hover:opacity-80">
            Terms of Service
          </router-link>
          and agree to be bound by them.
        </p>
      </div>

      <div class="flex items-start gap-3">
        <input
          id="confirmTerms"
          v-model="agreed"
          type="checkbox"
          class="mt-1 h-4 w-4 shrink-0 text-[var(--vt-c-indigo)] focus:ring-indigo-500 border-[var(--color-border)] rounded"
        />
        <label for="confirmTerms" class="text-sm text-[var(--color-text)]">
          I have read and accept the Terms and Conditions
          <span class="text-red-600">*</span>
        </label>
      </div>
      <p v-if="localError" class="text-sm text-red-600">{{ localError }}</p>

      <div v-if="authStore.error" class="rounded-md bg-red-50 p-4">
        <p class="text-sm font-medium text-red-800">{{ authStore.error }}</p>
      </div>

      <div class="flex flex-col sm:flex-row gap-3 sm:justify-end">
        <button
          type="button"
          class="w-full sm:w-auto py-2 px-4 border border-[var(--color-border)] rounded-md text-sm font-medium text-[var(--color-text)] bg-[var(--color-background)] hover:bg-[var(--color-background-soft)]"
          @click="router.push({ name: 'Login', query: { redirect: redirectPath } })"
        >
          Cancel
        </button>
        <button
          type="button"
          :disabled="authStore.isLoading || !agreed"
          class="w-full sm:w-auto py-2 px-4 border border-transparent rounded-md text-sm font-medium text-white bg-[var(--vt-c-indigo)] hover:bg-[var(--vt-c-black-soft)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          @click="handleAccept"
        >
          {{ authStore.isLoading ? 'Signing in…' : 'Accept & continue' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../../stores/authStore';

const PENDING_TERMS_KEY = 'ff-pending-terms';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const agreed = ref(false);
const localError = ref('');
const termsVersion = ref('');

const redirectPath = computed(() => {
  const r = route.query.redirect;
  return typeof r === 'string' && r ? r : '/';
});

onMounted(() => {
  authStore.clearError();
  const raw = sessionStorage.getItem(PENDING_TERMS_KEY);
  if (!raw) {
    router.replace({ name: 'Login', query: { redirect: redirectPath.value } });
    return;
  }
  try {
    const parsed = JSON.parse(raw);
    if (parsed?.termsVersion) termsVersion.value = parsed.termsVersion;
  } catch {
    /* ignore */
  }
});

const handleAccept = async () => {
  localError.value = '';
  if (!agreed.value) {
    localError.value = 'Please confirm the checkbox to continue.';
    return;
  }
  authStore.clearError();

  const result = await authStore.acceptTermsAndLogin();
  if (result.success) {
    router.push(redirectPath.value);
  }
};
</script>
