using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetCompanyMonth;

public sealed class GetCompanyMonthQueryHandler
    : IQueryHandler<GetCompanyMonthQuery, IReadOnlyList<TimesheetSummaryResponse>>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IAbsenceRepository _absences;
    private readonly IUserRepository _users;

    public GetCompanyMonthQueryHandler(
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
        GetCompanyMonthQuery query, CancellationToken ct = default)
    {
        var sheets = await _timesheets.GetForMonthAsync(query.Year, query.Month, ct);
        if (sheets.Count == 0) return [];

        var names = await _users.ResolveNamesAsync(ct);
        var summaries = new List<TimesheetSummaryResponse>(sheets.Count);

        foreach (var sheet in sheets)
        {
            var breaches = await TimesheetBreachEvaluation.ResolveAsync(
                sheet, _policies, _timesheets, _absences, ct);

            summaries.Add(sheet.ToSummary(
                names.GetValueOrDefault(sheet.UserId, "Unknown"), breaches.Count));
        }

        return summaries.OrderBy(t => t.EmployeeName).ToList();
    }
}
