namespace OrderFlow.Application.UnitTests.Orders.CancelOrder;

using FluentAssertions;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Orders.CancelOrder;
using OrderFlow.Application.UnitTests.TestDoubles;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.Events;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class CancelOrderCommandHandlerTests
{
    private readonly FakeOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CancelOrderCommandHandler Handler => new(_orders, _unitOfWork);

    private static Order APlacedOrder()
    {
        var order = Order.Create("CUST-1001", Address.Create("12 Market St", "Istanbul", "34000", "TR"), "USD");
        order.AddLine("SKU-1", 1, 10m);
        order.Place();
        order.ClearDomainEvents();
        return order;
    }

    [Fact]
    public async Task Handle_CancelsTheOrderAndCommits()
    {
        var order = APlacedOrder();
        _orders.Seed(order);

        await Handler.Handle(new CancelOrderCommand(order.Id.Value, "out of stock"), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RaisesOrderCancelledWithTheReason()
    {
        var order = APlacedOrder();
        _orders.Seed(order);

        await Handler.Handle(new CancelOrderCommand(order.Id.Value, "out of stock"), CancellationToken.None);

        var cancelled = order.DomainEvents.OfType<OrderCancelledDomainEvent>().Should().ContainSingle().Subject;
        cancelled.OrderId.Should().Be(order.Id.Value);
        cancelled.Reason.Should().Be("out of stock");
    }

    [Fact]
    public async Task Handle_WhenTheOrderIsMissing_ThrowsNotFoundAndDoesNotCommit()
    {
        var act = () => Handler.Handle(new CancelOrderCommand(Guid.CreateVersion7(), "any"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_OnAnAlreadyCancelledOrder_IsIdempotent()
    {
        var order = APlacedOrder();
        order.Cancel("first");
        order.ClearDomainEvents();
        _orders.Seed(order);

        await Handler.Handle(new CancelOrderCommand(order.Id.Value, "second"), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.DomainEvents.Should().BeEmpty();
    }
}
