import { useState } from 'react'
import { Moon, Search, SquareArrowOutUpRight, Sun } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useTheme } from '@/context/ThemeContext'
import type { PresenceResponse } from '@/api/chat'
import { hueFromId } from '../chat-utils'
import { ChatAvatar } from '../ChatAvatar'
import { PRESENCE_SECTIONS, relativeAgo } from './dock-utils'

export interface DockSummary {
  unread: number
  preview: string
}

interface Props {
  people: PresenceResponse[]
  summaries: Record<string, DockSummary>
  openIds: string[]
  showOffline?: boolean
  onOpenUser: (id: string) => void
  onOpenMessagesPage: () => void
}

/** 340 × 560 floating panel above the launcher: colleagues grouped by presence. */
export function PeoplePanel({ people, summaries, openIds, showOffline = true, onOpenUser, onOpenMessagesPage }: Props) {
  const { theme, toggleTheme } = useTheme()
  const [query, setQuery] = useState('')
  const q = query.trim().toLowerCase()
  const match = (u: PresenceResponse) => !q || u.name.toLowerCase().includes(q) || u.role.toLowerCase().includes(q)
  const sections = PRESENCE_SECTIONS.filter(([p]) => showOffline || p !== 'offline')
    .map(([p, label]) => ({ p, label, users: people.filter(u => u.presence === p && match(u)) }))
    .filter(s => s.users.length)
  const onlineCount = people.filter(u => u.presence === 'online').length

  return (
    <div className="fixed right-6 bottom-24 z-40 flex h-[min(560px,calc(100vh-128px))] w-[340px] max-w-[calc(100vw-48px)] flex-col overflow-hidden rounded-2xl border bg-card shadow-panel">
      <div className="flex items-center gap-2 pt-4 pr-3 pb-2.5 pl-4">
        <div className="min-w-0 flex-1">
          <div className="text-base font-semibold tracking-tight">Chat</div>
          <div className="text-xs text-muted-foreground">
            {onlineCount === 1 ? '1 colleague active now' : `${onlineCount} colleagues active now`}
          </div>
        </div>
        <button
          onClick={toggleTheme}
          title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          className="flex w-8 h-8 shrink-0 items-center justify-center rounded-lg border text-muted-foreground hover:bg-muted"
        >
          {theme === 'dark' ? <Sun className="h-[15px] w-[15px]" /> : <Moon className="h-[15px] w-[15px]" />}
        </button>
        <button
          onClick={onOpenMessagesPage}
          title="Open Messages page"
          className="flex w-8 h-8 shrink-0 items-center justify-center rounded-lg border text-muted-foreground hover:bg-muted"
        >
          <SquareArrowOutUpRight className="h-[15px] w-[15px]" strokeWidth={1.8} />
        </button>
      </div>
      <div className="px-3 pb-2">
        <label className="flex h-9 items-center gap-2 rounded-full bg-muted px-3 text-muted-foreground/70">
          <Search className="h-3.5 w-3.5" />
          <input
            value={query}
            onChange={e => setQuery(e.target.value)}
            placeholder="Search people"
            className="min-w-0 flex-1 border-0 bg-transparent text-[13px] text-foreground outline-none placeholder:text-muted-foreground/70"
          />
        </label>
      </div>
      <div className="min-h-0 flex-1 overflow-auto px-1.5 pb-2">
        {sections.map(s => (
          <div key={s.p}>
            <div className="px-2.5 pt-3 pb-1.5 text-[11px] font-semibold tracking-[0.06em] text-muted-foreground/70 uppercase">
              {s.label} · {s.users.length}
            </div>
            {s.users.map(u => {
              const sum = summaries[u.userId] ?? { unread: 0, preview: '' }
              const unread = sum.unread > 0
              return (
                <button
                  key={u.userId}
                  onClick={() => onOpenUser(u.userId)}
                  className={cn(
                    'flex w-full items-center gap-3 rounded-[10px] px-2.5 py-2 text-left text-foreground hover:bg-muted',
                    openIds.includes(u.userId) && 'bg-muted',
                  )}
                >
                  <ChatAvatar name={u.name} hue={hueFromId(u.userId)} size={40} fontSize={13} presence={u.presence} dimmed={u.presence === 'offline'} />
                  <div className="min-w-0 flex-1">
                    <div className={cn('truncate text-sm', unread ? 'font-semibold' : 'font-medium')}>{u.name}</div>
                    <div className={cn('truncate text-xs', unread ? 'font-medium text-foreground' : 'text-muted-foreground')}>
                      {unread ? sum.preview : u.role}
                    </div>
                  </div>
                  {unread && (
                    <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-primary px-1.5 text-[11px] font-semibold text-primary-foreground">
                      {sum.unread}
                    </span>
                  )}
                  {!unread && u.presence === 'offline' && u.lastSeenAt && (
                    <span className="shrink-0 text-[11px] text-muted-foreground/70">{relativeAgo(u.lastSeenAt)}</span>
                  )}
                </button>
              )
            })}
          </div>
        ))}
        {!sections.length && <div className="px-4 py-8 text-center text-[13px] text-muted-foreground">No one matches “{query}”.</div>}
      </div>
    </div>
  )
}
