using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ConversationId,
    string? Body,
    IReadOnlyList<Guid> AttachmentIds) : ICommand<ChatMessageResponse>;
