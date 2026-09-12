using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveAbsence;

public sealed class ApproveAbsenceCommandHandler : ICommandHandler<ApproveAbsenceCommand>
{
    private readonly IAbsenceRepository _absences;
    private readonly ITimesheetRepository _timesheets;
    private readonly ICurrentUserService _currentUser;

    public ApproveAbsenceCommandHandler(
        IAbsenceRepository absences,
        ITimesheetRepository timesheets,
        ICurrentUserService currentUser)
    {
        _absences = absences;
        _timesheets = timesheets;
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

        // Approving leave over a month already reported changes that month's target hours. The
        // reported figures stand; the month is flagged for a human to look at.
        await RevisedMonthMarker.MarkAffectedMonthsAsync(
            absence.UserId, absence.StartDate, absence.EndDate, _timesheets, ct);

        await _absences.SaveChangesAsync(ct);
    }
}
