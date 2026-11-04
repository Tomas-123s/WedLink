import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createInvitationPreviewLink, downloadFinalInvitationCard, formatInvitationLocalDate, getInvitation, invitationAction, saveFinalInvitationCard, saveInvitation } from './invitationsApi'
import { getMusicTracks, selectInvitationMusic } from './invitationsApi'
import { GuestAdminPanel } from './GuestAdminPanel'

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
    case 'Paused': return 'موقوفة مؤقتًا'
    case 'Expired': return 'منتهية'
    default: return 'مسودة'
  }
}

export function InvitationEditor({ orderId }: { orderId: number }) {
  const queryClient = useQueryClient()
  const invitationQuery = useQuery({ queryKey: ['invitation', orderId], queryFn: () => getInvitation(orderId) })
  const musicTracksQuery = useQuery({ queryKey: ['music-tracks'], queryFn: getMusicTracks,
    enabled: Boolean(invitationQuery.data?.features.some((feature) => feature.key === 'music' && feature.isEnabled)) })
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
  const lifecycleMutation = useMutation({
    mutationFn: (action: 'publish' | 'pause' | 'reactivate' | 'extend') => invitationAction(invitationQuery.data!.invitationId!, action),
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['invitation', orderId] }) },
  })
  const cardMutation = useMutation({
    mutationFn: async () => {
      const item = invitationQuery.data!
      const blob = await createInvitationCard({
        brideName: item.brideName, groomName: item.groomName, weddingDateTimeLocal: item.weddingDateTimeLocal ?? '',
        venueName: item.venueName ?? '', venueAddress: item.venueAddress ?? '', templateName: item.templateName,
        customizations: Object.fromEntries(Object.entries(item.customizations ?? {}).map(([key, value]) => [key, String(value)])),
      })
      await saveFinalInvitationCard(item.invitationId!, blob)
    },
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['invitation', orderId] }) },
  })
  const musicMutation = useMutation({
    mutationFn: (trackId: number | null) => selectInvitationMusic(invitationQuery.data!.invitationId!, trackId),
    onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['invitation', orderId] }) },
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
      {invitationQuery.data.features.some((feature) => feature.key === 'music' && feature.isEnabled) && <><label htmlFor="invitation-music-track">اختيار مسار من مكتبة الموسيقى</label><select id="invitation-music-track" disabled={musicMutation.isPending || ['Published', 'Paused', 'Expired'].includes(invitationQuery.data.status)} value={invitationQuery.data.musicTrackId ?? ''} onChange={(event) => musicMutation.mutate(event.target.value ? Number(event.target.value) : null)}><option value="">بدون موسيقى مكتبة</option>{musicTracksQuery.data?.filter((track) => track.isActive).map((track) => <option key={track.musicTrackId} value={track.musicTrackId}>{track.title}{track.artist ? ` · ${track.artist}` : ''}</option>)}</select>{musicMutation.isError && <p className="form-error">{musicMutation.error.message}</p>}</>}

      {invitationQuery.data.availableMedia.length > 0 && <><h3>الملفات التي ستظهر في المعاينة</h3>{invitationQuery.data.availableMedia.map((media) => <label className="checkbox-label" key={media.mediaId}><input checked={mediaIds.includes(media.mediaId)} type="checkbox" onChange={() => setMediaIds((items) => items.includes(media.mediaId) ? items.filter((id) => id !== media.mediaId) : [...items, media.mediaId])} /><span>{media.originalFileName} · {media.mediaType}</span></label>)}</>}

      {saveMutation.isError && <p className="form-error" role="alert">{saveMutation.error.message}</p>}
      {saveMutation.isSuccess && <p className="success-message" role="status">تم حفظ مسودة الدعوة.</p>}
      <div className="form-actions"><button className="primary-button" disabled={saveMutation.isPending} type="submit">{saveMutation.isPending ? 'جارٍ الحفظ...' : 'حفظ التعديلات'}</button><button className="secondary-button" disabled={saveMutation.isPending || previewMutation.isPending || !invitationQuery.data.invitationId} onClick={() => previewMutation.mutate()} type="button">{previewMutation.isPending ? 'جارٍ إنشاء الرابط...' : 'إنشاء رابط معاينة'}</button></div>
      {previewMutation.isError && <p className="form-error" role="alert">{previewMutation.error.message}</p>}
      {previewLink && <div className="submission-link-box"><p>الرابط خاص وصالح حتى {new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(previewLink.expiresAt))}. إنشاء رابط جديد يلغي السابق.</p><input aria-label="رابط معاينة الدعوة" readOnly value={previewLink.url} /><button className="secondary-button" onClick={() => navigator.clipboard.writeText(previewLink.url)} type="button">نسخ الرابط</button></div>}
      {invitationQuery.data.invitationId && <div className="invitation-publish-tools"><h3>النشر والبطاقة النهائية</h3>
        {invitationQuery.data.status === 'Approved' && <button className="primary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('publish')} type="button">نشر الدعوة</button>}
        {invitationQuery.data.status === 'Published' && <><button className="secondary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('pause')} type="button">إيقاف مؤقت</button><button className="secondary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('extend')} type="button">تمديد شهر</button></>}
        {invitationQuery.data.status === 'Paused' && <><button className="primary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('reactivate')} type="button">إعادة التفعيل</button><button className="secondary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('extend')} type="button">تمديد شهر</button></>}
        {lifecycleMutation.isError && <p className="form-error" role="alert">{lifecycleMutation.error.message}</p>}
        {invitationQuery.data.slug && <div className="submission-link-box"><p>الرابط العام{invitationQuery.data.expiresAt ? ` · ينتهي ${new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(invitationQuery.data.expiresAt))}` : ''}</p><input aria-label="رابط الدعوة العامة" readOnly value={`${window.location.origin}/invite/${invitationQuery.data.slug}`} /><button className="secondary-button" onClick={() => navigator.clipboard.writeText(`${window.location.origin}/invite/${invitationQuery.data.slug}`)} type="button">نسخ الرابط</button></div>}
        {invitationQuery.data.hasFinalPng ? <a className="secondary-button invitation-card-download" href={downloadFinalInvitationCard(invitationQuery.data.invitationId)} download>تنزيل بطاقة PNG</a> : <button className="secondary-button" disabled={cardMutation.isPending || !['Approved', 'Published'].includes(invitationQuery.data.status)} onClick={() => cardMutation.mutate()} type="button">{cardMutation.isPending ? 'جارٍ إنشاء البطاقة...' : 'إنشاء وحفظ بطاقة PNG'}</button>}
        {cardMutation.isError && <p className="form-error" role="alert">{cardMutation.error.message}</p>}
      </div>}
    </form>}
    {invitationQuery.data?.reviewEvents.length ? <div className="invitation-review-history"><h3>سجل مراجعات العميل</h3>{invitationQuery.data.reviewEvents.map((event) => <article key={event.eventId}><strong>{reviewLabel(event.action)}</strong><time>{new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(event.createdAt))} · {event.source === 'Customer' ? 'العميل' : 'الإدارة'}</time>{event.comment && <p>{event.comment}</p>}</article>)}</div> : null}
    {invitationQuery.data?.invitationId && <GuestAdminPanel invitationId={invitationQuery.data.invitationId} />}
  </section>
}

async function createInvitationCard(data: { brideName: string; groomName: string; weddingDateTimeLocal: string; venueName: string; venueAddress: string; templateName: string; customizations: Record<string, string> }): Promise<Blob> {
  const canvas = document.createElement('canvas')
  canvas.width = 1200
  canvas.height = 1600
  const context = canvas.getContext('2d')
  if (!context) throw new Error('تعذر تجهيز بطاقة الدعوة.')
  context.fillStyle = '#fbf7f3'; context.fillRect(0, 0, 1200, 1600)
  context.strokeStyle = '#b98c79'; context.lineWidth = 4; context.strokeRect(52, 52, 1096, 1496)
  context.strokeStyle = '#e4cfc2'; context.lineWidth = 2; context.strokeRect(72, 72, 1056, 1456)
  context.textAlign = 'center'; context.direction = 'rtl'
  context.fillStyle = '#9a705d'; context.font = '700 32px Tahoma, Arial'; context.fillText('دعوة زفاف', 600, 260)
  context.font = '40px Tahoma, Arial'; context.fillText('✦', 600, 350)
  context.fillStyle = '#806052'; context.font = '32px Tahoma, Arial'; context.fillText('بكل الحب والفرح ندعوكم لمشاركتنا', 600, 470)
  context.fillStyle = '#71495b'; context.font = '700 76px Tahoma, Arial'; context.fillText(data.brideName, 600, 650)
  context.fillStyle = '#b98c79'; context.font = '44px Georgia, serif'; context.fillText('&', 600, 735)
  context.fillStyle = '#71495b'; context.font = '700 76px Tahoma, Arial'; context.fillText(data.groomName, 600, 830)
  context.fillStyle = '#806052'; context.font = '30px Tahoma, Arial'
  if (data.weddingDateTimeLocal) context.fillText(formatInvitationLocalDate(data.weddingDateTimeLocal, 'Africa/Cairo', { dateStyle: 'full', timeStyle: 'short' }), 600, 1010)
  if (data.venueName) context.fillText(data.venueName, 600, 1110)
  context.font = '26px Tahoma, Arial'
  if (data.venueAddress) context.fillText(data.venueAddress.slice(0, 75), 600, 1170)
  const details = Object.values(data.customizations).map((value) => value.trim()).filter(Boolean).slice(0, 2)
  context.font = '24px Tahoma, Arial'; details.forEach((value, index) => context.fillText(value.slice(0, 70), 600, 1250 + index * 42))
  context.fillStyle = '#9a705d'; context.font = '26px Tahoma, Arial'; context.fillText(data.templateName, 600, 1400)
  context.fillText('ننتظر حضوركم ومشاركتكم فرحتنا', 600, 1470)
  return await new Promise<Blob>((resolve, reject) => canvas.toBlob((blob) => blob ? resolve(blob) : reject(new Error('تعذر إنشاء صورة البطاقة.')), 'image/png'))
}
