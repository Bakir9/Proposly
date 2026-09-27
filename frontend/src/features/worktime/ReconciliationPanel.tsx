import { useQuery } from '@tanstack/react-query'
import axios from 'axios'
import { getReconciliation } from '@/api/worktime-reports'
import { useAuth } from '@/features/auth/AuthContext'
import { FolderKanban, AlertTriangle } from 'lucide-react'

/**
 * Recorded working hours against hours booked to projects.
 *
 * Informational only — nothing here changes a timesheet or a project time log, and no project
 * cost or profitability figure is affected. Owner and Admin only, since it reads across projects.
 */
export function ReconciliationPanel({
  year, month, userId,
}: { year: number; month: number; userId: string }) {
  const { user } = useAuth()
  const canView = user?.role === 'Owner' || user?.role === 'Admin'

  const { data, isLoading, error } = useQuery({
    queryKey: ['worktime', 'reconciliation', year, month, userId],
    queryFn: () => getReconciliation(year, month, userId),
    enabled: canView,
    retry: false,
  })

  if (!canView) return null

  // 403 or 404 simply means there is nothing to show here.
  if (axios.isAxiosError(error)) return null
  if (isLoading || !data) return null

  const hasBookings = data.byProject.length > 0 || data.projectBookedHours > 0

  return (
    <section className="rounded-lg border">
      <header className="px-4 py-2 border-b bg-muted/50 flex items-center gap-2">
        <FolderKanban className="h-4 w-4" />
        <h2 className="text-sm font-semibold">Against project bookings</h2>
      </header>

      <dl className="divide-y text-sm">
        <Row label="Recorded working hours" value={`${data.recordedWorkingHours.toFixed(2)} h`} />
        <Row label="Booked to projects" value={`${data.projectBookedHours.toFixed(2)} h`} />

        {!data.overBooked && (
          <Row
            label="Not booked to any project"
            value={`${data.unbookedHours.toFixed(2)} h`}
            strong
          />
        )}

        {data.unattributedBookedHours > 0 && (
          <Row
            label="Bookings with no matching employee"
            value={`${data.unattributedBookedHours.toFixed(2)} h`}
            tone="muted"
          />
        )}
      </dl>

      {data.overBooked && (
        <div className="px-4 py-3 border-t flex gap-2 text-sm text-amber-500">
          <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5" />
          <span className="text-muted-foreground">
            More hours are booked to projects than were recorded as working time. Usually a day was
            booked but never entered on the timesheet.
          </span>
        </div>
      )}

      {hasBookings && data.byProject.length > 0 && (
        <div className="border-t">
          <table className="w-full text-sm">
            <tbody>
              {data.byProject.map(p => (
                <tr key={p.projectId} className="border-b last:border-b-0">
                  <td className="px-4 py-1.5 text-muted-foreground">{p.projectName}</td>
                  <td className="px-4 py-1.5 text-right tabular-nums">
                    {p.bookedHours.toFixed(2)} h
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {!hasBookings && (
        <p className="px-4 py-3 border-t text-sm text-muted-foreground">
          Nothing booked to a project this month, so every recorded hour is uncosted.
        </p>
      )}
    </section>
  )
}

function Row({
  label, value, strong, tone,
}: { label: string; value: string; strong?: boolean; tone?: 'muted' }) {
  return (
    <div className="flex justify-between px-4 py-2">
      <dt className={strong ? 'font-semibold' : ''}>{label}</dt>
      <dd className={`tabular-nums ${strong ? 'font-semibold' : ''} ${tone === 'muted' ? 'text-muted-foreground' : ''}`}>
        {value}
      </dd>
    </div>
  )
}
