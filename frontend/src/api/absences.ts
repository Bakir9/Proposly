import { api } from './client'

export type AbsenceType =
  | 'Vacation'
  | 'SickLeave'
  | 'UnpaidLeave'
  | 'ParentalLeave'
  | 'SpecialLeave'

export type AbsenceStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled'

/** Type and dates only — no health detail is stored, so none can be shown. */
export interface Absence {
  id: string
  userId: string
  employeeName: string
  type: AbsenceType
  startDate: string
  endDate: string
  firstDayIsHalf: boolean
  lastDayIsHalf: boolean
  consumedDays: number
  status: AbsenceStatus
  reason: string | null
  approverName: string | null
  decidedAt: string | null
  decisionReason: string | null
}

export interface AbsenceEntitlement {
  userId: string
  year: number
  entitledDays: number
  carriedOverDays: number
  usedDays: number
  remainingDays: number
}

export interface AbsencePreview {
  consumedDays: number
  remainingDaysAfter: number | null
  exceedsEntitlement: boolean
  overlapsExisting: boolean
  nonWorkingDatesExcluded: string[]
}

export interface RequestAbsenceRequest {
  type: AbsenceType
  startDate: string
  endDate: string
  firstDayIsHalf?: boolean
  lastDayIsHalf?: boolean
  reason?: string | null
}

export const ABSENCE_TYPE_LABELS: Record<AbsenceType, string> = {
  Vacation: 'Vacation',
  SickLeave: 'Sick leave',
  UnpaidLeave: 'Unpaid leave',
  ParentalLeave: 'Parental leave',
  SpecialLeave: 'Special leave',
}

/** Only vacation draws on the annual allowance. */
export const CONSUMES_ENTITLEMENT: Record<AbsenceType, boolean> = {
  Vacation: true,
  SickLeave: false,
  UnpaidLeave: false,
  ParentalLeave: false,
  SpecialLeave: false,
}

export const getAbsences = (year: number, status?: AbsenceStatus) =>
  api.get<Absence[]>('/absences', { params: { year, status } }).then(r => r.data)

export const getPendingAbsences = () =>
  api.get<Absence[]>('/absences/pending').then(r => r.data)

/** Rejects with 404 when no entitlement has been set for the year. */
export const getEntitlement = (year: number, userId?: string) =>
  api.get<AbsenceEntitlement>('/absences/entitlement', { params: { year, userId } }).then(r => r.data)

export const previewAbsence = (
  type: AbsenceType,
  start: string,
  end: string,
  firstHalf = false,
  lastHalf = false,
) =>
  api
    .get<AbsencePreview>('/absences/preview', {
      params: { type, start, end, firstHalf, lastHalf },
    })
    .then(r => r.data)

export const requestAbsence = (data: RequestAbsenceRequest) =>
  api.post<string>('/absences', data).then(r => r.data)

export const cancelAbsence = (id: string) =>
  api.post(`/absences/${id}/cancel`)

export const approveAbsence = (id: string) =>
  api.post(`/absences/${id}/approve`)

export const rejectAbsence = (id: string, reason: string) =>
  api.post(`/absences/${id}/reject`, { reason })

export const setEntitlement = (
  userId: string,
  year: number,
  entitledDays: number,
  carriedOverDays = 0,
) => api.put(`/absences/entitlement/${userId}/${year}`, { entitledDays, carriedOverDays })
