import { useRef, useState } from 'react'
import type { ScheduleDay, Worker, WorkerDraft, WorkModel, WorkerType } from './types'
import { FULL_TIME_HOURS, fmtDate, fmtNum, fmtSize, weeklyHours } from './types'
import { AddButton, Card, Field, FileIcon, INPUT, INPUT_MONO, SECTION_LABEL, Segmented } from './primitives'
import type { UpdateWorker, WorkerActions } from './TeamSettingsShared'

const TYPES: readonly WorkerType[] = ['Employee', 'Freelancer', 'Intern', 'Apprentice', 'Marginal']
const MODELS: readonly WorkModel[] = ['Full-time', 'Part-time', 'Hourly']
const DOC_CATEGORIES = ['Amendment', 'Previous contract', 'ID / Work permit', 'Certificate', 'Payroll', 'Invoice', 'Other']

const scheduleDays = (h: number, activeDays: number): ScheduleDay[] =>
  ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'].map((d, i) => ({ d, on: i < activeDays, h: i < activeDays ? h : 0 }))

export function ContractTab({
  worker: w,
  upd,
  actions,
}: {
  worker: Worker
  upd: UpdateWorker
  actions: WorkerActions
}) {
  const isFreelancer = w.type === 'Freelancer'
  const isHourly = w.model === 'Hourly'
  const wk = weeklyHours(w)
  const [spread, setSpread] = useState('')

  const ev = (field: keyof WorkerDraft) => (e: React.ChangeEvent<HTMLInputElement>) =>
    upd({ [field]: e.target.value } as Partial<WorkerDraft>)

  const modelHint = isHourly
    ? 'No fixed schedule — hours come from time tracking.'
    : w.model === 'Part-time'
      ? "Set the days and hours they're contracted for."
      : `Standard full-time is ${FULL_TIME_HOURS} h / week.`

  function pickModel(m: WorkModel) {
    upd(x => {
      if (m === 'Full-time') return { model: m, days: scheduleDays(FULL_TIME_HOURS / 5, 5) }
      if (m === 'Part-time' && x.model !== 'Part-time') return { model: m, days: scheduleDays(5, 4) }
      return { model: m }
    })
  }

  function applySpread() {
    const total = Number(spread === '' ? wk : spread) || 0
    upd(x => {
      const active = x.days.filter(d => d.on).length || 1
      return { days: x.days.map(d => ({ ...d, h: d.on ? Math.round((total / active) * 100) / 100 : 0 })) }
    })
    setSpread('')
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Type of work */}
      <Card className="p-5 flex flex-col gap-[18px]">
        <div className="flex flex-col gap-1">
          <h2 className="m-0 text-[15px] font-semibold">Type of work</h2>
          <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">
            Decides which rules apply — freelancers have no vacation entitlement or surcharges.
          </p>
        </div>
        <Segmented
          options={TYPES}
          value={w.type}
          onChange={t => upd(t === 'Freelancer' ? { type: t, model: 'Hourly' } : { type: t })}
          className="self-start"
        />
        <div className="grid grid-cols-[repeat(auto-fit,minmax(180px,1fr))] gap-3.5">
          <Field label="Start date">
            <input type="date" value={w.start} onChange={ev('start')} className={INPUT_MONO} />
          </Field>
          <Field label="End date" hint="optional">
            <input type="date" value={w.end} onChange={ev('end')} className={INPUT_MONO} />
          </Field>
          <Field label="Email" hint={w.accountStatus === 'ProfileOnly' ? 'set when inviting' : w.accountStatus.toLowerCase()}>
            <input type="email" value={w.email ?? ''} disabled readOnly placeholder="No login access" className={`${INPUT} opacity-60`} />
          </Field>
          {isFreelancer && (
            <Field label="Hourly rate">
              <div className="relative">
                <input
                  type="number"
                  min={0}
                  step={1}
                  value={w.rate}
                  onChange={ev('rate')}
                  placeholder="0"
                  className={`${INPUT_MONO} !pr-11`}
                />
                <span className="absolute right-3 top-[11px] text-[12.5px] text-[color:var(--wtr-text-sec)] pointer-events-none">€ / h</span>
              </div>
            </Field>
          )}
        </div>
      </Card>

      {/* Working hours */}
      <Card className="p-5 flex flex-col gap-[18px]">
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div className="flex flex-col gap-1">
            <h2 className="m-0 text-[15px] font-semibold">Working hours</h2>
            <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">{modelHint}</p>
          </div>
          <Segmented options={MODELS} value={w.model} onChange={pickModel} />
        </div>

        {!isHourly && (
          <div className="flex flex-col gap-3">
            <div className="grid grid-cols-7 gap-2 max-[760px]:grid-cols-4">
              {w.days.map((d, i) => (
                <DayCell
                  key={d.d}
                  day={d}
                  onToggle={() =>
                    upd(x => ({
                      days: x.days.map((y, j) => (j === i ? { d: y.d, on: !y.on, h: !y.on ? y.h || 8 : y.h } : y)),
                    }))
                  }
                  onHours={value =>
                    upd(x => ({ days: x.days.map((y, j) => (j === i ? { ...y, h: value } : y)) }))
                  }
                />
              ))}
            </div>
            <div className="flex items-center justify-between gap-3 flex-wrap">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-[13px] text-[color:var(--wtr-text-sec)]">Spread</span>
                <input
                  type="number"
                  min={0}
                  step={0.5}
                  value={spread === '' ? fmtNum(wk) : spread}
                  onChange={e => setSpread(e.target.value)}
                  className="wtr-input wtr-mono h-8 w-[84px] px-2 text-[13.5px]"
                />
                <span className="text-[13px] text-[color:var(--wtr-text-sec)]">h / week evenly across active days</span>
                <button onClick={applySpread} className="wtr-btn h-8 px-3 text-[12.5px] font-medium">
                  Apply
                </button>
              </div>
              <span className="text-[13px] text-[color:var(--wtr-text-sec)]">
                Total <span className="wtr-mono text-[color:var(--wtr-text)] font-medium">{fmtNum(wk)} h</span>
              </span>
            </div>
          </div>
        )}

        {isHourly && (
          <div className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-3.5">
            <Field label="Max hours / month">
              <input type="number" min={0} value={w.maxMonth} onChange={ev('maxMonth')} placeholder="No limit" className={INPUT_MONO} />
            </Field>
            <Field label="Expected hours / week">
              <input type="number" min={0} value={w.expectedWeek} onChange={ev('expectedWeek')} placeholder="Varies" className={INPUT_MONO} />
            </Field>
          </div>
        )}
      </Card>

      <DocumentsCard worker={w} actions={actions} />
    </div>
  )
}

function DayCell({
  day: d,
  onToggle,
  onHours,
}: {
  day: ScheduleDay
  onToggle: () => void
  onHours: (value: string) => void
}) {
  return (
    <div
      className="flex flex-col gap-1.5 p-2.5 rounded-[10px]"
      style={{ border: `1px solid ${d.on ? 'var(--wtr-border-active)' : 'var(--wtr-border)'}`, background: d.on ? 'var(--wtr-input)' : 'transparent' }}
    >
      <button
        onClick={onToggle}
        className="flex items-center justify-between gap-1 bg-transparent border-0 p-0 text-[13px] font-semibold cursor-pointer"
        style={{ color: d.on ? 'var(--wtr-text)' : 'var(--wtr-label)' }}
      >
        {d.d}
        <span
          className="w-3.5 h-3.5 rounded grid place-items-center text-[10px] leading-none text-[color:var(--wtr-on-accent)]"
          style={{
            border: `1px solid ${d.on ? 'var(--wtr-accent)' : 'var(--wtr-border4)'}`,
            background: d.on ? 'var(--wtr-accent)' : 'transparent',
          }}
        >
          {d.on ? '✓' : ''}
        </span>
      </button>
      <input
        type="number"
        min={0}
        max={12}
        step={0.25}
        value={d.on ? d.h : ''}
        disabled={!d.on}
        onChange={e => onHours(e.target.value)}
        className="wtr-input wtr-mono h-[34px] w-full px-2 !rounded-[7px] text-[13.5px]"
        style={{ opacity: d.on ? 1 : 0.35 }}
      />
    </div>
  )
}

function DocumentsCard({ worker: w, actions }: { worker: Worker; actions: WorkerActions }) {
  const contractRef = useRef<HTMLInputElement>(null)
  const otherRef = useRef<HTMLInputElement>(null)
  const [dragZone, setDragZone] = useState<'' | 'contract' | 'other'>('')

  const contract = w.docs.find(d => d.category === 'Contract')
  const otherDocs = w.docs.filter(d => d.category !== 'Contract')

  function addFiles(list: FileList | null, category: string) {
    const files = Array.from(list ?? [])
    if (files.length) actions.uploadDocuments(files, category)
  }

  const zone = (name: 'contract' | 'other') => ({
    onDragOver: (e: React.DragEvent) => {
      e.preventDefault()
      if (dragZone !== name) setDragZone(name)
    },
    onDragLeave: () => setDragZone(''),
    onDrop: (e: React.DragEvent) => {
      e.preventDefault()
      setDragZone('')
      addFiles(e.dataTransfer.files, name === 'contract' ? 'Contract' : 'Other')
    },
  })

  return (
    <Card>
      <input
        ref={contractRef}
        type="file"
        accept=".pdf,.doc,.docx,.png,.jpg,.jpeg"
        className="hidden"
        onChange={e => {
          addFiles(e.target.files, 'Contract')
          e.target.value = ''
        }}
      />
      <input
        ref={otherRef}
        type="file"
        multiple
        accept=".pdf,.doc,.docx,.png,.jpg,.jpeg"
        className="hidden"
        onChange={e => {
          addFiles(e.target.files, 'Other')
          e.target.value = ''
        }}
      />

      <div className="px-5 py-4 flex items-center justify-between gap-3 border-b border-[color:var(--wtr-border)] flex-wrap">
        <div className="flex flex-col gap-1">
          <h2 className="m-0 text-[15px] font-semibold">Documents</h2>
          <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">Contract and other files for this person. PDF, Word or images.</p>
        </div>
        <AddButton label="Upload file" onClick={() => otherRef.current?.click()} />
      </div>

      {/* Contract slot */}
      <div className="px-5 py-4 border-b border-[color:var(--wtr-border)] flex flex-col gap-2.5">
        <span className={SECTION_LABEL}>{w.type === 'Freelancer' ? 'Framework agreement' : 'Signed contract'}</span>
        {contract ? (
          <div
            {...zone('contract')}
            className="flex items-center gap-3.5 px-4 py-3.5 rounded-[10px] bg-[color:var(--wtr-input)] flex-wrap"
            style={{ border: `1px solid ${dragZone === 'contract' ? 'var(--wtr-accent)' : 'var(--wtr-border3)'}` }}
          >
            <FileIcon />
            <div className="flex-1 min-w-[180px] flex flex-col gap-[3px]">
              <span className="text-sm font-medium break-all">{contract.name}</span>
              <span className="text-[12.5px] text-[color:var(--wtr-text-sec)]">
                {fmtSize(contract.size)} · uploaded {fmtDate(contract.date)}
              </span>
            </div>
            <div className="flex gap-2 flex-wrap">
              <button
                onClick={() => actions.downloadDocument(contract.id, contract.name)}
                className="wtr-ghost h-8 px-3 text-[12.5px] font-medium"
              >
                Download
              </button>
              <button onClick={() => contractRef.current?.click()} className="wtr-ghost h-8 px-3 text-[12.5px] font-medium">
                Replace
              </button>
            </div>
          </div>
        ) : (
          <button
            onClick={() => contractRef.current?.click()}
            {...zone('contract')}
            className="w-full px-4 py-[22px] rounded-[10px] bg-transparent text-[color:var(--wtr-text)] flex flex-col items-center gap-1.5 cursor-pointer"
            style={{ border: `1px dashed ${dragZone === 'contract' ? 'var(--wtr-accent)' : 'var(--wtr-border3)'}` }}
          >
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="var(--wtr-accent)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 16V5M7 10l5-5 5 5M5 20h14" />
            </svg>
            <span className="text-sm font-medium">Upload signed contract</span>
            <span className="text-[12.5px] text-[color:var(--wtr-text-sec)]">Drop a file here or click to browse</span>
          </button>
        )}
      </div>

      {/* Other files */}
      <div {...zone('other')} style={{ background: dragZone === 'other' ? 'var(--wtr-pinned)' : 'transparent' }}>
        <div className="px-5 pt-3 pb-1.5">
          <span className={SECTION_LABEL}>Other files</span>
        </div>
        {otherDocs.map(d => (
          <div key={d.id} className="flex items-center gap-3 px-5 py-2.5 border-b border-[color:var(--wtr-row-border)] flex-wrap wtr-row-hover">
            <FileIcon size={18} muted />
            <div className="flex-1 min-w-[160px] flex flex-col gap-0.5">
              <span className="text-[13.5px] font-medium break-all">{d.name}</span>
              <span className="text-xs text-[color:var(--wtr-text-sec)]">
                {fmtSize(d.size)} · uploaded {fmtDate(d.date)}
              </span>
            </div>
            <select
              value={d.category}
              onChange={e => actions.setDocumentCategory(d.id, e.target.value)}
              className="wtr-input h-8 w-[150px] px-2 text-[13px]"
            >
              {DOC_CATEGORIES.map(c => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
            <div className="flex gap-1.5">
              <button
                onClick={() => actions.downloadDocument(d.id, d.name)}
                className="wtr-ghost h-8 px-3 text-[12.5px] font-medium"
              >
                Download
              </button>
              <button
                onClick={() => actions.deleteDocument(d.id)}
                className="wtr-ghost h-8 px-3 text-[12.5px] font-medium"
              >
                Remove
              </button>
            </div>
          </div>
        ))}
        {otherDocs.length === 0 && (
          <div
            className="mx-5 mt-1.5 mb-4 p-4 rounded-[10px] text-center text-[13px] text-[color:var(--wtr-text-sec)]"
            style={{ border: `1px dashed ${dragZone === 'other' ? 'var(--wtr-accent)' : 'var(--wtr-border3)'}` }}
          >
            Drop ID, permits, certificates or amendments here.
          </div>
        )}
      </div>
    </Card>
  )
}
