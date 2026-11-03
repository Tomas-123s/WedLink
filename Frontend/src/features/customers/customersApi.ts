const customersUrl = '/api/v1/admin/customers'

export interface Customer {
  customerId: number
  name: string
  phone: string
  email: string | null
  notes: string | null
  createdAt: string
  updatedAt: string | null
}

export interface CustomerRequest {
  name: string
  phone: string
  email: string | null
  notes: string | null
}

export interface CustomerPage {
  items: Customer[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

async function getErrorMessage(response: Response): Promise<string> {
  const problem = await response.json().catch(() => null)
  return problem?.detail ?? 'تعذر تنفيذ الطلب. حاول مرة أخرى.'
}

async function getCsrfToken(): Promise<string> {
  const response = await fetch('/api/v1/admin/auth/csrf', {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('تعذر تجهيز طلب آمن. حدّث الصفحة وحاول مرة أخرى.')
  }

  const result = (await response.json()) as { token: string }
  return result.token
}

export async function getCustomers(search: string, page: number): Promise<CustomerPage> {
  const query = new URLSearchParams({ page: String(page), pageSize: '10' })
  if (search.trim()) {
    query.set('search', search.trim())
  }

  const response = await fetch(`${customersUrl}?${query}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }

  return response.json() as Promise<CustomerPage>
}

export async function saveCustomer(
  customer: CustomerRequest,
  customerId?: number,
): Promise<void> {
  const token = await getCsrfToken()
  const response = await fetch(
    customerId ? `${customersUrl}/${customerId}` : customersUrl,
    {
      method: customerId ? 'PUT' : 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': token,
      },
      body: JSON.stringify(customer),
    },
  )

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }
}

export async function deleteCustomer(customerId: number): Promise<void> {
  const token = await getCsrfToken()
  const response = await fetch(`${customersUrl}/${customerId}`, {
    method: 'DELETE',
    credentials: 'include',
    headers: { 'X-CSRF-TOKEN': token },
  })

  if (!response.ok) {
    throw new Error(await getErrorMessage(response))
  }
}
