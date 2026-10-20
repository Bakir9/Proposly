import type { AccountStatus } from '@/api/hr'

/* Local view model of the Team page. Server data (api/hr.ts) is mapped into this shape by
   hr-mapping.ts; the editable subset lives in a draft and is saved as one batch. */

export type WorkerType = 'Employee' | 'Freelancer' | 'Intern' | 'Apprentice' | 'Marginal'
export type WorkModel = 'Full-time' | 'Part-time' | 'Hourly'
export type OvertimeMode = 'Paid out' | 'Time off' | 'All-in'
export type AbsenceKind = 'Vacation' | 'Sick leave' | 'Special leave' | 'Unpaid leave' | 'Time off in lieu' | 'Parental leave'
export type NoteTag = 'General' | 'HR' | 'Contract' | 'Performance'

export interface ScheduleDay {
  d: string
  on: boolean
  h: number | string
}

export interface WorkerAbsence {
  id: string
  kind: AbsenceKind
  from: string
  to: string
  days: number
  status: 'Approved' | 'Pending' | 'Rejected'
}

export interface Allowance {
  name: string
  amount: number | string
  unit: 'per day' | 'per month' | 'per hour' | 'per km'
}

export interface WorkerNote {
  id: string
  tag: NoteTag
  text: string
  date: string
  pinned: boolean
}

export interface WorkerDoc {
  id: string
  category: string
  name: string
  size: number
  date: string
}

export interface CvEntry {
  a: string
  b: string
  from: string
  to: string
  desc: string
}

export interface CvFile {
  name: string
  size: number
  date: string
}

export interface Language {
  name: string
  level: string
}

/** The editable subset — saved in one batch via PUT /api/hr/workers/{id}. */
export interface WorkerDraft {
  name: string
  role: string
  type: WorkerType
  model: WorkModel
  days: ScheduleDay[]
  start: string
  end: string
  rate: number | string
  maxMonth: number | string
  expectedWeek: number | string
  vacEnt: number | string
  vacCarry: number | string
  ot: OvertimeMode
  otPct: number | string
  night: number | string
  sunday: number | string
  holiday: number | string
  flex: boolean
  coreFrom: string
  coreTo: string
  autoBreak: boolean
  tracking: boolean
  onCall: boolean
  homeOffice: number | string
  maxDay: number | string
  allowances: Allowance[]
  phone: string
  birthday: string
  svnr: string
  nationality: string
  department: string
  location: string
  address: string
  gender: string
  emName: string
  emRelation: string
  emPhone: string
  cvExp: CvEntry[]
  cvEdu: CvEntry[]
  skills: string[]
  langs: Language[]
}

/** The merged view the tabs render: the draft plus server-managed parts (files, lists, account). */
export interface Worker extends WorkerDraft {
  userId: string
  email: string | null
  accountStatus: AccountStatus
  permissionRole: string
  photo: string // resolved object URL, '' when none
  cvFile: CvFile | null
  absences: WorkerAbsence[]
  notes: WorkerNote[]
  docs: WorkerDoc[]
}

export const FULL_TIME_HOURS = 38.5

// ---- shared helpers ----

export const initialsOf = (name: string) =>
  (name || '?').split(' ').filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase() || '?'

export const weeklyHours = (w: { days: ScheduleDay[] }) =>
  w.days.reduce((a, d) => a + (d.on ? Number(d.h) || 0 : 0), 0)

/** Workdays (Mon–Fri) between two ISO dates, inclusive — list preview only; the server counts authoritatively. */
export function workdaysBetween(from: string, to: string): number {
  if (!from || !to) return 0
  const a = new Date(`${from}T00:00`)
  const b = new Date(`${to}T00:00`)
  if (b < a) return 0
  let n = 0
  for (const d = new Date(a); d <= b; d.setDate(d.getDate() + 1)) {
    const x = d.getDay()
    if (x !== 0 && x !== 6) n++
  }
  return n
}

export function fmtDate(s: string): string {
  if (!s) return '—'
  const [y, m, d] = s.split('-')
  return `${d}.${m}.${y}`
}

export const fmtNum = (n: number) => String(Math.round(n * 100) / 100)

export function fmtSize(b: number): string {
  if (!b && b !== 0) return ''
  return b > 1e6 ? `${(b / 1e6).toFixed(1)} MB` : `${Math.max(1, Math.round(b / 1000))} KB`
}

/** [text color, background] of the type chip in the worker list. */
export function typeChip(t: WorkerType): [string, string] {
  if (t === 'Freelancer') return ['var(--wtr-tint-orange-fg)', 'var(--wtr-tint-orange-bg)']
  if (t === 'Intern' || t === 'Apprentice') return ['var(--wtr-tint-green-fg)', 'var(--wtr-tint-green-bg)']
  if (t === 'Marginal') return ['var(--wtr-btn-text)', 'var(--wtr-muted)']
  return ['var(--wtr-tint-cyan-fg)', 'var(--wtr-tint-cyan-bg)']
}

export const statusChip = (s: AccountStatus): [string, string] =>
  s === 'Active' ? ['var(--wtr-tint-green-fg)', 'var(--wtr-tint-green-bg)']
  : s === 'Invited' ? ['var(--wtr-tint-cyan-fg)', 'var(--wtr-tint-cyan-bg)']
  : s === 'Disabled' ? ['var(--wtr-tint-orange-fg)', 'var(--wtr-tint-orange-bg)']
  : ['var(--wtr-btn-text)', 'var(--wtr-muted)']

export const statusLabel = (s: AccountStatus): string =>
  s === 'ProfileOnly' ? 'No access' : s

export const absenceDot = (k: AbsenceKind) =>
  ({
    Vacation: 'var(--wtr-accent)',
    'Sick leave': 'var(--wtr-tint-orange-fg)',
    'Special leave': 'var(--wtr-tint-green-fg)',
    'Unpaid leave': 'var(--wtr-label)',
    'Time off in lieu': 'var(--wtr-btn-text)',
    'Parental leave': 'var(--wtr-tint-green-fg)',
  })[k] ?? 'var(--wtr-label)'
