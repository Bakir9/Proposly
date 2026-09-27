import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getEmploymentTerms, createEmploymentTerms, getTargetHours,
  WEEK_DAYS, EMPLOYMENT_TYPE_LABELS, SUGGESTED_WEEKLY_HOURS,
  type WeekDayName, type EmploymentType,
} from '@/api/worktime-calendar'
import { getUsers } from '@/api/users'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Dialog } from '@/components/ui/dialog'
import { Briefcase, Plus, Info } from 'lucide-react'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

export function EmploymentTermsPage() {
  const queryClient = useQueryClient()
  const [userId, setUserId] = useState<string>('')
  const [adding, setAdding] = useState(false)

  const now = new Date()
  const [year] = useState(now.getFullYear())
  const [month] = useState(now.getMonth() + 1)

  const { data: users } = useQuery({ queryKey: ['users'], queryFn: getUsers })

  // Default to the first employee once the list arrives, without syncing state in an effect.
  const selectedUserId = userId || users?.[0]?.id || ''

  const { data: history, isLoading } = useQuery({
    queryKey: ['worktime', 'terms', selectedUserId],
    queryFn: () => getEmploymentTerms(selectedUserId),
    enabled: selectedUserId !== '',
  })

  const { data: target } = useQuery({
    queryKey: ['worktime', 'target', selectedUserId, year, month],
    queryFn: () => getTargetHours(selectedUserId, year, month),
    enabled: selectedUserId !== '',
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['worktime'] })
    queryClient.invalidateQueries({ queryKey: ['absences'] })
  }

  return (
    <div className="p-6 space-y-6 max-w-4xl">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <Briefcase className="h-6 w-6" />
            Employment terms
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Contracted hours, working days and annual leave — the basis for everyone's target hours.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Select value={selectedUserId} onChange={e => setUserId(e.target.value)}>
            {(users ?? []).map(u => (
              <option key={u.id} value={u.id}>{u.fullName}</option>
            ))}
          </Select>
          <Button
            onClick={() => setAdding(true)}
            disabled={selectedUserId === ''}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            New version
          </Button>
        </div>
      </div>

      <div className="rounded-md border border-blue-500/30 bg-blue-500/5 p-3 flex gap-2 text-xs text-muted-foreground">
        <Info className="h-4 w-4 shrink-0 text-blue-400" />
        <span>
          Terms are versioned, never edited. Recording a change adds a new version from its start
          date onward, so months already reported keep the terms they were judged against.
        </span>
      </div>

      {target && (
        <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
          <Stat label={`Working days in ${MONTHS[month - 1]}`} value={String(target.workingDays)} />
          <Stat label="Non-working days excluded" value={String(target.nonWorkingDaysExcluded)} />
          <Stat
            label="Target hours this month"
            value={target.targetHours === null ? 'unavailable' : `${target.targetHours} h`}
            muted={target.targetHours === null}
          />
        </div>
      )}

      {target?.targetHours === null && (
        <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-4 text-sm">
          <p className="font-medium text-amber-500">No terms cover {MONTHS[month - 1]} {year}</p>
          <p className="text-muted-foreground mt-1">
            Recorded hours still show, but there is nothing to compare them against. Add a version
            starting on or before the first of the month.
          </p>
        </div>
      )}

      {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}

      {!isLoading && (history?.length ?? 0) === 0 && selectedUserId !== '' && (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          No employment terms recorded for this employee yet.
        </div>
      )}

      {(history?.length ?? 0) > 0 && (
        <div className="rounded-lg border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr className="text-left">
                <th className="px-4 py-2 font-medium">From</th>
                <th className="px-4 py-2 font-medium">To</th>
                <th className="px-4 py-2 font-medium">Type</th>
                <th className="px-4 py-2 font-medium text-right">Weekly</th>
                <th className="px-4 py-2 font-medium text-right">Daily</th>
                <th className="px-4 py-2 font-medium">Working days</th>
                <th className="px-4 py-2 font-medium text-right">Vacation</th>
                <th className="px-4 py-2 font-medium">Overtime</th>
              </tr>
            </thead>
            <tbody>
              {history!.map((row, i) => (
                <tr key={row.id} className={`border-t ${i === 0 ? '' : 'text-muted-foreground'}`}>
                  <td className="px-4 py-2 tabular-nums">{row.validFrom}</td>
                  <td className="px-4 py-2 tabular-nums">
                    {row.validTo ?? <span className="text-emerald-400">current</span>}
                  </td>
                  <td className="px-4 py-2 text-xs">
                    {EMPLOYMENT_TYPE_LABELS[row.employmentType]}
                  </td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.weeklyHours} h</td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.dailyHours} h</td>
                  <td className="px-4 py-2 text-xs">
                    {row.workingDays.map(d => d.slice(0, 2)).join(' · ')}
                  </td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.annualVacationDays} d</td>
                  <td className="px-4 py-2 text-xs">
                    <OvertimeTerms
                      isAllIn={row.isAllIn}
                      lumpSumHours={row.overtimeLumpSumHours}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <TermsDialog
        open={adding}
        userId={selectedUserId}
        onClose={() => setAdding(false)}
        onDone={() => { setAdding(false); invalidate() }}
      />
    </div>
  )
}

/**
 * The two options are mutually exclusive — all-in already covers every additional hour, so a lump
 * sum on top would pay the same overtime twice. The backend rejects the combination.
 */
function OvertimeTerms({
  isAllIn, lumpSumHours,
}: { isAllIn: boolean; lumpSumHours: number | null }) {
  if (isAllIn) {
    return (
      <span className="rounded-md bg-violet-500/10 text-violet-400 px-1.5 py-0.5 font-medium">
        All-in
      </span>
    )
  }

  if (lumpSumHours && lumpSumHours > 0) {
    return (
      <span className="rounded-md bg-sky-500/10 text-sky-400 px-1.5 py-0.5 font-medium tabular-nums">
        Pauschale {lumpSumHours} h/mo
      </span>
    )
  }

  return <span className="text-muted-foreground">—</span>
}

function Stat({ label, value, muted }: { label: string; value: string; muted?: boolean }) {
  return (
    <div className="rounded-lg border p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className={`text-xl font-bold tabular-nums ${muted ? 'text-muted-foreground' : ''}`}>
        {value}
      </p>
    </div>
  )
}

function TermsDialog({
  open, userId, onClose, onDone,
}: { open: boolean; userId: string; onClose: () => void; onDone: () => void }) {
  const [validFrom, setValidFrom] = useState(new Date().toISOString().slice(0, 10))
  const [employmentType, setEmploymentType] = useState<EmploymentType>('FullTime')
  const [weeklyHours, setWeeklyHours] = useState(38.5)
  const [annualVacationDays, setAnnualVacationDays] = useState(25)
  const [workingDays, setWorkingDays] = useState<WeekDayName[]>([
    'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday',
  ])
  const [isAllIn, setIsAllIn] = useState(false)
  const [lumpSum, setLumpSum] = useState('')

  /**
   * The type only *suggests* hours — a full-time week is 38.5 in one company and 40 in another,
   * so the figure stays freely editable and nothing is enforced against the type.
   */
  const changeType = (type: EmploymentType) => {
    setEmploymentType(type)
    const suggested = SUGGESTED_WEEKLY_HOURS[type]
    if (suggested !== null) setWeeklyHours(suggested)
  }

  const lumpSumHours = lumpSum.trim() === '' ? null : Number(lumpSum)

  const save = useMutation({
    mutationFn: () =>
      createEmploymentTerms(userId, {
        validFrom, weeklyHours, workingDays, annualVacationDays,
        employmentType, isAllIn,
        overtimeLumpSumHours: isAllIn ? null : lumpSumHours,
      }),
    onSuccess: () => {
      toast.success('New version saved. It applies from its start date onward.')
      onDone()
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const toggleDay = (day: WeekDayName) =>
    setWorkingDays(current =>
      current.includes(day) ? current.filter(d => d !== day) : [...current, day])

  const dailyHours = workingDays.length > 0
    ? (weeklyHours / workingDays.length).toFixed(2)
    : '—'

  return (
    <Dialog open={open} onClose={onClose} title="New employment terms">
      <div className="space-y-4">
        <div className="space-y-1.5">
          <Label>Applies from</Label>
          <Input type="date" value={validFrom} onChange={e => setValidFrom(e.target.value)} />
          <p className="text-xs text-muted-foreground">
            Must be later than the current version's start date.
          </p>
        </div>

        <div className="space-y-1.5">
          <Label>Employment type</Label>
          <Select
            value={employmentType}
            onChange={e => changeType(e.target.value as EmploymentType)}
          >
            {(Object.keys(EMPLOYMENT_TYPE_LABELS) as EmploymentType[]).map(type => (
              <option key={type} value={type}>{EMPLOYMENT_TYPE_LABELS[type]}</option>
            ))}
          </Select>
          <p className="text-xs text-muted-foreground">
            Picking a type fills in a typical week. The hours below are what actually count — edit
            them to whatever the contract says.
          </p>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <Label>Weekly hours</Label>
            <Input
              type="number" step={0.5} min={0} max={60} value={weeklyHours}
              onChange={e => setWeeklyHours(Number(e.target.value))}
            />
          </div>
          <div className="space-y-1.5">
            <Label>Annual vacation days</Label>
            <Input
              type="number" step={0.5} min={0} value={annualVacationDays}
              onChange={e => setAnnualVacationDays(Number(e.target.value))}
            />
          </div>
        </div>

        <div className="space-y-1.5">
          <Label>Working days</Label>
          <div className="flex flex-wrap gap-1.5">
            {WEEK_DAYS.map(day => (
              <button
                key={day}
                type="button"
                onClick={() => toggleDay(day)}
                className={`px-2.5 py-1 rounded-md border text-xs font-medium transition-colors ${
                  workingDays.includes(day)
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'text-muted-foreground hover:bg-accent'
                }`}
              >
                {day.slice(0, 3)}
              </button>
            ))}
          </div>
          <p className="text-xs text-muted-foreground">
            {dailyHours} hours per working day — weekly hours spread over the days selected, so a
            four-day week means longer days, not shorter ones.
          </p>
        </div>

        <div className="space-y-3 rounded-lg border p-3">
          <div>
            <Label>Overtime arrangement</Label>
            <p className="text-xs text-muted-foreground mt-0.5">
              Optional. Choose at most one — an all-in salary already covers additional hours, so a
              lump sum on top would pay the same overtime twice.
            </p>
          </div>

          <label className="flex items-start gap-2 text-sm cursor-pointer">
            <input
              type="checkbox"
              className="mt-0.5"
              checked={isAllIn}
              onChange={e => { setIsAllIn(e.target.checked); if (e.target.checked) setLumpSum('') }}
            />
            <span>
              All-in contract
              <span className="block text-xs text-muted-foreground">
                Surplus hours are still recorded and reported, but never banked into the balance.
              </span>
            </span>
          </label>

          <div className="space-y-1.5">
            <Label>Überstundenpauschale (hours per month)</Label>
            <Input
              type="number" step={0.5} min={0} max={200} placeholder="none"
              value={lumpSum}
              disabled={isAllIn}
              onChange={e => setLumpSum(e.target.value)}
            />
            <p className="text-xs text-muted-foreground">
              The first hours of monthly surplus are already paid by the flat rate, so they are
              absorbed here instead of being carried forward. Anything above it still banks.
            </p>
          </div>
        </div>

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button
            disabled={
              workingDays.length === 0 || weeklyHours <= 0 || save.isPending ||
              (lumpSumHours !== null && (Number.isNaN(lumpSumHours) || lumpSumHours < 0))
            }
            onClick={() => save.mutate()}
          >
            Save version
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
