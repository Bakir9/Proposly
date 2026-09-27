using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Services;

/// <summary>
/// Refuses calendar edits that would change a month somebody has already signed off.
/// <para>
/// Adding or removing a holiday shifts target hours and absence day counts for every employee in
/// that month. Doing that to an approved month would silently rewrite a reported figure, so the
/// edit is refused and the administrator is told to reopen the month instead.
/// </para>
/// </summary>
internal static class ClosedMonthGuard
{
    public static async Task EnsureMonthIsOpenAsync(
        DateOnly date,
        ITimesheetRepository timesheets,
        CancellationToken ct = default)
    {
        var monthSheets = await timesheets.GetForMonthAsync(date.Year, date.Month, ct);

        var closed = monthSheets
            .Where(t => t.Status is TimesheetStatus.Approved or TimesheetStatus.Locked)
            .ToList();

        if (closed.Count == 0) return;

        throw new InvalidOperationException(
            $"{date:yyyy-MM-dd} falls in {date.Year}-{date.Month:00}, which {closed.Count} " +
            $"employee(s) have already had approved. Reopen those months first, or leave the " +
            "calendar as it was when they were signed off.");
    }
}
