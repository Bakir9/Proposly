import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Bell, Check, CheckCheck } from 'lucide-react'
import { getAllNotifications, markNotificationRead, markAllNotificationsRead } from '@/api/notifications'
import { useAuth } from '@/features/auth/AuthContext'
import { useNotificationTask } from '@/contexts/NotificationTaskContext'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'

type Filter = 'all' | 'unread'

export function NotificationsPage() {
  const [filter, setFilter] = useState<Filter>('all')
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const { setPendingTaskId } = useNotificationTask()

  const { data: notifications = [], isLoading } = useQuery({
    queryKey: ['notifications-all', user?.userId],
    queryFn: getAllNotifications,
    staleTime: 0,
  })

  const markRead = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications-all', user?.userId] })
      queryClient.invalidateQueries({ queryKey: ['notifications', user?.userId] })
    },
  })

  const markAllRead = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications-all', user?.userId] })
      queryClient.invalidateQueries({ queryKey: ['notifications', user?.userId] })
    },
  })

  const displayed = filter === 'unread' ? notifications.filter(n => !n.isRead) : notifications
  const unreadCount = notifications.filter(n => !n.isRead).length

  const handleClick = (id: string, isRead: boolean, link: string | null) => {
    if (!isRead) markRead.mutate(id)
    if (link) {
      const url = new URL(link, window.location.origin)
      const taskId = url.searchParams.get('task')
      if (taskId) setPendingTaskId(taskId)
      navigate(url.pathname)
    }
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-2">
          <Bell className="h-5 w-5 text-muted-foreground" />
          <h1 className="text-xl font-semibold">Notifications</h1>
          {unreadCount > 0 && (
            <span className="text-xs font-semibold bg-primary text-primary-foreground rounded-full px-2 py-0.5">
              {unreadCount}
            </span>
          )}
        </div>
        {unreadCount > 0 && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => markAllRead.mutate()}
            disabled={markAllRead.isPending}
            className="gap-1.5"
          >
            <CheckCheck className="h-3.5 w-3.5" />
            Mark all read
          </Button>
        )}
      </div>

      <div className="flex gap-1 mb-4 border-b">
        {(['all', 'unread'] as Filter[]).map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={cn(
              'px-4 py-2 text-sm font-medium capitalize border-b-2 -mb-px transition-colors',
              filter === f
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            )}
          >
            {f}
            {f === 'unread' && unreadCount > 0 && (
              <span className="ml-1.5 text-xs bg-muted text-muted-foreground rounded-full px-1.5 py-0.5">
                {unreadCount}
              </span>
            )}
          </button>
        ))}
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground text-center py-12">Loading…</p>
      ) : displayed.length === 0 ? (
        <div className="text-center py-16">
          <Bell className="h-10 w-10 text-muted-foreground/30 mx-auto mb-3" />
          <p className="text-sm text-muted-foreground">
            {filter === 'unread' ? 'No unread notifications' : 'No notifications yet'}
          </p>
        </div>
      ) : (
        <div className="border rounded-lg overflow-hidden">
          {displayed.map(n => (
            <div
              key={n.id}
              className={cn(
                'flex items-start gap-3 px-4 py-4 border-b last:border-0',
                !n.isRead && 'bg-primary/5'
              )}
            >
              <span
                className={cn(
                  'mt-1.5 h-2 w-2 rounded-full shrink-0',
                  !n.isRead ? 'bg-primary' : 'bg-transparent'
                )}
              />

              <button
                onClick={() => handleClick(n.id, n.isRead, n.link)}
                className={cn(
                  'flex-1 text-left min-w-0',
                  n.link && 'hover:opacity-80 transition-opacity'
                )}
              >
                <p className={cn('text-sm leading-snug', !n.isRead && 'font-medium')}>{n.title}</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  {new Date(n.createdAt).toLocaleString()}
                </p>
              </button>

              {!n.isRead && (
                <button
                  onClick={() => markRead.mutate(n.id)}
                  title="Mark as read"
                  className="shrink-0 mt-0.5 p-1.5 rounded text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors"
                >
                  <Check className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
