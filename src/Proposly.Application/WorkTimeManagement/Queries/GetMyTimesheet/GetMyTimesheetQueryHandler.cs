using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetMyTimesheet;

public sealed class GetMyTimesheetQueryHandler
    : IQueryHandler<GetMyTimesheetQuery, TimesheetDetailResponse?>
{
    private readonly ITimesheetRepository _timesheets;
    private readonly IWorkTimePolicyRepository _policies;
    private readonly IAbsenceRepository _absences;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;

    public GetMyTimesheetQueryHandler(
        ITimesheetRepository timesheets,
        IWorkTimePolicyRepository policies,
        IAbsenceRepository absences,
        IUserRepository users,
        ICurrentUserService currentUser)
    {
        _timesheets = timesheets;
        _policies = policies;
        _absences = absences;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<TimesheetDetailResponse?> HandleAsync(
        GetMyTimesheetQuery query, CancellationToken ct = default)
    {
        var me = await _users.GetByIdAsync(_currentUser.UserId, ct);
        var employeeName = me?.FullName ?? "Unknown";

        var timesheet = await _timesheets.GetForUserAsync(
            _currentUser.UserId, query.Year, query.Month, ct);

        // A month nobody has touched is presented as an empty Draft with no id. Reading a month
        // deliberately does not persist it — the row appears when the first day is recorded, so
        // browsing months leaves nothing behind.
        if (timesheet is null)
        {
            return new TimesheetDetailResponse(
                Id: null,
                UserId: _currentUser.UserId,
                EmployeeName: employeeName,
                Year: query.Year,
                Month: query.Month,
                Status: TimesheetStatus.Draft,
                TotalWorkedHours: 0m,
                IsSelfApproved: false,
                SubmittedAt: null,
                ApprovedAt: null,
                ApprovedByName: null,
                ReopenedAt: null,
                Days: [],
                Breaches: []);
        }

        string? approvedByName = null;
        if (timesheet.ApprovedById.HasValue)
        {
            var approver = await _users.GetByIdAsync(timesheet.ApprovedById.Value, ct);
            approvedByName = approver?.FullName;
        }

        var breaches = await TimesheetBreachEvaluation.ResolveAsync(
            timesheet, _policies, _timesheets, _absences, ct);

        return timesheet.ToDetail(employeeName, approvedByName, breaches);
    }
}
