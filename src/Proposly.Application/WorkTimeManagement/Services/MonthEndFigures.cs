using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Services;

/// <summary>The month's numbers, however they were arrived at.</summary>
internal readonly record struct MonthFigures(
    decimal? TargetHours,
    decimal ActualHours,
    BalanceResult Balance,
    decimal? SurplusCapHours,
    decimal? DeficitFloorHours,
    IReadOnlyList<AbsenceDays> AbsenceDays,
    bool IsProvisional,
    bool IsAllIn = false,
    decimal? OvertimeLumpSumHours = null);

internal readonly record struct AbsenceDays(AbsenceType Type, decimal Days);

/// <summary>
/// Produces a month's figures either from the snapshot frozen at approval or by computing them
/// live, and is the single place that decides which.
/// <para>
/// Used by the approval path to create the snapshot and by the report to read it, so the numbers
/// an approver signed off are exactly the numbers the report shows.
/// </para>
/// </summary>
internal static class MonthEndFigures
{
    /// <summary>
    /// Snapshot for a closed month, live computation for an open one. An open month is marked
    /// provisional so nobody mistakes a work in progress for a final figure.
    /// </summary>
    public static async Task<MonthFigures> ResolveAsync(
        Timesheet timesheet,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        IAbsenceRepository absences,
        IWorkTimePolicyRepository policies,
        ITimesheetRepository timesheets,
        CancellationToken ct = default)
    {
        var (monthStart, monthEnd) = Bounds(timesheet);

        var policy = await policies.GetEffectiveAsync(monthStart, ct);
        var approvedAbsences = await absences.GetApprovedInRangeAsync(
            timesheet.UserId, monthStart, monthEnd, ct);

        var absenceDays = SummariseAbsence(approvedAbsences, monthStart, monthEnd);

        var isClosed = timesheet.Status is TimesheetStatus.Approved or TimesheetStatus.Locked;

        // The contract terms in force at month end decide how the month's overtime was treated.
        var contract = await ContractForMonthAsync(timesheet, terms, ct);

        if (isClosed && timesheet.ClosingBalanceHours.HasValue)
        {
            // Read back exactly what was frozen — never recomputed.
            var difference = Round(
                (timesheet.ActualHoursSnapshot ?? 0m) - (timesheet.TargetHoursSnapshot ?? 0m));

            var absorbed = timesheet.AbsorbedByLumpSumHours ?? 0m;
            var covered = timesheet.CoveredByAllInHours ?? 0m;

            var snapshot = new BalanceResult(
                timesheet.OpeningBalanceHours ?? 0m,
                difference,
                absorbed,
                covered,
                Round(difference - absorbed - covered),
                timesheet.ClosingBalanceHours.Value,
                timesheet.ForfeitedHours ?? 0m,
                policy?.DeficitFloorHours is { } floor && timesheet.ClosingBalanceHours.Value < floor);

            return new MonthFigures(
                timesheet.TargetHoursSnapshot,
                timesheet.ActualHoursSnapshot ?? 0m,
                snapshot,
                policy?.SurplusCapHours,
                policy?.DeficitFloorHours,
                absenceDays,
                IsProvisional: false,
                IsAllIn: contract.IsAllIn,
                OvertimeLumpSumHours: contract.LumpSum);
        }

        var live = await ComputeAsync(
            timesheet, terms, calendar, absences, policies, timesheets, ct);

        return live with { AbsenceDays = absenceDays, IsProvisional = !isClosed };
    }

    /// <summary>
    /// Computes the figures from current data. Called on the approval path to produce the
    /// snapshot, and for any month that has not been closed.
    /// </summary>
    public static async Task<MonthFigures> ComputeAsync(
        Timesheet timesheet,
        IEmploymentTermsRepository terms,
        INonWorkingDayRepository calendar,
        IAbsenceRepository absences,
        IWorkTimePolicyRepository policies,
        ITimesheetRepository timesheets,
        CancellationToken ct = default)
    {
        var (monthStart, monthEnd) = Bounds(timesheet);

        var termsVersions = await terms.GetForRangeAsync(
            timesheet.UserId, monthStart, monthEnd, ct);
        var nonWorkingDays = await calendar.GetForRangeAsync(monthStart, monthEnd, ct);
        var approvedAbsences = await absences.GetApprovedInRangeAsync(
            timesheet.UserId, monthStart, monthEnd, ct);
        var policy = await policies.GetEffectiveAsync(monthStart, ct);

        var targetHours = WorkingDayCalculator.TargetHoursForMonth(
            timesheet.Year, timesheet.Month, termsVersions, nonWorkingDays, approvedAbsences);

        var actualHours = timesheet.TotalWorkedHours;
        var openingBalance = await OpeningBalanceAsync(timesheet, timesheets, ct);

        // The version in force at month end decides how this month's overtime is compensated. A
        // contract change mid-month is rare and the later terms are the ones being paid under.
        var contract = ContractFrom(WorkingDayCalculator.TermsOn(termsVersions, monthEnd)
                                    ?? termsVersions.LastOrDefault());

        // With no employment terms there is nothing to measure against, so the month moves the
        // balance by nothing rather than by a fictitious deficit.
        var balance = BalanceCalculator.Calculate(
            openingBalance,
            targetHours ?? actualHours,
            actualHours,
            policy?.SurplusCapHours,
            policy?.DeficitFloorHours,
            contract.IsAllIn,
            contract.LumpSum);

        return new MonthFigures(
            targetHours,
            actualHours,
            balance,
            policy?.SurplusCapHours,
            policy?.DeficitFloorHours,
            SummariseAbsence(approvedAbsences, monthStart, monthEnd),
            IsProvisional: true,
            IsAllIn: contract.IsAllIn,
            OvertimeLumpSumHours: contract.LumpSum);
    }

    /// <summary>
    /// The compensation terms that applied to a month — the version in force on its last day.
    /// </summary>
    private static async Task<(bool IsAllIn, decimal? LumpSum)> ContractForMonthAsync(
        Timesheet timesheet, IEmploymentTermsRepository terms, CancellationToken ct)
    {
        var monthStart = new DateOnly(timesheet.Year, timesheet.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var versions = await terms.GetForRangeAsync(timesheet.UserId, monthStart, monthEnd, ct);

        return ContractFrom(WorkingDayCalculator.TermsOn(versions, monthEnd)
                            ?? versions.LastOrDefault());
    }

    private static (bool IsAllIn, decimal? LumpSum) ContractFrom(EmploymentTerms? terms)
        => terms is null ? (false, null) : (terms.IsAllIn, terms.OvertimeLumpSumHours);

    /// <summary>
    /// The previous month's closing balance, or zero when there is no closed month before this one
    /// — a new employee starts level rather than in debt.
    /// </summary>
    private static async Task<decimal> OpeningBalanceAsync(
        Timesheet timesheet, ITimesheetRepository timesheets, CancellationToken ct)
    {
        var previous = new DateOnly(timesheet.Year, timesheet.Month, 1).AddMonths(-1);

        var priorMonth = await timesheets.GetForUserAsync(
            timesheet.UserId, previous.Year, previous.Month, ct);

        return priorMonth?.ClosingBalanceHours ?? 0m;
    }

    /// <summary>
    /// Approved absence in the month, by type, counting only the portion that falls inside it so a
    /// leave spanning a month boundary is split rather than double counted.
    /// </summary>
    private static IReadOnlyList<AbsenceDays> SummariseAbsence(
        IReadOnlyCollection<AbsenceRequest> approved, DateOnly monthStart, DateOnly monthEnd)
        => approved
            .GroupBy(a => a.Type)
            .Select(g => new AbsenceDays(
                g.Key,
                Round(g.Sum(a => DaysInMonth(a, monthStart, monthEnd)))))
            .Where(a => a.Days > 0m)
            .OrderBy(a => a.Type)
            .ToList();

    private static decimal DaysInMonth(AbsenceRequest absence, DateOnly from, DateOnly to)
    {
        var total = 0m;
        for (var date = from; date <= to; date = date.AddDays(1))
            total += absence.DayFraction(date);

        return total;
    }

    private static (DateOnly Start, DateOnly End) Bounds(Timesheet timesheet)
    {
        var start = new DateOnly(timesheet.Year, timesheet.Month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
