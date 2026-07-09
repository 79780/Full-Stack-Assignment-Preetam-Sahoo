import { ApiError, type Lab, type ManifestDetail, type ManifestSummary } from './types'

const BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

/**
 * The lab id is sent as a header, standing in for the claim a real token would carry.
 * The server never trusts it blindly: it checks the lab exists, then scopes every query
 * to it. Changing this value in devtools gets you another lab's data only if you are
 * actually entitled to it — which, once this is behind auth, you are not.
 */
const LAB_HEADER = 'X-Lab-Id'
const LAB_STORAGE_KEY = 'ipipro.labId'

export function getLabId(): number {
  const stored = window.localStorage.getItem(LAB_STORAGE_KEY)
  return stored ? Number(stored) : 1
}

export function setLabId(labId: number): void {
  window.localStorage.setItem(LAB_STORAGE_KEY, String(labId))
}

async function request<T>(path: string, method: 'GET' | 'POST' = 'GET'): Promise<T> {
  let response: Response

  try {
    response = await fetch(`${BASE}/api${path}`, {
      method,
      headers: {
        Accept: 'application/json',
        [LAB_HEADER]: String(getLabId())
      }
    })
  } catch {
    // fetch only rejects on network-level failure, never on a 4xx/5xx.
    throw new ApiError('network_error', 'Cannot reach the check-in service.', 0)
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(
      problem?.code ?? 'unknown_error',
      problem?.detail ?? 'The request could not be completed.',
      response.status
    )
  }

  return response.json() as Promise<T>
}

export const api = {
  currentLab: () => request<Lab>('/labs/current'),
  listManifests: () => request<ManifestSummary[]>('/manifests'),
  getManifest: (id: number) => request<ManifestDetail>(`/manifests/${id}`),
  receive: (id: number, specimenId: number) =>
    request<ManifestDetail>(`/manifests/${id}/specimens/${specimenId}/receive`, 'POST'),
  flag: (id: number, specimenId: number) =>
    request<ManifestDetail>(`/manifests/${id}/specimens/${specimenId}/flag`, 'POST'),
  close: (id: number) => request<ManifestDetail>(`/manifests/${id}/close`, 'POST')
}
