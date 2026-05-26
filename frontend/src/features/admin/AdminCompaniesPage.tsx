import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getAllCompanies, adminUpdateCompanyPlan, type CompanyAdminSummary } from '@/api/admin'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog } from '@/components/ui/dialog'

type PlanTier = 'Free' | 'Starter' | 'Pro' | 'Business'

const PLAN_COLORS: Record<string, string> = {
  Free: 'bg-muted text-muted-foreground',
  Starter: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
  Pro: 'bg-violet-100 text-violet-800 dark:bg-violet-900/30 dark:text-violet-300',
  Business: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300',
}

const STATUS_COLORS: Record<string, string> = {
  Active: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
  Suspended: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
}

function usageLabel(current: number, max: number | null) {
  return max === null ? `${current} / ∞` : `${current} / ${max}`
}

function isExpired(expiresAt: string | null) {
  return expiresAt !== null && new Date(expiresAt) < new Date()
}

export function AdminCompaniesPage() {
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<CompanyAdminSummary | null>(null)
  const [planTier, setPlanTier] = useState<PlanTier>('Free')
  const [maxUsers, setMaxUsers] = useState('')
  const [maxProjects, setMaxProjects] = useState('')
  const [planExpiresAt, setPlanExpiresAt] = useState('')

  const { data: companies = [], isLoading } = useQuery({
    queryKey: ['admin-companies'],
    queryFn: getAllCompanies,
  })

  const { mutate: savePlan, isPending } = useMutation({
    mutationFn: () =>
      adminUpdateCompanyPlan(editing!.id, {
        planTier,
        maxUsers: maxUsers ? parseInt(maxUsers) : null,
        maxProjects: maxProjects ? parseInt(maxProjects) : null,
        planExpiresAt: planExpiresAt || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-companies'] })
      toast.success('Plan updated.')
      setEditing(null)
    },
    onError: () => toast.error('Failed to update plan.'),
  })

  function openEdit(company: CompanyAdminSummary) {
    setEditing(company)
    setPlanTier(company.planTier as PlanTier)
    setMaxUsers(company.maxUsers != null ? String(company.maxUsers) : '')
    setMaxProjects(company.maxProjects != null ? String(company.maxProjects) : '')
    setPlanExpiresAt(company.planExpiresAt ? company.planExpiresAt.slice(0, 10) : '')
  }

  return (
    <div className="p-6 space-y-6 max-w-5xl">
      <h1 className="text-2xl font-semibold">Companies</h1>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">All tenants ({companies.length})</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <p className="p-6 text-sm text-muted-foreground">Loading…</p>
          ) : companies.length === 0 ? (
            <p className="p-6 text-sm text-muted-foreground">No companies found.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-muted-foreground">
                    <th className="px-4 py-3 text-left font-medium">Company</th>
                    <th className="px-4 py-3 text-left font-medium">Status</th>
                    <th className="px-4 py-3 text-left font-medium">Plan</th>
                    <th className="px-4 py-3 text-left font-medium">Users</th>
                    <th className="px-4 py-3 text-left font-medium">Projects</th>
                    <th className="px-4 py-3 text-left font-medium">Expires</th>
                    <th className="px-4 py-3 text-left font-medium">Since</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody>
                  {companies.map(c => (
                    <tr key={c.id} className="border-b last:border-0 hover:bg-muted/40 transition-colors">
                      <td className="px-4 py-3 font-medium">{c.name}</td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${STATUS_COLORS[c.status] ?? 'bg-muted text-muted-foreground'}`}>
                          {c.status}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${PLAN_COLORS[c.planTier] ?? 'bg-muted text-muted-foreground'}`}>
                          {c.planTier}
                        </span>
                      </td>
                      <td className="px-4 py-3 tabular-nums">{usageLabel(c.userCount, c.maxUsers)}</td>
                      <td className="px-4 py-3 tabular-nums">{usageLabel(c.projectCount, c.maxProjects)}</td>
                      <td className="px-4 py-3">
                        {c.planExpiresAt ? (
                          <span className={isExpired(c.planExpiresAt) ? 'text-destructive font-medium' : ''}>
                            {new Date(c.planExpiresAt).toLocaleDateString()}
                            {isExpired(c.planExpiresAt) && ' (expired)'}
                          </span>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">
                        {new Date(c.createdAt).toLocaleDateString()}
                      </td>
                      <td className="px-4 py-3">
                        <Button size="sm" variant="outline" onClick={() => openEdit(c)}>
                          Edit plan
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {editing && (
        <Dialog open title={`Edit plan — ${editing.name}`} onClose={() => setEditing(null)}>
          <div className="grid grid-cols-1 gap-4">
            <div className="space-y-1.5">
              <Label>Plan tier</Label>
              <select
                value={planTier}
                onChange={e => setPlanTier(e.target.value as PlanTier)}
                className="h-9 w-full rounded-md border bg-background px-3 text-sm"
              >
                {(['Free', 'Starter', 'Pro', 'Business'] as PlanTier[]).map(t => (
                  <option key={t} value={t}>{t}</option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Max users</Label>
                <Input
                  type="number"
                  min="1"
                  value={maxUsers}
                  onChange={e => setMaxUsers(e.target.value)}
                  placeholder="blank = unlimited"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Max projects</Label>
                <Input
                  type="number"
                  min="1"
                  value={maxProjects}
                  onChange={e => setMaxProjects(e.target.value)}
                  placeholder="blank = unlimited"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Plan expires (optional)</Label>
              <Input
                type="date"
                value={planExpiresAt}
                onChange={e => setPlanExpiresAt(e.target.value)}
              />
            </div>

            <div className="flex gap-2 pt-2">
              <Button onClick={() => savePlan()} disabled={isPending}>
                {isPending ? 'Saving…' : 'Save plan'}
              </Button>
              <Button variant="outline" onClick={() => setEditing(null)}>Cancel</Button>
            </div>
          </div>
        </Dialog>
      )}
    </div>
  )
}
