import { api } from './client'

export interface CompanySettings {
  fiscalYearStartMonth: number
  companyCountry: string | null
  isVatRegistered: boolean
  companyVatNumber: string | null
  defaultVatRate: number
  isVatExempt: boolean
  vatExemptReason: string | null
}

export const getCompanySettings = () =>
  api.get<CompanySettings>('/settings/company').then(r => r.data)

export const updateCompanySettings = (data: CompanySettings) =>
  api.put('/settings/company', data)
