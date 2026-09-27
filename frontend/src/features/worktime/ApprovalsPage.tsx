import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getPendingTimesheets, approveTimesheet, returnTimesheet,
  type TimesheetSummary,
} from '@/api/timesheets'
import {
  getCompanyBreaches, BREACH_LABELS, formatBreachValues,
} from '@/api/worktime-settings'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Dialog } from '@/components/ui/dialog'
import { ClipboardCheck, Check, Undo2, AlertTriangle } from 'lucide-react'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

export function ApprovalsPage() {
  const queryClient = useQueryClient()
  const [returning, setReturning] = useState<TimesheetSummary | null>(null)
  const [acknowledging, setAcknowledging] = useState<TimesheetSummary | null>(null)
  const [reason, setReason] = useState('')

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['timesheets', 'pending'],
    queryFn: getPendingTimesheets,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['timesheets', 'pending'] })
    queryClient.invalidateQueries({ queryKey: ['timesheets', 'company'] })
  }

  const approve = useMutation({
    mutationFn: (vars: { id: string; acknowledgeBreaches: boolean }) =>
      approveTimesheet(vars.id, vars.acknowledgeBreaches),
    onSuccess: () => {
      toast.success('Timesheet approved.')
      setAcknowledging(null)
      invalidate()
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  // A month with breaches cannot be approved from the list — the approver has to see them first.
  const startApproval = (row: TimesheetSummary) => {
    if (row.breachCount > 0) {
      setAcknowledging(row)
      return
    }
    approve.mutate({ id: row.id, acknowledgeBreaches: false })
  }

  const sendBack = useMutation({
    mutationFn: (vars: { id: string; reason: string }) =>
      returnTimesheet(vars.id, vars.reason || undefined),
    onSuccess: () => {
      toast.success('Returned to the employee for correction.')
      setReturning(null)
      setReason('')
      invalidate()
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  if (isLoading) return <div className="p-6 text-sm text-muted-foreground">Loading…</div>
  if (isError) return <div className="p-6 text-sm text-destructive">{getApiErrorMessage(error)}</div>

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
          <ClipboardCheck className="h-6 w-6" />
          Working time approvals
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Months submitted by your team and waiting for a decision.
        </p>
      </div>

      {(data?.length ?? 0) === 0 ? (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          Nothing waiting for approval.
        </div>
      ) : (
        <div className="rounded-lg border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr className="text-left">
                <th className="px-4 py-2 font-medium">Employee</th>
                <th className="px-4 py-2 font-medium">Period</th>
                <th className="px-4 py-2 font-medium text-right">Recorded</th>
                <th className="px-4 py-2 font-medium">Issues</th>
                <th className="px-4 py-2 font-medium">Submitted</th>
                <th className="px-4 py-2 font-medium w-56" />
              </tr>
            </thead>
            <tbody>
              {data!.map(row => (
                <tr key={row.id} className="border-t">
                  <td className="px-4 py-2 font-medium">{row.employeeName}</td>
                  <td className="px-4 py-2">{MONTHS[row.month - 1]} {row.year}</td>
                  <td className="px-4 py-2 text-right tabular-nums">
                    {row.totalWorkedHours.toFixed(2)} h
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
                  </td>
                  <td className="px-4 py-2 text-muted-foreground">
                    {row.submittedAt ? new Date(row.submittedAt).toLocaleDateString() : '—'}
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="outline" size="sm" className="gap-1.5"
                        onClick={() => { setReturning(row); setReason('') }}
                      >
                        <Undo2 className="h-3.5 w-3.5" />
                        Return
                      </Button>
                      <Button
                        size="sm" className="gap-1.5"
                        disabled={approve.isPending}
                        onClick={() => startApproval(row)}
                      >
                        <Check className="h-3.5 w-3.5" />
                        {row.breachCount > 0 ? 'Review' : 'Approve'}
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Dialog
        open={returning !== null}
        onClose={() => setReturning(null)}
        title="Return for correction"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            {returning?.employeeName} will be notified and can edit the month again.
          </p>

          <Textarea
            value={reason}
            onChange={e => setReason(e.target.value)}
            placeholder="What needs correcting? (optional)"
            rows={3}
            maxLength={500}
          />

          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setReturning(null)}>Cancel</Button>
            <Button
              disabled={sendBack.isPending}
              onClick={() => returning && sendBack.mutate({ id: returning.id, reason })}
            >
              Return to employee
            </Button>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={acknowledging !== null}
        onClose={() => setAcknowledging(null)}
        title="Acknowledge working time issues"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            {acknowledging?.employeeName}'s {acknowledging && MONTHS[acknowledging.month - 1]}{' '}
            {acknowledging?.year} has{' '}
            <span className="font-medium text-foreground">
              {acknowledging?.breachCount} working time{' '}
              {acknowledging?.breachCount === 1 ? 'issue' : 'issues'}
            </span>
            . The recorded hours stand as entered — approving confirms you have seen the issues,
            and your acknowledgement is stored with the month.
          </p>

          {acknowledging && (
            <BreachSummary userId={acknowledging.userId} year={acknowledging.year} month={acknowledging.month} />
          )}

          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setAcknowledging(null)}>Cancel</Button>
            <Button
              disabled={approve.isPending}
              onClick={() => acknowledging &&
                approve.mutate({ id: acknowledging.id, acknowledgeBreaches: true })}
            >
              Acknowledge and approve
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

/** The breaches for one employee's month, pulled from the company overview endpoint. */
function BreachSummary({ userId, year, month }: { userId: string; year: number; month: number }) {
  const { data, isLoading } = useQuery({
    queryKey: ['timesheets', 'company', 'breaches', year, month],
    queryFn: () => getCompanyBreaches(year, month),
  })

  if (isLoading) return <p className="text-sm text-muted-foreground">Loading issues…</p>

  const mine = (data ?? []).filter(b => b.userId === userId)
  if (mine.length === 0) return null

  return (
    <ul className="space-y-1 text-sm rounded-md border p-3 max-h-52 overflow-y-auto">
      {mine.map((breach, i) => (
        <li key={i} className="flex gap-2">
          <span className="text-muted-foreground tabular-nums shrink-0 w-28">
            {breach.date ?? (breach.weekStartDate ? `week of ${breach.weekStartDate}` : '—')}
          </span>
          <span>
            <span className="font-medium">{BREACH_LABELS[breach.kind]}</span>
            {' — '}
            <span className="text-muted-foreground">{formatBreachValues(breach)}</span>
          </span>
        </li>
      ))}
    </ul>
  )
}
