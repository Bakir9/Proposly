import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Moon, Sun } from 'lucide-react'
import { useTheme } from '@/context/ThemeContext'
import { useAuth } from '@/features/auth/AuthContext'
import { cn } from '@/lib/utils'
import {
  getMessages,
  markConversationRead,
  sendMessage,
  type ChatAttachmentResponse,
  type ChatMessageResponse,
  type ConversationResponse,
} from '@/api/chat'
import { conversationHue, dayLabel, formatTime, hueFromId } from './chat-utils'
import { ChatAvatar } from './ChatAvatar'
import { AttachmentGrid } from './AttachmentGrid'
import { Composer } from './Composer'
import { Lightbox, type LightboxData } from './Lightbox'
import { usePendingAttachments } from './usePendingAttachments'

interface OutgoingMessage {
  localId: string
  body: string | null
  attachments: ChatAttachmentResponse[]
  createdAt: string
  status: 'sending' | 'sent' | 'failed'
  serverId: string | null
}

interface ThreadProps {
  conversation: ConversationResponse
  /** The Messages page shows the thread header; the project Discussion tab brings its own. */
  showHeader?: boolean
}

export function Thread({ conversation, showHeader = true }: ThreadProps) {
  const { user } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const scrollRef = useRef<HTMLDivElement>(null)
  const [atBottom, setAtBottom] = useState(true)
  const [lastSeenId, setLastSeenId] = useState<string | null>(null)
  const [dragging, setDragging] = useState(false)
  const [lightbox, setLightbox] = useState<LightboxData | null>(null)
  const [draft, setDraft] = useState('')
  const [outgoing, setOutgoing] = useState<OutgoingMessage[]>([])

  const { pending, addFiles, remove, clear, uploading, readyIds } = usePendingAttachments(conversation.id)

  const { data, fetchNextPage, hasNextPage, isFetchingNextPage } = useInfiniteQuery({
    queryKey: ['chat', 'messages', conversation.id],
    queryFn: ({ pageParam }) => getMessages(conversation.id, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: page => (page.hasMore ? page.messages[0]?.id : undefined),
    refetchInterval: 10_000,
  })

  const messages = useMemo(() => {
    const pages = data?.pages ?? []
    return [...pages].reverse().flatMap(p => p.messages)
  }, [data])

  // Optimistic messages disappear once the server copy is in the list.
  const serverIds = useMemo(() => new Set(messages.map(m => m.id)), [messages])
  const visibleOutgoing = outgoing.filter(o => o.serverId === null || !serverIds.has(o.serverId))

  const lastMessageId = messages.length > 0 ? messages[messages.length - 1].id : null

  const markRead = useCallback(() => {
    markConversationRead(conversation.id, lastMessageId)
      .then(() => {
        queryClient.invalidateQueries({ queryKey: ['chat', 'unread'] })
        queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
      })
      .catch(() => {})
  }, [conversation.id, lastMessageId, queryClient])

  // While the user sits at the bottom, every incoming message counts as seen
  // (render-adjust pattern — no effect needed). Away from the bottom, the
  // difference between lastMessageId and lastSeenId drives the "New messages" pill.
  if (atBottom && lastMessageId !== null && lastSeenId !== lastMessageId) {
    setLastSeenId(lastMessageId)
  }
  const showNewPill = !atBottom && lastMessageId !== null && lastSeenId !== lastMessageId

  // Seeing a message scrolls it into view and marks the conversation read on the server.
  useEffect(() => {
    if (lastSeenId === null) return
    const el = scrollRef.current
    if (el) el.scrollTop = el.scrollHeight
    markRead()
  }, [lastSeenId, markRead])

  const jumpToBottom = useCallback(() => {
    const el = scrollRef.current
    if (el) el.scrollTop = el.scrollHeight
    setAtBottom(true)
  }, [])

  const sendMutation = useMutation({
    mutationFn: (payload: { body: string | null; attachmentIds: string[]; localId: string }) =>
      sendMessage(conversation.id, { body: payload.body, attachmentIds: payload.attachmentIds }),
    onSuccess: (response, payload) => {
      setOutgoing(prev =>
        prev.map(o => (o.localId === payload.localId ? { ...o, status: 'sent', serverId: response.id } : o)),
      )
      queryClient.invalidateQueries({ queryKey: ['chat', 'messages', conversation.id] })
      queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
    },
    onError: (_err, payload) => {
      setOutgoing(prev => prev.map(o => (o.localId === payload.localId ? { ...o, status: 'failed' } : o)))
      toast.error('Message could not be sent.', {
        action: { label: 'Retry', onClick: () => retry(payload.localId, payload.body, payload.attachmentIds) },
      })
    },
  })

  function retry(localId: string, body: string | null, attachmentIds: string[]) {
    setOutgoing(prev => prev.map(o => (o.localId === localId ? { ...o, status: 'sending' } : o)))
    sendMutation.mutate({ body, attachmentIds, localId })
  }

  function handleSend() {
    const body = draft.trim()
    if (uploading) return
    if (!body && readyIds.length === 0) return

    const localId = `local-${Date.now()}`
    const optimisticAttachments = pending
      .filter(p => p.attachmentId !== null)
      .map(p => ({ id: p.attachmentId!, fileName: p.file.name, contentType: p.file.type, sizeBytes: p.file.size }))

    setOutgoing(prev => [
      ...prev,
      {
        localId,
        body: body || null,
        attachments: optimisticAttachments,
        createdAt: new Date().toISOString(),
        status: 'sending',
        serverId: null,
      },
    ])
    setDraft('')
    clear()
    sendMutation.mutate({ body: body || null, attachmentIds: readyIds, localId })
    requestAnimationFrame(jumpToBottom)
  }

  const canSend = !uploading && (draft.trim().length > 0 || readyIds.length > 0)
  const hue = conversationHue(conversation, user?.userId ?? '')

  return (
    <div
      className="relative flex-1 min-w-0 flex flex-col bg-card"
      onDragOver={e => {
        e.preventDefault()
        if (!dragging) setDragging(true)
      }}
      onDragLeave={e => {
        if (e.currentTarget.contains(e.relatedTarget as Node)) return
        setDragging(false)
      }}
      onDrop={e => {
        e.preventDefault()
        setDragging(false)
        if (e.dataTransfer.files?.length) addFiles(e.dataTransfer.files)
      }}
    >
      {dragging && (
        <div className="absolute inset-3 z-20 border-2 border-dashed border-muted-foreground rounded-[14px] bg-background/95 flex flex-col items-center justify-center gap-1.5 pointer-events-none">
          <div className="text-[15px] font-semibold">Drop files to attach</div>
          <div className="text-[13px] text-muted-foreground">Images, PDFs, Office documents · up to 25 MB each</div>
        </div>
      )}

      {showHeader && (
        <div className="h-16 shrink-0 border-b flex items-center gap-3 px-5">
          <ChatAvatar name={conversation.title} hue={hue} round={conversation.kind === 'Direct'} size={36} />
          <div className="flex-1 min-w-0">
            <div className="text-[15px] font-semibold truncate">{conversation.title}</div>
            <div className="text-xs text-muted-foreground truncate">{conversation.subtitle}</div>
          </div>
          <div className="shrink-0 flex items-center gap-3.5">
            <span className="text-xs text-muted-foreground/70 whitespace-nowrap">Auto-refresh · 10 s</span>
            <button
              onClick={toggleTheme}
              title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
              className="w-8 h-8 shrink-0 border rounded-lg flex items-center justify-center text-muted-foreground hover:bg-muted transition-colors"
            >
              {theme === 'dark' ? <Sun className="h-[15px] w-[15px]" /> : <Moon className="h-[15px] w-[15px]" />}
            </button>
            {conversation.kind === 'Project' && conversation.projectId && (
              <button
                onClick={() => navigate(`/projects/${conversation.projectId}`)}
                className="whitespace-nowrap h-8 px-3 rounded-lg border bg-card text-[13px] font-medium hover:bg-muted transition-colors"
              >
                Open project
              </button>
            )}
          </div>
        </div>
      )}

      <div
        ref={scrollRef}
        onScroll={e => {
          const el = e.currentTarget
          const nearBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 60
          setAtBottom(nearBottom)
        }}
        className="flex-1 min-h-0 overflow-auto px-6 pt-3 pb-5"
      >
        <div className="max-w-[820px] mx-auto flex flex-col">
          {hasNextPage && (
            <button
              onClick={() => fetchNextPage()}
              disabled={isFetchingNextPage}
              className="self-center mt-2 text-xs text-muted-foreground hover:text-foreground"
            >
              {isFetchingNextPage ? 'Loading…' : 'Load earlier messages'}
            </button>
          )}
          <MessageFlow
            messages={messages}
            outgoing={visibleOutgoing}
            currentUserId={user?.userId ?? ''}
            onOpenLightbox={setLightbox}
          />
        </div>
      </div>

      {showNewPill && (
        <button
          onClick={jumpToBottom}
          className="absolute bottom-28 left-1/2 -translate-x-1/2 z-10 h-7 px-3 rounded-full bg-primary text-primary-foreground text-xs font-medium shadow-md"
        >
          New messages
        </button>
      )}

      <Composer
        conversationTitle={conversation.title}
        pending={pending}
        uploading={uploading}
        canSend={canSend}
        draft={draft}
        onDraftChange={setDraft}
        onAddFiles={addFiles}
        onRemovePending={remove}
        onSend={handleSend}
      />

      {lightbox && <Lightbox data={lightbox} onClose={() => setLightbox(null)} />}
    </div>
  )
}

interface FlowItem {
  message: ChatMessageResponse
  isOwn: boolean
  isFirstOfGroup: boolean
  status?: string
}

function MessageFlow({
  messages,
  outgoing,
  currentUserId,
  onOpenLightbox,
}: {
  messages: ChatMessageResponse[]
  outgoing: OutgoingMessage[]
  currentUserId: string
  onOpenLightbox: (data: LightboxData) => void
}) {
  const all: (FlowItem | { divider: string; key: string })[] = []
  let prevDay: string | null = null
  let prevAuthor: string | null = null

  const combined: (ChatMessageResponse & { status?: string })[] = [
    ...messages,
    ...outgoing.map(o => ({
      id: o.localId,
      conversationId: '',
      authorUserId: currentUserId,
      authorName: 'You',
      body: o.body,
      createdAt: o.createdAt,
      attachments: o.attachments,
      status: o.status === 'sending' ? 'Sending…' : o.status === 'failed' ? 'Failed' : 'Sent',
    })),
  ]

  combined.forEach(m => {
    const day = dayLabel(m.createdAt)
    if (day !== prevDay) {
      all.push({ divider: day, key: `day-${m.id}` })
      prevDay = day
      prevAuthor = null
    }
    all.push({
      message: m,
      isOwn: m.authorUserId === currentUserId,
      isFirstOfGroup: m.authorUserId !== prevAuthor,
      status: m.status,
    })
    prevAuthor = m.authorUserId
  })

  // Only the last own message shows its status.
  const lastOwnIndex = all.findLastIndex(i => 'message' in i && i.isOwn)

  if (combined.length === 0)
    return <p className="text-sm text-muted-foreground text-center py-10">No messages yet — say hello!</p>

  return (
    <>
      {all.map((item, index) => {
        if ('divider' in item)
          return (
            <div key={item.key} className="flex items-center gap-3 mt-[18px] mb-1.5">
              <div className="flex-1 h-px bg-muted" />
              <span className="text-[11px] font-medium text-muted-foreground/70 uppercase tracking-wider">
                {item.divider}
              </span>
              <div className="flex-1 h-px bg-muted" />
            </div>
          )
        return (
          <MessageRow
            key={item.message.id}
            item={item}
            showStatus={index === lastOwnIndex}
            onOpenLightbox={onOpenLightbox}
          />
        )
      })}
    </>
  )
}

function MessageRow({
  item,
  showStatus,
  onOpenLightbox,
}: {
  item: FlowItem
  showStatus: boolean
  onOpenLightbox: (data: LightboxData) => void
}) {
  const { message: m, isOwn, isFirstOfGroup } = item
  const metaPrefix = `${m.authorName} · ${dayLabel(m.createdAt)} ${formatTime(m.createdAt)}`

  return (
    <div className={cn('flex items-start gap-2.5', isOwn && 'flex-row-reverse', isFirstOfGroup ? 'mt-4' : 'mt-1')}>
      {!isOwn && (
        <div className={cn('w-8 shrink-0', !isFirstOfGroup && 'invisible')}>
          <ChatAvatar name={m.authorName} hue={hueFromId(m.authorUserId)} size={32} fontSize={11} />
        </div>
      )}
      <div className={cn('max-w-[72%] flex flex-col gap-1 min-w-0', isOwn ? 'items-end' : 'items-start')}>
        {isFirstOfGroup && (
          <div className="flex gap-2 items-baseline">
            <span className="text-[13px] font-medium">{isOwn ? 'You' : m.authorName}</span>
            <span className="text-[11px] text-muted-foreground/70">{formatTime(m.createdAt)}</span>
          </div>
        )}
        {m.body && (
          <div
            className={cn(
              'py-[9px] px-[13px] rounded-[14px] text-sm leading-relaxed whitespace-pre-wrap [overflow-wrap:anywhere]',
              isOwn ? 'bg-primary text-primary-foreground' : 'bg-muted text-foreground',
            )}
          >
            {m.body}
          </div>
        )}
        {m.attachments.length > 0 && (
          <AttachmentGrid
            attachments={m.attachments}
            metaPrefix={metaPrefix}
            alignEnd={isOwn}
            onOpenLightbox={onOpenLightbox}
          />
        )}
        {showStatus && item.status && (
          <div className="text-[11px] text-muted-foreground/70">{item.status}</div>
        )}
      </div>
    </div>
  )
}
