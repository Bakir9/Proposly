import { useState } from 'react'
import { cn } from '@/lib/utils'
import { ApprovalsPage } from '@/features/worktime/ApprovalsPage'
import { AbsenceApprovalsPage } from '@/features/absences/AbsenceApprovalsPage'

const TYPES = [
  { id: 'time', label: 'Time entries' },
  { id: 'absences', label: 'Time off' },
] as const

type ApprovalType = (typeof TYPES)[number]['id']

/**
 * One Approvals inbox (workspace redesign): the former "Approvals" and "Time off approvals"
 * pages behind a type filter. Expense approvals join here in a later phase.
 * The old routes /worktime/approvals and /absences/approvals keep working.
 */
export function ApprovalsInboxPage() {
  const [type, setType] = useState<ApprovalType>(() => {
    try { return (sessionStorage.getItem('approvals-type') as ApprovalType) || 'time' } catch { return 'time' }
  })

  const pick = (t: ApprovalType) => {
    setType(t)
    try { sessionStorage.setItem('approvals-type', t) } catch { /* ignore */ }
  }

  return (
    <div>
      <div className="px-6 pt-6">
        <nav aria-label="Approval type" className="flex w-fit gap-[3px] rounded-[9px] bg-muted p-[3px]">
          {TYPES.map(t => (
            <button
              key={t.id}
              onClick={() => pick(t.id)}
              aria-pressed={type === t.id}
              className={cn(
                'inline-flex min-h-9 items-center rounded-[7px] px-3.5 text-[13px] font-semibold',
                type === t.id ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
              )}
            >
              {t.label}
            </button>
          ))}
        </nav>
      </div>
      {type === 'time' ? <ApprovalsPage /> : <AbsenceApprovalsPage />}
    </div>
  )
}
