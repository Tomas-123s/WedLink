import type { CustomerSubmission } from '../orders/ordersApi'

const url = '/api/v1/customer-submissions/current'

async function send<T>(token: string, path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${url}${path}`, {
    credentials: 'omit',
    ...init,
    headers: { Authorization: `Bearer ${token}`, ...init?.headers },
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new Error(problem?.detail ?? 'تعذر تنفيذ الطلب. تحقق من صلاحية الرابط وحاول مرة أخرى.')
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export function getCurrentSubmission(token: string): Promise<CustomerSubmission> {
  return send(token, '')
}

export function saveCurrentSubmission(token: string, input: {
  brideName: string
  groomName: string
  weddingDateTimeLocal: string
  timeZoneId: string
  venueName: string | null
  venueAddress: string | null
  locationUrl: string | null
  customizations: Record<string, string>
  submit: boolean
}): Promise<CustomerSubmission> {
  return send(token, '', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input) })
}

export async function uploadSubmissionMedia(token: string, file: File): Promise<void> {
  const form = new FormData()
  form.append('file', file)
  await send(token, '/media', { method: 'POST', body: form })
}

export function deleteSubmissionMedia(token: string, mediaId: number): Promise<void> {
  return send(token, `/media/${mediaId}`, { method: 'DELETE' })
}
