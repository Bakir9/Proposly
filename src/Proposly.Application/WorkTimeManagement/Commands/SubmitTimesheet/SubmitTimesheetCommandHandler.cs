using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.SubmitTimesheet;

public sealed class SubmitTimesheetCommandHandler : ICommandHandler<SubmitTimesheetCommand>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly ICurrentUserService _currentUser;

    public SubmitTimesheetCommandHandler(
        ITimesheetRepository timesheets,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _currentUser = currentUser;
    }

    // No plan gate: submitting advances an existing record rather than creating one, so a company
    // that has dropped below Pro can still close out the months it already recorded.
    public async Task HandleAsync(SubmitTimesheetCommand command, CancellationToken ct = default)
    {
        var timesheet = await _timesheets.GetForUserAsync(
            _currentUser.UserId, command.Year, command.Month, ct)
            ?? throw new InvalidOperationException(
                $"No timesheet recorded for {command.Year}-{command.Month:00}.");

        timesheet.Submit();

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
