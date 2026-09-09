namespace OrderFlow.Domain.UnitTests.Orders;

using FluentAssertions;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class OrderLineTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.CreateVersion7();

        var line = new OrderLine(id, "SKU-1", 3, Money.Create(9.99m, "USD"));

        line.Id.Should().Be(id);
        line.Sku.Should().Be("SKU-1");
        line.Quantity.Should().Be(3);
        line.UnitPrice.Amount.Should().Be(9.99m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutSku_Throws(string sku)
    {
        var act = () => new OrderLine(Guid.CreateVersion7(), sku, 1, Money.Create(1m, "USD"));

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("OrderLine.Sku");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveQuantity_Throws(int quantity)
    {
        var act = () => new OrderLine(Guid.CreateVersion7(), "SKU-1", quantity, Money.Create(1m, "USD"));

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("OrderLine.Quantity");
    }

    [Fact]
    public void LineTotal_IsUnitPriceTimesQuantityInTheSameCurrency()
    {
        var line = new OrderLine(Guid.CreateVersion7(), "SKU-1", 4, Money.Create(2.25m, "EUR"));

        line.LineTotal.Amount.Should().Be(9m);
        line.LineTotal.Currency.Should().Be("EUR");
    }
}
