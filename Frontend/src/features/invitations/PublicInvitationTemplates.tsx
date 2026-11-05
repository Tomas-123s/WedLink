import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState, type FormEvent, type RefObject } from 'react'
import { formatInvitationLocalDate, invitationLocalDateToEpoch, submitGuestMessage, submitRsvp, type GuestInteractions, type PublicInvitation } from './invitationsApi'

const countdownNumber = new Intl.NumberFormat('ar-EG', { minimumIntegerDigits: 2, useGrouping: false })

export interface InvitationTemplateProps {
  item: PublicInvitation
  slug: string
  guests: GuestInteractions | undefined
  previewMode?: boolean
}

function weddingDate(value: string | null, zone: string) {
  if (!value) return null
  return formatInvitationLocalDate(value, zone, { dateStyle: 'full', timeStyle: 'short' })
}

function invitationContactUrl(brideName: string, groomName: string) {
  const phone = (import.meta.env.VITE_PUBLIC_WHATSAPP_PHONE ?? '201210143454').replace(/\D/g, '')
  if (!phone) return null
  const message = `مرحبًا، لدي استفسار عن دعوة ${brideName} و${groomName}.`
  return `https://wa.me/${phone}?text=${encodeURIComponent(message)}`
}

function shareInvitation(item: PublicInvitation, slug: string) {
  const url = new URL(`/api/v1/public/invitations/${encodeURIComponent(slug)}/share`, window.location.origin).toString()
  if (navigator.share) return navigator.share({ title: `دعوة زفاف ${item.brideName} و${item.groomName}`, text: 'شاركوا فرحتنا وافتحوا تفاصيل الدعوة', url })
  return navigator.clipboard.writeText(url)
}

export function EditorialLuxuryTemplate({ item, slug, guests, previewMode = false }: InvitationTemplateProps) {
  const pageRef = useRef<HTMLElement>(null)
  const heroRef = useRef<HTMLElement>(null)
  const musicAudioRef = useRef<HTMLAudioElement>(null)
  const [envelopeOpened, setEnvelopeOpened] = useState(false)
  const [musicMuted, setMusicMuted] = useState(false)
  const [musicPlaying, setMusicPlaying] = useState(false)
  const date = weddingDate(item.weddingDateTimeLocal, item.timeZoneId)
  const contactUrl = invitationContactUrl(item.brideName, item.groomName)
  const customizations = item.customizations ?? {}
  const enabledKeys = new Set(item.features.filter((feature) => feature.isEnabled).map((feature) => feature.key))
  const countdownMessage = typeof customizations.countdown === 'string' ? customizations.countdown.trim() : ''
  const publicCustomizations = Object.entries(customizations).filter(([key, value]) => key !== 'countdown' && enabledKeys.has(key) && String(value).trim())
  const heroPortrait = item.media.find((media) => media.mediaType === 'Image')
  const uploadedAudioUrl = enabledKeys.has('music') ? item.media.find((media) => media.mediaType === 'Audio')?.url : undefined
  const invitationMusicUrl = guests?.musicEnabled && guests.musicUrl ? guests.musicUrl : uploadedAudioUrl
  const galleryImages = enabledKeys.has('gallery') ? item.media.filter((media) => media.mediaType === 'Image' && media.mediaId !== heroPortrait?.mediaId) : []
  const videos = enabledKeys.has('video') ? item.media.filter((media) => media.mediaType === 'Video') : []
  const groomFamily = findPersonalization(item, ['groom-family', 'grooms-family'], ['عائلة العريس', 'أسرة العريس', 'أهل العريس', 'groom family'])
  const brideFamily = findPersonalization(item, ['bride-family', 'brides-family'], ['عائلة العروس', 'أسرة العروس', 'أهل العروس', 'bride family'])
  const coupleMessage = findPersonalization(item, ['couple-message', 'romantic-message', 'love-note'], ['رسالة العروسين', 'رسالتنا', 'رسالة حب', 'رسالة شخصية', 'couple message', 'love note'])

  useEffect(() => {
    const page = pageRef.current
    if (!page) return
    const targets = page.querySelectorAll<HTMLElement>('[data-reveal]')
    if (!('IntersectionObserver' in window) || window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      targets.forEach((target) => target.classList.add('is-visible'))
      return
    }
    page.classList.add('has-reveal')
    const observer = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue
        entry.target.classList.add('is-visible')
        observer.unobserve(entry.target)
      }
    }, { threshold: 0.16, rootMargin: '0px 0px -8% 0px' })
    targets.forEach((target) => observer.observe(target))
    return () => { observer.disconnect(); page.classList.remove('has-reveal') }
  }, [])

  useEffect(() => {
    if (!envelopeOpened || !invitationMusicUrl || !musicAudioRef.current) return
    const audio = musicAudioRef.current
    audio.muted = musicMuted
    void audio.play().catch(() => undefined)
  }, [envelopeOpened, invitationMusicUrl])

  useEffect(() => {
    const hero = heroRef.current
    if (!hero || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return
    let frame = 0
    const update = () => {
      cancelAnimationFrame(frame)
      frame = requestAnimationFrame(() => {
        const bounds = hero.getBoundingClientRect()
        hero.style.setProperty('--hero-parallax', `${Math.max(-28, Math.min(28, -bounds.top * 0.055))}px`)
      })
    }
    window.addEventListener('scroll', update, { passive: true })
    update()
    return () => { window.removeEventListener('scroll', update); cancelAnimationFrame(frame) }
  }, [])

  function openEnvelope() {
    const audio = musicAudioRef.current
    if (audio) {
      audio.muted = musicMuted
      void audio.play().then(() => setMusicPlaying(true)).catch(() => setMusicPlaying(false))
    }
    setEnvelopeOpened(true)
    window.setTimeout(() => heroRef.current?.querySelector<HTMLAnchorElement>('.editorial-luxury-scroll-cue')?.focus({ preventScroll: true }), 0)
  }

  function toggleInvitationMusic() {
    const audio = musicAudioRef.current
    const nextMuted = !musicMuted
    if (audio) {
      audio.muted = nextMuted
      if (!nextMuted) void audio.play().then(() => setMusicPlaying(true)).catch(() => setMusicPlaying(false))
      else setMusicPlaying(!audio.paused)
    }
    setMusicMuted(nextMuted)
  }

  return <article className={`public-invitation editorial-luxury editorial-luxury-page${envelopeOpened ? ' is-invitation-open' : ''}`} dir="rtl" ref={pageRef}>
    <div className="editorial-luxury-petals editorial-luxury-petals--page" aria-hidden="true">
      {Array.from({ length: 12 }, (_, index) => <span className="editorial-luxury-falling-petal" key={`petal-${index}`} />)}
      {Array.from({ length: 6 }, (_, index) => <span className={`editorial-luxury-floating-flower editorial-luxury-floating-flower--${index + 1}`} key={`flower-${index}`}><FloralMark /><i>✿</i></span>)}
    </div>
    <section className={`editorial-luxury-envelope${envelopeOpened ? ' is-open' : ''}`} aria-label="مقدمة الدعوة" aria-hidden={envelopeOpened}>
      <div className="editorial-luxury-envelope__content">
        <FloralMark className="editorial-luxury-envelope__flower" />
        <p className="el-label">إلى أحبّائنا</p>
        <div className="editorial-luxury-envelope__paper"><span>بكل الحب</span><strong>{item.brideName}<i>و</i>{item.groomName}</strong></div>
        <div className="editorial-luxury-envelope__shape" aria-hidden="true"><span className="editorial-luxury-envelope__seal">✿</span></div>
        <button className="editorial-luxury-open" onClick={openEnvelope} type="button" aria-label="افتح دعوة الزفاف">
          <span>{envelopeOpened ? 'أهلًا بكم' : 'افتحوا دعوتنا'}</span><span aria-hidden="true">✦</span>
        </button>
        <a className="editorial-luxury-skip" href="#invitation-introduction" onClick={openEnvelope}>تخطي المقدمة</a>
      </div>
    </section>

    <section className="editorial-luxury-hero" id="invitation-hero" ref={heroRef}>
      {heroPortrait && <img className="editorial-luxury-hero__image" alt={`صورة للعروسين ${item.brideName} و${item.groomName}`} fetchPriority="high" src={heroPortrait.url} />}
      <div className="editorial-luxury-hero__shade" />
      <FloralMark className="editorial-luxury-hero__botanical editorial-luxury-hero__botanical--right" />
      <FloralMark className="editorial-luxury-hero__botanical editorial-luxury-hero__botanical--left" />
      <div className="editorial-luxury-hero__copy">
        <p className="el-label editorial-luxury-hero__kicker">دعوة إلى الفرح</p>
        <p className="editorial-luxury-hero__intro">بكل الحب والامتنان، نتمنى أن تكونوا معنا</p>
        <h1 className="editorial-luxury-hero__names"><span>{item.brideName}</span><i aria-hidden="true">و</i><span>{item.groomName}</span></h1>
        {date && <p className="editorial-luxury-hero__date">{date}</p>}
        <a className="editorial-luxury-scroll-cue" href="#invitation-introduction" aria-label="تابع تفاصيل الدعوة"><span />تابعوا الحكاية</a>
      </div>
      {!heroPortrait && <div className="editorial-luxury-hero__no-photo" aria-hidden="true">✿</div>}
      {!previewMode && <div className="editorial-luxury-hero__actions">
        <button className="el-button el-button--secondary" onClick={() => void shareInvitation(item, slug)} type="button">مشاركة الدعوة</button>
        {item.hasFinalPng && <a className="el-button el-button--secondary" download href={`/api/v1/public/invitations/${encodeURIComponent(slug)}/final-card`}>تنزيل البطاقة</a>}
      </div>}
    </section>

    <section className="editorial-luxury-introduction editorial-luxury-reveal" data-reveal id="invitation-introduction">
      <FloralMark className="editorial-luxury-section-flower" />
      <p className="el-label">حكاية قلبين</p>
      <h2>بداية عمرٍ<br /><em>من المودة والرحمة</em></h2>
      <p>يسعدنا أن تشاركونا لحظة ننتظرها بكل الحب.</p>
      <span className="editorial-luxury-rule" aria-hidden="true" />
    </section>

    {coupleMessage && <section className="editorial-luxury-love-note editorial-luxury-reveal" data-reveal>
      <p className="el-label">من القلب</p><blockquote>{coupleMessage}</blockquote><span className="editorial-luxury-love-note__sign">{item.brideName}<i>و</i>{item.groomName}</span>
      <FloralMark className="editorial-luxury-love-note__flower" />
    </section>}

    {(brideFamily || groomFamily) && <section className="editorial-luxury-families editorial-luxury-reveal" data-reveal>
      {brideFamily && <div className="editorial-luxury-family"><FloralMark className="editorial-luxury-family__flower" /><p className="el-label">بمحبة أهل العروس</p><h2>عائلة {item.brideName}</h2><p>{brideFamily}</p></div>}
      {groomFamily && <div className="editorial-luxury-family"><FloralMark className="editorial-luxury-family__flower" /><p className="el-label">بمحبة أهل العريس</p><h2>عائلة {item.groomName}</h2><p>{groomFamily}</p></div>}
    </section>}

    {galleryImages.length > 0 && <section className="editorial-luxury-gallery-section editorial-luxury-reveal" data-reveal>
      <header className="editorial-luxury-section-heading"><p className="el-label">فصول من حكايتنا</p><h2>لحظات نحبّها</h2><FloralMark className="editorial-luxury-section-heading__flower" /></header>
      <div className="editorial-luxury-gallery">{galleryImages.map((media, index) => <figure className={`editorial-luxury-gallery__item editorial-luxury-gallery__item--${index % 4}`} data-reveal key={media.mediaId} style={{ transitionDelay: `${Math.min(index * 120, 840)}ms` }}><img alt={`صورة من لحظات العروسين ${item.brideName} و${item.groomName}`} loading="lazy" src={media.url} /></figure>)}</div>
    </section>}

    {videos.length > 0 && <section className="editorial-luxury-film-section editorial-luxury-reveal" data-reveal>
      <header className="editorial-luxury-section-heading"><p className="el-label">صورة تتحرك، وذكرى تبقى</p><h2>مشهد من حكايتنا</h2></header>
      <InvitationVideo src={videos[0].url} />
      {videos.length > 1 && <div className="editorial-luxury-film-gallery">{videos.slice(1).map((video) => <InvitationVideo key={video.mediaId} src={video.url} />)}</div>}
    </section>}

    {publicCustomizations.length > 0 && <section className="editorial-luxury-extra editorial-luxury-reveal" data-reveal>
      <header className="editorial-luxury-section-heading"><p className="el-label">تفاصيل صغيرة، بفرحة كبيرة</p><h2>أعددنا لكم</h2></header>
      <dl>{publicCustomizations.map(([key, value]) => <div key={key}><dt>{item.features.find((feature) => feature.key === key)?.name ?? key}</dt><dd>{String(value)}</dd></div>)}</dl>
    </section>}

    {enabledKeys.has('countdown') && item.weddingDateTimeLocal && <div className="editorial-luxury-reveal" data-reveal><InvitationCountdown dateTime={item.weddingDateTimeLocal} timeZone={item.timeZoneId} postEventMessage={countdownMessage} editorialLuxury /></div>}
    {(date || item.venueName || item.venueAddress || item.locationUrl) && <section className="editorial-luxury-event editorial-luxury-reveal" data-reveal>
      <FloralMark className="editorial-luxury-event__flower" />
      <header className="editorial-luxury-section-heading"><p className="el-label">ننتظركم بكل الشوق</p><h2>موعدنا ومكاننا</h2></header>
      <dl>{date && <div><dt>التاريخ والوقت</dt><dd>{date}</dd></div>}{item.venueName && <div><dt>المكان</dt><dd>{item.venueName}</dd></div>}{item.venueAddress && <div><dt>العنوان</dt><dd>{item.venueAddress}</dd></div>}</dl>
      {item.locationUrl && <a className="editorial-luxury-map" href={item.locationUrl} rel="noreferrer" target="_blank">افتحوا الموقع على الخريطة <span aria-hidden="true">↗</span></a>}
    </section>}

    {previewMode ? <div className="editorial-luxury-reveal" data-reveal><InvitationInteractionPreview enabledKeys={enabledKeys} editorialLuxury /></div> : guests && <div className="editorial-luxury-reveal" data-reveal><GuestInteractionsPanel slug={slug} data={guests} editorialLuxury musicAudioRef={musicAudioRef} musicMuted={musicMuted} musicPlaying={musicPlaying} musicUrlOverride={invitationMusicUrl} musicControlVisible={envelopeOpened} onPlaybackChange={setMusicPlaying} onToggleMusic={toggleInvitationMusic} /></div>}

    <footer className="editorial-luxury-closing editorial-luxury-reveal" data-reveal>
      <FloralMark className="editorial-luxury-closing__flower" /><p className="el-label">إلى أن نلتقي</p><p className="editorial-luxury-closing__message">وجودكم هو أجمل هدية، وفرحتنا لا تكتمل إلا بكم</p>
      <h2>{item.brideName}<i>و</i>{item.groomName}</h2>{date && <p className="editorial-luxury-closing__date">{date}</p>}
      <span className="editorial-luxury-closing__ornament" aria-hidden="true">❧</span>
      {contactUrl && <a className="editorial-luxury-contact-link" href={contactUrl} rel="noreferrer" target="_blank">للاستفسار عن الدعوة تواصلوا معنا عبر واتساب <span aria-hidden="true">↗</span></a>}
    </footer>
  </article>
}

function findPersonalization(item: PublicInvitation, keyHints: string[], nameHints: string[]): string {
  const normalizedKeys = keyHints.map((hint) => hint.toLocaleLowerCase())
  const normalizedNames = nameHints.map((hint) => hint.toLocaleLowerCase())
  for (const [key, rawValue] of Object.entries(item.customizations ?? {})) {
    if (!item.features.some((feature) => feature.key === key && feature.isEnabled)) continue
    const featureName = item.features.find((feature) => feature.key === key)?.name.toLocaleLowerCase() ?? ''
    const normalizedKey = key.toLocaleLowerCase()
    if (!normalizedKeys.some((hint) => normalizedKey.includes(hint)) && !normalizedNames.some((hint) => featureName.includes(hint))) continue
    const value = String(rawValue).trim()
    if (value) return value
  }
  return ''
}

function FloralMark({ className = '' }: { className?: string }) {
  return <svg className={`editorial-luxury-floral ${className}`} viewBox="0 0 120 180" fill="none" aria-hidden="true" focusable="false">
    <path d="M60 174C64 130 57 92 60 49M59 117C35 111 23 96 18 77M61 137C86 128 99 109 104 88M60 91C76 80 83 63 81 45" />
    <path d="M18 77C32 75 43 81 50 93C34 98 22 92 18 77ZM104 88C89 84 77 91 70 105C87 110 99 103 104 88ZM81 45C68 48 61 59 62 73C77 71 84 61 81 45Z" />
    <path d="M42 40c-8-8-7-20 2-26 3 10 2 19-2 26Zm18-3c-7-9-5-21 5-26 2 10 0 19-5 26Zm16 5c-4-11 2-22 13-24-2 10-6 19-13 24ZM54 30c-3-12 5-22 16-22-4 10-9 17-16 22Z" />
  </svg>
}

export function DefaultInvitationTemplate({ item, slug, guests, previewMode = false }: InvitationTemplateProps) {
  const date = weddingDate(item.weddingDateTimeLocal, item.timeZoneId)
  const customizations = item.customizations ?? {}
  const enabledKeys = new Set(item.features.filter((feature) => feature.isEnabled).map((feature) => feature.key))
  const countdownMessage = typeof customizations.countdown === 'string' ? customizations.countdown.trim() : ''
  const details = Object.entries(customizations).filter(([key, value]) => key !== 'countdown' && enabledKeys.has(key) && String(value).trim())
  const galleryImages = enabledKeys.has('gallery') ? item.media.filter((media) => media.mediaType === 'Image') : []
  const videos = enabledKeys.has('video') ? item.media.filter((media) => media.mediaType === 'Video') : []
  const uploadedAudioUrl = enabledKeys.has('music') ? item.media.find((media) => media.mediaType === 'Audio')?.url : undefined

  return <article className="public-invitation" dir="rtl">
    <section className="public-invitation-hero">
      <p className="public-invitation-kicker">دعوة زفاف</p><div className="public-invitation-ornament" aria-hidden="true">✦</div>
      <p className="public-invitation-intro">بكل الحب والفرح، ندعوكم لمشاركتنا أجمل أيامنا</p>
      <h1>{item.brideName}<span>&amp;</span>{item.groomName}</h1><p className="public-invitation-template">{item.templateName}</p>
      {date && <p className="public-invitation-date">{date}</p>}{item.venueName && <p className="public-invitation-venue">{item.venueName}</p>}
      {item.venueAddress && <p className="public-invitation-address">{item.venueAddress}</p>}
      {item.locationUrl && <a className="public-invitation-map" href={item.locationUrl} rel="noreferrer" target="_blank">افتح الموقع</a>}
      {!previewMode && <button className="primary-button" onClick={() => void shareInvitation(item, slug)} type="button">مشاركة الدعوة</button>}
      {!previewMode && item.hasFinalPng && <a className="public-invitation-card-download" download href={`/api/v1/public/invitations/${encodeURIComponent(slug)}/final-card`}>تنزيل بطاقة الدعوة</a>}
    </section>
    {enabledKeys.has('countdown') && item.weddingDateTimeLocal && <InvitationCountdown dateTime={item.weddingDateTimeLocal} timeZone={item.timeZoneId} postEventMessage={countdownMessage} />}
    {details.length > 0 && <section className="public-invitation-section"><h2>تفاصيل إضافية</h2><dl className="public-invitation-details">{details.map(([key, value]) => <div key={key}><dt>{item.features.find((feature) => feature.key === key)?.name ?? key}</dt><dd>{String(value)}</dd></div>)}</dl></section>}
    {galleryImages.length > 0 && <section className="public-invitation-section"><h2>لحظاتنا</h2><div className="public-invitation-gallery">{galleryImages.map((media) => <img key={media.mediaId} alt="صورة من الدعوة" loading="lazy" src={media.url} />)}</div></section>}
    {videos.map((media) => <section className="public-invitation-section" key={media.mediaId}><video controls playsInline preload="metadata" src={media.url} /></section>)}
    {previewMode ? <InvitationInteractionPreview enabledKeys={enabledKeys} /> : guests && <GuestInteractionsPanel slug={slug} data={guests} musicUrlOverride={guests.musicUrl ?? uploadedAudioUrl} />}
    <footer className="public-invitation-footer">بكل الحب، ننتظر حضوركم ومشاركتكم فرحتنا</footer>
  </article>
}

function InvitationInteractionPreview({ enabledKeys, editorialLuxury = false }: { enabledKeys: Set<string>; editorialLuxury?: boolean }) {
  const hasRsvp = enabledKeys.has('rsvp')
  const hasMessages = enabledKeys.has('guest-messages')
  if (!hasRsvp && !hasMessages) return null

  const sectionClass = editorialLuxury
    ? 'public-invitation-section editorial-luxury-section guest-interactions editorial-luxury-interactions invitation-preview-interactions'
    : 'public-invitation-section guest-interactions invitation-preview-interactions'

  return <section className={sectionClass}>
    {hasRsvp && <div className={editorialLuxury ? 'editorial-luxury-interactions__group' : ''}>
      {editorialLuxury ? <header className="editorial-luxury-section__heading"><p className="el-label">يسعدنا حضوركم</p><h2 className="el-title">تأكيد الحضور</h2></header> : <h2>تأكيد الحضور</h2>}
      <form className="customer-form" onSubmit={(event) => event.preventDefault()}>
        <label>الاسم (اختياري)</label><input aria-label="معاينة حقل الاسم" disabled placeholder="اسم الضيف" />
        <label>هل ستحضرون؟</label><select aria-label="معاينة اختيار الحضور" disabled defaultValue="Yes"><option value="Yes">نعم، سأحضر</option><option value="No">أعتذر عن الحضور</option></select>
        <p className="invitation-preview-interactions__note">هذه معاينة لشكل النموذج؛ تأكيد الحضور يتاح بعد نشر الدعوة.</p>
        <button className="primary-button" disabled type="button">حفظ الرد</button>
      </form>
    </div>}
    {hasMessages && <div className={editorialLuxury ? 'editorial-luxury-interactions__group' : ''}>
      {editorialLuxury ? <header className="editorial-luxury-section__heading"><p className="el-label">كلماتكم تفرحنا</p><h2 className="el-title">رسائل التهنئة</h2></header> : <h2>رسائل التهنئة</h2>}
      <form className="customer-form" onSubmit={(event) => event.preventDefault()}>
        <label>رسالتكم</label><textarea aria-label="معاينة حقل رسالة التهنئة" disabled placeholder="اكتبوا كلمات التهنئة هنا" rows={3} />
        <p className="invitation-preview-interactions__note">إرسال الرسائل يتاح بعد نشر الدعوة.</p>
        <button className="primary-button" disabled type="button">إرسال التهنئة</button>
      </form>
    </div>}
  </section>
}

function InvitationVideo({ src }: { src: string }) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const [isPlaying, setIsPlaying] = useState(false)
  function playVideo() { void videoRef.current?.play().catch(() => setIsPlaying(false)) }

  return <div className={`editorial-luxury-video${isPlaying ? ' editorial-luxury-video--playing' : ''}`}>
    <video aria-label="فيديو من حكاية العروسين" controls playsInline preload="metadata" ref={videoRef} src={src} onEnded={() => setIsPlaying(false)} onPause={() => setIsPlaying(false)} onPlay={() => setIsPlaying(true)} />
    {!isPlaying && <button aria-label="تشغيل فيديو العروسين" className="editorial-luxury-video__play" onClick={playVideo} type="button"><span aria-hidden="true">▶</span></button>}
  </div>
}

function InvitationCountdown({ dateTime, timeZone, postEventMessage, editorialLuxury = false }: { dateTime: string; timeZone: string; postEventMessage: string; editorialLuxury?: boolean }) {
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
  const sectionClass = editorialLuxury
    ? 'public-invitation-section editorial-luxury-section invitation-countdown editorial-luxury-countdown'
    : 'public-invitation-section invitation-countdown'
  if (hasEnded) return <section className={`${sectionClass} invitation-countdown--ended`}><h2>{editorialLuxury ? 'موعدنا السعيد' : 'موعدنا السعيد'}</h2><p>{postEventMessage || 'تمّ الزفاف، ونشكركم على مشاركتكم فرحتنا.'}</p></section>

  const remainingSeconds = Math.max(0, Math.ceil((eventTime - now) / 1000))
  const parts = [
    { value: Math.floor(remainingSeconds / 86400), label: 'أيام' },
    { value: Math.floor((remainingSeconds % 86400) / 3600), label: 'ساعات' },
    { value: Math.floor((remainingSeconds % 3600) / 60), label: 'دقائق' },
    { value: remainingSeconds % 60, label: 'ثوانٍ' },
  ]
  return <section className={sectionClass} aria-label="الوقت المتبقي حتى موعد الزفاف" role="timer" aria-live="off">
    {editorialLuxury ? <header className="editorial-luxury-section__heading"><p className="el-label">نترقب لقاءكم</p><h2 className="el-title">نعدّ الأيام لفرحتنا</h2></header> : <h2>نعدّ الأيام للقائكم</h2>}
    <div className="invitation-countdown__parts">{parts.map((part) => <div key={part.label}><strong key={editorialLuxury ? `${part.label}-${part.value}` : part.label}>{countdownNumber.format(part.value)}</strong><span>{part.label}</span></div>)}</div>
  </section>
}

function GuestInteractionsPanel({ slug, data, editorialLuxury = false, musicAudioRef, musicMuted = false, musicPlaying = false, musicUrlOverride, musicControlVisible = true, onPlaybackChange, onToggleMusic }: {
  slug: string
  data: GuestInteractions
  editorialLuxury?: boolean
  musicAudioRef?: RefObject<HTMLAudioElement | null>
  musicMuted?: boolean
  musicPlaying?: boolean
  musicUrlOverride?: string
  musicControlVisible?: boolean
  onPlaybackChange?: (playing: boolean) => void
  onToggleMusic?: () => void
}) {
  const localMusicAudioRef = useRef<HTMLAudioElement>(null)
  const [localMusicMuted, setLocalMusicMuted] = useState(false)
  const [localMusicPlaying, setLocalMusicPlaying] = useState(false)
  const activeMusicAudioRef = musicAudioRef ?? localMusicAudioRef
  const activeMusicMuted = musicAudioRef ? musicMuted : localMusicMuted
  const activeMusicPlaying = musicAudioRef ? musicPlaying : localMusicPlaying
  const queryClient = useQueryClient()
  const tokenKey = `wedlink:rsvp:${slug}`
  const [response, setResponse] = useState<'Yes' | 'No'>('Yes')
  const [guestName, setGuestName] = useState('')
  const [companions, setCompanions] = useState('0')
  const [messageName, setMessageName] = useState('')
  const [message, setMessage] = useState('')
  const rsvpMutation = useMutation({
    mutationFn: () => submitRsvp(slug, { response, guestName, accompanyingPeople: response === 'Yes' ? Number(companions || 0) : 0, editToken: localStorage.getItem(tokenKey) }),
    onSuccess: async (result) => { localStorage.setItem(tokenKey, result.editToken); await queryClient.invalidateQueries({ queryKey: ['guest-interactions', slug] }) },
  })
  const messageMutation = useMutation({
    mutationFn: () => submitGuestMessage(slug, { guestName: messageName, message }),
    onSuccess: async () => { setMessage(''); await queryClient.invalidateQueries({ queryKey: ['guest-interactions', slug] }) },
  })
  function sendRsvp(event: FormEvent) { event.preventDefault(); rsvpMutation.mutate() }
  function sendMessage(event: FormEvent) { event.preventDefault(); messageMutation.mutate() }
  function toggleMusic() {
    if (onToggleMusic) { onToggleMusic(); return }
    const audio = activeMusicAudioRef.current
    const nextMuted = activeMusicPlaying ? !activeMusicMuted : false
    if (audio) {
      audio.muted = nextMuted
      if (!nextMuted) void audio.play().then(() => { onPlaybackChange?.(true); setLocalMusicPlaying(true) }).catch(() => { onPlaybackChange?.(false); setLocalMusicPlaying(false) })
    }
    setLocalMusicMuted(nextMuted)
  }
  useEffect(() => {
    if (editorialLuxury || !data.musicEnabled || !(musicUrlOverride || data.musicUrl)) return
    const audio = activeMusicAudioRef.current
    if (!audio) return
    void audio.play().then(() => setLocalMusicPlaying(true)).catch(() => undefined)
  }, [data.musicEnabled, data.musicUrl, editorialLuxury, musicUrlOverride])
  const sectionClass = editorialLuxury
    ? 'public-invitation-section editorial-luxury-section guest-interactions editorial-luxury-interactions'
    : 'public-invitation-section guest-interactions'
  const groupClass = editorialLuxury ? 'editorial-luxury-interactions__group' : ''

  return <section className={sectionClass}>
    {data.guestCountEnabled && <p className="guest-attendance-count">عدد الحضور المؤكد: <strong>{data.expectedAttendance}</strong></p>}
    {data.rsvpEnabled && <div className={groupClass}>{editorialLuxury ? <header className="editorial-luxury-section__heading"><p className="el-label">يسعدنا حضوركم</p><h2 className="el-title">تأكيد الحضور</h2></header> : <h2>تأكيد الحضور</h2>}<form className="customer-form" onSubmit={sendRsvp}>
      <label htmlFor="guest-rsvp-name">الاسم (اختياري)</label><input id="guest-rsvp-name" maxLength={150} value={guestName} onChange={(event) => setGuestName(event.target.value)} />
      <label htmlFor="guest-rsvp-response">هل ستحضرون؟</label><select id="guest-rsvp-response" value={response} onChange={(event) => setResponse(event.target.value as 'Yes' | 'No')}><option value="Yes">نعم، سأحضر</option><option value="No">أعتذر عن الحضور</option></select>
      {response === 'Yes' && <><label htmlFor="guest-rsvp-companions">عدد المرافقين الإضافيين</label><input id="guest-rsvp-companions" type="text" inputMode="numeric" pattern="[0-9]*" value={companions} onChange={(event) => setCompanions(event.target.value.replace(/[^0-9]/g, ''))} /></>}
      {rsvpMutation.isError && <p className="form-error" role="alert">{rsvpMutation.error.message}</p>}
      {rsvpMutation.isSuccess && <p className="success-message" role="status">تم حفظ تأكيد الحضور. يمكنك تحديث ردك من هذا المتصفح.</p>}
      <button className="primary-button" disabled={rsvpMutation.isPending} type="submit">{rsvpMutation.isPending ? 'جارٍ الإرسال...' : 'حفظ الرد'}</button>
    </form></div>}
    {data.musicEnabled && (data.musicUrl || musicUrlOverride) && <div className={`guest-music ${groupClass}`}>
      <audio aria-hidden="true" className="guest-music__audio" loop muted={activeMusicMuted} onPause={() => { onPlaybackChange?.(false); setLocalMusicPlaying(false) }} onPlay={() => { onPlaybackChange?.(true); setLocalMusicPlaying(true) }} preload="auto" ref={activeMusicAudioRef} src={musicUrlOverride ?? data.musicUrl ?? undefined} tabIndex={-1} />
      {musicControlVisible && <button aria-label={activeMusicPlaying && !activeMusicMuted ? 'كتم الموسيقى' : 'تشغيل الموسيقى'} aria-pressed={activeMusicMuted} className="guest-music__toggle" onClick={toggleMusic} type="button">
        <span aria-hidden="true">{activeMusicPlaying && !activeMusicMuted ? '♪' : '♪̸'}</span><span>{activeMusicPlaying && !activeMusicMuted ? 'كتم الموسيقى' : 'تشغيل الموسيقى'}</span>
      </button>}
    </div>}
    {data.messagesEnabled && <div className={`guest-messages ${groupClass}`}>{editorialLuxury ? <header className="editorial-luxury-section__heading"><p className="el-label">كلماتكم تفرحنا</p><h2 className="el-title">رسائل التهنئة</h2></header> : <h2>رسائل التهنئة</h2>}<form className="customer-form" onSubmit={sendMessage}>
      <label htmlFor="guest-message-name">اسمك (اختياري)</label><input id="guest-message-name" maxLength={150} value={messageName} onChange={(event) => setMessageName(event.target.value)} />
      <label htmlFor="guest-message-text">رسالتك</label><textarea id="guest-message-text" required maxLength={2000} rows={3} value={message} onChange={(event) => setMessage(event.target.value)} />
      {messageMutation.isError && <p className="form-error" role="alert">{messageMutation.error.message}</p>}
      {messageMutation.isSuccess && <p className="success-message" role="status">وصلت رسالتك، شكرًا لك.</p>}
      <button className="primary-button" disabled={messageMutation.isPending} type="submit">إرسال التهنئة</button>
    </form><div className="public-guest-messages">{data.messages.map((item) => <article key={item.guestMessageId}><strong>{item.guestName || 'ضيف'}</strong><p>{item.message}</p></article>)}</div></div>}
  </section>
}

