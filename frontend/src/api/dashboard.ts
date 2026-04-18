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
}

export interface DashboardData {
  openOffersCount: number
  openOffersValue: number
  currency: string
  activeProjectsCount: number
  totalLockedRevenue: number
  hoursThisMonth: number
  recentOffers: RecentOfferItem[]
  recentProjects: RecentProjectItem[]
}

export const getDashboard = () =>
  api.get<DashboardData>('/dashboard').then(r => r.data)
