function safeSet(key, value) {
  try {
    localStorage.setItem(key, value)
  } catch (e) {
    if (e.name === 'QuotaExceededError' || e.name === 'NS_ERROR_DOM_QUOTA_REACHED') {
      console.warn(`localStorage quota exceeded, "${key}" not persisted`)
    } else {
      throw e
    }
  }
}

export const storage = {
  theme: {
    get: () => localStorage.getItem('ff-theme'),
    set: (v) => safeSet('ff-theme', v),
    remove: () => localStorage.removeItem('ff-theme'),
  },
  readerPrefs: {
    get: (key) => {
      const raw = localStorage.getItem(`novelReader_${key}`)
      if (raw === null) return null
      try { return JSON.parse(raw) } catch { return null }
    },
    set: (key, v) => safeSet(`novelReader_${key}`, JSON.stringify(v)),
    remove: (key) => localStorage.removeItem(`novelReader_${key}`),
  },
  smartLoadingHistory: {
    get: () => {
      const raw = localStorage.getItem('smartLoading_history')
      if (!raw) return null
      try { return JSON.parse(raw) } catch { return null }
    },
    set: (v) => safeSet('smartLoading_history', JSON.stringify(v)),
  },
}

export default storage
