const adminUrl = '/api/v1/admin'

export interface Feature {
  featureId: number
  key: string
  name: string
  description: string | null
}

export interface TemplateFeature {
  featureId: number
  key: string
  name: string
  isEnabled: boolean
  maximumCount: number | null
  maximumFileSizeMb: number | null
}

export interface AddOn {
  addOnId: number
  name: string
  description: string | null
  currentPrice: number
  currency: string
  isActive: boolean
}

export interface TemplateVersion {
  templateVersionId: number
  templateId: number
  versionNumber: string
  packageKey: string
  isActive: boolean
  createdAt: string
  features: TemplateFeature[]
  addOns: AddOn[]
}

export interface Template {
  templateId: number
  name: string
  description: string | null
  previewImageReference: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
  versions: TemplateVersion[]
}

export interface TemplatePage {
  items: Template[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface TemplateRequest {
  name: string
  description: string | null
  previewImageReference: string | null
}

export interface TemplateFeatureRequest {
  featureId: number
  isEnabled: boolean
  maximumCount: number | null
  maximumFileSizeMb: number | null
}

export interface AddOnRequest {
  name: string
  description: string | null
  currentPrice: number
  isActive: boolean
}

async function getErrorMessage(response: Response): Promise<string> {
  const problem = await response.json().catch(() => null)
  return problem?.detail ?? problem?.title ?? 'تعذر تنفيذ الطلب. حاول مرة أخرى.'
}

async function sendRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${adminUrl}${path}`, {
    credentials: 'include',
    ...init,
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

async function getCsrfToken(): Promise<string> {
  const result = await sendRequest<{ token: string }>('/auth/csrf')
  return result.token
}

async function sendSecureRequest<T>(
  path: string,
  method: 'POST' | 'PUT' | 'PATCH',
  body?: unknown,
): Promise<T> {
  const token = await getCsrfToken()
  return sendRequest<T>(path, {
    method,
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF-TOKEN': token,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

export function getTemplates(): Promise<TemplatePage> {
  return sendRequest('/templates?page=1&pageSize=100')
}

export function getTemplate(templateId: number): Promise<Template> {
  return sendRequest(`/templates/${templateId}`)
}

export function getFeatures(): Promise<Feature[]> {
  return sendRequest('/features')
}

export function getAddOns(): Promise<AddOn[]> {
  return sendRequest('/addons')
}

export function createTemplate(input: TemplateRequest): Promise<Template> {
  return sendSecureRequest('/templates', 'POST', input)
}

export function updateTemplate(templateId: number, input: TemplateRequest): Promise<Template> {
  return sendSecureRequest(`/templates/${templateId}`, 'PUT', input)
}

export function setTemplateActive(templateId: number, isActive: boolean): Promise<void> {
  return sendSecureRequest(`/templates/${templateId}/status`, 'PATCH', { isActive })
}

export function createTemplateVersion(
  templateId: number,
  versionNumber: string,
  packageKey: string,
): Promise<TemplateVersion> {
  return sendSecureRequest(`/templates/${templateId}/versions`, 'POST', {
    versionNumber,
    packageKey,
  })
}

export function saveTemplateFeatures(
  versionId: number,
  features: TemplateFeatureRequest[],
): Promise<void> {
  return sendSecureRequest(`/template-versions/${versionId}/features`, 'PUT', features)
}

export function saveTemplateAddOns(versionId: number, addOnIds: number[]): Promise<void> {
  return sendSecureRequest(`/template-versions/${versionId}/addons`, 'PUT', { addOnIds })
}

export function activateTemplateVersion(versionId: number): Promise<void> {
  return sendSecureRequest(`/template-versions/${versionId}/activate`, 'POST')
}

export function createAddOn(input: AddOnRequest): Promise<AddOn> {
  return sendSecureRequest('/addons', 'POST', input)
}

export function updateAddOn(addOnId: number, input: AddOnRequest): Promise<AddOn> {
  return sendSecureRequest(`/addons/${addOnId}`, 'PUT', input)
}
