namespace OrderFlow.Domain.UnitTests.Orders.ValueObjects;

using FluentAssertions;
using OrderFlow.Domain.Common;
using OrderFlow.Domain.Orders.ValueObjects;

public sealed class AddressTests
{
    [Fact]
    public void Create_WithValidParts_ReturnsAddress()
    {
        var address = Address.Create("12 Market St", "Istanbul", "34000", "TR");

        address.Line1.Should().Be("12 Market St");
        address.City.Should().Be("Istanbul");
        address.PostalCode.Should().Be("34000");
        address.Country.Should().Be("TR");
    }

    [Fact]
    public void Create_TrimsEveryPart()
    {
        var address = Address.Create("  12 Market St  ", "  Istanbul ", " 34000 ", " TR ");

        address.Line1.Should().Be("12 Market St");
        address.City.Should().Be("Istanbul");
        address.PostalCode.Should().Be("34000");
        address.Country.Should().Be("TR");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutLine1_Throws(string line1)
    {
        var act = () => Address.Create(line1, "Istanbul", "34000", "TR");

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Address.Line1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutCountry_Throws(string country)
    {
        var act = () => Address.Create("12 Market St", "Istanbul", "34000", country);

        act.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("Address.Country");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = Address.Create("12 Market St", "Istanbul", "34000", "TR");
        var b = Address.Create("12 Market St", "Istanbul", "34000", "TR");
        var other = Address.Create("13 Market St", "Istanbul", "34000", "TR");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(other);
    }
}
