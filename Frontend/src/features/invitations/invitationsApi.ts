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
  musicTrackId: number | null
  templateName: string
  templateVersionNumber: string
  rendererKey: 'editorial-luxury' | 'default'
  brideName: string
  groomName: string
  weddingDateTimeLocal: string | null
  timeZoneId: string
  venueName: string | null
  venueAddress: string | null
  locationUrl: string | null
  customizations: Record<string, unknown> | null
  status: string
  slug: string
  publishedAt: string | null
  expiresAt: string | null
  pausedAt: string | null
  hasFinalPng: boolean
  updatedAt: string | null
  features: InvitationFeature[]
  availableMedia: InvitationMedia[]
  selectedMediaIds: number[]
  reviewEvents: InvitationReviewEvent[]
}

export interface PreviewInvitation {
  templateName: string
  templateVersionNumber: string
  rendererKey: 'editorial-luxury' | 'default'
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

export interface PublicInvitation {
  eventEnded: boolean
  hasFinalPng: boolean
  templateName: string
  rendererKey: 'editorial-luxury' | 'default'
  brideName: string
  groomName: string
  weddingDateTimeLocal: string | null
  timeZoneId: string
  venueName: string | null
  venueAddress: string | null
  locationUrl: string | null
  customizations: Record<string, unknown> | null
  features: InvitationFeature[]
  media: Array<InvitationMedia & { url: string }>
}

export interface GuestInteractions {
  rsvpEnabled: boolean
  maximumAccompanyingPeople: number | null
  guestCountEnabled: boolean
  expectedAttendance: number
  messagesEnabled: boolean
  messages: Array<{ guestMessageId: number; guestName: string | null; message: string; submittedAt: string }>
  musicEnabled: boolean
  musicTrack: { musicTrackId: number; title: string; artist: string | null } | null
  musicUrl: string | null
}

export interface MusicTrack {
  musicTrackId: number
  title: string
  artist: string | null
  rightsReference: string
  isActive: boolean
}

export function formatInvitationLocalDate(value: string, timeZone: string, options: Intl.DateTimeFormatOptions): string {
  const local = value.replace(/Z$/, '').slice(0, 19)
  const [datePart, timePart = '00:00:00'] = local.split('T')
  const [year, month, day] = datePart.split('-').map(Number)
  const [hour, minute, second] = timePart.split(':').map(Number)
  const wallClockUtc = Date.UTC(year, month - 1, day, hour, minute, second)
  let instant = wallClockUtc
  for (let attempt = 0; attempt < 2; attempt++) {
    const parts = new Intl.DateTimeFormat('en-US', {
      timeZone, year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23',
    }).formatToParts(new Date(instant))
    const part = (type: string) => Number(parts.find((item) => item.type === type)?.value)
    const displayedUtc = Date.UTC(part('year'), part('month') - 1, part('day'), part('hour'), part('minute'), part('second'))
    instant = wallClockUtc - (displayedUtc - instant)
  }
  return new Intl.DateTimeFormat('ar-EG', { ...options, timeZone }).format(new Date(instant))
}

export function invitationLocalDateToEpoch(value: string, timeZone: string): number | null {
  const local = value.replace(/Z$/, '').slice(0, 19)
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/.exec(local)
  if (!match) return null
  const [, yearText, monthText, dayText, hourText, minuteText, secondText = '0'] = match
  const [year, month, day, hour, minute, second] = [yearText, monthText, dayText, hourText, minuteText, secondText].map(Number)
  const wallClockUtc = Date.UTC(year, month - 1, day, hour, minute, second)
  if (new Date(wallClockUtc).getUTCFullYear() !== year
    || new Date(wallClockUtc).getUTCMonth() !== month - 1
    || new Date(wallClockUtc).getUTCDate() !== day
    || hour > 23 || minute > 59 || second > 59) return null

  try {
    let instant = wallClockUtc
    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone, year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23',
    })
    for (let attempt = 0; attempt < 3; attempt++) {
      const parts = formatter.formatToParts(new Date(instant))
      const part = (type: string) => Number(parts.find((item) => item.type === type)?.value)
      const displayedUtc = Date.UTC(part('year'), part('month') - 1, part('day'), part('hour'), part('minute'), part('second'))
      const offset = displayedUtc - instant
      if (offset === 0) break
      instant = wallClockUtc - offset
    }
    const parts = formatter.formatToParts(new Date(instant))
    const part = (type: string) => Number(parts.find((item) => item.type === type)?.value)
    return Date.UTC(part('year'), part('month') - 1, part('day'), part('hour'), part('minute'), part('second')) === wallClockUtc
      ? instant
      : null
  } catch {
    return null
  }
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

export async function uploadInvitationMedia(orderId: number, file: File): Promise<InvitationMedia> {
  const csrfResponse = await fetch('/api/v1/admin/auth/csrf', { credentials: 'include' })
  if (!csrfResponse.ok) throw new Error('تعذر تجهيز طلب آمن. حدّث الصفحة وحاول مرة أخرى.')
  const { token } = await csrfResponse.json() as { token: string }
  const body = new FormData()
  body.set('file', file)
  const response = await fetch(`/api/v1/admin/orders/${orderId}/customer-submission/media`, {
    method: 'POST', credentials: 'include', headers: { 'X-CSRF-TOKEN': token }, body,
  })
  if (!response.ok) throw new Error(await errorMessage(response))
  const uploaded = await response.json() as { mediaId: number; originalFileName: string; contentType: string; sizeBytes: number; mediaKind: string }
  return { mediaId: uploaded.mediaId, originalFileName: uploaded.originalFileName, contentType: uploaded.contentType,
    sizeBytes: uploaded.sizeBytes, mediaType: uploaded.mediaKind, sortOrder: 0 }
}

export function createInvitationPreviewLink(orderId: number): Promise<{ previewUrl: string; expiresAt: string }> {
  return secureSend(`${adminPath}/${orderId}/invitation/preview-links`, 'POST')
}

export async function getPublicInvitation(slug: string): Promise<PublicInvitation> {
  const response = await fetch(`/api/v1/public/invitations/${encodeURIComponent(slug)}`)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json() as Promise<PublicInvitation>
}

export async function getGuestInteractions(slug: string): Promise<GuestInteractions> {
  const response = await fetch(`/api/v1/public/invitations/${encodeURIComponent(slug)}/guest-interactions`)
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json() as Promise<GuestInteractions>
}

export async function submitRsvp(slug: string, input: { response: 'Yes' | 'No'; guestName: string; accompanyingPeople: number; editToken: string | null }) {
  const response = await fetch(`/api/v1/public/invitations/${encodeURIComponent(slug)}/rsvps`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input),
  })
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json() as Promise<{ editToken: string; expectedAttendance: number; isUpdate: boolean }>
}

export async function submitGuestMessage(slug: string, input: { guestName: string; message: string }) {
  const response = await fetch(`/api/v1/public/invitations/${encodeURIComponent(slug)}/messages`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(input),
  })
  if (!response.ok) throw new Error(await errorMessage(response))
}

async function adminSend<T>(url: string, method: 'GET' | 'POST' | 'PUT' | 'DELETE', body?: unknown): Promise<T> {
  if (method === 'GET') {
    const response = await fetch(url, { credentials: 'include' })
    if (!response.ok) throw new Error(await errorMessage(response))
    return response.json() as Promise<T>
  }
  const csrfResponse = await fetch('/api/v1/admin/auth/csrf', { credentials: 'include' })
  if (!csrfResponse.ok) throw new Error('تعذر تجهيز طلب آمن.')
  const { token } = await csrfResponse.json() as { token: string }
  const response = await fetch(url, { method, credentials: 'include', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token }, body: body === undefined ? undefined : JSON.stringify(body) })
  if (!response.ok) throw new Error(await errorMessage(response))
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export function getAdminRsvps(invitationId: number) { return adminSend<{ yesCount: number; noCount: number; expectedAttendance: number; responses: Array<{ rsvpResponseId: number; response: string; guestName: string | null; accompanyingPeople: number; expectedAttendance: number; submittedAt: string }> }>(`/api/v1/admin/invitations/${invitationId}/rsvps`, 'GET') }
export function getAdminGuestMessages(invitationId: number) { return adminSend<Array<{ guestMessageId: number; guestName: string | null; message: string; submittedAt: string; deletedAt: string | null }>>(`/api/v1/admin/invitations/${invitationId}/messages`, 'GET') }
export function deleteAdminGuestMessage(invitationId: number, messageId: number) { return adminSend<void>(`/api/v1/admin/invitations/${invitationId}/messages/${messageId}`, 'DELETE') }
export function restoreAdminGuestMessage(invitationId: number, messageId: number) { return adminSend<void>(`/api/v1/admin/invitations/${invitationId}/messages/${messageId}/restore`, 'POST') }
export function getMusicTracks() { return adminSend<MusicTrack[]>('/api/v1/admin/music-tracks', 'GET') }
export function updateMusicTrack(track: MusicTrack) { return adminSend<MusicTrack>(`/api/v1/admin/music-tracks/${track.musicTrackId}`, 'PUT', track) }
export function selectInvitationMusic(invitationId: number, musicTrackId: number | null) { return adminSend<void>(`/api/v1/admin/invitations/${invitationId}/music`, 'PUT', { musicTrackId }) }

export async function createMusicTrack(input: { title: string; artist: string; rightsReference: string; file: File }) {
  const csrfResponse = await fetch('/api/v1/admin/auth/csrf', { credentials: 'include' })
  if (!csrfResponse.ok) throw new Error('تعذر تجهيز طلب آمن.')
  const { token } = await csrfResponse.json() as { token: string }
  const body = new FormData(); body.set('title', input.title); body.set('artist', input.artist); body.set('rightsReference', input.rightsReference); body.set('file', input.file)
  const response = await fetch('/api/v1/admin/music-tracks', { method: 'POST', credentials: 'include', headers: { 'X-CSRF-TOKEN': token }, body })
  if (!response.ok) throw new Error(await errorMessage(response))
  return response.json() as Promise<MusicTrack>
}

export function invitationAction(invitationId: number, action: 'publish' | 'pause' | 'reactivate' | 'extend'): Promise<void> {
  return secureSend(`/api/v1/admin/invitations/${invitationId}/${action}`, 'POST')
}

export async function saveFinalInvitationCard(invitationId: number, blob: Blob): Promise<void> {
  const csrfResponse = await fetch('/api/v1/admin/auth/csrf', { credentials: 'include' })
  if (!csrfResponse.ok) throw new Error('تعذر تجهيز طلب آمن. حدّث الصفحة وحاول مرة أخرى.')
  const { token } = await csrfResponse.json() as { token: string }
  const form = new FormData()
  form.append('file', blob, 'wedlink-invitation.png')
  const response = await fetch(`/api/v1/admin/invitations/${invitationId}/final-card`, {
    method: 'POST', credentials: 'include', headers: { 'X-CSRF-TOKEN': token }, body: form,
  })
  if (!response.ok) throw new Error(await errorMessage(response))
}

export function downloadFinalInvitationCard(invitationId: number): string {
  return `/api/v1/admin/invitations/${invitationId}/final-card`
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

export async function getAdminInvitationMedia(orderId: number, mediaId: number): Promise<string> {
  const response = await fetch(`${adminPath}/${orderId}/customer-submission/media/${mediaId}/content`, { credentials: 'include' })
  if (!response.ok) throw new Error(await errorMessage(response))
  return URL.createObjectURL(await response.blob())
}
