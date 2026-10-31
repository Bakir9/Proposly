import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Check, Pencil } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  getProjectOverview,
  setBillingType,
  updateProjectMember,
  type BillingType,
  type OverviewTeamMember,
} from '@/api/projects'

const fmtMoney = (n: number, c: string, digits = 0) =>
  n.toLocaleString('de-AT', { style: 'currency', currency: c, minimumFractionDigits: digits, maximumFractionDigits: digits })
const fmtHours = (n: number) => `${n.toLocaleString('de-AT', { maximumFractionDigits: 1 })} h`
const fmtDate = (iso: string) => new Date(iso).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' })

const MEMBER_COLORS = ['#3b82f6', '#22c55e', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4']

const BILLING_NOTES: Record<BillingType, string> = {
  FixedPrice:
    'Fixed price: the budget is your revenue. Every hour logged beyond plan comes straight out of margin — Proposly warns you before it does.',
  TimeAndMaterials:
    'Time & materials: revenue grows with billable hours. Rates and logged hours drive the projection.',
  Internal: 'Internal project: no revenue, so no profit. Proposly tracks cost against the budget only.',
}

interface Props {
  projectId: string
  /** Owner/Admin — may change billing type and member rates. */
  canManage: boolean
}

/** Redesigned Overview: KPI strip, setup checklist, profitability, milestones, team & allocation. */
export function ProjectOverviewTab({ projectId, canManage }: Props) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [editMember, setEditMember] = useState<OverviewTeamMember | null>(null)
  const [dismissed, setDismissed] = useState(() => {
    try { return localStorage.getItem(`checklist-dismissed-${projectId}`) === '1' } catch { return false }
  })

  const { data: ov, isLoading } = useQuery({
    queryKey: ['project-overview', projectId],
    queryFn: () => getProjectOverview(projectId),
  })

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ['project-overview', projectId] })
    queryClient.invalidateQueries({ queryKey: ['project', projectId] })
  }

  const billingMutation = useMutation({
    mutationFn: (type: BillingType) => setBillingType(projectId, type),
    onSuccess: () => { toast.success('Billing type updated'); refresh() },
  })

  if (isLoading || !ov) return <div className="py-10 text-center text-sm text-muted-foreground">Loading overview…</div>

  const go = (path: string) => navigate(`/projects/${projectId}/${path}`)
  const k = ov.kpis
  const cl = ov.checklist
  const money = ov.canViewMoney

  // Setup checklist (spec §3) — raw counts from the API, wording built here.
  const steps = [
    {
      title: 'Assign team & rates',
      detail: cl.membersCount === 0 ? 'No members yet' : `${cl.membersCount} member${cl.membersCount > 1 ? 's' : ''}${cl.allRatesSet ? ' · cost rates set' : ' · cost rates missing'}`,
      done: cl.membersCount > 0 && cl.allRatesSet,
      action: 'Add team',
      href: 'overview/team',
    },
    {
      title: 'Create a milestone',
      detail: cl.milestonesCount > 0 ? `${cl.milestonesCount} milestone${cl.milestonesCount > 1 ? 's' : ''}` : 'Milestones give the plan checkpoints',
      done: cl.milestonesCount > 0,
      action: 'Add milestone',
      href: 'plan/milestones',
    },
    {
      title: 'Give every task dates',
      detail: cl.openTasks === 0 ? 'No open tasks yet' : `${cl.scheduledOpenTasks} of ${cl.openTasks} scheduled`,
      done: cl.openTasks > 0 && cl.scheduledOpenTasks === cl.openTasks,
      action: 'Schedule',
      href: 'plan/board',
    },
    {
      title: 'Estimate tasks',
      detail: cl.openTasks === 0 ? 'Needed for burndown and forecast' : `${cl.estimatedOpenTasks} of ${cl.openTasks} estimated — needed for burndown and forecast`,
      done: cl.openTasks > 0 && cl.estimatedOpenTasks === cl.openTasks,
      action: 'Add estimates',
      href: 'plan/board',
    },
    {
      title: 'Log the first hours',
      detail: cl.timeEntriesCount > 0 ? `${cl.timeEntriesCount} entries` : 'Profitability switches from projected to actual',
      done: cl.timeEntriesCount > 0,
      action: 'Log time',
      href: 'track/time',
    },
  ]
  const doneCount = steps.filter(s => s.done).length
  const showChecklist = !dismissed && doneCount < steps.length

  const dismiss = () => {
    setDismissed(true)
    try { localStorage.setItem(`checklist-dismissed-${projectId}`, '1') } catch { /* ignore */ }
  }

  const p = ov.profitability
  const billing = ov.billingType
  const isInternal = billing === 'Internal'
  const costShare = p && p.revenue != null && p.revenue > 0
    ? Math.min(100, Math.round(((p.isProjected ? p.plannedLaborCost + p.actualExpenses : p.forecastCost) / (billing === 'Internal' ? k.budgetAmount : p.revenue)) * 100))
    : p && isInternal && k.budgetAmount > 0
      ? Math.min(100, Math.round(((p.isProjected ? p.plannedLaborCost + p.actualExpenses : p.forecastCost) / k.budgetAmount) * 100))
      : 0

  return (
    <div className="space-y-4">
      {/* KPI strip */}
      <div className="grid gap-4 [grid-template-columns:repeat(auto-fit,minmax(220px,1fr))]">
        <Kpi label="Timeline" value={k.daysLeft != null ? `${k.daysLeft} days left` : 'No deadline'}
          bar={k.totalDays ? Math.round(((k.dayOfTotal ?? 0) / k.totalDays) * 100) : 0}
          sub={k.totalDays ? `Day ${k.dayOfTotal} of ${k.totalDays}` : 'Set a deadline in Edit project'} />
        <Kpi label="Budget spent"
          value={money && k.spentAmount != null ? fmtMoney(k.spentAmount, ov.currency) : money ? fmtMoney(0, ov.currency) : '—'}
          bar={money && k.spentAmount != null && k.budgetAmount > 0 ? Math.min(100, Math.round((k.spentAmount / k.budgetAmount) * 100)) : 0}
          sub={money ? `of ${fmtMoney(k.budgetAmount, ov.currency)}${(k.spentAmount ?? 0) === 0 ? ' · nothing logged yet' : ''}` : 'Hours visible, money is admin-only'} />
        <Kpi label="Tasks" value={`${k.tasksDone} / ${k.tasksTotal} done`}
          bar={k.tasksTotal > 0 ? Math.round((k.tasksDone / k.tasksTotal) * 100) : 0}
          sub={k.tasksWithoutDates > 0 ? `${k.tasksWithoutDates} task${k.tasksWithoutDates > 1 ? 's have' : ' has'} no dates` : k.tasksTotal === 0 ? 'No tasks yet' : 'All tasks scheduled'}
          warn={k.tasksWithoutDates > 0} />
        <Kpi label="Next milestone" value={k.nextMilestoneTitle ?? 'None planned'}
          bar={0}
          sub={k.nextMilestoneTitle
            ? `${k.nextMilestoneDue ? fmtDate(k.nextMilestoneDue) : ''}${k.nextMilestoneLinkedTasks === 0 ? ' · no tasks linked' : ` · ${k.nextMilestoneLinkedTasks} tasks`}`
            : 'Create one under Plan › Milestones'}
          warn={!!k.nextMilestoneTitle && k.nextMilestoneLinkedTasks === 0} />
      </div>

      <div className="flex flex-wrap items-stretch gap-4">
        {/* Setup checklist */}
        {showChecklist && (
          <section className="min-w-0 flex-[3_1_420px] rounded-xl border bg-card p-6 flex flex-col gap-4">
            <div className="flex flex-wrap items-end justify-between gap-3">
              <div>
                <h2 className="m-0 text-[17px] font-bold">Get this project ready</h2>
                <span className="text-[13px] text-muted-foreground">Finish these and your charts start telling the truth.</span>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-[13px] font-semibold text-primary">{doneCount} of {steps.length} done</span>
                <button onClick={dismiss} className="text-xs text-muted-foreground hover:text-foreground">Dismiss</button>
              </div>
            </div>
            <div className="h-1.5 rounded-full bg-muted">
              <div className="h-1.5 rounded-full bg-primary transition-all" style={{ width: `${(doneCount / steps.length) * 100}%` }} />
            </div>
            <ol className="m-0 flex list-none flex-col p-0">
              {steps.map((s, i) => (
                <li key={s.title} className="flex items-center gap-3.5 border-t py-3 first:border-t-0">
                  {s.done ? (
                    <span className="flex h-[26px] w-[26px] flex-none items-center justify-center rounded-full bg-green-600 text-white">
                      <Check className="h-3.5 w-3.5" strokeWidth={3} />
                    </span>
                  ) : (
                    <span className="flex h-[26px] w-[26px] flex-none items-center justify-center rounded-full border-2 border-border text-xs font-bold text-muted-foreground">
                      {i + 1}
                    </span>
                  )}
                  <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <span className={cn('text-sm font-semibold', s.done && 'text-muted-foreground line-through')}>{s.title}</span>
                    <span className="text-[12.5px] text-muted-foreground">{s.detail}</span>
                  </div>
                  {!s.done && (
                    <button
                      onClick={() => go(s.href)}
                      className="inline-flex min-h-9 items-center whitespace-nowrap rounded-lg border px-3 text-[13px] font-semibold text-primary hover:bg-muted"
                    >
                      {s.action}
                    </button>
                  )}
                </li>
              ))}
            </ol>
          </section>
        )}

        {/* Profitability — admins only */}
        {money && p && (
          <section className="min-w-0 flex-[2_1_340px] rounded-xl border bg-card p-6 flex flex-col gap-4">
            <div className="flex items-center justify-between gap-2">
              <h2 className="m-0 text-[17px] font-bold">Profitability</h2>
              <span className="rounded-md bg-muted px-2 py-1 text-[11.5px] font-semibold text-muted-foreground">
                {p.isProjected ? 'Projected' : 'Forecast'}
              </span>
            </div>
            {canManage && (
              <div role="group" aria-label="Billing type" className="flex gap-[3px] rounded-[9px] bg-muted p-[3px]">
                {([['FixedPrice', 'Fixed price'], ['TimeAndMaterials', 'Time & materials'], ['Internal', 'Internal']] as [BillingType, string][]).map(([id, label]) => (
                  <button
                    key={id}
                    aria-pressed={billing === id}
                    disabled={billingMutation.isPending}
                    onClick={() => billing !== id && billingMutation.mutate(id)}
                    className={cn(
                      'min-h-9 flex-1 rounded-[7px] text-[12.5px] font-semibold',
                      billing === id ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
            )}
            <div className="flex flex-col gap-2.5 text-sm">
              <Row
                label={billing === 'FixedPrice' ? 'Revenue · fixed price' : billing === 'TimeAndMaterials' ? 'Revenue · billable hours' : 'Internal budget'}
                sub={billing === 'FixedPrice' ? 'The project budget' : billing === 'TimeAndMaterials' ? `${fmtHours(p.plannedHours)} × member bill rates` : 'No client revenue'}
                value={fmtMoney(billing === 'Internal' ? k.budgetAmount : p.revenue ?? 0, ov.currency)}
              />
              <Row
                label={p.isProjected ? 'Projected labor cost' : 'Forecast labor cost'}
                sub={`${fmtHours(p.plannedHours)} allocated × cost rates${p.isProjected ? '' : ` · ${fmtHours(p.loggedHours)} logged`}`}
                value={fmtMoney(p.isProjected ? p.plannedLaborCost : p.forecastCost - Math.max(p.actualExpenses, 0), ov.currency)}
                amber
              />
              <Row label="Expenses" value={fmtMoney(p.actualExpenses, ov.currency)} />
            </div>
            <div className="flex flex-col gap-2.5 rounded-[10px] bg-muted/50 p-4">
              <div className="flex flex-wrap items-baseline justify-between gap-2">
                <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  {isInternal ? 'Budget headroom' : p.isProjected ? 'Projected profit' : 'Forecast profit'}
                </span>
                <span className="text-[12.5px] font-semibold text-muted-foreground">
                  {p.marginPercent != null ? `${p.marginPercent.toLocaleString('de-AT')} % margin` : isInternal ? `${costShare} % of budget planned` : ''}
                </span>
              </div>
              <span className={cn('text-[28px] font-bold', p.result >= 0 ? 'text-green-600 dark:text-green-400' : 'text-destructive')}>
                {fmtMoney(p.result, ov.currency)}
              </span>
              <div className="flex h-2.5 overflow-hidden rounded-[5px] bg-green-200 dark:bg-green-900">
                <div className="bg-[#EA8C2C]" style={{ width: `${costShare}%` }} />
              </div>
              <div className="flex gap-3.5 text-xs text-muted-foreground">
                <span className="flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-sm bg-[#EA8C2C]" />Cost</span>
                <span className="flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-sm bg-green-300 dark:bg-green-800" />{isInternal ? 'Headroom' : 'Profit'}</span>
              </div>
            </div>
            <p className="m-0 text-[12.5px] leading-normal text-muted-foreground">{BILLING_NOTES[billing]}</p>
          </section>
        )}
      </div>

      <div className="flex flex-wrap gap-4">
        {/* Milestones */}
        <section className="min-w-0 flex-[2_1_340px] rounded-xl border bg-card p-6 flex flex-col gap-3.5">
          <div className="flex items-center justify-between">
            <h2 className="m-0 text-[17px] font-bold">Milestones</h2>
            <button onClick={() => go('plan/milestones')} className="text-[13px] font-semibold text-primary hover:underline">View all</button>
          </div>
          {ov.milestones.length === 0 && (
            <div className="rounded-[10px] border border-dashed py-6 text-center text-[13px] text-muted-foreground">
              No milestones yet — create one under Plan › Milestones.
            </div>
          )}
          {ov.milestones.map(m => (
            <div key={m.id} className="flex items-start gap-3.5 rounded-[10px] border p-3.5">
              <span className={cn('mt-1 h-3.5 w-3.5 flex-none rotate-45 rounded-[2px]', m.isCompleted ? 'bg-green-600' : 'bg-amber-500')} />
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <span className="font-semibold">{m.title}</span>
                <span className="text-[12.5px] text-muted-foreground">
                  {m.isCompleted ? 'Reached' : `Due ${fmtDate(m.dueDate)}`}
                  {m.linkedTasks > 0 ? ` · ${m.doneLinkedTasks}/${m.linkedTasks} tasks done` : ''}
                </span>
                {!m.isCompleted && m.linkedTasks === 0 && (
                  <span className="mt-1 rounded-md bg-amber-50 px-2 py-1.5 text-[12.5px] text-amber-800 dark:bg-amber-950/50 dark:text-amber-400">
                    No tasks linked — progress can't be measured yet.
                  </span>
                )}
              </div>
              {!m.isCompleted && m.linkedTasks === 0 && (
                <button
                  onClick={() => go('plan/board')}
                  className="inline-flex min-h-9 items-center whitespace-nowrap rounded-lg border px-3 text-[13px] font-semibold text-primary hover:bg-muted"
                >
                  Link tasks
                </button>
              )}
            </div>
          ))}
        </section>

        {/* Team & allocation */}
        <section className="min-w-0 flex-[3_1_420px] rounded-xl border bg-card p-6 flex flex-col gap-1.5">
          <div className="mb-2 flex items-center justify-between">
            <h2 className="m-0 text-[17px] font-bold">Team & allocation</h2>
            <span className="text-[13px] text-muted-foreground">{fmtHours(ov.plannedHours)} planned</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[480px] border-collapse text-[13.5px]">
              <thead>
                <tr className="text-left text-xs text-muted-foreground">
                  <th className="py-1.5 font-semibold">Member</th>
                  <th className="px-2 py-1.5 font-semibold">Role</th>
                  {money && <th className="px-2 py-1.5 text-right font-semibold">Cost rate</th>}
                  {money && <th className="px-2 py-1.5 text-right font-semibold">Bill rate</th>}
                  <th className="py-1.5 text-right font-semibold">Allocated</th>
                  {canManage && <th className="py-1.5" />}
                </tr>
              </thead>
              <tbody>
                {ov.team.map((t, i) => (
                  <tr key={t.memberId} className="border-t">
                    <td className="py-2.5">
                      <div className="flex items-center gap-2.5">
                        <span
                          className="flex h-7 w-7 flex-none items-center justify-center rounded-full text-[11px] font-bold text-white"
                          style={{ background: MEMBER_COLORS[i % MEMBER_COLORS.length] }}
                        >
                          {t.name.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase()}
                        </span>
                        <span className="font-semibold">{t.name}</span>
                      </div>
                    </td>
                    <td className="px-2 py-2.5 text-muted-foreground">{t.role}</td>
                    {money && <td className="px-2 py-2.5 text-right">{t.costRate != null ? `${fmtMoney(t.costRate, ov.currency)}/h` : '—'}</td>}
                    {money && <td className="px-2 py-2.5 text-right">{t.billRate != null ? `${fmtMoney(t.billRate, ov.currency)}/h` : '—'}</td>}
                    <td className={cn('py-2.5 text-right font-semibold', t.allocatedHours === 0 && 'text-amber-600 dark:text-amber-400')}>
                      {t.allocatedHours > 0 ? fmtHours(t.allocatedHours) : 'Not set'}
                    </td>
                    {canManage && (
                      <td className="py-2.5 pl-2 text-right">
                        <button onClick={() => setEditMember(t)} title={`Edit ${t.name}`} className="rounded-md p-1.5 text-muted-foreground hover:bg-muted hover:text-foreground">
                          <Pencil className="h-3.5 w-3.5" />
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
                {ov.team.length === 0 && (
                  <tr><td colSpan={6} className="py-6 text-center text-[13px] text-muted-foreground">No members yet — add them under Overview › Team.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          {money && (
            <span className="text-xs text-muted-foreground">
              Cost rate = what an hour costs you. Bill rate = what the client pays on T&M projects.
            </span>
          )}
        </section>
      </div>

      {editMember && (
        <EditMemberDialog
          projectId={projectId}
          member={editMember}
          currency={ov.currency}
          onClose={() => setEditMember(null)}
          onSaved={() => { setEditMember(null); refresh() }}
        />
      )}
    </div>
  )
}

function Kpi({ label, value, sub, bar, warn }: { label: string; value: string; sub: string; bar: number; warn?: boolean }) {
  return (
    <div className="flex flex-col gap-1.5 rounded-xl border bg-card px-[18px] py-4">
      <span className="text-xs font-semibold text-muted-foreground">{label}</span>
      <span className="truncate text-[22px] font-bold" title={value}>{value}</span>
      <div className="h-1.5 rounded-full bg-muted">
        {bar > 0 && <div className="h-1.5 rounded-full bg-primary" style={{ width: `${Math.min(bar, 100)}%` }} />}
      </div>
      <span className={cn('text-xs', warn ? 'text-amber-600 dark:text-amber-400' : 'text-muted-foreground')}>{sub}</span>
    </div>
  )
}

function Row({ label, sub, value, amber }: { label: string; sub?: string; value: string; amber?: boolean }) {
  return (
    <div className="flex justify-between gap-3">
      <div className="flex flex-col">
        <span>{label}</span>
        {sub && <span className="text-xs text-muted-foreground">{sub}</span>}
      </div>
      <span className={cn('font-semibold', amber && 'text-amber-700 dark:text-amber-400')}>{value}</span>
    </div>
  )
}

function EditMemberDialog({
  projectId,
  member,
  currency,
  onClose,
  onSaved,
}: {
  projectId: string
  member: OverviewTeamMember
  currency: string
  onClose: () => void
  onSaved: () => void
}) {
  const [role, setRole] = useState(member.role)
  const [costRate, setCostRate] = useState(String(member.costRate ?? 0))
  const [billRate, setBillRate] = useState(String(member.billRate ?? 0))
  const [allocated, setAllocated] = useState(String(member.allocatedHours))

  const save = useMutation({
    mutationFn: () =>
      updateProjectMember(projectId, member.memberId, {
        role,
        costRate: parseFloat(costRate) || 0,
        billRate: parseFloat(billRate) || 0,
        allocatedHours: parseFloat(allocated) || 0,
      }),
    onSuccess: () => { toast.success(`${member.name} updated`); onSaved() },
  })

  return (
    <Dialog open onClose={onClose} title={`Edit ${member.name}`}>
      <div className="space-y-4">
        <div className="space-y-1.5">
          <Label>Role</Label>
          <Input value={role} onChange={e => setRole(e.target.value)} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <Label>Cost rate ({currency}/h)</Label>
            <Input type="number" min="0" step="0.5" value={costRate} onChange={e => setCostRate(e.target.value)} />
            <p className="text-[11px] text-muted-foreground">What an hour costs you</p>
          </div>
          <div className="space-y-1.5">
            <Label>Bill rate ({currency}/h)</Label>
            <Input type="number" min="0" step="0.5" value={billRate} onChange={e => setBillRate(e.target.value)} />
            <p className="text-[11px] text-muted-foreground">What the client pays (T&M)</p>
          </div>
        </div>
        <div className="space-y-1.5">
          <Label>Allocated hours</Label>
          <Input type="number" min="0" step="1" value={allocated} onChange={e => setAllocated(e.target.value)} />
          <p className="text-[11px] text-muted-foreground">Planned hours on this project — drives projected cost and capacity</p>
        </div>
        <p className="text-xs text-muted-foreground">
          Rate changes apply to future time entries only — logged hours keep their snapshot.
        </p>
        <div className="flex justify-end gap-2 pt-1">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={() => save.mutate()} disabled={save.isPending || !role.trim()}>
            {save.isPending ? 'Saving…' : 'Save'}
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
