
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}