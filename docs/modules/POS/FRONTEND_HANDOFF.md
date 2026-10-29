# POS — frontend handoff

Verified 2026-10-29 on `feature/pos` against the real API. An end-to-end run used a throwaway LocalDB database (all
thirteen migrations), the real host over HTTPS, real cookie sessions and anti-forgery, a bootstrap administrator and four
users created through Identity: `cashier1` (sell, discounts, cash, returns) and `cashier2` (sell only) in the shop
warehouse, a `supervisor` (shifts, card incidents, receipt copies) in the shop, and an `outsider` cashier scoped to another
warehouse. 60 checks passed; the API log had no 500s and no passwords; the database was then dropped. The examples below
are real responses (ids shortened).

The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md). Business rules: [`V1_DECISIONS.md`](V1_DECISIONS.md) and
[ADR 0019](../../adr/0019-pos-module.md).

## Where it goes
- The screens extend the existing ERP frontend (Lovable project `mahmoudtaha311/screenshot-match`, cloned to
  `D:\work\WarehouseERP\warehouse-erp-web`), following its `AGENTS.md` UI conventions; code in `src/features/pos/`.
- Running the backend, the Vite proxy and the cookie/XSRF rules are the same as for
  [Organization](../Organization/FRONTEND_HANDOFF.md).
- OpenAPI tags: `PosSettings`, `PosTerminals`, `PosItems`, `PosCustomers`, `PosShifts`, `PosSales`, `PosReturns`,
  `PosCardIncidents`, and Sales' `ProductTaxes`.
- **`WarehouseERP_Dev` does not have `Migration11`–`Migration13` yet**, and the dev API on `https://localhost:49571` runs
  the `main` build. Applying them (after a copy-only backup, as for Sales) and restarting the dev API on the `feature/pos`
  build is the owner's call before the screens are tried there. Then classify the products sold at tills
  (`/api/sales/product-taxes`) and register a terminal.
- A cashier needs only `pos.sell` (+ `pos.read` to pick a terminal and see sales; `pos.discounts.apply`,
  `pos.cash.manage`, `pos.returns.manage` as the shop decides). Item lookup and customer search go through POS endpoints,
  so no Catalog or Sales permission is needed at the till. Back-office terminal forms also need
  `organization.warehouses.read` (warehouse and location pickers); links to stock documents need
  `inventory.documents.read`.
- **Self-approval:** a user holding `pos.returns.manage` refunds their own sales; there is no second-person step.

## Endpoints
All under `/api/pos` unless noted. Lists are paged `{ items, page, pageSize, totalCount, totalPages }` and limited to the
user's warehouses.

| Method | Route | Permission | Body / notes |
|---|---|---|---|
| GET | `/settings` | signed in | `{ maxLineDiscountPercent, returnWindowDays, updatedAtUtc, rowVersion }` |
| PUT | `/settings` | `pos.settings.manage` + company-wide | `{ maxLineDiscountPercent, returnWindowDays, rowVersion }` → `{ rowVersion }` |
| GET | `/terminals?page&pageSize&warehouseId&isActive&search`, `/terminals/{id}` | `pos.read` | terminal with open shift (`openShiftNumber`, `openShiftCashierName`) |
| POST | `/terminals` | `pos.terminals.manage` | `{ code, name, warehouseId, sellingLocationId?, acceptsCash, acceptsCard, receiptHeader?, receiptFooter? }` → 201 |
| PUT | `/terminals/{id}` | `pos.terminals.manage` | `{ name, sellingLocationId?, acceptsCash, acceptsCard, receiptHeader?, receiptFooter?, rowVersion }` |
| POST | `/terminals/{id}/activate`, `/deactivate` | `pos.terminals.manage` | 204 |
| GET | `/terminals/{id}/current-shift` | `pos.sell` | the caller's open shift there, 404 `Pos.Shift.NotFound` |
| GET | `/terminals/{id}/items/lookup?barcode=` | `pos.sell` | one `PosItemResponse` |
| GET | `/terminals/{id}/items?search=&take=` | `pos.sell` | array: every active unit of up to `take` (20) products |
| GET | `/customers?search=&take=` | `pos.sell` | active registered customers (array) |
| POST | `/shifts` | `pos.sell` | `{ terminalId, openingFloat }` → 201 `{ id }` |
| GET | `/shifts?page&pageSize&terminalId&warehouseId&cashierUserId&status`, `/shifts/{id}` | `pos.read` | summary rows / full report |
| POST | `/shifts/{id}/cash-movements` | `pos.cash.manage` | `{ movementType: CashIn|CashOut, amount, reason, rowVersion }` → `{ rowVersion }` |
| POST | `/shifts/{id}/close` | own shift `pos.sell`, others `pos.shifts.manage` | `{ countedCash, note?, rowVersion }` → the closed report |
| POST | `/sales/quote` | `pos.sell` | `{ terminalId, lines }` → priced cart, nothing recorded |
| POST | `/sales` | `pos.sell` | **Idempotency-Key**; `{ terminalId, customerId?, customerName?, customerPhone?, notes?, lines, payments }` → receipt |
| GET | `/sales?page&pageSize&terminalId&shiftId&warehouseId&customerId&cashierUserId&search&from&to`, `/sales/{id}` | `pos.read` | `from`/`to` are company dates |
| POST | `/sales/{id}/reprint` | `pos.receipts.reprint` | receipt with `isCopy: true`, `copyNumber` |
| POST | `/returns` | `pos.returns.manage` | **Idempotency-Key**; `{ terminalId, saleId, reason, notes?, lines[{ saleLineId, quantity, condition }], refunds }` |
| GET | `/returns?page&pageSize&terminalId&shiftId&saleId&warehouseId&search&from&to`, `/returns/{id}` | `pos.read` | |
| POST | `/returns/{id}/reprint` | `pos.receipts.reprint` | |
| GET | `/card-incidents?page&pageSize&terminalId&shiftId&status` | `pos.read` | |
| POST | `/card-incidents/{id}/resolve` | `pos.shifts.manage` | `{ note, rowVersion }` → `{ rowVersion }` |
| GET | `/api/sales/product-taxes?page&pageSize&productId&taxTreatment` | `sales.documents.read` | |
| PUT, DELETE | `/api/sales/product-taxes/{productId}` | `sales.prices.manage` | `{ taxTreatment, taxRate }` → 204; delete → 204 |

Cart line: `{ productId, productUnitId, quantity, discountPercent }`. Payment / refund line: `{ method: "Cash" | "Card",
amount, reference, cardLast4 }` — for cash paid in, `amount` is what the customer handed over; a card needs `reference`
(the card terminal's authorization number) and may carry exactly four `cardLast4` digits (400 otherwise).

## Examples (real responses)
Scan — `GET /api/pos/terminals/{id}/items/lookup?barcode=6221234500010`:
```json
{ "productId": "9c4b…4662", "sku": "E2E-TEA", "productName": "شاي", "productUnitId": "a548…4670", "unitId": "638f…464d",
  "unitCode": "PCS", "unitName": "قطعة", "conversionFactor": 1, "isBaseUnit": true, "unitAllowsFraction": false,
  "currencyCode": "EGP", "unitPrice": 10, "unitPriceWithTax": 11.4, "taxTreatment": "Standard", "taxRate": 14,
  "availableBaseQuantity": 30, "availableQuantity": 30, "canSell": true, "problemCode": null }
```
An unclassified product comes back with `"canSell": false, "problemCode": "Sales.ProductTax.NotClassified"`; a unit
without a list price with `"Sales.Line.PriceMissing"`.

Checkout — 2 tea at 10 with 5% discount (standard 14%) + 4 sugar at 5 (zero-rated), cash 50 → receipt (abridged):
```json
{ "id": "4924…4a6c", "number": "CAI-T1-2026-000001", "isTaxInvoice": false, "isCopy": false, "copyNumber": null,
  "printCount": 1, "companyName": "شركة الاختبار", "timeZoneId": "Africa/Cairo", "branchCode": "CAI", "branchName": "القاهرة",
  "warehouseCode": "SHOP", "terminalCode": "CAI-T1", "receiptHeader": "متجر الاختبار - القاهرة",
  "receiptFooter": "الاستبدال خلال 14 يوماً", "shiftNumber": "CAI-T1-SH-2026-000001", "cashierName": "cashier1",
  "customerName": "زبون نقدي", "currencyCode": "EGP", "currencyDecimals": 2, "soldAtUtc": "2026-10-29T13:55:28.824Z",
  "lines": [
    { "lineNumber": 1, "sku": "E2E-TEA", "productName": "شاي", "unitCode": "PCS", "quantity": 2, "unitPrice": 10,
      "unitPriceWithTax": 11.4, "discountPercent": 5, "grossAmount": 20, "discountAmount": 1, "netAmount": 19,
      "taxTreatment": "Standard", "taxRate": 14, "taxAmount": 2.66, "totalAmount": 21.66, "returnedQuantity": 0,
      "returnableQuantity": 2 },
    { "lineNumber": 2, "sku": "E2E-SUGAR", "productName": "سكر", "unitCode": "PCS", "quantity": 4, "unitPrice": 5,
      "unitPriceWithTax": 5, "discountPercent": 0, "grossAmount": 20, "discountAmount": 0, "netAmount": 20,
      "taxTreatment": "ZeroRated", "taxRate": 0, "taxAmount": 0, "totalAmount": 20, "returnedQuantity": 0,
      "returnableQuantity": 4 } ],
  "taxBreakdown": [ { "taxTreatment": "Standard", "taxRate": 14, "netAmount": 19, "taxAmount": 2.66 },
                    { "taxTreatment": "ZeroRated", "taxRate": 0, "netAmount": 20, "taxAmount": 0 } ],
  "grossAmount": 40, "discountAmount": 1, "netAmount": 39, "taxAmount": 2.66, "totalAmount": 41.66,
  "payments": [ { "lineNumber": 1, "method": "Cash", "status": "Settled", "amount": 41.66, "tenderedAmount": 50,
                  "changeAmount": 8.34, "reference": null, "cardLast4": null } ],
  "cashTenderedAmount": 50, "changeAmount": 8.34, "cashReceivedAmount": 41.66, "cardAmount": 0, "refundedAmount": 0,
  "refundedCashAmount": 0, "refundedCardAmount": 0, "returns": [], "stockDocumentId": "c3af…476d", "stockDocumentNumber": "PSL-CAI-2026-000001", "notes": null }
```
Split payment of a carton (125.40): `payments` = `[{ "method": "Card", "status": "ExternallyConfirmed", "amount": 100,
"tenderedAmount": 100, "changeAmount": 0, "reference": "AUTH-778899", "cardLast4": "4242" }, { "method": "Cash", "status":
"Settled", "amount": 25.4, "tenderedAmount": 50, "changeAmount": 24.6 }]`.

Card charged but checkout refused (50 tea, not in stock) → `422`:
```json
{ "status": 422, "errorCode": "Pos.Checkout.CardReversalRequired",
  "detail": "The sale was not recorded (Inventory.Stock.Insufficient: Not enough available stock of E2E-TEA in this warehouse.). Reverse card payment AUTH-555 (*1234) on the card terminal; card incident … keeps its reference until it is resolved." }
```
and `GET /api/pos/card-incidents?status=ReversalRequired` → `{ "terminalCode": "CAI-T1", "cashierName": "cashier1",
"cardAmount": 570, "references": "AUTH-555 (*1234)", "errorCode": "Inventory.Stock.Insufficient", "status":
"ReversalRequired", "attempts": 1, "saleId": null, "rowVersion": "AAAAAAAAJ00=" }`.

Return of one tea from that sale — `POST /api/pos/returns` with `lines: [{ saleLineId, quantity: 1, condition:
"FitForResale" }]`, `refunds: [{ method: "Cash", amount: 10.83 }]` → `{ "number": "CAI-T1-R-2026-000001", "saleNumber":
"CAI-T1-2026-000001", "lines": [{ "quantity": 1, "unitPrice": 10, "unitPriceWithTax": 11.4, "discountPercent": 5, "taxRate":
14, "netAmount": 9.5, "taxAmount": 1.33, "totalAmount": 10.83 }], "taxBreakdown": [{ "taxTreatment": "Standard", "taxRate":
14, "netAmount": 9.5, "taxAmount": 1.33 }], "totalAmount": 10.83, "refunds": [{ "method": "Cash", "status": "Settled",
"amount": 10.83 }], "cashRefundAmount": 10.83, "stockDocumentNumber": "PSR-CAI-2026-000001" }`. The second unit also
refunds 10.83 (cumulative shares; the last unit takes the rest). The sale then reports `refundedCashAmount` /
`refundedCardAmount`, so a return screen proposes refunds within what each method may still give back.

Shift report while open (X) → `{ "number": "CAI-T1-SH-2026-000001", "status": "Open", "openingFloat": 200,
"cashSalesAmount": 124.06, "cashRefundsAmount": 21.66, "cashInAmount": 0, "cashOutAmount": 0, "expectedCash": 302.4,
"cardSalesAmount": 100, "salesAmount": 224.06, "salesCount": 4, "returnsCount": 2, "openCardIncidents": 0, "cashMovements":
[], "events": [{ "action": "Opened", "amount": 200, "userName": "cashier1" }], "rowVersion": "AAAAAAAAJ1k=" }`. After a
150 cash-out and a close counted 5 short (with a note): `"status": "Closed", "expectedCash": 152.4, "countedCash": 147.4,
"cashDifference": -5`.

## Rules the screens must respect
- Prices, VAT, discounts and totals come from the server (quote, receipt); never compute VAT in the browser except to
  preview a refund before the server confirms it.
- One `Idempotency-Key` per checkout or return; reuse it for every retry of the same request until a definitive answer.
- A shift's `rowVersion` changes with every sale: read the shift just before a cash movement or the close.
- Receipts: print only the checkout / return response (original) or a reprint response (copy). Always print "not a tax
  invoice"; print "COPY" when `isCopy`.
- Times are UTC (`…Z`); show them in `timeZoneId`.

## Manual test steps (after the migrations are applied to the dev database)
1. Sales → Product taxes: classify a product standard-rated, one zero-rated; leave one unclassified.
2. POS → Terminals: create `CAI-T1` in a selling warehouse with stock; a cashier role with `pos.sell`, `pos.read`.
3. Till: pick the terminal, open a shift with 200; scan a barcode, add a search result in another unit; the unclassified
   product can't be added. Quote shows VAT per line.
4. Pay cash with change, print; pay split card + cash (enter a reference), print; enter a full card number → refused.
5. Retry a checkout while the API is stopped mid-request (or reload during Pay) and confirm the same receipt comes back.
6. Try a discount above 10% → refused; a cashier without `pos.discounts.apply` sees no discount field.
7. Return part of a sale; try more than sold; try Damaged → refused; refund by card for a cash sale → refused.
8. Cash out with a reason, X report, close with a difference (note required), Z report.
9. Open a shift as another cashier and leave it; as a supervisor force-close it with a note.
10. Sell more than in stock with a card payment → reversal dialog; resolve the incident as a supervisor.
11. Inventory: the PSL-/PSR- documents link back to the receipt and offer no edit or reverse.

## Known limits (V1)
Online only; standalone card terminals (no gateway); no held carts, promotions, loyalty, header discounts, manual prices
or cash rounding; damaged returns refused; no hardware drawer/printer integration beyond browser printing; the receipt is
not an Egyptian electronic tax receipt (ETA integration is a production prerequisite).
