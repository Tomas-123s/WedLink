import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { deleteSubmissionMedia, getCurrentSubmission, saveCurrentSubmission, uploadSubmissionMedia } from './submissionApi'

function takeTokenFromHash(): string {
  const params = new URLSearchParams(window.location.hash.slice(1))
  const token = params.get('token') ?? ''
  if (token) window.history.replaceState(null, '', `${window.location.pathname}${window.location.search}`)
  return token
}

const initialSubmissionToken = window.location.pathname === '/submit' ? takeTokenFromHash() : ''

function localDateValue(value: string | null): string {
  if (!value) return ''
  return value.slice(0, 16)
}

export function CustomerSubmissionPage() {
  const [token] = useState(initialSubmissionToken)
  const [brideName, setBrideName] = useState('')
  const [groomName, setGroomName] = useState('')
  const [weddingDateTimeLocal, setWeddingDateTimeLocal] = useState('')
  const [venueName, setVenueName] = useState('')
  const [venueAddress, setVenueAddress] = useState('')
  const [locationUrl, setLocationUrl] = useState('')
  const [customizations, setCustomizations] = useState<Record<string, string>>({})
  const [file, setFile] = useState<File | null>(null)
  const submissionQuery = useQuery({ queryKey: ['current-customer-submission', token], queryFn: () => getCurrentSubmission(token), enabled: Boolean(token), retry: false })

  useEffect(() => {
    const data = submissionQuery.data
    if (!data) return
    setBrideName(data.brideName)
    setGroomName(data.groomName)
    setWeddingDateTimeLocal(localDateValue(data.weddingDateTimeLocal))
    setVenueName(data.venueName ?? '')
    setVenueAddress(data.venueAddress ?? '')
    setLocationUrl(data.locationUrl ?? '')
    setCustomizations(Object.fromEntries(Object.entries(data.customizations ?? {}).map(([key, value]) => [key, String(value)])))
  }, [submissionQuery.data])

  const saveMutation = useMutation({
    mutationFn: () => saveCurrentSubmission(token, {
      brideName, groomName, weddingDateTimeLocal, timeZoneId: 'Africa/Cairo',
      venueName: venueName.trim() || null, venueAddress: venueAddress.trim() || null,
      locationUrl: locationUrl.trim() || null, customizations, submit: true,
    }),
    onSuccess: (data) => submissionQuery.refetch().then(() => data),
  })
  const uploadMutation = useMutation({ mutationFn: () => uploadSubmissionMedia(token, file!), onSuccess: async () => { setFile(null); await submissionQuery.refetch() } })
  const deleteMutation = useMutation({ mutationFn: (mediaId: number) => deleteSubmissionMedia(token, mediaId), onSuccess: () => submissionQuery.refetch() })

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    saveMutation.mutate()
  }

  if (!token) return <section className="submission-page" dir="rtl"><p className="page-card__eyebrow">WedLink</p><h1>رابط التقديم غير مكتمل</h1><p>افتح الرابط الخاص الذي أرسله لك فريق WedLink.</p></section>
  if (submissionQuery.isPending) return <p className="auth-status" role="status">جارٍ تحميل نموذج بيانات الزفاف...</p>
  if (submissionQuery.isError) return <section className="submission-page" dir="rtl"><p className="page-card__eyebrow">WedLink</p><h1>تعذر فتح نموذج التقديم</h1><p className="form-error" role="alert">{submissionQuery.error.message}</p><p>قد يكون الرابط منتهيًا أو تم استبداله برابط جديد. تواصل مع فريق WedLink لطلب رابط صالح.</p></section>

  return <section className="submission-page" dir="rtl">
    <div className="section-heading"><div><p className="page-card__eyebrow">WedLink / بيانات الزفاف</p><h1>أهلًا {submissionQuery.data.customerName}</h1><p>أرسل بيانات الموعد والمكان والتفضيلات، ثم أرفق الصور والملفات المطلوبة.</p></div></div>
    <section className="customer-panel"><h2>تفاصيل الزفاف</h2><form className="customer-form" onSubmit={submit}>
      <label htmlFor="submission-bride">اسم العروس</label><input id="submission-bride" maxLength={150} required value={brideName} onChange={(event) => setBrideName(event.target.value)} />
      <label htmlFor="submission-groom">اسم العريس</label><input id="submission-groom" maxLength={150} required value={groomName} onChange={(event) => setGroomName(event.target.value)} />
      <label htmlFor="submission-date">موعد الزفاف (توقيت القاهرة)</label><input id="submission-date" required type="datetime-local" value={weddingDateTimeLocal} onChange={(event) => setWeddingDateTimeLocal(event.target.value)} />
      <label htmlFor="submission-venue">اسم المكان (اختياري)</label><input id="submission-venue" maxLength={200} value={venueName} onChange={(event) => setVenueName(event.target.value)} />
      <label htmlFor="submission-address">عنوان المكان (اختياري)</label><textarea id="submission-address" maxLength={500} rows={2} value={venueAddress} onChange={(event) => setVenueAddress(event.target.value)} />
      <label htmlFor="submission-location">رابط الموقع (اختياري)</label><input id="submission-location" maxLength={1000} type="url" value={locationUrl} onChange={(event) => setLocationUrl(event.target.value)} />
      {saveMutation.isError && <p className="form-error" role="alert">{saveMutation.error.message}</p>}
      {saveMutation.isSuccess && <p className="success-message" role="status">تم حفظ بيانات الزفاف.</p>}
      <button className="primary-button" disabled={saveMutation.isPending} type="submit">{saveMutation.isPending ? 'جارٍ الحفظ...' : 'حفظ بيانات الزفاف'}</button>
    </form></section>

    <section className="customer-panel"><h2>الصور والملفات</h2><p>صور JPEG/PNG/WebP بحد 10MB، فيديو MP4 بحد 100MB، صوت MP3/M4A بحد 20MB. الحد الإجمالي للطلب 500MB.</p>
      <form className="customer-form" onSubmit={(event) => { event.preventDefault(); uploadMutation.mutate() }}><label htmlFor="submission-file">اختر ملفًا</label><input id="submission-file" accept=".jpg,.jpeg,.png,.webp,.mp4,.mp3,.m4a" required type="file" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /><button className="primary-button" disabled={!file || uploadMutation.isPending} type="submit">{uploadMutation.isPending ? 'جارٍ رفع الملف...' : 'رفع الملف'}</button></form>
      {uploadMutation.isError && <p className="form-error" role="alert">{uploadMutation.error.message}</p>}
      {deleteMutation.isError && <p className="form-error" role="alert">{deleteMutation.error.message}</p>}
      {submissionQuery.data.media.length === 0 ? <p className="empty-state">لم ترفع ملفات بعد.</p> : <div className="submission-file-list">{submissionQuery.data.media.map((media) => <div className="addon-row" key={media.mediaId}><div><strong>{media.originalFileName}</strong><p>{media.mediaKind} · {(media.sizeBytes / 1024 / 1024).toFixed(1)} MB</p></div>{!submissionQuery.data.adminReviewedAt && <button className="text-button text-button--danger" disabled={deleteMutation.isPending} onClick={() => { if (window.confirm(`حذف الملف «${media.originalFileName}»؟`)) deleteMutation.mutate(media.mediaId) }} type="button">حذف</button>}</div>)}</div>}
    </section>
  </section>
}
