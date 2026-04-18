import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getClients, createClient } from '@/api/clients'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog } from '@/components/ui/dialog'

export function ClientsPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: clients, isLoading, isError } = useQuery({
    queryKey: ['clients'],
    queryFn: getClients,
  })

  const [dialogOpen, setDialogOpen] = useState(false)
  const [name, setName] = useState('')
  const [contactPerson, setContactPerson] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')

  const mutCreate = useMutation({
    mutationFn: () => createClient({ name, contactPerson: contactPerson || undefined, email: email || undefined, phone: phone || undefined }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      setDialogOpen(false)
      setName('')
      setContactPerson('')
      setEmail('')
      setPhone('')
    },
  })

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Clients</h1>
        <Button onClick={() => setDialogOpen(true)}>New Client</Button>
      </div>

      {isLoading && <p className="text-muted-foreground">Loading…</p>}
      {isError && <p className="text-destructive">Failed to load clients.</p>}

      {clients && clients.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No clients yet.
          </CardContent>
        </Card>
      )}

      <div className="grid gap-3">
        {clients?.map(client => (
          <Card
            key={client.id}
            className="cursor-pointer hover:bg-muted/50 transition-colors"
            onClick={() => navigate(`/clients/${client.id}`)}
          >
            <CardHeader className="py-4">
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="text-base">{client.name}</CardTitle>
                  {client.contactPerson && (
                    <p className="text-sm text-muted-foreground mt-0.5">{client.contactPerson}</p>
                  )}
                </div>
                <div className="text-right text-sm text-muted-foreground">
                  {client.email && <p>{client.email}</p>}
                  {client.phone && <p>{client.phone}</p>}
                </div>
              </div>
            </CardHeader>
          </Card>
        ))}
      </div>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} title="New Client">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Name</Label>
            <Input value={name} onChange={e => setName(e.target.value)} placeholder="Client name" />
          </div>
          <div className="space-y-1">
            <Label>Contact Person (optional)</Label>
            <Input value={contactPerson} onChange={e => setContactPerson(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Email (optional)</Label>
            <Input type="email" value={email} onChange={e => setEmail(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label>Phone (optional)</Label>
            <Input value={phone} onChange={e => setPhone(e.target.value)} />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button onClick={() => mutCreate.mutate()} disabled={mutCreate.isPending || !name.trim()}>
              {mutCreate.isPending ? 'Creating…' : 'Create Client'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
