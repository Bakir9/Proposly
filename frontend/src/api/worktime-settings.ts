import { api } from './client'

export type BreachKind =
  | 'DailyMaximum'
  | 'WeeklyMaximum'
  | 'AveragingWindow'
  | 'InsufficientBreak'
  | 'InsufficientDailyRest'

export interface BreakRule {
  aboveHours: number
  minBreakMinutes: number
}

export interface PolicyVersion {
  id: string
  validFrom: string
  validTo: string | null
}

export interface WorkTimePolicy {
  id: string | null
  validFrom: string
  validTo: string | null
  jurisdiction: string
  holidayRegionCode: string | null
  maxHoursPerDay: number
  maxHoursPerWeek: number
  averagingWindowWeeks: number
  maxAverageHoursPerWeek: number
  minDailyRestHours: number
  minWeeklyRestHours: number
  surplusCapHours: number | null
  deficitFloorHours: number | null
  breakRules: BreakRule[]
  history: PolicyVersion[]
}

export interface Breach {
  kind: BreachKind
  date: string | null
  weekStartDate: string | null
  limitValue: number
  actualValue: number
  acknowledgedAt: string | null
}

export interface BreachOverview extends Breach {
  userId: string
  employeeName: string
  year: number
  month: number
  timesheetStatus: string
}

export interface CreateWorkTimePolicyRequest {
  validFrom: string
  jurisdiction: string
  holidayRegionCode?: string | null
  maxHoursPerDay: number
  maxHoursPerWeek: number
  averagingWindowWeeks: number
  maxAverageHoursPerWeek: number
  minDailyRestHours: number
  minWeeklyRestHours: number
  surplusCapHours?: number | null
  deficitFloorHours?: number | null
  breakRules: BreakRule[]
}

/** Rejects with a 404 when no rule set is configured — surface the prompt, do not assume no limits. */
export const getWorkTimePolicy = () =>
  api.get<WorkTimePolicy>('/worktime-settings/policy').then(r => r.data)

export const getPolicyDefaults = (jurisdiction: string) =>
  api
    .get<WorkTimePolicy>('/worktime-settings/policy/defaults', { params: { jurisdiction } })
    .then(r => r.data)

export const createWorkTimePolicy = (data: CreateWorkTimePolicyRequest) =>
  api.post<string>('/worktime-settings/policy', data).then(r => r.data)

export const getCompanyBreaches = (year: number, month: number) =>
  api
    .get<BreachOverview[]>(`/timesheets/company/${year}/${month}/breaches`)
    .then(r => r.data)

export const BREACH_LABELS: Record<BreachKind, string> = {
  DailyMaximum: 'Daily maximum exceeded',
  WeeklyMaximum: 'Weekly maximum exceeded',
  AveragingWindow: 'Average over the window exceeded',
  InsufficientBreak: 'Break too short',
  InsufficientDailyRest: 'Rest between days too short',
}

/** The dates a breach touches, for highlighting rows in the month grid. */
export function breachedDates(breaches: Breach[]): Set<string> {
  return new Set(breaches.map(b => b.date).filter((d): d is string => d !== null))
}

/** Hours for most kinds, minutes for the break rule. */
export function formatBreachValues(breach: Breach): string {
  const unit = breach.kind === 'InsufficientBreak' ? 'min' : 'h'
  return breach.kind === 'InsufficientBreak' || breach.kind === 'InsufficientDailyRest'
    ? `${breach.actualValue} ${unit} recorded, ${breach.limitValue} ${unit} required`
    : `${breach.actualValue} ${unit} recorded, limit ${breach.limitValue} ${unit}`
}
