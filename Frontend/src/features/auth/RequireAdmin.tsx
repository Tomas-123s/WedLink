import type { ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Navigate } from 'react-router-dom'
import { getCurrentAdmin } from './adminAuthApi'

export function RequireAdmin({ children }: { children: ReactNode }) {
  const session = useQuery({
    queryKey: ['admin', 'session'],
    queryFn: getCurrentAdmin,
    retry: false,
  })

  if (session.isPending) {
    return <p className="auth-status" role="status">جارٍ التحقق من تسجيل الدخول...</p>
  }

  if (session.isError) {
    return <p className="auth-status" role="alert">تعذر الاتصال بالخادم. حاول تحديث الصفحة.</p>
  }

  if (!session.data) {
    return <Navigate to="/login" replace />
  }

  return children
}
