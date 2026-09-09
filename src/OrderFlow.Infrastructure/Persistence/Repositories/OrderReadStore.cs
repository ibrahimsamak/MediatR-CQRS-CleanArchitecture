namespace OrderFlow.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;

public sealed class OrderReadStore(OrderDbContext db) : IOrderReadStore
{
    public async Task<PagedResult<OrderDto>> ListAsync(int page, int pageSize, string? status, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderFlow.Domain.Orders.OrderStatus>(status, true, out var s))
            query = query.Where(o => o.Status == s);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderDto(
                o.Id.Value,
                o.CustomerId,
                o.Status.ToString(),
                o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
                o.Currency,
                o.Lines.Select(l => new OrderLineDto(l.Sku, l.Quantity, l.UnitPrice.Amount, l.UnitPrice.Amount * l.Quantity)).ToList()))
            .ToListAsync(ct);

        return new PagedResult<OrderDto>(items, page, pageSize, total);
    }
}
