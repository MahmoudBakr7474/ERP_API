# Purchasing V1 — approved decisions

Status: approved by the product owner on 2026-10-28, from the decision checklist proposed the same day, with the owner's
corrections; the same day the owner made document self-approval a permanent rule for every module (`CLAUDE.md`,
*Document approvals*), which replaces the maker-checker wording of that approval. This file replaces the checklist;
where they differ, this file wins. How the code implements it: [ADR 0016](../../adr/0016-purchasing-module.md).

## 1. Scope and workflow
Suppliers → optional purchase requests → purchase orders → partial or full goods receipts → supplier invoices →
supplier returns. Single company, multi-branch, multi-warehouse trading and distribution; no manufacturing.

Purchasing owns suppliers, procurement documents, commercial prices, invoice matching and its workflows. Inventory stays
the only owner of physical stock and its valuation: Purchasing never writes Inventory tables and posts stock only
through `IPurchaseStockPosting`.

## 2. Approval (global rule)
- Approval is decided by the granted permission (`purchasing.documents.approve`), the warehouse scope of the document
  and its state. **The creator or submitter may approve their own document.** No creator/approver comparison exists.
- Whether a document *needs* approval:
  * purchase requests and purchase orders: a configurable policy per document type (`/api/purchasing/policies`,
    company-wide users with `purchasing.settings.manage`), seeded *required*. Each document copies the policy when it
    is created; without approval, submitting approves and numbers it at once;
  * supplier invoices: only when a line's net unit price differs from the order's (purchase price variance);
  * supplier returns: always.
- Who created, submitted, approved, rejected (with the reason), closed or cancelled is kept on the document and in
  its event history.

## 3. Purchase requests (optional)
- One physical warehouse per request (its branch numbers it); lines are active untracked stock items with an active
  product unit.
- Orders of the same warehouse draw on approved request lines (same product unit). A request line never has more on
  order than was approved; partial conversion keeps the remaining quantity; a cancelled order (or a draft order that
  drops the line) gives its quantity back. Closing a short-received order does not.
- Statuses: Draft, Submitted, Approved, Ordered (every line fully on order), Closed (by hand), Cancelled (nothing ordered).

## 4. Purchase orders
- One supplier and one physical warehouse per order; the branch comes from the warehouse. Inactive suppliers can't be
  chosen for new orders or approvals; approved orders are still received, invoiced and returned.
- Company base currency only (V1); foreign currency is deferred.
- Prices are tax-exclusive. Each line has a discount percent and a VAT rate. Amounts are rounded per line to the
  currency's minor unit, half away from zero: gross, discount, net, tax, total; totals are the sums of the lines. The
  line's net unit price (8 decimals) is the provisional inventory cost and the price invoices are matched against.
- Tax treatment is explicit per line. Only **recoverable** tax is supported and it never enters inventory cost; a
  line declaring non-recoverable tax is refused (`Purchasing.Line.NonRecoverableTaxNotSupported`), never silently
  treated as recoverable. Catalog's tax master data stays deferred.
- Statuses: Draft, Submitted, Approved (numbered), PartiallyReceived, Received, Closed, Cancelled. Lines change only
  in draft. Cancellation only before anything is received; after a partial receipt the rest is closed with a reason.
  An order becomes Received automatically and returns never reopen it.

## 5. Goods receipts
- Against an approved order, one or more receipts, each posted in one step with an `Idempotency-Key`: a retry
  answers the first receipt; the same key with another request is refused.
- Zero over-receipt tolerance, enforced by the order (locked for the receipt) and by a database check
  (`ReceivedQuantity <= Quantity`); concurrent receipts of the same order serialize.
- Quantities in the order line's unit, into a location of the order's warehouse (its DEFAULT one by default).
- Inventory posts a `PurchaseReceipt` stock document at the order's net unit price, in the same transaction as the
  receipt; the receipt keeps the stock document's id and number and the stock document keeps the receipt's id
  (`SourceDocumentId`, unique). Inventory refuses to create, edit or reverse purchase receipts and returns itself.

## 6. Supplier invoices
- Each invoice references one approved order; an order may have several invoices.
- Lines match order lines; each bills at most what was received and not yet invoiced or returned before invoicing,
  checked when drafting and again, under the order's lock, when posting (so concurrent invoices can't double-bill;
  a database check backs it).
- A net price unlike the order's records a purchase price variance per line and makes the invoice need approval.
  **Posted inventory is never revalued in V1: the order price is a provisional cost; this financial treatment needs
  the accountant's approval before production financial reporting.**
- The supplier's invoice number is unique per supplier after normalization (letters and digits, upper case), among
  invoices that are not cancelled. Invoice date not after today (company time zone); due date defaults to invoice
  date plus the order's payment terms.
- Posting records the liability (totals, tax, due date) and numbers it; posted invoices are immutable. No payments,
  AP ledger or general ledger in V1.

## 7. Supplier returns
- Against one posted goods receipt; each line at most what that receipt line brought in and was not returned yet.
- Always approved before posting (its creator may approve). Approving issues the goods through a `PurchaseReturn`
  stock document at Inventory's current moving average, refusing what is not physically there.
- Each returned quantity is first taken from goods not yet invoiced (they simply won't be invoiced), then from goods
  already invoiced: those become a **pending supplier credit claim** valued at the line's average invoiced net price and
  tax, for future Accounting. It is not a posted credit note.

## 8. Suppliers
- Code unique and fixed. Tax registration number unique per country, commercial registration number unique per country
  and registry office, both normalized (separators dropped, Arabic digits read as digits); an Egyptian tax number has
  9 digits. Similar normalized names only warn (`GET /api/purchasing/suppliers/similar`).
- Contacts, addresses, payment terms; temporal history like all master data.
- (2026-10-29, owner) Country, governorate and city are linked drop-down lists from lookup tables; governorate and city
  are required in every address (ADR 0017).

## 9. Products
Only active stock items without batch or serial tracking (Inventory's own V1 limit); anything else is refused with
its own error code.
