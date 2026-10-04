import { Download } from 'lucide-react'
import { useTheme } from '@/context/ThemeContext'
import { cn } from '@/lib/utils'
import type { ChatAttachmentResponse } from '@/api/chat'
import {
  downloadAttachment,
  extColors,
  extOf,
  formatBytes,
  IMAGE_RE,
  useAttachmentUrl,
} from './chat-utils'
import type { LightboxData } from './Lightbox'

interface AttachmentGridProps {
  attachments: ChatAttachmentResponse[]
  /** "Author · Day HH:mm" — the lightbox meta prefix for these attachments. */
  metaPrefix: string
  alignEnd: boolean
  onOpenLightbox: (data: LightboxData) => void
}

/** Image tiles + document cards below a message, per the design handoff. */
export function AttachmentGrid({ attachments, metaPrefix, alignEnd, onOpenLightbox }: AttachmentGridProps) {
  const images = attachments.filter(a => IMAGE_RE.test(a.fileName))
  const docs = attachments.filter(a => !IMAGE_RE.test(a.fileName))

  return (
    <>
      {images.length > 0 && (
        <div className={cn('flex flex-wrap gap-1.5', alignEnd && 'justify-end')}>
          {images.map(a => (
            <ImageTile
              key={a.id}
              attachment={a}
              wide={images.length === 1}
              onOpen={url =>
                onOpenLightbox({
                  attachmentId: a.id,
                  fileName: a.fileName,
                  meta: `${metaPrefix} · ${formatBytes(a.sizeBytes)}`,
                  url,
                })
              }
            />
          ))}
        </div>
      )}
      {docs.length > 0 && (
        <div className="flex flex-col gap-1.5 w-[290px] max-w-full">
          {docs.map(a => (
            <FileCard key={a.id} attachment={a} />
          ))}
        </div>
      )}
    </>
  )
}

function ImageTile({
  attachment,
  wide,
  onOpen,
}: {
  attachment: ChatAttachmentResponse
  wide: boolean
  onOpen: (url: string | null) => void
}) {
  const url = useAttachmentUrl(attachment.id, true)
  return (
    <button
      onClick={() => onOpen(url)}
      title={attachment.fileName}
      className="relative h-[150px] rounded-xl border overflow-hidden bg-muted cursor-zoom-in"
      style={{ width: wide ? 260 : 180 }}
    >
      {url ? (
        <img src={url} alt={attachment.fileName} className="w-full h-full object-cover" />
      ) : (
        <div className="w-full h-full flex items-center justify-center text-[11px] font-semibold tracking-wider text-muted-foreground">
          {extOf(attachment.fileName)}
        </div>
      )}
      <div
        className="absolute inset-x-0 bottom-0 pt-3.5 px-2 pb-1.5 text-white text-[11px] text-left truncate"
        style={{ background: 'linear-gradient(rgba(9,9,11,0), rgba(9,9,11,0.6))' }}
      >
        {attachment.fileName}
      </div>
    </button>
  )
}

export function FileCard({ attachment }: { attachment: ChatAttachmentResponse }) {
  const { theme } = useTheme()
  const ext = extOf(attachment.fileName)
  return (
    <div className="flex items-center gap-2.5 py-2 pl-2.5 pr-2 border rounded-[10px] bg-card">
      <div
        className="w-[34px] h-10 shrink-0 rounded-md flex items-center justify-center text-[10px] font-semibold tracking-wide"
        style={extColors(ext, theme === 'dark')}
      >
        {ext}
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-[13px] font-medium truncate">{attachment.fileName}</div>
        <div className="text-xs text-muted-foreground">{formatBytes(attachment.sizeBytes)}</div>
      </div>
      <button
        onClick={() => downloadAttachment(attachment.id, attachment.fileName)}
        title="Download"
        className="w-8 h-8 shrink-0 rounded-lg flex items-center justify-center text-muted-foreground hover:bg-muted transition-colors"
      >
        <Download className="h-4 w-4" />
      </button>
    </div>
  )
}
