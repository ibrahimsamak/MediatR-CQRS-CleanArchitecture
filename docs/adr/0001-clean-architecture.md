# 1. Clean Architecture

Date: 2026-09-10

## Context

I want the order logic testable without a database or web host, and I expect to swap infrastructure (SQL provider, cache, maybe a message broker later).

## Decision

Four projects, dependencies point inward:

- Domain doesn't reference other projects, only the MediatR package (for `INotification`).
- Application references Domain.
- Infrastructure references Application.
- Api references Application and Infrastructure and does all the wiring.

Interfaces like `IOrderRepository`, `IUnitOfWork`, `ICacheService` and `IOrderReadStore` live in the inner layers and are implemented in Infrastructure.

## Other options

- Classic N-tier: the domain ends up depending on EF and is harder to test.
- Vertical slices: less ceremony, and I might try it later. For this one I wanted the layered version.
- One project: fastest, but the boundaries are just folders.

## Consequences

More projects and more jumping between files, but the domain stays clean and easy to test.
