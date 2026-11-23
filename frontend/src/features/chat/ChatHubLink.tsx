import { useQueryClient } from "@tanstack/react-query";
import { useAuth } from "../auth/AuthContext";
import { useEffect } from "react";
import { getChatHub, startChatHub } from "@/lib/chat-hub";
import type { PresenceResponse } from "@/api/chat";

export function ChatHubLink() {
    const {user} = useAuth()
    const queryClient = useQueryClient()

    useEffect( () => {
        if(!user || user.role === 'SuperAdmin') return

        const hub = getChatHub()

        const onMessage = (conversationId: string) => {
            queryClient.invalidateQueries({ queryKey: ['chat', 'messages', conversationId] })
            queryClient.invalidateQueries({ queryKey: ['chat', 'conversations'] })
            queryClient.invalidateQueries({ queryKey: ['chat', 'unread'] })
        }

        // Live presence: patch the cached dock list in place; fall back to a refetch
        // when the list isn't loaded yet.
        const onPresence = (userId: string, status: 'online' | 'offline') => {
            const cached = queryClient.getQueryData<PresenceResponse[]>(['chat', 'presence'])
            if (!cached) {
                queryClient.invalidateQueries({ queryKey: ['chat', 'presence'] })
                return
            }
            queryClient.setQueryData<PresenceResponse[]>(['chat', 'presence'], cached.map(p =>
                p.userId === userId
                    ? { ...p, presence: status, lastSeenAt: status === 'online' ? new Date().toISOString() : p.lastSeenAt }
                    : p,
            ))
        }

        hub.on('chat:message', onMessage)
        hub.on('presence:changed', onPresence)
        startChatHub()

        return () => {
            hub.off('chat:message', onMessage)
            hub.off('presence:changed', onPresence)
        }
    }, [user?.userId, user?.role, queryClient])

    return null
}