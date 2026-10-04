using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Queries.GetConversations;

/// <summary>Filter: null/"all", "direct", "group" or "project".</summary>
public sealed record GetConversationsQuery(string? Filter) : IQuery<IReadOnlyList<ConversationResponse>>;
