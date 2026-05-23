import { api } from './client'

export interface ExpenseCategoryBreakdown {
  category: string
  amount: number
}

export interface QuarterlyReport {
  quarter: number
  fiscalYear: number
  fiscalYearStartMonth: number
  startDate: string
  endDate: string
  currency: string
  offersCreatedCount: number
  offersCreatedValue: number
  offersAcceptedCount: number
  offersAcceptedValue: number
  offersRejectedCount: number
  offersRejectedValue: number
  offersExpiredCount: number
  offersExpiredValue: number
  conversionRate: number
  laborCost: number
  totalHoursWorked: number
  expensesByCategory: ExpenseCategoryBreakdown[]
  totalExpenses: number
  grossProfit: number
  profitMargin: number
}

export const getQuarterlyReport = (fiscalYear: number, quarter: number) =>
  api.get<QuarterlyReport>('/reports/quarterly', { params: { fiscalYear, quarter } }).then(r => r.data)

export const downloadQuarterlyReportPdf = (fiscalYear: number, quarter: number) =>
  api.get('/reports/quarterly/pdf', { params: { fiscalYear, quarter }, responseType: 'blob' }).then(r => r.data as Blob)
