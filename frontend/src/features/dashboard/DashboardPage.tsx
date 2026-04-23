import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { getDashboard } from '@/api/dashboard'
import { useAuth } from '@/features/auth/AuthContext'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { FileText, FolderKanban, TrendingUp, Clock } from 'lucide-react'

export function DashboardPage() {
  const { data, isLoading } = useQuery({ queryKey: ['dashboard'], queryFn: getDashboard })
  const navigate = useNavigate()
  const { user } = useAuth()
  const isAdminOrOwner = user?.role === 'Owner' || user?.role === 'Admin'

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (!data) return null

  const fmt = (n: number) => n.toLocaleString('de-AT', { style: 'currency', currency: data.currency || 'EUR' })

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-semibold">Dashboard</h1>

      <div className={`grid gap-4 ${isAdminOrOwner ? 'grid-cols-2 lg:grid-cols-4' : 'grid-cols-2'}`}>
        {isAdminOrOwner && (
          <Card>
            <CardContent className="pt-6 flex items-start gap-4">
              <div className="rounded-xl bg-blue-100 dark:bg-blue-900/40 p-3">
                <FileText className="h-6 w-6 text-blue-600 dark:text-blue-400" />
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Open Offers</p>
                <p className="text-3xl font-bold mt-0.5">{data.openOffersCount}</p>
                <p className="text-sm text-muted-foreground mt-0.5">{fmt(data.openOffersValue)}</p>
              </div>
            </CardContent>
          </Card>
        )}
        <Card>
          <CardContent className="pt-6 flex items-start gap-4">
            <div className="rounded-xl bg-violet-100 dark:bg-violet-900/40 p-3">
              <FolderKanban className="h-6 w-6 text-violet-600 dark:text-violet-400" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Active Projects</p>
              <p className="text-3xl font-bold mt-0.5">{data.activeProjectsCount}</p>
            </div>
          </CardContent>
        </Card>
        {isAdminOrOwner && (
          <Card>
            <CardContent className="pt-6 flex items-start gap-4">
              <div className="rounded-xl bg-emerald-100 dark:bg-emerald-900/40 p-3">
                <TrendingUp className="h-6 w-6 text-emerald-600 dark:text-emerald-400" />
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Locked Revenue</p>
                <p className="text-3xl font-bold mt-0.5">{fmt(data.totalLockedRevenue)}</p>
              </div>
            </CardContent>
          </Card>
        )}
        <Card>
          <CardContent className="pt-6 flex items-start gap-4">
            <div className="rounded-xl bg-amber-100 dark:bg-amber-900/40 p-3">
              <Clock className="h-6 w-6 text-amber-600 dark:text-amber-400" />
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Hours This Month</p>
              <p className="text-3xl font-bold mt-0.5">{data.hoursThisMonth}h</p>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className={`grid grid-cols-1 gap-6 ${isAdminOrOwner ? 'lg:grid-cols-2' : ''}`}>
        {isAdminOrOwner && (
          <div className="space-y-3">
            <h2 className="text-lg font-semibold flex items-center gap-2"><FileText className="h-4 w-4" /> Recent Offers</h2>
            {data.recentOffers.length === 0 && <p className="text-sm text-muted-foreground">No offers yet.</p>}
            {data.recentOffers.map(o => (
              <Card key={o.id} className="cursor-pointer hover:bg-muted/50 transition-colors" onClick={() => navigate(`/offers/${o.id}`)}>
                <CardContent className="py-3 flex items-center justify-between">
                  <div>
                    <p className="font-medium text-sm">{o.title}</p>
                    <p className="text-xs text-muted-foreground">{o.clientName}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-medium">{o.total.toLocaleString('de-AT', { style: 'currency', currency: o.currency })}</p>
                    <p className="text-xs text-muted-foreground">{o.status}</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
        <div className="space-y-3">
          <h2 className="text-lg font-semibold flex items-center gap-2"><FolderKanban className="h-4 w-4" /> Recent Projects</h2>
          {data.recentProjects.length === 0 && <p className="text-sm text-muted-foreground">No projects yet.</p>}
          {data.recentProjects.map(p => (
            <Card key={p.id} className="cursor-pointer hover:bg-muted/50 transition-colors" onClick={() => navigate(`/projects/${p.id}`)}>
              <CardContent className="py-3 flex items-center justify-between">
                <div>
                  <p className="font-medium text-sm">{p.name}</p>
                  <p className="text-xs text-muted-foreground">{p.status}</p>
                </div>
                <p className="text-sm font-medium">{p.budgetAmount.toLocaleString('de-AT', { style: 'currency', currency: p.currency })}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}
