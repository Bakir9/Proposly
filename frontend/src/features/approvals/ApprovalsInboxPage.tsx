import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { getPendingTimesheets } from '@/api/timesheets'
import { getPendingAbsences } from '@/api/absences'
import { ApprovalsPage } from '@/features/worktime/ApprovalsPage'
import { AbsenceApprovalsPage } from '@/features/absences/AbsenceApprovalsPage'

type ApprovalType = 'time' | 'absences'

/**
 * One Approvals inbox (workspace redesign): the former "Approvals" and "Time off approvals"
 * pages behind a type filter, with pending counts on the tabs. When the user lands here the
 * tab with waiting items wins — a card that said "2 approvals waiting" must never open on an
 * empty list. The old routes /worktime/approvals and /absences/approvals keep working.
 */
export function ApprovalsInboxPage() {
  const [picked, setPicked] = useState<ApprovalType | null>(null)

  // Same query keys as the two pages, so the data is fetched once and shared.
  const { data: pendingSheets } = useQuery({ queryKey: ['timesheets', 'pending'], queryFn: getPendingTimesheets })
  const { data: pendingAbsences } = useQuery({ queryKey: ['absences', 'pending'], queryFn: getPendingAbsences })

  const sheetCount = pendingSheets?.length ?? 0
  const absenceCount = pendingAbsences?.length ?? 0

  // No explicit choice yet → open whichever type actually has items (time entries on a tie).
  const type: ApprovalType = picked ?? (sheetCount === 0 && absenceCount > 0 ? 'absences' : 'time')

  const TABS: { id: ApprovalType; label: string; count: number }[] = [
    { id: 'time', label: 'Time entries', count: sheetCount },
    { id: 'absences', label: 'Time off', count: absenceCount },
  ]

  return (
    <div>
      <div className="px-6 pt-6">
        <nav aria-label="Approval type" className="flex w-fit gap-[3px] rounded-[9px] bg-muted p-[3px]">
          {TABS.map(t => (
            <button
              key={t.id}
              onClick={() => setPicked(t.id)}
              aria-pressed={type === t.id}
              className={cn(
                'inline-flex min-h-9 items-center gap-1.5 rounded-[7px] px-3.5 text-[13px] font-semibold',
                type === t.id ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
              )}
            >
              {t.label}
              {t.count > 0 && (
                <span className="flex h-[18px] min-w-[18px] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-bold text-primary-foreground">
                  {t.count}
                </span>
              )}
            </button>
          ))}
        </nav>
      </div>
      {type === 'time' ? <ApprovalsPage /> : <AbsenceApprovalsPage />}
    </div>
  )
}
