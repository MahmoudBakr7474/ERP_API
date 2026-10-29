# Sales — frontend handoff

Verified 2026-10-29 on `feature/sales` against the real API. An end-to-end run used a throwaway LocalDB database (all
ten migrations), the real host over HTTPS, real cookie sessions and anti-forgery, a bootstrap administrator and three
users created through Identity (a sales rep with every sales right except prices, credit and settings in `MAIN`, a
clerk who may draft orders and returns but not approve, an approver scoped to `SIDE`); the database was then dropped.
49 checks passed; the API log had no 500s and no passwords. The examples below are real responses (ids shortened).

The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md). Business rules: [`V1_DECISIONS.md`](V1_DECISIONS.md) and
[ADR 0018](../../adr/0018-sales-module.md).

## Where it goes
- The screens extend the existing ERP frontend (Lovable project `mahmoudtaha311/screenshot-match`, cloned to
  `D:\work\WarehouseERP\warehouse-erp-web`), following its `AGENTS.md` UI conventions.
- Running the backend, the Vite proxy and the cookie/XSRF rules are the same as for
  [Organization](../Organization/FRONTEND_HANDOFF.md).
- OpenAPI tags: `Customers`, `SalesPrices`, `SalesSettings`, `SalesQuotations`, `SalesOrders`, `SalesDeliveries`,
  `SalesInvoices`, `SalesReturns`; Inventory adds `GET /api/inventory/reservations` (tag `Stock`).
- **`WarehouseERP_Dev` does not have `Migration09`/`Migration10` yet** and the dev API on `https://localhost:49571`
  still runs the `main` build. Applying them (after a copy-only backup, as for Purchasing) and restarting the dev API
  on the `feature/sales` build is the owner's call before the screens are tried there.
- The bootstrap administrator holds `"*"`. Give other roles the `sales.*` permissions in the role editor; sales users
  also need `catalog.products.read` and `organization.warehouses.read` for the pickers, `inventory.stock.read` for the
  availability hints and `inventory.documents.read` to open the stock documents deliveries and returns link to.
- **Approval:** anyone with `sales.documents.approve` and the warehouse in scope approves, their own quotations,
  orders and returns included (global rule). Never hide Approve from the creator.
- Inventory's screens must label the new stock document types `SalesDelivery` / `SalesReturn`, hide edit, cancel and
  reverse for them (the API refuses with `Inventory.StockDocument.ManagedBySales`), link `sourceDocumentId` to the
  delivery or return, and show the reservations list and the reconciliation's `reservationDifferences`.

## Endpoints
All under `/api/sales`. Updates and actions answer `200 { rowVersion }` (conversion answers `201 { id }` of the new
order; posting and approving with a key answer the result shown below); creates `201 { id }`.

| Method | Route | Operation | Body / notes |
|---|---|---|---|
| GET | `/customers?page&pageSize&search&isActive&customerType&isOnCreditHold&countryCode` | ListCustomers | `search` matches code, names, tax number, phone and mobile |
| GET | `/customers/similar?nameAr&nameEn&mobile&exceptCustomerId` | FindSimilarCustomers | probable duplicates (warning only) |
| GET | `/customers/{id}`, `/customers/{id}/exposure` | GetCustomer, GetCustomerExposure | exposure is partial (`isPartial: true`) |
| POST | `/customers` | CreateCustomer | `code`, `customerType` (`Individual`/`Business`), names, `countryCode`, registrations, phone, mobile, email, notes, `contacts[]`, `addresses[{ addressType, line1, line2, cityId, postalCode, isPrimary }]` |
| PUT | `/customers/{id}` | UpdateCustomer | same without code and type, + `rowVersion`; lists replace the old ones |
| PUT | `/customers/{id}/credit` | SetCustomerCredit | `{ paymentTermDays, creditLimit, isOnCreditHold, creditHoldReason, rowVersion }` |
| POST | `/customers/{id}/activate`, `/deactivate` | Activate/DeactivateCustomer | 204 |
| GET | `/prices?page&pageSize&productId&productUnitId` | ListSalesPrices | |
| PUT, DELETE | `/prices/{productUnitId}` | SetSalesPrice, RemoveSalesPrice | `{ unitPrice }` → 204; delete → 204 |
| GET, PUT | `/settings` | Get/UpdateSalesSettings | every field + `rowVersion`; company-wide only |
| GET | `/quotations?page&pageSize&warehouseId&customerId&status&search`, `/quotations/{id}` | List/GetSalesQuotation(s) | |
| POST, PUT | `/quotations`, `/quotations/{id}` | Create/UpdateSalesQuotation | `customerId`, `warehouseId` (create only), `quotationDate`, `validUntil`, `customerReference`, `paymentTermDays`, `notes`, `lines[{ productId, productUnitId, quantity, unitPrice, discountPercent, taxTreatment, taxRate }]` (`unitPrice`, `taxTreatment`, `taxRate` may be `null`: list price, `Standard`, default VAT) |
| POST | `/quotations/{id}/submit`, `/approve`, `/cancel`, `/convert` | … | `{ rowVersion }`; convert → `201 { id }` of the draft order |
| POST | `/quotations/{id}/reject`, `/decline` | … | `{ reason, rowVersion }` |
| GET | `/orders?page&pageSize&warehouseId&customerId&status&search`, `/orders/{id}` | List/GetSalesOrder(s) | detail has `lines`, `documents`, `events` |
| POST, PUT | `/orders`, `/orders/{id}` | Create/UpdateSalesOrder | as quotations, with `orderDate`, `requestedDeliveryDate`, `shippingAddressId` |
| POST | `/orders/{id}/submit`, `/approve` | … | `{ rowVersion }`; confirming reserves every line |
| POST | `/orders/{id}/reject`, `/cancel`, `/close` | … | `{ reason, rowVersion }` (reason optional to cancel) |
| POST | `/orders/{id}/lines/{lineId}/reduce` | ReduceSalesOrderLine | `{ quantity, reason, rowVersion }` |
| GET | `/deliveries?page&pageSize&warehouseId&orderId&customerId&status&search`, `/deliveries/{id}` | List/GetSalesDelivery(s) | |
| POST, PUT | `/deliveries`, `/deliveries/{id}` | Create/UpdateSalesDelivery | `orderId` (create only), `notes`, `lines[{ orderLineId, quantity, locationId }]` |
| POST | `/deliveries/{id}/cancel` | CancelSalesDelivery | `{ rowVersion }` |
| POST | `/deliveries/{id}/post` | PostSalesDelivery | **Idempotency-Key**; `{ rowVersion }` |
| GET | `/invoices?page&pageSize&warehouseId&orderId&customerId&status&search`, `/invoices/{id}` | List/GetSalesInvoice(s) | |
| POST, PUT | `/invoices`, `/invoices/{id}` | Create/UpdateSalesInvoice | `orderId` (create only), `invoiceDate`, `customerReference`, `notes`, `lines[{ orderLineId, quantity }]` |
| POST | `/invoices/{id}/cancel` | CancelSalesInvoice | `{ rowVersion }` |
| POST | `/invoices/{id}/post` | PostSalesInvoice | **Idempotency-Key**; `{ rowVersion }` |
| GET | `/returns?page&pageSize&warehouseId&orderId&deliveryId&customerId&status&search`, `/returns/{id}` | List/GetSalesReturn(s) | |
| POST, PUT | `/returns`, `/returns/{id}` | Create/UpdateSalesReturn | `deliveryId` (create only), `reason`, `notes`, `lines[{ deliveryLineId, quantity, condition, locationId }]` |
| POST | `/returns/{id}/submit`, `/cancel` | … | `{ rowVersion }` |
| POST | `/returns/{id}/approve` | ApproveSalesReturn | **Idempotency-Key**; `{ rowVersion }` |
| POST | `/returns/{id}/reject` | RejectSalesReturn | `{ reason, rowVersion }` |

## Request / response examples

Settings as seeded (`GET /api/sales/settings`):
```json
{ "quotationsRequireApproval": false, "ordersRequireApproval": false, "discountApprovalThreshold": null,
  "manualPriceRequiresApproval": false, "priceBelowListRequiresApproval": false,
  "creditHoldPolicy": "BlockConfirmation", "creditHoldBlocksDeliveries": true, "defaultVatRate": 14,
  "quotationValidityDays": 14, "updatedAtUtc": null, "rowVersion": "AAAAAAAACf0=" }
```

Create a quotation (`POST /api/sales/quotations` → `201 { "id": "…" }`), price from the list, default VAT:
```json
{ "customerId": "402e…e239", "warehouseId": "…", "quotationDate": null, "validUntil": null, "customerReference": "RFQ-1",
  "paymentTermDays": 30, "notes": null,
  "lines": [ { "productId": "10e9…dfcd", "productUnitId": "b79f…dfea", "quantity": 4, "discountPercent": 10 } ] }
```
After Submit (no approval needed) it is issued (`GET /api/sales/quotations/{id}`, shortened):
```json
{ "draftReference": "DQT-000001", "number": "QT-CAI-2026-000001", "status": "Issued", "isExpired": false,
  "quotationDate": "2026-10-29", "validUntil": "2026-11-12", "paymentTermDays": 30,
  "grossAmount": 40, "discountAmount": 4, "netAmount": 36, "taxAmount": 5.04, "totalAmount": 41.04, "approvalReasons": [],
  "lines": [ { "sku": "E2E-TEA", "unitCode": "PCS", "quantity": 4, "unitPrice": 10, "priceSource": "PriceList",
               "listUnitPrice": 10, "discountPercent": 10, "taxTreatment": "Standard", "taxRate": 14,
               "grossAmount": 40, "discountAmount": 4, "netAmount": 36, "taxAmount": 5.04, "totalAmount": 41.04, "netUnitPrice": 9 } ] }
```

The order after a delivery from two locations, an invoice, a return and closing (`GET /api/sales/orders/{id}`, shortened):
```json
{ "draftReference": "DSO-000001", "number": "SO-CAI-2026-000001", "status": "Closed",
  "sourceQuotationNumber": "QT-CAI-2026-000001", "currencyCode": "EGP", "paymentTermDays": 30,
  "totalAmount": 41.04, "invoicedAmount": 20.52, "creditedAmount": 10.26, "closedReason": "العميل لا يريد الباقي",
  "createdBy": "rep", "submittedBy": "rep", "approvedBy": null,
  "lines": [ { "quantity": 4, "deliveredQuantity": 3, "closedQuantity": 1, "remainingQuantity": 0, "invoicedQuantity": 2,
               "invoiceableQuantity": 0, "returnedQuantity": 2, "returnedInvoicedQuantity": 1 } ],
  "documents": [
    { "documentType": "Delivery", "reference": "DN-CAI-2026-000001", "status": "Posted", "amount": 18 },
    { "documentType": "Delivery", "reference": "DDN-000002", "status": "Draft", "amount": 0 },
    { "documentType": "Invoice", "reference": "INV-CAI-2026-000001", "status": "Posted", "amount": 20.52 },
    { "documentType": "Return", "reference": "SR-CAI-2026-000001", "status": "Approved", "amount": 10.26 } ],
  "events": [ { "action": "Created", "userName": "rep" }, { "action": "Confirmed", "userName": "rep" },
              { "action": "Delivered", "note": "DN-CAI-2026-000001" }, { "action": "Invoiced", "note": "INV-CAI-2026-000001" },
              { "action": "Returned", "note": "SR-CAI-2026-000001" }, { "action": "Closed", "note": "العميل لا يريد الباقي" } ] }
```
A delivery's amount is its cost; an invoice's and a return's are customer amounts.

Pick from two locations (`POST /api/sales/deliveries`), then post (header `Idempotency-Key: 6f1c…`, `{ rowVersion }`):
```json
{ "orderId": "…", "notes": null, "lines": [ { "orderLineId": "75e0…e2ea", "quantity": 2, "locationId": "<DEFAULT>" },
                                           { "orderLineId": "75e0…e2ea", "quantity": 1, "locationId": "<SHELF>" } ] }
```
→ `200 { "deliveryId": "1f5c…e365", "number": "DN-CAI-2026-000001", "stockDocumentId": "96b1…e06c",
"stockDocumentNumber": "SDL-CAI-2026-000001", "costAmount": 18, "postedAtUtc": "2026-10-29T03:15:31.29Z" }`. The same key
and body again answer the same delivery; the same key on another delivery → 409 `Sales.Idempotency.KeyReused`. Its lines:
`DEFAULT` 2 PCS cost 12, `SHELF` 1 PCS cost 6.

Post an invoice of 2 (`POST /api/sales/invoices/{id}/post`, Idempotency-Key) →
`200 { "invoiceId": "66ed…e403", "number": "INV-CAI-2026-000001", "totalAmount": 20.52, "dueDate": "2026-11-28", … }`;
the invoice: `invoiceDate` 2026-10-29, `paymentTermDays` 30, 20 / 2 / 18 / 2.52 / 20.52, `postedBy` "rep".

Approve a return of 2 from the `DEFAULT` pick (its creator approves; Idempotency-Key) →
`200 { "number": "SR-CAI-2026-000001", "stockDocumentNumber": "SRT-CAI-2026-000001", "costAmount": 12,
"creditTotalAmount": 10.26, … }`. Its line:
```json
{ "quantity": 2, "condition": "FitForResale", "locationCode": "DEFAULT", "costAmount": 12,
  "uninvoicedQuantity": 1, "invoicedQuantity": 1, "creditNetAmount": 9, "creditTaxAmount": 1.26 }
```

Customer exposure (`GET /api/sales/customers/{id}/exposure`):
```json
{ "currencyCode": "EGP", "isPartial": true, "creditLimit": null, "isOnCreditHold": false, "paymentTermDays": 0,
  "openOrderCount": 0, "uninvoicedOrderAmount": 0, "deliveredNotInvoicedAmount": 0, "invoicedAmount": 20.52,
  "pendingCreditAmount": 10.26 }
```

A customer address as returned (`GET /api/sales/customers/{id}` → `addresses[0]`):
```json
{ "addressType": "Shipping", "line1": "12 شارع النصر", "countryCode": "EG", "countryNameAr": "مصر",
  "governorateNameAr": "أسوان", "cityId": "d093…15d4", "cityNameAr": "أبو سمبل", "postalCode": null, "isPrimary": true }
```

## Integration test steps (manual, against the running API)
Set up: company (EGP), branch `CAI`, selling warehouses `MAIN` (+ location `SHELF`) and `SIDE`, unit `PCS` and `CTN`,
product (stock item, untracked, base `PCS`, `CTN` = 12). Receive 10 PCS into `MAIN` and 4 into `SHELF` at 6. Users: a
**rep** scoped to `MAIN` with `sales.customers.read/manage`, `sales.documents.read`, `sales.quotations.manage`,
`sales.orders.manage`, `sales.deliveries.manage`, `sales.invoices.manage`, `sales.returns.manage`,
`sales.documents.approve`; a **clerk** scoped to `MAIN` with `sales.customers.read`, `sales.documents.read`,
`sales.orders.manage`, `sales.returns.manage`; an **outsider** scoped to `SIDE` with `sales.documents.read`,
`sales.documents.approve`.
1. **Settings and prices:** the seeded settings need no approval. The rep can't change settings or prices (403); the
   administrator sets PCS at 10.
2. **Customer:** tax number `200-300-400`; another with `200300400` → 409 `TaxNumberTaken`; typing a similar name shows
   the first in the warning list.
3. **Quotation:** 4 PCS, 10 % discount: 40 / 4 / 36 / 5.04 / 41.04; Submit → `QT-CAI-2026-000001`; Convert → a draft order.
4. **Order:** Submit → `SO-CAI-2026-000001` Confirmed; reserved 4 of 14. A manual price as the rep → 403
   `ManualPriceNotAllowed`; a CTN line (no price) → 422 `PriceMissing`; an exempt line with 14 % → 422
   `TaxRateNotAllowed`. The outsider can't open it (403 `WarehouseDenied`).
5. **Delivery:** 2 from DEFAULT + 1 from SHELF; post (retry with the same key: same `DN-…`); on hand 11, reserved 1.
   The key on another delivery → 409 `KeyReused`; 2 more → 422 `OverDelivery`. Inventory shows a `SalesDelivery` naming
   the delivery; reversing it → 422 `ManagedBySales`.
6. **Invoice:** 2 → `INV-…` 20.52, due in 30 days; 2 more → 422 `InvoiceExceedsDeliveries`.
7. **Return:** from the DEFAULT pick: Damaged → 422 `ConditionNotSupported`; 3 → 422 `ReturnExceedsDelivery`; 2 fit for
   resale, submit; the clerk can't approve (403), the outsider can't (403 `WarehouseDenied`), the rep approves: `SR-…`,
   cost 12, 1 not invoiced + 1 invoiced, credit 9.00 + 1.26 = 10.26; on hand 13.
8. **Close** the order: reserved 0. Exposure: partial, invoiced 20.52, pending credit 10.26.
9. **Threshold:** the administrator sets a 5 % discount threshold; a 10 % order waits (`DiscountAboveThreshold`); the clerk
   can't approve; the rep approves their own order (reserves 2); cancelling releases it; the threshold is removed.
10. **Credit:** the rep can't change credit (403); the administrator puts the customer on hold; confirming → 422
    `OnCreditHold`; lifting it with a limit of 1, an order of 11.40 confirms (the limit is informational).
11. **Inventory:** one open reservation (that order's, 1 PCS); valuation 13 at 6 = 78; reconciliation consistent; no
    session → 401.

## Backend facts
- Confirming or approving an order, posting a delivery or an invoice and approving a return each run in one
  transaction with the reservation, stock or amounts; a refusal leaves nothing behind (no number, no stock movement).
- Line amounts are rounded per line (half away from zero) and totals are sums of lines; previews may differ by a cent
  only if they round differently — always show the server's values after saving.
- An order line's `remainingQuantity` is what is still reserved for it; `invoiceableQuantity` = delivered − returned
  before invoicing − invoiced.
- Errors are ProblemDetails with `errorCode`; 400 (`Validation.Failed`) has `errors[]` per field (`lines[0].quantity` …);
  line errors carry the line in `detail`.
