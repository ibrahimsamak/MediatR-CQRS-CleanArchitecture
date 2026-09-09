namespace OrderFlow.Application.UnitTests.Orders.GetOrderById;

using FluentAssertions;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.GetOrderById;
using OrderFlow.Application.UnitTests.TestDoubles;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class GetOrderByIdQueryHandlerTests
{
    private readonly FakeOrderRepository _orders = new();
    private readonly InMemoryCacheService _cache = new();

    private GetOrderByIdQueryHandler Handler => new(_orders, _cache);

    private static Order APlacedOrder()
    {
        var order = Order.Create("CUST-1001", Address.Create("12 Market St", "Istanbul", "34000", "TR"), "USD");
        order.AddLine("SKU-1", 2, 19.99m);
        order.AddLine("SKU-2", 1, 5.02m);
        order.Place();
        return order;
    }

    [Fact]
    public async Task Handle_OnAMiss_LoadsTheOrderAndMapsItToADto()
    {
        var order = APlacedOrder();
        _orders.Seed(order);

        var dto = await Handler.Handle(new GetOrderByIdQuery(order.Id.Value), CancellationToken.None);

        dto.Id.Should().Be(order.Id.Value);
        dto.CustomerId.Should().Be("CUST-1001");
        dto.Status.Should().Be(nameof(OrderStatus.Placed));
        dto.Currency.Should().Be("USD");
        dto.Total.Should().Be(45m);
        dto.Lines.Should().BeEquivalentTo(new[]
        {
            new OrderLineDto("SKU-1", 2, 19.99m, 39.98m),
            new OrderLineDto("SKU-2", 1, 5.02m, 5.02m)
        });
    }

    [Fact]
    public async Task Handle_OnAMiss_CachesUnderTheOrderKeyForFiveMinutes()
    {
        var order = APlacedOrder();
        _orders.Seed(order);

        await Handler.Handle(new GetOrderByIdQuery(order.Id.Value), CancellationToken.None);

        _cache.Keys.Should().ContainSingle().Which.Should().Be($"order:{order.Id.Value}");
        _cache.LastTtl.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Handle_OnAHit_ServesTheCacheWithoutTouchingTheRepository()
    {
        var order = APlacedOrder();
        _orders.Seed(order);
        var cached = new OrderDto(order.Id.Value, "CUST-CACHED", "Placed", 1m, "USD", []);
        _cache.Preload($"order:{order.Id.Value}", cached);

        var dto = await Handler.Handle(new GetOrderByIdQuery(order.Id.Value), CancellationToken.None);

        dto.Should().BeSameAs(cached);
        _orders.GetByIdCalls.Should().Be(0);
        _cache.FactoryInvocations.Should().Be(0);
    }

    [Fact]
    public async Task Handle_HitsTheDatabaseOnlyOnceAcrossRepeatedReads()
    {
        var order = APlacedOrder();
        _orders.Seed(order);
        var query = new GetOrderByIdQuery(order.Id.Value);

        await Handler.Handle(query, CancellationToken.None);
        await Handler.Handle(query, CancellationToken.None);
        await Handler.Handle(query, CancellationToken.None);

        _orders.GetByIdCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenTheOrderIsMissing_ThrowsNotFoundAndCachesNothing()
    {
        var missingId = Guid.CreateVersion7();

        var act = () => Handler.Handle(new GetOrderByIdQuery(missingId), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>())
            .WithMessage($"*{missingId}*");
        _cache.Keys.Should().BeEmpty();
    }
}
