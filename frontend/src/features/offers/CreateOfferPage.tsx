import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { createOffer, addOfferItem } from '@/api/offers'
import { getClients } from '@/api/clients'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'

interface LocalItem {
  description: string
  quantity: string
  unitPrice: string
}

export function CreateOfferPage() {
  const navigate = useNavigate()

  const { data: clients } = useQuery({ queryKey: ['clients'], queryFn: getClients })

  const [clientId, setClientId] = useState('')
  const [title, setTitle] = useState('')
  const [currency, setCurrency] = useState('EUR')
  const [validUntil, setValidUntil] = useState('')
  const [notes, setNotes] = useState('')
  const [items, setItems] = useState<LocalItem[]>([{ description: '', quantity: '1', unitPrice: '' }])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const addItemRow = () => setItems(prev => [...prev, { description: '', quantity: '1', unitPrice: '' }])

  const removeItemRow = (index: number) => setItems(prev => prev.filter((_, i) => i !== index))

  const updateItem = (index: number, field: keyof LocalItem, value: string) => {
    setItems(prev => prev.map((item, i) => i === index ? { ...item, [field]: value } : item))
  }

  const handleSubmit = async () => {
    if (!clientId || !title.trim()) {
      setError('Client and title are required.')
      return
    }
    setError('')
    setSubmitting(true)
    try {
      const offerId = await createOffer({
        clientId,
        title: title.trim(),
        notes: notes.trim() || undefined,
        currency,
        validUntil: validUntil || undefined,
      })
      const validItems = items.filter(i => i.description.trim() && !isNaN(parseFloat(i.unitPrice)))
      for (const item of validItems) {
        await addOfferItem(offerId, {
          description: item.description.trim(),
          quantity: parseFloat(item.quantity) || 1,
          unitPrice: parseFloat(item.unitPrice),
        })
      }
      navigate(`/offers/${offerId}`)
    } catch {
      setError('Failed to create offer.')
      setSubmitting(false)
    }
  }

  const computedTotal = items.reduce((sum, item) => {
    const qty = parseFloat(item.quantity) || 0
    const price = parseFloat(item.unitPrice) || 0
    return sum + qty * price
  }, 0)

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>← Back</Button>
        <h1 className="text-2xl font-semibold">New Offer</h1>
      </div>

      {error && <p className="text-destructive">{error}</p>}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Offer Details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1">
              <Label htmlFor="client">Client</Label>
              <Select id="client" value={clientId} onChange={e => setClientId(e.target.value)}>
                <option value="">Select a client…</option>
                {clients?.map(c => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </Select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="title">Title</Label>
              <Input id="title" value={title} onChange={e => setTitle(e.target.value)} placeholder="Offer title" />
            </div>
            <div className="space-y-1">
              <Label htmlFor="currency">Currency</Label>
              <Select id="currency" value={currency} onChange={e => setCurrency(e.target.value)}>
                <option value="EUR">EUR</option>
                <option value="USD">USD</option>
                <option value="GBP">GBP</option>
                <option value="CHF">CHF</option>
              </Select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="validUntil">Valid Until (optional)</Label>
              <Input id="validUntil" type="date" value={validUntil} onChange={e => setValidUntil(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1">
            <Label htmlFor="notes">Notes (optional)</Label>
            <Textarea id="notes" value={notes} onChange={e => setNotes(e.target.value)} placeholder="Additional notes…" />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <CardTitle className="text-base">Line Items</CardTitle>
          <Button size="sm" variant="outline" onClick={addItemRow}>+ Add Item</Button>
        </CardHeader>
        <CardContent className="space-y-3">
          {items.map((item, index) => (
            <div key={index} className="grid grid-cols-[1fr_auto_auto_auto] gap-2 items-end">
              <div className="space-y-1">
                {index === 0 && <Label>Description</Label>}
                <Input
                  value={item.description}
                  onChange={e => updateItem(index, 'description', e.target.value)}
                  placeholder="Item description"
                />
              </div>
              <div className="space-y-1 w-20">
                {index === 0 && <Label>Qty</Label>}
                <Input
                  type="number"
                  step="0.01"
                  value={item.quantity}
                  onChange={e => updateItem(index, 'quantity', e.target.value)}
                />
              </div>
              <div className="space-y-1 w-28">
                {index === 0 && <Label>Unit Price</Label>}
                <Input
                  type="number"
                  step="0.01"
                  value={item.unitPrice}
                  onChange={e => updateItem(index, 'unitPrice', e.target.value)}
                  placeholder="0.00"
                />
              </div>
              <div className="space-y-1 w-28 text-right">
                {index === 0 && <Label>Line Total</Label>}
                <p className="h-9 flex items-center justify-end text-sm font-medium">
                  {((parseFloat(item.quantity) || 0) * (parseFloat(item.unitPrice) || 0)).toLocaleString('de-AT', { style: 'currency', currency })}
                </p>
              </div>
              {items.length > 1 && (
                <button
                  className="text-muted-foreground hover:text-destructive text-xl leading-none self-end mb-1"
                  onClick={() => removeItemRow(index)}
                >
                  &times;
                </button>
              )}
            </div>
          ))}
          <div className="flex justify-end pt-3 border-t">
            <p className="text-base font-semibold">
              Total: {computedTotal.toLocaleString('de-AT', { style: 'currency', currency })}
            </p>
          </div>
        </CardContent>
      </Card>

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={() => navigate(-1)}>Cancel</Button>
        <Button onClick={handleSubmit} disabled={submitting}>
          {submitting ? 'Creating…' : 'Create Offer'}
        </Button>
      </div>
    </div>
  )
}
