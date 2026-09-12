using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Services;

/// <summary>
/// Loads the employment terms and company calendar a day calculation needs, in one place.
/// <para>
/// Absence counting, target hours, and the preview all have to agree on which days are chargeable;
/// resolving the inputs here means they cannot silently diverge.
/// </para>
/// </summary>
internal static class WorkCalendarContext
{
    /// <summary>
    /// The working-day pattern in force for an employee over a range, and the non-working days
    /// covering it. Falls back to Monday-to-Friday when no terms cover the range, which is what
    /// the module assumed before the calendar existed.
    /// </summary>
    public static async Task<(WeekDays Pattern, IReadOnlyList<NonWorkingDay> Calendar)> LoadAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        CancellationToken ct = default)
    {
        var versions = await terms.GetForRangeAsync(userId, from, to, ct);
        var nonWorkingDays = await calendar.GetForRangeAsync(from, to, ct);

        // A range can straddle a contract change. The pattern from the version covering the start
        // is used for counting; per-day resolution is reserved for target hours, where the
        // difference actually shows up in the figures.
        var pattern = WorkingDayCalculator.TermsOn(versions, from)?.WorkingDays
                      ?? versions.FirstOrDefault()?.WorkingDays
                      ?? WeekDays.MondayToFriday;

        return (pattern, nonWorkingDays);
    }
}
