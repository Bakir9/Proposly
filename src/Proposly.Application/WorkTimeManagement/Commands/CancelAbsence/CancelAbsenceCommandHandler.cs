using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.CancelAbsence;

public sealed class CancelAbsenceCommandHandler : ICommandHandler<CancelAbsenceCommand>
{
    private readonly IAbsenceRepository _absences;
    private readonly ITimesheetRepository _timesheets;
    private readonly TimeProvider _clock;

    public CancelAbsenceCommandHandler(
        IAbsenceRepository absences,
        ITimesheetRepository timesheets,
        TimeProvider clock)
    {
        _absences = absences;
        _timesheets = timesheets;
        _clock = clock;
    }

    public async Task HandleAsync(CancelAbsenceCommand command, CancellationToken ct = default)
    {
        var absence = await _absences.GetByIdAsync(command.AbsenceId, ct)
            ?? throw new InvalidOperationException($"Absence {command.AbsenceId} not found.");

        var wasApproved = absence.Status == AbsenceStatus.Approved;
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        // Refuses an absence already under way — that is a fact, not a plan.
        absence.Cancel(today);

        // Only an approved vacation had drawn down entitlement, so only that needs returning.
        if (wasApproved && absence.ConsumesEntitlement)
        {
            var entitlement = await _absences.GetEntitlementAsync(
                absence.UserId, absence.StartDate.Year, ct);

            entitlement?.Release(absence.ConsumedDays);
        }

        // Withdrawing approved leave raises the target hours of any month it covered.
        if (wasApproved)
        {
            await RevisedMonthMarker.MarkAffectedMonthsAsync(
                absence.UserId, absence.StartDate, absence.EndDate, _timesheets, ct);
        }

        await _absences.SaveChangesAsync(ct);
    }
}
