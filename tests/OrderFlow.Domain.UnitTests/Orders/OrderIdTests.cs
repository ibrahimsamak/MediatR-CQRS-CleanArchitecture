namespace OrderFlow.Domain.UnitTests.Orders;

using FluentAssertions;
using OrderFlow.Domain.Orders;

public sealed class OrderIdTests
{
    [Fact]
    public void New_ProducesDistinctNonEmptyIds()
    {
        var first = OrderId.New();
        var second = OrderId.New();

        first.Value.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
    }

    [Fact]
    public void Equality_IsByWrappedValue()
    {
        var value = Guid.CreateVersion7();

        new OrderId(value).Should().Be(new OrderId(value));
        (new OrderId(value) == new OrderId(value)).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsTheWrappedGuid()
    {
        var value = Guid.CreateVersion7();

        new OrderId(value).ToString().Should().Be(value.ToString());
    }
}
