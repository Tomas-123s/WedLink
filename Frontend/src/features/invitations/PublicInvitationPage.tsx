import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { formatInvitationLocalDate, getGuestInteractions, getPublicInvitation, invitationLocalDateToEpoch, submitGuestMessage, submitRsvp, type GuestInteractions } from './invitationsApi'

const countdownNumber = new Intl.NumberFormat('ar-EG', { minimumIntegerDigits: 2, useGrouping: false })

function weddingDate(value: string | null, zone: string) {
  if (!value) return null
  return formatInvitationLocalDate(value, zone, { dateStyle: 'full', timeStyle: 'short' })
}

export function PublicInvitationPage() {
  const { slug = '' } = useParams()
  const invitation = useQuery({ queryKey: ['public-invitation', slug], queryFn: () => getPublicInvitation(slug), retry: false })
  const guests = useQuery({ queryKey: ['guest-interactions', slug], queryFn: () => getGuestInteractions(slug), retry: false })
  if (invitation.isPending) return <p className="public-invitation-state" role="status">جارٍ فتح الدعوة...</p>
  if (invitation.isError) return <section className="public-invitation-state" dir="rtl"><h1>الدعوة غير متاحة</h1><p>قد يكون الرابط غير صحيح أو أن الدعوة متوقفة.</p></section>
  const item = invitation.data
  if (item.eventEnded) return <section className="public-invitation-state" dir="rtl"><p className="public-invitation-kicker">WedLink</p><h1>انتهى موعد هذه الدعوة</h1><p>نتمنى للعروسين حياة مليئة بالفرح والسعادة.</p>{item.hasFinalPng && <a className="public-invitation-card-download" download href={`/api/v1/public/invitations/${encodeURIComponent(slug)}/final-card`}>تنزيل بطاقة الدعوة</a>}</section>
  const date = weddingDate(item.weddingDateTimeLocal, item.timeZoneId)
  const customizations = item.customizations ?? {}
  const enabledKeys = new Set(item.features.filter((feature) => feature.isEnabled).map((feature) => feature.key))
  const countdownMessage = typeof customizations.countdown === 'string' ? customizations.countdown.trim() : ''
  const publicCustomizations = Object.entries(customizations).filter(([key, value]) => key !== 'countdown' && enabledKeys.has(key) && String(value).trim())
  const share = async () => {
    const url = new URL(`/api/v1/public/invitations/${encodeURIComponent(slug)}/share`, window.location.origin).toString()
    if (navigator.share) await navigator.share({ title: `دعوة زفاف ${item.brideName} و${item.groomName}`, text: 'شاركوا فرحتنا وافتحوا تفاصيل الدعوة', url })
    else await navigator.clipboard.writeText(url)
  }

  return <article className="public-invitation" dir="rtl">
    <section className="public-invitation-hero">
      <p className="public-invitation-kicker">دعوة زفاف</p>
      <div className="public-invitation-ornament" aria-hidden="true">✦</div>
      <p className="public-invitation-intro">بكل الحب والفرح، ندعوكم لمشاركتنا أجمل أيامنا</p>
      <h1>{item.brideName}<span>&amp;</span>{item.groomName}</h1>
      <p className="public-invitation-template">{item.templateName}</p>
      {date && <p className="public-invitation-date">{date}</p>}
      {item.venueName && <p className="public-invitation-venue">{item.venueName}</p>}
      {item.venueAddress && <p className="public-invitation-address">{item.venueAddress}</p>}
      {item.locationUrl && <a className="public-invitation-map" href={item.locationUrl} rel="noreferrer" target="_blank">افتح الموقع</a>}
      <button className="primary-button" onClick={() => void share()} type="button">مشاركة الدعوة</button>
      {item.hasFinalPng && <a className="public-invitation-card-download" download href={`/api/v1/public/invitations/${encodeURIComponent(slug)}/final-card`}>تنزيل بطاقة الدعوة</a>}
    </section>
    {enabledKeys.has('countdown') && item.weddingDateTimeLocal && <InvitationCountdown dateTime={item.weddingDateTimeLocal} timeZone={item.timeZoneId} postEventMessage={countdownMessage} />}
    {publicCustomizations.length > 0 && <section className="public-invitation-section"><h2>تفاصيل إضافية</h2><dl className="public-invitation-details">{publicCustomizations.map(([key, value]) => <div key={key}><dt>{item.features.find((feature) => feature.key === key)?.name ?? key}</dt><dd>{String(value)}</dd></div>)}</dl></section>}
    {item.media.some((media) => media.mediaType === 'Image') && <section className="public-invitation-section"><h2>لحظاتنا</h2><div className="public-invitation-gallery">{item.media.filter((media) => media.mediaType === 'Image' && enabledKeys.has('gallery')).map((media) => <img key={media.mediaId} alt="صورة من الدعوة" loading="lazy" src={media.url} />)}</div></section>}
    {item.media.filter((media) => media.mediaType === 'Video' && enabledKeys.has('video')).map((media) => <section className="public-invitation-section" key={media.mediaId}><video controls playsInline preload="metadata" src={media.url} /></section>)}
    {item.media.filter((media) => media.mediaType === 'Audio' && enabledKeys.has('music')).map((media) => <section className="public-invitation-section" key={media.mediaId}><audio controls preload="none" src={media.url} /></section>)}
    {guests.data && <GuestInteractionsPanel slug={slug} data={guests.data} />}
    <footer className="public-invitation-footer">بكل الحب، ننتظر حضوركم ومشاركتكم فرحتنا</footer>
  </article>
}

function InvitationCountdown({ dateTime, timeZone, postEventMessage }: { dateTime: string; timeZone: string; postEventMessage: string }) {
  const eventTime = invitationLocalDateToEpoch(dateTime, timeZone)
  const [now, setNow] = useState(() => Date.now())
  const hasEnded = eventTime !== null && now >= eventTime

  useEffect(() => {
    if (eventTime === null || hasEnded) return
    const interval = window.setInterval(() => {
      const currentTime = Date.now()
      setNow(currentTime)
      if (currentTime >= eventTime) window.clearInterval(interval)
    }, 1000)
    return () => window.clearInterval(interval)
  }, [eventTime, hasEnded])

  if (eventTime === null) return null
  if (hasEnded) {
    return <section className="public-invitation-section invitation-countdown invitation-countdown--ended"><h2>موعدنا السعيد</h2><p>{postEventMessage || 'تمّ الزفاف، ونشكركم على مشاركتكم فرحتنا.'}</p></section>
  }

  const remainingSeconds = Math.max(0, Math.ceil((eventTime - now) / 1000))
  const parts = [
    { value: Math.floor(remainingSeconds / 86400), label: 'أيام' },
    { value: Math.floor((remainingSeconds % 86400) / 3600), label: 'ساعات' },
    { value: Math.floor((remainingSeconds % 3600) / 60), label: 'دقائق' },
    { value: remainingSeconds % 60, label: 'ثوانٍ' },
  ]
  return <section className="public-invitation-section invitation-countdown" aria-label="الوقت المتبقي حتى موعد الزفاف" role="timer" aria-live="off">
    <h2>نعدّ الأيام للقائكم</h2><div className="invitation-countdown__parts">{parts.map((part) => <div key={part.label}><strong>{countdownNumber.format(part.value)}</strong><span>{part.label}</span></div>)}</div>
  </section>
}

function GuestInteractionsPanel({ slug, data }: { slug: string; data: GuestInteractions }) {
  const queryClient = useQueryClient()
  const tokenKey = `wedlink:rsvp:${slug}`
  const [response, setResponse] = useState<'Yes' | 'No'>('Yes')
  const [guestName, setGuestName] = useState('')
  const [companions, setCompanions] = useState(0)
  const [messageName, setMessageName] = useState('')
  const [message, setMessage] = useState('')
  const rsvpMutation = useMutation({
    mutationFn: () => submitRsvp(slug, { response, guestName, accompanyingPeople: response === 'Yes' ? companions : 0, editToken: localStorage.getItem(tokenKey) }),
    onSuccess: async (result) => { localStorage.setItem(tokenKey, result.editToken); await queryClient.invalidateQueries({ queryKey: ['guest-interactions', slug] }) },
  })
  const messageMutation = useMutation({
    mutationFn: () => submitGuestMessage(slug, { guestName: messageName, message }),
    onSuccess: async () => { setMessage(''); await queryClient.invalidateQueries({ queryKey: ['guest-interactions', slug] }) },
  })
  function sendRsvp(event: FormEvent) { event.preventDefault(); rsvpMutation.mutate() }
  function sendMessage(event: FormEvent) { event.preventDefault(); messageMutation.mutate() }
  return <section className="public-invitation-section guest-interactions">
    {data.guestCountEnabled && <p className="guest-attendance-count">عدد الحضور المؤكد: <strong>{data.expectedAttendance}</strong></p>}
    {data.rsvpEnabled && <div><h2>تأكيد الحضور</h2><form className="customer-form" onSubmit={sendRsvp}>
      <label htmlFor="guest-rsvp-name">الاسم (اختياري)</label><input id="guest-rsvp-name" maxLength={150} value={guestName} onChange={(event) => setGuestName(event.target.value)} />
      <label htmlFor="guest-rsvp-response">هل ستحضرون؟</label><select id="guest-rsvp-response" value={response} onChange={(event) => setResponse(event.target.value as 'Yes' | 'No')}><option value="Yes">نعم، سأحضر</option><option value="No">أعتذر عن الحضور</option></select>
      {response === 'Yes' && <><label htmlFor="guest-rsvp-companions">عدد المرافقين الإضافيين</label><input id="guest-rsvp-companions" type="number" min="0" max={data.maximumAccompanyingPeople ?? 2147483646} value={companions} onChange={(event) => setCompanions(Number(event.target.value))} /></>}
      {rsvpMutation.isError && <p className="form-error" role="alert">{rsvpMutation.error.message}</p>}
      {rsvpMutation.isSuccess && <p className="success-message" role="status">تم حفظ تأكيد الحضور. يمكنك تحديث ردك من هذا المتصفح.</p>}
      <button className="primary-button" disabled={rsvpMutation.isPending} type="submit">{rsvpMutation.isPending ? 'جارٍ الإرسال...' : 'حفظ الرد'}</button>
    </form></div>}
    {data.musicEnabled && data.musicTrack && data.musicUrl && <div className="guest-music"><h2>موسيقى الدعوة</h2><p>{data.musicTrack.title}{data.musicTrack.artist ? ` · ${data.musicTrack.artist}` : ''}</p><audio controls preload="none" src={data.musicUrl} /></div>}
    {data.messagesEnabled && <div className="guest-messages"><h2>رسائل التهنئة</h2><form className="customer-form" onSubmit={sendMessage}>
      <label htmlFor="guest-message-name">اسمك (اختياري)</label><input id="guest-message-name" maxLength={150} value={messageName} onChange={(event) => setMessageName(event.target.value)} />
      <label htmlFor="guest-message-text">رسالتك</label><textarea id="guest-message-text" required maxLength={2000} rows={3} value={message} onChange={(event) => setMessage(event.target.value)} />
      {messageMutation.isError && <p className="form-error" role="alert">{messageMutation.error.message}</p>}
      {messageMutation.isSuccess && <p className="success-message" role="status">وصلت رسالتك، شكرًا لك.</p>}
      <button className="primary-button" disabled={messageMutation.isPending} type="submit">إرسال التهنئة</button>
    </form><div className="public-guest-messages">{data.messages.map((item) => <article key={item.guestMessageId}><strong>{item.guestName || 'ضيف'}</strong><p>{item.message}</p></article>)}</div></div>}
  </section>
}
