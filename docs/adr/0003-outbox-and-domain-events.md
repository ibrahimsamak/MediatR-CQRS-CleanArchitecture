# 0003. Transactional Outbox for domain events (avoid the dual-write problem)

- Status: Accepted
- Date: 2026-09-05
- Deciders: <you>

## Context

Placing an order must both persist the aggregate **and** eventually publish an
`OrderPlaced` event that other services (Week 2) will consume. Writing to the DB
and publishing to a broker are two separate systems: if we commit the DB then the
broker call fails (or the process crashes in between), we get a lost event or a
phantom event — the classic **dual-write problem**. Banking/FinTech cannot lose or
duplicate order/payment events.

## Decision

Use the **Transactional Outbox** pattern, introduced now and wired to a broker in Week 2:

- Aggregates raise domain events (`OrderPlacedDomainEvent`, `OrderCancelledDomainEvent`)
  into an in-memory list; they carry no infrastructure concern.
- A `SaveChanges` **interceptor** harvests those events and inserts them as
  `OutboxMessage` rows **in the same transaction** as the aggregate — one atomic commit.
- A `BackgroundService` (`OutboxDispatcherService`) polls unprocessed rows and, in
  Week 1, logs + marks them processed. In Week 2 it publishes to Kafka before
  setting `ProcessedOnUtc`. Consumers dedupe (inbox) so at-least-once delivery is
  effectively-once.
- Keep **domain events** (inside the aggregate) distinct from **integration events**
  (what crosses service boundaries) — the outbox is the seam between them.

## Alternatives considered

- **Publish directly after commit (dual write).** Rejected: no atomicity; a crash
  between commit and publish loses the event.
- **Two-phase commit (2PC) across DB + broker.** Rejected: poor scalability, broker
  support is weak, and it is the anti-pattern this design exists to avoid.
- **Change Data Capture (Debezium on the DB log).** Powerful and avoids app code,
  but adds infrastructure (connectors) heavier than needed at this stage; revisit
  at scale.

## Consequences

- (+) No lost or phantom events; DB state and the outbox commit atomically.
- (+) Broker choice is deferred to Week 2; Week 1 already proves the mechanism.
- (+) Clean separation of domain vs integration events.
- (−) At-least-once delivery means consumers must be idempotent (inbox/dedup).
- (−) Polling adds a small dispatch latency and a background moving part to operate.
