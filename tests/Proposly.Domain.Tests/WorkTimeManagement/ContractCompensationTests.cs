using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

/// <summary>
/// How an all-in agreement and an Überstundenpauschale change what reaches the balance.
/// Both compensate overtime, so both reduce what carries forward — neither touches a shortfall.
/// </summary>
public class ContractCompensationTests
{
    private const decimal Cap = 80m;
    private const decimal Floor = -20m;

    private static BalanceResult WithLumpSum(
        decimal target, decimal actual, decimal lumpSum, decimal opening = 0m)
        => BalanceCalculator.Calculate(
            opening, target, actual, Cap, Floor, isAllIn: false, overtimeLumpSumHours: lumpSum);

    private static BalanceResult AllIn(decimal target, decimal actual, decimal opening = 0m)
        => BalanceCalculator.Calculate(
            opening, target, actual, Cap, Floor, isAllIn: true);

    // --- Überstundenpauschale ---

    [Fact]
    public void A_lump_sum_absorbs_the_surplus_up_to_its_size()
    {
        // 16.8 surplus against a 10-hour lump sum: 10 absorbed, 6.8 banked.
        var result = WithLumpSum(target: 161.7m, actual: 178.5m, lumpSum: 10m);

        Assert.Equal(16.8m, result.MonthlyDifference);
        Assert.Equal(10m, result.AbsorbedByLumpSumHours);
        Assert.Equal(6.8m, result.CarriedForward);
        Assert.Equal(6.8m, result.ClosingBalance);
    }

    [Fact]
    public void A_surplus_smaller_than_the_lump_sum_is_absorbed_entirely()
    {
        // Only 4 hours of surplus against a 10-hour lump sum — nothing is banked, and the unused
        // part of the lump sum is not credited either. It is paid whether used or not.
        var result = WithLumpSum(target: 160m, actual: 164m, lumpSum: 10m);

        Assert.Equal(4m, result.AbsorbedByLumpSumHours);
        Assert.Equal(0m, result.CarriedForward);
        Assert.Equal(0m, result.ClosingBalance);
    }

    [Fact]
    public void A_lump_sum_never_absorbs_more_than_the_month_produced()
    {
        var result = WithLumpSum(target: 160m, actual: 162m, lumpSum: 20m);

        Assert.Equal(2m, result.AbsorbedByLumpSumHours);
    }

    [Fact]
    public void A_lump_sum_does_not_offset_a_shortfall()
    {
        // Being paid for overtime does not excuse hours not worked.
        var result = WithLumpSum(target: 160m, actual: 150m, lumpSum: 10m);

        Assert.Equal(-10m, result.MonthlyDifference);
        Assert.Equal(0m, result.AbsorbedByLumpSumHours);
        Assert.Equal(-10m, result.CarriedForward);
        Assert.Equal(-10m, result.ClosingBalance);
    }

    [Fact]
    public void A_lump_sum_absorbs_before_the_cap_is_applied()
    {
        // 74 carried in, 16.8 surplus, 10 absorbed => 6.8 banked => 80.8, capped at 80.
        var result = WithLumpSum(target: 161.7m, actual: 178.5m, lumpSum: 10m, opening: 74m);

        Assert.Equal(10m, result.AbsorbedByLumpSumHours);
        Assert.Equal(80m, result.ClosingBalance);
        Assert.Equal(0.8m, result.ForfeitedHours);
    }

    // --- All-in ---

    [Fact]
    public void An_all_in_contract_covers_the_whole_surplus()
    {
        var result = AllIn(target: 161.7m, actual: 178.5m);

        // The surplus is still reported — it is the record, and the coverage check.
        Assert.Equal(16.8m, result.MonthlyDifference);
        Assert.Equal(16.8m, result.CoveredByAllInHours);

        // But nothing carries as time owed.
        Assert.Equal(0m, result.CarriedForward);
        Assert.Equal(0m, result.ClosingBalance);
    }

    [Fact]
    public void An_all_in_contract_does_not_forgive_a_shortfall()
    {
        // Salary covers overtime, not undertime.
        var result = AllIn(target: 160m, actual: 150m);

        Assert.Equal(0m, result.CoveredByAllInHours);
        Assert.Equal(-10m, result.CarriedForward);
        Assert.Equal(-10m, result.ClosingBalance);
    }

    [Fact]
    public void An_all_in_balance_never_grows_so_nothing_is_ever_forfeited()
    {
        var result = AllIn(target: 160m, actual: 220m, opening: 79m);

        Assert.Equal(60m, result.CoveredByAllInHours);
        Assert.Equal(79m, result.ClosingBalance);   // unchanged
        Assert.Equal(0m, result.ForfeitedHours);
    }

    [Fact]
    public void An_existing_all_in_balance_can_still_be_drawn_down()
    {
        // Hours banked before the contract became all-in remain the employee's to take.
        var result = AllIn(target: 160m, actual: 152m, opening: 20m);

        Assert.Equal(-8m, result.CarriedForward);
        Assert.Equal(12m, result.ClosingBalance);
    }

    // --- Neither, and the compensation flag ---

    [Fact]
    public void Without_either_term_the_whole_difference_carries()
    {
        var result = BalanceCalculator.Calculate(0m, 161.7m, 178.5m, Cap, Floor);

        Assert.Equal(16.8m, result.CarriedForward);
        Assert.Equal(0m, result.AbsorbedByLumpSumHours);
        Assert.Equal(0m, result.CoveredByAllInHours);
        Assert.False(result.HasCompensatedHours);
    }

    [Fact]
    public void Compensated_hours_are_flagged_so_a_report_can_explain_the_gap()
    {
        Assert.True(AllIn(160m, 170m).HasCompensatedHours);
        Assert.True(WithLumpSum(160m, 170m, 5m).HasCompensatedHours);
    }

    [Fact]
    public void Every_hour_of_the_month_is_accounted_for()
    {
        // The breakdown must reconcile: what the month produced equals what was compensated plus
        // what carried. Otherwise hours vanish silently, which is the whole thing to avoid.
        var result = WithLumpSum(target: 161.7m, actual: 178.5m, lumpSum: 10m);

        Assert.Equal(
            result.MonthlyDifference,
            result.AbsorbedByLumpSumHours + result.CoveredByAllInHours + result.CarriedForward);
    }

    [Fact]
    public void The_same_holds_for_an_all_in_month()
    {
        var result = AllIn(target: 161.7m, actual: 178.5m);

        Assert.Equal(
            result.MonthlyDifference,
            result.AbsorbedByLumpSumHours + result.CoveredByAllInHours + result.CarriedForward);
    }
}
