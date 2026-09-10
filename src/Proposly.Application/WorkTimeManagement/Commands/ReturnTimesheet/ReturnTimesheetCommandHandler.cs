using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.ReturnTimesheet;

public sealed class ReturnTimesheetCommandHandler : ICommandHandler<ReturnTimesheetCommand>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly ICurrentUserService _currentUser;

    public ReturnTimesheetCommandHandler(
        ITimesheetRepository timesheets,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(ReturnTimesheetCommand command, CancellationToken ct = default)
    {
        var timesheet = await _timesheets.GetByIdAsync(command.TimesheetId, ct)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");

        timesheet.ReturnForCorrection(_currentUser.UserId, command.Reason);

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
