namespace OrderFlow.Api.Controllers.V1;

using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Contracts;
using OrderFlow.Api.Infrastructure;
using OrderFlow.Application.Orders.CancelOrder;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.GetOrderById;
using OrderFlow.Application.Orders.ListOrders;
using OrderFlow.Application.Orders.PlaceOrder;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
[Produces("application/json")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    /// <summary>Places a new order. Requires an Idempotency-Key header.</summary>
    [HttpPost]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var command = new PlaceOrderCommand(
            request.CustomerId, request.Currency, request.AddressLine1, request.City,
            request.PostalCode, request.Country,
            request.Lines.Select(l => new PlaceOrderLine(l.Sku, l.Quantity, l.UnitPrice)).ToList());

        var id = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, new { id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetOrderByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, CancellationToken ct = default) =>
        Ok(await sender.Send(new ListOrdersQuery(page, pageSize, status), ct));

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest request, CancellationToken ct)
    {
        await sender.Send(new CancelOrderCommand(id, request.Reason), ct);
        return NoContent();
    }
}
