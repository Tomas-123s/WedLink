import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createMusicTrack, getMusicTracks, updateMusicTrack, type MusicTrack } from '../invitations/invitationsApi'

export function MusicLibraryPage() {
  const client = useQueryClient()
  const tracks = useQuery({ queryKey: ['music-tracks'], queryFn: getMusicTracks })
  const [title, setTitle] = useState('')
  const [artist, setArtist] = useState('')
  const [rightsReference, setRightsReference] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const create = useMutation({ mutationFn: () => {
    if (!file) throw new Error('اختر ملف MP3 أو M4A.')
    return createMusicTrack({ title, artist, rightsReference, file })
  }, onSuccess: async () => { setTitle(''); setArtist(''); setRightsReference(''); setFile(null); await client.invalidateQueries({ queryKey: ['music-tracks'] }) } })
  const update = useMutation({ mutationFn: (track: MusicTrack) => updateMusicTrack(track), onSuccess: async () => client.invalidateQueries({ queryKey: ['music-tracks'] }) })
  function submit(event: FormEvent) { event.preventDefault(); create.mutate() }
  return <section className="music-library-page" dir="rtl">
    <header className="section-heading"><p className="page-card__eyebrow">WedLink</p><h1>مكتبة الموسيقى</h1><p>أضف فقط المسارات التي لديك إذن باستخدامها، واكتب مرجع الترخيص أو الإذن.</p></header>
    <div className="music-library-layout">
      <section className="customer-panel"><h2>إضافة مسار مرخص</h2><form className="customer-form" onSubmit={submit}>
        <label htmlFor="music-title">عنوان المسار</label><input id="music-title" maxLength={200} required value={title} onChange={(event) => setTitle(event.target.value)} />
        <label htmlFor="music-artist">الفنان</label><input id="music-artist" maxLength={150} value={artist} onChange={(event) => setArtist(event.target.value)} />
        <label htmlFor="music-rights">مرجع الترخيص أو الإذن</label><textarea id="music-rights" maxLength={1000} required rows={3} value={rightsReference} onChange={(event) => setRightsReference(event.target.value)} />
        <label htmlFor="music-file">ملف MP3 أو M4A (حتى 20MB)</label><input id="music-file" accept=".mp3,.m4a,audio/mpeg,audio/mp4" required type="file" onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
        {create.isError && <p className="form-error" role="alert">{create.error.message}</p>}
        {create.isSuccess && <p className="success-message" role="status">تمت إضافة المسار.</p>}
        <button className="primary-button" disabled={create.isPending} type="submit">{create.isPending ? 'جارٍ الرفع...' : 'إضافة للمكتبة'}</button>
      </form></section>
      <section className="customer-panel"><h2>المسارات</h2>{tracks.isPending && <p>جارٍ التحميل...</p>}{tracks.isError && <p className="form-error">{tracks.error.message}</p>}
        {tracks.data?.map((track) => <article className="music-track-row" key={track.musicTrackId}><div><strong>{track.title}</strong><p>{track.artist || 'فنان غير محدد'} · {track.isActive ? 'متاح' : 'موقوف'}</p><small>{track.rightsReference}</small></div><button className="secondary-button" disabled={update.isPending} onClick={() => update.mutate({ ...track, isActive: !track.isActive })} type="button">{track.isActive ? 'إيقاف' : 'تفعيل'}</button></article>)}
      </section>
    </div>
  </section>
}
