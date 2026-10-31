import { useMemo, useRef, useState, type DragEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Download, Search, Trash2, Upload } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useTheme } from '@/context/ThemeContext'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { getApiErrorMessage } from '@/lib/api-errors'
import {
  deleteProjectFile,
  fetchProjectFileBlob,
  getProjectFiles,
  uploadProjectFile,
  type ProjectFileItem,
} from '@/api/projects'
import {
  ACCEPTED_FILE_TYPES,
  MAX_FILE_SIZE,
  downloadAttachment,
  extColors,
  extOf,
  formatBytes,
} from '@/features/chat/chat-utils'

const SOURCES = [
  { id: 'all', label: 'All' },
  { id: 'Discussion', label: 'Discussion & notes' },
  { id: 'Upload', label: 'Uploads' },
] as const

type SourceFilter = (typeof SOURCES)[number]['id']

interface PendingUpload {
  name: string
  progress: number
}

const fmtDate = (iso: string) =>
  new Date(iso).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' })

interface Props {
  projectId: string
}

/** Collaborate › Files: every project file in one place, filterable by source, with uploads. */
export function CollabFilesTab({ projectId }: Props) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { theme } = useTheme()
  const dark = theme === 'dark'
  const fileRef = useRef<HTMLInputElement>(null)
  const [search, setSearch] = useState('')
  const [source, setSource] = useState<SourceFilter>('all')
  const [pending, setPending] = useState<PendingUpload[]>([])
  const [dragging, setDragging] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState<ProjectFileItem | null>(null)

  const { data: files = [], isLoading } = useQuery({
    queryKey: ['project-files', projectId],
    queryFn: () => getProjectFiles(projectId),
  })

  const counts = useMemo(() => {
    const bySource = { all: files.length, Discussion: 0, Upload: 0 }
    for (const f of files) bySource[f.source]++
    return bySource
  }, [files])

  const q = search.trim().toLowerCase()
  const visible = files.filter(f => {
    if (source !== 'all' && f.source !== source) return false
    if (q && !f.fileName.toLowerCase().includes(q) && !f.uploadedByName.toLowerCase().includes(q)) return false
    return true
  })

  const totalBytes = files.reduce((s, f) => s + f.sizeBytes, 0)

  const addFiles = (incoming: File[]) => {
    const allowed = ACCEPTED_FILE_TYPES.split(',')
    for (const file of incoming) {
      const ext = '.' + (file.name.split('.').pop()?.toLowerCase() ?? '')
      if (!allowed.includes(ext)) {
        toast.error(`${file.name}: file type not allowed`)
        continue
      }
      if (file.size > MAX_FILE_SIZE) {
        toast.error(`${file.name} is too large · max 25 MB`)
        continue
      }
      setPending(prev => [...prev, { name: file.name, progress: 0 }])
      uploadProjectFile(projectId, file, pct =>
        setPending(prev => prev.map(p => (p.name === file.name ? { ...p, progress: pct } : p))),
      )
        .then(() => {
          queryClient.invalidateQueries({ queryKey: ['project-files', projectId] })
          toast.success(`${file.name} uploaded`)
        })
        .catch(err => toast.error(getApiErrorMessage(err)))
        .finally(() => setPending(prev => prev.filter(p => p.name !== file.name)))
    }
  }

  const download = async (f: ProjectFileItem) => {
    if (f.source === 'Discussion') {
      await downloadAttachment(f.id, f.fileName)
      return
    }
    const blob = await fetchProjectFileBlob(projectId, f.id)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = f.fileName
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  }

  const del = useMutation({
    mutationFn: (f: ProjectFileItem) => deleteProjectFile(projectId, f.id),
    onSuccess: (_d, f) => {
      toast.success(`${f.fileName} deleted`)
      setConfirmDelete(null)
      queryClient.invalidateQueries({ queryKey: ['project-files', projectId] })
    },
  })

  const onDrop = (e: DragEvent) => {
    e.preventDefault()
    setDragging(false)
    addFiles(Array.from(e.dataTransfer.files ?? []))
  }

  return (
    <div
      className="flex flex-wrap items-start gap-4"
      onDragOver={e => {
        if (!e.dataTransfer.types.includes('Files')) return
        e.preventDefault()
        if (!dragging) setDragging(true)
      }}
      onDragLeave={e => {
        if (!e.currentTarget.contains(e.relatedTarget as Node)) setDragging(false)
      }}
      onDrop={onDrop}
    >
      <div className="min-w-0 flex-[3_1_520px] space-y-3">
        {/* Toolbar: source pills + search + upload */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-2">
            {SOURCES.map(s => (
              <button
                key={s.id}
                onClick={() => setSource(s.id)}
                className={cn(
                  'min-h-9 whitespace-nowrap rounded-full border px-3.5 text-[13px] font-semibold',
                  source === s.id
                    ? 'border-transparent bg-primary text-primary-foreground'
                    : 'bg-card text-muted-foreground hover:text-foreground',
                )}
              >
                {s.label} · {counts[s.id]}
              </button>
            ))}
          </div>
          <div className="flex items-center gap-2">
            <label className="flex min-h-[38px] items-center gap-2 rounded-lg border bg-card px-3 text-muted-foreground">
              <Search className="h-4 w-4" />
              <input
                type="search"
                aria-label="Search files"
                placeholder="Search files"
                value={search}
                onChange={e => setSearch(e.target.value)}
                className="w-40 border-0 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground/70"
              />
            </label>
            <Button onClick={() => fileRef.current?.click()}>
              <Upload className="mr-1.5 h-4 w-4" />Upload
            </Button>
            <input
              ref={fileRef}
              type="file"
              multiple
              accept={ACCEPTED_FILE_TYPES}
              className="hidden"
              onChange={e => {
                addFiles(Array.from(e.target.files ?? []))
                e.target.value = ''
              }}
            />
          </div>
        </div>

        {/* Files table */}
        <section className="overflow-hidden rounded-xl border bg-card">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[680px] border-collapse text-[13.5px]">
              <thead>
                <tr className="border-b bg-muted/40 text-left text-xs text-muted-foreground">
                  <th className="py-2.5 pl-4 font-semibold">Name</th>
                  <th className="px-2 py-2.5 font-semibold">Attached to</th>
                  <th className="px-2 py-2.5 font-semibold">Added by</th>
                  <th className="px-2 py-2.5 font-semibold">Date</th>
                  <th className="px-2 py-2.5 text-right font-semibold">Size</th>
                  <th className="py-2.5 pr-4" />
                </tr>
              </thead>
              <tbody>
                {visible.map(f => {
                  const ext = extOf(f.fileName)
                  return (
                    <tr key={`${f.source}-${f.id}`} className="border-b last:border-b-0 hover:bg-muted/30">
                      <td className="py-2.5 pl-4">
                        <div className="flex items-center gap-2.5">
                          <span
                            className="flex h-8 w-7 flex-none items-center justify-center rounded-[5px] text-[8px] font-semibold tracking-[0.04em]"
                            style={extColors(ext, dark)}
                          >
                            {ext}
                          </span>
                          <span className="max-w-[320px] truncate font-semibold" title={f.fileName}>{f.fileName}</span>
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-2 py-2.5">
                        {f.source === 'Discussion' ? (
                          <button
                            onClick={() => navigate(`/projects/${projectId}/collaborate/discussion`)}
                            className="rounded-md bg-blue-100 px-2 py-1 text-xs font-semibold text-blue-800 hover:underline dark:bg-blue-950 dark:text-blue-300"
                          >
                            Discussion
                          </button>
                        ) : (
                          <span className="rounded-md bg-muted px-2 py-1 text-xs font-semibold text-muted-foreground">
                            Direct upload
                          </span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-2 py-2.5 text-muted-foreground">{f.uploadedByName}</td>
                      <td className="whitespace-nowrap px-2 py-2.5 text-muted-foreground">{fmtDate(f.uploadedAt)}</td>
                      <td className="whitespace-nowrap px-2 py-2.5 text-right text-muted-foreground">{formatBytes(f.sizeBytes)}</td>
                      <td className="py-2.5 pr-3">
                        <div className="flex justify-end gap-1">
                          <button
                            onClick={() => download(f)}
                            title={`Download ${f.fileName}`}
                            className="flex h-8 w-8 items-center justify-center rounded-lg text-muted-foreground hover:bg-muted hover:text-foreground"
                          >
                            <Download className="h-4 w-4" />
                          </button>
                          {f.canDelete && (
                            <button
                              onClick={() => setConfirmDelete(f)}
                              title={`Delete ${f.fileName}`}
                              className="flex h-8 w-8 items-center justify-center rounded-lg text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          {!isLoading && visible.length === 0 && (
            <div className="py-10 text-center text-[13px] text-muted-foreground">
              {q || source !== 'all'
                ? 'No files match the current filter.'
                : 'No files yet — upload one or share something in the discussion.'}
            </div>
          )}
          {isLoading && <div className="py-10 text-center text-[13px] text-muted-foreground">Loading files…</div>}
        </section>
      </div>

      {/* Side panel: drop zone + storage */}
      <aside className="flex min-w-[240px] flex-[1_1_240px] flex-col gap-4">
        <button
          onClick={() => fileRef.current?.click()}
          className={cn(
            'flex flex-col items-center gap-2 rounded-xl border-2 border-dashed px-4 py-8 text-center transition-colors',
            dragging ? 'border-primary bg-primary/5 text-primary' : 'text-muted-foreground hover:border-primary/50 hover:text-foreground',
          )}
        >
          <Upload className="h-6 w-6" />
          <span className="text-sm font-semibold">{dragging ? 'Drop to upload' : 'Drop files here'}</span>
          <span className="text-xs">Images, PDFs, Office documents · up to 25 MB each</span>
        </button>

        {pending.length > 0 && (
          <div className="space-y-2 rounded-xl border bg-card p-4">
            {pending.map(p => (
              <div key={p.name} className="space-y-1">
                <div className="flex justify-between gap-2 text-xs">
                  <span className="truncate font-medium">{p.name}</span>
                  <span className="shrink-0 text-muted-foreground">{p.progress}%</span>
                </div>
                <div className="h-1 rounded-full bg-muted">
                  <div className="h-1 rounded-full bg-primary transition-[width]" style={{ width: `${p.progress}%` }} />
                </div>
              </div>
            ))}
          </div>
        )}

        <div className="rounded-xl border bg-card p-4">
          <p className="text-[13px] font-semibold">Storage</p>
          <p className="mt-1 text-[22px] font-bold">{totalBytes === 0 ? '0 KB' : formatBytes(totalBytes)}</p>
          <p className="mt-0.5 text-xs text-muted-foreground">{files.length} file{files.length === 1 ? '' : 's'} in this project</p>
          <p className="mt-3 border-t pt-3 text-xs leading-relaxed text-muted-foreground">
            Stored in the EU · only project members can open these files.
          </p>
        </div>
      </aside>

      {confirmDelete && (
        <Dialog open onClose={() => setConfirmDelete(null)} title="Delete file">
          <div className="space-y-4">
            <p className="text-sm">
              Delete <span className="font-semibold">{confirmDelete.fileName}</span>? This cannot be undone.
            </p>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setConfirmDelete(null)}>Cancel</Button>
              <Button variant="destructive" onClick={() => del.mutate(confirmDelete)} disabled={del.isPending}>
                {del.isPending ? 'Deleting…' : 'Delete'}
              </Button>
            </div>
          </div>
        </Dialog>
      )}
    </div>
  )
}
