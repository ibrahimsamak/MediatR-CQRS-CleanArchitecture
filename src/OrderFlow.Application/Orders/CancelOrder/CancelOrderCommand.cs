
using MediatR;
using OrderFlow.Application.Common.Behaviors;

namespace OrderFlow.Application.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest, ITransactionalRequest;

