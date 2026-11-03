import { useState, type FormEvent } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { loginAdmin } from './adminAuthApi'

export function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const login = useMutation({
    mutationFn: () => loginAdmin(email.trim(), password),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'session'] })
      navigate('/admin', { replace: true })
    },
  })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    login.mutate()
  }

  return (
    <section className="login-card" dir="rtl">
      <div className="login-card__intro">
        <p className="login-card__eyebrow">WedLink</p>
        <h1>تسجيل دخول الإدارة</h1>
        <p>سجّل الدخول لإدارة الطلبات والدعوات.</p>
      </div>

      <form className="login-form" onSubmit={handleSubmit}>
        <label htmlFor="admin-email">البريد الإلكتروني</label>
        <input
          autoComplete="username"
          id="admin-email"
          name="email"
          onChange={(event) => setEmail(event.target.value)}
          required
          type="email"
          value={email}
        />

        <label htmlFor="admin-password">كلمة المرور</label>
        <input
          autoComplete="current-password"
          id="admin-password"
          name="password"
          onChange={(event) => setPassword(event.target.value)}
          required
          type="password"
          value={password}
        />

        {login.isError && <p className="login-error" role="alert">{login.error.message}</p>}

        <button className="login-button" disabled={login.isPending} type="submit">
          {login.isPending ? 'جارٍ تسجيل الدخول...' : 'دخول'}
        </button>
      </form>
    </section>
  )
}
