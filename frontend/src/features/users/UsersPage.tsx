import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getUsers, inviteUser, toggleUserStatus, removeUser } from '@/api/users'
import { useAuth } from '@/features/auth/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Dialog } from '@/components/ui/dialog'
import { Users, UserPlus, UserX, UserCheck, Trash2 } from 'lucide-react'

const roleVariant: Record<string, 'default' | 'secondary' | 'outline'> = {
  Owner: 'default',
  Admin: 'secondary',
  Member: 'outline',
}

export function UsersPage() {
  const queryClient = useQueryClient()
  const { user: currentUser } = useAuth()

  const canManage = currentUser?.role === 'Owner' || currentUser?.role === 'Admin'

  const { data: users, isLoading, isError } = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
  })

  const [dialogOpen, setDialogOpen] = useState(false)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('Member')

  const mutInvite = useMutation({
    mutationFn: () => inviteUser({ firstName, lastName, email, role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      setDialogOpen(false)
      setFirstName('')
      setLastName('')
      setEmail('')
      setRole('Member')
    },
  })

  const mutToggle = useMutation({
    mutationFn: ({ id, disable }: { id: string; disable: boolean }) =>
      toggleUserStatus(id, disable),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  })

  const mutRemove = useMutation({
    mutationFn: (id: string) => removeUser(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  })

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold flex items-center gap-2">
          <Users className="h-6 w-6" /> Team
        </h1>
        {canManage && (
          <Button onClick={() => setDialogOpen(true)}>
            <UserPlus className="h-4 w-4 mr-1" /> Invite User
          </Button>
        )}
      </div>

      {isLoading && <p className="text-muted-foreground">Loading…</p>}
      {isError && <p className="text-destructive">Failed to load users.</p>}

      {users && users.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No team members yet.
          </CardContent>
        </Card>
      )}

      <div className="grid gap-3">
        {users?.map(user => {
          const isSelf = user.id === currentUser?.userId
          const isOwner = user.role === 'Owner'
          const canAct = canManage && !isSelf && !isOwner

          return (
            <Card key={user.id} className={user.isDisabled ? 'opacity-60' : ''}>
              <CardHeader className="py-4">
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="text-base flex items-center gap-2">
                      {user.fullName}
                      {user.isPendingInvite && (
                        <span className="text-xs font-normal text-amber-500">(invite pending)</span>
                      )}
                      {user.isDisabled && !user.isPendingInvite && (
                        <span className="text-xs font-normal text-muted-foreground">(disabled)</span>
                      )}
                    </CardTitle>
                    <p className="text-sm text-muted-foreground mt-0.5">{user.email}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge variant={roleVariant[user.role] ?? 'outline'}>{user.role}</Badge>
                    {canAct && (
                      <>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => mutToggle.mutate({ id: user.id, disable: !user.isDisabled })}
                          disabled={mutToggle.isPending}
                          title={user.isDisabled ? 'Enable user' : 'Disable user'}
                        >
                          {user.isDisabled
                            ? <UserCheck className="h-4 w-4" />
                            : <UserX className="h-4 w-4" />}
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-destructive hover:bg-destructive hover:text-destructive-foreground"
                          onClick={() => {
                            if (confirm(`Remove ${user.fullName} from the team?`)) {
                              mutRemove.mutate(user.id)
                            }
                          }}
                          disabled={mutRemove.isPending}
                          title="Remove user"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </>
                    )}
                  </div>
                </div>
              </CardHeader>
            </Card>
          )
        })}
      </div>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} title="Invite User">
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>First Name</Label>
              <Input value={firstName} onChange={e => setFirstName(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Last Name</Label>
              <Input value={lastName} onChange={e => setLastName(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1">
            <Label>Email</Label>
            <Input type="email" value={email} onChange={e => setEmail(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Role</Label>
            <Select value={role} onChange={e => setRole(e.target.value)}>
              <option value="Admin">Admin</option>
              <option value="Member">Member</option>
            </Select>
          </div>
          {mutInvite.isError && (
            <p className="text-sm text-destructive">{getApiErrorMessage(mutInvite.error)}</p>
          )}
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button
              onClick={() => mutInvite.mutate()}
              disabled={mutInvite.isPending || !firstName.trim() || !lastName.trim() || !email.trim()}
            >
              {mutInvite.isPending ? 'Inviting…' : 'Invite'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
