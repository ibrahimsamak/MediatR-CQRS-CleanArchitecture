
using MediatR;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Orders;
using OrderFlow.Domain.Orders.ValueObjects;

namespace OrderFlow.Application.Orders.PlaceOrder;

public sealed class PlaceOrderCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IRequestHandler<PlaceOrderCommand, Guid>
{
    public async Task<Guid> Handle(PlaceOrderCommand cmd, CancellationToken cancellationToken)
    {
        var address = Address.Create(cmd.AddressLine1, cmd.City, cmd.PostalCode, cmd.Country);
        var order = Order.Create(cmd.CustomerId, address, cmd.Currency);

        foreach (var line in cmd.Lines)
        {
            order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
        }

        order.Place();
        orders.Add(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.Id.Value;
    }
}
