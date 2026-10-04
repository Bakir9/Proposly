using Proposly.Application.Abstractions;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Application.Chat.Commands.MarkConversationUnread;

public sealed class MarkConversationUnreadCommandHandler(
    IConversationRepository conversations,
    ICurrentUserService currentUser) : ICommandHandler<MarkConversationUnreadCommand>
{
    public async Task HandleAsync(MarkConversationUnreadCommand command, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {command.ConversationId} not found.");

        conversation.MarkUnread(currentUser.UserId);
        await conversations.SaveChangesAsync(ct);
    }
}
