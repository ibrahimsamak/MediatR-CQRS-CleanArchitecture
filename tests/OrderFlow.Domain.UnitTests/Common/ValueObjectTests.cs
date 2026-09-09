namespace OrderFlow.Domain.UnitTests.Common;

using FluentAssertions;
using OrderFlow.Domain.Common;

public sealed class ValueObjectTests
{
    private sealed class Point(int x, int y) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return x;
            yield return y;
        }
    }

    private sealed class Label(string? text) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return text;
        }
    }

    [Fact]
    public void ValueObjects_WithTheSameComponents_AreEqual()
    {
        var left = new Point(1, 2);
        var right = new Point(1, 2);

        left.Should().Be(right);
        left.Equals((object)right).Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void ValueObjects_WithDifferentComponents_AreNotEqual()
    {
        new Point(1, 2).Should().NotBe(new Point(2, 1));
    }

    [Fact]
    public void ValueObject_IsNotEqualToNull()
    {
        new Point(1, 2).Equals(null).Should().BeFalse();
    }

    [Fact]
    public void NullComponents_AreHandled()
    {
        var left = new Label(null);
        var right = new Label(null);

        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
        left.Should().NotBe(new Label("x"));
    }
}
