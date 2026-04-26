import { useState, useEffect } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { useNotificationTask } from '@/contexts/NotificationTaskContext'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getProjectById,
  getBurndown,
  updateProject,
  addTask,
  updateTask,
  updateTaskStatus,
  addProjectMember,
  logTime,
  addExpense,
  addMilestone,
  completeMilestone,
  addTaskComment,
  editTaskComment,
  deleteTaskComment,
} from '@/api/projects'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts'
import type { ProjectTask } from '@/api/projects'
import { getActiveUsers } from '@/api/users'
import { useAuth } from '@/features/auth/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Tabs } from '@/components/ui/tabs'
import { Dialog } from '@/components/ui/dialog'
import { Textarea } from '@/components/ui/textarea'
import { ArrowLeft, Pencil, Plus, UserPlus, Clock, Receipt, Flag, CheckCircle2, MessageSquare, Send, Trash2, CalendarDays, SquareCheck, Maximize2, Minimize2 } from 'lucide-react'

const KANBAN_COLUMNS = [
  { status: 'Todo', label: 'Not Started' },
  { status: 'InProgress', label: 'In Progress' },
  { status: 'InReview', label: 'In Review' },
  { status: 'Done', label: 'Finished' },
]

const projectStatusVariant: Record<string, 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'> = {
  Planning: 'secondary',
  Active: 'default',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'destructive',
}

const TABS = [
  { id: 'overview', label: 'Overview' },
  { id: 'tasks', label: 'Tasks' },
  { id: 'gantt', label: 'Gantt' },
  { id: 'burndown', label: 'Burndown' },
  { id: 'team', label: 'Team' },
  { id: 'timelog', label: 'Time Log' },
  { id: 'expenses', label: 'Expenses' },
  { id: 'milestones', label: 'Milestones' },
]

const TASK_STATUS_COLOR: Record<string, string> = {
  Todo: 'bg-slate-400',
  InProgress: 'bg-blue-500',
  InReview: 'bg-amber-400',
  Done: 'bg-green-500',
}

const TASK_STATUS_BORDER_COLOR: Record<string, string> = {
  Todo: '#94a3b8',
  InProgress: '#3b82f6',
  InReview: '#fbbf24',
  Done: '#22c55e',
}

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d ago`
  return new Date(dateStr).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function GanttChart({ project }: { project: import('@/api/projects').ProjectDetail }) {
  const parseDate = (s: string) => new Date(s)
  const toMs = (d: Date) => d.getTime()

  const tasksWithDates = project.tasks.filter(t => t.startDate && t.dueDate)
  const milestones = project.milestones

  const allDates = [
    parseDate(project.startDate),
    ...(project.deadline ? [parseDate(project.deadline)] : []),
    ...tasksWithDates.flatMap(t => [parseDate(t.startDate!), parseDate(t.dueDate!)]),
    ...milestones.map(m => parseDate(m.dueDate)),
  ]

  if (allDates.length === 0) return <p className="text-sm text-muted-foreground">No tasks with dates to display.</p>

  const minMs = Math.min(...allDates.map(toMs))
  const maxMs = Math.max(...allDates.map(toMs))
  const rangeMs = maxMs - minMs || 1

  const pct = (d: Date) => ((toMs(d) - minMs) / rangeMs) * 100

  const DAY_MS = 86_400_000
  const totalDays = Math.ceil(rangeMs / DAY_MS)
  const step = totalDays <= 30 ? 7 : totalDays <= 90 ? 14 : 30
  const ticks: Date[] = []
  for (let i = 0; i * DAY_MS <= rangeMs; i += step) {
    ticks.push(new Date(minMs + i * DAY_MS))
  }

  return (
    <div className="overflow-x-auto">
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
          {project.tasks.map(task => (
            <div key={task.id} className="flex items-center h-8 mb-1">
              <div className="w-40 shrink-0 pr-3 text-xs text-right text-foreground truncate" title={task.title}>
                {task.title}
              </div>
              <div className="relative flex-1 h-5 bg-muted rounded">
                {task.startDate && task.dueDate ? (
                  <div
                    className={`absolute top-0 h-full rounded ${TASK_STATUS_COLOR[task.status] ?? 'bg-slate-400'} opacity-80`}
                    style={{
                      left: `${pct(parseDate(task.startDate))}%`,
                      width: `${Math.max(pct(parseDate(task.dueDate)) - pct(parseDate(task.startDate)), 1)}%`,
                    }}
                    title={`${task.status} · ${task.startDate} → ${task.dueDate}`}
                  />
                ) : (
                  <span className="absolute left-1 top-0 h-full flex items-center text-xs text-muted-foreground">no dates</span>
                )}
              </div>
            </div>
          ))}

          {/* Milestone markers */}
          {milestones.map(m => (
            <div key={m.id} className="flex items-center h-8 mb-1">
              <div className="w-40 shrink-0 pr-3 text-xs text-right text-muted-foreground truncate" title={m.title}>
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
          <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-slate-400 inline-block" />Todo</span>
          <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-blue-500 inline-block" />In Progress</span>
          <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-amber-400 inline-block" />In Review</span>
          <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-green-500 inline-block" />Done</span>
          <span className="flex items-center gap-1"><span className="w-3 h-3 rotate-45 bg-amber-400 inline-block" />Milestone</span>
          <span className="flex items-center gap-1"><span className="border-l-2 border-red-400 h-3 inline-block" />Today</span>
        </div>
      </div>
    </div>
  )
}

function BurndownTab({ projectId }: { projectId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['burndown', projectId],
    queryFn: () => getBurndown(projectId),
  })

  const fmtDay = (s: string) => {
    const [, m, d] = s.split('-')
    return `${d}/${m}`
  }

  if (isLoading) return <p className="text-sm text-muted-foreground py-8 text-center">Loading...</p>
  if (!data || data.totalTasks === 0) return <p className="text-sm text-muted-foreground py-8 text-center">No tasks yet — add tasks to see the burndown chart.</p>

  const chartData = data.actual.map((point, i) => ({
    date: point.date,
    actual: point.count,
    ideal: data.ideal[i]?.count ?? 0,
  }))

  const completedCount = data.totalTasks - (chartData[chartData.length - 1]?.actual ?? data.totalTasks)

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-3 gap-3 text-sm">
        <Card><CardContent className="py-3"><span className="text-muted-foreground text-xs">Total tasks</span><p className="font-semibold text-lg">{data.totalTasks}</p></CardContent></Card>
        <Card><CardContent className="py-3"><span className="text-muted-foreground text-xs">Completed</span><p className="font-semibold text-lg text-green-600">{completedCount}</p></CardContent></Card>
        <Card><CardContent className="py-3"><span className="text-muted-foreground text-xs">Remaining</span><p className="font-semibold text-lg text-blue-600">{data.totalTasks - completedCount}</p></CardContent></Card>
      </div>
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Remaining Tasks Over Time</CardTitle>
          <p className="text-xs text-muted-foreground">{data.startDate} → {data.endDate}</p>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={320}>
            <LineChart data={chartData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} tickFormatter={fmtDay} minTickGap={30} />
              <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
              <Tooltip
                labelFormatter={l => String(l)}
                formatter={(value: number, name: string) => [value, name === 'actual' ? 'Actual remaining' : 'Ideal remaining']}
              />
              <Legend formatter={v => v === 'actual' ? 'Actual' : 'Ideal'} />
              <Line type="monotone" dataKey="ideal" stroke="#94a3b8" strokeDasharray="5 5" dot={false} strokeWidth={1.5} />
              <Line type="monotone" dataKey="actual" stroke="#3b82f6" strokeWidth={2} dot={false} activeDot={{ r: 4 }} />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </div>
  )
}

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { pendingTaskId, setPendingTaskId } = useNotificationTask()
  const queryClient = useQueryClient()

  const { user: authUser } = useAuth()

  const { data: project, isLoading, isError } = useQuery({
    queryKey: ['project', id],
    queryFn: () => getProjectById(id!),
  })

  const { data: users } = useQuery({ queryKey: ['users', 'active'], queryFn: getActiveUsers })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['project', id] })

  // Notification click: task ID passed via context
  useEffect(() => {
    if (!project || !pendingTaskId) return
    const task = project.tasks.find(t => t.id === pendingTaskId)
    if (task) {
      setPendingTaskId(null)
      openEditTask(task)
    }
  }, [project, pendingTaskId]) // eslint-disable-line react-hooks/exhaustive-deps

  // Direct link: task ID in URL ?task= param
  useEffect(() => {
    if (!project) return
    const taskId = searchParams.get('task')
    if (!taskId) return
    const task = project.tasks.find(t => t.id === taskId)
    if (task) {
      setSearchParams(p => { const n = new URLSearchParams(p); n.delete('task'); return n }, { replace: true })
      openEditTask(task)
    }
  }, [project, searchParams]) // eslint-disable-line react-hooks/exhaustive-deps

  // Task dialog
  const [taskOpen, setTaskOpen] = useState(false)
  const [taskTitle, setTaskTitle] = useState('')
  const [taskDesc, setTaskDesc] = useState('')
  const [taskHours, setTaskHours] = useState('')
  const [taskStart, setTaskStart] = useState('')
  const [taskDue, setTaskDue] = useState('')
  const [taskMilestoneId, setTaskMilestoneId] = useState('')
  const [taskAssignedMemberId, setTaskAssignedMemberId] = useState('')

  // Edit task dialog
  const [editTaskOpen, setEditTaskOpen] = useState(false)
  const [editingTask, setEditingTask] = useState<ProjectTask | null>(null)
  const [editTaskTitle, setEditTaskTitle] = useState('')
  const [editTaskDesc, setEditTaskDesc] = useState('')
  const [editTaskHours, setEditTaskHours] = useState('')
  const [editTaskStart, setEditTaskStart] = useState('')
  const [editTaskDue, setEditTaskDue] = useState('')
  const [editTaskMilestoneId, setEditTaskMilestoneId] = useState('')
  const [editTaskAssignedMemberId, setEditTaskAssignedMemberId] = useState('')
  const [editTaskStatus, setEditTaskStatus] = useState('')

  // Member dialog
  const [memberOpen, setMemberOpen] = useState(false)
  const [memberUserId, setMemberUserId] = useState('')
  const [memberName, setMemberName] = useState('')
  const [memberRole, setMemberRole] = useState('')
  const [memberRate, setMemberRate] = useState('')
  const [memberCurrency, setMemberCurrency] = useState('EUR')

  // Time dialog
  const [timeOpen, setTimeOpen] = useState(false)
  const [timeMemberId, setTimeMemberId] = useState('')
  const [timeHours, setTimeHours] = useState('')
  const [timeDate, setTimeDate] = useState('')
  const [timeDesc, setTimeDesc] = useState('')

  // Expense dialog
  const [expenseOpen, setExpenseOpen] = useState(false)
  const [expenseDesc, setExpenseDesc] = useState('')
  const [expenseAmount, setExpenseAmount] = useState('')
  const [expenseCurrency, setExpenseCurrency] = useState('EUR')
  const [expenseCategory, setExpenseCategory] = useState('')
  const [expenseDate, setExpenseDate] = useState('')

  // Edit project dialog
  const [editOpen, setEditOpen] = useState(false)
  const [editName, setEditName] = useState('')
  const [editDescription, setEditDescription] = useState('')
  const [editDeadline, setEditDeadline] = useState('')
  const [editStatus, setEditStatus] = useState('')
  const [editBudget, setEditBudget] = useState('')
  const [editCurrency, setEditCurrency] = useState('EUR')

  // Milestone dialog
  const [milestoneOpen, setMilestoneOpen] = useState(false)
  const [milestoneTitle, setMilestoneTitle] = useState('')
  const [milestoneDue, setMilestoneDue] = useState('')

  // Task filters
  const [taskDateFilter, setTaskDateFilter] = useState<'all' | 'this-week' | 'this-month' | 'next-week' | 'next-month' | 'overdue'>('all')
  const [taskMemberFilter, setTaskMemberFilter] = useState<string>('all')

  // Description expand state (shared across both task modals)
  const [descExpanded, setDescExpanded] = useState(false)

  // Comment state (within edit task dialog)
  const [commentBody, setCommentBody] = useState('')
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null)
  const [editingCommentBody, setEditingCommentBody] = useState('')

  // Kanban drag state
  const [draggedTaskId, setDraggedTaskId] = useState<string | null>(null)
  const [dragOverColumn, setDragOverColumn] = useState<string | null>(null)

  const mutAddTask = useMutation({
    mutationFn: () => addTask(id!, {
      title: taskTitle,
      description: taskDesc || undefined,
      estimatedHours: taskHours ? parseFloat(taskHours) : undefined,
      startDate: taskStart || undefined,
      dueDate: taskDue || undefined,
      milestoneId: taskMilestoneId || undefined,
      assignedMemberId: taskAssignedMemberId || undefined,
    }),
    onSuccess: () => {
      invalidate()
      setTaskOpen(false)
      setTaskTitle('')
      setTaskDesc('')
      setTaskHours('')
      setTaskStart('')
      setTaskDue('')
      setTaskMilestoneId('')
      setTaskAssignedMemberId('')
    },
  })

  const mutTaskStatus = useMutation({
    mutationFn: ({ taskId, status }: { taskId: string; status: string }) => updateTaskStatus(id!, taskId, status),
    onSuccess: invalidate,
  })

  const openEditTask = (task: ProjectTask) => {
    setEditingTask(task)
    setEditTaskTitle(task.title)
    setEditTaskDesc(task.description ?? '')
    setEditTaskHours(task.estimatedHours?.toString() ?? '')
    setEditTaskStart(task.startDate ? task.startDate.slice(0, 10) : '')
    setEditTaskDue(task.dueDate ? task.dueDate.slice(0, 10) : '')
    setEditTaskMilestoneId(task.milestoneId ?? '')
    setEditTaskAssignedMemberId(task.assignedMemberId ?? '')
    setEditTaskStatus(task.status)
    setCommentBody('')
    setEditingCommentId(null)
    setEditingCommentBody('')
    setDescExpanded(false)
    setEditTaskOpen(true)
  }

  const mutUpdateTask = useMutation({
    mutationFn: async () => {
      await updateTask(id!, editingTask!.id, {
        title: editTaskTitle,
        description: editTaskDesc || undefined,
        estimatedHours: editTaskHours ? parseFloat(editTaskHours) : undefined,
        startDate: editTaskStart || undefined,
        dueDate: editTaskDue || undefined,
        milestoneId: editTaskMilestoneId || undefined,
        assignedMemberId: editTaskAssignedMemberId || undefined,
      })
      if (editTaskStatus !== editingTask!.status) {
        await updateTaskStatus(id!, editingTask!.id, editTaskStatus)
      }
    },
    onSuccess: () => {
      invalidate()
      setEditTaskOpen(false)
      setEditingTask(null)
      setCommentBody('')
      setEditingCommentId(null)
      setEditingCommentBody('')
    },
  })

  const mutAddComment = useMutation({
    mutationFn: () => addTaskComment(id!, editingTask!.id, commentBody),
    onSuccess: () => { invalidate(); setCommentBody('') },
  })

  const mutEditComment = useMutation({
    mutationFn: ({ commentId }: { commentId: string }) => editTaskComment(id!, editingTask!.id, commentId, editingCommentBody),
    onSuccess: () => { invalidate(); setEditingCommentId(null); setEditingCommentBody('') },
  })

  const mutDeleteComment = useMutation({
    mutationFn: ({ commentId }: { commentId: string }) => deleteTaskComment(id!, editingTask!.id, commentId),
    onSuccess: invalidate,
  })

  const mutAddMember = useMutation({
    mutationFn: () => addProjectMember(id!, {
      userId: memberUserId,
      name: memberName,
      role: memberRole,
      hourlyRate: parseFloat(memberRate),
      currency: memberCurrency,
    }),
    onSuccess: () => { invalidate(); setMemberOpen(false); setMemberUserId(''); setMemberName(''); setMemberRole(''); setMemberRate('') },
  })

  const mutLogTime = useMutation({
    mutationFn: () => logTime(id!, {
      memberId: timeMemberId,
      hoursWorked: parseFloat(timeHours),
      description: timeDesc || undefined,
      date: timeDate,
    }),
    onSuccess: () => { invalidate(); setTimeOpen(false); setTimeMemberId(''); setTimeHours(''); setTimeDate(''); setTimeDesc('') },
  })

  const mutAddExpense = useMutation({
    mutationFn: () => addExpense(id!, {
      description: expenseDesc,
      amount: parseFloat(expenseAmount),
      currency: expenseCurrency,
      category: expenseCategory,
      date: expenseDate,
    }),
    onSuccess: () => { invalidate(); setExpenseOpen(false); setExpenseDesc(''); setExpenseAmount(''); setExpenseCategory(''); setExpenseDate('') },
  })

  const mutAddMilestone = useMutation({
    mutationFn: () => addMilestone(id!, { title: milestoneTitle, dueDate: milestoneDue }),
    onSuccess: () => { invalidate(); setMilestoneOpen(false); setMilestoneTitle(''); setMilestoneDue('') },
  })

  const mutCompleteMilestone = useMutation({
    mutationFn: (milestoneId: string) => completeMilestone(id!, milestoneId),
    onSuccess: invalidate,
  })

  const mutUpdateProject = useMutation({
    mutationFn: () => updateProject(id!, {
      name: editName,
      description: editDescription || undefined,
      deadline: editDeadline || undefined,
      status: editStatus || undefined,
      budgetAmount: editBudget ? parseFloat(editBudget) : undefined,
      budgetCurrency: editBudget ? editCurrency : undefined,
    }),
    onSuccess: () => { invalidate(); setEditOpen(false) },
  })

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (isError || !project) return <div className="p-6"><p className="text-destructive">Failed to load project.</p></div>

  const p = project
  const prof = p.profitability
  const fmtCur = (n: number, cur: string) => n.toLocaleString('de-AT', { style: 'currency', currency: cur })
  const margin = prof.revenue > 0 ? ((prof.profit / prof.revenue) * 100).toFixed(1) : null

  const filteredTasks = (() => {
    const now = new Date()
    now.setHours(0, 0, 0, 0)

    const startOfWeek = (d: Date) => { const c = new Date(d); c.setDate(c.getDate() - c.getDay() + 1); c.setHours(0,0,0,0); return c }
    const endOfWeek   = (d: Date) => { const c = startOfWeek(d); c.setDate(c.getDate() + 6); c.setHours(23,59,59,999); return c }
    const startOfMonth = (d: Date) => new Date(d.getFullYear(), d.getMonth(), 1)
    const endOfMonth   = (d: Date) => new Date(d.getFullYear(), d.getMonth() + 1, 0, 23, 59, 59, 999)

    const nextWeek  = new Date(now); nextWeek.setDate(nextWeek.getDate() + 7)
    const nextMonth = new Date(now.getFullYear(), now.getMonth() + 1, 1)

    return p.tasks.filter(t => {
      if (taskMemberFilter !== 'all' && t.assignedMemberId !== taskMemberFilter) return false

      if (taskDateFilter === 'all') return true
      const due = t.dueDate ? new Date(t.dueDate) : null
      if (!due) return false

      if (taskDateFilter === 'overdue')    return due < now && t.status !== 'Done'
      if (taskDateFilter === 'this-week')  return due >= startOfWeek(now) && due <= endOfWeek(now)
      if (taskDateFilter === 'this-month') return due >= startOfMonth(now) && due <= endOfMonth(now)
      if (taskDateFilter === 'next-week')  return due >= startOfWeek(nextWeek) && due <= endOfWeek(nextWeek)
      if (taskDateFilter === 'next-month') return due >= startOfMonth(nextMonth) && due <= endOfMonth(nextMonth)
      return true
    })
  })()

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}><ArrowLeft className="h-4 w-4 mr-1" /> Back</Button>
      </div>

      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{p.name}</h1>
          <p className="text-sm text-muted-foreground mt-1">{p.clientName}</p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={projectStatusVariant[p.status] ?? 'outline'}>{p.status}</Badge>
          <Button size="sm" variant="outline" onClick={() => {
            setEditName(p.name)
            setEditDescription(p.description ?? '')
            setEditDeadline(p.deadline ? p.deadline.slice(0, 10) : '')
            setEditStatus(p.status)
            setEditBudget(p.budgetAmount.toString())
            setEditCurrency(p.currency)
            setEditOpen(true)
          }}><Pencil className="h-3.5 w-3.5 mr-1" />Edit</Button>
        </div>
      </div>

      <Tabs tabs={TABS}>
        {activeTab => (
          <>
            {activeTab === 'overview' && (
              <div className="space-y-4">
                <Card>
                  <CardHeader><CardTitle className="text-base">Project Info</CardTitle></CardHeader>
                  <CardContent className="grid grid-cols-2 gap-3 text-sm">
                    <div><span className="text-muted-foreground">Budget</span><p className="font-medium">{fmtCur(p.budgetAmount, p.currency)}</p></div>
                    <div><span className="text-muted-foreground">Start Date</span><p className="font-medium">{new Date(p.startDate).toLocaleDateString()}</p></div>
                    {p.deadline && <div><span className="text-muted-foreground">Deadline</span><p className="font-medium">{new Date(p.deadline).toLocaleDateString()}</p></div>}
                    <div><span className="text-muted-foreground">Members</span><p className="font-medium">{p.memberCount}</p></div>
                    {p.description && <div className="col-span-2"><span className="text-muted-foreground">Description</span><p className="font-medium">{p.description}</p></div>}
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader><CardTitle className="text-base">Profitability</CardTitle></CardHeader>
                  <CardContent className="grid grid-cols-2 gap-3 text-sm">
                    <div><span className="text-muted-foreground">Labor Cost</span><p className="font-medium">{fmtCur(prof.laborCost, prof.currency)}</p></div>
                    <div><span className="text-muted-foreground">Expenses</span><p className="font-medium">{fmtCur(prof.expensesTotal, prof.currency)}</p></div>
                    <div><span className="text-muted-foreground">Total Cost</span><p className="font-medium">{fmtCur(prof.totalCost, prof.currency)}</p></div>
                    <div><span className="text-muted-foreground">Revenue</span><p className="font-medium">{fmtCur(prof.revenue, prof.currency)}</p></div>
                    <div className="col-span-2">
                      <span className="text-muted-foreground">Profit</span>
                      <p className={`font-semibold text-base ${prof.profit >= 0 ? 'text-green-600' : 'text-destructive'}`}>
                        {fmtCur(prof.profit, prof.currency)}
                        {margin !== null && <span className="text-sm font-normal ml-2">({margin}% margin)</span>}
                      </p>
                    </div>
                  </CardContent>
                </Card>
              </div>
            )}

            {activeTab === 'gantt' && (
              <div className="space-y-3">
                <GanttChart project={p} />
              </div>
            )}

            {activeTab === 'burndown' && (
              <BurndownTab projectId={p.id} />
            )}

            {activeTab === 'tasks' && (
              <div className="space-y-3">
                <div className="flex items-center justify-between gap-3 flex-wrap">
                  <div className="flex items-center gap-2 flex-wrap">
                    <Select value={taskDateFilter} onChange={e => setTaskDateFilter(e.target.value as typeof taskDateFilter)} className="h-8 text-xs w-36">
                      <option value="all">All dates</option>
                      <option value="this-week">This week</option>
                      <option value="this-month">This month</option>
                      <option value="next-week">Next week</option>
                      <option value="next-month">Next month</option>
                      <option value="overdue">Overdue</option>
                    </Select>
                    <Select value={taskMemberFilter} onChange={e => setTaskMemberFilter(e.target.value)} className="h-8 text-xs w-40">
                      <option value="all">All members</option>
                      {p.members.map(m => (
                        <option key={m.id} value={m.id}>{m.name}</option>
                      ))}
                    </Select>
                    {(taskDateFilter !== 'all' || taskMemberFilter !== 'all') && (
                      <button
                        className="text-xs text-muted-foreground hover:text-foreground underline"
                        onClick={() => { setTaskDateFilter('all'); setTaskMemberFilter('all') }}
                      >
                        Clear
                      </button>
                    )}
                  </div>
                  <Button size="sm" onClick={() => { setTaskOpen(true); setDescExpanded(false) }}><Plus className="h-3.5 w-3.5 mr-1" /> Add Task</Button>
                </div>
                {p.tasks.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No tasks yet.</p>
                ) : (
                  <div className="grid grid-cols-4 gap-4">
                    {KANBAN_COLUMNS.map(col => (
                      <div
                        key={col.status}
                        className={`rounded-lg border-2 border-dashed p-3 min-h-48 transition-colors ${dragOverColumn === col.status ? 'border-primary bg-primary/5' : 'border-muted'}`}
                        onDragOver={e => { e.preventDefault(); setDragOverColumn(col.status) }}
                        onDragLeave={e => {
                          if (!e.currentTarget.contains(e.relatedTarget as Node)) setDragOverColumn(null)
                        }}
                        onDrop={e => {
                          e.preventDefault()
                          if (draggedTaskId) {
                            const task = p.tasks.find(t => t.id === draggedTaskId)
                            if (task && task.status !== col.status)
                              mutTaskStatus.mutate({ taskId: draggedTaskId, status: col.status })
                          }
                          setDraggedTaskId(null)
                          setDragOverColumn(null)
                        }}
                      >
                        <div className="flex items-center gap-2 mb-3">
                          <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{col.label}</span>
                          <span className="text-xs bg-muted rounded-full px-1.5 py-0.5 font-medium">
                            {filteredTasks.filter(t => t.status === col.status).length}
                          </span>
                        </div>
                        <div className="space-y-2">
                          {filteredTasks.filter(t => t.status === col.status).map(task => (
                            <div
                              key={task.id}
                              draggable
                              onDragStart={() => setDraggedTaskId(task.id)}
                              onDragEnd={() => { setDraggedTaskId(null); setDragOverColumn(null) }}
                              onClick={() => { if (!draggedTaskId) openEditTask(task) }}
                              className={`bg-card border rounded-md p-3 cursor-pointer shadow-sm transition-opacity select-none ${draggedTaskId === task.id ? 'opacity-40 cursor-grabbing' : 'hover:border-primary/50'}`}
                            >
                              <p className="font-medium text-sm leading-snug">{task.title}</p>
                              {task.description && <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{task.description}</p>}
                              <div className="flex items-center flex-wrap gap-2.5 mt-2 text-xs text-muted-foreground">
                                {task.estimatedHours && (
                                  <span className="flex items-center gap-1">
                                    <Clock className="h-3 w-3 shrink-0" />{task.estimatedHours}h
                                  </span>
                                )}
                                {task.dueDate && (
                                  <span className="flex items-center gap-1">
                                    <CalendarDays className="h-3 w-3 shrink-0" />
                                    {new Date(task.dueDate).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' })}
                                  </span>
                                )}
                                {task.comments.length > 0 && (
                                  <span className="flex items-center gap-1">
                                    <MessageSquare className="h-3 w-3 shrink-0" />{task.comments.length}
                                  </span>
                                )}
                              </div>
                              {task.assignedMemberName && (
                                <p className="text-xs text-muted-foreground mt-2 flex items-center gap-1.5">
                                  <span className="inline-flex items-center justify-center w-4 h-4 rounded-full bg-primary/15 text-primary font-semibold text-[10px] shrink-0">
                                    {task.assignedMemberName.charAt(0).toUpperCase()}
                                  </span>
                                  {task.assignedMemberName}
                                </p>
                              )}
                            </div>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {activeTab === 'team' && (
              <div className="space-y-3">
                <div className="flex justify-end">
                  <Button size="sm" onClick={() => setMemberOpen(true)}><UserPlus className="h-3.5 w-3.5 mr-1" /> Add Member</Button>
                </div>
                {p.members.length === 0 && <p className="text-sm text-muted-foreground">No team members yet.</p>}
                {p.members.map(member => (
                  <Card key={member.id}>
                    <CardContent className="py-3 flex items-center justify-between">
                      <div>
                        <p className="font-medium text-sm">{member.name}</p>
                        <p className="text-xs text-muted-foreground">{member.role}</p>
                      </div>
                      <p className="text-sm font-medium">
                        {fmtCur(member.hourlyRate, member.currency)}/h
                      </p>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}

            {activeTab === 'timelog' && (
              <div className="space-y-3">
                <div className="flex justify-end">
                  <Button size="sm" onClick={() => setTimeOpen(true)}><Clock className="h-3.5 w-3.5 mr-1" /> Log Time</Button>
                </div>
                {p.timeEntries.length === 0 && <p className="text-sm text-muted-foreground">No time logged yet.</p>}
                {p.timeEntries.map(entry => (
                  <Card key={entry.id}>
                    <CardContent className="py-3 flex items-center justify-between">
                      <div>
                        <p className="font-medium text-sm">{entry.memberName}</p>
                        {entry.description && <p className="text-xs text-muted-foreground">{entry.description}</p>}
                        <p className="text-xs text-muted-foreground">{new Date(entry.date).toLocaleDateString()}</p>
                      </div>
                      <div className="text-right">
                        <p className="text-sm font-medium">{entry.hoursWorked}h</p>
                        <p className="text-xs text-muted-foreground">{fmtCur(entry.cost, entry.currency)}</p>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}

            {activeTab === 'expenses' && (
              <div className="space-y-3">
                <div className="flex justify-end">
                  <Button size="sm" onClick={() => setExpenseOpen(true)}><Receipt className="h-3.5 w-3.5 mr-1" /> Add Expense</Button>
                </div>
                {p.expenses.length === 0 && <p className="text-sm text-muted-foreground">No expenses yet.</p>}
                {p.expenses.map(expense => (
                  <Card key={expense.id}>
                    <CardContent className="py-3 flex items-center justify-between">
                      <div>
                        <p className="font-medium text-sm">{expense.description}</p>
                        <p className="text-xs text-muted-foreground">{expense.category} · {new Date(expense.date).toLocaleDateString()}</p>
                      </div>
                      <p className="text-sm font-medium">{fmtCur(expense.amount, expense.currency)}</p>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}

            {activeTab === 'milestones' && (
              <div className="space-y-3">
                <div className="flex justify-end">
                  <Button size="sm" onClick={() => setMilestoneOpen(true)}><Flag className="h-3.5 w-3.5 mr-1" /> Add Milestone</Button>
                </div>
                {p.milestones.length === 0 && <p className="text-sm text-muted-foreground">No milestones yet.</p>}
                {p.milestones.map(milestone => (
                  <Card key={milestone.id}>
                    <CardContent className="py-3 flex items-center justify-between">
                      <div>
                        <p className={`font-medium text-sm ${milestone.isCompleted ? 'line-through text-muted-foreground' : ''}`}>
                          {milestone.title}
                        </p>
                        <p className="text-xs text-muted-foreground">Due {new Date(milestone.dueDate).toLocaleDateString()}</p>
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant={milestone.isCompleted ? 'success' : 'secondary'}>
                          {milestone.isCompleted ? 'Completed' : 'Pending'}
                        </Badge>
                        {!milestone.isCompleted && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => mutCompleteMilestone.mutate(milestone.id)}
                            disabled={mutCompleteMilestone.isPending}
                          >
                            <CheckCircle2 className="h-3.5 w-3.5 mr-1" />Mark Complete
                          </Button>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </>
        )}
      </Tabs>

      {/* Azure DevOps–style task detail modal */}
      {editTaskOpen && editingTask && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="fixed inset-0 bg-black/50" onClick={() => { setEditTaskOpen(false); setEditingTask(null); setCommentBody(''); setEditingCommentId(null) }} />
          <div
            className="relative z-50 bg-background w-full max-w-5xl flex flex-col rounded-lg shadow-2xl overflow-hidden border-l-4"
            style={{
              height: 'min(85vh, 800px)',
              borderLeftColor: TASK_STATUS_BORDER_COLOR[editTaskStatus] ?? '#3b82f6',
            }}
          >
            {/* Header */}
            <div className="flex items-center justify-between px-5 py-3 border-b bg-muted/30 shrink-0">
              <div className="flex items-center gap-3 min-w-0">
                <span className="inline-flex items-center gap-1.5 text-xs font-medium text-blue-600 bg-blue-100 dark:bg-blue-950 dark:text-blue-400 px-2 py-1 rounded shrink-0">
                  <SquareCheck className="h-3.5 w-3.5" /> Task
                </span>
                <span className="text-xs text-muted-foreground truncate">{p.name}</span>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <Button size="sm" onClick={() => mutUpdateTask.mutate()} disabled={mutUpdateTask.isPending || !editTaskTitle.trim()}>
                  {mutUpdateTask.isPending ? 'Saving…' : 'Save'}
                </Button>
                <button
                  className="text-muted-foreground hover:text-foreground text-xl leading-none w-7 h-7 flex items-center justify-center rounded hover:bg-muted"
                  onClick={() => { setEditTaskOpen(false); setEditingTask(null); setCommentBody(''); setEditingCommentId(null) }}
                >
                  ×
                </button>
              </div>
            </div>

            {/* Body */}
            <div className="flex flex-1 overflow-hidden">
              {/* Left: main content */}
              <div className={`flex-1 flex flex-col min-w-0 p-6 ${descExpanded ? 'overflow-hidden gap-3' : 'overflow-y-auto gap-6'}`}>
                {/* Title */}
                <input
                  value={editTaskTitle}
                  onChange={e => setEditTaskTitle(e.target.value)}
                  placeholder="Task title"
                  className="w-full text-xl font-semibold bg-transparent border-0 border-b border-transparent hover:border-border focus:border-primary focus:outline-none py-1 transition-colors placeholder:text-muted-foreground/50 shrink-0"
                />

                {/* Description */}
                <div className={`flex flex-col gap-2 ${descExpanded ? 'flex-1 min-h-0' : ''}`}>
                  <div className="flex items-center justify-between shrink-0">
                    <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Description</p>
                    <button
                      onClick={() => setDescExpanded(v => !v)}
                      title={descExpanded ? 'Collapse description' : 'Expand description'}
                      className="text-muted-foreground hover:text-foreground p-1 rounded hover:bg-muted transition-colors"
                    >
                      {descExpanded ? <Minimize2 className="h-3.5 w-3.5" /> : <Maximize2 className="h-3.5 w-3.5" />}
                    </button>
                  </div>
                  <Textarea
                    value={editTaskDesc}
                    onChange={e => setEditTaskDesc(e.target.value)}
                    placeholder="Add a description…"
                    className={descExpanded ? 'flex-1 min-h-0 resize-none' : 'resize-y min-h-[180px]'}
                  />
                </div>

                {/* Discussion — hidden when description is expanded */}
                {!descExpanded && <div className="space-y-4">
                  <div className="flex items-center gap-2 border-b pb-2">
                    <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Discussion</p>
                    {(p.tasks.find(t => t.id === editingTask.id)?.comments.length ?? 0) > 0 && (
                      <span className="text-xs text-muted-foreground">
                        ({p.tasks.find(t => t.id === editingTask.id)?.comments.length})
                      </span>
                    )}
                  </div>

                  {/* New comment input */}
                  <div className="flex gap-3 items-start">
                    <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-primary/15 text-primary font-semibold text-xs shrink-0 mt-0.5">
                      {authUser?.fullName?.charAt(0).toUpperCase() ?? '?'}
                    </span>
                    <div className="flex-1 space-y-2">
                      <Textarea
                        value={commentBody}
                        onChange={e => setCommentBody(e.target.value)}
                        placeholder="Add a comment… (Ctrl+Enter to post)"
                        rows={2}
                        className="resize-none"
                        onKeyDown={e => {
                          if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && commentBody.trim()) {
                            e.preventDefault()
                            mutAddComment.mutate()
                          }
                        }}
                      />
                      {commentBody.trim() && (
                        <div className="flex justify-end">
                          <Button size="sm" onClick={() => mutAddComment.mutate()} disabled={mutAddComment.isPending}>
                            <Send className="h-3.5 w-3.5 mr-1.5" />
                            {mutAddComment.isPending ? 'Posting…' : 'Comment'}
                          </Button>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Comment list */}
                  <div className="space-y-5">
                    {(p.tasks.find(t => t.id === editingTask.id)?.comments ?? []).length === 0 && (
                      <p className="text-sm text-muted-foreground">No comments yet. Be the first to add one.</p>
                    )}
                    {(p.tasks.find(t => t.id === editingTask.id)?.comments ?? []).map(comment => (
                      <div key={comment.id} className="flex gap-3 group">
                        <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-muted font-semibold text-xs shrink-0 mt-0.5">
                          {comment.authorName.charAt(0).toUpperCase()}
                        </span>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-medium">{comment.authorName}</span>
                            <span className="text-xs text-muted-foreground">
                              {timeAgo(comment.createdAt)}{comment.updatedAt ? ' · edited' : ''}
                            </span>
                            <div className="ml-auto flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                              {comment.authorId === authUser?.userId && editingCommentId !== comment.id && (
                                <button
                                  className="text-muted-foreground hover:text-foreground p-1 rounded hover:bg-muted"
                                  onClick={() => { setEditingCommentId(comment.id); setEditingCommentBody(comment.body) }}
                                >
                                  <Pencil className="h-3 w-3" />
                                </button>
                              )}
                              {(comment.authorId === authUser?.userId || authUser?.role === 'Owner' || authUser?.role === 'Admin') && editingCommentId !== comment.id && (
                                <button
                                  className="text-muted-foreground hover:text-destructive p-1 rounded hover:bg-muted"
                                  onClick={() => mutDeleteComment.mutate({ commentId: comment.id })}
                                  disabled={mutDeleteComment.isPending}
                                >
                                  <Trash2 className="h-3 w-3" />
                                </button>
                              )}
                            </div>
                          </div>
                          {editingCommentId === comment.id ? (
                            <div className="mt-1.5 space-y-2">
                              <Textarea
                                value={editingCommentBody}
                                onChange={e => setEditingCommentBody(e.target.value)}
                                rows={2}
                                className="resize-none"
                              />
                              <div className="flex gap-2">
                                <Button size="sm" onClick={() => mutEditComment.mutate({ commentId: comment.id })} disabled={mutEditComment.isPending || !editingCommentBody.trim()}>
                                  {mutEditComment.isPending ? 'Saving…' : 'Save'}
                                </Button>
                                <Button size="sm" variant="outline" onClick={() => { setEditingCommentId(null); setEditingCommentBody('') }}>Cancel</Button>
                              </div>
                            </div>
                          ) : (
                            <p className="text-sm mt-1 whitespace-pre-wrap break-words text-foreground/90">{comment.body}</p>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>}
              </div>

              {/* Right: details sidebar */}
              <div className="w-64 shrink-0 border-l overflow-y-auto p-5 space-y-5 bg-muted/10">
                <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Details</p>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">State</Label>
                  <Select value={editTaskStatus} onChange={e => setEditTaskStatus(e.target.value)}>
                    <option value="Todo">To Do</option>
                    <option value="InProgress">In Progress</option>
                    <option value="InReview">In Review</option>
                    <option value="Done">Done</option>
                  </Select>
                </div>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Assigned To</Label>
                  <Select value={editTaskAssignedMemberId} onChange={e => setEditTaskAssignedMemberId(e.target.value)}>
                    <option value="">Unassigned</option>
                    {p.members.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
                  </Select>
                </div>

                {p.milestones.length > 0 && (
                  <div className="space-y-1.5">
                    <Label className="text-xs text-muted-foreground font-normal">Milestone</Label>
                    <Select value={editTaskMilestoneId} onChange={e => setEditTaskMilestoneId(e.target.value)}>
                      <option value="">None</option>
                      {p.milestones.map(m => <option key={m.id} value={m.id}>{m.title}</option>)}
                    </Select>
                  </div>
                )}

                <div className="w-full h-px bg-border" />

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Estimated Hours</Label>
                  <Input type="number" step="0.5" min="0" value={editTaskHours} onChange={e => setEditTaskHours(e.target.value)} placeholder="—" />
                </div>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Start Date</Label>
                  <Input type="date" value={editTaskStart} onChange={e => setEditTaskStart(e.target.value)} />
                </div>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Due Date</Label>
                  <Input type="date" value={editTaskDue} onChange={e => setEditTaskDue(e.target.value)} />
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Azure DevOps–style new task modal */}
      {taskOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="fixed inset-0 bg-black/50" onClick={() => setTaskOpen(false)} />
          <div
            className="relative z-50 bg-background w-full max-w-5xl flex flex-col rounded-lg shadow-2xl overflow-hidden border-l-4 border-l-slate-400"
            style={{ height: 'min(75vh, 700px)' }}
          >
            {/* Header */}
            <div className="flex items-center justify-between px-5 py-3 border-b bg-muted/30 shrink-0">
              <div className="flex items-center gap-3 min-w-0">
                <span className="inline-flex items-center gap-1.5 text-xs font-medium text-blue-600 bg-blue-100 dark:bg-blue-950 dark:text-blue-400 px-2 py-1 rounded shrink-0">
                  <SquareCheck className="h-3.5 w-3.5" /> New Task
                </span>
                <span className="text-xs text-muted-foreground truncate">{p.name}</span>
              </div>
              <div className="flex items-center gap-2 shrink-0">
                <Button size="sm" onClick={() => mutAddTask.mutate()} disabled={mutAddTask.isPending || !taskTitle.trim()}>
                  {mutAddTask.isPending ? 'Creating…' : 'Create Task'}
                </Button>
                <button
                  className="text-muted-foreground hover:text-foreground text-xl leading-none w-7 h-7 flex items-center justify-center rounded hover:bg-muted"
                  onClick={() => setTaskOpen(false)}
                >
                  ×
                </button>
              </div>
            </div>

            {/* Body */}
            <div className="flex flex-1 overflow-hidden">
              {/* Left: main content */}
              <div className={`flex-1 flex flex-col min-w-0 p-6 ${descExpanded ? 'overflow-hidden gap-3' : 'overflow-y-auto gap-6'}`}>
                <input
                  value={taskTitle}
                  onChange={e => setTaskTitle(e.target.value)}
                  placeholder="Task title"
                  autoFocus
                  className="w-full text-xl font-semibold bg-transparent border-0 border-b border-transparent hover:border-border focus:border-primary focus:outline-none py-1 transition-colors placeholder:text-muted-foreground/50 shrink-0"
                />

                <div className={`flex flex-col gap-2 ${descExpanded ? 'flex-1 min-h-0' : ''}`}>
                  <div className="flex items-center justify-between shrink-0">
                    <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Description</p>
                    <button
                      onClick={() => setDescExpanded(v => !v)}
                      title={descExpanded ? 'Collapse description' : 'Expand description'}
                      className="text-muted-foreground hover:text-foreground p-1 rounded hover:bg-muted transition-colors"
                    >
                      {descExpanded ? <Minimize2 className="h-3.5 w-3.5" /> : <Maximize2 className="h-3.5 w-3.5" />}
                    </button>
                  </div>
                  <Textarea
                    value={taskDesc}
                    onChange={e => setTaskDesc(e.target.value)}
                    placeholder="Add a description…"
                    className={descExpanded ? 'flex-1 min-h-0 resize-none' : 'resize-y min-h-[180px]'}
                  />
                </div>
              </div>

              {/* Right: details sidebar */}
              <div className="w-64 shrink-0 border-l overflow-y-auto p-5 space-y-5 bg-muted/10">
                <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Details</p>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Assigned To</Label>
                  <Select value={taskAssignedMemberId} onChange={e => setTaskAssignedMemberId(e.target.value)}>
                    <option value="">Unassigned</option>
                    {p.members.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
                  </Select>
                </div>

                {p.milestones.length > 0 && (
                  <div className="space-y-1.5">
                    <Label className="text-xs text-muted-foreground font-normal">Milestone</Label>
                    <Select value={taskMilestoneId} onChange={e => setTaskMilestoneId(e.target.value)}>
                      <option value="">None</option>
                      {p.milestones.map(m => <option key={m.id} value={m.id}>{m.title}</option>)}
                    </Select>
                  </div>
                )}

                <div className="w-full h-px bg-border" />

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Estimated Hours</Label>
                  <Input type="number" step="0.5" min="0" value={taskHours} onChange={e => setTaskHours(e.target.value)} placeholder="—" />
                </div>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Start Date</Label>
                  <Input type="date" value={taskStart} onChange={e => setTaskStart(e.target.value)} />
                </div>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Due Date</Label>
                  <Input type="date" value={taskDue} onChange={e => setTaskDue(e.target.value)} />
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      <Dialog open={memberOpen} onClose={() => setMemberOpen(false)} title="Add Team Member">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>User</Label>
            <Select value={memberUserId} onChange={e => {
              const uid = e.target.value
              setMemberUserId(uid)
              const found = users?.find(u => u.id === uid)
              if (found) setMemberName(found.fullName)
            }}>
              <option value="">Select a user…</option>
              {users?.map(u => (
                <option key={u.id} value={u.id}>{u.fullName}</option>
              ))}
            </Select>
          </div>
          <div className="space-y-1">
            <Label>Name</Label>
            <Input value={memberName} onChange={e => setMemberName(e.target.value)} placeholder="Display name" />
          </div>
          <div className="space-y-1">
            <Label>Role</Label>
            <Input value={memberRole} onChange={e => setMemberRole(e.target.value)} placeholder="e.g. Developer" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Hourly Rate</Label>
              <Input type="number" step="0.01" value={memberRate} onChange={e => setMemberRate(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Currency</Label>
              <Select value={memberCurrency} onChange={e => setMemberCurrency(e.target.value)}>
                <option value="EUR">EUR</option>
                <option value="USD">USD</option>
                <option value="GBP">GBP</option>
                <option value="CHF">CHF</option>
              </Select>
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setMemberOpen(false)}>Cancel</Button>
            <Button onClick={() => mutAddMember.mutate()} disabled={mutAddMember.isPending || !memberUserId || !memberName.trim() || !memberRole.trim() || !memberRate}>
              {mutAddMember.isPending ? 'Adding…' : 'Add Member'}
            </Button>
          </div>
        </div>
      </Dialog>

      <Dialog open={timeOpen} onClose={() => setTimeOpen(false)} title="Log Time">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Team Member</Label>
            <Select value={timeMemberId} onChange={e => setTimeMemberId(e.target.value)}>
              <option value="">Select a member…</option>
              {p.members.map(m => (
                <option key={m.id} value={m.id}>{m.name}</option>
              ))}
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Hours Worked</Label>
              <Input type="number" step="0.25" value={timeHours} onChange={e => setTimeHours(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Date</Label>
              <Input type="date" value={timeDate} onChange={e => setTimeDate(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1">
            <Label>Description (optional)</Label>
            <Textarea value={timeDesc} onChange={e => setTimeDesc(e.target.value)} placeholder="What did you work on?" />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setTimeOpen(false)}>Cancel</Button>
            <Button onClick={() => mutLogTime.mutate()} disabled={mutLogTime.isPending || !timeMemberId || !timeHours || !timeDate}>
              {mutLogTime.isPending ? 'Logging…' : 'Log Time'}
            </Button>
          </div>
        </div>
      </Dialog>

      <Dialog open={expenseOpen} onClose={() => setExpenseOpen(false)} title="Add Expense">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Description</Label>
            <Input value={expenseDesc} onChange={e => setExpenseDesc(e.target.value)} placeholder="Expense description" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Amount</Label>
              <Input type="number" step="0.01" value={expenseAmount} onChange={e => setExpenseAmount(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Currency</Label>
              <Select value={expenseCurrency} onChange={e => setExpenseCurrency(e.target.value)}>
                <option value="EUR">EUR</option>
                <option value="USD">USD</option>
                <option value="GBP">GBP</option>
                <option value="CHF">CHF</option>
              </Select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Category</Label>
              <Select value={expenseCategory} onChange={e => setExpenseCategory(e.target.value)}>
                <option value="">Select category…</option>
                <option value="Materials">Materials</option>
                <option value="Equipment">Equipment</option>
                <option value="Subcontractors">Subcontractors</option>
                <option value="Travel">Travel</option>
                <option value="Other">Other</option>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>Date</Label>
              <Input type="date" value={expenseDate} onChange={e => setExpenseDate(e.target.value)} />
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setExpenseOpen(false)}>Cancel</Button>
            <Button onClick={() => mutAddExpense.mutate()} disabled={mutAddExpense.isPending || !expenseDesc.trim() || !expenseAmount || !expenseCategory || !expenseDate}>
              {mutAddExpense.isPending ? 'Adding…' : 'Add Expense'}
            </Button>
          </div>
        </div>
      </Dialog>

      <Dialog open={editOpen} onClose={() => setEditOpen(false)} title="Edit Project">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Name</Label>
            <Input value={editName} onChange={e => setEditName(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Description (optional)</Label>
            <Textarea value={editDescription} onChange={e => setEditDescription(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Deadline (optional)</Label>
            <Input type="date" value={editDeadline} onChange={e => setEditDeadline(e.target.value)} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Budget</Label>
              <Input type="number" step="0.01" value={editBudget} onChange={e => setEditBudget(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Currency</Label>
              <Select value={editCurrency} onChange={e => setEditCurrency(e.target.value)}>
                <option value="EUR">EUR</option>
                <option value="USD">USD</option>
                <option value="GBP">GBP</option>
                <option value="CHF">CHF</option>
              </Select>
            </div>
          </div>
          <div className="space-y-1">
            <Label>Status</Label>
            <Select value={editStatus} onChange={e => setEditStatus(e.target.value)}>
              <option value="Planning">Planning</option>
              <option value="Active">Active</option>
              <option value="OnHold">On Hold</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </Select>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setEditOpen(false)}>Cancel</Button>
            <Button onClick={() => mutUpdateProject.mutate()} disabled={mutUpdateProject.isPending || !editName.trim()}>
              {mutUpdateProject.isPending ? 'Saving…' : 'Save'}
            </Button>
          </div>
        </div>
      </Dialog>

      <Dialog open={milestoneOpen} onClose={() => setMilestoneOpen(false)} title="Add Milestone">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Title</Label>
            <Input value={milestoneTitle} onChange={e => setMilestoneTitle(e.target.value)} placeholder="Milestone title" />
          </div>
          <div className="space-y-1">
            <Label>Due Date</Label>
            <Input type="date" value={milestoneDue} onChange={e => setMilestoneDue(e.target.value)} />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setMilestoneOpen(false)}>Cancel</Button>
            <Button onClick={() => mutAddMilestone.mutate()} disabled={mutAddMilestone.isPending || !milestoneTitle.trim() || !milestoneDue}>
              {mutAddMilestone.isPending ? 'Adding…' : 'Add Milestone'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
