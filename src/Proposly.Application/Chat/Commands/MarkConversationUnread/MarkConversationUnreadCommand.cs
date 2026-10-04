using Proposly.Application.Abstractions;

namespace Proposly.Application.Chat.Commands.MarkConversationUnread;

public sealed record MarkConversationUnreadCommand(Guid ConversationId) : ICommand;
