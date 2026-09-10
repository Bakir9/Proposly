import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getPendingAbsences, approveAbsence, rejectAbsence,
  ABSENCE_TYPE_LABELS, CONSUMES_ENTITLEMENT, type Absence,
} from '@/api/absences'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Dialog } from '@/components/ui/dialog'
import { CalendarCheck, Check, X } from 'lucide-react'

export function AbsenceApprovalsPage() {
  const queryClient = useQueryClient()
  const [rejecting, setRejecting] = useState<Absence | null>(null)
  const [reason, setReason] = useState('')

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['absences', 'pending'],
    queryFn: getPendingAbsences,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['absences'] })

  const approve = useMutation({
    mutationFn: (id: string) => approveAbsence(id),
    onSuccess: () => { toast.success('Time off approved.'); invalidate() },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const reject = useMutation({
    mutationFn: (vars: { id: string; reason: string }) => rejectAbsence(vars.id, vars.reason),
    onSuccess: () => {
      toast.success('Request declined. The employee has been notified with your reason.')
      setRejecting(null)
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
          <CalendarCheck className="h-6 w-6" />
          Time off approvals
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Requests from your team waiting for a decision.
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
                <th className="px-4 py-2 font-medium">Type</th>
                <th className="px-4 py-2 font-medium">Dates</th>
                <th className="px-4 py-2 font-medium text-right">Days</th>
                <th className="px-4 py-2 font-medium">Note</th>
                <th className="px-4 py-2 font-medium w-52" />
              </tr>
            </thead>
            <tbody>
              {data!.map(row => (
                <tr key={row.id} className="border-t">
                  <td className="px-4 py-2 font-medium">{row.employeeName}</td>
                  <td className="px-4 py-2">
                    {ABSENCE_TYPE_LABELS[row.type]}
                    {!CONSUMES_ENTITLEMENT[row.type] && (
                      <span className="block text-[11px] text-muted-foreground">
                        no allowance used
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-2 tabular-nums whitespace-nowrap">
                    {row.startDate} → {row.endDate}
                  </td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.consumedDays}</td>
                  <td className="px-4 py-2 text-muted-foreground max-w-xs truncate">
                    {row.reason ?? '—'}
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="outline" size="sm" className="gap-1.5"
                        onClick={() => { setRejecting(row); setReason('') }}
                      >
                        <X className="h-3.5 w-3.5" />
                        Decline
                      </Button>
                      <Button
                        size="sm" className="gap-1.5"
                        disabled={approve.isPending}
                        onClick={() => approve.mutate(row.id)}
                      >
                        <Check className="h-3.5 w-3.5" />
                        Approve
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
        open={rejecting !== null}
        onClose={() => setRejecting(null)}
        title="Decline this request"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            {rejecting?.employeeName} will be notified with your reason. No allowance is used.
          </p>

          <Textarea
            value={reason}
            onChange={e => setReason(e.target.value)}
            placeholder="Why is this being declined?"
            rows={3}
            maxLength={500}
          />

          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setRejecting(null)}>Cancel</Button>
            <Button
              disabled={reason.trim() === '' || reject.isPending}
              onClick={() => rejecting && reject.mutate({ id: rejecting.id, reason })}
            >
              Decline request
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
