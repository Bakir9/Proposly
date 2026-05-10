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
import { getApiErrorMessage } from '@/lib/api-errors'
import { Textarea } from '@/components/ui/textarea'
import { ArrowLeft, Plus } from 'lucide-react'

interface LocalItem {
  description: string
  quantity: string
  unitPrice: string
}

interface ClientInfo {
  name: string
  contactPerson: string | null
  email: string | null
  phone: string | null
  street: string | null
  city: string | null
  postalCode: string | null
  country: string | null
  vatNumber: string | null
}

interface OfferPreviewProps {
  title: string
  client: ClientInfo | null
  currency: string
  validUntil: string
  notes: string
  items: LocalItem[]
  discountPercent: string
}

function OfferPreview({ title, client, currency, validUntil, notes, items, discountPercent }: OfferPreviewProps) {
  const fmt = (n: number) => n.toLocaleString('de-AT', { style: 'currency', currency })
  const validItems = items.filter(i => i.description.trim())
  const subtotal = items.reduce((sum, i) => sum + (parseFloat(i.quantity) || 0) * (parseFloat(i.unitPrice) || 0), 0)
  const discount = parseFloat(discountPercent) || 0
  const discountAmount = subtotal * (discount / 100)
  const total = subtotal - discountAmount
  const today = new Date().toLocaleDateString('de-AT')

  return (
    <div className="bg-white rounded-lg shadow-lg border text-sm text-gray-800 overflow-hidden">
      {/* Header bar */}
      <div className="bg-primary px-8 py-5 flex items-center justify-between">
        <span className="text-primary-foreground font-bold text-lg tracking-wide">PROPOSLY</span>
        <div className="text-right">
          <p className="text-primary-foreground/70 text-xs uppercase tracking-widest">Offer</p>
          <span className="inline-block bg-white/20 text-primary-foreground text-xs font-semibold px-2 py-0.5 rounded mt-0.5">
            DRAFT
          </span>
        </div>
      </div>

      <div className="px-8 py-6 space-y-6">
        {/* Meta row */}
        <div className="flex justify-between text-xs text-gray-500">
          <span>Date: {today}</span>
          {validUntil && <span>Valid until: {new Date(validUntil).toLocaleDateString('de-AT')}</span>}
        </div>

        {/* To / Subject */}
        <div className="grid grid-cols-2 gap-6">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-gray-400 mb-1">Prepared for</p>
            {client ? (
              <div className="space-y-0.5">
                <p className="font-semibold text-base text-gray-900">{client.name}</p>
                {client.contactPerson && <p className="text-gray-600 text-xs">{client.contactPerson}</p>}
                {client.street && <p className="text-gray-600 text-xs">{client.street}</p>}
                {(client.postalCode || client.city) && (
                  <p className="text-gray-600 text-xs">{[client.postalCode, client.city].filter(Boolean).join(' ')}</p>
                )}
                {client.country && <p className="text-gray-600 text-xs">{client.country}</p>}
                {client.vatNumber && <p className="text-gray-500 text-xs mt-1">VAT: {client.vatNumber}</p>}
                {client.email && <p className="text-gray-500 text-xs">{client.email}</p>}
                {client.phone && <p className="text-gray-500 text-xs">{client.phone}</p>}
              </div>
            ) : (
              <p className="text-gray-300 italic">Select a client…</p>
            )}
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-gray-400 mb-1">Subject</p>
            <p className="font-semibold text-base text-gray-900">
              {title || <span className="text-gray-300 font-normal italic">Offer title…</span>}
            </p>
          </div>
        </div>

        {/* Notes */}
        {notes.trim() && (
          <div className="bg-gray-50 rounded p-3 text-gray-600 text-xs leading-relaxed whitespace-pre-wrap">
            {notes}
          </div>
        )}

        {/* Line items */}
        <div>
          <table className="w-full text-xs">
            <thead>
              <tr className="border-b-2 border-gray-200">
                <th className="text-left pb-2 font-semibold text-gray-500 uppercase tracking-wider">Description</th>
                <th className="text-right pb-2 font-semibold text-gray-500 uppercase tracking-wider w-12">Qty</th>
                <th className="text-right pb-2 font-semibold text-gray-500 uppercase tracking-wider w-24">Unit Price</th>
                <th className="text-right pb-2 font-semibold text-gray-500 uppercase tracking-wider w-24">Total</th>
              </tr>
            </thead>
            <tbody>
              {validItems.length === 0 ? (
                <tr>
                  <td colSpan={4} className="py-4 text-center text-gray-300 italic">No items yet…</td>
                </tr>
              ) : (
                validItems.map((item, i) => {
                  const qty = parseFloat(item.quantity) || 0
                  const price = parseFloat(item.unitPrice) || 0
                  return (
                    <tr key={i} className="border-b border-gray-100">
                      <td className="py-2 text-gray-800">{item.description}</td>
                      <td className="py-2 text-right text-gray-600">{qty}</td>
                      <td className="py-2 text-right text-gray-600">{fmt(price)}</td>
                      <td className="py-2 text-right font-medium text-gray-800">{fmt(qty * price)}</td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>

          {/* Subtotal / Discount / Total */}
          <div className="flex justify-end mt-4 pt-3 border-t-2 border-gray-200">
            <div className="text-right space-y-1 min-w-[160px]">
              {discount > 0 ? (
                <>
                  <div className="flex justify-between gap-8 text-xs text-gray-500">
                    <span>Subtotal</span>
                    <span>{fmt(subtotal)}</span>
                  </div>
                  <div className="flex justify-between gap-8 text-xs text-green-600">
                    <span>Discount ({discount}%)</span>
                    <span>−{fmt(discountAmount)}</span>
                  </div>
                  <div className="flex justify-between gap-8 pt-1 border-t border-gray-200">
                    <span className="text-xs font-semibold text-gray-700 uppercase tracking-wider">Total</span>
                    <span className="text-xl font-bold text-gray-900">{fmt(total)}</span>
                  </div>
                </>
              ) : (
                <>
                  <p className="text-xs text-gray-500 uppercase tracking-wider mb-0.5">Total</p>
                  <p className="text-xl font-bold text-gray-900">{fmt(subtotal)}</p>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Footer note */}
        <p className="text-xs text-gray-400 border-t pt-4">
          This is a preview. The final offer will be generated as a PDF.
        </p>
      </div>
    </div>
  )
}

export function CreateOfferPage() {
  const navigate = useNavigate()
  const { data: clients } = useQuery({ queryKey: ['clients'], queryFn: getClients })

  const [clientId, setClientId] = useState('')
  const [title, setTitle] = useState('')
  const [currency, setCurrency] = useState('EUR')
  const [validUntil, setValidUntil] = useState('')
  const [notes, setNotes] = useState('')
  const [discountPercent, setDiscountPercent] = useState('')
  const [items, setItems] = useState<LocalItem[]>([{ description: '', quantity: '1', unitPrice: '' }])
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const addItemRow = () => setItems(prev => [...prev, { description: '', quantity: '1', unitPrice: '' }])
  const removeItemRow = (index: number) => setItems(prev => prev.filter((_, i) => i !== index))
  const updateItem = (index: number, field: keyof LocalItem, value: string) =>
    setItems(prev => prev.map((item, i) => i === index ? { ...item, [field]: value } : item))

  const handleSubmit = async () => {
    if (!clientId || !title.trim()) {
      setError('Client and title are required.')
      return
    }
    setError('')
    setSubmitting(true)
    try {
      const discount = parseFloat(discountPercent)
      const offerId = await createOffer({
        clientId,
        title: title.trim(),
        notes: notes.trim() || undefined,
        currency,
        validUntil: validUntil || undefined,
        discountPercent: !isNaN(discount) && discount > 0 ? discount : undefined,
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
    } catch (err) {
      setError(getApiErrorMessage(err))
      setSubmitting(false)
    }
  }

  const selectedClient = clients?.find(c => c.id === clientId) ?? null

  const handleClientChange = (id: string) => {
    setClientId(id)
    const c = clients?.find(cl => cl.id === id)
    if (c?.currency) setCurrency(c.currency)
  }

  return (
    <div className="p-6">
      <div className="flex items-center gap-3 mb-6">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}><ArrowLeft className="h-4 w-4 mr-1" /> Back</Button>
        <h1 className="text-2xl font-semibold">New Offer</h1>
      </div>

      {error && <p className="text-destructive mb-4">{error}</p>}

      <div className="grid grid-cols-1 xl:grid-cols-[1fr_1fr] gap-6 items-start">
        {/* ── Form ── */}
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Offer Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-1">
                  <Label htmlFor="client">Client</Label>
                  <Select id="client" value={clientId} onChange={e => handleClientChange(e.target.value)}>
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
              <Button size="sm" variant="outline" onClick={addItemRow}><Plus className="h-3.5 w-3.5 mr-1" /> Add Item</Button>
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
                    <Input type="number" step="0.01" value={item.quantity} onChange={e => updateItem(index, 'quantity', e.target.value)} />
                  </div>
                  <div className="space-y-1 w-28">
                    {index === 0 && <Label>Unit Price</Label>}
                    <Input type="number" step="0.01" value={item.unitPrice} onChange={e => updateItem(index, 'unitPrice', e.target.value)} placeholder="0.00" />
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

              {/* Discount row */}
              <div className="flex items-center justify-end gap-3 pt-3 border-t mt-1">
                <Label htmlFor="discount" className="text-sm shrink-0">Discount (%)</Label>
                <Input
                  id="discount"
                  type="number"
                  min="0"
                  max="100"
                  step="0.01"
                  value={discountPercent}
                  onChange={e => setDiscountPercent(e.target.value)}
                  placeholder="0"
                  className="w-28 text-right"
                />
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

        {/* ── Live Preview ── */}
        <div className="sticky top-6">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Live Preview</p>
          <OfferPreview
            title={title}
            client={selectedClient}
            currency={currency}
            validUntil={validUntil}
            notes={notes}
            items={items}
            discountPercent={discountPercent}
          />
        </div>
      </div>
    </div>
  )
}
