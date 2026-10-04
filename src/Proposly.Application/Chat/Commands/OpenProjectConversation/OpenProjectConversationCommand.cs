using Proposly.Application.Abstractions;

namespace Proposly.Application.Chat.Commands.OpenProjectConversation;

/// <summary>
/// Returns the project's channel, creating it when missing (projects that predate the chat
/// feature have none) and syncing its participants with the project's current members.
/// Idempotent — the Discussion tab calls it on every open.
/// </summary>
public sealed record OpenProjectConversationCommand(Guid ProjectId) : ICommand<Guid>;
