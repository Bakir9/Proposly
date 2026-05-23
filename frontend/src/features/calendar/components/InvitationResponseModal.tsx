import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { X } from 'lucide-react'
import { respondToInvitation, proposeReschedule, type TerminDetail } from '@/api/calendar'
import { Button } from '@/components/ui/button'

interface Props {
  termin: TerminDetail
  onClose: () => void
}

type View = 'main' | 'propose'

function toLocalInput(iso: string) {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function InvitationResponseModal({ termin, onClose }: Props) {
  const qc = useQueryClient()
  const [view, setView] = useState<View>('main')
  const [propStart, setPropStart] = useState(toLocalInput(termin.start))
  const [propEnd, setPropEnd] = useState(toLocalInput(termin.end))
  const [message, setMessage] = useState('')

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['termins'] })
    qc.invalidateQueries({ queryKey: ['termin', termin.id] })
    qc.invalidateQueries({ queryKey: ['termins-pending'] })
  }

  const { mutate: accept, isPending: accepting } = useMutation({
    mutationFn: () => respondToInvitation(termin.id, 'Accept'),
    onSuccess: () => { invalidate(); toast.success('You accepted the invitation.'); onClose() },
    onError: () => toast.error('Failed to respond.'),
  })

  const { mutate: decline, isPending: declining } = useMutation({
    mutationFn: () => respondToInvitation(termin.id, 'Decline'),
    onSuccess: () => { invalidate(); toast.success('You declined the invitation.'); onClose() },
    onError: () => toast.error('Failed to respond.'),
  })

  const { mutate: propose, isPending: proposing } = useMutation({
    mutationFn: () => proposeReschedule(termin.id,
      new Date(propStart).toISOString(),
      new Date(propEnd).toISOString(),
      message.trim() || undefined),
    onSuccess: () => { invalidate(); toast.success('Reschedule proposal sent.'); onClose() },
    onError: () => toast.error('Failed to send proposal.'),
  })

  const start = new Date(termin.start)
  const end = new Date(termin.end)
  const dateStr = start.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })
  const timeStr = `${start.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })} – ${end.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}`

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="bg-card rounded-xl shadow-xl w-full max-w-md">
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <h2 className="text-lg font-semibold">{view === 'propose' ? 'Propose New Time' : 'Respond to Invitation'}</h2>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground"><X className="h-5 w-5" /></button>
        </div>

        {view === 'main' && (
          <div className="px-6 py-4 space-y-4">
            <div>
              <p className="font-medium">{termin.title}</p>
              <p className="text-sm text-muted-foreground mt-0.5">{dateStr}</p>
              <p className="text-sm text-muted-foreground">{timeStr}</p>
              {termin.location && <p className="text-sm text-muted-foreground">{termin.location}</p>}
            </div>
            <p className="text-sm">Organized by <span className="font-medium">{termin.organizerName}</span></p>

            <div className="flex flex-col gap-2 pt-2">
              <Button onClick={() => accept()} disabled={accepting} className="bg-green-600 hover:bg-green-700 text-white">
                {accepting ? 'Accepting…' : 'Accept'}
              </Button>
              <Button variant="outline" onClick={() => setView('propose')}>
                Propose New Time
              </Button>
              <Button variant="outline" className="text-red-500 border-red-200 hover:bg-red-50 hover:text-red-600"
                onClick={() => decline()} disabled={declining}>
                {declining ? 'Declining…' : 'Decline'}
              </Button>
            </div>
          </div>
        )}

        {view === 'propose' && (
          <div className="px-6 py-4 space-y-4">
            <p className="text-sm text-muted-foreground">Suggest an alternative time for <span className="font-medium text-foreground">{termin.title}</span>.</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-sm font-medium">Proposed start</label>
                <input type="datetime-local" value={propStart} onChange={e => setPropStart(e.target.value)}
                  className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm" />
              </div>
              <div>
                <label className="text-sm font-medium">Proposed end</label>
                <input type="datetime-local" value={propEnd} onChange={e => setPropEnd(e.target.value)}
                  className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm" />
              </div>
            </div>
            <div>
              <label className="text-sm font-medium">Message (optional)</label>
              <textarea value={message} onChange={e => setMessage(e.target.value)} rows={2}
                className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm resize-none"
                placeholder="Reason or note for the organizer" />
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setView('main')}>Back</Button>
              <Button onClick={() => propose()} disabled={proposing}>
                {proposing ? 'Sending…' : 'Send Proposal'}
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
