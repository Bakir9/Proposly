import { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Bell, Check } from 'lucide-react'
import { useNavigate, Link } from 'react-router-dom'
import {
  getNotifications,
  markNotificationRead,
  markAllNotificationsRead,
  type NotificationResponse,
} from '@/api/notifications'
import { useAuth } from '@/features/auth/AuthContext'
import { useNotificationTask } from '@/contexts/NotificationTaskContext'
import { cn } from '@/lib/utils'

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const { setPendingTaskId } = useNotificationTask()

  const { data: notifications = [] } = useQuery({
    queryKey: ['notifications', user?.userId],
    queryFn: () => getNotifications(),
    refetchInterval: 30_000,
    staleTime: 0,
  })

  const unreadCount = notifications.filter(n => !n.isRead).length

  const markRead = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications', user?.userId] }),
  })

  const markAllRead = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications', user?.userId] }),
  })

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node))
        setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const handleClick = (n: NotificationResponse) => {
    if (!n.isRead) markRead.mutate(n.id)
    setOpen(false)
    if (n.link) {
      const url = new URL(n.link, window.location.origin)
      const taskId = url.searchParams.get('task')
      if (taskId) setPendingTaskId(taskId)
      navigate(url.pathname)
    }
  }

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(o => !o)}
        className="relative flex items-center justify-center w-8 h-8 rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors"
      >
        <Bell className="h-4 w-4" />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 h-4 w-4 rounded-full bg-red-500 text-white text-[10px] font-bold flex items-center justify-center leading-none">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-10 w-80 bg-card border rounded-lg shadow-lg z-50 overflow-hidden">
          <div className="flex items-center justify-between px-4 py-3 border-b">
            <span className="text-sm font-semibold">Notifications</span>
            {unreadCount > 0 && (
              <button
                onClick={() => markAllRead.mutate()}
                className="text-xs text-primary hover:underline"
              >
                Mark all read
              </button>
            )}
          </div>

          <div className="max-h-72 overflow-y-auto">
            {notifications.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-8">No notifications</p>
            ) : (
              notifications.map(n => (
                <div
                  key={n.id}
                  className={cn(
                    'flex items-start gap-2 px-4 py-3 text-sm border-b last:border-0 group',
                    !n.isRead && 'bg-primary/5'
                  )}
                >
                  {/* blue dot */}
                  <span className={cn('mt-1.5 h-2 w-2 rounded-full shrink-0', !n.isRead ? 'bg-primary' : 'bg-transparent')} />

                  {/* text — clickable, navigates */}
                  <button
                    onClick={() => handleClick(n)}
                    className="flex-1 text-left hover:opacity-80 transition-opacity min-w-0"
                  >
                    <p className={cn('leading-snug truncate', !n.isRead && 'font-medium')}>{n.title}</p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {new Date(n.createdAt).toLocaleString()}
                    </p>
                  </button>

                  {/* mark-as-read button — only shown for unread */}
                  {!n.isRead && (
                    <button
                      onClick={e => { e.stopPropagation(); markRead.mutate(n.id) }}
                      title="Mark as read"
                      className="shrink-0 mt-0.5 p-1 rounded text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors"
                    >
                      <Check className="h-3.5 w-3.5" />
                    </button>
                  )}
                </div>
              ))
            )}
          </div>

          <div className="border-t px-4 py-2">
            <Link
              to="/notifications"
              onClick={() => setOpen(false)}
              className="text-xs text-primary hover:underline"
            >
              View all notifications
            </Link>
          </div>
        </div>
      )}
    </div>
  )
}
