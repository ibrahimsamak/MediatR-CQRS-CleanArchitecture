namespace OrderFlow.Domain.UnitTests.Orders.ValueObjects;

using FluentAssertions;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithValidAmountAndCurrency_ReturnsMoney()
    {
        var money = Money.Create(19.99m, "USD");

        money.Amount.Should().Be(19.99m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_NormalizesCurrencyToUpperCase()
    {
        Money.Create(1m, "usd").Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        var act = () => Money.Create(-0.01m, "USD");

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Money.Negative");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Create_WithInvalidCurrency_Throws(string currency)
    {
        var act = () => Money.Create(1m, currency);

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Money.Currency");
    }

    [Fact]
    public void Zero_ReturnsZeroAmountInGivenCurrency()
    {
        var zero = Money.Zero("EUR");

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Add_WithSameCurrency_SumsAmounts()
    {
        var sum = Money.Create(10.50m, "USD").Add(Money.Create(4.50m, "USD"));

        sum.Should().Be(Money.Create(15m, "USD"));
    }

    [Fact]
    public void Add_WithDifferentCurrency_Throws()
    {
        var act = () => Money.Create(10m, "USD").Add(Money.Create(10m, "EUR"));

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Money.CurrencyMismatch");
    }

    [Fact]
    public void PlusOperator_SumsAmounts()
    {
        var sum = Money.Create(1m, "TRY") + Money.Create(2m, "TRY");

        sum.Amount.Should().Be(3m);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = Money.Create(5m, "USD");
        var b = Money.Create(5m, "USD");
        var other = Money.Create(5m, "EUR");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(other);
    }
}
