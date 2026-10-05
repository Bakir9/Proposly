import { useState } from 'react'
import type { NoteTag, Worker } from './types'
import { fmtDate, today } from './types'
import { Card, EmptyRow, Segmented } from './primitives'
import type { UpdateWorker } from './TeamSettingsShared'

const TAGS: readonly NoteTag[] = ['General', 'HR', 'Contract', 'Performance']

export function NotesTab({ worker: w, upd }: { worker: Worker; upd: UpdateWorker }) {
  const [draft, setDraft] = useState('')
  const [tag, setTag] = useState<NoteTag>('General')

  function addNote() {
    const text = draft.trim()
    if (!text) return
    upd(x => ({ notes: [{ tag, text, date: today(), pinned: false }, ...x.notes] }))
    setDraft('')
  }

  // Pinned first, then newest first — the index into w.notes is kept for updates.
  const sorted = w.notes
    .map((n, i) => ({ n, i }))
    .sort((a, b) => Number(b.n.pinned) - Number(a.n.pinned) || (b.n.date > a.n.date ? 1 : -1))

  return (
    <Card>
      <div className="px-5 py-4 flex flex-col gap-1 border-b border-[#162035]">
        <h2 className="m-0 text-[15px] font-semibold">Notes</h2>
        <p className="m-0 text-[13px] text-[#8C9AB4]">Internal only — not visible to the worker.</p>
      </div>

      <div className="px-5 py-4 flex flex-col gap-3 bg-[#0E1524] border-b border-[#162035]">
        <textarea
          value={draft}
          rows={3}
          placeholder="Write a note…"
          onChange={e => setDraft(e.target.value)}
          className="wtr-input w-full resize-y px-3 py-2.5 text-sm leading-normal !bg-[#0A101C]"
        />
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <Segmented options={TAGS} value={tag} onChange={setTag} className="!bg-[#0A101C]" />
          <button onClick={addNote} className="wtr-primary h-9 px-[18px] !rounded-lg text-[13.5px] whitespace-nowrap">
            Add note
          </button>
        </div>
      </div>

      {sorted.map(({ n, i }) => (
        <div
          key={i}
          className="px-5 py-3.5 border-b border-[#0F1727] flex flex-col gap-2"
          style={{ background: n.pinned ? '#0C1626' : 'transparent' }}
        >
          <div className="flex items-center gap-2.5 flex-wrap">
            <span className="text-[11px] font-semibold tracking-[0.4px] px-2 py-0.5 rounded-full text-[#9BE0FA] bg-[#12253A] whitespace-nowrap">
              {n.tag}
            </span>
            <span className="wtr-mono text-[12.5px] text-[#8C9AB4]">{fmtDate(n.date)}</span>
            <span className="flex-1" />
            <button
              onClick={() => upd(x => ({ notes: x.notes.map((m, j) => (j === i ? { ...m, pinned: !m.pinned } : m)) }))}
              className="h-7 px-2.5 bg-transparent rounded-lg text-[12.5px] cursor-pointer"
              style={{
                border: `1px solid ${n.pinned ? '#28405C' : '#1C2740'}`,
                color: n.pinned ? '#5EC8F2' : '#8C9AB4',
              }}
            >
              {n.pinned ? 'Pinned' : 'Pin'}
            </button>
            <button
              onClick={() => upd(x => ({ notes: x.notes.filter((_, j) => j !== i) }))}
              className="wtr-ghost h-7 px-2.5 !rounded-lg text-[12.5px]"
            >
              Remove
            </button>
          </div>
          <p className="m-0 text-sm leading-relaxed text-[#E6ECF6] whitespace-pre-wrap">{n.text}</p>
        </div>
      ))}
      {w.notes.length === 0 && <EmptyRow>No notes yet.</EmptyRow>}
    </Card>
  )
}
