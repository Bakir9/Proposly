import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getProjects, createProject } from '@/api/projects'
import type { ProjectSummary } from '@/api/projects'
import { getClients } from '@/api/clients'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Dialog } from '@/components/ui/dialog'
import {
  FolderKanban, Plus, Search, MoreHorizontal,
  TrendingUp, DollarSign, Users, AlertTriangle,
} from 'lucide-react'

const STATUS_LABELS: Record<string, string> = {
  Planning: 'Planning',
  Active: 'Active',
  OnHold: 'On Hold',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
}

const STATUS_BADGE: Record<string, string> = {
  Planning: 'bg-blue-500/15 text-blue-400 border-blue-500/20',
  Active: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20',
  OnHold: 'bg-amber-500/15 text-amber-400 border-amber-500/20',
  Completed: 'bg-slate-500/15 text-slate-400 border-slate-500/20',
  Cancelled: 'bg-red-500/15 text-red-400 border-red-500/20',
}

const STATUS_DOT: Record<string, string> = {
  Planning: 'bg-blue-400',
  Active: 'bg-emerald-400',
  OnHold: 'bg-amber-400',
  Completed: 'bg-slate-400',
  Cancelled: 'bg-red-400',
}

const CLIENT_COLORS = [
  'bg-violet-500', 'bg-blue-500', 'bg-emerald-500', 'bg-amber-500',
  'bg-rose-500', 'bg-cyan-500', 'bg-pink-500', 'bg-indigo-500',
]

function clientColor(name: string) {
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) & 0xffff
  return CLIENT_COLORS[h % CLIENT_COLORS.length]
}

function clientInitials(name: string) {
  return name.split(' ').slice(0, 2).map(w => w[0]).join('').toUpperCase()
}

function taskProgress(p: ProjectSummary) {
  if (p.totalTasksCount === 0) return p.status === 'Completed' ? 100 : 0
  return Math.round((p.completedTasksCount / p.totalTasksCount) * 100)
}

function isAtRisk(p: ProjectSummary) {
  if (p.status === 'Completed' || p.status === 'Cancelled') return false
  if (p.deadline) {
    const now = new Date()
    const due = new Date(p.deadline)
    if (due < now) return true
  }
  return false
}

export function ProjectsPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: projects, isLoading, isError } = useQuery({
    queryKey: ['projects'],
    queryFn: getProjects,
  })

  const { data: clients } = useQuery({
    queryKey: ['clients'],
    queryFn: getClients,
  })
  const activeClients = (clients ?? []).filter(c => c.status === 'Active')

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!openMenuId) return
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpenMenuId(null)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [openMenuId])

  const [dialogOpen, setDialogOpen] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [clientId, setClientId] = useState('')
  const [budgetAmount, setBudgetAmount] = useState('')
  const [currency, setCurrency] = useState('EUR')
  const [startDate, setStartDate] = useState('')
  const [deadline, setDeadline] = useState('')

  const mutCreate = useMutation({
    mutationFn: () => createProject({
      name,
      description: description || undefined,
      clientId,
      budgetAmount: parseFloat(budgetAmount),
      currency,
      startDate,
      deadline: deadline || undefined,
    }),
    onSuccess: (newId) => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      setDialogOpen(false)
      setName('')
      setDescription('')
      setClientId('')
      setBudgetAmount('')
      setCurrency('EUR')
      setStartDate('')
      setDeadline('')
      navigate(`/projects/${newId}`)
    },
  })

  const list = projects ?? []

  const activeProjects = list.filter(p => p.status === 'Active').length
  const totalValue = list.reduce((s, p) => s + p.budgetAmount, 0)
  const totalMembers = list.reduce((s, p) => s + p.memberCount, 0)
  const atRiskCount = list.filter(isAtRisk).length

  const totalValueFormatted = totalValue >= 1_000_000
    ? `$${(totalValue / 1_000_000).toFixed(1)}M`
    : totalValue >= 1_000
      ? `$${(totalValue / 1_000).toFixed(0)}K`
      : `$${totalValue.toFixed(0)}`

  const filtered = list.filter(p => {
    const matchSearch = !search || p.name.toLowerCase().includes(search.toLowerCase()) || p.clientName.toLowerCase().includes(search.toLowerCase())
    const matchStatus = statusFilter === 'all' || p.status === statusFilter
    return matchSearch && matchStatus
  })

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <FolderKanban className="h-6 w-6 text-primary" />
            Project Portfolio
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage and track all your active client engagements
          </p>
        </div>
        <Button onClick={() => setDialogOpen(true)} className="gap-1.5">
          <Plus className="h-4 w-4" />
          Create New Project
        </Button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="rounded-xl border bg-card p-4 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Active Projects</span>
            <div className="h-8 w-8 rounded-lg bg-blue-500/10 flex items-center justify-center">
              <TrendingUp className="h-4 w-4 text-blue-400" />
            </div>
          </div>
          <p className="text-3xl font-bold">{activeProjects}</p>
          <p className="text-xs text-muted-foreground">{list.length} total projects</p>
        </div>

        <div className="rounded-xl border bg-card p-4 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Total Value</span>
            <div className="h-8 w-8 rounded-lg bg-emerald-500/10 flex items-center justify-center">
              <DollarSign className="h-4 w-4 text-emerald-400" />
            </div>
          </div>
          <p className="text-3xl font-bold">{totalValueFormatted}</p>
          <p className="text-xs text-muted-foreground">Across all engagements</p>
        </div>

        <div className="rounded-xl border bg-card p-4 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Resources Allocated</span>
            <div className="h-8 w-8 rounded-lg bg-violet-500/10 flex items-center justify-center">
              <Users className="h-4 w-4 text-violet-400" />
            </div>
          </div>
          <p className="text-3xl font-bold">{totalMembers}</p>
          <p className="text-xs text-muted-foreground">Team members total</p>
        </div>

        <div className="rounded-xl border bg-card p-4 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">At Risk</span>
            <div className="h-8 w-8 rounded-lg bg-red-500/10 flex items-center justify-center">
              <AlertTriangle className="h-4 w-4 text-red-400" />
            </div>
          </div>
          <p className="text-3xl font-bold">{atRiskCount}</p>
          <p className="text-xs text-muted-foreground">Past deadline</p>
        </div>
      </div>

      {/* Active Engagements */}
      <div className="rounded-xl border bg-card">
        <div className="p-4 border-b flex items-center justify-between gap-3">
          <div>
            <h2 className="font-semibold">Active Engagements</h2>
            <p className="text-xs text-muted-foreground">{filtered.length} projects</p>
          </div>
          <div className="flex items-center gap-2">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Search projects..."
                value={search}
                onChange={e => setSearch(e.target.value)}
                className="pl-8 h-8 text-sm w-52"
              />
            </div>
            <Select
              value={statusFilter}
              onChange={e => setStatusFilter(e.target.value)}
              className="h-8 text-sm w-36"
            >
              <option value="all">All Statuses</option>
              <option value="Planning">Planning</option>
              <option value="Active">Active</option>
              <option value="OnHold">On Hold</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </Select>
          </div>
        </div>

        {isLoading && (
          <div className="p-8 text-center text-muted-foreground text-sm">Loading projects…</div>
        )}
        {isError && (
          <div className="p-8 text-center text-destructive text-sm">Failed to load projects.</div>
        )}
        {!isLoading && !isError && filtered.length === 0 && (
          <div className="p-8 text-center text-muted-foreground text-sm">No projects found.</div>
        )}

        {filtered.length > 0 && (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Project Name</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Client</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Status</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide w-48">Timeline</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Value</th>
                <th className="px-4 py-3 w-10" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filtered.map(p => {
                const pct = taskProgress(p)
                const color = clientColor(p.clientName)
                const initials = clientInitials(p.clientName)
                const isMenu = openMenuId === p.id
                const risk = isAtRisk(p)

                return (
                  <tr
                    key={p.id}
                    className="hover:bg-muted/30 transition-colors cursor-pointer group"
                    onClick={() => navigate(`/projects/${p.id}`)}
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className={`h-2 w-2 rounded-full flex-shrink-0 ${STATUS_DOT[p.status] ?? 'bg-muted'}`} />
                        <div>
                          <p className="font-medium">{p.name}</p>
                          {risk && (
                            <span className="text-xs text-red-400 flex items-center gap-0.5">
                              <AlertTriangle className="h-3 w-3" /> Overdue
                            </span>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className={`h-7 w-7 rounded-full ${color} flex items-center justify-center text-white text-xs font-semibold flex-shrink-0`}>
                          {initials}
                        </span>
                        <span className="text-muted-foreground">{p.clientName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${STATUS_BADGE[p.status] ?? 'bg-muted text-muted-foreground border-border'}`}>
                        {STATUS_LABELS[p.status] ?? p.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 w-48">
                      <div className="space-y-1">
                        <div className="flex items-center justify-between text-xs text-muted-foreground">
                          <span>{pct}% Complete</span>
                          <span>{p.completedTasksCount}/{p.totalTasksCount}</span>
                        </div>
                        <div className="h-1.5 rounded-full bg-muted overflow-hidden">
                          <div
                            className={`h-full rounded-full transition-all ${pct >= 75 ? 'bg-emerald-500' : pct >= 40 ? 'bg-blue-500' : 'bg-amber-500'}`}
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <span className="font-medium tabular-nums">
                        {p.budgetAmount.toLocaleString('de-AT', { style: 'currency', currency: p.currency })}
                      </span>
                    </td>
                    <td className="px-4 py-3 relative" onClick={e => e.stopPropagation()}>
                      <button
                        className="h-7 w-7 rounded flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                        onClick={() => setOpenMenuId(isMenu ? null : p.id)}
                      >
                        <MoreHorizontal className="h-4 w-4" />
                      </button>
                      {isMenu && (
                        <div
                          ref={menuRef}
                          className="absolute right-4 top-full z-50 mt-1 w-40 rounded-lg border shadow-lg py-1 text-sm"
                          style={{ background: 'hsl(var(--card))', isolation: 'isolate' }}
                        >
                          <button
                            className="w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors"
                            onClick={() => { navigate(`/projects/${p.id}`); setOpenMenuId(null) }}
                          >
                            View Details
                          </button>
                          <button
                            className="w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors"
                            onClick={() => { navigate(`/projects/${p.id}?tab=tasks`); setOpenMenuId(null) }}
                          >
                            View Tasks
                          </button>
                          <button
                            className="w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors"
                            onClick={() => { navigate(`/projects/${p.id}?tab=team`); setOpenMenuId(null) }}
                          >
                            View Team
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>

      {/* Create Project Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} title="New Project">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Project Name</Label>
            <Input value={name} onChange={e => setName(e.target.value)} placeholder="Project name" />
          </div>
          <div className="space-y-1">
            <Label>Client</Label>
            <Select value={clientId} onChange={e => setClientId(e.target.value)}>
              <option value="">Select a client…</option>
              {activeClients.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Budget</Label>
              <Input type="number" step="0.01" value={budgetAmount} onChange={e => setBudgetAmount(e.target.value)} placeholder="0.00" />
            </div>
            <div className="space-y-1">
              <Label>Currency</Label>
              <Select value={currency} onChange={e => setCurrency(e.target.value)}>
                <option value="EUR">EUR</option>
                <option value="USD">USD</option>
                <option value="GBP">GBP</option>
                <option value="CHF">CHF</option>
              </Select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Start Date</Label>
              <Input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Deadline (optional)</Label>
              <Input type="date" value={deadline} onChange={e => setDeadline(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1">
            <Label>Description (optional)</Label>
            <Textarea value={description} onChange={e => setDescription(e.target.value)} placeholder="Project description…" />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button
              onClick={() => mutCreate.mutate()}
              disabled={mutCreate.isPending || !name.trim() || !clientId || !budgetAmount || !startDate}
            >
              {mutCreate.isPending ? 'Creating…' : 'Create Project'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
