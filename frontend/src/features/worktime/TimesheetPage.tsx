import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getMyTimesheet, upsertWorkDay, deleteWorkDay, submitTimesheet,
  type TimesheetStatus, type WorkDay,
} from '@/api/timesheets'
import { breachedDates } from '@/api/worktime-settings'
import {
  getMyEmploymentTerms, getMyTargetHours, getNonWorkingDays,
  type EmploymentTerms, type WeekDayName,
} from '@/api/worktime-calendar'
import { getApiErrorMessage } from '@/lib/api-errors'
import { BreachList } from './BreachList'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Dialog } from '@/components/ui/dialog'
import {
  ChevronLeft, ChevronRight, Clock, Send, Trash2, Lock, RotateCcw, AlertTriangle, Plus,
} from 'lucide-react'

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

/** Sunday-first, matching `Date.getDay()`. */
const WEEKDAY_NAMES: WeekDayName[] = [
  'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday',
]

const DEFAULT_PATTERN: WeekDayName[] = [
  'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday',
]

/** Falls back to a standard 8-hour day when no employment terms cover the month. */
const FALLBACK_DAILY_HOURS = 8

interface DayRow {
  date: string
  dayOfMonth: number
  weekday: string
  isWeekend: boolean
  /** A day the employee is contracted to work: in their pattern and not a holiday or closure. */
  isExpectedWorkday: boolean
  nonWorkingName: string | null
  entry: WorkDay | undefined
}

interface WeekGroup {
  week: number
  label: string
  days: DayRow[]
  recordedHours: number
  targetHours: number
}

/**
 * ISO-8601 week number — the Thursday of the same week decides which year and week it belongs to,
 * which is what makes a week spanning New Year land consistently.
 */
function isoWeek(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7))
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  return Math.ceil(((d.getTime() - yearStart.getTime()) / 86_400_000 + 1) / 7)
}

function toIsoDate(year: number, month: number, day: number) {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

function formatHours(hours: number) {
  return hours.toFixed(2)
}

/** 8 rather than 8.00, 7.7 rather than 7.70 — this is a contract figure, not a total. */
function trimHours(hours: number) {
  return String(Number(hours.toFixed(2)))
}

function minutesToTime(minutes: number) {
  const wrapped = ((minutes % 1440) + 1440) % 1440
  return `${String(Math.floor(wrapped / 60)).padStart(2, '0')}:${String(wrapped % 60).padStart(2, '0')}`
}

/**
 * The times a "fill the week" click writes: an 08:00 start, the statutory half-hour break once the
 * day passes six hours, and an end that lands exactly on the contracted daily hours.
 */
function defaultTimesFor(dailyHours: number) {
  const breakMinutes = dailyHours > 6 ? 30 : 0
  const start = 8 * 60

  return {
    startTime: minutesToTime(start),
    endTime: minutesToTime(start + Math.round(dailyHours * 60) + breakMinutes),
    breakMinutes,
  }
}

/** The terms version in force on the last day of the month, matching how the month is reported. */
function termsForMonth(history: EmploymentTerms[] | undefined, lastDay: string) {
  return history?.find(t => t.validFrom <= lastDay && (t.validTo === null || t.validTo >= lastDay))
}

function buildWeeks(
  year: number,
  month: number,
  days: WorkDay[],
  pattern: Set<WeekDayName>,
  nonWorking: Map<string, string>,
  dailyTarget: number,
): WeekGroup[] {
  const byDate = new Map(days.map(d => [d.date, d]))
  const total = new Date(year, month, 0).getDate()
  const monthLabel = new Date(year, month - 1, 1)
    .toLocaleDateString(undefined, { month: 'short' })

  const weeks: WeekGroup[] = []

  for (let dayOfMonth = 1; dayOfMonth <= total; dayOfMonth++) {
    const jsDate = new Date(year, month - 1, dayOfMonth)
    const date = toIsoDate(year, month, dayOfMonth)
    const weekdayIndex = jsDate.getDay()
    const holiday = nonWorking.get(date) ?? null

    const row: DayRow = {
      date,
      dayOfMonth,
      weekday: jsDate.toLocaleDateString(undefined, { weekday: 'short' }),
      isWeekend: weekdayIndex === 0 || weekdayIndex === 6,
      isExpectedWorkday: pattern.has(WEEKDAY_NAMES[weekdayIndex]) && holiday === null,
      nonWorkingName: holiday,
      entry: byDate.get(date),
    }

    const week = isoWeek(jsDate)
    let group = weeks.at(-1)

    if (!group || group.week !== week) {
      group = { week, label: '', days: [], recordedHours: 0, targetHours: 0 }
      weeks.push(group)
    }

    group.days.push(row)
    group.recordedHours += row.entry?.workedHours ?? 0
    if (row.isExpectedWorkday) group.targetHours += dailyTarget
  }

  for (const group of weeks) {
    const first = group.days[0].dayOfMonth
    const last = group.days.at(-1)!.dayOfMonth
    group.label = `${monthLabel} ${first} – ${last}`
  }

  return weeks
}

export function TimesheetPage() {
  const queryClient = useQueryClient()
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const [clearing, setClearing] = useState<WeekGroup | null>(null)

  const queryKey = ['timesheet', year, month]
  const daysInMonth = new Date(year, month, 0).getDate()
  const firstDay = toIsoDate(year, month, 1)
  const lastDay = toIsoDate(year, month, daysInMonth)

  const { data, isLoading, isError, error } = useQuery({
    queryKey,
    queryFn: () => getMyTimesheet(year, month),
  })

  const { data: terms } = useQuery({
    queryKey: ['worktime', 'terms', 'mine'],
    queryFn: getMyEmploymentTerms,
  })

  const { data: target } = useQuery({
    queryKey: ['worktime', 'target', 'mine', year, month],
    queryFn: () => getMyTargetHours(year, month),
  })

  const { data: nonWorkingDays } = useQuery({
    queryKey: ['worktime', 'non-working-days', firstDay, lastDay],
    queryFn: () => getNonWorkingDays(firstDay, lastDay),
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

  // Days are written one at a time rather than in parallel: they all land on the same timesheet
  // aggregate, and concurrent saves would race each other's version of it.
  const fillWeek = useMutation({
    mutationFn: async (vars: { dates: string[]; startTime: string; endTime: string; breakMinutes: number }) => {
      for (const date of vars.dates) {
        await upsertWorkDay(year, month, date, {
          startTime: vars.startTime,
          endTime: vars.endTime,
          breakMinutes: vars.breakMinutes,
          crossesMidnight: false,
        })
      }
      return vars.dates.length
    },
    onSuccess: (count) => {
      toast.success(`${count} ${count === 1 ? 'day' : 'days'} filled. Adjust the ones that differed.`)
      invalidate()
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const clearWeek = useMutation({
    mutationFn: async (dates: string[]) => {
      for (const date of dates) await deleteWorkDay(year, month, date)
      return dates.length
    },
    onSuccess: (count) => {
      toast.success(`${count} ${count === 1 ? 'day' : 'days'} cleared.`)
      setClearing(null)
      invalidate()
    },
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

  const activeTerms = useMemo(() => termsForMonth(terms, lastDay), [terms, lastDay])

  const pattern = useMemo(
    () => new Set<WeekDayName>(activeTerms?.workingDays ?? DEFAULT_PATTERN),
    [activeTerms],
  )

  const dailyTarget = activeTerms?.dailyHours ?? FALLBACK_DAILY_HOURS

  const nonWorking = useMemo(
    () => new Map((nonWorkingDays ?? []).map(d => [d.date, d.name])),
    [nonWorkingDays],
  )

  const weeks = useMemo(
    () => buildWeeks(year, month, data?.days ?? [], pattern, nonWorking, dailyTarget),
    [year, month, data, pattern, nonWorking, dailyTarget],
  )

  const flagged = useMemo(() => breachedDates(data?.breaches ?? []), [data])

  const rows = useMemo(() => weeks.flatMap(w => w.days), [weeks])
  const expectedWorkdays = rows.filter(r => r.isExpectedWorkday).length
  const filledWorkdays = rows.filter(r => r.isExpectedWorkday && r.entry).length
  const workdaysLeft = Math.max(expectedWorkdays - filledWorkdays, 0)
  const progress = expectedWorkdays === 0 ? 0 : Math.round((filledWorkdays / expectedWorkdays) * 100)

  const recordedHours = data?.totalWorkedHours ?? 0
  const recordedDays = data?.days.length ?? 0
  const status = data?.status ?? 'Draft'
  const isEditable = status === 'Draft'

  // Only the days already filled are counted, so an unworked future does not read as a deficit.
  const balanceSoFar = recordedHours - filledWorkdays * dailyTarget

  const shiftMonth = (delta: number) => {
    const next = new Date(year, month - 1 + delta, 1)
    setYear(next.getFullYear())
    setMonth(next.getMonth() + 1)
  }

  const onFillWeek = (group: WeekGroup) => {
    const dates = group.days.filter(d => d.isExpectedWorkday && !d.entry).map(d => d.date)

    if (dates.length === 0) {
      toast.info('Every working day in this week is already filled in.')
      return
    }

    fillWeek.mutate({ dates, ...defaultTimesFor(dailyTarget) })
  }

  if (isLoading) {
    return <div className="p-6 text-sm text-muted-foreground">Loading timesheet…</div>
  }

  if (isError) {
    return <div className="p-6 text-sm text-destructive">{getApiErrorMessage(error)}</div>
  }

  return (
    <div className="p-6 space-y-5 max-w-6xl mx-auto">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <Clock className="h-6 w-6" />
            My working time
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Fill a whole week in one go, then adjust the days that were different.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-lg border">
            <Button variant="ghost" size="sm" onClick={() => shiftMonth(-1)} aria-label="Previous month">
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="text-sm font-semibold min-w-[8.5rem] text-center">
              {MONTHS[month - 1]} {year}
            </span>
            <Button variant="ghost" size="sm" onClick={() => shiftMonth(1)} aria-label="Next month">
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>

          {isEditable && (
            <Button
              onClick={() => submit.mutate()}
              disabled={submit.isPending || recordedDays === 0}
              className="gap-2"
            >
              <Send className="h-4 w-4" />
              Submit month
            </Button>
          )}
        </div>
      </div>

      {/* Where the month stands, before any of the detail */}
      <div className="rounded-xl border grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 divide-y sm:divide-y-0 sm:divide-x">
        <Stat
          label="Recorded"
          value={formatHours(recordedHours)}
          unit="h"
          hint={`${recordedDays} of ${daysInMonth} days recorded`}
        />
        <Stat
          label="Monthly target"
          value={target?.targetHours == null ? '—' : trimHours(target.targetHours)}
          unit={target?.targetHours == null ? undefined : 'h'}
          hint={
            target === undefined ? '…'
              // Null target means no terms cover the month — unavailable, not a month of zero.
              : target.targetHours === null ? 'no employment terms cover this month'
                : `${target.workingDays} workdays × ${trimHours(dailyTarget)} h`
          }
          muted={target?.targetHours == null}
        />
        <Stat
          label="Balance"
          value={`${balanceSoFar >= 0 ? '+' : '−'}${formatHours(Math.abs(balanceSoFar))}`}
          unit="h"
          hint="vs. target so far"
          tone={balanceSoFar < 0 ? 'negative' : balanceSoFar > 0 ? 'positive' : undefined}
        />

        <div className="p-4 space-y-2">
          <div className="flex items-baseline justify-between gap-2">
            <span className={`text-xs px-2 py-0.5 rounded-md border font-medium ${STATUS_BADGE[status]}`}>
              {status}
            </span>
            <span className="text-xs text-muted-foreground tabular-nums">{progress}%</span>
          </div>

          <div className="h-1.5 rounded-full bg-muted overflow-hidden">
            <div
              className="h-full rounded-full bg-sky-500 transition-all"
              style={{ width: `${progress}%` }}
            />
          </div>

          <p className="text-xs text-muted-foreground">
            {workdaysLeft === 0
              ? 'Every working day is filled in.'
              : `${workdaysLeft} ${workdaysLeft === 1 ? 'workday' : 'workdays'} left to fill`}
            {data?.isSelfApproved && (
              <span className="inline-flex items-center gap-1 ml-2">
                <Lock className="h-3 w-3" /> self-approved
              </span>
            )}
            {data?.reopenedAt && (
              <span className="inline-flex items-center gap-1 ml-2">
                <RotateCcw className="h-3 w-3" /> reopened
              </span>
            )}
          </p>
        </div>
      </div>

      <BreachList breaches={data?.breaches ?? []} />

      {weeks.map(group => (
        <section key={group.week} className="rounded-xl border">
          <header className="flex items-center justify-between gap-3 flex-wrap px-4 py-3 border-b">
            <div className="flex items-baseline gap-2">
              <h2 className="text-sm font-semibold">Week {group.week}</h2>
              <span className="text-xs text-muted-foreground">{group.label}</span>
            </div>

            <div className="flex items-center gap-2">
              {isEditable && (
                <>
                  <Button
                    variant="outline" size="sm" className="gap-1.5"
                    disabled={fillWeek.isPending}
                    onClick={() => onFillWeek(group)}
                  >
                    <Plus className="h-3.5 w-3.5" />
                    Fill {patternLabel(pattern)}
                  </Button>
                  <Button
                    variant="ghost" size="sm"
                    disabled={group.days.every(d => !d.entry) || clearWeek.isPending}
                    onClick={() => setClearing(group)}
                  >
                    Clear
                  </Button>
                </>
              )}

              <span className="text-xs tabular-nums">
                <span className={
                  group.targetHours > 0 && group.recordedHours >= group.targetHours
                    ? 'text-emerald-400 font-medium'
                    : 'font-medium'
                }>
                  {formatHours(group.recordedHours)}
                </span>
                <span className="text-muted-foreground"> / {trimHours(group.targetHours)} h</span>
              </span>
            </div>
          </header>

          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[42rem]">
              <thead>
                <tr className="text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-4 py-2 font-medium w-40">Day</th>
                  <th className="px-2 py-2 font-medium">Start</th>
                  <th className="px-2 py-2 font-medium">End</th>
                  <th className="px-2 py-2 font-medium w-32">Break</th>
                  <th className="px-4 py-2 font-medium w-28 text-right">Hours</th>
                </tr>
              </thead>
              <tbody>
                {group.days.map(row => (
                  <DayEditor
                    // Remounts when the stored day changes, so a week fill is reflected in the
                    // inputs without syncing state in an effect.
                    key={`${row.date}:${row.entry?.startTime ?? ''}:${row.entry?.endTime ?? ''}:${row.entry?.breakMinutes ?? ''}`}
                    row={row}
                    editable={isEditable}
                    flagged={flagged.has(row.date)}
                    onSave={vars => saveDay.mutate(vars)}
                    onRemove={() => removeDay.mutate(row.date)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ))}

      {!isEditable && (
        <p className="text-sm text-muted-foreground">
          This month is {status.toLowerCase()} and can no longer be edited. Ask an approver to
          reopen it if something needs correcting.
        </p>
      )}

      <Dialog
        open={clearing !== null}
        onClose={() => setClearing(null)}
        title={clearing ? `Clear week ${clearing.week}?` : ''}
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            This deletes the recorded days for {clearing?.label}. Working time is a statutory
            record, so re-enter anything that was actually worked.
          </p>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setClearing(null)}>Cancel</Button>
            <Button
              disabled={clearWeek.isPending}
              onClick={() => clearWeek.mutate(
                clearing!.days.filter(d => d.entry).map(d => d.date))}
            >
              Clear {clearing?.days.filter(d => d.entry).length} days
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

/** "Mon–Fri" when the pattern is contiguous, otherwise the days listed. */
function patternLabel(pattern: Set<WeekDayName>) {
  const ordered = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
    .filter((d): d is WeekDayName => pattern.has(d as WeekDayName))

  if (ordered.length === 0) return 'week'

  const indexes = ordered.map(d => ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'].indexOf(d))
  const contiguous = indexes.every((v, i) => i === 0 || v === indexes[i - 1] + 1)

  if (!contiguous) return ordered.map(d => d.slice(0, 3)).join(' · ')
  if (ordered.length === 1) return ordered[0].slice(0, 3)

  return `${ordered[0].slice(0, 3)}–${ordered.at(-1)!.slice(0, 3)}`
}

function Stat({
  label, value, unit, hint, tone, muted,
}: {
  label: string
  value: string
  unit?: string
  hint: string
  tone?: 'positive' | 'negative'
  muted?: boolean
}) {
  const toneClass =
    tone === 'negative' ? 'text-amber-400'
      : tone === 'positive' ? 'text-emerald-400'
        : muted ? 'text-muted-foreground' : ''

  return (
    <div className="p-4">
      <p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className={`mt-1 text-2xl font-bold tabular-nums ${toneClass}`}>
        {value}
        {unit && <span className="text-sm font-medium text-muted-foreground ml-1">{unit}</span>}
      </p>
      <p className="text-xs text-muted-foreground mt-1">{hint}</p>
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

  const rowClass = flagged
    ? 'bg-amber-500/10 border-t'
    : row.nonWorkingName || row.isWeekend
      ? 'bg-muted/20 border-t'
      : 'border-t'

  return (
    <tr className={`group ${rowClass}`}>
      <td className="px-4 py-1.5 whitespace-nowrap">
        <span className="inline-flex items-center gap-2">
          <span
            className={`h-1.5 w-1.5 rounded-full ${
              row.entry ? 'bg-sky-400'
                : row.isExpectedWorkday ? 'bg-muted-foreground/40' : 'bg-muted-foreground/15'
            }`}
          />
          <span className={row.isExpectedWorkday ? 'text-muted-foreground' : 'text-muted-foreground/60'}>
            {row.weekday}
          </span>
          <span className={`font-medium tabular-nums ${row.isExpectedWorkday ? '' : 'text-muted-foreground'}`}>
            {String(row.dayOfMonth).padStart(2, '0')}
          </span>
        </span>

        {row.nonWorkingName && (
          <span className="ml-2 text-xs text-muted-foreground">{row.nonWorkingName}</span>
        )}

        {flagged && (
          <AlertTriangle
            className="h-3 w-3 inline-block ml-1.5 text-amber-500 align-baseline"
            aria-label="Working time issue on this day"
          />
        )}
      </td>

      <td className="px-2 py-1.5">
        <Input
          type="time" value={start} disabled={!editable}
          onChange={e => setStart(e.target.value)} onBlur={commit}
          className="h-8 tabular-nums"
        />
      </td>

      <td className="px-2 py-1.5">
        <Input
          type="time" value={end} disabled={!editable}
          onChange={e => setEnd(e.target.value)} onBlur={commit}
          className="h-8 tabular-nums"
        />
      </td>

      <td className="px-2 py-1.5">
        <div className="relative">
          <Input
            type="number" min={0} value={breakMinutes} disabled={!editable} placeholder="—"
            onChange={e => setBreakMinutes(e.target.value)} onBlur={commit}
            className="h-8 pr-10 tabular-nums"
          />
          <span className="absolute right-2.5 top-1/2 -translate-y-1/2 text-xs text-muted-foreground pointer-events-none">
            min
          </span>
        </div>
      </td>

      <td className="px-4 py-1.5 text-right tabular-nums whitespace-nowrap">
        {row.entry ? formatHours(row.entry.workedHours) : <span className="text-muted-foreground">—</span>}
        {row.entry && editable && (
          <Button
            variant="ghost" size="sm"
            className="ml-1 h-6 w-6 p-0 align-middle opacity-0 group-hover:opacity-100 focus:opacity-100"
            onClick={onRemove}
            aria-label={`Clear ${row.date}`}
          >
            <Trash2 className="h-3.5 w-3.5" />
          </Button>
        )}
      </td>
    </tr>
  )
}
