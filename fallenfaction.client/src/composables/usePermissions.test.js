import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('@/services/apiClient.js', () => ({
  default: { get: vi.fn() }
}))

const flush = async () => {
  await Promise.resolve()
  await Promise.resolve()
}

describe('usePermissions', () => {
  let apiClient
  let usePermissions
  let clearPermissions

  beforeEach(async () => {
    vi.resetModules()
    apiClient = (await import('@/services/apiClient.js')).default
    apiClient.get.mockReset()
    const mod = await import('./usePermissions.js')
    usePermissions = mod.usePermissions
    clearPermissions = mod.clearPermissions
    clearPermissions()
  })

  it('fetches /Permissions/me on first use and exposes isAdmin/isModerator', async () => {
    apiClient.get.mockResolvedValueOnce({
      data: { isAdmin: true, isModerator: true, canAddTitleTeamIds: [1], canEditTitleTeamIds: [1, 2] }
    })
    const { isAdmin, isModerator } = usePermissions()
    await flush()
    expect(apiClient.get).toHaveBeenCalledWith('/Permissions/me')
    expect(isAdmin.value).toBe(true)
    expect(isModerator.value).toBe(true)
  })

  it('canAddTitle/canEditTitle answer per team-id', async () => {
    apiClient.get.mockResolvedValueOnce({
      data: { isAdmin: false, isModerator: false, canAddTitleTeamIds: [3, 5], canEditTitleTeamIds: [5] }
    })
    const { canAddTitle, canEditTitle } = usePermissions()
    await flush()
    expect(canAddTitle(3)).toBe(true)
    expect(canAddTitle(5)).toBe(true)
    expect(canAddTitle(99)).toBe(false)
    expect(canEditTitle(5)).toBe(true)
    expect(canEditTitle(3)).toBe(false)
  })

  it('falls back to empty permissions when the endpoint fails', async () => {
    apiClient.get.mockRejectedValueOnce(new Error('401'))
    const { isAdmin, canAddAnyTitle } = usePermissions()
    await flush()
    expect(isAdmin.value).toBe(false)
    expect(canAddAnyTitle.value).toBe(false)
  })

  it('shares one in-flight request across concurrent callers', async () => {
    let resolve
    apiClient.get.mockImplementationOnce(() => new Promise(r => { resolve = r }))
    usePermissions()
    usePermissions()
    usePermissions()
    expect(apiClient.get).toHaveBeenCalledTimes(1)
    resolve({ data: { isAdmin: false, isModerator: false, canAddTitleTeamIds: [], canEditTitleTeamIds: [] } })
    await flush()
  })

  it('refresh re-issues the request', async () => {
    apiClient.get.mockResolvedValueOnce({
      data: { isAdmin: false, isModerator: false, canAddTitleTeamIds: [], canEditTitleTeamIds: [] }
    })
    apiClient.get.mockResolvedValueOnce({
      data: { isAdmin: true, isModerator: true, canAddTitleTeamIds: [], canEditTitleTeamIds: [] }
    })
    const { isAdmin, refresh } = usePermissions()
    await flush()
    expect(isAdmin.value).toBe(false)
    await refresh()
    expect(apiClient.get).toHaveBeenCalledTimes(2)
    expect(isAdmin.value).toBe(true)
  })

  it('clearPermissions wipes the cached state and forces a re-fetch', async () => {
    apiClient.get.mockResolvedValueOnce({
      data: { isAdmin: true, isModerator: true, canAddTitleTeamIds: [], canEditTitleTeamIds: [] }
    })
    const { isAdmin } = usePermissions()
    await flush()
    expect(isAdmin.value).toBe(true)

    clearPermissions()
    expect(isAdmin.value).toBe(false)

    apiClient.get.mockResolvedValueOnce({
      data: { isAdmin: false, isModerator: false, canAddTitleTeamIds: [], canEditTitleTeamIds: [] }
    })
    usePermissions()
    await flush()
    expect(apiClient.get).toHaveBeenCalledTimes(2)
  })
})
