namespace OrderFlow.Domain.Orders;

using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders.Events;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    private Order(OrderId id) : base(id) { } 

    private Order(OrderId id, string customerId, Address shippingAddress, string currency) : base(id)
    {
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        Currency = currency;
        Status = OrderStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string CustomerId { get; private set; } = default!;
    public Address ShippingAddress { get; private set; } = default!;
    public string Currency { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // Optimistic concurrency token (mapped to SQL rowversion on Day 4).
    public byte[] Version { get; private set; } = [];

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public Money Total =>
        _lines.Count == 0
            ? Money.Zero(Currency)
            : _lines.Select(l => l.LineTotal).Aggregate((a, b) => a + b);

    // --- Factory: the only way to create a valid Order ---
    public static Order Create(string customerId, Address shippingAddress, string currency)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new DomainException(new CustError("Order.Customer", "CustomerId is required."));
        }
        return new Order(OrderId.New(), customerId, shippingAddress, currency);
    }

    public void AddLine(string sku, int quantity, decimal unitPrice)
    {
        EnsureMutable();

        var existing = _lines.FirstOrDefault(l => l.Sku == sku);
        if (existing is not null)
        {
            throw new DomainException(new CustError("Order.DuplicateSku", $"SKU {sku} already on the order."));
        }

        _lines.Add(new OrderLine(Guid.CreateVersion7(), sku, quantity, Money.Create(unitPrice, Currency)));
    }

    public void Place()
    {
        EnsureMutable();
        if (_lines.Count == 0)
            throw new DomainException(new CustError("Order.Empty", "Cannot place an order with no lines."));
   

        Status = OrderStatus.Placed;
        Raise(new OrderPlacedDomainEvent(Id.Value, Total.Amount, Currency));
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Cancelled)
            return; 

        Status = OrderStatus.Cancelled;
        Raise(new OrderCancelledDomainEvent(Id.Value, reason));
    }

    private void EnsureMutable()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException(new CustError("Order.Immutable", $"Order in status {Status} cannot be modified."));
        
    }
}
