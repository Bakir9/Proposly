import { useQuery } from '@tanstack/react-query'
import { getUnreadTotal } from '@/api/chat'
import { useAuth } from '@/features/auth/AuthContext'

/** Total unread messages, polled every 30 s — drives the sidebar and header badges. */
export function useChatUnread() {
  const { user } = useAuth()
  const { data } = useQuery({
    queryKey: ['chat', 'unread', user?.userId],
    queryFn: getUnreadTotal,
    refetchInterval: 30_000,
    staleTime: 0,
    enabled: !!user && user.role !== 'SuperAdmin',
  })
  return data ?? 0
}
