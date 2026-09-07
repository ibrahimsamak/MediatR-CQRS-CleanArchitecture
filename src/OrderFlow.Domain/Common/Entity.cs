
namespace OrderFlow.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public TId Id { get; protected init; }
    protected Entity(TId id) => Id = id;

    public bool Equals(Entity<TId>? other) => other is not null && GetType() == other.GetType() && Id.Equals(other.Id);
    public override bool Equals(object? obj) => obj is Entity<TId> e && Equals(e);
    public override int GetHashCode() => Id.GetHashCode();

}
