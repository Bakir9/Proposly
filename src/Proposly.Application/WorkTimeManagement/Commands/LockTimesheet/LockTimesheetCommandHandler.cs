using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.LockTimesheet;

public sealed class LockTimesheetCommandHandler : ICommandHandler<LockTimesheetCommand>
{
    private readonly ITimesheetRepository _timesheets;

    public LockTimesheetCommandHandler(ITimesheetRepository timesheets) => _timesheets = timesheets;

    public async Task HandleAsync(LockTimesheetCommand command, CancellationToken ct = default)
    {
        var timesheet = await _timesheets.GetByIdAsync(command.TimesheetId, ct)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");

        timesheet.Lock();

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
