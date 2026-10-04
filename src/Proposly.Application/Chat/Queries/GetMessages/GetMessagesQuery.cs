using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Queries.GetMessages;

/// <summary>Cursor-paged, newest window first; pass the oldest loaded message id as Before to page back.</summary>
public sealed record GetMessagesQuery(
    Guid ConversationId,
    Guid? Before,
    int Limit = 50) : IQuery<ChatMessagesPageResponse>;
