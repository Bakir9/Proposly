using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.DeleteNonWorkingDay;

public sealed class DeleteNonWorkingDayCommandHandler : ICommandHandler<DeleteNonWorkingDayCommand>
{
    private readonly INonWorkingDayRepository _calendar;
    private readonly IAbsenceRepository _absences;
    private readonly ITimesheetRepository _timesheets;

    public DeleteNonWorkingDayCommandHandler(
        INonWorkingDayRepository calendar,
        IAbsenceRepository absences,
        ITimesheetRepository timesheets)
    {
        _calendar = calendar;
        _absences = absences;
        _timesheets = timesheets;
    }

    public async Task HandleAsync(DeleteNonWorkingDayCommand command, CancellationToken ct = default)
    {
        var day = await _calendar.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Non-working day {command.Id} not found.");

        await ClosedMonthGuard.EnsureMonthIsOpenAsync(day.Date, _timesheets, ct);

        // Removing a holiday makes the day chargeable again, so any absence already approved over
        // it is now short by a day. Rather than silently adjusting someone's entitlement, the
        // deletion is refused and the administrator is pointed at the affected requests.
        var affected = await _absences.GetApprovedCoveringDateAsync(day.Date, ct);

        if (affected.Count > 0)
            throw new InvalidOperationException(
                $"{affected.Count} approved absence(s) cover {day.Date:yyyy-MM-dd}. " +
                "Cancel or re-approve them before removing this day, so the consumed days stay " +
                "correct.");

        await _calendar.RemoveAsync(day, ct);
    }
}
