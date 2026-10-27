# Inventory — Accepted Decisions and Invariants

> Design contract for future Inventory implementation, **not** authorization to build Inventory during the current architecture refactor.
> Scope: trading and distribution only, no manufacturing. Confirm accounting policies before finalizing valuation and external tax integrations.

## Organization and catalog
- Company -> Branch -> Warehouse -> Location. A warehouse belongs to one branch and has an automatically created default location.
- **Integration requirement, before stock operations go live:** deactivating a warehouse or a location that holds stock must be refused. Organization's deactivate endpoints (ADR 0013) don't check stock today; Inventory supplies the check through a contract, without Organization referencing Inventory internals.
- A Product is independent of Warehouse. Multiple barcodes and units per product; base unit is defined by **`Product.BaseUnitId` only**. The matching `ProductUnit` row has conversion factor 1; do not add `ProductUnit.IsBaseUnit`.
- Product tracking mode defined from the start (None/Batch/Serial); product and unit display names in Arabic and English.
- SKU is separate from tax/e-invoicing item codes such as EGS/GS1. Tax rules and effective dates are modeled separately.

## Stock quantity and reservation
- `StockBalances` is quantity by Product, Warehouse, **non-null Location**, optional Lot and StockStatus.
- `ProductWarehouseStock` holds **OnHandSellable** and **ReservedQuantity** per Product + Warehouse and is the serialization point for every operation that can reduce availability, not only reservations.
- Reservation is Product + Warehouse; allocate actual locations/lots later. Allocation has FromLocationId and ToLocationId (nullable by operation); posted movement always has a non-null LocationId.
- Do **not** add a database check requiring `ReservedQuantity <= OnHandSellable`. Reservations and ordinary outbound issues use atomic conditional updates, but authorized loss/damage/count adjustments may leave `ReservedQuantity > OnHandSellable`. Raise and record **OverReserved** for resolution, don't silently drop reservations.
- Preserve `OnHandSellable >= 0` and `ReservedQuantity >= 0`; negative stock is not the normal posting policy.
- Serialize stock-changing transactions with deterministic resource order; handle deadlocks/retries safely and apply idempotency.
- For SQL Server unique balances including nullable LotId, configure an **unfiltered** unique index (`HasFilter(null)` in EF Core) to avoid unexpected EF provider nullable-column filters.
- Reconciliation job checks `OnHandSellable` against the sum of sellable StockBalances for each physical, selling-enabled warehouse and checks reserved total against active remaining reservations; capture consistent snapshots. Flag inconsistencies, don't silently rewrite balances.

## Transfers and counts
- A Transit warehouse is never sellable and **never contributes to OnHandSellable**. Each dispatched transfer has its own auto-created Transit Location; partial receipt leaves remaining stock there.
- V1 uses StockTransfers/StockTransferLines plus posted documents/movements for receipt history. Close-with-discrepancy creates an approved adjustment from the Transit Location for loss/damage; do not just clear a numeric field.
- Stock counts freeze active scopes in `StockCountScope` only, **not** `WarehouseLocation.IsFrozen`.
- Use `sp_getapplock` with a stable warehouse resource name. Beginning a count obtains an Exclusive, transaction-owned lock to persist the freeze/snapshot. Each posting uses a Shared transaction-owned lock, then checks persistent frozen scopes. The SQL lock is **not** held throughout the physical count.
- Count differences result in a posted adjustment document; no direct edit to stock balances.

## Valuation and audit
- `StockMovement` is a quantity ledger and has **no UnitCost or ValueDelta**. Financial values belong in `InventoryCostEntry`.
- Tentative policy: company-wide moving weighted average in base currency; confirm with accountant before building Costing. Outbound issues must reduce InventoryCostState.TotalQuantity and TotalValue (even if AverageUnitCost stays the same).
- Unit price and calculated average unit cost precision: `decimal(19,8)`; define rounding rules before financial posting.
- V1 does not post movements to historical business dates. Record the real event timestamp separately when syncing offline POS actions.
- Document approvals are configurable by DocumentType. Explicitly permitted documents sourced from other modules may post directly Draft -> Posted, subject to permissions and invariants.
- Final document numbers are allocated via a transactional counter at posting (scope: document type + branch + fiscal year); draft references are separate.
- Temporal Tables for master data; operational AuditLog for actions. Failed Outbox/accounting entries need monitoring and a reconciliation report matching inventory valuation to the general ledger.

## Tests required before inventory production use
- Last units: concurrent direct POS issue vs sales reservation — only a feasible combination can succeed.
- Count starts concurrently with posting — no stock mutation bypasses the persisted freeze and snapshot.
- Transfer partial receipt + transit loss adjustment — final transit balance reconciles to zero.
- Receipt and issue for same Product concurrently — quantities and moving-average values reconcile.
- Posted document repeated with same idempotency key — only one stock effect.
- Adjustment can make stock over-reserved — posted successfully, explicit OverReserved alert recorded.
- Reservations expiry worker safely releases remaining reserved quantity only once.
- Deactivating a warehouse or location that holds stock is refused.
