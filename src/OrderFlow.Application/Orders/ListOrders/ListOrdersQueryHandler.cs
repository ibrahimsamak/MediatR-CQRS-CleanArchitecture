
using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Orders.ListOrders;

public sealed class ListOrdersQueryHandler(IOrderReadStore readStore)
    : IRequestHandler<ListOrdersQuery, PagedResult<OrderDto>>
{
    public Task<PagedResult<OrderDto>> Handle(ListOrdersQuery q, CancellationToken cancellationToken) =>
        readStore.ListAsync(Math.Max(1, q.Page), Math.Clamp(q.PageSize, 1, 100), q.Status, cancellationToken);
}