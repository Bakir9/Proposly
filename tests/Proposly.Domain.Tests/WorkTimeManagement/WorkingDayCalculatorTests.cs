using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class WorkingDayCalculatorTests
{
    // July 2026: the 6th is a Monday, the 17th a Friday.
    private static readonly DateOnly Mon6 = new(2026, 7, 6);
    private static readonly DateOnly Fri10 = new(2026, 7, 10);
    private static readonly DateOnly Sat11 = new(2026, 7, 11);
    private static readonly DateOnly Sun12 = new(2026, 7, 12);
    private static readonly DateOnly Fri17 = new(2026, 7, 17);

    [Fact]
    public void A_two_week_range_counts_ten_working_days()
    {
        Assert.Equal(10, WorkingDayCalculator.WorkingDaysInRange(Mon6, Fri17));
    }

    [Fact]
    public void A_single_working_day_counts_one()
    {
        Assert.Equal(1, WorkingDayCalculator.WorkingDaysInRange(Mon6, Mon6));
    }

    [Fact]
    public void Weekends_are_excluded()
    {
        Assert.Equal(0, WorkingDayCalculator.WorkingDaysInRange(Sat11, Sun12));
        Assert.Equal(5, WorkingDayCalculator.WorkingDaysInRange(Mon6, Fri10));
    }

    [Fact]
    public void A_reversed_range_counts_nothing()
    {
        Assert.Equal(0, WorkingDayCalculator.WorkingDaysInRange(Fri17, Mon6));
    }

    [Theory]
    [InlineData(false, false, 10.0)]
    [InlineData(true, false, 9.5)]
    [InlineData(false, true, 9.5)]
    [InlineData(true, true, 9.0)]
    public void Half_day_markers_reduce_the_count_by_half_a_day_each(
        bool firstHalf, bool lastHalf, decimal expected)
    {
        Assert.Equal(expected, WorkingDayCalculator.ConsumedDays(Mon6, Fri17, firstHalf, lastHalf));
    }

    [Fact]
    public void A_single_day_marked_half_costs_half_a_day_not_none()
    {
        Assert.Equal(0.5m, WorkingDayCalculator.ConsumedDays(Mon6, Mon6, true, false));
        Assert.Equal(0.5m, WorkingDayCalculator.ConsumedDays(Mon6, Mon6, false, true));

        // Both markers on the same single day still mean one half day.
        Assert.Equal(0.5m, WorkingDayCalculator.ConsumedDays(Mon6, Mon6, true, true));
    }

    [Fact]
    public void A_range_entirely_on_a_weekend_costs_nothing()
    {
        Assert.Equal(0m, WorkingDayCalculator.ConsumedDays(Sat11, Sun12, false, false));
    }

    [Fact]
    public void A_half_day_marker_on_a_weekend_day_does_not_reduce_the_count()
    {
        // Sat 11 to Fri 17: five working days. Marking the Saturday half changes nothing.
        Assert.Equal(5m, WorkingDayCalculator.ConsumedDays(Sat11, Fri17, firstDayIsHalf: true, lastDayIsHalf: false));
    }

    [Fact]
    public void A_reversed_range_is_rejected_when_counting_consumed_days()
    {
        Assert.Throws<ArgumentException>(
            () => WorkingDayCalculator.ConsumedDays(Fri17, Mon6, false, false));
    }

    [Fact]
    public void The_non_working_dates_are_reported_so_a_request_can_explain_its_total()
    {
        var excluded = WorkingDayCalculator.NonWorkingDatesInRange(Mon6, Fri17);

        // Monday to the Friday of the following week spans exactly one weekend: 12 calendar days,
        // 10 of them working.
        Assert.Equal(2, excluded.Count);
        Assert.Contains(Sat11, excluded);
        Assert.Contains(Sun12, excluded);
    }

    [Fact]
    public void The_working_dates_are_listed_in_order()
    {
        var working = WorkingDayCalculator.WorkingDatesInRange(Mon6, Fri10);

        Assert.Equal(5, working.Count);
        Assert.Equal(Mon6, working[0]);
        Assert.Equal(Fri10, working[^1]);
    }
}
