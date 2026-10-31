import { useMemo, useState } from 'react'
import { useQueries, useQuery } from '@tanstack/react-query'
import { Clock, Search } from 'lucide-react'
import { cn } from '@/lib/utils'
import { getWorkers } from '@/api/hr'
import { getMonthlyReport, type MonthlyWorkTimeReport } from '@/api/worktime-reports'
import { MonthReportPanel } from './MonthReportPanel'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

const MEMBER_COLORS = ['#2563eb', '#7c3aed', '#0d9488', '#b45309', '#15803d', '#db2777']

const fmtH = (n: number) => `${n.toLocaleString('de-AT', { maximumFractionDigits: 1 })} h`
const fmtMoney = (n: number, c: string) =>
  n.toLocaleString('de-AT', { style: 'currency', currency: c, maximumFractionDigits: 0 })

const STATUS_CHIP: Record<string, string> = {
  Draft: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Submitted: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300',
  Approved: 'bg-green-100 text-green-800 dark:bg-green-950 dark:text-green-400',
  Locked: 'bg-violet-100 text-violet-800 dark:bg-violet-950 dark:text-violet-300',
}

/**
 * Team time: pick an employee and read their recorded time per month or per year —
 * hours vs target, pay estimate, vacation and sick days, and the day entries.
 */
export function TeamTimePage() {
  const now = new Date()
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<string | null>(null)
  const [mode, setMode] = useState<'month' | 'year'>('month')
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  const { data: workers = [] } = useQuery({ queryKey: ['hr', 'workers'], queryFn: getWorkers })

  const q = search.trim().toLowerCase()
  const visible = workers.filter(w =>
    !q || `${w.firstName} ${w.lastName}`.toLowerCase().includes(q) || (w.jobTitle ?? '').toLowerCase().includes(q))

  const selectedWorker = workers.find(w => w.userId === selected) ?? null

  return (
    <div className="p-6 space-y-5">
      <div>
        <h1 className="flex items-center gap-2 text-2xl font-bold tracking-tight">
          <Clock className="h-6 w-6" />
          Team time
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Pick an employee to see their recorded hours, pay estimate and absences — per month or per year.
        </p>
      </div>

      <div className="flex flex-wrap items-start gap-4">
        {/* Employee list */}
        <aside className="flex w-full max-w-[280px] min-w-[220px] flex-col gap-2 rounded-xl border bg-card p-3">
          <label className="flex min-h-9 items-center gap-2 rounded-lg border bg-background px-2.5 text-muted-foreground">
            <Search className="h-3.5 w-3.5" />
            <input
              type="search"
              aria-label="Search employees"
              placeholder="Search employees"
              value={search}
              onChange={e => setSearch(e.target.value)}
              className="min-w-0 flex-1 border-0 bg-transparent text-[13px] text-foreground outline-none placeholder:text-muted-foreground/70"
            />
          </label>
          <div className="flex max-h-[520px] flex-col gap-0.5 overflow-y-auto">
            {visible.map((w, i) => (
              <button
                key={w.userId}
                onClick={() => setSelected(w.userId)}
                className={cn(
                  'flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-left',
                  selected === w.userId ? 'bg-primary/10' : 'hover:bg-muted',
                )}
              >
                <span
                  className="flex h-8 w-8 flex-none items-center justify-center rounded-full text-[11px] font-bold text-white"
                  style={{ background: MEMBER_COLORS[i % MEMBER_COLORS.length] }}
                >
                  {(w.firstName[0] + (w.lastName[0] ?? '')).toUpperCase()}
                </span>
                <span className="flex min-w-0 flex-col">
                  <span className="truncate text-sm font-semibold">{w.firstName} {w.lastName}</span>
                  <span className="truncate text-xs text-muted-foreground">{w.jobTitle ?? '—'}</span>
                </span>
              </button>
            ))}
            {visible.length === 0 && (
              <p className="px-2 py-4 text-center text-[13px] text-muted-foreground">No employees match.</p>
            )}
          </div>
        </aside>

        {/* Report area */}
        <section className="min-w-0 flex-1 space-y-4">
          {!selectedWorker ? (
            <div className="rounded-xl border bg-card py-16 text-center text-sm text-muted-foreground">
              Select an employee on the left.
            </div>
          ) : (
            <>
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h2 className="m-0 text-lg font-bold">{selectedWorker.firstName} {selectedWorker.lastName}</h2>
                <div className="flex flex-wrap items-center gap-2">
                  <nav className="flex gap-[3px] rounded-[9px] bg-muted p-[3px]" aria-label="Period mode">
                    {(['month', 'year'] as const).map(m => (
                      <button
                        key={m}
                        onClick={() => setMode(m)}
                        aria-pressed={mode === m}
                        className={cn(
                          'inline-flex min-h-9 items-center rounded-[7px] px-3.5 text-[13px] font-semibold capitalize',
                          mode === m ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
                        )}
                      >
                        {m}
                      </button>
                    ))}
                  </nav>
                  {mode === 'month' && (
                    <input
                      type="month"
                      aria-label="Month"
                      value={`${year}-${String(month).padStart(2, '0')}`}
                      onChange={e => {
                        const [y, m] = e.target.value.split('-').map(Number)
                        if (y && m) { setYear(y); setMonth(m) }
                      }}
                      className="h-9 rounded-lg border bg-card px-2.5 text-sm"
                    />
                  )}
                  {mode === 'year' && (
                    <select
                      aria-label="Year"
                      value={year}
                      onChange={e => setYear(Number(e.target.value))}
                      className="h-9 rounded-lg border bg-card px-2.5 text-sm"
                    >
                      {Array.from({ length: 5 }, (_, i) => now.getFullYear() - i).map(y => (
                        <option key={y} value={y}>{y}</option>
                      ))}
                    </select>
                  )}
                </div>
              </div>

              {mode === 'month' ? (
                <div className="rounded-xl border bg-card p-4">
                  <MonthReportPanel userId={selectedWorker.userId} year={year} month={month} scrollDays={false} />
                </div>
              ) : (
                <YearReport userId={selectedWorker.userId} year={year} />
              )}
            </>
          )}
        </section>
      </div>
    </div>
  )
}

/** Twelve month summaries + totals. Months without a timesheet show as empty. */
function YearReport({ userId, year }: { userId: string; year: number }) {
  const results = useQueries({
    queries: Array.from({ length: 12 }, (_, i) => ({
      queryKey: ['worktime-report', userId, year, i + 1],
      queryFn: () => getMonthlyReport(year, i + 1, userId),
      staleTime: 60_000,
    })),
  })

  const months = results.map(r => r.data ?? null)
  const loading = results.some(r => r.isLoading)

  const totals = useMemo(() => {
    const present = months.filter((m): m is MonthlyWorkTimeReport => m !== null)
    const days = (m: MonthlyWorkTimeReport, type: string) => m.absenceDays.find(a => a.type === type)?.days ?? 0
    return {
      worked: present.reduce((s, m) => s + m.actualHours, 0),
      target: present.reduce((s, m) => s + (m.targetHours ?? 0), 0),
      pay: present.reduce((s, m) => s + (m.estimatedPay ?? 0), 0),
      vacation: present.reduce((s, m) => s + days(m, 'Vacation'), 0),
      sick: present.reduce((s, m) => s + days(m, 'SickLeave'), 0),
      currency: present.find(m => m.currency)?.currency ?? 'EUR',
      any: present.length > 0,
    }
  }, [months])

  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px] text-[13.5px]">
          <thead>
            <tr className="border-b bg-muted/40 text-left text-xs text-muted-foreground">
              <th className="px-4 py-2.5 font-semibold">Month</th>
              <th className="px-2 py-2.5 font-semibold">Status</th>
              <th className="px-2 py-2.5 text-right font-semibold">Target</th>
              <th className="px-2 py-2.5 text-right font-semibold">Worked</th>
              <th className="px-2 py-2.5 text-right font-semibold">Difference</th>
              <th className="px-2 py-2.5 text-right font-semibold">Vacation</th>
              <th className="px-2 py-2.5 text-right font-semibold">Sick</th>
              <th className="px-4 py-2.5 text-right font-semibold">Est. pay</th>
            </tr>
          </thead>
          <tbody>
            {months.map((m, i) => {
              const vacation = m?.absenceDays.find(a => a.type === 'Vacation')?.days ?? 0
              const sick = m?.absenceDays.find(a => a.type === 'SickLeave')?.days ?? 0
              return (
                <tr key={i} className={cn('border-b', !m && 'text-muted-foreground/60')}>
                  <td className="px-4 py-2 font-semibold">{MONTHS[i]}</td>
                  <td className="px-2 py-2">
                    {m ? (
                      <span className={cn('rounded-md px-2 py-0.5 text-[11px] font-bold', STATUS_CHIP[m.status] ?? '')}>
                        {m.status === 'Draft' ? 'Draft' : m.status}
                      </span>
                    ) : '—'}
                  </td>
                  <td className="px-2 py-2 text-right tabular-nums">{m?.targetHours != null ? fmtH(m.targetHours) : '—'}</td>
                  <td className="px-2 py-2 text-right font-semibold tabular-nums">{m ? fmtH(m.actualHours) : '—'}</td>
                  <td className={cn('px-2 py-2 text-right tabular-nums',
                    m?.monthlyDifference != null && m.monthlyDifference < 0 && 'text-amber-700 dark:text-amber-400')}>
                    {m?.monthlyDifference != null ? `${m.monthlyDifference > 0 ? '+' : ''}${fmtH(m.monthlyDifference)}` : '—'}
                  </td>
                  <td className="px-2 py-2 text-right tabular-nums">{m && vacation > 0 ? `${vacation} d` : m ? '0 d' : '—'}</td>
                  <td className="px-2 py-2 text-right tabular-nums">{m && sick > 0 ? `${sick} d` : m ? '0 d' : '—'}</td>
                  <td className="px-4 py-2 text-right font-semibold tabular-nums">
                    {m?.estimatedPay != null && m.currency ? fmtMoney(m.estimatedPay, m.currency) : '—'}
                  </td>
                </tr>
              )
            })}
          </tbody>
          {totals.any && (
            <tfoot>
              <tr className="bg-muted/40 font-bold">
                <td className="px-4 py-2.5" colSpan={2}>Total {year}</td>
                <td className="px-2 py-2.5 text-right tabular-nums">{fmtH(totals.target)}</td>
                <td className="px-2 py-2.5 text-right tabular-nums">{fmtH(totals.worked)}</td>
                <td className="px-2 py-2.5 text-right tabular-nums">
                  {`${totals.worked - totals.target > 0 ? '+' : ''}${fmtH(totals.worked - totals.target)}`}
                </td>
                <td className="px-2 py-2.5 text-right tabular-nums">{totals.vacation} d</td>
                <td className="px-2 py-2.5 text-right tabular-nums">{totals.sick} d</td>
                <td className="px-4 py-2.5 text-right tabular-nums">{fmtMoney(totals.pay, totals.currency)}</td>
              </tr>
            </tfoot>
          )}
        </table>
      </div>
      {loading && <div className="py-3 text-center text-xs text-muted-foreground">Loading months…</div>}
      {!loading && !totals.any && (
        <div className="py-8 text-center text-sm text-muted-foreground">No recorded time in {year}.</div>
      )}
    </div>
  )
}
