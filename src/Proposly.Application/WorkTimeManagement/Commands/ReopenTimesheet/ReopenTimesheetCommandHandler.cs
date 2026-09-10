using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.ReopenTimesheet;

public sealed class ReopenTimesheetCommandHandler : ICommandHandler<ReopenTimesheetCommand>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly ICurrentUserService _currentUser;

    public ReopenTimesheetCommandHandler(
        ITimesheetRepository timesheets,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(ReopenTimesheetCommand command, CancellationToken ct = default)
    {
        var timesheet = await _timesheets.GetByIdAsync(command.TimesheetId, ct)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");

        timesheet.Reopen(_currentUser.UserId);

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
