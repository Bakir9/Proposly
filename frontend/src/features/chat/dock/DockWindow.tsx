import { useLayoutEffect, useRef, useState, type KeyboardEvent, type ReactNode } from 'react'
import { Download, Minus, Paperclip, SendHorizontal, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useTheme } from '@/context/ThemeContext'
import type { ChatAttachmentResponse, PresenceResponse } from '@/api/chat'
import {
  ACCEPTED_FILE_TYPES,
  IMAGE_RE,
  downloadAttachment,
  extColors,
  extOf,
  formatBytes,
  formatTime,
  hueFromId,
  useAttachmentUrl,
} from '../chat-utils'
import { ChatAvatar } from '../ChatAvatar'
import { buildDockThread, presenceLabel, type DockThreadItem } from './dock-utils'

const R = 18 // bubble radius
const S = 5 // joined corner radius inside an author group

export type DockMessage = Parameters<typeof buildDockThread>[0][number]

interface Props {
  user: PresenceResponse
  messages: DockMessage[]
  meId: string
  /** e.g. "Uploading 1 file…" while attachments are on their way. */
  notice?: string
  onSend: (body: string) => void
  onAttachFiles: (files: File[]) => void
  onMinimize: () => void
  onClose: () => void
  autoFocus?: boolean
}

/** Messenger-style 328 × 456 window docked to the bottom edge. */
export function DockWindow({ user, messages, meId, notice, onSend, onAttachFiles, onMinimize, onClose, autoFocus }: Props) {
  const [draft, setDraft] = useState('')
  const scrollRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const fileRef = useRef<HTMLInputElement>(null)
  const hue = hueFromId(user.userId)
  const items = buildDockThread(messages, meId)

  useLayoutEffect(() => {
    const el = scrollRef.current
    if (el) el.scrollTop = el.scrollHeight
  }, [messages.length, notice])
  useLayoutEffect(() => {
    if (autoFocus) inputRef.current?.focus()
  }, [autoFocus])

  const send = () => {
    const t = draft.trim()
    if (!t) return
    setDraft('')
    onSend(t)
  }
  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.nativeEvent.isComposing) {
      e.preventDefault()
      send()
    }
    if (e.key === 'Escape') onMinimize()
  }

  return (
    <section
      aria-label={`Chat with ${user.name}`}
      className="pointer-events-auto flex h-[min(456px,calc(100vh-72px))] w-[328px] flex-col overflow-hidden rounded-t-xl border border-b-0 bg-card shadow-dock"
    >
      <header onClick={onMinimize} className="flex h-14 shrink-0 cursor-pointer items-center gap-2.5 border-b pr-2 pl-3">
        <ChatAvatar name={user.name} hue={hue} size={36} fontSize={12} presence={user.presence} />
        <div className="min-w-0 flex-1">
          <div className="truncate text-sm font-semibold">{user.name}</div>
          <div className="truncate text-xs text-muted-foreground">{presenceLabel(user)}</div>
        </div>
        <IconBtn title="Minimize" onClick={onMinimize}>
          <Minus className="h-4 w-4" />
        </IconBtn>
        <IconBtn title="Close" onClick={onClose}>
          <X className="h-4 w-4" />
        </IconBtn>
      </header>

      <div ref={scrollRef} className="flex min-h-0 flex-1 flex-col overflow-auto px-3 pt-2 pb-3">
        {messages.length === 0 && (
          <div className="m-auto flex shrink-0 flex-col items-center gap-2 p-4 text-center">
            <ChatAvatar name={user.name} hue={hue} size={56} fontSize={18} />
            <div className="text-sm font-semibold">{user.name}</div>
            <div className="text-xs text-muted-foreground">{user.role}</div>
            <div className="mt-1 text-xs text-muted-foreground/70">No messages yet. Say hello.</div>
          </div>
        )}
        {items.map(it => (
          <DockItem key={it.key} it={it} user={user} hue={hue} />
        ))}
        {notice && <div className="mt-2 shrink-0 text-right text-[10px] text-muted-foreground/70">{notice}</div>}
      </div>

      <div className="flex shrink-0 items-center gap-1 border-t p-2">
        <IconBtn title="Attach file" onClick={() => fileRef.current?.click()} size="lg">
          <Paperclip className="h-[17px] w-[17px]" strokeWidth={1.8} />
        </IconBtn>
        <input
          ref={inputRef}
          value={draft}
          onChange={e => setDraft(e.target.value)}
          onKeyDown={onKeyDown}
          placeholder="Aa"
          className="h-[34px] min-w-0 flex-1 rounded-full border-0 bg-muted px-3 text-[13px] text-foreground outline-none placeholder:text-muted-foreground/70"
        />
        <IconBtn title="Send" onClick={send} size="lg" className={draft.trim() ? 'text-foreground' : 'text-muted-foreground/70'}>
          <SendHorizontal className="h-[17px] w-[17px]" strokeWidth={1.9} />
        </IconBtn>
        <input
          ref={fileRef}
          type="file"
          multiple
          accept={ACCEPTED_FILE_TYPES}
          className="hidden"
          onChange={e => {
            onAttachFiles(Array.from(e.target.files ?? []))
            e.target.value = ''
          }}
        />
      </div>
    </section>
  )
}

function DockItem({ it, user, hue }: { it: DockThreadItem; user: PresenceResponse; hue: number }) {
  if (it.type === 'day')
    return <div className="mt-3 mb-1 shrink-0 text-center text-[11px] font-medium text-muted-foreground/70">{it.label}</div>

  const { message: m, mine, first, last, isLatest } = it
  const radius = mine
    ? `${R}px ${first ? R : S}px ${last ? R : S}px ${R}px`
    : `${first ? R : S}px ${R}px ${R}px ${last ? R : S}px`
  const time = formatTime(m.createdAt)
  const status = m.status === 'sending' ? 'Sending…' : m.status === 'failed' ? 'Not sent' : 'Sent'

  return (
    <div title={time} className={cn('flex shrink-0 items-end gap-1.5', mine && 'flex-row-reverse', first ? 'mt-2.5' : 'mt-0.5')}>
      {!mine && (
        <div className={cn('shrink-0', !last && 'invisible')}>
          <ChatAvatar name={user.name} hue={hue} size={26} fontSize={10} />
        </div>
      )}
      <div className={cn('flex max-w-[78%] min-w-0 flex-col gap-[3px]', mine ? 'items-end' : 'items-start')}>
        {m.body && (
          <div
            className={cn(
              'px-[11px] py-[7px] text-[13px] leading-[1.45] [overflow-wrap:anywhere] whitespace-pre-wrap',
              mine ? 'bg-primary text-primary-foreground' : 'bg-muted text-foreground',
            )}
            style={{ borderRadius: radius }}
          >
            {m.body}
          </div>
        )}
        {m.attachments.map(a =>
          IMAGE_RE.test(a.fileName) || a.contentType.startsWith('image/') ? (
            <DockImage key={a.id} attachment={a} />
          ) : (
            <DockFileCard key={a.id} attachment={a} />
          ),
        )}
        {last && (
          <div className={cn('px-1 text-[10px]', m.status === 'failed' ? 'text-destructive' : 'text-muted-foreground/70')}>
            {mine && isLatest && m.status ? `${time} · ${status}` : time}
          </div>
        )}
      </div>
    </div>
  )
}

/** 200 × 140 image tile; click downloads (the lightbox lives on the Messages page). */
function DockImage({ attachment }: { attachment: ChatAttachmentResponse }) {
  const url = useAttachmentUrl(attachment.id, true)
  const persisted = !attachment.id.startsWith('local-')
  return (
    <button
      type="button"
      title={`Download ${attachment.fileName}`}
      onClick={() => persisted && downloadAttachment(attachment.id, attachment.fileName)}
      className="block h-[140px] w-[200px] shrink-0 overflow-hidden rounded-xl border bg-muted"
    >
      {url && <img src={url} alt={attachment.fileName} className="h-full w-full object-cover" />}
    </button>
  )
}

/** Compact 220 px document card for the dock. */
function DockFileCard({ attachment }: { attachment: ChatAttachmentResponse }) {
  const { theme } = useTheme()
  const ext = extOf(attachment.fileName)
  return (
    <div className="flex w-[220px] max-w-full items-center gap-2 rounded-[10px] border bg-card py-[7px] pr-1.5 pl-2">
      <span
        className="flex h-[34px] w-7 shrink-0 items-center justify-center rounded-[5px] text-[9px] font-semibold tracking-[0.04em]"
        style={extColors(ext, theme === 'dark')}
      >
        {ext}
      </span>
      <div className="min-w-0 flex-1">
        <div className="truncate text-xs font-medium text-foreground">{attachment.fileName}</div>
        <div className="text-[11px] text-muted-foreground">{formatBytes(attachment.sizeBytes)}</div>
      </div>
      <button
        type="button"
        onClick={() => downloadAttachment(attachment.id, attachment.fileName)}
        title="Download"
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md text-muted-foreground hover:bg-muted"
      >
        <Download className="h-[15px] w-[15px]" />
      </button>
    </div>
  )
}

function IconBtn({
  title,
  onClick,
  children,
  size = 'md',
  className,
}: {
  title: string
  onClick: () => void
  children: ReactNode
  size?: 'md' | 'lg'
  className?: string
}) {
  return (
    <button
      type="button"
      title={title}
      onClick={e => {
        e.stopPropagation()
        onClick()
      }}
      className={cn(
        'flex shrink-0 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted',
        size === 'lg' ? 'h-[34px] w-[34px]' : 'h-8 w-8',
        className,
      )}
    >
      {children}
    </button>
  )
}
