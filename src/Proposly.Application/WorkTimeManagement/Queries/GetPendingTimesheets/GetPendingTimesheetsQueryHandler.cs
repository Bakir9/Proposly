using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetPendingTimesheets;

public sealed class GetPendingTimesheetsQueryHandler
    : IQueryHandler<GetPendingTimesheetsQuery, IReadOnlyList<TimesheetSummaryResponse>>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IAbsenceRepository _absences;
    private readonly IUserRepository _users;

    public GetPendingTimesheetsQueryHandler(
        ITimesheetRepository timesheets,
        IWorkTimePolicyRepository policies,
        IAbsenceRepository absences,
        IUserRepository users)
    {
        _timesheets = timesheets;
        _policies = policies;
        _absences = absences;
        _users = users;
    }

    public async Task<IReadOnlyList<TimesheetSummaryResponse>> HandleAsync(
        GetPendingTimesheetsQuery query, CancellationToken ct = default)
    {
        // The query filter already limits this to the caller's company, and to their own rows if
        // they are not an approver — so no role branch is needed here.
        var submitted = await _timesheets.GetSubmittedAsync(ct);
        if (submitted.Count == 0) return [];

        var names = await _users.ResolveNamesAsync(ct);
        var summaries = new List<TimesheetSummaryResponse>(submitted.Count);

        foreach (var sheet in submitted)
        {
            // The count is shown before the approver opens a month, so they know up front that
            // approving it will require acknowledging breaches.
            var breaches = await TimesheetBreachEvaluation.ResolveAsync(
                sheet, _policies, _timesheets, _absences, ct);

            summaries.Add(sheet.ToSummary(
                names.GetValueOrDefault(sheet.UserId, "Unknown"), breaches.Count));
        }

        return summaries.OrderBy(t => t.EmployeeName).ToList();
    }
}
