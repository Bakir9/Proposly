namespace Proposly.Application.WorkTimeManagement.Services;

public sealed record ProjectBookedHours(Guid ProjectId, string ProjectName, decimal BookedHours);

/// <summary>
/// Hours booked to projects for one employee over a period.
/// <para>
/// <paramref name="UnattributedHours"/> covers bookings whose project membership no longer
/// resolves to a user — reported rather than dropped, so the totals still add up.
/// </para>
/// </summary>
public sealed record ProjectBookingSummary(
    IReadOnlyList<ProjectBookedHours> ByProject,
    decimal UnattributedHours);

/// <summary>
/// A read-only window onto project time logs, for the reconciliation view.
/// <para>
/// Deliberately its own seam rather than a method on IProjectRepository: this module reads project
/// data and must never write it, and ProjectManagement should not grow an interface for another
/// context's benefit.
/// </para>
/// <para>
/// Project time is recorded against a ProjectMember, not a user, so an implementation has to walk
/// TimeEntry → ProjectMember → User, summing across every membership one person holds.
/// </para>
/// </summary>
public interface IProjectBookingReader
{
    Task<ProjectBookingSummary> GetBookedHoursAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
