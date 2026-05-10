import { api } from './client'

export type ClientStatus = 'Active' | 'Lead' | 'Inactive'

export interface ClientSummary {
  id: string
  name: string
  contactPerson: string | null
  email: string | null
  phone: string | null
  website: string | null
  street: string | null
  city: string | null
  postalCode: string | null
  country: string | null
  currency: string | null
  vatNumber: string | null
  status: ClientStatus
}

export interface ClientNote {
  id: string
  content: string
  authorName: string
  createdAt: string
}

export interface ClientDetail extends ClientSummary {
  createdAt: string
  notes: ClientNote[]
}

export interface CreateClientRequest {
  name: string
  contactPerson?: string
  email?: string
  phone?: string
  website?: string
  street?: string
  city?: string
  postalCode?: string
  country?: string
  currency?: string
  vatNumber?: string
  status?: ClientStatus
}

export interface UpdateClientRequest {
  name: string
  contactPerson?: string
  email?: string
  phone?: string
  website?: string
  street?: string
  city?: string
  postalCode?: string
  country?: string
  currency?: string
  vatNumber?: string
  status?: ClientStatus
}

export const getClients = () =>
  api.get<ClientSummary[]>('/clients').then(r => r.data)

export const getClientById = (id: string) =>
  api.get<ClientDetail>(`/clients/${id}`).then(r => r.data)

export const createClient = (data: CreateClientRequest) =>
  api.post<string>('/clients', data).then(r => r.data)

export const updateClient = (id: string, data: UpdateClientRequest) =>
  api.put(`/clients/${id}`, data)

export const deleteClient = (id: string) =>
  api.delete(`/clients/${id}`)

export const addClientNote = (id: string, content: string) =>
  api.post<string>(`/clients/${id}/notes`, { content }).then(r => r.data)
