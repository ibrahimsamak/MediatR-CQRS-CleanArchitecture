// Orders/GetOrderById/GetOrderByIdQueryHandler.cs
namespace OrderFlow.Application.Orders.GetOrderById;

using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Domain.Orders;

public sealed class GetOrderByIdQueryHandler(IOrderRepository orders, ICacheService cache)
    : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var key = $"order:{query.OrderId}";
        return await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(5), async token =>
        {
            var order = await orders.GetByIdAsync(new OrderId(query.OrderId), token)
                        ?? throw new NotFoundException(nameof(Order), query.OrderId);

            return new OrderDto(
                order.Id.Value,
                order.CustomerId,
                order.Status.ToString(),
                order.Total.Amount,
                order.Currency,
                order.Lines.Select(l => new OrderLineDto(l.Sku, l.Quantity, l.UnitPrice.Amount, l.LineTotal.Amount)).ToList());
        }, cancellationToken);
    }
}
