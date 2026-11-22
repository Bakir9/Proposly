using Microsoft.AspNetCore.SignalR;
using Proposly.API.Hubs;
using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.API.Services
{
    public sealed class SignalRChatNotifier(IHubContext<ChatHub> hub) : IChatNotifier
    {
        public async Task MessageSentAsync(
            Guid conversationId, 
            IReadOnlyList<Guid> participantUserIds,
            ChatMessageResponse message, 
            CancellationToken ct = default)
        {
            foreach (var uid in participantUserIds)
                await hub.Clients.Group($"user:{uid}")
                    .SendAsync("chat:message", conversationId, message, ct);
        }
    }
}
