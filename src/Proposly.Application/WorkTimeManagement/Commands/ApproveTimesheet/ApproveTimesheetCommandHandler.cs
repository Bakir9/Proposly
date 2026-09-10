using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveTimesheet;

public sealed class ApproveTimesheetCommandHandler : ICommandHandler<ApproveTimesheetCommand>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IAbsenceRepository _absences;
    private readonly ICurrentUserService _currentUser;

    public ApproveTimesheetCommandHandler(
        ITimesheetRepository timesheets,
        IWorkTimePolicyRepository policies,
        IAbsenceRepository absences,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _policies = policies;
        _absences = absences;
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

        await _timesheets.UpdateAsync(timesheet, ct);
    }
}
