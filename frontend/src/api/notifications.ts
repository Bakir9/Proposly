import { api } from './client'

export interface NotificationResponse {
  id: string
  title: string
  link: string | null
  isRead: boolean
  createdAt: string
}

export const getNotifications = (limit = 50) =>
  api.get<NotificationResponse[]>('/notifications', { params: { limit } }).then(r => r.data)

export const getAllNotifications = () =>
  api.get<NotificationResponse[]>('/notifications', { params: { limit: null } }).then(r => r.data)

export const markNotificationRead = (id: string) =>
  api.put(`/notifications/${id}/read`)

export const markAllNotificationsRead = () =>
  api.put('/notifications/read-all')
