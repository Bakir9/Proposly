import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getCompanyMonth, lockTimesheet, reopenTimesheet, type TimesheetStatus,
} from '@/api/timesheets'
import { getCompanyMonthOverview, signedHours } from '@/api/worktime-reports'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Button } from '@/components/ui/button'
import { ChevronLeft, ChevronRight, Users, Lock, RotateCcw, AlertTriangle } from 'lucide-react'

const STATUS_BADGE: Record<TimesheetStatus, string> = {
  Draft: 'bg-slate-500/15 text-slate-400 border-slate-500/20',
  Submitted: 'bg-blue-500/15 text-blue-400 border-blue-500/20',
  Approved: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20',
  Locked: 'bg-amber-500/15 text-amber-400 border-amber-500/20',
}

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

export function CompanyOverviewPage() {
  const queryClient = useQueryClient()
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  const queryKey = ['timesheets', 'company', year, month]

  const { data, isLoading, isError, error } = useQuery({
    queryKey,
    queryFn: () => getCompanyMonth(year, month),
  })

  // Month-end figures live in the reports endpoint; the timesheet list carries status and hours.
  // Keyed by user so the two can be shown side by side without a second round trip per row.
  const { data: figures } = useQuery({
    queryKey: ['worktime', 'company-overview', year, month],
    queryFn: () => getCompanyMonthOverview(year, month),
  })

  const figuresByUser = new Map((figures ?? []).map(f => [f.userId, f]))

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const lock = useMutation({
    mutationFn: (id: string) => lockTimesheet(id),
    onSuccess: () => { toast.success('Month closed.'); invalidate() },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const reopen = useMutation({
    mutationFn: (id: string) => reopenTimesheet(id),
    onSuccess: () => { toast.success('Month reopened for editing.'); invalidate() },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const shiftMonth = (delta: number) => {
    const next = new Date(year, month - 1 + delta, 1)
    setYear(next.getFullYear())
    setMonth(next.getMonth() + 1)
  }

  const totalHours = data?.reduce((sum, row) => sum + row.totalWorkedHours, 0) ?? 0

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <Users className="h-6 w-6" />
            Company working time
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Every employee's month in one view.
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
        </div>
      </div>

      {isLoading && <div className="text-sm text-muted-foreground">Loading…</div>}
      {isError && <div className="text-sm text-destructive">{getApiErrorMessage(error)}</div>}

      {!isLoading && !isError && (
        (data?.length ?? 0) === 0 ? (
          <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
            Nobody has recorded working time for this month yet.
          </div>
        ) : (
          <div className="rounded-lg border overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr className="text-left">
                  <th className="px-4 py-2 font-medium">Employee</th>
                  <th className="px-4 py-2 font-medium">Status</th>
                  <th className="px-4 py-2 font-medium text-right">Target</th>
                  <th className="px-4 py-2 font-medium text-right">Recorded</th>
                  <th className="px-4 py-2 font-medium text-right">Difference</th>
                  <th className="px-4 py-2 font-medium text-right">Balance</th>
                  <th className="px-4 py-2 font-medium">Issues</th>
                  <th className="px-4 py-2 font-medium w-48" />
                </tr>
              </thead>
              <tbody>
                {data!.map(row => (
                  <tr key={row.id} className="border-t">
                    <td className="px-4 py-2 font-medium">{row.employeeName}</td>
                    <td className="px-4 py-2">
                      <span className={`text-xs px-2 py-0.5 rounded-md border font-medium ${STATUS_BADGE[row.status]}`}>
                        {row.status}
                      </span>
                      {row.isSelfApproved && (
                        <span className="ml-2 text-[11px] text-muted-foreground">self-approved</span>
                      )}
                    </td>
                    <td className="px-4 py-2 text-right tabular-nums text-muted-foreground">
                      {figuresByUser.get(row.userId)?.targetHours?.toFixed(2) ?? '—'}
                    </td>
                    <td className="px-4 py-2 text-right tabular-nums">
                      {row.totalWorkedHours.toFixed(2)} h
                    </td>
                    <td className="px-4 py-2 text-right tabular-nums">
                      {(() => {
                        const diff = figuresByUser.get(row.userId)?.monthlyDifference
                        if (diff === null || diff === undefined) return <span className="text-muted-foreground">—</span>
                        return (
                          <span className={diff >= 0 ? 'text-emerald-400' : 'text-amber-400'}>
                            {signedHours(diff)}
                          </span>
                        )
                      })()}
                    </td>
                    <td className="px-4 py-2 text-right tabular-nums">
                      {(() => {
                        const f = figuresByUser.get(row.userId)
                        if (!f) return <span className="text-muted-foreground">—</span>
                        return (
                          <>
                            {signedHours(f.closingBalanceHours)}
                            {f.forfeitedHours > 0 && (
                              <span className="block text-[11px] text-amber-400">
                                −{f.forfeitedHours.toFixed(2)} forfeited
                              </span>
                            )}
                          </>
                        )
                      })()}
                    </td>
                    <td className="px-4 py-2">
                      {row.breachCount > 0 ? (
                        <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-md border font-medium bg-amber-500/15 text-amber-400 border-amber-500/20">
                          <AlertTriangle className="h-3 w-3" />
                          {row.breachCount}
                        </span>
                      ) : (
                        <span className="text-muted-foreground text-xs">none</span>
                      )}
                      {figuresByUser.get(row.userId)?.isRevised && (
                        <span className="block text-[11px] text-amber-400 mt-0.5">revised</span>
                      )}
                    </td>
                    <td className="px-4 py-2">
                      <div className="flex justify-end gap-2">
                        {row.status === 'Approved' && (
                          <Button
                            variant="outline" size="sm" className="gap-1.5"
                            disabled={lock.isPending}
                            onClick={() => lock.mutate(row.id)}
                          >
                            <Lock className="h-3.5 w-3.5" />
                            Close
                          </Button>
                        )}
                        {(row.status === 'Approved' || row.status === 'Locked') && (
                          <Button
                            variant="ghost" size="sm" className="gap-1.5"
                            disabled={reopen.isPending}
                            onClick={() => reopen.mutate(row.id)}
                          >
                            <RotateCcw className="h-3.5 w-3.5" />
                            Reopen
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="bg-muted/50 font-semibold">
                <tr>
                  <td className="px-4 py-2" colSpan={3}>Total</td>
                  <td className="px-4 py-2 text-right tabular-nums">{totalHours.toFixed(2)} h</td>
                  <td colSpan={4} />
                </tr>
              </tfoot>
            </table>
          </div>
        )
      )}
    </div>
  )
}
