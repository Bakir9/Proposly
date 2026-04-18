import { api } from './client'

export interface ClientSummary {
  id: string
  name: string
  contactPerson: string | null
  email: string | null
  phone: string | null
}

export interface ClientDetail extends ClientSummary {
  street: string | null
  city: string | null
  postalCode: string | null
  country: string | null
  createdAt: string
}

export interface CreateClientRequest {
  name: string
  contactPerson?: string
  email?: string
  phone?: string
}

export interface UpdateClientRequest {
  name: string
  contactPerson?: string
  email?: string
  phone?: string
  street?: string
  city?: string
  postalCode?: string
  country?: string
}

export const getClients = () =>
  api.get<ClientSummary[]>('/clients').then(r => r.data)

export const getClientById = (id: string) =>
  api.get<ClientDetail>(`/clients/${id}`).then(r => r.data)

export const createClient = (data: CreateClientRequest) =>
  api.post<string>('/clients', data).then(r => r.data)

export const updateClient = (id: string, data: UpdateClientRequest) =>
  api.put(`/clients/${id}`, data)
