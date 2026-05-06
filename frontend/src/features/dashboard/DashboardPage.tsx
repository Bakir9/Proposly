import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { getDashboard } from '@/api/dashboard'
import { useAuth } from '@/features/auth/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { FileText, FolderKanban, TrendingUp, Clock, Plus, ArrowRight } from 'lucide-react'

type BadgeVariant = 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'

const OFFER_STATUS_VARIANT: Record<string, BadgeVariant> = {
  Draft: 'secondary',
  Sent: 'default',
  Accepted: 'success',
  Rejected: 'destructive',
  Expired: 'destructive',
}

const PROJECT_STATUS_VARIANT: Record<string, BadgeVariant> = {
  Planning: 'secondary',
  Active: 'default',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'destructive',
}

const PROJECT_DOT_COLOR: Record<string, string> = {
  Active: 'bg-green-500',
  Completed: 'bg-slate-400',
  Planning: 'bg-blue-400',
  OnHold: 'bg-amber-500',
  Cancelled: 'bg-red-500',
}

const PROJECT_BAR_COLOR: Record<string, string> = {
  Active: 'bg-blue-500',
  Completed: 'bg-green-500',
  Planning: 'bg-slate-400',
  OnHold: 'bg-amber-500',
  Cancelled: 'bg-red-400',
}

function getGreeting() {
  const h = new Date().getHours()
  if (h < 12) return 'Good Morning'
  if (h < 18) return 'Good Afternoon'
  return 'Good Evening'
}

export function DashboardPage() {
  const { data, isLoading } = useQuery({ queryKey: ['dashboard'], queryFn: getDashboard })
  const navigate = useNavigate()
  const { user } = useAuth()
  const isAdminOrOwner = user?.role === 'Owner' || user?.role === 'Admin'
  const firstName = user?.fullName?.split(' ')[0] ?? 'there'

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (!data) return null

  const fmt = (n: number) => n.toLocaleString('de-AT', { style: 'currency', currency: data.currency || 'EUR' })
  const totalOffers = data.totalOffersCount ?? data.openOffersCount
  const acceptedOffers = data.acceptedOffersCount ?? 0
  const conversionPct = totalOffers > 0 ? Math.round((acceptedOffers / totalOffers) * 100) : 0

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold">{getGreeting()}, {firstName}</h1>
          <p className="text-sm text-muted-foreground mt-1">Here is what's happening with your suite today.</p>
        </div>
        <Button onClick={() => navigate('/projects')} className="gap-1.5">
          <Plus className="h-4 w-4" /> New Project
        </Button>
      </div>

      {/* KPI cards */}
      <div className={`grid gap-4 ${isAdminOrOwner ? 'grid-cols-2 lg:grid-cols-4' : 'grid-cols-2'}`}>
        {isAdminOrOwner && (
          <Card>
            <CardContent className="pt-5 pb-5">
              <div className="flex items-start justify-between">
                <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">All Offers</p>
                <div className="rounded-lg bg-blue-500/15 p-2">
                  <FileText className="h-5 w-5 text-blue-500" />
                </div>
              </div>
              <p className="text-4xl font-bold mt-3">{totalOffers}</p>
              <p className="text-sm text-muted-foreground mt-1">
                {data.openOffersCount} open · {acceptedOffers} accepted
              </p>
            </CardContent>
          </Card>
        )}

        <Card>
          <CardContent className="pt-5 pb-5">
            <div className="flex items-start justify-between">
              <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Active Projects</p>
              <div className="rounded-lg bg-violet-500/15 p-2">
                <FolderKanban className="h-5 w-5 text-violet-500" />
              </div>
            </div>
            <p className="text-4xl font-bold mt-3">{data.activeProjectsCount}</p>
            <p className="text-sm text-muted-foreground mt-1">
              {data.activeProjectsCount === 1 ? '1 project running' : `${data.activeProjectsCount} projects running`}
            </p>
          </CardContent>
        </Card>

        {isAdminOrOwner && (
          <Card>
            <CardContent className="pt-5 pb-5">
              <div className="flex items-start justify-between">
                <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Locked Revenue</p>
                <div className="rounded-lg bg-emerald-500/15 p-2">
                  <TrendingUp className="h-5 w-5 text-emerald-500" />
                </div>
              </div>
              <p className="text-3xl font-bold mt-3">{fmt(data.totalLockedRevenue)}</p>
              <p className="text-sm text-green-500 font-medium mt-1">
                {data.acceptedOffersCount} accepted offer{data.acceptedOffersCount !== 1 ? 's' : ''}
              </p>
            </CardContent>
          </Card>
        )}

        <Card>
          <CardContent className="pt-5 pb-5">
            <div className="flex items-start justify-between">
              <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Hours This Month</p>
              <div className="rounded-lg bg-amber-500/15 p-2">
                <Clock className="h-5 w-5 text-amber-500" />
              </div>
            </div>
            <p className="text-4xl font-bold mt-3">{data.hoursThisMonth}h</p>
            <p className="text-sm text-muted-foreground mt-1">Logged this month</p>
          </CardContent>
        </Card>
      </div>

      {/* Recent Offers + Conversion (admin only) */}
      {isAdminOrOwner && (
        <div className="grid grid-cols-[1fr_280px] gap-6">
          <Card>
            <CardContent className="p-0">
              <div className="flex items-center justify-between px-5 py-4 border-b">
                <h2 className="font-bold text-base">Recent Offers</h2>
                <button
                  onClick={() => navigate('/offers')}
                  className="text-xs text-primary hover:underline font-medium"
                >
                  View All
                </button>
              </div>
              {data.recentOffers.length === 0 ? (
                <p className="text-sm text-muted-foreground py-10 text-center">No offers yet.</p>
              ) : (
                <>
                  <div className="grid grid-cols-[1fr_160px_120px_100px] px-5 py-2.5 text-[10px] font-bold uppercase tracking-widest text-muted-foreground border-b bg-muted/30">
                    <span>Title</span>
                    <span>Client / Company</span>
                    <span className="text-right">Amount</span>
                    <span className="text-right">Status</span>
                  </div>
                  <div className="divide-y divide-border">
                    {data.recentOffers.map(o => (
                      <div
                        key={o.id}
                        className="grid grid-cols-[1fr_160px_120px_100px] items-center px-5 py-3.5 cursor-pointer hover:bg-muted/30 transition-colors"
                        onClick={() => navigate(`/offers/${o.id}`)}
                      >
                        <p className="text-sm font-semibold">{o.title}</p>
                        <p className="text-sm text-muted-foreground">{o.clientName}</p>
                        <p className="text-sm font-semibold text-right">
                          {o.total.toLocaleString('de-AT', { style: 'currency', currency: o.currency })}
                        </p>
                        <div className="flex justify-end">
                          <Badge
                            variant={OFFER_STATUS_VARIANT[o.status] ?? 'outline'}
                            className="text-[10px] uppercase tracking-wide"
                          >
                            {o.status}
                          </Badge>
                        </div>
                      </div>
                    ))}
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* Offer Conversion */}
          <Card>
            <CardContent className="p-5 flex flex-col h-full">
              <h2 className="font-bold text-base">Offers Overview</h2>
              <div className="mt-5 space-y-3 flex-1">
                {[
                  { label: 'Total Offers', value: data.totalOffersCount ?? data.openOffersCount, color: '' },
                  { label: 'Open', value: data.openOffersCount, color: 'text-muted-foreground' },
                  { label: 'Accepted', value: data.acceptedOffersCount ?? 0, color: 'text-green-500' },
                ].map(row => (
                  <div key={row.label} className="flex items-center justify-between text-sm border-b border-border/40 pb-3 last:border-0">
                    <span className="text-muted-foreground">{row.label}</span>
                    <span className={`font-semibold ${row.color}`}>{row.value}</span>
                  </div>
                ))}
              </div>
              <div className="border-t pt-4 mt-2">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm text-muted-foreground">Conversion Rate</span>
                  <span className="text-sm font-bold text-primary">{conversionPct}%</span>
                </div>
                <div className="w-full bg-muted rounded-full h-2">
                  <div
                    className="h-2 rounded-full bg-primary transition-all"
                    style={{ width: `${conversionPct}%` }}
                  />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Recent Projects */}
      <Card>
        <CardContent className="p-0">
          <div className="flex items-center justify-between px-5 py-4 border-b">
            <h2 className="font-bold text-base">Recent Projects</h2>
            <button
              onClick={() => navigate('/projects')}
              className="text-xs text-primary hover:underline font-medium flex items-center gap-1"
            >
              Manage All <ArrowRight className="h-3 w-3" />
            </button>
          </div>
          {data.recentProjects.length === 0 ? (
            <p className="text-sm text-muted-foreground py-10 text-center">No projects yet.</p>
          ) : (
            <>
              <div className="grid grid-cols-[1fr_130px_1fr_160px] px-5 py-2.5 text-[10px] font-bold uppercase tracking-widest text-muted-foreground border-b bg-muted/30">
                <span>Project Name</span>
                <span>Status</span>
                <span>Timeline</span>
                <span className="text-right">Total Value</span>
              </div>
              <div className="divide-y divide-border">
                {data.recentProjects.map(p => {
                  const pct = p.totalTasksCount > 0
                    ? Math.round((p.completedTasksCount / p.totalTasksCount) * 100)
                    : p.status === 'Completed' ? 100 : 0
                  const barColor = PROJECT_BAR_COLOR[p.status] ?? 'bg-slate-400'
                  const dotColor = PROJECT_DOT_COLOR[p.status] ?? 'bg-slate-400'
                  return (
                    <div
                      key={p.id}
                      className="grid grid-cols-[1fr_130px_1fr_160px] items-center px-5 py-3.5 cursor-pointer hover:bg-muted/30 transition-colors"
                      onClick={() => navigate(`/projects/${p.id}`)}
                    >
                      <div className="flex items-center gap-2.5">
                        <span className={`w-2 h-2 rounded-full shrink-0 ${dotColor}`} />
                        <p className="text-sm font-semibold">{p.name}</p>
                      </div>
                      <div>
                        <Badge
                          variant={PROJECT_STATUS_VARIANT[p.status] ?? 'outline'}
                          className="text-[10px]"
                        >
                          {p.status}
                        </Badge>
                      </div>
                      <div className="flex items-center gap-3 pr-8">
                        <div className="flex-1 bg-muted rounded-full h-1.5">
                          <div
                            className={`h-1.5 rounded-full transition-all ${barColor}`}
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                        <span className="text-xs font-medium text-muted-foreground w-8 shrink-0 text-right">
                          {pct}%
                        </span>
                      </div>
                      <p className="text-sm font-semibold text-right">
                        {p.budgetAmount.toLocaleString('de-AT', { style: 'currency', currency: p.currency })}
                      </p>
                    </div>
                  )
                })}
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
