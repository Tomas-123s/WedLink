import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  changeOrderStatus,
  createSubmissionLink,
  createOrder,
  deleteOrder,
  deleteSubmissionMedia,
  getCustomerSubmission,
  getCurrentSubmissionLink,
  getOrderBalance,
  getOrders,
  getOrderCustomers,
  getOrderTemplates,
  recordOrderPayment,
  markSubmissionReviewed,
  revokeSubmissionLink,
  type OrderSummary,
} from './ordersApi'
import { InvitationEditor } from '../invitations/InvitationEditor'

const money = (amount: number, currency = 'EGP') => new Intl.NumberFormat('ar-EG', {
  style: 'currency', currency, maximumFractionDigits: 2,
}).format(amount)

const nextStatuses: Record<string, string[]> = {
  New: ['Confirmed', 'Cancelled'],
  Confirmed: ['InProgress', 'Cancelled'],
  InProgress: ['Completed'],
}

export function OrdersPage() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [customerSearch, setCustomerSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)
  const [selectedOrder, setSelectedOrder] = useState<OrderSummary | null>(null)
  const [customerId, setCustomerId] = useState('')
  const [versionId, setVersionId] = useState('')
  const [basePrice, setBasePrice] = useState('')
  const [addOnIds, setAddOnIds] = useState<number[]>([])
  const [paymentAmount, setPaymentAmount] = useState('')
  const [paymentMethod, setPaymentMethod] = useState('نقدي')
  const [paymentReference, setPaymentReference] = useState('')
  const [paymentNote, setPaymentNote] = useState('')
  const [submissionLink, setSubmissionLink] = useState<{ linkId: number; url: string; expiresAt: string } | null>(null)
  const queryClient = useQueryClient()

  const ordersQuery = useQuery({ queryKey: ['orders', search, statusFilter, page], queryFn: () => getOrders(search, statusFilter, page) })
  const customersQuery = useQuery({ queryKey: ['order-customers', customerSearch], queryFn: () => getOrderCustomers(customerSearch, 1) })
  const templatesQuery = useQuery({ queryKey: ['order-templates'], queryFn: getOrderTemplates })
  const balanceQuery = useQuery({
    queryKey: ['order-balance', selectedOrder?.orderId],
    queryFn: () => getOrderBalance(selectedOrder!.orderId),
    enabled: Boolean(selectedOrder),
  })
  const submissionQuery = useQuery({
    queryKey: ['customer-submission', selectedOrder?.orderId],
    queryFn: () => getCustomerSubmission(selectedOrder!.orderId),
    enabled: Boolean(selectedOrder),
    retry: false,
  })
  const currentLinkQuery = useQuery({
    queryKey: ['current-submission-link', selectedOrder?.orderId],
    queryFn: () => getCurrentSubmissionLink(selectedOrder!.orderId),
    enabled: Boolean(selectedOrder),
  })

  const activeVersions = useMemo(() => templatesQuery.data?.items.flatMap((template) =>
    template.isActive ? template.versions.filter((version) => version.isActive).map((version) => ({ ...version, templateName: template.name, templatePrice: template.currentPrice })) : [],
  ) ?? [], [templatesQuery.data])
  const selectedVersion = activeVersions.find((version) => String(version.templateVersionId) === versionId)

  const refreshOrders = async () => {
    await queryClient.invalidateQueries({ queryKey: ['orders'] })
    if (selectedOrder) await queryClient.invalidateQueries({ queryKey: ['order-balance', selectedOrder.orderId] })
  }

  const createMutation = useMutation({
    mutationFn: createOrder,
    onSuccess: async (order) => {
      await queryClient.invalidateQueries({ queryKey: ['orders'] })
      setSelectedOrder({ ...order, templateName: activeVersions.find((item) => item.templateVersionId === order.selectedTemplateVersionId)?.templateName ?? '', templateVersionNumber: activeVersions.find((item) => item.templateVersionId === order.selectedTemplateVersionId)?.versionNumber ?? '' })
      setCustomerId(''); setVersionId(''); setBasePrice(''); setAddOnIds([])
    },
  })
  const statusMutation = useMutation({ mutationFn: ({ id, status }: { id: number; status: string }) => changeOrderStatus(id, status), onSuccess: refreshOrders })
  const deleteOrderMutation = useMutation({
    mutationFn: deleteOrder,
    onSuccess: async (_, orderId) => {
      if (selectedOrder?.orderId === orderId) setSelectedOrder(null)
      const refreshedPage = await getOrders(search, statusFilter, page)
      if (refreshedPage.items.length === 0 && page > 1) setPage(page - 1)
      await queryClient.invalidateQueries({ queryKey: ['orders'] })
    },
  })
  const paymentMutation = useMutation({
    mutationFn: () => recordOrderPayment(selectedOrder!.orderId, {
      amount: Number(paymentAmount), method: paymentMethod, reference: paymentReference.trim() || null, note: paymentNote.trim() || null,
    }),
    onSuccess: async () => { await refreshOrders(); setPaymentAmount(''); setPaymentReference(''); setPaymentNote('') },
  })
  const linkMutation = useMutation({
    mutationFn: () => createSubmissionLink(selectedOrder!.orderId),
    onSuccess: (link) => setSubmissionLink({ linkId: link.linkId, url: new URL(link.submissionUrl, window.location.origin).toString(), expiresAt: link.expiresAt }),
  })
  const revokeLinkMutation = useMutation({
    mutationFn: () => revokeSubmissionLink(selectedOrder!.orderId, submissionLink?.linkId ?? currentLinkQuery.data!.linkId),
    onSuccess: async () => { setSubmissionLink(null); await queryClient.invalidateQueries({ queryKey: ['current-submission-link', selectedOrder?.orderId] }) },
  })
  const reviewMutation = useMutation({
    mutationFn: () => markSubmissionReviewed(selectedOrder!.orderId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['customer-submission', selectedOrder?.orderId] }),
  })
  const deleteMediaMutation = useMutation({
    mutationFn: (mediaId: number) => deleteSubmissionMedia(selectedOrder!.orderId, mediaId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['customer-submission', selectedOrder?.orderId] }),
  })

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSearch(searchInput.trim()); setPage(1)
  }

  function submitOrder(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    createMutation.mutate({ customerId: Number(customerId), selectedTemplateVersionId: Number(versionId), agreedBasePrice: Number(basePrice), addOnIds })
  }

  function submitPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); paymentMutation.mutate()
  }

  return (
    <section className="orders-page" dir="rtl">
      <div className="section-heading"><div><p className="page-card__eyebrow">WedLink / الإدارة</p><h1>الطلبات والمدفوعات</h1><p>إنشاء الطلبات ومتابعة حالتها وتسجيل الأقساط والرصيد.</p></div></div>

      <div className="orders-layout">
        <section className="customer-panel">
          <h2>إنشاء طلب</h2>
          <form className="customer-form" onSubmit={submitOrder}>
            <label htmlFor="order-customer">العميل</label>
            <input aria-label="ابحث عن العميل بالاسم أو الهاتف" onChange={(event) => setCustomerSearch(event.target.value)} placeholder="ابحث عن العميل بالاسم أو الهاتف" value={customerSearch} />
            <select id="order-customer" onChange={(event) => setCustomerId(event.target.value)} required value={customerId}>
              <option value="">اختر العميل</option>{customersQuery.data?.items.map((customer) => <option key={customer.customerId} value={customer.customerId}>{customer.name} — {customer.phone}</option>)}
            </select>
            <label htmlFor="order-version">القالب والإصدار</label>
            <select id="order-version" onChange={(event) => { const chosen = activeVersions.find((version) => String(version.templateVersionId) === event.target.value); setVersionId(event.target.value); setBasePrice(chosen ? String(chosen.templatePrice) : ''); setAddOnIds([]) }} required value={versionId}>
              <option value="">اختر إصدارًا نشطًا</option>{activeVersions.map((version) => <option key={version.templateVersionId} value={version.templateVersionId}>{version.templateName} — {version.versionNumber}</option>)}
            </select>
            <label htmlFor="order-base-price">السعر الأساسي (جنيه مصري)</label>
            <input id="order-base-price" min="0" max="99999999.99" required step="0.01" type="number" value={basePrice} onChange={(event) => setBasePrice(event.target.value)} />
            {selectedVersion?.addOns.filter((addOn) => addOn.isActive).map((addOn) => (
              <label className="checkbox-label" key={addOn.addOnId}><input type="checkbox" checked={addOnIds.includes(addOn.addOnId)} onChange={() => setAddOnIds((items) => items.includes(addOn.addOnId) ? items.filter((id) => id !== addOn.addOnId) : [...items, addOn.addOnId])} /><span>{addOn.name} — {money(addOn.currentPrice)}</span></label>
            ))}
            {createMutation.isError && <p className="form-error" role="alert">{createMutation.error.message}</p>}
            {customersQuery.isError && <p className="form-error" role="alert">{customersQuery.error.message}</p>}
            {templatesQuery.isError && <p className="form-error" role="alert">{templatesQuery.error.message}</p>}
            <button className="primary-button" disabled={createMutation.isPending || customersQuery.isPending || templatesQuery.isPending} type="submit">{createMutation.isPending ? 'جارٍ إنشاء الطلب...' : 'إنشاء الطلب'}</button>
          </form>
        </section>

        <section className="customer-panel order-list-panel">
          <div className="list-heading"><h2>قائمة الطلبات</h2><span>{ordersQuery.data?.totalItems ?? 0} طلب</span></div>
          <form className="order-filters" onSubmit={submitSearch}>
            <label className="visually-hidden" htmlFor="order-search">بحث</label>
            <input id="order-search" placeholder="بحث بالعميل أو القالب" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} />
            <label className="visually-hidden" htmlFor="order-status">الحالة</label>
            <select id="order-status" value={statusFilter} onChange={(event) => { setStatusFilter(event.target.value); setPage(1) }}><option value="">كل الحالات</option><option>New</option><option>Confirmed</option><option>InProgress</option><option>Completed</option><option>Cancelled</option></select>
            <button className="secondary-button" type="submit">بحث</button>
          </form>
          {ordersQuery.isPending && <p role="status">جارٍ تحميل الطلبات...</p>}
          {ordersQuery.isError && <p className="form-error" role="alert">{ordersQuery.error.message}</p>}
          {statusMutation.isError && <p className="form-error" role="alert">{statusMutation.error.message}</p>}
          {deleteOrderMutation.isError && <p className="form-error" role="alert">{deleteOrderMutation.error.message}</p>}
          {ordersQuery.data?.items.length === 0 && <p className="empty-state">لا توجد طلبات مطابقة.</p>}
          {ordersQuery.data && ordersQuery.data.items.length > 0 && <div className="table-scroll"><table className="customers-table"><thead><tr><th>الطلب</th><th>العميل</th><th>القالب</th><th>الحالة</th><th>الإجمالي</th><th>إجراء</th></tr></thead><tbody>
            {ordersQuery.data.items.map((order) => <tr key={order.orderId}><td>#{order.orderId}</td><td>{order.customerName}</td><td>{order.templateName} · {order.templateVersionNumber}</td><td>{order.status}</td><td>{money(order.totalAmount, order.currency)}</td><td className="table-actions"><button className="text-button" type="button" onClick={() => { setSelectedOrder(order); setSubmissionLink(null) }}>التفاصيل</button>{nextStatuses[order.status]?.length ? <select aria-label={`تغيير حالة الطلب ${order.orderId}`} disabled={statusMutation.isPending} value="" onChange={(event) => { if (event.target.value) statusMutation.mutate({ id: order.orderId, status: event.target.value }) }}><option value="">تغيير الحالة</option>{nextStatuses[order.status].map((status) => <option key={status} value={status}>{status}</option>)}</select> : null}<button className="text-button text-button--danger" type="button" disabled={deleteOrderMutation.isPending} onClick={() => { if (window.confirm(`حذف الطلب #${order.orderId} للعميل ${order.customerName}؟\n\nسيختفي الطلب ودعوته من العرض، مع الاحتفاظ بسجل المدفوعات والمراجعات.`)) deleteOrderMutation.mutate(order.orderId) }}>{deleteOrderMutation.isPending ? 'جارٍ الحذف...' : 'حذف'}</button></td></tr>)}
          </tbody></table></div>}
          {ordersQuery.data && ordersQuery.data.totalPages > 1 && <div className="pagination"><button className="secondary-button" disabled={page <= 1} onClick={() => setPage(page - 1)} type="button">السابق</button><span>صفحة {page} من {ordersQuery.data.totalPages}</span><button className="secondary-button" disabled={page >= ordersQuery.data.totalPages} onClick={() => setPage(page + 1)} type="button">التالي</button></div>}
        </section>
      </div>

      {selectedOrder && <section className="customer-panel order-detail-panel">
        <div className="list-heading"><div><h2>الطلب #{selectedOrder.orderId}</h2><span>{selectedOrder.customerName} · {selectedOrder.templateName} · {selectedOrder.status}</span></div><button className="text-button" onClick={() => setSelectedOrder(null)} type="button">إغلاق</button></div>
        {balanceQuery.isPending && <p role="status">جارٍ تحميل الرصيد...</p>}
        {balanceQuery.isError && <p className="form-error" role="alert">{balanceQuery.error.message}</p>}
        {balanceQuery.data && <>
          <dl className="balance-grid"><div><dt>السعر الأساسي</dt><dd>{money(balanceQuery.data.agreedBasePrice, balanceQuery.data.currency)}</dd></div><div><dt>الإضافات</dt><dd>{money(balanceQuery.data.addOnsTotal, balanceQuery.data.currency)}</dd></div><div><dt>الإجمالي</dt><dd>{money(balanceQuery.data.totalAmount, balanceQuery.data.currency)}</dd></div><div><dt>المدفوع</dt><dd>{money(balanceQuery.data.paidAmount, balanceQuery.data.currency)}</dd></div><div><dt>المتبقي</dt><dd>{money(balanceQuery.data.balance, balanceQuery.data.currency)}</dd></div></dl>
          <h3>سجل الدفعات</h3>
          {balanceQuery.data.payments.length === 0 ? <p className="empty-state">لم تُسجل دفعات بعد.</p> : <div className="table-scroll"><table className="customers-table"><thead><tr><th>التاريخ</th><th>المبلغ</th><th>الطريقة</th><th>المرجع</th><th>ملاحظة</th></tr></thead><tbody>{balanceQuery.data.payments.map((payment) => <tr key={payment.paymentId}><td>{new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(payment.recordedAt))}</td><td>{money(payment.amount, payment.currency)}</td><td>{payment.method}</td><td>{payment.reference || '—'}</td><td>{payment.note || '—'}</td></tr>)}</tbody></table></div>}
          <form className="customer-form payment-form" onSubmit={submitPayment}><h3>تسجيل دفعة</h3><label htmlFor="payment-amount">المبلغ (جنيه مصري)</label><input id="payment-amount" min="0.01" max="99999999.99" required step="0.01" type="number" value={paymentAmount} onChange={(event) => setPaymentAmount(event.target.value)} /><label htmlFor="payment-method">الطريقة</label><input id="payment-method" maxLength={40} required value={paymentMethod} onChange={(event) => setPaymentMethod(event.target.value)} /><label htmlFor="payment-reference">مرجع (اختياري)</label><input id="payment-reference" maxLength={200} value={paymentReference} onChange={(event) => setPaymentReference(event.target.value)} /><label htmlFor="payment-note">ملاحظة (اختياري)</label><textarea id="payment-note" maxLength={1000} rows={2} value={paymentNote} onChange={(event) => setPaymentNote(event.target.value)} />{paymentMutation.isError && <p className="form-error" role="alert">{paymentMutation.error.message}</p>}<button className="primary-button" disabled={paymentMutation.isPending} type="submit">{paymentMutation.isPending ? 'جارٍ تسجيل الدفعة...' : 'تسجيل الدفعة'}</button></form>
        </>}
        <section className="submission-review">
          <div className="list-heading"><h3>بيانات الزفاف وملفات العميل</h3><button className="secondary-button" disabled={linkMutation.isPending} onClick={() => linkMutation.mutate()} type="button">{linkMutation.isPending ? 'جارٍ إنشاء الرابط...' : 'إنشاء رابط تقديم (14 يومًا)'}</button></div>
          {linkMutation.isError && <p className="form-error" role="alert">{linkMutation.error.message}</p>}
          {submissionLink && <div className="submission-link-box"><p>الرابط صالح حتى {new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(submissionLink.expiresAt))}. إنشاء رابط آخر يلغي هذا الرابط.</p><input aria-label="رابط تقديم العميل" readOnly value={submissionLink.url} /><div className="form-actions"><button className="secondary-button" onClick={() => navigator.clipboard.writeText(submissionLink.url)} type="button">نسخ الرابط</button><button className="text-button text-button--danger" onClick={() => revokeLinkMutation.mutate()} type="button">إلغاء الرابط</button></div></div>}
          {!submissionLink && currentLinkQuery.data && <div className="submission-link-box"><p>يوجد رابط صالح حتى {new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(currentLinkQuery.data.expiresAt))}.</p><button className="text-button text-button--danger" disabled={revokeLinkMutation.isPending} onClick={() => revokeLinkMutation.mutate()} type="button">إلغاء الرابط</button></div>}
          {revokeLinkMutation.isError && <p className="form-error" role="alert">{revokeLinkMutation.error.message}</p>}
          {submissionQuery.isPending && <p role="status">جارٍ تحميل بيانات العميل...</p>}
          {submissionQuery.isError && submissionQuery.error.message && !submissionQuery.error.message.includes('غير موجود') && <p className="form-error" role="alert">{submissionQuery.error.message}</p>}
          {submissionQuery.data && <>
            <div className="submission-summary"><p><strong>العميل:</strong> {submissionQuery.data.customerName}</p><p><strong>العروسان:</strong> {submissionQuery.data.brideName || '—'} و{submissionQuery.data.groomName || '—'}</p><p><strong>موعد الزفاف:</strong> {submissionQuery.data.weddingDateTimeLocal ? new Intl.DateTimeFormat('ar-EG', { dateStyle: 'full', timeStyle: 'short' }).format(new Date(submissionQuery.data.weddingDateTimeLocal)) : 'لم يُحدد بعد'}</p><p><strong>المكان:</strong> {submissionQuery.data.venueName || '—'} · {submissionQuery.data.venueAddress || ''}</p>{submissionQuery.data.locationUrl && <p><a href={submissionQuery.data.locationUrl} rel="noreferrer" target="_blank">فتح رابط الموقع</a></p>}</div>
            {submissionQuery.data.media.length > 0 && <div className="table-scroll"><table className="customers-table"><thead><tr><th>الملف</th><th>النوع</th><th>الحجم</th><th>إجراء</th></tr></thead><tbody>{submissionQuery.data.media.map((media) => <tr key={media.mediaId}><td>{media.originalFileName}</td><td>{media.mediaKind}</td><td>{(media.sizeBytes / 1024 / 1024).toFixed(1)} MB</td><td className="table-actions"><a href={`/api/v1/admin/orders/${selectedOrder.orderId}/customer-submission/media/${media.mediaId}/content`} target="_blank" rel="noreferrer">عرض</a><button className="text-button text-button--danger" disabled={deleteMediaMutation.isPending} onClick={() => { if (window.confirm(`حذف الملف «${media.originalFileName}»؟`)) deleteMediaMutation.mutate(media.mediaId) }} type="button">حذف</button></td></tr>)}</tbody></table></div>}
            {deleteMediaMutation.isError && <p className="form-error" role="alert">{deleteMediaMutation.error.message}</p>}
            <div className="form-actions"><span>{submissionQuery.data.adminReviewedAt ? `تمت المراجعة في ${new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(submissionQuery.data.adminReviewedAt))}` : submissionQuery.data.submittedAt ? `أُرسلت البيانات في ${new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(submissionQuery.data.submittedAt))}` : 'لم يرسل العميل البيانات بعد'}</span><button className="primary-button" disabled={reviewMutation.isPending || !submissionQuery.data.submittedAt || Boolean(submissionQuery.data.adminReviewedAt)} onClick={() => reviewMutation.mutate()} type="button">{submissionQuery.data.adminReviewedAt ? 'تمت مراجعة البيانات' : 'تأكيد مراجعة البيانات'}</button></div>
            {reviewMutation.isError && <p className="form-error" role="alert">{reviewMutation.error.message}</p>}
          </>}
        </section>
      </section>}
      {selectedOrder && <InvitationEditor key={selectedOrder.orderId} orderId={selectedOrder.orderId} />}
    </section>
  )
}
