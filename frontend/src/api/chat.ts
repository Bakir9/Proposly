import { api } from './client'

export type ConversationKind = 'Direct' | 'Group' | 'Project'

export interface ChatParticipant {
  userId: string
  name: string
  role: string
}

export interface ConversationResponse {
  id: string
  kind: ConversationKind
  title: string
  subtitle: string
  projectId: string | null
  lastMessageAt: string | null
  lastMessagePreview: string | null
  lastMessageHasAttachmentsOnly: boolean
  lastMessageAuthorFirstName: string | null
  lastMessageIsOwn: boolean
  unreadCount: number
  participants: ChatParticipant[]
}

export interface ChatAttachmentResponse {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
}

export interface ChatMessageResponse {
  id: string
  conversationId: string
  authorUserId: string
  authorName: string
  body: string | null
  createdAt: string
  attachments: ChatAttachmentResponse[]
}

export interface ChatMessagesPage {
  messages: ChatMessageResponse[]
  hasMore: boolean
}

export interface ConversationFileResponse {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploaderName: string
  createdAt: string
}

export const getConversations = (filter?: string) =>
  api.get<ConversationResponse[]>('/chat/conversations', { params: { filter } }).then(r => r.data)

export const getConversation = (id: string) =>
  api.get<ConversationResponse>(`/chat/conversations/${id}`).then(r => r.data)

export const createConversation = (data: { kind: 'Direct' | 'Group'; participantUserIds: string[]; title?: string }) =>
  api.post<string>('/chat/conversations', data).then(r => r.data)

export const getMessages = (conversationId: string, before?: string, limit = 50) =>
  api.get<ChatMessagesPage>(`/chat/conversations/${conversationId}/messages`, { params: { before, limit } }).then(r => r.data)

export const sendMessage = (conversationId: string, data: { body: string | null; attachmentIds: string[] }) =>
  api.post<ChatMessageResponse>(`/chat/conversations/${conversationId}/messages`, data).then(r => r.data)

export const markConversationRead = (conversationId: string, lastReadMessageId: string | null) =>
  api.put(`/chat/conversations/${conversationId}/read`, { lastReadMessageId })

export const getUnreadTotal = () =>
  api.get<{ totalUnread: number }>('/chat/unread').then(r => r.data.totalUnread)

export const uploadAttachment = (
  conversationId: string,
  file: File,
  onProgress?: (percent: number) => void,
) => {
  const form = new FormData()
  form.append('file', file)
  return api
    .post<ChatAttachmentResponse>(`/chat/conversations/${conversationId}/attachments`, form, {
      onUploadProgress: e => {
        if (e.total) onProgress?.(Math.round((e.loaded / e.total) * 100))
      },
    })
    .then(r => r.data)
}

/** Attachments sit behind the JWT, so downloads go through axios as blobs. */
export const fetchAttachmentBlob = (attachmentId: string) =>
  api.get<Blob>(`/chat/attachments/${attachmentId}`, { responseType: 'blob' }).then(r => r.data)

export const getConversationFiles = (conversationId: string) =>
  api.get<ConversationFileResponse[]>(`/chat/conversations/${conversationId}/files`).then(r => r.data)

export const openProjectConversation = (projectId: string) =>
  api.post<string>(`/chat/projects/${projectId}/conversation`).then(r => r.data)
