using Proposly.Application.Chat.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Proposly.Application.Abstractions
{
    public interface IChatNotifier
    {
        Task MessageSentAsync(
            Guid conversationId,
            IReadOnlyList<Guid> participantUserId,
            ChatMessageResponse message,
            CancellationToken ct = default);
    }
}
