const authUrl = '/api/v1/admin/auth'

export interface AdminSession {
  id: string
  email: string
}

interface CsrfResponse {
  token: string
}

export async function getCurrentAdmin(): Promise<AdminSession | null> {
  const response = await fetch(`${authUrl}/me`, {
    credentials: 'include',
  })

  if (response.status === 401) {
    return null
  }

  if (!response.ok) {
    throw new Error('تعذر التحقق من جلسة الدخول.')
  }

  return response.json() as Promise<AdminSession>
}

export async function loginAdmin(email: string, password: string): Promise<void> {
  const csrfResponse = await fetch(`${authUrl}/csrf`, {
    credentials: 'include',
  })

  if (!csrfResponse.ok) {
    throw new Error('تعذر تجهيز تسجيل الدخول. حاول مرة أخرى.')
  }

  const { token } = (await csrfResponse.json()) as CsrfResponse
  const loginResponse = await fetch(`${authUrl}/login`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF-TOKEN': token,
    },
    body: JSON.stringify({ email, password }),
  })

  if (loginResponse.status === 401) {
    throw new Error('البريد الإلكتروني أو كلمة المرور غير صحيحة.')
  }

  if (!loginResponse.ok) {
    throw new Error('تعذر تسجيل الدخول. حاول مرة أخرى.')
  }
}
