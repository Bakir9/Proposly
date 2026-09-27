using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetReconciliation;

public sealed class GetReconciliationQueryHandler
    : IQueryHandler<GetReconciliationQuery, ReconciliationResponse?>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IProjectBookingReader _bookings;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;

    public GetReconciliationQueryHandler(
        ITimesheetRepository timesheets,
        IProjectBookingReader bookings,
        IUserRepository users,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _bookings = bookings;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<ReconciliationResponse?> HandleAsync(
        GetReconciliationQuery query, CancellationToken ct = default)
    {
        var userId = query.UserId ?? _currentUser.UserId;

        var timesheet = await _timesheets.GetForUserAsync(userId, query.Year, query.Month, ct);
        if (timesheet is null) return null;

        var monthStart = new DateOnly(query.Year, query.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var bookings = await _bookings.GetBookedHoursAsync(userId, monthStart, monthEnd, ct);

        var recorded = timesheet.TotalWorkedHours;
        var booked = bookings.ByProject.Sum(p => p.BookedHours);

        var employee = await _users.GetByIdAsync(userId, ct);

        return new ReconciliationResponse(
            UserId: userId,
            EmployeeName: employee?.FullName ?? "Unknown",
            Year: query.Year,
            Month: query.Month,
            RecordedWorkingHours: recorded,
            ProjectBookedHours: booked,

            // Work that happened but was never costed against a project. Never negative — an
            // over-booked month is reported through the flag instead.
            UnbookedHours: Math.Max(0m, Round(recorded - booked)),

            // Booked exceeding recorded is surfaced for review, not treated as an error: it
            // usually means a day was booked to a project but never recorded as working time.
            OverBooked: booked > recorded,

            ByProject: bookings.ByProject
                .Select(p => new ProjectBookingResponse(p.ProjectId, p.ProjectName, p.BookedHours))
                .ToList(),

            UnattributedBookedHours: bookings.UnattributedHours);
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
