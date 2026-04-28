import { ref } from 'vue'

const scrollY = ref(0)

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

export function useScrollSignal() {
  return { scrollY }
}
