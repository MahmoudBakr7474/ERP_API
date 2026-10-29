# ADR 0019 — POS V1: terminals, cashier shifts, checkout, receipts, returns and refunds

Status: accepted (2026-10-29), on `feature/pos`. The product owner approved the scope and decisions in
[`docs/modules/POS/V1_DECISIONS.md`](../modules/POS/V1_DECISIONS.md), including the global approval rule in `CLAUDE.md`
(*Document approvals*); this ADR records how the module and its Inventory, Sales and Catalog contracts implement them.

## Context
Retail tills needed fast immediate sales from a shop's stock, cash and standalone card payments, a drawer reconciled per
cashier shift, printable receipts and returns — without a second price or VAT engine, without the Sales order →
delivery → invoice workflow per receipt, and without POS ever writing Inventory's tables. Sales orders already reserve
stock (ADR 0018), so a till must never sell what an order holds, and two tills must never sell the last unit twice. The
physical card charge happens on a device outside the database transaction.

## Decision
1. **Module layout.** `WarehouseERP.Pos` (+ `.Domain`, `.Application`, `.Contracts`), schema `pos`, like every module
   (ADR 0001, 0011). Other modules only through their Contracts: Organization (company, branches, warehouses, locations),
   Catalog (`IProductCatalog`), Sales (`ISalesPricing`, `ICustomerDirectory`), Inventory (`IPosStockPosting`,
   `IStockAvailabilityLookup`), Identity (`IUserDirectory`). Architecture tests discover it from the host.
2. **Inventory: two generated stock document types** (`Migration11_InventoryPos`, check constraints only):
   `PosSale = 13` (`PSL-`) and `PosReturn = 14` (`PSR-`), posted only through `IPosStockPosting` inside the caller's
   transaction by the same engine as every document (`StockDocumentPosting.ApplyAsync`): warehouse applock, count freezes,
   location balances, no negative stock, moving average, numbering. A sale is an ordinary **unreserved** issue
   (`OnHandSellable − ReservedQuantity >= qty`) and checks `AllowsSelling`; a return comes back at the cost its sale line
   left with (the Sales-return valuation, generalized to `ReversesLineId` of a PosSale line, `ReturnExceedsSale`). They
   carry `SourceDocumentId`; Inventory refuses to draft, edit or reverse them (`Inventory.StockDocument.ManagedByPos`).
   `IStockAvailabilityLookup` reads warehouse availability and the selling location's balance without locks (a hint).
3. **Sales: pricing and VAT stay in Sales.** `ISalesPricing.PriceLinesAsync` prices lines with the list price of the
   product unit, the product's **tax classification** and the line discount through the same `LineAmounts.Calculate` as
   sales documents (with no lines it only answers the currency and its decimals); `FindUnitPricesAsync` feeds the scan
   screen. The new `sales.ProductTaxes` table (`Migration12_SalesProductTaxes`, temporal, one row per product:
   `Standard` with an optional own rate — null takes `SalesSettings.DefaultVatRate` — or `ZeroRated`/`Exempt` without a
   rate) is managed under `/api/sales/product-taxes` with `sales.prices.manage`. Sales documents keep their per-line
   treatment unchanged. `ICustomerDirectory` exposes customers (find, search active) as projections.
4. **Catalog:** `IProductCatalog` gains `FindUnitByBarcodeAsync` (only fully active barcode/unit/product),
   `SearchActiveAsync` and `FindUnitsOfProductsAsync`.
5. **Terminals** (`pos.PosTerminals`, temporal): unique fixed code, fixed warehouse (physical, selling), changeable
   selling location, payment methods (at least one, `CK_PosTerminals_PaymentMethod`) and receipt texts.
6. **Shifts** (`PosShifts`, `PosCashMovements`, `PosShiftEvents`): running totals on the shift row (net cash sales, cash
   refunds, cash in/out, card sales/refunds, sales/refund totals and counts). Filtered unique indexes
   `UX_PosShifts_OpenTerminal` / `UX_PosShifts_OpenCashier` (`[Status] = 1`) make one open shift per terminal and per
   cashier race-safe; `CK_PosShifts_Drawer` keeps the drawer ≥ 0; `CK_PosShifts_Closed` ties the closed state to count,
   expected cash and difference. Cash movements and the close lock the shift (`UPDLOCK`) at the client's row version, so a
   retried click can't move cash twice. Closing someone else's shift needs `pos.shifts.manage` and a note (forced).
7. **Checkout** (`PosSales`, `PosSaleLines`, `PosSalePayments`) in one `ITransactionRunner` transaction, lock order:
   idempotency-key applock → the terminal's open shift (`UPDLOCK`; checkouts of one till queue) → Sales pricing →
   per-terminal number (`PosNumberSequences`, locked row) → Inventory (warehouse applock, stock rows, balances, valuation,
   Inventory number). The sale, payments, stock document, shift totals, number and `pos.IdempotencyRecords` row commit
   together. Fingerprint = SHA-256 of the operation, the request body without its key, and the user. Payment arithmetic is
   enforced in the domain and again by `CK_PosSales_Payments` / `CK_PosSalePayments_*` (cash received = tendered −
   change; cards ≤ total; received + cards = total; card rows carry a reference, no change; only `[0-9]{4}` last digits).
8. **Card incidents** (`PosCardIncidents`, unique per idempotency key): when a checkout with card payments is refused, the
   handler records the card references, amount and the refusal in a second transaction after the rollback (the change
   tracker is cleared by the runner) and answers `Pos.Checkout.CardReversalRequired`. A later successful retry with the same
   key marks it `SaleRecorded` in the checkout transaction; otherwise `pos.shifts.manage` resolves it with a note. A
   validation failure (400) or `Pos.Idempotency.Busy` records nothing; clients call `POST /api/pos/sales/quote` before
   charging a card.
9. **Returns** (`PosReturns`, `PosReturnLines`, `PosRefunds`): idempotency applock → the caller's open shift → the sale
   (`UPDLOCK`) → window check (company calendar) → cumulative shares per sale line (`RefundedNet/TaxAmount` on the line;
   `CK_PosSaleLines_Returned` backs quantities and amounts) → refunds exactly equal to the lines, each method capped by
   what was paid that way (`CK_PosSales_Refunds`) and cash by the drawer → number → `PosReturn` stock document into the
   terminal's selling location. `CK_PosReturnLines_Condition` = fit for resale only.
10. **Receipts:** `PosSaleResponse`/`PosReturnResponse` are receipt-shaped (company, branch, terminal texts, shift,
    cashier, lines with tax-inclusive unit price, VAT per rate, payments, returns), `isTaxInvoice: false`. The checkout
    and return responses are originals; `POST …/reprint` counts copies (`PrintCount`, last printer) and returns
    `isCopy: true` with its copy number; a plain GET reads as a copy and prints nothing.
11. **Access:** nine permissions in `PosPermissions.All`; endpoints check the permission, handlers the terminal's
    warehouse scope (scope before existence) and shift ownership. Settings (`pos.PosSettings`, one row, seeded 10% / 14
    days) need company-wide access. No creator/approver comparison anywhere.

## Consequences
- Three additive migrations: `Migration11_InventoryPos` (widens `CK_StockDocuments_Type` to 1–14 and the source check to
  9–14), `Migration12_SalesProductTaxes` (one temporal table), `Migration13_Pos` (`pos` schema, 14 tables, settings seed).
  Migrations 01–10 are untouched; each `Down` reverses exactly its `Up` (11's `Down` fails while POS stock documents
  exist, like 09's).
- OpenAPI: +24 paths (22 under `/api/pos`, 2 under `/api/sales/product-taxes`), +50 schemas, enum members `PosSale` and
  `PosReturn` on `StockDocumentType`; `SalesTaxTreatment` lost a stray `null` member (nullability stays on
  `SalesLineRequest.taxTreatment` through `oneOf`). No existing path or schema changed otherwise.
- `IStockDocumentRepository.FindDeliveredLinesAsync` takes the outgoing document type; Inventory valuation reads PosSale /
  PosReturn lines like sales deliveries / returns.
- The Inventory frontend needs labels for the two stock document types; the Sales frontend a product tax screen.

## Not done (backlog)
Offline POS and device registration (ADR 0009), integrated card gateways, wallets/InstaPay, held carts, loyalty and
promotions, header discounts, manual till prices, cash rounding and denomination counts, barcode scales, quarantine for
damaged returns, cash-drawer/printer hardware, credit sales, GL posting, batch/serial products.

## Production prerequisites
1. **Egyptian ETA e-Receipt**: registration, product coding (EGS/GS1), POS device registration and integration before
   POS issues receipts in production where the law requires electronic receipts. V1 receipts are internal commercial
   receipts and claim no compliance.
2. Every product sold at a till has a reviewed tax classification (standard/zero-rated/exempt, own rates).
3. The accountant approves the cost treatment (sales at the moving average, returns at the cost they left with) and the
   handling of card incidents and cash differences until Accounting exists.
4. Inventory's (ADR 0015), Purchasing's (ADR 0016) and Sales' (ADR 0018) prerequisites still apply.
