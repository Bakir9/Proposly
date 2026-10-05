import { useState } from 'react'
import type { Absence, AbsenceKind, Worker } from './types'
import { absenceDot, fmtDate, workdaysBetween } from './types'
import { Card, EmptyRow, Field, INPUT_MONO, RemoveButton, SECTION_LABEL } from './primitives'
import type { UpdateWorker } from './TeamSettingsShared'

const KINDS: AbsenceKind[] = ['Vacation', 'Sick leave', 'Special leave', 'Unpaid leave', 'Time off in lieu']

const ROW_GRID = 'grid grid-cols-[1.1fr_1fr_1fr_70px_110px_92px] gap-3 max-[860px]:grid-cols-2'

export function VacationTab({ worker: w, upd }: { worker: Worker; upd: UpdateWorker }) {
  const [draft, setDraft] = useState<{ kind: AbsenceKind; from: string; to: string }>({
    kind: 'Vacation',
    from: '',
    to: '',
  })

  const isFreelancer = w.type === 'Freelancer'

  const vacation = w.absences.filter(a => a.kind === 'Vacation')
  const taken = vacation.filter(a => a.status === 'Approved').reduce((n, a) => n + workdaysBetween(a.from, a.to), 0)
  const pending = vacation.filter(a => a.status === 'Pending').reduce((n, a) => n + workdaysBetween(a.from, a.to), 0)
  const total = (Number(w.vacEnt) || 0) + (Number(w.vacCarry) || 0)
  const left = total - taken - pending
  const pct = (v: number) => (total > 0 ? `${Math.min(100, (v / total) * 100)}%` : '0%')

  function addAbsence() {
    if (!draft.from) return
    const to = draft.to || draft.from
    const absence: Absence = {
      kind: draft.kind,
      from: draft.from,
      to,
      status: draft.kind === 'Sick leave' ? 'Approved' : 'Pending',
    }
    upd(x => ({ absences: [...x.absences, absence] }))
    setDraft(d => ({ ...d, from: '', to: '' }))
  }

  return (
    <div className="flex flex-col gap-4">
      {isFreelancer ? (
        <div className="border border-dashed border-[#253352] rounded-[14px] px-5 py-7 text-center flex flex-col gap-1.5">
          <span className="text-[15px] font-semibold">No vacation entitlement</span>
          <span className="text-[13px] text-[#8C9AB4]">
            Freelancers manage their own time off. You can still note absences below so planning stays accurate.
          </span>
        </div>
      ) : (
        <Card className="p-5 flex flex-col gap-[18px]">
          <div className="flex items-start justify-between gap-4 flex-wrap">
            <div className="flex flex-col gap-1">
              <h2 className="m-0 text-[15px] font-semibold">Entitlement {new Date().getFullYear()}</h2>
              <p className="m-0 text-[13px] text-[#8C9AB4]">
                In workdays. Part-time entitlement scales with the days they work.
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
            <div className="flex h-2.5 rounded-full overflow-hidden bg-[#18223A] gap-0.5">
              <div className="h-full bg-[#5EC8F2]" style={{ width: pct(taken) }} />
              <div className="h-full bg-[#3C6E8F]" style={{ width: pct(pending) }} />
            </div>
            <div className="flex gap-[22px] flex-wrap text-[13px] text-[#8C9AB4]">
              <span className="flex items-center gap-[7px]">
                <span className="w-2 h-2 rounded-sm bg-[#5EC8F2]" />
                Taken <span className="wtr-mono text-[#E6ECF6]">{taken}</span>
              </span>
              <span className="flex items-center gap-[7px]">
                <span className="w-2 h-2 rounded-sm bg-[#3C6E8F]" />
                Pending <span className="wtr-mono text-[#E6ECF6]">{pending}</span>
              </span>
              <span className="flex items-center gap-[7px]">
                <span className="w-2 h-2 rounded-sm bg-[#18223A] border border-[#253352]" />
                Remaining <span className="wtr-mono" style={{ color: left < 0 ? '#F2A25E' : '#E6ECF6' }}>{left}</span> of {total}
              </span>
            </div>
          </div>
        </Card>
      )}

      <Card>
        <div className="px-5 py-4 flex flex-col gap-1 border-b border-[#162035]">
          <h2 className="m-0 text-[15px] font-semibold">Absences</h2>
          <p className="m-0 text-[13px] text-[#8C9AB4]">Weekends are excluded from day counts.</p>
        </div>

        {/* Add row */}
        <div className={`${ROW_GRID} items-end px-5 py-3.5 bg-[#0E1524] border-b border-[#162035]`}>
          <label className="flex flex-col gap-1.5">
            <span className={`${SECTION_LABEL} whitespace-nowrap`}>Type</span>
            <select
              value={draft.kind}
              onChange={e => setDraft(d => ({ ...d, kind: e.target.value as AbsenceKind }))}
              className="wtr-input h-9 w-full px-2.5 text-[13.5px] !bg-[#0A101C]"
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
              className="wtr-input wtr-mono h-9 w-full px-2.5 text-[13px] !bg-[#0A101C]"
            />
          </label>
          <label className="flex flex-col gap-1.5">
            <span className={`${SECTION_LABEL} whitespace-nowrap`}>To</span>
            <input
              type="date"
              value={draft.to}
              onChange={e => setDraft(d => ({ ...d, to: e.target.value }))}
              className="wtr-input wtr-mono h-9 w-full px-2.5 text-[13px] !bg-[#0A101C]"
            />
          </label>
          <div className="flex flex-col gap-1.5">
            <span className={`${SECTION_LABEL} whitespace-nowrap`}>Days</span>
            <span className="wtr-mono h-9 flex items-center text-sm">{workdaysBetween(draft.from, draft.to || draft.from)}</span>
          </div>
          <div className="max-[860px]:hidden" />
          <button onClick={addAbsence} className="wtr-primary h-9 !rounded-lg text-[13.5px]">
            Add
          </button>
        </div>

        {/* List */}
        {w.absences.map((a, i) => {
          const ok = a.status === 'Approved'
          return (
            <div key={i} className={`${ROW_GRID} items-center px-5 py-[11px] border-b border-[#0F1727] wtr-row-hover`}>
              <span className="flex items-center gap-[9px] text-sm">
                <span className="w-[7px] h-[7px] rounded-full" style={{ background: absenceDot(a.kind) }} />
                {a.kind}
              </span>
              <span className="wtr-mono text-[13.5px] text-[#C3CDDF]">{fmtDate(a.from)}</span>
              <span className="wtr-mono text-[13.5px] text-[#C3CDDF]">{fmtDate(a.to)}</span>
              <span className="wtr-mono text-sm font-medium">{workdaysBetween(a.from, a.to)}</span>
              <button
                onClick={() =>
                  upd(x => ({
                    absences: x.absences.map((b, j) =>
                      j === i ? { ...b, status: b.status === 'Approved' ? 'Pending' : 'Approved' } : b,
                    ),
                  }))
                }
                className="justify-self-start h-[26px] px-2.5 rounded-full text-xs font-semibold cursor-pointer"
                style={{
                  border: `1px solid ${ok ? '#1E4A38' : '#4A3420'}`,
                  background: ok ? '#11281F' : '#2A1D12',
                  color: ok ? '#63D9A4' : '#F2A25E',
                }}
              >
                {a.status}
              </button>
              <RemoveButton
                className="justify-self-end"
                onClick={() => upd(x => ({ absences: x.absences.filter((_, j) => j !== i) }))}
              />
            </div>
          )
        })}
        {w.absences.length === 0 && <EmptyRow>No absences recorded yet.</EmptyRow>}
      </Card>
    </div>
  )
}
