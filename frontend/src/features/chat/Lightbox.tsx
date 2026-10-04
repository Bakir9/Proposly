import { useEffect } from 'react'
import { Download } from 'lucide-react'
import { downloadAttachment } from './chat-utils'

export interface LightboxData {
  attachmentId: string
  fileName: string
  meta: string
  url: string | null
}

export function Lightbox({ data, onClose }: { data: LightboxData; onClose: () => void }) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [onClose])

  return (
    <div
      className="fixed inset-0 z-[100] flex items-center justify-center p-8"
      style={{ background: 'rgba(9,9,11,0.82)' }}
      onClick={onClose}
    >
      <div className="flex flex-col gap-3 max-w-[min(960px,90vw)]" onClick={e => e.stopPropagation()}>
        <div className="flex items-center gap-2.5 text-zinc-50">
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium truncate">{data.fileName}</div>
            <div className="text-xs text-zinc-400">{data.meta}</div>
          </div>
          <button
            onClick={() => downloadAttachment(data.attachmentId, data.fileName)}
            className="shrink-0 flex items-center gap-1.5 h-[34px] px-3.5 rounded-lg bg-zinc-50 text-zinc-950 text-[13px] font-medium"
          >
            <Download className="h-4 w-4" />
            Download
          </button>
          <button
            onClick={onClose}
            className="shrink-0 h-[34px] px-3.5 rounded-lg border border-zinc-600 text-zinc-50 text-[13px] font-medium"
          >
            Close
          </button>
        </div>
        {data.url ? (
          <img
            src={data.url}
            alt={data.fileName}
            className="max-w-[min(900px,85vw)] max-h-[75vh] object-contain rounded-[10px]"
          />
        ) : (
          <div className="w-[min(720px,80vw)] h-[min(460px,60vh)] rounded-[10px] bg-zinc-800 text-zinc-400 flex items-center justify-center text-[13px] font-semibold tracking-wider">
            LOADING…
          </div>
        )}
      </div>
    </div>
  )
}
