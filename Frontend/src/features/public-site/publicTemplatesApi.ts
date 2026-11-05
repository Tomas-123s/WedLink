export type PublicTemplateFeature = { key: string; name: string; description: string | null; maximumCount: number | null; maximumFileSizeMb: number | null }
export type PublicTemplate = { templateId: number; name: string; description: string | null; previewImageUrl: string | null; versionNumber: string; features: PublicTemplateFeature[] }

export async function getPublicTemplates(): Promise<PublicTemplate[]> {
  const response = await fetch('/api/v1/public/templates')
  if (!response.ok) throw new Error('تعذر تحميل التصاميم الآن. حاول مرة أخرى بعد قليل.')
  return response.json() as Promise<PublicTemplate[]>
}

export function getWhatsAppUrl(message: string) {
  const phone = (import.meta.env.VITE_PUBLIC_WHATSAPP_PHONE ?? '201210143454').replace(/\D/g, '')
  if (!phone) return null
  return `https://wa.me/${phone}?text=${encodeURIComponent(message)}`
}
