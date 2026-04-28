import axios from 'axios'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
  withCredentials: true,
  timeout: 15000,
})

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Guard against multiple concurrent 401s each scheduling a redirect
let redirectingToLogin = false

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const url = error.config?.url || ''

    if (error.response?.status === 401) {
      const skip = url.includes('/auth/logout') || url.includes('/auth/accept-terms')
      if (!skip && !redirectingToLogin) {
        redirectingToLogin = true
        localStorage.removeItem('authToken')
        localStorage.removeItem('authUser')
        setTimeout(() => {
          if (!window.location.pathname.includes('/account/login')) {
            window.location.href = '/account/login'
          }
          redirectingToLogin = false
        }, 100)
      }
    }

    if (error.response?.status === 429) {
      const backgroundEndpoints = ['/auth/heartbeat', '/auth/online-status', '/auth/health']
      const isBackground = backgroundEndpoints.some(ep => url.includes(ep))
      if (!isBackground && !window.location.pathname.startsWith('/error/')) {
        const retryAfter = error.response.headers['retry-after']
        const message = retryAfter
          ? `Too many requests. Please try again in ${retryAfter} seconds.`
          : 'Too many requests. Please slow down and try again.'
        window.location.href = `/error/429?message=${encodeURIComponent(message)}&retry=true`
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
