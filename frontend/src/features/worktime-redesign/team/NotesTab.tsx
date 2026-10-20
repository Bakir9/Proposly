import { useState } from 'react'
import type { NoteTag, Worker } from './types'
import { fmtDate } from './types'
import { Card, EmptyRow, Segmented } from './primitives'
import type { WorkerActions } from './TeamSettingsShared'

const TAGS: readonly NoteTag[] = ['General', 'HR', 'Contract', 'Performance']

export function NotesTab({ worker: w, actions }: { worker: Worker; actions: WorkerActions }) {
  const [draft, setDraft] = useState('')
  const [tag, setTag] = useState<NoteTag>('General')

  function addNote() {
    const text = draft.trim()
    if (!text) return
    actions.addNote(tag, text)
    setDraft('')
  }

  const sorted = [...w.notes].sort(
    (a, b) => Number(b.pinned) - Number(a.pinned) || (b.date > a.date ? 1 : -1),
  )

  return (
    <Card>
      <div className="px-5 py-4 flex flex-col gap-1 border-b border-[color:var(--wtr-border)]">
        <h2 className="m-0 text-[15px] font-semibold">Notes</h2>
        <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">Internal only — not visible to the worker.</p>
      </div>

      <div className="px-5 py-4 flex flex-col gap-3 bg-[color:var(--wtr-head)] border-b border-[color:var(--wtr-border)]">
        <textarea
          value={draft}
          rows={3}
          placeholder="Write a note…"
          onChange={e => setDraft(e.target.value)}
          className="wtr-input w-full resize-y px-3 py-2.5 text-sm leading-normal !bg-[color:var(--wtr-card)]"
        />
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <Segmented options={TAGS} value={tag} onChange={setTag} className="!bg-[color:var(--wtr-card)]" />
          <button onClick={addNote} className="wtr-primary h-9 px-[18px] !rounded-lg text-[13.5px] whitespace-nowrap">
            Add note
          </button>
        </div>
      </div>

      {sorted.map(n => (
        <div
          key={n.id}
          className="px-5 py-3.5 border-b border-[color:var(--wtr-row-border)] flex flex-col gap-2"
          style={{ background: n.pinned ? 'var(--wtr-pinned)' : 'transparent' }}
        >
          <div className="flex items-center gap-2.5 flex-wrap">
            <span className="text-[11px] font-semibold tracking-[0.4px] px-2 py-0.5 rounded-full text-[color:var(--wtr-tint-cyan-fg)] bg-[color:var(--wtr-tint-cyan-bg)] whitespace-nowrap">
              {n.tag}
            </span>
            <span className="wtr-mono text-[12.5px] text-[color:var(--wtr-text-sec)]">{fmtDate(n.date)}</span>
            <span className="flex-1" />
            <button
              onClick={() => actions.pinNote(n.id, !n.pinned)}
              className="h-7 px-2.5 bg-transparent rounded-lg text-[12.5px] cursor-pointer"
              style={{
                border: `1px solid ${n.pinned ? 'var(--wtr-border-active)' : 'var(--wtr-border2)'}`,
                color: n.pinned ? 'var(--wtr-accent)' : 'var(--wtr-text-sec)',
              }}
            >
              {n.pinned ? 'Pinned' : 'Pin'}
            </button>
            <button
              onClick={() => actions.deleteNote(n.id)}
              className="wtr-ghost h-7 px-2.5 !rounded-lg text-[12.5px]"
            >
              Remove
            </button>
          </div>
          <p className="m-0 text-sm leading-relaxed text-[color:var(--wtr-text)] whitespace-pre-wrap">{n.text}</p>
        </div>
      ))}
      {w.notes.length === 0 && <EmptyRow>No notes yet.</EmptyRow>}
    </Card>
  )
}
