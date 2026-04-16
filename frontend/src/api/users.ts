import { api } from './client'

export type UserRole = 'Owner' | 'Admin' | 'Member'

export interface UserSummary {
  id: string
  fullName: string
  email: string
  role: UserRole
  createdAt: string
}

export interface UserDetail extends UserSummary {
  firstName: string
  lastName: string
  companyId: string
}

export interface InviteUserRequest {
  firstName: string
  lastName: string
  email: string
  password: string
  role: string
}

export const getMe = () =>
  api.get<UserDetail>('/users/me').then(r => r.data)

export const getUsers = () =>
  api.get<UserSummary[]>('/users').then(r => r.data)

export const inviteUser = (data: InviteUserRequest) =>
  api.post<UserDetail>('/users', data).then(r => r.data)

export const updateUserRole = (id: string, role: string) =>
  api.put(`/users/${id}/role`, { role })

export const removeUser = (id: string) =>
  api.delete(`/users/${id}`)
