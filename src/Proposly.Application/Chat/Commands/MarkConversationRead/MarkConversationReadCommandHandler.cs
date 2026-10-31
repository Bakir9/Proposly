using Proposly.Application.Abstractions;
using Proposly.Domain.Chat.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.Chat.Commands.MarkConversationRead;

public sealed class MarkConversationReadCommandHandler(
    IConversationRepository conversations,
    ICurrentUserService currentUser) : ICommandHandler<MarkConversationReadCommand>
{
    public async Task HandleAsync(MarkConversationReadCommand command, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByIdAsync(command.ConversationId, ct)
            ?? throw AppException.NotFound("conversation.not_found", $"Conversation {command.ConversationId} not found.");

        conversation.MarkRead(currentUser.UserId, command.LastReadMessageId);
        await conversations.SaveChangesAsync(ct);
    }
}
