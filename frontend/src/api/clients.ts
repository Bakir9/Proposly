import { api } from './client'

export interface ClientSummary {
  id: string
  name: string
  contactPerson: string | null
  email: string | null
  phone: string | null
}

export interface CreateClientRequest {
  name: string
  contactPerson?: string
  email?: string
  phone?: string
}

export const getClients = () =>
  api.get<ClientSummary[]>('/clients').then(r => r.data)

export const createClient = (data: CreateClientRequest) =>
  api.post<string>('/clients', data).then(r => r.data)
