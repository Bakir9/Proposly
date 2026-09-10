using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Responses;

/// <summary>One recorded working day.</summary>
public sealed record WorkDayResponse(
    Guid Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    bool CrossesMidnight,
    decimal WorkedHours,
    string? Note);

/// <summary>
/// One employee's month in full. <paramref name="Id"/> is null when nothing has been recorded yet:
/// the month is presented as an empty Draft and is only persisted once a day is entered.
/// </summary>
public sealed record TimesheetDetailResponse(
    Guid? Id,
    Guid UserId,
    string EmployeeName,
    int Year,
    int Month,
    TimesheetStatus Status,
    decimal TotalWorkedHours,
    bool IsSelfApproved,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    string? ApprovedByName,
    DateTime? ReopenedAt,
    IReadOnlyList<WorkDayResponse> Days,
    /// <summary>
    /// Computed live while the month is open; the set acknowledged at approval once it is closed.
    /// Empty when the company has no working time policy configured.
    /// </summary>
    IReadOnlyList<BreachResponse> Breaches);

/// <summary>Row shape for the approver queue and the company month overview.</summary>
public sealed record TimesheetSummaryResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    int Year,
    int Month,
    TimesheetStatus Status,
    decimal TotalWorkedHours,
    bool IsSelfApproved,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    int BreachCount);
