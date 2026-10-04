using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Queries.GetUnreadTotal;

public sealed record GetUnreadTotalQuery : IQuery<ChatUnreadResponse>;
