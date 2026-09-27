using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Queries.GetTargetHours;

public sealed class GetTargetHoursQueryHandler
    : IQueryHandler<GetTargetHoursQuery, TargetHoursResponse>
{
    private readonly IEmploymentTermsRepository _terms;
    private readonly INonWorkingDayRepository _calendar;
    private readonly IAbsenceRepository _absences;
    private readonly ICurrentUserService _currentUser;

    public GetTargetHoursQueryHandler(
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        IAbsenceRepository absences,
        ICurrentUserService currentUser)
    {
        _terms = terms;
        _calendar = calendar;
        _absences = absences;
        _currentUser = currentUser;
    }

    public async Task<TargetHoursResponse> HandleAsync(
        GetTargetHoursQuery query, CancellationToken ct = default)
    {
        var userId = query.UserId ?? _currentUser.UserId;

        var monthStart = new DateOnly(query.Year, query.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // Every version touching the month, so a contract change mid-month is resolved per day
        // rather than by whichever version happened to start it.
        var termsVersions = await _terms.GetForRangeAsync(userId, monthStart, monthEnd, ct);
        var calendar = await _calendar.GetForRangeAsync(monthStart, monthEnd, ct);
        var approvedAbsences = await _absences.GetApprovedInRangeAsync(
            userId, monthStart, monthEnd, ct);

        var workingDays = WorkingDayCalculator.ExpectedWorkingDays(
            query.Year, query.Month, termsVersions, calendar);

        var targetHours = WorkingDayCalculator.TargetHoursForMonth(
            query.Year, query.Month, termsVersions, calendar, approvedAbsences);

        // Only the days that actually cost nothing are reported as excluded — a closure day the
        // employee has to cover with entitlement is still a working day.
        var excluded = calendar.Count(d => !d.ConsumesVacation);

        return new TargetHoursResponse(
            userId, query.Year, query.Month, workingDays, excluded, targetHours);
    }
}
