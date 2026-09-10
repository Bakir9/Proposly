using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.UpsertWorkDay;

public sealed class UpsertWorkDayCommandHandler : ICommandHandler<UpsertWorkDayCommand>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly ICompanyRepository _companies;
    private readonly ICurrentUserService _currentUser;

    public UpsertWorkDayCommandHandler(
        ITimesheetRepository timesheets,
        ICompanyRepository companies,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _companies = companies;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(UpsertWorkDayCommand command, CancellationToken ct = default)
    {
        await WorkTimeModuleGuard.EnsureEnabledAsync(_companies, _currentUser.CompanyId, ct);

        var timesheet = await _timesheets.GetForUserAsync(
            _currentUser.UserId, command.Year, command.Month, ct);

        // The month is persisted on first entry rather than on first read, so opening a month you
        // never fill in leaves no row behind.
        if (timesheet is null)
        {
            timesheet = Timesheet.Create(
                _currentUser.CompanyId, _currentUser.UserId, command.Year, command.Month);

            timesheet.AddOrUpdateDay(
                command.Date, command.StartTime, command.EndTime,
                command.BreakMinutes, command.CrossesMidnight, command.Note);

            await _timesheets.AddAsync(timesheet, ct);
            return;
        }

        timesheet.AddOrUpdateDay(
            command.Date, command.StartTime, command.EndTime,
            command.BreakMinutes, command.CrossesMidnight, command.Note);

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
