import { useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getMonthlyReport, downloadMonthlyReportPdf, signedHours,
  type MonthlyWorkTimeReport,
} from '@/api/worktime-reports'
import { ABSENCE_TYPE_LABELS } from '@/api/absences'
import { getApiErrorMessage } from '@/lib/api-errors'
import { BreachList } from './BreachList'
import { ReconciliationPanel } from './ReconciliationPanel'
import { Button } from '@/components/ui/button'
import {
  ChevronLeft, ChevronRight, FileText, Download, AlertTriangle, Info,
} from 'lucide-react'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

export function MonthlyReportPage() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['worktime', 'report', year, month],
    queryFn: () => getMonthlyReport(year, month),
    retry: false,
  })

  const download = useMutation({
    mutationFn: () => downloadMonthlyReportPdf(year, month),
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const shiftMonth = (delta: number) => {
    const next = new Date(year, month - 1 + delta, 1)
    setYear(next.getFullYear())
    setMonth(next.getMonth() + 1)
  }

  return (
    <div className="p-6 space-y-6 max-w-4xl">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <FileText className="h-6 w-6" />
            Month-end report
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            What you were expected to work, what you recorded, and where that leaves your balance.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => shiftMonth(-1)} aria-label="Previous month">
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <span className="text-sm font-semibold min-w-[9rem] text-center">
            {MONTHS[month - 1]} {year}
          </span>
          <Button variant="outline" size="sm" onClick={() => shiftMonth(1)} aria-label="Next month">
            <ChevronRight className="h-4 w-4" />
          </Button>
          <Button
            className="gap-2 ml-2"
            disabled={!data || download.isPending}
            onClick={() => download.mutate()}
          >
            <Download className="h-4 w-4" />
            PDF
          </Button>
        </div>
      </div>

      {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}

      {isError && (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          Nothing recorded for {MONTHS[month - 1]} {year}.
          <span className="block text-xs mt-1">{getApiErrorMessage(error)}</span>
        </div>
      )}

      {data && <Report report={data} />}
    </div>
  )
}

function Report({ report: r }: { report: MonthlyWorkTimeReport }) {
  return (
    <div className="space-y-6">
      {r.isProvisional && (
        <Banner tone="info">
          This month is <strong>{r.status.toLowerCase()}</strong> and these figures are still
          provisional. They are final once an approver signs the month off.
        </Banner>
      )}

      {r.isRevised && (
        <Banner tone="warning">
          Something behind this month changed after it was approved — a retroactive absence, most
          likely. The figures below are as reported; ask an approver to reopen the month if they
          need to be brought up to date.
        </Banner>
      )}

      <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
        <Stat
          label="Target"
          value={r.targetHours === null ? 'unavailable' : `${r.targetHours.toFixed(2)} h`}
          muted={r.targetHours === null}
        />
        <Stat label="Recorded" value={`${r.actualHours.toFixed(2)} h`} />
        <Stat
          label="Difference"
          value={r.monthlyDifference === null ? '—' : signedHours(r.monthlyDifference)}
          tone={
            r.monthlyDifference === null ? undefined
              : r.monthlyDifference >= 0 ? 'positive' : 'negative'
          }
        />
      </div>

      {r.targetHours === null && (
        <Banner tone="warning">
          No employment terms cover this month, so there is nothing to compare your recorded hours
          against. An administrator needs to add a terms version starting on or before the first of
          the month.
        </Banner>
      )}

      {/* Balance */}
      <section className="rounded-lg border">
        <header className="px-4 py-2 border-b bg-muted/50">
          <h2 className="text-sm font-semibold">Flexitime balance</h2>
        </header>

        <dl className="divide-y text-sm">
          <Row label="Opening balance" value={signedHours(r.openingBalanceHours)} />
          <Row
            label="This month"
            value={r.monthlyDifference === null ? '—' : signedHours(r.monthlyDifference)}
          />

          {r.absorbedByLumpSumHours > 0 && (
            <Row
              label={`Covered by the ${r.overtimeLumpSumHours?.toFixed(2)} h Überstundenpauschale`}
              value={`−${r.absorbedByLumpSumHours.toFixed(2)} h`}
            />
          )}

          {r.coveredByAllInHours > 0 && (
            <Row
              label="Covered by the all-in salary"
              value={`−${r.coveredByAllInHours.toFixed(2)} h`}
            />
          )}

          {(r.absorbedByLumpSumHours > 0 || r.coveredByAllInHours > 0) && (
            <Row label="Carried to the balance" value={signedHours(r.carriedForwardHours)} />
          )}

          {r.forfeitedHours > 0 && (
            <Row
              label={`Forfeited at the ${r.surplusCapHours?.toFixed(2)} h cap`}
              value={`−${r.forfeitedHours.toFixed(2)} h`}
              tone="warning"
            />
          )}

          <Row label="Closing balance" value={signedHours(r.closingBalanceHours)} strong />
        </dl>

        {(r.isAllIn || (r.overtimeLumpSumHours ?? 0) > 0) && (
          <p className="px-4 py-2 border-t text-xs text-muted-foreground">
            {r.isAllIn
              ? 'Your contract is all-in: additional hours are part of the salary, so a surplus is recorded and reported in full but is not banked as flexitime. A shortfall still counts.'
              : `Your contract includes an Überstundenpauschale of ${r.overtimeLumpSumHours?.toFixed(2)} h per month. That much surplus is already paid, so it is not banked again; anything above it is. A shortfall is never offset by it.`}
          </p>
        )}
      </section>

      {r.deficitFloorBreached && (
        <Banner tone="danger">
          Your balance is below the agreed floor of {r.deficitFloorHours?.toFixed(2)} h. This needs
          a decision by your employer — the hours have not been written off.
        </Banner>
      )}

      {!r.deficitFloorBreached && r.approachingCap && (
        <Banner tone="warning">
          Your balance is close to the {r.surplusCapHours?.toFixed(2)} h cap. Further surplus will
          be forfeited unless you take it as time off.
        </Banner>
      )}

      {/* Absence */}
      {r.absenceDays.length > 0 && (
        <section className="rounded-lg border">
          <header className="px-4 py-2 border-b bg-muted/50">
            <h2 className="text-sm font-semibold">Absence</h2>
          </header>
          <dl className="divide-y text-sm">
            {r.absenceDays.map(a => (
              <Row
                key={a.type}
                label={ABSENCE_TYPE_LABELS[a.type]}
                value={`${a.days} day${a.days === 1 ? '' : 's'}`}
              />
            ))}
          </dl>
        </section>
      )}

      <BreachList breaches={r.breaches} />

      <ReconciliationPanel year={r.year} month={r.month} userId={r.userId} />

      {/* Daily records */}
      <section className="rounded-lg border overflow-x-auto">
        <header className="px-4 py-2 border-b bg-muted/50">
          <h2 className="text-sm font-semibold">Daily records</h2>
        </header>

        <table className="w-full text-sm">
          <thead>
            <tr className="text-left border-b">
              <th className="px-4 py-2 font-medium">Date</th>
              <th className="px-4 py-2 font-medium">Start</th>
              <th className="px-4 py-2 font-medium">End</th>
              <th className="px-4 py-2 font-medium text-right">Break</th>
              <th className="px-4 py-2 font-medium text-right">Hours</th>
            </tr>
          </thead>
          <tbody>
            {r.days.map(day => (
              <tr key={day.id} className="border-t">
                <td className="px-4 py-1.5 tabular-nums">{day.date}</td>
                <td className="px-4 py-1.5 tabular-nums">{day.startTime.slice(0, 5)}</td>
                <td className="px-4 py-1.5 tabular-nums">
                  {day.endTime.slice(0, 5)}
                  {day.crossesMidnight && <span className="text-muted-foreground"> +1</span>}
                </td>
                <td className="px-4 py-1.5 text-right tabular-nums">{day.breakMinutes} min</td>
                <td className="px-4 py-1.5 text-right tabular-nums">{day.workedHours.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot className="bg-muted/50 font-semibold">
            <tr>
              <td className="px-4 py-2" colSpan={4}>Total</td>
              <td className="px-4 py-2 text-right tabular-nums">{r.actualHours.toFixed(2)} h</td>
            </tr>
          </tfoot>
        </table>
      </section>
    </div>
  )
}

function Stat({
  label, value, tone, muted,
}: { label: string; value: string; tone?: 'positive' | 'negative'; muted?: boolean }) {
  const color =
    muted ? 'text-muted-foreground'
      : tone === 'positive' ? 'text-emerald-400'
      : tone === 'negative' ? 'text-amber-400'
      : ''

  return (
    <div className="rounded-lg border p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={`text-xl font-bold tabular-nums ${color}`}>{value}</p>
    </div>
  )
}

function Row({
  label, value, strong, tone,
}: { label: string; value: string; strong?: boolean; tone?: 'warning' }) {
  return (
    <div className="flex justify-between px-4 py-2">
      <dt className={strong ? 'font-semibold' : ''}>{label}</dt>
      <dd className={`tabular-nums ${strong ? 'font-semibold' : ''} ${tone === 'warning' ? 'text-amber-400' : ''}`}>
        {value}
      </dd>
    </div>
  )
}

function Banner({
  tone, children,
}: { tone: 'info' | 'warning' | 'danger'; children: React.ReactNode }) {
  const styles = {
    info: 'border-blue-500/30 bg-blue-500/5 text-blue-400',
    warning: 'border-amber-500/30 bg-amber-500/5 text-amber-500',
    danger: 'border-rose-500/30 bg-rose-500/5 text-rose-400',
  }[tone]

  const Icon = tone === 'info' ? Info : AlertTriangle

  return (
    <div className={`rounded-lg border p-3 flex gap-2 text-sm ${styles}`}>
      <Icon className="h-4 w-4 shrink-0 mt-0.5" />
      <span className="text-muted-foreground">{children}</span>
    </div>
  )
}
