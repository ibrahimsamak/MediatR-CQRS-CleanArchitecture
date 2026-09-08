
using MediatR;
using OrderFlow.Application.Common.Behaviors;

namespace OrderFlow.Application.Orders.PlaceOrder;

public sealed record PlaceOrderLine(string Sku, int Quantity, decimal UnitPrice);
public sealed record PlaceOrderCommand(
    string CustomerId,
    string Currency,
    string AddressLine1,
    string City,
    string PostalCode,
    string Country,
    IReadOnlyList<PlaceOrderLine> Lines)
    : IRequest<Guid>, ITransactionalRequest;