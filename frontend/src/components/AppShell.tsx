import { useState, useEffect } from 'react'
import { NavLink, Outlet, useNavigate, useLocation } from 'react-router-dom'
import { LayoutDashboard, FileText, FolderKanban, Users, LogOut, Building2, Settings, BarChart3, CalendarDays, ShieldCheck, Clock, ClipboardCheck, CalendarOff, CalendarCheck, MessageCircle, ChevronLeft, ChevronRight } from 'lucide-react'
import { useAuth } from '@/features/auth/AuthContext'
import { useQueryClient } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { getAvatarColor } from '@/features/profile/ProfilePage'
import { TopBar } from './TopBar'
import proposlyLogo from '@/assets/proposly.png'
import { NotificationTaskProvider } from '@/contexts/NotificationTaskContext'
import { useChatUnread } from '@/features/chat/useChatUnread'
import { ChatDock } from '@/features/chat/dock/ChatDock'

// Grouped navigation per the workspace redesign: Workspace · Sales · Delivery · People.
// Old routes stay alive (e.g. /worktime, /worktime/approvals) — only the entries moved:
// "Work time" now opens the redesigned page, the two approval pages merged into /approvals.
const NAV_GROUPS = [
  {
    label: 'Workspace',
    items: [
      { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, adminOnly: false, superAdminOnly: false },
      { to: '/chat',      label: 'Messages',  icon: MessageCircle,   adminOnly: false, superAdminOnly: false },
      { to: '/calendar',  label: 'Calendar',  icon: CalendarDays,    adminOnly: false, superAdminOnly: false },
    ],
  },
  {
    label: 'Sales',
    items: [
      { to: '/offers',  label: 'Offers',  icon: FileText,  adminOnly: true, superAdminOnly: false },
      { to: '/clients', label: 'Clients', icon: Building2, adminOnly: true, superAdminOnly: false },
    ],
  },
  {
    label: 'Delivery',
    items: [
      { to: '/projects', label: 'Projects', icon: FolderKanban, adminOnly: false, superAdminOnly: false },
      { to: '/reports',  label: 'Reports',  icon: BarChart3,    adminOnly: true,  superAdminOnly: false },
    ],
  },
  {
    label: 'People',
    items: [
      { to: '/redesign/working-time', label: 'Work time',     icon: Clock,          adminOnly: false, superAdminOnly: false },
      { to: '/absences',              label: 'Time off',      icon: CalendarOff,    adminOnly: false, superAdminOnly: false },
      { to: '/approvals',             label: 'Approvals',     icon: ClipboardCheck, adminOnly: true,  superAdminOnly: false },
      { to: '/team-time',             label: 'Team time',     icon: Clock,          adminOnly: true,  superAdminOnly: false },
      { to: '/users',                 label: 'Team',          icon: Users,          adminOnly: true,  superAdminOnly: false },
      { to: '/redesign/team',         label: 'Team settings', icon: CalendarCheck,  adminOnly: true,  superAdminOnly: false },
    ],
  },
  {
    label: 'Admin',
    items: [
      { to: '/admin/companies', label: 'Companies', icon: ShieldCheck, adminOnly: false, superAdminOnly: true },
    ],
  },
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
  const navGroups = NAV_GROUPS
    .map(g => ({ ...g, items: g.items.filter(byRole) }))
    .filter(g => g.items.length > 0)
  const navItems = navGroups.flatMap(g => g.items)

  const { pathname } = useLocation()

  // NavLink's own isActive matches on prefix, which lights up a parent entry whenever a child
  // route is open — /worktime/approvals would highlight both "Work time" and "Approvals". Marking
  // the parents `end` instead would leave routes like /worktime/report highlighting nothing, so
  // the most specific matching entry wins and it alone is active.
  const activePath = navItems
    .filter(item => pathname === item.to || pathname.startsWith(`${item.to}/`))
    .sort((a, b) => b.to.length - a.to.length)[0]?.to ?? null

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

  // Narrow rail: icon on top, label underneath — the sidebar needs far less width this way.
  // Collapsed (64px) drops the labels and keeps icons only.
  const itemClass = (active: boolean) =>
    cn(
      'rounded-lg transition-colors',
      collapsed
        ? 'flex items-center justify-center p-2'
        : 'flex w-full flex-col items-center gap-1 px-1 py-2 text-[10.5px] font-medium text-center leading-tight break-words',
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
            collapsed ? 'w-16' : 'w-24'
          )}
        >
        <div className="px-2 py-4 border-b">
          <div className="flex flex-col items-center gap-1.5">
            <img src={proposlyLogo} alt="Proposly" className="h-9 w-9 object-contain rounded-xl shadow-sm shrink-0" />
            {!collapsed && <p className="font-bold text-[13px] leading-tight tracking-tight">Proposly</p>}
          </div>
        </div>

        <nav className="flex-1 p-2 space-y-1 overflow-y-auto overflow-x-hidden">
          {navGroups.map((g, gi) => (
            <div key={g.label} className="space-y-1">
              {/* Collapsed: a short divider; expanded: a small centered group heading. */}
              <div className={cn('relative h-6', gi > 0 && 'mt-1.5')}>
                {gi > 0 && (
                  <span className={cn('absolute left-2 right-2 top-1/2 border-t transition-opacity', collapsed ? 'opacity-100' : 'opacity-0')} />
                )}
                {!collapsed && (
                  <p className="absolute inset-x-0 bottom-0 text-center text-[9.5px] font-semibold uppercase tracking-wider text-muted-foreground/70">
                    {g.label}
                  </p>
                )}
              </div>
              {g.items.map(({ to, label, icon: Icon }) => (
                <NavLink key={to} to={to} title={label} className={itemClass(activePath === to)}>
                  <span className="relative shrink-0">
                    <Icon className="h-5 w-5" />
                    {to === '/chat' && chatUnread > 0 && (
                      <span
                        className={cn(
                          'absolute -right-2.5 -top-1.5 min-w-4 h-4 px-1 rounded-full text-[9px] font-bold flex items-center justify-center',
                          activePath === to
                            ? 'bg-primary-foreground text-primary'
                            : 'bg-primary text-primary-foreground ring-2 ring-card'
                        )}
                      >
                        {chatUnread > 99 ? '99+' : chatUnread}
                      </span>
                    )}
                  </span>
                  {!collapsed && <span>{label}</span>}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        <div className="p-2 border-t space-y-1">
          <div
            title="EU-hosted · DSGVO-konform"
            className={cn(
              'rounded-md bg-green-100 text-green-800 dark:bg-green-950/40 dark:text-green-400',
              collapsed ? 'flex items-center justify-center p-2' : 'flex flex-col items-center gap-0.5 px-1 py-1.5'
            )}
          >
            <ShieldCheck className="h-4 w-4 shrink-0" />
            {!collapsed && <span className="text-[9px] font-semibold leading-tight">EU · DSGVO</span>}
          </div>
          <NavLink
            to="/settings"
            title="Settings"
            className={({ isActive }) => itemClass(isActive)}
          >
            <Settings className="h-5 w-5 shrink-0" />
            {!collapsed && <span>Settings</span>}
          </NavLink>

          <NavLink
            to="/profile"
            title="My Profile"
            className={({ isActive }) => itemClass(isActive)}
          >
            <div className={`${avatarColor} w-6 h-6 rounded-full flex items-center justify-center text-white text-[10px] font-bold shrink-0`}>
              {initials}
            </div>
            {!collapsed && <span>Profile</span>}
          </NavLink>

          <button
            type="button"
            title="Sign out"
            className={itemClass(false)}
            onClick={handleLogout}
          >
            <LogOut className="h-5 w-5 shrink-0" />
            {!collapsed && <span>Sign out</span>}
          </button>
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

      {/* Floating chat dock — launcher, people panel, docked DM windows */}
      <ChatDock />
    </div>
    </NotificationTaskProvider>
  )
}
