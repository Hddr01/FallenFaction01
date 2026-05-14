export function useUserDisplayName() {
  function getDisplayName(user, fallback = 'Unknown') {
    if (!user) return fallback
    return (
      user.profileName ||
      user.displayName ||
      user.userName ||
      (user.userHandle ? `@${user.userHandle}` : null) ||
      fallback
    )
  }

  function getHandle(user) {
    if (!user) return ''
    return user.userHandle || user.userName || ''
  }

  return { getDisplayName, getHandle }
}
