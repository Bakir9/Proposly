import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getProjectById,
  updateProject,
  addTask,
  updateTaskStatus,
  addProjectMember,
  logTime,
  addExpense,
  addMilestone,
  completeMilestone,
} from '@/api/projects'
import { getUsers } from '@/api/users'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Tabs } from '@/components/ui/tabs'
import { Dialog } from '@/components/ui/dialog'
import { Textarea } from '@/components/ui/textarea'

const taskStatusVariant: Record<string, 'default' | 'secondary' | 'success' | 'outline'> = {
  Todo: 'secondary',
  InProgress: 'default',
  Done: 'success',
}

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
  { id: 'team', label: 'Team' },
  { id: 'timelog', label: 'Time Log' },
  { id: 'expenses', label: 'Expenses' },
  { id: 'milestones', label: 'Milestones' },
]

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: project, isLoading, isError } = useQuery({
    queryKey: ['project', id],
    queryFn: () => getProjectById(id!),
  })

  const { data: users } = useQuery({ queryKey: ['users'], queryFn: getUsers })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['project', id] })

  // Task dialog
  const [taskOpen, setTaskOpen] = useState(false)
  const [taskTitle, setTaskTitle] = useState('')
  const [taskDesc, setTaskDesc] = useState('')
  const [taskHours, setTaskHours] = useState('')
  const [taskDue, setTaskDue] = useState('')
  const [taskMilestoneId, setTaskMilestoneId] = useState('')

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

  const mutAddTask = useMutation({
    mutationFn: () => addTask(id!, {
      title: taskTitle,
      description: taskDesc || undefined,
      estimatedHours: taskHours ? parseFloat(taskHours) : undefined,
      dueDate: taskDue || undefined,
      milestoneId: taskMilestoneId || undefined,
    }),
    onSuccess: () => {
      invalidate()
      setTaskOpen(false)
      setTaskTitle('')
      setTaskDesc('')
      setTaskHours('')
      setTaskDue('')
      setTaskMilestoneId('')
    },
  })

  const mutTaskStatus = useMutation({
    mutationFn: ({ taskId, status }: { taskId: string; status: string }) => updateTaskStatus(id!, taskId, status),
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

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>← Back</Button>
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
          }}>Edit</Button>
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

            {activeTab === 'tasks' && (
              <div className="space-y-3">
                <div className="flex justify-end">
                  <Button size="sm" onClick={() => setTaskOpen(true)}>+ Add Task</Button>
                </div>
                {p.tasks.length === 0 && <p className="text-sm text-muted-foreground">No tasks yet.</p>}
                {p.tasks.map(task => (
                  <Card key={task.id}>
                    <CardContent className="py-3 flex items-center justify-between gap-4">
                      <div className="flex-1">
                        <p className="font-medium text-sm">{task.title}</p>
                        {task.description && <p className="text-xs text-muted-foreground">{task.description}</p>}
                        <div className="flex gap-3 mt-1 text-xs text-muted-foreground">
                          {task.estimatedHours && <span>{task.estimatedHours}h estimated</span>}
                          {task.dueDate && <span>Due {new Date(task.dueDate).toLocaleDateString()}</span>}
                        </div>
                      </div>
                      <Select
                        className="w-32"
                        value={task.status}
                        onChange={e => mutTaskStatus.mutate({ taskId: task.id, status: e.target.value })}
                      >
                        <option value="Todo">Todo</option>
                        <option value="InProgress">In Progress</option>
                        <option value="Done">Done</option>
                      </Select>
                      <Badge variant={taskStatusVariant[task.status] ?? 'outline'}>{task.status}</Badge>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}

            {activeTab === 'team' && (
              <div className="space-y-3">
                <div className="flex justify-end">
                  <Button size="sm" onClick={() => setMemberOpen(true)}>+ Add Member</Button>
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
                  <Button size="sm" onClick={() => setTimeOpen(true)}>+ Log Time</Button>
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
                  <Button size="sm" onClick={() => setExpenseOpen(true)}>+ Add Expense</Button>
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
                  <Button size="sm" onClick={() => setMilestoneOpen(true)}>+ Add Milestone</Button>
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
                            Mark Complete
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

      <Dialog open={taskOpen} onClose={() => setTaskOpen(false)} title="Add Task">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Title</Label>
            <Input value={taskTitle} onChange={e => setTaskTitle(e.target.value)} placeholder="Task title" />
          </div>
          <div className="space-y-1">
            <Label>Description (optional)</Label>
            <Textarea value={taskDesc} onChange={e => setTaskDesc(e.target.value)} placeholder="What needs to be done?" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Estimated Hours (optional)</Label>
              <Input type="number" step="0.5" value={taskHours} onChange={e => setTaskHours(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Due Date (optional)</Label>
              <Input type="date" value={taskDue} onChange={e => setTaskDue(e.target.value)} />
            </div>
          </div>
          {p.milestones.length > 0 && (
            <div className="space-y-1">
              <Label>Milestone (optional)</Label>
              <Select value={taskMilestoneId} onChange={e => setTaskMilestoneId(e.target.value)}>
                <option value="">No milestone</option>
                {p.milestones.map(m => (
                  <option key={m.id} value={m.id}>{m.title}</option>
                ))}
              </Select>
            </div>
          )}
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setTaskOpen(false)}>Cancel</Button>
            <Button onClick={() => mutAddTask.mutate()} disabled={mutAddTask.isPending || !taskTitle.trim()}>
              {mutAddTask.isPending ? 'Adding…' : 'Add Task'}
            </Button>
          </div>
        </div>
      </Dialog>

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
