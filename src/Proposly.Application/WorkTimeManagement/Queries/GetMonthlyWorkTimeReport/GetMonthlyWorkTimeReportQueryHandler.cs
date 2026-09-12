using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetMonthlyWorkTimeReport;

public sealed class GetMonthlyWorkTimeReportQueryHandler
    : IQueryHandler<GetMonthlyWorkTimeReportQuery, MonthlyWorkTimeReportResponse?>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IEmploymentTermsRepository _terms;
    private readonly INonWorkingDayRepository _calendar;
    private readonly IAbsenceRepository _absences;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;

    public GetMonthlyWorkTimeReportQueryHandler(
        ITimesheetRepository timesheets,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        IAbsenceRepository absences,
        IWorkTimePolicyRepository policies,
        IUserRepository users,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _terms = terms;
        _calendar = calendar;
        _absences = absences;
        _policies = policies;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<MonthlyWorkTimeReportResponse?> HandleAsync(
        GetMonthlyWorkTimeReportQuery query, CancellationToken ct = default)
    {
        var userId = query.UserId ?? _currentUser.UserId;

        var timesheet = await _timesheets.GetForUserAsync(userId, query.Year, query.Month, ct);
        if (timesheet is null) return null;

        var figures = await MonthEndFigures.ResolveAsync(
            timesheet, _terms, _calendar, _absences, _policies, _timesheets, ct);

        var breaches = await TimesheetBreachEvaluation.ResolveAsync(
            timesheet, _policies, _timesheets, _absences, ct);

        var employee = await _users.GetByIdAsync(userId, ct);

        return new MonthlyWorkTimeReportResponse(
            UserId: userId,
            EmployeeName: employee?.FullName ?? "Unknown",
            Year: query.Year,
            Month: query.Month,
            Status: timesheet.Status,
            IsProvisional: figures.IsProvisional,
            IsRevised: timesheet.IsRevised,
            IsSelfApproved: timesheet.IsSelfApproved,

            TargetHours: figures.TargetHours,
            ActualHours: figures.ActualHours,
            // Null target means there is nothing to measure against, so no difference is claimed.
            MonthlyDifference: figures.TargetHours.HasValue
                ? figures.Balance.MonthlyDifference
                : null,

            AbsenceDays: figures.AbsenceDays
                .Select(a => new AbsenceDaysByType(a.Type, a.Days))
                .ToList(),

            OpeningBalanceHours: figures.Balance.OpeningBalance,
            ClosingBalanceHours: figures.Balance.ClosingBalance,
            ForfeitedHours: figures.Balance.ForfeitedHours,
            SurplusCapHours: figures.SurplusCapHours,
            DeficitFloorHours: figures.DeficitFloorHours,
            DeficitFloorBreached: figures.Balance.DeficitFloorBreached,
            ApproachingCap: figures.Balance.ApproachingCap(figures.SurplusCapHours),

            Breaches: breaches.Select(b => b.ToResponse()).ToList(),
            Days: timesheet.Days
                .OrderBy(d => d.Date)
                .Select(d => new WorkDayResponse(
                    d.Id, d.Date, d.StartTime, d.EndTime,
                    d.BreakMinutes, d.CrossesMidnight, d.WorkedHours, d.Note))
                .ToList());
    }
}
