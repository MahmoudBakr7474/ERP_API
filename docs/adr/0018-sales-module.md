# ADR 0018 — Sales V1: customers, prices, quotations, orders, reservations, deliveries, invoices and returns

Status: accepted (2026-10-29), on `feature/sales`. The product owner approved the scope and decisions in
[`docs/modules/Sales/V1_DECISIONS.md`](../modules/Sales/V1_DECISIONS.md), including the global approval rule in
`CLAUDE.md` (*Document approvals*); this ADR records how the module and its Inventory contracts implement them.

## Context
Sales had to promise stock to customers before it leaves (reservations), take it out from several locations, bill
only what left, and take it back at the cost it left with — without owning stock. Inventory (ADR 0015) owned
`ProductWarehouseStock.ReservedQuantity` but nothing explained or changed it, and only Purchasing could post through a
contract (ADR 0016). The workflow had to stay correct under concurrent confirmations, deliveries, invoices and ordinary
Inventory issues of the same units, and under retries.

## Decision
1. **Module layout.** `WarehouseERP.Sales` (+ `.Domain`, `.Application`, `.Contracts`) like every module (ADR 0001,
   0011). Schema `sales`. Other modules only through their Contracts: Organization (company, branches, warehouses,
   locations, geography), Catalog (`IProductCatalog`), Identity (`IUserDirectory`) and Inventory (`IStockReservations`,
   `ISalesStockPosting`, read-only lookups). Architecture tests know the module.
2. **Reservations belong to Inventory** (`Migration09_InventoryReservations`). `inventory.StockReservations` holds one
   row per order line (source document, line, product, warehouse; reserved, consumed, released) and
   `StockReservationEntries` an insert-only history (Reserved, Released, Consumed with the stock document). Every change
   updates the reservation row and `ProductWarehouseStock.ReservedQuantity` in one transaction:
   * reserve: all lines of an order or none, under the warehouse applock, with a conditional `UPDATE … WHERE OnHand −
     Reserved >= @quantity` (`Inventory.Stock.Insufficient` otherwise); a line is reserved once
     (`Inventory.Reservation.AlreadyReserved`);
   * release: at most what the line still holds (`Inventory.Reservation.Exceeded`);
   * consume: only through a sales delivery, lowering on hand and reserved together, and still requiring the stock.
   Reconciliation compares `ReservedQuantity` with what reservations hold (`ReservationDifference`);
   `GET /api/inventory/reservations` lists them. A loss below the reserved quantity leaves every reservation as it is
   and shows as over-reserved.
3. **Sales stock posting contract.** `ISalesStockPosting` posts inside the caller's transaction, like ADR 0016:
   `SalesDelivery = 11` (`SDL-`, stock out at the moving average, consuming reservations per picked location) and
   `SalesReturn = 12` (`SRT-`, stock in at the delivery line's cost, proportionally, the last units taking the
   remainder, never more than delivered — `Inventory.StockDocument.ReturnExceedsDelivery`). `SourceDocumentId` names the
   delivery or return; Inventory refuses to draft, edit or reverse these types (`Inventory.StockDocument.ManagedBySales`).
   Refusals pass through with Inventory's code (`StockErrors.Refused`).
4. **Numbering.** `sales.DocumentNumberSequences` per type, branch and fiscal year (calendar year in the company's time
   zone), locked-row SQL as in Inventory and Purchasing: `QT-`, `SO-`, `DN-`, `INV-`, `SR-{branch}-{year}-{000001}`,
   taken when a quotation is issued, an order confirmed, a delivery or invoice posted, a return approved. Drafts carry
   `DQT-`/`DSO-`/`DDN-`/`DINV-`/`DSR-000123` from identity columns. A rolled-back transaction gives the number back.
5. **Approval.** `sales.SalesSettings` (one row, seeded: nothing requires approval, no discount threshold, credit hold
   blocks confirmation and deliveries, VAT 14%, quotations valid 14 days). `SalesSettings.ApprovalReasons` computes why
   a quotation or order needs approval at submit; none → issued/confirmed at once. Returns always need approval. The
   approver may be the creator. History tables `Sales*Events`.
6. **Prices.** `sales.SalesPrices` (temporal): one price per product unit, base currency, tax-exclusive. Each document
   line stores unit price, its source (`PriceList`/`Manual`), the list price at the time, discount, tax treatment
   (`Standard`/`ZeroRated`/`Exempt`) and rate, and its rounded amounts; checks reject price ≤ 0 and inconsistent
   tax. Manual prices need `sales.prices.manage`.
7. **Concurrency and lock order.** Inside `ITransactionRunner`: idempotency key applock → the document's own row
   (`UPDLOCK`) → the order row → the delivery row (returns) → the Sales number → Inventory (reservation rows, then
   warehouse applocks in id order, stock rows, balances, valuation, Inventory numbers). Remaining, invoiceable and
   returnable quantities are checked under the order lock and again by check constraints on order lines
   (`Delivered + Closed <= Quantity`, `Invoiced + Returned − ReturnedInvoiced <= Delivered`, `Returned <= Delivered`)
   and delivery lines (`Returned <= Quantity`).
8. **Idempotency.** Posting deliveries and invoices and approving returns need an `Idempotency-Key`: an applock on
   the key, then `sales.IdempotencyRecords` (key, operation, SHA-256 fingerprint of subject, row version, user and
   lines). A retry answers the first outcome; the same key with another request is `Sales.Idempotency.KeyReused` (409).
9. **Invoices and returns.** Each order line tracks delivered, closed, invoiced (quantity, net, tax), returned and
   returned-after-invoicing. A return splits uninvoiced first, then invoiced; the invoiced part is a pending customer
   credit at the line's average invoiced net and tax, stored on return lines and header (`Credit*`) and summed on the
   order (`CreditedAmount`). Only `FitForResale` is accepted (`CK_SalesReturnLines_Condition`).
10. **Customers and credit.** Temporal `Customers`, `CustomerContacts`, `CustomerAddresses` (city from Organization's
    lookups, ADR 0017). Unique code, tax number per country, commercial registration per country + registry key;
    normalized name and phone keys for duplicate warnings. Credit terms, optional limit and manual hold with reason;
    exposure is a read model flagged `IsPartial`, never enforced.

## Consequences
- `Migration09_InventoryReservations` adds the two reservation tables, widens `CK_StockDocuments_Type` to 1–12 and the
  source check to types 9–12. `Migration10_Sales` creates the `sales` schema only (22 tables, 4 temporal with history:
  Customers, CustomerContacts, CustomerAddresses, SalesPrices) and seeds the settings row. Both `Down`s reverse exactly
  that; Migrations 01–08 are untouched.
- OpenAPI grows by 40 Sales paths (tags Customers, SalesPrices, SalesSettings, SalesQuotations, SalesOrders,
  SalesDeliveries, SalesInvoices, SalesReturns) and 73 schemas; Inventory gains `/api/inventory/reservations`, the enum
  members `SalesDelivery`, `SalesReturn` and the reconciliation's `reservationDifferences`. No existing path or schema
  changed.
- The Inventory frontend needs labels for the two new stock document types and the reservations list.

## Not done (backlog)
- Backorders; reservation expiry (needed by POS); reserving at quotation time.
- Quarantine / non-sellable stock and returns of damaged or uninspected goods.
- Customer-specific price lists, quantity breaks, promotions, header discounts; foreign currency.
- Services and non-stock lines; consolidated invoices across orders; delivery notes printing.
- Credit notes, refunds, customer payments, receivables, hard credit-limit enforcement, GL posting (Accounting).
- Batch, expiry and serial tracking (Inventory prerequisite).

## Production prerequisites
1. **Egyptian e-invoicing (ETA)**: invoices must be signed and submitted before Sales is used in production in Egypt.
2. The accountant approves the cost and credit treatment: deliveries at the moving average, returns at their delivery's
   cost, pending customer credits not posted as credit notes.
3. Inventory's (ADR 0015) and Purchasing's (ADR 0016) prerequisites still apply.
