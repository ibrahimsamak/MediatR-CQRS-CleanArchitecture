// Orders/ValueObjects/Address.cs
namespace OrderFlow.Domain.Orders.ValueObjects;

using OrderFlow.Domain.Common;

public sealed class Address : ValueObject
{
    public string Line1 { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address(string line1, string city, string postalCode, string country)
    {
        Line1 = line1;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public static Address Create(string line1, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(line1))
        {
            throw new DomainException(new CustError("Address.Line1", "Line1 required."));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new DomainException(new CustError("Address.Country", "Country required."));
        }

        return new Address(line1.Trim(), city.Trim(), postalCode.Trim(), country.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }
}
