using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Responses;

public sealed record AbsenceDaysByType(AbsenceType Type, decimal Days);

/// <summary>
/// One employee's month end to end.
/// <para>
/// For an Approved or Locked month every figure comes from the snapshot frozen at approval. For an
/// open month the figures are computed live and <see cref="IsProvisional"/> is true.
/// </para>
/// </summary>
public sealed record MonthlyWorkTimeReportResponse(
    Guid UserId,
    string EmployeeName,
    int Year,
    int Month,
    TimesheetStatus Status,
    bool IsProvisional,
    bool IsRevised,
    bool IsSelfApproved,

    /// <summary>Null when no employment terms cover the month — unavailable, not zero.</summary>
    decimal? TargetHours,
    decimal ActualHours,
    decimal? MonthlyDifference,

    IReadOnlyList<AbsenceDaysByType> AbsenceDays,

    decimal OpeningBalanceHours,
    decimal ClosingBalanceHours,
    /// <summary>Surplus lost to the cap. Always stated so nothing disappears quietly.</summary>
    decimal ForfeitedHours,
    decimal? SurplusCapHours,
    decimal? DeficitFloorHours,
    bool DeficitFloorBreached,
    bool ApproachingCap,

    // --- How the contract compensated this month's overtime ---

    /// <summary>Salary covers additional hours, so surplus is reported but not banked.</summary>
    bool IsAllIn,
    /// <summary>Überstundenpauschale in hours per month, or null when there is none.</summary>
    decimal? OvertimeLumpSumHours,
    /// <summary>Surplus absorbed by the lump sum — already paid.</summary>
    decimal AbsorbedByLumpSumHours,
    /// <summary>Surplus covered by the all-in agreement.</summary>
    decimal CoveredByAllInHours,
    /// <summary>What actually reached the balance, after compensation.</summary>
    decimal CarriedForwardHours,

    IReadOnlyList<BreachResponse> Breaches,
    IReadOnlyList<WorkDayResponse> Days);

/// <summary>A row of the company-wide month overview.</summary>
public sealed record CompanyMonthOverviewResponse(
    Guid UserId,
    string EmployeeName,
    TimesheetStatus Status,
    decimal? TargetHours,
    decimal ActualHours,
    decimal? MonthlyDifference,
    decimal ClosingBalanceHours,
    decimal ForfeitedHours,
    int BreachCount,
    bool IsProvisional,
    bool IsRevised);

/// <summary>
/// Recorded working hours against hours booked to projects. Read-only: nothing here writes to a
/// timesheet or a project time log.
/// </summary>
public sealed record ProjectBookingResponse(Guid ProjectId, string ProjectName, decimal BookedHours);

public sealed record ReconciliationResponse(
    Guid UserId,
    string EmployeeName,
    int Year,
    int Month,
    decimal RecordedWorkingHours,
    decimal ProjectBookedHours,
    /// <summary>Recorded but never booked to a project — work that was never costed.</summary>
    decimal UnbookedHours,
    /// <summary>Booked exceeds recorded. Surfaced for review rather than treated as an error.</summary>
    bool OverBooked,
    IReadOnlyList<ProjectBookingResponse> ByProject,
    /// <summary>Bookings whose project membership no longer resolves to a user.</summary>
    decimal UnattributedBookedHours);
