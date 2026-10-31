import type { ChatMessageResponse, Presence, PresenceResponse } from '@/api/chat'
import { dayLabel } from '../chat-utils'

export function presenceLabel(u: PresenceResponse): string {
  if (u.presence === 'online') return 'Active now'
  if (u.presence === 'away') return 'Away'
  return u.lastSeenAt ? `Active ${relativeAgo(u.lastSeenAt)} ago` : 'Offline'
}

export function relativeAgo(iso: string, now = new Date()): string {
  const m = Math.max(1, Math.round((now.getTime() - new Date(iso).getTime()) / 60_000))
  if (m < 60) return `${m} min`
  const h = Math.round(m / 60)
  if (h < 24) return `${h} h`
  return `${Math.round(h / 24)} d`
}

export const PRESENCE_SECTIONS: [Presence, string][] = [
  ['online', 'Active now'],
  ['away', 'Away'],
  ['offline', 'Offline'],
]

export type DockThreadItem =
  | { type: 'day'; key: string; label: string }
  | {
      type: 'message'
      key: string
      message: ChatMessageResponse & { status?: 'sending' | 'sent' | 'failed' }
      mine: boolean
      /** First / last message of an author group (same author, same day). */
      first: boolean
      last: boolean
      /** Very last message in the window. */
      isLatest: boolean
    }

/** Day labels + author groups for the dock window (joined bubble corners inside a group). */
export function buildDockThread(
  messages: (ChatMessageResponse & { status?: 'sending' | 'sent' | 'failed' })[],
  meId: string,
): DockThreadItem[] {
  const out: DockThreadItem[] = []
  const dayOf = (iso: string) => new Date(iso).toDateString()
  messages.forEach((m, i) => {
    const prev = messages[i - 1]
    const next = messages[i + 1]
    const newDay = !prev || dayOf(prev.createdAt) !== dayOf(m.createdAt)
    if (newDay) out.push({ type: 'day', key: 'd-' + m.id, label: dayLabel(m.createdAt) })
    const nextSameGroup = !!next && next.authorUserId === m.authorUserId && dayOf(next.createdAt) === dayOf(m.createdAt)
    out.push({
      type: 'message',
      key: m.id,
      message: m,
      mine: m.authorUserId === meId,
      first: newDay || prev.authorUserId !== m.authorUserId,
      last: !nextSameGroup,
      isLatest: !next,
    })
  })
  return out
}
