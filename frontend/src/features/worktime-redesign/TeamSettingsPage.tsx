import { useCallback, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Plus, Users } from 'lucide-react'
import './redesign.css'
import {
  addWorker,
  addWorkerAbsence,
  addWorkerNote,
  approveAbsence,
  cancelAbsence,
  deleteWorkerFile,
  deleteWorkerNote,
  fetchWorkerFileBlob,
  getWorker,
  getWorkers,
  inviteWorker,
  pinWorkerNote,
  setWorkerDocumentCategory,
  updateWorker,
  uploadWorkerFile,
  type WorkerListItem,
} from '@/api/hr'
import { getApiErrorMessage } from '@/lib/api-errors'
import type { Worker, WorkerDraft, WorkerType } from './team/types'
import { FULL_TIME_HOURS, fmtNum, initialsOf, statusChip, statusLabel, typeChip, weeklyHours } from './team/types'
import { absenceKindToApi, draftFromApi, mergeWorkerView, payloadFromDraft } from './team/hr-mapping'
import type { UpdateWorker, WorkerActions } from './team/TeamSettingsShared'
import { ProfileTab } from './team/ProfileTab'
import { CvTab } from './team/CvTab'
import { ContractTab } from './team/ContractTab'
import { VacationTab } from './team/VacationTab'
import { AdditionalsTab } from './team/AdditionalsTab'
import { NotesTab } from './team/NotesTab'
import { AddWorkerDialog, InviteWorkerDialog } from './team/WorkerDialogs'

const TABS = ['Profile', 'CV', 'Contract', 'Vacation & absences', 'Additionals', 'Notes'] as const
type Tab = (typeof TABS)[number]

const FILTERS: readonly (WorkerType | 'All')[] = ['All', 'Employee', 'Freelancer', 'Intern', 'Apprentice', 'Marginal']

const uiType = (t: WorkerListItem['employmentType']): WorkerType =>
  t === 'Freelancer' ? 'Freelancer'
  : t === 'Intern' ? 'Intern'
  : t === 'Apprentice' ? 'Apprentice'
  : t === 'MarginalEmployment' ? 'Marginal'
  : 'Employee'

export function TeamSettingsPage() {
  const queryClient = useQueryClient()
  const [selId, setSelId] = useState<string | null>(null)
  const [tab, setTab] = useState<Tab>('Profile')
  const [query, setQuery] = useState('')
  const [filter, setFilter] = useState<(typeof FILTERS)[number]>('All')
  const [addOpen, setAddOpen] = useState(false)
  const [inviteOpen, setInviteOpen] = useState(false)
  const headerPhotoRef = useRef<HTMLInputElement>(null)

  // Draft of the editable fields; null means "not loaded yet for this worker".
  const [draft, setDraft] = useState<WorkerDraft | null>(null)
  const [draftFor, setDraftFor] = useState<string | null>(null)

  const { data: workers = [], isLoading: listLoading } = useQuery({
    queryKey: ['hr', 'workers'],
    queryFn: getWorkers,
  })

  const activeId = selId ?? workers[0]?.userId ?? null

  const { data: detail } = useQuery({
    queryKey: ['hr', 'worker', activeId],
    queryFn: () => getWorker(activeId!),
    enabled: !!activeId,
    // Keep the previous worker on screen while the next one loads — switching
    // stays fluent instead of unmounting the whole right section.
    placeholderData: previous => previous,
  })

  const { data: photoUrl = '' } = useQuery({
    queryKey: ['hr', 'photo', activeId, detail?.hasPhoto],
    queryFn: async () => {
      if (!detail?.hasPhoto) return ''
      const blob = await fetchWorkerFileBlob(activeId!, 'Photo')
      return URL.createObjectURL(blob)
    },
    enabled: !!activeId && !!detail,
    staleTime: Infinity,
  })

  // Adopt the server copy as the draft whenever a different worker is loaded (render-adjust pattern).
  if (detail && draftFor !== detail.userId) {
    setDraft(draftFromApi(detail))
    setDraftFor(detail.userId)
  }

  const serverDraft = useMemo(() => (detail ? draftFromApi(detail) : null), [detail])
  const dirty = !!draft && !!serverDraft && JSON.stringify(draft) !== JSON.stringify(serverDraft)

  const view: Worker | null =
    detail && draft && draftFor === detail.userId ? mergeWorkerView(detail, draft, photoUrl) : null

  const upd: UpdateWorker = patch => {
    setDraft(prev => {
      if (!prev || !view) return prev
      return { ...prev, ...(typeof patch === 'function' ? patch(view) : patch) }
    })
  }

  function invalidateWorker() {
    queryClient.invalidateQueries({ queryKey: ['hr', 'worker', activeId] })
    queryClient.invalidateQueries({ queryKey: ['hr', 'workers'] })
  }

  const onError = (err: unknown) => toast.error(getApiErrorMessage(err))

  const saveMutation = useMutation({
    mutationFn: () => updateWorker(payloadFromDraft(activeId!, draft!)),
    onSuccess: async () => {
      toast.success('Worker saved.')
      // Refetch first, then re-adopt — adopting before the refetch would restore a stale draft.
      await queryClient.invalidateQueries({ queryKey: ['hr', 'worker', activeId] })
      queryClient.invalidateQueries({ queryKey: ['hr', 'workers'] })
      setDraftFor(null)
    },
    onError,
  })

  const addMutation = useMutation({
    mutationFn: (data: { firstName: string; lastName: string; jobTitle: string | null }) => addWorker(data),
    onSuccess: userId => {
      toast.success('Worker added — no login access until you invite them.')
      setAddOpen(false)
      setSelId(userId)
      setTab('Profile')
      invalidateWorker()
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError,
  })

  const inviteMutation = useMutation({
    mutationFn: (data: { email: string; role: string }) => inviteWorker(activeId!, data.email, data.role),
    onSuccess: () => {
      toast.success('Invite sent.')
      setInviteOpen(false)
      invalidateWorker()
      queryClient.invalidateQueries({ queryKey: ['users'] })
    },
    onError,
  })

  const act = useCallback(
    (fn: () => Promise<unknown>, success?: string) => {
      fn()
        .then(() => {
          if (success) toast.success(success)
          invalidateWorker()
        })
        .catch(onError)
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [activeId],
  )

  function download(kind: 'Cv' | 'Document', fileName: string, documentId?: string) {
    fetchWorkerFileBlob(activeId!, kind, documentId)
      .then(blob => {
        const url = URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = fileName
        document.body.appendChild(a)
        a.click()
        a.remove()
        URL.revokeObjectURL(url)
      })
      .catch(onError)
  }

  const actions: WorkerActions = {
    uploadPhoto: file => act(() => uploadWorkerFile(activeId!, 'Photo', file)),
    removePhoto: () => act(() => deleteWorkerFile(activeId!, 'Photo')),
    uploadCv: file => act(() => uploadWorkerFile(activeId!, 'Cv', file)),
    removeCv: () => act(() => deleteWorkerFile(activeId!, 'Cv')),
    downloadCv: fileName => download('Cv', fileName),
    uploadDocuments: (files, category) =>
      act(async () => {
        for (const file of files) await uploadWorkerFile(activeId!, 'Document', file, category)
      }),
    deleteDocument: documentId => act(() => deleteWorkerFile(activeId!, 'Document', documentId)),
    setDocumentCategory: (documentId, category) =>
      act(() => setWorkerDocumentCategory(activeId!, documentId, category)),
    downloadDocument: (documentId, fileName) => download('Document', fileName, documentId),
    addAbsence: (kind, from, to) =>
      act(() => addWorkerAbsence(activeId!, absenceKindToApi(kind), from, to)),
    approveAbsence: absenceId => act(() => approveAbsence(absenceId)),
    removeAbsence: absenceId => act(() => cancelAbsence(absenceId)),
    addNote: (tag, text) => act(() => addWorkerNote(activeId!, tag, text)),
    pinNote: (noteId, pinned) => act(() => pinWorkerNote(activeId!, noteId, pinned)),
    deleteNote: noteId => act(() => deleteWorkerNote(activeId!, noteId)),
  }

  const q = query.trim().toLowerCase()
  const visible = workers.filter(w => {
    const t = uiType(w.employmentType)
    const name = `${w.firstName} ${w.lastName} ${w.jobTitle ?? ''}`.toLowerCase()
    return (filter === 'All' || t === filter) && (!q || name.includes(q))
  })

  const wk = view ? weeklyHours(view) : 0
  const isHourly = view?.model === 'Hourly'
  const isFreelancer = view?.type === 'Freelancer'
  const taken = (view?.absences ?? []).filter(a => a.kind === 'Vacation' && a.status === 'Approved').reduce((n, a) => n + a.days, 0)
  const pending = (view?.absences ?? []).filter(a => a.kind === 'Vacation' && a.status === 'Pending').reduce((n, a) => n + a.days, 0)
  const vacLeft = view ? (Number(view.vacEnt) || 0) + (Number(view.vacCarry) || 0) - taken - pending : 0

  return (
    <div className="wtr min-h-full px-8 pt-7 pb-16">
      <div className="max-w-[1600px] mx-auto flex flex-col gap-[22px]">
        {/* Page header */}
        <div className="flex items-start justify-between gap-6 flex-wrap">
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-[11px]">
              <Users className="h-[22px] w-[22px] text-[color:var(--wtr-accent)]" strokeWidth={1.8} />
              <h1 className="m-0 text-[25px] font-semibold tracking-[-0.4px]">Team</h1>
            </div>
            <p className="m-0 text-sm text-[color:var(--wtr-text-sec)] max-w-[56ch]">
              Contracts, working hours, vacation and time rules for each person.
            </p>
          </div>
          <button onClick={() => setAddOpen(true)} className="wtr-primary h-10 px-[18px] flex items-center gap-2 whitespace-nowrap flex-none text-sm">
            <Plus className="h-3.5 w-3.5" strokeWidth={2.4} />
            Add worker
          </button>
        </div>

        <div className="flex gap-[18px] items-start flex-wrap">
          {/* Worker list */}
          <div className="flex-[1_1_260px] max-w-[320px] border border-[color:var(--wtr-border)] rounded-[14px] bg-[color:var(--wtr-card)] overflow-hidden">
            <div className="p-3 border-b border-[color:var(--wtr-border)] flex flex-col gap-2.5 bg-[color:var(--wtr-head)]">
              <input
                type="text"
                value={query}
                onChange={e => setQuery(e.target.value)}
                placeholder="Search name or role"
                className="wtr-input h-9 w-full px-3 text-sm !bg-[color:var(--wtr-card)]"
              />
              <div className="flex gap-1.5 flex-wrap">
                {FILTERS.map(f => (
                  <button
                    key={f}
                    onClick={() => setFilter(f)}
                    className="h-7 px-2.5 rounded-full text-[12.5px] font-medium cursor-pointer"
                    style={{
                      border: `1px solid ${filter === f ? 'var(--wtr-active-bg)' : 'var(--wtr-border2)'}`,
                      background: filter === f ? 'var(--wtr-active-bg)' : 'transparent',
                      color: filter === f ? 'var(--wtr-active-fg)' : 'var(--wtr-text-sec)',
                    }}
                  >
                    {f}
                  </button>
                ))}
              </div>
            </div>
            {listLoading && <div className="px-4 py-7 text-center text-[13px] text-[color:var(--wtr-text-sec)]">Loading…</div>}
            {visible.map(w => {
              const t = uiType(w.employmentType)
              const [chipColor, chipBg] = typeChip(t)
              const isSel = w.userId === activeId
              return (
                <button
                  key={w.userId}
                  onClick={() => {
                    if (dirty && !confirm('Discard unsaved changes?')) return
                    // The draft re-adopts automatically once the new worker's data arrives;
                    // resetting draftFor here would blank the panel while it loads.
                    setSelId(w.userId)
                  }}
                  className="w-full flex items-center gap-3 px-3.5 py-3 border-0 border-b border-solid border-b-[color:var(--wtr-row-border)] text-[color:var(--wtr-text)] text-left cursor-pointer wtr-row-hover"
                  style={{
                    background: isSel ? 'var(--wtr-selected-bg)' : 'transparent',
                    borderLeft: `3px solid ${isSel ? 'var(--wtr-accent)' : 'transparent'}`,
                  }}
                >
                  <span className="w-9 h-9 flex-none rounded-full grid place-items-center bg-[color:var(--wtr-tint-cyan-bg)] text-[color:var(--wtr-tint-cyan-fg)] text-[13px] font-semibold">
                    {initialsOf(`${w.firstName} ${w.lastName}`)}
                  </span>
                  <span className="flex-1 min-w-0 flex flex-col gap-0.5">
                    <span className="text-sm font-medium truncate">
                      {`${w.firstName} ${w.lastName}`.trim() || 'New worker'}
                    </span>
                    <span className="text-[12.5px] text-[color:var(--wtr-text-sec)] truncate">{w.jobTitle || 'No role'}</span>
                  </span>
                  <span className="flex flex-col items-end gap-1 flex-none">
                    <span
                      className="text-[11px] font-semibold tracking-[0.4px] px-[7px] py-0.5 whitespace-nowrap rounded-full"
                      style={{ color: chipColor, background: chipBg }}
                    >
                      {t}
                    </span>
                    <span className="wtr-mono text-xs text-[color:var(--wtr-text-sec)] whitespace-nowrap">
                      {w.workModel === 'Hourly' ? 'hourly' : `${fmtNum(w.weeklyHours)} h`}
                    </span>
                  </span>
                </button>
              )
            })}
            {!listLoading && visible.length === 0 && (
              <div className="px-4 py-7 text-center text-[13px] text-[color:var(--wtr-text-sec)]">No one matches.</div>
            )}
          </div>

          {/* Detail */}
          {view ? (
            <div className="flex-[999_1_460px] min-w-0 flex flex-col gap-4">
              {/* Worker header */}
              <div className="border border-[color:var(--wtr-border)] rounded-[14px] bg-[color:var(--wtr-panel)] px-5 py-[18px] flex flex-col gap-4">
                <div className="flex items-center gap-3.5 flex-wrap">
                  <input
                    ref={headerPhotoRef}
                    type="file"
                    accept="image/*"
                    className="hidden"
                    onChange={e => {
                      const f = e.target.files?.[0]
                      if (f) actions.uploadPhoto(f)
                      e.target.value = ''
                    }}
                  />
                  <button
                    onClick={() => headerPhotoRef.current?.click()}
                    title="Change photo"
                    className="w-12 h-12 flex-none rounded-full border border-[color:var(--wtr-border3)] p-0 grid place-items-center bg-[color:var(--wtr-tint-cyan-bg)] text-[color:var(--wtr-tint-cyan-fg)] text-[17px] font-semibold cursor-pointer overflow-hidden hover:border-[color:var(--wtr-accent)]"
                  >
                    {view.photo ? (
                      <span className="w-full h-full block bg-cover bg-center" style={{ backgroundImage: `url(${view.photo})` }} />
                    ) : (
                      initialsOf(view.name)
                    )}
                  </button>
                  <div className="flex-1 min-w-[220px] flex flex-col gap-1">
                    <input
                      type="text"
                      value={view.name}
                      placeholder="Full name"
                      onChange={e => upd({ name: e.target.value })}
                      className="bg-transparent border-0 border-b border-transparent focus:border-b-[color:var(--wtr-accent)] text-[color:var(--wtr-text)] text-xl font-semibold tracking-[-0.3px] p-0 outline-none w-full"
                    />
                    <div className="flex items-center gap-2 flex-wrap">
                      <input
                        type="text"
                        value={view.role}
                        placeholder="Role"
                        onChange={e => upd({ role: e.target.value })}
                        className="bg-transparent border-0 text-[color:var(--wtr-text-sec)] text-sm p-0 outline-none w-44"
                      />
                      <span
                        className="text-[11px] font-semibold tracking-[0.4px] px-[7px] py-0.5 rounded-full whitespace-nowrap"
                        style={{ color: statusChip(view.accountStatus)[0], background: statusChip(view.accountStatus)[1] }}
                        title={view.email ?? 'No login access'}
                      >
                        {statusLabel(view.accountStatus)}
                      </span>
                      {view.accountStatus === 'ProfileOnly' && (
                        <button onClick={() => setInviteOpen(true)} className="wtr-btn h-6 px-2 !rounded-md text-[11.5px] font-medium">
                          Invite to Proposly
                        </button>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-2.5">
                    <div className="flex flex-wrap gap-px bg-[color:var(--wtr-border)] border border-[color:var(--wtr-border)] rounded-[10px] overflow-hidden">
                      <HeaderStat label="Week" value={isHourly ? (view.expectedWeek ? `~${view.expectedWeek} h` : 'varies') : `${fmtNum(wk)} h`} />
                      <HeaderStat label="FTE" value={isHourly ? '—' : (wk / FULL_TIME_HOURS).toFixed(2)} />
                      <HeaderStat
                        label="Vacation left"
                        value={isFreelancer ? 'n/a' : `${vacLeft} d`}
                        color={isFreelancer ? 'var(--wtr-label)' : vacLeft < 0 ? 'var(--wtr-tint-orange-fg)' : 'var(--wtr-text)'}
                      />
                    </div>
                    <button
                      onClick={() => saveMutation.mutate()}
                      disabled={!dirty || saveMutation.isPending}
                      className="wtr-primary h-10 px-[18px] text-sm disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      {saveMutation.isPending ? 'Saving…' : dirty ? 'Save changes' : 'Saved'}
                    </button>
                  </div>
                </div>
                <div className="flex gap-0.5 border-t border-[color:var(--wtr-border)] pt-3 flex-wrap">
                  {TABS.map(t => (
                    <button
                      key={t}
                      onClick={() => setTab(t)}
                      className="h-[34px] px-[11px] whitespace-nowrap rounded-lg border-0 text-[13.5px] font-medium cursor-pointer"
                      style={{
                        background: tab === t ? 'var(--wtr-active-bg)' : 'transparent',
                        color: tab === t ? 'var(--wtr-active-fg)' : 'var(--wtr-text-sec)',
                      }}
                    >
                      {t}
                    </button>
                  ))}
                </div>
              </div>

              {tab === 'Profile' && <ProfileTab key={view.userId} worker={view} upd={upd} actions={actions} />}
              {tab === 'CV' && <CvTab key={view.userId} worker={view} upd={upd} actions={actions} />}
              {tab === 'Contract' && <ContractTab key={view.userId} worker={view} upd={upd} actions={actions} />}
              {tab === 'Vacation & absences' && <VacationTab key={view.userId} worker={view} upd={upd} actions={actions} />}
              {tab === 'Additionals' && <AdditionalsTab key={view.userId} worker={view} upd={upd} />}
              {tab === 'Notes' && <NotesTab key={view.userId} worker={view} actions={actions} />}
            </div>
          ) : (
            <div className="flex-[999_1_460px] min-w-0 py-16 text-center text-[13px] text-[color:var(--wtr-text-sec)]">
              {workers.length === 0 && !listLoading ? 'No workers yet — add your first one.' : 'Loading…'}
            </div>
          )}
        </div>
      </div>

      <AddWorkerDialog
        open={addOpen}
        pending={addMutation.isPending}
        onClose={() => setAddOpen(false)}
        onAdd={(firstName, lastName, jobTitle) => addMutation.mutate({ firstName, lastName, jobTitle })}
      />
      {view && (
        <InviteWorkerDialog
          open={inviteOpen}
          pending={inviteMutation.isPending}
          workerName={view.name}
          onClose={() => setInviteOpen(false)}
          onInvite={(email, role) => inviteMutation.mutate({ email, role })}
        />
      )}
    </div>
  )
}

function HeaderStat({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div className="bg-[color:var(--wtr-card)] px-3.5 py-2 flex flex-col gap-0.5">
      <span className="text-[11px] font-semibold tracking-[0.9px] uppercase text-[color:var(--wtr-label)] whitespace-nowrap">{label}</span>
      <span className="wtr-mono text-[15px] font-medium" style={color ? { color } : undefined}>
        {value}
      </span>
    </div>
  )
}
