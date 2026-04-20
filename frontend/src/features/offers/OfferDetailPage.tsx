import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getOfferById,
  updateOffer,
  sendOffer,
  acceptOffer,
  rejectOffer,
  expireOffer,
  deleteOffer,
  addOfferItem,
  removeOfferItem,
  downloadOfferPdf,
  sendOfferEmail,
  type AddOfferItemRequest,
} from '@/api/offers'
import { useAuth } from '@/features/auth/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Dialog } from '@/components/ui/dialog'
import { ArrowLeft, Pencil, Trash2, Send, CheckCircle, XCircle, Clock, Download, Mail, Plus } from 'lucide-react'

const statusVariant: Record<string, 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'> = {
  Draft: 'secondary',
  Sent: 'default',
  Accepted: 'success',
  Rejected: 'destructive',
  Expired: 'warning',
}

export function OfferDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user } = useAuth()

  const { data: offer, isLoading, isError } = useQuery({
    queryKey: ['offer', id],
    queryFn: () => getOfferById(id!),
  })

  const [editTitle, setEditTitle] = useState('')
  const [editNotes, setEditNotes] = useState('')
  const [editValidUntil, setEditValidUntil] = useState('')
  const [editMode, setEditMode] = useState(false)

  const [addItemOpen, setAddItemOpen] = useState(false)
  const [newDesc, setNewDesc] = useState('')
  const [newQty, setNewQty] = useState('1')
  const [newUnitPrice, setNewUnitPrice] = useState('')

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['offer', id] })
    queryClient.invalidateQueries({ queryKey: ['offers'] })
  }

  const [emailSent, setEmailSent] = useState(false)
  const [statusError, setStatusError] = useState<string | null>(null)

  const clearStatusError = () => setStatusError(null)
  const onStatusError = (err: unknown) => {
    const msg =
      (err as { response?: { data?: { detail?: string; title?: string; message?: string } } })
        ?.response?.data?.detail ??
      (err as { response?: { data?: { title?: string } } })?.response?.data?.title ??
      (err as { message?: string })?.message ??
      'Status change failed.'
    setStatusError(msg)
  }

  const mutSend = useMutation({ mutationFn: () => sendOffer(id!), onSuccess: () => { clearStatusError(); invalidate() }, onError: onStatusError })
  const mutAccept = useMutation({ mutationFn: () => acceptOffer(id!), onSuccess: () => { clearStatusError(); invalidate() }, onError: onStatusError })
  const mutReject = useMutation({ mutationFn: () => rejectOffer(id!), onSuccess: () => { clearStatusError(); invalidate() }, onError: onStatusError })
  const mutExpire = useMutation({ mutationFn: () => expireOffer(id!), onSuccess: () => { clearStatusError(); invalidate() }, onError: onStatusError })
  const mutSendEmail = useMutation({
    mutationFn: () => sendOfferEmail(id!),
    onSuccess: () => setEmailSent(true),
  })

  const mutDelete = useMutation({
    mutationFn: () => deleteOffer(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['offers'] })
      navigate('/offers')
    },
  })

  const mutUpdate = useMutation({
    mutationFn: () => updateOffer(id!, { title: editTitle, notes: editNotes || undefined, validUntil: editValidUntil || undefined }),
    onSuccess: () => { invalidate(); setEditMode(false) },
  })

  const mutAddItem = useMutation({
    mutationFn: (data: AddOfferItemRequest) => addOfferItem(id!, data),
    onSuccess: () => {
      invalidate()
      setAddItemOpen(false)
      setNewDesc('')
      setNewQty('1')
      setNewUnitPrice('')
    },
  })

  const mutRemoveItem = useMutation({
    mutationFn: (itemId: string) => removeOfferItem(id!, itemId),
    onSuccess: invalidate,
  })

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (isError || !offer) return <div className="p-6"><p className="text-destructive">Failed to load offer.</p></div>

  const isDraft = offer.status === 'Draft'
  const isSent = offer.status === 'Sent'
  const isAdmin = user?.role === 'Admin'
  const canDelete = isAdmin || offer.status === 'Draft' || offer.status === 'Expired'

  const handleEditOpen = () => {
    setEditTitle(offer.title)
    setEditNotes(offer.notes ?? '')
    setEditValidUntil(offer.validUntil ? offer.validUntil.slice(0, 10) : '')
    setEditMode(true)
  }

  const handleAddItem = () => {
    const qty = parseFloat(newQty)
    const price = parseFloat(newUnitPrice)
    if (!newDesc.trim() || isNaN(qty) || isNaN(price)) return
    mutAddItem.mutate({ description: newDesc.trim(), quantity: qty, unitPrice: price })
  }

  const total = offer.items.reduce((sum, item) => sum + item.lineTotal, 0)

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}><ArrowLeft className="h-4 w-4 mr-1" /> Back</Button>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div>
              <CardTitle className="text-xl">{offer.title}</CardTitle>
              <p className="text-sm text-muted-foreground mt-1">{offer.clientName}</p>
              {offer.notes && <p className="text-sm text-muted-foreground mt-1">{offer.notes}</p>}
            </div>
            <div className="flex items-center gap-2">
              <Badge variant={statusVariant[offer.status] ?? 'outline'}>{offer.status}</Badge>
              <Button variant="outline" size="sm" onClick={handleEditOpen}><Pencil className="h-3.5 w-3.5 mr-1" /> Edit</Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={() => { if (window.confirm('Delete this offer? This cannot be undone.')) mutDelete.mutate() }}
                disabled={!canDelete || mutDelete.isPending}
                title={!canDelete ? 'Only Draft or Expired offers can be deleted' : undefined}
              >
                <Trash2 className="h-3.5 w-3.5 mr-1" />{mutDelete.isPending ? 'Deleting…' : 'Delete'}
              </Button>
            </div>
          </div>
          <div className="flex gap-6 text-sm text-muted-foreground mt-2">
            {offer.validUntil && <span>Valid until {new Date(offer.validUntil).toLocaleDateString()}</span>}
            <span>Created {new Date(offer.createdAt).toLocaleDateString()}</span>
            {offer.sentAt && <span>Sent {new Date(offer.sentAt).toLocaleDateString()}</span>}
          </div>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm text-muted-foreground w-28 shrink-0">Change status:</span>
            {isDraft && (
              <Button
                onClick={() => mutSend.mutate()}
                disabled={mutSend.isPending || offer.items.length === 0}
                title={offer.items.length === 0 ? 'Add at least one item before sending' : undefined}
              >
                <Send className="h-3.5 w-3.5 mr-1" />{mutSend.isPending ? 'Sending…' : 'Mark as Sent'}
              </Button>
            )}
            {isDraft && offer.items.length === 0 && (
              <span className="text-xs text-muted-foreground">Add items first</span>
            )}
            {isSent && (
              <>
                <Button variant="default" onClick={() => mutAccept.mutate()} disabled={mutAccept.isPending}>
                  <CheckCircle className="h-3.5 w-3.5 mr-1" />{mutAccept.isPending ? 'Accepting…' : 'Accept'}
                </Button>
                <Button variant="destructive" onClick={() => mutReject.mutate()} disabled={mutReject.isPending}>
                  <XCircle className="h-3.5 w-3.5 mr-1" />{mutReject.isPending ? 'Rejecting…' : 'Reject'}
                </Button>
                <Button variant="outline" onClick={() => mutExpire.mutate()} disabled={mutExpire.isPending}>
                  <Clock className="h-3.5 w-3.5 mr-1" />{mutExpire.isPending ? 'Expiring…' : 'Expire'}
                </Button>
              </>
            )}
            {!isDraft && !isSent && (
              <span className="text-sm text-muted-foreground">No transitions available for this status.</span>
            )}
          </div>
          {statusError && (
            <p className="text-sm text-destructive">{statusError}</p>
          )}
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm text-muted-foreground w-28 shrink-0">Export:</span>
            <Button variant="outline" onClick={() => downloadOfferPdf(id!, offer.title)}>
              <Download className="h-3.5 w-3.5 mr-1" /> Download PDF
            </Button>
            <Button variant="outline" onClick={() => mutSendEmail.mutate()} disabled={mutSendEmail.isPending}>
              <Mail className="h-3.5 w-3.5 mr-1" />{mutSendEmail.isPending ? 'Sending…' : emailSent ? 'Email sent ✓' : 'Send by Email'}
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <CardTitle className="text-base">Line Items</CardTitle>
          {isDraft && (
            <Button size="sm" onClick={() => setAddItemOpen(true)}><Plus className="h-3.5 w-3.5 mr-1" /> Add Item</Button>
          )}
        </CardHeader>
        <CardContent>
          {offer.items.length === 0 ? (
            <p className="text-sm text-muted-foreground">No items yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-muted-foreground border-b">
                  <th className="pb-2 font-medium">Description</th>
                  <th className="pb-2 font-medium text-right">Qty</th>
                  <th className="pb-2 font-medium text-right">Unit Price</th>
                  <th className="pb-2 font-medium text-right">Line Total</th>
                  {isDraft && <th className="pb-2 w-8" />}
                </tr>
              </thead>
              <tbody>
                {offer.items.map(item => (
                  <tr key={item.id} className="border-b last:border-0">
                    <td className="py-2">{item.description}</td>
                    <td className="py-2 text-right">{item.quantity}</td>
                    <td className="py-2 text-right">
                      {item.unitPrice.toLocaleString('de-AT', { style: 'currency', currency: item.currency })}
                    </td>
                    <td className="py-2 text-right font-medium">
                      {item.lineTotal.toLocaleString('de-AT', { style: 'currency', currency: item.currency })}
                    </td>
                    {isDraft && (
                      <td className="py-2 text-right">
                        <button
                          className="text-muted-foreground hover:text-destructive text-lg leading-none"
                          onClick={() => mutRemoveItem.mutate(item.id)}
                          disabled={mutRemoveItem.isPending}
                        >
                          &times;
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <div className="flex justify-end mt-4 pt-3 border-t">
            <p className="text-base font-semibold">
              Total: {total.toLocaleString('de-AT', { style: 'currency', currency: offer.currency })}
            </p>
          </div>
        </CardContent>
      </Card>

      <Dialog open={editMode} onClose={() => setEditMode(false)} title="Edit Offer">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label htmlFor="edit-title">Title</Label>
            <Input id="edit-title" value={editTitle} onChange={e => setEditTitle(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="edit-notes">Notes</Label>
            <Textarea id="edit-notes" value={editNotes} onChange={e => setEditNotes(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="edit-valid">Valid Until</Label>
            <Input id="edit-valid" type="date" value={editValidUntil} onChange={e => setEditValidUntil(e.target.value)} />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setEditMode(false)}>Cancel</Button>
            <Button onClick={() => mutUpdate.mutate()} disabled={mutUpdate.isPending}>
              {mutUpdate.isPending ? 'Saving…' : 'Save'}
            </Button>
          </div>
        </div>
      </Dialog>

      <Dialog open={addItemOpen} onClose={() => setAddItemOpen(false)} title="Add Item">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label htmlFor="item-desc">Description</Label>
            <Input id="item-desc" value={newDesc} onChange={e => setNewDesc(e.target.value)} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="item-qty">Quantity</Label>
              <Input id="item-qty" type="number" step="0.01" value={newQty} onChange={e => setNewQty(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="item-price">Unit Price</Label>
              <Input id="item-price" type="number" step="0.01" value={newUnitPrice} onChange={e => setNewUnitPrice(e.target.value)} />
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setAddItemOpen(false)}>Cancel</Button>
            <Button onClick={handleAddItem} disabled={mutAddItem.isPending}>
              {mutAddItem.isPending ? 'Adding…' : 'Add'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
