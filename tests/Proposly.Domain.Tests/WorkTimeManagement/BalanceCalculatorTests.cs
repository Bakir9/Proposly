using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class BalanceCalculatorTests
{
    private const decimal Cap = 80m;
    private const decimal Floor = -20m;

    [Fact]
    public void A_surplus_month_adds_to_the_balance()
    {
        var result = BalanceCalculator.Calculate(
            openingBalance: 10m, targetHours: 160m, actualHours: 168m, Cap, Floor);

        Assert.Equal(8m, result.MonthlyDifference);
        Assert.Equal(18m, result.ClosingBalance);
        Assert.Equal(0m, result.ForfeitedHours);
        Assert.False(result.DeficitFloorBreached);
    }

    [Fact]
    public void A_deficit_month_subtracts_from_the_balance()
    {
        var result = BalanceCalculator.Calculate(
            openingBalance: 10m, targetHours: 160m, actualHours: 152m, Cap, Floor);

        Assert.Equal(-8m, result.MonthlyDifference);
        Assert.Equal(2m, result.ClosingBalance);
    }

    [Fact]
    public void An_exactly_matching_month_leaves_the_balance_unchanged()
    {
        var result = BalanceCalculator.Calculate(10m, 160m, 160m, Cap, Floor);

        Assert.Equal(0m, result.MonthlyDifference);
        Assert.Equal(10m, result.ClosingBalance);
    }

    // --- The cap forfeits, and says so ---

    [Fact]
    public void Surplus_above_the_cap_is_held_at_the_cap_and_the_excess_is_reported()
    {
        // The spec's worked example: 74 carried in, 9 hours gained, 80 cap.
        var result = BalanceCalculator.Calculate(
            openingBalance: 74m, targetHours: 160m, actualHours: 169m, Cap, Floor);

        Assert.Equal(9m, result.MonthlyDifference);
        Assert.Equal(80m, result.ClosingBalance);
        Assert.Equal(3m, result.ForfeitedHours);
    }

    [Fact]
    public void Landing_exactly_on_the_cap_forfeits_nothing()
    {
        var result = BalanceCalculator.Calculate(74m, 160m, 166m, Cap, Floor);

        Assert.Equal(80m, result.ClosingBalance);
        Assert.Equal(0m, result.ForfeitedHours);
    }

    [Fact]
    public void A_balance_already_at_the_cap_forfeits_every_further_hour()
    {
        var result = BalanceCalculator.Calculate(80m, 160m, 165m, Cap, Floor);

        Assert.Equal(80m, result.ClosingBalance);
        Assert.Equal(5m, result.ForfeitedHours);
    }

    [Fact]
    public void With_no_cap_the_surplus_accumulates_untouched()
    {
        var result = BalanceCalculator.Calculate(
            200m, 160m, 200m, surplusCapHours: null, deficitFloorHours: null);

        Assert.Equal(240m, result.ClosingBalance);
        Assert.Equal(0m, result.ForfeitedHours);
    }

    // --- The floor reports, and does NOT clamp ---

    [Fact]
    public void A_deficit_past_the_floor_is_flagged_but_not_written_off()
    {
        // Clamping here would quietly forgive hours the employee still owes.
        var result = BalanceCalculator.Calculate(
            openingBalance: -15m, targetHours: 160m, actualHours: 150m, Cap, Floor);

        Assert.Equal(-25m, result.ClosingBalance);
        Assert.True(result.DeficitFloorBreached);
        Assert.Equal(0m, result.ForfeitedHours);
    }

    [Fact]
    public void Landing_exactly_on_the_floor_is_not_a_breach()
    {
        var result = BalanceCalculator.Calculate(-10m, 160m, 150m, Cap, Floor);

        Assert.Equal(-20m, result.ClosingBalance);
        Assert.False(result.DeficitFloorBreached);
    }

    [Fact]
    public void With_no_floor_a_deficit_is_never_flagged()
    {
        var result = BalanceCalculator.Calculate(
            -100m, 160m, 100m, surplusCapHours: null, deficitFloorHours: null);

        Assert.Equal(-160m, result.ClosingBalance);
        Assert.False(result.DeficitFloorBreached);
    }

    // --- Carrying across the year ---

    [Fact]
    public void The_balance_carries_across_the_year_boundary_with_no_reset()
    {
        var december = BalanceCalculator.Calculate(38.5m, 160m, 163m, Cap, Floor);
        var january = BalanceCalculator.Calculate(
            december.ClosingBalance, 160m, 162m, Cap, Floor);

        Assert.Equal(41.5m, december.ClosingBalance);
        Assert.Equal(41.5m, january.OpeningBalance);
        Assert.Equal(43.5m, january.ClosingBalance);
    }

    // --- Warning before hours are lost ---

    [Theory]
    [InlineData(80, true)]   // at the cap
    [InlineData(76, true)]   // 4 from the cap
    [InlineData(75, true)]   // exactly at the 5-hour threshold
    [InlineData(74, false)]  // 6 from the cap — not yet
    [InlineData(60, false)]
    public void The_employee_is_warned_once_the_balance_is_within_five_hours_of_the_cap(
        decimal closingTarget, bool expected)
    {
        var result = BalanceCalculator.Calculate(closingTarget, 160m, 160m, Cap, Floor);

        Assert.Equal(expected, result.ApproachingCap(Cap));
    }

    [Fact]
    public void With_no_cap_there_is_nothing_to_warn_about()
    {
        var result = BalanceCalculator.Calculate(500m, 160m, 160m, null, null);

        Assert.False(result.ApproachingCap(null));
    }

    // --- Rounding ---

    [Fact]
    public void Figures_are_rounded_to_two_places()
    {
        var result = BalanceCalculator.Calculate(0m, 161.7m, 168.333m, Cap, Floor);

        Assert.Equal(6.63m, result.MonthlyDifference);
        Assert.Equal(6.63m, result.ClosingBalance);
    }

    [Fact]
    public void Opening_plus_difference_always_reconciles_to_closing_when_uncapped()
    {
        var result = BalanceCalculator.Calculate(12.25m, 161.7m, 155.4m, Cap, Floor);

        Assert.Equal(
            result.OpeningBalance + result.MonthlyDifference,
            result.ClosingBalance + result.ForfeitedHours);
    }
}
