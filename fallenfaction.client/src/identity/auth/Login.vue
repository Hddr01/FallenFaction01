<template>
  <div class="min-h-screen flex items-center justify-center bg-[var(--color-background)] py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
      <div>
        <div class="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-[var(--color-background-soft)]">
          <svg class="h-6 w-6 text-[var(--color-heading)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
          </svg>
        </div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-[var(--color-heading)]">
          Sign in to your account
        </h2>
        <p class="mt-2 text-center text-sm text-[var(--color-text)]">
          Or
          <router-link to="/account/register" class="font-medium text-[var(--vt-c-indigo)] hover:opacity-80">
            create a new account
          </router-link>
        </p>
      </div>
      
      <form class="mt-8 space-y-6" @submit.prevent="handleLogin">
        <div class="rounded-md shadow-sm -space-y-px">
          <div>
            <label for="email" class="sr-only">Email address</label>
            <input
              id="email"
              v-model="form.email"
              name="email"
              type="email"
              autocomplete="email"
              required
              class="appearance-none rounded-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-t-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] focus:z-10 sm:text-sm"
              :class="{ 'border-red-300': errors.email }"
              placeholder="Email address"
            />
            <div v-if="errors.email" class="mt-1 text-sm text-red-600">
              {{ errors.email }}
            </div>
          </div>
          <div>
            <label for="password" class="sr-only">Password</label>
            <input
              id="password"
              v-model="form.password"
              name="password"
              type="password"
              autocomplete="current-password"
              required
              class="appearance-none rounded-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-b-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] focus:z-10 sm:text-sm"
              :class="{ 'border-red-300': errors.password }"
              placeholder="Password"
            />
            <div v-if="errors.password" class="mt-1 text-sm text-red-600">
              {{ errors.password }}
            </div>
          </div>
        </div>

        <div class="flex items-center justify-between">
          <div class="flex items-center">
            <input
              id="remember-me"
              v-model="form.rememberMe"
              name="remember-me"
              type="checkbox"
              class="h-4 w-4 text-[var(--vt-c-indigo)] focus:ring-indigo-500 border-[var(--color-border)] rounded"
            />
            <label for="remember-me" class="ml-2 block text-sm text-[var(--color-text)]">
              Remember me
            </label>
          </div>

          <div class="text-sm">
            <a href="#" class="font-medium text-[var(--vt-c-indigo)] hover:opacity-80">
              Forgot your password?
            </a>
          </div>
        </div>

        <!-- Error message -->
        <div v-if="authStore.error" class="rounded-md bg-red-50 p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
              </svg>
            </div>
            <div class="ml-3">
              <h3 class="text-sm font-medium text-red-800">
                {{ authStore.error }}
              </h3>
            </div>
          </div>
        </div>

        <div>
          <button
            type="submit"
            :disabled="authStore.isLoading"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-[var(--vt-c-indigo)] hover:bg-[var(--vt-c-black-soft)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span class="absolute left-0 inset-y-0 flex items-center pl-3">
              <svg v-if="!authStore.isLoading" class="h-5 w-5 text-indigo-300 group-hover:text-indigo-200" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd"/>
              </svg>
              <svg v-else class="animate-spin h-5 w-5 text-indigo-300" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
              </svg>
            </span>
            {{ authStore.isLoading ? 'Signing in...' : 'Sign in' }}
          </button>
        </div>

        <!-- Social login buttons -->
        <div class="mt-6">
          <div class="relative">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-[var(--color-border)]" />
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-2 bg-[var(--color-background)] text-[var(--color-text)]">Or continue with</span>
            </div>
          </div>

          <div class="mt-6 grid grid-cols-2 gap-3">
            <button
              type="button"
              class="w-full inline-flex justify-center py-2 px-4 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-sm font-medium text-[var(--color-text)] hover:bg-[var(--color-background-soft)]"
            >
              <svg class="h-5 w-5" viewBox="0 0 24 24">
                <path fill="currentColor" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path fill="currentColor" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path fill="currentColor" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                <path fill="currentColor" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
              </svg>
              <span class="ml-2">Google</span>
            </button>

            <button
              type="button"
              class="w-full inline-flex justify-center py-2 px-4 border border-[var(--color-border)] rounded-md shadow-sm bg-[var(--color-background)] text-sm font-medium text-[var(--color-text)] hover:bg-[var(--color-background-soft)]"
            >
              <svg class="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z"/>
              </svg>
              <span class="ml-2">Facebook</span>
            </button>
          </div>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../../stores/authStore';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const form = reactive({
  email: '',
  password: '',
  rememberMe: false
});

const errors = ref({});

const validateForm = () => {
  errors.value = {};
  
  if (!form.email) {
    errors.value.email = 'Email is required';
  } else if (!/\S+@\S+\.\S+/.test(form.email)) {
    errors.value.email = 'Email is invalid';
  }
  
  if (!form.password) {
    errors.value.password = 'Password is required';
  }
  
  return Object.keys(errors.value).length === 0;
};

const handleLogin = async () => {
  if (!validateForm()) return;
  
  authStore.clearError();
  
  const result = await authStore.login({
    email: form.email,
    password: form.password,
    rememberMe: form.rememberMe
  });

  if (result.requiresTermsAcceptance) {
    const redirectTo = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
    router.push({ path: '/terms/accept', query: { redirect: redirectTo || '/' } });
    return;
  }

  if (result.success) {
    const redirectTo = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
    router.push(redirectTo || '/');
  }
};
</script>