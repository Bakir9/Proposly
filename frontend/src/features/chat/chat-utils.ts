import { useEffect, useState } from 'react'
import { fetchAttachmentBlob, type ConversationResponse } from '@/api/chat'

/** Stable hue (0–359) derived from an id, so every user/conversation keeps its avatar color. */
export function hueFromId(id: string): number {
  let hash = 0
  for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0
  return hash % 360
}

/** Avatar colors per the design handoff: oklch pairs, different lightness per theme. */
export function avatarColors(hue: number, dark: boolean) {
  return dark
    ? { background: `oklch(0.32 0.06 ${hue})`, color: `oklch(0.86 0.08 ${hue})` }
    : { background: `oklch(0.93 0.04 ${hue})`, color: `oklch(0.4 0.1 ${hue})` }
}

export function initials(name: string): string {
  return name
    .split(/[\s·]+/)
    .filter(w => /^[A-Za-zÀ-ž0-9]/.test(w))
    .slice(0, 2)
    .map(w => w[0])
    .join('')
    .toUpperCase()
}

/** The hue of a conversation avatar: direct uses the other person, groups/projects the conversation id. */
export function conversationHue(c: ConversationResponse, currentUserId: string): number {
  if (c.kind === 'Direct') {
    const other = c.participants.find(p => p.userId !== currentUserId)
    return hueFromId(other?.userId ?? c.id)
  }
  return hueFromId(c.id)
}

export const IMAGE_RE = /\.(jpe?g|png|gif|webp|heic|avif)$/i

export const MAX_FILE_SIZE = 25 * 1024 * 1024

export const ACCEPTED_FILE_TYPES =
  '.jpg,.jpeg,.png,.webp,.heic,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.dwg,.zip'

export function extOf(fileName: string): string {
  return (fileName.includes('.') ? fileName.split('.').pop()! : 'FILE').toUpperCase().slice(0, 4)
}

const EXT_HUE: Record<string, number> = { PDF: 25, XLSX: 150, XLS: 150, CSV: 150, DOCX: 250, DOC: 250, DWG: 300, ZIP: 80 }

/** Extension badge colors (PDF warm, spreadsheets green, docs blue …) per the handoff. */
export function extColors(ext: string, dark: boolean): { background: string; color: string } {
  const hue = EXT_HUE[ext]
  if (hue == null) return { background: 'var(--ext-muted-bg, #f4f4f5)', color: 'var(--ext-muted-fg, #52525b)' }
  return dark
    ? { background: `oklch(0.3 0.06 ${hue})`, color: `oklch(0.82 0.1 ${hue})` }
    : { background: `oklch(0.94 0.05 ${hue})`, color: `oklch(0.45 0.13 ${hue})` }
}

export function formatBytes(bytes: number): string {
  return bytes >= 1048576
    ? `${(bytes / 1048576).toFixed(1).replace('.0', '')} MB`
    : `${Math.max(1, Math.round(bytes / 1024))} KB`
}

/** List timestamps: HH:mm today, weekday inside the last week, else "26 Sep". */
export function formatWhen(iso: string): string {
  const date = new Date(iso)
  const now = new Date()
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  if (date >= startOfToday) return date.toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' })
  const weekAgo = new Date(startOfToday)
  weekAgo.setDate(weekAgo.getDate() - 6)
  if (date >= weekAgo) return date.toLocaleDateString('en-GB', { weekday: 'short' })
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })
}

export function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' })
}

/** Day-divider label: Today, Yesterday, weekday inside the last week, else "26 September". */
export function dayLabel(iso: string): string {
  const date = new Date(iso)
  const now = new Date()
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const startOfDay = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const diffDays = Math.round((startOfToday.getTime() - startOfDay.getTime()) / 86_400_000)
  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return date.toLocaleDateString('en-GB', { weekday: 'long' })
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'long' })
}

export function conversationPreview(c: ConversationResponse): string {
  if (!c.lastMessagePreview) return 'No messages yet'
  const prefix = c.lastMessageIsOwn
    ? 'You: '
    : c.kind !== 'Direct' && c.lastMessageAuthorFirstName
      ? `${c.lastMessageAuthorFirstName}: `
      : ''
  return prefix + c.lastMessagePreview
}

/** Downloads an authorized attachment via a temporary object URL. */
export async function downloadAttachment(attachmentId: string, fileName: string) {
  const blob = await fetchAttachmentBlob(attachmentId)
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

/** Fetches an image attachment once and exposes it as an object URL (revoked on unmount). */
export function useAttachmentUrl(attachmentId: string, enabled: boolean): string | null {
  const [url, setUrl] = useState<string | null>(null)

  useEffect(() => {
    if (!enabled) return
    let revoked: string | null = null
    let cancelled = false
    fetchAttachmentBlob(attachmentId)
      .then(blob => {
        if (cancelled) return
        revoked = URL.createObjectURL(blob)
        setUrl(revoked)
      })
      .catch(() => {})
    return () => {
      cancelled = true
      if (revoked) URL.revokeObjectURL(revoked)
    }
  }, [attachmentId, enabled])

  return url
}
