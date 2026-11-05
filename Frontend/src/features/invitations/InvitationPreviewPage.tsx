import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { getPreviewInvitation, getPreviewMedia, sendPreviewReview, type PublicInvitation } from './invitationsApi'
import { DefaultInvitationTemplate, EditorialLuxuryTemplate } from './PublicInvitationTemplates'

function takePreviewToken(): string {
  const params = new URLSearchParams(window.location.hash.slice(1))
  const token = params.get('token') ?? ''
  if (token) window.history.replaceState(null, '', `${window.location.pathname}${window.location.search}`)
  return token
}

const previewToken = window.location.pathname === '/preview' ? takePreviewToken() : ''

export function InvitationPreviewPage() {
  const [token] = useState(previewToken)
  const [comment, setComment] = useState('')
  const [mediaUrls, setMediaUrls] = useState<Record<number, string>>({})
  const [mediaErrors, setMediaErrors] = useState<Record<number, string>>({})
  const previewQuery = useQuery({ queryKey: ['private-invitation-preview', token], queryFn: () => getPreviewInvitation(token), enabled: Boolean(token), retry: false })
  const reviewMutation = useMutation({
    mutationFn: (action: 'Approved' | 'ChangesRequested') => sendPreviewReview(token, action, comment),
    onSuccess: () => setComment(''),
  })

  useEffect(() => {
    let active = true
    const objectUrls: string[] = []
    setMediaUrls({})
    setMediaErrors({})
    for (const media of previewQuery.data?.media ?? []) {
      void getPreviewMedia(token, media.mediaId).then((url) => {
        if (!active) {
          URL.revokeObjectURL(url)
          return
        }
        objectUrls.push(url)
        setMediaUrls((current) => ({ ...current, [media.mediaId]: url }))
      }).catch((reason: unknown) => {
        if (active) setMediaErrors((current) => ({
          ...current,
          [media.mediaId]: reason instanceof Error ? reason.message : 'تعذر تحميل الملف.',
        }))
      })
    }
    return () => {
      active = false
      objectUrls.forEach((url) => URL.revokeObjectURL(url))
    }
  }, [previewQuery.data, token])

  function requestChanges(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    reviewMutation.mutate('ChangesRequested')
  }

  if (!token) return <section className="preview-page" dir="rtl"><h1>رابط المعاينة غير مكتمل</h1><p>افتح رابط المعاينة الخاص الذي أرسله لك فريق WedLink.</p></section>
  if (previewQuery.isPending) return <p className="auth-status" role="status">جارٍ تجهيز معاينة الدعوة...</p>
  if (previewQuery.isError) return <section className="preview-page" dir="rtl"><h1>تعذر فتح المعاينة</h1><p className="form-error" role="alert">{previewQuery.error.message}</p><p>قد يكون الرابط منتهيًا أو تم استبداله برابط جديد. تواصل مع فريق WedLink للحصول على رابط آخر.</p></section>

  const invitation = previewQuery.data
  const media = invitation.media.flatMap((item) => mediaUrls[item.mediaId]
    ? [{ ...item, url: mediaUrls[item.mediaId] }]
    : [])
  const previewItem: PublicInvitation = {
    eventEnded: false,
    hasFinalPng: false,
    rendererKey: invitation.rendererKey,
    templateName: invitation.templateName,
    brideName: invitation.brideName,
    groomName: invitation.groomName,
    weddingDateTimeLocal: invitation.weddingDateTimeLocal,
    timeZoneId: invitation.timeZoneId,
    venueName: invitation.venueName,
    venueAddress: invitation.venueAddress,
    locationUrl: invitation.locationUrl,
    customizations: invitation.customizations,
    features: invitation.features,
    media,
  }
  const templateProps = { item: previewItem, slug: '', guests: undefined, previewMode: true }
  const pendingMediaCount = invitation.media.length - media.length

  return <main className="preview-page preview-page--template" dir="rtl">
    <header className="preview-template-banner"><p className="page-card__eyebrow">معاينة خاصة · {invitation.templateName} · إصدار {invitation.templateVersionNumber}</p><p>هذا هو شكل القالب قبل النشر. الدعوة ما زالت خاصة.</p></header>
    {pendingMediaCount > 0 && <p className="preview-template-media-state" role="status">جارٍ تحميل {pendingMediaCount} من ملفات الدعوة...</p>}
    {Object.entries(mediaErrors).map(([mediaId, message]) => <p className="form-error" role="alert" key={mediaId}>تعذر تحميل أحد ملفات المعاينة: {message}</p>)}
    <div className="preview-template-shell">
      {invitation.rendererKey === 'editorial-luxury'
        ? <EditorialLuxuryTemplate {...templateProps} />
        : <DefaultInvitationTemplate {...templateProps} />}
    </div>
    <section className="preview-content preview-feedback"><h2>رأيك في الدعوة</h2><p>يمكنك اعتماد المعاينة أو إرسال ملاحظاتك لفريق WedLink.</p>
      <form className="customer-form" onSubmit={requestChanges}><label htmlFor="preview-comment">التعديلات المطلوبة</label><textarea id="preview-comment" maxLength={2000} rows={4} required value={comment} onChange={(event) => setComment(event.target.value)} />
        {reviewMutation.isError && <p className="form-error" role="alert">{reviewMutation.error.message}</p>}
        <div className="form-actions"><button className="secondary-button" disabled={reviewMutation.isPending} type="submit">إرسال طلب تعديل</button><button className="primary-button" disabled={reviewMutation.isPending} onClick={() => reviewMutation.mutate('Approved')} type="button">اعتماد الدعوة</button></div>
        {reviewMutation.isSuccess && <p className="success-message" role="status">تم تسجيل مراجعتك وإبلاغ فريق WedLink.</p>}
      </form>
    </section>
  </main>
}
