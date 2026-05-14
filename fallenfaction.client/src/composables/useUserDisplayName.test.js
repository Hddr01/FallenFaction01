import { describe, it, expect } from 'vitest'
import { useUserDisplayName } from './useUserDisplayName.js'

describe('useUserDisplayName', () => {
  it('getDisplayName returns profileName when set', () => {
    const { getDisplayName } = useUserDisplayName()
    const user = {
      profileName: 'Jane Doe',
      displayName: 'jane-d',
      userName: 'jdoe',
      userHandle: 'jdoe'
    }
    expect(getDisplayName(user)).toBe('Jane Doe')
  })

  it('getDisplayName falls back to displayName when profileName is null', () => {
    const { getDisplayName } = useUserDisplayName()
    const user = {
      profileName: null,
      displayName: 'jane-d',
      userName: 'jdoe',
      userHandle: 'jdoe'
    }
    expect(getDisplayName(user)).toBe('jane-d')
  })

  it('getDisplayName falls back to userName when profileName and displayName are null', () => {
    const { getDisplayName } = useUserDisplayName()
    const user = {
      profileName: null,
      displayName: null,
      userName: 'jdoe',
      userHandle: 'jdoe'
    }
    expect(getDisplayName(user)).toBe('jdoe')
  })

  it('getDisplayName returns @handle format when only userHandle is available', () => {
    const { getDisplayName } = useUserDisplayName()
    const user = {
      profileName: null,
      displayName: null,
      userName: null,
      userHandle: 'jdoe'
    }
    expect(getDisplayName(user)).toBe('@jdoe')
  })

  it('getDisplayName returns the default fallback when user is null', () => {
    const { getDisplayName } = useUserDisplayName()
    expect(getDisplayName(null)).toBe('Unknown')
  })

  it('getDisplayName uses a custom fallback when provided', () => {
    const { getDisplayName } = useUserDisplayName()
    expect(getDisplayName(null, 'Anonymous')).toBe('Anonymous')
    expect(getDisplayName({}, 'Anonymous')).toBe('Anonymous')
  })

  it('getHandle returns userHandle when available', () => {
    const { getHandle } = useUserDisplayName()
    const user = { userHandle: 'jdoe', userName: 'jane' }
    expect(getHandle(user)).toBe('jdoe')
  })

  it('getHandle returns userName as fallback when userHandle is absent', () => {
    const { getHandle } = useUserDisplayName()
    const user = { userName: 'jane' }
    expect(getHandle(user)).toBe('jane')
  })

  it('getHandle returns an empty string for a null user', () => {
    const { getHandle } = useUserDisplayName()
    expect(getHandle(null)).toBe('')
  })
})
