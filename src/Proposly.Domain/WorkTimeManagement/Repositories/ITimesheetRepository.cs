using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Domain.WorkTimeManagement.Repositories;

/// <summary>
/// Per-employee scoping is applied by the AppDbContext query filter, not here. No method on this
/// interface may bypass query filters: IgnoreQueryFilters() would drop company isolation too.
/// </summary>
public interface ITimesheetRepository
{
    /// <summary>The timesheet for one employee and month, or null. Days are included.</summary>
    Task<Timesheet?> GetForUserAsync(Guid userId, int year, int month, CancellationToken ct = default);

    Task<Timesheet?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Every timesheet for a month. Returns only the caller's own unless they may view all
    /// employees — the filter decides, so no role check is needed here.
    /// </summary>
    Task<IReadOnlyList<Timesheet>> GetForMonthAsync(int year, int month, CancellationToken ct = default);

    /// <summary>Submitted timesheets awaiting a decision.</summary>
    Task<IReadOnlyList<Timesheet>> GetSubmittedAsync(CancellationToken ct = default);

    /// <summary>
    /// One employee's recorded days across a date range, regardless of which month's timesheet
    /// holds them. Compliance checks reach backwards for two reasons: the rest period between the
    /// last day of one month and the first of the next, and the rolling averaging window.
    /// </summary>
    Task<IReadOnlyList<WorkDayEntry>> GetDaysInRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task AddAsync(Timesheet timesheet, CancellationToken ct = default);
    Task UpdateAsync(Timesheet timesheet, CancellationToken ct = default);
}
