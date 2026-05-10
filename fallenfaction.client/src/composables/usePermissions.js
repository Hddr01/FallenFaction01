import { ref, computed } from 'vue'
import apiClient from '@/services/apiClient.js'

// Module-level singleton state. The /api/permissions/me payload doesn't change
// often per session, and we want every component asking the same question
// (`canEditTitle(teamId)`) to share the same answer without each re-fetching.
const state = ref(null)
const loading = ref(false)
let inFlight = null

async function fetchPermissions() {
  if (inFlight) return inFlight
  loading.value = true
  inFlight = apiClient.get('/Permissions/me')
    .then(response => {
      state.value = response.data
      return response.data
    })
    .catch(() => {
      // Anonymous, expired token, etc — fall back to "no permissions". The
      // role-based isAdmin/isModerator getters will defer to the auth store
      // for backwards compat with the existing meta-route check.
      state.value = emptyPermissions()
    })
    .finally(() => {
      loading.value = false
      inFlight = null
    })
  return inFlight
}

function emptyPermissions() {
  return {
    isAdmin: false,
    isModerator: false,
    canAddTitleTeamIds: [],
    canEditTitleTeamIds: []
  }
}

/**
 * Reactive view over the user's permission summary.
 *
 * Templates can do `v-if="canEditTitle(teamId)"` instead of inlining
 * `authStore.isAdmin || /* manual team walk *​/`. The first call kicks off
 * the fetch; subsequent calls share the same in-flight promise.
 */
export function usePermissions({ immediate = true } = {}) {
  if (immediate && state.value === null && !inFlight) {
    fetchPermissions()
  }

  const isAdmin = computed(() => state.value?.isAdmin ?? false)
  const isModerator = computed(() => state.value?.isModerator ?? false)
  const canAddAnyTitle = computed(() => (state.value?.canAddTitleTeamIds?.length ?? 0) > 0)
  const canEditAnyTitle = computed(() => (state.value?.canEditTitleTeamIds?.length ?? 0) > 0)

  const canAddTitle = teamId => Boolean(state.value?.canAddTitleTeamIds?.includes(teamId))
  const canEditTitle = teamId => Boolean(state.value?.canEditTitleTeamIds?.includes(teamId))

  return {
    state,
    loading,
    isAdmin,
    isModerator,
    canAddAnyTitle,
    canEditAnyTitle,
    canAddTitle,
    canEditTitle,
    refresh: fetchPermissions
  }
}

// Hook for the auth store to clear cached permissions on logout / user change.
export function clearPermissions() {
  state.value = null
  inFlight = null
}
