import { api } from './client'

export type ProjectStatus = 'Planning' | 'Active' | 'OnHold' | 'Completed' | 'Cancelled'

export interface ProjectSummary {
  id: string
  name: string
  clientName: string
  status: ProjectStatus
  budgetAmount: number
  currency: string
  startDate: string
  deadline: string | null
  memberCount: number
}

export interface ProjectDetail extends ProjectSummary {
  description: string | null
  linkedOfferId: string | null
  offeredAmount: number | null
  profitability: {
    laborCost: number
    expensesTotal: number
    totalCost: number
    revenue: number
    profit: number
    currency: string
  }
  members: {
    id: string
    userId: string
    name: string
    role: string
    hourlyRate: number
    currency: string
  }[]
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

export const getProjects = () =>
  api.get<ProjectSummary[]>('/projects').then(r => r.data)

export const getProjectById = (id: string) =>
  api.get<ProjectDetail>(`/projects/${id}`).then(r => r.data)

export const createProject = (data: CreateProjectRequest) =>
  api.post<string>('/projects', data).then(r => r.data)
