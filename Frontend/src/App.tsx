import { Link, Route, Routes, useParams } from 'react-router-dom'
import { LoginPage } from './features/auth/LoginPage'
import { RequireAdmin } from './features/auth/RequireAdmin'
import { CustomersPage } from './features/customers/CustomersPage'
import { TemplatesPage } from './features/templates/TemplatesPage'
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

function HomePage() {
  return (
    <PageCard
      title="أهلًا بك في WedLink"
      description="المسارات الأساسية جاهزة. سنضيف صفحات الإدارة والدعوات خطوة بخطوة."
    />
  )
}

function AdminPage() {
  return (
    <section className="page-card">
      <p className="page-card__eyebrow">WedLink</p>
      <h1>لوحة الإدارة</h1>
      <p>اختر قسمًا لإدارة بيانات العمل.</p>
      <Link className="dashboard-link" to="/admin/customers">إدارة العملاء</Link>
      <Link className="dashboard-link" to="/admin/templates">إدارة القوالب والإضافات</Link>
    </section>
  )
}

function PublicInvitationPage() {
  const { slug } = useParams()

  return (
    <PageCard
      title="صفحة الدعوة"
      description={`مسار الدعوة العامة جاهز. رمز الدعوة الحالي: ${slug ?? ''}`}
    />
  )
}

function CustomerSubmissionPage() {
  return (
    <PageCard
      title="إرسال بيانات الزفاف"
      description="هذا مسار العميل الخاص، وسيُربط بنموذج الإرسال لاحقًا."
    />
  )
}

function PrivatePreviewPage() {
  return (
    <PageCard
      title="معاينة خاصة"
      description="هذا مسار معاينة العميل، وسيُربط بالدعوة لاحقًا."
    />
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
  return (
    <div className="app-shell">
      <header className="site-header">
        <Link className="site-brand" to="/">
          WedLink
        </Link>
        <nav aria-label="التنقل الرئيسي" className="site-nav">
          <Link to="/">الرئيسية</Link>
          <Link to="/admin">لوحة الإدارة</Link>
        </nav>
      </header>

      <main className="page-content">
        <Routes>
          <Route element={<HomePage />} path="/" />
          <Route element={<LoginPage />} path="/login" />
          <Route element={<RequireAdmin><AdminPage /></RequireAdmin>} path="/admin/*" />
          <Route element={<RequireAdmin><CustomersPage /></RequireAdmin>} path="/admin/customers" />
          <Route element={<RequireAdmin><TemplatesPage /></RequireAdmin>} path="/admin/templates" />
          <Route element={<PublicInvitationPage />} path="/invite/:slug" />
          <Route element={<CustomerSubmissionPage />} path="/submit" />
          <Route element={<PrivatePreviewPage />} path="/preview" />
          <Route element={<NotFoundPage />} path="*" />
        </Routes>
      </main>
    </div>
  )
}

export default App
