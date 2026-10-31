using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Queries.GetPresence;

/// <summary>Colleagues of the current user's company with presence, for the chat dock.</summary>
public sealed record GetPresenceQuery : IQuery<IReadOnlyList<PresenceResponse>>;
