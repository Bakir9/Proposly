using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.RejectAbsence;

public sealed class RejectAbsenceCommandHandler : ICommandHandler<RejectAbsenceCommand>
{
    private readonly IAbsenceRepository _absences;
    private readonly ICurrentUserService _currentUser;

    public RejectAbsenceCommandHandler(
        IAbsenceRepository absences,
        ICurrentUserService currentUser)
    {
        _absences = absences;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(RejectAbsenceCommand command, CancellationToken ct = default)
    {
        var absence = await _absences.GetByIdAsync(command.AbsenceId, ct)
            ?? throw new InvalidOperationException($"Absence {command.AbsenceId} not found.");

        // No entitlement is consumed by a rejection, so nothing to adjust.
        absence.Reject(_currentUser.UserId, command.Reason);

        await _absences.SaveChangesAsync(ct);
    }
}
