import { api } from './client'
import type { Breach } from './worktime-settings'

export type TimesheetStatus = 'Draft' | 'Submitted' | 'Approved' | 'Locked'

export interface WorkDay {
  id: string
  date: string
  startTime: string
  endTime: string
  breakMinutes: number
  crossesMidnight: boolean
  workedHours: number
  note: string | null
}

export interface TimesheetDetail {
  /** Null until the first day is recorded — reading a month does not create it. */
  id: string | null
  userId: string
  employeeName: string
  year: number
  month: number
  status: TimesheetStatus
  totalWorkedHours: number
  isSelfApproved: boolean
  submittedAt: string | null
  approvedAt: string | null
  approvedByName: string | null
  reopenedAt: string | null
  days: WorkDay[]
  /** Computed live while the month is open; acknowledged at approval once it is closed. */
  breaches: Breach[]
}

export interface TimesheetSummary {
  id: string
  userId: string
  employeeName: string
  year: number
  month: number
  status: TimesheetStatus
  totalWorkedHours: number
  isSelfApproved: boolean
  submittedAt: string | null
  approvedAt: string | null
  breachCount: number
}

export interface UpsertWorkDayRequest {
  startTime: string
  endTime: string
  breakMinutes: number
  crossesMidnight?: boolean
  note?: string | null
}

export const getMyTimesheet = (year: number, month: number) =>
  api.get<TimesheetDetail>(`/timesheets/${year}/${month}`).then(r => r.data)

export const upsertWorkDay = (
  year: number,
  month: number,
  date: string,
  data: UpsertWorkDayRequest,
) => api.put(`/timesheets/${year}/${month}/days/${date}`, data)

export const deleteWorkDay = (year: number, month: number, date: string) =>
  api.delete(`/timesheets/${year}/${month}/days/${date}`)

export const submitTimesheet = (year: number, month: number) =>
  api.post(`/timesheets/${year}/${month}/submit`)

export const getPendingTimesheets = () =>
  api.get<TimesheetSummary[]>('/timesheets/pending').then(r => r.data)

/** A month with outstanding breaches is refused unless they are acknowledged. */
export const approveTimesheet = (id: string, acknowledgeBreaches = false) =>
  api.post(`/timesheets/${id}/approve`, { acknowledgeBreaches })

export const returnTimesheet = (id: string, reason?: string) =>
  api.post(`/timesheets/${id}/return`, { reason: reason ?? null })

export const lockTimesheet = (id: string) =>
  api.post(`/timesheets/${id}/lock`)

export const reopenTimesheet = (id: string) =>
  api.post(`/timesheets/${id}/reopen`)

export const getCompanyMonth = (year: number, month: number) =>
  api.get<TimesheetSummary[]>(`/timesheets/company/${year}/${month}`).then(r => r.data)
