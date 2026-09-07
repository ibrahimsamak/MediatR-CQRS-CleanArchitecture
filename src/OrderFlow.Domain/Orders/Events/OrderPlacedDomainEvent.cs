// Orders/Events/OrderPlacedDomainEvent.cs
namespace OrderFlow.Domain.Orders.Events;

using OrderFlow.Domain.Common;

public sealed record OrderPlacedDomainEvent(Guid OrderId, decimal Total, string Currency) : IDomainEvent;
