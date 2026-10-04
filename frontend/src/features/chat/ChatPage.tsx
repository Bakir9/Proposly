import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { MessageCircle } from 'lucide-react'
import { getConversations } from '@/api/chat'
import { useAuth } from '@/features/auth/AuthContext'
import { ConversationList, type ConversationFilter } from './ConversationList'
import { Thread } from './Thread'

export function ChatPage() {
  const { conversationId } = useParams<{ conversationId: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const [filter, setFilter] = useState<ConversationFilter>('all')

  const { data: conversations = [], isLoading } = useQuery({
    queryKey: ['chat', 'conversations', filter, user?.userId],
    queryFn: () => getConversations(filter === 'all' ? undefined : filter),
    refetchInterval: 10_000,
    staleTime: 0,
  })

  const active = conversations.find(c => c.id === conversationId) ?? null

  // With no conversation selected, open the most recent one.
  useEffect(() => {
    if (!conversationId && conversations.length > 0)
      navigate(`/chat/${conversations[0].id}`, { replace: true })
  }, [conversationId, conversations, navigate])

  return (
    <div className="flex h-full min-h-0">
      <ConversationList
        conversations={conversations}
        activeId={conversationId ?? null}
        filter={filter}
        onFilterChange={setFilter}
        onSelect={id => navigate(`/chat/${id}`)}
      />
      {active ? (
        <Thread key={active.id} conversation={active} />
      ) : (
        <div className="flex-1 bg-card flex flex-col items-center justify-center gap-3 text-muted-foreground">
          <MessageCircle className="h-10 w-10" />
          <p className="text-sm">
            {isLoading
              ? 'Loading conversations…'
              : conversations.length === 0
                ? 'No conversations yet — start one with "New message".'
                : 'Pick a conversation on the left.'}
          </p>
        </div>
      )}
    </div>
  )
}
