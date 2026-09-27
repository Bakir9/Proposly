namespace Proposly.Domain.WorkTimeManagement.Services;

/// <summary>
/// The month's flexitime arithmetic, step by step, so a report can show where every hour went.
/// <para>
/// <c>MonthlyDifference</c> is what the month actually produced. From it, an overtime lump sum
/// absorbs its share and an all-in agreement covers whatever surplus remains; only
/// <c>CarriedForward</c> reaches the balance.
/// </para>
/// </summary>
public readonly record struct BalanceResult(
    decimal OpeningBalance,
    decimal MonthlyDifference,
    decimal AbsorbedByLumpSumHours,
    decimal CoveredByAllInHours,
    decimal CarriedForward,
    decimal ClosingBalance,
    decimal ForfeitedHours,
    bool DeficitFloorBreached)
{
    /// <summary>True when the balance is close enough to the cap to warn the employee.</summary>
    public bool ApproachingCap(decimal? surplusCap, decimal warnWithin = 5m)
        => surplusCap.HasValue && ClosingBalance >= surplusCap.Value - warnWithin;

    /// <summary>True when some part of the month's surplus never reached the balance.</summary>
    public bool HasCompensatedHours => AbsorbedByLumpSumHours > 0m || CoveredByAllInHours > 0m;
}

/// <summary>
/// Carries a flexitime balance from one month to the next, applying the contract's compensation
/// terms and then the company's agreed bounds.
/// <para>
/// The bounds behave deliberately differently. Surplus above the cap is <b>forfeited</b> and
/// reported as a distinct figure, because the employee needs to see hours they are about to lose.
/// A deficit past the floor is <b>not</b> clamped: writing it off would quietly forgive time the
/// employee still owes, so the breach is flagged for the employer to decide.
/// </para>
/// <para>
/// There is no annual reset. The balance carries across the year boundary; settlement is a payroll
/// decision this module reports on rather than performs.
/// </para>
/// </summary>
public static class BalanceCalculator
{
    public static BalanceResult Calculate(
        decimal openingBalance,
        decimal targetHours,
        decimal actualHours,
        decimal? surplusCapHours = null,
        decimal? deficitFloorHours = null,
        bool isAllIn = false,
        decimal? overtimeLumpSumHours = null)
    {
        var difference = Round(actualHours - targetHours);
        var remaining = difference;

        // A lump sum is paid whether or not it is used, so it absorbs the surplus first — up to
        // its size, and never more than the month actually produced.
        var absorbed = 0m;
        if (overtimeLumpSumHours is > 0m && remaining > 0m)
        {
            absorbed = Math.Min(remaining, overtimeLumpSumHours.Value);
            remaining = Round(remaining - absorbed);
        }

        // All-in salary covers whatever surplus is left. A shortfall still carries: the agreement
        // covers overtime, not undertime.
        var coveredByAllIn = 0m;
        if (isAllIn && remaining > 0m)
        {
            coveredByAllIn = remaining;
            remaining = 0m;
        }

        var carriedForward = Round(remaining);
        var uncapped = Round(openingBalance + carriedForward);

        var forfeited = 0m;
        var closing = uncapped;

        if (surplusCapHours.HasValue && uncapped > surplusCapHours.Value)
        {
            forfeited = Round(uncapped - surplusCapHours.Value);
            closing = surplusCapHours.Value;
        }

        // Deliberately not clamped — see the class remarks.
        var floorBreached = deficitFloorHours.HasValue && closing < deficitFloorHours.Value;

        return new BalanceResult(
            Round(openingBalance),
            difference,
            Round(absorbed),
            Round(coveredByAllIn),
            carriedForward,
            closing,
            forfeited,
            floorBreached);
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
