import { api } from './client'

export interface CompanyAdminSummary {
  id: string
  name: string
  status: string
  planTier: string
  maxUsers: number | null
  maxProjects: number | null
  planExpiresAt: string | null
  userCount: number
  projectCount: number
  createdAt: string
}

export interface AdminUpdatePlanRequest {
  planTier: string
  maxUsers: number | null
  maxProjects: number | null
  planExpiresAt: string | null
}

export const getAllCompanies = () =>
  api.get<CompanyAdminSummary[]>('/admin/companies').then(r => r.data)

export const adminUpdateCompanyPlan = (companyId: string, data: AdminUpdatePlanRequest) =>
  api.put(`/admin/companies/${companyId}/plan`, data)
