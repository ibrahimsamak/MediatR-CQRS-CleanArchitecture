namespace OrderFlow.Domain.UnitTests.Common;

using FluentAssertions;
using OrderFlow.Domain.Common;

public sealed class EntityTests
{
    private sealed class Customer(Guid id) : Entity<Guid>(id)
    {
        public void RaiseSomething() => Raise(new SomethingHappened(Id));
    }

    private sealed class Supplier(Guid id) : Entity<Guid>(id);

    private sealed record SomethingHappened(Guid Id) : IDomainEvent;

    [Fact]
    public void Entities_OfTheSameTypeWithTheSameId_AreEqual()
    {
        var id = Guid.CreateVersion7();

        var left = new Customer(id);
        var right = new Customer(id);

        left.Should().Be(right);
        left.Equals((object)right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentIds_AreNotEqual()
    {
        new Customer(Guid.CreateVersion7()).Should().NotBe(new Customer(Guid.CreateVersion7()));
    }

    [Fact]
    public void Entities_OfDifferentTypesWithTheSameId_AreNotEqual()
    {
        var id = Guid.CreateVersion7();

        new Customer(id).Equals(new Supplier(id)).Should().BeFalse();
    }

    [Fact]
    public void Raise_AppendsToDomainEvents_AndClearEmptiesThem()
    {
        var customer = new Customer(Guid.CreateVersion7());

        customer.RaiseSomething();
        customer.RaiseSomething();

        customer.DomainEvents.Should().HaveCount(2);

        customer.ClearDomainEvents();

        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_CannotBeMutatedFromOutside()
    {
        var customer = new Customer(Guid.CreateVersion7());
        customer.RaiseSomething();

        var asCollection = (ICollection<IDomainEvent>)customer.DomainEvents;

        asCollection.IsReadOnly.Should().BeTrue();
    }
}
