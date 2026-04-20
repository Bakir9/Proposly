import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getClientById } from '@/api/clients'
import { getOffers } from '@/api/offers'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ArrowLeft, FileText, Mail, Phone, User, MapPin } from 'lucide-react'

const statusVariant: Record<string, 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'> = {
  Draft: 'secondary',
  Sent: 'default',
  Accepted: 'success',
  Rejected: 'destructive',
  Expired: 'warning',
}

export function ClientDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data: client, isLoading, isError } = useQuery({
    queryKey: ['client', id],
    queryFn: () => getClientById(id!),
  })

  const { data: allOffers } = useQuery({
    queryKey: ['offers'],
    queryFn: () => getOffers(),
  })

  const clientOffers = allOffers?.filter(o => o.clientId === id) ?? []

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (isError || !client) return <div className="p-6"><p className="text-destructive">Failed to load client.</p></div>

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}><ArrowLeft className="h-4 w-4 mr-1" /> Back</Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-xl">{client.name}</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
          {client.contactPerson && (
            <div><span className="text-muted-foreground flex items-center gap-1"><User className="h-3 w-3" /> Contact Person</span><p className="font-medium">{client.contactPerson}</p></div>
          )}
          {client.email && (
            <div><span className="text-muted-foreground flex items-center gap-1"><Mail className="h-3 w-3" /> Email</span><p className="font-medium">{client.email}</p></div>
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
