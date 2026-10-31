import { useQuery } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { getMonthlyReport } from '@/api/worktime-reports'
import { ABSENCE_TYPE_LABELS } from '@/api/absences'

const fmtH = (n: number) => `${n.toLocaleString('de-AT', { maximumFractionDigits: 2 })} h`
const fmtMoney = (n: number, c: string) =>
  n.toLocaleString('de-AT', { style: 'currency', currency: c, maximumFractionDigits: 2 })
const fmtDate = (iso: string) =>
  new Date(iso).toLocaleDateString('de-AT', { weekday: 'short', day: '2-digit', month: '2-digit' })

interface Props {
  userId: string
  year: number
  month: number
  /** False shows every day entry at full height (Team time); true caps the list (approvals row). */
  scrollDays?: boolean
}

/**
 * One employee's month at a glance: worked vs target, pay estimate, absences by type, and the
 * recorded day entries. Used inside the approvals list (review before deciding) and on the
 * Team time page.
 */
export function MonthReportPanel({ userId, year, month, scrollDays = true }: Props) {
  const { data: report, isLoading } = useQuery({
    queryKey: ['worktime-report', userId, year, month],
    queryFn: () => getMonthlyReport(year, month, userId),
  })

  if (isLoading) return <div className="py-6 text-center text-sm text-muted-foreground">Loading report…</div>
  if (!report) return <div className="py-6 text-center text-sm text-muted-foreground">No timesheet for this month.</div>

  const diff = report.monthlyDifference
  const vacation = report.absenceDays.find(a => a.type === 'Vacation')?.days ?? 0
  const sick = report.absenceDays.find(a => a.type === 'SickLeave')?.days ?? 0
  const otherAbsences = report.absenceDays.filter(a => a.type !== 'Vacation' && a.type !== 'SickLeave')

  return (
    <div className="space-y-4">
      {/* Summary tiles */}
      <div className="grid gap-3 [grid-template-columns:repeat(auto-fit,minmax(150px,1fr))]">
        <Tile label="Worked" value={fmtH(report.actualHours)}
          sub={report.targetHours != null ? `target ${fmtH(report.targetHours)}` : 'no target set'} />
        <Tile
          label="Difference"
          value={diff != null ? `${diff > 0 ? '+' : ''}${fmtH(diff)}` : '—'}
          sub={`carried forward ${report.carriedForwardHours > 0 ? '+' : ''}${fmtH(report.carriedForwardHours)}`}
          tone={diff == null ? undefined : diff < 0 ? 'amber' : 'green'}
        />
        <Tile
          label="Estimated pay"
          value={report.estimatedPay != null && report.currency ? fmtMoney(report.estimatedPay, report.currency) : '—'}
          sub={report.hourlyRate != null && report.currency
            ? `${fmtH(report.actualHours)} × ${fmtMoney(report.hourlyRate, report.currency)}/h`
            : 'no hourly rate in the contract'}
        />
        <Tile label="Vacation" value={`${vacation.toLocaleString('de-AT')} d`} sub="taken this month" />
        <Tile label="Sick leave" value={`${sick.toLocaleString('de-AT')} d`} sub="this month"
          tone={sick > 0 ? 'amber' : undefined} />
      </div>

      {otherAbsences.length > 0 && (
        <div className="flex flex-wrap gap-2 text-xs">
          {otherAbsences.map(a => (
            <span key={a.type} className="rounded-md bg-muted px-2 py-1 font-medium text-muted-foreground">
              {ABSENCE_TYPE_LABELS[a.type] ?? a.type}: {a.days.toLocaleString('de-AT')} d
            </span>
          ))}
        </div>
      )}

      {report.breaches.length > 0 && (
        <p className="rounded-md bg-amber-50 px-3 py-2 text-xs text-amber-800 dark:bg-amber-950/40 dark:text-amber-300">
          {report.breaches.length} working-time issue{report.breaches.length > 1 ? 's' : ''} in this month —
          shown in detail when you approve.
        </p>
      )}

      {/* Day entries */}
      <div className={cn('rounded-lg border', scrollDays && 'max-h-72 overflow-y-auto')}>
        <table className="w-full text-[13px]">
          <thead className={cn(scrollDays && 'sticky top-0 bg-muted/60 backdrop-blur', !scrollDays && 'bg-muted/40')}>
            <tr className="text-left text-xs text-muted-foreground">
              <th className="px-3 py-2 font-semibold">Day</th>
              <th className="px-3 py-2 font-semibold">Time</th>
              <th className="px-3 py-2 text-right font-semibold">Break</th>
              <th className="px-3 py-2 text-right font-semibold">Hours</th>
              <th className="px-3 py-2 font-semibold">Note</th>
            </tr>
          </thead>
          <tbody>
            {report.days.map(d => (
              <tr key={d.id} className="border-t">
                <td className="whitespace-nowrap px-3 py-1.5">{fmtDate(d.date)}</td>
                <td className="whitespace-nowrap px-3 py-1.5 tabular-nums">
                  {d.startTime.slice(0, 5)} – {d.endTime.slice(0, 5)}{d.crossesMidnight ? ' (+1)' : ''}
                </td>
                <td className="px-3 py-1.5 text-right tabular-nums">{d.breakMinutes} min</td>
                <td className="px-3 py-1.5 text-right font-semibold tabular-nums">{fmtH(d.workedHours)}</td>
                <td className="max-w-[260px] truncate px-3 py-1.5 text-muted-foreground" title={d.note ?? ''}>
                  {d.note ?? ''}
                </td>
              </tr>
            ))}
            {report.days.length === 0 && (
              <tr><td colSpan={5} className="px-3 py-4 text-center text-muted-foreground">No entries recorded.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function Tile({ label, value, sub, tone }: { label: string; value: string; sub: string; tone?: 'green' | 'amber' }) {
  return (
    <div className="rounded-lg border bg-card px-3.5 py-3">
      <p className="text-xs font-semibold text-muted-foreground">{label}</p>
      <p className={cn(
        'mt-0.5 text-lg font-bold tabular-nums',
        tone === 'green' && 'text-green-700 dark:text-green-400',
        tone === 'amber' && 'text-amber-700 dark:text-amber-400',
      )}>
        {value}
      </p>
      <p className="text-[11.5px] text-muted-foreground">{sub}</p>
    </div>
  )
}
