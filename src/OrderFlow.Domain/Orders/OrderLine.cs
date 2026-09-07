namespace OrderFlow.Domain.Orders;

using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class OrderLine : Entity<Guid>
{
    private OrderLine(Guid id) : base(id) { }

    public OrderLine(Guid id, string sku, int quantity, Money unitPrice) : base(id)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException(new CustError("OrderLine.Sku", "SKU is required."));
        }

        if (quantity <= 0)
        {
            throw new DomainException(new CustError("OrderLine.Quantity", "Quantity must be positive."));
        }

        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public string Sku { get; private set; } = default!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    public Money LineTotal => Money.Create(UnitPrice.Amount * Quantity, UnitPrice.Currency);
}
