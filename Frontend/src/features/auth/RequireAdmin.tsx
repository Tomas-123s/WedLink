import type { ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Navigate, useNavigate } from 'react-router-dom'
import { getCurrentAdmin, logoutAdmin } from './adminAuthApi'
import { setMyDisplayName } from './adminAccountsApi'

export function RequireAdmin({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const session = useQuery({
    queryKey: ['admin', 'session'],
    queryFn: getCurrentAdmin,
    retry: false,
    refetchOnWindowFocus: true,
    refetchInterval: 60_000,
  })
  const logout = useMutation({
    mutationFn: logoutAdmin,
    onSuccess: () => {
      queryClient.clear()
      navigate('/login', { replace: true })
    },
  })
  const updateName = useMutation({
    mutationFn: setMyDisplayName,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'session'] })
    },
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

  return (
    <>
      <div className="admin-session-bar" dir="rtl">
        {session.data.displayName ? (
          <strong className="admin-display-name">{session.data.displayName}</strong>
        ) : (
          <form className="admin-display-name-form" onSubmit={(event) => {
            event.preventDefault()
            const formData = new FormData(event.currentTarget)
            updateName.mutate(String(formData.get('displayName') ?? ''))
          }}>
            <label htmlFor="admin-display-name">أدخل اسمك مرة واحدة</label>
            <input
              id="admin-display-name"
              maxLength={100}
              name="displayName"
              required
            />
            <button className="secondary-button" disabled={updateName.isPending} type="submit">
              {updateName.isPending ? 'جارٍ الحفظ...' : 'حفظ الاسم'}
            </button>
          </form>
        )}
        {updateName.isError && <p className="form-error" role="alert">{updateName.error.message}</p>}
        <button
          className="secondary-button"
          disabled={logout.isPending}
          onClick={() => logout.mutate()}
          type="button"
        >
          {logout.isPending ? 'جارٍ تسجيل الخروج...' : 'تسجيل الخروج'}
        </button>
        {logout.isError && <p className="form-error" role="alert">{logout.error.message}</p>}
      </div>
      {children}
    </>
  )
}
