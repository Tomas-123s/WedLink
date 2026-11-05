import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { getGuestInteractions, getPublicInvitation } from './invitationsApi'
import { DefaultInvitationTemplate, EditorialLuxuryTemplate } from './PublicInvitationTemplates'

export function PublicInvitationPage() {
  const { slug = '' } = useParams()
  const invitation = useQuery({ queryKey: ['public-invitation', slug], queryFn: () => getPublicInvitation(slug), retry: false })
  const guests = useQuery({ queryKey: ['guest-interactions', slug], queryFn: () => getGuestInteractions(slug), retry: false })

  if (invitation.isPending) return <p className="public-invitation-state" role="status">جارٍ فتح الدعوة...</p>
  if (invitation.isError) return <section className="public-invitation-state" dir="rtl"><h1>الدعوة غير متاحة</h1><p>قد يكون الرابط غير صحيح أو أن الدعوة متوقفة.</p></section>

  const item = invitation.data
  if (item.eventEnded) return <section className="public-invitation-state" dir="rtl"><p className="public-invitation-kicker">WedLink</p><h1>انتهى موعد هذه الدعوة</h1><p>نتمنى للعروسين حياة مليئة بالفرح والسعادة.</p>{item.hasFinalPng && <a className="public-invitation-card-download" download href={`/api/v1/public/invitations/${encodeURIComponent(slug)}/final-card`}>تنزيل بطاقة الدعوة</a>}</section>

  const props = { item, slug, guests: guests.data }
  return item.rendererKey === 'editorial-luxury'
    ? <EditorialLuxuryTemplate {...props} />
    : <DefaultInvitationTemplate {...props} />
}
