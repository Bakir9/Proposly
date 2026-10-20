import { useState, useEffect, useMemo } from 'react'
import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { LayoutDashboard, FileText, FolderKanban, Users, LogOut, Building2, Settings, BarChart3, CalendarDays, ShieldCheck, Clock, ClipboardCheck, CalendarOff, CalendarCheck, MessageCircle, ChevronLeft, ChevronRight } from 'lucide-react'
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

// HR section — the working-time redesign pages (frontend preview, backend wiring follows).
const HR_NAV_ITEMS = [
  { to: '/redesign/working-time', label: 'My working time', icon: Clock, adminOnly: false, superAdminOnly: false },
  { to: '/redesign/team',         label: 'Team settings',   icon: Users, adminOnly: true,  superAdminOnly: false },
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
  const [collapsed, setCollapsed] = useState(() => {
    try {
      return localStorage.getItem('sidebar-collapsed') === '1'
    } catch {
      return false
    }
  })
  const chatUnread = useChatUnread()

  const toggleSidebar = () => {
    setCollapsed(prev => {
      const next = !prev
      try {
        localStorage.setItem('sidebar-collapsed', next ? '1' : '0')
      } catch {
        /* private mode — the choice just won't persist */
      }
      return next
    })
  }

  const isSuperAdmin = user?.role === 'SuperAdmin'
  const isAdminOrOwner = user?.role === 'Owner' || user?.role === 'Admin'
  const byRole = (item: { adminOnly: boolean; superAdminOnly: boolean }) => {
    if (item.superAdminOnly) return isSuperAdmin
    if (item.adminOnly) return isAdminOrOwner && !isSuperAdmin
    return !isSuperAdmin
  }
  const navItems = ALL_NAV_ITEMS.filter(byRole)
  const hrItems = HR_NAV_ITEMS.filter(byRole)

  const { pathname } = useLocation()

  // NavLink's own isActive matches on prefix, which lights up a parent entry whenever a child
  // route is open — /worktime/approvals would highlight both "Work time" and "Approvals". Marking
  // the parents `end` instead would leave routes like /worktime/report highlighting nothing, so
  // the most specific matching entry wins and it alone is active.
  const activePath = useMemo(() => {
    const matches = [...navItems, ...hrItems]
      .filter(item => pathname === item.to || pathname.startsWith(`${item.to}/`))
      .sort((a, b) => b.to.length - a.to.length)

    return matches[0]?.to ?? null
  }, [navItems, hrItems, pathname])

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

  // Collapsed rail = 64px (icons only); the arrow button on the edge toggles back to the full
  // 224px sidebar in normal flow. Icons keep their x-position — only labels fade in/out.
  const fade = cn('transition-opacity duration-150', collapsed ? 'opacity-0' : 'opacity-100 delay-75')

  const itemClass = (active: boolean) =>
    cn(
      'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors whitespace-nowrap',
      active
        ? 'bg-primary text-primary-foreground'
        : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
    )

  return (
    <NotificationTaskProvider>
    <div className="flex h-screen bg-background">
      {/* Sidebar — the arrow on the edge collapses it to an icon rail or expands it back. */}
      <div className="relative flex-none">
        <aside
          className={cn(
            'h-full flex flex-col overflow-hidden border-r bg-card transition-[width] duration-200 ease-out',
            collapsed ? 'w-16' : 'w-56'
          )}
        >
        <div className="px-3.5 py-4 border-b">
          <div className="flex items-center gap-3 whitespace-nowrap">
            <img src={proposlyLogo} alt="Proposly" className="h-9 w-9 object-contain rounded-xl shadow-sm shrink-0" />
            <div className={cn('min-w-0', fade)}>
              <p className="font-bold text-base leading-tight tracking-tight">Proposly</p>
              <p className="text-[11px] text-muted-foreground leading-tight">Business Suite</p>
            </div>
          </div>
        </div>

        <nav className="flex-1 p-3 space-y-1 overflow-y-auto overflow-x-hidden">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} title={label} className={itemClass(activePath === to)}>
              <span className="relative shrink-0">
                <Icon className="h-4 w-4" />
                {to === '/chat' && chatUnread > 0 && (
                  <span
                    className={cn(
                      'absolute -right-1 -top-1 h-2 w-2 rounded-full transition-opacity',
                      collapsed ? 'opacity-100' : 'opacity-0',
                      activePath === to ? 'bg-primary-foreground' : 'bg-primary ring-2 ring-card'
                    )}
                  />
                )}
              </span>
              <span className={cn('flex-1', fade)}>{label}</span>
              {to === '/chat' && chatUnread > 0 && (
                <span
                  className={cn(
                    'min-w-5 h-5 px-1.5 rounded-full text-[11px] font-semibold flex items-center justify-center',
                    fade,
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

          {hrItems.length > 0 && (
            <>
              {/* Collapsed: a short divider; expanded: the HR heading. Same height, no jump. */}
              <div className="relative h-7 mt-2">
                <span className={cn('absolute left-3 right-3 top-1/2 border-t transition-opacity', collapsed ? 'opacity-100' : 'opacity-0')} />
                <p className={cn('absolute inset-x-0 bottom-0.5 px-3 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground/70', fade)}>
                  HR
                </p>
              </div>
              {hrItems.map(({ to, label, icon: Icon }) => (
                <NavLink key={to} to={to} title={label} className={itemClass(activePath === to)}>
                  <Icon className="h-4 w-4 shrink-0" />
                  <span className={fade}>{label}</span>
                </NavLink>
              ))}
            </>
          )}
        </nav>

        <div className="p-3 border-t space-y-1">
          <NavLink
            to="/settings"
            title="Settings"
            className={({ isActive }) => itemClass(isActive)}
          >
            <Settings className="h-4 w-4 shrink-0" />
            <span className={fade}>Settings</span>
          </NavLink>

          <NavLink
            to="/profile"
            title="My Profile"
            className={({ isActive }) => itemClass(isActive)}
          >
            <div className={`${avatarColor} w-5 h-5 -ml-0.5 rounded-full flex items-center justify-center text-white text-[10px] font-bold shrink-0`}>
              {initials}
            </div>
            <span className={fade}>My Profile</span>
          </NavLink>

          <Button
            variant="ghost"
            size="sm"
            title="Sign out"
            className="w-full justify-start gap-3 text-muted-foreground whitespace-nowrap"
            onClick={handleLogout}
          >
            <LogOut className="h-4 w-4 shrink-0" />
            <span className={fade}>Sign out</span>
          </Button>
        </div>
        </aside>

        {/* Collapse/expand toggle — floats on the sidebar's edge, vertically centered. */}
        <button
          onClick={toggleSidebar}
          title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          className="absolute -right-3 top-1/2 -translate-y-1/2 z-40 h-6 w-6 rounded-full border bg-card shadow-sm flex items-center justify-center text-muted-foreground transition-colors hover:text-foreground hover:border-primary cursor-pointer"
        >
          {collapsed ? <ChevronRight className="h-3.5 w-3.5" /> : <ChevronLeft className="h-3.5 w-3.5" />}
        </button>
      </div>

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
