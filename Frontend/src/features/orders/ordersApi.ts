import { getCustomers } from '../customers/customersApi'
import { getTemplates } from '../templates/templatesApi'

const ordersUrl = '/api/v1/admin/orders'

export interface OrderSummary {
  orderId: number
  customerId: number
  customerName: string
  selectedTemplateVersionId: number
  templateName: string
  templateVersionNumber: string
  status: string
  agreedBasePrice: number
  currency: string
  addOnsTotal: number
  totalAmount: number
  createdAt: string
}

export interface OrderPage {
  items: OrderSummary[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface OrderPayment {
  paymentId: number
  orderId: number
  amount: number
  currency: string
  method: string
  reference: string | null
  note: string | null
  recordedByAdminUserId: string
  recordedAt: string
}

export interface OrderBalance {
  orderId: number
  agreedBasePrice: number
  addOnsTotal: number
  totalAmount: number
  paidAmount: number
  balance: number
  currency: string
  payments: OrderPayment[]
}

export interface CustomerSubmission {
  orderId: number
  customerName: string
  brideName: string
  groomName: string
  weddingDateTimeLocal: string | null
  timeZoneId: string
  venueName: string | null
  venueAddress: string | null
  locationUrl: string | null
  customizations: Record<string, unknown> | null
  submittedAt: string | null
  adminReviewedAt: string | null
  media: { mediaId: number; orderId: number; originalFileName: string; contentType: string; sizeBytes: number; mediaKind: string; uploadedAt: string }[]
  availableFeatures: { key: string; name: string; description: string | null; maximumCount: number | null; maximumFileSizeMb: number | null }[]
}

export const getOrderCustomers = getCustomers
export const getOrderTemplates = getTemplates

async function getErrorMessage(response: Response): Promise<string> {
  const problem = await response.json().catch(() => null)
  return problem?.detail ?? 'تعذر تنفيذ الطلب. حاول مرة أخرى.'
}

async function send<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${ordersUrl}${path}`, { credentials: 'include', ...init })
  if (!response.ok) throw new Error(await getErrorMessage(response))
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

async function sendSecure<T>(path: string, method: 'POST' | 'PATCH' | 'DELETE', body?: unknown): Promise<T> {
  const csrfResponse = await fetch('/api/v1/admin/auth/csrf', { credentials: 'include' })
  if (!csrfResponse.ok) throw new Error('تعذر تجهيز طلب آمن. حدّث الصفحة وحاول مرة أخرى.')
  const { token } = await csrfResponse.json() as { token: string }
  return send<T>(path, {
    method,
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

export function getOrders(search: string, status: string, page: number): Promise<OrderPage> {
  const query = new URLSearchParams({ page: String(page), pageSize: '10' })
  if (search.trim()) query.set('search', search.trim())
  if (status) query.set('status', status)
  return send(`?${query}`)
}

export function getOrderBalance(orderId: number): Promise<OrderBalance> {
  return send(`/${orderId}/balance`)
}

export function createOrder(input: {
  customerId: number
  selectedTemplateVersionId: number
  agreedBasePrice: number
  addOnIds: number[]
}): Promise<OrderSummary> {
  return sendSecure('', 'POST', input)
}

export function changeOrderStatus(orderId: number, status: string): Promise<void> {
  return sendSecure(`/${orderId}/status`, 'PATCH', { status, note: null })
}

export function recordOrderPayment(orderId: number, input: {
  amount: number
  method: string
  reference: string | null
  note: string | null
}): Promise<OrderPayment> {
  return sendSecure(`/${orderId}/payments`, 'POST', input)
}

export function createSubmissionLink(orderId: number): Promise<{ linkId: number; submissionUrl: string; expiresAt: string }> {
  return sendSecure(`/${orderId}/submission-links`, 'POST')
}

export function getCurrentSubmissionLink(orderId: number): Promise<{ linkId: number; expiresAt: string } | null> {
  return fetch(`/api/v1/admin/orders/${orderId}/submission-links/current`, { credentials: 'include' }).then(async (response) => {
    if (!response.ok) throw new Error(await getErrorMessage(response))
    if (response.status === 204) return null
    return response.json() as Promise<{ linkId: number; expiresAt: string } | null>
  })
}

export function getCustomerSubmission(orderId: number): Promise<CustomerSubmission> {
  return fetch(`/api/v1/admin/orders/${orderId}/customer-submission`, { credentials: 'include' }).then(async (response) => {
    if (!response.ok) throw new Error(await getErrorMessage(response))
    return response.json() as Promise<CustomerSubmission>
  })
}

export function markSubmissionReviewed(orderId: number): Promise<void> {
  return sendSecure(`/${orderId}/customer-submission/review`, 'POST')
}

export function deleteSubmissionMedia(orderId: number, mediaId: number): Promise<void> {
  return sendSecure(`/${orderId}/customer-submission/media/${mediaId}`, 'DELETE')
}

export async function revokeSubmissionLink(orderId: number, linkId: number): Promise<void> {
  return sendSecure(`/${orderId}/submission-links/${linkId}`, 'DELETE')
}
