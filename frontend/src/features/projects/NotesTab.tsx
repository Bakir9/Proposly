import { useState, useEffect, useRef, useCallback } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addNote, updateNote, deleteNote } from '@/api/projects'
import type { ProjectNote } from '@/api/projects'
import { RichTextEditor } from '@/components/ui/rich-text-editor'
import { Button } from '@/components/ui/button'
import { Plus, FileText, Trash2, Check, Loader2, AlertCircle } from 'lucide-react'

const AUTOSAVE_DELAY = 1500

type SaveStatus = 'idle' | 'saving' | 'saved' | 'error'

interface Props {
  projectId: string
  notes: ProjectNote[]
}

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d ago`
  return new Date(dateStr).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export function NotesTab({ projectId, notes }: Props) {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['project', projectId] })

  const [selectedId, setSelectedId] = useState<string | null>(notes[0]?.id ?? null)
  const [title, setTitle] = useState(notes[0]?.title ?? '')
  const [content, setContent] = useState(notes[0]?.content ?? '')
  const [saveStatus, setSaveStatus] = useState<SaveStatus>('idle')
  const [pendingSelectId, setPendingSelectId] = useState<string | null>(null)

  // Always-fresh reference so debounce callbacks never read stale closures
  const stateRef = useRef({ title, content, selectedId })
  stateRef.current = { title, content, selectedId }

  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const savedTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const isDirty = useRef(false)

  const clearDebounce = () => {
    if (debounceTimer.current) {
      clearTimeout(debounceTimer.current)
      debounceTimer.current = null
    }
  }

  // ── mutations ──────────────────────────────────────────────────────────────

  const mutSave = useMutation({
    mutationFn: async () => {
      const { title: t, content: c, selectedId: sid } = stateRef.current
      const safeTitle = t.trim() || 'Untitled'
      if (sid) {
        await updateNote(projectId, sid, { title: safeTitle, content: c })
        return sid
      } else {
        return addNote(projectId, { title: safeTitle, content: c })
      }
    },
    onMutate: () => setSaveStatus('saving'),
    onSuccess: (id: string) => {
      isDirty.current = false
      setSaveStatus('saved')
      setPendingSelectId(id)
      invalidate()
      savedTimer.current = setTimeout(() => setSaveStatus('idle'), 2000)
    },
    onError: () => {
      setSaveStatus('error')
      savedTimer.current = setTimeout(() => setSaveStatus('idle'), 3000)
    },
  })

  const mutDelete = useMutation({
    mutationFn: () => deleteNote(projectId, selectedId!),
    onSuccess: () => {
      clearDebounce()
      isDirty.current = false
      setSelectedId(null)
      setTitle('')
      setContent('')
      setSaveStatus('idle')
      invalidate()
    },
  })

  // ── helpers ────────────────────────────────────────────────────────────────

  const loadNote = useCallback((note: ProjectNote | null) => {
    clearDebounce()
    isDirty.current = false
    setSaveStatus('idle')
    setTitle(note?.title ?? '')
    setContent(note?.content ?? '')
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const triggerAutoSave = () => {
    clearDebounce()
    debounceTimer.current = setTimeout(() => {
      const { title: t, content: c } = stateRef.current
      if (!t.trim() && !c) return
      mutSave.mutate()
    }, AUTOSAVE_DELAY)
  }

  const saveImmediately = useCallback(() => {
    clearDebounce()
    const { title: t, content: c } = stateRef.current
    if (!t.trim() && !c) return
    mutSave.mutate()
  }, [mutSave]) // eslint-disable-line react-hooks/exhaustive-deps

  // Ctrl+S
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault()
        if (isDirty.current) saveImmediately()
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [saveImmediately])

  // Cleanup timers on unmount
  useEffect(() => {
    return () => {
      clearDebounce()
      if (savedTimer.current) clearTimeout(savedTimer.current)
    }
  }, [])

  // When notes list refreshes (after save/delete), sync selection
  useEffect(() => {
    if (pendingSelectId) {
      const note = notes.find(n => n.id === pendingSelectId) ?? null
      setSelectedId(note?.id ?? null)
      // Don't call loadNote — we keep the editor content the user already sees
      setPendingSelectId(null)
      return
    }
    if (selectedId) {
      const stillExists = notes.find(n => n.id === selectedId)
      if (!stillExists) {
        const first = notes[0] ?? null
        setSelectedId(first?.id ?? null)
        loadNote(first)
      }
    }
  }, [notes]) // eslint-disable-line react-hooks/exhaustive-deps

  // ── user actions ───────────────────────────────────────────────────────────

  const selectNote = (note: ProjectNote) => {
    if (note.id === selectedId) return
    setSelectedId(note.id)
    loadNote(note)
  }

  const startNew = () => {
    setSelectedId(null)
    loadNote(null)
  }

  const handleTitleChange = (val: string) => {
    setTitle(val)
    isDirty.current = true
    setSaveStatus('idle')
    triggerAutoSave()
  }

  const handleContentChange = (html: string) => {
    setContent(html)
    isDirty.current = true
    setSaveStatus('idle')
    triggerAutoSave()
  }

  // ── render helpers ─────────────────────────────────────────────────────────

  const isNew = !selectedId

  const statusEl = (() => {
    if (saveStatus === 'saving') return (
      <span className="flex items-center gap-1 text-xs text-muted-foreground">
        <Loader2 className="h-3 w-3 animate-spin" /> Saving…
      </span>
    )
    if (saveStatus === 'saved') return (
      <span className="flex items-center gap-1 text-xs text-green-600 dark:text-green-500">
        <Check className="h-3 w-3" /> Saved
      </span>
    )
    if (saveStatus === 'error') return (
      <span className="flex items-center gap-1 text-xs text-destructive">
        <AlertCircle className="h-3 w-3" /> Failed to save
      </span>
    )
    return null
  })()

  return (
    <div className="flex h-[600px] border rounded-lg overflow-hidden">
      {/* Sidebar */}
      <div className="w-56 shrink-0 border-r flex flex-col bg-muted/10">
        <div className="p-3 border-b">
          <Button size="sm" className="w-full" onClick={startNew}>
            <Plus className="h-3.5 w-3.5 mr-1.5" /> New Note
          </Button>
        </div>

        <div className="flex-1 overflow-y-auto">
          {notes.length === 0 && !isNew && (
            <p className="text-xs text-muted-foreground p-4 text-center">No notes yet.<br />Click "New Note" to start.</p>
          )}

          {isNew && (
            <div className="px-3 py-2.5 bg-primary/10 border-l-2 border-primary">
              <p className="text-sm font-medium truncate text-primary">{title || 'New note'}</p>
              <p className="text-xs text-muted-foreground mt-0.5">Draft</p>
            </div>
          )}

          {notes.map(note => (
            <button
              key={note.id}
              onClick={() => selectNote(note)}
              className={`w-full text-left px-3 py-2.5 border-b border-border/50 hover:bg-muted/50 transition-colors ${selectedId === note.id ? 'bg-primary/10 border-l-2 border-l-primary' : ''}`}
            >
              <div className="flex items-start gap-2">
                <FileText className="h-3.5 w-3.5 shrink-0 mt-0.5 text-muted-foreground" />
                <div className="min-w-0">
                  <p className="text-sm font-medium truncate">{note.title}</p>
                  <p className="text-xs text-muted-foreground mt-0.5">{timeAgo(note.updatedAt)}</p>
                  <p className="text-xs text-muted-foreground/70 truncate">{note.authorName}</p>
                </div>
              </div>
            </button>
          ))}
        </div>
      </div>

      {/* Editor */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Title bar */}
        <div className="flex items-center gap-3 px-5 py-3 border-b bg-muted/10 shrink-0">
          <input
            value={title}
            onChange={e => handleTitleChange(e.target.value)}
            placeholder="Note title…"
            className="flex-1 text-base font-semibold bg-transparent border-0 border-b border-transparent hover:border-border focus:border-primary focus:outline-none py-0.5 transition-colors placeholder:text-muted-foreground/40"
          />
          <div className="flex items-center gap-3 shrink-0">
            {statusEl}
            {selectedId && (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => mutDelete.mutate()}
                disabled={mutDelete.isPending}
                className="text-muted-foreground hover:text-destructive h-7 w-7 p-0"
                title="Delete note"
              >
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            )}
          </div>
        </div>

        {/* Rich text editor */}
        <RichTextEditor
          key={selectedId ?? 'new'}
          value={content}
          onChange={handleContentChange}
          placeholder="Start writing… (auto-saves as you type)"
          className="flex-1 border-0 rounded-none shadow-none"
          editorClassName="min-h-0 h-full"
        />
      </div>
    </div>
  )
}
