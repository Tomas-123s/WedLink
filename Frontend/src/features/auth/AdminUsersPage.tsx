import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createAdminAccount, getAdminAccounts, setAdminAccountActive, setAdminAccountRoles } from './adminAccountsApi'
import type { AdminAccount } from './adminAccountsApi'

const adminUsersKey = ['admin', 'users']

export function AdminUsersPage() {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [createSuperAdmin, setCreateSuperAdmin] = useState(false)
  const [roleDrafts, setRoleDrafts] = useState<Record<string, string[]>>({})
  const queryClient = useQueryClient()
  const accounts = useQuery({ queryKey: adminUsersKey, queryFn: getAdminAccounts })
  const refreshAccounts = () => queryClient.invalidateQueries({ queryKey: adminUsersKey })

  const create = useMutation({
    mutationFn: () => createAdminAccount({
      displayName: displayName.trim(),
      email: email.trim(),
      password,
      roles: createSuperAdmin ? ['Admin', 'SuperAdmin'] : ['Admin'],
    }),
    onSuccess: async () => {
      setDisplayName('')
      setEmail('')
      setPassword('')
      setCreateSuperAdmin(false)
      await refreshAccounts()
    },
  })
  const setActive = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      setAdminAccountActive(id, isActive),
    onSuccess: refreshAccounts,
  })
  const setRoles = useMutation({
    mutationFn: ({ id, roles }: { id: string; roles: string[] }) => setAdminAccountRoles(id, roles),
    onSuccess: async (_account, variables) => {
      setRoleDrafts((current) => {
        const next = { ...current }
        delete next[variables.id]
        return next
      })
      await refreshAccounts()
    },
  })

  function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    create.mutate()
  }

  function rolesFor(account: AdminAccount) {
    return roleDrafts[account.id] ?? account.roles
  }

  function toggleRole(account: AdminAccount, role: string) {
    const currentRoles = rolesFor(account)
    const nextRoles = currentRoles.includes(role)
      ? currentRoles.filter((item) => item !== role)
      : [...currentRoles, role]
    setRoleDrafts((current) => ({ ...current, [account.id]: nextRoles }))
  }

  return (
    <section className="admin-users-page" dir="rtl">
      <header className="section-heading">
        <p className="page-card__eyebrow">WedLink</p>
        <h1>إدارة حسابات الإدارة</h1>
        <p>إدارة المستخدمين والأدوار متاحة لحسابات SuperAdmin فقط. دور Admin لا يتيح الدخول إلى وظائف النظام.</p>
      </header>

      <div className="customer-layout">
        <section className="customer-panel">
          <h2>إضافة حساب</h2>
          <form className="customer-form" onSubmit={handleCreate}>
            <label htmlFor="admin-account-display-name">اسم المسؤول</label>
            <input
              autoComplete="name"
              id="admin-account-display-name"
              maxLength={100}
              onChange={(event) => setDisplayName(event.target.value)}
              required
              value={displayName}
            />
            <label htmlFor="admin-account-email">البريد الإلكتروني</label>
            <input
              autoComplete="email"
              id="admin-account-email"
              onChange={(event) => setEmail(event.target.value)}
              required
              type="email"
              value={email}
            />
            <label htmlFor="admin-account-password">كلمة مرور مؤقتة</label>
            <input
              autoComplete="new-password"
              id="admin-account-password"
              onChange={(event) => setPassword(event.target.value)}
              required
              type="password"
              value={password}
            />
            <small>8 أحرف على الأقل، تشمل حرفًا كبيرًا وصغيرًا ورقمًا ورمزًا.</small>
            <label className="admin-role-option">
              <input
                checked={createSuperAdmin}
                onChange={(event) => setCreateSuperAdmin(event.target.checked)}
                type="checkbox"
              />
              إضافة دور SuperAdmin
            </label>
            {create.isError && <p className="form-error" role="alert">{create.error.message}</p>}
            <button className="primary-button" disabled={create.isPending} type="submit">
              {create.isPending ? 'جارٍ الحفظ...' : 'إنشاء الحساب'}
            </button>
          </form>
        </section>

        <section className="customer-panel">
          <div className="list-heading">
            <h2>الحسابات</h2>
            <span>{accounts.data?.length ?? 0} حساب</span>
          </div>
          {accounts.isPending && <p role="status">جارٍ تحميل الحسابات...</p>}
          {accounts.isError && <p className="form-error" role="alert">{accounts.error.message}</p>}
          {[setActive, setRoles].some((mutation) => mutation.isError) && (
            <p className="form-error" role="alert">
              {[setActive, setRoles].find((mutation) => mutation.isError)?.error?.message}
            </p>
          )}
          {accounts.data && (
            <div className="table-scroll">
              <table className="customers-table">
                <thead>
                  <tr><th>الاسم</th><th>الحالة</th><th>الأدوار</th><th>الإجراءات</th></tr>
                </thead>
                <tbody>
                  {accounts.data.map((account) => {
                    const roles = rolesFor(account)
                    const hasDraft = roleDrafts[account.id] !== undefined
                    return (
                      <tr key={account.id}>
                        <td>{account.displayName || '—'}</td>
                        <td>{account.isActive ? 'نشط' : 'متوقف'}</td>
                        <td>
                          <div className="admin-role-list">
                            {['Admin', 'SuperAdmin'].map((role) => (
                              <label key={role}>
                                <input
                                  checked={roles.includes(role)}
                                  onChange={() => toggleRole(account, role)}
                                  type="checkbox"
                                />
                                {role}
                              </label>
                            ))}
                          </div>
                        </td>
                        <td>
                          <div className="admin-account-actions">
                            {hasDraft && (
                              <button
                                className="secondary-button"
                                disabled={setRoles.isPending}
                                onClick={() => setRoles.mutate({ id: account.id, roles })}
                                type="button"
                              >حفظ الأدوار</button>
                            )}
                            <button
                              className="secondary-button"
                              disabled={setActive.isPending}
                              onClick={() => setActive.mutate({ id: account.id, isActive: !account.isActive })}
                              type="button"
                            >{account.isActive ? 'إيقاف' : 'تفعيل'}</button>
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </section>
  )
}
