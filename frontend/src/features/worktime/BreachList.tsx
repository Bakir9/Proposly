import { BREACH_LABELS, formatBreachValues, type Breach } from '@/api/worktime-settings'
import { AlertTriangle } from 'lucide-react'

/**
 * Working time breaches for a month. Deliberately advisory in tone: the record is accepted as
 * entered, and this explains what the employer needs to look at.
 */
export function BreachList({ breaches }: { breaches: Breach[] }) {
  if (breaches.length === 0) return null

  return (
    <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-4 space-y-2">
      <div className="flex items-center gap-2 text-sm font-semibold text-amber-500">
        <AlertTriangle className="h-4 w-4" />
        {breaches.length} working time {breaches.length === 1 ? 'issue' : 'issues'}
      </div>

      <ul className="space-y-1 text-sm">
        {breaches.map((breach, i) => (
          <li key={`${breach.kind}-${breach.date ?? breach.weekStartDate}-${i}`} className="flex gap-2">
            <span className="text-muted-foreground tabular-nums shrink-0 w-28">
              {breach.date ?? (breach.weekStartDate ? `week of ${breach.weekStartDate}` : '—')}
            </span>
            <span>
              <span className="font-medium">{BREACH_LABELS[breach.kind]}</span>
              {' — '}
              <span className="text-muted-foreground">{formatBreachValues(breach)}</span>
            </span>
          </li>
        ))}
      </ul>

      <p className="text-xs text-muted-foreground pt-1">
        Your entries were saved as recorded. These are flagged for your employer to review.
      </p>
    </div>
  )
}
