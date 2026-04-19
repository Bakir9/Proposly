import { api } from './client'

export type ProjectStatus = 'Planning' | 'Active' | 'OnHold' | 'Completed' | 'Cancelled'
export type TaskStatus = 'Todo' | 'InProgress' | 'Done'

export interface ProjectSummary {
  id: string
  name: string
  clientName: string
  status: ProjectStatus
  budgetAmount: number
  currency: string
  startDate: string
  deadline: string | null
  createdAt: string
  memberCount: number
}

export interface ProjectMember {
  id: string
  userId: string
  name: string
  role: string
  hourlyRate: number
  currency: string
}

export interface ProjectTask {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  estimatedHours: number | null
  dueDate: string | null
  milestoneId: string | null
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
}

export interface CreateProjectRequest {
  name: string
  description?: string
  clientName: string
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

export const addTask = (projectId: string, data: { title: string; description?: string; estimatedHours?: number; dueDate?: string; milestoneId?: string }) =>
  api.post<string>(`/projects/${projectId}/tasks`, data).then(r => r.data)

export const updateTaskStatus = (projectId: string, taskId: string, status: string) =>
  api.put(`/projects/${projectId}/tasks/${taskId}/status`, { status })

export const addMilestone = (projectId: string, data: { title: string; dueDate: string }) =>
  api.post<string>(`/projects/${projectId}/milestones`, data).then(r => r.data)

export const completeMilestone = (projectId: string, milestoneId: string) =>
  api.put(`/projects/${projectId}/milestones/${milestoneId}/complete`, {})
