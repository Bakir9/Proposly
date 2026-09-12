namespace Proposly.Domain.WorkTimeManagement.Services;

/// <summary>
/// The month's flexitime arithmetic: what carried in, what the month added or took away, and what
/// carries out once the agreed bounds are applied.
/// </summary>
public readonly record struct BalanceResult(
    decimal OpeningBalance,
    decimal MonthlyDifference,
    decimal ClosingBalance,
    decimal ForfeitedHours,
    bool DeficitFloorBreached)
{
    /// <summary>True when the balance is close enough to the cap to warn the employee.</summary>
    public bool ApproachingCap(decimal? surplusCap, decimal warnWithin = 5m)
        => surplusCap.HasValue && ClosingBalance >= surplusCap.Value - warnWithin;
}

/// <summary>
/// Carries a flexitime balance from one month to the next, bounded by the company's agreement.
/// <para>
/// The two bounds behave deliberately differently. Surplus above the cap is <b>forfeited</b> and
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
        decimal? deficitFloorHours = null)
    {
        var difference = Round(actualHours - targetHours);
        var uncapped = Round(openingBalance + difference);

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
            Round(openingBalance), difference, closing, forfeited, floorBreached);
    }

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
