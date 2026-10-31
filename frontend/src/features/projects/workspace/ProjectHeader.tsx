import { useNavigate } from 'react-router-dom'
import { Pencil, Clock } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import type { BillingType, ProjectStatus } from '@/api/projects'
import { PROJECT_GROUPS, type ProjectGroupId } from './project-nav'

const STATUS_CHIP: Record<ProjectStatus, { label: string; cls: string }> = {
  Planning: { label: 'Planning', cls: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-400' },
  Active: { label: 'In progress', cls: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300' },
  OnHold: { label: 'On hold', cls: 'bg-slate-200 text-slate-700 dark:bg-slate-800 dark:text-slate-300' },
  Completed: { label: 'Done', cls: 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-400' },
  Cancelled: { label: 'Cancelled', cls: 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-400' },
}

const BILLING_CHIP: Record<BillingType, string> = {
  FixedPrice: 'Fixed price',
  TimeAndMaterials: 'Time & materials',
  Internal: 'Internal',
}

const MEMBER_COLORS = ['#3b82f6', '#22c55e', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4']

interface Props {
  projectId: string
  name: string
  clientName: string
  status: ProjectStatus
  billingType?: BillingType
  startDate: string
  deadline: string | null
  budgetAmount: number
  currency: string
  memberNames: string[]
  group: ProjectGroupId
  sub: string
  /** Hide money chips/meta for plain members. */
  showMoney: boolean
  onEditProject: () => void
}

const fmtDate = (iso: string) => new Date(iso).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' })
const fmtMoney = (n: number, c: string) => n.toLocaleString('de-AT', { style: 'currency', currency: c, maximumFractionDigits: 0 })

/** Breadcrumb, title + chips, meta line, group tabs and the sub-view switcher. */
export function ProjectHeader(p: Props) {
  const navigate = useNavigate()
  const status = STATUS_CHIP[p.status] ?? STATUS_CHIP.Planning
  const go = (group: string, sub?: string) => navigate(`/projects/${p.projectId}/${group}${sub ? `/${sub}` : ''}`)
  const activeGroup = PROJECT_GROUPS.find(g => g.id === p.group)!

  return (
    <header className="flex flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex min-w-0 flex-col gap-2">
          <div className="text-[13px] text-muted-foreground">
            <button onClick={() => navigate('/projects')} className="hover:text-foreground">Projects</button>
            {' / '}{p.clientName}
          </div>
          <div className="flex flex-wrap items-center gap-2.5">
            <h1 className="m-0 text-[26px] font-bold tracking-tight leading-tight">{p.name}</h1>
            <span className={cn('rounded-md px-2 py-1 text-xs font-semibold', status.cls)}>{status.label}</span>
            {p.billingType && p.showMoney && (
              <span className="rounded-md bg-indigo-100 px-2 py-1 text-xs font-semibold text-indigo-800 dark:bg-indigo-950 dark:text-indigo-300">
                {BILLING_CHIP[p.billingType]}
              </span>
            )}
          </div>
          <div className="flex flex-wrap items-center gap-4 text-[13px] text-muted-foreground">
            <span>{fmtDate(p.startDate)}{p.deadline ? ` – ${fmtDate(p.deadline)}` : ''}</span>
            {p.showMoney && <span>Budget {fmtMoney(p.budgetAmount, p.currency)}</span>}
            {p.memberNames.length > 0 && (
              <div className="flex">
                {p.memberNames.slice(0, 5).map((n, i) => (
                  <span
                    key={i}
                    title={n}
                    className="flex h-[26px] w-[26px] items-center justify-center rounded-full border-2 border-background text-[10px] font-bold text-white"
                    style={{ background: MEMBER_COLORS[i % MEMBER_COLORS.length], marginLeft: i > 0 ? -6 : 0 }}
                  >
                    {n.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase()}
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={p.onEditProject}>
            <Pencil className="mr-1.5 h-3.5 w-3.5" />Edit project
          </Button>
          <Button onClick={() => go('track', 'time')}>
            <Clock className="mr-1.5 h-3.5 w-3.5" />Log time
          </Button>
        </div>
      </div>

      {/* Group tabs */}
      <nav aria-label="Project sections" className="flex flex-wrap gap-1 border-b">
        {PROJECT_GROUPS.map(g => {
          const on = g.id === p.group
          return (
            <button
              key={g.id}
              onClick={() => go(g.id, g.subs[0].id)}
              className={cn(
                '-mb-px flex flex-col gap-0.5 px-4 py-2.5 text-left',
                on ? 'border-b-2 border-primary text-primary' : 'text-foreground hover:text-primary',
              )}
            >
              <span className={cn('text-sm', on ? 'font-bold' : 'font-semibold')}>{g.label}</span>
              <span className="text-[11.5px] text-muted-foreground">{g.caption}</span>
            </button>
          )
        })}
      </nav>

      {/* Sub-view switcher */}
      {activeGroup.subs.length > 1 && (
        <nav aria-label="Views" className="flex w-fit flex-wrap gap-[3px] rounded-[9px] bg-muted p-[3px]">
          {activeGroup.subs.map(s => (
            <button
              key={s.id}
              onClick={() => go(p.group, s.id)}
              className={cn(
                'inline-flex min-h-9 items-center rounded-[7px] px-3.5 text-[13px] font-semibold',
                s.id === p.sub ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
              )}
            >
              {s.label}
            </button>
          ))}
        </nav>
      )}
    </header>
  )
}
