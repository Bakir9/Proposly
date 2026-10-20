using Proposly.Application.Abstractions;

namespace Proposly.Application.Hr.Commands.InviteWorker;

/// <summary>Grants a profile-only worker login access via the normal invite-email flow.</summary>
public sealed record InviteWorkerCommand(
    Guid UserId,
    string Email,
    string Role) : ICommand;
