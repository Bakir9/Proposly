import { useState } from 'react'
import { Field, INPUT } from './primitives'

/* Small modals in the page's own visual language. */

function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) {
  return (
    <div className="wtr fixed inset-0 z-50 flex items-center justify-center p-6" style={{ background: 'rgba(4,7,13,0.7)' }} onClick={onClose}>
      <div
        className="w-full max-w-md border border-[color:var(--wtr-border3)] rounded-[14px] bg-[color:var(--wtr-panel)] p-5 flex flex-col gap-4"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-center justify-between">
          <h2 className="m-0 text-[16px] font-semibold">{title}</h2>
          <button onClick={onClose} className="wtr-ghost w-7 h-7 !rounded-md text-base leading-none">×</button>
        </div>
        {children}
      </div>
    </div>
  )
}

export function AddWorkerDialog({
  open,
  pending,
  onClose,
  onAdd,
}: {
  open: boolean
  pending: boolean
  onClose: () => void
  onAdd: (firstName: string, lastName: string, jobTitle: string | null) => void
}) {
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [jobTitle, setJobTitle] = useState('')

  if (!open) return null
  const canAdd = firstName.trim().length > 0 && lastName.trim().length > 0

  return (
    <Modal title="Add worker" onClose={onClose}>
      <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">
        Creates the person without login access — they can be assigned to projects right away.
        You can invite them to Proposly later.
      </p>
      <div className="grid grid-cols-2 gap-3">
        <Field label="First name">
          <input type="text" value={firstName} onChange={e => setFirstName(e.target.value)} className={INPUT} />
        </Field>
        <Field label="Last name">
          <input type="text" value={lastName} onChange={e => setLastName(e.target.value)} className={INPUT} />
        </Field>
      </div>
      <Field label="Role" hint="optional">
        <input type="text" value={jobTitle} onChange={e => setJobTitle(e.target.value)} placeholder="e.g. Site engineer" className={INPUT} />
      </Field>
      <div className="flex justify-end gap-2">
        <button onClick={onClose} className="wtr-ghost h-9 px-3.5 text-[13px]">Cancel</button>
        <button
          onClick={() => onAdd(firstName.trim(), lastName.trim(), jobTitle.trim() || null)}
          disabled={!canAdd || pending}
          className="wtr-primary h-9 px-4 !rounded-lg text-[13.5px] disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {pending ? 'Adding…' : 'Add worker'}
        </button>
      </div>
    </Modal>
  )
}

export function InviteWorkerDialog({
  open,
  pending,
  workerName,
  onClose,
  onInvite,
}: {
  open: boolean
  pending: boolean
  workerName: string
  onClose: () => void
  onInvite: (email: string, role: string) => void
}) {
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('Member')

  if (!open) return null
  const canInvite = /.+@.+\..+/.test(email)

  return (
    <Modal title={`Invite ${workerName || 'worker'}`} onClose={onClose}>
      <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">
        Sends an invite email. Once accepted, they can log in — and start counting toward your
        plan's user limit.
      </p>
      <Field label="Email">
        <input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="name@company.at" className={INPUT} />
      </Field>
      <Field label="Role">
        <select value={role} onChange={e => setRole(e.target.value)} className={`${INPUT} px-2.5`}>
          <option value="Member">Member — sees only their own projects</option>
          <option value="Admin">Admin — manages everything</option>
        </select>
      </Field>
      <div className="flex justify-end gap-2">
        <button onClick={onClose} className="wtr-ghost h-9 px-3.5 text-[13px]">Cancel</button>
        <button
          onClick={() => onInvite(email.trim().toLowerCase(), role)}
          disabled={!canInvite || pending}
          className="wtr-primary h-9 px-4 !rounded-lg text-[13.5px] disabled:opacity-40 disabled:cursor-not-allowed"
        >
          {pending ? 'Sending…' : 'Send invite'}
        </button>
      </div>
    </Modal>
  )
}
