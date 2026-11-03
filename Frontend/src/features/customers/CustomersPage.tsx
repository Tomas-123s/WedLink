import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  deleteCustomer,
  getCustomers,
  saveCustomer,
  type Customer,
  type CustomerRequest,
} from './customersApi'

const emptyForm: CustomerRequest = {
  name: '',
  phone: '',
  email: '',
  notes: '',
}

export function CustomersPage() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [editingId, setEditingId] = useState<number | undefined>()
  const [form, setForm] = useState<CustomerRequest>(emptyForm)
  const queryClient = useQueryClient()

  const customersQuery = useQuery({
    queryKey: ['customers', search, page],
    queryFn: () => getCustomers(search, page),
  })

  const saveMutation = useMutation({
    mutationFn: () => saveCustomer(form, editingId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['customers'] })
      setEditingId(undefined)
      setForm(emptyForm)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteCustomer,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['customers'] }),
  })

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSearch(searchInput.trim())
    setPage(1)
  }

  function submitForm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    saveMutation.mutate()
  }

  function startEditing(customer: Customer) {
    setEditingId(customer.customerId)
    setForm({
      name: customer.name,
      phone: customer.phone,
      email: customer.email ?? '',
      notes: customer.notes ?? '',
    })
  }

  function cancelEditing() {
    setEditingId(undefined)
    setForm(emptyForm)
    saveMutation.reset()
  }

  return (
    <section className="customers-page" dir="rtl">
      <div className="section-heading">
        <div>
          <p className="page-card__eyebrow">WedLink / الإدارة</p>
          <h1>العملاء</h1>
          <p>إضافة بيانات العملاء والبحث فيها وتحديثها.</p>
        </div>
      </div>

      <div className="customer-layout">
        <section className="customer-panel">
          <h2>{editingId ? 'تعديل بيانات العميل' : 'إضافة عميل'}</h2>
          <form className="customer-form" onSubmit={submitForm}>
            <label htmlFor="customer-name">اسم العميل</label>
            <input
              id="customer-name"
              maxLength={150}
              onChange={(event) => setForm({ ...form, name: event.target.value })}
              required
              value={form.name}
            />

            <label htmlFor="customer-phone">رقم الهاتف</label>
            <input
              id="customer-phone"
              maxLength={30}
              onChange={(event) => setForm({ ...form, phone: event.target.value })}
              required
              value={form.phone}
            />
            <small>يجب أن يكون رقم الهاتف فريدًا؛ تُزال المسافات عند الحفظ.</small>

            <label htmlFor="customer-email">البريد الإلكتروني (اختياري)</label>
            <input
              autoComplete="email"
              id="customer-email"
              maxLength={254}
              onChange={(event) => setForm({ ...form, email: event.target.value })}
              type="email"
              value={form.email ?? ''}
            />

            <label htmlFor="customer-notes">ملاحظات (اختياري)</label>
            <textarea
              id="customer-notes"
              maxLength={1000}
              onChange={(event) => setForm({ ...form, notes: event.target.value })}
              rows={3}
              value={form.notes ?? ''}
            />

            {saveMutation.isError && (
              <p className="form-error" role="alert">{saveMutation.error.message}</p>
            )}

            <div className="form-actions">
              <button className="primary-button" disabled={saveMutation.isPending} type="submit">
                {saveMutation.isPending ? 'جارٍ الحفظ...' : editingId ? 'حفظ التعديل' : 'إضافة العميل'}
              </button>
              {editingId && <button className="secondary-button" onClick={cancelEditing} type="button">إلغاء</button>}
            </div>
          </form>
        </section>

        <section className="customer-panel customer-list">
          <div className="list-heading">
            <h2>قائمة العملاء</h2>
            <span>{customersQuery.data?.totalItems ?? 0} عميل</span>
          </div>

          <form className="customer-search" onSubmit={submitSearch}>
            <label className="visually-hidden" htmlFor="customer-search">ابحث بالاسم أو الهاتف</label>
            <input
              id="customer-search"
              onChange={(event) => setSearchInput(event.target.value)}
              placeholder="ابحث بالاسم أو الهاتف"
              value={searchInput}
            />
            <button className="secondary-button" type="submit">بحث</button>
          </form>

          {customersQuery.isPending && <p role="status">جارٍ تحميل العملاء...</p>}
          {customersQuery.isError && <p className="form-error" role="alert">{customersQuery.error.message}</p>}
          {deleteMutation.isError && <p className="form-error" role="alert">{deleteMutation.error.message}</p>}

          {customersQuery.data && customersQuery.data.items.length === 0 && (
            <p className="empty-state">لا يوجد عملاء مطابقون للبحث.</p>
          )}

          {customersQuery.data && customersQuery.data.items.length > 0 && (
            <div className="table-scroll">
              <table className="customers-table">
                <thead>
                  <tr><th>الاسم</th><th>الهاتف</th><th>البريد</th><th>الإجراءات</th></tr>
                </thead>
                <tbody>
                  {customersQuery.data.items.map((customer) => (
                    <tr key={customer.customerId}>
                      <td>{customer.name}</td>
                      <td dir="ltr">{customer.phone}</td>
                      <td>{customer.email || '—'}</td>
                      <td className="table-actions">
                        <button className="text-button" onClick={() => startEditing(customer)} type="button">تعديل</button>
                        <button
                          className="text-button text-button--danger"
                          disabled={deleteMutation.isPending}
                          onClick={() => {
                            if (window.confirm(`هل تريد حذف العميل «${customer.name}»؟`)) {
                              deleteMutation.mutate(customer.customerId)
                            }
                          }}
                          type="button"
                        >حذف</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {customersQuery.data && customersQuery.data.totalPages > 1 && (
            <div className="pagination">
              <button className="secondary-button" disabled={page <= 1} onClick={() => setPage(page - 1)} type="button">السابق</button>
              <span>صفحة {page} من {customersQuery.data.totalPages}</span>
              <button className="secondary-button" disabled={page >= customersQuery.data.totalPages} onClick={() => setPage(page + 1)} type="button">التالي</button>
            </div>
          )}
        </section>
      </div>
    </section>
  )
}
