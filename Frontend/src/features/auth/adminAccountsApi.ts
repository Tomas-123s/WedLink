const usersUrl = '/api/v1/admin/users'

export interface AdminAccount {
  id: string
  email: string
  isActive: boolean
  roles: string[]
}

async function errorMessage(response: Response): Promise<string> {
  const body = await response.json().catch(() => null)
  return body?.message ?? body?.detail ?? 'تعذر تنفيذ الطلب. حاول مرة أخرى.'
}

export async function getAdminAccounts(): Promise<AdminAccount[]> {
  const response = await fetch(usersUrl, { credentials: 'include' })
  if (!response.ok) {
    throw new Error(await errorMessage(response))
  }
  return response.json() as Promise<AdminAccount[]>
}

async function getCsrfToken(): Promise<string> {
  const response = await fetch('/api/v1/admin/auth/csrf', {
    credentials: 'include',
  })
  if (!response.ok) {
    throw new Error('تعذر تجهيز الطلب الآمن. حدّث الصفحة وحاول مرة أخرى.')
  }
  const result = (await response.json()) as { token: string }
  return result.token
}

async function sendSecureRequest<T>(
  path: string,
  method: 'POST' | 'PATCH' | 'PUT',
  body: unknown,
): Promise<T> {
  const token = await getCsrfToken()
  const response = await fetch(`${usersUrl}${path}`, {
    method,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF-TOKEN': token,
    },
    body: JSON.stringify(body),
  })
  if (!response.ok) {
    throw new Error(await errorMessage(response))
  }
  return response.json() as Promise<T>
}

export function createAdminAccount(input: {
  email: string
  password: string
  roles: string[]
}): Promise<AdminAccount> {
  return sendSecureRequest('', 'POST', input)
}

export function setAdminAccountActive(
  id: string,
  isActive: boolean,
): Promise<AdminAccount> {
  return sendSecureRequest(`/${encodeURIComponent(id)}/status`, 'PATCH', { isActive })
}

export function setAdminAccountRoles(
  id: string,
  roles: string[],
): Promise<AdminAccount> {
  return sendSecureRequest(`/${encodeURIComponent(id)}/roles`, 'PUT', { roles })
}
