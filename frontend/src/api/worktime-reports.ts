import { api } from './client'
import type { TimesheetStatus, WorkDay } from './timesheets'
import type { Breach } from './worktime-settings'
import type { AbsenceType } from './absences'

export interface AbsenceDaysByType {
  type: AbsenceType
  days: number
}

export interface MonthlyWorkTimeReport {
  userId: string
  employeeName: string
  year: number
  month: number
  status: TimesheetStatus
  /** True while the month is not yet approved — figures are live, not final. */
  isProvisional: boolean
  /** True when an input changed after the month was reported. */
  isRevised: boolean
  isSelfApproved: boolean

  /** Null when no employment terms cover the month — unavailable, not zero. */
  targetHours: number | null
  actualHours: number
  monthlyDifference: number | null

  absenceDays: AbsenceDaysByType[]

  openingBalanceHours: number
  closingBalanceHours: number
  /** Surplus lost to the cap. Always shown, never silent. */
  forfeitedHours: number
  surplusCapHours: number | null
  deficitFloorHours: number | null
  deficitFloorBreached: boolean
  approachingCap: boolean

  breaches: Breach[]
  days: WorkDay[]
}

export interface CompanyMonthOverviewRow {
  userId: string
  employeeName: string
  status: TimesheetStatus
  targetHours: number | null
  actualHours: number
  monthlyDifference: number | null
  closingBalanceHours: number
  forfeitedHours: number
  breachCount: number
  isProvisional: boolean
  isRevised: boolean
}

export interface ProjectBooking {
  projectId: string
  projectName: string
  bookedHours: number
}

export interface Reconciliation {
  userId: string
  employeeName: string
  year: number
  month: number
  recordedWorkingHours: number
  projectBookedHours: number
  /** Recorded but never booked to a project — work that was never costed. */
  unbookedHours: number
  /** Booked exceeds recorded. Surfaced for review, not an error. */
  overBooked: boolean
  byProject: ProjectBooking[]
  unattributedBookedHours: number
}

export const getMonthlyReport = (year: number, month: number, userId?: string) =>
  api
    .get<MonthlyWorkTimeReport>(`/worktime-reports/monthly/${year}/${month}`, { params: { userId } })
    .then(r => r.data)

export const getCompanyMonthOverview = (year: number, month: number) =>
  api
    .get<CompanyMonthOverviewRow[]>(`/worktime-reports/company/${year}/${month}`)
    .then(r => r.data)

export const getReconciliation = (year: number, month: number, userId?: string) =>
  api
    .get<Reconciliation>(`/worktime-reports/reconciliation/${year}/${month}`, { params: { userId } })
    .then(r => r.data)

/** Downloads the month-end document. A provisional month arrives watermarked. */
export async function downloadMonthlyReportPdf(year: number, month: number, userId?: string) {
  const response = await api.get(`/worktime-reports/monthly/${year}/${month}/pdf`, {
    params: { userId },
    responseType: 'blob',
  })

  const disposition = response.headers['content-disposition'] as string | undefined
  const match = disposition?.match(/filename="?([^";]+)"?/i)
  const fileName = match?.[1] ?? `worktime-${year}-${String(month).padStart(2, '0')}.pdf`

  const url = URL.createObjectURL(response.data as Blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

/** Hours with a sign, so a surplus and a deficit are never confused. */
export function signedHours(hours: number): string {
  return `${hours >= 0 ? '+' : ''}${hours.toFixed(2)} h`
}
