import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { getPreviewInvitation, getPreviewMedia, sendPreviewReview } from './invitationsApi'

function takePreviewToken(): string {
  const params = new URLSearchParams(window.location.hash.slice(1))
  const token = params.get('token') ?? ''
  if (token) window.history.replaceState(null, '', `${window.location.pathname}${window.location.search}`)
  return token
}

const previewToken = window.location.pathname === '/preview' ? takePreviewToken() : ''

function PrivateMedia({ token, mediaId, mediaType, alt }: { token: string; mediaId: number; mediaType: string; alt: string }) {
  const [url, setUrl] = useState('')
  const [error, setError] = useState('')
  useEffect(() => {
    let active = true
    let objectUrl = ''
    getPreviewMedia(token, mediaId).then((value) => {
      objectUrl = value
      if (active) setUrl(value)
      else URL.revokeObjectURL(value)
    }).catch((reason: unknown) => {
      if (active) setError(reason instanceof Error ? reason.message : 'تعذر تحميل الملف.')
    })
    return () => { active = false; if (objectUrl) URL.revokeObjectURL(objectUrl) }
  }, [token, mediaId])

  if (error) return <p className="form-error" role="alert">{alt}: {error}</p>
  if (!url) return <p role="status">جارٍ تحميل {alt}...</p>
  if (mediaType === 'Image') return <img alt={alt} className="preview-media__image" src={url} />
  if (mediaType === 'Video') return <video className="preview-media__player" controls src={url} />
  return <audio className="preview-media__player" controls src={url} />
}

export function InvitationPreviewPage() {
  const [token] = useState(previewToken)
  const [comment, setComment] = useState('')
  const previewQuery = useQuery({ queryKey: ['private-invitation-preview', token], queryFn: () => getPreviewInvitation(token), enabled: Boolean(token), retry: false })
  const reviewMutation = useMutation({
    mutationFn: (action: 'Approved' | 'ChangesRequested') => sendPreviewReview(token, action, comment),
    onSuccess: () => setComment(''),
  })

  function requestChanges(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    reviewMutation.mutate('ChangesRequested')
  }

  if (!token) return <section className="preview-page" dir="rtl"><h1>رابط المعاينة غير مكتمل</h1><p>افتح رابط المعاينة الخاص الذي أرسله لك فريق WedLink.</p></section>
  if (previewQuery.isPending) return <p className="auth-status" role="status">جارٍ تجهيز معاينة الدعوة...</p>
  if (previewQuery.isError) return <section className="preview-page" dir="rtl"><h1>تعذر فتح المعاينة</h1><p className="form-error" role="alert">{previewQuery.error.message}</p><p>قد يكون الرابط منتهيًا أو تم استبداله برابط جديد. تواصل مع فريق WedLink للحصول على رابط آخر.</p></section>

  const invitation = previewQuery.data
  const customizationEntries = Object.entries(invitation.customizations ?? {}).filter(([, value]) => value !== null && value !== '')
  const featureNames = new Map(invitation.features.map((feature) => [feature.key, feature.name]))
  return <section className="preview-page" dir="rtl">
    <header className="preview-hero"><p className="page-card__eyebrow">معاينة خاصة · {invitation.templateName} · إصدار {invitation.templateVersionNumber}</p><h1>{invitation.brideName} & {invitation.groomName}</h1>
      {invitation.weddingDateTimeLocal && <p>{new Intl.DateTimeFormat('ar-EG', { dateStyle: 'full', timeStyle: 'short' }).format(new Date(invitation.weddingDateTimeLocal))}</p>}
      {invitation.venueName && <h2>{invitation.venueName}</h2>}{invitation.venueAddress && <p>{invitation.venueAddress}</p>}
      {invitation.locationUrl && <a href={invitation.locationUrl} rel="noreferrer" target="_blank">عرض الموقع</a>}
    </header>
    {customizationEntries.length > 0 && <section className="preview-content"><h2>تفاصيل الدعوة</h2><dl className="preview-customizations">{customizationEntries.map(([key, value]) => <div key={key}><dt>{featureNames.get(key) ?? key}</dt><dd>{typeof value === 'string' ? value : JSON.stringify(value)}</dd></div>)}</dl></section>}
    {invitation.media.length > 0 && <section className="preview-content"><h2>الصور والوسائط</h2><div className="preview-gallery">{invitation.media.map((media) => <figure className="preview-media" key={media.mediaId}><PrivateMedia token={token} mediaId={media.mediaId} mediaType={media.mediaType} alt={media.originalFileName} /><figcaption>{media.originalFileName}</figcaption></figure>)}</div></section>}
    <section className="preview-content preview-feedback"><h2>رأيك في الدعوة</h2><p>يمكنك اعتماد المعاينة أو إرسال ملاحظاتك لفريق WedLink.</p>
      <form className="customer-form" onSubmit={requestChanges}><label htmlFor="preview-comment">التعديلات المطلوبة</label><textarea id="preview-comment" maxLength={2000} rows={4} required value={comment} onChange={(event) => setComment(event.target.value)} />
        {reviewMutation.isError && <p className="form-error" role="alert">{reviewMutation.error.message}</p>}
        <div className="form-actions"><button className="secondary-button" disabled={reviewMutation.isPending} type="submit">إرسال طلب تعديل</button><button className="primary-button" disabled={reviewMutation.isPending} onClick={() => reviewMutation.mutate('Approved')} type="button">اعتماد الدعوة</button></div>
        {reviewMutation.isSuccess && <p className="success-message" role="status">تم تسجيل مراجعتك وإبلاغ فريق WedLink.</p>}
      </form>
    </section>
  </section>
}
