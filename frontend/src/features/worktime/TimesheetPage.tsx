import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getMyTimesheet, upsertWorkDay, deleteWorkDay, submitTimesheet,
  type TimesheetStatus, type WorkDay,
} from '@/api/timesheets'
import { breachedDates } from '@/api/worktime-settings'
import { getApiErrorMessage } from '@/lib/api-errors'
import { BreachList } from './BreachList'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ChevronLeft, ChevronRight, Clock, Send, Trash2, Lock, RotateCcw, AlertTriangle } from 'lucide-react'

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

interface DayRow {
  date: string
  dayOfMonth: number
  weekday: string
  isWeekend: boolean
  entry: WorkDay | undefined
}

function buildMonth(year: number, month: number, days: WorkDay[]): DayRow[] {
  const byDate = new Map(days.map(d => [d.date, d]))
  const total = new Date(year, month, 0).getDate()

  return Array.from({ length: total }, (_, i) => {
    const dayOfMonth = i + 1
    const jsDate = new Date(year, month - 1, dayOfMonth)
    const date = `${year}-${String(month).padStart(2, '0')}-${String(dayOfMonth).padStart(2, '0')}`
    const weekdayIndex = jsDate.getDay()

    return {
      date,
      dayOfMonth,
      weekday: jsDate.toLocaleDateString(undefined, { weekday: 'short' }),
      isWeekend: weekdayIndex === 0 || weekdayIndex === 6,
      entry: byDate.get(date),
    }
  })
}

function formatHours(hours: number) {
  return `${hours.toFixed(2)} h`
}

export function TimesheetPage() {
  const queryClient = useQueryClient()
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  const queryKey = ['timesheet', year, month]

  const { data, isLoading, isError, error } = useQuery({
    queryKey,
    queryFn: () => getMyTimesheet(year, month),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const saveDay = useMutation({
    mutationFn: (vars: { date: string; startTime: string; endTime: string; breakMinutes: number; crossesMidnight: boolean }) =>
      upsertWorkDay(year, month, vars.date, {
        startTime: vars.startTime,
        endTime: vars.endTime,
        breakMinutes: vars.breakMinutes,
        crossesMidnight: vars.crossesMidnight,
      }),
    onSuccess: invalidate,
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const removeDay = useMutation({
    mutationFn: (date: string) => deleteWorkDay(year, month, date),
    onSuccess: invalidate,
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const submit = useMutation({
    mutationFn: () => submitTimesheet(year, month),
    onSuccess: () => {
      toast.success('Timesheet submitted for approval.')
      invalidate()
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const rows = useMemo(() => buildMonth(year, month, data?.days ?? []), [year, month, data])
  const flagged = useMemo(() => breachedDates(data?.breaches ?? []), [data])

  const shiftMonth = (delta: number) => {
    const next = new Date(year, month - 1 + delta, 1)
    setYear(next.getFullYear())
    setMonth(next.getMonth() + 1)
  }

  const isEditable = (data?.status ?? 'Draft') === 'Draft'
  const recordedDays = data?.days.length ?? 0

  if (isLoading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading timesheet…</div>
  }

  if (isError) {
    return <div className="p-6 text-sm text-destructive">{getApiErrorMessage(error)}</div>
  }

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <Clock className="h-6 w-6" />
            My working time
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Record your start, end and break for each day. You can fill in a whole week at once.
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

      <div className="flex items-center gap-3 flex-wrap">
        <span className={`text-xs px-2 py-1 rounded-md border font-medium ${STATUS_BADGE[data?.status ?? 'Draft']}`}>
          {data?.status ?? 'Draft'}
        </span>
        <span className="text-sm text-muted-foreground">
          {recordedDays} {recordedDays === 1 ? 'day' : 'days'} recorded
        </span>
        {data?.isSelfApproved && (
          <span className="text-xs text-muted-foreground inline-flex items-center gap-1">
            <Lock className="h-3 w-3" /> self-approved
          </span>
        )}
        {data?.reopenedAt && (
          <span className="text-xs text-muted-foreground inline-flex items-center gap-1">
            <RotateCcw className="h-3 w-3" /> reopened
          </span>
        )}
      </div>

      <BreachList breaches={data?.breaches ?? []} />

      <div className="rounded-lg border overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr className="text-left">
              <th className="px-3 py-2 font-medium w-28">Day</th>
              <th className="px-3 py-2 font-medium w-28">Start</th>
              <th className="px-3 py-2 font-medium w-28">End</th>
              <th className="px-3 py-2 font-medium w-24">Break</th>
              <th className="px-3 py-2 font-medium w-24 text-right">Hours</th>
              <th className="px-3 py-2 font-medium w-16" />
            </tr>
          </thead>
          <tbody>
            {rows.map(row => (
              <DayEditor
                key={row.date}
                row={row}
                editable={isEditable}
                flagged={flagged.has(row.date)}
                onSave={vars => saveDay.mutate(vars)}
                onRemove={() => removeDay.mutate(row.date)}
              />
            ))}
          </tbody>
          <tfoot className="bg-muted/50 font-semibold">
            <tr>
              <td className="px-3 py-2" colSpan={4}>Month total</td>
              <td className="px-3 py-2 text-right">{formatHours(data?.totalWorkedHours ?? 0)}</td>
              <td />
            </tr>
          </tfoot>
        </table>
      </div>

      {isEditable && (
        <div className="flex justify-end">
          <Button
            onClick={() => submit.mutate()}
            disabled={submit.isPending || recordedDays === 0}
            className="gap-2"
          >
            <Send className="h-4 w-4" />
            Submit for approval
          </Button>
        </div>
      )}

      {!isEditable && (
        <p className="text-sm text-muted-foreground">
          This month is {data?.status.toLowerCase()} and can no longer be edited. Ask an approver to
          reopen it if something needs correcting.
        </p>
      )}
    </div>
  )
}

interface DayEditorProps {
  row: DayRow
  editable: boolean
  flagged: boolean
  onSave: (vars: { date: string; startTime: string; endTime: string; breakMinutes: number; crossesMidnight: boolean }) => void
  onRemove: () => void
}

function DayEditor({ row, editable, flagged, onSave, onRemove }: DayEditorProps) {
  const [start, setStart] = useState(row.entry?.startTime.slice(0, 5) ?? '')
  const [end, setEnd] = useState(row.entry?.endTime.slice(0, 5) ?? '')
  const [breakMinutes, setBreakMinutes] = useState(String(row.entry?.breakMinutes ?? ''))

  const commit = () => {
    if (!editable || !start || !end) return

    const minutes = Number(breakMinutes || 0)
    const unchanged =
      row.entry &&
      row.entry.startTime.slice(0, 5) === start &&
      row.entry.endTime.slice(0, 5) === end &&
      row.entry.breakMinutes === minutes

    if (unchanged) return

    // An end at or before the start is treated as a night shift rather than rejected outright.
    onSave({
      date: row.date,
      startTime: start,
      endTime: end,
      breakMinutes: minutes,
      crossesMidnight: end <= start,
    })
  }

  return (
    <tr className={
      flagged ? 'bg-amber-500/10 border-t' : row.isWeekend ? 'bg-muted/20 border-t' : 'border-t'
    }>
      <td className="px-3 py-1.5 whitespace-nowrap">
        <span className="text-muted-foreground">{row.weekday}</span>{' '}
        <span className="font-medium">{row.dayOfMonth}</span>
        {flagged && (
          <AlertTriangle
            className="h-3 w-3 inline-block ml-1.5 text-amber-500 align-baseline"
            aria-label="Working time issue on this day"
          />
        )}
      </td>
      <td className="px-3 py-1.5">
        <Input
          type="time" value={start} disabled={!editable}
          onChange={e => setStart(e.target.value)} onBlur={commit}
          className="h-8"
        />
      </td>
      <td className="px-3 py-1.5">
        <Input
          type="time" value={end} disabled={!editable}
          onChange={e => setEnd(e.target.value)} onBlur={commit}
          className="h-8"
        />
      </td>
      <td className="px-3 py-1.5">
        <Input
          type="number" min={0} value={breakMinutes} disabled={!editable} placeholder="min"
          onChange={e => setBreakMinutes(e.target.value)} onBlur={commit}
          className="h-8"
        />
      </td>
      <td className="px-3 py-1.5 text-right tabular-nums">
        {row.entry ? formatHours(row.entry.workedHours) : <span className="text-muted-foreground">—</span>}
      </td>
      <td className="px-3 py-1.5">
        {row.entry && editable && (
          <Button variant="ghost" size="sm" onClick={onRemove} aria-label={`Clear ${row.date}`}>
            <Trash2 className="h-3.5 w-3.5" />
          </Button>
        )}
      </td>
    </tr>
  )
}
