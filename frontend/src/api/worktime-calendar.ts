import { api } from './client'

export type NonWorkingDayKind = 'PublicHoliday' | 'CompanyClosure'
export type EntrySource = 'Manual' | 'Imported'

export type WeekDayName =
  | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday'

export interface NonWorkingDay {
  id: string
  date: string
  name: string
  kind: NonWorkingDayKind
  /** Always false for a public holiday; true by default for a company closure. */
  consumesVacation: boolean
  source: EntrySource
}

export interface EmploymentTerms {
  id: string
  userId: string
  validFrom: string
  validTo: string | null
  weeklyHours: number
  workingDays: WeekDayName[]
  dailyHours: number
  annualVacationDays: number
}

export interface TargetHours {
  userId: string
  year: number
  month: number
  workingDays: number
  nonWorkingDaysExcluded: number
  /** Null when no employment terms cover the month — unavailable, not zero. */
  targetHours: number | null
}

export interface CreateEmploymentTermsRequest {
  validFrom: string
  weeklyHours: number
  workingDays: WeekDayName[]
  annualVacationDays: number
}

export interface CreateNonWorkingDayRequest {
  date: string
  name: string
  kind: NonWorkingDayKind
  consumesVacation?: boolean | null
}

export const WEEK_DAYS: WeekDayName[] = [
  'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday',
]

export const KIND_LABELS: Record<NonWorkingDayKind, string> = {
  PublicHoliday: 'Public holiday',
  CompanyClosure: 'Company closure',
}

// --- Employment terms ---

export const getMyEmploymentTerms = () =>
  api.get<EmploymentTerms[]>('/worktime-settings/terms/mine').then(r => r.data)

export const getEmploymentTerms = (userId: string) =>
  api.get<EmploymentTerms[]>(`/worktime-settings/terms/${userId}`).then(r => r.data)

export const createEmploymentTerms = (userId: string, data: CreateEmploymentTermsRequest) =>
  api.post<string>(`/worktime-settings/terms/${userId}`, data).then(r => r.data)

export const getTargetHours = (userId: string, year: number, month: number) =>
  api
    .get<TargetHours>(`/worktime-settings/terms/${userId}/target`, { params: { year, month } })
    .then(r => r.data)

export const getMyTargetHours = (year: number, month: number) =>
  api
    .get<TargetHours>('/worktime-settings/terms/mine/target', { params: { year, month } })
    .then(r => r.data)

// --- Holiday and closure calendar ---

export const getNonWorkingDays = (from: string, to: string) =>
  api
    .get<NonWorkingDay[]>('/worktime-settings/non-working-days', { params: { from, to } })
    .then(r => r.data)

export const createNonWorkingDay = (data: CreateNonWorkingDayRequest) =>
  api.post<string>('/worktime-settings/non-working-days', data).then(r => r.data)

export const updateNonWorkingDay = (
  id: string,
  data: { name: string; kind: NonWorkingDayKind; consumesVacation: boolean },
) => api.put(`/worktime-settings/non-working-days/${id}`, data)

export const deleteNonWorkingDay = (id: string) =>
  api.delete(`/worktime-settings/non-working-days/${id}`)
