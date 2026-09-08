// Orders/ListOrders/ListOrdersQuery.cs
namespace OrderFlow.Application.Orders.ListOrders;

using MediatR;
using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;

public sealed record ListOrdersQuery(int Page = 1, int PageSize = 20, string? Status = null)
    : IRequest<PagedResult<OrderDto>>;
