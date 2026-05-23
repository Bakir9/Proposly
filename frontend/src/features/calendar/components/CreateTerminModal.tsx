import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { X, AlertTriangle } from 'lucide-react'
import { createTermin, getTermins } from '@/api/calendar'
import { getActiveUsers } from '@/api/users'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/features/auth/AuthContext'

interface Props {
  defaultStart?: Date
  onClose: () => void
}

function toLocalInput(d: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function CreateTerminModal({ defaultStart, onClose }: Props) {
  const qc = useQueryClient()
  const { user } = useAuth()

  const start = defaultStart ?? (() => { const d = new Date(); d.setMinutes(0, 0, 0); return d })()
  const end = new Date(start.getTime() + 60 * 60 * 1000)

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [location, setLocation] = useState('')
  const [startVal, setStartVal] = useState(toLocalInput(start))
  const [endVal, setEndVal] = useState(toLocalInput(end))
  const [dateError, setDateError] = useState<string | null>(null)
  const [selectedInvitees, setSelectedInvitees] = useState<string[]>([])

  const handleStartChange = (val: string) => {
    setStartVal(val)
    if (val && endVal) {
      const s = new Date(val)
      const e = new Date(endVal)
      if (s >= e) {
        const newEnd = new Date(s.getTime() + 60 * 60 * 1000)
        setEndVal(toLocalInput(newEnd))
        setDateError(null)
      }
    }
  }

  const handleEndChange = (val: string) => {
    setEndVal(val)
    if (val && startVal) {
      const s = new Date(startVal)
      const e = new Date(val)
      setDateError(e <= s ? 'End time must be after start time' : null)
    }
  }

  const { data: activeUsers } = useQuery({ queryKey: ['active-users'], queryFn: getActiveUsers })
  const invitableUsers = activeUsers?.filter(u => u.id !== user?.userId) ?? []

  const proposedStart = startVal ? new Date(startVal) : null
  const proposedEnd = endVal ? new Date(endVal) : null
  const validRange = !!(proposedStart && proposedEnd && proposedStart < proposedEnd)

  const { data: rangeTermins = [] } = useQuery({
    queryKey: ['termins-conflict', startVal, endVal],
    queryFn: () => getTermins(proposedStart!, proposedEnd!),
    enabled: validRange,
    staleTime: 30_000,
  })

  const conflicts = validRange
    ? rangeTermins.filter(t =>
        t.status === 'Scheduled' &&
        t.myInvitationStatus !== 'Declined' &&
        new Date(t.start) < proposedEnd! &&
        new Date(t.end) > proposedStart!
      )
    : []

  const { mutate, isPending } = useMutation({
    mutationFn: () => createTermin({
      title: title.trim(),
      description: description.trim() || null,
      start: new Date(startVal).toISOString(),
      end: new Date(endVal).toISOString(),
      location: location.trim() || null,
      inviteeUserIds: selectedInvitees,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['termins'] })
      qc.invalidateQueries({ queryKey: ['termins-pending'] })
      toast.success('Meeting created.')
      onClose()
    },
    onError: () => toast.error('Failed to create meeting.'),
  })

  const toggleInvitee = (id: string) =>
    setSelectedInvitees(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="bg-card rounded-xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <h2 className="text-lg font-semibold">New Meeting</h2>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground"><X className="h-5 w-5" /></button>
        </div>

        <div className="px-6 py-4 space-y-4">
          <div>
            <label className="text-sm font-medium">Title *</label>
            <input
              autoFocus
              value={title}
              onChange={e => setTitle(e.target.value)}
              className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm"
              placeholder="Meeting title"
            />
          </div>

          <div className="space-y-1">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-sm font-medium">Start *</label>
                <input type="datetime-local" value={startVal} onChange={e => handleStartChange(e.target.value)}
                  className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm" />
              </div>
              <div>
                <label className="text-sm font-medium">End *</label>
                <input type="datetime-local" value={endVal} onChange={e => handleEndChange(e.target.value)}
                  className={`mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm ${dateError ? 'border-red-500' : ''}`} />
              </div>
            </div>
            {dateError && <p className="text-xs text-red-500">{dateError}</p>}
          </div>

          <div>
            <label className="text-sm font-medium">Location</label>
            <input value={location} onChange={e => setLocation(e.target.value)}
              className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm"
              placeholder="e.g. Conference room B, Zoom" />
          </div>

          <div>
            <label className="text-sm font-medium">Description</label>
            <textarea value={description} onChange={e => setDescription(e.target.value)} rows={3}
              className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm resize-none"
              placeholder="Optional agenda or notes" />
          </div>

          {invitableUsers.length > 0 && (
            <div>
              <label className="text-sm font-medium">Invite participants</label>
              <div className="mt-2 space-y-1.5 max-h-40 overflow-y-auto">
                {invitableUsers.map(u => (
                  <label key={u.id} className="flex items-center gap-2.5 cursor-pointer rounded-md px-2 py-1.5 hover:bg-muted">
                    <input type="checkbox" checked={selectedInvitees.includes(u.id)}
                      onChange={() => toggleInvitee(u.id)} className="rounded" />
                    <span className="text-sm">{u.fullName}</span>
                    <span className="text-xs text-muted-foreground ml-auto">{u.role}</span>
                  </label>
                ))}
              </div>
            </div>
          )}
        </div>

        {conflicts.length > 0 && (
          <div className="mx-6 mb-2 rounded-lg border border-amber-300 bg-amber-50 dark:bg-amber-950/30 dark:border-amber-700 p-3">
            <div className="flex items-start gap-2">
              <AlertTriangle className="h-4 w-4 text-amber-500 shrink-0 mt-0.5" />
              <div className="text-sm">
                <p className="font-medium text-amber-700 dark:text-amber-400">Time conflict</p>
                <p className="text-xs text-amber-600 dark:text-amber-500 mt-0.5">This slot overlaps with:</p>
                <ul className="mt-1 space-y-0.5">
                  {conflicts.map(t => (
                    <li key={t.id} className="text-xs text-amber-700 dark:text-amber-400">
                      • {t.title} ({new Date(t.start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}–{new Date(t.end).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })})
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        )}

        <div className="flex justify-end gap-2 px-6 py-4 border-t">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          {conflicts.length > 0 ? (
            <Button
              onClick={() => mutate()}
              disabled={!title.trim() || !!dateError || isPending}
              className="bg-amber-500 hover:bg-amber-600 text-white"
            >
              {isPending ? 'Creating…' : 'Create anyway'}
            </Button>
          ) : (
            <Button onClick={() => mutate()} disabled={!title.trim() || !!dateError || isPending}>
              {isPending ? 'Creating…' : 'Create Meeting'}
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
