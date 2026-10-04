using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Chat.Commands.SendMessage;

public sealed class SendMessageCommandHandler(
    IConversationRepository conversations,
    IChatMessageRepository messages,
    IUserRepository users,
    ICurrentUserService currentUser) : ICommandHandler<SendMessageCommand, ChatMessageResponse>
{
    public async Task<ChatMessageResponse> HandleAsync(SendMessageCommand command, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByIdAsync(command.ConversationId, ct)
            ?? throw new InvalidOperationException($"Conversation {command.ConversationId} not found.");

        if (!conversation.IsParticipant(currentUser.UserId))
            throw new InvalidOperationException("Only participants can send messages in this conversation.");

        var author = await users.GetByIdAsync(currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Current user not found.");
        var authorName = $"{author.FirstName} {author.LastName}";

        var attachmentIds = command.AttachmentIds.Distinct().ToList();
        var pending = await messages.GetPendingAttachmentsAsync(attachmentIds, conversation.Id, ct);
        if (pending.Count != attachmentIds.Count)
            throw new InvalidOperationException("One or more attachments were not found in this conversation or are already sent.");

        var message = ChatMessage.Create(
            currentUser.CompanyId,
            conversation.Id,
            currentUser.UserId,
            authorName,
            command.Body,
            hasAttachments: pending.Count > 0);

        foreach (var attachment in pending)
            attachment.AttachTo(message.Id, currentUser.UserId);

        await messages.AddAsync(message, ct);

        conversation.RegisterMessageSent(message.CreatedAt);
        // Sending implies having read the conversation up to your own message.
        conversation.MarkRead(currentUser.UserId, message.Id);

        await messages.SaveChangesAsync(ct);

        return new ChatMessageResponse(
            message.Id,
            message.ConversationId,
            message.AuthorUserId,
            message.AuthorName,
            message.Body,
            message.CreatedAt,
            pending.Select(a => new ChatAttachmentResponse(a.Id, a.FileName, a.ContentType, a.SizeBytes)).ToList());
    }
}
