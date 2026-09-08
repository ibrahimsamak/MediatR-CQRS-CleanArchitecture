
using MediatR;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Orders.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;

