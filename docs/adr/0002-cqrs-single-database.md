# 0002. CQRS as separate read/write models over a single database

- Status: Accepted
- Date: 2026-09-05
- Deciders: <you>

## Context

Reads and writes have different shapes. Writes must enforce invariants through the
`Order` aggregate; reads (get-by-id, paged list) just need fast, flat projections
and should not pay for change tracking. We also want cross-cutting concerns
(validation, logging, transactions) applied consistently to every operation. At
the same time, the data volume does not justify the operational cost of two
physical stores or eventual consistency between them.

## Decision

Apply **CQRS at the model level, not the storage level**:

- Every operation is a MediatR `IRequest` — a **Command** (write) or **Query** (read).
- Commands load/mutate the aggregate through `IOrderRepository` and commit via
  `IUnitOfWork`; they are marked `ITransactionalRequest`.
- Queries bypass the domain and read projections directly (`AsNoTracking`,
  `AsSplitQuery`) via `IOrderReadStore`, with Redis cache-aside for hot reads.
- Cross-cutting concerns are MediatR **pipeline behaviors** in a fixed order:
  Validation → Logging → Transaction → Handler.
- **One SQL Server database** backs both sides.

## Alternatives considered

- **Two databases / separate read store synced by events.** Rejected for now:
  adds eventual consistency, a projection pipeline, and ops burden we do not need
  yet. The outbox (ADR 0003) leaves the door open if a read store is added later.
- **No mediator — inject services directly.** Simpler, but each handler would repeat
  validation/logging/transaction wiring; the pipeline removes that duplication.
  (Trade-off noted: MediatR v13+ is commercially licensed for larger orgs.)

## Consequences

- (+) Read and write paths evolve independently; reads are cheap and cacheable.
- (+) Guaranteed, uniform validation/logging/transaction on every request.
- (−) Two models to maintain (DTO vs aggregate) even against one database.
- (−) MediatR indirection ("where is the handler?") and a licensing consideration.
