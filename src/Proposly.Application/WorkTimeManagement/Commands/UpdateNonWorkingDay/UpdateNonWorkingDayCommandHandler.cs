using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.UpdateNonWorkingDay;

public sealed class UpdateNonWorkingDayCommandHandler : ICommandHandler<UpdateNonWorkingDayCommand>
{
    private readonly INonWorkingDayRepository _calendar;
    private readonly ITimesheetRepository _timesheets;

    public UpdateNonWorkingDayCommandHandler(
        INonWorkingDayRepository calendar,
        ITimesheetRepository timesheets)
    {
        _calendar = calendar;
        _timesheets = timesheets;
    }

    public async Task HandleAsync(UpdateNonWorkingDayCommand command, CancellationToken ct = default)
    {
        var day = await _calendar.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Non-working day {command.Id} not found.");

        // Changing whether a day costs the employee anything moves target hours and absence
        // counts for that month, so it cannot be done behind an approval.
        await ClosedMonthGuard.EnsureMonthIsOpenAsync(day.Date, _timesheets, ct);

        day.Update(command.Name, command.Kind, command.ConsumesVacation);

        await _calendar.SaveChangesAsync(ct);
    }
}
