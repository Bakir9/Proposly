using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.Dashboard.Services;

/// <summary>
/// Company-wide presence data for the dashboard's "Who's out" card. Absences and employment
/// terms are user-owned entities, so the global query filter would hide colleagues' rows from
/// regular members — but every employee may see that a colleague is absent (never why; the
/// handler redacts the type for non-privileged viewers, and the reason is never read at all).
/// The implementation re-applies the company filter explicitly and selects only the narrow
/// fields below — no reasons, notes, rates or balances.
/// </summary>
public interface IWhosOutReader
{
    /// <summary>Approved absences overlapping the range, for the whole company.</summary>
    Task<IReadOnlyList<AbsenceSlice>> GetApprovedAbsencesAsync(Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Latest working-day pattern per employee (no money fields).</summary>
    Task<IReadOnlyList<WorkPatternSlice>> GetWorkPatternsAsync(Guid companyId, CancellationToken ct = default);
}

public sealed record AbsenceSlice(
    Guid UserId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record WorkPatternSlice(
    Guid UserId,
    WeekDays WorkingDays);
