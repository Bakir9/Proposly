/* Data model for the Team page — ported 1:1 from the design prototype (Team Settings.dc.html).
   Frontend only: everything lives in component state until the backend exists. */

export type WorkerType = 'Employee' | 'Freelancer' | 'Intern' | 'Apprentice' | 'Marginal'
export type WorkModel = 'Full-time' | 'Part-time' | 'Hourly'
export type OvertimeMode = 'Paid out' | 'Time off' | 'All-in'
export type AbsenceKind = 'Vacation' | 'Sick leave' | 'Special leave' | 'Unpaid leave' | 'Time off in lieu'
export type AbsenceStatus = 'Approved' | 'Pending'
export type NoteTag = 'General' | 'HR' | 'Contract' | 'Performance'
export type DocCategory =
  | 'Contract'
  | 'Amendment'
  | 'Previous contract'
  | 'ID / Work permit'
  | 'Certificate'
  | 'Payroll'
  | 'Invoice'
  | 'Other'

export interface ScheduleDay {
  d: string
  on: boolean
  h: number | string
}

export interface Absence {
  kind: AbsenceKind
  from: string
  to: string
  status: AbsenceStatus
}

export interface Allowance {
  name: string
  amount: number | string
  unit: 'per day' | 'per month' | 'per hour' | 'per km'
}

export interface WorkerNote {
  tag: NoteTag
  text: string
  date: string
  pinned: boolean
}

export interface WorkerDoc {
  id: string
  category: DocCategory
  name: string
  size: number
  date: string
  url?: string
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
  url?: string
}

export interface Language {
  name: string
  level: string
}

export interface Worker {
  id: number
  name: string
  role: string
  email: string
  type: WorkerType
  model: WorkModel
  days: ScheduleDay[]
  photo: string
  start: string
  end: string
  rate: number | string
  maxMonth: number | string
  expectedWeek: number | string
  vacEnt: number | string
  vacCarry: number | string
  absences: Absence[]
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
  notes: WorkerNote[]
  docs: WorkerDoc[]
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
  cvFile: CvFile | null
  cvExp: CvEntry[]
  cvEdu: CvEntry[]
  skills: string[]
  langs: Language[]
}

export const FULL_TIME_HOURS = 38.5
export const DEFAULT_VACATION_DAYS = 25

export const scheduleDays = (h: number, activeDays: number): ScheduleDay[] =>
  ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'].map((d, i) => ({ d, on: i < activeDays, h: i < activeDays ? h : 0 }))

export function baseWorker(overrides: Partial<Worker> & { id: number }): Worker {
  return {
    name: '',
    role: '',
    email: '',
    type: 'Employee',
    model: 'Full-time',
    days: scheduleDays(FULL_TIME_HOURS / 5, 5),
    photo: '',
    start: '2024-01-01',
    end: '',
    rate: '',
    maxMonth: '',
    expectedWeek: '',
    vacEnt: DEFAULT_VACATION_DAYS,
    vacCarry: 0,
    absences: [],
    ot: 'Time off',
    otPct: 50,
    night: 50,
    sunday: 100,
    holiday: 100,
    flex: true,
    coreFrom: '09:30',
    coreTo: '15:00',
    autoBreak: true,
    tracking: true,
    onCall: false,
    homeOffice: 2,
    maxDay: 10,
    allowances: [],
    notes: [],
    docs: [],
    phone: '',
    birthday: '',
    svnr: '',
    nationality: '',
    department: '',
    location: '',
    address: '',
    gender: '',
    emName: '',
    emRelation: '',
    emPhone: '',
    cvFile: null,
    cvExp: [],
    cvEdu: [],
    skills: [],
    langs: [],
    ...overrides,
  }
}

/** Demo data from the handoff — replaced by the real team once the backend endpoints exist. */
export const SEED_WORKERS: Worker[] = [
  baseWorker({
    id: 1,
    name: 'Lena Huber',
    role: 'Project lead',
    email: 'lena.huber@kost.at',
    type: 'Employee',
    model: 'Full-time',
    days: scheduleDays(7.7, 5),
    vacCarry: 4,
    start: '2021-03-01',
    gender: 'Female',
    phone: '+43 664 218 4471',
    birthday: '1988-04-12',
    svnr: '4821 120488',
    nationality: 'Austria',
    department: 'Planning',
    location: 'Graz office',
    address: 'Annenstraße 24, 8020 Graz',
    emName: 'Thomas Huber',
    emRelation: 'Partner',
    emPhone: '+43 676 552 1093',
    cvFile: { name: 'CV_Lena_Huber_2021.pdf', size: 320000, date: '2021-01-20' },
    cvExp: [
      { a: 'Senior architect', b: 'Atelier Nord, Linz', from: '2016-09', to: '2021-02', desc: 'Led residential projects from design to handover.' },
      { a: 'Architect', b: 'Bauplan GmbH, Wien', from: '2012-03', to: '2016-08', desc: '' },
    ],
    cvEdu: [{ a: 'DI Architektur', b: 'TU Graz', from: '2006-10', to: '2012-01', desc: '' }],
    skills: ['Revit', 'AutoCAD', 'Project management', 'ÖNORM B 1801'],
    langs: [
      { name: 'German', level: 'Native' },
      { name: 'English', level: 'C1' },
    ],
    absences: [
      { kind: 'Vacation', from: '2026-07-13', to: '2026-07-24', status: 'Approved' },
      { kind: 'Sick leave', from: '2026-02-09', to: '2026-02-11', status: 'Approved' },
      { kind: 'Vacation', from: '2026-12-21', to: '2026-12-31', status: 'Pending' },
    ],
    allowances: [{ name: 'Meal voucher', amount: 8, unit: 'per day' }],
    docs: [
      { id: 's1', category: 'Contract', name: 'Dienstvertrag_Huber_2021.pdf', size: 248000, date: '2021-02-18' },
      { id: 's2', category: 'Amendment', name: 'Arbeitszeitvereinbarung_2024.pdf', size: 96000, date: '2024-01-09' },
      { id: 's3', category: 'Certificate', name: 'Ersthelfer_Zertifikat.pdf', size: 412000, date: '2025-06-02' },
    ],
    notes: [
      { tag: 'HR', text: 'Requested switch to 4-day week from January — discuss in year-end review.', date: '2026-09-18', pinned: true },
      { tag: 'General', text: 'Prefers home office on Mondays and Fridays.', date: '2026-05-04', pinned: false },
    ],
  }),
  baseWorker({
    id: 2,
    name: 'Markus Gruber',
    role: 'Site engineer',
    email: 'markus.gruber@kost.at',
    type: 'Employee',
    model: 'Full-time',
    days: scheduleDays(8, 5),
    ot: 'Paid out',
    flex: false,
    onCall: true,
    homeOffice: 0,
    gender: 'Male',
    phone: '+43 699 104 2287',
    department: 'Construction',
    location: 'On site',
    absences: [{ kind: 'Vacation', from: '2026-08-03', to: '2026-08-14', status: 'Approved' }],
    allowances: [
      { name: 'Travel allowance', amount: 0.5, unit: 'per km' },
      { name: 'Site bonus', amount: 12, unit: 'per day' },
    ],
  }),
  baseWorker({
    id: 3,
    name: 'Sophie Wagner',
    role: 'Accounting',
    email: 'sophie.wagner@kost.at',
    type: 'Employee',
    model: 'Part-time',
    days: scheduleDays(6, 4),
    vacEnt: 20,
    absences: [{ kind: 'Vacation', from: '2026-10-26', to: '2026-10-30', status: 'Pending' }],
  }),
  baseWorker({
    id: 4,
    name: 'David Bauer',
    role: 'Apprentice, drafting',
    type: 'Apprentice',
    model: 'Full-time',
    days: scheduleDays(7.7, 5),
    homeOffice: 0,
    maxDay: 9,
    start: '2025-09-01',
  }),
  baseWorker({
    id: 5,
    name: 'Anna Pichler',
    role: 'Marketing',
    type: 'Marginal',
    model: 'Hourly',
    days: scheduleDays(0, 0),
    maxMonth: 40,
    expectedWeek: 8,
    vacEnt: 5,
  }),
  baseWorker({
    id: 6,
    name: 'Tobias Steiner',
    role: '3D visualisation',
    email: 'hello@steiner.studio',
    type: 'Freelancer',
    model: 'Hourly',
    days: scheduleDays(0, 0),
    rate: 85,
    vacEnt: 0,
    tracking: false,
    flex: false,
    autoBreak: false,
    homeOffice: 5,
    docs: [{ id: 's4', category: 'Contract', name: 'Werkvertrag_Steiner_Studio.pdf', size: 181000, date: '2026-03-11' }],
  }),
  baseWorker({
    id: 7,
    name: 'Mira Kovač',
    role: 'Intern, architecture',
    type: 'Intern',
    model: 'Full-time',
    days: scheduleDays(7.7, 5),
    vacEnt: 2,
    start: '2026-07-01',
    end: '2026-12-31',
  }),
]

// ---- shared helpers ----

export const initialsOf = (name: string) =>
  (name || '?').split(' ').filter(Boolean).map(p => p[0]).slice(0, 2).join('').toUpperCase() || '?'

export const weeklyHours = (w: Worker) => w.days.reduce((a, d) => a + (d.on ? Number(d.h) || 0 : 0), 0)

/** Workdays (Mon–Fri) between two ISO dates, inclusive. */
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
  if (t === 'Freelancer') return ['#F2A25E', '#2A1D12']
  if (t === 'Intern' || t === 'Apprentice') return ['#63D9A4', '#11281F']
  if (t === 'Marginal') return ['#B9C6DC', '#18223A']
  return ['#9BE0FA', '#12253A']
}

export const absenceDot = (k: AbsenceKind) =>
  ({
    Vacation: '#5EC8F2',
    'Sick leave': '#F2A25E',
    'Special leave': '#63D9A4',
    'Unpaid leave': '#8391AB',
    'Time off in lieu': '#B9C6DC',
  })[k] ?? '#8391AB'

export const today = () => new Date().toISOString().slice(0, 10)
