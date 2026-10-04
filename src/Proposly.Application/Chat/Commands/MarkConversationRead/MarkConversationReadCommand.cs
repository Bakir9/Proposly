using Proposly.Application.Abstractions;

namespace Proposly.Application.Chat.Commands.MarkConversationRead;

public sealed record MarkConversationReadCommand(
    Guid ConversationId,
    Guid? LastReadMessageId) : ICommand;
