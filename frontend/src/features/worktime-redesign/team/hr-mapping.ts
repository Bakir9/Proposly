import type {
  ApiAbsenceType,
  ApiEmploymentType,
  ApiOvertimeMode,
  ApiWorkModel,
  UpdateWorkerPayload,
  WorkerDetail,
} from '@/api/hr'
import type {
  AbsenceKind,
  CvEntry,
  CvFile,
  Language,
  OvertimeMode,
  ScheduleDay,
  Worker,
  WorkerDraft,
  WorkerType,
  WorkModel,
} from './types'

/* Translation between the API contract (api/hr.ts) and the page's view model (types.ts). */

const DOW = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So']

interface CvData {
  exp: CvEntry[]
  edu: CvEntry[]
  skills: string[]
  langs: Language[]
}

const typeFromApi = (t: ApiEmploymentType): WorkerType =>
  t === 'Freelancer' ? 'Freelancer'
  : t === 'Intern' ? 'Intern'
  : t === 'Apprentice' ? 'Apprentice'
  : t === 'MarginalEmployment' ? 'Marginal'
  : 'Employee'

const typeToApi = (t: WorkerType, model: WorkModel): ApiEmploymentType =>
  t === 'Freelancer' ? 'Freelancer'
  : t === 'Intern' ? 'Intern'
  : t === 'Apprentice' ? 'Apprentice'
  : t === 'Marginal' ? 'MarginalEmployment'
  : model === 'Part-time' ? 'PartTime'
  : 'FullTime'

const modelFromApi = (m: ApiWorkModel): WorkModel =>
  m === 'Hourly' ? 'Hourly' : m === 'PartTime' ? 'Part-time' : 'Full-time'

const modelToApi = (m: WorkModel): ApiWorkModel =>
  m === 'Hourly' ? 'Hourly' : m === 'Part-time' ? 'PartTime' : 'FullTime'

const otFromApi = (o: ApiOvertimeMode): OvertimeMode =>
  o === 'PaidOut' ? 'Paid out' : o === 'AllIn' ? 'All-in' : 'Time off'

const otToApi = (o: OvertimeMode): ApiOvertimeMode =>
  o === 'Paid out' ? 'PaidOut' : o === 'All-in' ? 'AllIn' : 'TimeOff'

export const absenceKindFromApi = (t: ApiAbsenceType): AbsenceKind =>
  t === 'SickLeave' ? 'Sick leave'
  : t === 'SpecialLeave' ? 'Special leave'
  : t === 'UnpaidLeave' ? 'Unpaid leave'
  : t === 'TimeOffInLieu' ? 'Time off in lieu'
  : t === 'ParentalLeave' ? 'Parental leave'
  : 'Vacation'

export const absenceKindToApi = (k: AbsenceKind): ApiAbsenceType =>
  k === 'Sick leave' ? 'SickLeave'
  : k === 'Special leave' ? 'SpecialLeave'
  : k === 'Unpaid leave' ? 'UnpaidLeave'
  : k === 'Time off in lieu' ? 'TimeOffInLieu'
  : k === 'Parental leave' ? 'ParentalLeave'
  : 'Vacation'

const daysFromApi = (dayHours: (number | null)[]): ScheduleDay[] =>
  DOW.map((d, i) => ({ d, on: dayHours[i] != null, h: dayHours[i] ?? 0 }))

const timeFromApi = (t: string | null) => (t ? t.slice(0, 5) : '')

function parseCv(json: string | null): CvData {
  if (!json) return { exp: [], edu: [], skills: [], langs: [] }
  try {
    const parsed = JSON.parse(json) as Partial<CvData>
    return {
      exp: parsed.exp ?? [],
      edu: parsed.edu ?? [],
      skills: parsed.skills ?? [],
      langs: parsed.langs ?? [],
    }
  } catch {
    return { exp: [], edu: [], skills: [], langs: [] }
  }
}

export function draftFromApi(w: WorkerDetail): WorkerDraft {
  const cv = parseCv(w.cvJson)
  return {
    name: `${w.firstName} ${w.lastName}`.trim(),
    role: w.jobTitle ?? '',
    type: typeFromApi(w.employmentType),
    model: modelFromApi(w.workModel),
    days: daysFromApi(w.dayHours),
    start: w.employmentStart ?? '',
    end: w.employmentEnd ?? '',
    rate: w.hourlyRateAmount ?? '',
    maxMonth: w.maxHoursPerMonth ?? '',
    expectedWeek: w.expectedWeeklyHours ?? '',
    vacEnt: w.annualVacationDays,
    vacCarry: w.carriedOverDays,
    ot: otFromApi(w.overtimeMode),
    otPct: w.overtimeSurchargePct ?? '',
    night: w.nightSurchargePct ?? '',
    sunday: w.sundaySurchargePct ?? '',
    holiday: w.holidaySurchargePct ?? '',
    flex: w.flexitime,
    coreFrom: timeFromApi(w.coreFrom),
    coreTo: timeFromApi(w.coreTo),
    autoBreak: w.autoDeductBreak,
    tracking: w.trackingRequired,
    onCall: w.onCallDuty,
    homeOffice: w.homeOfficeDaysPerWeek ?? '',
    maxDay: w.maxHoursPerDay ?? '',
    allowances: w.allowances.map(a => ({
      name: a.name,
      amount: a.amount,
      unit: a.unit as Worker['allowances'][number]['unit'],
    })),
    phone: w.phone ?? '',
    birthday: w.birthday ?? '',
    svnr: w.svnr ?? '',
    nationality: w.nationality ?? '',
    department: w.department ?? '',
    location: w.location ?? '',
    address: w.address ?? '',
    gender: w.gender ?? '',
    emName: w.emergencyName ?? '',
    emRelation: w.emergencyRelation ?? '',
    emPhone: w.emergencyPhone ?? '',
    cvExp: cv.exp,
    cvEdu: cv.edu,
    skills: cv.skills,
    langs: cv.langs,
  }
}

/** Server-managed parts (files, lists, account state) merged onto a draft for rendering. */
export function mergeWorkerView(w: WorkerDetail, draft: WorkerDraft, photoUrl: string): Worker {
  const cvFile: CvFile | null = w.cvFileName
    ? { name: w.cvFileName, size: w.cvFileSize ?? 0, date: w.cvFileUploadedAt ?? '' }
    : null
  return {
    ...draft,
    userId: w.userId,
    email: w.email,
    accountStatus: w.accountStatus,
    permissionRole: w.role,
    photo: photoUrl,
    cvFile,
    absences: w.absences.map(a => ({
      id: a.id,
      kind: absenceKindFromApi(a.type),
      from: a.from,
      to: a.to,
      days: a.days,
      status: a.status,
    })),
    notes: w.notes.map(n => ({ id: n.id, tag: n.tag as Worker['notes'][number]['tag'], text: n.text, date: n.date, pinned: n.pinned })),
    docs: w.documents.map(d => ({ id: d.id, category: d.category, name: d.fileName, size: d.sizeBytes, date: d.uploadedAt })),
  }
}

const num = (v: number | string): number | null => {
  if (v === '' || v === null || v === undefined) return null
  const n = Number(v)
  return isFinite(n) ? n : null
}

export function payloadFromDraft(userId: string, d: WorkerDraft): UpdateWorkerPayload {
  const parts = d.name.trim().split(/\s+/)
  const firstName = parts[0] ?? ''
  const lastName = parts.slice(1).join(' ')
  const cvJson = JSON.stringify({ exp: d.cvExp, edu: d.cvEdu, skills: d.skills, langs: d.langs })

  return {
    userId,
    firstName,
    lastName: lastName || firstName, // the API requires both; single-name workers repeat it
    jobTitle: d.role.trim() || null,
    phone: d.phone || null,
    gender: d.gender || null,
    birthday: d.birthday || null,
    svnr: d.svnr || null,
    nationality: d.nationality || null,
    department: d.department || null,
    location: d.location || null,
    address: d.address || null,
    emergencyName: d.emName || null,
    emergencyRelation: d.emRelation || null,
    emergencyPhone: d.emPhone || null,
    employmentStart: d.start || null,
    employmentEnd: d.end || null,
    cvJson,
    employmentType: typeToApi(d.type, d.model),
    workModel: modelToApi(d.model),
    dayHours: d.days.map(day => (day.on && Number(day.h) > 0 ? Number(day.h) : null)),
    hourlyRateAmount: num(d.rate),
    hourlyRateCurrency: num(d.rate) != null ? 'EUR' : null,
    maxHoursPerMonth: num(d.maxMonth),
    expectedWeeklyHours: num(d.expectedWeek),
    overtimeMode: otToApi(d.ot),
    overtimeSurchargePct: num(d.otPct),
    nightSurchargePct: num(d.night),
    sundaySurchargePct: num(d.sunday),
    holidaySurchargePct: num(d.holiday),
    trackingRequired: d.tracking,
    autoDeductBreak: d.autoBreak,
    flexitime: d.flex,
    onCallDuty: d.onCall,
    coreFrom: d.flex && d.coreFrom ? d.coreFrom : null,
    coreTo: d.flex && d.coreTo ? d.coreTo : null,
    homeOfficeDaysPerWeek: num(d.homeOffice),
    maxHoursPerDay: num(d.maxDay),
    annualVacationDays: num(d.vacEnt) ?? 0,
    carriedOverDays: num(d.vacCarry) ?? 0,
    allowances: d.allowances
      .filter(a => a.name.trim())
      .map(a => ({ name: a.name.trim(), amount: num(a.amount) ?? 0, currency: 'EUR', unit: a.unit })),
  }
}
