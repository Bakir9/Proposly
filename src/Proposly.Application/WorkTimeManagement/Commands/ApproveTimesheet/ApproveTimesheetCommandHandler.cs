using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveTimesheet;

public sealed class ApproveTimesheetCommandHandler : ICommandHandler<ApproveTimesheetCommand>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IAbsenceRepository _absences;
    private readonly IEmploymentTermsRepository _terms;
    private readonly INonWorkingDayRepository _calendar;
    private readonly ICurrentUserService _currentUser;

    public ApproveTimesheetCommandHandler(
        ITimesheetRepository timesheets,
        IWorkTimePolicyRepository policies,
        IAbsenceRepository absences,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _policies = policies;
        _absences = absences;
        _terms = terms;
        _calendar = calendar;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(ApproveTimesheetCommand command, CancellationToken ct = default)
    {
        // Approvers can read every employee's rows through the query filter, so a plain lookup is
        // enough — no IgnoreQueryFilters(), which would also drop company isolation.
        var timesheet = await _timesheets.GetByIdAsync(command.TimesheetId, ct)
            ?? throw new InvalidOperationException($"Timesheet {command.TimesheetId} not found.");

        // Evaluated server-side rather than trusted from the client: the approver acknowledges
        // whatever the month actually breaches, not a list the browser sent back.
        var breaches = await TimesheetBreachEvaluation.EvaluateAsync(
            timesheet, _policies, _timesheets, _absences, ct);

        timesheet.Approve(_currentUser.UserId, breaches, command.AcknowledgeBreaches);

        // Approval is the moment the month's figures stop being a live calculation and become a
        // reported fact. Computed from current data once, then frozen.
        var figures = await MonthEndFigures.ComputeAsync(
            timesheet, _terms, _calendar, _absences, _policies, _timesheets, ct);

        timesheet.ApplySnapshot(
            figures.TargetHours,
            figures.ActualHours,
            figures.Balance.OpeningBalance,
            figures.Balance.ClosingBalance,
            figures.Balance.ForfeitedHours,
            figures.Balance.AbsorbedByLumpSumHours,
            figures.Balance.CoveredByAllInHours);

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
