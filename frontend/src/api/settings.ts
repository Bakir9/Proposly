import { api } from './client'

export type PlanTier = 'Free' | 'Starter' | 'Pro' | 'Business'

export interface CompanySettings {
  fiscalYearStartMonth: number
  companyEmail: string | null
  companyPhone: string | null
  companyStreet: string | null
  companyCity: string | null
  companyPostalCode: string | null
  companyCountry: string | null
  isVatRegistered: boolean
  companyVatNumber: string | null
  defaultVatRate: number
  isVatExempt: boolean
  vatExemptReason: string | null
  planTier: PlanTier
  maxUsers: number | null
  maxProjects: number | null
  planExpiresAt: string | null
  currentUserCount: number
  currentProjectCount: number
}

export const getCompanySettings = () =>
  api.get<CompanySettings>('/settings/company').then(r => r.data)

export const updateCompanySettings = (data: Omit<CompanySettings, 'planTier' | 'maxUsers' | 'maxProjects' | 'planExpiresAt' | 'currentUserCount' | 'currentProjectCount'>) =>
  api.put('/settings/company', data)
