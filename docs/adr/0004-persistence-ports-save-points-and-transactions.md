# ADR 0004 — Persistence ports, save points and transactions

Status: accepted (2026-10-26)

## Context
The Application may not see EF Core, yet each use case must save once and atomically. A generic repository or an
`IUnitOfWork` that only forwards to `SaveChangesAsync` would add a layer without adding anything.

## Decision
* Ports are purpose-specific and owned by the module's Application: `IWarehouseRepository`,
  `IWarehouseReadStore`, `IProductRepository`, `IProductReadStore`. Read stores project to DTOs with `AsNoTracking`.
* **The repository write method is the use case's save point**: `AddAsync(entity)` / `SaveChangesAsync()` call
  `SaveChanges` once. EF Core's implicit transaction covers it, so no handler opens a transaction. Aggregates are
  created whole by the domain (warehouse + DEFAULT location, product + base unit row), so one save is enough.
* At the save point `DbFailures.SaveAsync` maps:
  * SQL 2601/2627 on a constraint the module lists (e.g. `IX_Products_SKU` → `Catalog.Product.SkuTaken`) to its
    business error; an unlisted constraint is rethrown and becomes `Common.Unexpected` (500);
  * `DbUpdateConcurrencyException` to `Common.ConcurrencyConflict` (409), without database details.
  Pre-checks in handlers give the same codes for the common case; the index decides when requests race.
* Optimistic concurrency uses the `RowVersion` column: an update loads the row and sets the caller's row version
  as the original value (`GetForUpdateAsync`), so a stale update fails at the database.

## Future: explicit transactions (Inventory) — designed, not built
A posting that changes balances, movements, reservations, cost and the outbox in one unit will use a port like:

```csharp
public interface ITransactionRunner
{
    // Runs work in one serializable-enough transaction under EF Core's execution strategy (retry-safe),
    // after taking sp_getapplock locks on the given resources in a fixed order.
    Task<Result<T>> RunAsync<T>(IReadOnlyList<string> lockResources, Func<CancellationToken, Task<Result<T>>> work, CancellationToken ct);
}
```

implemented over `ErpDbContext` (`Database.BeginTransactionAsync`, commit only when the result succeeds, rollback
otherwise). It will be added with the first use case that needs it, not before (see INVENTORY_DECISIONS.md for the
locking rules).

## Consequences
A handler that changes two aggregates must not call two saving repositories; it waits for `ITransactionRunner`.
Constraint names in the maps come from Migration 01; renaming an index means updating its map.
