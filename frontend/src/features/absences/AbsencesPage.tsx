import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { toast } from 'sonner'
import {
  getAbsences, getEntitlement, previewAbsence, requestAbsence, cancelAbsence,
  approveAbsence, rejectAbsence,
  ABSENCE_TYPE_LABELS, CONSUMES_ENTITLEMENT,
  type Absence, type AbsenceStatus, type AbsenceType,
} from '@/api/absences'
import { useAuth } from '@/features/auth/AuthContext'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Dialog } from '@/components/ui/dialog'
import { CalendarOff, Plus, X, Check, AlertTriangle } from 'lucide-react'

const STATUS_BADGE: Record<AbsenceStatus, string> = {
  Pending: 'bg-blue-500/15 text-blue-400 border-blue-500/20',
  Approved: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20',
  Rejected: 'bg-rose-500/15 text-rose-400 border-rose-500/20',
  Cancelled: 'bg-slate-500/15 text-slate-400 border-slate-500/20',
}

const TYPES = Object.keys(ABSENCE_TYPE_LABELS) as AbsenceType[]

function today() {
  return new Date().toISOString().slice(0, 10)
}

export function AbsencesPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [year, setYear] = useState(new Date().getFullYear())
  const [requesting, setRequesting] = useState(false)
  const [rejecting, setRejecting] = useState<Absence | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const { data: absences, isLoading } = useQuery({
    queryKey: ['absences', year],
    queryFn: () => getAbsences(year),
  })

  const { data: entitlement, error: entitlementError } = useQuery({
    queryKey: ['absences', 'entitlement', year],
    queryFn: () => getEntitlement(year),
    retry: false,
  })

  const noEntitlement = axios.isAxiosError(entitlementError) && entitlementError.response?.status === 404

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['absences'] })

  const cancel = useMutation({
    mutationFn: (id: string) => cancelAbsence(id),
    onSuccess: () => { toast.success('Absence cancelled.'); invalidate() },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

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
      setRejectReason('')
      invalidate()
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  // Admins see the whole team's absences here; everyone else only their own.
  const myId = user?.userId
  const hasOthers = (absences ?? []).some(a => a.userId !== myId)

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <CalendarOff className="h-6 w-6" />
            Time off
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            {hasOthers
              ? 'Your own and your team’s absences — pending requests can be decided right here.'
              : 'Your vacation, sick leave and other absences.'}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Select value={String(year)} onChange={e => setYear(Number(e.target.value))}>
            {[year - 1, year, year + 1].map(y => (
              <option key={y} value={y}>{y}</option>
            ))}
          </Select>
          <Button onClick={() => setRequesting(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Request time off
          </Button>
        </div>
      </div>

      {entitlement && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <Stat label="Entitled" value={entitlement.entitledDays} />
          <Stat label="Carried over" value={entitlement.carriedOverDays} />
          <Stat label="Used" value={entitlement.usedDays} />
          <Stat label="Remaining" value={entitlement.remainingDays} emphasis />
        </div>
      )}

      {noEntitlement && (
        <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-4 text-sm">
          <p className="font-medium text-amber-500">No vacation entitlement set for {year}</p>
          <p className="text-muted-foreground mt-1">
            Ask an administrator to set your allowance. You can still request sick leave and other
            absence types, which do not draw on it.
          </p>
        </div>
      )}

      {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}

      {!isLoading && (absences?.length ?? 0) === 0 && (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          Nothing recorded for {year}.
        </div>
      )}

      {(absences?.length ?? 0) > 0 && (
        <div className="rounded-lg border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr className="text-left">
                {hasOthers && <th className="px-4 py-2 font-medium">Employee</th>}
                <th className="px-4 py-2 font-medium">Type</th>
                <th className="px-4 py-2 font-medium">From</th>
                <th className="px-4 py-2 font-medium">To</th>
                <th className="px-4 py-2 font-medium text-right">Days</th>
                <th className="px-4 py-2 font-medium">Status</th>
                <th className="px-4 py-2 font-medium w-48" />
              </tr>
            </thead>
            <tbody>
              {absences!.map(row => {
                const isMine = row.userId === myId
                return (
                <tr key={row.id} className="border-t">
                  {hasOthers && (
                    <td className="px-4 py-2 font-medium whitespace-nowrap">
                      {row.employeeName}
                      {isMine && <span className="ml-1.5 text-[11px] font-semibold text-primary">(me)</span>}
                    </td>
                  )}
                  <td className="px-4 py-2 font-medium">{ABSENCE_TYPE_LABELS[row.type]}</td>
                  <td className="px-4 py-2 tabular-nums">
                    {row.startDate}{row.firstDayIsHalf && <span className="text-muted-foreground"> ½</span>}
                  </td>
                  <td className="px-4 py-2 tabular-nums">
                    {row.endDate}{row.lastDayIsHalf && <span className="text-muted-foreground"> ½</span>}
                  </td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.consumedDays}</td>
                  <td className="px-4 py-2">
                    <span className={`text-xs px-2 py-0.5 rounded-md border font-medium ${STATUS_BADGE[row.status]}`}>
                      {row.status}
                    </span>
                    {row.approverName && row.status !== 'Pending' && row.status !== 'Cancelled' && (
                      <p className="text-xs text-muted-foreground mt-1">by {row.approverName}</p>
                    )}
                    {row.decisionReason && (
                      <p className="text-xs text-muted-foreground mt-1">{row.decisionReason}</p>
                    )}
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex items-center justify-end gap-1.5">
                      {/* Someone else's pending request: the admin decides it right here. */}
                      {!isMine && row.status === 'Pending' && (
                        <>
                          <Button
                            variant="outline" size="sm" className="gap-1.5"
                            onClick={() => { setRejecting(row); setRejectReason('') }}
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
                        </>
                      )}
                      {(row.status === 'Approved' || (isMine && row.status === 'Pending')) && (
                        <Button
                          variant="ghost" size="sm" className="gap-1.5"
                          disabled={cancel.isPending}
                          onClick={() => cancel.mutate(row.id)}
                        >
                          <X className="h-3.5 w-3.5" />
                          Cancel
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      <RequestDialog
        open={requesting}
        onClose={() => setRequesting(false)}
        onDone={() => { setRequesting(false); invalidate() }}
      />

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
            value={rejectReason}
            onChange={e => setRejectReason(e.target.value)}
            placeholder="Why is this being declined?"
            rows={3}
            maxLength={500}
          />
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setRejecting(null)}>Cancel</Button>
            <Button
              disabled={rejectReason.trim() === '' || reject.isPending}
              onClick={() => rejecting && reject.mutate({ id: rejecting.id, reason: rejectReason })}
            >
              Decline request
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

function Stat({ label, value, emphasis }: { label: string; value: number; emphasis?: boolean }) {
  return (
    <div className="rounded-lg border p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={`text-xl font-bold tabular-nums ${emphasis ? 'text-emerald-400' : ''}`}>
        {value}
      </p>
    </div>
  )
}

function RequestDialog({
  open, onClose, onDone,
}: { open: boolean; onClose: () => void; onDone: () => void }) {
  const [type, setType] = useState<AbsenceType>('Vacation')
  const [startDate, setStartDate] = useState(today())
  const [endDate, setEndDate] = useState(today())
  const [firstHalf, setFirstHalf] = useState(false)
  const [lastHalf, setLastHalf] = useState(false)
  const [reason, setReason] = useState('')

  const valid = startDate !== '' && endDate !== '' && endDate >= startDate

  // Shows the cost and the resulting balance live, before anything is submitted.
  const { data: preview } = useQuery({
    queryKey: ['absences', 'preview', type, startDate, endDate, firstHalf, lastHalf],
    queryFn: () => previewAbsence(type, startDate, endDate, firstHalf, lastHalf),
    enabled: open && valid,
  })

  const submit = useMutation({
    mutationFn: () => requestAbsence({
      type, startDate, endDate,
      firstDayIsHalf: firstHalf, lastDayIsHalf: lastHalf,
      reason: reason || null,
    }),
    onSuccess: () => { toast.success('Request submitted for approval.'); onDone() },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const blocked = preview?.overlapsExisting || preview?.exceedsEntitlement

  return (
    <Dialog open={open} onClose={onClose} title="Request time off">
      <div className="space-y-4">
        <div className="space-y-1.5">
          <Label>Type</Label>
          <Select value={type} onChange={e => setType(e.target.value as AbsenceType)}>
            {TYPES.map(t => (
              <option key={t} value={t}>{ABSENCE_TYPE_LABELS[t]}</option>
            ))}
          </Select>
          {!CONSUMES_ENTITLEMENT[type] && (
            <p className="text-xs text-muted-foreground">
              Does not use your vacation allowance.
            </p>
          )}
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <Label>From</Label>
            <Input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
            <label className="flex items-center gap-2 text-xs text-muted-foreground">
              <input type="checkbox" checked={firstHalf} onChange={e => setFirstHalf(e.target.checked)} />
              Half day
            </label>
          </div>

          <div className="space-y-1.5">
            <Label>To</Label>
            <Input type="date" value={endDate} min={startDate} onChange={e => setEndDate(e.target.value)} />
            <label className="flex items-center gap-2 text-xs text-muted-foreground">
              <input type="checkbox" checked={lastHalf} onChange={e => setLastHalf(e.target.checked)} />
              Half day
            </label>
          </div>
        </div>

        {preview && valid && (
          <div className="rounded-md border p-3 text-sm space-y-1">
            <p>
              <span className="font-medium">{preview.consumedDays}</span> working day(s)
              {preview.nonWorkingDatesExcluded.length > 0 && (
                <span className="text-muted-foreground">
                  {' '}· {preview.nonWorkingDatesExcluded.length} non-working day(s) excluded
                </span>
              )}
            </p>

            {preview.remainingDaysAfter !== null && (
              <p className="text-muted-foreground">
                {preview.remainingDaysAfter} day(s) would remain
              </p>
            )}

            {preview.overlapsExisting && (
              <p className="flex items-center gap-1.5 text-amber-500">
                <AlertTriangle className="h-3.5 w-3.5" />
                This overlaps an absence you already have
              </p>
            )}

            {preview.exceedsEntitlement && (
              <p className="flex items-center gap-1.5 text-amber-500">
                <AlertTriangle className="h-3.5 w-3.5" />
                This exceeds your remaining allowance
              </p>
            )}
          </div>
        )}

        <div className="space-y-1.5">
          <Label>Note (optional)</Label>
          <Textarea
            value={reason}
            onChange={e => setReason(e.target.value)}
            rows={2}
            maxLength={500}
            placeholder="Anything your approver should know"
          />
        </div>

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button
            disabled={!valid || blocked || submit.isPending}
            onClick={() => submit.mutate()}
          >
            Submit request
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
