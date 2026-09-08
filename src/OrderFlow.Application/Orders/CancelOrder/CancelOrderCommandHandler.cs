// Orders/CancelOrder/CancelOrderCommandHandler.cs
namespace OrderFlow.Application.Orders.CancelOrder;

using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Orders;

public sealed class CancelOrderCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand cmd, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(new OrderId(cmd.OrderId), cancellationToken)
                    ?? throw new NotFoundException(nameof(Order), cmd.OrderId);

        order.Cancel(cmd.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
