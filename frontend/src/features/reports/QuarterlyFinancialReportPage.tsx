import { useState, useMemo } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { getQuarterlyReport, downloadQuarterlyReportPdf } from '@/api/reports'
import { getCompanySettings } from '@/api/settings'
import { Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts'
import { TrendingUp, TrendingDown, DollarSign, Clock, Target, FileText, Download, RefreshCw } from 'lucide-react'

const MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const EXPENSE_COLORS = ['#8b5cf6', '#06b6d4', '#94a3b8', '#f59e0b', '#ef4444', '#22c55e']
const PIPELINE_COLORS: Record<string, string> = {
  Created: '#3b82f6',
  Accepted: '#22c55e',
  Rejected: '#f87171',
  Expired: '#94a3b8',
}

function getQuarterMonthRange(quarter: number, fiscalYearStartMonth: number): string {
  const offset = (quarter - 1) * 3
  const m1 = MONTH_NAMES[(fiscalYearStartMonth - 1 + offset) % 12]
  const m3 = MONTH_NAMES[(fiscalYearStartMonth - 1 + offset + 2) % 12]
  return `${m1}–${m3}`
}

function fmt(value: number, currency: string, opts?: { decimals?: number }) {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: opts?.decimals ?? 0,
    maximumFractionDigits: opts?.decimals ?? 0,
  }).format(value)
}

function fmtShort(value: number, currency: string) {
  const sym = new Intl.NumberFormat('en-US', { style: 'currency', currency, maximumFractionDigits: 0 })
    .formatToParts(0).find(p => p.type === 'currency')?.value ?? '$'
  if (value >= 1_000_000) return `${sym}${(value / 1_000_000).toFixed(1)}M`
  if (value >= 1_000) return `${sym}${(value / 1_000).toFixed(0)}k`
  return `${sym}${value.toFixed(0)}`
}

function getCurrentFiscalYear(fiscalYearStartMonth: number): number {
  const now = new Date()
  const year = now.getFullYear()
  const month = now.getMonth() + 1
  return month >= fiscalYearStartMonth ? year : year - 1
}

function getCurrentQuarter(fiscalYearStartMonth: number): number {
  const now = new Date()
  const month = now.getMonth() + 1
  const elapsed = ((month - fiscalYearStartMonth + 12) % 12)
  return Math.floor(elapsed / 3) + 1
}

export function QuarterlyFinancialReportPage() {
  const qc = useQueryClient()

  const { data: settings, isLoading: loadingSettings } = useQuery({
    queryKey: ['company-settings'],
    queryFn: getCompanySettings,
  })

  const fiscalYearStartMonth = settings?.fiscalYearStartMonth ?? 1

  const resolvedYear = useMemo(() => getCurrentFiscalYear(fiscalYearStartMonth), [fiscalYearStartMonth])
  const resolvedQuarter = useMemo(() => getCurrentQuarter(fiscalYearStartMonth), [fiscalYearStartMonth])

  const [fiscalYear, setFiscalYear] = useState<number>(() => getCurrentFiscalYear(1))
  const [quarter, setQuarter] = useState<number>(() => getCurrentQuarter(1))

  const [yearInitialized, setYearInitialized] = useState(false)
  if (!yearInitialized && !loadingSettings && settings) {
    setFiscalYear(resolvedYear)
    setQuarter(resolvedQuarter)
    setYearInitialized(true)
  }

  const { data: report, isLoading: loadingReport, isError } = useQuery({
    queryKey: ['quarterly-report', fiscalYear, quarter],
    queryFn: () => getQuarterlyReport(fiscalYear, quarter),
    enabled: !!settings,
  })

  const yearOptions = useMemo(() => {
    const current = resolvedYear
    return [current + 1, current, current - 1, current - 2, current - 3]
  }, [resolvedYear])

  const currency = report?.currency ?? 'EUR'

  const offerPipelineData = report
    ? [
        { name: 'Created', amount: report.offersCreatedValue, count: report.offersCreatedCount },
        { name: 'Accepted', amount: report.offersAcceptedValue, count: report.offersAcceptedCount },
        { name: 'Rejected', amount: report.offersRejectedValue, count: report.offersRejectedCount },
        { name: 'Expired', amount: report.offersExpiredValue, count: report.offersExpiredCount },
      ]
    : []

  const costsBreakdownData = report
    ? [
        { name: 'Labor', amount: report.laborCost },
        ...report.expensesByCategory.map(e => ({ name: e.category, amount: e.amount })),
      ].filter(x => x.amount > 0)
    : []

  const totalCosts = report ? report.laborCost + report.totalExpenses : 0
  const costsTotal = costsBreakdownData.reduce((s, d) => s + d.amount, 0)

  const revVsCostData = report
    ? [
        { name: 'Revenue', value: report.offersAcceptedValue, color: '#3b82f6' },
        { name: 'Labor', value: report.laborCost, color: '#f59e0b' },
        { name: 'Expenses', value: report.totalExpenses, color: '#ef4444' },
        { name: 'Profit', value: Math.max(0, report.grossProfit), color: '#22c55e' },
      ]
    : []

  const maxRevVal = revVsCostData.reduce((m, d) => Math.max(m, d.value), 0)

  const [exporting, setExporting] = useState(false)
  const handleExportPdf = async () => {
    if (!report) return
    setExporting(true)
    try {
      const blob = await downloadQuarterlyReportPdf(fiscalYear, quarter)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `report-Q${quarter}-FY${fiscalYear}.pdf`
      a.click()
      URL.revokeObjectURL(url)
    } finally {
      setExporting(false)
    }
  }
  const handleRefresh = () => qc.invalidateQueries({ queryKey: ['quarterly-report', fiscalYear, quarter] })

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">Quarterly Financial Report</h1>
          <p className="text-sm text-muted-foreground mt-1">
            {report
              ? `Q${quarter} ${fiscalYear} — ${new Date(report.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} to ${new Date(report.endDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`
              : `Q${quarter} ${fiscalYear} performance review and budget breakdown`
            }
          </p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <select
            value={fiscalYear}
            onChange={e => setFiscalYear(Number(e.target.value))}
            className="h-9 rounded-md border bg-card px-3 text-sm"
          >
            {yearOptions.map(y => <option key={y} value={y}>FY {y}</option>)}
          </select>
          <select
            value={quarter}
            onChange={e => setQuarter(Number(e.target.value))}
            className="h-9 rounded-md border bg-card px-3 text-sm"
          >
            {[1, 2, 3, 4].map(q => (
              <option key={q} value={q}>Q{q} ({getQuarterMonthRange(q, fiscalYearStartMonth)})</option>
            ))}
          </select>
          <button
            onClick={handleExportPdf}
            disabled={!report || exporting}
            className="flex items-center gap-1.5 h-9 px-3 rounded-md border bg-card text-sm hover:bg-muted transition-colors disabled:opacity-50"
          >
            <Download className="h-3.5 w-3.5" /> {exporting ? 'Generating…' : 'Export PDF'}
          </button>
          <button
            onClick={handleRefresh}
            className="flex items-center gap-1.5 h-9 px-3 rounded-md bg-emerald-500 hover:bg-emerald-600 text-white text-sm transition-colors"
          >
            <RefreshCw className="h-3.5 w-3.5" /> Update Data
          </button>
        </div>
      </div>

      {(loadingSettings || loadingReport) && (
        <p className="text-sm text-muted-foreground">Loading report…</p>
      )}
      {isError && (
        <p className="text-sm text-destructive">Failed to load report. Please try again.</p>
      )}

      {report && (
        <>
          {/* KPI Cards */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              {
                label: 'Revenue',
                value: fmtShort(report.offersAcceptedValue, currency),
                sub: `${report.offersAcceptedCount} accepted offer${report.offersAcceptedCount !== 1 ? 's' : ''}`,
                icon: <DollarSign className="h-4 w-4 text-blue-400" />,
                iconBg: 'bg-blue-500/10',
                positive: true,
              },
              {
                label: 'Total Costs',
                value: fmtShort(totalCosts, currency),
                sub: `${report.totalHoursWorked.toFixed(0)}h labor + ${fmtShort(report.totalExpenses, currency)} expenses`,
                icon: <Clock className="h-4 w-4 text-amber-400" />,
                iconBg: 'bg-amber-500/10',
                positive: null,
              },
              {
                label: 'Gross Profit',
                value: fmtShort(report.grossProfit, currency),
                sub: `${report.profitMargin.toFixed(1)}% margin`,
                icon: report.grossProfit >= 0
                  ? <TrendingUp className="h-4 w-4 text-emerald-400" />
                  : <TrendingDown className="h-4 w-4 text-red-400" />,
                iconBg: report.grossProfit >= 0 ? 'bg-emerald-500/10' : 'bg-red-500/10',
                positive: report.grossProfit >= 0,
              },
              {
                label: 'Conv. Rate',
                value: `${report.conversionRate.toFixed(1)}%`,
                sub: `${report.offersCreatedCount} offers created`,
                icon: <Target className="h-4 w-4 text-violet-400" />,
                iconBg: 'bg-violet-500/10',
                positive: report.conversionRate >= 50,
              },
            ].map(card => (
              <div key={card.label} className="rounded-xl border bg-card p-4">
                <div className="flex items-start justify-between mb-3">
                  <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{card.label}</span>
                  <div className={`h-8 w-8 rounded-lg ${card.iconBg} flex items-center justify-center`}>
                    {card.icon}
                  </div>
                </div>
                <p className="text-2xl font-bold">{card.value}</p>
                <p className={`text-xs mt-1 ${card.positive === true ? 'text-emerald-400' : card.positive === false ? 'text-red-400' : 'text-muted-foreground'}`}>
                  {card.sub}
                </p>
              </div>
            ))}
          </div>

          {/* Offer Pipeline + Cost Breakdown */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            {/* Offer Pipeline */}
            <div className="rounded-xl border bg-card p-4">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h2 className="font-semibold flex items-center gap-2">
                    <FileText className="h-4 w-4 text-muted-foreground" /> Offer Pipeline
                  </h2>
                  <p className="text-xs text-muted-foreground">Offers by outcome this quarter</p>
                </div>
                <span className="text-xs text-muted-foreground font-medium">VALUE ({currency})</span>
              </div>

              {report.offersCreatedCount === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-8">No offers created this quarter.</p>
              ) : (
                <>
                  <div className="space-y-3">
                    {offerPipelineData.map(row => {
                      const maxAmt = Math.max(...offerPipelineData.map(d => d.amount), 1)
                      const pct = (row.amount / maxAmt) * 100
                      const color = PIPELINE_COLORS[row.name] ?? '#94a3b8'
                      return (
                        <div key={row.name} className="space-y-1">
                          <div className="flex items-center justify-between text-sm">
                            <span className="text-muted-foreground">{row.name}</span>
                            <span className="font-medium tabular-nums">{fmtShort(row.amount, currency)}</span>
                          </div>
                          <div className="h-2 rounded-full bg-muted overflow-hidden">
                            <div
                              className="h-full rounded-full transition-all"
                              style={{ width: `${pct}%`, background: color }}
                            />
                          </div>
                        </div>
                      )
                    })}
                  </div>
                  <div className="mt-4 grid grid-cols-2 gap-2 text-xs">
                    <div className="rounded-lg border p-2.5 text-center">
                      <p className="font-semibold text-emerald-400">{fmtShort(report.offersAcceptedValue, currency)}</p>
                      <p className="text-muted-foreground mt-0.5">Won revenue</p>
                    </div>
                    <div className="rounded-lg border p-2.5 text-center">
                      <p className="font-semibold text-red-400">{fmtShort(report.offersRejectedValue + report.offersExpiredValue, currency)}</p>
                      <p className="text-muted-foreground mt-0.5">Lost revenue</p>
                    </div>
                  </div>
                </>
              )}
            </div>

            {/* Cost Breakdown */}
            <div className="rounded-xl border bg-card p-4">
              <div className="mb-4">
                <h2 className="font-semibold">Cost Breakdown</h2>
                <p className="text-xs text-muted-foreground">Labor and expenses logged this quarter</p>
              </div>

              {costsBreakdownData.length === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-8">No costs recorded this quarter.</p>
              ) : (
                <div className="flex items-center gap-4">
                  {/* Donut with center label */}
                  <div className="relative flex-shrink-0" style={{ width: 160, height: 160 }}>
                    <ResponsiveContainer width={160} height={160}>
                      <PieChart>
                        <Pie
                          data={costsBreakdownData}
                          dataKey="amount"
                          cx="50%"
                          cy="50%"
                          innerRadius={52}
                          outerRadius={72}
                          strokeWidth={2}
                          stroke="hsl(var(--card))"
                        >
                          {costsBreakdownData.map((_, i) => (
                            <Cell key={i} fill={EXPENSE_COLORS[i % EXPENSE_COLORS.length]} />
                          ))}
                        </Pie>
                        <Tooltip formatter={(v) => fmt(Number(v), currency)} />
                      </PieChart>
                    </ResponsiveContainer>
                    <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
                      <span className="text-sm font-bold">{fmtShort(costsTotal, currency)}</span>
                      <span className="text-xs text-muted-foreground">total</span>
                    </div>
                  </div>

                  {/* Legend */}
                  <div className="flex-1 space-y-2">
                    {costsBreakdownData.map((d, i) => {
                      const pct = costsTotal > 0 ? (d.amount / costsTotal) * 100 : 0
                      const color = EXPENSE_COLORS[i % EXPENSE_COLORS.length]
                      return (
                        <div key={d.name} className="flex items-center gap-2 text-xs">
                          <span className="h-2.5 w-2.5 rounded-sm flex-shrink-0" style={{ background: color }} />
                          <span className="uppercase tracking-wide text-muted-foreground font-medium flex-1">{d.name}</span>
                          <span className="font-semibold tabular-nums">{fmtShort(d.amount, currency)}</span>
                          <span className="text-muted-foreground tabular-nums w-9 text-right">({pct.toFixed(0)}%)</span>
                        </div>
                      )
                    })}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Revenue vs. Costs Analysis */}
          <div className="rounded-xl border bg-card p-4">
            <h2 className="font-semibold mb-1">Revenue vs. Costs Analysis</h2>
            <p className="text-xs text-muted-foreground mb-5">Accepted offer revenue compared to labor, expenses, and profit</p>

            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {revVsCostData.map(col => {
                const pct = maxRevVal > 0 ? (col.value / maxRevVal) * 100 : 0
                return (
                  <div key={col.name} className="rounded-lg bg-muted/30 border p-4 flex flex-col gap-3">
                    <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{col.name}</span>
                    <div className="flex-1 flex items-end">
                      <div className="w-full h-20 flex items-end">
                        <div
                          className="w-full rounded-t-md transition-all"
                          style={{ height: `${Math.max(pct, 4)}%`, background: col.color, opacity: 0.85 }}
                        />
                      </div>
                    </div>
                    <p className="text-sm font-bold tabular-nums" style={{ color: col.color }}>
                      {fmt(col.value, currency)}
                    </p>
                  </div>
                )
              })}
            </div>
          </div>

          {/* Detailed Summary */}
          <div className="rounded-xl border bg-card p-4">
            <h2 className="font-semibold mb-4">Detailed Summary</h2>
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left pb-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Category</th>
                  <th className="text-left pb-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Quantity / Unit</th>
                  <th className="text-right pb-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Total Amount</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                <tr>
                  <td className="py-3">
                    <div className="flex items-center gap-2">
                      <div className="h-7 w-7 rounded-full bg-emerald-500/15 flex items-center justify-center flex-shrink-0">
                        <TrendingUp className="h-3.5 w-3.5 text-emerald-400" />
                      </div>
                      <span className="font-medium">Accepted Offer Revenue</span>
                    </div>
                  </td>
                  <td className="py-3 text-muted-foreground">{report.offersAcceptedCount} project{report.offersAcceptedCount !== 1 ? 's' : ''}</td>
                  <td className="py-3 text-right font-medium tabular-nums">{fmt(report.offersAcceptedValue, currency, { decimals: 2 })}</td>
                </tr>
                <tr>
                  <td className="py-3">
                    <div className="flex items-center gap-2">
                      <div className="h-7 w-7 rounded-full bg-amber-500/15 flex items-center justify-center flex-shrink-0">
                        <Clock className="h-3.5 w-3.5 text-amber-400" />
                      </div>
                      <span className="font-medium">Labor Costs</span>
                    </div>
                  </td>
                  <td className="py-3 text-muted-foreground">{report.totalHoursWorked.toFixed(0)} hours</td>
                  <td className="py-3 text-right font-medium tabular-nums text-red-400">−{fmt(report.laborCost, currency, { decimals: 2 })}</td>
                </tr>
                {report.expensesByCategory.map((e) => (
                  <tr key={e.category}>
                    <td className="py-3">
                      <div className="flex items-center gap-2">
                        <div className="h-7 w-7 rounded-full bg-muted flex items-center justify-center flex-shrink-0">
                          <span className="text-[10px] font-bold text-muted-foreground">{e.category.slice(0, 2).toUpperCase()}</span>
                        </div>
                        <span className="font-medium">{e.category}</span>
                      </div>
                    </td>
                    <td className="py-3 text-muted-foreground">—</td>
                    <td className="py-3 text-right font-medium tabular-nums text-red-400">−{fmt(e.amount, currency, { decimals: 2 })}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Bottom row: Gross Profit + Profit Margin */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="rounded-xl border bg-card p-6 text-center">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-widest mb-3">Final Gross Profit</p>
              <p className={`text-4xl font-bold tabular-nums ${report.grossProfit >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                {fmt(report.grossProfit, currency, { decimals: 2 })}
              </p>
              <div className="mt-4 h-1.5 rounded-full bg-muted overflow-hidden">
                <div
                  className={`h-full rounded-full ${report.grossProfit >= 0 ? 'bg-emerald-500' : 'bg-red-500'}`}
                  style={{ width: report.offersAcceptedValue > 0 ? `${Math.min((report.grossProfit / report.offersAcceptedValue) * 100, 100)}%` : '0%' }}
                />
              </div>
            </div>

            <div className="rounded-xl border bg-card p-6 text-center">
              <p className="text-xs text-muted-foreground font-medium uppercase tracking-widest mb-3">Profit Margin</p>
              <p className={`text-4xl font-bold tabular-nums ${report.profitMargin >= 0 ? 'text-foreground' : 'text-red-400'}`}>
                {report.profitMargin.toFixed(2)}%
              </p>
              <p className="text-xs text-muted-foreground mt-2">
                {report.profitMargin >= 20
                  ? `${(report.profitMargin - 20).toFixed(1)}% above industry avg`
                  : report.profitMargin >= 0
                    ? `${(20 - report.profitMargin).toFixed(1)}% below industry avg`
                    : 'Negative margin this quarter'}
              </p>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
