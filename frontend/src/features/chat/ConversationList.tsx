import { useState } from 'react'
import { cn } from '@/lib/utils'
import { useAuth } from '@/features/auth/AuthContext'
import type { ConversationResponse } from '@/api/chat'
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
          <button
            key={c.id}
            onClick={() => onSelect(c.id)}
            className={cn(
              'flex items-center gap-3 p-2.5 rounded-lg text-left transition-colors',
              c.id === activeId ? 'bg-muted' : 'hover:bg-muted/60',
            )}
          >
            <ChatAvatar
              name={c.title}
              hue={conversationHue(c, user?.userId ?? '')}
              round={c.kind === 'Direct'}
            />
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
                  <span className="ml-auto text-[11px] text-muted-foreground/70 shrink-0">
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
          </button>
        ))}
      </div>

      <NewMessageDialog open={newOpen} onClose={() => setNewOpen(false)} onCreated={onSelect} />
    </div>
  )
}
