import { ref } from 'vue'

const scrollY = ref(0)

let listenerInitialized = false

export function useScrollSignal() {
  if (!listenerInitialized) {
    listenerInitialized = true
    let ticking = false
    window.addEventListener('scroll', () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          scrollY.value = window.scrollY
          ticking = false
        })
        ticking = true
      }
    }, { passive: true })
  }
  return { scrollY }
}
