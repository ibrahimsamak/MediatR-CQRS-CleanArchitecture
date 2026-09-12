namespace OrderFlow.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain.Orders;

public sealed class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default) =>
        await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public void Add(Order order) => db.Orders.Add(order);
}
