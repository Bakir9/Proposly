using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Application.Chat.Commands.UploadChatAttachment;

public sealed class UploadChatAttachmentCommandHandler(
    IConversationRepository conversations,
    IChatMessageRepository messages,
    IFileStorage fileStorage,
    ICurrentUserService currentUser) : ICommandHandler<UploadChatAttachmentCommand, ChatAttachmentResponse>
{
    public async Task<ChatAttachmentResponse> HandleAsync(UploadChatAttachmentCommand command, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {command.ConversationId} not found.");

        if (!conversation.IsParticipant(currentUser.UserId))
            throw new InvalidOperationException("Only participants can upload files in this conversation.");

        var storageKey = await fileStorage.SaveAsync(command.Content, command.ContentType, ct);

        var attachment = ChatAttachment.Create(
            currentUser.CompanyId,
            conversation.Id,
            currentUser.UserId,
            command.FileName,
            command.ContentType,
            command.Content.LongLength,
            storageKey);

        await messages.AddAttachmentAsync(attachment, ct);
        await messages.SaveChangesAsync(ct);

        return new ChatAttachmentResponse(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes);
    }
}
