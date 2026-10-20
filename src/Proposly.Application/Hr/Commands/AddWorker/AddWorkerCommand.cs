using Proposly.Application.Abstractions;

namespace Proposly.Application.Hr.Commands.AddWorker;

/// <summary>
/// Creates a worker without login access: a profile-only user, an HR profile and default
/// employment terms. From this moment the person can be assigned to projects; login access
/// is granted later via InviteWorker.
/// </summary>
public sealed record AddWorkerCommand(
    string FirstName,
    string LastName,
    string? JobTitle) : ICommand<Guid>;
