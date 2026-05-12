import { api } from './client'

export type ProjectStatus = 'Planning' | 'Active' | 'OnHold' | 'Completed' | 'Cancelled'
export type TaskStatus = 'Todo' | 'InProgress' | 'Done'

export interface ProjectSummary {
  id: string
  name: string
  clientId: string | null
  clientName: string
  status: ProjectStatus
  budgetAmount: number
  currency: string
  startDate: string
  deadline: string | null
  createdAt: string
  memberCount: number
  completedTasksCount: number
  totalTasksCount: number
}

export interface ProjectMember {
  id: string
  userId: string
  name: string
  role: string
  hourlyRate: number
  currency: string
}

export interface TaskComment {
  id: string
  authorId: string
  authorName: string
  body: string
  createdAt: string
  updatedAt: string | null
}

export interface ProjectTask {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  estimatedHours: number | null
  actualHours: number | null
  startDate: string | null
  dueDate: string | null
  completedAt: string | null
  milestoneId: string | null
  assignedMemberId: string | null
  assignedMemberName: string | null
  comments: TaskComment[]
  blockedByTaskIds: string[]
}

export interface ProjectNote {
  id: string
  title: string
  content: string
  authorId: string
  authorName: string
  createdAt: string
  updatedAt: string
}

export interface BurndownDataPoint {
  date: string
  count: number
}

export interface BurndownData {
  totalTasks: number
  startDate: string
  endDate: string
  actual: BurndownDataPoint[]
  ideal: BurndownDataPoint[]
}

export interface VelocityWeek {
  weekLabel: string
  weekStart: string
  tasksCompleted: number
  hoursCompleted: number
}

export interface VelocityData {
  weeks: VelocityWeek[]
  totalHoursCompleted: number
  totalTasksCompleted: number
}

export interface MemberCapacity {
  memberName: string
  hoursLogged: number
}

export interface CapacityData {
  members: MemberCapacity[]
  totalHoursLogged: number
}

export interface Milestone {
  id: string
  title: string
  dueDate: string
  isCompleted: boolean
}

export interface Expense {
  id: string
  description: string
  amount: number
  currency: string
  category: string
  date: string
}

export interface TimeEntry {
  id: string
  memberId: string
  memberName: string
  hoursWorked: number
  hourlyRateSnapshot: number
  currency: string
  description: string | null
  date: string
  cost: number
}

export interface Profitability {
  laborCost: number
  expensesTotal: number
  totalCost: number
  revenue: number
  profit: number
  currency: string
}

export interface ProjectDetail extends ProjectSummary {
  description: string | null
  linkedOfferId: string | null
  offeredAmount: number | null
  profitability: Profitability
  members: ProjectMember[]
  tasks: ProjectTask[]
  milestones: Milestone[]
  expenses: Expense[]
  timeEntries: TimeEntry[]
  notes: ProjectNote[]
}

export interface CreateProjectRequest {
  name: string
  description?: string
  clientId: string
  budgetAmount: number
  currency: string
  startDate: string
  deadline?: string
}

export interface UpdateProjectRequest {
  name: string
  description?: string
  deadline?: string
  status?: string
  budgetAmount?: number
  budgetCurrency?: string
  clientId?: string
}

export const getProjects = () =>
  api.get<ProjectSummary[]>('/projects').then(r => r.data)

export const getProjectById = (id: string) =>
  api.get<ProjectDetail>(`/projects/${id}`).then(r => r.data)

export const createProject = (data: CreateProjectRequest) =>
  api.post<string>('/projects', data).then(r => r.data)

export const updateProject = (id: string, data: UpdateProjectRequest) =>
  api.put(`/projects/${id}`, data)

export const addProjectMember = (projectId: string, data: { userId: string; name: string; role: string; hourlyRate: number; currency: string }) =>
  api.post<string>(`/projects/${projectId}/members`, data).then(r => r.data)

export const logTime = (projectId: string, data: { memberId: string; hoursWorked: number; description?: string; date: string }) =>
  api.post<string>(`/projects/${projectId}/time`, data).then(r => r.data)

export const addExpense = (projectId: string, data: { description: string; amount: number; currency: string; category: string; date: string }) =>
  api.post<string>(`/projects/${projectId}/expenses`, data).then(r => r.data)

export const addTask = (projectId: string, data: { title: string; description?: string; estimatedHours?: number; startDate?: string; dueDate?: string; milestoneId?: string; assignedMemberId?: string }) =>
  api.post<string>(`/projects/${projectId}/tasks`, data).then(r => r.data)

export const updateTask = (projectId: string, taskId: string, data: { title: string; description?: string; estimatedHours?: number; startDate?: string; dueDate?: string; milestoneId?: string; assignedMemberId?: string }) =>
  api.put(`/projects/${projectId}/tasks/${taskId}`, data)

export const updateTaskStatus = (projectId: string, taskId: string, status: string, actualHours?: number) =>
  api.put(`/projects/${projectId}/tasks/${taskId}/status`, { status, actualHours })

export const addMilestone = (projectId: string, data: { title: string; dueDate: string }) =>
  api.post<string>(`/projects/${projectId}/milestones`, data).then(r => r.data)

export const completeMilestone = (projectId: string, milestoneId: string) =>
  api.put(`/projects/${projectId}/milestones/${milestoneId}/complete`, {})

export const addTaskComment = (projectId: string, taskId: string, body: string) =>
  api.post<string>(`/projects/${projectId}/tasks/${taskId}/comments`, { body }).then(r => r.data)

export const editTaskComment = (projectId: string, taskId: string, commentId: string, body: string) =>
  api.put(`/projects/${projectId}/tasks/${taskId}/comments/${commentId}`, { body })

export const deleteTaskComment = (projectId: string, taskId: string, commentId: string) =>
  api.delete(`/projects/${projectId}/tasks/${taskId}/comments/${commentId}`)

export const getBurndown = (projectId: string) =>
  api.get<BurndownData>(`/projects/${projectId}/burndown`).then(r => r.data)

export const getVelocity = (projectId: string) =>
  api.get<VelocityData>(`/projects/${projectId}/velocity`).then(r => r.data)

export const getCapacity = (projectId: string) =>
  api.get<CapacityData>(`/projects/${projectId}/capacity`).then(r => r.data)

export const addTaskDependency = (projectId: string, taskId: string, blockingTaskId: string) =>
  api.post(`/projects/${projectId}/tasks/${taskId}/dependencies`, { blockingTaskId })

export const removeTaskDependency = (projectId: string, taskId: string, blockingTaskId: string) =>
  api.delete(`/projects/${projectId}/tasks/${taskId}/dependencies/${blockingTaskId}`)

export const addNote = (projectId: string, data: { title: string; content: string }) =>
  api.post<string>(`/projects/${projectId}/notes`, data).then(r => r.data)

export const updateNote = (projectId: string, noteId: string, data: { title: string; content: string }) =>
  api.put(`/projects/${projectId}/notes/${noteId}`, data)

export const deleteNote = (projectId: string, noteId: string) =>
  api.delete(`/projects/${projectId}/notes/${noteId}`)
