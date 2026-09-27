using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class AbsenceEntitlementTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static AbsenceEntitlement Entitlement(decimal entitled = 25m, decimal carried = 0m)
        => AbsenceEntitlement.Create(CompanyId, UserId, 2026, entitled, carried);

    [Fact]
    public void Remaining_is_entitled_plus_carry_over_less_used()
    {
        var entitlement = Entitlement(25m, carried: 5m);

        Assert.Equal(30m, entitlement.RemainingDays);

        entitlement.Consume(9.5m);

        Assert.Equal(9.5m, entitlement.UsedDays);
        Assert.Equal(20.5m, entitlement.RemainingDays);
    }

    [Fact]
    public void Consuming_more_than_remains_is_refused_and_changes_nothing()
    {
        var entitlement = Entitlement(25m);

        var ex = Assert.Throws<InvalidOperationException>(() => entitlement.Consume(25.5m));

        Assert.Contains("Only 25 day(s) remain", ex.Message);
        Assert.Equal(0m, entitlement.UsedDays);
    }

    [Fact]
    public void Consuming_exactly_the_remaining_balance_is_allowed()
    {
        var entitlement = Entitlement(25m);

        entitlement.Consume(25m);

        Assert.Equal(0m, entitlement.RemainingDays);
    }

    [Fact]
    public void Releasing_returns_days_to_the_balance()
    {
        var entitlement = Entitlement(25m);
        entitlement.Consume(10m);

        entitlement.Release(4m);

        Assert.Equal(6m, entitlement.UsedDays);
        Assert.Equal(19m, entitlement.RemainingDays);
    }

    [Fact]
    public void Releasing_more_than_was_used_floors_at_zero_rather_than_going_negative()
    {
        var entitlement = Entitlement(25m);
        entitlement.Consume(3m);

        entitlement.Release(10m);

        Assert.Equal(0m, entitlement.UsedDays);
        Assert.Equal(25m, entitlement.RemainingDays);
    }

    [Fact]
    public void Adjusting_the_entitlement_keeps_days_already_used()
    {
        var entitlement = Entitlement(25m);
        entitlement.Consume(10m);

        entitlement.SetEntitlement(entitledDays: 30m, carriedOverDays: 2m);

        Assert.Equal(10m, entitlement.UsedDays);
        Assert.Equal(22m, entitlement.RemainingDays);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(25, -1)]
    public void Negative_figures_are_rejected(decimal entitled, decimal carried)
    {
        Assert.Throws<ArgumentException>(() =>
            AbsenceEntitlement.Create(CompanyId, UserId, 2026, entitled, carried));
    }

    [Fact]
    public void Negative_consumption_or_release_is_rejected()
    {
        var entitlement = Entitlement();

        Assert.Throws<ArgumentException>(() => entitlement.Consume(-1m));
        Assert.Throws<ArgumentException>(() => entitlement.Release(-1m));
    }

    [Fact]
    public void An_out_of_range_year_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AbsenceEntitlement.Create(CompanyId, UserId, 1999, 25m));
    }
}
