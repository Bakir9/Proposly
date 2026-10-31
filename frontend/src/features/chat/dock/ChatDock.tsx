import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useInfiniteQuery, useQuery, useQueryClient } from '@tanstack/react-query'
import { MessageCircle, X } from 'lucide-react'
import { toast } from 'sonner'
import { useAuth } from '@/features/auth/AuthContext'
import {
  createConversation,
  getConversations,
  getMessages,
  getPresence,
  markConversationRead,
  sendMessage,
  uploadAttachment,
  type ChatAttachmentResponse,
  type ConversationResponse,
  type PresenceResponse,
} from '@/api/chat'
import { MAX_FILE_SIZE, conversationPreview, hueFromId } from '../chat-utils'
import { ChatAvatar } from '../ChatAvatar'
import { useDockState } from './useDockState'
import { PeoplePanel, type DockSummary } from './PeoplePanel'
import { DockWindow, type DockMessage } from './DockWindow'

/**
 * Floating chat on every page: launcher (bottom-right), people panel with presence,
 * up to 3 docked DM windows, minimized windows as chat heads. Mounted once in AppShell.
 */
export function ChatDock() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const dock = useDockState()
  const [focusId, setFocusId] = useState<string | null>(null)

  const meId = user?.userId ?? ''
  const enabled = !!user && user.role !== 'SuperAdmin'

  const { data: people = [] } = useQuery({
    queryKey: ['chat', 'presence'],
    queryFn: getPresence,
    refetchInterval: 30_000,
    enabled,
  })
  const { data: directs = [] } = useQuery({
    queryKey: ['chat', 'conversations', 'direct', meId],
    queryFn: () => getConversations('direct'),
    refetchInterval: 15_000,
    enabled,
  })

  const byId = useMemo(() => Object.fromEntries(people.map(u => [u.userId, u])), [people])
  const directByUser = useMemo(() => {
    const out: Record<string, ConversationResponse> = {}
    for (const c of directs) {
      const other = c.participants.find(p => p.userId !== meId)
      if (other) out[other.userId] = c
    }
    return out
  }, [directs, meId])
  const summaries = useMemo(() => {
    const out: Record<string, DockSummary> = {}
    for (const [userId, c] of Object.entries(directByUser)) out[userId] = { unread: c.unreadCount, preview: conversationPreview(c) }
    return out
  }, [directByUser])

  if (!enabled) return null

  const open = (id: string) => {
    dock.openUser(id)
    setFocusId(id)
  }
  const totalUnread = Object.entries(summaries).reduce(
    (sum, [id, s]) => sum + (dock.expandedIds.includes(id) ? 0 : s.unread),
    0,
  )

  return (
    <>
      {/* Docked windows: newest on the right, next to the panel/launcher. */}
      <div className="pointer-events-none fixed bottom-0 z-30 flex flex-row-reverse items-end gap-3" style={{ right: dock.rightOffset }}>
        {dock.expandedIds.map(id =>
          byId[id] ? (
            <ConnectedDockWindow
              key={id}
              user={byId[id]}
              meId={meId}
              meName={user!.fullName}
              conversation={directByUser[id]}
              onMinimize={() => dock.minimize(id)}
              onClose={() => dock.close(id)}
              autoFocus={focusId === id}
            />
          ) : null,
        )}
      </div>

      {/* Chat heads for minimized / overflow windows (panel closed only). */}
      {!dock.panelOpen && dock.headIds.length > 0 && (
        <div className="fixed right-7 bottom-24 z-[35] flex flex-col-reverse gap-2.5">
          {dock.headIds.map(id => {
            const u = byId[id]
            if (!u) return null
            const unread = summaries[id]?.unread ?? 0
            return (
              <button key={id} onClick={() => open(id)} title={u.name} className="relative rounded-full shadow-head">
                <ChatAvatar name={u.name} hue={hueFromId(u.userId)} size={48} fontSize={14} presence={u.presence} />
                {unread > 0 && (
                  <span className="absolute -top-[3px] -right-[3px] flex h-[19px] min-w-[19px] items-center justify-center rounded-full border-2 border-background bg-red-500 px-[5px] text-[10px] font-semibold text-white">
                    {unread}
                  </span>
                )}
              </button>
            )
          })}
        </div>
      )}

      {dock.panelOpen && (
        <PeoplePanel
          people={people}
          summaries={summaries}
          openIds={dock.expandedIds}
          onOpenUser={open}
          onOpenMessagesPage={() => navigate('/chat')}
        />
      )}

      <button
        onClick={dock.togglePanel}
        title="Chat"
        aria-expanded={dock.panelOpen}
        className="fixed right-6 bottom-6 z-[45] flex h-14 w-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-launcher transition-transform hover:scale-105"
      >
        {dock.panelOpen ? <X className="h-[22px] w-[22px]" strokeWidth={2} /> : <MessageCircle className="h-6 w-6" strokeWidth={1.8} />}
        {!dock.panelOpen && totalUnread > 0 && (
          <span className="absolute -top-0.5 -right-0.5 flex h-[22px] min-w-[22px] items-center justify-center rounded-full border-2 border-background bg-red-500 px-1.5 text-[11px] font-semibold text-white">
            {totalUnread}
          </span>
        )}
      </button>
    </>
  )
}

interface OutgoingDockMessage {
  localId: string
  body: string | null
  attachments: ChatAttachmentResponse[]
  createdAt: string
  status: 'sending' | 'sent' | 'failed'
  serverId: string | null
}

function ConnectedDockWindow({
  user,
  meId,
  meName,
  conversation,
  onMinimize,
  onClose,
  autoFocus,
}: {
  user: PresenceResponse
  meId: string
  meName: string
  conversation?: ConversationResponse
  onMinimize: () => void
  onClose: () => void
  autoFocus: boolean
}) {
  const queryClient = useQueryClient()
  const conversationId = conversation?.id
  const [outgoing, setOutgoing] = useState<OutgoingDockMessage[]>([])
  const [uploadingCount, setUploadingCount] = useState(0)

  // Same query key and shape as the Messages page thread, so both share the cache.
  const { data } = useInfiniteQuery({
    queryKey: ['chat', 'messages', conversationId],
    queryFn: ({ pageParam }) => getMessages(conversationId!, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: page => (page.hasMore ? page.messages[0]?.id : undefined),
    refetchInterval: 10_000,
    enabled: !!conversationId,
  })
  const messages = useMemo(() => {
    const pages = data?.pages ?? []
    return [...pages].reverse().flatMap(p => p.messages)
  }, [data])

  // Optimistic copies disappear once the server message is in the list.
  const serverIds = useMemo(() => new Set(messages.map(m => m.id)), [messages])
  const combined: DockMessage[] = useMemo(
    () => [
      ...messages,
      ...outgoing
        .filter(o => o.serverId === null || !serverIds.has(o.serverId))
        .map(o => ({
          id: o.localId,
          conversationId: conversationId ?? '',
          authorUserId: meId,
          authorName: meName,
          body: o.body,
          createdAt: o.createdAt,
          attachments: o.attachments,
          status: o.status,
        })),
    ],
    [messages, outgoing, serverIds, conversationId, meId, meName],
  )

  // Opening the window (or new messages arriving while it is open) marks the DM read.
  const lastMessageId = messages.length > 0 ? messages[messages.length - 1].id : null
  const unreadCount = conversation?.unreadCount ?? 0
  useEffect(() => {
    if (!conversationId || !lastMessageId || unreadCount === 0) return
    markConversationRead(conversationId, lastMessageId)
      .then(() => {
        queryClient.invalidateQueries({ queryKey: ['chat', 'unread'] })
        queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
      })
      .catch(() => {})
  }, [conversationId, lastMessageId, unreadCount, queryClient])

  // A DM conversation is created lazily on the first message.
  const ensureConversation = async () => {
    if (conversationId) return conversationId
    const id = await createConversation({ kind: 'Direct', participantUserIds: [user.userId] })
    await queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
    return id
  }

  const sendBody = async (body: string, files: File[] = []) => {
    const localId = `local-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`
    try {
      const id = await ensureConversation()
      let attachments: ChatAttachmentResponse[] = []
      if (files.length) {
        setUploadingCount(n => n + files.length)
        attachments = await Promise.all(
          files.map(f => uploadAttachment(id, f).finally(() => setUploadingCount(n => Math.max(0, n - 1)))),
        )
      }
      if (!body && attachments.length === 0) return
      setOutgoing(prev => [
        ...prev,
        { localId, body: body || null, attachments, createdAt: new Date().toISOString(), status: 'sending', serverId: null },
      ])
      const response = await sendMessage(id, { body: body || null, attachmentIds: attachments.map(a => a.id) })
      setOutgoing(prev => prev.map(o => (o.localId === localId ? { ...o, status: 'sent', serverId: response.id } : o)))
      queryClient.invalidateQueries({ queryKey: ['chat', 'messages', id] })
      queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
    } catch {
      setOutgoing(prev => prev.map(o => (o.localId === localId ? { ...o, status: 'failed' } : o)))
      toast.error(`Message to ${user.name} not sent`)
    }
  }

  return (
    <DockWindow
      user={user}
      messages={combined}
      meId={meId}
      notice={uploadingCount > 0 ? `Uploading ${uploadingCount} file${uploadingCount > 1 ? 's' : ''}…` : undefined}
      onSend={body => void sendBody(body)}
      onAttachFiles={files => {
        const tooBig = files.find(f => f.size > MAX_FILE_SIZE)
        if (tooBig) toast.error(`${tooBig.name} is too large · max 25 MB`)
        const ok = files.filter(f => f.size <= MAX_FILE_SIZE)
        if (ok.length) void sendBody('', ok)
      }}
      onMinimize={onMinimize}
      onClose={onClose}
      autoFocus={autoFocus}
    />
  )
}
