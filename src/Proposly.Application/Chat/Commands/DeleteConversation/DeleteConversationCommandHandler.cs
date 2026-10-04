using Proposly.Application.Abstractions;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Application.Chat.Commands.DeleteConversation;

public sealed class DeleteConversationCommandHandler(
    IConversationRepository conversations,
    IChatMessageRepository messages,
    IFileStorage fileStorage,
    ICurrentUserService currentUser) : ICommandHandler<DeleteConversationCommand>
{
    public async Task HandleAsync(DeleteConversationCommand command, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {command.ConversationId} not found.");

        conversation.EnsureDeletableBy(currentUser.UserId);

        // Blobs live behind IFileStorage and are not reached by the database cascade.
        var storageKeys = await messages.GetAttachmentStorageKeysAsync(conversation.Id, ct);
        foreach (var key in storageKeys)
            await fileStorage.DeleteAsync(key, ct);

        // Pending attachments carry no FK to a message, so the cascade cannot reach them either.
        await messages.RemoveAttachmentsForConversationAsync(conversation.Id, ct);

        await conversations.RemoveAsync(conversation, ct);
        await conversations.SaveChangesAsync(ct);
    }
}
