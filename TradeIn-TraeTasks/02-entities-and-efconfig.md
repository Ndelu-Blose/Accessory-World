# Entities & EF Core Configuration

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Goal
Define EF Core entities and Fluent configurations with concurrency tokens and value conversions.

## Steps
1. Entities: `TradeIn`, `CreditNote` with enums for Status.
2. Use `RowVersion` as `[Timestamp]` for optimistic concurrency.
3. Configure enum-to-string conversions for `Status`.
4. Seed `MovementType` enum values if needed.

## Acceptance
- `dotnet build` succeeds.
- `Add-Migration` produces no noisy diffs after first creation.
