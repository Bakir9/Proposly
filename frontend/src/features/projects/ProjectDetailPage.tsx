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
  addTaskDependency,
  removeTaskDependency,
} from '@/api/projects'
import {
  LineChart, Line, BarChart, Bar, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
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
import { ArrowLeft, Pencil, Plus, UserPlus, Clock, Receipt, Flag, CheckCircle2, MessageSquare, Send, Trash2, CalendarDays, SquareCheck, Maximize2, Minimize2, Lock, X, TrendingUp, Search } from 'lucide-react'
import { RichTextEditor } from '@/components/ui/rich-text-editor'
import { NotesTab } from './NotesTab'
import { VelocityCapacityTab } from './VelocityCapacityTab'
import { GanttChart } from './GanttChart'
import { stripHtml, hasRichContent } from '@/lib/utils'

const KANBAN_COLUMNS = [
  { status: 'Todo', label: 'Not Started' },
  { status: 'InProgress', label: 'In Progress' },
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
  { id: 'velocity', label: 'Velocity' },
  { id: 'team', label: 'Team' },
  { id: 'timelog', label: 'Time Log' },
  { id: 'expenses', label: 'Expenses' },
  { id: 'milestones', label: 'Milestones' },
  { id: 'notes', label: 'Notes' },
]

const TASK_STATUS_COLOR: Record<string, string> = {
  Todo: 'bg-slate-400',
  InProgress: 'bg-blue-500',
  Done: 'bg-green-500',
}

const TASK_STATUS_BORDER_COLOR: Record<string, string> = {
  Todo: '#94a3b8',
  InProgress: '#3b82f6',
  Done: '#22c55e',
}

const CATEGORY_COLOR: Record<string, string> = {
  Materials:      'bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-400',
  Equipment:      'bg-purple-100 text-purple-700 dark:bg-purple-950 dark:text-purple-400',
  Subcontractors: 'bg-orange-100 text-orange-700 dark:bg-orange-950 dark:text-orange-400',
  Travel:         'bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-400',
  Other:          'bg-muted text-muted-foreground',
}

const MEMBER_COLORS = ['#3b82f6', '#22c55e', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4', '#ec4899', '#f97316']

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
                formatter={(value, name) => [value, name === 'actual' ? 'Actual remaining' : 'Ideal remaining']}
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
  const [editTaskActualHours, setEditActualTaskHours] = useState('')
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

  // Time log filters
  const [timeSearch, setTimeSearch] = useState('')
  const [timeDateFilter, setTimeDateFilter] = useState<'all' | '7d' | '30d'>('all')
  const [timeListMemberFilter, setTimeListMemberFilter] = useState('all')
  const [timePage, setTimePage] = useState(1)

  // Expense dialog
  const [expenseOpen, setExpenseOpen] = useState(false)
  const [expenseDesc, setExpenseDesc] = useState('')
  const [expenseAmount, setExpenseAmount] = useState('')
  const [expenseCurrency, setExpenseCurrency] = useState('EUR')
  const [expenseCategory, setExpenseCategory] = useState('')
  const [expenseDate, setExpenseDate] = useState('')

  // Expense list filters
  const [expenseSearch, setExpenseSearch] = useState('')
  const [expenseListCategoryFilter, setExpenseListCategoryFilter] = useState('all')

  // Team search
  const [teamSearch, setTeamSearch] = useState('')

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

  // Dependency state (within edit task dialog)
  const [addingDependency, setAddingDependency] = useState(false)
  const [pendingBlockerId, setPendingBlockerId] = useState('')

  // Kanban drag state
  const [draggedTaskId, setDraggedTaskId] = useState<string | null>(null)
  const [dragOverColumn, setDragOverColumn] = useState<string | null>(null)

  // Actual hours prompt (shown when dragging to Done)
  const [donePromptTaskId, setDonePromptTaskId] = useState<string | null>(null)
  const [donePromptHours, setDonePromptHours] = useState('')

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
    mutationFn: ({ taskId, status, actualHours }: { taskId: string; status: string; actualHours?: number }) =>
      updateTaskStatus(id!, taskId, status, actualHours),
    onSuccess: invalidate,
  })

  const openEditTask = (task: ProjectTask) => {
    setEditingTask(task)
    setEditTaskTitle(task.title)
    setEditTaskDesc(task.description ?? '')
    setEditTaskHours(task.estimatedHours?.toString() ?? '')
    setEditActualTaskHours(task.actualHours?.toString() ?? '')
    setEditTaskStart(task.startDate ? task.startDate.slice(0, 10) : '')
    setEditTaskDue(task.dueDate ? task.dueDate.slice(0, 10) : '')
    setEditTaskMilestoneId(task.milestoneId ?? '')
    setEditTaskAssignedMemberId(task.assignedMemberId ?? '')
    setEditTaskStatus(task.status)
    setCommentBody('')
    setEditingCommentId(null)
    setEditingCommentBody('')
    setAddingDependency(false)
    setPendingBlockerId('')
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
        const actualHours = editTaskActualHours ? parseFloat(editTaskActualHours) : undefined
        await updateTaskStatus(id!, editingTask!.id, editTaskStatus, actualHours)
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

  const mutAddDependency = useMutation({
    mutationFn: ({ blockingTaskId }: { blockingTaskId: string }) =>
      addTaskDependency(id!, editingTask!.id, blockingTaskId),
    onSuccess: () => { invalidate(); setAddingDependency(false); setPendingBlockerId('') },
  })

  const mutRemoveDependency = useMutation({
    mutationFn: ({ blockingTaskId }: { blockingTaskId: string }) =>
      removeTaskDependency(id!, editingTask!.id, blockingTaskId),
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

  const mutGanttDrag = useMutation({
    mutationFn: ({ taskId, startDate, dueDate }: { taskId: string; startDate: string; dueDate: string }) => {
      const task = p?.tasks.find(t => t.id === taskId)
      if (!task) throw new Error('task not found')
      return updateTask(id!, taskId, {
        title: task.title,
        description: task.description || undefined,
        estimatedHours: task.estimatedHours ?? undefined,
        startDate,
        dueDate,
        milestoneId: task.milestoneId || undefined,
        assignedMemberId: task.assignedMemberId || undefined,
      })
    },
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

  const roleChartData = (() => {
    const roleMap: Record<string, number> = {}
    p.timeEntries.forEach(entry => {
      const role = p.members.find(m => m.id === entry.memberId)?.role ?? 'Other'
      roleMap[role] = (roleMap[role] ?? 0) + entry.hoursWorked
    })
    return Object.entries(roleMap).map(([role, hours]) => ({ role, hours }))
  })()

  const activityItems = (() => {
    const items: { type: 'time' | 'milestone' | 'overdue'; bold: string; label: string; time: string; sortMs: number }[] = []

    ;[...p.timeEntries]
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
      .slice(0, 3)
      .forEach(entry => {
        items.push({
          type: 'time',
          bold: entry.memberName,
          label: `logged ${entry.hoursWorked}h${entry.description ? ` — ${entry.description}` : ''}`,
          time: timeAgo(entry.date),
          sortMs: new Date(entry.date).getTime(),
        })
      })

    p.milestones.filter(m => m.isCompleted).forEach(m => {
      items.push({
        type: 'milestone',
        bold: m.title,
        label: 'milestone reached',
        time: new Date(m.dueDate).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' }),
        sortMs: new Date(m.dueDate).getTime(),
      })
    })

    const today = new Date(); today.setHours(0, 0, 0, 0)
    p.tasks
      .filter(t => t.dueDate && new Date(t.dueDate) < today && t.status !== 'Done')
      .forEach(t => {
        items.push({
          type: 'overdue',
          bold: t.title,
          label: 'is overdue',
          time: 'Overdue',
          sortMs: t.dueDate ? new Date(t.dueDate).getTime() : 0,
        })
      })

    return items.sort((a, b) => b.sortMs - a.sortMs).slice(0, 6)
  })()

  const totalLoggedHours = p.timeEntries.reduce((s, e) => s + e.hoursWorked, 0)
  const totalLoggedValue = p.timeEntries.reduce((s, e) => s + e.cost, 0)
  const budgetUtilPct = p.budgetAmount > 0 ? Math.min(100, (prof.totalCost / p.budgetAmount) * 100) : 0

  const filteredTimeEntries = p.timeEntries
    .filter(e => {
      if (timeListMemberFilter !== 'all' && e.memberId !== timeListMemberFilter) return false
      if (timeSearch && !e.memberName.toLowerCase().includes(timeSearch.toLowerCase()) && !e.description?.toLowerCase().includes(timeSearch.toLowerCase())) return false
      if (timeDateFilter !== 'all') {
        const days = timeDateFilter === '7d' ? 7 : 30
        const cutoff = new Date(); cutoff.setDate(cutoff.getDate() - days)
        if (new Date(e.date) < cutoff) return false
      }
      return true
    })
    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())

  const TIME_PER_PAGE = 10
  const totalTimePages = Math.ceil(filteredTimeEntries.length / TIME_PER_PAGE)
  const pagedTimeEntries = filteredTimeEntries.slice((timePage - 1) * TIME_PER_PAGE, timePage * TIME_PER_PAGE)

  const weeklyChartData = (() => {
    const result: { label: string; hours: number }[] = []
    for (let i = 6; i >= 0; i--) {
      const d = new Date(); d.setDate(d.getDate() - i); d.setHours(0, 0, 0, 0)
      const dateStr = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
      const hours = p.timeEntries.filter(e => e.date.slice(0, 10) === dateStr).reduce((s, e) => s + e.hoursWorked, 0)
      result.push({ label: d.toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' }), hours })
    }
    return result
  })()

  const totalExpenses = prof.expensesTotal
  const remainingBudget = p.budgetAmount - totalExpenses
  const budgetExpensePct = p.budgetAmount > 0 ? Math.min(100, (totalExpenses / p.budgetAmount) * 100) : 0
  const uniqueExpenseCategories = [...new Set(p.expenses.map(e => e.category))]
  const filteredExpenses = p.expenses
    .filter(e => {
      if (expenseListCategoryFilter !== 'all' && e.category !== expenseListCategoryFilter) return false
      if (expenseSearch && !e.description.toLowerCase().includes(expenseSearch.toLowerCase())) return false
      return true
    })
    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
  const spendingByCategoryData = (() => {
    const map: Record<string, number> = {}
    p.expenses.forEach(e => { map[e.category] = (map[e.category] ?? 0) + e.amount })
    return Object.entries(map).map(([category, amount]) => ({ category, amount })).sort((a, b) => b.amount - a.amount)
  })()
  const expenseTimelineData = (() => {
    const map: Record<string, number> = {}
    p.expenses.forEach(e => {
      const month = new Date(e.date).toLocaleDateString('de-AT', { month: 'short', year: '2-digit' })
      map[month] = (map[month] ?? 0) + e.amount
    })
    return Object.entries(map).map(([month, amount]) => ({ month, amount }))
  })()

  const activeMilestones = p.milestones
    .filter(m => !m.isCompleted)
    .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())
  const completedMilestones = p.milestones.filter(m => m.isCompleted)
  const milestoneCompletionPct = p.milestones.length > 0 ? (completedMilestones.length / p.milestones.length) * 100 : 0
  const nextMilestoneDays = activeMilestones.length > 0
    ? Math.ceil((new Date(activeMilestones[0].dueDate).getTime() - Date.now()) / 86_400_000)
    : null

  const doneTasks = p.tasks.filter(t => t.status === 'Done')
  const overdueTasks = p.tasks.filter(t => {
    if (!t.dueDate || t.status === 'Done') return false
    const d = new Date(t.dueDate); d.setHours(0, 0, 0, 0)
    const tod = new Date(); tod.setHours(0, 0, 0, 0)
    return d < tod
  })
  const completionRate = p.tasks.length > 0 ? Math.round((doneTasks.length / p.tasks.length) * 100) : 0

  const memberHoursMap: Record<string, number> = {}
  p.timeEntries.forEach(e => { memberHoursMap[e.memberId] = (memberHoursMap[e.memberId] ?? 0) + e.hoursWorked })
  const avgRate = p.members.length > 0 ? p.members.reduce((s, m) => s + m.hourlyRate, 0) / p.members.length : 0
  const filteredMembers = p.members.filter(m =>
    !teamSearch ||
    m.name.toLowerCase().includes(teamSearch.toLowerCase()) ||
    m.role.toLowerCase().includes(teamSearch.toLowerCase())
  )
  const memberChartData = p.members.map((m, i) => ({
    name: m.name.split(' ')[0],
    hours: memberHoursMap[m.id] ?? 0,
    fill: MEMBER_COLORS[i % MEMBER_COLORS.length],
  }))
  const totalTeamCost = p.members.reduce((s, m) => s + (memberHoursMap[m.id] ?? 0) * m.hourlyRate, 0)

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
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" onClick={() => navigate(-1)} className="h-8 w-8 p-0 shrink-0">
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold">{p.name}</h1>
              <Badge variant={projectStatusVariant[p.status] ?? 'outline'}>{p.status}</Badge>
            </div>
            <p className="text-sm text-muted-foreground">{p.clientName}</p>
          </div>
        </div>
        <Button size="sm" variant="outline" onClick={() => {
          setEditName(p.name)
          setEditDescription(p.description ?? '')
          setEditDeadline(p.deadline ? p.deadline.slice(0, 10) : '')
          setEditStatus(p.status)
          setEditBudget(p.budgetAmount.toString())
          setEditCurrency(p.currency)
          setEditOpen(true)
        }}><Pencil className="h-3.5 w-3.5 mr-1" />Edit Project</Button>
      </div>

      <Tabs tabs={TABS}>
        {activeTab => (
          <>
            {activeTab === 'overview' && (
              <div className="space-y-4">
                {/* Top row */}
                <div className="grid grid-cols-2 gap-4">
                  <Card>
                    <CardHeader className="pb-3">
                      <CardTitle className="text-base">Project Info</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <p className="text-xs text-muted-foreground uppercase tracking-wide">Budget</p>
                          <p className="text-lg font-semibold mt-0.5">{fmtCur(p.budgetAmount, p.currency)}</p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground uppercase tracking-wide">Timeline</p>
                          <p className="text-sm font-medium mt-0.5">
                            {new Date(p.startDate).toLocaleDateString('de-AT', { day: 'numeric', month: 'short', year: 'numeric' })}
                            {p.deadline ? ` — ${new Date(p.deadline).toLocaleDateString('de-AT', { day: 'numeric', month: 'short', year: 'numeric' })}` : ''}
                          </p>
                        </div>
                      </div>
                      {p.description && (
                        <div className="bg-muted/40 rounded-md p-3 text-sm text-foreground/80 leading-relaxed">
                          {p.description}
                        </div>
                      )}
                      <div>
                        <p className="text-xs text-muted-foreground uppercase tracking-wide mb-2">Team Members</p>
                        <div className="flex items-center gap-1.5 flex-wrap">
                          {p.members.slice(0, 5).map(m => (
                            <span
                              key={m.id}
                              title={m.name}
                              className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-primary/20 text-primary font-semibold text-xs"
                            >
                              {m.name.charAt(0).toUpperCase()}
                            </span>
                          ))}
                          {p.members.length > 5 && (
                            <span className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-muted text-muted-foreground font-medium text-xs">
                              +{p.members.length - 5}
                            </span>
                          )}
                          {p.members.length === 0 && <span className="text-sm text-muted-foreground">No members yet</span>}
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader className="pb-3">
                      <div className="flex items-center justify-between">
                        <CardTitle className="text-base">Profitability</CardTitle>
                        <TrendingUp className="h-4 w-4 text-green-500" />
                      </div>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-0 text-sm">
                        {[
                          { label: 'Labor Cost', value: fmtCur(prof.laborCost, prof.currency), red: false },
                          { label: 'Expenses', value: fmtCur(prof.expensesTotal, prof.currency), red: false },
                          { label: 'Total Cost', value: fmtCur(prof.totalCost, prof.currency), red: true },
                          { label: 'Revenue', value: fmtCur(prof.revenue, prof.currency), red: false },
                        ].map(row => (
                          <div key={row.label} className="flex justify-between py-2 border-b border-border/40 last:border-0">
                            <span className="text-muted-foreground">{row.label}</span>
                            <span className={row.red ? 'font-medium text-destructive' : 'font-medium'}>{row.value}</span>
                          </div>
                        ))}
                      </div>
                      <div className="mt-4 rounded-lg bg-muted/30 p-4">
                        <p className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground mb-2">Net Profit</p>
                        <div className="flex items-end justify-between mb-3">
                          <p className={`text-2xl font-bold ${prof.profit >= 0 ? 'text-green-500' : 'text-destructive'}`}>
                            {fmtCur(prof.profit, prof.currency)}
                          </p>
                          {margin !== null && (
                            <span className={`text-xs font-semibold px-2 py-1 rounded-md ${prof.profit >= 0 ? 'bg-green-500/20 text-green-600' : 'bg-destructive/20 text-destructive'}`}>
                              {margin}%
                            </span>
                          )}
                        </div>
                        {prof.revenue > 0 && (
                          <div className="w-full bg-muted rounded-full h-1.5">
                            <div
                              className={`h-1.5 rounded-full transition-all ${prof.profit >= 0 ? 'bg-green-500' : 'bg-destructive'}`}
                              style={{ width: `${Math.min(100, Math.max(0, (prof.profit / prof.revenue) * 100))}%` }}
                            />
                          </div>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                </div>

                {/* Bottom row */}
                <div className="grid grid-cols-2 gap-4">
                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">Resource Allocation</CardTitle>
                      <p className="text-xs text-muted-foreground">Hours logged per role</p>
                    </CardHeader>
                    <CardContent>
                      {roleChartData.length === 0 ? (
                        <p className="text-sm text-muted-foreground py-6 text-center">No time logged yet.</p>
                      ) : (
                        <ResponsiveContainer width="100%" height={180}>
                          <BarChart data={roleChartData} margin={{ top: 5, right: 10, left: -20, bottom: 5 }}>
                            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                            <XAxis dataKey="role" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                            <YAxis tick={{ fontSize: 11 }} allowDecimals={false} axisLine={false} tickLine={false} />
                            <Tooltip formatter={(v) => [`${v}h`, 'Hours']} />
                            <Bar dataKey="hours" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                          </BarChart>
                        </ResponsiveContainer>
                      )}
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">Recent Project Activity</CardTitle>
                    </CardHeader>
                    <CardContent>
                      {activityItems.length === 0 ? (
                        <p className="text-sm text-muted-foreground py-6 text-center">No activity yet.</p>
                      ) : (
                        <div className="space-y-3.5">
                          {activityItems.map((item, i) => (
                            <div key={i} className="flex items-start gap-3">
                              <span className={`mt-1.5 w-2 h-2 rounded-full shrink-0 ${
                                item.type === 'time' ? 'bg-green-500' :
                                item.type === 'milestone' ? 'bg-blue-500' : 'bg-destructive'
                              }`} />
                              <p className="flex-1 text-sm leading-snug">
                                <span className="font-medium">{item.bold}</span>
                                {item.label ? ` ${item.label}` : ''}
                              </p>
                              <span className="text-xs text-muted-foreground shrink-0 mt-0.5">{item.time}</span>
                            </div>
                          ))}
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </div>
              </div>
            )}

            {activeTab === 'gantt' && (
              <div className="space-y-3">
                <GanttChart
                  project={p}
                  onTaskDatesChange={(taskId, startDate, dueDate) =>
                    mutGanttDrag.mutate({ taskId, startDate, dueDate })
                  }
                />
              </div>
            )}

            {activeTab === 'burndown' && (
              <BurndownTab projectId={p.id} />
            )}

            {activeTab === 'velocity' && (
              <VelocityCapacityTab projectId={p.id} />
            )}

            {activeTab === 'tasks' && (
              <div className="space-y-5">
                {/* Header */}
                <div className="flex items-start justify-between">
                  <div>
                    <h2 className="text-xl font-bold">Project Tasks</h2>
                    <p className="text-sm text-muted-foreground mt-0.5">Manage and track daily team activities.</p>
                  </div>
                  <Button size="sm" onClick={() => { setTaskOpen(true); setDescExpanded(false) }}>
                    <Plus className="h-3.5 w-3.5 mr-1.5" /> Add Task
                  </Button>
                </div>

                {/* Filters + live indicator */}
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
                  <div className="ml-auto flex items-center gap-1.5 text-xs text-green-500 font-medium">
                    <span className="w-1.5 h-1.5 rounded-full bg-green-500 animate-pulse" />
                    Live Tracking
                  </div>
                </div>

                {/* Stat cards */}
                <div className="grid grid-cols-3 gap-4">
                  <Card>
                    <CardContent className="py-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Completion Rate</p>
                      <p className="text-3xl font-bold mt-1">{completionRate}%</p>
                      <div className="mt-2 w-full bg-muted rounded-full h-1.5">
                        <div className="h-1.5 rounded-full bg-primary transition-all" style={{ width: `${completionRate}%` }} />
                      </div>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardContent className="py-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Total Tasks</p>
                      <p className="text-3xl font-bold mt-1">{p.tasks.length}</p>
                      <p className="text-xs text-muted-foreground mt-1">{doneTasks.length} completed · {overdueTasks.length} overdue</p>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardContent className="py-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Overdue</p>
                      <p className={`text-3xl font-bold mt-1 ${overdueTasks.length > 0 ? 'text-destructive' : ''}`}>
                        {String(overdueTasks.length).padStart(2, '0')}
                      </p>
                      <p className="text-xs text-muted-foreground mt-1">{overdueTasks.length === 0 ? 'All tasks on track' : 'Require attention'}</p>
                    </CardContent>
                  </Card>
                </div>
                {p.tasks.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No tasks yet.</p>
                ) : (
                  <>
                    <div className="grid grid-cols-3 gap-4">
                      {KANBAN_COLUMNS.map(col => {
                        const colTasks = filteredTasks.filter(t => t.status === col.status)
                        const dotColor = col.status === 'Todo' ? 'bg-slate-400' : col.status === 'InProgress' ? 'bg-blue-500' : 'bg-green-500'
                        const today = new Date(); today.setHours(0, 0, 0, 0)
                        return (
                          <div
                            key={col.status}
                            className={`rounded-xl p-3 min-h-48 transition-colors ${dragOverColumn === col.status ? 'bg-primary/10 ring-2 ring-primary/30' : 'bg-muted/30'}`}
                            onDragOver={e => { e.preventDefault(); setDragOverColumn(col.status) }}
                            onDragLeave={e => {
                              if (!e.currentTarget.contains(e.relatedTarget as Node)) setDragOverColumn(null)
                            }}
                            onDrop={e => {
                              e.preventDefault()
                              if (draggedTaskId) {
                                const task = p.tasks.find(t => t.id === draggedTaskId)
                                if (task && task.status !== col.status) {
                                  if (col.status === 'Done') {
                                    setDonePromptTaskId(draggedTaskId)
                                    setDonePromptHours(task.actualHours?.toString() ?? '')
                                  } else {
                                    mutTaskStatus.mutate({ taskId: draggedTaskId, status: col.status })
                                  }
                                }
                              }
                              setDraggedTaskId(null)
                              setDragOverColumn(null)
                            }}
                          >
                            {/* Column header */}
                            <div className="flex items-center gap-2 mb-3 px-1">
                              <span className={`w-2 h-2 rounded-full shrink-0 ${dotColor}`} />
                              <span className="text-xs font-bold uppercase tracking-widest flex-1">{col.label}</span>
                              <span className="text-xs bg-background/60 rounded-full px-2 py-0.5 font-semibold tabular-nums">
                                {colTasks.length}
                              </span>
                            </div>

                            {/* Cards */}
                            <div className="space-y-2.5">
                              {colTasks.map(task => {
                                const isBlocked = task.blockedByTaskIds.length > 0 && task.blockedByTaskIds.some(bid => {
                                  const blocker = p.tasks.find(t => t.id === bid)
                                  return blocker && blocker.status !== 'Done'
                                })
                                const milestoneName = task.milestoneId ? p.milestones.find(m => m.id === task.milestoneId)?.title : null
                                const isDue = task.dueDate ? new Date(task.dueDate) < today && task.status !== 'Done' : false
                                const isDueSoon = task.dueDate && !isDue ? (() => {
                                  const d = new Date(task.dueDate); d.setHours(0,0,0,0)
                                  return (d.getTime() - today.getTime()) <= 3 * 86_400_000
                                })() : false

                                if (col.status === 'Done') {
                                  return (
                                    <div
                                      key={task.id}
                                      draggable
                                      onDragStart={() => setDraggedTaskId(task.id)}
                                      onDragEnd={() => { setDraggedTaskId(null); setDragOverColumn(null) }}
                                      onClick={() => { if (!draggedTaskId) openEditTask(task) }}
                                      className={`bg-card border rounded-lg p-3 cursor-pointer shadow-sm select-none transition-opacity ${draggedTaskId === task.id ? 'opacity-40' : 'hover:border-primary/40 opacity-70 hover:opacity-100'}`}
                                    >
                                      <p className="text-sm font-medium line-through text-muted-foreground leading-snug">{task.title}</p>
                                      <div className="flex items-center justify-between mt-2">
                                        <span className="text-[10px] text-muted-foreground">
                                          {task.completedAt ? `Closed ${timeAgo(task.completedAt)}` : 'Completed'}
                                        </span>
                                        {task.assignedMemberName && (
                                          <span
                                            title={task.assignedMemberName}
                                            className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-primary/20 text-primary font-semibold text-[9px] shrink-0"
                                          >
                                            {task.assignedMemberName.charAt(0).toUpperCase()}
                                          </span>
                                        )}
                                      </div>
                                    </div>
                                  )
                                }

                                return (
                                  <div
                                    key={task.id}
                                    draggable
                                    onDragStart={() => setDraggedTaskId(task.id)}
                                    onDragEnd={() => { setDraggedTaskId(null); setDragOverColumn(null) }}
                                    onClick={() => { if (!draggedTaskId) openEditTask(task) }}
                                    className={`bg-card border rounded-lg p-3 cursor-pointer shadow-sm select-none transition-colors ${draggedTaskId === task.id ? 'opacity-40 cursor-grabbing' : 'hover:border-primary/50'}`}
                                  >
                                    {milestoneName && (
                                      <span className="inline-flex items-center gap-1 text-[10px] font-medium text-amber-600 bg-amber-100 dark:bg-amber-950 dark:text-amber-400 px-1.5 py-0.5 rounded mb-2">
                                        <Flag className="h-2.5 w-2.5" />{milestoneName}
                                      </span>
                                    )}
                                    <div className="flex items-start gap-1.5">
                                      <p className="font-semibold text-sm leading-snug flex-1">{task.title}</p>
                                      {isBlocked && <Lock className="h-3 w-3 shrink-0 mt-0.5 text-amber-500" />}
                                    </div>
                                    {hasRichContent(task.description) && (
                                      <p className="text-xs text-muted-foreground mt-1 line-clamp-2 leading-relaxed">{stripHtml(task.description!)}</p>
                                    )}
                                    <div className="flex items-center flex-wrap gap-x-3 gap-y-1 mt-2.5 text-xs">
                                      {task.dueDate && (
                                        <span className={`flex items-center gap-1 font-medium ${isDue ? 'text-destructive' : isDueSoon ? 'text-amber-500' : 'text-muted-foreground'}`}>
                                          <CalendarDays className="h-3 w-3 shrink-0" />
                                          {new Date(task.dueDate).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit' })}
                                        </span>
                                      )}
                                      {task.estimatedHours && (
                                        <span className="flex items-center gap-1 text-muted-foreground">
                                          <Clock className="h-3 w-3 shrink-0" />{task.estimatedHours}h
                                        </span>
                                      )}
                                      {task.comments.length > 0 && (
                                        <span className="flex items-center gap-1 text-muted-foreground">
                                          <MessageSquare className="h-3 w-3 shrink-0" />{task.comments.length}
                                        </span>
                                      )}
                                      {task.assignedMemberName && (
                                        <span className="ml-auto flex items-center gap-1 text-muted-foreground">
                                          <span
                                            title={task.assignedMemberName}
                                            className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-primary/20 text-primary font-semibold text-[9px] shrink-0"
                                          >
                                            {task.assignedMemberName.charAt(0).toUpperCase()}
                                          </span>
                                          <span className="truncate max-w-[80px]">{task.assignedMemberName}</span>
                                        </span>
                                      )}
                                    </div>
                                  </div>
                                )
                              })}
                            </div>

                            {/* Add Card button */}
                            <button
                              onClick={() => { setTaskOpen(true); setDescExpanded(false) }}
                              className="mt-2.5 w-full flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground py-1.5 px-2 rounded-md hover:bg-background/60 transition-colors"
                            >
                              <Plus className="h-3.5 w-3.5" /> Add Card
                            </button>
                          </div>
                        )
                      })}
                    </div>

                    {/* Task Metrics */}
                    {(() => {
                      const tasksWithBoth = doneTasks.filter(t => t.estimatedHours && t.actualHours && t.actualHours > 0)
                      const efficiencyPct = tasksWithBoth.length > 0
                        ? Math.round(tasksWithBoth.reduce((s, t) => s + Math.min(1, t.estimatedHours! / t.actualHours!), 0) / tasksWithBoth.length * 100)
                        : null
                      return (
                        <div className="grid grid-cols-3 gap-4 pt-2">
                          <Card>
                            <CardContent className="py-4">
                              <p className="text-xs text-muted-foreground uppercase tracking-wide">Efficiency</p>
                              <p className="text-3xl font-bold mt-1">{efficiencyPct !== null ? `${efficiencyPct}%` : '—'}</p>
                              <p className="text-xs text-muted-foreground mt-1">Est. vs actual hours</p>
                            </CardContent>
                          </Card>
                          <Card>
                            <CardContent className="py-4">
                              <p className="text-xs text-muted-foreground uppercase tracking-wide">Resolved</p>
                              <p className="text-3xl font-bold mt-1 text-green-500">{doneTasks.length}</p>
                              <p className="text-xs text-muted-foreground mt-1">Tasks completed</p>
                            </CardContent>
                          </Card>
                          <Card>
                            <CardContent className="py-4">
                              <p className="text-xs text-muted-foreground uppercase tracking-wide">Overdue</p>
                              <p className={`text-3xl font-bold mt-1 ${overdueTasks.length > 0 ? 'text-destructive' : 'text-green-500'}`}>
                                {String(overdueTasks.length).padStart(2, '0')}
                              </p>
                              <p className="text-xs text-muted-foreground mt-1">{overdueTasks.length === 0 ? 'All on track' : 'Need attention'}</p>
                            </CardContent>
                          </Card>
                        </div>
                      )
                    })()}
                  </>
                )}
              </div>
            )}

            {activeTab === 'team' && (
              <div className="space-y-5">
                {/* Header */}
                <div className="flex items-start justify-between">
                  <div>
                    <h2 className="text-xl font-bold">Project Team</h2>
                    <p className="text-sm text-muted-foreground mt-0.5">Manage resource allocation and billing rates for {p.name}.</p>
                  </div>
                  <Button size="sm" onClick={() => setMemberOpen(true)}>
                    <UserPlus className="h-3.5 w-3.5 mr-1.5" /> Add Member
                  </Button>
                </div>

                {/* Stat cards */}
                <div className="grid grid-cols-3 gap-4">
                  <Card>
                    <CardContent className="py-5">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Total Headcount</p>
                      <p className="text-4xl font-bold mt-2">{p.members.length}</p>
                      <p className="text-xs text-muted-foreground mt-1">
                        {p.members.length === 0 ? 'No members yet' : `${p.members.length} team member${p.members.length !== 1 ? 's' : ''}`}
                      </p>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardContent className="py-5">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Average Rate</p>
                      <div className="flex items-baseline gap-1 mt-2">
                        <p className="text-4xl font-bold">{p.members.length > 0 ? fmtCur(avgRate, p.currency) : '—'}</p>
                        {p.members.length > 0 && <span className="text-sm text-muted-foreground">/hour</span>}
                      </div>
                      <p className="text-xs text-muted-foreground mt-1">Across {p.members.length} member{p.members.length !== 1 ? 's' : ''}</p>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardContent className="py-5">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Team Allocation</p>
                      {p.members.length > 0 && totalLoggedHours > 0 ? (
                        <>
                          <div className="flex w-full h-4 rounded-full overflow-hidden mt-3 mb-2">
                            {p.members.map((m, i) => {
                              const h = memberHoursMap[m.id] ?? 0
                              const pct = (h / totalLoggedHours) * 100
                              return pct > 0 ? (
                                <div key={m.id} style={{ width: `${pct}%`, backgroundColor: MEMBER_COLORS[i % MEMBER_COLORS.length] }} title={`${m.name}: ${h}h`} />
                              ) : null
                            })}
                          </div>
                          <div className="flex justify-between text-xs text-muted-foreground">
                            <span>{totalLoggedHours}h total</span>
                            <span>{p.members.length} members</span>
                          </div>
                          <div className="flex flex-wrap gap-x-3 gap-y-1 mt-2">
                            {p.members.slice(0, 5).map((m, i) => (
                              <span key={m.id} className="flex items-center gap-1 text-[10px] text-muted-foreground">
                                <span className="w-2 h-2 rounded-full shrink-0" style={{ backgroundColor: MEMBER_COLORS[i % MEMBER_COLORS.length] }} />
                                {m.name.split(' ')[0]}
                              </span>
                            ))}
                          </div>
                        </>
                      ) : (
                        <p className="text-sm text-muted-foreground mt-3">No hours logged yet</p>
                      )}
                    </CardContent>
                  </Card>
                </div>

                {/* Active Members table */}
                <Card>
                  <CardContent className="p-0">
                    <div className="flex items-center justify-between px-5 py-4 border-b">
                      <h3 className="font-semibold">Active Members</h3>
                      <div className="relative w-56">
                        <Search className="absolute left-2.5 top-2 h-4 w-4 text-muted-foreground" />
                        <input
                          type="text"
                          placeholder="Search members..."
                          value={teamSearch}
                          onChange={e => setTeamSearch(e.target.value)}
                          className="w-full h-8 pl-8 pr-3 text-sm bg-muted rounded-md border-0 focus:outline-none focus:ring-1 focus:ring-primary placeholder:text-muted-foreground"
                        />
                      </div>
                    </div>
                    {filteredMembers.length === 0 ? (
                      <p className="text-sm text-muted-foreground py-10 text-center">
                        {p.members.length === 0 ? 'No team members yet.' : 'No members match your search.'}
                      </p>
                    ) : (
                      <>
                        <div className="grid grid-cols-[1fr_160px_140px_120px] px-5 py-2.5 text-[10px] font-bold uppercase tracking-widest text-muted-foreground border-b bg-muted/30">
                          <span>Team Member</span>
                          <span>Role</span>
                          <span>Hourly Rate</span>
                          <span className="text-right">Hours Logged</span>
                        </div>
                        <div className="divide-y divide-border">
                          {filteredMembers.map((member, i) => {
                            const idx = p.members.indexOf(member)
                            const hours = memberHoursMap[member.id] ?? 0
                            const cost = hours * member.hourlyRate
                            return (
                              <div key={member.id} className="grid grid-cols-[1fr_160px_140px_120px] items-center px-5 py-3.5">
                                <div className="flex items-center gap-3">
                                  <span
                                    className="inline-flex items-center justify-center w-9 h-9 rounded-full font-bold text-sm text-white shrink-0"
                                    style={{ backgroundColor: MEMBER_COLORS[idx % MEMBER_COLORS.length] }}
                                  >
                                    {member.name.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase()}
                                  </span>
                                  <div>
                                    <p className="text-sm font-semibold leading-tight">{member.name}</p>
                                    <p className="text-xs text-muted-foreground">{hours > 0 ? `${fmtCur(cost, member.currency)} total cost` : 'No hours logged'}</p>
                                  </div>
                                </div>
                                <p className="text-sm text-muted-foreground">{member.role}</p>
                                <p className="text-sm font-semibold">{fmtCur(member.hourlyRate, member.currency)}/h</p>
                                <p className="text-sm font-semibold text-right">{hours > 0 ? `${hours}h` : '—'}</p>
                              </div>
                            )
                          })}
                        </div>
                      </>
                    )}
                  </CardContent>
                </Card>

                {/* Resource Utilization + Cost Breakdown */}
                <div className="grid grid-cols-2 gap-4">
                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">Resource Utilization</CardTitle>
                      <p className="text-xs text-muted-foreground">Hours logged per team member</p>
                    </CardHeader>
                    <CardContent>
                      {memberChartData.every(d => d.hours === 0) ? (
                        <p className="text-sm text-muted-foreground py-6 text-center">No hours logged yet.</p>
                      ) : (
                        <ResponsiveContainer width="100%" height={180}>
                          <BarChart data={memberChartData} margin={{ top: 5, right: 10, left: -20, bottom: 5 }}>
                            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                            <XAxis dataKey="name" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                            <YAxis tick={{ fontSize: 11 }} allowDecimals={false} axisLine={false} tickLine={false} />
                            <Tooltip formatter={(v) => [`${v}h`, 'Hours']} />
                            <Bar dataKey="hours" radius={[4, 4, 0, 0]}>
                              {memberChartData.map((entry, index) => (
                                <Cell key={index} fill={entry.fill} />
                              ))}
                            </Bar>
                          </BarChart>
                        </ResponsiveContainer>
                      )}
                      <div className="grid grid-cols-2 gap-3 mt-3 pt-3 border-t">
                        <div>
                          <p className="text-xs text-muted-foreground uppercase tracking-wide">Total Hours</p>
                          <p className="text-xl font-bold mt-0.5">{totalLoggedHours}h</p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground uppercase tracking-wide">Team Cost</p>
                          <p className="text-xl font-bold mt-0.5">{fmtCur(totalTeamCost, p.currency)}</p>
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">Cost Breakdown</CardTitle>
                      <p className="text-xs text-muted-foreground">Labor cost per member</p>
                    </CardHeader>
                    <CardContent>
                      {p.members.length === 0 ? (
                        <p className="text-sm text-muted-foreground py-6 text-center">No team members yet.</p>
                      ) : (
                        <div className="space-y-4">
                          {p.members.map((member, i) => {
                            const hours = memberHoursMap[member.id] ?? 0
                            const cost = hours * member.hourlyRate
                            const costPct = totalTeamCost > 0 ? (cost / totalTeamCost) * 100 : 0
                            return (
                              <div key={member.id} className="space-y-1.5">
                                <div className="flex items-center justify-between text-sm">
                                  <div className="flex items-center gap-2">
                                    <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: MEMBER_COLORS[i % MEMBER_COLORS.length] }} />
                                    <span className="font-medium">{member.name}</span>
                                  </div>
                                  <span className="font-semibold">{hours > 0 ? fmtCur(cost, member.currency) : '—'}</span>
                                </div>
                                <div className="w-full bg-muted rounded-full h-1.5">
                                  <div
                                    className="h-1.5 rounded-full transition-all"
                                    style={{ width: `${costPct}%`, backgroundColor: MEMBER_COLORS[i % MEMBER_COLORS.length] }}
                                  />
                                </div>
                              </div>
                            )
                          })}
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </div>

              </div>
            )}

            {activeTab === 'timelog' && (
              <div className="space-y-4">
                {/* Header */}
                <div className="flex items-start justify-between">
                  <div>
                    <h2 className="text-xl font-bold">Project Time Log</h2>
                    <p className="text-sm text-muted-foreground mt-0.5">Detailed breakdown of hours and resource allocation for {p.name}.</p>
                  </div>
                  <Button size="sm" onClick={() => setTimeOpen(true)}>
                    <Clock className="h-3.5 w-3.5 mr-1.5" /> Log Time
                  </Button>
                </div>

                {/* Stat cards */}
                <div className="grid grid-cols-3 gap-4">
                  <Card>
                    <CardContent className="py-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Total Logged Time</p>
                      <p className="text-3xl font-bold mt-1">{totalLoggedHours}<span className="text-lg font-normal ml-1">h</span></p>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardContent className="py-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Budget Utilization</p>
                      <p className="text-3xl font-bold mt-1">{budgetUtilPct.toFixed(0)}<span className="text-lg font-normal">%</span></p>
                      <div className="mt-2 w-full bg-muted rounded-full h-1.5">
                        <div
                          className={`h-1.5 rounded-full ${budgetUtilPct > 90 ? 'bg-destructive' : 'bg-primary'}`}
                          style={{ width: `${budgetUtilPct}%` }}
                        />
                      </div>
                      <p className="text-xs text-muted-foreground mt-1">{fmtCur(prof.totalCost, prof.currency)} of {fmtCur(p.budgetAmount, p.currency)}</p>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardContent className="py-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Logged Value</p>
                      <p className="text-2xl font-bold mt-1">{fmtCur(totalLoggedValue, prof.currency)}</p>
                    </CardContent>
                  </Card>
                </div>

                {/* Filter bar + entry list */}
                <Card>
                  <CardContent className="pt-3 pb-0">
                    <div className="flex items-center gap-2 pb-3 border-b border-border">
                      <div className="relative flex-1 max-w-xs">
                        <Search className="absolute left-2.5 top-2 h-4 w-4 text-muted-foreground" />
                        <input
                          type="text"
                          placeholder="Filter logs..."
                          value={timeSearch}
                          onChange={e => { setTimeSearch(e.target.value); setTimePage(1) }}
                          className="w-full h-8 pl-8 pr-3 text-sm bg-muted rounded-md border-0 focus:outline-none focus:ring-1 focus:ring-primary placeholder:text-muted-foreground"
                        />
                      </div>
                      <Select value={timeDateFilter} onChange={e => { setTimeDateFilter(e.target.value as 'all' | '7d' | '30d'); setTimePage(1) }} className="h-8 text-xs w-36">
                        <option value="all">All Time</option>
                        <option value="7d">Last 7 Days</option>
                        <option value="30d">Last 30 Days</option>
                      </Select>
                      <Select value={timeListMemberFilter} onChange={e => { setTimeListMemberFilter(e.target.value); setTimePage(1) }} className="h-8 text-xs w-40">
                        <option value="all">All Members</option>
                        {p.members.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
                      </Select>
                    </div>

                    {filteredTimeEntries.length === 0 ? (
                      <p className="text-sm text-muted-foreground py-8 text-center">No time entries match your filters.</p>
                    ) : (
                      <div className="divide-y divide-border">
                        {pagedTimeEntries.map(entry => {
                          const memberRole = p.members.find(m => m.id === entry.memberId)?.role ?? '—'
                          return (
                            <div key={entry.id} className="flex items-center gap-4 py-3.5">
                              <span className="inline-flex items-center justify-center w-10 h-10 rounded-full bg-primary/20 text-primary font-semibold text-sm shrink-0">
                                {entry.memberName.charAt(0).toUpperCase()}
                              </span>
                              <div className="w-36 shrink-0">
                                <p className="text-sm font-semibold leading-tight">{entry.memberName}</p>
                                <p className="text-xs text-muted-foreground">{memberRole}</p>
                              </div>
                              <div className="flex-1 min-w-0">
                                {entry.description && <p className="text-sm truncate">{entry.description}</p>}
                                <p className="text-xs text-muted-foreground flex items-center gap-1 mt-0.5">
                                  <CalendarDays className="h-3 w-3 shrink-0" />
                                  {new Date(entry.date).toLocaleDateString('de-AT', { day: 'numeric', month: 'numeric', year: 'numeric' })}
                                </p>
                              </div>
                              <p className="text-sm font-semibold shrink-0 w-12 text-right">{entry.hoursWorked}h</p>
                              <p className="text-sm font-medium shrink-0 w-24 text-right">{fmtCur(entry.cost, entry.currency)}</p>
                            </div>
                          )
                        })}
                      </div>
                    )}
                  </CardContent>

                  {filteredTimeEntries.length > TIME_PER_PAGE && (
                    <div className="flex items-center justify-between px-6 py-3 border-t text-sm text-muted-foreground">
                      <span>Showing {(timePage - 1) * TIME_PER_PAGE + 1}–{Math.min(timePage * TIME_PER_PAGE, filteredTimeEntries.length)} of {filteredTimeEntries.length} logs</span>
                      <div className="flex items-center gap-1">
                        <button
                          className="w-7 h-7 rounded border flex items-center justify-center hover:bg-muted disabled:opacity-40"
                          onClick={() => setTimePage(prev => Math.max(1, prev - 1))}
                          disabled={timePage === 1}
                        >‹</button>
                        {Array.from({ length: Math.min(totalTimePages, 5) }, (_, i) => i + 1).map(pg => (
                          <button
                            key={pg}
                            className={`w-7 h-7 rounded border flex items-center justify-center text-xs ${timePage === pg ? 'bg-primary text-primary-foreground border-primary' : 'hover:bg-muted'}`}
                            onClick={() => setTimePage(pg)}
                          >{pg}</button>
                        ))}
                        <button
                          className="w-7 h-7 rounded border flex items-center justify-center hover:bg-muted disabled:opacity-40"
                          onClick={() => setTimePage(prev => Math.min(totalTimePages, prev + 1))}
                          disabled={timePage === totalTimePages}
                        >›</button>
                      </div>
                    </div>
                  )}
                </Card>

                {/* Weekly Distribution */}
                <Card>
                  <CardHeader className="pb-2">
                    <CardTitle className="text-base">Weekly Distribution</CardTitle>
                    <p className="text-xs text-muted-foreground">Hours logged per day over the last 7 days</p>
                  </CardHeader>
                  <CardContent>
                    {weeklyChartData.every(d => d.hours === 0) ? (
                      <p className="text-sm text-muted-foreground py-4 text-center">No time logged in the last 7 days.</p>
                    ) : (
                      <ResponsiveContainer width="100%" height={160}>
                        <BarChart data={weeklyChartData} margin={{ top: 5, right: 10, left: -20, bottom: 5 }}>
                          <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                          <XAxis dataKey="label" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                          <YAxis tick={{ fontSize: 11 }} allowDecimals={false} axisLine={false} tickLine={false} />
                          <Tooltip formatter={(v) => [`${v}h`, 'Hours']} />
                          <Bar dataKey="hours" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                        </BarChart>
                      </ResponsiveContainer>
                    )}
                  </CardContent>
                </Card>
              </div>
            )}

            {activeTab === 'expenses' && (
              <div className="space-y-4">
                {/* Header */}
                <div className="flex items-start justify-between">
                  <div>
                    <h2 className="text-xl font-bold">Project Expenses</h2>
                    <p className="text-sm text-muted-foreground mt-0.5">Manage and track all project-related expenditures.</p>
                  </div>
                  <Button size="sm" onClick={() => setExpenseOpen(true)}>
                    <Receipt className="h-3.5 w-3.5 mr-1.5" /> Add Expense
                  </Button>
                </div>

                {/* Stat cards */}
                <div className="grid grid-cols-2 gap-4">
                  <Card>
                    <CardContent className="py-4">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Total Expense Overview</p>
                      <div className="flex items-end justify-between mt-1">
                        <p className="text-3xl font-bold">{fmtCur(totalExpenses, prof.currency)}</p>
                        <p className="text-xs text-muted-foreground">Budget: {fmtCur(p.budgetAmount, p.currency)}</p>
                      </div>
                      <div className="grid grid-cols-3 gap-3 mt-4 pt-3 border-t border-border">
                        <div>
                          <p className="text-xs text-muted-foreground">Categories</p>
                          <p className="text-sm font-semibold mt-0.5">{uniqueExpenseCategories.length}</p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground">Entries</p>
                          <p className="text-sm font-semibold mt-0.5">{p.expenses.length}</p>
                        </div>
                        <div>
                          <p className="text-xs text-muted-foreground">Avg / Entry</p>
                          <p className="text-sm font-semibold mt-0.5">{p.expenses.length > 0 ? fmtCur(totalExpenses / p.expenses.length, prof.currency) : '—'}</p>
                        </div>
                      </div>
                    </CardContent>
                  </Card>

                  <Card className={remainingBudget >= 0 ? 'border-primary/30' : 'border-destructive/30'}>
                    <CardContent className="py-4">
                      <p className="text-xs font-semibold uppercase tracking-wide text-primary">Remaining Budget</p>
                      <p className={`text-3xl font-bold mt-1 ${remainingBudget >= 0 ? '' : 'text-destructive'}`}>
                        {fmtCur(Math.abs(remainingBudget), p.currency)}
                      </p>
                      <p className="text-xs text-muted-foreground mt-1">
                        {remainingBudget >= 0
                          ? `${(100 - budgetExpensePct).toFixed(1)}% of allocated funds remaining.`
                          : 'Budget exceeded.'}
                      </p>
                      <div className="mt-3 w-full bg-muted rounded-full h-1.5">
                        <div
                          className={`h-1.5 rounded-full ${budgetExpensePct > 90 ? 'bg-destructive' : 'bg-primary'}`}
                          style={{ width: `${Math.min(100, budgetExpensePct)}%` }}
                        />
                      </div>
                      <div className="flex justify-between mt-2 text-xs text-muted-foreground">
                        <span>Spent: {budgetExpensePct.toFixed(1)}%</span>
                        <span>Budget: {fmtCur(p.budgetAmount, p.currency)}</span>
                      </div>
                    </CardContent>
                  </Card>
                </div>

                {/* Filter bar + expense list */}
                <Card>
                  <CardContent className="pt-3 pb-0">
                    <div className="flex items-center gap-2 pb-3 border-b border-border">
                      <div className="relative flex-1 max-w-xs">
                        <Search className="absolute left-2.5 top-2 h-4 w-4 text-muted-foreground" />
                        <input
                          type="text"
                          placeholder="Search expenses..."
                          value={expenseSearch}
                          onChange={e => setExpenseSearch(e.target.value)}
                          className="w-full h-8 pl-8 pr-3 text-sm bg-muted rounded-md border-0 focus:outline-none focus:ring-1 focus:ring-primary placeholder:text-muted-foreground"
                        />
                      </div>
                      <Select value={expenseListCategoryFilter} onChange={e => setExpenseListCategoryFilter(e.target.value)} className="h-8 text-xs w-40">
                        <option value="all">All Categories</option>
                        {uniqueExpenseCategories.map(c => <option key={c} value={c}>{c}</option>)}
                      </Select>
                      <span className="ml-auto text-xs text-muted-foreground shrink-0">
                        Showing {filteredExpenses.length} Expense{filteredExpenses.length !== 1 ? 's' : ''}
                      </span>
                    </div>

                    {filteredExpenses.length === 0 ? (
                      <p className="text-sm text-muted-foreground py-8 text-center">No expenses match your filters.</p>
                    ) : (
                      <>
                        <div className="grid grid-cols-[1fr_120px_120px] py-2 px-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground border-b border-border">
                          <span>Description &amp; Category</span>
                          <span className="text-center">Date</span>
                          <span className="text-right">Amount</span>
                        </div>
                        <div className="divide-y divide-border">
                          {filteredExpenses.map(expense => (
                            <div key={expense.id} className="grid grid-cols-[1fr_120px_120px] items-center py-3.5 px-1">
                              <div>
                                <p className="text-sm font-semibold">{expense.description}</p>
                                <span className={`inline-block text-[10px] font-medium uppercase tracking-wide px-1.5 py-0.5 rounded mt-1 ${CATEGORY_COLOR[expense.category] ?? CATEGORY_COLOR.Other}`}>
                                  {expense.category}
                                </span>
                              </div>
                              <p className="text-sm text-muted-foreground text-center">
                                {new Date(expense.date).toLocaleDateString('de-AT', { day: 'numeric', month: 'numeric', year: 'numeric' })}
                              </p>
                              <p className="text-sm font-semibold text-right">{fmtCur(expense.amount, expense.currency)}</p>
                            </div>
                          ))}
                        </div>
                      </>
                    )}
                  </CardContent>
                </Card>

                {/* Bottom charts */}
                <div className="grid grid-cols-2 gap-4">
                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">Spending by Category</CardTitle>
                    </CardHeader>
                    <CardContent>
                      {spendingByCategoryData.length === 0 ? (
                        <p className="text-sm text-muted-foreground py-4 text-center">No expenses yet.</p>
                      ) : (
                        <ResponsiveContainer width="100%" height={200}>
                          <BarChart data={spendingByCategoryData} layout="vertical" margin={{ top: 0, right: 20, left: 10, bottom: 0 }}>
                            <XAxis type="number" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} tickFormatter={v => `€${v}`} />
                            <YAxis type="category" dataKey="category" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} width={95} />
                            <Tooltip formatter={(v) => [typeof v === 'number' ? fmtCur(v, prof.currency) : v, 'Amount']} />
                            <Bar dataKey="amount" fill="#3b82f6" radius={[0, 4, 4, 0]} />
                          </BarChart>
                        </ResponsiveContainer>
                      )}
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base">Expense Timeline</CardTitle>
                      <p className="text-xs text-muted-foreground">Amount spent per month</p>
                    </CardHeader>
                    <CardContent>
                      {expenseTimelineData.length === 0 ? (
                        <p className="text-sm text-muted-foreground py-4 text-center">No expenses yet.</p>
                      ) : (
                        <ResponsiveContainer width="100%" height={200}>
                          <BarChart data={expenseTimelineData} margin={{ top: 5, right: 10, left: -10, bottom: 5 }}>
                            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
                            <XAxis dataKey="month" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
                            <YAxis tick={{ fontSize: 11 }} axisLine={false} tickLine={false} tickFormatter={v => `€${v}`} />
                            <Tooltip formatter={(v) => [typeof v === 'number' ? fmtCur(v, prof.currency) : v, 'Amount']} />
                            <Bar dataKey="amount" fill="#8b5cf6" radius={[4, 4, 0, 0]} />
                          </BarChart>
                        </ResponsiveContainer>
                      )}
                    </CardContent>
                  </Card>
                </div>
              </div>
            )}

            {activeTab === 'notes' && (
              <NotesTab projectId={p.id} notes={p.notes} />
            )}

            {activeTab === 'milestones' && (
              <div className="space-y-6">
                {/* Header */}
                <div className="flex items-start justify-between">
                  <div>
                    <h2 className="text-xl font-bold">Project Milestones</h2>
                    <p className="text-sm text-muted-foreground mt-0.5">Track key project achievements and upcoming deadlines.</p>
                  </div>
                  <Button size="sm" onClick={() => setMilestoneOpen(true)}>
                    <Plus className="h-3.5 w-3.5 mr-1.5" /> Add Milestone
                  </Button>
                </div>

                {/* Stat cards */}
                <div className="grid grid-cols-2 gap-4">
                  <Card>
                    <CardContent className="py-5">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Overall Completion</p>
                      <div className="flex items-baseline gap-2 mt-2">
                        <span className="text-4xl font-bold">{completedMilestones.length}</span>
                        <span className="text-lg text-muted-foreground">/ {p.milestones.length} Milestones</span>
                      </div>
                      <div className="mt-3 w-full bg-muted rounded-full h-2">
                        <div className="h-2 rounded-full bg-primary transition-all" style={{ width: `${milestoneCompletionPct}%` }} />
                      </div>
                    </CardContent>
                  </Card>
                  <Card>
                    <CardContent className="py-5">
                      <p className="text-xs text-muted-foreground uppercase tracking-wide">Upcoming Deadlines</p>
                      <div className="flex items-center gap-4 mt-2">
                        <div className="w-12 h-12 rounded-lg bg-destructive/15 flex items-center justify-center shrink-0">
                          <Flag className="h-6 w-6 text-destructive" />
                        </div>
                        <div>
                          <p className="text-3xl font-bold">{activeMilestones.length}</p>
                          <p className="text-xs text-muted-foreground">Active Milestones</p>
                        </div>
                      </div>
                      {nextMilestoneDays !== null && (
                        <p className={`text-xs mt-3 flex items-center gap-1 font-medium ${nextMilestoneDays < 0 ? 'text-destructive' : nextMilestoneDays <= 7 ? 'text-amber-500' : 'text-green-500'}`}>
                          <TrendingUp className="h-3 w-3" />
                          {nextMilestoneDays < 0
                            ? `Next is ${Math.abs(nextMilestoneDays)}d overdue`
                            : nextMilestoneDays === 0
                            ? 'Next due today'
                            : `Next due in ${nextMilestoneDays}d`}
                        </p>
                      )}
                    </CardContent>
                  </Card>
                </div>

                {/* Active Milestones */}
                {p.milestones.length === 0 && (
                  <div className="py-12 text-center">
                    <Flag className="h-8 w-8 text-muted-foreground mx-auto mb-3" />
                    <p className="text-sm text-muted-foreground">No milestones yet. Add your first to track project progress.</p>
                  </div>
                )}

                {activeMilestones.length > 0 && (
                  <div className="space-y-3">
                    <h3 className="text-base font-semibold">Active Milestones</h3>
                    {activeMilestones.map(milestone => {
                      const daysLeft = Math.ceil((new Date(milestone.dueDate).getTime() - Date.now()) / 86_400_000)
                      const isOverdue = daysLeft < 0
                      return (
                        <Card key={milestone.id} className={isOverdue ? 'border-destructive/40' : ''}>
                          <CardContent className="py-4 flex items-center gap-4">
                            <div className={`w-10 h-10 rounded-lg flex items-center justify-center shrink-0 ${isOverdue ? 'bg-destructive/15' : 'bg-muted'}`}>
                              <Flag className={`h-5 w-5 ${isOverdue ? 'text-destructive' : 'text-muted-foreground'}`} />
                            </div>
                            <div className="flex-1 min-w-0">
                              <p className="font-semibold text-sm">{milestone.title}</p>
                              <div className="flex items-center gap-2 mt-1 flex-wrap">
                                <span className="flex items-center gap-1 text-xs text-muted-foreground">
                                  <CalendarDays className="h-3 w-3 shrink-0" />
                                  Due {new Date(milestone.dueDate).toLocaleDateString('de-AT', { day: 'numeric', month: 'numeric', year: 'numeric' })}
                                </span>
                                <Badge variant={isOverdue ? 'destructive' : 'secondary'}>
                                  {isOverdue ? 'Overdue' : 'Pending'}
                                </Badge>
                                {!isOverdue && daysLeft <= 7 && (
                                  <span className="text-xs text-amber-500 font-medium">• Due soon</span>
                                )}
                                {!isOverdue && daysLeft > 7 && (
                                  <span className="text-xs text-muted-foreground">• {daysLeft}d remaining</span>
                                )}
                              </div>
                            </div>
                            <Button
                              size="sm"
                              onClick={() => mutCompleteMilestone.mutate(milestone.id)}
                              disabled={mutCompleteMilestone.isPending}
                              className="shrink-0"
                            >
                              <CheckCircle2 className="h-3.5 w-3.5 mr-1.5" />Mark Complete
                            </Button>
                          </CardContent>
                        </Card>
                      )
                    })}
                  </div>
                )}

                {/* Recently Completed */}
                {completedMilestones.length > 0 && (
                  <div className="space-y-3">
                    <h3 className="text-base font-semibold text-muted-foreground">Recently Completed</h3>
                    {completedMilestones.map(milestone => (
                      <Card key={milestone.id} className="opacity-60">
                        <CardContent className="py-4 flex items-center gap-4">
                          <div className="w-10 h-10 rounded-lg bg-green-500/15 flex items-center justify-center shrink-0">
                            <CheckCircle2 className="h-5 w-5 text-green-500" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="font-semibold text-sm line-through text-muted-foreground">{milestone.title}</p>
                            <div className="flex items-center gap-2 mt-1">
                              <span className="flex items-center gap-1 text-xs text-muted-foreground">
                                <CalendarDays className="h-3 w-3 shrink-0" />
                                Due {new Date(milestone.dueDate).toLocaleDateString('de-AT', { day: 'numeric', month: 'numeric', year: 'numeric' })}
                              </span>
                              <Badge variant="success">Completed</Badge>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                )}
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
                  <RichTextEditor
                    value={editTaskDesc}
                    onChange={setEditTaskDesc}
                    placeholder="Add a description…"
                    className={descExpanded ? 'flex-1 min-h-0' : ''}
                    editorClassName={descExpanded ? 'min-h-0' : 'min-h-[160px]'}
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
                  <Label className="text-xs text-muted-foreground font-normal">Actual Hours</Label>
                  <Input type="number" step="0.5" min="0" value={editTaskActualHours} onChange={e => setEditActualTaskHours(e.target.value)} placeholder="—" />
                </div>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Start Date</Label>
                  <Input type="date" value={editTaskStart} onChange={e => setEditTaskStart(e.target.value)} />
                </div>

                <div className="space-y-1.5">
                  <Label className="text-xs text-muted-foreground font-normal">Due Date</Label>
                  <Input type="date" value={editTaskDue} onChange={e => setEditTaskDue(e.target.value)} />
                </div>

                <div className="w-full h-px bg-border" />

                {/* Dependencies */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <Label className="text-xs text-muted-foreground font-normal">Blocked By</Label>
                    {!addingDependency && (
                      <button
                        className="text-xs text-muted-foreground hover:text-foreground underline"
                        onClick={() => setAddingDependency(true)}
                      >
                        + Add
                      </button>
                    )}
                  </div>

                  {/* Current blockers */}
                  {(p.tasks.find(t => t.id === editingTask.id)?.blockedByTaskIds ?? []).map(blockerId => {
                    const blocker = p.tasks.find(t => t.id === blockerId)
                    if (!blocker) return null
                    return (
                      <div key={blockerId} className="flex items-center justify-between gap-1 text-xs bg-muted/50 rounded px-2 py-1">
                        <span className="flex items-center gap-1 truncate">
                          <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${blocker.status === 'Done' ? 'bg-green-500' : 'bg-amber-400'}`} />
                          <span className="truncate">{blocker.title}</span>
                        </span>
                        <button
                          className="text-muted-foreground hover:text-destructive shrink-0"
                          onClick={() => mutRemoveDependency.mutate({ blockingTaskId: blockerId })}
                          disabled={mutRemoveDependency.isPending}
                        >
                          <X className="h-3 w-3" />
                        </button>
                      </div>
                    )
                  })}

                  {addingDependency && (
                    <div className="space-y-1.5">
                      <Select
                        value={pendingBlockerId}
                        onChange={e => setPendingBlockerId(e.target.value)}
                        className="text-xs"
                      >
                        <option value="">Select a task…</option>
                        {p.tasks
                          .filter(t =>
                            t.id !== editingTask.id &&
                            !(p.tasks.find(tt => tt.id === editingTask.id)?.blockedByTaskIds ?? []).includes(t.id)
                          )
                          .map(t => (
                            <option key={t.id} value={t.id}>{t.title}</option>
                          ))
                        }
                      </Select>
                      <div className="flex gap-1">
                        <Button
                          size="sm"
                          className="flex-1 text-xs h-7"
                          onClick={() => pendingBlockerId && mutAddDependency.mutate({ blockingTaskId: pendingBlockerId })}
                          disabled={!pendingBlockerId || mutAddDependency.isPending}
                        >
                          {mutAddDependency.isPending ? '…' : 'Add'}
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-xs h-7"
                          onClick={() => { setAddingDependency(false); setPendingBlockerId('') }}
                        >
                          Cancel
                        </Button>
                      </div>
                    </div>
                  )}
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
                  <RichTextEditor
                    value={taskDesc}
                    onChange={setTaskDesc}
                    placeholder="Add a description…"
                    className={descExpanded ? 'flex-1 min-h-0' : ''}
                    editorClassName={descExpanded ? 'min-h-0' : 'min-h-[160px]'}
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

      {/* Actual hours prompt — shown when dragging a task to Done */}
      <Dialog
        open={!!donePromptTaskId}
        onClose={() => { setDonePromptTaskId(null); setDonePromptHours('') }}
        title="Log actual hours"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            How many hours did this task actually take?
          </p>
          <div className="space-y-1">
            <Label>Actual Hours</Label>
            <Input
              type="number"
              step="0.5"
              min="0.5"
              value={donePromptHours}
              onChange={e => setDonePromptHours(e.target.value)}
              autoFocus
              onKeyDown={e => {
                if (e.key === 'Enter' && donePromptHours && parseFloat(donePromptHours) > 0) {
                  mutTaskStatus.mutate({ taskId: donePromptTaskId!, status: 'Done', actualHours: parseFloat(donePromptHours) })
                  setDonePromptTaskId(null)
                  setDonePromptHours('')
                }
              }}
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => { setDonePromptTaskId(null); setDonePromptHours('') }}>Cancel</Button>
            <Button
              onClick={() => {
                mutTaskStatus.mutate({ taskId: donePromptTaskId!, status: 'Done', actualHours: parseFloat(donePromptHours) })
                setDonePromptTaskId(null)
                setDonePromptHours('')
              }}
              disabled={!donePromptHours || parseFloat(donePromptHours) <= 0 || mutTaskStatus.isPending}
            >
              {mutTaskStatus.isPending ? 'Saving…' : 'Mark as Done'}
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
