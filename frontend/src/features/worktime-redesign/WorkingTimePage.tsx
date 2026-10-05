import { useState } from 'react'
import { toast } from 'sonner'
import { ChevronLeft, ChevronRight, Clock, Plus } from 'lucide-react'
import './redesign.css'

/*
 * "My working time" — month editor from the design handoff (Working Time.dc.html).
 * Frontend only: entries live in component state and persist to localStorage per month;
 * the backend wiring (load/save/submit) comes later.
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
const storageKey = (key: string) => `wtr-draft-${key}`

function loadMonth(key: string): MonthEntries {
  try {
    const raw = localStorage.getItem(storageKey(key))
    return raw ? (JSON.parse(raw) as MonthEntries) : {}
  } catch {
    return {}
  }
}

function persistMonth(key: string, entries: MonthEntries) {
  try {
    localStorage.setItem(storageKey(key), JSON.stringify(entries))
  } catch {
    /* storage unavailable — draft stays in memory */
  }
}

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

export function WorkingTimePage() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth())
  const [byMonth, setByMonth] = useState<Record<string, MonthEntries>>(() => {
    const key = monthKey(now.getFullYear(), now.getMonth())
    return { [key]: loadMonth(key) }
  })

  const key = monthKey(year, month)
  const entries = byMonth[key] ?? {}

  function updateEntries(mutate: (prev: MonthEntries) => MonthEntries) {
    setByMonth(prev => {
      const next = mutate(prev[key] ?? {})
      persistMonth(key, next)
      return { ...prev, [key]: next }
    })
  }

  function setField(day: number, field: keyof DayEntry, value: string) {
    updateEntries(prev => {
      const existing = prev[day] ?? { start: '', end: '', brk: '' }
      return { ...prev, [day]: { ...existing, [field]: value } }
    })
  }

  function navigate(delta: number) {
    const d = new Date(year, month + delta, 1)
    const nextKey = monthKey(d.getFullYear(), d.getMonth())
    setYear(d.getFullYear())
    setMonth(d.getMonth())
    setByMonth(prev => (prev[nextKey] ? prev : { ...prev, [nextKey]: loadMonth(nextKey) }))
  }

  const all = daysOf(year, month)

  // Group into weeks (Monday starts a new group).
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

  return (
    <div className="wtr min-h-full px-8 pt-7 pb-16">
      <div className="max-w-[1180px] mx-auto flex flex-col gap-[22px]">
        {/* Header */}
        <div className="flex items-start justify-between gap-6 flex-wrap">
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-[11px]">
              <Clock className="h-[22px] w-[22px] text-[#5EC8F2]" strokeWidth={1.8} />
              <h1 className="m-0 text-[25px] font-semibold tracking-[-0.4px]">My working time</h1>
            </div>
            <p className="m-0 text-sm text-[#8C9AB4] max-w-[52ch]">
              Fill a whole week in one go, then adjust the days that were different.
            </p>
          </div>
          <div className="flex items-center gap-2.5">
            <div className="flex items-center gap-0.5 bg-[#0E1524] border border-[#1C2740] rounded-[10px] p-1">
              <button
                onClick={() => navigate(-1)}
                className="w-[30px] h-[30px] grid place-items-center bg-transparent border-0 rounded-[7px] text-[#8C9AB4] cursor-pointer hover:bg-[#18223A] hover:text-[#E6ECF6]"
              >
                <ChevronLeft className="h-[15px] w-[15px]" strokeWidth={2.2} />
              </button>
              <div className="min-w-[138px] text-center text-sm font-medium">
                {MONTH_NAMES[month]} {year}
              </div>
              <button
                onClick={() => navigate(1)}
                className="w-[30px] h-[30px] grid place-items-center bg-transparent border-0 rounded-[7px] text-[#8C9AB4] cursor-pointer hover:bg-[#18223A] hover:text-[#E6ECF6]"
              >
                <ChevronRight className="h-[15px] w-[15px]" strokeWidth={2.2} />
              </button>
            </div>
            <button
              onClick={() => toast.info('Submit is not wired up yet — the backend endpoint follows.')}
              className="wtr-primary h-10 px-[18px] text-sm"
            >
              Submit month
            </button>
          </div>
        </div>

        {/* Stat strip */}
        <div className="grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-px bg-[#162035] border border-[#162035] rounded-[14px] overflow-hidden">
          <StatCell label="Recorded" sub={`${recorded} of ${all.length} days recorded`}>
            <span className="wtr-mono text-2xl font-medium">
              {fmt(totalH)}
              <span className="text-sm text-[#7B89A3] ml-[3px]">h</span>
            </span>
          </StatCell>
          <StatCell label="Monthly target" sub={`${workdays} workdays × ${TARGET_PER_DAY} h`}>
            <span className="wtr-mono text-2xl font-medium text-[#C3CDDF]">
              {targetH.toFixed(0)}
              <span className="text-sm text-[#7B89A3] ml-[3px]">h</span>
            </span>
          </StatCell>
          <StatCell label="Balance" sub="vs. target so far">
            <span
              className="wtr-mono text-2xl font-medium"
              style={{
                color: Math.abs(balance) < 0.01 ? '#C3CDDF' : balance > 0 ? '#63D9A4' : '#F2A25E',
              }}
            >
              {balance >= 0 ? '+' : '−'}
              {fmt(Math.abs(balance))}
              <span className="text-sm text-[#7B89A3] ml-[3px]">h</span>
            </span>
          </StatCell>
          <div className="bg-[#0C1322] px-[18px] py-4 flex flex-col gap-2.5 justify-center">
            <div className="flex items-baseline justify-between gap-2">
              <span className="text-[11px] font-semibold tracking-[1px] uppercase text-[#7B89A3]">Draft</span>
              <span className="wtr-mono text-[13px] text-[#C3CDDF]">{pct}%</span>
            </div>
            <div className="h-1.5 rounded-full bg-[#18223A] overflow-hidden">
              <div
                className="h-full rounded-full"
                style={{ width: `${pct}%`, background: 'linear-gradient(90deg,#3C9BD6,#5EC8F2)' }}
              />
            </div>
            <div className="text-[12.5px] text-[#7B89A3]">{workdays - recordedWorkdays} workdays left to fill</div>
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
            onSet={setField}
            onFill={() =>
              updateEntries(prev => {
                const next = { ...prev }
                g.filter(d => !d.weekend).forEach(d => {
                  next[d.day] = { start: '08:00', end: '16:30', brk: '30' }
                })
                return next
              })
            }
            onClear={() =>
              updateEntries(prev => {
                const next = { ...prev }
                g.forEach(d => delete next[d.day])
                return next
              })
            }
          />
        ))}

        {/* Legend */}
        <div className="flex items-center gap-[18px] text-[12.5px] text-[#8391AB] pt-0.5 flex-wrap">
          <span className="flex items-center gap-[7px]">
            <span className="w-[5px] h-[5px] rounded-full bg-[#5EC8F2]" />
            Recorded
          </span>
          <span className="flex items-center gap-[7px]">
            <span className="w-[5px] h-[5px] rounded-full bg-[#2E3A54]" />
            Not yet filled
          </span>
          <span className="flex items-center gap-[7px]">
            <span className="w-2.5 h-2.5 rounded-[3px] bg-[#080D17] border border-[#18223A]" />
            Weekend
          </span>
          <span className="ml-auto">Times are stored in your local timezone · draft saves automatically</span>
        </div>
      </div>
    </div>
  )
}

function StatCell({ label, sub, children }: { label: string; sub: string; children: React.ReactNode }) {
  return (
    <div className="bg-[#0C1322] px-[18px] py-4 flex flex-col gap-[7px]">
      <div className="text-[11px] font-semibold tracking-[1px] uppercase text-[#7B89A3]">{label}</div>
      {children}
      <div className="text-[12.5px] text-[#7B89A3]">{sub}</div>
    </div>
  )
}

const GRID = 'grid grid-cols-[132px_1fr_1fr_118px_96px] gap-3'

function WeekCard({
  days,
  year,
  month,
  entries,
  onSet,
  onFill,
  onClear,
}: {
  days: DayInfo[]
  year: number
  month: number
  entries: MonthEntries
  onSet: (day: number, field: keyof DayEntry, value: string) => void
  onFill: () => void
  onClear: () => void
}) {
  const total = days.reduce((a, d) => a + (hoursFor(entries[d.day]) ?? 0), 0)
  const weekTarget = days.filter(d => !d.weekend).length * TARGET_PER_DAY
  const weekNo = isoWeekNumber(new Date(year, month, days[0].day))
  const monthShort = MONTH_NAMES[month].slice(0, 3)

  return (
    <div className="border border-[#162035] rounded-[14px] bg-[#0A101C] overflow-hidden">
      {/* Week header */}
      <div className="flex items-center justify-between gap-4 px-[18px] py-[13px] bg-[#0E1524] border-b border-[#162035] flex-wrap">
        <div className="flex items-center gap-3">
          <span className="text-sm font-semibold">Week {weekNo}</span>
          <span className="text-[12.5px] text-[#7B89A3]">
            {monthShort} {days[0].day} – {days[days.length - 1].day}
          </span>
        </div>
        <div className="flex items-center gap-2.5">
          <button onClick={onFill} className="wtr-btn h-8 px-[13px] flex items-center gap-[7px] text-[12.5px] font-medium">
            <Plus className="h-[13px] w-[13px]" strokeWidth={2.2} />
            Fill Mon–Fri
          </button>
          <button onClick={onClear} className="wtr-ghost h-8 px-[11px] text-[12.5px]">
            Clear
          </button>
          <div className="flex items-baseline gap-1.5 pl-1.5 border-l border-[#1C2740]">
            <span
              className="wtr-mono text-[15px] font-medium"
              style={{ color: total === 0 ? '#6B7893' : total >= weekTarget ? '#63D9A4' : '#E6ECF6' }}
            >
              {fmt(total)}
            </span>
            <span className="text-xs text-[#7B89A3]">/ {weekTarget} h</span>
          </div>
        </div>
      </div>

      {/* Column headers */}
      <div className={`${GRID} px-[18px] py-[9px] border-b border-[#121B2D]`}>
        {['Day', 'Start', 'End', 'Break', 'Hours'].map((h, i) => (
          <div
            key={h}
            className={`text-[11px] font-semibold tracking-[0.9px] uppercase text-[#8391AB] ${i === 4 ? 'text-right' : ''}`}
          >
            {h}
          </div>
        ))}
      </div>

      {/* Day rows */}
      {days.map(d => {
        const e = entries[d.day]
        const h = hoursFor(e)
        const filled = h != null
        const inputStyle = {
          background: d.weekend ? '#0B1120' : '#0E1727',
          borderColor: filled ? '#28405C' : '#18223A',
        }
        return (
          <div
            key={d.day}
            className={`${GRID} items-center px-[18px] py-[7px] border-b border-[#0F1727] wtr-row-hover`}
            style={{ background: d.weekend ? '#080D17' : 'transparent' }}
          >
            <div className="flex items-center gap-[9px]">
              <span
                className="w-[5px] h-[5px] rounded-full flex-none"
                style={{ background: filled ? '#5EC8F2' : d.weekend ? '#1B2439' : '#2E3A54' }}
              />
              <span className="text-[13px] text-[#8C9AB4] w-[22px]">{d.dow}</span>
              <span className="wtr-mono text-sm font-medium" style={{ color: d.weekend ? '#7B89A3' : '#E6ECF6' }}>
                {String(d.day).padStart(2, '0')}
              </span>
            </div>
            <input
              type="time"
              value={e?.start ?? ''}
              onChange={ev => onSet(d.day, 'start', ev.target.value)}
              className="wtr-input wtr-mono h-9 w-full px-2.5 text-sm"
              style={inputStyle}
            />
            <input
              type="time"
              value={e?.end ?? ''}
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
                onChange={ev => onSet(d.day, 'brk', ev.target.value)}
                className="wtr-input wtr-mono h-9 w-full pl-2.5 pr-[34px] text-sm"
                style={inputStyle}
              />
              <span className="absolute right-2.5 top-2.5 text-xs text-[#8C9AB4] pointer-events-none">min</span>
            </div>
            <div
              className="wtr-mono text-right text-sm font-medium"
              style={{ color: filled ? '#E6ECF6' : '#6B7893' }}
            >
              {fmt(h)}
            </div>
          </div>
        )
      })}
    </div>
  )
}
