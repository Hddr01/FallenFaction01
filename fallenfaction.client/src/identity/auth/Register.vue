<template>
  <div class="min-h-screen flex items-center justify-center bg-[var(--color-background)] py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">

      <!-- Email confirmation sent state -->
      <div v-if="emailConfirmationSent" class="text-center space-y-6">
        <div class="mx-auto h-16 w-16 flex items-center justify-center rounded-full bg-indigo-100">
          <svg class="h-8 w-8 text-[var(--vt-c-indigo)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
        </div>
        <h2 class="text-2xl font-bold text-[var(--color-heading)]">Check your email</h2>
        <p class="text-[var(--color-text)] opacity-80">
          We sent a confirmation link to <strong>{{ registeredEmail }}</strong>.
          Click the link in the email to activate your account.
        </p>
        <p class="text-sm text-[var(--color-text)] opacity-60">
          Didn't receive it? Check your spam folder or
          <button @click="handleResend" :disabled="resendCooldown > 0"
                  class="text-[var(--vt-c-indigo)] hover:opacity-80 underline disabled:opacity-40">
            resend{{ resendCooldown > 0 ? ` (${resendCooldown}s)` : '' }}
          </button>.
        </p>
        <router-link to="/account/login"
                     class="inline-block text-sm text-[var(--color-text)] opacity-60 hover:opacity-100 mt-2">
          ← Back to login
        </router-link>
      </div>
      <!-- Registration form -->
      <div v-else>
      <div>
        <div class="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-[var(--color-background-soft)]">
          <svg class="h-6 w-6 text-[var(--color-heading)]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
          </svg>
        </div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-[var(--color-heading)]">
          Create your account
        </h2>
        <p class="mt-2 text-center text-sm text-[var(--color-text)]">
          Or
          <router-link to="/account/login" class="font-medium text-[var(--vt-c-indigo)] hover:opacity-80">
            sign in to your existing account
          </router-link>
        </p>
      </div>

      <form class="mt-8 space-y-6" @submit.prevent="handleRegister">
        <div class="space-y-4">
          <!-- Username -->
          <div>
            <label for="userName" class="block text-sm font-medium text-[var(--color-text)]">Username</label>
            <input id="userName"
                   v-model="form.userName"
                   name="userName"
                   type="text"
                   required
                   class="mt-1 appearance-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] sm:text-sm"
                   :class="{ 'border-red-300': errors.userName }"
                   placeholder="Choose a unique username" />
            <div v-if="errors.userName" class="mt-1 text-sm text-red-600">
              {{ errors.userName }}
            </div>
            <div class="mt-1 text-xs text-[var(--color-text)] opacity-75">
              Username must be 3-30 characters and can only contain letters, numbers, hyphens, and underscores
            </div>
          </div>

          <!-- Email -->
          <div>
            <label for="email" class="block text-sm font-medium text-[var(--color-text)]">Email address</label>
            <input id="email"
                   v-model="form.email"
                   name="email"
                   type="email"
                   autocomplete="email"
                   required
                   class="mt-1 appearance-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] sm:text-sm"
                   :class="{ 'border-red-300': errors.email }"
                   placeholder="Email address" />
            <div v-if="errors.email" class="mt-1 text-sm text-red-600">
              {{ errors.email }}
            </div>
          </div>

          <!-- Date of Birth (Optional) -->
          <div>
            <label for="dateOfBirth" class="block text-sm font-medium text-[var(--color-text)]">Date of Birth (Optional)</label>
            <input id="dateOfBirth"
                   v-model="form.dateOfBirth"
                   name="dateOfBirth"
                   type="date"
                   class="mt-1 appearance-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] sm:text-sm"
                   :class="{ 'border-red-300': errors.dateOfBirth }" />
            <div v-if="errors.dateOfBirth" class="mt-1 text-sm text-red-600">
              {{ errors.dateOfBirth }}
            </div>
            <div class="mt-1 text-xs text-[var(--color-text)] opacity-75">
              Optional: Help us provide age-appropriate content
            </div>
          </div>

          <!-- Bio (Optional) -->
          <div>
            <label for="bio" class="block text-sm font-medium text-[var(--color-text)]">Bio (Optional)</label>
            <textarea id="bio"
                      v-model="form.bio"
                      name="bio"
                      rows="3"
                      class="mt-1 appearance-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] sm:text-sm"
                      :class="{ 'border-red-300': errors.bio }"
                      placeholder="Tell us about yourself..." />
            <div v-if="errors.bio" class="mt-1 text-sm text-red-600">
              {{ errors.bio }}
            </div>
          </div>

          <!-- Password -->
          <div>
            <label for="password" class="block text-sm font-medium text-[var(--color-text)]">Password</label>
            <input id="password"
                   v-model="form.password"
                   name="password"
                   type="password"
                   autocomplete="new-password"
                   required
                   class="mt-1 appearance-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] sm:text-sm"
                   :class="{ 'border-red-300': errors.password }"
                   placeholder="Password" />
            <div v-if="errors.password" class="mt-1 text-sm text-red-600">
              {{ errors.password }}
            </div>
            <div class="mt-1 text-xs text-[var(--color-text)] opacity-75">
              Password must be at least 8 characters with uppercase, lowercase, and number
            </div>
          </div>

          <!-- Terms -->
          <div class="flex items-start gap-3">
            <input
              id="acceptedTerms"
              v-model="form.acceptedTerms"
              name="acceptedTerms"
              type="checkbox"
              class="mt-1 h-4 w-4 shrink-0 text-[var(--vt-c-indigo)] focus:ring-indigo-500 border-[var(--color-border)] rounded"
            />
            <label for="acceptedTerms" class="text-sm text-[var(--color-text)]">
              I have read and agree to the
              <router-link to="/terms" class="font-medium text-[var(--vt-c-indigo)] hover:opacity-80">
                Terms and Conditions
              </router-link>
              <span class="text-red-600">*</span>
            </label>
          </div>
          <div v-if="errors.acceptedTerms" class="text-sm text-red-600">
            {{ errors.acceptedTerms }}
          </div>

          <!-- Confirm Password -->
          <div>
            <label for="confirmPassword" class="block text-sm font-medium text-[var(--color-text)]">Confirm Password</label>
            <input id="confirmPassword"
                   v-model="form.confirmPassword"
                   name="confirmPassword"
                   type="password"
                   autocomplete="new-password"
                   required
                   class="mt-1 appearance-none relative block w-full px-3 py-2 border border-[var(--color-border)] placeholder-gray-500 text-[var(--color-text)] bg-[var(--color-background)] rounded-md focus:outline-none focus:ring-indigo-500 focus:border-[var(--color-border-hover)] sm:text-sm"
                   :class="{ 'border-red-300': errors.confirmPassword }"
                   placeholder="Confirm Password" />
            <div v-if="errors.confirmPassword" class="mt-1 text-sm text-red-600">
              {{ errors.confirmPassword }}
            </div>
          </div>
        </div>

        <!-- Error message -->
        <div v-if="authStore.error" class="rounded-md bg-red-50 p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
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
          <button type="submit"
                  :disabled="authStore.isLoading"
                  class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-[var(--vt-c-indigo)] hover:bg-[var(--vt-c-black-soft)] focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed">
            <span class="absolute left-0 inset-y-0 flex items-center pl-3">
              <svg v-if="!authStore.isLoading" class="h-5 w-5 text-indigo-300 group-hover:text-indigo-200" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
              </svg>
              <svg v-else class="animate-spin h-5 w-5 text-indigo-300" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
            </span>
            {{ authStore.isLoading ? 'Creating account...' : 'Create account' }}
          </button>
        </div>
      </form>
      </div> <!-- end v-else registration form -->
    </div>
  </div>
</template>

<script setup>
  import { reactive, ref, onUnmounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { useAuthStore } from '../../stores/authStore';
  import authApi from '../../services/authApi';

  const router = useRouter();
  const authStore = useAuthStore();

  const emailConfirmationSent = ref(false);
  const registeredEmail = ref('');
  const resendCooldown = ref(0);
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
    if (resendCooldown.value > 0) return;
    await authApi.resendConfirmation(registeredEmail.value);
    startCooldown();
  };

  const form = reactive({
    userName: '',
    email: '',
    dateOfBirth: '',
    bio: '',
    password: '',
    confirmPassword: '',
    acceptedTerms: false
  });

  const errors = ref({});

  const validateForm = () => {
    errors.value = {};

    if (!form.userName.trim()) {
      errors.value.userName = 'Username is required';
    } else if (form.userName.length < 3 || form.userName.length > 30) {
      errors.value.userName = 'Username must be between 3 and 30 characters';
    } else if (!/^[a-zA-Z0-9_-]+$/.test(form.userName)) {
      errors.value.userName = 'Username can only contain letters, numbers, hyphens, and underscores';
    }

    if (!form.email) {
      errors.value.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(form.email)) {
      errors.value.email = 'Email is invalid';
    }

    // Date of birth is now optional, only validate if provided
    if (form.dateOfBirth) {
      const birthDate = new Date(form.dateOfBirth);
      const today = new Date();
      let age = today.getFullYear() - birthDate.getFullYear();
      const monthDiff = today.getMonth() - birthDate.getMonth();

      if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
        age--;
      }

      if (age < 13) {
        errors.value.dateOfBirth = 'You must be at least 13 years old';
      }

      if (birthDate > today) {
        errors.value.dateOfBirth = 'Date of birth cannot be in the future';
      }
    }

    if (form.bio && form.bio.length > 500) {
      errors.value.bio = 'Bio must be less than 500 characters';
    }

    if (!form.password) {
      errors.value.password = 'Password is required';
    } else if (form.password.length < 8) {
      errors.value.password = 'Password must be at least 8 characters';
    } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(form.password)) {
      errors.value.password = 'Password must contain uppercase, lowercase, and number';
    }

    if (!form.confirmPassword) {
      errors.value.confirmPassword = 'Please confirm your password';
    } else if (form.password !== form.confirmPassword) {
      errors.value.confirmPassword = 'Passwords do not match';
    }

    if (!form.acceptedTerms) {
      errors.value.acceptedTerms = 'You must accept the Terms and Conditions to register';
    }

    return Object.keys(errors.value).length === 0;
  };

  const handleRegister = async () => {
    if (!validateForm()) return;

    authStore.clearError();

    const result = await authStore.register({
      userName: form.userName.trim(),
      email: form.email,
      dateOfBirth: form.dateOfBirth || null,
      bio: form.bio || null,
      password: form.password,
      confirmPassword: form.confirmPassword,
      acceptedTerms: form.acceptedTerms
    });

    if (result.success && result.requiresEmailConfirmation) {
      registeredEmail.value = form.email;
      emailConfirmationSent.value = true;
      startCooldown();
    } else if (result.success) {
      router.push('/');
    }
  };
</script>
