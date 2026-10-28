# ADR 0015 — Inventory Milestone A: stock documents, posting, availability and valuation

Status: accepted (2026-10-28), on `feature/inventory`. The product owner approved the scope and decisions in
[`docs/modules/Inventory/V1_DECISIONS.md`](../modules/Inventory/V1_DECISIONS.md); this ADR records how Milestone A
implements them. [`INVENTORY_DECISIONS.md`](../INVENTORY_DECISIONS.md) still binds the design.

## Context
There was no Inventory module and no stock table. Inventory had to keep stock per location without negative stock,
keep sellable stock per warehouse for later reservations, value stock at a company-wide moving average, number and
approve documents, survive concurrent postings and retried requests, and stop Organization from retiring a warehouse
or location that still holds stock. Milestone B (transfers through transit, stock counts, reconciliation) builds on
this one.

## Decision
1. **Module and schema.** `src/Modules/Inventory` with the usual four projects; everything in the `inventory` schema,
   created by `Migration04_Inventory` (Migration01–03 unchanged). Operational data, so no temporal tables: a document's
   history is `StockDocumentEvents` and, once posted, its movements and cost entries, all insert-only.
2. **Documents.** `StockDocuments` → `StockDocumentLines` → `StockAllocations`.
   * Types: `OpeningBalance` (OPN), `Receipt` (RCV), `Issue` (ISS), `Adjustment` (ADJ), `Reversal` (REV).
   * Statuses: `Draft` → (`Submitted`) → `Posted`, or `Cancelled`.
   * An ordinary document has one physical warehouse. A line keeps the product unit as entered, the conversion factor
     Catalog had then, the entered quantity and the base quantity. One allocation per line places it in a location
     (the DEFAULT location when none is named).
   * Allocations have From/To warehouse and location pairs and a nullable `LotId`, so transfers (Milestone B) and lots
     (later) fit without reshaping the tables.
3. **Quantities and money.**
   * Quantities are `decimal(18,4)` in base units, up to 4 decimals as entered, and whole for a unit that can't be
     split. The base quantity must convert exactly (whole for a base unit that can't be split); otherwise the line is
     refused (`ConversionNotExact`), never rounded.
   * Unit costs and averages are `decimal(19,8)`, values `decimal(19,4)`, in the base currency. A line is limited to a
     unit cost of 1e10 per base unit and a value of 1e14, so stored values can't overflow.
4. **Posting** is one `ITransactionRunner` transaction (BuildingBlocks port, EF implementation registered by
   Persistence). It takes these steps in a fixed order:
   1. the request key's `sp_getapplock`;
   2. the document row (`UPDLOCK`);
   3. the warehouse's applock (Shared; Exclusive for opening stock);
   4. a re-check of warehouse, locations and products, now that the lock is held;
   5. `ProductWarehouseStock` rows by warehouse then product;
   6. `StockBalances` rows by warehouse, location, product;
   7. `InventoryCostStates` rows by product (`UPDLOCK, HOLDLOCK`);
   8. the `DocumentNumberSequences` row.

   Decreases are atomic conditional `UPDATE`s that match nothing when they would go below zero; increases are upserts
   under a key-range lock. `CHECK (… >= 0)` constraints are the last guard.
5. **Availability.** `ProductWarehouseStock` exists for physical warehouses only.
   * `OnHandSellable` is their available stock across all locations; `AllowsSelling` is left to the operation that
     sells.
   * An `Issue` takes only unreserved stock (`OnHandSellable - ReservedQuantity >= q`).
   * An `Adjustment` or `Reversal` may cut into reserved stock but never below zero; when it leaves
     `OnHandSellable < ReservedQuantity`, it records an `OverReservationAlert`. The posting response lists it, and so
     does `GET /api/inventory/over-reservations`.
   * `ReservedQuantity` stays 0 until reservations exist.
6. **Valuation (moving weighted average, company-wide).** `InventoryCostStates` holds quantity, value and average per
   product; `InventoryCostEntries` holds one immutable entry per posted line with the state it left.
   * A receipt or opening line adds `quantity × unit cost` (rounded to 4 decimals) and recomputes the average (rounded
     to 8 decimals).
   * Stock going out leaves at `quantity × average`, and the last units take the value that is left, so value never
     outlives quantity.
   * An adjustment increase without a cost comes in at the average. A product never valued needs a cost
     (`Inventory.Valuation.UnitCostRequired`).
   * A reversed issue comes back at the value it left with; a reversed receipt leaves at the average.
   * `StockMovements` carry no cost. The method stays subject to the accountant's approval before production use, and
     nothing posts to a general ledger.
7. **Idempotency.** Post and approve need an `Idempotency-Key` header and the `rowVersion` the user saw.
   `IdempotencyRecords` stores the key with a SHA-256 fingerprint of operation, document, row version and user, in the
   posting's transaction.
   * The same key and request get the first outcome back with no second effect.
   * The same key with another request is 409 `Inventory.Idempotency.KeyReused`.
   * A new key on a posted document is 422 `NotDraft`.
   * Deadlocks, transient connection errors and a commit that failed transiently run the whole transaction again (at
     most 3 attempts, change tracker cleared). Because each run first reads its idempotency record, an earlier attempt
     that did commit is answered from that record instead of posting again. Each attempt runs inside EF Core's
     execution strategy.
8. **Numbering.** The final number is taken only inside a successful posting, from `DocumentNumberSequences` per
   (type, branch, fiscal year).
   * Format: `RCV-<branch code>-2026-000001`.
   * The fiscal year is the calendar year in the company's time zone (`ICompanyDirectory`).
   * A draft's reference `DR-000123` comes from an identity column; gaps there are harmless.
   * Numbers are unique and never reused.
9. **Approval.** `DocumentTypePolicies` holds one row per type.
   * Seeded with the owner's initial policy: opening stock and adjustments need approval; receipts and issues post
     directly.
   * It can be changed by `inventory.settings.manage` with company-wide access.
   * A document copies its policy when created, and a reversal always needs approval.
   * Approving posts in the same step; nobody approves a document they created (`SelfApproval`); a rejection returns
     it to draft with its reason.
   * Every step is a `StockDocumentEvent` (who, when, note).
10. **Reversal.** `POST /documents/{id}/reverse` creates a `Reversal` already submitted, with the posted lines turned
    the other way. Only one reversal can be open per document (filtered unique index). A reversal can't be reversed,
    and posted documents stay immutable.
11. **Opening stock** is allowed in a warehouse until anything else posts there (the reversal of an opening document
    doesn't count). Its posting takes the warehouse lock exclusively to check that.
12. **Permissions and scope** (`InventoryPermissions`).
    * Fixed permissions: `stock.read`, `documents.read`, `documents.approve`, `valuation.read`, `settings.manage`.
    * Drafting, submitting, posting, cancelling and reversing need the manage permission of the document's type
      (`opening`, `receipts`, `issues`, `adjustments`). The handler checks it, because the type is in the body or the
      document.
    * The warehouse scope is checked before existence: 403 `Inventory.StockDocument.AccessDenied` for a scoped user,
      404 only for company-wide users. Lists show only the user's warehouses.
13. **Organization integration.** `Inventory.Contracts.IWarehouseStockGuard` is implemented by Inventory.
    * Organization's deactivate handlers run in an `ITransactionRunner` transaction and call the guard. The guard takes
      the warehouse's applock exclusively and refuses while there is a non-zero balance, availability or reservation
      (`Organization.Warehouse.HoldsStock`, `Organization.WarehouseLocation.HoldsStock`). It also refuses while a
      submitted document would post there (`…HasOpenInventoryDocuments`). A lock timeout returns 409
      `Organization.Warehouse.InventoryBusy`.
    * A posting re-checks active status under its shared lock, so a deactivation and a posting can't both succeed
      (tested by racing them).
14. **Contracts read by Inventory** (additive):
    * `IProductCatalog.FindManyAsync`, `FindUnitsAsync`, `FindBaseUnitsAsync` with `ProductUnitInfo` (conversion
      factor, fraction rules, active);
    * Organization's `IWarehouseLocationDirectory` and `ICompanyDirectory`;
    * Identity's existing `IUserDirectory` for display names.
15. **Reservation contract (documented, not built).** Sales and POS will get an `Inventory.Contracts` port to reserve
    and release. A reservation must:
    * reserve per product and physical warehouse with `ReservedQuantity += q WHERE OnHandSellable - ReservedQuantity
      >= q`, under the same warehouse lock and row order;
    * refuse transit warehouses;
    * carry an idempotency key;
    * release only what remains, exactly once (expiry worker);
    * turn into an issue that reduces both columns together.

    No code exists until a caller does.

## Consequences
* `Migration04_Inventory` adds 13 tables, 32 indexes and the policy seed. Foreign keys to Organization and Catalog
  live in `ErpDbContext`. Locations are always referenced with their warehouse, through the `(WarehouseId, Id)`
  alternate key.
* The OpenAPI contract grew additively: 16 paths (tags `StockDocuments`, `Stock`, `Valuation`, `DocumentPolicies`) and
  their schemas, plus a 422 response on `DeactivateWarehouse`.
* **Deviations from earlier ADRs:**
  * ADR 0004 sketched `ITransactionRunner` with lock names as a parameter. Locks are taken by the module that owns
    their names (Inventory's are private to it), so the port only runs the transaction.
  * ADR 0008 put the outbox in Inventory's first migration. It stays deferred until the first consumer: there is none
    in V1. Posted documents, movements and cost entries are immutable source records for it.
* Tests:
  * Integration tests against SQL Server cover posting, approval, numbering, idempotency, concurrent last-unit issues,
    concurrent receipts and issues reconciling quantity and value, reversal, over-reservation, deactivation refused
    (and raced) and scope.
  * Unit tests cover the document rules, conversions and the moving average.

## Not done
* Milestone B: transfers through transit, stock counts with freeze and snapshot, reconciliation reports.
* Batch, expiry and serial tracking: tracked products are refused. This is a pre-production dependency if the business
  needs them.
* Reservations, the outbox, general-ledger posting, and costing method sign-off by the accountant.
