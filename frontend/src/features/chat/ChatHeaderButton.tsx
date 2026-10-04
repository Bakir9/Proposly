import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { MessageCircle } from 'lucide-react'
import { cn } from '@/lib/utils'
import { getConversations, markConversationRead } from '@/api/chat'
import { useAuth } from '@/features/auth/AuthContext'
import { conversationHue, conversationPreview, formatWhen } from './chat-utils'
import { ChatAvatar } from './ChatAvatar'
import { useChatUnread } from './useChatUnread'

/** Header chat icon with unread badge and the recent-conversations popover. */
export function ChatHeaderButton() {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const unread = useChatUnread()

  const { data: conversations = [] } = useQuery({
    queryKey: ['chat', 'conversations', 'all', user?.userId],
    queryFn: () => getConversations(),
    enabled: open,
    staleTime: 10_000,
  })

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    const escHandler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    document.addEventListener('keydown', escHandler)
    return () => {
      document.removeEventListener('mousedown', handler)
      document.removeEventListener('keydown', escHandler)
    }
  }, [])

  const recent = conversations.slice(0, 5)

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['chat', 'unread'] })
    queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
  }

  async function markAllRead() {
    await Promise.all(
      conversations.filter(c => c.unreadCount > 0).map(c => markConversationRead(c.id, null)),
    ).catch(() => {})
    invalidate()
  }

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(o => !o)}
        title="Messages"
        className={cn(
          'relative flex items-center justify-center w-8 h-8 rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors',
          open && 'bg-accent text-accent-foreground',
        )}
      >
        <MessageCircle className="h-4 w-4" />
        {unread > 0 && (
          <span className="absolute -top-0.5 -right-0.5 h-4 min-w-4 px-0.5 rounded-full bg-red-500 text-white text-[10px] font-bold flex items-center justify-center leading-none ring-2 ring-card">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-10 w-[360px] bg-card border rounded-xl shadow-lg z-50 overflow-hidden">
          <div className="flex items-center justify-between px-4 pt-3.5 pb-2.5">
            <span className="text-sm font-semibold">Messages</span>
            {unread > 0 && (
              <button onClick={markAllRead} className="text-xs text-muted-foreground hover:text-foreground">
                Mark all as read
              </button>
            )}
          </div>
          <div className="flex flex-col px-1.5 pb-1.5">
            {recent.length === 0 && (
              <p className="text-sm text-muted-foreground text-center py-6">No conversations yet</p>
            )}
            {recent.map(c => (
              <button
                key={c.id}
                onClick={() => {
                  setOpen(false)
                  navigate(`/chat/${c.id}`)
                }}
                className="flex items-center gap-2.5 px-2.5 py-2 rounded-lg text-left hover:bg-muted transition-colors"
              >
                <ChatAvatar
                  name={c.title}
                  hue={conversationHue(c, user?.userId ?? '')}
                  round={c.kind === 'Direct'}
                  size={32}
                  fontSize={11}
                />
                <div className="flex-1 min-w-0">
                  <div className="flex justify-between gap-2">
                    <span className={cn('text-[13px] truncate', c.unreadCount > 0 ? 'font-semibold' : 'font-medium')}>
                      {c.title}
                    </span>
                    {c.lastMessageAt && (
                      <span className="text-[11px] text-muted-foreground/70 shrink-0">
                        {formatWhen(c.lastMessageAt)}
                      </span>
                    )}
                  </div>
                  <div
                    className={cn(
                      'text-xs truncate',
                      c.unreadCount > 0 ? 'text-foreground' : 'text-muted-foreground',
                    )}
                  >
                    {conversationPreview(c)}
                  </div>
                </div>
                {c.unreadCount > 0 && <span className="w-2 h-2 rounded-full bg-primary shrink-0" />}
              </button>
            ))}
          </div>
          <button
            onClick={() => {
              setOpen(false)
              navigate('/chat')
            }}
            className="w-full border-t bg-background py-[11px] text-[13px] font-medium hover:bg-muted transition-colors"
          >
            Open Messages
          </button>
        </div>
      )}
    </div>
  )
}
