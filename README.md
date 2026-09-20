# OrderFlow

Small order service in .NET 10 that I use to practice Clean Architecture with MediatR and CQRS. It places, cancels and lists orders. The interesting parts are around the edges: pipeline behaviors, the outbox and the idempotent POST.

## Projects

- `OrderFlow.Domain`: the `Order` aggregate, `OrderLine`, value objects (`Money`, `Address`, `OrderId`) and domain events. No references to EF or anything else.
- `OrderFlow.Application`: commands/queries + handlers, FluentValidation validators and the MediatR behaviors.
- `OrderFlow.Infrastructure`: EF Core (SQL Server), repositories, Redis cache, the outbox interceptor and the background dispatcher.
- `OrderFlow.Api`: controllers, the global exception handler, the idempotency filter and health checks.
- `tests/`: unit tests for the domain and application layers (xUnit + FluentAssertions). The integration test project is still empty.

There are short notes on the main decisions in `docs/adr`.

## How a request goes through

Every endpoint sends a MediatR request. The behaviors run in this order:

1. `ValidationBehavior`: runs the FluentValidation validators and throws `ValidationException` (400) if something fails.
2. `LoggingBehavior`: logs the request name and how long it took.
3. `TransactionBehavior`: only for requests that implement `ITransactionalRequest` (the commands). It wraps the handler in a transaction inside the EF execution strategy, so transient SQL errors get retried.

Queries don't go through the aggregate. They project straight to DTOs with `AsNoTracking`, and get-by-id uses Redis (cache-aside, 5 min).

`GlobalExceptionHandler` turns exceptions into ProblemDetails: validation 400, not found 404, domain rule or concurrency conflict 409, anything else 500.

## Outbox

When an order is placed or cancelled, the aggregate raises a domain event. On `SaveChanges`, an interceptor turns those events into `OutboxMessage` rows in the same transaction. `OutboxDispatcherService` checks every 10 seconds, takes up to 20 unprocessed messages and marks them processed. Nothing is published yet, there is no broker.

## API

| | |
| --- | --- |
| `POST /api/v1/orders` | place an order, needs an `Idempotency-Key` header |
| `GET /api/v1/orders/{id}` | get one order |
| `GET /api/v1/orders?page=1&pageSize=20&status=Placed` | list orders |
| `POST /api/v1/orders/{id}/cancel` | cancel |
| `GET /health/live`, `GET /health/ready` | health (ready checks SQL Server and Redis) |

If you send the same POST again with the same `Idempotency-Key`, you get the first response back and no new order is created.

```bash
curl -X POST https://localhost:7001/api/v1/orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 7f1c9e04-2f6c-4f1a-9a3f-6b1f1f1a2b3c" \
  -d '{"customerId":"CUST-1001","currency":"USD","addressLine1":"12 Market St","city":"Istanbul","postalCode":"34000","country":"TR","lines":[{"sku":"SKU-1","quantity":2,"unitPrice":19.99}]}'
```

## Running it

```bash
docker compose up -d
dotnet run --project src/OrderFlow.Api
```

Swagger is at `/swagger` in Development. The connection strings in `appsettings.json` match the compose file.

I haven't committed migrations yet, so add one before the first run (in Development the API applies migrations on startup):

```bash
dotnet ef migrations add Initial --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
```

Tests:

```bash
dotnet test
```

## TODO

- migrations
- integration tests with Testcontainers
- publish outbox messages to a real broker
- the benchmark project is just a placeholder for now
