using Proposly.Application.Abstractions;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Application.Hr.Commands.SetWorkerNotePinned;

public sealed class SetWorkerNotePinnedCommandHandler(
    IEmployeeProfileRepository profiles) : ICommandHandler<SetWorkerNotePinnedCommand>
{
    public async Task HandleAsync(SetWorkerNotePinnedCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Worker {command.UserId} has no HR profile.");

        profile.SetNotePinned(command.NoteId, command.Pinned);
        await profiles.SaveChangesAsync(ct);
    }
}
