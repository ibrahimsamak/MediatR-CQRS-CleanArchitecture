namespace OrderFlow.Application.UnitTests.Orders.PlaceOrder;

using FluentAssertions;
using OrderFlow.Application.Orders.PlaceOrder;
using OrderFlow.Application.UnitTests.TestDoubles;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.Events;

public sealed class PlaceOrderCommandHandlerTests
{
    private readonly FakeOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private PlaceOrderCommandHandler Handler => new(_orders, _unitOfWork);

    private static PlaceOrderCommand ACommand(params PlaceOrderLine[] lines) =>
        new("CUST-1001", "USD", "12 Market St", "Istanbul", "34000", "TR",
            lines.Length == 0 ? [new PlaceOrderLine("SKU-1", 2, 19.99m)] : lines);

    [Fact]
    public async Task Handle_AddsThePlacedOrderAndCommitsOnce()
    {
        var id = await Handler.Handle(ACommand(), CancellationToken.None);

        var order = _orders.Added.Should().ContainSingle().Subject;
        order.Id.Value.Should().Be(id);
        order.Status.Should().Be(OrderStatus.Placed);
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MapsCustomerCurrencyAndAddress()
    {
        await Handler.Handle(ACommand(), CancellationToken.None);

        var order = _orders.Added[0];
        order.CustomerId.Should().Be("CUST-1001");
        order.Currency.Should().Be("USD");
        order.ShippingAddress.Line1.Should().Be("12 Market St");
        order.ShippingAddress.City.Should().Be("Istanbul");
        order.ShippingAddress.PostalCode.Should().Be("34000");
        order.ShippingAddress.Country.Should().Be("TR");
    }

    [Fact]
    public async Task Handle_AddsEveryLineAndComputesTheTotal()
    {
        await Handler.Handle(
            ACommand(new PlaceOrderLine("SKU-1", 2, 19.99m), new PlaceOrderLine("SKU-2", 1, 5.02m)),
            CancellationToken.None);

        var order = _orders.Added[0];
        order.Lines.Select(l => l.Sku).Should().Equal("SKU-1", "SKU-2");
        order.Total.Amount.Should().Be(45m);
    }

    [Fact]
    public async Task Handle_RaisesOrderPlacedForTheOutbox()
    {
        var id = await Handler.Handle(ACommand(), CancellationToken.None);

        var placed = _orders.Added[0].DomainEvents.OfType<OrderPlacedDomainEvent>().Should().ContainSingle().Subject;
        placed.OrderId.Should().Be(id);
        placed.Total.Should().Be(39.98m);
        placed.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WithADuplicateSku_ThrowsAndDoesNotCommit()
    {
        var command = ACommand(new PlaceOrderLine("SKU-1", 1, 10m), new PlaceOrderLine("SKU-1", 2, 10m));

        var act = () => Handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Error.Code.Should().Be("Order.DuplicateSku");
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithAnInvalidAddress_ThrowsBeforeTouchingTheRepository()
    {
        var command = new PlaceOrderCommand("CUST-1001", "USD", "  ", "Istanbul", "34000", "TR",
            [new PlaceOrderLine("SKU-1", 1, 10m)]);

        var act = () => Handler.Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.Error.Code.Should().Be("Address.Line1");
        _orders.Added.Should().BeEmpty();
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }
}
