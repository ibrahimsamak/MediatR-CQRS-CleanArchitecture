namespace OrderFlow.Application.UnitTests.Orders.PlaceOrder;

using FluentAssertions;
using OrderFlow.Application.Orders.PlaceOrder;

public sealed class PlaceOrderCommandValidatorTests
{
    private readonly PlaceOrderCommandValidator _validator = new();

    private static PlaceOrderCommand Valid() =>
        new("CUST-1001", "USD", "12 Market St", "Istanbul", "34000", "TR",
            [new PlaceOrderLine("SKU-1", 2, 19.99m)]);

    [Fact]
    public void AValidCommand_Passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingCustomerId_Fails()
    {
        var result = _validator.Validate(Valid() with { CustomerId = "" });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(PlaceOrderCommand.CustomerId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void ACurrencyThatIsNotThreeLetters_Fails(string currency)
    {
        var result = _validator.Validate(Valid() with { Currency = currency });

        result.Errors.Should().Contain(e => e.PropertyName == nameof(PlaceOrderCommand.Currency));
    }

    [Fact]
    public void MissingAddressLine1_Fails()
    {
        var result = _validator.Validate(Valid() with { AddressLine1 = "" });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(PlaceOrderCommand.AddressLine1));
    }

    [Fact]
    public void MissingCountry_Fails()
    {
        var result = _validator.Validate(Valid() with { Country = "" });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(PlaceOrderCommand.Country));
    }

    [Fact]
    public void AnOrderWithNoLines_Fails()
    {
        var result = _validator.Validate(Valid() with { Lines = [] });

        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(PlaceOrderCommand.Lines));
    }

    [Fact]
    public void ALineWithoutASku_Fails()
    {
        var result = _validator.Validate(Valid() with { Lines = [new PlaceOrderLine("", 1, 10m)] });

        result.Errors.Should().ContainSingle(e => e.PropertyName == "Lines[0].Sku");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ALineWithANonPositiveQuantity_Fails(int quantity)
    {
        var result = _validator.Validate(Valid() with { Lines = [new PlaceOrderLine("SKU-1", quantity, 10m)] });

        result.Errors.Should().ContainSingle(e => e.PropertyName == "Lines[0].Quantity");
    }

    [Fact]
    public void ALineWithANegativeUnitPrice_Fails()
    {
        var result = _validator.Validate(Valid() with { Lines = [new PlaceOrderLine("SKU-1", 1, -0.01m)] });

        result.Errors.Should().ContainSingle(e => e.PropertyName == "Lines[0].UnitPrice");
    }

    [Fact]
    public void EveryFailure_IsReportedInOnePass()
    {
        var invalid = new PlaceOrderCommand("", "US", "", "Istanbul", "34000", "", []);

        var result = _validator.Validate(invalid);

        result.Errors.Select(e => e.PropertyName).Should().Contain(
        [
            nameof(PlaceOrderCommand.CustomerId),
            nameof(PlaceOrderCommand.Currency),
            nameof(PlaceOrderCommand.AddressLine1),
            nameof(PlaceOrderCommand.Country),
            nameof(PlaceOrderCommand.Lines)
        ]);
    }
}
