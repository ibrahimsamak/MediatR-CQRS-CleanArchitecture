# 2. CQRS with one database

Date: 2026-09-10

## Context

Writes need to go through the `Order` aggregate so the rules are enforced. Reads (get by id, paged list) only need flat data and don't need change tracking. I also want validation, logging and transactions handled the same way for every request.

## Decision

Split commands and queries at the code level, but keep one SQL Server database.

- Every operation is a MediatR request: a command or a query.
- Commands load and change the aggregate through `IOrderRepository` and save through `IUnitOfWork`. They implement `ITransactionalRequest`.
- Queries go through `IOrderReadStore` and project to DTOs with `AsNoTracking`. Get by id is cached in Redis.
- Pipeline behaviors: validation, then logging, then transaction.

## Other options

- A separate read database synced by events: too much for this size, and it brings eventual consistency. The outbox (ADR 3) would make it possible later.
- No mediator, just services: simpler, but every handler would repeat the same validation/logging/transaction code. Also note: newer MediatR versions need a commercial license for bigger companies.

## Consequences

Two models to maintain (DTOs and the aggregate), and with MediatR it's sometimes harder to find which handler runs.
