import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getClients, createClient } from '@/api/clients'
import type { ClientStatus } from '@/api/clients'
import { getOffers } from '@/api/offers'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Dialog } from '@/components/ui/dialog'
import { Building2, Plus, Search, Users, CheckCircle2, DollarSign, MoreHorizontal } from 'lucide-react'

const STATUS_BADGE: Record<ClientStatus, string> = {
  Active: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20',
  Lead: 'bg-blue-500/15 text-blue-400 border-blue-500/20',
  Inactive: 'bg-slate-500/15 text-slate-400 border-slate-500/20',
}

const CLIENT_COLORS = [
  'bg-violet-500', 'bg-blue-500', 'bg-emerald-500', 'bg-amber-500',
  'bg-rose-500', 'bg-cyan-500', 'bg-pink-500', 'bg-indigo-500',
]

function clientColor(name: string) {
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) & 0xffff
  return CLIENT_COLORS[h % CLIENT_COLORS.length]
}

function clientInitials(name: string) {
  return name.split(/\s+/).slice(0, 2).map(w => w[0]).join('').toUpperCase()
}

const PAGE_SIZE = 10

export function ClientsPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: clients, isLoading, isError } = useQuery({
    queryKey: ['clients'],
    queryFn: getClients,
  })

  const { data: acceptedOffers } = useQuery({
    queryKey: ['offers', 'Accepted'],
    queryFn: () => getOffers('Accepted'),
  })

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [page, setPage] = useState(1)
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!openMenuId) return
    const handler = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpenMenuId(null)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [openMenuId])

  const [dialogOpen, setDialogOpen] = useState(false)
  const [name, setName] = useState('')
  const [contactPerson, setContactPerson] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [website, setWebsite] = useState('')
  const [street, setStreet] = useState('')
  const [city, setCity] = useState('')
  const [postalCode, setPostalCode] = useState('')
  const [country, setCountry] = useState('')
  const [currency, setCurrency] = useState('')
  const [vatNumber, setVatNumber] = useState('')
  const [newStatus, setNewStatus] = useState<ClientStatus>('Active')

  const resetForm = () => {
    setName(''); setContactPerson(''); setEmail(''); setPhone('')
    setWebsite(''); setStreet(''); setCity(''); setPostalCode('')
    setCountry(''); setCurrency(''); setVatNumber('')
    setNewStatus('Active')
  }

  const mutCreate = useMutation({
    mutationFn: () => createClient({
      name,
      contactPerson: contactPerson || undefined,
      email: email || undefined,
      phone: phone || undefined,
      website: website || undefined,
      street: street || undefined,
      city: city || undefined,
      postalCode: postalCode || undefined,
      country: country || undefined,
      currency: currency || undefined,
      vatNumber: vatNumber || undefined,
      status: newStatus,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      setDialogOpen(false)
      resetForm()
    },
  })

  const list = clients ?? []
  const totalClients = list.length
  const activeClients = list.filter(c => c.status === 'Active').length
  const clientRevenue = acceptedOffers?.reduce((s, o) => s + o.subtotal, 0) ?? 0
  const revenueFormatted = clientRevenue >= 1_000_000
    ? `$${(clientRevenue / 1_000_000).toFixed(1)}M`
    : clientRevenue >= 1_000
      ? `$${(clientRevenue / 1_000).toFixed(0)}K`
      : `$${clientRevenue.toFixed(0)}`

  const filtered = list.filter(c => {
    const matchSearch = !search ||
      c.name.toLowerCase().includes(search.toLowerCase()) ||
      (c.contactPerson ?? '').toLowerCase().includes(search.toLowerCase()) ||
      (c.email ?? '').toLowerCase().includes(search.toLowerCase())
    const matchStatus = statusFilter === 'all' || c.status === statusFilter
    return matchSearch && matchStatus
  })

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const currentPage = Math.min(page, totalPages)
  const paginated = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE)

  const goTo = (p: number) => setPage(Math.max(1, Math.min(p, totalPages)))

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <Building2 className="h-6 w-6 text-primary" />
            Client Management
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage and track your business relationships.
          </p>
        </div>
        <Button onClick={() => setDialogOpen(true)} className="gap-1.5">
          <Plus className="h-4 w-4" />
          New Client
        </Button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="rounded-xl border bg-card p-4 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Total Clients</span>
            <div className="h-8 w-8 rounded-lg bg-blue-500/10 flex items-center justify-center">
              <Users className="h-4 w-4 text-blue-400" />
            </div>
          </div>
          <p className="text-3xl font-bold">{totalClients}</p>
          <div className="h-1 rounded-full bg-muted overflow-hidden">
            <div className="h-full w-2/3 rounded-full bg-blue-500" />
          </div>
        </div>

        <div className="rounded-xl border bg-card p-4 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Active Clients</span>
            <div className="h-8 w-8 rounded-lg bg-emerald-500/10 flex items-center justify-center">
              <CheckCircle2 className="h-4 w-4 text-emerald-400" />
            </div>
          </div>
          <p className="text-3xl font-bold">{activeClients}</p>
          <div className="h-1 rounded-full bg-muted overflow-hidden">
            <div
              className="h-full rounded-full bg-emerald-500"
              style={{ width: totalClients ? `${(activeClients / totalClients) * 100}%` : '0%' }}
            />
          </div>
        </div>

        <div className="rounded-xl border bg-card p-4 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Client Revenue</span>
            <div className="h-8 w-8 rounded-lg bg-violet-500/10 flex items-center justify-center">
              <DollarSign className="h-4 w-4 text-violet-400" />
            </div>
          </div>
          <p className="text-3xl font-bold">{revenueFormatted}</p>
          <div className="h-1 rounded-full bg-muted overflow-hidden">
            <div className="h-full w-3/4 rounded-full bg-violet-500" />
          </div>
        </div>
      </div>

      {/* Client Directory */}
      <div className="rounded-xl border bg-card">
        <div className="p-4 border-b flex items-center justify-between gap-3">
          <div>
            <h2 className="font-semibold">Client Directory</h2>
            <p className="text-xs text-muted-foreground">{filtered.length} clients</p>
          </div>
          <div className="flex items-center gap-2">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Search clients..."
                value={search}
                onChange={e => { setSearch(e.target.value); setPage(1) }}
                className="pl-8 h-8 text-sm w-52"
              />
            </div>
            <Select
              value={statusFilter}
              onChange={e => { setStatusFilter(e.target.value); setPage(1) }}
              className="h-8 text-sm w-32"
            >
              <option value="all">All</option>
              <option value="Active">Active</option>
              <option value="Lead">Lead</option>
              <option value="Inactive">Inactive</option>
            </Select>
          </div>
        </div>

        {isLoading && (
          <div className="p-8 text-center text-muted-foreground text-sm">Loading clients…</div>
        )}
        {isError && (
          <div className="p-8 text-center text-destructive text-sm">Failed to load clients.</div>
        )}
        {!isLoading && !isError && filtered.length === 0 && (
          <div className="p-8 text-center text-muted-foreground text-sm">No clients found.</div>
        )}

        {paginated.length > 0 && (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b">
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Client / Company</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Contact Person</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Email</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">Status</th>
                <th className="px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {paginated.map(c => {
                const color = clientColor(c.name)
                const initials = clientInitials(c.name)
                const isMenu = openMenuId === c.id

                return (
                  <tr
                    key={c.id}
                    className="hover:bg-muted/30 transition-colors cursor-pointer"
                    onClick={() => navigate(`/clients/${c.id}`)}
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <span className={`h-8 w-8 rounded-lg ${color} flex items-center justify-center text-white text-xs font-bold flex-shrink-0`}>
                          {initials}
                        </span>
                        <span className="font-medium">{c.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{c.contactPerson ?? '—'}</td>
                    <td className="px-4 py-3 text-muted-foreground">{c.email ?? '—'}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${STATUS_BADGE[c.status]}`}>
                        {c.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right relative" onClick={e => e.stopPropagation()}>
                      <button
                        className="h-7 w-7 rounded flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-muted transition-colors ml-auto"
                        onClick={() => setOpenMenuId(isMenu ? null : c.id)}
                      >
                        <MoreHorizontal className="h-4 w-4" />
                      </button>
                      {isMenu && (
                        <div
                          ref={menuRef}
                          className="absolute right-4 top-full z-50 mt-1 w-40 rounded-lg border shadow-lg py-1 text-sm"
                          style={{ background: 'hsl(var(--card))', isolation: 'isolate' }}
                        >
                          <button
                            className="w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors"
                            onClick={() => { navigate(`/clients/${c.id}`); setOpenMenuId(null) }}
                          >
                            View Details
                          </button>
                          <button
                            className="w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors"
                            onClick={() => { navigate(`/clients/${c.id}`); setOpenMenuId(null) }}
                          >
                            Edit Client
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

        {/* Pagination */}
        {filtered.length > PAGE_SIZE && (
          <div className="px-4 py-3 border-t flex items-center justify-between text-sm text-muted-foreground">
            <span>
              Showing {(currentPage - 1) * PAGE_SIZE + 1}–{Math.min(currentPage * PAGE_SIZE, filtered.length)} of {filtered.length} clients
            </span>
            <div className="flex items-center gap-1">
              <button
                className="px-2 py-1 rounded hover:bg-muted disabled:opacity-40"
                disabled={currentPage === 1}
                onClick={() => goTo(currentPage - 1)}
              >
                ‹ Previous
              </button>
              {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => i + 1).map(p => (
                <button
                  key={p}
                  className={`h-7 w-7 rounded text-xs font-medium ${p === currentPage ? 'bg-primary text-primary-foreground' : 'hover:bg-muted'}`}
                  onClick={() => goTo(p)}
                >
                  {p}
                </button>
              ))}
              <button
                className="px-2 py-1 rounded hover:bg-muted disabled:opacity-40"
                disabled={currentPage === totalPages}
                onClick={() => goTo(currentPage + 1)}
              >
                Next ›
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Create Client Dialog */}
      <Dialog open={dialogOpen} onClose={() => { setDialogOpen(false); resetForm() }} title="New Client" className="max-w-3xl">
        <div className="grid grid-cols-2 gap-x-6 gap-y-4">

          {/* Left column */}
          <div className="space-y-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Basic Info</p>
              <div className="space-y-3">
                <div className="space-y-1">
                  <Label>Company / Client Name <span className="text-destructive">*</span></Label>
                  <Input value={name} onChange={e => setName(e.target.value)} placeholder="Acme GmbH" />
                </div>
                <div className="space-y-1">
                  <Label>Contact Person</Label>
                  <Input value={contactPerson} onChange={e => setContactPerson(e.target.value)} placeholder="John Doe" />
                </div>
                <div className="space-y-1">
                  <Label>Status</Label>
                  <Select value={newStatus} onChange={e => setNewStatus(e.target.value as ClientStatus)}>
                    <option value="Active">Active</option>
                    <option value="Lead">Lead</option>
                    <option value="Inactive">Inactive</option>
                  </Select>
                </div>
                <div className="space-y-1">
                  <Label>Email</Label>
                  <Input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="info@acme.com" />
                </div>
                <div className="space-y-1">
                  <Label>Phone</Label>
                  <Input value={phone} onChange={e => setPhone(e.target.value)} placeholder="+43 1 234 567" />
                </div>
                <div className="space-y-1">
                  <Label>Website</Label>
                  <Input value={website} onChange={e => setWebsite(e.target.value)} placeholder="https://acme.com" />
                </div>
              </div>
            </div>
          </div>

          {/* Right column */}
          <div className="space-y-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Address & Billing</p>
              <div className="space-y-3">
                <div className="space-y-1">
                  <Label>Street</Label>
                  <Input value={street} onChange={e => setStreet(e.target.value)} placeholder="Hauptstraße 1" />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <Label>Postal Code</Label>
                    <Input value={postalCode} onChange={e => setPostalCode(e.target.value)} placeholder="1010" />
                  </div>
                  <div className="space-y-1">
                    <Label>City</Label>
                    <Input value={city} onChange={e => setCity(e.target.value)} placeholder="Vienna" />
                  </div>
                </div>
                <div className="space-y-1">
                  <Label>Country</Label>
                  <Input value={country} onChange={e => setCountry(e.target.value)} placeholder="Austria" />
                </div>
                <div className="space-y-1">
                  <Label>Currency</Label>
                  <Select value={currency} onChange={e => setCurrency(e.target.value)}>
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
                  <Input value={vatNumber} onChange={e => setVatNumber(e.target.value)} placeholder="ATU12345678" />
                </div>
              </div>
            </div>
          </div>

          {/* Footer — full width */}
          <div className="col-span-2 flex justify-end gap-2 pt-2 border-t">
            <Button variant="outline" onClick={() => { setDialogOpen(false); resetForm() }}>Cancel</Button>
            <Button onClick={() => mutCreate.mutate()} disabled={mutCreate.isPending || !name.trim()}>
              {mutCreate.isPending ? 'Creating…' : 'Create Client'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
