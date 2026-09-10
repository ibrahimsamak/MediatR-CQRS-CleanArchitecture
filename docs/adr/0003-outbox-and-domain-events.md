# 3. Outbox for domain events

Date: 2026-09-10

## Context

Placing an order has to save the order and also publish an event for other services. If I save to the database and then publish to a broker as two separate steps, a crash in between loses the event, or publishes one for data that never got saved.

## Decision

Use a transactional outbox.

- The aggregate adds domain events (`OrderPlacedDomainEvent`, `OrderCancelledDomainEvent`) to an in-memory list.
- A `SaveChanges` interceptor converts them to `OutboxMessage` rows in the same transaction as the order.
- `OutboxDispatcherService` (a `BackgroundService`) reads unprocessed rows and marks them processed. Right now that is all it does. Later it should publish to a broker (probably Kafka) before marking them.

## Other options

- Publish right after commit: not atomic.
- 2PC between the database and the broker: no.
- CDC with Debezium: works, but it's a lot of extra infrastructure for this.

## Consequences

Delivery is at-least-once, so consumers need to handle duplicates. Polling also adds a few seconds of delay.
