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

export interface UpdateOfferRequest {
  title: string
  notes?: string
  validUntil?: string
}

export interface AddOfferItemRequest {
  description: string
  quantity: number
  unitPrice: number
}

export const getOffers = (status?: OfferStatus) =>
  api.get<OfferSummary[]>('/offers', { params: status ? { status } : {} }).then(r => r.data)

export const getOfferById = (id: string) =>
  api.get<OfferDetail>(`/offers/${id}`).then(r => r.data)

export const createOffer = (data: CreateOfferRequest) =>
  api.post<string>('/offers', data).then(r => r.data)

export const updateOffer = (id: string, data: UpdateOfferRequest) =>
  api.put(`/offers/${id}`, data)

export const sendOffer = (id: string) =>
  api.post(`/offers/${id}/send`)

export const acceptOffer = (id: string) =>
  api.post(`/offers/${id}/accept`)

export const rejectOffer = (id: string) =>
  api.post(`/offers/${id}/reject`)

export const expireOffer = (id: string) =>
  api.post(`/offers/${id}/expire`)

export const addOfferItem = (offerId: string, data: AddOfferItemRequest) =>
  api.post<string>(`/offers/${offerId}/items`, data).then(r => r.data)

export const updateOfferItem = (offerId: string, itemId: string, data: AddOfferItemRequest) =>
  api.put(`/offers/${offerId}/items/${itemId}`, data)

export const removeOfferItem = (offerId: string, itemId: string) =>
  api.delete(`/offers/${offerId}/items/${itemId}`)

export const downloadOfferPdf = async (id: string, title: string) => {
  const response = await api.get(`/offers/${id}/pdf`, { responseType: 'blob' })
  const url = URL.createObjectURL(response.data as Blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `Offer-${title.replace(/\s+/g, '_')}.pdf`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

export const sendOfferEmail = (id: string) =>
  api.post(`/offers/${id}/email`)
