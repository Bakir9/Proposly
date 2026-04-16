import { useQuery } from '@tanstack/react-query'
import { getOffers } from '@/api/offers'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const statusVariant: Record<string, 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'> = {
  Draft: 'secondary',
  Sent: 'default',
  Accepted: 'success',
  Rejected: 'destructive',
  Expired: 'warning',
}

export function OffersPage() {
  const { data: offers, isLoading, isError } = useQuery({
    queryKey: ['offers'],
    queryFn: () => getOffers(),
  })

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Offers</h1>
      </div>

      {isLoading && <p className="text-muted-foreground">Loading…</p>}
      {isError && <p className="text-destructive">Failed to load offers.</p>}

      {offers && offers.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No offers yet.
          </CardContent>
        </Card>
      )}

      <div className="grid gap-3">
        {offers?.map(offer => (
          <Card key={offer.id}>
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="text-base">{offer.title}</CardTitle>
                  <p className="text-sm text-muted-foreground mt-0.5">{offer.clientName}</p>
                </div>
                <Badge variant={statusVariant[offer.status] ?? 'outline'}>{offer.status}</Badge>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex items-center gap-6 text-sm text-muted-foreground">
                <span className="font-medium text-foreground">
                  {offer.subtotal.toLocaleString('de-AT', { style: 'currency', currency: offer.currency })}
                </span>
                {offer.validUntil && (
                  <span>Valid until {new Date(offer.validUntil).toLocaleDateString()}</span>
                )}
                <span>{new Date(offer.createdAt).toLocaleDateString()}</span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
