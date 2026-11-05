import type { ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { getCurrentAdmin, logoutAdmin } from './adminAuthApi'
import { setMyDisplayName } from './adminAccountsApi'

export function RequireAdmin({ children }: { children: ReactNode }) {
  const location = useLocation()
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

  const sections = [
    { to: '/admin/dashboard', label: 'نظرة عامة', mark: '◫' },
    { to: '/admin/orders', label: 'الطلبات والمدفوعات', mark: '▤' },
    { to: '/admin/customers', label: 'العملاء', mark: '♧' },
    { to: '/admin/templates', label: 'القوالب والإضافات', mark: '▧' },
    { to: '/admin/music', label: 'مكتبة الموسيقى', mark: '♫' },
    { to: '/admin/users', label: 'حسابات الإدارة', mark: '♙' },
    { to: '/admin/audit-logs', label: 'سجل النشاط', mark: '≋' },
  ]

  return (
    <div className="admin-workspace" dir="rtl">
      <aside className="admin-sidebar">
        <Link className="admin-sidebar__brand" to="/admin"><span>W</span><span>WedLink<small>مساحة الإدارة</small></span></Link>
        <Link className={`admin-sidebar__home${location.pathname === '/admin' ? ' is-active' : ''}`} to="/admin">الرئيسية</Link>
        <p className="admin-sidebar__label">إدارة المنصة</p>
        <nav aria-label="أقسام الإدارة" className="admin-sidebar__nav">
          {sections.map(section => <Link aria-current={location.pathname === section.to ? 'page' : undefined} className={location.pathname === section.to ? 'is-active' : ''} key={section.to} to={section.to}><span aria-hidden="true">{section.mark}</span>{section.label}</Link>)}
        </nav>
        <Link className="admin-sidebar__site" to="/">↗ <span>معاينة الموقع</span></Link>
      </aside>
      <div className="admin-workspace__main">
        <div className="admin-session-bar">
          <div className="admin-session-bar__identity">
            {session.data.displayName ? <><span className="admin-session-bar__avatar">{session.data.displayName.slice(0, 1)}</span><strong className="admin-display-name">{session.data.displayName}</strong></> : (
              <form className="admin-display-name-form" onSubmit={(event) => {
                event.preventDefault()
                const formData = new FormData(event.currentTarget)
                updateName.mutate(String(formData.get('displayName') ?? ''))
              }}>
                <label htmlFor="admin-display-name">أدخل اسمك مرة واحدة</label>
                <input id="admin-display-name" maxLength={100} name="displayName" required />
                <button className="secondary-button" disabled={updateName.isPending} type="submit">{updateName.isPending ? 'جارٍ الحفظ...' : 'حفظ الاسم'}</button>
              </form>
            )}
          </div>
          <div className="admin-session-bar__actions">
            {updateName.isError && <p className="form-error" role="alert">{updateName.error.message}</p>}
            <button className="secondary-button" disabled={logout.isPending} onClick={() => logout.mutate()} type="button">{logout.isPending ? 'جارٍ تسجيل الخروج...' : 'تسجيل الخروج'}</button>
            {logout.isError && <p className="form-error" role="alert">{logout.error.message}</p>}
          </div>
        </div>
        <div className="admin-workspace__content">{children}</div>
      </div>
    </div>
  )
}
