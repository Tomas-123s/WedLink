export interface InvitationFeature {
  key: string
  name: string
  isEnabled: boolean
  maximumCount: number | null
  maximumFileSizeMb: number | null
}

export interface InvitationMedia {
  mediaId: number
  mediaType: string
  originalFileName: string
  contentType: string
  sizeBytes: number
  sortOrder: number
}

export interface InvitationReviewEvent {
  eventId: number
  source: string
  action: string
  comment: string | null
  adminUserId: string | null
  createdAt: string
}

export interface InvitationDraft {
  orderId: number
  invitationId: number | null
  templateVersionId: number
  templateName: string
  templateVersionNumber: string
  brideName: string
  groomName: string
  weddingDateTimeLocal: string | null
  timeZoneId: string
  venueName: string | null
  venueAddress: string | null
  locationUrl: string | null
  customizations: Record<string, unknown> | null
  status: string
  updatedAt: string | null
  features: InvitationFeature[]
  availableMedia: InvitationMedia[]
  selectedMediaIds: number[]
  reviewEvents: InvitationReviewEvent[]
}

export interface PreviewInvitation {
  templateName: string
  templateVersionNumber: string
  brideName: string
  groomName: string
  weddingDateTimeLocal: string | null
  timeZoneId: string
  venueName: string | null
  venueAddress: string | null
  locationUrl: string | null
  customizations: Record<string, unknown> | null
  features: InvitationFeature[]
  media: InvitationMedia[]
}

const adminPath = '/api/v1/admin/orders'

async function errorMessage(response: Response): Promise<string> {
  const problem = await response.json().catch(() => null)
  return problem?.detail ?? 'تعذر تنفيذ الطلب. حاول مرة أخرى.'
}

async function secureSend<T>(url: string, method: 'PUT' | 'POST', body?: unknown): Promise<T> {
  const csrfResponse = await fetch('/api/v1/admin/auth/csrf', { credentials: 'include' })
  if (!csrfResponse.ok) throw new Error('تعذر تجهيز طلب آمن. حدّث الصفحة وحاول مرة أخرى.')
  const { token } = await csrfResponse.json() as { token: string }
  const response = await fetch(url, {
    method,
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) throw new Error(await errorMessage(response))
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export async function getInvitation(orderId: number): Promise<InvitationDraft> {
  const response = await fetch(`${adminPath}/${orderId}/invitation`, { credentials: 'include' })
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json() as Promise<InvitationDraft>
}

export function saveInvitation(orderId: number, input: {
  brideName: string
  groomName: string
  weddingDateTimeLocal: string
  timeZoneId: string
  venueName: string | null
  venueAddress: string | null
  locationUrl: string | null
  customizations: Record<string, string>
  mediaIds: number[]
}): Promise<InvitationDraft> {
  return secureSend(`${adminPath}/${orderId}/invitation`, 'PUT', input)
}

export function createInvitationPreviewLink(orderId: number): Promise<{ previewUrl: string; expiresAt: string }> {
  return secureSend(`${adminPath}/${orderId}/invitation/preview-links`, 'POST')
}

async function sendPrivate<T>(token: string, path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/v1/invitation-previews/current${path}`, {
    credentials: 'omit', ...init,
    headers: { Authorization: `Bearer ${token}`, ...init?.headers },
  })
  if (!response.ok) throw new Error(await errorMessage(response))
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export function getPreviewInvitation(token: string): Promise<PreviewInvitation> {
  return sendPrivate(token, '')
}

export function sendPreviewReview(token: string, action: 'Approved' | 'ChangesRequested', comment: string): Promise<void> {
  return sendPrivate(token, '/review', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ action, comment: comment.trim() || null }),
  })
}

export async function getPreviewMedia(token: string, mediaId: number): Promise<string> {
  const response = await fetch(`/api/v1/invitation-previews/current/media/${mediaId}`, {
    credentials: 'omit', headers: { Authorization: `Bearer ${token}` },
  })
  if (!response.ok) throw new Error(await errorMessage(response))
  return URL.createObjectURL(await response.blob())
}
