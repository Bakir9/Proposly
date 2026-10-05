import { useRef, useState } from 'react'
import { Upload } from 'lucide-react'
import type { CvEntry, Worker } from './types'
import { fmtDate, fmtSize, today } from './types'
import { AddButton, Card, CardHeader, EmptyRow, Field, FileIcon, INPUT, INPUT_MONO, RemoveButton } from './primitives'
import type { UpdateWorker } from './TeamSettingsShared'

export function CvTab({ worker: w, upd }: { worker: Worker; upd: UpdateWorker }) {
  const cvRef = useRef<HTMLInputElement>(null)
  const [drag, setDrag] = useState(false)
  const [skillDraft, setSkillDraft] = useState('')

  function setCv(file: File | null | undefined) {
    if (file) upd({ cvFile: { name: file.name, size: file.size, date: today(), url: URL.createObjectURL(file) } })
  }

  function addSkill() {
    const t = skillDraft.trim()
    if (!t) return
    upd(x => ({ skills: x.skills.filter(s => s !== t).concat(t) }))
    setSkillDraft('')
  }

  const dragProps = {
    onDragOver: (e: React.DragEvent) => {
      e.preventDefault()
      if (!drag) setDrag(true)
    },
    onDragLeave: () => setDrag(false),
    onDrop: (e: React.DragEvent) => {
      e.preventDefault()
      setDrag(false)
      setCv(e.dataTransfer.files?.[0])
    },
  }

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <input
          ref={cvRef}
          type="file"
          accept=".pdf,.doc,.docx"
          className="hidden"
          onChange={e => {
            setCv(e.target.files?.[0])
            e.target.value = ''
          }}
        />
        <CardHeader title="CV" sub="Upload their CV file, add details manually below — or both." />
        <div className="p-5">
          {w.cvFile ? (
            <div className="flex items-center gap-3.5 px-4 py-3.5 rounded-[10px] border border-[#253352] bg-[#0E1727] flex-wrap">
              <FileIcon />
              <div className="flex-1 min-w-[180px] flex flex-col gap-[3px]">
                <span className="text-sm font-medium break-all">{w.cvFile.name}</span>
                <span className="text-[12.5px] text-[#8C9AB4]">
                  {fmtSize(w.cvFile.size)} · uploaded {fmtDate(w.cvFile.date)}
                </span>
              </div>
              <div className="flex gap-2 flex-wrap">
                <a
                  href={w.cvFile.url}
                  target="_blank"
                  rel="noopener"
                  title={w.cvFile.url ? '' : 'Sample file — upload a real one to open it'}
                  className="wtr-ghost h-8 px-3 flex items-center text-[12.5px] font-medium no-underline"
                  style={{ opacity: w.cvFile.url ? 1 : 0.4 }}
                >
                  View
                </a>
                <a
                  href={w.cvFile.url}
                  download={w.cvFile.name}
                  title={w.cvFile.url ? '' : 'Sample file — upload a real one to open it'}
                  className="wtr-ghost h-8 px-3 flex items-center text-[12.5px] font-medium no-underline"
                  style={{ opacity: w.cvFile.url ? 1 : 0.4 }}
                >
                  Download
                </a>
                <button onClick={() => cvRef.current?.click()} className="wtr-ghost h-8 px-3 text-[12.5px] font-medium">
                  Replace
                </button>
                <button onClick={() => upd({ cvFile: null })} className="wtr-ghost h-8 px-3 text-[12.5px] font-medium">
                  Remove
                </button>
              </div>
            </div>
          ) : (
            <button
              onClick={() => cvRef.current?.click()}
              {...dragProps}
              className="w-full px-4 py-[22px] rounded-[10px] bg-transparent text-[#E6ECF6] flex flex-col items-center gap-1.5 cursor-pointer"
              style={{ border: `1px dashed ${drag ? '#5EC8F2' : '#253352'}` }}
            >
              <Upload className="h-4 w-4 text-[#5EC8F2]" strokeWidth={2} />
              <span className="text-sm font-medium">Upload CV</span>
              <span className="text-[12.5px] text-[#8C9AB4]">Drop a PDF or Word file, or click to browse</span>
            </button>
          )}
        </div>
      </Card>

      <CvEntryList
        title="Work experience"
        sub="Most recent first."
        entries={w.cvExp}
        labels={{ a: 'Position', b: 'Company' }}
        empty="No experience added."
        onChange={cvExp => upd({ cvExp })}
      />

      <CvEntryList
        title="Education"
        entries={w.cvEdu}
        labels={{ a: 'Degree / training', b: 'School' }}
        empty="No education added."
        onChange={cvEdu => upd({ cvEdu })}
      />

      <div className="grid grid-cols-[repeat(auto-fit,minmax(300px,1fr))] gap-4">
        <Card>
          <CardHeader title="Skills" />
          <div className="p-5 flex flex-col gap-3">
            <div className="flex gap-2 flex-wrap">
              {w.skills.map(s => (
                <span
                  key={s}
                  className="flex items-center gap-1.5 h-[30px] pl-3 pr-1.5 whitespace-nowrap rounded-full bg-[#12253A] text-[#9BE0FA] text-[13px] font-medium"
                >
                  {s}
                  <button
                    onClick={() => upd(x => ({ skills: x.skills.filter(y => y !== s) }))}
                    aria-label="Remove"
                    className="w-5 h-5 rounded-full border-0 bg-transparent text-[#9BE0FA] grid place-items-center cursor-pointer p-0 hover:bg-[#1E3A55]"
                  >
                    <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round">
                      <path d="M6 6l12 12M18 6 6 18" />
                    </svg>
                  </button>
                </span>
              ))}
            </div>
            <div className="flex gap-2">
              <input
                type="text"
                value={skillDraft}
                onChange={e => setSkillDraft(e.target.value)}
                onKeyDown={e => {
                  if (e.key === 'Enter') {
                    e.preventDefault()
                    addSkill()
                  }
                }}
                placeholder="e.g. AutoCAD, Revit"
                className={`${INPUT} flex-1 !h-9`}
              />
              <button onClick={addSkill} className="wtr-btn h-9 px-3.5 text-[13px] font-medium">
                Add
              </button>
            </div>
          </div>
        </Card>

        <Card>
          <CardHeader
            title="Languages"
            action={<AddButton onClick={() => upd(x => ({ langs: [...x.langs, { name: '', level: 'B2' }] }))} />}
          />
          {w.langs.map((l, i) => (
            <div
              key={i}
              className="grid grid-cols-[minmax(0,1fr)_120px_auto] gap-2.5 items-center px-5 py-2.5 border-b border-[#0F1727]"
            >
              <input
                type="text"
                value={l.name}
                placeholder="Language"
                onChange={e => upd(x => ({ langs: x.langs.map((y, j) => (j === i ? { ...y, name: e.target.value } : y)) }))}
                className={`${INPUT} !h-9`}
              />
              <select
                value={l.level}
                onChange={e => upd(x => ({ langs: x.langs.map((y, j) => (j === i ? { ...y, level: e.target.value } : y)) }))}
                className={`${INPUT} !h-9 px-2`}
              >
                {['Native', 'C2', 'C1', 'B2', 'B1', 'A2', 'A1'].map(lv => (
                  <option key={lv} value={lv}>
                    {lv}
                  </option>
                ))}
              </select>
              <RemoveButton onClick={() => upd(x => ({ langs: x.langs.filter((_, j) => j !== i) }))} />
            </div>
          ))}
          {w.langs.length === 0 && <EmptyRow>No languages added.</EmptyRow>}
        </Card>
      </div>
    </div>
  )
}

function CvEntryList({
  title,
  sub,
  entries,
  labels,
  empty,
  onChange,
}: {
  title: string
  sub?: string
  entries: CvEntry[]
  labels: { a: string; b: string }
  empty: string
  onChange: (entries: CvEntry[]) => void
}) {
  const set = (i: number, field: keyof CvEntry, value: string) =>
    onChange(entries.map((e, j) => (j === i ? { ...e, [field]: value } : e)))

  return (
    <Card>
      <CardHeader
        title={title}
        sub={sub}
        action={<AddButton onClick={() => onChange([...entries, { a: '', b: '', from: '', to: '', desc: '' }])} />}
      />
      {entries.map((x, i) => (
        <div key={i} className="px-5 py-3.5 border-b border-[#0F1727] flex flex-col gap-2.5">
          <div className="grid grid-cols-[repeat(auto-fit,minmax(180px,1fr))] gap-2.5">
            <Field label={labels.a}>
              <input type="text" value={x.a} placeholder={labels.a} onChange={e => set(i, 'a', e.target.value)} className={`${INPUT} !h-9`} />
            </Field>
            <Field label={labels.b}>
              <input type="text" value={x.b} placeholder={labels.b} onChange={e => set(i, 'b', e.target.value)} className={`${INPUT} !h-9`} />
            </Field>
            <div className="flex gap-2.5 items-end">
              <Field label="From">
                <input type="month" value={x.from} onChange={e => set(i, 'from', e.target.value)} className={`${INPUT_MONO} !h-9 !text-[13.5px]`} />
              </Field>
              <Field label="To">
                <input type="month" value={x.to} onChange={e => set(i, 'to', e.target.value)} className={`${INPUT_MONO} !h-9 !text-[13.5px]`} />
              </Field>
            </div>
          </div>
          <div className="flex gap-2.5 items-start">
            <textarea
              value={x.desc}
              rows={2}
              placeholder="Short description (optional)"
              onChange={e => set(i, 'desc', e.target.value)}
              className="wtr-input flex-1 resize-y px-3 py-2 text-[13.5px] leading-normal"
            />
            <RemoveButton onClick={() => onChange(entries.filter((_, j) => j !== i))} />
          </div>
        </div>
      ))}
      {entries.length === 0 && <EmptyRow>{empty}</EmptyRow>}
    </Card>
  )
}
