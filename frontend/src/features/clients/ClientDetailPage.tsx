import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getClientById, updateClient, deleteClient, addClientNote } from '@/api/clients'
import type { ClientStatus } from '@/api/clients'
import { getOffers } from '@/api/offers'
import { useAuth } from '@/features/auth/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Dialog } from '@/components/ui/dialog'
import {
  ChevronRight, Pencil, Trash2, Mail, Phone, MapPin,
  DollarSign, FileText, Clock, Send, MoreHorizontal, Globe, Receipt,
} from 'lucide-react'

const STATUS_BADGE: Record<string, string> = {
  Draft: 'bg-slate-500/15 text-slate-400 border-slate-500/20',
  Sent: 'bg-blue-500/15 text-blue-400 border-blue-500/20',
  Accepted: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20',
  Rejected: 'bg-red-500/15 text-red-400 border-red-500/20',
  Expired: 'bg-amber-500/15 text-amber-400 border-amber-500/20',
}

const STATUS_DATE_PREFIX: Record<string, string> = {
  Draft: 'Drafted', Sent: 'Sent', Accepted: 'Accepted', Rejected: 'Rejected', Expired: 'Expired',
}

function timeAgo(date: string) {
  const diff = Date.now() - new Date(date).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  const days = Math.floor(hrs / 24)
  if (days === 1) return 'Yesterday'
  if (days < 7) return `${days} days ago`
  return new Date(date).toLocaleDateString()
}

function daysSince(date: string) {
  return Math.floor((Date.now() - new Date(date).getTime()) / 86400000)
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

  const clientOffers = (allOffers?.filter(o => o.clientId === id) ?? [])
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())

  // KPI computations
  const totalRevenue = clientOffers
    .filter(o => o.status === 'Accepted')
    .reduce((s, o) => s + o.subtotal, 0)

  const activeOffers = clientOffers.filter(o => o.status === 'Draft' || o.status === 'Sent')
  const pendingSignature = clientOffers.filter(o => o.status === 'Sent').length

  const lastContactDays = (() => {
    const lastSent = clientOffers.find(o => o.status === 'Sent' || o.status === 'Accepted')
    if (!lastSent) return null
    return daysSince(lastSent.createdAt)
  })()

  // Edit dialog state
  const [editOpen, setEditOpen] = useState(false)
  const [editName, setEditName] = useState('')
  const [editContact, setEditContact] = useState('')
  const [editEmail, setEditEmail] = useState('')
  const [editPhone, setEditPhone] = useState('')
  const [editWebsite, setEditWebsite] = useState('')
  const [editStreet, setEditStreet] = useState('')
  const [editCity, setEditCity] = useState('')
  const [editPostal, setEditPostal] = useState('')
  const [editCountry, setEditCountry] = useState('')
  const [editCurrency, setEditCurrency] = useState('')
  const [editVatNumber, setEditVatNumber] = useState('')
  const [editStatus, setEditStatus] = useState<ClientStatus>('Active')

  const openEdit = () => {
    if (!client) return
    setEditName(client.name)
    setEditContact(client.contactPerson ?? '')
    setEditEmail(client.email ?? '')
    setEditPhone(client.phone ?? '')
    setEditWebsite(client.website ?? '')
    setEditStreet(client.street ?? '')
    setEditCity(client.city ?? '')
    setEditPostal(client.postalCode ?? '')
    setEditCountry(client.country ?? '')
    setEditCurrency(client.currency ?? '')
    setEditVatNumber(client.vatNumber ?? '')
    setEditStatus(client.status)
    setEditOpen(true)
  }

  const mutUpdate = useMutation({
    mutationFn: () => updateClient(id!, {
      name: editName,
      contactPerson: editContact || undefined,
      email: editEmail || undefined,
      phone: editPhone || undefined,
      website: editWebsite || undefined,
      street: editStreet || undefined,
      city: editCity || undefined,
      postalCode: editPostal || undefined,
      country: editCountry || undefined,
      currency: editCurrency || undefined,
      vatNumber: editVatNumber || undefined,
      status: editStatus,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['client', id] })
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      setEditOpen(false)
    },
  })

  // Delete
  const [deleteOpen, setDeleteOpen] = useState(false)
  const mutDelete = useMutation({
    mutationFn: () => deleteClient(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      navigate('/clients')
    },
  })

  // Quick notes
  const [noteText, setNoteText] = useState('')
  const mutAddNote = useMutation({
    mutationFn: () => addClientNote(id!, noteText.trim()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['client', id] })
      setNoteText('')
    },
  })

  // Offer actions dropdown
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)
  const menuRef = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!openMenuId) return
    const h = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setOpenMenuId(null)
    }
    document.addEventListener('mousedown', h)
    return () => document.removeEventListener('mousedown', h)
  }, [openMenuId])

  if (isLoading) return <div className="p-6 text-muted-foreground text-sm">Loading…</div>
  if (isError || !client) return <div className="p-6 text-destructive text-sm">Failed to load client.</div>

  const currency = clientOffers[0]?.currency ?? 'EUR'
  const revenueFormatted = totalRevenue.toLocaleString('de-AT', { style: 'currency', currency })

  return (
    <div className="p-6 space-y-6">
      {/* Breadcrumb + header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-1 text-sm text-muted-foreground mb-1">
            <button onClick={() => navigate('/clients')} className="hover:text-foreground transition-colors">Clients</button>
            <ChevronRight className="h-3.5 w-3.5" />
            <span className="text-foreground">{client.name}</span>
          </div>
          <h1 className="text-2xl font-bold">{client.name}</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Client since {new Date(client.createdAt).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {canEdit && (
            <Button variant="outline" size="sm" onClick={() => setDeleteOpen(true)} className="gap-1.5 text-destructive border-destructive/30 hover:bg-destructive/10">
              <Trash2 className="h-4 w-4" /> Delete
            </Button>
          )}
          {canEdit && (
            <Button size="sm" onClick={openEdit} className="gap-1.5">
              <Pencil className="h-4 w-4" /> Edit
            </Button>
          )}
        </div>
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="rounded-xl border bg-card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Total Revenue</span>
            <div className="h-8 w-8 rounded-lg bg-emerald-500/10 flex items-center justify-center">
              <DollarSign className="h-4 w-4 text-emerald-400" />
            </div>
          </div>
          <p className="text-2xl font-bold">{revenueFormatted}</p>
          <p className="text-xs text-muted-foreground mt-1">From accepted offers</p>
        </div>

        <div className="rounded-xl border bg-card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Active Offers</span>
            <div className="h-8 w-8 rounded-lg bg-blue-500/10 flex items-center justify-center">
              <FileText className="h-4 w-4 text-blue-400" />
            </div>
          </div>
          <p className="text-2xl font-bold">{activeOffers.length}</p>
          <p className="text-xs text-muted-foreground mt-1">{pendingSignature} pending signature</p>
        </div>

        <div className="rounded-xl border bg-card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Last Contact</span>
            <div className="h-8 w-8 rounded-lg bg-violet-500/10 flex items-center justify-center">
              <Clock className="h-4 w-4 text-violet-400" />
            </div>
          </div>
          {lastContactDays !== null ? (
            <>
              <p className="text-2xl font-bold">{lastContactDays} days</p>
              {lastContactDays > 30 && (
                <p className="text-xs text-amber-400 mt-1">⚠ Follow-up suggested</p>
              )}
              {lastContactDays <= 30 && (
                <p className="text-xs text-muted-foreground mt-1">Since last offer</p>
              )}
            </>
          ) : (
            <>
              <p className="text-2xl font-bold">—</p>
              <p className="text-xs text-muted-foreground mt-1">No offers yet</p>
            </>
          )}
        </div>
      </div>

      {/* Main 2-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-5 gap-4">
        {/* Left: Recent Offers */}
        <div className="lg:col-span-3 rounded-xl border bg-card">
          <div className="p-4 border-b flex items-center justify-between">
            <h2 className="font-semibold">Recent Offers</h2>
            {clientOffers.length > 3 && (
              <button
                onClick={() => navigate('/offers')}
                className="text-xs text-primary hover:underline"
              >
                View all
              </button>
            )}
          </div>

          {clientOffers.length === 0 && (
            <div className="p-6 text-center text-sm text-muted-foreground">No offers for this client yet.</div>
          )}

          {clientOffers.length > 0 && (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Offer Name</th>
                  <th className="text-left px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Status</th>
                  <th className="text-right px-4 py-2.5 text-xs font-medium text-muted-foreground uppercase tracking-wide">Amount</th>
                  <th className="px-4 py-2.5 w-10" />
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {clientOffers.slice(0, 5).map(offer => {
                  const isMenu = openMenuId === offer.id
                  const prefix = STATUS_DATE_PREFIX[offer.status] ?? ''
                  const dateStr = `${prefix} ${new Date(offer.createdAt).toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' })}`

                  return (
                    <tr
                      key={offer.id}
                      className="hover:bg-muted/30 transition-colors cursor-pointer"
                      onClick={() => navigate(`/offers/${offer.id}`)}
                    >
                      <td className="px-4 py-3">
                        <p className="font-medium">{offer.title}</p>
                        <p className="text-xs text-muted-foreground mt-0.5">{dateStr}</p>
                      </td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${STATUS_BADGE[offer.status] ?? 'bg-muted text-muted-foreground border-border'}`}>
                          {offer.status}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right font-medium tabular-nums">
                        {offer.subtotal.toLocaleString('de-AT', { style: 'currency', currency: offer.currency })}
                      </td>
                      <td className="px-4 py-3 relative" onClick={e => e.stopPropagation()}>
                        <button
                          className="h-7 w-7 rounded flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                          onClick={() => setOpenMenuId(isMenu ? null : offer.id)}
                        >
                          <MoreHorizontal className="h-4 w-4" />
                        </button>
                        {isMenu && (
                          <div
                            ref={menuRef}
                            className="absolute right-4 top-full z-50 mt-1 w-36 rounded-lg border shadow-lg py-1 text-sm"
                            style={{ background: 'hsl(var(--card))', isolation: 'isolate' }}
                          >
                            <button
                              className="w-full text-left px-3 py-1.5 hover:bg-muted/50"
                              onClick={() => { navigate(`/offers/${offer.id}`); setOpenMenuId(null) }}
                            >
                              View Offer
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
        </div>

        {/* Right column */}
        <div className="lg:col-span-2 space-y-4">
          {/* Contact Information */}
          <div className="rounded-xl border bg-card p-4 space-y-4">
            <h2 className="font-semibold">Contact Information</h2>
            <div className="space-y-3 text-sm">
              {client.email && (
                <div className="flex items-start gap-3">
                  <div className="h-8 w-8 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                    <Mail className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground uppercase tracking-wide font-medium">Email</p>
                    <p className="mt-0.5">{client.email}</p>
                  </div>
                </div>
              )}
              {client.phone && (
                <div className="flex items-start gap-3">
                  <div className="h-8 w-8 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                    <Phone className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground uppercase tracking-wide font-medium">Phone</p>
                    <p className="mt-0.5">{client.phone}</p>
                  </div>
                </div>
              )}
              {(client.street || client.city) && (
                <div className="flex items-start gap-3">
                  <div className="h-8 w-8 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                    <MapPin className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground uppercase tracking-wide font-medium">Address</p>
                    <p className="mt-0.5">
                      {[client.street, [client.postalCode, client.city].filter(Boolean).join(' '), client.country].filter(Boolean).join(', ')}
                    </p>
                  </div>
                </div>
              )}
              {client.website && (
                <div className="flex items-start gap-3">
                  <div className="h-8 w-8 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                    <Globe className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground uppercase tracking-wide font-medium">Website</p>
                    <a href={client.website} target="_blank" rel="noopener noreferrer" className="mt-0.5 hover:underline text-primary">{client.website}</a>
                  </div>
                </div>
              )}
              {client.vatNumber && (
                <div className="flex items-start gap-3">
                  <div className="h-8 w-8 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                    <Receipt className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground uppercase tracking-wide font-medium">VAT Number</p>
                    <p className="mt-0.5">{client.vatNumber}</p>
                  </div>
                </div>
              )}
              {client.currency && (
                <div className="flex items-start gap-3">
                  <div className="h-8 w-8 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                    <DollarSign className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground uppercase tracking-wide font-medium">Currency</p>
                    <p className="mt-0.5">{client.currency}</p>
                  </div>
                </div>
              )}
              {!client.email && !client.phone && !client.street && !client.website && (
                <p className="text-sm text-muted-foreground">No contact information available.</p>
              )}
            </div>
          </div>

          {/* Quick Notes */}
          <div className="rounded-xl border bg-card p-4 space-y-3">
            <div className="flex items-center justify-between">
              <h2 className="font-semibold">Quick Notes</h2>
            </div>

            {/* Note list */}
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {(client.notes ?? []).length === 0 && (
                <p className="text-xs text-muted-foreground">No notes yet. Add one below.</p>
              )}
              {(client.notes ?? []).map(note => (
                <div key={note.id} className="rounded-lg border-l-2 border-primary/50 bg-muted/30 px-3 py-2.5">
                  <p className="text-sm">{note.content}</p>
                  <p className="text-xs text-muted-foreground mt-1">
                    Added by {note.authorName} • {timeAgo(note.createdAt)}
                  </p>
                </div>
              ))}
            </div>

            {/* Add note input */}
            <div className="flex gap-2 pt-1 border-t">
              <input
                value={noteText}
                onChange={e => setNoteText(e.target.value)}
                placeholder="Type a note..."
                className="flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground py-1"
                onKeyDown={e => {
                  if (e.key === 'Enter' && noteText.trim() && !mutAddNote.isPending) mutAddNote.mutate()
                }}
              />
              <button
                onClick={() => mutAddNote.mutate()}
                disabled={!noteText.trim() || mutAddNote.isPending}
                className="text-primary hover:text-primary/80 disabled:opacity-40 transition-colors"
              >
                <Send className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Edit Dialog */}
      {canEdit && (
        <Dialog open={editOpen} onClose={() => setEditOpen(false)} title="Edit Client">
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1 col-span-2">
                <Label>Name</Label>
                <Input value={editName} onChange={e => setEditName(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>Contact Person</Label>
                <Input value={editContact} onChange={e => setEditContact(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>Status</Label>
                <Select value={editStatus} onChange={e => setEditStatus(e.target.value as ClientStatus)}>
                  <option value="Active">Active</option>
                  <option value="Lead">Lead</option>
                  <option value="Inactive">Inactive</option>
                </Select>
              </div>
              <div className="space-y-1">
                <Label>Email</Label>
                <Input type="email" value={editEmail} onChange={e => setEditEmail(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>Phone</Label>
                <Input value={editPhone} onChange={e => setEditPhone(e.target.value)} />
              </div>
              <div className="space-y-1 col-span-2">
                <Label>Website</Label>
                <Input value={editWebsite} onChange={e => setEditWebsite(e.target.value)} placeholder="https://…" />
              </div>
              <div className="space-y-1 col-span-2">
                <Label>Street</Label>
                <Input value={editStreet} onChange={e => setEditStreet(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>City</Label>
                <Input value={editCity} onChange={e => setEditCity(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>Postal Code</Label>
                <Input value={editPostal} onChange={e => setEditPostal(e.target.value)} />
              </div>
              <div className="space-y-1 col-span-2">
                <Label>Country</Label>
                <Input value={editCountry} onChange={e => setEditCountry(e.target.value)} />
              </div>
              <div className="space-y-1">
                <Label>Currency</Label>
                <Select value={editCurrency} onChange={e => setEditCurrency(e.target.value)}>
                  <option value="">— none —</option>
                  <option value="EUR">EUR</option>
                  <option value="USD">USD</option>
                  <option value="GBP">GBP</option>
                  <option value="CHF">CHF</option>
                  <option value="BAM">BAM</option>
                  <option value="HRK">HRK</option>
                  <option value="RSD">RSD</option>
                </Select>
              </div>
              <div className="space-y-1">
                <Label>VAT Number</Label>
                <Input value={editVatNumber} onChange={e => setEditVatNumber(e.target.value)} placeholder="ATU12345678" />
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-1">
              <Button variant="outline" onClick={() => setEditOpen(false)}>Cancel</Button>
              <Button onClick={() => mutUpdate.mutate()} disabled={mutUpdate.isPending || !editName.trim()}>
                {mutUpdate.isPending ? 'Saving…' : 'Save Changes'}
              </Button>
            </div>
          </div>
        </Dialog>
      )}

      {/* Delete Confirm Dialog */}
      {canEdit && (
        <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)} title="Delete Client">
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Are you sure you want to delete <span className="font-medium text-foreground">{client.name}</span>? This action cannot be undone.
            </p>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setDeleteOpen(false)}>Cancel</Button>
              <Button
                variant="destructive"
                onClick={() => mutDelete.mutate()}
                disabled={mutDelete.isPending}
              >
                {mutDelete.isPending ? 'Deleting…' : 'Delete Client'}
              </Button>
            </div>
          </div>
        </Dialog>
      )}
    </div>
  )
}
