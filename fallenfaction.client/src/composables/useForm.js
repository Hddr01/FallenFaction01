import { reactive, ref, computed } from 'vue'

/**
 * Form state machine: reactive field bag + per-field errors + submission flow.
 *
 * Replaces the hand-rolled pattern across AddTeam / EditTitle / AddChapter etc.,
 * which each spelled out: declare form/errors/loading/message refs, write a
 * submit handler that toggles loading, calls the service, maps server-side
 * validationErrors back onto per-field error refs.
 *
 * @template {Record<string, unknown>} T
 * @param {Object} options
 * @param {T} options.initialValues
 *   Field bag — keys define the shape of `form` and `errors`.
 * @param {(form: T) => Partial<Record<keyof T, string>>} [options.validate]
 *   Pure function returning per-field error messages. Empty object = valid.
 * @param {(form: T) => Promise<{
 *   success: boolean
 *   error?: string
 *   message?: string
 *   data?: unknown
 *   validationErrors?: Record<string, string | string[]>
 * }>} options.submit
 *   The service call. Must return the standard service-result envelope used
 *   throughout this codebase.
 * @param {(result: { data?: unknown, message?: string }) => void} [options.onSuccess]
 *   Optional post-submit hook (navigation, toasts, etc.).
 */
export function useForm({ initialValues, validate, submit, onSuccess }) {
  const initialSnapshot = clone(initialValues)
  const form = reactive(clone(initialValues))
  const errors = reactive(emptyErrors(initialValues))
  const message = reactive({ text: '', type: '' })
  const loading = ref(false)

  const clearErrors = () => {
    for (const key of Object.keys(errors)) errors[key] = ''
    message.text = ''
    message.type = ''
  }

  // Live form-validity check used to disable the submit button. Re-runs the
  // validator on every read (Vue tracks the form ref); validators here are
  // expected to be pure and cheap.
  const isValid = computed(() => {
    if (!validate) return true
    const result = validate(form) || {}
    return Object.keys(result).length === 0
  })

  const handleSubmit = async () => {
    if (loading.value) return
    clearErrors()

    if (validate) {
      const validationResult = validate(form) || {}
      let hasError = false
      for (const key of Object.keys(errors)) {
        if (validationResult[key]) {
          errors[key] = validationResult[key]
          hasError = true
        }
      }
      if (hasError) return
    }

    loading.value = true
    try {
      const result = await submit(form)
      if (result?.success) {
        message.text = result.message || ''
        message.type = 'success'
        onSuccess?.(result)
      } else {
        message.text = result?.error || 'An unexpected error occurred. Please try again.'
        message.type = 'error'
        applyServerValidationErrors(errors, result?.validationErrors)
      }
    } catch {
      message.text = 'An unexpected error occurred. Please try again.'
      message.type = 'error'
    } finally {
      loading.value = false
    }
  }

  const reset = () => {
    Object.assign(form, clone(initialSnapshot))
    clearErrors()
  }

  return {
    form,
    errors,
    message,
    loading,
    isValid,
    handleSubmit,
    reset,
    clearErrors
  }
}

function clone(obj) {
  return JSON.parse(JSON.stringify(obj))
}

function emptyErrors(initialValues) {
  return Object.fromEntries(Object.keys(initialValues).map(k => [k, '']))
}

// Backend ValidationProblemDetails sends `{ FieldName: ['message'] }` — match
// against the client-side camelCase keys and pull the first message.
function applyServerValidationErrors(errors, serverErrors) {
  if (!serverErrors) return
  for (const [serverKey, messages] of Object.entries(serverErrors)) {
    const clientKey = serverKey.charAt(0).toLowerCase() + serverKey.slice(1)
    if (clientKey in errors) {
      errors[clientKey] = Array.isArray(messages) ? messages[0] : messages
    }
  }
}
