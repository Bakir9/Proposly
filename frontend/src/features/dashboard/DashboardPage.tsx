import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  CalendarOff,
  CheckSquare,
  Clock,
  FileText,
  Flag,
  Mail,
  Plus,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/features/auth/AuthContext'
import {
  getDashboardHome,
  getWhosOut,
  type AttentionItem,
  type WhosOutRange,
} from '@/api/dashboard'

const fmtMoney = (n: number, c: string) =>
  n.toLocaleString('de-AT', { style: 'currency', currency: c, maximumFractionDigits: 0 })
const fmtHours = (n: number) => `${n.toLocaleString('de-AT', { maximumFractionDigits: 1 })} h`
const fmtDate = (iso: string) => new Date(iso).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' })
const fmtDateLong = (iso: string) =>
  new Date(iso).toLocaleDateString('de-AT', { weekday: 'short', day: '2-digit', month: '2-digit' })

function isoWeek(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  const day = d.getUTCDay() || 7
  d.setUTCDate(d.getUTCDate() + 4 - day)
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  return Math.ceil(((d.getTime() - yearStart.getTime()) / 86_400_000 + 1) / 7)
}

const TONE_ICON: Record<AttentionItem['tone'], string> = {
  blue: 'bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300',
  amber: 'bg-amber-100 text-amber-700 dark:bg-amber-950 dark:text-amber-400',
  red: 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-400',
  violet: 'bg-violet-100 text-violet-700 dark:bg-violet-950 dark:text-violet-400',
}

const KIND_ICON: Record<string, typeof Mail> = {
  approvals: CheckSquare,
  timesheet: Clock,
  offer_no_reply: Mail,
  milestone_overdue: Flag,
  offer_expiring: FileText,
  contract_expiring: Clock,
  tasks_overdue: Flag,
}

const OFFER_CHIP: Record<string, string> = {
  Draft: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Sent: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300',
  Accepted: 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-400',
  Rejected: 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-400',
  Expired: 'bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400',
}

const PROJECT_CHIP: Record<string, { label: string; cls: string }> = {
  Active: { label: 'In progress', cls: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300' },
  Planning: { label: 'Planning', cls: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-400' },
  OnHold: { label: 'On hold', cls: 'bg-slate-200 text-slate-700 dark:bg-slate-800 dark:text-slate-300' },
}

const AVATAR_COLORS = ['#2563eb', '#7c3aed', '#0d9488', '#b45309', '#15803d', '#db2777']

const initials = (name: string) => name.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase()

export function DashboardPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const [range, setRange] = useState<WhosOutRange>('today')

  // staleTime 0 + refetchOnMount: opening the dashboard always shows the current state —
  // an offer accepted a moment ago must not linger as "waiting for reply".
  const { data: home } = useQuery({
    queryKey: ['dashboard-home'],
    queryFn: getDashboardHome,
    refetchInterval: 60_000,
    staleTime: 0,
    refetchOnMount: 'always',
  })
  const { data: whosOut } = useQuery({
    queryKey: ['whos-out', range],
    queryFn: () => getWhosOut(range),
    refetchInterval: 60_000,
    staleTime: 0,
    refetchOnMount: 'always',
  })

  const now = new Date()
  const greeting = now.getHours() < 12 ? 'Good morning' : now.getHours() < 18 ? 'Good afternoon' : 'Good evening'
  const firstName = user?.fullName.split(' ')[0] ?? ''
  const dateLine = `${now.toLocaleDateString('en-GB', { weekday: 'long' })}, ${now.toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' })} · KW ${isoWeek(now)}`

  const money = home?.canViewMoney ?? false
  const cur = home?.currency ?? 'EUR'
  const k = home?.kpis
  const maxBar = Math.max(1, ...(whosOut?.perDay.map(d => d.outCount) ?? [1]))
  const absentThreshold = Math.max(3, Math.ceil((whosOut?.teamTotal ?? 0) * 0.3))

  return (
    <div className="mx-auto max-w-[1400px] space-y-5 p-6">
      {/* Header */}
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex flex-col gap-1">
          <span className="text-[13px] font-semibold text-muted-foreground">{dateLine}</span>
          <h1 className="m-0 text-[28px] font-bold tracking-tight">{greeting}, {firstName}</h1>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={() => navigate('/redesign/working-time')}>
            <Clock className="mr-1.5 h-4 w-4" />Log time
          </Button>
          {money && (
            <Button variant="outline" onClick={() => navigate('/offers')}>
              <Plus className="mr-1 h-4 w-4" />New offer
            </Button>
          )}
          {money && (
            <Button onClick={() => navigate('/projects')}>
              <Plus className="mr-1 h-4 w-4" />New project
            </Button>
          )}
        </div>
      </div>

      {/* Needs your attention */}
      {home && home.attention.length > 0 && (
        <section aria-label="Needs your attention" className="space-y-2.5">
          <h2 className="m-0 text-[15px] font-bold">
            Needs your attention
            <span className="ml-1.5 rounded-full bg-red-600 px-2 py-px text-xs font-bold text-white">
              {home.attention.length}
            </span>
          </h2>
          <div className="grid gap-3 [grid-template-columns:repeat(auto-fit,minmax(230px,1fr))]">
            {home.attention.map(a => {
              const Icon = KIND_ICON[a.kind] ?? Flag
              return (
                <Link
                  key={a.kind + a.title}
                  to={a.link}
                  className="flex items-start gap-3 rounded-xl border bg-card px-4 py-3.5 hover:border-primary/50"
                >
                  <span className={cn('flex h-9 w-9 flex-none items-center justify-center rounded-[9px]', TONE_ICON[a.tone])}>
                    <Icon className="h-[18px] w-[18px]" />
                  </span>
                  <span className="flex min-w-0 flex-col gap-0.5">
                    <span className="font-bold leading-snug">{a.title}</span>
                    <span className="text-[12.5px] text-muted-foreground">{a.sub}</span>
                    <span className="text-[12.5px] font-semibold text-primary">{a.cta}</span>
                  </span>
                </Link>
              )
            })}
          </div>
        </section>
      )}

      {/* KPI row */}
      {k && (
        <div className="grid gap-3.5 [grid-template-columns:repeat(auto-fit,minmax(210px,1fr))]">
          {money && (
            <Kpi
              label="Won this year"
              value={fmtMoney(k.wonThisYear ?? 0, cur)}
              sub={`${k.acceptedThisYear} accepted offer${k.acceptedThisYear === 1 ? '' : 's'}${k.winRatePercent != null ? ` · ${k.winRatePercent} % win rate` : ''}`}
              subCls="text-green-700 dark:text-green-400"
            />
          )}
          {money && (
            <Kpi
              label="Open pipeline"
              value={fmtMoney(k.openPipeline ?? 0, cur)}
              sub={`${k.openOffersCount} open offer${k.openOffersCount === 1 ? '' : 's'} · ${k.waitingForReply} waiting for reply`}
            />
          )}
          <Kpi
            label="Projects"
            value={String(k.projectsActive)}
            sub={`${k.projectsInProgress} in progress · ${k.projectsPlanning} planning${k.projectsAtRisk > 0 ? ` · ${k.projectsAtRisk} at risk` : ''}`}
            subCls={k.projectsAtRisk > 0 ? 'text-amber-700 dark:text-amber-400' : undefined}
          />
          <Kpi
            label="Team today"
            value={whosOut ? `${whosOut.inToday} / ${whosOut.teamTotal} in` : '—'}
            sub={whosOut ? `${whosOut.outToday} out · see Who's out` : ''}
          />
        </div>
      )}

      <div className="flex flex-wrap items-start gap-4">
        {/* Left column */}
        <div className="flex min-w-0 flex-[999_1_560px] flex-col gap-4">
          {/* Offers */}
          {money && home && (
            <section className="overflow-hidden rounded-xl border bg-card">
              <div className="flex items-center justify-between border-b px-5 py-4">
                <h2 className="m-0 text-[17px] font-bold">Offers</h2>
                <Link to="/offers" className="text-[13px] font-semibold text-primary hover:underline">All offers</Link>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[640px] border-collapse text-[13.5px]">
                  <thead>
                    <tr className="text-left text-xs text-muted-foreground">
                      <th className="px-5 py-2.5 font-semibold">Offer</th>
                      <th className="px-2 py-2.5 font-semibold">Client</th>
                      <th className="px-2 py-2.5 text-right font-semibold">Amount</th>
                      <th className="px-2 py-2.5 font-semibold">Status</th>
                      <th className="py-2.5 pr-5 pl-2 text-right font-semibold">Next step</th>
                    </tr>
                  </thead>
                  <tbody>
                    {home.offers.map(o => {
                      const warn =
                        (o.status === 'Sent' && o.ageDays >= 7) || (o.status === 'Draft' && o.ageDays >= 14)
                      const age =
                        o.status === 'Sent'
                          ? `sent ${o.ageDays} day${o.ageDays === 1 ? '' : 's'} ago`
                          : o.status === 'Draft'
                            ? `draft for ${o.ageDays} day${o.ageDays === 1 ? '' : 's'}`
                            : o.acceptedOn
                              ? `accepted ${fmtDate(o.acceptedOn)}`
                              : ''
                      const cta =
                        o.nextStep === 'FollowUp' ? 'Follow up'
                        : o.nextStep === 'CreateProject' ? 'Create project'
                        : o.nextStep === 'FinishSend' ? 'Finish & send'
                        : 'Open'
                      const primary = o.nextStep === 'FollowUp' || o.nextStep === 'CreateProject'
                      return (
                        <tr key={o.id} className="border-t">
                          <td className="px-5 py-3 font-semibold">{o.title}</td>
                          <td className="px-2 py-3 text-muted-foreground">{o.clientName}</td>
                          <td className="px-2 py-3 text-right font-semibold tabular-nums">
                            {o.amount != null ? fmtMoney(o.amount, cur) : '—'}
                          </td>
                          <td className="px-2 py-3">
                            <div className="flex flex-col items-start gap-0.5">
                              <span className={cn('rounded-md px-2 py-0.5 text-[11px] font-bold uppercase', OFFER_CHIP[o.status])}>
                                {o.status}
                              </span>
                              <span className={cn('text-[11.5px]', warn ? 'font-semibold text-amber-700 dark:text-amber-400' : 'text-muted-foreground')}>
                                {age}
                              </span>
                            </div>
                          </td>
                          <td className="py-3 pr-5 pl-2 text-right">
                            <Button
                              size="sm"
                              variant={primary ? 'default' : 'outline'}
                              className="h-8 whitespace-nowrap text-[12.5px]"
                              onClick={() => navigate(`/offers/${o.id}`)}
                            >
                              {cta}
                            </Button>
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
              {home.offers.length === 0 && (
                <div className="py-8 text-center text-[13px] text-muted-foreground">No open offers — create the first one.</div>
              )}
            </section>
          )}

          {/* Projects */}
          {home && (
            <section className="overflow-hidden rounded-xl border bg-card">
              <div className="flex items-center justify-between border-b px-5 py-4">
                <h2 className="m-0 text-[17px] font-bold">Projects</h2>
                <Link to="/projects" className="text-[13px] font-semibold text-primary hover:underline">All projects</Link>
              </div>
              {home.projects.map(p => {
                const chip = PROJECT_CHIP[p.status] ?? PROJECT_CHIP.Planning
                const planning = p.status === 'Planning'
                const pct = planning ? (p.setupDone / 5) * 100 : Math.min(p.budgetUsedPercent ?? 0, 100)
                const barColor = planning
                  ? 'bg-slate-400'
                  : (p.budgetUsedPercent ?? 0) > 100 ? 'bg-red-600' : (p.budgetUsedPercent ?? 0) > 80 ? 'bg-amber-500' : 'bg-primary'
                const barLabel = planning
                  ? `Setup ${p.setupDone} of 5 steps`
                  : money && p.budgetUsedPercent != null
                    ? `Budget used ${p.budgetUsedPercent.toLocaleString('de-AT')} % · tasks ${p.tasksDone}/${p.tasksTotal}`
                    : `Tasks ${p.tasksDone}/${p.tasksTotal}`
                return (
                  <Link
                    key={p.id}
                    to={`/projects/${p.id}/overview/summary`}
                    className={cn(
                      'grid items-center gap-4 border-t px-5 py-3.5 hover:bg-muted/30',
                      money ? '[grid-template-columns:minmax(0,1.6fr)_minmax(0,1.1fr)_minmax(0,1.2fr)_110px]' : '[grid-template-columns:minmax(0,1.6fr)_minmax(0,1.1fr)_minmax(0,1.2fr)]',
                    )}
                  >
                    <div className="flex min-w-0 flex-col gap-1">
                      <span className="truncate font-bold">{p.name}</span>
                      <span className="flex items-center gap-1.5">
                        <span className={cn('rounded-md px-2 py-0.5 text-[11px] font-bold', chip.cls)}>{chip.label}</span>
                        <span className="truncate text-xs text-muted-foreground">{p.clientName}</span>
                      </span>
                    </div>
                    <div className="flex flex-col gap-1.5">
                      <span className="text-xs text-muted-foreground">{barLabel}</span>
                      <div className="h-1.5 rounded-full bg-muted">
                        <div className={cn('h-1.5 rounded-full', barColor)} style={{ width: `${Math.max(pct, 2)}%` }} />
                      </div>
                    </div>
                    <span className={cn('text-[12.5px] leading-snug', p.nextAtRisk ? 'font-semibold text-destructive' : 'text-muted-foreground')}>
                      {p.nextLabel}
                    </span>
                    {money && (
                      <span className="text-right font-bold tabular-nums">
                        {p.contractValue != null ? fmtMoney(p.contractValue, cur) : '—'}
                      </span>
                    )}
                  </Link>
                )
              })}
              {home.projects.length === 0 && (
                <div className="py-8 text-center text-[13px] text-muted-foreground">No active projects.</div>
              )}
            </section>
          )}
        </div>

        {/* Right column */}
        <aside className="flex min-w-0 flex-[1_1_340px] flex-col gap-4">
          {/* Who's out */}
          <section aria-label="Who's out" className="flex flex-col gap-3.5 rounded-xl border bg-card px-5 py-[18px]">
            <div className="flex items-center justify-between gap-2">
              <h2 className="m-0 text-[17px] font-bold">Who's out</h2>
              <Link to="/absences" className="text-[13px] font-semibold text-primary hover:underline">Team calendar</Link>
            </div>
            <div role="group" aria-label="Period" className="flex gap-[3px] rounded-[9px] bg-muted p-[3px]">
              {([['today', 'Today'], ['week', 'This week'], ['month', 'Next 30 days']] as [WhosOutRange, string][]).map(([id, label]) => (
                <button
                  key={id}
                  aria-pressed={range === id}
                  onClick={() => setRange(id)}
                  className={cn(
                    'min-h-[34px] flex-1 rounded-[7px] text-[12.5px] font-semibold',
                    range === id ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
                  )}
                >
                  {label}
                </button>
              ))}
            </div>
            <div className="flex items-center gap-4 rounded-[10px] bg-muted/50 px-3.5 py-3">
              <div className="flex flex-col">
                <span className="text-[22px] font-bold text-green-700 dark:text-green-400">{whosOut?.inToday ?? '—'}</span>
                <span className="text-xs text-muted-foreground">in today</span>
              </div>
              <div className="flex flex-col">
                <span className="text-[22px] font-bold text-amber-700 dark:text-amber-400">{whosOut?.outToday ?? '—'}</span>
                <span className="text-xs text-muted-foreground">out today</span>
              </div>
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <span className="text-[11.5px] text-muted-foreground">Absent per day · next 2 weeks</span>
                <div
                  role="img"
                  aria-label="People absent per day for the next two weeks"
                  className="flex h-[34px] items-end gap-[3px]"
                >
                  {(whosOut?.perDay ?? []).map(d => (
                    <span
                      key={d.date}
                      title={`${fmtDate(d.date)}: ${d.isNonWorking ? 'non-working day' : `${d.outCount} absent`}`}
                      className={cn(
                        'flex-1 rounded-[2px]',
                        d.isNonWorking ? 'bg-muted' : d.outCount >= absentThreshold ? 'bg-amber-500' : 'bg-blue-300 dark:bg-blue-800',
                      )}
                      style={{ height: d.isNonWorking ? 4 : Math.max(4, Math.min(34, (d.outCount / maxBar) * 30 + 4)) }}
                    />
                  ))}
                </div>
              </div>
            </div>
            <div className="flex flex-col">
              {(whosOut?.people ?? []).map((o, i) => (
                <div key={o.userId + o.startDate} className="flex items-center gap-3 border-b py-2 last:border-b-0">
                  <span
                    className="flex h-[34px] w-[34px] flex-none items-center justify-center rounded-full text-xs font-bold text-white"
                    style={{ background: AVATAR_COLORS[i % AVATAR_COLORS.length] }}
                  >
                    {initials(o.name)}
                  </span>
                  <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <span className="truncate font-semibold">{o.name}</span>
                    <span className="text-xs text-muted-foreground">
                      {o.startDate === o.endDate ? fmtDateLong(o.startDate) : `${fmtDate(o.startDate)} – ${fmtDate(o.endDate)}`}
                    </span>
                  </div>
                  <div className="flex flex-col items-end gap-0.5">
                    <span className={cn(
                      'rounded-md px-2 py-0.5 text-[11.5px] font-bold',
                      o.type === 'Sick' ? 'bg-pink-100 text-pink-800 dark:bg-pink-950 dark:text-pink-300'
                      : o.type === 'Vacation' ? 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300'
                      : 'bg-muted text-muted-foreground',
                    )}>
                      {o.type}
                    </span>
                    {o.backOn && <span className="text-[11.5px] text-muted-foreground">back {fmtDateLong(o.backOn)}</span>}
                  </div>
                </div>
              ))}
              {whosOut && whosOut.people.length === 0 && (
                <p className="m-0 py-3 text-[13px] text-muted-foreground">Everyone is in.</p>
              )}
            </div>
            <span className="text-xs leading-relaxed text-muted-foreground">
              {whosOut?.canSeeTypes
                ? 'You see reasons as admin. Colleagues only see “Absent” — sick leave stays private.'
                : 'You see “Absent” only — absence reasons stay private.'}
            </span>
          </section>

          {/* My time */}
          {home && (
            <section className="flex flex-col gap-3 rounded-xl border bg-card px-5 py-[18px]">
              <div className="flex items-center justify-between">
                <h2 className="m-0 text-[17px] font-bold">My time</h2>
                <Link to="/redesign/working-time" className="text-[13px] font-semibold text-primary hover:underline">Work time</Link>
              </div>
              <div className="flex justify-between text-[13.5px]">
                <span className="text-muted-foreground">This week</span>
                <span className="font-bold">
                  {fmtHours(home.myTime.weekHours)}
                  {home.myTime.weekTargetHours != null && (
                    <span className="font-normal text-muted-foreground"> / {fmtHours(home.myTime.weekTargetHours)}</span>
                  )}
                </span>
              </div>
              <div className="flex justify-between text-[13.5px]">
                <span className="text-muted-foreground">{home.myTime.previousMonthName}</span>
                <span className={cn(
                  'font-semibold',
                  home.myTime.previousMonthStatus === 'NotSubmitted'
                    ? 'text-amber-700 dark:text-amber-400'
                    : home.myTime.previousMonthStatus === 'Approved' || home.myTime.previousMonthStatus === 'Locked'
                      ? 'text-green-700 dark:text-green-400'
                      : '',
                )}>
                  {home.myTime.previousMonthStatus === 'NotSubmitted' ? 'Not submitted'
                    : home.myTime.previousMonthStatus === 'NoData' ? '—'
                    : home.myTime.previousMonthStatus}
                </span>
              </div>
              <div className="flex justify-between text-[13.5px]">
                <span className="text-muted-foreground">Vacation left</span>
                <span className="font-bold">
                  {home.myTime.vacationLeftDays != null
                    ? `${home.myTime.vacationLeftDays.toLocaleString('de-AT')} days`
                    : '—'}
                </span>
              </div>
              {!home.myTime.hasContract && (
                <div className="rounded-lg bg-amber-50 px-2.5 py-2 text-[12.5px] leading-relaxed text-amber-800 dark:bg-amber-950/40 dark:text-amber-300">
                  <CalendarOff className="mr-1 inline h-3.5 w-3.5" />
                  No contract set for you — targets and vacation can't be calculated.
                </div>
              )}
            </section>
          )}
        </aside>
      </div>
    </div>
  )
}

function Kpi({ label, value, sub, subCls }: { label: string; value: string; sub: string; subCls?: string }) {
  return (
    <div className="flex flex-col gap-1 rounded-xl border bg-card px-[18px] py-4">
      <span className="text-xs font-semibold text-muted-foreground">{label}</span>
      <span className="truncate text-2xl font-bold" title={value}>{value}</span>
      <span className={cn('text-[12.5px] text-muted-foreground', subCls)}>{sub}</span>
    </div>
  )
}
