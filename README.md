# OrderFlow

A production-shaped **.NET 10** order service built as a reference implementation of
**MediatR + CQRS over Clean Architecture**, with a rich domain model, a transactional
outbox for domain events, and the cross-cutting concerns (validation, transactions,
idempotency, caching, ProblemDetails) wired in as pipeline behaviors rather than
scattered through controllers.

---

## Table of contents

- [Why this exists](#why-this-exists)
- [Architecture](#architecture)
- [The request pipeline](#the-request-pipeline)
- [CQRS: two models, one database](#cqrs-two-models-one-database)
- [Domain model](#domain-model)
- [Transactional outbox](#transactional-outbox)
- [API](#api)
- [Getting started](#getting-started)
- [Project layout](#project-layout)
- [Tech stack](#tech-stack)
- [Design decisions](#design-decisions)

---

## Why this exists

Most CQRS samples stop at "a handler per endpoint". OrderFlow goes further and shows
the parts that decide whether the pattern survives contact with production:

| Concern | How it is solved here |
| --- | --- |
| Business rules leaking into services | Invariants live inside the `Order` aggregate; handlers only orchestrate |
| Repetitive validation / logging / transaction code | MediatR `IPipelineBehavior` chain, applied uniformly to every request |
| Slow, over-fetching reads | A separate read path — flat DTO projections, `AsNoTracking`, Redis cache-aside |
| Lost or duplicated integration events | Transactional outbox written in the same commit as the aggregate |
| A retried `POST` creating duplicate orders | `Idempotency-Key` filter that replays the cached response |
| Exceptions turning into opaque 500s | A global `IExceptionHandler` mapping to RFC 7807 ProblemDetails |
| Concurrent edits silently overwriting each other | SQL `rowversion` optimistic concurrency, surfaced as `409 Conflict` |

## Architecture

Four projects, dependencies pointing **inward**. Nothing in the inner rings knows the
outer ones exist.

```
        ┌────────────────────────────────────────────────┐
        │  OrderFlow.Api  (composition root)             │  controllers, filters,
        │                                                │  ProblemDetails, health
        └──────────────┬───────────────────┬─────────────┘
                       │                   │
        ┌──────────────▼─────────────┐     │
        │  OrderFlow.Infrastructure  │     │  EF Core, Redis, outbox dispatcher
        └──────────────┬─────────────┘     │
                       │                   │
        ┌──────────────▼───────────────────▼─────────────┐
        │  OrderFlow.Application                         │  commands, queries,
        │  commands · queries · behaviors · contracts    │  pipeline behaviors
        └──────────────┬─────────────────────────────────┘
                       │
        ┌──────────────▼─────────────────────────────────┐
        │  OrderFlow.Domain                              │  aggregates, value objects,
        │  no infrastructure dependencies                │  domain events, invariants
        └────────────────────────────────────────────────┘
```

Interfaces are declared where they are **used** and implemented where the technology
lives: `IOrderRepository` in `Domain`, `IUnitOfWork` / `ICacheService` / `IOrderReadStore`
in `Application`, all implemented in `Infrastructure`. The API project is the only place
that knows about every layer, and it exists to compose them:

```csharp
builder.Services.AddApplication();                         // MediatR, validators, behaviors
builder.Services.AddInfrastructure(builder.Configuration);  // EF Core, Redis, outbox, clock
```

## The request pipeline

Every operation — read or write — is a MediatR request and travels the same chain.
Behavior order is fixed and intentional:

```
HTTP request
   │
   ├─ IdempotencyFilter        writes only: replays the cached response for a repeated key
   │
   ▼
ISender.Send(request)
   │
   ├─ ValidationBehavior       FluentValidation → ValidationException (400 + per-field errors)
   ├─ LoggingBehavior          request name + elapsed ms, via source-generated LoggerMessage
   ├─ TransactionBehavior      opt-in through ITransactionalRequest; the EF execution strategy
   │                           retries transient SQL faults around a single transaction
   ▼
Handler
   │
   ▼
GlobalExceptionHandler → ProblemDetails (400 / 404 / 409 / 500)
```

A command opts into a transaction by declaring a marker interface — no attribute
scanning, no base class:

```csharp
public sealed record PlaceOrderCommand(...) : IRequest<Guid>, ITransactionalRequest;
```

Queries simply omit the marker and skip the transaction entirely.

## CQRS: two models, one database

CQRS is applied at the **model** level, not the storage level — one SQL Server database
backs both sides, so there is no eventual consistency to reason about.

**Write path** — through the aggregate, so invariants always hold:

```csharp
var order = Order.Create(cmd.CustomerId, address, cmd.Currency);
foreach (var line in cmd.Lines) order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
order.Place();                            // raises OrderPlacedDomainEvent
orders.Add(order);
await unitOfWork.SaveChangesAsync(ct);    // aggregate + outbox row in one commit
```

**Read path** — straight to flat projections: no change tracking, no aggregate loading,
no lazy-loaded graph:

```csharp
var query = db.Orders.AsNoTracking().AsSplitQuery();
...
.Select(o => new OrderDto(...))           // projected in SQL, not in memory
```

Hot single-order reads go through Redis cache-aside guarded by a per-key in-process
semaphore, so a cache miss under load costs **one** database round-trip instead of a
stampede:

```csharp
return await cache.GetOrCreateAsync($"order:{id}", TimeSpan.FromMinutes(5), async ct => { ... });
```

## Domain model

`Order` is a real aggregate root, not an anonymous property bag:

- **Private constructors and a `Create` factory** — an invalid `Order` cannot be
  constructed.
- **Encapsulated collection** — `Lines` is an `IReadOnlyList` over a private backing
  field; lines are added only through `AddLine`, which rejects duplicate SKUs.
- **Explicit state machine** — `EnsureMutable()` blocks changes once an order leaves
  `Pending`, and `Cancel` is idempotent.
- **Value objects** — `Money` (amount + currency, with currency-mismatch protection),
  `Address`, and a strongly-typed `OrderId` mapped to a `Guid` by a value converter.
- **Domain events** — raised into an in-memory list on the aggregate, which never
  touches a message broker itself.
- **Computed state** — `Total` is derived from the lines and deliberately unmapped.

EF Core adapts to the model rather than the other way round: owned types for `Address`
and `Money`, an owned collection for `OrderLines`, field access for the backing list,
enum-to-string conversion, and `IsRowVersion()` for optimistic concurrency.

## Transactional outbox

Committing to the database and publishing to a broker are two different systems; doing
both naively is the dual-write problem, and it loses events. Instead:

```
order.Place()
   └─ raises OrderPlacedDomainEvent (in memory, on the aggregate)
        │
SaveChangesAsync
   └─ ConvertDomainEventsToOutboxInterceptor
        ├─ serializes each domain event into an OutboxMessage row
        └─ COMMIT  ← aggregate rows and outbox rows are atomic
                     │
OutboxDispatcherService (BackgroundService, 10s PeriodicTimer)
   └─ takes unprocessed rows oldest-first, dispatches them, stamps ProcessedOnUtc
```

Because the outbox row is written inside the aggregate's own transaction, an event can
never be lost after a successful commit and never published for a rolled-back one.
Delivery is at-least-once by design, so consumers are expected to dedupe. Domain events
(internal to the aggregate) and integration events (what crosses a service boundary)
stay distinct — the outbox is the seam between them.

## API

Versioned by URL segment (`Asp.Versioning`), documented with Swagger, responses
compressed, output caching enabled.

| Method | Route | Notes |
| --- | --- | --- |
| `POST` | `/api/v1/orders` | Places an order. **Requires an `Idempotency-Key` header.** Returns `201` + id |
| `GET` | `/api/v1/orders/{id}` | Single order; Redis cache-aside, 5-minute TTL |
| `GET` | `/api/v1/orders?page=1&pageSize=20&status=Placed` | Paged, filterable projection |
| `POST` | `/api/v1/orders/{id}/cancel` | Cancels an order; returns `204` |
| `GET` | `/health/live` | Liveness — the process is up |
| `GET` | `/health/ready` | Readiness — SQL Server + Redis, with per-check JSON detail |

Failures come back as RFC 7807 ProblemDetails:

| Exception | Status | Body |
| --- | --- | --- |
| `ValidationException` | `400` | Title plus an `errors` extension keyed by property |
| `NotFoundException` | `404` | Resource not found |
| `DomainException` | `409` | Domain rule violated |
| `DbUpdateConcurrencyException` | `409` | Concurrent update conflict |
| anything else | `500` | Generic title; the detail stays in the logs |

Example:

```bash
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 7f1c9e04-2f6c-4f1a-9a3f-6b1f1f1a2b3c" \
  -d '{
        "customerId": "CUST-1001",
        "currency": "USD",
        "addressLine1": "12 Market St",
        "city": "Istanbul",
        "postalCode": "34000",
        "country": "TR",
        "lines": [ { "sku": "SKU-1", "quantity": 2, "unitPrice": 19.99 } ]
      }'
```

Replaying that exact call with the same `Idempotency-Key` returns the original response
instead of creating a second order.

## Getting started

**Prerequisites:** .NET 10 SDK and Docker.

```bash
# 1. start SQL Server and Redis
docker compose up -d

# 2. run the API (pending migrations are applied automatically in Development)
dotnet run --project src/OrderFlow.Api

# 3. open Swagger at the URL printed in the console, e.g.
#    https://localhost:7001/swagger
```

Run the tests:

```bash
dotnet test
```

Connection strings live in `appsettings.json` and match the compose file
(`localhost,1433` for SQL Server, `localhost:6379` for Redis). Override them with
user-secrets or environment variables anywhere else.

## Project layout

```
src/
  OrderFlow.Domain/              aggregates, value objects, domain events, invariants
    Common/                      Entity, AggregateRoot, ValueObject, IDomainEvent
    Orders/                      Order, OrderLine, OrderId, OrderStatus, IOrderRepository
      ValueObjects/              Money, Address
      Events/                    OrderPlaced, OrderCancelled

  OrderFlow.Application/         use cases and cross-cutting behavior
    Common/Behaviors/            Validation, Logging, Transaction
    Common/Interfaces/           IUnitOfWork, ICacheService, IOrderReadStore, IDateTimeProvider
    Orders/PlaceOrder/           command + validator + handler
    Orders/CancelOrder/          command + handler
    Orders/GetOrderById/         query + handler (cached)
    Orders/ListOrders/           query + handler (paged projection)

  OrderFlow.Infrastructure/      technology choices, all replaceable
    Persistence/                 OrderDbContext, configurations, repositories, UnitOfWork
    Persistence/Interceptors/    domain events → outbox, inside the same transaction
    Persistence/Outbox/          OutboxMessage
    BackgroundJobs/              OutboxDispatcherService
    Caching/                     RedisCacheService (single-flight cache-aside)
    Time/                        SystemDateTimeProvider

  OrderFlow.Api/                 composition root
    Controllers/V1/              OrdersController
    Contracts/                   request records
    Infrastructure/              GlobalExceptionHandler, IdempotencyFilter
    Extensions/                  health checks

tests/                           domain, application, and API integration tests
bench/                           BenchmarkDotNet harness
docs/adr/                        architecture decision records
```

## Tech stack

| Area | Choice |
| --- | --- |
| Runtime | .NET 10, latest C#, nullable enabled, warnings as errors |
| Mediation | MediatR — commands, queries, pipeline behaviors |
| Validation | FluentValidation, executed as a behavior |
| Persistence | EF Core 10 + SQL Server, owned types, `rowversion` concurrency |
| Caching | Redis through `IDistributedCache` |
| API | ASP.NET Core controllers, `Asp.Versioning`, Swashbuckle, ProblemDetails |
| Resilience | `Microsoft.Extensions.Http.Resilience` standard handler, EF execution strategy |
| Observability | Live/ready health checks, source-generated structured logging |
| Testing | xUnit, FluentAssertions |

Quality gates are enforced at build time from `Directory.Build.props`:
`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, and the latest recommended
analyzer set.

## Design decisions

The reasoning behind the three structural choices — including the alternatives that
were rejected and the trade-offs accepted — is recorded as ADRs:

- [0001 — Clean Architecture with an inward dependency rule](docs/adr/0001-clean-architecture.md)
- [0002 — CQRS as separate read/write models over a single database](docs/adr/0002-cqrs-single-database.md)
- [0003 — Transactional outbox for domain events](docs/adr/0003-outbox-and-domain-events.md)
