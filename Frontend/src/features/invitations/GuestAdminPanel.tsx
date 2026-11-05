import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { deleteAdminGuestMessage, getAdminGuestMessages, getAdminRsvps, restoreAdminGuestMessage } from './invitationsApi'

export function GuestAdminPanel({ invitationId }: { invitationId: number }) {
  const queryClient = useQueryClient()
  const rsvps = useQuery({ queryKey: ['admin-rsvps', invitationId], queryFn: () => getAdminRsvps(invitationId) })
  const messages = useQuery({ queryKey: ['admin-guest-messages', invitationId], queryFn: () => getAdminGuestMessages(invitationId) })
  const deleteMutation = useMutation({ mutationFn: (messageId: number) => deleteAdminGuestMessage(invitationId, messageId), onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['admin-guest-messages', invitationId] }) })
  const restoreMutation = useMutation({ mutationFn: (messageId: number) => restoreAdminGuestMessage(invitationId, messageId), onSuccess: async () => queryClient.invalidateQueries({ queryKey: ['admin-guest-messages', invitationId] }) })
  return <section className="guest-admin-panel">
    <h3>ردود تأكيد الحضور</h3>
    {rsvps.isPending && <p>جارٍ تحميل الردود...</p>}{rsvps.isError && <p className="form-error">{rsvps.error.message}</p>}
    {rsvps.data && <><p>حضور: {rsvps.data.yesCount} · اعتذار: {rsvps.data.noCount} · إجمالي الحضور المتوقع: <strong>{rsvps.data.expectedAttendance}</strong></p><div className="guest-admin-list">{rsvps.data.responses.map((row) => <article key={row.rsvpResponseId}><strong>{row.guestName || 'ضيف بدون اسم'}</strong><span>{row.response === 'Yes' ? `حاضر · ${row.expectedAttendance} فرد` : 'اعتذر عن الحضور'}</span><time>{new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(row.submittedAt))}</time></article>)}</div></>}
    <h3>رسائل التهنئة</h3>
    {messages.isPending && <p>جارٍ تحميل الرسائل...</p>}{messages.isError && <p className="form-error">{messages.error.message}</p>}
    {messages.data?.map((item) => <article className={`admin-guest-message${item.deletedAt ? ' is-deleted' : ''}`} key={item.guestMessageId}><div><strong>{item.guestName || 'ضيف'}</strong><time>{new Intl.DateTimeFormat('ar-EG', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(item.submittedAt))}</time></div><p>{item.message}</p>{item.deletedAt ? <><span>محذوفة من العرض العام</span><button className="text-button" disabled={restoreMutation.isPending} onClick={() => restoreMutation.mutate(item.guestMessageId)} type="button">استعادة التعليق</button></> : <button className="text-button text-button--danger" disabled={deleteMutation.isPending} onClick={() => deleteMutation.mutate(item.guestMessageId)} type="button">حذف الرسالة</button>}</article>)}
    {deleteMutation.isError && <p className="form-error">{deleteMutation.error.message}</p>}
    {restoreMutation.isError && <p className="form-error">{restoreMutation.error.message}</p>}
  </section>
}
