import { useCallback, useEffect, useState } from 'react'
import { uploadAttachment } from '@/api/chat'
import { IMAGE_RE, MAX_FILE_SIZE } from './chat-utils'

export interface PendingAttachment {
  localId: string
  file: File
  isImage: boolean
  previewUrl: string
  progress: number
  error: string | null
  /** Server id once the upload finished. */
  attachmentId: string | null
}

/** Composer upload state: validates size client-side, uploads with progress, keeps server ids. */
export function usePendingAttachments(conversationId: string) {
  const [pending, setPending] = useState<PendingAttachment[]>([])

  // Switching conversations drops unsent attachments (and their preview URLs).
  useEffect(() => {
    return () => {
      setPending(prev => {
        prev.forEach(p => URL.revokeObjectURL(p.previewUrl))
        return []
      })
    }
  }, [conversationId])

  const update = useCallback((localId: string, patch: Partial<PendingAttachment>) => {
    setPending(prev => prev.map(p => (p.localId === localId ? { ...p, ...patch } : p)))
  }, [])

  const addFiles = useCallback(
    (files: FileList | File[]) => {
      const items: PendingAttachment[] = Array.from(files).map((file, i) => ({
        localId: `${Date.now()}-${i}-${Math.random().toString(36).slice(2)}`,
        file,
        isImage: file.type.startsWith('image/') || IMAGE_RE.test(file.name),
        previewUrl: URL.createObjectURL(file),
        progress: 0,
        error: file.size > MAX_FILE_SIZE ? 'Too large · max 25 MB' : null,
        attachmentId: null,
      }))
      if (items.length === 0) return
      setPending(prev => [...prev, ...items])

      for (const item of items.filter(p => !p.error)) {
        uploadAttachment(conversationId, item.file, percent => update(item.localId, { progress: Math.min(percent, 99) }))
          .then(res => update(item.localId, { progress: 100, attachmentId: res.id }))
          .catch(() => update(item.localId, { error: 'Upload failed' }))
      }
    },
    [conversationId, update],
  )

  const remove = useCallback((localId: string) => {
    setPending(prev => {
      const item = prev.find(p => p.localId === localId)
      if (item) URL.revokeObjectURL(item.previewUrl)
      return prev.filter(p => p.localId !== localId)
    })
  }, [])

  const clear = useCallback(() => {
    setPending(prev => {
      prev.forEach(p => URL.revokeObjectURL(p.previewUrl))
      return []
    })
  }, [])

  const uploading = pending.some(p => !p.error && p.attachmentId === null)
  const readyIds = pending.filter(p => p.attachmentId !== null).map(p => p.attachmentId!)

  return { pending, addFiles, remove, clear, uploading, readyIds }
}
