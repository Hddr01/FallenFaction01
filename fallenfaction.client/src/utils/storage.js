export const storage = {
  theme: {
    get: () => localStorage.getItem('ff-theme'),
    set: (v) => localStorage.setItem('ff-theme', v),
    remove: () => localStorage.removeItem('ff-theme'),
  },
  readerPrefs: {
    get: (key) => {
      const raw = localStorage.getItem(`novelReader_${key}`)
      if (raw === null) return null
      try { return JSON.parse(raw) } catch { return null }
    },
    set: (key, v) => localStorage.setItem(`novelReader_${key}`, JSON.stringify(v)),
    remove: (key) => localStorage.removeItem(`novelReader_${key}`),
  },
  smartLoadingHistory: {
    get: () => {
      const raw = localStorage.getItem('smartLoading_history')
      if (!raw) return null
      try { return JSON.parse(raw) } catch { return null }
    },
    set: (v) => localStorage.setItem('smartLoading_history', JSON.stringify(v)),
  },
}

export default storage
