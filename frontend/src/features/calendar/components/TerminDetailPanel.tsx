import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { X, MapPin, Clock, Users, Calendar, CheckCircle, XCircle, AlertCircle, Pencil, Trash2 } from 'lucide-react'
import {
  getTerminById, getUserAvailability, cancelTermin, deleteTermin, respondToProposal,
  rescheduleTermin, updateTermin,
  type TerminDetail, type InvitationDetail,
} from '@/api/calendar'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/features/auth/AuthContext'
import { InvitationResponseModal } from './InvitationResponseModal'

const STATUS_ICON: Record<string, React.ReactNode> = {
  Accepted: <CheckCircle className="h-3.5 w-3.5 text-green-500" />,
  Declined: <XCircle className="h-3.5 w-3.5 text-red-500" />,
  Pending: <Clock className="h-3.5 w-3.5 text-yellow-500" />,
  RescheduleProposed: <AlertCircle className="h-3.5 w-3.5 text-blue-500" />,
}

const STATUS_LABEL: Record<string, string> = {
  Accepted: 'Accepted',
  Declined: 'Declined',
  Pending: 'Pending',
  RescheduleProposed: 'Proposed reschedule',
}

function toLocalInput(iso: string) {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

interface AvailabilityMiniProps { userId: string; userName: string; weekStart: Date }
function AvailabilityMini({ userId, userName, weekStart }: AvailabilityMiniProps) {
  const weekEnd = new Date(weekStart.getTime() + 7 * 24 * 60 * 60 * 1000)
  const { data } = useQuery({
    queryKey: ['availability', userId, weekStart.toISOString()],
    queryFn: () => getUserAvailability(userId, weekStart, weekEnd),
  })
  if (!data || data.termins.length === 0)
    return <p className="text-xs text-muted-foreground">No meetings this week.</p>
  return (
    <div className="space-y-1">
      {data.termins.map(t => {
        const s = new Date(t.start)
        const e = new Date(t.end)
        return (
          <div key={t.id} className="flex items-center gap-2 text-xs">
            <div className="w-1.5 h-1.5 rounded-full bg-blue-400 shrink-0" />
            <span className="truncate font-medium">{t.title}</span>
            <span className="text-muted-foreground ml-auto shrink-0">
              {s.toLocaleDateString('en-US', { weekday: 'short' })} {s.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}–{e.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
            </span>
          </div>
        )
      })}
    </div>
  )
}

interface Props {
  terminId: string
  onClose: () => void
}

export function TerminDetailPanel({ terminId, onClose }: Props) {
  const { user } = useAuth()
  const qc = useQueryClient()

  const { data: termin, isLoading } = useQuery({
    queryKey: ['termin', terminId],
    queryFn: () => getTerminById(terminId),
  })

  const [showResponseModal, setShowResponseModal] = useState(false)
  const [editMode, setEditMode] = useState(false)
  const [editTitle, setEditTitle] = useState('')
  const [editDesc, setEditDesc] = useState('')
  const [editLoc, setEditLoc] = useState('')
  const [rescheduleMode, setRescheduleMode] = useState(false)
  const [newStart, setNewStart] = useState('')
  const [newEnd, setNewEnd] = useState('')
  const [expandedInvitee, setExpandedInvitee] = useState<string | null>(null)

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['termins'] })
    qc.invalidateQueries({ queryKey: ['termin', terminId] })
    qc.invalidateQueries({ queryKey: ['termins-pending'] })
  }

  const { mutate: doCancel } = useMutation({
    mutationFn: () => cancelTermin(terminId),
    onSuccess: () => { invalidate(); toast.success('Meeting cancelled.') },
    onError: () => toast.error('Failed to cancel.'),
  })

  const { mutate: doDelete } = useMutation({
    mutationFn: () => deleteTermin(terminId),
    onSuccess: () => { invalidate(); toast.success('Meeting deleted.'); onClose() },
    onError: () => toast.error('Failed to delete.'),
  })

  const { mutate: doUpdate, isPending: updating } = useMutation({
    mutationFn: () => updateTermin(terminId, { title: editTitle, description: editDesc || null, location: editLoc || null }),
    onSuccess: () => { invalidate(); setEditMode(false); toast.success('Meeting updated.') },
    onError: () => toast.error('Failed to update.'),
  })

  const { mutate: doReschedule, isPending: rescheduling } = useMutation({
    mutationFn: () => rescheduleTermin(terminId, new Date(newStart).toISOString(), new Date(newEnd).toISOString()),
    onSuccess: () => { invalidate(); setRescheduleMode(false); toast.success('Meeting rescheduled.') },
    onError: () => toast.error('Failed to reschedule.'),
  })

  const { mutate: doRespondProposal } = useMutation({
    mutationFn: ({ inviteeId, accept }: { inviteeId: string; accept: boolean }) =>
      respondToProposal(terminId, inviteeId, accept),
    onSuccess: (_, { accept }) => { invalidate(); toast.success(accept ? 'Proposal accepted — meeting rescheduled.' : 'Proposal declined.') },
    onError: () => toast.error('Failed.'),
  })

  if (isLoading) return (
    <div className="w-full h-full flex items-center justify-center">
      <p className="text-sm text-muted-foreground">Loading…</p>
    </div>
  )
  if (!termin) return null

  const isOrganizer = termin.myRole === 'Organizer'
  const isScheduled = termin.status === 'Scheduled'
  const weekStart = (() => { const d = new Date(termin.start); d.setHours(0, 0, 0, 0); d.setDate(d.getDate() - d.getDay()); return d })()

  const proposals = termin.invitations.filter(i => i.status === 'RescheduleProposed')
  const visibleInvitations = termin.invitations.filter(inv => inv.inviteeId !== user?.userId)

  const startEditMode = () => {
    setEditTitle(termin.title)
    setEditDesc(termin.description ?? '')
    setEditLoc(termin.location ?? '')
    setEditMode(true)
  }

  const startReschedule = () => {
    setNewStart(toLocalInput(termin.start))
    setNewEnd(toLocalInput(termin.end))
    setRescheduleMode(true)
  }

  return (
    <div className="flex flex-col h-full overflow-hidden bg-card border-l">
      {/* Header */}
      <div className="flex items-start justify-between px-5 py-4 border-b gap-2 shrink-0">
        <div className="min-w-0">
          {editMode ? (
            <input value={editTitle} onChange={e => setEditTitle(e.target.value)}
              className="w-full rounded border bg-background px-2 py-1 text-base font-semibold" />
          ) : (
            <h2 className="text-base font-semibold truncate">{termin.title}</h2>
          )}
          <span className={`inline-block mt-1 text-xs px-2 py-0.5 rounded-full font-medium ${
            termin.status === 'Scheduled' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
              : 'bg-muted text-muted-foreground'}`}>
            {termin.status}
          </span>
        </div>
        <button onClick={onClose} className="text-muted-foreground hover:text-foreground shrink-0 mt-1">
          <X className="h-4 w-4" />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto px-5 py-4 space-y-5">
        {/* Time & location */}
        <div className="space-y-2 text-sm">
          {rescheduleMode ? (
            <div className="space-y-2">
              <div>
                <label className="text-xs font-medium text-muted-foreground">New start</label>
                <input type="datetime-local" value={newStart} onChange={e => setNewStart(e.target.value)}
                  className="mt-1 w-full rounded border bg-background px-2 py-1 text-sm" />
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground">New end</label>
                <input type="datetime-local" value={newEnd} onChange={e => setNewEnd(e.target.value)}
                  className="mt-1 w-full rounded border bg-background px-2 py-1 text-sm" />
              </div>
              <div className="flex gap-2">
                <Button size="sm" onClick={() => doReschedule()} disabled={rescheduling}>Save</Button>
                <Button size="sm" variant="outline" onClick={() => setRescheduleMode(false)}>Cancel</Button>
              </div>
            </div>
          ) : (
            <>
              <div className="flex items-center gap-2 text-muted-foreground">
                <Calendar className="h-3.5 w-3.5 shrink-0" />
                <span>{new Date(termin.start).toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })}</span>
              </div>
              <div className="flex items-center gap-2 text-muted-foreground">
                <Clock className="h-3.5 w-3.5 shrink-0" />
                <span>
                  {new Date(termin.start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                  {' – '}
                  {new Date(termin.end).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                </span>
              </div>
            </>
          )}

          {editMode ? (
            <div>
              <label className="text-xs font-medium text-muted-foreground">Location</label>
              <input value={editLoc} onChange={e => setEditLoc(e.target.value)}
                className="mt-1 w-full rounded border bg-background px-2 py-1 text-sm" placeholder="Location" />
            </div>
          ) : termin.location && (
            <div className="flex items-center gap-2 text-muted-foreground">
              <MapPin className="h-3.5 w-3.5 shrink-0" />
              <span>{termin.location}</span>
            </div>
          )}
        </div>

        {/* Description */}
        {editMode ? (
          <div>
            <label className="text-xs font-medium text-muted-foreground">Description</label>
            <textarea value={editDesc} onChange={e => setEditDesc(e.target.value)} rows={3}
              className="mt-1 w-full rounded border bg-background px-2 py-1 text-sm resize-none" />
          </div>
        ) : termin.description && (
          <p className="text-sm text-muted-foreground">{termin.description}</p>
        )}

        {editMode && (
          <div className="flex gap-2">
            <Button size="sm" onClick={() => doUpdate()} disabled={updating}>Save changes</Button>
            <Button size="sm" variant="outline" onClick={() => setEditMode(false)}>Cancel</Button>
          </div>
        )}

        {/* Organizer actions */}
        {isOrganizer && isScheduled && !editMode && !rescheduleMode && (
          <div className="flex flex-wrap gap-2">
            <Button size="sm" variant="outline" onClick={startEditMode}>
              <Pencil className="h-3 w-3 mr-1" /> Edit
            </Button>
            <Button size="sm" variant="outline" onClick={startReschedule}>
              <Clock className="h-3 w-3 mr-1" /> Reschedule
            </Button>
            <Button size="sm" variant="outline" className="text-red-500 border-red-200"
              onClick={() => { if (confirm('Cancel this meeting?')) doCancel() }}>
              Cancel meeting
            </Button>
            <Button size="sm" variant="outline" className="text-red-600 border-red-200"
              onClick={() => { if (confirm('Delete this meeting permanently?')) doDelete() }}>
              <Trash2 className="h-3 w-3 mr-1" /> Delete
            </Button>
          </div>
        )}

        {/* Invitee actions */}
        {!isOrganizer && isScheduled && (termin.myInvitationStatus === 'Pending' || termin.myInvitationStatus === 'RescheduleProposed') && (
          <Button size="sm" onClick={() => setShowResponseModal(true)} className="w-full">
            Respond to invitation
          </Button>
        )}

        {/* Reschedule proposals (for organizer) */}
        {isOrganizer && proposals.length > 0 && (
          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground mb-2">Reschedule Proposals</h3>
            <div className="space-y-3">
              {proposals.map(inv => (
                <div key={inv.id} className="rounded-lg border p-3 space-y-1.5 bg-blue-50/50 dark:bg-blue-950/20">
                  <p className="text-sm font-medium">{inv.inviteeName}</p>
                  <p className="text-xs text-muted-foreground">
                    {new Date(inv.proposedStart!).toLocaleString('en-US', { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' })}
                    {' – '}
                    {new Date(inv.proposedEnd!).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                  </p>
                  {inv.proposedMessage && <p className="text-xs italic text-muted-foreground">"{inv.proposedMessage}"</p>}
                  <div className="flex gap-2 pt-1">
                    <Button size="sm" className="bg-green-600 hover:bg-green-700 text-white h-7 text-xs"
                      onClick={() => doRespondProposal({ inviteeId: inv.inviteeId, accept: true })}>
                      Accept & reschedule
                    </Button>
                    <Button size="sm" variant="outline" className="h-7 text-xs"
                      onClick={() => doRespondProposal({ inviteeId: inv.inviteeId, accept: false })}>
                      Decline
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Participants */}
        <div>
          <div className="flex items-center gap-1.5 mb-2">
            <Users className="h-3.5 w-3.5 text-muted-foreground" />
            <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Participants ({visibleInvitations.length + 1})
            </h3>
          </div>

          {/* Organizer row */}
          <div className="flex items-center gap-2 py-1.5 text-sm">
            <div className="w-6 h-6 rounded-full bg-primary/10 flex items-center justify-center text-[10px] font-bold text-primary shrink-0">
              {termin.organizerName.charAt(0)}
            </div>
            <span className="flex-1 truncate">{termin.organizerName}</span>
            <span className="text-xs text-muted-foreground">Organizer</span>
          </div>

          {/* Invitee rows — current user excluded */}
          {visibleInvitations.map(inv => (
            <div key={inv.id}>
              <div
                className="flex items-center gap-2 py-1.5 text-sm cursor-pointer hover:bg-muted/50 rounded px-1 -mx-1"
                onClick={() => setExpandedInvitee(expandedInvitee === inv.inviteeId ? null : inv.inviteeId)}
              >
                <div className="w-6 h-6 rounded-full bg-muted flex items-center justify-center text-[10px] font-bold shrink-0">
                  {inv.inviteeName.charAt(0)}
                </div>
                <span className="flex-1 truncate">{inv.inviteeName}</span>
                <span className="flex items-center gap-1 text-xs text-muted-foreground">
                  {STATUS_ICON[inv.status]}
                  {STATUS_LABEL[inv.status]}
                </span>
              </div>

              {expandedInvitee === inv.inviteeId && (
                <div className="ml-8 mb-2 px-3 py-2 rounded bg-muted/50 border">
                  <p className="text-xs font-medium mb-1.5">
                    {inv.inviteeName}'s schedule this week
                  </p>
                  <AvailabilityMini userId={inv.inviteeId} userName={inv.inviteeName} weekStart={weekStart} />
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      {showResponseModal && termin && (
        <InvitationResponseModal termin={termin} onClose={() => setShowResponseModal(false)} />
      )}
    </div>
  )
}
