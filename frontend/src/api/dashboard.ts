import { api } from './client'

export interface RecentOfferItem {
  id: string
  title: string
  clientName: string
  status: string
  total: number
  currency: string
}

export interface RecentProjectItem {
  id: string
  name: string
  status: string
  budgetAmount: number
  currency: string
  completedTasksCount: number
  totalTasksCount: number
}

export interface DashboardData {
  openOffersCount: number
  openOffersValue: number
  currency: string
  activeProjectsCount: number
  totalLockedRevenue: number
  hoursThisMonth: number
  totalOffersCount: number
  acceptedOffersCount: number
  recentOffers: RecentOfferItem[]
  recentProjects: RecentProjectItem[]
}

export const getDashboard = () =>
  api.get<DashboardData>('/dashboard').then(r => r.data)

// ── Redesigned home dashboard ────────────────────────────────────────────────

export interface AttentionItem {
  kind: string
  tone: 'blue' | 'amber' | 'red' | 'violet'
  title: string
  sub: string
  cta: string
  link: string
}

export interface DashboardKpis {
  wonThisYear: number | null
  acceptedThisYear: number
  winRatePercent: number | null
  openPipeline: number | null
  openOffersCount: number
  waitingForReply: number
  projectsActive: number
  projectsInProgress: number
  projectsPlanning: number
  projectsAtRisk: number
}

export interface DashboardOfferItem {
  id: string
  title: string
  clientName: string
  amount: number | null
  status: 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Expired'
  ageDays: number
  acceptedOn: string | null
  nextStep: 'FollowUp' | 'CreateProject' | 'FinishSend' | 'Open'
}

export interface DashboardProjectItem {
  id: string
  name: string
  clientName: string
  status: 'Active' | 'Planning' | 'OnHold'
  budgetUsedPercent: number | null
  tasksDone: number
  tasksTotal: number
  setupDone: number
  nextLabel: string
  nextAtRisk: boolean
  contractValue: number | null
}

export interface MyTimeSummary {
  weekHours: number
  weekTargetHours: number | null
  previousMonthName: string
  previousMonthStatus: 'NotSubmitted' | 'Submitted' | 'Approved' | 'Locked' | 'NoData'
  vacationLeftDays: number | null
  hasContract: boolean
}

export interface DashboardHome {
  canViewMoney: boolean
  currency: string
  attention: AttentionItem[]
  kpis: DashboardKpis
  offers: DashboardOfferItem[]
  projects: DashboardProjectItem[]
  myTime: MyTimeSummary
}

export type WhosOutRange = 'today' | 'week' | 'month'

export interface WhosOutDay {
  date: string
  outCount: number
  isNonWorking: boolean
}

export interface WhosOutPerson {
  userId: string
  name: string
  type: string
  startDate: string
  endDate: string
  backOn: string | null
}

export interface WhosOut {
  teamTotal: number
  inToday: number
  outToday: number
  notScheduledToday: number
  canSeeTypes: boolean
  perDay: WhosOutDay[]
  people: WhosOutPerson[]
}

export const getDashboardHome = () =>
  api.get<DashboardHome>('/dashboard/home').then(r => r.data)

export const getWhosOut = (range: WhosOutRange) =>
  api.get<WhosOut>('/dashboard/whos-out', { params: { range } }).then(r => r.data)
