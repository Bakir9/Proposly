import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Dialog } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { getActiveUsers } from '@/api/users'
import { createConversation } from '@/api/chat'
import { useAuth } from '@/features/auth/AuthContext'
import { getApiErrorMessage } from '@/lib/api-errors'
import { ChatAvatar } from './ChatAvatar'
import { hueFromId } from './chat-utils'

interface NewMessageDialogProps {
  open: boolean
  onClose: () => void
  onCreated: (conversationId: string) => void
}

export function NewMessageDialog({ open, onClose, onCreated }: NewMessageDialogProps) {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [selected, setSelected] = useState<string[]>([])
  const [title, setTitle] = useState('')

  const { data: users = [] } = useQuery({
    queryKey: ['users', 'active'],
    queryFn: getActiveUsers,
    enabled: open,
  })

  const colleagues = users.filter(u => u.id !== user?.userId && !u.isPendingInvite)
  const isGroup = selected.length > 1

  const create = useMutation({
    mutationFn: () =>
      createConversation(
        isGroup
          ? { kind: 'Group', participantUserIds: selected, title: title.trim() }
          : { kind: 'Direct', participantUserIds: selected },
      ),
    onSuccess: id => {
      queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
      reset()
      onClose()
      onCreated(id)
    },
    onError: err => toast.error(getApiErrorMessage(err)),
  })

  function reset() {
    setSelected([])
    setTitle('')
  }

  const canCreate = selected.length > 0 && (!isGroup || title.trim().length > 0)

  return (
    <Dialog open={open} onClose={() => { reset(); onClose() }} title="New message">
      <div className="space-y-4">
        <div>
          <p className="text-sm text-muted-foreground mb-2">
            Pick one person for a direct message, or several for a group.
          </p>
          <div className="max-h-64 overflow-auto border rounded-lg divide-y">
            {colleagues.length === 0 && (
              <p className="text-sm text-muted-foreground text-center py-6">No other team members yet.</p>
            )}
            {colleagues.map(u => {
              const checked = selected.includes(u.id)
              return (
                <button
                  key={u.id}
                  onClick={() =>
                    setSelected(prev => (checked ? prev.filter(id => id !== u.id) : [...prev, u.id]))
                  }
                  className={cn(
                    'w-full flex items-center gap-3 px-3 py-2 text-left transition-colors',
                    checked ? 'bg-muted' : 'hover:bg-muted/60',
                  )}
                >
                  <input type="checkbox" readOnly checked={checked} className="accent-primary" />
                  <ChatAvatar name={u.fullName} hue={hueFromId(u.id)} size={30} fontSize={11} />
                  <div className="min-w-0">
                    <p className="text-sm font-medium truncate">{u.fullName}</p>
                    <p className="text-xs text-muted-foreground">{u.role}</p>
                  </div>
                </button>
              )
            })}
          </div>
        </div>

        {isGroup && (
          <div>
            <label className="text-sm font-medium block mb-1.5">Group name</label>
            <Input value={title} onChange={e => setTitle(e.target.value)} placeholder="e.g. Offer review" />
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => { reset(); onClose() }}>
            Cancel
          </Button>
          <Button onClick={() => create.mutate()} disabled={!canCreate || create.isPending}>
            {create.isPending ? 'Starting…' : 'Start conversation'}
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
