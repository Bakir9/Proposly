import { useQuery } from '@tanstack/react-query'
import { Download } from 'lucide-react'
import { useTheme } from '@/context/ThemeContext'
import {
  getConversation,
  getConversationFiles,
  openProjectConversation,
} from '@/api/chat'
import { useAuth } from '@/features/auth/AuthContext'
import {
  downloadAttachment,
  extColors,
  extOf,
  formatBytes,
  hueFromId,
} from './chat-utils'
import { ChatAvatar } from './ChatAvatar'
import { Thread } from './Thread'

/** The project channel embedded in the project page: thread + members and shared files panel. */
export function ProjectDiscussionTab({ projectId }: { projectId: string }) {
  const { user } = useAuth()
  const { theme } = useTheme()

  // Idempotent: creates the channel for projects that predate chat and joins missing members.
  const { data: conversationId, isError } = useQuery({
    queryKey: ['chat', 'project-conversation', projectId],
    queryFn: () => openProjectConversation(projectId),
    staleTime: 60_000,
    retry: false,
  })

  const { data: conversation } = useQuery({
    queryKey: ['chat', 'conversation', conversationId],
    queryFn: () => getConversation(conversationId!),
    enabled: !!conversationId,
    refetchInterval: 30_000,
  })

  const { data: files = [] } = useQuery({
    queryKey: ['chat', 'files', conversationId],
    queryFn: () => getConversationFiles(conversationId!),
    enabled: !!conversationId,
    refetchInterval: 30_000,
  })

  if (isError)
    return (
      <p className="text-sm text-muted-foreground py-10 text-center">
        Only project members can open this discussion.
      </p>
    )

  if (!conversation)
    return <p className="text-sm text-muted-foreground py-10 text-center">Loading discussion…</p>

  const others = conversation.participants

  return (
    <div className="flex border rounded-lg overflow-hidden bg-card" style={{ height: 'calc(100vh - 260px)', minHeight: 420 }}>
      <Thread key={conversation.id} conversation={conversation} showHeader={false} />

      <div className="w-[260px] shrink-0 border-l bg-background py-5 px-[18px] flex flex-col gap-3.5 overflow-auto">
        <div className="text-[13px] font-semibold">Channel members</div>
        <div className="flex flex-col gap-3">
          {others.map(p => (
            <div key={p.userId} className="flex items-center gap-2.5">
              <ChatAvatar name={p.name} hue={hueFromId(p.userId)} size={30} fontSize={11} />
              <div className="min-w-0">
                <div className="text-[13px] font-medium truncate">
                  {p.name}
                  {p.userId === user?.userId && <span className="text-muted-foreground"> (you)</span>}
                </div>
                <div className="text-xs text-muted-foreground">{p.role}</div>
              </div>
            </div>
          ))}
        </div>

        <div className="text-[13px] font-semibold border-t pt-3.5">Shared files</div>
        <div className="flex flex-col gap-2">
          {files.length === 0 && <p className="text-xs text-muted-foreground">No files shared yet.</p>}
          {files.map(f => {
            const ext = extOf(f.fileName)
            return (
              <div key={f.id} className="flex items-center gap-2.5">
                <div
                  className="w-7 h-8 shrink-0 rounded-[5px] flex items-center justify-center text-[8px] font-semibold tracking-wide"
                  style={extColors(ext, theme === 'dark')}
                >
                  {ext}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-xs font-medium truncate">{f.fileName}</div>
                  <div className="text-[11px] text-muted-foreground">{formatBytes(f.sizeBytes)}</div>
                </div>
                <button
                  onClick={() => downloadAttachment(f.id, f.fileName)}
                  title="Download"
                  className="w-7 h-7 shrink-0 rounded-md flex items-center justify-center text-muted-foreground hover:bg-border transition-colors"
                >
                  <Download className="h-4 w-4" />
                </button>
              </div>
            )
          })}
        </div>

        <div className="text-xs leading-relaxed text-muted-foreground border-t pt-3.5">
          People assigned to this project join automatically.
        </div>
      </div>
    </div>
  )
}
