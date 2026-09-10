using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Domain.WorkTimeManagement.Repositories;

/// <summary>
/// Absence requests and entitlements together — they change in the same operation, so keeping
/// them behind one interface avoids a two-repository dance on every approval.
/// <para>
/// Per-employee scoping comes from the AppDbContext query filter. No method here may call
/// IgnoreQueryFilters(): that would drop company isolation along with the user scope.
/// </para>
/// </summary>
public interface IAbsenceRepository
{
    Task<AbsenceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Requests visible to the caller for a year — their own, or the whole company for an
    /// approver. The filter decides, so no role check is needed here.
    /// </summary>
    Task<IReadOnlyList<AbsenceRequest>> GetForYearAsync(
        int year, AbsenceStatus? status = null, CancellationToken ct = default);

    Task<IReadOnlyList<AbsenceRequest>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>
    /// Pending or approved requests for one employee that overlap a range. Used to refuse
    /// double-booking, and by the compliance check to exclude absence days from working limits.
    /// </summary>
    Task<IReadOnlyList<AbsenceRequest>> GetOverlappingAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Approved absences only, for one employee over a range.</summary>
    Task<IReadOnlyList<AbsenceRequest>> GetApprovedInRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task AddAsync(AbsenceRequest request, CancellationToken ct = default);

    Task<AbsenceEntitlement?> GetEntitlementAsync(
        Guid userId, int year, CancellationToken ct = default);

    Task AddEntitlementAsync(AbsenceEntitlement entitlement, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
