import { api } from './client'

export type OfferStatus = 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Expired'

export interface OfferSummary {
  id: string
  clientId: string
  clientName: string
  title: string
  status: OfferStatus
  subtotal: number
  currency: string
  validUntil: string | null
  createdAt: string
}

export interface OfferItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
  currency: string
}

export interface OfferDetail extends OfferSummary {
  notes: string | null
  sentAt: string | null
  items: OfferItem[]
}

export interface CreateOfferRequest {
  clientId: string
  title: string
  notes?: string
  currency: string
  validUntil?: string
}

export const getOffers = (status?: OfferStatus) =>
  api.get<OfferSummary[]>('/offers', { params: status ? { status } : {} }).then(r => r.data)

export const getOfferById = (id: string) =>
  api.get<OfferDetail>(`/offers/${id}`).then(r => r.data)

export const createOffer = (data: CreateOfferRequest) =>
  api.post<string>('/offers', data).then(r => r.data)

export const sendOffer = (id: string) =>
  api.post(`/offers/${id}/send`)

export const acceptOffer = (id: string) =>
  api.post(`/offers/${id}/accept`)

export const rejectOffer = (id: string) =>
  api.post(`/offers/${id}/reject`)
