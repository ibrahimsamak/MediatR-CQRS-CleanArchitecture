
namespace OrderFlow.Domain.Common;

public sealed record CustError(string Code, string Message)
{
    public static readonly CustError None = new(string.Empty, string.Empty);
}

public sealed class DomainException(CustError error) : Exception(error.Message)
{
    public CustError Error { get; } = error;
}