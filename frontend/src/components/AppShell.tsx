import { useState, useEffect, useMemo } from 'react'
import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { LayoutDashboard, FileText, FolderKanban, Users, LogOut, Building2, Settings, BarChart3, CalendarDays, ShieldCheck, Clock, ClipboardCheck, CalendarOff, CalendarCheck, MessageCircle } from 'lucide-react'
import { useAuth } from '@/features/auth/AuthContext'
import { useQueryClient } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { Button } from './ui/button'
import { getAvatarColor } from '@/features/profile/ProfilePage'
import { TopBar } from './TopBar'
import proposlyLogo from '@/assets/proposly.png'
import { NotificationTaskProvider } from '@/contexts/NotificationTaskContext'
import { useChatUnread } from '@/features/chat/useChatUnread'

const ALL_NAV_ITEMS = [
  { to: '/dashboard',        label: 'Dashboard', icon: LayoutDashboard, adminOnly: false, superAdminOnly: false },
  { to: '/offers',           label: 'Offers',    icon: FileText,        adminOnly: true,  superAdminOnly: false },
  { to: '/projects',         label: 'Projects',  icon: FolderKanban,   adminOnly: false, superAdminOnly: false },
  { to: '/calendar',         label: 'Calendar',  icon: CalendarDays,   adminOnly: false, superAdminOnly: false },
  { to: '/worktime',         label: 'Work time', icon: Clock,          adminOnly: false, superAdminOnly: false },
  { to: '/absences',         label: 'Time off',  icon: CalendarOff,    adminOnly: false, superAdminOnly: false },
  { to: '/worktime/approvals', label: 'Approvals', icon: ClipboardCheck, adminOnly: true, superAdminOnly: false },
  { to: '/absences/approvals', label: 'Time off approvals', icon: CalendarCheck, adminOnly: true, superAdminOnly: false },
  { to: '/chat',             label: 'Messages',  icon: MessageCircle,  adminOnly: false, superAdminOnly: false },
  { to: '/clients',          label: 'Clients',   icon: Building2,      adminOnly: true,  superAdminOnly: false },
  { to: '/users',            label: 'Team',      icon: Users,          adminOnly: true,  superAdminOnly: false },
  { to: '/reports',          label: 'Reports',   icon: BarChart3,      adminOnly: true,  superAdminOnly: false },
  { to: '/admin/companies',  label: 'Companies', icon: ShieldCheck,    adminOnly: false, superAdminOnly: true  },
]

function getInitials(fullName: string) {
  const parts = fullName.trim().split(' ')
  return parts.length >= 2
    ? `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
    : fullName.slice(0, 2).toUpperCase()
}

export function AppShell() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [avatarColor, setAvatarColorState] = useState(getAvatarColor)
  const chatUnread = useChatUnread()

  const isSuperAdmin = user?.role === 'SuperAdmin'
  const isAdminOrOwner = user?.role === 'Owner' || user?.role === 'Admin'
  const navItems = ALL_NAV_ITEMS.filter(item => {
    if (item.superAdminOnly) return isSuperAdmin
    if (item.adminOnly) return isAdminOrOwner && !isSuperAdmin
    return !isSuperAdmin
  })

  const { pathname } = useLocation()

  // NavLink's own isActive matches on prefix, which lights up a parent entry whenever a child
  // route is open — /worktime/approvals would highlight both "Work time" and "Approvals". Marking
  // the parents `end` instead would leave routes like /worktime/report highlighting nothing, so
  // the most specific matching entry wins and it alone is active.
  const activePath = useMemo(() => {
    const matches = navItems
      .filter(item => pathname === item.to || pathname.startsWith(`${item.to}/`))
      .sort((a, b) => b.to.length - a.to.length)

    return matches[0]?.to ?? null
  }, [navItems, pathname])

  useEffect(() => {
    const handler = () => setAvatarColorState(getAvatarColor())
    window.addEventListener('avatarChanged', handler)
    return () => window.removeEventListener('avatarChanged', handler)
  }, [])

  const handleLogout = () => {
    logout()
    queryClient.clear()
    navigate('/login')
  }

  const initials = user ? getInitials(user.fullName) : '?'

  return (
    <NotificationTaskProvider>
    <div className="flex h-screen bg-background">
      {/* Sidebar */}
      <aside className="w-56 flex flex-col border-r bg-card">
        <div className="px-4 py-4 border-b">
          <div className="flex items-center gap-3">
            <img src={proposlyLogo} alt="Proposly" className="h-9 w-9 object-contain rounded-xl shadow-sm shrink-0" />
            <div className="min-w-0">
              <p className="font-bold text-base leading-tight tracking-tight">Proposly</p>
              <p className="text-[11px] text-muted-foreground leading-tight">Business Suite</p>
            </div>
          </div>
        </div>

        <nav className="flex-1 p-3 space-y-1">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                activePath === to
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )}
            >
              <Icon className="h-4 w-4" />
              <span className="flex-1">{label}</span>
              {to === '/chat' && chatUnread > 0 && (
                <span
                  className={cn(
                    'min-w-5 h-5 px-1.5 rounded-full text-[11px] font-semibold flex items-center justify-center',
                    activePath === to
                      ? 'bg-primary-foreground text-primary'
                      : 'bg-primary text-primary-foreground'
                  )}
                >
                  {chatUnread > 99 ? '99+' : chatUnread}
                </span>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="p-3 border-t space-y-1">
          <NavLink
            to="/settings"
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )
            }
          >
            <Settings className="h-4 w-4" />
            Settings
          </NavLink>

          <NavLink
            to="/profile"
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )
            }
          >
            <div className={`${avatarColor} w-5 h-5 rounded-full flex items-center justify-center text-white text-[10px] font-bold shrink-0`}>
              {initials}
            </div>
            My Profile
          </NavLink>

          <Button variant="ghost" size="sm" className="w-full justify-start gap-3 text-muted-foreground" onClick={handleLogout}>
            <LogOut className="h-4 w-4" />
            Sign out
          </Button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 flex flex-col overflow-hidden">
        <TopBar />
        <div className="flex-1 overflow-auto">
          <Outlet />
        </div>
      </main>
    </div>
    </NotificationTaskProvider>
  )
}
