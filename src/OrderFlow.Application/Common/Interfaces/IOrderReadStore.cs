using OrderFlow.Application.Common.Models;
using OrderFlow.Application.Orders.Dtos;

namespace OrderFlow.Application.Common.Interfaces;

public interface IOrderReadStore
{
    Task<PagedResult<OrderDto>> ListAsync(int page, int pageSize, string? status, CancellationToken ct);
}
