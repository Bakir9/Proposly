import { useRef, useState } from 'react'
import type { Worker } from './types'
import { initialsOf } from './types'
import { Card, CardHeader, Field, INPUT, INPUT_MONO } from './primitives'
import type { UpdateWorker } from './TeamSettingsShared'

export function ProfileTab({ worker: w, upd }: { worker: Worker; upd: UpdateWorker }) {
  const photoRef = useRef<HTMLInputElement>(null)
  const [drag, setDrag] = useState(false)

  const firstName = w.name.trim().split(/\s+/)[0] ?? ''
  const lastName = w.name.trim().split(/\s+/).slice(1).join(' ')

  function setPhoto(file: File | null | undefined) {
    if (file && file.type.startsWith('image/')) upd({ photo: URL.createObjectURL(file) })
  }

  const text = (field: keyof Worker) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    upd({ [field]: e.target.value } as Partial<Worker>)

  return (
    <div className="flex flex-col gap-4">
      <Card className="!overflow-visible">
        <div className="p-5 flex gap-6 flex-wrap">
          <input
            ref={photoRef}
            type="file"
            accept="image/*"
            className="hidden"
            onChange={e => {
              setPhoto(e.target.files?.[0])
              e.target.value = ''
            }}
          />
          <div className="flex-none flex flex-col items-center gap-2.5 w-[168px]">
            <button
              onClick={() => photoRef.current?.click()}
              onDragOver={e => {
                e.preventDefault()
                if (!drag) setDrag(true)
              }}
              onDragLeave={() => setDrag(false)}
              onDrop={e => {
                e.preventDefault()
                setDrag(false)
                setPhoto(e.dataTransfer.files?.[0])
              }}
              className="w-[168px] h-[200px] rounded-xl p-0 bg-[#0E1727] text-[#8C9AB4] cursor-pointer overflow-hidden flex flex-col items-center justify-center gap-2 hover:!border-[#5EC8F2]"
              style={{ border: `1px dashed ${drag ? '#5EC8F2' : '#253352'}` }}
            >
              {w.photo ? (
                <span className="w-full h-full block bg-cover bg-center" style={{ backgroundImage: `url(${w.photo})` }} />
              ) : (
                <>
                  <span className="w-16 h-16 rounded-full grid place-items-center bg-[#12253A] text-[#9BE0FA] text-[22px] font-semibold">
                    {initialsOf(w.name)}
                  </span>
                  <span className="text-[13px] font-medium text-[#E6ECF6]">Upload photo</span>
                  <span className="text-xs">Drop or click · JPG, PNG</span>
                </>
              )}
            </button>
            {w.photo && (
              <div className="flex gap-2">
                <button onClick={() => photoRef.current?.click()} className="wtr-btn h-[30px] px-[11px] text-[12.5px]">
                  Change
                </button>
                <button onClick={() => upd({ photo: '' })} className="wtr-ghost h-[30px] px-[11px] text-[12.5px]">
                  Remove
                </button>
              </div>
            )}
          </div>

          <div className="flex-1 min-w-[260px] flex flex-col gap-[18px]">
            <div className="flex flex-col gap-1">
              <h2 className="m-0 text-[15px] font-semibold">Personal information</h2>
              <p className="m-0 text-[13px] text-[#8C9AB4]">Visible to managers and HR only.</p>
            </div>
            <div className="grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-3.5">
              <Field label="First name">
                <input
                  type="text"
                  value={firstName}
                  placeholder="First name"
                  onChange={e => upd({ name: `${e.target.value} ${lastName}`.trim() })}
                  className={INPUT}
                />
              </Field>
              <Field label="Last name">
                <input
                  type="text"
                  value={lastName}
                  placeholder="Last name"
                  onChange={e => upd({ name: `${firstName} ${e.target.value}`.trim() })}
                  className={INPUT}
                />
              </Field>
              <Field label="Phone">
                <input type="tel" value={w.phone} onChange={text('phone')} placeholder="+43 …" className={INPUT_MONO} />
              </Field>
              <Field label="Gender">
                <select value={w.gender} onChange={text('gender')} className={`${INPUT} px-2.5`}>
                  <option value="">Not specified</option>
                  <option value="Female">Female</option>
                  <option value="Male">Male</option>
                  <option value="Diverse">Diverse</option>
                  <option value="Inter">Inter</option>
                  <option value="Open">Open</option>
                  <option value="Prefer not to say">Prefer not to say</option>
                </select>
              </Field>
              <Field label="Date of birth">
                <input type="date" value={w.birthday} onChange={text('birthday')} className={INPUT_MONO} />
              </Field>
              <Field label="Social security no.">
                <input type="text" value={w.svnr} onChange={text('svnr')} placeholder="1234 010190" className={INPUT_MONO} />
              </Field>
              <Field label="Nationality">
                <input type="text" value={w.nationality} onChange={text('nationality')} placeholder="e.g. Austria" className={INPUT} />
              </Field>
              <Field label="Department">
                <input type="text" value={w.department} onChange={text('department')} placeholder="e.g. Planning" className={INPUT} />
              </Field>
              <Field label="Location">
                <input type="text" value={w.location} onChange={text('location')} placeholder="e.g. Graz office" className={INPUT} />
              </Field>
            </div>
            <Field label="Address">
              <input type="text" value={w.address} onChange={text('address')} placeholder="Street, postcode, city" className={INPUT} />
            </Field>
          </div>
        </div>
      </Card>

      <Card>
        <CardHeader title="Emergency contact" />
        <div className="p-5 grid grid-cols-[repeat(auto-fit,minmax(190px,1fr))] gap-3.5">
          <Field label="Name">
            <input type="text" value={w.emName} onChange={text('emName')} placeholder="Full name" className={INPUT} />
          </Field>
          <Field label="Relation">
            <input type="text" value={w.emRelation} onChange={text('emRelation')} placeholder="e.g. Partner" className={INPUT} />
          </Field>
          <Field label="Phone">
            <input type="tel" value={w.emPhone} onChange={text('emPhone')} placeholder="+43 …" className={INPUT_MONO} />
          </Field>
        </div>
      </Card>
    </div>
  )
}
