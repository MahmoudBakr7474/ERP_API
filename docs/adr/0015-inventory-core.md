# ADR 0015 — Inventory V1: documents, posting, valuation, transfers, counts and reconciliation

Status: accepted (2026-10-28), on `feature/inventory`. The product owner approved the scope and decisions in
[`docs/modules/Inventory/V1_DECISIONS.md`](../modules/Inventory/V1_DECISIONS.md); this ADR records how Milestone A
(`5dfdf1a`, decisions 1–15) and Milestone B (`cc75d3d`, `4c3170d`, decisions 16–24) implement them.
[`INVENTORY_DECISIONS.md`](../INVENTORY_DECISIONS.md) still binds the design.

## Context
There was no Inventory module and no stock table. Inventory had to keep stock per location without negative stock,
keep sellable stock per warehouse for later reservations, value stock at a company-wide moving average, number and
approve documents, survive concurrent postings and retried requests, and stop Organization from retiring a warehouse
or location that still holds stock. Milestone B adds transfers through transit warehouses, stock counts with a
persisted freeze, and a reconciliation report on top of Milestone A's posting.

## Decision — Milestone A
1. **Module and schema.** `src/Modules/Inventory` with the usual four projects; everything in the `inventory` schema,
   created by `Migration04_Inventory` (Migration01–03 unchanged). Operational data, so no temporal tables: a document's
   history is `StockDocumentEvents` and, once posted, its movements and cost entries, all insert-only.
2. **Documents.** `StockDocuments` → `StockDocumentLines` → `StockAllocations`.
   * Types: `OpeningBalance` (OPN), `Receipt` (RCV), `Issue` (ISS), `Adjustment` (ADJ), `Reversal` (REV); Milestone B
     adds the generated types of decision 17.
   * Statuses: `Draft` → (`Submitted`) → `Posted`, or `Cancelled`.
   * An ordinary document has one physical warehouse. A line keeps the product unit as entered, the conversion factor
     Catalog had then, the entered quantity and the base quantity. One allocation per line places it in a location
     (the DEFAULT location when none is named).
   * Allocations have From/To warehouse and location pairs and a nullable `LotId`, so transfers and lots (later) fit
     without reshaping the tables.
3. **Quantities and money.**
   * Quantities are `decimal(18,4)` in base units, up to 4 decimals as entered, and whole for a unit that can't be
     split. The base quantity must convert exactly (whole for a base unit that can't be split); otherwise the line is
     refused (`ConversionNotExact`), never rounded.
   * Unit costs and averages are `decimal(19,8)`, values `decimal(19,4)`, in the base currency. A line is limited to a
     unit cost of 1e10 per base unit and a value of 1e14, so stored values can't overflow.
4. **Posting** is one `ITransactionRunner` transaction (BuildingBlocks port, EF implementation registered by
   Persistence). It takes these steps in a fixed order:
   1. the request key's `sp_getapplock`;
   2. the document row (`UPDLOCK`); a dispatch, receipt or count approval locks its transfer's or count's row here
      instead (decision 24);
   3. the applock of every warehouse the document touches, in id order (Shared; Exclusive for opening stock);
   4. a re-check of warehouses, locations and products, now that the locks are held, then the count freezes
      (decision 19);
   5. `ProductWarehouseStock` rows by warehouse then product;
   6. `StockBalances` rows by warehouse, location, product;
   7. `InventoryCostStates` rows by product (`UPDLOCK, HOLDLOCK`);
   8. the `DocumentNumberSequences` row.

   Decreases are atomic conditional `UPDATE`s that match nothing when they would go below zero; increases are upserts
   under a key-range lock. `CHECK (… >= 0)` constraints are the last guard. The posting code is shared by every area
   (`Application/Shared/StockDocumentPosting`): direct posting, approval, and the generated documents of Milestone B.
5. **Availability.** `ProductWarehouseStock` exists for physical warehouses only.
   * `OnHandSellable` is their available stock across all locations; `AllowsSelling` is left to the operation that
     sells.
   * An `Issue` (and a transfer dispatch) takes only unreserved stock (`OnHandSellable - ReservedQuantity >= q`).
   * An `Adjustment`, `Reversal` or count adjustment may cut into reserved stock but never below zero; when it leaves
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
   * A reversed issue comes back at the value it left with; a reversed receipt leaves at the average (decision 23).
   * `StockMovements` carry no cost. The method stays subject to the accountant's approval before production use, and
     nothing posts to a general ledger.
7. **Idempotency.** Post and approve need an `Idempotency-Key` header and the `rowVersion` the user saw.
   `IdempotencyRecords` stores the key with a SHA-256 fingerprint of operation, subject, row version and user (and the
   received lines for a transfer receipt), in the posting's transaction. Its `SubjectId` is the document, transfer or
   count the request acted on; `DocumentId` is the document it posted, if any.
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
9. **Approval.** `DocumentTypePolicies` holds one row per type that can be configured.
   * Seeded with the owner's initial policy: opening stock and adjustments need approval; receipts, issues and transfer
     dispatches post directly.
   * It can be changed by `inventory.settings.manage` with company-wide access.
   * A document (or transfer) copies its policy when created, and a reversal always needs approval.
   * Approving posts in the same step; a rejection returns it to draft with its reason.
   * Anyone with `documents.approve` and the warehouse in scope approves, their own document, transfer or count
     included. The owner withdrew the first rule, that nobody approves what they created (`SelfApproval`), on
     2026-10-28: a small team often has one administrator, and keeping the roles apart still separates the duties.
   * Every step is a `StockDocumentEvent` (who, when, note).
10. **Reversal.** `POST /documents/{id}/reverse` creates a `Reversal` already submitted, with the posted lines turned
    the other way. Only one reversal can be open per document (filtered unique index). A reversal can't be reversed,
    and posted documents stay immutable.
11. **Opening stock** is allowed in a warehouse until anything else posts there (the reversal of an opening document
    doesn't count). Its posting takes the warehouse lock exclusively to check that.
12. **Permissions and scope** (`InventoryPermissions`).
    * Fixed permissions: `stock.read`, `documents.read`, `documents.approve`, `valuation.read`, `settings.manage`;
      Milestone B adds `transfers.manage`, `transfers.receive`, `counts.manage` and `reconciliation.read`.
    * Drafting, submitting, posting, cancelling and reversing need the manage permission of the document's type
      (`opening`, `receipts`, `issues`, `adjustments`). The handler checks it, because the type is in the body or the
      document.
    * The warehouse scope is checked before existence: 403 `Inventory.StockDocument.AccessDenied` for a scoped user,
      404 only for company-wide users. Lists show only the user's warehouses.
13. **Organization integration.** `Inventory.Contracts.IWarehouseStockGuard` is implemented by Inventory.
    * Organization's deactivate handlers run in an `ITransactionRunner` transaction and call the guard. The guard takes
      the warehouse's applock exclusively and refuses while there is a non-zero balance, availability or reservation
      (`Organization.Warehouse.HoldsStock`, `Organization.WarehouseLocation.HoldsStock`). It also refuses while a
      submitted document, an open transfer or a running count still needs it (`…HasOpenInventoryDocuments`). A lock
      timeout returns 409 `Organization.Warehouse.InventoryBusy`.
    * A posting re-checks active status under its shared lock, so a deactivation and a posting can't both succeed
      (tested by racing them).
14. **Contracts read by Inventory** (additive):
    * `IProductCatalog.FindManyAsync`, `FindUnitsAsync`, `FindBaseUnitsAsync` with `ProductUnitInfo` (conversion
      factor, fraction rules, active);
    * Organization's `IWarehouseLocationDirectory`, `ICompanyDirectory` and, in Milestone B, `ITransitLocations`;
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

## Decision — Milestone B
16. **Transfers.** `StockTransfers` → `StockTransferLines`, `StockTransferEvents`; reference `TR-000123` until dispatch.
    * Source and destination are different active physical warehouses (any branches). The transit warehouse is the one
      named, or else the source branch's only active transit warehouse (`TransitWarehouseRequired` when there is none
      or several). A line takes any active unit of the product, converts exactly to base units, and is picked from a
      source location (DEFAULT when none is named).
    * Statuses: `Draft` → (`Submitted`) → `InTransit` → `PartiallyReceived` → `Received`; `Closing` → `Closed`;
      `Cancelled` only before dispatch. Every step is a `StockTransferEvent`.
    * Dispatch follows the `TransferDispatch` approval policy copied at creation: directly, or submit then approve
      (approving dispatches). It needs an `Idempotency-Key`.
    * Dispatching opens a location `TR-000123` (named "ترانزيت TR-000123") in the transit warehouse through
      Organization's `ITransitLocations` in the same transaction, then posts a `TransferDispatch` document moving every
      line from its source location into it. A refused dispatch rolls everything back, location included.
    * Receiving (`transfers.receive`, destination in scope, `Idempotency-Key`) posts a `TransferReceipt` document
      moving base quantities from the transit location into destination locations (DEFAULT when none is named), never
      more than a line still has in transit (`ReceiptExceedsOutstanding`). Nothing outstanding → `Received`, and the
      transit location is deactivated.
    * Who: the source side drafts, edits, submits, cancels, dispatches and approves; the destination side receives;
      either side reads and closes. Lists show transfers with either warehouse in the user's scope.
    * `GET /transfers/destinations` (`transfers.manage`) lists every active physical warehouse with its names and
      branch code, through Organization's `IWarehouseListing`: Organization's own list shows only the user's scope, and
      a sender must be able to name a destination outside it. It lists names only; receiving there still needs the
      destination in scope.
17. **Generated documents.** `TransferDispatch` (TRF, source branch), `TransferReceipt` (TRR, destination branch) and
    `CountAdjustment` (CNT) are created and posted only by their transfer or count, which carries the approval
    (`MarkPostedGenerated`, no approval check of their own). Transfer documents use the new `Move` direction (one
    allocation from a place to another). They point back through `StockDocuments.TransferId`/`CountId` (also in the
    document's details, with `fromLocationId`/`fromLocationCode` on a move line), can't be edited (`LinkedLinesFixed`)
    and can't be reversed (`NotReversible`): an error is corrected by a new transfer, an adjustment or a new count.
18. **What never arrives.** `POST /transfers/{id}/close` (reason, notes) submits an `Adjustment` in the transit
    warehouse for every outstanding quantity, out of the transfer's transit location, and puts the transfer in
    `Closing` (receiving stops). Approving that adjustment like any other (`documents.approve`, the transit
    warehouse in scope) posts it at the average, writes the outstanding quantities off as `LostQuantity`,
    closes the transfer and deactivates its transit location. Cancelling the adjustment returns the transfer to
    `InTransit`/`PartiallyReceived`.
19. **Stock counts.** `StockCounts` → `StockCountScopes`, `StockCountLines`, `StockCountEvents`; reference `CT-000123`.
    * A count covers one physical warehouse, whole (no locations) or up to 200 of its active locations.
    * Statuses: `Draft` → `Counting` → `Submitted` → `Completed`; a rejection returns `Submitted` to `Counting`;
      `Cancelled` from any open state.
    * **Start** runs in one transaction: count row (`UPDLOCK`), the warehouse's applock **Exclusive** (so no posting is
      in flight), overlap check, scopes made active, and a snapshot of the scope's available balances as expected
      quantities. It commits at once: nothing is held while people count.
    * **The freeze is the active `StockCountScope` rows** (no `WarehouseLocation.IsFrozen`): filtered unique indexes
      allow one active whole-warehouse scope per warehouse and one active scope per location
      (`Inventory.StockCount.ScopeOverlaps`, 409). Every posting reads them after taking its warehouse locks and
      refuses a line touching a frozen place (`Inventory.StockCount.ScopeFrozen`, 422), its own count's adjustment
      excepted. A posting is therefore either committed before the snapshot or refused; transfers are held at either
      end. Transit warehouses are never counted.
    * Counted quantities are entered in base units and can be re-entered; a product found where nothing was expected
      gets a line expecting zero. A surplus may carry a unit cost (required for a product never valued). Submitting
      needs every line counted.
    * **Approval** (`documents.approve`, `Idempotency-Key`) runs in one transaction: the warehouse's
      applock (Shared), a re-check that the frozen balances still equal the snapshot (409
      `StockChangedDuringCount` otherwise), a `CountAdjustment` of the differences (shortages out at the average,
      surpluses in at their cost or the average), then completion, which releases the freeze. No difference → no
      document.
20. **Reconciliation.** `GET /api/inventory/reconciliation` (`reconciliation.read`, company-wide only). It reads
    balances, availability, movements, cost states, cost entries and transfers in one `SNAPSHOT` transaction
    (`Migration06` turns `ALLOW_SNAPSHOT_ISOLATION` on; everything else keeps READ COMMITTED), compares in memory and
    returns the lists below. SQL Server keeps a pooled connection's isolation level, so the report sets its connection
    back to READ COMMITTED before releasing it; otherwise the next request on that connection ran under SNAPSHOT and two
    requests updating the same session row failed with an update conflict (500). The report returns:
    * availability differences: a physical warehouse's available balances vs. its `OnHandSellable`;
    * ledger differences: the posted movements of each location vs. its balance;
    * valuation differences: company stock (transit included) vs. the cost state, the cost state vs. its entries
      (quantity and value), and value left without quantity;
    * transfer differences: what each dispatched transfer still has in transit vs. its transit location's balance;
    * stock positions per product (physical, in transit, valued quantity and value) and open transfers with their days
      in transit.

    It never writes. Reserved totals join it once reservations exist.
21. **Organization.** `Organization.Contracts.ITransitLocations` opens a transfer's location in a transit warehouse
    and retires it; it joins the caller's unit of work. `IWarehouseListing` lists active warehouses of a type with
    their names, whatever the caller's scope (decision 16). The guard (decision 13) also counts open transfers (submitted,
    in transit, partially received, closing) on any of their three warehouses, transfers waiting to pick from a
    location, and running counts.
22. **Valuation of transfers.** Dispatch and receipt create no cost entry: company quantity and value include stock in
    transit and don't change. Only the loss adjustment of decision 18 takes value out, at the average.
23. **Receipt reversal (costing clarification).** A reversed receipt leaves at the current average, like any stock
    going out; it does not take back its own cost. Taking back the original cost would need a price-variance account
    (general ledger, not in V1) and can leave the valuation negative once part of the stock was issued: receipts of
    10 @ 1000 and 10 @ 1, then 9 issued at the 500.5 average, leave 11 units worth 5505.5; reversing the first receipt
    at its cost would leave one unit worth −4494.5, at the average one unit worth 500.5. No integrity problem was
    found, so the code is unchanged; this is the documented V1 policy pending the accountant's approval
    (`ReceiptReversalCostingTests`, `A_reversed_receipt_leaves_at_the_current_average_not_at_its_own_cost`).
24. **Lock order with transfers and counts.** Dispatch, receipt and count approval: request key → transfer or count
    row (`UPDLOCK`) → warehouse applocks in id order → availability, balance, valuation and number rows as in
    decision 4. A count start takes its warehouse Exclusive; postings and count approval take theirs Shared. The loss
    adjustment's approval is an ordinary posting (decision 4) that locks its transfer's row last; it can't wait on a
    receipt, because a transfer being closed refuses receipts before taking any warehouse lock.

## Consequences
* `Migration04_Inventory` adds 13 tables, 32 indexes and the policy seed. Foreign keys to Organization and Catalog
  live in `ErpDbContext`. Locations are always referenced with their warehouse, through the `(WarehouseId, Id)`
  alternate key.
* `Migration05_InventoryTransfersCounts` adds 7 tables and 30 indexes, `StockDocuments.TransferId`/`CountId`,
  `IdempotencyRecords.SubjectId` (backfilled from `DocumentId`, which becomes nullable), widened check constraints
  (document types 1–8, direction `Move`) and the `TransferDispatch` policy row. Nothing existing is dropped or
  rewritten.
* `Migration06_InventorySnapshotIsolation` only runs `ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION ON`. It is
  alone because it can't run in a migration's transaction (EF's advice: a failure can't leave a migration half
  applied). It waits for open transactions; a DBA must allow it on a shared production server.
* The OpenAPI contract grew additively: Milestone A 16 paths (tags `StockDocuments`, `Stock`, `Valuation`,
  `DocumentPolicies`) and a 422 on `DeactivateWarehouse`; Milestone B 19 paths (tags `StockTransfers`, `StockCounts`,
  `Reconciliation`), 39 schemas, new properties on `StockDocumentDetailsResponse` (`transferId`, `countId`) and
  `StockDocumentLineResponse` (`fromLocationId`, `fromLocationCode`), and new enum members (`StockDocumentType`
  `TransferDispatch`, `TransferReceipt`, `CountAdjustment`; `StockDirection` `Move`), which clients must accept.
* **Deviations from earlier ADRs:**
  * ADR 0004 sketched `ITransactionRunner` with lock names as a parameter. Locks are taken by the module that owns
    their names (Inventory's are private to it), so the port only runs the transaction.
  * ADR 0008 put the outbox in Inventory's first migration. It stays deferred until the first consumer: there is none
    in V1. Posted documents, movements and cost entries are immutable source records for it.
* Tests:
  * Integration tests against SQL Server cover posting, approval, numbering, idempotency, concurrent last-unit issues,
    concurrent receipts and issues reconciling quantity and value, reversal (receipt reversal at the average),
    over-reservation, deactivation refused (and raced) and scope. Milestone B adds transfers (dispatch, partial and
    full receipt, in-transit balances, valuation unchanged, approval, loss closure, refused dispatch leaving nothing,
    racing receipts, each side's scope, counts holding transfers), counts (freeze per location and whole warehouse,
    overlap, a posting racing a start, approval with differences, changed stock refused, scope) and reconciliation
    (transit in the company's stock, differences reported and not corrected, company-wide only).
  * Unit tests cover the document rules, conversions, the moving average, the transfer and count rules and the
    receipt-reversal costing.
  * The real host was checked end to end with cookie sessions on a throwaway database (dropped).

## Not done
* Batch, expiry and serial tracking: tracked products are refused. This is a pre-production dependency if the business
  needs them.
* Reservations, the outbox, general-ledger posting, and costing method sign-off by the accountant.
* Transfers: receipts are entered in base units; a dispatched transfer can't be cancelled (receive it or close it);
  the transit location of a closed or received transfer is deactivated, not removed.
* Counts: no blind-count mode or recount of single lines by a second counter; counts cover available stock without
  lots.
* Reconciliation is on demand only (no schedule, no stored history).
