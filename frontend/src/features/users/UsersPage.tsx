import { useQuery } from '@tanstack/react-query'
import { getUsers } from '@/api/users'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const roleVariant: Record<string, 'default' | 'secondary' | 'outline'> = {
  Owner: 'default',
  Admin: 'secondary',
  Member: 'outline',
}

export function UsersPage() {
  const { data: users, isLoading, isError } = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
  })

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Team</h1>
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
    </div>
  )
}
