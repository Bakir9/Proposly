import { useRef, useState } from 'react'
import { Plus, Users } from 'lucide-react'
import './redesign.css'
import type { Worker, WorkerType } from './team/types'
import {
  DEFAULT_VACATION_DAYS,
  FULL_TIME_HOURS,
  SEED_WORKERS,
  baseWorker,
  fmtNum,
  initialsOf,
  scheduleDays,
  typeChip,
  weeklyHours,
  workdaysBetween,
} from './team/types'
import type { UpdateWorker } from './team/TeamSettingsShared'
import { ProfileTab } from './team/ProfileTab'
import { CvTab } from './team/CvTab'
import { ContractTab } from './team/ContractTab'
import { VacationTab } from './team/VacationTab'
import { AdditionalsTab } from './team/AdditionalsTab'
import { NotesTab } from './team/NotesTab'

const TABS = ['Profile', 'CV', 'Contract', 'Vacation & absences', 'Additionals', 'Notes'] as const
type Tab = (typeof TABS)[number]

const FILTERS: readonly (WorkerType | 'All')[] = ['All', 'Employee', 'Freelancer', 'Intern', 'Apprentice', 'Marginal']

export function TeamSettingsPage() {
  const [workers, setWorkers] = useState<Worker[]>(SEED_WORKERS)
  const [selId, setSelId] = useState(1)
  const [tab, setTab] = useState<Tab>('Profile')
  const [query, setQuery] = useState('')
  const [filter, setFilter] = useState<(typeof FILTERS)[number]>('All')
  const headerPhotoRef = useRef<HTMLInputElement>(null)

  const sel = workers.find(w => w.id === selId) ?? workers[0]

  const upd: UpdateWorker = patch => {
    setWorkers(prev =>
      prev.map(w => (w.id === sel.id ? { ...w, ...(typeof patch === 'function' ? patch(w) : patch) } : w)),
    )
  }

  function addWorker() {
    const id = Math.max(...workers.map(w => w.id)) + 1
    const fresh = baseWorker({
      id,
      days: scheduleDays(FULL_TIME_HOURS / 5, 5),
      vacEnt: DEFAULT_VACATION_DAYS,
      start: new Date().toISOString().slice(0, 10),
    })
    setWorkers(prev => [...prev, fresh])
    setSelId(id)
    setTab('Profile')
    setFilter('All')
    setQuery('')
  }

  const q = query.trim().toLowerCase()
  const visible = workers.filter(
    w => (filter === 'All' || w.type === filter) && (!q || `${w.name} ${w.role}`.toLowerCase().includes(q)),
  )

  const wk = weeklyHours(sel)
  const isHourly = sel.model === 'Hourly'
  const isFreelancer = sel.type === 'Freelancer'

  const vacation = sel.absences.filter(a => a.kind === 'Vacation')
  const taken = vacation.filter(a => a.status === 'Approved').reduce((n, a) => n + workdaysBetween(a.from, a.to), 0)
  const pending = vacation.filter(a => a.status === 'Pending').reduce((n, a) => n + workdaysBetween(a.from, a.to), 0)
  const vacLeft = (Number(sel.vacEnt) || 0) + (Number(sel.vacCarry) || 0) - taken - pending

  return (
    <div className="wtr min-h-full px-8 pt-7 pb-16">
      <div className="max-w-[1280px] mx-auto flex flex-col gap-[22px]">
        {/* Page header */}
        <div className="flex items-start justify-between gap-6 flex-wrap">
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-[11px]">
              <Users className="h-[22px] w-[22px] text-[#5EC8F2]" strokeWidth={1.8} />
              <h1 className="m-0 text-[25px] font-semibold tracking-[-0.4px]">Team</h1>
            </div>
            <p className="m-0 text-sm text-[#8C9AB4] max-w-[56ch]">
              Contracts, working hours, vacation and time rules for each person.
            </p>
          </div>
          <button onClick={addWorker} className="wtr-primary h-10 px-[18px] flex items-center gap-2 whitespace-nowrap flex-none text-sm">
            <Plus className="h-3.5 w-3.5" strokeWidth={2.4} />
            Add worker
          </button>
        </div>

        <div className="flex gap-[18px] items-start flex-wrap">
          {/* Worker list */}
          <div className="flex-[1_1_260px] max-w-[320px] border border-[#162035] rounded-[14px] bg-[#0A101C] overflow-hidden">
            <div className="p-3 border-b border-[#162035] flex flex-col gap-2.5 bg-[#0E1524]">
              <input
                type="text"
                value={query}
                onChange={e => setQuery(e.target.value)}
                placeholder="Search name or role"
                className="wtr-input h-9 w-full px-3 text-sm !bg-[#0A101C]"
              />
              <div className="flex gap-1.5 flex-wrap">
                {FILTERS.map(f => (
                  <button
                    key={f}
                    onClick={() => setFilter(f)}
                    className="h-7 px-2.5 rounded-full text-[12.5px] font-medium cursor-pointer"
                    style={{
                      border: `1px solid ${filter === f ? '#2B3A5C' : '#1C2740'}`,
                      background: filter === f ? '#1E2B47' : 'transparent',
                      color: filter === f ? '#E6ECF6' : '#8C9AB4',
                    }}
                  >
                    {f}
                  </button>
                ))}
              </div>
            </div>
            {visible.map(w => {
              const [chipColor, chipBg] = typeChip(w.type)
              const isSel = w.id === sel.id
              return (
                <button
                  key={w.id}
                  onClick={() => setSelId(w.id)}
                  className="w-full flex items-center gap-3 px-3.5 py-3 border-0 border-b border-solid border-b-[#0F1727] text-[#E6ECF6] text-left cursor-pointer wtr-row-hover"
                  style={{
                    background: isSel ? '#101A2C' : 'transparent',
                    borderLeft: `3px solid ${isSel ? '#5EC8F2' : 'transparent'}`,
                  }}
                >
                  <WorkerAvatar worker={w} size={36} fontSize={13} />
                  <span className="flex-1 min-w-0 flex flex-col gap-0.5">
                    <span className="text-sm font-medium truncate">{w.name || 'New worker'}</span>
                    <span className="text-[12.5px] text-[#8C9AB4] truncate">{w.role || 'No role'}</span>
                  </span>
                  <span className="flex flex-col items-end gap-1 flex-none">
                    <span
                      className="text-[11px] font-semibold tracking-[0.4px] px-[7px] py-0.5 whitespace-nowrap rounded-full"
                      style={{ color: chipColor, background: chipBg }}
                    >
                      {w.type}
                    </span>
                    <span className="wtr-mono text-xs text-[#8C9AB4] whitespace-nowrap">
                      {w.model === 'Hourly' ? 'hourly' : `${fmtNum(weeklyHours(w))} h`}
                    </span>
                  </span>
                </button>
              )
            })}
            {visible.length === 0 && (
              <div className="px-4 py-7 text-center text-[13px] text-[#8C9AB4]">No one matches.</div>
            )}
          </div>

          {/* Detail */}
          <div className="flex-[999_1_460px] min-w-0 flex flex-col gap-4">
            {/* Worker header */}
            <div className="border border-[#162035] rounded-[14px] bg-[#0C1322] px-5 py-[18px] flex flex-col gap-4">
              <div className="flex items-center gap-3.5 flex-wrap">
                <input
                  ref={headerPhotoRef}
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={e => {
                    const f = e.target.files?.[0]
                    if (f && f.type.startsWith('image/')) upd({ photo: URL.createObjectURL(f) })
                    e.target.value = ''
                  }}
                />
                <button
                  onClick={() => headerPhotoRef.current?.click()}
                  title="Change photo"
                  className="w-12 h-12 flex-none rounded-full border border-[#253352] p-0 grid place-items-center bg-[#12253A] text-[#9BE0FA] text-[17px] font-semibold cursor-pointer overflow-hidden hover:border-[#5EC8F2]"
                >
                  {sel.photo ? (
                    <span className="w-full h-full block bg-cover bg-center" style={{ backgroundImage: `url(${sel.photo})` }} />
                  ) : (
                    initialsOf(sel.name)
                  )}
                </button>
                <div className="flex-1 min-w-[220px] flex flex-col gap-1">
                  <input
                    type="text"
                    value={sel.name}
                    placeholder="Full name"
                    onChange={e => upd({ name: e.target.value })}
                    className="bg-transparent border-0 border-b border-transparent focus:border-b-[#5EC8F2] text-[#E6ECF6] text-xl font-semibold tracking-[-0.3px] p-0 outline-none w-full"
                  />
                  <input
                    type="text"
                    value={sel.role}
                    placeholder="Role"
                    onChange={e => upd({ role: e.target.value })}
                    className="bg-transparent border-0 text-[#8C9AB4] text-sm p-0 outline-none w-full"
                  />
                </div>
                <div className="flex flex-wrap gap-px bg-[#162035] border border-[#162035] rounded-[10px] overflow-hidden">
                  <HeaderStat label="Week" value={isHourly ? (sel.expectedWeek ? `~${sel.expectedWeek} h` : 'varies') : `${fmtNum(wk)} h`} />
                  <HeaderStat label="FTE" value={isHourly ? '—' : (wk / FULL_TIME_HOURS).toFixed(2)} />
                  <HeaderStat
                    label="Vacation left"
                    value={isFreelancer ? 'n/a' : `${vacLeft} d`}
                    color={isFreelancer ? '#8391AB' : vacLeft < 0 ? '#F2A25E' : '#E6ECF6'}
                  />
                </div>
              </div>
              <div className="flex gap-0.5 border-t border-[#162035] pt-3 flex-wrap">
                {TABS.map(t => (
                  <button
                    key={t}
                    onClick={() => setTab(t)}
                    className="h-[34px] px-[11px] whitespace-nowrap rounded-lg border-0 text-[13.5px] font-medium cursor-pointer hover:!text-[#E6ECF6]"
                    style={{
                      background: tab === t ? '#1E2B47' : 'transparent',
                      color: tab === t ? '#E6ECF6' : '#8C9AB4',
                    }}
                  >
                    {t}
                  </button>
                ))}
              </div>
            </div>

            {/* Active tab — key resets per-tab local state when switching workers */}
            {tab === 'Profile' && <ProfileTab key={sel.id} worker={sel} upd={upd} />}
            {tab === 'CV' && <CvTab key={sel.id} worker={sel} upd={upd} />}
            {tab === 'Contract' && <ContractTab key={sel.id} worker={sel} upd={upd} />}
            {tab === 'Vacation & absences' && <VacationTab key={sel.id} worker={sel} upd={upd} />}
            {tab === 'Additionals' && <AdditionalsTab key={sel.id} worker={sel} upd={upd} />}
            {tab === 'Notes' && <NotesTab key={sel.id} worker={sel} upd={upd} />}
          </div>
        </div>
      </div>
    </div>
  )
}

function WorkerAvatar({ worker: w, size, fontSize }: { worker: Worker; size: number; fontSize: number }) {
  return (
    <span
      className="flex-none rounded-full grid place-items-center bg-[#12253A] text-[#9BE0FA] font-semibold overflow-hidden"
      style={{ width: size, height: size, fontSize }}
    >
      {w.photo ? (
        <span className="w-full h-full block bg-cover bg-center" style={{ backgroundImage: `url(${w.photo})` }} />
      ) : (
        initialsOf(w.name)
      )}
    </span>
  )
}

function HeaderStat({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="bg-[#0A101C] px-3.5 py-2 flex flex-col gap-0.5">
      <span className="text-[11px] font-semibold tracking-[0.9px] uppercase text-[#8391AB] whitespace-nowrap">{label}</span>
      <span className="wtr-mono text-[15px] font-medium" style={color ? { color } : undefined}>
        {value}
      </span>
    </div>
  )
}
