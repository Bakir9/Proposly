using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Services;

/// <summary>
/// Flags already-reported months whose inputs have changed.
/// <para>
/// Approving an absence retroactively, or cancelling one, alters the target hours of any month it
/// touches. Where such a month has already been approved, its figures are left exactly as
/// reported and the month is marked revised instead — reopening it is an approver's decision, not
/// a silent recalculation behind their back.
/// </para>
/// </summary>
internal static class RevisedMonthMarker
{
    public static async Task MarkAffectedMonthsAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        ITimesheetRepository timesheets,
        CancellationToken ct = default)
    {
        var month = new DateOnly(from.Year, from.Month, 1);
        var lastMonth = new DateOnly(to.Year, to.Month, 1);

        while (month <= lastMonth)
        {
            var timesheet = await timesheets.GetForUserAsync(userId, month.Year, month.Month, ct);

            // MarkRevised is a no-op on an open month, which is the common case.
            timesheet?.MarkRevised();

            month = month.AddMonths(1);
        }
    }
}
