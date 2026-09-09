namespace OrderFlow.Domain.UnitTests.Orders;

using FluentAssertions;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.Events;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class OrderTests
{
    private static Address AnAddress() => Address.Create("12 Market St", "Istanbul", "34000", "TR");

    private static Order AnOrder(string currency = "USD") => Order.Create("CUST-1001", AnAddress(), currency);

    [Fact]
    public void Create_StartsPendingWithNoLinesAndNoEvents()
    {
        var order = AnOrder();

        order.Status.Should().Be(OrderStatus.Pending);
        order.CustomerId.Should().Be("CUST-1001");
        order.Currency.Should().Be("USD");
        order.Lines.Should().BeEmpty();
        order.DomainEvents.Should().BeEmpty();
        order.Id.Value.Should().NotBe(Guid.Empty);
        order.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutCustomerId_Throws(string customerId)
    {
        var act = () => Order.Create(customerId, AnAddress(), "USD");

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Order.Customer");
    }

    [Fact]
    public void AddLine_AppendsLinePricedInTheOrderCurrency()
    {
        var order = AnOrder();

        order.AddLine("SKU-1", 2, 19.99m);

        order.Lines.Should().ContainSingle();
        var line = order.Lines[0];
        line.Sku.Should().Be("SKU-1");
        line.Quantity.Should().Be(2);
        line.UnitPrice.Should().Be(Money.Create(19.99m, "USD"));
        line.LineTotal.Amount.Should().Be(39.98m);
    }

    [Fact]
    public void AddLine_WithDuplicateSku_Throws()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 1, 10m);

        var act = () => order.AddLine("SKU-1", 3, 10m);

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Order.DuplicateSku");
    }

    [Fact]
    public void AddLine_WithNonPositiveQuantity_Throws()
    {
        var order = AnOrder();

        var act = () => order.AddLine("SKU-1", 0, 10m);

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("OrderLine.Quantity");
    }

    [Fact]
    public void Lines_CannotBeMutatedFromOutsideTheAggregate()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 1, 10m);

        var asCollection = (ICollection<OrderLine>)order.Lines;
        var act = () => asCollection.Add(new OrderLine(Guid.CreateVersion7(), "SKU-2", 1, Money.Create(1m, "USD")));

        asCollection.IsReadOnly.Should().BeTrue();
        act.Should().Throw<NotSupportedException>();
        order.Lines.Should().ContainSingle();
    }

    [Fact]
    public void Total_WithNoLines_IsZeroInTheOrderCurrency()
    {
        var order = AnOrder("EUR");

        order.Total.Should().Be(Money.Zero("EUR"));
    }

    [Fact]
    public void Total_SumsEveryLineTotal()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 2, 19.99m);   // 39.98
        order.AddLine("SKU-2", 3, 5m);       // 15.00

        order.Total.Amount.Should().Be(54.98m);
        order.Total.Currency.Should().Be("USD");
    }

    [Fact]
    public void Place_SetsStatusAndRaisesOrderPlaced()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 2, 19.99m);

        order.Place();

        order.Status.Should().Be(OrderStatus.Placed);
        var placed = order.DomainEvents.OfType<OrderPlacedDomainEvent>().Should().ContainSingle().Subject;
        placed.OrderId.Should().Be(order.Id.Value);
        placed.Total.Should().Be(39.98m);
        placed.Currency.Should().Be("USD");
    }

    [Fact]
    public void Place_WithNoLines_Throws()
    {
        var order = AnOrder();

        var act = order.Place;

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Order.Empty");
        order.Status.Should().Be(OrderStatus.Pending);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Place_Twice_Throws()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 1, 10m);
        order.Place();

        var act = order.Place;

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Order.Immutable");
    }

    [Fact]
    public void AddLine_AfterPlace_Throws()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 1, 10m);
        order.Place();

        var act = () => order.AddLine("SKU-2", 1, 10m);

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Order.Immutable");
        order.Lines.Should().ContainSingle();
    }

    [Fact]
    public void Cancel_SetsStatusAndRaisesOrderCancelled()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 1, 10m);
        order.Place();
        order.ClearDomainEvents();

        order.Cancel("customer changed their mind");

        order.Status.Should().Be(OrderStatus.Cancelled);
        var cancelled = order.DomainEvents.OfType<OrderCancelledDomainEvent>().Should().ContainSingle().Subject;
        cancelled.OrderId.Should().Be(order.Id.Value);
        cancelled.Reason.Should().Be("customer changed their mind");
    }

    [Fact]
    public void Cancel_IsIdempotent()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 1, 10m);
        order.Place();
        order.Cancel("first");
        order.ClearDomainEvents();

        order.Cancel("second");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_BeforePlace_IsAllowed()
    {
        var order = AnOrder();

        order.Cancel("abandoned");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.OfType<OrderCancelledDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void AddLine_AfterCancel_Throws()
    {
        var order = AnOrder();
        order.Cancel("abandoned");

        var act = () => order.AddLine("SKU-1", 1, 10m);

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Order.Immutable");
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheEventList()
    {
        var order = AnOrder();
        order.AddLine("SKU-1", 1, 10m);
        order.Place();

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }
}
