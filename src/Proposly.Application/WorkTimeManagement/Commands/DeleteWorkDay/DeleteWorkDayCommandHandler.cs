using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.DeleteWorkDay;

public sealed class DeleteWorkDayCommandHandler : ICommandHandler<DeleteWorkDayCommand>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly ICurrentUserService _currentUser;

    public DeleteWorkDayCommandHandler(
        ITimesheetRepository timesheets,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _currentUser = currentUser;
    }

    // No plan gate: this corrects an existing record rather than creating one, so a company that
    // has dropped below Pro can still finish and tidy the months it already has.
    public async Task HandleAsync(DeleteWorkDayCommand command, CancellationToken ct = default)
    {
        var timesheet = await _timesheets.GetForUserAsync(
            _currentUser.UserId, command.Year, command.Month, ct)
            ?? throw new InvalidOperationException(
                $"No timesheet recorded for {command.Year}-{command.Month:00}.");

        timesheet.RemoveDay(command.Date);

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
