import { watch, onUnmounted } from 'vue'

export function useScrollLock(isLocked) {
  watch(isLocked, (locked) => {
    document.body.style.overflow = locked ? 'hidden' : ''
  })
  onUnmounted(() => { document.body.style.overflow = '' })
}
