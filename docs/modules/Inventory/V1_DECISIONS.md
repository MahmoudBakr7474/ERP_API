# Inventory V1 — approved decisions

Status: approved by the product owner on 2026-10-28, with corrections, from the decision checklist proposed the same
day. This file replaces that checklist; where they differ, this file wins. The invariants in
[`docs/INVENTORY_DECISIONS.md`](../../INVENTORY_DECISIONS.md) still bind everything below. How Milestones A and B
implement them is [ADR 0015](../../adr/0015-inventory-core.md).

## 1. Delivery
Inventory is one module, delivered in two milestones. Inventory's backend is not complete until both milestones and
their tests are finished. The Lovable prompt and the frontend handoff are written only after the backend is verified.

| Milestone A — Inventory Core | Milestone B — Inventory Operations |
|---|---|
| Opening stock, goods receipt, goods issue, stock adjustments | Warehouse transfers through a transit warehouse, one transit location per dispatched transfer |
| Stock documents and posting, approval, numbering | Partial receipt, close with discrepancy (approved loss adjustment from the transit location) |
| Stock balances and availability (`ProductWarehouseStock`) | Stock counting: freeze through `StockCountScope`, snapshot, count adjustments |
| Movement ledger, stock card and movement history | Reconciliation reports |
| Company-wide moving weighted average valuation | |
| Permissions and warehouse scope | |
| Refusing to deactivate a warehouse or location with stock or open obligations | |

Migration01–03 never change; each milestone adds only the migrations it needs.

## 2. Stock documents
- Tables: `StockDocuments`, `StockDocumentLines`, `StockAllocations`, `StockMovements`.
- An ordinary document (opening, receipt, issue, adjustment) works on **one physical warehouse**. Transfers are the
  explicit exception (source, transit and destination warehouses).
- A line keeps the entered product unit, its conversion factor, the entered quantity and the base quantity.
  Quantities are decimals with a defined precision; a conversion that does not fit it exactly is refused, never
  rounded.
- Allocations place a line's base quantity in locations (`FromLocationId`/`ToLocationId`, by operation). Every
  balance and movement has a non-null `LocationId`; every warehouse has its default location.
- Only stock items. Batch- or serial-tracked products are refused before posting (see 5).
- Posted documents are immutable. Corrections use approved reversal documents or adjustments. No backdated posting.

## 3. Availability and reservations
- `ProductWarehouseStock`: `ProductId`, `WarehouseId`, `OnHandSellable`, `ReservedQuantity`, `RowVersion`.
- `OnHandSellable` = sellable stock across all locations of a **physical** warehouse. `AllowsSelling` is checked
  separately by the operation that sells. Transit warehouses never contribute and never accept reservations.
- `ReservedQuantity` stays 0 until the Sales/POS reservation workflow exists. No `ReservedQuantity <= OnHandSellable`
  check constraint. Unreserved issues use atomic conditional updates.
- Authorized losses and adjustments may leave a warehouse over-reserved as long as physical stock does not go
  negative; the condition is recorded and reported explicitly.
- The reservation contract is documented for Sales/POS; no reservation code is written before it has a caller.

## 4. Valuation
- Physical quantities and financial value stay apart: `StockMovements` (quantity only), `InventoryCostStates`
  (current company-wide valuation per product), `InventoryCostEntries` (valuation history).
- Company-level moving weighted average, unit cost `decimal(19,8)`.
  * Receipts (and opening stock) add quantity and value and recompute the average.
  * Issues remove quantity and value at the current average.
  * Internal transfers, transit included, change neither company quantity nor value.
- The costing method stays subject to the accountant's approval before production inventory transactions begin.
  No general ledger or journal posting now. Valuation must be testable and reconcilable.

## 5. Tracking modes
Only untracked stock in this iteration. Tracked products are explicitly refused before posting. The data design keeps
room for lot and serial allocations (`LotId` on balances, allocations and movements) without building them.
**Pre-production dependency:** if the business sells batch-, expiry- or serial-tracked goods, that support must be
built before going live.

## 6. Numbering and approval
- The final number is assigned only when posting succeeds, from a transactional sequence table scoped by document
  type, branch and fiscal year (calendar year in the company's time zone). Drafts carry a separate reference.
  Numbers are kept forever, also for reversed documents.
- Approval is configurable per document type. Initial policy: opening stock and adjustments need approval; goods
  receipt and goods issue post directly. Whoever holds the approval permission approves, their own documents
  included (changed on 2026-10-28; at first a creator never approved their own document).
- Approval checks permission, warehouse scope, the document's state and its `rowVersion`.
- Each document records the approval policy that applied to it, so a later settings change does not alter it.

## 7. Transfers and counting (Milestone B)
- Transfers go through a transit warehouse, with a dedicated transit location per dispatched transfer; partial
  receipts; close with discrepancy through an approved adjustment from that transit location.
- Counts freeze through persisted `StockCountScope` only (no `WarehouseLocation.IsFrozen`).
- Transaction-owned `sp_getapplock` per warehouse: Shared for ordinary posting, Exclusive for starting a count freeze
  or deactivating a warehouse/location; deterministic lock order. No transaction stays open during physical counting.

## 8. Concurrency and idempotency
- A posting updates the document, balances, movements, valuation and its idempotency record in one transaction.
- Durable idempotency key with a request fingerprint: a retry returns the original outcome without posting twice;
  the same key with a different request is refused.
- `rowVersion` and document state are validated. Only deadlocks and transient failures are retried, a bounded number
  of times, each time rolling back the whole transaction. An uncertain commit is resolved by reading the persisted
  idempotency record before replaying. Explicit transactions run inside EF Core's execution strategy.

## 9. Organization and Catalog integration
- Only module contracts; no access to another module's entities.
- `IProductCatalog` grows only what Inventory needs: product units and conversion factors.
- Deactivating a warehouse or location that holds stock or has open inventory obligations is refused, under the same
  warehouse lock protocol as posting.
- Permissions are checked against warehouse scope.

## 10. Reconciliation and integration
- Reconciliation (Milestone B) reports, and never silently corrects:
  * physical sellable balances vs. `ProductWarehouseStock`;
  * reserved totals, once reservations exist;
  * `InventoryCostStates` quantity vs. company-owned stock, transit included;
  * posted movements vs. valuation entries (quantity and value).
- The outbox stays deferred until the first real consumer. Posted documents, movements and cost entries are
  immutable, so they remain the source records for that later integration.
