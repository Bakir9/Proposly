import { useMemo, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ChevronDown, Plus, Search } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  updateTask,
  updateTaskStatus,
  type ProjectDetail,
  type ProjectTask,
  type TaskStatus,
} from '@/api/projects'

const MEMBER_COLORS = ['#3b82f6', '#22c55e', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4']

const STATUS_CHIP: Record<TaskStatus, { label: string; cls: string }> = {
  Todo: { label: 'To do', cls: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300' },
  InProgress: { label: 'In progress', cls: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300' },
  Done: { label: 'Done', cls: 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-400' },
}

const STATUS_FILTERS = [
  { id: 'open', label: 'All open' },
  { id: 'Todo', label: 'To do' },
  { id: 'InProgress', label: 'In progress' },
  { id: 'Done', label: 'Done' },
  { id: 'all', label: 'All' },
] as const

const GROUP_BYS = [
  { id: 'milestone', label: 'Milestone' },
  { id: 'assignee', label: 'Assignee' },
  { id: 'status', label: 'Status' },
] as const

type StatusFilter = (typeof STATUS_FILTERS)[number]['id']
type GroupBy = (typeof GROUP_BYS)[number]['id']
type BulkPanel = 'status' | 'assign' | 'dates' | 'milestone' | null

const fmtShort = (iso: string) => new Date(iso).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' })
const fmtHours = (n: number) => `${n.toLocaleString('de-AT', { maximumFractionDigits: 1 })} h`

/** Full task update payload from the current task, so a partial patch never clears fields. */
function patchPayload(t: ProjectTask, patch: Partial<Parameters<typeof updateTask>[2]>) {
  return {
    title: t.title,
    description: t.description ?? undefined,
    estimatedHours: t.estimatedHours ?? undefined,
    actualHours: t.actualHours ?? undefined,
    startDate: t.startDate ? t.startDate.slice(0, 10) : undefined,
    dueDate: t.dueDate ? t.dueDate.slice(0, 10) : undefined,
    milestoneId: t.milestoneId ?? undefined,
    assignedMemberId: t.assignedMemberId ?? undefined,
    ...patch,
  }
}

interface Props {
  project: ProjectDetail
  canManage: boolean
  onNewTask: () => void
  onOpenTask: (task: ProjectTask) => void
}

/** Plan › List: grouped task table with search, filters and a bulk-edit bar (PlanList design). */
export function PlanTaskListTab({ project: p, canManage, onNewTask, onOpenTask }: Props) {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('open')
  const [groupBy, setGroupBy] = useState<GroupBy>('milestone')
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [panel, setPanel] = useState<BulkPanel>(null)
  const [bulkAssignee, setBulkAssignee] = useState('')
  const [bulkMilestone, setBulkMilestone] = useState('')
  const [bulkStart, setBulkStart] = useState('')
  const [bulkDue, setBulkDue] = useState('')

  const memberIndex = useMemo(() => new Map(p.members.map((m, i) => [m.id, i])), [p.members])
  const taskById = useMemo(() => new Map(p.tasks.map(t => [t.id, t])), [p.tasks])
  const loggedByTask = useMemo(() => {
    const out = new Map<string, number>()
    for (const e of p.timeEntries) if (e.taskId) out.set(e.taskId, (out.get(e.taskId) ?? 0) + e.hoursWorked)
    return out
  }, [p.timeEntries])

  const q = search.trim().toLowerCase()
  const visible = p.tasks.filter(t => {
    if (statusFilter === 'open' && t.status === 'Done') return false
    if (statusFilter !== 'open' && statusFilter !== 'all' && t.status !== statusFilter) return false
    if (q && !t.title.toLowerCase().includes(q) && !(t.description ?? '').toLowerCase().includes(q)) return false
    return true
  })

  const groups = useMemo(() => {
    const byKey = new Map<string, { name: string; rows: ProjectTask[] }>()
    const add = (key: string, name: string, t: ProjectTask) => {
      if (!byKey.has(key)) byKey.set(key, { name, rows: [] })
      byKey.get(key)!.rows.push(t)
    }
    for (const t of visible) {
      if (groupBy === 'milestone') {
        const m = t.milestoneId ? p.milestones.find(x => x.id === t.milestoneId) : null
        add(t.milestoneId ?? 'none', m?.title ?? 'No milestone', t)
      } else if (groupBy === 'assignee') {
        add(t.assignedMemberId ?? 'none', t.assignedMemberName ?? 'Unassigned', t)
      } else {
        add(t.status, STATUS_CHIP[t.status].label, t)
      }
    }
    return [...byKey.entries()]
      .sort((a, b) => (a[0] === 'none' ? 1 : b[0] === 'none' ? -1 : a[1].name.localeCompare(b[1].name)))
      .map(([key, g]) => ({
        key,
        name: g.name,
        rows: g.rows,
        done: g.rows.filter(r => r.status === 'Done').length,
        estimate: g.rows.reduce((s, r) => s + (r.estimatedHours ?? 0), 0),
        logged: g.rows.reduce((s, r) => s + (loggedByTask.get(r.id) ?? 0), 0),
      }))
  }, [visible, groupBy, p.milestones, loggedByTask])

  const toggle = (id: string) =>
    setSelected(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })

  const clearSelection = () => { setSelected(new Set()); setPanel(null) }

  const bulk = useMutation({
    mutationFn: async (apply: (t: ProjectTask) => Promise<unknown>) => {
      // Sequential on purpose — parallel writes race over the same aggregate.
      for (const id of selected) {
        const t = taskById.get(id)
        if (t) await apply(t)
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['project', p.id] })
      queryClient.invalidateQueries({ queryKey: ['project-overview', p.id] })
      toast.success(`${selected.size} task${selected.size > 1 ? 's' : ''} updated`)
      clearSelection()
    },
  })

  const runBulk = (apply: (t: ProjectTask) => Promise<unknown>) => bulk.mutate(apply)

  const bulkBtn = 'min-h-[34px] rounded-[7px] border border-slate-600 px-3 text-[13px] font-semibold text-white hover:bg-slate-700 disabled:opacity-50'

  return (
    <div className="space-y-3">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <label className="flex min-h-[38px] items-center gap-2 rounded-lg border bg-card px-3 text-muted-foreground">
            <Search className="h-4 w-4" />
            <input
              type="search"
              aria-label="Search tasks"
              placeholder="Search tasks"
              value={search}
              onChange={e => setSearch(e.target.value)}
              className="w-44 border-0 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground/70"
            />
          </label>
          <label className="flex min-h-[38px] items-center gap-2 rounded-lg border bg-card px-3 text-[13px] text-muted-foreground">
            <span>Status</span>
            <select
              value={statusFilter}
              onChange={e => setStatusFilter(e.target.value as StatusFilter)}
              className="border-0 bg-transparent text-sm text-foreground outline-none"
            >
              {STATUS_FILTERS.map(f => <option key={f.id} value={f.id}>{f.label}</option>)}
            </select>
          </label>
          <label className="flex min-h-[38px] items-center gap-2 rounded-lg border bg-card px-3 text-[13px] text-muted-foreground">
            <span>Group by</span>
            <select
              value={groupBy}
              onChange={e => setGroupBy(e.target.value as GroupBy)}
              className="border-0 bg-transparent text-sm text-foreground outline-none"
            >
              {GROUP_BYS.map(g => <option key={g.id} value={g.id}>{g.label}</option>)}
            </select>
          </label>
        </div>
        <Button onClick={onNewTask}><Plus className="mr-1 h-4 w-4" />New task</Button>
      </div>

      {/* Bulk bar */}
      {canManage && selected.size > 0 && (
        <div className="rounded-[10px] bg-slate-900 py-2 pr-2.5 pl-4 text-white dark:bg-slate-800">
          <div className="flex flex-wrap items-center gap-2">
            <span className="mr-2 font-semibold">
              {selected.size} task{selected.size > 1 ? 's' : ''} selected
            </span>
            <button className={bulkBtn} onClick={() => setPanel(panel === 'status' ? null : 'status')}>Set status</button>
            <button className={bulkBtn} onClick={() => setPanel(panel === 'assign' ? null : 'assign')}>Assign</button>
            <button className={bulkBtn} onClick={() => setPanel(panel === 'dates' ? null : 'dates')}>Set dates</button>
            <button className={bulkBtn} onClick={() => setPanel(panel === 'milestone' ? null : 'milestone')}>Link milestone</button>
            <button onClick={clearSelection} className="ml-auto min-h-[34px] px-3 text-[13px] font-semibold text-slate-300 hover:text-white">
              Clear
            </button>
          </div>
          {panel === 'status' && (
            <div className="mt-2 flex flex-wrap items-center gap-2 border-t border-slate-700 pt-2">
              <span className="text-[13px] text-slate-300">Set all to:</span>
              {(['Todo', 'InProgress'] as TaskStatus[]).map(s => (
                <button key={s} disabled={bulk.isPending} className={bulkBtn}
                  onClick={() => runBulk(t => updateTaskStatus(p.id, t.id, s))}>
                  {STATUS_CHIP[s].label}
                </button>
              ))}
              <span className="text-xs text-slate-400">Done needs actual hours — set it per task.</span>
            </div>
          )}
          {panel === 'assign' && (
            <div className="mt-2 flex flex-wrap items-center gap-2 border-t border-slate-700 pt-2">
              <select value={bulkAssignee} onChange={e => setBulkAssignee(e.target.value)}
                className="min-h-[34px] rounded-[7px] border border-slate-600 bg-slate-900 px-2 text-[13px] text-white dark:bg-slate-800">
                <option value="">Pick a member…</option>
                {p.members.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
              </select>
              <button disabled={!bulkAssignee || bulk.isPending} className={bulkBtn}
                onClick={() => runBulk(t => updateTask(p.id, t.id, patchPayload(t, { assignedMemberId: bulkAssignee })))}>
                Apply
              </button>
            </div>
          )}
          {panel === 'dates' && (
            <div className="mt-2 flex flex-wrap items-center gap-2 border-t border-slate-700 pt-2">
              <input type="date" value={bulkStart} onChange={e => setBulkStart(e.target.value)}
                className="min-h-[34px] rounded-[7px] border border-slate-600 bg-slate-900 px-2 text-[13px] text-white dark:bg-slate-800" />
              <span className="text-slate-400">–</span>
              <input type="date" value={bulkDue} onChange={e => setBulkDue(e.target.value)}
                className="min-h-[34px] rounded-[7px] border border-slate-600 bg-slate-900 px-2 text-[13px] text-white dark:bg-slate-800" />
              <button disabled={!bulkStart || !bulkDue || bulk.isPending} className={bulkBtn}
                onClick={() => runBulk(t => updateTask(p.id, t.id, patchPayload(t, { startDate: bulkStart, dueDate: bulkDue })))}>
                Apply
              </button>
            </div>
          )}
          {panel === 'milestone' && (
            <div className="mt-2 flex flex-wrap items-center gap-2 border-t border-slate-700 pt-2">
              <select value={bulkMilestone} onChange={e => setBulkMilestone(e.target.value)}
                className="min-h-[34px] rounded-[7px] border border-slate-600 bg-slate-900 px-2 text-[13px] text-white dark:bg-slate-800">
                <option value="">Pick a milestone…</option>
                {p.milestones.map(m => <option key={m.id} value={m.id}>{m.title}</option>)}
              </select>
              <button disabled={!bulkMilestone || bulk.isPending} className={bulkBtn}
                onClick={() => runBulk(t => updateTask(p.id, t.id, patchPayload(t, { milestoneId: bulkMilestone })))}>
                Apply
              </button>
            </div>
          )}
        </div>
      )}

      {/* Grouped table */}
      <section className="overflow-hidden rounded-xl border bg-card">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[920px] border-collapse text-[13.5px]">
            <thead>
              <tr className="border-b bg-muted/40 text-left text-xs text-muted-foreground">
                {canManage && <th className="w-11 py-2.5 pl-4"><span className="sr-only">Select</span></th>}
                <th className="px-2 py-2.5 font-semibold">Task</th>
                <th className="px-2 py-2.5 font-semibold">Status</th>
                <th className="px-2 py-2.5 font-semibold">Assignee</th>
                <th className="px-2 py-2.5 font-semibold">Dates</th>
                <th className="px-2 py-2.5 text-right font-semibold">Estimate</th>
                <th className="px-2 py-2.5 text-right font-semibold">Logged</th>
                <th className="py-2.5 pr-4 pl-2 font-semibold">Depends on</th>
              </tr>
            </thead>
            {groups.map(g => (
              <tbody key={g.key}>
                <tr className="border-b bg-muted/30">
                  {canManage && <td className="py-2.5 pl-4"><ChevronDown className="h-3.5 w-3.5 text-muted-foreground" /></td>}
                  <td colSpan={4} className="px-2 py-2.5">
                    <span className="font-bold">{g.name}</span>
                    <span className="ml-2.5 text-xs text-muted-foreground">
                      {g.rows.length} task{g.rows.length > 1 ? 's' : ''} · {g.done} done
                    </span>
                  </td>
                  <td className="px-2 py-2.5 text-right font-bold">{g.estimate > 0 ? fmtHours(g.estimate) : '—'}</td>
                  <td className="px-2 py-2.5 text-right font-bold">{fmtHours(g.logged)}</td>
                  <td className="py-2.5 pr-4 pl-2" />
                </tr>
                {g.rows.map(t => {
                  const logged = loggedByTask.get(t.id) ?? 0
                  const over = t.estimatedHours != null && logged > t.estimatedHours
                  const deps = t.blockedByTaskIds.map(id => taskById.get(id)?.title).filter(Boolean)
                  const chip = STATUS_CHIP[t.status]
                  const memberIdx = t.assignedMemberId ? memberIndex.get(t.assignedMemberId) ?? 0 : 0
                  const isSel = selected.has(t.id)
                  return (
                    <tr key={t.id} className={cn('border-b', isSel ? 'bg-primary/10' : 'hover:bg-muted/30')}>
                      {canManage && (
                        <td className="py-0 pl-4">
                          <input
                            type="checkbox"
                            checked={isSel}
                            onChange={() => toggle(t.id)}
                            aria-label={`Select ${t.title}`}
                            className="h-[17px] w-[17px] accent-[color:hsl(var(--primary))]"
                          />
                        </td>
                      )}
                      <td className="px-2 py-3 font-semibold">
                        <button onClick={() => onOpenTask(t)} className="text-left hover:text-primary hover:underline">
                          {t.title}
                        </button>
                      </td>
                      <td className="px-2 py-3">
                        <span className={cn('whitespace-nowrap rounded-md px-2 py-[3px] text-xs font-semibold', chip.cls)}>{chip.label}</span>
                      </td>
                      <td className="px-2 py-3">
                        {t.assignedMemberName ? (
                          <span className="flex items-center gap-2">
                            <span
                              className="flex h-[26px] w-[26px] items-center justify-center rounded-full text-[10px] font-bold text-white"
                              style={{ background: MEMBER_COLORS[memberIdx % MEMBER_COLORS.length] }}
                            >
                              {t.assignedMemberName.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase()}
                            </span>
                            <span className="whitespace-nowrap text-[13px] text-muted-foreground">{t.assignedMemberName.split(' ')[0]}</span>
                          </span>
                        ) : (
                          <span className="text-[12.5px] font-semibold text-amber-600 dark:text-amber-400">Unassigned</span>
                        )}
                      </td>
                      <td className={cn('whitespace-nowrap px-2 py-3', !(t.startDate && t.dueDate) && 'font-semibold text-amber-600 dark:text-amber-400')}>
                        {t.startDate && t.dueDate ? `${fmtShort(t.startDate)} – ${fmtShort(t.dueDate)}` : 'No dates'}
                      </td>
                      <td className={cn('whitespace-nowrap px-2 py-3 text-right', t.estimatedHours == null && 'font-semibold text-amber-600 dark:text-amber-400')}>
                        {t.estimatedHours != null ? fmtHours(t.estimatedHours) : 'Add estimate'}
                      </td>
                      <td className={cn('whitespace-nowrap px-2 py-3 text-right', over && 'font-bold text-destructive')}>
                        {fmtHours(logged)}
                      </td>
                      <td className="max-w-[220px] truncate py-3 pr-4 pl-2 text-muted-foreground" title={deps.join(', ')}>
                        {deps.length > 0 ? deps.join(', ') : '—'}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            ))}
          </table>
        </div>
        {groups.length === 0 && (
          <div className="py-10 text-center text-[13px] text-muted-foreground">
            {q || statusFilter !== 'open' ? 'No tasks match the current filters.' : 'No tasks yet — create the first one.'}
          </div>
        )}
      </section>
      {canManage && (
        <span className="text-[12.5px] text-muted-foreground">
          Tip: select rows and use the bar above to fix several tasks at once — e.g. give every unscheduled task dates in one go.
        </span>
      )}
    </div>
  )
}
