import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Check, MailOpen, MoreVertical, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAuth } from '@/features/auth/AuthContext'
import { Dialog } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { getApiErrorMessage } from '@/lib/api-errors'
import {
  deleteConversation,
  markConversationRead,
  markConversationUnread,
  type ConversationResponse,
} from '@/api/chat'
import { conversationHue, conversationPreview, formatWhen } from './chat-utils'
import { ChatAvatar } from './ChatAvatar'
import { NewMessageDialog } from './NewMessageDialog'

const FILTERS = [
  { id: 'all', label: 'All' },
  { id: 'direct', label: 'Direct' },
  { id: 'group', label: 'Groups' },
  { id: 'project', label: 'Projects' },
] as const

export type ConversationFilter = (typeof FILTERS)[number]['id']

interface ConversationListProps {
  conversations: ConversationResponse[]
  activeId: string | null
  filter: ConversationFilter
  onFilterChange: (filter: ConversationFilter) => void
  onSelect: (id: string) => void
}

export function ConversationList({ conversations, activeId, filter, onFilterChange, onSelect }: ConversationListProps) {
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [newOpen, setNewOpen] = useState(false)

  const visible = conversations.filter(
    c => !search || c.title.toLowerCase().includes(search.toLowerCase()),
  )

  return (
    <div className="w-[clamp(250px,28vw,316px)] shrink-0 bg-card border-r flex flex-col min-h-0">
      <div className="pt-[18px] px-4 pb-3 flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <h1 className="text-lg font-semibold tracking-tight">Messages</h1>
          <button
            onClick={() => setNewOpen(true)}
            className="h-8 px-3 rounded-lg bg-primary text-primary-foreground text-[13px] font-medium hover:bg-primary/90 transition-colors"
          >
            New message
          </button>
        </div>
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Search conversations"
          className="h-[34px] px-3 rounded-lg border bg-transparent text-[13px] outline-none focus:ring-2 focus:ring-ring placeholder:text-muted-foreground"
        />
        <div className="flex gap-1 p-[3px] bg-muted rounded-lg">
          {FILTERS.map(f => (
            <button
              key={f.id}
              onClick={() => onFilterChange(f.id)}
              className={cn(
                'flex-1 h-7 rounded-md text-xs font-medium transition-colors',
                filter === f.id ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground',
              )}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      <div className="flex-1 min-h-0 overflow-auto px-2 pb-2 flex flex-col gap-0.5">
        {visible.length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-8">No conversations</p>
        )}
        {visible.map(c => (
          <ConversationRow
            key={c.id}
            conversation={c}
            isActive={c.id === activeId}
            currentUserId={user?.userId ?? ''}
            onSelect={() => onSelect(c.id)}
          />
        ))}
      </div>

      <NewMessageDialog open={newOpen} onClose={() => setNewOpen(false)} onCreated={onSelect} />
    </div>
  )
}

function ConversationRow({
  conversation: c,
  isActive,
  currentUserId,
  onSelect,
}: {
  conversation: ConversationResponse
  isActive: boolean
  currentUserId: string
  onSelect: () => void
}) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  useEffect(() => {
    if (!menuOpen) return
    const onMouseDown = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setMenuOpen(false)
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setMenuOpen(false)
    }
    document.addEventListener('mousedown', onMouseDown)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onMouseDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [menuOpen])

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
    queryClient.invalidateQueries({ queryKey: ['chat', 'unread'] })
  }

  async function run(action: () => Promise<unknown>) {
    setMenuOpen(false)
    try {
      await action()
      invalidate()
    } catch (err) {
      toast.error(getApiErrorMessage(err))
    }
  }

  const handleDelete = () =>
    run(async () => {
      await deleteConversation(c.id)
      setConfirmDelete(false)
      if (isActive) navigate('/chat')
    })

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onSelect}
      onKeyDown={e => {
        if (e.key === 'Enter' || e.key === ' ') onSelect()
      }}
      className={cn(
        'group relative flex items-center gap-3 p-2.5 rounded-lg text-left cursor-pointer transition-colors',
        isActive ? 'bg-muted' : 'hover:bg-muted/60',
      )}
    >
      <ChatAvatar name={c.title} hue={conversationHue(c, currentUserId)} round={c.kind === 'Direct'} />
      <div className="flex-1 min-w-0 flex flex-col gap-0.5">
        <div className="flex items-center gap-1.5">
          <span className={cn('text-sm truncate', c.unreadCount > 0 ? 'font-semibold' : 'font-medium')}>
            {c.title}
          </span>
          {c.kind === 'Project' && (
            <span className="text-[10px] font-medium px-1.5 py-px rounded border bg-muted text-muted-foreground shrink-0">
              Project
            </span>
          )}
          {c.lastMessageAt && (
            <span className="ml-auto text-[11px] text-muted-foreground/70 shrink-0 group-hover:opacity-0 transition-opacity">
              {formatWhen(c.lastMessageAt)}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          <span
            className={cn(
              'flex-1 text-[13px] truncate',
              c.unreadCount > 0 ? 'text-foreground' : 'text-muted-foreground',
            )}
          >
            {conversationPreview(c)}
          </span>
          {c.unreadCount > 0 && (
            <span className="min-w-[18px] h-[18px] px-[5px] rounded-full bg-primary text-primary-foreground text-[10px] font-semibold flex items-center justify-center shrink-0">
              {c.unreadCount}
            </span>
          )}
        </div>
      </div>

      <div ref={menuRef} className="absolute right-1.5 top-1.5" onClick={e => e.stopPropagation()}>
        <button
          onClick={() => setMenuOpen(o => !o)}
          title="Conversation options"
          className={cn(
            'w-7 h-7 rounded-md flex items-center justify-center text-muted-foreground transition-opacity',
            'hover:bg-border focus:opacity-100',
            menuOpen ? 'opacity-100 bg-border' : 'opacity-0 group-hover:opacity-100',
          )}
        >
          <MoreVertical className="h-4 w-4" />
        </button>

        {menuOpen && (
          <div className="absolute right-0 top-8 w-44 z-50 bg-card border rounded-lg shadow-lg overflow-hidden py-1">
            {c.unreadCount > 0 ? (
              <MenuItem
                icon={<Check className="h-3.5 w-3.5" />}
                label="Mark as read"
                onClick={() => run(() => markConversationRead(c.id, null))}
              />
            ) : (
              <MenuItem
                icon={<MailOpen className="h-3.5 w-3.5" />}
                label="Mark as unread"
                onClick={() => run(() => markConversationUnread(c.id))}
              />
            )}
            {c.kind !== 'Project' && (
              <MenuItem
                icon={<Trash2 className="h-3.5 w-3.5" />}
                label="Delete chat"
                destructive
                onClick={() => {
                  setMenuOpen(false)
                  setConfirmDelete(true)
                }}
              />
            )}
          </div>
        )}
      </div>

      <Dialog open={confirmDelete} onClose={() => setConfirmDelete(false)} title="Delete chat">
        <div className="space-y-4" onClick={e => e.stopPropagation()}>
          <p className="text-sm text-muted-foreground">
            Delete the conversation with <span className="font-medium text-foreground">{c.title}</span>? All
            messages and shared files are removed for every participant. This cannot be undone.
          </p>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setConfirmDelete(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete}>
              Delete chat
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

function MenuItem({
  icon,
  label,
  destructive,
  onClick,
}: {
  icon: React.ReactNode
  label: string
  destructive?: boolean
  onClick: () => void
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'w-full flex items-center gap-2.5 px-3 py-2 text-[13px] text-left transition-colors hover:bg-muted',
        destructive ? 'text-red-600 dark:text-red-400' : 'text-foreground',
      )}
    >
      {icon}
      {label}
    </button>
  )
}
