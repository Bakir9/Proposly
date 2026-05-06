import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import {
  getOffers, sendOffer, acceptOffer, rejectOffer, expireOffer, deleteOffer, downloadOfferPdf,
} from '@/api/offers'
import type { OfferStatus } from '@/api/offers'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Select } from '@/components/ui/select'
import { FileText, Plus, Search, TrendingUp, Target, Users, MoreVertical, ArrowUpRight } from 'lucide-react'

type BadgeVariant = 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'

const STATUS_VARIANT: Record<OfferStatus, BadgeVariant> = {
  Draft: 'secondary',
  Sent: 'default',
  Accepted: 'success',
  Rejected: 'destructive',
  Expired: 'warning',
}

const CLIENT_COLORS = ['#3b82f6', '#22c55e', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4', '#ec4899']

function clientColor(name: string) {
  let hash = 0
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash)
  return CLIENT_COLORS[Math.abs(hash) % CLIENT_COLORS.length]
}

function clientInitials(name: string) {
  return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase()
}

const OFFERS_PER_PAGE = 10

export function OffersPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { data: offers = [], isLoading, isError } = useQuery({
    queryKey: ['offers'],
    queryFn: () => getOffers(),
  })

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<OfferStatus | 'all'>('all')
  const [sortBy, setSortBy] = useState<'createdAt' | 'amount' | 'status'>('createdAt')
  const [page, setPage] = useState(1)
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['offers'] })
  const mutSend   = useMutation({ mutationFn: (id: string) => sendOffer(id),   onSuccess: invalidate })
  const mutAccept = useMutation({ mutationFn: (id: string) => acceptOffer(id), onSuccess: invalidate })
  const mutReject = useMutation({ mutationFn: (id: string) => rejectOffer(id), onSuccess: invalidate })
  const mutExpire = useMutation({ mutationFn: (id: string) => expireOffer(id), onSuccess: invalidate })
  const mutDelete = useMutation({ mutationFn: (id: string) => deleteOffer(id), onSuccess: invalidate })

  useEffect(() => {
    if (!openMenuId) return
    const close = () => setOpenMenuId(null)
    document.addEventListener('click', close)
    return () => document.removeEventListener('click', close)
  }, [openMenuId])

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (isError)   return <div className="p-6"><p className="text-destructive">Failed to load offers.</p></div>

  const currency     = offers[0]?.currency ?? 'EUR'
  const fmt          = (n: number) => n.toLocaleString('de-AT', { style: 'currency', currency })
  const sentOffers   = offers.filter(o => o.status === 'Sent')
  const acceptedCount = offers.filter(o => o.status === 'Accepted').length
  const totalPending  = sentOffers.reduce((s, o) => s + o.subtotal, 0)
  const conversionPct = offers.length > 0 ? (acceptedCount / offers.length * 100).toFixed(1) : '0.0'
  const avgDealSize   = offers.length > 0 ? offers.reduce((s, o) => s + o.subtotal, 0) / offers.length : 0
  const activeCampaigns = offers.filter(o => o.status === 'Sent' || o.status === 'Draft').length

  const filtered = offers
    .filter(o => {
      if (statusFilter !== 'all' && o.status !== statusFilter) return false
      if (search) {
        const q = search.toLowerCase()
        if (!o.title.toLowerCase().includes(q) && !o.clientName.toLowerCase().includes(q)) return false
      }
      return true
    })
    .sort((a, b) => {
      if (sortBy === 'amount') return b.subtotal - a.subtotal
      if (sortBy === 'status') return a.status.localeCompare(b.status)
      return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    })

  const totalPages  = Math.ceil(filtered.length / OFFERS_PER_PAGE)
  const pagedOffers = filtered.slice((page - 1) * OFFERS_PER_PAGE, page * OFFERS_PER_PAGE)
  const fmtDate     = (s: string) =>
    new Date(s).toLocaleDateString('de-AT', { day: '2-digit', month: 'short', year: 'numeric' })

  return (
    <div className="p-6 space-y-5">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl font-bold">Offers Management</h1>
          <p className="text-sm text-muted-foreground mt-1">Manage and track your business proposals and client quotations.</p>
        </div>
        <Button onClick={() => navigate('/offers/new')} className="gap-1.5">
          <Plus className="h-4 w-4" /> New Offer
        </Button>
      </div>

      {/* KPI cards */}
      <div className="grid grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-5 pb-5">
            <div className="flex items-start justify-between">
              <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Total Pending Value</p>
              <div className="rounded-lg bg-blue-500/15 p-2"><TrendingUp className="h-5 w-5 text-blue-500" /></div>
            </div>
            <p className="text-3xl font-bold mt-3 leading-tight">{fmt(totalPending)}</p>
            <p className="text-xs text-muted-foreground mt-1">{sentOffers.length} sent offer{sentOffers.length !== 1 ? 's' : ''} pending</p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5">
            <div className="flex items-start justify-between">
              <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Conversion Rate</p>
              <div className="rounded-lg bg-green-500/15 p-2"><Target className="h-5 w-5 text-green-500" /></div>
            </div>
            <p className="text-4xl font-bold mt-3">{conversionPct}%</p>
            <div className="mt-2 w-full bg-muted rounded-full h-1.5">
              <div className="h-1.5 rounded-full bg-green-500 transition-all" style={{ width: `${conversionPct}%` }} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5">
            <div className="flex items-start justify-between">
              <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Avg. Deal Size</p>
              <div className="rounded-lg bg-violet-500/15 p-2"><ArrowUpRight className="h-5 w-5 text-violet-500" /></div>
            </div>
            <p className="text-3xl font-bold mt-3 leading-tight">{fmt(avgDealSize)}</p>
            <p className="text-xs text-muted-foreground mt-1">Based on {offers.length} offer{offers.length !== 1 ? 's' : ''}</p>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-5 pb-5">
            <div className="flex items-start justify-between">
              <p className="text-[10px] font-bold uppercase tracking-widest text-muted-foreground">Active Campaigns</p>
              <div className="rounded-lg bg-amber-500/15 p-2"><Users className="h-5 w-5 text-amber-500" /></div>
            </div>
            <p className="text-4xl font-bold mt-3">{activeCampaigns}</p>
            <p className="text-xs text-muted-foreground mt-1">{acceptedCount} accepted total</p>
          </CardContent>
        </Card>
      </div>

      {/* Table card */}
      <Card>
        <CardContent className="p-0">
          {/* Search + filter bar */}
          <div className="flex items-center gap-3 px-5 py-4 border-b">
            <div className="relative flex-1 max-w-md">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search offers or clients..."
                value={search}
                onChange={e => { setSearch(e.target.value); setPage(1) }}
                className="w-full h-9 pl-9 pr-3 text-sm bg-muted rounded-lg border-0 focus:outline-none focus:ring-1 focus:ring-primary placeholder:text-muted-foreground"
              />
            </div>
            <Select
              value={statusFilter}
              onChange={e => { setStatusFilter(e.target.value as typeof statusFilter); setPage(1) }}
              className="h-9 text-sm w-36"
            >
              <option value="all">All Statuses</option>
              <option value="Draft">Draft</option>
              <option value="Sent">Sent</option>
              <option value="Accepted">Accepted</option>
              <option value="Rejected">Rejected</option>
              <option value="Expired">Expired</option>
            </Select>
            <div className="flex items-center gap-2 ml-auto text-sm text-muted-foreground shrink-0">
              <span>Sort by:</span>
              <Select value={sortBy} onChange={e => setSortBy(e.target.value as typeof sortBy)} className="h-9 text-sm w-40">
                <option value="createdAt">Created Date</option>
                <option value="amount">Amount</option>
                <option value="status">Status</option>
              </Select>
            </div>
          </div>

          {/* Table header */}
          {filtered.length > 0 && (
            <div className="grid grid-cols-[1fr_180px_150px_120px_190px_48px] px-5 py-2.5 text-[10px] font-bold uppercase tracking-widest text-muted-foreground border-b bg-muted/30">
              <span>Offer Details</span>
              <span>Client</span>
              <span className="text-right">Amount ({currency})</span>
              <span className="text-center">Status</span>
              <span>Validity</span>
              <span />
            </div>
          )}

          {/* Rows */}
          {filtered.length === 0 ? (
            <div className="py-16 text-center">
              <FileText className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
              <p className="text-sm text-muted-foreground">
                {offers.length === 0 ? 'No offers yet. Create your first offer.' : 'No offers match your filters.'}
              </p>
            </div>
          ) : (
            <div className="divide-y divide-border">
              {pagedOffers.map(offer => {
                const isExpiredDate = offer.validUntil
                  && new Date(offer.validUntil) < new Date()
                  && offer.status !== 'Accepted'
                return (
                  <div
                    key={offer.id}
                    className="grid grid-cols-[1fr_180px_150px_120px_190px_48px] items-center px-5 py-4 hover:bg-muted/20 transition-colors cursor-pointer"
                    onClick={() => navigate(`/offers/${offer.id}`)}
                  >
                    {/* Offer details */}
                    <div>
                      <p className="text-sm font-semibold leading-tight">{offer.title}</p>
                      <p className="text-xs text-muted-foreground mt-0.5">Created: {fmtDate(offer.createdAt)}</p>
                    </div>

                    {/* Client */}
                    <div className="flex items-center gap-2.5">
                      <span
                        className="inline-flex items-center justify-center w-8 h-8 rounded-lg font-bold text-xs text-white shrink-0"
                        style={{ backgroundColor: clientColor(offer.clientName) }}
                      >
                        {clientInitials(offer.clientName)}
                      </span>
                      <p className="text-sm font-medium truncate">{offer.clientName}</p>
                    </div>

                    {/* Amount */}
                    <p className="text-sm font-semibold text-right">
                      {offer.subtotal.toLocaleString('de-AT', { style: 'currency', currency: offer.currency })}
                    </p>

                    {/* Status */}
                    <div className="flex justify-center">
                      <Badge variant={STATUS_VARIANT[offer.status]} className="text-[10px] uppercase tracking-wide">
                        {offer.status}
                      </Badge>
                    </div>

                    {/* Validity */}
                    <div>
                      {offer.validUntil ? (
                        <>
                          <p className={`text-xs font-medium ${isExpiredDate || offer.status === 'Expired' ? 'text-destructive' : ''}`}>
                            {offer.status === 'Expired' ? 'Expired' : 'Valid until'}: {fmtDate(offer.validUntil)}
                          </p>
                        </>
                      ) : (
                        <p className="text-xs text-muted-foreground">No expiry set</p>
                      )}
                    </div>

                    {/* Actions menu */}
                    <div className="relative flex justify-center" onClick={e => e.stopPropagation()}>
                      <button
                        className="w-8 h-8 rounded-md flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                        onClick={e => { e.stopPropagation(); setOpenMenuId(openMenuId === offer.id ? null : offer.id) }}
                      >
                        <MoreVertical className="h-4 w-4" />
                      </button>
                      {openMenuId === offer.id && (
                        <div className="absolute right-0 top-9 z-50 w-44 border border-border rounded-lg shadow-xl py-1 text-sm" style={{ background: 'hsl(var(--card))', isolation: 'isolate' }}>
                          <button
                            className="w-full text-left px-3 py-2 hover:bg-muted transition-colors"
                            onClick={() => { setOpenMenuId(null); navigate(`/offers/${offer.id}`) }}
                          >
                            View Details
                          </button>
                          {offer.status === 'Draft' && (
                            <button
                              className="w-full text-left px-3 py-2 hover:bg-muted transition-colors"
                              onClick={() => { setOpenMenuId(null); mutSend.mutate(offer.id) }}
                            >
                              Send to Client
                            </button>
                          )}
                          {offer.status === 'Sent' && (<>
                            <button
                              className="w-full text-left px-3 py-2 hover:bg-muted text-green-600 transition-colors"
                              onClick={() => { setOpenMenuId(null); mutAccept.mutate(offer.id) }}
                            >
                              Mark Accepted
                            </button>
                            <button
                              className="w-full text-left px-3 py-2 hover:bg-muted transition-colors"
                              onClick={() => { setOpenMenuId(null); mutReject.mutate(offer.id) }}
                            >
                              Mark Rejected
                            </button>
                            <button
                              className="w-full text-left px-3 py-2 hover:bg-muted transition-colors"
                              onClick={() => { setOpenMenuId(null); mutExpire.mutate(offer.id) }}
                            >
                              Mark Expired
                            </button>
                          </>)}
                          {offer.status === 'Accepted' && (
                            <button
                              className="w-full text-left px-3 py-2 hover:bg-muted transition-colors"
                              onClick={() => { setOpenMenuId(null); downloadOfferPdf(offer.id, offer.title) }}
                            >
                              Download PDF
                            </button>
                          )}
                          <div className="border-t border-border my-1" />
                          <button
                            className="w-full text-left px-3 py-2 hover:bg-muted text-destructive transition-colors"
                            onClick={() => { setOpenMenuId(null); mutDelete.mutate(offer.id) }}
                          >
                            Delete
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          {/* Pagination */}
          {filtered.length > OFFERS_PER_PAGE && (
            <div className="flex items-center justify-between px-5 py-3.5 border-t text-sm text-muted-foreground">
              <span>
                Showing {(page - 1) * OFFERS_PER_PAGE + 1}–{Math.min(page * OFFERS_PER_PAGE, filtered.length)} of {filtered.length} results
              </span>
              <div className="flex items-center gap-1">
                <button
                  className="w-8 h-8 rounded border flex items-center justify-center hover:bg-muted disabled:opacity-40"
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={page === 1}
                >‹</button>
                {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => i + 1).map(pg => (
                  <button
                    key={pg}
                    className={`w-8 h-8 rounded border flex items-center justify-center text-xs ${page === pg ? 'bg-primary text-primary-foreground border-primary' : 'hover:bg-muted'}`}
                    onClick={() => setPage(pg)}
                  >{pg}</button>
                ))}
                <button
                  className="w-8 h-8 rounded border flex items-center justify-center hover:bg-muted disabled:opacity-40"
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                >›</button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
