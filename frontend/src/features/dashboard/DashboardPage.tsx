import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { getDashboard } from '@/api/dashboard'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function DashboardPage() {
  const { data, isLoading } = useQuery({ queryKey: ['dashboard'], queryFn: getDashboard })
  const navigate = useNavigate()

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (!data) return null

  const fmt = (n: number) => n.toLocaleString('de-AT', { style: 'currency', currency: data.currency || 'EUR' })

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-semibold">Dashboard</h1>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Open Offers</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{data.openOffersCount}</p>
            <p className="text-sm text-muted-foreground mt-1">{fmt(data.openOffersValue)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Active Projects</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{data.activeProjectsCount}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Locked Revenue</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{fmt(data.totalLockedRevenue)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">Hours This Month</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{data.hoursThisMonth}h</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="space-y-3">
          <h2 className="text-lg font-semibold">Recent Offers</h2>
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
        <div className="space-y-3">
          <h2 className="text-lg font-semibold">Recent Projects</h2>
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
