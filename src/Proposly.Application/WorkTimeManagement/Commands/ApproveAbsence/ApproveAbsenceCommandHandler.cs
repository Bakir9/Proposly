using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveAbsence;

public sealed class ApproveAbsenceCommandHandler : ICommandHandler<ApproveAbsenceCommand>
{
    private readonly IAbsenceRepository _absences;
    private readonly ICurrentUserService _currentUser;

    public ApproveAbsenceCommandHandler(
        IAbsenceRepository absences,
        ICurrentUserService currentUser)
    {
        _absences = absences;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(ApproveAbsenceCommand command, CancellationToken ct = default)
    {
        var absence = await _absences.GetByIdAsync(command.AbsenceId, ct)
            ?? throw new InvalidOperationException($"Absence {command.AbsenceId} not found.");

        // Approve() refuses a request that is already decided, so a second approver racing the
        // first is told it has been handled rather than overwriting the decision.
        absence.Approve(_currentUser.UserId);

        if (absence.ConsumesEntitlement)
        {
            var entitlement = await _absences.GetEntitlementAsync(
                absence.UserId, absence.StartDate.Year, ct)
                ?? throw new InvalidOperationException(
                    $"No vacation entitlement is set for {absence.StartDate.Year}.");

            // Refuses to overdraw, so approving cannot grant days the employee does not have.
            entitlement.Consume(absence.ConsumedDays);
        }

        await _absences.SaveChangesAsync(ct);
    }
}
