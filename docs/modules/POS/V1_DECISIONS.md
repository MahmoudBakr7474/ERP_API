# POS V1 — approved decisions

Status: approved by the product owner on 2026-10-29 ("POS V1 Final Owner Approval"), from the decision checklist proposed
the same day, with the owner's corrections. The permanent document self-approval rule of `CLAUDE.md` (*Document
approvals*) applies throughout. This file replaces the checklist; where they differ, this file wins. How the code
implements it: [ADR 0019](../../adr/0019-pos-module.md).

## 1. Scope and workflow
POS terminal → cashier shift → scanning or searching items → cart (client side) → quote → checkout → payment → stock
posted through Inventory → receipt (original, then counted copies) → returns and refunds → shift close and cash
reconciliation. Single company, multi-branch, multi-warehouse trading and distribution; no manufacturing.

- **Online only in V1.** Offline selling is a later phase (it needs device registration and offline grants, ADR 0009).
- POS records its own sales and returns (`pos` schema). A checkout never creates a Sales order, delivery or invoice.
- Reused, never duplicated: Identity (users, permissions, warehouse scope), Organization (company, branches,
  warehouses, locations), Catalog (products, units, barcodes), Sales (price list, product tax classification, line amount
  rules, customers), Inventory (stock, movements, availability, locks, count freezes, valuation).

## 2. Terminals
- One **physical warehouse that allows selling** per terminal; the branch comes from the warehouse. A **selling location**
  of that warehouse (default: its DEFAULT location) is where sales leave and returns come back.
- Code unique and **fixed** (upper-case letters, digits, dashes); the warehouse is fixed too (another warehouse = another
  terminal). Name, selling location, accepted payment methods (cash and/or card, at least one) and receipt header/footer
  may change. Master data with temporal history.
- A terminal with an open shift can't be deactivated. Managing terminals needs `pos.terminals.manage` and the warehouse in
  scope. No hardware device binding in V1.

## 3. Cashier shifts and cash
- **One open shift per terminal and one per cashier** (database-enforced). Only the shift's cashier sells and refunds on it.
- Opening float counted at opening. Cash in / cash out with a reason (`pos.cash.manage`, the cashier or a shift manager);
  never more cash out than the drawer holds.
- Tracked per shift: opening float, **net cash sales** (tendered − change), cash refunds, cash in, cash out, expected
  drawer, counted cash, difference, **card sales and card refunds kept outside the drawer**, sales and refund totals and
  counts.
- Close: counted cash; expected = float + net cash sales − cash refunds + cash in − cash out; **a non-zero difference
  needs a note**. A shift manager (`pos.shifts.manage`) may **force-close** an abandoned shift, always with a note; it is
  recorded as forced, by the manager. Closed shifts never change. Every step is kept in the shift's history.

## 4. Customers
- Default **walk-in**: no customer record; an optional name and phone are printed on the receipt only.
- Optionally a **registered, active Sales customer** (snapshot of name, mobile, tax number on the sale). No credit sales.

## 5. Prices, discounts and taxes
- **Sales' price list per product unit** (tax-exclusive, base currency), taken as a snapshot. No manual prices in POS V1;
  a unit without a list price is refused (`Sales.Line.PriceMissing`).
- **No VAT engine in POS:** Sales prices every line (`ISalesPricing`) with the same `LineAmounts` rules as sales
  documents (per-line rounding to the currency's minor unit, half away from zero).
- **Product-level tax classification** (owner correction): every product sold at a till must be classified in Sales as
  **standard-rated** (the default VAT, 14% seeded, or its own rate), **zero-rated** or **exempt**. The cashier never
  chooses VAT. An unclassified product is refused at checkout (`Sales.ProductTax.NotClassified`). Classifying needs
  `sales.prices.manage`. This is deliberately not a tax-management subsystem (no tax codes, effective dates or ETA codes).
- Prices stay tax-exclusive; the till and the receipt show the **tax-inclusive** unit price and totals clearly.
- **Line discounts** in percent need `pos.discounts.apply` and never exceed the **POS settings' maximum (seeded 10%)**.
  No manager approval or override at the till.

## 6. Payments
- **Cash**, **card** (standalone card terminal, not integrated) and **split** payments.
- Rules: card payments never exceed the amount due; cash tendered pays the rest; **change comes out of cash only**;
  card + (cash tendered − change) = total exactly; one cash line per sale; cash isn't accepted when cards already cover the
  total. Gross cash tendered, change given and net cash received are recorded separately; only net cash counts in the
  drawer.
- A card payment needs the card terminal's **authorization/reference number**; optionally the last four digits. Full card
  numbers, CVV or any credential are never accepted or stored.
- Each payment records method, amount, tendered, change, status (`Settled` for cash, `ExternallyConfirmed` for cards),
  reference, and — through its sale — cashier, shift and timestamps.

## 7. Checkout, Inventory and failures
- **Immediate checkout, no cart reservations.** The cart lives on the screen; a **quote** prices it and hints at stock
  before a card is charged.
- One SQL transaction commits together, or nothing: the POS sale, its payments, the PosSale stock document (Inventory),
  the shift's totals, the receipt number and the idempotency record.
- Inventory: new stock document types **PosSale = 13** and **PosReturn = 14** through **`IPosStockPosting`**; a sale takes
  **only unreserved stock** (never what Sales orders reserved) under the warehouse lock, needs the stock in the selling
  location, is blocked by **count freezes**, and leaves at the moving average. Inventory refuses to draft, edit or reverse
  POS stock documents (`Inventory.StockDocument.ManagedByPos`). `IStockAvailabilityLookup` gives tills a stock hint.
- **Idempotency:** checkouts and returns carry an `Idempotency-Key`; a retry with the same key gets the first receipt
  back; the same key with another request is refused (409). For uncertain outcomes the client retries with the same key.
- **The physical card charge is not part of the SQL transaction.** When a checkout with card payments is refused, POS
  answers `Pos.Checkout.CardReversalRequired` and keeps a **card incident** (references, amount, terminal, shift, cashier,
  reason) recorded on its own after the rollback. A later retry with the same key that records the sale marks it
  `SaleRecorded`; otherwise a shift manager resolves it with a note after reversing the charge on the card terminal. A
  failed checkout is never reported as a sale.

## 8. Returns and refunds
- Against a **completed POS sale**, at a terminal of the **same warehouse**, on the caller's open shift, within the
  **return window (POS settings, seeded 14 days**, company calendar).
- Partial and full returns; cumulative quantities never exceed what was sold, also under concurrent returns (sale row
  lock + database check).
- Original price, discount, VAT and cost are preserved. Partial refunds use **cumulative shares** of each sale line, so
  the last units take exactly what is left and returns always add up to the sale.
- **Only goods fit for resale** come back into sellable stock (PosReturn, at the cost they left with). **Damaged or
  uninspected goods are refused** (`Pos.Return.ConditionNotSupported`) until Inventory supports quarantine.
- Refunds add up exactly to the returned goods; each method at most what the sale was paid that way and not yet refunded
  (cash refunds also need the drawer to hold them). Card refunds record their external reference; no electronic
  settlement is claimed.
- `pos.returns.manage` processes returns and refunds, the user's own sales included. No second person, no approval step.

## 9. Receipts and tax compliance
- Unique numbers per **terminal and fiscal year** (company calendar year), taken inside the transaction:
  `CAI-T1-2026-000001` (sale), `CAI-T1-R-2026-000001` (return), `CAI-T1-SH-2026-000001` (shift).
- Printable through standard browser printing: company, branch, terminal, cashier, shift, date and time zone, items with
  units, quantities, prices (with and without VAT), discounts, VAT per line and per rate, payments, cash tendered and
  change, customer, returns made against the sale.
- The checkout response is the original; **copies** (`pos.receipts.reprint`) are counted and marked COPY.
- **The V1 receipt is an internal commercial receipt, not a tax invoice and not an Egyptian electronic tax receipt.**
  ETA e-Receipt registration, product coding (EGS/GS1), POS device registration and integration are production
  compliance work; no legal compliance is claimed until they are implemented and verified.

## 10. Permissions and scope
| Permission | Allows |
|---|---|
| `pos.read` | Read terminals, shifts, sales, returns, receipts, card incidents |
| `pos.sell` | Open/close one's own shift, scan and search, customers, quote, checkout |
| `pos.discounts.apply` | Line discounts up to the settings' maximum |
| `pos.cash.manage` | Cash in / cash out |
| `pos.returns.manage` | Returns and refunds |
| `pos.receipts.reprint` | Counted receipt copies |
| `pos.shifts.manage` | Force-close abandoned shifts, cash on another's shift, resolve card incidents |
| `pos.terminals.manage` | Register and configure terminals |
| `pos.settings.manage` | POS settings (company-wide users only) |

Everything follows the warehouse scope of the terminal. Self-approval rule applies; no maker-checker anywhere.

## 11. Deferred (backlog, not V1)
Offline POS; integrated payment gateways; mobile wallets and InstaPay; held/parked carts; loyalty; promotions; barcode
scales (weight/price barcodes); full fiscal / ETA e-Receipt integration; GL posting; quarantine inventory for damaged
returns; cash-drawer, customer-display and printer hardware beyond browser printing; manual prices at the till; header
(whole-sale) discounts; cash rounding; denomination counting; tax-inclusive retail price list; credit sales; batch/serial
tracked products (Inventory prerequisite).
