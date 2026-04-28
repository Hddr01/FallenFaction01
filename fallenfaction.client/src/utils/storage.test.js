import { describe, it, expect, beforeEach } from 'vitest'
import { storage } from './storage.js'

describe('storage', () => {
  beforeEach(() => localStorage.clear())

  describe('theme', () => {
    it('returns null when unset', () => {
      expect(storage.theme.get()).toBeNull()
    })

    it('persists and retrieves a value', () => {
      storage.theme.set('light')
      expect(storage.theme.get()).toBe('light')
    })

    it('removes the value', () => {
      storage.theme.set('dark')
      storage.theme.remove()
      expect(storage.theme.get()).toBeNull()
    })
  })

  describe('readerPrefs', () => {
    it('returns null for unknown key', () => {
      expect(storage.readerPrefs.get('fontSize')).toBeNull()
    })

    it('round-trips a primitive value', () => {
      storage.readerPrefs.set('fontSize', 18)
      expect(storage.readerPrefs.get('fontSize')).toBe(18)
    })

    it('round-trips an object value', () => {
      storage.readerPrefs.set('theme', { name: 'sepia' })
      expect(storage.readerPrefs.get('theme')).toEqual({ name: 'sepia' })
    })

    it('removes a key', () => {
      storage.readerPrefs.set('fontSize', 18)
      storage.readerPrefs.remove('fontSize')
      expect(storage.readerPrefs.get('fontSize')).toBeNull()
    })
  })

  describe('smartLoadingHistory', () => {
    it('returns null when unset', () => {
      expect(storage.smartLoadingHistory.get()).toBeNull()
    })

    it('round-trips an array', () => {
      const history = [{ duration: 200 }, { duration: 350 }]
      storage.smartLoadingHistory.set(history)
      expect(storage.smartLoadingHistory.get()).toEqual(history)
    })
  })
})
