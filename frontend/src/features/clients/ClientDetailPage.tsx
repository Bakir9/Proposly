import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getClientById, updateClient } from '@/api/clients'
import { getOffers } from '@/api/offers'
import { useAuth } from '@/features/auth/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { ArrowLeft, FileText, Mail, Phone, User, MapPin, Pencil, Check, X } from 'lucide-react'

const statusVariant: Record<string, 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'> = {
  Draft: 'secondary',
  Sent: 'default',
  Accepted: 'success',
  Rejected: 'destructive',
  Expired: 'warning',
}

interface InlineEditProps {
  value: string
  onSave: (v: string) => void
  isPending: boolean
}

function InlineEdit({ value, onSave, isPending }: InlineEditProps) {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(value)

  if (!editing) {
    return (
      <button
        className="ml-1.5 text-muted-foreground hover:text-foreground transition-colors"
        onClick={() => { setDraft(value); setEditing(true) }}
        title="Edit"
      >
        <Pencil className="h-3.5 w-3.5" />
      </button>
    )
  }

  return (
    <span className="flex items-center gap-1 ml-2">
      <Input
        autoFocus
        value={draft}
        onChange={e => setDraft(e.target.value)}
        className="h-7 text-sm py-0 w-48"
        onKeyDown={e => {
          if (e.key === 'Enter' && draft.trim()) { onSave(draft.trim()); setEditing(false) }
          if (e.key === 'Escape') setEditing(false)
        }}
      />
      <button
        className="text-green-600 hover:text-green-700 disabled:opacity-50"
        disabled={isPending || !draft.trim()}
        onClick={() => { onSave(draft.trim()); setEditing(false) }}
        title="Save"
      >
        <Check className="h-4 w-4" />
      </button>
      <button className="text-muted-foreground hover:text-foreground" onClick={() => setEditing(false)} title="Cancel">
        <X className="h-4 w-4" />
      </button>
    </span>
  )
}

export function ClientDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const canEdit = user?.role === 'Owner' || user?.role === 'Admin'

  const { data: client, isLoading, isError } = useQuery({
    queryKey: ['client', id],
    queryFn: () => getClientById(id!),
  })

  const { data: allOffers } = useQuery({
    queryKey: ['offers'],
    queryFn: () => getOffers(),
  })

  const clientOffers = allOffers?.filter(o => o.clientId === id) ?? []

  const mutUpdate = useMutation({
    mutationFn: (patch: { name?: string; email?: string; contactPerson?: string }) =>
      updateClient(id!, {
        name: patch.name ?? client!.name,
        contactPerson: patch.contactPerson !== undefined ? patch.contactPerson : (client!.contactPerson ?? undefined),
        email: patch.email !== undefined ? patch.email : (client!.email ?? undefined),
        phone: client!.phone ?? undefined,
        street: client!.street ?? undefined,
        city: client!.city ?? undefined,
        postalCode: client!.postalCode ?? undefined,
        country: client!.country ?? undefined,
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['client', id] }),
  })

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (isError || !client) return <div className="p-6"><p className="text-destructive">Failed to load client.</p></div>

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}><ArrowLeft className="h-4 w-4 mr-1" /> Back</Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-xl flex items-center">
            {client.name}
            {canEdit && (
              <InlineEdit
                value={client.name}
                onSave={v => mutUpdate.mutate({ name: v })}
                isPending={mutUpdate.isPending}
              />
            )}
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
          {(client.contactPerson !== null || canEdit) && (
            <div>
              <span className="text-muted-foreground flex items-center gap-1"><User className="h-3 w-3" /> Contact Person</span>
              <p className="font-medium flex items-center">
                {client.contactPerson || <span className="text-muted-foreground italic">—</span>}
                {canEdit && (
                  <InlineEdit
                    value={client.contactPerson ?? ''}
                    onSave={v => mutUpdate.mutate({ contactPerson: v })}
                    isPending={mutUpdate.isPending}
                  />
                )}
              </p>
            </div>
          )}
          {(client.email !== null || canEdit) && (
            <div>
              <span className="text-muted-foreground flex items-center gap-1"><Mail className="h-3 w-3" /> Email</span>
              <p className="font-medium flex items-center">
                {client.email || <span className="text-muted-foreground italic">—</span>}
                {canEdit && (
                  <InlineEdit
                    value={client.email ?? ''}
                    onSave={v => mutUpdate.mutate({ email: v })}
                    isPending={mutUpdate.isPending}
                  />
                )}
              </p>
            </div>
          )}
          {client.phone && (
            <div><span className="text-muted-foreground flex items-center gap-1"><Phone className="h-3 w-3" /> Phone</span><p className="font-medium">{client.phone}</p></div>
          )}
          {client.street && (
            <div><span className="text-muted-foreground flex items-center gap-1"><MapPin className="h-3 w-3" /> Street</span><p className="font-medium">{client.street}</p></div>
          )}
          {client.city && (
            <div><span className="text-muted-foreground">City</span><p className="font-medium">{client.city}</p></div>
          )}
          {client.postalCode && (
            <div><span className="text-muted-foreground">Postal Code</span><p className="font-medium">{client.postalCode}</p></div>
          )}
          {client.country && (
            <div><span className="text-muted-foreground">Country</span><p className="font-medium">{client.country}</p></div>
          )}
          <div><span className="text-muted-foreground">Client since</span><p className="font-medium">{new Date(client.createdAt).toLocaleDateString()}</p></div>
        </CardContent>
      </Card>

      <div className="space-y-3">
        <h2 className="text-lg font-semibold flex items-center gap-2"><FileText className="h-4 w-4" /> Offers</h2>
        {clientOffers.length === 0 && <p className="text-sm text-muted-foreground">No offers for this client yet.</p>}
        {clientOffers.map(offer => (
          <Card
            key={offer.id}
            className="cursor-pointer hover:bg-muted/50 transition-colors"
            onClick={() => navigate(`/offers/${offer.id}`)}
          >
            <CardContent className="py-3 flex items-center justify-between">
              <div>
                <p className="font-medium text-sm">{offer.title}</p>
                <div className="flex gap-3 text-xs text-muted-foreground mt-0.5">
                  {offer.validUntil && <span>Valid until {new Date(offer.validUntil).toLocaleDateString()}</span>}
                  <span>{new Date(offer.createdAt).toLocaleDateString()}</span>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <p className="text-sm font-medium">
                  {offer.subtotal.toLocaleString('de-AT', { style: 'currency', currency: offer.currency })}
                </p>
                <Badge variant={statusVariant[offer.status] ?? 'outline'}>{offer.status}</Badge>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
