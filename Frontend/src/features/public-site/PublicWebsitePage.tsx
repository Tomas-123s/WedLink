import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { getPublicTemplates, getWhatsAppUrl } from './publicTemplatesApi'
import { InvitationPagePreview } from './InvitationPagePreview'

export function PublicWebsitePage() {
  const templates = useQuery({ queryKey: ['public-templates'], queryFn: getPublicTemplates, retry: false })
  const whatsApp = getWhatsAppUrl(import.meta.env.VITE_PUBLIC_WHATSAPP_MESSAGE || 'مرحبًا، أود الاستفسار عن دعوات WedLink الرقمية.')

  return <div className="public-site" dir="rtl">
    <header className="public-site-header">
      <a className="public-brand" href="#home"><span className="public-brand-mark">W</span><span>WedLink<small>حكايتكم، بطريقتكم</small></span></a>
      <nav aria-label="التنقل الرئيسي"><a href="#designs">التصاميم</a><a href="#how-it-works">طريقة العمل</a><a href="#contact">عن WedLink</a></nav>
      <Link className="public-header-cta public-admin-switch" to="/admin"><svg aria-hidden="true" viewBox="0 0 20 20"><path d="M3 8.2 10 3l7 5.2v8.1h-5v-5H8v5H3z"/><path d="M1.8 8.1 10 2l8.2 6.1"/></svg><span>مساحة الإدارة</span></Link>
    </header>
    <main>
      <section className="public-hero" id="home">
        <div className="public-hero-copy">
          <p className="public-eyebrow"><span /> يومكم يستاهل بداية مميزة</p>
          <h1>خلّوا دعوتكم<br/>تحكي <em>حكايتكم</em></h1>
          <p className="public-hero-lead">تفاصيل يومكم الجميل في دعوة رقمية أنيقة، مصممة لكم وسهلة المشاركة مع كل اللي بتحبوهم.</p>
          <div className="public-hero-actions"><a className="public-button public-button--dark" href="#designs">شوفوا التصاميم <span>←</span></a></div>
          <div className="public-hero-note"><span>✳</span> تصميم خاص بيكم، ورابط واحد يجمع كل التفاصيل</div>
        </div>
        <div className="public-hero-art" aria-label="نموذج دعوة زفاف رقمية بألوان WedLink الجديدة">
          <div className="hero-sun"/><div className="hero-ring hero-ring--one"/><div className="hero-ring hero-ring--two"/><div className="hero-flower">✳</div>
          <div className="hero-invite"><span className="hero-invite-kicker">يوم من العمر</span><span className="hero-invite-names">ليلى <i>&amp;</i> عمر</span><span className="hero-invite-line"/><span className="hero-invite-date">٢٠ ديسمبر · القاهرة</span><span className="hero-invite-tag">يسعدنا حضوركم</span></div>
          <span className="hero-orbit hero-orbit--a">✦</span><span className="hero-orbit hero-orbit--b">✧</span>
          <span className="hero-art-caption">لحظاتكم، بكل تفاصيلها</span>
        </div>
      </section>

      <section className="public-designs section-wrap" id="designs">
        <div className="public-section-heading"><div><p className="public-eyebrow">اختاروا اللي يشبهكم</p><h2>كل حكاية وليها شكل</h2></div><p>تصفحوا التصاميم واختاروا البداية الأقرب لذوقكم، وكل تفصيلة بعدها علينا.</p></div>
        {templates.isPending ? <p className="public-state">بنجهز لكم التصاميم...</p> : templates.isError ? <p className="public-state public-state--error" role="alert">{templates.error.message}</p> : templates.data.length === 0 ? <div className="public-empty"><span>✳</span><h3>تصاميم جديدة في الطريق</h3><p>قريبًا هتلاقوا هنا اختيارات أكتر ليومكم.</p></div> : <div className="public-design-grid">{templates.data.map((design, index) => <article className="public-design-card" key={design.templateId}><Link aria-label={`معاينة تصميم ${design.name}`} className={`public-design-art public-design-art--${index % 3}`} to={`/designs/${design.templateId}`}><InvitationPagePreview compact design={design} /><span className="public-preview-chip">شوفوا تفاصيل التصميم <span>↗</span></span></Link><div className="public-design-info"><div><p>تصميم {String(index + 1).padStart(2, '0')}</p><h3>{design.name}</h3></div><Link aria-label={`افتح معاينة ${design.name}`} className="public-design-open" to={`/designs/${design.templateId}`}>↗</Link></div>{design.description && <p className="public-design-description">{design.description}</p>}<div className="public-feature-list">{design.features.slice(0, 3).map(feature => <span key={feature.key}>{feature.name}</span>)}</div></article>)}</div>}
      </section>

      <section className="public-how section-wrap" id="how-it-works"><div className="public-how-intro"><p className="public-eyebrow">من أول فكرة لحد ما تبعتوها</p><h2>خطوات بسيطة<br/>ليوم كبير</h2><p>نكون معاكم في كل خطوة، من اختيار التصميم لحد ما دعوتكم توصل لكل ضيوفكم.</p></div><div className="public-steps"><article><span>01</span><h3>اختاروا تصميمكم</h3><p>تصفحوا المجموعة واختاروا التصميم الأقرب لذوقكم.</p></article><article><span>02</span><h3>احكوا لنا التفاصيل</h3><p>الأسماء والميعاد والمكان، وكل تفصيلة تحبوا تضيفوها.</p></article><article><span>03</span><h3>راجعوا دعوتكم</h3><p>نجهز لكم معاينة خاصة ونظبطها سوا لحد ما تبقى زي ما تخيلتوها.</p></article><article><span>04</span><h3>شاركوها مع أحبابكم</h3><p>رابط واحد سهل، يوصل دعوتكم للعيلة والصحاب.</p></article></div></section>

      <section className="public-contact" id="contact"><div><p className="public-eyebrow">أهلًا بيكم في WedLink</p><h2>أجمل يوم يبدأ بتفصيلة</h2><p>إحنا هنا عشان نحول فكرتكم لدعوة تليق بيومكم. ابعتولنا من زر واتساب العائم.</p></div><span className="public-contact-mark" aria-hidden="true">W</span></section>
    </main>
    <footer className="public-footer"><a className="public-brand" href="#home"><span className="public-brand-mark">W</span><span>WedLink<small>حكايتكم، بطريقتكم</small></span></a><p>تفاصيل صغيرة، ذكرى كبيرة.</p><Link to="/admin">الإدارة</Link><span>© WedLink 2026</span></footer>
    {whatsApp && <a className="public-whatsapp-float" href={whatsApp} rel="noreferrer" target="_blank" aria-label="تواصلوا مع WedLink عبر واتساب"><svg aria-hidden="true" viewBox="0 0 32 32"><path d="M16 3.2A12.7 12.7 0 0 0 5.1 22.4L3.4 29l6.8-1.8A12.8 12.8 0 1 0 16 3.2Zm0 23.2c-1.9 0-3.7-.5-5.3-1.5l-.4-.2-4 .1 1.1-3.8-.3-.4A10.3 10.3 0 1 1 16 26.4Zm5.7-7.7c-.3-.2-1.8-.9-2.1-1-.3-.1-.5-.2-.7.2-.2.3-.8 1-1 1.2-.2.2-.4.2-.7.1-.3-.2-1.3-.5-2.4-1.5-.9-.8-1.5-1.8-1.7-2.1-.2-.3 0-.5.1-.6l.5-.6.3-.5c.1-.2 0-.4 0-.6l-1-2.3c-.2-.6-.5-.5-.7-.5h-.6c-.2 0-.6.1-.9.4-.3.3-1.2 1.1-1.2 2.7s1.2 3.1 1.4 3.3c.2.2 2.3 3.5 5.6 4.8.8.3 1.4.5 1.9.6.8.2 1.5.2 2 .1.6-.1 1.8-.7 2.1-1.4.3-.7.3-1.3.2-1.4-.1-.2-.3-.3-.6-.4Z"/></svg><span>راسلونا على واتساب</span></a>}
  </div>
}
