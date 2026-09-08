
namespace OrderFlow.Application.Common.Models;


public sealed record PagedResult<T>(IReadOnlyCollection<T> items, int page, int pageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)pageSize);
}
