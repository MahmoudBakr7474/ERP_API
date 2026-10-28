# ADR 0016 — Purchasing V1: suppliers, requests, orders, receipts, invoices and returns

Status: accepted (2026-10-28), on `feature/purchasing`. The product owner approved the scope and decisions in
[`docs/modules/Purchasing/V1_DECISIONS.md`](../modules/Purchasing/V1_DECISIONS.md), including the global approval rule
in `CLAUDE.md` (*Document approvals*); this ADR records how the module implements them.

## Context
Purchasing had to turn supplier orders into stock without owning stock: Inventory (ADR 0015) is the only owner of
balances, movements, locks, valuation and stock numbering, and nothing in its contracts let another module post. The
workflow had to survive retries, races between receipts, invoices and returns of one order, and keep every document
traceable to the stock documents it caused.

## Decision
1. **Module layout.** `WarehouseERP.Purchasing` (+ `.Domain`, `.Application`, `.Contracts`) like every module (ADR 0001,
   0011). Schema `purchasing`. Other modules only through their Contracts: Organization (company, branches, warehouses,
   locations), Catalog (`IProductCatalog`: products, units, conversion factors), Identity (`IUserDirectory`) and
   Inventory (`IPurchaseStockPosting`). Architecture tests discover the module (the known-modules list grows by one).
2. **Inventory integration contract (additive).** `Inventory.Contracts.IPurchaseStockPosting` posts a purchase receipt
   (stock in at each line's unit cost) or return (stock out at the moving average) *inside the caller's transaction*:
   Inventory builds a `StockDocument` with the new types `PurchaseReceipt = 9` (PRC) and `PurchaseReturn = 10` (PRT)
   and posts it through the existing `StockDocumentPosting.ApplyAsync` — warehouse applocks in id order, count freezes,
   no negative stock, moving average, numbering — then joins the caller's unit of work, so the stock and the
   purchasing document commit or roll back together. A refusal comes back as Inventory's own error code
   (`PurchaseStockOutcome`), which Purchasing passes through unchanged (`StockPostingErrors.Refused`).
3. **Traceability both ways.** `StockDocuments.SourceDocumentId` (nullable, filtered unique `UX_StockDocuments_Source`,
   check `CK_StockDocuments_Source`: set exactly for types 9 and 10) names the goods receipt or supplier return; the
   receipt and the return keep `StockDocumentId` (unique) and the stock number. `ExternalReference` carries the
   GRN/SRN number. Inventory refuses to draft, edit, cancel or reverse these types
   (`Inventory.StockDocument.ManagedByPurchasing`); other generated types (transfer, count) now answer
   `Inventory.StockDocument.TypeNotDrafted` instead of failing.
4. **Numbering.** Purchasing's own series table (`purchasing.DocumentNumberSequences`, same locked-row SQL as
   Inventory), per document type, branch and fiscal year (calendar year in the company's time zone):
   `PR-`, `PO-`, `GRN-`, `SIN-`, `SRN-{branch}-{year}-{000001}`, taken at approval or posting inside its transaction.
   Drafts carry `DPR-`/`DPO-`/`DSIN-`/`DSRN-000123` from identity columns.
5. **Approval (global rule).** `purchasing.documents.approve` + warehouse scope + document state; the creator may
   approve. `purchasing.DocumentPolicies` (PR, PO; seeded required) is copied onto each document when created; without
   approval, submit approves and numbers at once. Invoices need approval only for price variances; returns always.
   History tables `PurchaseRequestEvents`, `PurchaseOrderEvents`, `SupplierInvoiceEvents`, `SupplierReturnEvents`.
6. **Concurrency.** Receiving, invoicing and returning all lock the order row (`UPDLOCK`) after their own document, in
   a fixed order (document → order → receipt → Purchasing number → Inventory locks), inside `ITransactionRunner`.
   Remaining, invoiceable and returnable quantities are checked under that lock and again by check constraints:
   `ReceivedQuantity <= Quantity`, `InvoicedQuantity + ReturnedQuantity - ReturnedInvoicedQuantity <= ReceivedQuantity`,
   `ReturnedQuantity <= ReceivedQuantity` on order lines, `ReturnedQuantity <= Quantity` on receipt lines,
   `OrderedQuantity <= Quantity` on request lines. Orders drawing on requests lock the requests in id order.
7. **Idempotency.** Receiving, posting/approving an invoice and approving a return take an `Idempotency-Key`: an
   applock on the key, then `purchasing.IdempotencyRecords` (key, operation, SHA-256 fingerprint of the action, subject,
   row version, user and, for receipts, the lines). A retry answers the first outcome; the same key with another
   request is `Purchasing.Idempotency.KeyReused` (409). The record is saved with the posting.
8. **Amounts.** Prices `decimal(19,8)`, quantities `(18,4)`, percentages `(9,4)`, amounts `(19,4)` rounded per line to
   the currency's minor unit (ISO 4217; 2 unless known otherwise), half away from zero: gross, discount, net, tax.
   The net unit price is the provisional inventory cost. Tax treatment is stored per line; only recoverable is accepted.
9. **Invoices and returns.** Each order line tracks received, invoiced (quantity, net, tax), returned and returned-
   after-invoicing. An invoice line bills at most `received − returned before invoicing − invoiced`. A return splits
   uninvoiced first, then invoiced; the invoiced part is a pending credit claim at the line's average invoiced net
   price and tax, stored on the return lines and header (`CreditClaim*`). No revaluation, payments or ledger.
10. **Suppliers.** Temporal tables (`Suppliers`, `SupplierContacts`, `SupplierAddresses`). Unique code, tax number per
    country, commercial registration per country + registry key; normalized name keys for duplicate warnings.

## Consequences
- `Migration07_Purchasing` creates the `purchasing` schema (20 tables, 3 of them temporal with history) and, in
  `inventory.StockDocuments`, only: widens `CK_StockDocuments_Type` to 1–10, adds `SourceDocumentId` with its filtered
  unique index and check. Its `Down` reverses exactly that. Migrations 01–06 are untouched.
- OpenAPI grows by 47 operations (tags Suppliers, PurchasingPolicies, PurchaseRequests, PurchaseOrders, GoodsReceipts,
  SupplierInvoices, SupplierReturns); Inventory gains `StockDocumentDetailsResponse.sourceDocumentId` and the enum
  members `PurchaseReceipt`, `PurchaseReturn`. Nothing else changed.
- The Inventory frontend needs labels for the two new stock document types and should link them to Purchasing.

## Not done (backlog)
- Foreign currency and exchange rates; header-level discounts; non-recoverable tax capitalization; Catalog tax codes.
- Revaluing stock for invoice price variances (landed cost); supplier credit notes, payments, AP balances, GL posting.
- Receiving in another unit than the order line's; over-receipt tolerance; reopening an order after returns.
- Requests released when an order is closed short; supplier price lists; approval thresholds by amount.
- Batch, expiry and serial tracking (Inventory prerequisite).

## Production prerequisites
1. The accountant approves the provisional cost treatment: receipts at the order's net price, invoice differences kept
   as purchase price variances without revaluing stock, returns leaving at the moving average.
2. Inventory's prerequisites (ADR 0015) still apply.
