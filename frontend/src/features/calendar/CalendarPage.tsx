import { useState, useMemo, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { ChevronLeft, ChevronRight, Plus, Calendar, LayoutGrid, Bell, ChevronDown, ChevronUp } from 'lucide-react'
import { getTermins, getPendingInvitations, type TerminSummary } from '@/api/calendar'
import { getNonWorkingDays, type NonWorkingDay } from '@/api/worktime-calendar'
import { getAbsences, ABSENCE_TYPE_LABELS, type Absence } from '@/api/absences'
import { Button } from '@/components/ui/button'
import { CreateTerminModal } from './components/CreateTerminModal'
import { TerminDetailPanel } from './components/TerminDetailPanel'

// ─── work time markers ────────────────────────────────────────────────────────

export interface DayMarker {
  label: string
  tone: 'holiday' | 'closure' | 'absence'
}

const MARKER_TONE: Record<DayMarker['tone'], string> = {
  holiday: 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/20',
  closure: 'bg-amber-500/15 text-amber-400 border border-amber-500/20',
  absence: 'bg-violet-500/15 text-violet-400 border border-violet-500/20',
}

/** Local calendar date as yyyy-MM-dd, avoiding the UTC shift toISOString would introduce. */
function toDateKey(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

/**
 * Read-only markers keyed by date: company non-working days, plus the viewer's own approved
 * absences. Absences of other employees are deliberately not shown — they are private.
 */
function buildDayMarkers(
  nonWorkingDays: NonWorkingDay[],
  absences: Absence[],
): Map<string, DayMarker[]> {
  const map = new Map<string, DayMarker[]>()

  const push = (key: string, marker: DayMarker) => {
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(marker)
  }

  for (const day of nonWorkingDays) {
    push(day.date, {
      label: day.name,
      tone: day.kind === 'PublicHoliday' ? 'holiday' : 'closure',
    })
  }

  for (const absence of absences) {
    const end = new Date(absence.endDate)
    for (let d = new Date(absence.startDate); d <= end; d.setDate(d.getDate() + 1)) {
      push(toDateKey(d), { label: ABSENCE_TYPE_LABELS[absence.type], tone: 'absence' })
    }
  }

  return map
}

function DayMarkers({ markers }: { markers: DayMarker[] }) {
  if (markers.length === 0) return null

  return (
    <div className="space-y-0.5 mb-1">
      {markers.map((marker, i) => (
        <div
          key={i}
          title={marker.label}
          className={`truncate text-[10px] px-1.5 py-0.5 rounded font-medium ${MARKER_TONE[marker.tone]}`}
        >
          {marker.label}
        </div>
      ))}
    </div>
  )
}

// ─── color helpers ────────────────────────────────────────────────────────────

const STATUS_COLORS: Record<string, string> = {
  Scheduled: 'bg-blue-500',
  Cancelled: 'bg-muted text-muted-foreground line-through',
}

function pillColor(t: TerminSummary) {
  if (t.status === 'Cancelled') return 'bg-muted/60 text-muted-foreground'
  if (t.myRole === 'Organizer') return 'bg-blue-500 text-white'
  const s = t.myInvitationStatus
  if (s === 'Accepted') return 'bg-green-500 text-white'
  if (s === 'Declined') return 'bg-red-400 text-white'
  if (s === 'RescheduleProposed') return 'bg-amber-400 text-white'
  return 'bg-violet-500 text-white' // Pending invite
}

// ─── Month view ───────────────────────────────────────────────────────────────

/** Month is zero-based here, matching the Date conventions used throughout this file. */
function dateKey(year: number, month: number, day: number) {
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

interface MonthViewProps {
  year: number
  month: number
  termins: TerminSummary[]
  /** Read-only work time markers by date — holidays, closures, and the viewer's own absences. */
  dayMarkers: Map<string, DayMarker[]>
  selectedId: string | null
  onSelect: (id: string) => void
  onCreate: (date: Date) => void
}

function MonthView({
  year, month, termins, dayMarkers, selectedId, onSelect, onCreate,
}: MonthViewProps) {
  const firstDay = new Date(year, month, 1)
  const startOffset = firstDay.getDay() // 0 = Sunday
  const daysInMonth = new Date(year, month + 1, 0).getDate()
  const today = new Date()

  const cells: (number | null)[] = [
    ...Array(startOffset).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ]
  while (cells.length % 7 !== 0) cells.push(null)

  const byDay = useMemo(() => {
    const map = new Map<string, TerminSummary[]>()
    for (const t of termins) {
      const d = new Date(t.start)
      if (d.getFullYear() === year && d.getMonth() === month) {
        const key = `${d.getDate()}`
        if (!map.has(key)) map.set(key, [])
        map.get(key)!.push(t)
      }
    }
    return map
  }, [termins, year, month])

  const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

  return (
    <div className="flex-1 overflow-auto">
      {/* Day headers */}
      <div className="grid grid-cols-7 border-b">
        {DAYS.map(d => (
          <div key={d} className="py-2 text-center text-xs font-medium text-muted-foreground">{d}</div>
        ))}
      </div>

      {/* Grid */}
      <div className="grid grid-cols-7 flex-1">
        {cells.map((day, i) => {
          const isToday = day !== null && today.getDate() === day && today.getMonth() === month && today.getFullYear() === year
          const dayTermins = day !== null ? (byDay.get(String(day)) ?? []) : []
          return (
            <div
              key={i}
              className={`min-h-[100px] border-r border-b p-1.5 ${day ? 'cursor-pointer hover:bg-muted/30' : 'bg-muted/10'}`}
              onDoubleClick={() => day && onCreate(new Date(year, month, day, 9, 0))}
            >
              {day !== null && (
                <>
                  <div className={`w-6 h-6 flex items-center justify-center rounded-full text-xs font-medium mb-1 ${isToday ? 'bg-primary text-primary-foreground' : 'text-muted-foreground'}`}>
                    {day}
                  </div>
                  <DayMarkers markers={dayMarkers.get(dateKey(year, month, day)) ?? []} />
                  <div className="space-y-0.5">
                    {dayTermins.slice(0, 3).map(t => (
                      <div
                        key={t.id}
                        onClick={e => { e.stopPropagation(); onSelect(t.id) }}
                        className={`truncate text-[11px] px-1.5 py-0.5 rounded cursor-pointer font-medium ${pillColor(t)} ${selectedId === t.id ? 'ring-2 ring-offset-1 ring-primary' : ''}`}
                      >
                        {new Date(t.start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })} {t.title}
                      </div>
                    ))}
                    {dayTermins.length > 3 && (
                      <p className="text-[11px] text-muted-foreground pl-1">+{dayTermins.length - 3} more</p>
                    )}
                  </div>
                </>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}

// ─── Week view ────────────────────────────────────────────────────────────────

interface WeekViewProps {
  weekStart: Date
  termins: TerminSummary[]
  selectedId: string | null
  onSelect: (id: string) => void
  onCreate: (date: Date) => void
}

function WeekView({ weekStart, termins, selectedId, onSelect, onCreate }: WeekViewProps) {
  const HOURS = Array.from({ length: 24 }, (_, i) => i)
  const days = Array.from({ length: 7 }, (_, i) => {
    const d = new Date(weekStart)
    d.setDate(weekStart.getDate() + i)
    return d
  })
  const today = new Date()
  const CELL_H = 56 // px per hour

  const byDay = useMemo(() => {
    const map = new Map<string, TerminSummary[]>()
    for (const t of termins) {
      const d = new Date(t.start)
      const key = d.toDateString()
      if (!map.has(key)) map.set(key, [])
      map.get(key)!.push(t)
    }
    return map
  }, [termins])

  return (
    <div className="flex-1 overflow-auto">
      {/* Day headers */}
      <div className="grid grid-cols-[48px_repeat(7,1fr)] border-b sticky top-0 bg-card z-10">
        <div />
        {days.map(d => {
          const isToday = d.toDateString() === today.toDateString()
          return (
            <div key={d.toISOString()} className="py-2 text-center">
              <p className="text-xs text-muted-foreground">{d.toLocaleDateString('en-US', { weekday: 'short' })}</p>
              <div className={`mx-auto mt-0.5 w-7 h-7 flex items-center justify-center rounded-full text-sm font-medium ${isToday ? 'bg-primary text-primary-foreground' : ''}`}>
                {d.getDate()}
              </div>
            </div>
          )
        })}
      </div>

      {/* Time grid */}
      <div className="grid grid-cols-[48px_repeat(7,1fr)]">
        {/* Hour labels */}
        <div>
          {HOURS.map(h => (
            <div key={h} style={{ height: CELL_H }} className="flex items-start justify-end pr-2 pt-0.5">
              <span className="text-[10px] text-muted-foreground">
                {h === 0 ? '' : `${h % 12 || 12}${h < 12 ? 'a' : 'p'}`}
              </span>
            </div>
          ))}
        </div>

        {/* Day columns */}
        {days.map(day => (
          <div
            key={day.toISOString()}
            className="relative border-l"
            style={{ height: CELL_H * 24 }}
            onDoubleClick={e => {
              const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
              const y = e.clientY - rect.top
              const hour = Math.floor(y / CELL_H)
              const d = new Date(day)
              d.setHours(hour, 0, 0, 0)
              onCreate(d)
            }}
          >
            {/* Hour lines */}
            {HOURS.map(h => (
              <div key={h} style={{ top: h * CELL_H }} className="absolute w-full border-t border-dashed border-border/40" />
            ))}

            {/* Events */}
            {(byDay.get(day.toDateString()) ?? []).map(t => {
              const s = new Date(t.start)
              const e = new Date(t.end)
              const startMin = s.getHours() * 60 + s.getMinutes()
              const duration = Math.max(15, (e.getTime() - s.getTime()) / 60000)
              const top = (startMin / 60) * CELL_H
              const height = (duration / 60) * CELL_H

              return (
                <div
                  key={t.id}
                  style={{ top, height: Math.max(height, 20), left: 2, right: 2 }}
                  className={`absolute rounded px-1.5 py-0.5 text-[11px] font-medium cursor-pointer overflow-hidden ${pillColor(t)} ${selectedId === t.id ? 'ring-2 ring-offset-1 ring-primary' : ''}`}
                  onClick={e => { e.stopPropagation(); onSelect(t.id) }}
                >
                  <p className="truncate">{t.title}</p>
                  {height > 30 && (
                    <p className="opacity-80">
                      {s.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                    </p>
                  )}
                </div>
              )
            })}
          </div>
        ))}
      </div>
    </div>
  )
}

// ─── CalendarPage ─────────────────────────────────────────────────────────────

type ViewMode = 'month' | 'week'

export function CalendarPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [viewMode, setViewMode] = useState<ViewMode>('month')
  const [currentDate, setCurrentDate] = useState(new Date())
  const [showCreate, setShowCreate] = useState(false)
  const [createDefaultStart, setCreateDefaultStart] = useState<Date | undefined>()
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [pendingExpanded, setPendingExpanded] = useState(true)

  // open from notification link ?termin=xxx
  useEffect(() => {
    const t = searchParams.get('termin')
    if (t) { setSelectedId(t); setSearchParams({}, { replace: true }) }
  }, [])

  const year = currentDate.getFullYear()
  const month = currentDate.getMonth()

  const weekStart = useMemo(() => {
    const d = new Date(currentDate)
    d.setHours(0, 0, 0, 0)
    d.setDate(d.getDate() - d.getDay())
    return d
  }, [currentDate])

  const { start, end } = useMemo(() => {
    if (viewMode === 'month') {
      return {
        start: new Date(year, month, 1),
        end: new Date(year, month + 1, 0, 23, 59, 59),
      }
    }
    return {
      start: weekStart,
      end: new Date(weekStart.getTime() + 7 * 24 * 60 * 60 * 1000 - 1),
    }
  }, [viewMode, year, month, weekStart])

  const { data: termins = [] } = useQuery({
    queryKey: ['termins', start.toISOString(), end.toISOString()],
    queryFn: () => getTermins(start, end),
  })

  const { data: pendingInvitations = [] } = useQuery({
    queryKey: ['termins-pending'],
    queryFn: getPendingInvitations,
  })

  // Non-working days and the viewer's own approved absences are shown as read-only markers.
  // Nothing here creates a Termin — the calendar reads this data, it does not own it.
  const { data: nonWorkingDays = [] } = useQuery({
    queryKey: ['worktime', 'non-working-days', 'calendar', toDateKey(start), toDateKey(end)],
    queryFn: () => getNonWorkingDays(toDateKey(start), toDateKey(end)),
  })

  const { data: myAbsences = [] } = useQuery({
    queryKey: ['absences', 'calendar', year],
    queryFn: () => getAbsences(year, 'Approved'),
  })

  const dayMarkers = useMemo(
    () => buildDayMarkers(nonWorkingDays, myAbsences),
    [nonWorkingDays, myAbsences],
  )

  const navigate = (dir: -1 | 1) => {
    if (viewMode === 'month') {
      setCurrentDate(new Date(year, month + dir, 1))
    } else {
      setCurrentDate(prev => new Date(prev.getTime() + dir * 7 * 24 * 60 * 60 * 1000))
    }
  }

  const goToday = () => setCurrentDate(new Date())

  const headingLabel = viewMode === 'month'
    ? currentDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
    : `${weekStart.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} – ${new Date(weekStart.getTime() + 6 * 24 * 60 * 60 * 1000).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`

  const openCreate = (date?: Date) => {
    setCreateDefaultStart(date)
    setShowCreate(true)
  }

  return (
    <div className="flex h-full overflow-hidden">
      {/* Calendar pane */}
      <div className={`flex flex-col flex-1 overflow-hidden transition-all ${selectedId ? 'min-w-0' : ''}`}>
        {/* Toolbar */}
        <div className="flex items-center gap-3 px-6 py-3 border-b bg-card shrink-0">
          <Button size="sm" variant="outline" onClick={goToday}>Today</Button>
          <button onClick={() => navigate(-1)} className="p-1.5 rounded hover:bg-muted"><ChevronLeft className="h-4 w-4" /></button>
          <button onClick={() => navigate(1)} className="p-1.5 rounded hover:bg-muted"><ChevronRight className="h-4 w-4" /></button>
          <h2 className="text-base font-semibold flex-1">{headingLabel}</h2>

          <div className="flex items-center gap-1 rounded-lg border p-0.5">
            <button onClick={() => setViewMode('month')}
              className={`flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium transition-colors ${viewMode === 'month' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted'}`}>
              <LayoutGrid className="h-3 w-3" /> Month
            </button>
            <button onClick={() => setViewMode('week')}
              className={`flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium transition-colors ${viewMode === 'week' ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted'}`}>
              <Calendar className="h-3 w-3" /> Week
            </button>
          </div>

          <Button size="sm" onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-1" /> New Meeting
          </Button>
        </div>

        {/* Legend */}
        <div className="flex items-center gap-4 px-6 py-1.5 border-b bg-card/50 text-[11px] text-muted-foreground shrink-0">
          {[
            { color: 'bg-blue-500', label: 'Organized by me' },
            { color: 'bg-violet-500', label: 'Invited (pending)' },
            { color: 'bg-green-500', label: 'Accepted' },
            { color: 'bg-amber-400', label: 'Reschedule proposed' },
            { color: 'bg-red-400', label: 'Declined' },
          ].map(({ color, label }) => (
            <span key={label} className="flex items-center gap-1">
              <span className={`w-2 h-2 rounded-full ${color}`} /> {label}
            </span>
          ))}
          <span className="ml-auto">Double-click a day to create</span>
        </div>

        {/* Pending invitations banner */}
        {pendingInvitations.length > 0 && (
          <div className="shrink-0 border-b bg-violet-50/60 dark:bg-violet-950/20">
            <button
              className="w-full flex items-center gap-2 px-6 py-2 text-xs font-semibold text-violet-700 dark:text-violet-400 hover:bg-violet-100/60 dark:hover:bg-violet-900/30 transition-colors"
              onClick={() => setPendingExpanded(p => !p)}
            >
              <Bell className="h-3.5 w-3.5" />
              <span>Awaiting your response ({pendingInvitations.length})</span>
              {pendingExpanded ? <ChevronUp className="h-3.5 w-3.5 ml-auto" /> : <ChevronDown className="h-3.5 w-3.5 ml-auto" />}
            </button>

            {pendingExpanded && (
              <div className="px-6 pb-2 space-y-1">
                {pendingInvitations.map(t => (
                  <div
                    key={t.id}
                    className="flex items-center gap-3 rounded-lg border border-violet-200 dark:border-violet-800 bg-card px-3 py-2 cursor-pointer hover:bg-muted/50"
                    onClick={() => setSelectedId(t.id)}
                  >
                    <div className={`w-2 h-2 rounded-full shrink-0 ${t.myInvitationStatus === 'RescheduleProposed' ? 'bg-amber-400' : 'bg-violet-500'}`} />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate">{t.title}</p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(t.start).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })}
                        {' · '}
                        {new Date(t.start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                        {' – '}
                        {new Date(t.end).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                        {' · by '}
                        {t.organizerName}
                      </p>
                    </div>
                    <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium shrink-0 ${t.myInvitationStatus === 'RescheduleProposed' ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400' : 'bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-400'}`}>
                      {t.myInvitationStatus === 'RescheduleProposed' ? 'Reschedule proposed' : 'Pending'}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* View */}
        {viewMode === 'month' ? (
          <MonthView
            year={year} month={month} termins={termins}
            dayMarkers={dayMarkers}
            selectedId={selectedId} onSelect={setSelectedId}
            onCreate={openCreate}
          />
        ) : (
          <WeekView
            weekStart={weekStart} termins={termins}
            selectedId={selectedId} onSelect={setSelectedId}
            onCreate={openCreate}
          />
        )}
      </div>

      {/* Detail panel — split panel */}
      {selectedId && (
        <div className="w-80 xl:w-96 shrink-0 overflow-hidden flex flex-col border-l">
          <TerminDetailPanel terminId={selectedId} onClose={() => setSelectedId(null)} />
        </div>
      )}

      {showCreate && (
        <CreateTerminModal defaultStart={createDefaultStart} onClose={() => setShowCreate(false)} />
      )}
    </div>
  )
}
