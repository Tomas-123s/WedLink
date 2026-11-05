import type { PublicTemplate } from './publicTemplatesApi'
import './InvitationPagePreview.css'

export function InvitationPagePreview({ design, compact = false }: { design: PublicTemplate; compact?: boolean }) {
  const isEditorialLuxury = design.name.toLowerCase().includes('editorial luxury')
  const featureKeys = new Set(design.features.map((feature) => feature.key))
  const hasCountdown = featureKeys.has('countdown')
  const hasGallery = featureKeys.has('gallery')
  const hasRsvp = featureKeys.has('rsvp')
  const hasMessages = featureKeys.has('guest-messages')

  return <article className={`invitation-page-preview${compact ? ' invitation-page-preview--compact' : ''}${isEditorialLuxury ? ' invitation-page-preview--editorial' : ''}`} aria-label={`نموذج كامل لتصميم ${design.name}`}>
    <div className="invitation-page-preview__canvas">
      <div className="invitation-page-preview__brand"><span>W</span><small>دعوة خاصة</small></div>
      <header className="invitation-page-preview__hero">
        <p className="invitation-page-preview__kicker">بكل الحب والفرح</p>
        <div className={`invitation-page-preview__portrait${design.previewImageUrl ? ' invitation-page-preview__portrait--image' : ''}`}>
          {design.previewImageUrl
            ? <img alt="صورة تجريبية لتصميم الدعوة" src={design.previewImageUrl} />
            : <span aria-hidden="true">L <i>&amp;</i> O</span>}
        </div>
        <p className="invitation-page-preview__invitation-line">يتشرفان بدعوتكم لمشاركتهما فرحتهما</p>
        <h2>ليلى <i>&amp;</i> عمر</h2>
        <p className="invitation-page-preview__date">الأحد، ٢٠ ديسمبر ٢٠٢٦ · السابعة مساءً</p>
      </header>

      {hasCountdown && <section className="invitation-page-preview__section invitation-page-preview__countdown">
        <p className="invitation-page-preview__eyebrow">نترقب لقاءكم</p>
        <h3>العد التنازلي لفرحتنا</h3>
        <div><span><b>٤٥</b><small>يومًا</small></span><span><b>١٢</b><small>ساعة</small></span><span><b>٣٠</b><small>دقيقة</small></span><span><b>٠٨</b><small>ثانية</small></span></div>
      </section>}

      <section className="invitation-page-preview__section invitation-page-preview__details">
        <p className="invitation-page-preview__eyebrow">موعدنا ومكاننا</p>
        <h3>تفاصيل المناسبة</h3>
        <p><strong>قصر النيل</strong><small>كورنيش النيل · القاهرة</small></p>
        <span className="invitation-page-preview__map">عرض الموقع على الخريطة ↗</span>
      </section>

      {hasGallery && <section className="invitation-page-preview__section invitation-page-preview__gallery">
        <p className="invitation-page-preview__eyebrow">من حكايتنا</p>
        <h3>لحظاتنا</h3>
        <div aria-hidden="true"><span /><span /><span /></div>
      </section>}

      {hasRsvp && <section className="invitation-page-preview__section invitation-page-preview__rsvp">
        <p className="invitation-page-preview__eyebrow">يسعدنا حضوركم</p>
        <h3>تأكيد الحضور</h3>
        <div><span>الاسم</span><span>نعم، سأحضر</span></div>
        <b>إرسال التأكيد</b>
      </section>}

      {hasMessages && <section className="invitation-page-preview__wishes"><span>رسائل التهنئة</span><p>«فرحتكم أجمل بداية، ألف مبروك»</p></section>}
      <footer className="invitation-page-preview__footer">بكل الحب، ننتظر مشاركتكم فرحتنا <span>ليلى &amp; عمر</span></footer>
    </div>
    {compact && <span className="invitation-page-preview__sample-label">معاينة كاملة · بيانات تجريبية</span>}
  </article>
}
