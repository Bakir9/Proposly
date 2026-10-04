using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Application.Chat.Queries.GetChatAttachment;

public sealed class GetChatAttachmentQueryHandler(
    IChatMessageRepository messages,
    IConversationRepository conversations,
    IFileStorage fileStorage,
    ICurrentUserService currentUser) : IQueryHandler<GetChatAttachmentQuery, ChatFileContentResponse?>
{
    public async Task<ChatFileContentResponse?> HandleAsync(GetChatAttachmentQuery query, CancellationToken ct = default)
    {
        var attachment = await messages.GetAttachmentByIdAsync(query.AttachmentId, ct);
        if (attachment is null)
            return null;

        var conversation = await conversations.GetByIdAsync(attachment.ConversationId, ct);
        if (conversation is null || !conversation.IsParticipant(currentUser.UserId))
            return null;

        var content = await fileStorage.OpenAsync(attachment.StorageKey, ct);
        if (content is null)
            return null;

        return new ChatFileContentResponse(attachment.FileName, attachment.ContentType, content);
    }
}
