using Proposly.Application.Abstractions;

namespace Proposly.Application.Chat.Commands.CreateConversation;

/// <summary>
/// Starts a direct or group conversation. Project channels are created through
/// OpenProjectConversation, never directly.
/// </summary>
public sealed record CreateConversationCommand(
    string Kind,
    IReadOnlyList<Guid> ParticipantUserIds,
    string? Title) : ICommand<Guid>;
