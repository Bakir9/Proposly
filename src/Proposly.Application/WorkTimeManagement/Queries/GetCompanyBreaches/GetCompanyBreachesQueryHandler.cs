using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetCompanyBreaches;

public sealed class GetCompanyBreachesQueryHandler
    : IQueryHandler<GetCompanyBreachesQuery, IReadOnlyList<BreachOverviewResponse>>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IAbsenceRepository _absences;
    private readonly IUserRepository _users;

    public GetCompanyBreachesQueryHandler(
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

    public async Task<IReadOnlyList<BreachOverviewResponse>> HandleAsync(
        GetCompanyBreachesQuery query, CancellationToken ct = default)
    {
        var sheets = await _timesheets.GetForMonthAsync(query.Year, query.Month, ct);
        if (sheets.Count == 0) return [];

        var names = await _users.ResolveNamesAsync(ct);
        var results = new List<BreachOverviewResponse>();

        foreach (var sheet in sheets)
        {
            var breaches = await TimesheetBreachEvaluation.ResolveAsync(
                sheet, _policies, _timesheets, _absences, ct);

            results.AddRange(breaches.Select(b => new BreachOverviewResponse(
                sheet.UserId,
                names.GetValueOrDefault(sheet.UserId, "Unknown"),
                sheet.Year,
                sheet.Month,
                b.Kind,
                b.Date,
                b.WeekStartDate,
                b.LimitValue,
                b.ActualValue,
                sheet.Status.ToString())));
        }

        return results
            .OrderBy(r => r.EmployeeName)
            .ThenBy(r => r.Date ?? r.WeekStartDate)
            .ToList();
    }
}
