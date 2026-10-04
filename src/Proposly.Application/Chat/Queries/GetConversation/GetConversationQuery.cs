using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Queries.GetConversation;

public sealed record GetConversationQuery(Guid ConversationId) : IQuery<ConversationResponse?>;
