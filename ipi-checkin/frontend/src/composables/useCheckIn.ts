import { computed, ref } from 'vue'
import { api, getLabId, setLabId } from '../api/client'
import { ApiError, type Lab, type ManifestDetail, type ManifestSummary } from '../api/types'

/**
 * One store for the whole screen. Every mutating endpoint returns the full manifest, so the
 * client never recomputes counts or "ready to close" locally — the server is the only place
 * those rules live, and the two can therefore never disagree.
 */
export function useCheckIn() {
  const lab = ref<Lab | null>(null)
  const manifests = ref<ManifestSummary[]>([])
  const detail = ref<ManifestDetail | null>(null)

  const selectedId = ref<number | null>(null)
  const loadingWorklist = ref(false)
  const loadingDetail = ref(false)
  const busySpecimenId = ref<number | null>(null)
  const closing = ref(false)
  const error = ref<ApiError | null>(null)

  const labId = ref(getLabId())

  const isEmpty = computed(() => !loadingWorklist.value && manifests.value.length === 0)

  function dismissError() {
    error.value = null
  }

  async function guard<T>(fn: () => Promise<T>): Promise<T | null> {
    try {
      error.value = null
      return await fn()
    } catch (e) {
      error.value = e instanceof ApiError ? e : new ApiError('unknown_error', String(e), 0)
      return null
    }
  }

  async function loadWorklist() {
    loadingWorklist.value = true
    await guard(async () => {
      lab.value = await api.currentLab()
      manifests.value = await api.listManifests()

      // Keep the current selection if it survives the reload, otherwise open the first
      // manifest that still needs work.
      const stillThere = manifests.value.some((m) => m.id === selectedId.value)
      const next = stillThere
        ? selectedId.value
        : (manifests.value.find((m) => m.status === 'Open') ?? manifests.value[0])?.id ?? null

      if (next !== null) await selectManifest(next)
      else detail.value = null
    })
    loadingWorklist.value = false
  }

  async function selectManifest(id: number) {
    selectedId.value = id
    loadingDetail.value = true
    const result = await guard(() => api.getManifest(id))
    if (result) detail.value = result
    loadingDetail.value = false
  }

  /** Mutations replace the detail wholesale and patch the matching worklist row's counts. */
  function apply(updated: ManifestDetail) {
    detail.value = updated

    const row = manifests.value.find((m) => m.id === updated.id)
    if (row) {
      row.counts = updated.counts
      row.status = updated.status
    }
  }

  async function receive(specimenId: number) {
    if (!detail.value) return
    busySpecimenId.value = specimenId
    const updated = await guard(() => api.receive(detail.value!.id, specimenId))
    if (updated) apply(updated)
    else await refreshDetailQuietly()
    busySpecimenId.value = null
  }

  async function flag(specimenId: number) {
    if (!detail.value) return
    busySpecimenId.value = specimenId
    const updated = await guard(() => api.flag(detail.value!.id, specimenId))
    if (updated) apply(updated)
    else await refreshDetailQuietly()
    busySpecimenId.value = null
  }

  async function close() {
    if (!detail.value) return
    closing.value = true
    const updated = await guard(() => api.close(detail.value!.id))
    if (updated) apply(updated)
    else await refreshDetailQuietly()
    closing.value = false
  }

  /**
   * A rejected action means our copy of the manifest disagrees with the server's. Re-fetch
   * without clobbering the error the technician still needs to read.
   */
  async function refreshDetailQuietly() {
    if (!detail.value) return
    try {
      detail.value = await api.getManifest(detail.value.id)
    } catch {
      /* the visible error already explains the problem */
    }
  }

  async function switchLab(id: number) {
    setLabId(id)
    labId.value = id
    selectedId.value = null
    detail.value = null
    manifests.value = []
    await loadWorklist()
  }

  return {
    lab,
    labId,
    manifests,
    detail,
    selectedId,
    loadingWorklist,
    loadingDetail,
    busySpecimenId,
    closing,
    error,
    isEmpty,
    loadWorklist,
    selectManifest,
    receive,
    flag,
    close,
    switchLab,
    dismissError
  }
}
