import { api } from './client'

export type TerminStatus = 'Scheduled' | 'Cancelled'
export type InvitationStatus = 'Pending' | 'Accepted' | 'Declined' | 'RescheduleProposed'
export type MyRole = 'Organizer' | 'Invitee'

export interface TerminSummary {
  id: string
  title: string
  description: string | null
  start: string
  end: string
  location: string | null
  status: TerminStatus
  organizerId: string
  organizerName: string
  myRole: MyRole
  myInvitationStatus: InvitationStatus | null
  invitationCount: number
  acceptedCount: number
}

export interface InvitationDetail {
  id: string
  inviteeId: string
  inviteeName: string
  status: InvitationStatus
  respondedAt: string | null
  proposedStart: string | null
  proposedEnd: string | null
  proposedMessage: string | null
}

export interface TerminDetail extends TerminSummary {
  invitations: InvitationDetail[]
}

export interface UserAvailability {
  userId: string
  termins: TerminSummary[]
}

export interface CreateTerminBody {
  title: string
  description?: string | null
  start: string
  end: string
  location?: string | null
  inviteeUserIds: string[]
}

export const getTermins = (start: Date, end: Date) =>
  api.get<TerminSummary[]>('/calendar', {
    params: { start: start.toISOString(), end: end.toISOString() },
  }).then(r => r.data)

export const getPendingInvitations = () =>
  api.get<TerminSummary[]>('/calendar/pending').then(r => r.data)

export const getTerminById = (id: string) =>
  api.get<TerminDetail>(`/calendar/${id}`).then(r => r.data)

export const getUserAvailability = (userId: string, start: Date, end: Date) =>
  api.get<UserAvailability>(`/calendar/availability/${userId}`, {
    params: { start: start.toISOString(), end: end.toISOString() },
  }).then(r => r.data)

export const createTermin = (body: CreateTerminBody) =>
  api.post<string>('/calendar', body).then(r => r.data)

export const updateTermin = (id: string, body: { title: string; description?: string | null; location?: string | null }) =>
  api.put(`/calendar/${id}`, body)

export const rescheduleTermin = (id: string, newStart: string, newEnd: string) =>
  api.put(`/calendar/${id}/reschedule`, { newStart, newEnd })

export const cancelTermin = (id: string) =>
  api.post(`/calendar/${id}/cancel`)

export const deleteTermin = (id: string) =>
  api.delete(`/calendar/${id}`)

export const respondToInvitation = (id: string, action: 'Accept' | 'Decline') =>
  api.post(`/calendar/${id}/respond`, { action })

export const proposeReschedule = (id: string, proposedStart: string, proposedEnd: string, message?: string) =>
  api.post(`/calendar/${id}/propose-reschedule`, { proposedStart, proposedEnd, message })

export const respondToProposal = (id: string, inviteeId: string, accept: boolean) =>
  api.post(`/calendar/${id}/respond-to-proposal`, { inviteeId, accept })
