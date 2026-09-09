namespace OrderFlow.Application.UnitTests.TestDoubles;

using OrderFlow.Domain.Orders;

/// <summary>In-memory <see cref="IOrderRepository"/> that records what a handler did to it.</summary>
internal sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _orders = [];

    public List<Order> Added { get; } = [];

    public int GetByIdCalls { get; private set; }

    public FakeOrderRepository Seed(Order order)
    {
        _orders[order.Id] = order;
        return this;
    }

    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        GetByIdCalls++;
        return Task.FromResult(_orders.GetValueOrDefault(id));
    }

    public void Add(Order order)
    {
        Added.Add(order);
        _orders[order.Id] = order;
    }
}
