using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.Tests.Shared;

public class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var a = new Money(100m, "EUR");
        var b = new Money(50m, "EUR");

        var result = a + b;

        Assert.Equal(150m, result.Amount);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var eur = new Money(100m, "EUR");
        var usd = new Money(50m, "USD");

        Assert.Throws<InvalidOperationException>(() => _ = eur + usd);
    }

    [Fact]
    public void Multiply_ReturnsCorrectAmount()
    {
        var price = new Money(25m, "EUR");

        var result = price * 4m;

        Assert.Equal(100m, result.Amount);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        var zero = Money.Zero("EUR");

        Assert.Equal(0m, zero.Amount);
        Assert.Equal("EUR", zero.Currency);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Money(99.99m, "EUR");
        var b = new Money(99.99m, "EUR");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
