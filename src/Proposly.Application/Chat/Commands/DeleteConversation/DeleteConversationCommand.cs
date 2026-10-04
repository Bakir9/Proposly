using Proposly.Application.Abstractions;

namespace Proposly.Application.Chat.Commands.DeleteConversation;

/// <summary>
/// Deletes a direct or group conversation for everyone, including its messages, attachments
/// and stored blobs. Project channels cannot be deleted.
/// </summary>
public sealed record DeleteConversationCommand(Guid ConversationId) : ICommand;
