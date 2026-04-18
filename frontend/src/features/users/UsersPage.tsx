import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getUsers, inviteUser } from '@/api/users'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Dialog } from '@/components/ui/dialog'

const roleVariant: Record<string, 'default' | 'secondary' | 'outline'> = {
  Owner: 'default',
  Admin: 'secondary',
  Member: 'outline',
}

export function UsersPage() {
  const queryClient = useQueryClient()

  const { data: users, isLoading, isError } = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
  })

  const [dialogOpen, setDialogOpen] = useState(false)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState('Member')

  const mutInvite = useMutation({
    mutationFn: () => inviteUser({ firstName, lastName, email, password, role }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      setDialogOpen(false)
      setFirstName('')
      setLastName('')
      setEmail('')
      setPassword('')
      setRole('Member')
    },
  })

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Team</h1>
        <Button onClick={() => setDialogOpen(true)}>Invite User</Button>
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
        {users?.map(user => (
          <Card key={user.id}>
            <CardHeader className="py-4">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle className="text-base">{user.fullName}</CardTitle>
                  <p className="text-sm text-muted-foreground mt-0.5">{user.email}</p>
                </div>
                <Badge variant={roleVariant[user.role] ?? 'outline'}>{user.role}</Badge>
              </div>
            </CardHeader>
          </Card>
        ))}
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
            <Label>Password</Label>
            <Input type="password" value={password} onChange={e => setPassword(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Role</Label>
            <Select value={role} onChange={e => setRole(e.target.value)}>
              <option value="Admin">Admin</option>
              <option value="Member">Member</option>
            </Select>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button
              onClick={() => mutInvite.mutate()}
              disabled={mutInvite.isPending || !firstName.trim() || !lastName.trim() || !email.trim() || !password.trim()}
            >
              {mutInvite.isPending ? 'Inviting…' : 'Invite'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
