using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Responses;
using Proposly.Application.Dashboard.Services;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Dashboard.Queries.GetWhosOut;

/// <summary>
/// "Who's out" for the dashboard. Everyone sees that a colleague is absent and the date range;
/// only viewers with the admin view (Owner/Admin) see the absence type — everyone else gets
/// "Absent", and sick-leave details are redacted server-side (spec §6, DSGVO). Public holidays
/// and non-working weekdays never count anyone as out.
/// </summary>
public sealed class GetWhosOutQueryHandler(
    IUserRepository users,
    IWhosOutReader reader,
    INonWorkingDayRepository nonWorkingDays,
    ICurrentUserService currentUser) : IQueryHandler<GetWhosOutQuery, WhosOutResponse>
{
    private const int StripDays = 14;
    private const int MonthRangeDays = 30;

    public async Task<WhosOutResponse> HandleAsync(GetWhosOutQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var rangeEnd = query.Range switch
        {
            "week" => today.AddDays(7 - (int)(today.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)today.DayOfWeek)),
            "month" => today.AddDays(MonthRangeDays),
            _ => today,
        };
        var horizon = today.AddDays(Math.Max(MonthRangeDays, StripDays));

        var employees = (await users.GetActiveAsync(ct)).Where(u => u.Role != UserRole.SuperAdmin).ToList();
        var patterns = (await reader.GetWorkPatternsAsync(currentUser.CompanyId, ct)).ToDictionary(p => p.UserId, p => p.WorkingDays);
        var holidays = (await nonWorkingDays.GetForRangeAsync(today, horizon, ct)).Select(h => h.Date).ToHashSet();
        var absences = await reader.GetApprovedAbsencesAsync(currentUser.CompanyId, today, horizon, ct);

        // Without terms, assume the default full-time week so new accounts are not invisible.
        bool WorksOn(Guid userId, DateOnly date) =>
            patterns.TryGetValue(userId, out var days)
                ? days.Includes(date)
                : date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

        bool IsCompanyOff(DateOnly date) =>
            date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.Contains(date);

        HashSet<Guid> OutOn(DateOnly date) =>
            IsCompanyOff(date)
                ? []
                : absences
                    .Where(a => a.StartDate <= date && a.EndDate >= date && WorksOn(a.UserId, date) && !holidays.Contains(date))
                    .Select(a => a.UserId)
                    .ToHashSet();

        var outToday = OutOn(today);
        var scheduledToday = employees.Where(e => WorksOn(e.Id, today) && !IsCompanyOff(today)).ToList();
        var inToday = scheduledToday.Count(e => !outToday.Contains(e.Id));
        var notScheduled = employees.Count - scheduledToday.Count;

        var perDay = Enumerable.Range(0, StripDays)
            .Select(i =>
            {
                var d = today.AddDays(i);
                return new WhosOutDay(d, OutOn(d).Count, IsCompanyOff(d));
            })
            .ToList();

        var canSeeTypes = currentUser.CanViewAllEmployees;
        var names = employees.ToDictionary(e => e.Id, e => e.FullName);

        DateOnly BackOn(Guid userId, DateOnly end)
        {
            var d = end.AddDays(1);
            for (var i = 0; i < 30 && (!WorksOn(userId, d) || holidays.Contains(d)); i++) d = d.AddDays(1);
            return d;
        }

        var people = absences
            .Where(a => a.EndDate >= today && a.StartDate <= rangeEnd && names.ContainsKey(a.UserId))
            .OrderBy(a => a.StartDate)
            .ThenBy(a => names[a.UserId])
            .Select(a => new WhosOutPerson(
                a.UserId,
                names[a.UserId],
                canSeeTypes || a.UserId == currentUser.UserId ? TypeLabel(a.Type) : "Absent",
                a.StartDate,
                a.EndDate,
                BackOn(a.UserId, a.EndDate)))
            .ToList();

        return new WhosOutResponse(
            TeamTotal: employees.Count,
            InToday: inToday,
            OutToday: outToday.Count,
            NotScheduledToday: notScheduled,
            CanSeeTypes: canSeeTypes,
            PerDay: perDay,
            People: people);
    }

    private static string TypeLabel(AbsenceType type) => type switch
    {
        AbsenceType.Vacation => "Vacation",
        AbsenceType.SickLeave => "Sick",
        AbsenceType.UnpaidLeave => "Unpaid leave",
        AbsenceType.ParentalLeave => "Parental leave",
        AbsenceType.SpecialLeave => "Special leave",
        _ => "Absent",
    };
}
