import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createInvitationPreviewLink, getInvitation, saveInvitation } from './invitationsApi'

function localDateValue(value: string | null): string {
  return value ? value.slice(0, 16) : ''
}

function reviewLabel(action: string): string {
  return action === 'Approved' ? 'موافقة العميل' : action === 'ChangesRequested' ? 'طلب تعديلات' : action
}

function statusLabel(status: string | undefined): string {
  switch (status) {
    case 'Approved': return 'موافق عليها من العميل'
    case 'ChangesRequested': return 'مطلوب تعديل'
    case 'Published': return 'منشورة'
    case 'Expired': return 'منتهية'
    default: return 'مسودة'
  }
}

export function InvitationEditor({ orderId }: { orderId: number }) {
  const queryClient = useQueryClient()
  const invitationQuery = useQuery({ queryKey: ['invitation', orderId], queryFn: () => getInvitation(orderId) })
  const [brideName, setBrideName] = useState('')
  const [groomName, setGroomName] = useState('')
  const [weddingDateTimeLocal, setWeddingDateTimeLocal] = useState('')
  const [venueName, setVenueName] = useState('')
  const [venueAddress, setVenueAddress] = useState('')
  const [locationUrl, setLocationUrl] = useState('')
  const [customizations, setCustomizations] = useState<Record<string, string>>({})
  const [mediaIds, setMediaIds] = useState<number[]>([])
  const [previewLink, setPreviewLink] = useState<{ url: string; expiresAt: string } | null>(null)

  useEffect(() => {
    const item = invitationQuery.data
    if (!item) return
    setBrideName(item.brideName)
    setGroomName(item.groomName)
    setWeddingDateTimeLocal(localDateValue(item.weddingDateTimeLocal))
    setVenueName(item.venueName ?? '')
    setVenueAddress(item.venueAddress ?? '')
    setLocationUrl(item.locationUrl ?? '')
    setCustomizations(Object.fromEntries(Object.entries(item.customizations ?? {}).map(([key, value]) => [key, String(value)])))
    setMediaIds(item.selectedMediaIds)
  }, [invitationQuery.data])

  const saveMutation = useMutation({
    mutationFn: () => saveInvitation(orderId, {
      brideName, groomName, weddingDateTimeLocal, timeZoneId: 'Africa/Cairo',
      venueName: venueName.trim() || null, venueAddress: venueAddress.trim() || null,
      locationUrl: locationUrl.trim() || null, customizations, mediaIds,
    }),
    onSuccess: async (data) => { queryClient.setQueryData(['invitation', orderId], data); setPreviewLink(null) },
  })
  const previewMutation = useMutation({
    mutationFn: () => createInvitationPreviewLink(orderId),
    onSuccess: (link) => setPreviewLink({ url: new URL(link.previewUrl, window.location.origin).toString(), expiresAt: link.expiresAt }),
  })

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    saveMutation.mutate()
  }

  return <section className="customer-panel invitation-editor" dir="rtl">
    <div className="list-heading"><div><h2>إعداد الدعوة ومعاينتها</h2><span>{invitationQuery.data?.templateName} · إصدار {invitationQuery.data?.templateVersionNumber}</span></div><span>الحالة: {statusLabel(invitationQuery.data?.status)}</span></div>
    {invitationQuery.isPending && <p role="status">جارٍ تحميل إعدادات الدعوة...</p>}
    {invitationQuery.isError && <p className="form-error" role="alert">{invitationQuery.error.message}</p>}
    {invitationQuery.data && <form className="customer-form invitation-form" onSubmit={submit}>
      <label htmlFor="invitation-bride">اسم العروس</label><input id="invitation-bride" maxLength={150} required value={brideName} onChange={(event) => setBrideName(event.target.value)} />
      <label htmlFor="invitation-groom">اسم العريس</label><input id="invitation-groom" maxLength={150} required value={groomName} onChange={(event) => setGroomName(event.target.value)} />
      <label htmlFor="invitation-date">موعد الزفاف (توقيت القاهرة)</label><input id="invitation-date" required type="datetime-local" value={weddingDateTimeLocal} onChange={(event) => setWeddingDateTimeLocal(event.target.value)} />
      <label htmlFor="invitation-venue">اسم المكان</label><input id="invitation-venue" maxLength={200} value={venueName} onChange={(event) => setVenueName(event.target.value)} />
      <label htmlFor="invitation-address">عنوان المكان</label><textarea id="invitation-address" maxLength={500} rows={2} value={venueAddress} onChange={(event) => setVenueAddress(event.target.value)} />
      <label htmlFor="invitation-location">رابط الموقع</label><input id="invitation-location" maxLength={1000} type="url" value={locationUrl} onChange={(event) => setLocationUrl(event.target.value)} />

      {invitationQuery.data.features.some((feature) => feature.isEnabled) && <><h3>تخصيصات القالب المدعومة</h3>{invitationQuery.data.features.filter((feature) => feature.isEnabled).map((feature) => <div key={feature.key}><label htmlFor={`invitation-feature-${feature.key}`}>{feature.name}</label><input id={`invitation-feature-${feature.key}`} value={customizations[feature.key] ?? ''} onChange={(event) => setCustomizations((items) => ({ ...items, [feature.key]: event.target.value }))} /></div>)}</>}

      {invitationQuery.data.availableMedia.length > 0 && <><h3>الملفات التي ستظهر في المعاينة</h3>{invitationQuery.data.availableMedia.map((media) => <label className="checkbox-label" key={media.mediaId}><input checked={mediaIds.includes(media.mediaId)} type="checkbox" onChange={() => setMediaIds((items) => items.includes(media.mediaId) ? items.filter((id) => id !== media.mediaId) : [...items, media.mediaId])} /><span>{media.originalFileName} · {media.mediaType}</span></label>)}</>}

      {saveMutation.isError && <p className="form-error" role="alert">{saveMutation.error.message}</p>}
      {saveMutation.isSuccess && <p className="success-message" role="status">تم حفظ مسودة الدعوة.</p>}
      <div className="form-actions"><button className="primary-button" disabled={saveMutation.isPending} type="submit">{saveMutation.isPending ? 'جارٍ الحفظ...' : 'حفظ التعديلات'}</button><button className="secondary-button" disabled={saveMutation.isPending || previewMutation.isPending || !invitationQuery.data.invitationId} onClick={() => previewMutation.mutate()} type="button">{previewMutation.isPending ? 'جارٍ إنشاء الرابط...' : 'إنشاء رابط معاينة'}</button></div>
      {previewMutation.isError && <p className="form-error" role="alert">{previewMutation.error.message}</p>}
      {previewLink && <div className="submission-link-box"><p>الرابط خاص وصالح حتى {new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(previewLink.expiresAt))}. إنشاء رابط جديد يلغي السابق.</p><input aria-label="رابط معاينة الدعوة" readOnly value={previewLink.url} /><button className="secondary-button" onClick={() => navigator.clipboard.writeText(previewLink.url)} type="button">نسخ الرابط</button></div>}
    </form>}
    {invitationQuery.data?.reviewEvents.length ? <div className="invitation-review-history"><h3>سجل مراجعات العميل</h3>{invitationQuery.data.reviewEvents.map((event) => <article key={event.eventId}><strong>{reviewLabel(event.action)}</strong><time>{new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(event.createdAt))} · {event.source === 'Customer' ? 'العميل' : 'الإدارة'}</time>{event.comment && <p>{event.comment}</p>}</article>)}</div> : null}
  </section>
}
