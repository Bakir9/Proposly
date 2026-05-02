import { useState, useEffect, useRef } from 'react'
import type { ProjectDetail } from '@/api/projects'

const TASK_STATUS_COLOR: Record<string, string> = {
  Todo: 'bg-slate-400',
  InProgress: 'bg-blue-500',
  InReview: 'bg-amber-400',
  Done: 'bg-green-500',
}

const DAY_MS = 86_400_000

interface DragState {
  taskId: string
  type: 'left' | 'right' | 'move'
  startX: number
  containerWidth: number
  originalStartMs: number
  originalDueMs: number
  currentStartMs: number
  currentDueMs: number
  minMs: number
  rangeMs: number
}

function snapToDay(ms: number) {
  return Math.round(ms / DAY_MS) * DAY_MS
}

function toDateString(ms: number) {
  return new Date(ms).toISOString().slice(0, 10)
}

function fmtShort(ms: number) {
  return new Date(ms).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' })
}

interface Props {
  project: ProjectDetail
  onTaskDatesChange: (taskId: string, startDate: string, dueDate: string) => void
}

export function GanttChart({ project, onTaskDatesChange }: Props) {
  const [drag, setDrag] = useState<DragState | null>(null)
  const dragRef = useRef<DragState | null>(null)
  dragRef.current = drag

  const containerRefs = useRef<Map<string, HTMLDivElement>>(new Map())

  const parseDate = (s: string) => new Date(s)
  const toMs = (d: Date) => d.getTime()

  const milestones = project.milestones
  const tasksWithDates = project.tasks.filter(t => t.startDate && t.dueDate)

  const allDates = [
    parseDate(project.startDate),
    ...(project.deadline ? [parseDate(project.deadline)] : []),
    ...tasksWithDates.flatMap(t => [parseDate(t.startDate!), parseDate(t.dueDate!)]),
    ...milestones.map(m => parseDate(m.dueDate)),
  ]

  if (allDates.length === 0) {
    return <p className="text-sm text-muted-foreground">No tasks with dates to display.</p>
  }

  const minMs = Math.min(...allDates.map(toMs))
  const maxMs = Math.max(...allDates.map(toMs))
  const rangeMs = maxMs - minMs || 1

  const pct = (d: Date) => ((toMs(d) - minMs) / rangeMs) * 100

  const totalDays = Math.ceil(rangeMs / DAY_MS)
  const step = totalDays <= 30 ? 7 : totalDays <= 90 ? 14 : 30
  const ticks: Date[] = []
  for (let i = 0; i * DAY_MS <= rangeMs; i += step) {
    ticks.push(new Date(minMs + i * DAY_MS))
  }

  const startDrag = (
    e: React.MouseEvent,
    taskId: string,
    type: DragState['type'],
    startMs: number,
    dueMs: number,
  ) => {
    if (dragRef.current) return
    e.preventDefault()
    e.stopPropagation()
    const containerEl = containerRefs.current.get(taskId)
    if (!containerEl) return
    const rect = containerEl.getBoundingClientRect()
    const state: DragState = {
      taskId,
      type,
      startX: e.clientX,
      containerWidth: rect.width,
      originalStartMs: startMs,
      originalDueMs: dueMs,
      currentStartMs: startMs,
      currentDueMs: dueMs,
      minMs,
      rangeMs,
    }
    document.body.style.cursor = type === 'move' ? 'grabbing' : 'ew-resize'
    setDrag(state)
  }

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      const prev = dragRef.current
      if (!prev) return
      const dx = e.clientX - prev.startX
      const dMs = (dx / prev.containerWidth) * prev.rangeMs
      const minDuration = DAY_MS

      let newStart = prev.originalStartMs
      let newDue = prev.originalDueMs

      if (prev.type === 'move') {
        newStart = snapToDay(prev.originalStartMs + dMs)
        newDue = snapToDay(prev.originalDueMs + dMs)
      } else if (prev.type === 'left') {
        newStart = snapToDay(prev.originalStartMs + dMs)
        if (newStart >= prev.originalDueMs - minDuration) {
          newStart = prev.originalDueMs - minDuration
        }
        newDue = prev.originalDueMs
      } else {
        newDue = snapToDay(prev.originalDueMs + dMs)
        if (newDue <= prev.originalStartMs + minDuration) {
          newDue = prev.originalStartMs + minDuration
        }
        newStart = prev.originalStartMs
      }

      setDrag({ ...prev, currentStartMs: newStart, currentDueMs: newDue })
    }

    const onUp = () => {
      const cur = dragRef.current
      if (!cur) return
      if (cur.currentStartMs !== cur.originalStartMs || cur.currentDueMs !== cur.originalDueMs) {
        onTaskDatesChange(cur.taskId, toDateString(cur.currentStartMs), toDateString(cur.currentDueMs))
      }
      document.body.style.cursor = ''
      setDrag(null)
    }

    window.addEventListener('mousemove', onMove)
    window.addEventListener('mouseup', onUp)
    return () => {
      window.removeEventListener('mousemove', onMove)
      window.removeEventListener('mouseup', onUp)
      document.body.style.cursor = ''
    }
  }, [onTaskDatesChange])

  return (
    <div className={`overflow-x-auto${drag ? ' select-none' : ''}`}>
      <div style={{ minWidth: 600 }}>
        {/* Date axis */}
        <div className="relative h-6 mb-1 ml-40">
          {ticks.map((tick, i) => (
            <span
              key={i}
              className="absolute text-xs text-muted-foreground -translate-x-1/2"
              style={{ left: `${pct(tick)}%` }}
            >
              {tick.toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' })}
            </span>
          ))}
        </div>

        {/* Gridlines + rows */}
        <div className="relative">
          {/* Vertical grid ticks */}
          <div className="absolute inset-0 ml-40 pointer-events-none">
            {ticks.map((tick, i) => (
              <div
                key={i}
                className="absolute top-0 bottom-0 border-l border-dashed border-muted"
                style={{ left: `${pct(tick)}%` }}
              />
            ))}
          </div>

          {/* Today marker */}
          {(() => {
            const todayMs = new Date().setHours(0, 0, 0, 0)
            if (todayMs >= minMs && todayMs <= maxMs) {
              return (
                <div
                  className="absolute top-0 bottom-0 border-l-2 border-red-400 ml-40 pointer-events-none z-10"
                  style={{ left: `${((todayMs - minMs) / rangeMs) * 100}%` }}
                />
              )
            }
          })()}

          {/* Task rows */}
          {project.tasks.map(task => {
            const isDragging = drag?.taskId === task.id
            const rawStartMs = task.startDate ? parseDate(task.startDate).getTime() : null
            const rawDueMs = task.dueDate ? parseDate(task.dueDate).getTime() : null
            const startMs = isDragging ? drag!.currentStartMs : rawStartMs
            const dueMs = isDragging ? drag!.currentDueMs : rawDueMs
            const hasBar = startMs !== null && dueMs !== null

            const leftPct = hasBar ? ((startMs! - minMs) / rangeMs) * 100 : 0
            const widthPct = hasBar ? Math.max(((dueMs! - startMs!) / rangeMs) * 100, 0.5) : 0

            return (
              <div key={task.id} className="flex items-center h-8 mb-1">
                <div
                  className="w-40 shrink-0 pr-3 text-xs text-right text-foreground truncate"
                  title={task.title}
                >
                  {task.title}
                </div>
                <div
                  ref={el => {
                    if (el) containerRefs.current.set(task.id, el)
                    else containerRefs.current.delete(task.id)
                  }}
                  className="relative flex-1 h-5 bg-muted rounded"
                >
                  {hasBar ? (
                    <div
                      className={`absolute top-0 h-full rounded ${TASK_STATUS_COLOR[task.status] ?? 'bg-slate-400'} ${isDragging ? 'opacity-100 ring-2 ring-white/50' : 'opacity-80'} group`}
                      style={{ left: `${leftPct}%`, width: `${widthPct}%` }}
                      title={`${task.status} · ${task.startDate} → ${task.dueDate}`}
                    >
                      {/* Left resize handle */}
                      <div
                        className="absolute left-0 top-0 h-full w-1.5 cursor-ew-resize rounded-l z-20 opacity-0 group-hover:opacity-100 bg-white/30"
                        onMouseDown={e => startDrag(e, task.id, 'left', rawStartMs!, rawDueMs!)}
                      />
                      {/* Bar body — move */}
                      <div
                        className="absolute inset-x-1.5 inset-y-0 cursor-grab active:cursor-grabbing"
                        onMouseDown={e => startDrag(e, task.id, 'move', rawStartMs!, rawDueMs!)}
                      />
                      {/* Right resize handle */}
                      <div
                        className="absolute right-0 top-0 h-full w-1.5 cursor-ew-resize rounded-r z-20 opacity-0 group-hover:opacity-100 bg-white/30"
                        onMouseDown={e => startDrag(e, task.id, 'right', rawStartMs!, rawDueMs!)}
                      />
                      {/* Date tooltip during drag */}
                      {isDragging && (
                        <div className="absolute -top-6 left-1/2 -translate-x-1/2 bg-popover text-popover-foreground text-xs px-2 py-0.5 rounded shadow-md whitespace-nowrap pointer-events-none z-30">
                          {fmtShort(drag!.currentStartMs)} → {fmtShort(drag!.currentDueMs)}
                        </div>
                      )}
                    </div>
                  ) : (
                    <span className="absolute left-1 top-0 h-full flex items-center text-xs text-muted-foreground">
                      no dates
                    </span>
                  )}
                </div>
              </div>
            )
          })}

          {/* Milestone markers */}
          {milestones.map(m => (
            <div key={m.id} className="flex items-center h-8 mb-1">
              <div
                className="w-40 shrink-0 pr-3 text-xs text-right text-muted-foreground truncate"
                title={m.title}
              >
                ◆ {m.title}
              </div>
              <div className="relative flex-1 h-5">
                <div
                  className={`absolute top-1/2 -translate-y-1/2 -translate-x-1/2 w-3 h-3 rotate-45 ${m.isCompleted ? 'bg-green-500' : 'bg-amber-400'}`}
                  style={{ left: `${pct(parseDate(m.dueDate))}%` }}
                  title={`${m.title} · ${m.dueDate}${m.isCompleted ? ' (done)' : ''}`}
                />
              </div>
            </div>
          ))}
        </div>

        {/* Legend */}
        <div className="flex items-center gap-4 mt-3 ml-40 text-xs text-muted-foreground">
          <span className="flex items-center gap-1">
            <span className="w-3 h-3 rounded bg-slate-400 inline-block" />Todo
          </span>
          <span className="flex items-center gap-1">
            <span className="w-3 h-3 rounded bg-blue-500 inline-block" />In Progress
          </span>
          <span className="flex items-center gap-1">
            <span className="w-3 h-3 rounded bg-amber-400 inline-block" />In Review
          </span>
          <span className="flex items-center gap-1">
            <span className="w-3 h-3 rounded bg-green-500 inline-block" />Done
          </span>
          <span className="flex items-center gap-1">
            <span className="w-3 h-3 rotate-45 bg-amber-400 inline-block" />Milestone
          </span>
          <span className="flex items-center gap-1">
            <span className="border-l-2 border-red-400 h-3 inline-block" />Today
          </span>
          <span className="flex items-center gap-1 ml-2 text-muted-foreground/70">
            Drag bars to move · drag edges to resize
          </span>
        </div>
      </div>
    </div>
  )
}
