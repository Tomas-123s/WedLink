import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toBlob as captureNodeToBlob } from 'html-to-image'
import { flushSync } from 'react-dom'
import { createRoot } from 'react-dom/client'
import { createInvitationPreviewLink, downloadFinalInvitationCard, getInvitation, getPreviewInvitation, getPreviewMedia, getPublicInvitation, invitationAction, saveFinalInvitationCard, saveInvitation, uploadInvitationMedia, type InvitationDraft, type PublicInvitation } from './invitationsApi'
import { getMusicTracks, selectInvitationMusic } from './invitationsApi'
import { GuestAdminPanel } from './GuestAdminPanel'
import { DefaultInvitationTemplate, EditorialLuxuryTemplate } from './PublicInvitationTemplates'

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
  const invitationQuery = useQuery({
    queryKey: ['invitation', orderId],
    queryFn: () => getInvitation(orderId),
    refetchInterval: (query) => ['Draft', 'ChangesRequested'].includes(query.state.data?.status ?? '') ? 5000 : false,
    refetchOnWindowFocus: 'always',
  })
  const musicTracksQuery = useQuery({ queryKey: ['music-tracks'], queryFn: getMusicTracks,
    enabled: Boolean(invitationQuery.data?.features.some((feature) => feature.key === 'music' && feature.isEnabled)) })
  const activeMusicTracks = musicTracksQuery.data?.filter((track) => track.isActive) ?? []
  const [brideName, setBrideName] = useState('')
  const [groomName, setGroomName] = useState('')
  const [weddingDateTimeLocal, setWeddingDateTimeLocal] = useState('')
  const [venueName, setVenueName] = useState('')
  const [venueAddress, setVenueAddress] = useState('')
  const [locationUrl, setLocationUrl] = useState('')
  const [customizations, setCustomizations] = useState<Record<string, string>>({})
  const [mediaIds, setMediaIds] = useState<number[]>([])
  const [mediaFile, setMediaFile] = useState<File | null>(null)
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
  const uploadMediaMutation = useMutation({
    mutationFn: () => uploadInvitationMedia(orderId, mediaFile!),
    onSuccess: (uploaded) => {
      queryClient.setQueryData<InvitationDraft>(['invitation', orderId], (current) => current ? {
        ...current,
        availableMedia: [...current.availableMedia, uploaded],
        selectedMediaIds: [...new Set([...current.selectedMediaIds, uploaded.mediaId])],
      } : current)
      setMediaFile(null)
      const input = document.getElementById('invitation-media-upload') as HTMLInputElement | null
      if (input) input.value = ''
    },
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
      let templateItem: PublicInvitation
      const temporaryImageUrls: string[] = []
      if (item.status === 'Published' && item.slug) {
        templateItem = await getPublicInvitation(item.slug)
      } else {
        const previewLink = await createInvitationPreviewLink(orderId)
        const previewUrl = new URL(previewLink.previewUrl, window.location.origin)
        const token = new URLSearchParams(previewUrl.hash.slice(1)).get('token')
        if (!token) throw new Error('تعذر تجهيز رابط معاينة لإنشاء البطاقة.')
        const invitation = await getPreviewInvitation(token)
        const media = await Promise.all(invitation.media.map(async (entry) => {
          if (entry.mediaType !== 'Image') return { ...entry, url: '' }
          const url = await getPreviewMedia(token, entry.mediaId)
          temporaryImageUrls.push(url)
          return { ...entry, url }
        }))
        templateItem = { ...invitation, eventEnded: false, hasFinalPng: item.hasFinalPng, media }
      }
      try {
        const blob = await createInvitationCard(templateItem)
        await saveFinalInvitationCard(item.invitationId!, blob)
      } finally {
        temporaryImageUrls.forEach((url) => URL.revokeObjectURL(url))
      }
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

  function moveSelectedImage(mediaId: number, direction: -1 | 1) {
    const availableMedia = invitationQuery.data?.availableMedia ?? []
    const imageIds = new Set(availableMedia.filter((media) => media.mediaType === 'Image').map((media) => media.mediaId))
    setMediaIds((current) => {
      const imagePositions = current.flatMap((id, index) => imageIds.has(id) ? [index] : [])
      const imageOrder = imagePositions.map((index) => current[index])
      const currentPosition = imageOrder.indexOf(mediaId)
      const nextPosition = currentPosition + direction
      if (currentPosition < 0 || nextPosition < 0 || nextPosition >= imageOrder.length) return current
      ;[imageOrder[currentPosition], imageOrder[nextPosition]] = [imageOrder[nextPosition], imageOrder[currentPosition]]
      const reordered = [...current]
      imagePositions.forEach((index, position) => { reordered[index] = imageOrder[position] })
      return reordered
    })
  }

  return <section className="customer-panel invitation-editor" dir="rtl">
    <div className="list-heading"><div><h2>إعداد الدعوة ومعاينتها</h2><span>{invitationQuery.data?.templateName} · إصدار {invitationQuery.data?.templateVersionNumber}</span></div><div className="form-actions"><span>الحالة: {statusLabel(invitationQuery.data?.status)}</span><button className="text-button" disabled={invitationQuery.isFetching} onClick={() => void invitationQuery.refetch()} type="button">{invitationQuery.isFetching ? 'جارٍ تحديث الحالة...' : 'تحديث الحالة'}</button></div></div>
    {invitationQuery.isPending && <p role="status">جارٍ تحميل إعدادات الدعوة...</p>}
    {invitationQuery.isError && <p className="form-error" role="alert">{invitationQuery.error.message}</p>}
    {invitationQuery.data && <form className="customer-form invitation-form" onSubmit={submit}>
      <label htmlFor="invitation-bride">اسم العروس</label><input id="invitation-bride" maxLength={150} required value={brideName} onChange={(event) => setBrideName(event.target.value)} />
      <label htmlFor="invitation-groom">اسم العريس</label><input id="invitation-groom" maxLength={150} required value={groomName} onChange={(event) => setGroomName(event.target.value)} />
      <label htmlFor="invitation-date">موعد الزفاف (توقيت القاهرة)</label><input id="invitation-date" required type="datetime-local" value={weddingDateTimeLocal} onChange={(event) => setWeddingDateTimeLocal(event.target.value)} />
      <label htmlFor="invitation-venue">اسم المكان</label><input id="invitation-venue" maxLength={200} value={venueName} onChange={(event) => setVenueName(event.target.value)} />
      <label htmlFor="invitation-address">عنوان المكان</label><textarea id="invitation-address" maxLength={500} rows={2} value={venueAddress} onChange={(event) => setVenueAddress(event.target.value)} />
      <label htmlFor="invitation-location">رابط الموقع</label><input id="invitation-location" maxLength={1000} type="url" value={locationUrl} onChange={(event) => setLocationUrl(event.target.value)} />

      {invitationQuery.data.features.some((feature) => feature.key === 'countdown' && feature.isEnabled) && <>
        <h3>رسالة ما بعد موعد الزفاف</h3>
        <label htmlFor="invitation-feature-countdown">رسالة تظهر بدل العد التنازلي بعد موعد الزفاف (اختياري)</label>
        <input id="invitation-feature-countdown" placeholder="مثل: شكرًا لمشاركتنا فرحتنا" value={customizations.countdown ?? ''} onChange={(event) => setCustomizations((items) => ({ ...items, countdown: event.target.value }))} />
      </>}
      {invitationQuery.data.features.some((feature) => feature.key === 'music' && feature.isEnabled) && <>
        <label htmlFor="invitation-music-track">اختيار مسار من مكتبة الموسيقى</label>
        <select id="invitation-music-track" disabled={musicMutation.isPending || musicTracksQuery.isPending || activeMusicTracks.length === 0 || !invitationQuery.data.invitationId || ['Published', 'Expired'].includes(invitationQuery.data.status)} value={invitationQuery.data.musicTrackId ?? ''} onChange={(event) => musicMutation.mutate(event.target.value ? Number(event.target.value) : null)}>
          <option value="">{musicTracksQuery.isPending ? 'جارٍ تحميل المسارات...' : activeMusicTracks.length ? 'بدون موسيقى مكتبة' : 'لا توجد مسارات نشطة'}</option>
          {activeMusicTracks.map((track) => <option key={track.musicTrackId} value={track.musicTrackId}>{track.title}{track.artist ? ` · ${track.artist}` : ''}</option>)}
        </select>
        {musicTracksQuery.isError && <p className="form-error" role="alert">تعذر تحميل مكتبة الموسيقى: {musicTracksQuery.error.message}</p>}
        {!musicTracksQuery.isPending && !musicTracksQuery.isError && activeMusicTracks.length === 0 && <p className="form-hint">أضف مسارًا مرخصًا وفعّله أولًا من <a href="/admin/music">مكتبة الموسيقى</a>.</p>}
        {invitationQuery.data.status === 'Published' && <p className="form-hint">أوقف الرابط مؤقتًا أولًا لتغيير الموسيقى، ثم أعد تفعيله بعد الحفظ.</p>}
        {invitationQuery.data.status === 'Paused' && <p className="form-hint">اختيار الموسيقى متاح والرابط موقوف. سيظل الرابط موقوفًا حتى تضغط «إعادة التفعيل».</p>}
        {invitationQuery.data.status === 'Expired' && <p className="form-hint">انتهت صلاحية الرابط؛ لا يمكن تغيير الموسيقى.</p>}
        {musicMutation.isError && <p className="form-error" role="alert">{musicMutation.error.message}</p>}
      </>}

      {(invitationQuery.data.availableMedia.length > 0 || invitationQuery.data.features.some((feature) => ['gallery', 'video'].includes(feature.key) && feature.isEnabled)) && <>
        <h3>الصور والوسائط التي ستظهر في الدعوة</h3>
        <p className="invitation-media-order-hint">حدد الملفات، ثم استخدم السهمين لترتيب الصور في المعرض.</p>
        {invitationQuery.data.features.some((feature) => ['gallery', 'video'].includes(feature.key) && feature.isEnabled) && <div className="invitation-media-upload">
          <label htmlFor="invitation-media-upload">إضافة صورة أو فيديو من جهازك</label>
          <div><input accept=".jpg,.jpeg,.png,.webp,.mp4" disabled={uploadMediaMutation.isPending || invitationQuery.data.status === 'Expired'} id="invitation-media-upload" onChange={(event) => setMediaFile(event.target.files?.[0] ?? null)} type="file" />
            <button className="secondary-button" disabled={!mediaFile || uploadMediaMutation.isPending || invitationQuery.data.status === 'Expired'} onClick={() => uploadMediaMutation.mutate()} type="button">{uploadMediaMutation.isPending ? 'جارٍ رفع الملف...' : 'رفع وإضافة'}</button></div>
          <small>الصور حتى 10MB والفيديو MP4 حتى 100MB. يمكنك رفع ملف بديل ثم إلغاء اختيار الملف القديم؛ تُطبّق حدود عدد الملفات عند حفظ الدعوة.</small>
          {uploadMediaMutation.isError && <p className="form-error" role="alert">{uploadMediaMutation.error.message}</p>}
          {uploadMediaMutation.isSuccess && <p className="success-message" role="status">تم رفع الملف وإضافته للاختيارات. اضغط «حفظ التعديلات» لنشره على الدعوة.</p>}
          {invitationQuery.data.status === 'Expired' && <p className="form-hint">انتهت صلاحية الرابط؛ لا يمكن إضافة وسائط إلى هذه الدعوة.</p>}
        </div>}
        <div className="invitation-media-order-list">{invitationQuery.data.availableMedia.map((media) => {
          const isSelected = mediaIds.includes(media.mediaId)
          const imagePosition = media.mediaType === 'Image' ? mediaIds.filter((id) => invitationQuery.data!.availableMedia.find((item) => item.mediaId === id)?.mediaType === 'Image').indexOf(media.mediaId) : -1
          return <div className="invitation-media-order-row" key={media.mediaId}>
            <label className="checkbox-label" htmlFor={`invitation-media-${media.mediaId}`}>
              <input checked={isSelected} id={`invitation-media-${media.mediaId}`} type="checkbox" onChange={() => setMediaIds((items) => items.includes(media.mediaId) ? items.filter((id) => id !== media.mediaId) : [...items, media.mediaId])} />
              <span>{media.originalFileName} · {media.mediaType}{imagePosition >= 0 && <small>ترتيب الصورة {imagePosition + 1}</small>}</span>
            </label>
            {imagePosition >= 0 && <div className="invitation-media-order-actions">
              <button aria-label={`تقديم الصورة ${media.originalFileName}`} disabled={imagePosition === 0} onClick={() => moveSelectedImage(media.mediaId, -1)} title="تقديم الصورة" type="button">↑</button>
              <button aria-label={`تأخير الصورة ${media.originalFileName}`} disabled={imagePosition === mediaIds.filter((id) => invitationQuery.data!.availableMedia.find((item) => item.mediaId === id)?.mediaType === 'Image').length - 1} onClick={() => moveSelectedImage(media.mediaId, 1)} title="تأخير الصورة" type="button">↓</button>
            </div>}
          </div>
        })}</div>
      </>}

      {mediaIds.some((id) => invitationQuery.data!.availableMedia.find((media) => media.mediaId === id)?.mediaType === 'Image') && <>
        <h3>ضبط صورة الهيرو</h3>
        <p className="invitation-media-order-hint">الصورة الأولى في ترتيب الصور هي صورة الهيرو. غيّر التكبير ومكان التركيز، ثم احفظ لمعاينة النتيجة على الدعوة.</p>
        <label htmlFor="invitation-hero-zoom">حجم الصورة: {customizations.heroZoom ?? '103'}%</label>
        <input id="invitation-hero-zoom" max="200" min="100" onChange={(event) => setCustomizations((items) => ({ ...items, heroZoom: event.target.value }))} step="1" type="range" value={customizations.heroZoom ?? '103'} />
        <label htmlFor="invitation-hero-position-x">الجزء الظاهر أفقيًا: {customizations.heroPositionX ?? '50'}%</label>
        <input id="invitation-hero-position-x" max="100" min="0" onChange={(event) => setCustomizations((items) => ({ ...items, heroPositionX: event.target.value }))} step="1" type="range" value={customizations.heroPositionX ?? '50'} />
        <label htmlFor="invitation-hero-position-y">الجزء الظاهر رأسيًا: {customizations.heroPositionY ?? '42'}%</label>
        <input id="invitation-hero-position-y" max="100" min="0" onChange={(event) => setCustomizations((items) => ({ ...items, heroPositionY: event.target.value }))} step="1" type="range" value={customizations.heroPositionY ?? '42'} />
      </>}

      {saveMutation.isError && <p className="form-error" role="alert">{saveMutation.error.message}</p>}
      {saveMutation.isSuccess && <p className="success-message" role="status">{saveMutation.data?.status === 'Published' ? 'تم حفظ التعديلات وتحديث الدعوة على الرابط المنشور نفسه.' : saveMutation.data?.status === 'Paused' ? 'تم حفظ التعديلات. الرابط ما زال موقوفًا؛ أعد تفعيله عند الانتهاء.' : saveMutation.data?.status === 'Draft' ? 'تم حفظ الدعوة كمسودة. لأن بياناتها تغيرت، أنشئ رابط معاينة جديدًا ليوافق العميل عليها قبل إنشاء البطاقة النهائية.' : 'تم حفظ الدعوة مع الاحتفاظ بموافقة العميل.'}</p>}
      <div className="form-actions"><button className="primary-button" disabled={saveMutation.isPending || uploadMediaMutation.isPending || invitationQuery.data.status === 'Expired'} type="submit">{saveMutation.isPending ? 'جارٍ الحفظ...' : 'حفظ التعديلات'}</button><button className="secondary-button" disabled={saveMutation.isPending || previewMutation.isPending || !invitationQuery.data.invitationId || ['Published', 'Expired'].includes(invitationQuery.data.status)} onClick={() => previewMutation.mutate()} type="button">{previewMutation.isPending ? 'جارٍ إنشاء الرابط...' : 'إنشاء رابط معاينة'}</button></div>
      {['Published', 'Expired'].includes(invitationQuery.data.status) && <p className="form-hint">{invitationQuery.data.status === 'Published' ? 'الدعوة منشورة بالفعل؛ احفظ التعديلات لتظهر على الرابط العام، ولا تحتاج إلى رابط معاينة جديد.' : 'انتهت صلاحية الدعوة ولا يمكن إنشاء معاينة أو حفظ تعديلات.'}</p>}
      {previewMutation.isError && <p className="form-error" role="alert">{previewMutation.error.message}</p>}
      {previewLink && <div className="submission-link-box"><p>الرابط خاص وصالح حتى {new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(previewLink.expiresAt))}. إنشاء رابط جديد يلغي السابق.</p><input aria-label="رابط معاينة الدعوة" readOnly value={previewLink.url} /><button className="secondary-button" onClick={() => navigator.clipboard.writeText(previewLink.url)} type="button">نسخ الرابط</button></div>}
      {invitationQuery.data.invitationId && <div className="invitation-publish-tools"><h3>النشر والبطاقة النهائية</h3>
        {invitationQuery.data.status === 'Approved' && <button className="primary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('publish')} type="button">نشر الدعوة</button>}
        {invitationQuery.data.status === 'Published' && <><button className="secondary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('pause')} type="button">إيقاف مؤقت</button><button className="secondary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('extend')} type="button">تمديد شهر</button></>}
        {invitationQuery.data.status === 'Paused' && <><button className="primary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('reactivate')} type="button">إعادة التفعيل</button><button className="secondary-button" disabled={lifecycleMutation.isPending} onClick={() => lifecycleMutation.mutate('extend')} type="button">تمديد شهر</button></>}
        {lifecycleMutation.isError && <p className="form-error" role="alert">{lifecycleMutation.error.message}</p>}
        {invitationQuery.data.slug && <div className="submission-link-box"><p>الرابط العام{invitationQuery.data.expiresAt ? ` · ينتهي ${new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium' }).format(new Date(invitationQuery.data.expiresAt))}` : ''}</p><input aria-label="رابط الدعوة العامة" readOnly value={`${window.location.origin}/invite/${invitationQuery.data.slug}`} /><button className="secondary-button" onClick={() => navigator.clipboard.writeText(`${window.location.origin}/invite/${invitationQuery.data.slug}`)} type="button">نسخ الرابط</button></div>}
        {invitationQuery.data.hasFinalPng && <a className="secondary-button invitation-card-download" href={downloadFinalInvitationCard(invitationQuery.data.invitationId)} download>تنزيل بطاقة PNG</a>}
        <button className="secondary-button" disabled={cardMutation.isPending || !['Approved', 'Paused', 'Published'].includes(invitationQuery.data.status)} onClick={() => cardMutation.mutate()} type="button">{cardMutation.isPending ? 'جارٍ تجهيز تصميم القالب...' : invitationQuery.data.hasFinalPng ? 'تحديث البطاقة حسب القالب' : 'إنشاء وحفظ بطاقة PNG'}</button>
        {!['Approved', 'Paused', 'Published'].includes(invitationQuery.data.status) && <p className="form-hint">يتاح إنشاء البطاقة بعد موافقة العميل على المعاينة.</p>}
        {cardMutation.isError && <p className="form-error" role="alert">{cardMutation.error.message}</p>}
      </div>}
    </form>}
    {invitationQuery.data?.reviewEvents.length ? <div className="invitation-review-history"><h3>سجل مراجعات العميل</h3>{invitationQuery.data.reviewEvents.map((event) => <article key={event.eventId}><strong>{reviewLabel(event.action)}</strong><time>{new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(event.createdAt))} · {event.source === 'Customer' ? 'العميل' : 'الإدارة'}</time>{event.comment && <p>{event.comment}</p>}</article>)}</div> : null}
    {invitationQuery.data?.invitationId && <GuestAdminPanel invitationId={invitationQuery.data.invitationId} />}
  </section>
}

async function createInvitationCard(item: PublicInvitation): Promise<Blob> {
  const mount = document.createElement('div')
  mount.className = 'preview-template-shell'
  Object.assign(mount.style, {
    position: 'fixed',
    top: '0',
    left: '-10000px',
    width: `${Math.max(900, Math.min(window.innerWidth, 1440))}px`,
    zIndex: '-1',
    pointerEvents: 'none',
  })
  document.body.appendChild(mount)
  const root = createRoot(mount)
  try {
    flushSync(() => root.render(item.rendererKey === 'editorial-luxury'
      ? <EditorialLuxuryTemplate item={item} slug="" guests={undefined} previewMode />
      : <DefaultInvitationTemplate item={item} slug="" guests={undefined} previewMode />))

    const hero = await waitForCardElement(mount, '.editorial-luxury-hero, .public-invitation-hero')
    await document.fonts.ready
    const envelopeButton = mount.querySelector<HTMLButtonElement>('.editorial-luxury-open')
    if (envelopeButton) {
      envelopeButton.click()
      await new Promise((resolve) => window.setTimeout(resolve, 4000))
    }
    await Promise.all(Array.from(hero.querySelectorAll('img')).map(waitForCardImage))
    const blob = await captureNodeToBlob(hero, {
      backgroundColor: item.rendererKey === 'editorial-luxury' ? '#151515' : '#fbf7f3',
      pixelRatio: 2,
      filter: (node) => !(node instanceof HTMLElement
        && (node.classList.contains('editorial-luxury-hero__actions') || node.classList.contains('editorial-luxury-scroll-cue'))),
    })
    if (!blob) throw new Error('???? ????? ???? ??????? ?? ??????.')
    return blob
  } finally {
    root.unmount()
    mount.remove()
  }
}

function waitForCardElement(container: HTMLElement, selector: string): Promise<HTMLElement> {
  return new Promise((resolve, reject) => {
    const startedAt = Date.now()
    const check = () => {
      const element = container.querySelector<HTMLElement>(selector)
      if (element) { resolve(element); return }
      if (Date.now() - startedAt > 15000) { reject(new Error('???? ????? ????? ?????? ?????? ???????.')); return }
      window.setTimeout(check, 50)
    }
    check()
  })
}

function waitForCardImage(image: HTMLImageElement): Promise<void> {
  if (image.complete) {
    return image.naturalWidth > 0 ? Promise.resolve() : Promise.reject(new Error('???? ????? ???? ?????? ?????? ???????.'))
  }
  return new Promise((resolve, reject) => {
    image.addEventListener('load', () => resolve(), { once: true })
    image.addEventListener('error', () => reject(new Error('???? ????? ???? ?????? ?????? ???????.')), { once: true })
    window.setTimeout(() => reject(new Error('?????? ????? ???? ?????? ????? ??????.')), 15000)
  })
}
