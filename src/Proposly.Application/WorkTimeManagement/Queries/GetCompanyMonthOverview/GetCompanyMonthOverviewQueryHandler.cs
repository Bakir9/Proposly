using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetCompanyMonthOverview;

public sealed class GetCompanyMonthOverviewQueryHandler
    : IQueryHandler<GetCompanyMonthOverviewQuery, IReadOnlyList<CompanyMonthOverviewResponse>>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IEmploymentTermsRepository _terms;
    private readonly INonWorkingDayRepository _calendar;
    private readonly IAbsenceRepository _absences;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IUserRepository _users;

    public GetCompanyMonthOverviewQueryHandler(
        ITimesheetRepository timesheets,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        IAbsenceRepository absences,
        IWorkTimePolicyRepository policies,
        IUserRepository users)
    {
        _timesheets = timesheets;
        _terms = terms;
        _calendar = calendar;
        _absences = absences;
        _policies = policies;
        _users = users;
    }

    public async Task<IReadOnlyList<CompanyMonthOverviewResponse>> HandleAsync(
        GetCompanyMonthOverviewQuery query, CancellationToken ct = default)
    {
        var sheets = await _timesheets.GetForMonthAsync(query.Year, query.Month, ct);
        if (sheets.Count == 0) return [];

        var names = await _users.ResolveNamesAsync(ct);
        var rows = new List<CompanyMonthOverviewResponse>(sheets.Count);

        foreach (var sheet in sheets)
        {
            var figures = await MonthEndFigures.ResolveAsync(
                sheet, _terms, _calendar, _absences, _policies, _timesheets, ct);

            var breaches = await TimesheetBreachEvaluation.ResolveAsync(
                sheet, _policies, _timesheets, _absences, ct);

            rows.Add(new CompanyMonthOverviewResponse(
                sheet.UserId,
                names.GetValueOrDefault(sheet.UserId, "Unknown"),
                sheet.Status,
                figures.TargetHours,
                figures.ActualHours,
                figures.TargetHours.HasValue ? figures.Balance.MonthlyDifference : null,
                figures.Balance.ClosingBalance,
                figures.Balance.ForfeitedHours,
                breaches.Count,
                figures.IsProvisional,
                sheet.IsRevised));
        }

        return rows.OrderBy(r => r.EmployeeName).ToList();
    }
}
