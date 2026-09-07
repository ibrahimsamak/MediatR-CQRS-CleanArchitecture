# 0001. Adopt Clean Architecture with an inward dependency rule

- Status: Accepted
- Date: 2026-09-05
- Deciders: <you>

## Context

`OrderFlow` starts as one Order service but is the reference implementation that
three more services (Payment, Inventory, Notification) will copy in Week 2. The
domain (orders, money, invariants) must stay stable while infrastructure choices
(SQL provider, cache, message broker) are expected to change. We need business
logic that is testable without a database or web host, and boundaries an analyzer
can enforce so the structure does not rot under deadline pressure.

## Decision

Use four projects with dependencies pointing **inward** toward the Domain:

- `Domain` references nothing (only `MediatR.Contracts` for the `INotification` marker).
- `Application` references `Domain`.
- `Infrastructure` references `Application` (+ `Domain`).
- `Api` references `Application` + `Infrastructure` and is the only composition root.

Interfaces (`IOrderRepository`, `IUnitOfWork`, `ICacheService`, `IOrderReadStore`)
are declared in the inner layers and implemented in `Infrastructure` (Dependency
Inversion). Boundaries are enforced by project references + analyzers-as-errors.

## Alternatives considered

- **Classic N-tier (UI → BLL → DAL).** Rejected: dependencies point _down_ toward
  the database, so the domain ends up coupled to EF Core and is hard to unit-test.
- **Vertical Slice Architecture.** Attractive and lower ceremony, but a bank
  interview expects the named layered model, and multiple services copying one
  reference benefit from an identical, obvious skeleton. Revisit if ceremony hurts.
- **Single project ("just an API").** Fastest to start, but the boundaries we need
  in Week 2 (per-service DB, independent deploy) would have to be retrofitted.

## Consequences

- (+) Domain is pure and unit-testable; infrastructure is swappable.
- (+) Every later service reuses the same, enforced structure.
- (−) More projects and indirection than a single-project API.
- (−) A small learning tax (where does this class go?) — mitigated by the folder map.
