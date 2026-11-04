import { Link, Route, Routes, useLocation } from 'react-router-dom'
import { LoginPage } from './features/auth/LoginPage'
import { RequireAdmin } from './features/auth/RequireAdmin'
import { AdminUsersPage } from './features/auth/AdminUsersPage'
import { CustomersPage } from './features/customers/CustomersPage'
import { TemplatesPage } from './features/templates/TemplatesPage'
import { OrdersPage } from './features/orders/OrdersPage'
import { CustomerSubmissionPage } from './features/customer-submission/CustomerSubmissionPage'
import { InvitationPreviewPage } from './features/invitations/InvitationPreviewPage'
import { PublicInvitationPage } from './features/invitations/PublicInvitationPage'
import { MusicLibraryPage } from './features/music/MusicLibraryPage'
import { AdminDashboardPage } from './features/dashboard/AdminDashboardPage'
import { AuditLogsPage } from './features/audit/AuditLogsPage'
import { PublicWebsitePage } from './features/public-site/PublicWebsitePage'
import { PublicDesignPreviewPage } from './features/public-site/PublicDesignPreviewPage'
import './App.css'

function PageCard({ title, description }: { title: string; description: string }) {
  return (
    <section className="page-card">
      <p className="page-card__eyebrow">WedLink</p>
      <h1>{title}</h1>
      <p>{description}</p>
    </section>
  )
}

function AdminPage() {
  return (
    <section className="page-card">
      <p className="page-card__eyebrow">WedLink</p>
      <h1>لوحة الإدارة</h1>
      <p>اختر مهمة للبدء في إدارة WedLink.</p>
      <Link className="dashboard-link" to="/admin/customers">إدارة العملاء</Link>
      <Link className="dashboard-link" to="/admin/orders">إدارة الطلبات والمدفوعات</Link>
      <Link className="dashboard-link" to="/admin/templates">إدارة القوالب والإضافات</Link>
      <Link className="dashboard-link" to="/admin/music">مكتبة الموسيقى</Link>
      <Link className="dashboard-link" to="/admin/users">إدارة حسابات الإدارة</Link>
    </section>
  )
}

function NotFoundPage() {
  return (
    <PageCard
      title="الصفحة غير موجودة"
      description="تأكد من الرابط وحاول مرة أخرى."
    />
  )
}

function App() {
  const location = useLocation()
  const isPublicInvitation = location.pathname.startsWith('/invite/')
  const isPublicShowcase = location.pathname === '/' || location.pathname.startsWith('/designs/')
  return (
    <div className="app-shell">
      {!isPublicInvitation && !isPublicShowcase && <header className="site-header">
        <Link className="site-brand" to="/admin">
          WedLink
        </Link>
        <nav aria-label="التنقل الرئيسي" className="site-nav">
          <Link to="/">الرئيسية</Link>
          <Link to="/admin">لوحة الإدارة</Link>
        </nav>
      </header>}

      <main className={`page-content${isPublicShowcase ? ' page-content--public' : ''}`}>
        <Routes>
          <Route element={<PublicWebsitePage />} path="/" />
          <Route element={<PublicDesignPreviewPage />} path="/designs/:templateId" />
          <Route element={<LoginPage />} path="/login" />
          <Route element={<RequireAdmin><AdminPage /></RequireAdmin>} path="/admin" />
          <Route element={<RequireAdmin><AdminDashboardPage /></RequireAdmin>} path="/admin/dashboard" />
          <Route element={<RequireAdmin><AuditLogsPage /></RequireAdmin>} path="/admin/audit-logs" />
          <Route element={<RequireAdmin><AdminPage /></RequireAdmin>} path="/admin/*" />
          <Route element={<RequireAdmin><CustomersPage /></RequireAdmin>} path="/admin/customers" />
          <Route element={<RequireAdmin><OrdersPage /></RequireAdmin>} path="/admin/orders" />
          <Route element={<RequireAdmin><TemplatesPage /></RequireAdmin>} path="/admin/templates" />
          <Route element={<RequireAdmin><AdminUsersPage /></RequireAdmin>} path="/admin/users" />
          <Route element={<RequireAdmin><MusicLibraryPage /></RequireAdmin>} path="/admin/music" />
          <Route element={<PublicInvitationPage />} path="/invite/:slug" />
          <Route element={<CustomerSubmissionPage />} path="/submit" />
          <Route element={<InvitationPreviewPage />} path="/preview" />
          <Route element={<NotFoundPage />} path="*" />
        </Routes>
      </main>
    </div>
  )
}

export default App
