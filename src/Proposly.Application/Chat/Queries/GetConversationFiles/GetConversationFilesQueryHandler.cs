using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Application.Chat.Services;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Application.Chat.Queries.GetConversationFiles;

public sealed class GetConversationFilesQueryHandler(
    IChatReadService readService,
    IConversationRepository conversations,
    ICurrentUserService currentUser) : IQueryHandler<GetConversationFilesQuery, IReadOnlyList<ConversationFileResponse>>
{
    public async Task<IReadOnlyList<ConversationFileResponse>> HandleAsync(GetConversationFilesQuery query, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByIdAsync(query.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {query.ConversationId} not found.");

        if (!conversation.IsParticipant(currentUser.UserId))
            throw new InvalidOperationException("Only participants can list this conversation's files.");

        return await readService.GetConversationFilesAsync(conversation.Id, ct);
    }
}
