import { useState } from 'react'
import type { AbsenceKind, Worker } from './types'
import { absenceDot, fmtDate, workdaysBetween } from './types'
import { Card, EmptyRow, Field, INPUT_MONO, RemoveButton, SECTION_LABEL } from './primitives'
import type { UpdateWorker, WorkerActions } from './TeamSettingsShared'

const KINDS: AbsenceKind[] = ['Vacation', 'Sick leave', 'Special leave', 'Unpaid leave', 'Time off in lieu']

const ROW_GRID = 'grid grid-cols-[1.1fr_1fr_1fr_70px_110px_92px] gap-3 max-[860px]:grid-cols-2'

export function VacationTab({
  worker: w,
  upd,
  actions,
}: {
  worker: Worker
  upd: UpdateWorker
  actions: WorkerActions
}) {
  const [draft, setDraft] = useState<{ kind: AbsenceKind; from: string; to: string }>({
    kind: 'Vacation',
    from: '',
    to: '',
  })

  const isFreelancer = w.type === 'Freelancer'

  // Day counts come from the server (they respect the real working pattern and holidays).
  const vacation = w.absences.filter(a => a.kind === 'Vacation')
  const taken = vacation.filter(a => a.status === 'Approved').reduce((n, a) => n + a.days, 0)
  const pending = vacation.filter(a => a.status === 'Pending').reduce((n, a) => n + a.days, 0)
  const total = (Number(w.vacEnt) || 0) + (Number(w.vacCarry) || 0)
  const left = total - taken - pending
  const pct = (v: number) => (total > 0 ? `${Math.min(100, (v / total) * 100)}%` : '0%')

  function addAbsence() {
    if (!draft.from) return
    actions.addAbsence(draft.kind, draft.from, draft.to || draft.from)
    setDraft(d => ({ ...d, from: '', to: '' }))
  }

  return (
    <div className="flex flex-col gap-4">
      {isFreelancer ? (
        <div className="border border-dashed border-[color:var(--wtr-border3)] rounded-[14px] px-5 py-7 text-center flex flex-col gap-1.5">
          <span className="text-[15px] font-semibold">No vacation entitlement</span>
          <span className="text-[13px] text-[color:var(--wtr-text-sec)]">
            Freelancers manage their own time off. You can still note absences below so planning stays accurate.
          </span>
        </div>
      ) : (
        <Card className="p-5 flex flex-col gap-[18px]">
          <div className="flex items-start justify-between gap-4 flex-wrap">
            <div className="flex flex-col gap-1">
              <h2 className="m-0 text-[15px] font-semibold">Entitlement {new Date().getFullYear()}</h2>
              <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">
                In workdays. Saved with the worker — part-time entitlement scales with the days they work.
              </p>
            </div>
            <div className="flex gap-3">
              <Field label="Days / year">
                <input
                  type="number"
                  min={0}
                  value={w.vacEnt}
                  onChange={e => upd({ vacEnt: e.target.value })}
                  className={`${INPUT_MONO} !h-9 !w-24 !px-2.5`}
                />
              </Field>
              <Field label="Carried over">
                <input
                  type="number"
                  min={0}
                  value={w.vacCarry}
                  onChange={e => upd({ vacCarry: e.target.value })}
                  className={`${INPUT_MONO} !h-9 !w-24 !px-2.5`}
                />
              </Field>
            </div>
          </div>
          <div className="flex flex-col gap-2.5">
            <div className="flex h-2.5 rounded-full overflow-hidden bg-[color:var(--wtr-muted)] gap-0.5">
              <div className="h-full bg-[color:var(--wtr-accent)]" style={{ width: pct(taken) }} />
              <div className="h-full bg-[color:var(--wtr-bar-pending)]" style={{ width: pct(pending) }} />
            </div>
            <div className="flex gap-[22px] flex-wrap text-[13px] text-[color:var(--wtr-text-sec)]">
              <span className="flex items-center gap-[7px]">
                <span className="w-2 h-2 rounded-sm bg-[color:var(--wtr-accent)]" />
                Taken <span className="wtr-mono text-[color:var(--wtr-text)]">{taken}</span>
              </span>
              <span className="flex items-center gap-[7px]">
                <span className="w-2 h-2 rounded-sm bg-[color:var(--wtr-bar-pending)]" />
                Pending <span className="wtr-mono text-[color:var(--wtr-text)]">{pending}</span>
              </span>
              <span className="flex items-center gap-[7px]">
                <span className="w-2 h-2 rounded-sm bg-[color:var(--wtr-muted)] border border-[color:var(--wtr-border3)]" />
                Remaining <span className="wtr-mono" style={{ color: left < 0 ? 'var(--wtr-tint-orange-fg)' : 'var(--wtr-text)' }}>{left}</span> of {total}
              </span>
            </div>
          </div>
        </Card>
      )}

      <Card>
        <div className="px-5 py-4 flex flex-col gap-1 border-b border-[color:var(--wtr-border)]">
          <h2 className="m-0 text-[15px] font-semibold">Absences</h2>
          <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">
            Recorded immediately. Sick leave is approved on entry; everything else starts pending.
          </p>
        </div>

        {/* Add row */}
        <div className={`${ROW_GRID} items-end px-5 py-3.5 bg-[color:var(--wtr-head)] border-b border-[color:var(--wtr-border)]`}>
          <label className="flex flex-col gap-1.5">
            <span className={`${SECTION_LABEL} whitespace-nowrap`}>Type</span>
            <select
              value={draft.kind}
              onChange={e => setDraft(d => ({ ...d, kind: e.target.value as AbsenceKind }))}
              className="wtr-input h-9 w-full px-2.5 text-[13.5px] !bg-[color:var(--wtr-card)]"
            >
              {KINDS.map(k => (
                <option key={k} value={k}>
                  {k}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1.5">
            <span className={`${SECTION_LABEL} whitespace-nowrap`}>From</span>
            <input
              type="date"
              value={draft.from}
              onChange={e => setDraft(d => ({ ...d, from: e.target.value }))}
              className="wtr-input wtr-mono h-9 w-full px-2.5 text-[13px] !bg-[color:var(--wtr-card)]"
            />
          </label>
          <label className="flex flex-col gap-1.5">
            <span className={`${SECTION_LABEL} whitespace-nowrap`}>To</span>
            <input
              type="date"
              value={draft.to}
              onChange={e => setDraft(d => ({ ...d, to: e.target.value }))}
              className="wtr-input wtr-mono h-9 w-full px-2.5 text-[13px] !bg-[color:var(--wtr-card)]"
            />
          </label>
          <div className="flex flex-col gap-1.5">
            <span className={`${SECTION_LABEL} whitespace-nowrap`}>Days</span>
            <span className="wtr-mono h-9 flex items-center text-sm" title="Estimate — the server counts against the real working pattern">
              ~{workdaysBetween(draft.from, draft.to || draft.from)}
            </span>
          </div>
          <div className="max-[860px]:hidden" />
          <button onClick={addAbsence} className="wtr-primary h-9 !rounded-lg text-[13.5px]">
            Add
          </button>
        </div>

        {/* List */}
        {w.absences.map(a => {
          const ok = a.status === 'Approved'
          const rejected = a.status === 'Rejected'
          return (
            <div key={a.id} className={`${ROW_GRID} items-center px-5 py-[11px] border-b border-[color:var(--wtr-row-border)] wtr-row-hover`}>
              <span className="flex items-center gap-[9px] text-sm">
                <span className="w-[7px] h-[7px] rounded-full" style={{ background: absenceDot(a.kind) }} />
                {a.kind}
              </span>
              <span className="wtr-mono text-[13.5px] text-[color:var(--wtr-text2)]">{fmtDate(a.from)}</span>
              <span className="wtr-mono text-[13.5px] text-[color:var(--wtr-text2)]">{fmtDate(a.to)}</span>
              <span className="wtr-mono text-sm font-medium">{a.days}</span>
              <button
                onClick={() => {
                  if (a.status === 'Pending') actions.approveAbsence(a.id)
                }}
                title={a.status === 'Pending' ? 'Click to approve' : a.status}
                className="justify-self-start h-[26px] px-2.5 rounded-full text-xs font-semibold"
                style={{
                  cursor: a.status === 'Pending' ? 'pointer' : 'default',
                  border: `1px solid ${ok ? 'var(--wtr-tint-green-border)' : rejected ? 'var(--wtr-tint-red-border)' : 'var(--wtr-tint-orange-border)'}`,
                  background: ok ? 'var(--wtr-tint-green-bg)' : rejected ? 'var(--wtr-tint-red-bg)' : 'var(--wtr-tint-orange-bg)',
                  color: ok ? 'var(--wtr-tint-green-fg)' : rejected ? 'var(--wtr-tint-red-fg)' : 'var(--wtr-tint-orange-fg)',
                }}
              >
                {a.status}
              </button>
              <RemoveButton className="justify-self-end" onClick={() => actions.removeAbsence(a.id)} />
            </div>
          )
        })}
        {w.absences.length === 0 && <EmptyRow>No absences recorded yet.</EmptyRow>}
      </Card>
    </div>
  )
}
