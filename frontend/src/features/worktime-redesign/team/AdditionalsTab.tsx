import type { OvertimeMode, Worker } from './types'
import { AddButton, Card, CardHeader, EmptyRow, Field, INPUT_MONO, RemoveButton, Segmented, Toggle } from './primitives'
import type { UpdateWorker } from './TeamSettingsShared'

const OT_MODES: readonly OvertimeMode[] = ['Paid out', 'Time off', 'All-in']
const ALLOWANCE_UNITS = ['per day', 'per month', 'per hour', 'per km'] as const

export function AdditionalsTab({ worker: w, upd }: { worker: Worker; upd: UpdateWorker }) {
  const isFreelancer = w.type === 'Freelancer'

  const surcharges: { key: 'otPct' | 'night' | 'sunday' | 'holiday'; label: string }[] = [
    { key: 'otPct', label: 'Overtime surcharge' },
    { key: 'night', label: 'Night (22–06)' },
    { key: 'sunday', label: 'Sunday' },
    { key: 'holiday', label: 'Public holiday' },
  ]

  const toggles: { key: 'tracking' | 'autoBreak' | 'flex' | 'onCall'; label: string; desc: string }[] = [
    { key: 'tracking', label: 'Time tracking required', desc: 'They record start, end and break every workday.' },
    { key: 'autoBreak', label: 'Auto-deduct break', desc: '30 min deducted when a day exceeds 6 h without a recorded break.' },
    { key: 'flex', label: 'Flexitime', desc: 'Free start and end times around core hours; balance carries over.' },
    { key: 'onCall', label: 'On-call duty', desc: 'Can be scheduled for standby shifts outside regular hours.' },
  ]

  return (
    <div className="flex flex-col gap-4">
      {/* Overtime */}
      <Card className="p-5 flex flex-col gap-[18px]">
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div className="flex flex-col gap-1">
            <h2 className="m-0 text-[15px] font-semibold">Overtime</h2>
            <p className="m-0 text-[13px] text-[#8C9AB4]">How hours beyond the schedule are compensated.</p>
          </div>
          <Segmented options={OT_MODES} value={w.ot} onChange={ot => upd({ ot })} />
        </div>
        <div className="grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] gap-3.5">
          {surcharges.map(s => (
            <Field key={s.key} label={s.label}>
              <div className="relative">
                <input
                  type="number"
                  min={0}
                  step={5}
                  value={w[s.key]}
                  disabled={isFreelancer}
                  onChange={e => upd({ [s.key]: e.target.value } as Partial<Worker>)}
                  className={`${INPUT_MONO} !pr-8`}
                  style={{ opacity: isFreelancer ? 0.4 : 1 }}
                />
                <span className="absolute right-3 top-[11px] text-[12.5px] text-[#8C9AB4] pointer-events-none">%</span>
              </div>
            </Field>
          ))}
        </div>
      </Card>

      {/* Time rules */}
      <Card>
        <CardHeader title="Time rules" />
        {toggles.map(t => (
          <div key={t.key} className="flex items-center justify-between gap-4 px-5 py-3.5 border-b border-[#0F1727] flex-wrap">
            <div className="flex flex-col gap-[3px] flex-1 min-w-[220px]">
              <span className="text-sm font-medium">{t.label}</span>
              <span className="text-[13px] text-[#8C9AB4]">{t.desc}</span>
            </div>
            <Toggle on={!!w[t.key]} onClick={() => upd({ [t.key]: !w[t.key] } as Partial<Worker>)} />
          </div>
        ))}
        {w.flex && (
          <div className="flex items-center gap-2.5 px-5 py-3.5 bg-[#0C1322] border-b border-[#0F1727] flex-wrap">
            <span className="text-[13px] text-[#8C9AB4]">Core hours</span>
            <input
              type="time"
              value={w.coreFrom}
              onChange={e => upd({ coreFrom: e.target.value })}
              className="wtr-input wtr-mono h-[34px] px-2 !rounded-[7px] text-[13.5px]"
            />
            <span className="text-[13px] text-[#8C9AB4]">to</span>
            <input
              type="time"
              value={w.coreTo}
              onChange={e => upd({ coreTo: e.target.value })}
              className="wtr-input wtr-mono h-[34px] px-2 !rounded-[7px] text-[13.5px]"
            />
          </div>
        )}
        <div className="grid grid-cols-[repeat(auto-fit,minmax(180px,1fr))] gap-3.5 px-5 py-4">
          <Field label="Home office days / week">
            <input
              type="number"
              min={0}
              max={5}
              value={w.homeOffice}
              onChange={e => upd({ homeOffice: e.target.value })}
              className={INPUT_MONO}
            />
          </Field>
          <Field label="Max hours / day">
            <input
              type="number"
              min={0}
              max={12}
              step={0.5}
              value={w.maxDay}
              onChange={e => upd({ maxDay: e.target.value })}
              className={INPUT_MONO}
            />
          </Field>
        </div>
      </Card>

      {/* Allowances */}
      <Card>
        <CardHeader
          title="Allowances"
          sub="Recurring extras paid with working time."
          action={
            <AddButton onClick={() => upd(x => ({ allowances: [...x.allowances, { name: '', amount: '', unit: 'per day' }] }))} />
          }
        />
        {w.allowances.map((al, i) => {
          const set = (patch: Partial<typeof al>) =>
            upd(x => ({ allowances: x.allowances.map((b, j) => (j === i ? { ...b, ...patch } : b)) }))
          return (
            <div
              key={i}
              className="grid grid-cols-[minmax(0,1fr)_120px_140px_84px] gap-3 items-center px-5 py-2.5 border-b border-[#0F1727] max-[760px]:grid-cols-2"
            >
              <input
                type="text"
                value={al.name}
                placeholder="e.g. Travel allowance"
                onChange={e => set({ name: e.target.value })}
                className="wtr-input h-9 w-full px-3 text-sm"
              />
              <div className="relative">
                <input
                  type="number"
                  min={0}
                  step={0.5}
                  value={al.amount}
                  placeholder="0"
                  onChange={e => set({ amount: e.target.value })}
                  className="wtr-input wtr-mono h-9 w-full pl-3 pr-7 text-sm"
                />
                <span className="absolute right-3 top-2.5 text-[12.5px] text-[#8C9AB4] pointer-events-none">€</span>
              </div>
              <select
                value={al.unit}
                onChange={e => set({ unit: e.target.value as (typeof ALLOWANCE_UNITS)[number] })}
                className="wtr-input h-9 w-full px-2.5 text-[13.5px]"
              >
                {ALLOWANCE_UNITS.map(u => (
                  <option key={u} value={u}>
                    {u}
                  </option>
                ))}
              </select>
              <RemoveButton
                className="justify-self-end"
                onClick={() => upd(x => ({ allowances: x.allowances.filter((_, j) => j !== i) }))}
              />
            </div>
          )
        })}
        {w.allowances.length === 0 && <EmptyRow>No allowances.</EmptyRow>}
      </Card>
    </div>
  )
}
