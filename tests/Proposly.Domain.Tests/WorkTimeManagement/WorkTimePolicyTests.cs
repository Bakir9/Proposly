using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class WorkTimePolicyTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    private static WorkTimePolicy Policy(DateOnly? validFrom = null)
        => WorkTimePolicy.Create(
            CompanyId, validFrom ?? Jan1, "AT", "AT-4",
            maxHoursPerDay: 12m, maxHoursPerWeek: 60m,
            averagingWindowWeeks: 17, maxAverageHoursPerWeek: 48m,
            minDailyRestHours: 11m, minWeeklyRestHours: 36m,
            surplusCapHours: 80m, deficitFloorHours: -20m);

    // --- Shipped defaults ---

    [Theory]
    [InlineData("AT", 12, 17, 1)]
    [InlineData("DE", 10, 24, 2)]
    public void Defaults_are_seeded_per_jurisdiction(
        string jurisdiction, decimal maxDaily, int windowWeeks, int breakTiers)
    {
        var policy = WorkTimePolicyDefaults.For(jurisdiction, CompanyId, Jan1)!;

        Assert.Equal(maxDaily, policy.MaxHoursPerDay);
        Assert.Equal(windowWeeks, policy.AveragingWindowWeeks);
        Assert.Equal(breakTiers, policy.BreakRules.Count);
        Assert.Equal(jurisdiction, policy.Jurisdiction);
    }

    [Fact]
    public void An_unsupported_jurisdiction_returns_null_rather_than_guessing()
    {
        Assert.Null(WorkTimePolicyDefaults.For("US", CompanyId, Jan1));
        Assert.Null(WorkTimePolicyDefaults.For("", CompanyId, Jan1));
    }

    // --- Break tiers ---

    [Theory]
    [InlineData(5.0, 0)]
    [InlineData(6.0, 0)]
    [InlineData(6.5, 30)]
    [InlineData(9.0, 30)]
    [InlineData(9.5, 45)]
    [InlineData(12.0, 45)]
    public void The_highest_matching_break_tier_applies(decimal workedHours, int expected)
    {
        var policy = WorkTimePolicyDefaults.For("DE", CompanyId, Jan1)!;

        Assert.Equal(expected, policy.RequiredBreakMinutes(workedHours));
    }

    [Fact]
    public void A_duplicate_break_tier_is_rejected()
    {
        var policy = Policy();
        policy.AddBreakRule(6m, 30);

        Assert.Throws<InvalidOperationException>(() => policy.AddBreakRule(6m, 45));
    }

    // --- Versioning ---

    [Fact]
    public void Closing_a_version_ends_it_the_day_before_its_successor()
    {
        var policy = Policy(new DateOnly(2026, 1, 1));

        policy.CloseAt(new DateOnly(2026, 7, 1));

        Assert.Equal(new DateOnly(2026, 6, 30), policy.ValidTo);
    }

    [Fact]
    public void A_successor_must_take_effect_after_the_version_it_closes()
    {
        var policy = Policy(new DateOnly(2026, 7, 1));

        Assert.Throws<InvalidOperationException>(() => policy.CloseAt(new DateOnly(2026, 1, 1)));
        Assert.Throws<InvalidOperationException>(() => policy.CloseAt(new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void An_open_version_applies_to_every_date_from_its_start()
    {
        var policy = Policy(new DateOnly(2026, 1, 1));

        Assert.False(policy.AppliesOn(new DateOnly(2025, 12, 31)));
        Assert.True(policy.AppliesOn(new DateOnly(2026, 1, 1)));
        Assert.True(policy.AppliesOn(new DateOnly(2030, 1, 1)));
    }

    [Fact]
    public void A_closed_version_stops_applying_after_its_end()
    {
        var policy = Policy(new DateOnly(2026, 1, 1));
        policy.CloseAt(new DateOnly(2026, 7, 1));

        Assert.True(policy.AppliesOn(new DateOnly(2026, 6, 30)));
        Assert.False(policy.AppliesOn(new DateOnly(2026, 7, 1)));
    }

    // --- Validation ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    public void An_implausible_daily_maximum_is_rejected(decimal maxHoursPerDay)
    {
        Assert.Throws<ArgumentException>(() => WorkTimePolicy.Create(
            CompanyId, Jan1, "AT", null,
            maxHoursPerDay, 60m, 17, 48m, 11m, 36m, 80m, -20m));
    }

    [Fact]
    public void A_negative_surplus_cap_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => WorkTimePolicy.Create(
            CompanyId, Jan1, "AT", null,
            12m, 60m, 17, 48m, 11m, 36m, surplusCapHours: -5m, deficitFloorHours: -20m));
    }

    [Fact]
    public void A_positive_deficit_floor_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => WorkTimePolicy.Create(
            CompanyId, Jan1, "AT", null,
            12m, 60m, 17, 48m, 11m, 36m, surplusCapHours: 80m, deficitFloorHours: 5m));
    }

    [Fact]
    public void Null_bounds_mean_the_balance_is_unbounded()
    {
        var policy = WorkTimePolicy.Create(
            CompanyId, Jan1, "AT", null,
            12m, 60m, 17, 48m, 11m, 36m, surplusCapHours: null, deficitFloorHours: null);

        Assert.Null(policy.SurplusCapHours);
        Assert.Null(policy.DeficitFloorHours);
    }
}
