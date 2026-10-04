import { useEffect, useRef } from 'react'
import { Paperclip } from 'lucide-react'
import { useTheme } from '@/context/ThemeContext'
import { cn } from '@/lib/utils'
import { ACCEPTED_FILE_TYPES, extColors, extOf, formatBytes } from './chat-utils'
import type { PendingAttachment } from './usePendingAttachments'

interface ComposerProps {
  conversationTitle: string
  pending: PendingAttachment[]
  uploading: boolean
  canSend: boolean
  draft: string
  onDraftChange: (value: string) => void
  onAddFiles: (files: FileList) => void
  onRemovePending: (localId: string) => void
  onSend: () => void
}

export function Composer({
  conversationTitle,
  pending,
  uploading,
  canSend,
  draft,
  onDraftChange,
  onAddFiles,
  onRemovePending,
  onSend,
}: ComposerProps) {
  const fileRef = useRef<HTMLInputElement>(null)
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  // Auto-grow up to ~8 rows.
  useEffect(() => {
    const el = textareaRef.current
    if (!el) return
    el.style.height = 'auto'
    el.style.height = `${Math.min(el.scrollHeight, 176)}px`
  }, [draft])

  return (
    <div className="shrink-0 px-6 pb-5">
      <div className="max-w-[820px] mx-auto border rounded-xl bg-card shadow-sm">
        {pending.length > 0 && (
          <div className="flex flex-wrap gap-2 pt-2.5 px-2.5">
            {pending.map(p => (
              <PendingChip key={p.localId} item={p} onRemove={() => onRemovePending(p.localId)} />
            ))}
          </div>
        )}
        <textarea
          ref={textareaRef}
          rows={2}
          value={draft}
          onChange={e => onDraftChange(e.target.value)}
          onKeyDown={e => {
            if (e.key === 'Enter' && !e.shiftKey) {
              e.preventDefault()
              onSend()
            }
          }}
          placeholder={`Message ${conversationTitle}`}
          className="block w-full border-0 outline-none resize-none pt-3 px-3.5 pb-1 text-sm leading-relaxed bg-transparent text-foreground placeholder:text-muted-foreground"
        />
        <div className="flex items-center justify-between gap-2.5 px-2 pb-2 pt-1.5">
          <div className="flex items-center gap-1.5 min-w-0">
            <button
              onClick={() => fileRef.current?.click()}
              title="Attach files"
              className="w-8 h-8 shrink-0 rounded-lg flex items-center justify-center text-muted-foreground hover:bg-muted transition-colors"
            >
              <Paperclip className="h-[17px] w-[17px]" />
            </button>
            <span className="text-[11px] text-muted-foreground/70 truncate">
              Enter to send · Shift + Enter for a new line · Drop files to attach
            </span>
          </div>
          <button
            onClick={onSend}
            disabled={!canSend}
            className={cn(
              'shrink-0 h-8 px-3.5 rounded-lg text-[13px] font-medium transition-colors',
              canSend
                ? 'bg-primary text-primary-foreground hover:bg-primary/90'
                : 'bg-muted-foreground/40 text-primary-foreground cursor-not-allowed',
            )}
          >
            {uploading ? 'Uploading…' : 'Send'}
          </button>
          <input
            ref={fileRef}
            type="file"
            multiple
            accept={ACCEPTED_FILE_TYPES}
            className="hidden"
            onChange={e => {
              if (e.target.files?.length) onAddFiles(e.target.files)
              e.target.value = ''
            }}
          />
        </div>
      </div>
    </div>
  )
}

function PendingChip({ item, onRemove }: { item: PendingAttachment; onRemove: () => void }) {
  const { theme } = useTheme()
  const ext = extOf(item.file.name)
  const sub = item.error ?? (item.attachmentId === null ? `Uploading ${item.progress}%` : formatBytes(item.file.size))

  return (
    <div
      className={cn(
        'relative flex items-center gap-2.5 w-[230px] p-2 rounded-[10px] bg-background overflow-hidden border',
        item.error ? 'border-red-200 dark:border-red-900' : 'border-border',
      )}
    >
      {item.isImage ? (
        <div
          className="w-9 h-9 shrink-0 rounded-md bg-cover bg-center"
          style={{ backgroundImage: `url(${item.previewUrl})` }}
        />
      ) : (
        <div
          className="w-9 h-9 shrink-0 rounded-md flex items-center justify-center text-[9px] font-semibold tracking-wide"
          style={extColors(ext, theme === 'dark')}
        >
          {ext}
        </div>
      )}
      <div className="flex-1 min-w-0">
        <div className="text-xs font-medium truncate">{item.file.name}</div>
        <div className={cn('text-[11px]', item.error ? 'text-red-600 dark:text-red-400' : 'text-muted-foreground')}>
          {sub}
        </div>
      </div>
      <button
        onClick={onRemove}
        title="Remove"
        className="w-6 h-6 shrink-0 rounded-md text-muted-foreground hover:bg-border text-base leading-none"
      >
        ×
      </button>
      {!item.error && item.attachmentId === null && (
        <div
          className="absolute left-0 bottom-0 h-0.5 bg-primary transition-[width] duration-150"
          style={{ width: `${item.progress}%` }}
        />
      )}
    </div>
  )
}
