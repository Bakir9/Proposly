import { api } from './client'

export interface AuthResponse {
  token: string
  userId: string
  companyId: string
  fullName: string
  email: string
  role: string
}

export interface RegisterRequest {
  companyName: string
  firstName: string
  lastName: string
  email: string
  password: string
}

export interface LoginRequest {
  email: string
  password: string
}

export const register = (data: RegisterRequest) =>
  api.post<AuthResponse>('/auth/register', data).then(r => r.data)

export const login = (data: LoginRequest) =>
  api.post<AuthResponse>('/auth/login', data).then(r => r.data)
