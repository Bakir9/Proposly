using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Responses;

/// <summary>
/// One absence. Carries the type and dates only — no diagnosis or medical detail exists to expose.
/// </summary>
public sealed record AbsenceResponse(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool FirstDayIsHalf,
    bool LastDayIsHalf,
    decimal ConsumedDays,
    AbsenceStatus Status,
    string? Reason,
    string? ApproverName,
    DateTime? DecidedAt,
    string? DecisionReason);

public sealed record AbsenceEntitlementResponse(
    Guid UserId,
    int Year,
    decimal EntitledDays,
    decimal CarriedOverDays,
    decimal UsedDays,
    decimal RemainingDays);

/// <summary>
/// What a prospective request would cost, so the form can show it before anything is submitted.
/// </summary>
public sealed record AbsencePreviewResponse(
    decimal ConsumedDays,
    decimal? RemainingDaysAfter,
    bool ExceedsEntitlement,
    bool OverlapsExisting,
    IReadOnlyList<DateOnly> NonWorkingDatesExcluded);

public static class AbsenceMappingExtensions
{
    public static AbsenceResponse ToResponse(
        this AbsenceRequest absence, string employeeName, string? approverName = null)
        => new(
            absence.Id,
            absence.UserId,
            employeeName,
            absence.Type,
            absence.StartDate,
            absence.EndDate,
            absence.FirstDayIsHalf,
            absence.LastDayIsHalf,
            absence.ConsumedDays,
            absence.Status,
            absence.Reason,
            approverName,
            absence.DecidedAt,
            absence.DecisionReason);

    public static AbsenceEntitlementResponse ToResponse(this AbsenceEntitlement entitlement)
        => new(
            entitlement.UserId,
            entitlement.Year,
            entitlement.EntitledDays,
            entitlement.CarriedOverDays,
            entitlement.UsedDays,
            entitlement.RemainingDays);
}
