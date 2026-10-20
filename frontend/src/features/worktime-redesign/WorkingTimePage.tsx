import { useMemo, useRef, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ChevronLeft, ChevronRight, Clock, Plus } from 'lucide-react'
import { deleteWorkDay, getMyTimesheet, submitTimesheet, upsertWorkDay } from '@/api/timesheets'
import { getApiErrorMessage } from '@/lib/api-errors'
import './redesign.css'

/*
 * "My working time" — month editor from the design handoff, wired to the timesheets API.
 * Edits save automatically per day (debounced); Submit month hands the sheet to approval.
 */

interface DayEntry {
  start: string
  end: string
  brk: string
}

type MonthEntries = Record<number, DayEntry>

const DOW = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So']
const TARGET_PER_DAY = 8

const monthKey = (year: number, month: number) => `${year}-${String(month + 1).padStart(2, '0')}`

function hoursFor(e: DayEntry | undefined): number | null {
  if (!e || !e.start || !e.end) return null
  const [sh, sm] = e.start.split(':').map(Number)
  const [eh, em] = e.end.split(':').map(Number)
  const mins = eh * 60 + em - (sh * 60 + sm) - (Number(e.brk) || 0)
  if (!isFinite(mins) || mins <= 0) return null
  return mins / 60
}

const fmt = (h: number | null) => (h == null ? '—' : (Math.round(h * 100) / 100).toFixed(2))

interface DayInfo {
  day: number
  dow: string
  weekend: boolean
}

function daysOf(year: number, month: number): DayInfo[] {
  const count = new Date(year, month + 1, 0).getDate()
  const out: DayInfo[] = []
  for (let d = 1; d <= count; d++) {
    const idx = (new Date(year, month, d).getDay() + 6) % 7 // 0 = Monday
    out.push({ day: d, dow: DOW[idx], weekend: idx > 4 })
  }
  return out
}

function isoWeekNumber(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  const dayNum = d.getUTCDay() || 7
  d.setUTCDate(d.getUTCDate() + 4 - dayNum)
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1))
  return Math.ceil(((d.getTime() - yearStart.getTime()) / 86_400_000 + 1) / 7)
}

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

const STATUS_COLOR: Record<string, [string, string]> = {
  Draft: ['var(--wtr-btn-text)', 'var(--wtr-muted)'],
  Submitted: ['var(--wtr-tint-cyan-fg)', 'var(--wtr-tint-cyan-bg)'],
  Approved: ['var(--wtr-tint-green-fg)', 'var(--wtr-tint-green-bg)'],
  Locked: ['var(--wtr-tint-orange-fg)', 'var(--wtr-tint-orange-bg)'],
}

export function WorkingTimePage() {
  const now = new Date()
  const queryClient = useQueryClient()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth()) // 0-based
  // User edits overlaid on the server copy; null means "cleared".
  const [localByKey, setLocalByKey] = useState<Record<string, Record<number, DayEntry | null>>>({})
  const saveTimers = useRef<Record<string, ReturnType<typeof setTimeout>>>({})

  const key = monthKey(year, month)

  const { data: sheet } = useQuery({
    queryKey: ['wtr-timesheet', year, month + 1],
    queryFn: () => getMyTimesheet(year, month + 1),
  })

  const serverEntries = useMemo(() => {
    const entries: MonthEntries = {}
    for (const d of sheet?.days ?? []) {
      const day = Number(d.date.slice(8, 10))
      entries[day] = {
        start: d.startTime.slice(0, 5),
        end: d.endTime.slice(0, 5),
        brk: String(d.breakMinutes || ''),
      }
    }
    return entries
  }, [sheet])

  const entries: MonthEntries = useMemo(() => {
    const merged: MonthEntries = { ...serverEntries }
    for (const [day, entry] of Object.entries(localByKey[key] ?? {})) {
      if (entry === null) delete merged[Number(day)]
      else merged[Number(day)] = entry
    }
    return merged
  }, [serverEntries, localByKey, key])

  const editable = !sheet || sheet.status === 'Draft'

  const onError = (err: unknown) => {
    toast.error(getApiErrorMessage(err))
    queryClient.invalidateQueries({ queryKey: ['wtr-timesheet', year, month + 1] })
  }

  const dateOf = (day: number) => `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`

  function persistDay(y: number, m: number, day: number, entry: DayEntry | null, hadServerEntry: boolean) {
    if (entry && entry.start && entry.end && hoursFor(entry) != null) {
      upsertWorkDay(y, m + 1, dateOf(day), {
        startTime: entry.start,
        endTime: entry.end,
        breakMinutes: Number(entry.brk) || 0,
      }).catch(onError)
    } else if ((entry === null || !entry.start || !entry.end) && hadServerEntry) {
      deleteWorkDay(y, m + 1, dateOf(day)).catch(onError)
    }
  }

  function setField(day: number, field: keyof DayEntry, value: string) {
    if (!editable) return
    const current = entries[day] ?? { start: '', end: '', brk: '' }
    const next = { ...current, [field]: value }
    setLocalByKey(prev => ({ ...prev, [key]: { ...(prev[key] ?? {}), [day]: next } }))

    const timerKey = `${key}-${day}`
    clearTimeout(saveTimers.current[timerKey])
    saveTimers.current[timerKey] = setTimeout(
      () => persistDay(year, month, day, next, serverEntries[day] !== undefined),
      700,
    )
  }

  function fillWeek(group: DayInfo[]) {
    if (!editable) return
    const filled: DayEntry = { start: '08:00', end: '16:30', brk: '30' }
    const weekdays = group.filter(d => !d.weekend)
    setLocalByKey(prev => {
      const monthLocal = { ...(prev[key] ?? {}) }
      weekdays.forEach(d => (monthLocal[d.day] = filled))
      return { ...prev, [key]: monthLocal }
    })
    // Sequential: each upsert loads the same monthly timesheet aggregate, so parallel
    // requests would race over its creation and drop days.
    ;(async () => {
      for (const d of weekdays)
        await upsertWorkDay(year, month + 1, dateOf(d.day), { startTime: '08:00', endTime: '16:30', breakMinutes: 30 })
    })()
      .then(() => queryClient.invalidateQueries({ queryKey: ['wtr-timesheet', year, month + 1] }))
      .catch(onError)
  }

  function clearWeek(group: DayInfo[]) {
    if (!editable) return
    setLocalByKey(prev => {
      const monthLocal = { ...(prev[key] ?? {}) }
      group.forEach(d => (monthLocal[d.day] = null))
      return { ...prev, [key]: monthLocal }
    })
    ;(async () => {
      for (const d of group.filter(x => serverEntries[x.day] !== undefined))
        await deleteWorkDay(year, month + 1, dateOf(d.day))
    })()
      .then(() => queryClient.invalidateQueries({ queryKey: ['wtr-timesheet', year, month + 1] }))
      .catch(onError)
  }

  function submit() {
    submitTimesheet(year, month + 1)
      .then(() => {
        toast.success('Month submitted for approval.')
        queryClient.invalidateQueries({ queryKey: ['wtr-timesheet', year, month + 1] })
      })
      .catch(err => toast.error(getApiErrorMessage(err)))
  }

  function navigate(delta: number) {
    const d = new Date(year, month + delta, 1)
    setYear(d.getFullYear())
    setMonth(d.getMonth())
  }

  const all = daysOf(year, month)
  const groups: DayInfo[][] = []
  all.forEach(d => {
    if (!groups.length || d.dow === 'Mo') groups.push([])
    groups[groups.length - 1].push(d)
  })

  const totalH = all.reduce((a, d) => a + (hoursFor(entries[d.day]) ?? 0), 0)
  const recorded = all.filter(d => hoursFor(entries[d.day]) != null).length
  const workdays = all.filter(d => !d.weekend).length
  const targetH = workdays * TARGET_PER_DAY
  const recordedWorkdays = all.filter(d => !d.weekend && hoursFor(entries[d.day]) != null).length
  const balance = totalH - recordedWorkdays * TARGET_PER_DAY
  const pct = workdays === 0 ? 0 : Math.min(100, Math.round((recordedWorkdays / workdays) * 100))
  const status = sheet?.status ?? 'Draft'
  const [statusColor, statusBg] = STATUS_COLOR[status] ?? STATUS_COLOR.Draft

  return (
    <div className="wtr min-h-full px-8 pt-7 pb-16">
      <div className="max-w-[1400px] mx-auto flex flex-col gap-[22px]">
        {/* Header */}
        <div className="flex items-start justify-between gap-6 flex-wrap">
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-[11px]">
              <Clock className="h-[22px] w-[22px] text-[color:var(--wtr-accent)]" strokeWidth={1.8} />
              <h1 className="m-0 text-[25px] font-semibold tracking-[-0.4px]">My working time</h1>
              <span
                className="text-[11px] font-semibold tracking-[0.4px] px-2 py-0.5 rounded-full whitespace-nowrap"
                style={{ color: statusColor, background: statusBg }}
              >
                {status}
              </span>
            </div>
            <p className="m-0 text-sm text-[color:var(--wtr-text-sec)] max-w-[52ch]">
              Fill a whole week in one go, then adjust the days that were different.
            </p>
          </div>
          <div className="flex items-center gap-2.5">
            <div className="flex items-center gap-0.5 bg-[color:var(--wtr-head)] border border-[color:var(--wtr-border2)] rounded-[10px] p-1">
              <button
                onClick={() => navigate(-1)}
                className="w-[30px] h-[30px] grid place-items-center bg-transparent border-0 rounded-[7px] text-[color:var(--wtr-text-sec)] cursor-pointer hover:bg-[color:var(--wtr-muted)] hover:text-[color:var(--wtr-text)]"
              >
                <ChevronLeft className="h-[15px] w-[15px]" strokeWidth={2.2} />
              </button>
              <div className="min-w-[138px] text-center text-sm font-medium">
                {MONTH_NAMES[month]} {year}
              </div>
              <button
                onClick={() => navigate(1)}
                className="w-[30px] h-[30px] grid place-items-center bg-transparent border-0 rounded-[7px] text-[color:var(--wtr-text-sec)] cursor-pointer hover:bg-[color:var(--wtr-muted)] hover:text-[color:var(--wtr-text)]"
              >
                <ChevronRight className="h-[15px] w-[15px]" strokeWidth={2.2} />
              </button>
            </div>
            <button
              onClick={submit}
              disabled={!editable || recorded === 0}
              className="wtr-primary h-10 px-[18px] text-sm disabled:opacity-40 disabled:cursor-not-allowed"
            >
              Submit month
            </button>
          </div>
        </div>

        {/* Stat strip */}
        <div className="grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-px bg-[color:var(--wtr-border)] border border-[color:var(--wtr-border)] rounded-[14px] overflow-hidden">
          <StatCell label="Recorded" sub={`${recorded} of ${all.length} days recorded`}>
            <span className="wtr-mono text-2xl font-medium">
              {fmt(totalH)}
              <span className="text-sm text-[color:var(--wtr-dim2)] ml-[3px]">h</span>
            </span>
          </StatCell>
          <StatCell label="Monthly target" sub={`${workdays} workdays × ${TARGET_PER_DAY} h`}>
            <span className="wtr-mono text-2xl font-medium text-[color:var(--wtr-text2)]">
              {targetH.toFixed(0)}
              <span className="text-sm text-[color:var(--wtr-dim2)] ml-[3px]">h</span>
            </span>
          </StatCell>
          <StatCell label="Balance" sub="vs. target so far">
            <span
              className="wtr-mono text-2xl font-medium"
              style={{
                color: Math.abs(balance) < 0.01 ? 'var(--wtr-text2)' : balance > 0 ? 'var(--wtr-tint-green-fg)' : 'var(--wtr-tint-orange-fg)',
              }}
            >
              {balance >= 0 ? '+' : '−'}
              {fmt(Math.abs(balance))}
              <span className="text-sm text-[color:var(--wtr-dim2)] ml-[3px]">h</span>
            </span>
          </StatCell>
          <div className="bg-[color:var(--wtr-panel)] px-[18px] py-4 flex flex-col gap-2.5 justify-center">
            <div className="flex items-baseline justify-between gap-2">
              <span className="text-[11px] font-semibold tracking-[1px] uppercase text-[color:var(--wtr-dim2)]">{status}</span>
              <span className="wtr-mono text-[13px] text-[color:var(--wtr-text2)]">{pct}%</span>
            </div>
            <div className="h-1.5 rounded-full bg-[color:var(--wtr-muted)] overflow-hidden">
              <div
                className="h-full rounded-full"
                style={{ width: `${pct}%`, background: 'linear-gradient(90deg,#3C9BD6,var(--wtr-accent))' }}
              />
            </div>
            <div className="text-[12.5px] text-[color:var(--wtr-dim2)]">{workdays - recordedWorkdays} workdays left to fill</div>
          </div>
        </div>

        {/* Week cards */}
        {groups.map(g => (
          <WeekCard
            key={g[0].day}
            days={g}
            year={year}
            month={month}
            entries={entries}
            editable={editable}
            onSet={setField}
            onFill={() => fillWeek(g)}
            onClear={() => clearWeek(g)}
          />
        ))}

        {/* Legend */}
        <div className="flex items-center gap-[18px] text-[12.5px] text-[color:var(--wtr-label)] pt-0.5 flex-wrap">
          <span className="flex items-center gap-[7px]">
            <span className="w-[5px] h-[5px] rounded-full bg-[color:var(--wtr-accent)]" />
            Recorded
          </span>
          <span className="flex items-center gap-[7px]">
            <span className="w-[5px] h-[5px] rounded-full bg-[color:var(--wtr-dot-empty)]" />
            Not yet filled
          </span>
          <span className="flex items-center gap-[7px]">
            <span className="w-2.5 h-2.5 rounded-[3px] bg-[color:var(--wtr-weekend)] border border-[color:var(--wtr-muted)]" />
            Weekend
          </span>
          <span className="ml-auto">
            {editable
              ? 'Times are stored in your local timezone · changes save automatically'
              : 'This month is submitted — reopen it from Approvals to edit.'}
          </span>
        </div>
      </div>
    </div>
  )
}

function StatCell({ label, sub, children }: { label: string; sub: string; children: React.ReactNode }) {
  return (
    <div className="bg-[color:var(--wtr-panel)] px-[18px] py-4 flex flex-col gap-[7px]">
      <div className="text-[11px] font-semibold tracking-[1px] uppercase text-[color:var(--wtr-dim2)]">{label}</div>
      {children}
      <div className="text-[12.5px] text-[color:var(--wtr-dim2)]">{sub}</div>
    </div>
  )
}

const GRID = 'grid grid-cols-[132px_1fr_1fr_118px_96px] gap-3'

function WeekCard({
  days,
  year,
  month,
  entries,
  editable,
  onSet,
  onFill,
  onClear,
}: {
  days: DayInfo[]
  year: number
  month: number
  entries: MonthEntries
  editable: boolean
  onSet: (day: number, field: keyof DayEntry, value: string) => void
  onFill: () => void
  onClear: () => void
}) {
  const total = days.reduce((a, d) => a + (hoursFor(entries[d.day]) ?? 0), 0)
  const weekTarget = days.filter(d => !d.weekend).length * TARGET_PER_DAY
  const weekNo = isoWeekNumber(new Date(year, month, days[0].day))
  const monthShort = MONTH_NAMES[month].slice(0, 3)

  return (
    <div className="border border-[color:var(--wtr-border)] rounded-[14px] bg-[color:var(--wtr-card)] overflow-hidden">
      <div className="flex items-center justify-between gap-4 px-[18px] py-[13px] bg-[color:var(--wtr-head)] border-b border-[color:var(--wtr-border)] flex-wrap">
        <div className="flex items-center gap-3">
          <span className="text-sm font-semibold">Week {weekNo}</span>
          <span className="text-[12.5px] text-[color:var(--wtr-dim2)]">
            {monthShort} {days[0].day} – {days[days.length - 1].day}
          </span>
        </div>
        <div className="flex items-center gap-2.5">
          <button
            onClick={onFill}
            disabled={!editable}
            className="wtr-btn h-8 px-[13px] flex items-center gap-[7px] text-[12.5px] font-medium disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <Plus className="h-[13px] w-[13px]" strokeWidth={2.2} />
            Fill Mon–Fri
          </button>
          <button onClick={onClear} disabled={!editable} className="wtr-ghost h-8 px-[11px] text-[12.5px] disabled:opacity-40 disabled:cursor-not-allowed">
            Clear
          </button>
          <div className="flex items-baseline gap-1.5 pl-1.5 border-l border-[color:var(--wtr-border2)]">
            <span
              className="wtr-mono text-[15px] font-medium"
              style={{ color: total === 0 ? 'var(--wtr-dim)' : total >= weekTarget ? 'var(--wtr-tint-green-fg)' : 'var(--wtr-text)' }}
            >
              {fmt(total)}
            </span>
            <span className="text-xs text-[color:var(--wtr-dim2)]">/ {weekTarget} h</span>
          </div>
        </div>
      </div>

      <div className={`${GRID} px-[18px] py-[9px] border-b border-[color:var(--wtr-row-border)]`}>
        {['Day', 'Start', 'End', 'Break', 'Hours'].map((h, i) => (
          <div
            key={h}
            className={`text-[11px] font-semibold tracking-[0.9px] uppercase text-[color:var(--wtr-label)] ${i === 4 ? 'text-right' : ''}`}
          >
            {h}
          </div>
        ))}
      </div>

      {days.map(d => {
        const e = entries[d.day]
        const h = hoursFor(e)
        const filled = h != null
        const inputStyle = {
          background: d.weekend ? 'var(--wtr-input-weekend)' : 'var(--wtr-input)',
          borderColor: filled ? 'var(--wtr-border-active)' : 'var(--wtr-muted)',
          opacity: editable ? 1 : 0.6,
        }
        return (
          <div
            key={d.day}
            className={`${GRID} items-center px-[18px] py-[7px] border-b border-[color:var(--wtr-row-border)] wtr-row-hover`}
            style={{ background: d.weekend ? 'var(--wtr-weekend)' : 'transparent' }}
          >
            <div className="flex items-center gap-[9px]">
              <span
                className="w-[5px] h-[5px] rounded-full flex-none"
                style={{ background: filled ? 'var(--wtr-accent)' : d.weekend ? 'var(--wtr-dot-weekend)' : 'var(--wtr-dot-empty)' }}
              />
              <span className="text-[13px] text-[color:var(--wtr-text-sec)] w-[22px]">{d.dow}</span>
              <span className="wtr-mono text-sm font-medium" style={{ color: d.weekend ? 'var(--wtr-dim2)' : 'var(--wtr-text)' }}>
                {String(d.day).padStart(2, '0')}
              </span>
            </div>
            <input
              type="time"
              value={e?.start ?? ''}
              disabled={!editable}
              onChange={ev => onSet(d.day, 'start', ev.target.value)}
              className="wtr-input wtr-mono h-9 w-full px-2.5 text-sm"
              style={inputStyle}
            />
            <input
              type="time"
              value={e?.end ?? ''}
              disabled={!editable}
              onChange={ev => onSet(d.day, 'end', ev.target.value)}
              className="wtr-input wtr-mono h-9 w-full px-2.5 text-sm"
              style={inputStyle}
            />
            <div className="relative">
              <input
                type="number"
                min={0}
                step={5}
                placeholder="—"
                value={e?.brk ?? ''}
                disabled={!editable}
                onChange={ev => onSet(d.day, 'brk', ev.target.value)}
                className="wtr-input wtr-mono h-9 w-full pl-2.5 pr-[34px] text-sm"
                style={inputStyle}
              />
              <span className="absolute right-2.5 top-2.5 text-xs text-[color:var(--wtr-text-sec)] pointer-events-none">min</span>
            </div>
            <div
              className="wtr-mono text-right text-sm font-medium"
              style={{ color: filled ? 'var(--wtr-text)' : 'var(--wtr-dim)' }}
            >
              {fmt(h)}
            </div>
          </div>
        )
      })}
    </div>
  )
}
