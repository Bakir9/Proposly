import { api } from './client'

/* Typed client for HrController (Owner/Admin only). */

export type AccountStatus = 'ProfileOnly' | 'Invited' | 'Active' | 'Disabled'
export type ApiEmploymentType =
  | 'FullTime' | 'PartTime' | 'MarginalEmployment' | 'Apprentice' | 'Freelancer' | 'Intern' | 'Other'
export type ApiWorkModel = 'FullTime' | 'PartTime' | 'Hourly'
export type ApiOvertimeMode = 'PaidOut' | 'TimeOff' | 'AllIn'
export type ApiAbsenceType = 'Vacation' | 'SickLeave' | 'UnpaidLeave' | 'ParentalLeave' | 'SpecialLeave' | 'TimeOffInLieu'
export type WorkerFileKind = 'Photo' | 'Cv' | 'Document'

export interface WorkerListItem {
  userId: string
  firstName: string
  lastName: string
  jobTitle: string | null
  employmentType: ApiEmploymentType
  workModel: ApiWorkModel
  weeklyHours: number
  accountStatus: AccountStatus
  hasPhoto: boolean
}

export interface WorkerAllowanceApi {
  name: string
  amount: number
  currency: string
  unit: string
}

export interface WorkerNoteApi {
  id: string
  tag: string
  text: string
  date: string
  pinned: boolean
}

export interface WorkerDocumentApi {
  id: string
  category: string
  fileName: string
  sizeBytes: number
  uploadedAt: string
}

export interface WorkerAbsenceApi {
  id: string
  type: ApiAbsenceType
  from: string
  to: string
  days: number
  status: 'Pending' | 'Approved' | 'Rejected'
}

export interface WorkerDetail {
  userId: string
  firstName: string
  lastName: string
  email: string | null
  role: string
  accountStatus: AccountStatus
  jobTitle: string | null
  phone: string | null
  gender: string | null
  birthday: string | null
  svnr: string | null
  nationality: string | null
  department: string | null
  location: string | null
  address: string | null
  emergencyName: string | null
  emergencyRelation: string | null
  emergencyPhone: string | null
  employmentStart: string | null
  employmentEnd: string | null
  hasPhoto: boolean
  cvJson: string | null
  cvFileName: string | null
  cvFileSize: number | null
  cvFileUploadedAt: string | null
  employmentType: ApiEmploymentType
  workModel: ApiWorkModel
  weeklyHours: number
  dayHours: (number | null)[]
  hourlyRateAmount: number | null
  hourlyRateCurrency: string | null
  maxHoursPerMonth: number | null
  expectedWeeklyHours: number | null
  overtimeMode: ApiOvertimeMode
  overtimeSurchargePct: number | null
  nightSurchargePct: number | null
  sundaySurchargePct: number | null
  holidaySurchargePct: number | null
  trackingRequired: boolean
  autoDeductBreak: boolean
  flexitime: boolean
  onCallDuty: boolean
  coreFrom: string | null
  coreTo: string | null
  homeOfficeDaysPerWeek: number | null
  maxHoursPerDay: number | null
  annualVacationDays: number
  carriedOverDays: number
  absences: WorkerAbsenceApi[]
  allowances: WorkerAllowanceApi[]
  notes: WorkerNoteApi[]
  documents: WorkerDocumentApi[]
}

export interface UpdateWorkerPayload {
  userId: string
  firstName: string
  lastName: string
  jobTitle: string | null
  phone: string | null
  gender: string | null
  birthday: string | null
  svnr: string | null
  nationality: string | null
  department: string | null
  location: string | null
  address: string | null
  emergencyName: string | null
  emergencyRelation: string | null
  emergencyPhone: string | null
  employmentStart: string | null
  employmentEnd: string | null
  cvJson: string | null
  employmentType: ApiEmploymentType
  workModel: ApiWorkModel
  dayHours: (number | null)[]
  hourlyRateAmount: number | null
  hourlyRateCurrency: string | null
  maxHoursPerMonth: number | null
  expectedWeeklyHours: number | null
  overtimeMode: ApiOvertimeMode
  overtimeSurchargePct: number | null
  nightSurchargePct: number | null
  sundaySurchargePct: number | null
  holidaySurchargePct: number | null
  trackingRequired: boolean
  autoDeductBreak: boolean
  flexitime: boolean
  onCallDuty: boolean
  coreFrom: string | null
  coreTo: string | null
  homeOfficeDaysPerWeek: number | null
  maxHoursPerDay: number | null
  annualVacationDays: number
  carriedOverDays: number
  allowances: WorkerAllowanceApi[]
}

export const getWorkers = () =>
  api.get<WorkerListItem[]>('/hr/workers').then(r => r.data)

export const getWorker = (userId: string) =>
  api.get<WorkerDetail>(`/hr/workers/${userId}`).then(r => r.data)

export const addWorker = (data: { firstName: string; lastName: string; jobTitle?: string | null }) =>
  api.post<string>('/hr/workers', data).then(r => r.data)

export const inviteWorker = (userId: string, email: string, role: string) =>
  api.post(`/hr/workers/${userId}/invite`, { email, role })

export const updateWorker = (payload: UpdateWorkerPayload) =>
  api.put(`/hr/workers/${payload.userId}`, payload)

export const addWorkerAbsence = (userId: string, type: ApiAbsenceType, startDate: string, endDate: string) =>
  api.post<string>(`/hr/workers/${userId}/absences`, { type, startDate, endDate }).then(r => r.data)

export const addWorkerNote = (userId: string, tag: string, text: string) =>
  api.post<WorkerNoteApi>(`/hr/workers/${userId}/notes`, { tag, text }).then(r => r.data)

export const pinWorkerNote = (userId: string, noteId: string, pinned: boolean) =>
  api.put(`/hr/workers/${userId}/notes/${noteId}/pin`, { pinned })

export const deleteWorkerNote = (userId: string, noteId: string) =>
  api.delete(`/hr/workers/${userId}/notes/${noteId}`)

export const setWorkerDocumentCategory = (userId: string, documentId: string, category: string) =>
  api.put(`/hr/workers/${userId}/documents/${documentId}/category`, { category })

export const uploadWorkerFile = (
  userId: string,
  kind: WorkerFileKind,
  file: File,
  category?: string,
) => {
  const form = new FormData()
  form.append('file', file)
  form.append('kind', kind)
  if (category) form.append('category', category)
  return api
    .post<WorkerDocumentApi | null>(`/hr/workers/${userId}/files`, form)
    .then(r => r.data)
}

export const deleteWorkerFile = (userId: string, kind: WorkerFileKind, documentId?: string) =>
  api.delete(`/hr/workers/${userId}/files/${kind}`, { params: { documentId } })

/** Files sit behind the JWT, so downloads go through axios as blobs. */
export const fetchWorkerFileBlob = (userId: string, kind: WorkerFileKind, documentId?: string) =>
  api
    .get<Blob>(`/hr/workers/${userId}/files/${kind}`, { params: { documentId }, responseType: 'blob' })
    .then(r => r.data)

// Absence approve/cancel reuse the existing endpoints — see api/absences.ts.
export { approveAbsence, cancelAbsence } from './absences'
