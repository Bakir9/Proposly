import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getNonWorkingDays, createNonWorkingDay, updateNonWorkingDay, deleteNonWorkingDay,
  KIND_LABELS, type NonWorkingDay, type NonWorkingDayKind,
} from '@/api/worktime-calendar'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Dialog } from '@/components/ui/dialog'
import { CalendarDays, Plus, Trash2, Pencil, Info } from 'lucide-react'

const KIND_BADGE: Record<NonWorkingDayKind, string> = {
  PublicHoliday: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20',
  CompanyClosure: 'bg-amber-500/15 text-amber-400 border-amber-500/20',
}

export function HolidayCalendarPage() {
  const queryClient = useQueryClient()
  const [year, setYear] = useState(new Date().getFullYear())
  const [adding, setAdding] = useState(false)
  const [editing, setEditing] = useState<NonWorkingDay | null>(null)

  const from = `${year}-01-01`
  const to = `${year}-12-31`

  const { data: days, isLoading } = useQuery({
    queryKey: ['worktime', 'non-working-days', year],
    queryFn: () => getNonWorkingDays(from, to),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['worktime'] })
    // Target hours and absence day counts both depend on this calendar.
    queryClient.invalidateQueries({ queryKey: ['timesheet'] })
    queryClient.invalidateQueries({ queryKey: ['absences'] })
  }

  const remove = useMutation({
    mutationFn: (id: string) => deleteNonWorkingDay(id),
    onSuccess: () => { toast.success('Day removed.'); invalidate() },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  return (
    <div className="p-6 space-y-6 max-w-4xl">
      <div className="flex items-start justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <CalendarDays className="h-6 w-6" />
            Holidays and closures
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Days nobody is expected to work. These shape everyone's target hours and how many days
            an absence costs.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Select value={String(year)} onChange={e => setYear(Number(e.target.value))}>
            {[year - 1, year, year + 1].map(y => (
              <option key={y} value={y}>{y}</option>
            ))}
          </Select>
          <Button onClick={() => setAdding(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Add day
          </Button>
        </div>
      </div>

      <div className="rounded-md border border-blue-500/30 bg-blue-500/5 p-3 flex gap-2 text-xs text-muted-foreground">
        <Info className="h-4 w-4 shrink-0 text-blue-400" />
        <span>
          A <strong>public holiday</strong> costs the employee nothing. A <strong>company
          closure</strong> normally comes out of their own vacation allowance — turn that off for a
          bridge day you are granting outright. Days inside a month that has already been approved
          cannot be changed; reopen the month first.
        </span>
      </div>

      {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}

      {!isLoading && (days?.length ?? 0) === 0 && (
        <div className="rounded-lg border p-8 text-center text-sm text-muted-foreground">
          No days recorded for {year}. Target hours will treat every weekday as a working day.
        </div>
      )}

      {(days?.length ?? 0) > 0 && (
        <div className="rounded-lg border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr className="text-left">
                <th className="px-4 py-2 font-medium">Date</th>
                <th className="px-4 py-2 font-medium">Name</th>
                <th className="px-4 py-2 font-medium">Kind</th>
                <th className="px-4 py-2 font-medium">Costs the employee</th>
                <th className="px-4 py-2 font-medium w-28" />
              </tr>
            </thead>
            <tbody>
              {days!.map(day => (
                <tr key={day.id} className="border-t">
                  <td className="px-4 py-2 tabular-nums whitespace-nowrap">
                    {day.date}
                    <span className="text-muted-foreground ml-2 text-xs">
                      {new Date(day.date).toLocaleDateString(undefined, { weekday: 'short' })}
                    </span>
                  </td>
                  <td className="px-4 py-2 font-medium">{day.name}</td>
                  <td className="px-4 py-2">
                    <span className={`text-xs px-2 py-0.5 rounded-md border font-medium ${KIND_BADGE[day.kind]}`}>
                      {KIND_LABELS[day.kind]}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-muted-foreground">
                    {day.consumesVacation ? 'one vacation day' : 'nothing'}
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" size="sm" onClick={() => setEditing(day)}>
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      <Button
                        variant="ghost" size="sm"
                        disabled={remove.isPending}
                        onClick={() => remove.mutate(day.id)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <DayDialog
        open={adding}
        year={year}
        onClose={() => setAdding(false)}
        onDone={() => { setAdding(false); invalidate() }}
      />

      <DayDialog
        open={editing !== null}
        year={year}
        existing={editing}
        onClose={() => setEditing(null)}
        onDone={() => { setEditing(null); invalidate() }}
      />
    </div>
  )
}

function DayDialog({
  open, year, existing, onClose, onDone,
}: {
  open: boolean
  year: number
  existing?: NonWorkingDay | null
  onClose: () => void
  onDone: () => void
}) {
  const [date, setDate] = useState(`${year}-01-01`)
  const [name, setName] = useState('')
  const [kind, setKind] = useState<NonWorkingDayKind>('PublicHoliday')
  const [consumesVacation, setConsumesVacation] = useState(false)

  // Seed from the row being edited without syncing state in an effect.
  const [seededFor, setSeededFor] = useState<string | null>(null)
  if (open && existing && seededFor !== existing.id) {
    setSeededFor(existing.id)
    setDate(existing.date)
    setName(existing.name)
    setKind(existing.kind)
    setConsumesVacation(existing.consumesVacation)
  }
  if (!open && seededFor !== null) setSeededFor(null)

  const save = useMutation({
    mutationFn: async () => {
      if (existing) {
        await updateNonWorkingDay(existing.id, { name, kind, consumesVacation })
      } else {
        await createNonWorkingDay({ date, name, kind, consumesVacation })
      }
    },
    onSuccess: () => { toast.success(existing ? 'Day updated.' : 'Day added.'); onDone() },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const onKindChange = (next: NonWorkingDayKind) => {
    setKind(next)
    // A public holiday can never cost the employee a day, so the choice is not offered.
    setConsumesVacation(next === 'CompanyClosure')
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={existing ? 'Edit non-working day' : 'Add non-working day'}
    >
      <div className="space-y-4">
        <div className="space-y-1.5">
          <Label>Date</Label>
          <Input
            type="date" value={date} disabled={existing != null}
            onChange={e => setDate(e.target.value)}
          />
          {existing && (
            <p className="text-xs text-muted-foreground">
              The date cannot be changed. Delete this entry and add a new one to move it.
            </p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label>Name</Label>
          <Input
            value={name} maxLength={200}
            onChange={e => setName(e.target.value)}
            placeholder="Staatsfeiertag, Betriebsurlaub, …"
          />
        </div>

        <div className="space-y-1.5">
          <Label>Kind</Label>
          <Select value={kind} onChange={e => onKindChange(e.target.value as NonWorkingDayKind)}>
            <option value="PublicHoliday">{KIND_LABELS.PublicHoliday}</option>
            <option value="CompanyClosure">{KIND_LABELS.CompanyClosure}</option>
          </Select>
        </div>

        {kind === 'CompanyClosure' && (
          <label className="flex items-start gap-2 text-sm">
            <input
              type="checkbox"
              className="mt-0.5"
              checked={consumesVacation}
              onChange={e => setConsumesVacation(e.target.checked)}
            />
            <span>
              Comes out of the employee's vacation allowance
              <span className="block text-xs text-muted-foreground">
                Uncheck for a bridge day the company grants outright.
              </span>
            </span>
          </label>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button
            disabled={name.trim() === '' || save.isPending}
            onClick={() => save.mutate()}
          >
            {existing ? 'Save changes' : 'Add day'}
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
