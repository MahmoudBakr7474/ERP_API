# Sales V1 — approved decisions

Status: approved by the product owner on 2026-10-29 ("Sales V1 Final Owner Approval & Implementation"), from the
decision checklist proposed the same day, with the owner's corrections. The permanent document self-approval rule of
`CLAUDE.md` (*Document approvals*) applies throughout. This file replaces the checklist; where they differ, this file
wins. How the code implements it: [ADR 0018](../../adr/0018-sales-module.md).

## 1. Scope and workflow
Customers → quotations (optional) → sales orders → stock reservations → picking and partial or full deliveries →
sales invoices → customer returns. Single company, multi-branch, multi-warehouse trading and distribution.

Sales owns customers, the selling price list, commercial documents, their workflows and credit information. Inventory
stays the only owner of physical stock, reservations and valuation: Sales never writes Inventory tables and works on
stock only through `IStockReservations` and `ISalesStockPosting`. POS and Accounting are not started.

## 2. Approval (global rule)
- Who approves: whoever holds `sales.documents.approve`, with the document's warehouse in scope, for a document in the
  right state. **The creator or submitter may approve their own document.** No creator/approver comparison exists.
- Whether a document *needs* approval comes from the sales settings (`/api/sales/settings`, company-wide users with
  `sales.settings.manage`), **seeded so that nothing needs approval**:
  * a policy per type: quotations require approval / orders require approval (both off);
  * an optional discount threshold: a line discount *above* it needs approval (none by default; a threshold is never
    set to zero implicitly);
  * optional: manual prices need approval; manual prices below the list price need approval (both off);
  * a credit hold either blocks confirmation (default) or makes the order need approval.
- Submitting computes the reasons (`Policy`, `DiscountAboveThreshold`, `PriceBelowList`, `ManualPrice`, `CreditHold`);
  with none, submitting issues the quotation or confirms the order at once. Customer returns always need approval.
- Created, submitted, approved, rejected (with reason), declined, cancelled, closed, reduced, delivered, invoiced and
  returned are kept on the document and in its event history.

## 3. Customers
- Company-level master data (no warehouse scope), temporal history. Code unique and fixed; type Individual or Business
  (fixed). Contacts and addresses; an address names a city of Organization's lookups (country → governorate → city).
- Tax registration number unique per country, normalized (separators dropped, Arabic digits read as digits); an
  Egyptian one has 9 digits. Commercial registration only for businesses, unique per country and registry office.
  Jurisdiction-specific checks apply only where they are relevant (Egypt).
- Search, paging and filters (text, type, active, credit hold, country). Likely duplicates (similar normalized name or
  the same mobile) only warn: `GET /api/sales/customers/similar`.
- An inactive customer can't be put on a new or changed quotation or order, nor have one issued, converted or
  confirmed. Documents already confirmed continue.

## 4. Prices, discounts and tax
- One company-wide sales price list: at most one price per product unit, in the company's base currency, tax-exclusive,
  with temporal history. Maintained by users with `sales.prices.manage`.
- A line takes the list price of its product unit as a **snapshot**. Only a user with `sales.prices.manage` may set a
  manual price, where the list has none or instead of it (`Sales.Line.ManualPriceNotAllowed`, 403). A line with no
  price is refused (`Sales.Line.PriceMissing`); a document is never confirmed with a missing or invalid price. Later
  list changes never touch existing documents.
- Line discount in percent (0–100). Tax per line: **standard** (rate above 0, the settings' default VAT when omitted —
  14% initially, editable, not forced), **zero-rated** or **exempt** (rate 0). A rate on a zero-rated or exempt line, or
  an unknown treatment, is refused, never silently changed.
- Amounts rounded per line to the currency's minor unit, half away from zero, as in Purchasing: gross, discount, net,
  tax, total; document totals are sums of lines. Prices, discounts and taxes are stored on every document line.

## 5. Quotations
- One customer and one selling warehouse each. Draft → (Submitted) → Issued (numbered `QT-`) → Converted, Declined or
  Cancelled. Valid until a date (default: today + the settings' validity days); an issued quotation past it shows as
  expired and can't be converted.
- Converting creates a draft order with the quotation's lines and prices; the quotation records the order.
- A quotation reserves nothing.

## 6. Sales orders and reservations
- One customer and one selling warehouse each; the branch comes from the warehouse. Draft → (Submitted) → Confirmed
  (numbered `SO-`) → PartiallyDelivered → Delivered, or Closed; Cancelled before anything is delivered.
- **Confirmation reserves every line all-or-nothing** through Inventory, in the same transaction; a shortfall refuses
  the confirmation (`Inventory.Stock.Insufficient`) and the order stays as it was. Reservations are made only from
  unreserved on-hand stock with a conditional update under the warehouse lock, so concurrent orders never reserve the
  same units.
- Reservations are released for what is cancelled, reduced (a line reduced with a reason) or closed (the rest of a
  partially delivered order, with a reason), consumed by deliveries, and kept linked to their order line. An order
  never changes another order's reservations.
- A loss that leaves less on hand than reserved shows as **OverReserved** in Inventory's reports; no reservation is cut
  automatically and deliveries still need the physical stock.
- Credit hold: the policy in §2; when "credit hold blocks deliveries" is on (default), deliveries of a held customer are
  refused (`Sales.Customer.OnCreditHold`).
- Backorders are deferred.

## 7. Picking and deliveries
- A delivery drafts a picking list for one confirmed order: lines per order line and location (several locations per
  line allowed, the warehouse's default when none is given). It never exceeds what remains on the order
  (`Sales.Order.OverDelivery`).
- Posting (with an `Idempotency-Key`) numbers it (`DN-`) and makes Inventory post a `SalesDelivery` stock document
  (type 11, `SDL-`): it consumes the order line's reservation, lowering on hand and reserved together, at the moving
  average cost, and still needs the stock in **each picked location** (`Inventory.Stock.InsufficientAtLocation`) and in
  the warehouse. Atomic, idempotent and safe under concurrency: concurrent deliveries of one order serialize on the
  order and can't over-deliver. A refused posting changes nothing.

## 8. Sales invoices
- One order per invoice, several partial invoices per order. Each line bills at most what was delivered and not yet
  invoiced or returned before invoicing, checked when drafting and again under the order's lock when posting
  (concurrent invoices can't double-bill; a database check backs it).
- Prices, discounts and taxes come from the order. Invoice date not after today (company time zone); due date = invoice
  date + the order's payment terms. Totals, taxes and due date are stored.
- Posting (with an `Idempotency-Key`) numbers it (`INV-`); posted invoices are immutable. No payments, receivables
  ledger or general ledger in V1. **Electronic invoicing (Egyptian ETA) is deferred and is a production prerequisite.**

## 9. Customer returns
- Against one posted delivery; each line at most what that delivery line delivered and was not returned yet.
- Condition per line: only **fit for resale** is accepted and restocked. Damaged or uninspected goods are refused
  explicitly (`Sales.Return.ConditionNotSupported`) until Inventory has quarantine or non-sellable stock (backlog).
- Always approved before posting (self-approval allowed), with an `Idempotency-Key`. Approving numbers it (`SR-`) and
  makes Inventory post a `SalesReturn` stock document (type 12, `SRT-`) into sellable stock at **the cost its delivery
  line left with**.
- Each returned quantity is first taken from goods not invoiced yet (they simply won't be invoiced), then from goods
  already invoiced: those become a **pending customer credit** valued at the line's average invoiced net price and
  tax. It is not a credit note and nothing is refunded.

## 10. Credit
- Per customer: payment terms (days), an optional credit limit, and a manual credit hold with a reason
  (`sales.credit.manage`), enforced per the settings.
- **No hard credit-limit check in V1.** `GET /api/sales/customers/{id}/exposure` shows uninvoiced confirmed orders,
  delivered-not-invoiced, invoiced and pending credits, labelled **partial** (`isPartial: true`): payments are not
  tracked until Accounting, so it is informational only.

## 11. Access
- Customers and prices are company-wide; quotations, orders, deliveries, invoices and returns follow the warehouse scope
  of their document (`Sales.Access.WarehouseDenied`, 403); sales settings need company-wide access.
- Only active, untracked stock items in a warehouse that allows selling (Inventory's own V1 limits).
