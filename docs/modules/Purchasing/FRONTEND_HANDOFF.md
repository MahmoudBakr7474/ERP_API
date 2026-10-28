# Purchasing — frontend handoff

Verified 2026-10-29 on `feature/purchasing` against the real API. An end-to-end run used a throwaway LocalDB database
(all seven migrations), the real host, real cookie sessions and anti-forgery, the bootstrap administrator and three
users created through Identity (a buyer with every purchasing right in `MAIN`, a clerk who may only draft orders, an
approver scoped to another warehouse); the database was then dropped. 34 checks passed; the API log had no 500s and no
passwords. The examples below are real responses (ids shortened).

The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md). Business rules: [`V1_DECISIONS.md`](V1_DECISIONS.md) and
[ADR 0016](../../adr/0016-purchasing-module.md).

## Where it goes
- The screens extend the existing ERP frontend (Lovable project `mahmoudtaha311/screenshot-match`, cloned to
  `D:\work\WarehouseERP\warehouse-erp-web`), following its `AGENTS.md` UI conventions.
- Running the backend, the Vite proxy and the cookie/XSRF rules are the same as for
  [Organization](../Organization/FRONTEND_HANDOFF.md).
- OpenAPI tags: `Suppliers`, `PurchasingPolicies`, `PurchaseRequests`, `PurchaseOrders`, `GoodsReceipts`,
  `SupplierInvoices`, `SupplierReturns`.
- The database needs `Migration07_Purchasing` (`dotnet ef database update`, README *Database*).
- The bootstrap administrator holds `"*"`. Give other roles the `purchasing.*` permissions in the role editor; buyers
  also need `catalog.products.read` and `organization.warehouses.read` for the pickers, and `inventory.documents.read`
  to open the stock documents a receipt or return links to.
- **Approval:** anyone with `purchasing.documents.approve` and the warehouse in scope approves, their own requests,
  orders, invoices and returns included (global rule). Never hide Approve from the creator.
- Inventory's screens must label the new stock document types `PurchaseReceipt` / `PurchaseReturn`, hide edit, cancel
  and reverse for them (the API refuses with `Inventory.StockDocument.ManagedByPurchasing`) and link
  `sourceDocumentId` to the receipt or return.

## Endpoints
All under `/api/purchasing`. Updates and actions answer `200 { rowVersion }`; creates `201 { id }`.

| Method | Route | Operation | Body / notes |
|---|---|---|---|
| GET | `/suppliers?page&pageSize&search&isActive&countryCode` | ListSuppliers | |
| GET | `/suppliers/similar?nameAr&nameEn&exceptSupplierId` | FindSimilarSuppliers | list of probable duplicates (warning only) |
| GET | `/suppliers/{id}` | GetSupplier | with contacts, addresses, `rowVersion` |
| POST | `/suppliers` | CreateSupplier | code, names, country, registrations, terms, contacts, addresses |
| PUT | `/suppliers/{id}` | UpdateSupplier | same without code, + `rowVersion`; lists replace the old ones |
| POST | `/suppliers/{id}/activate`, `/deactivate` | Activate/DeactivateSupplier | 204 |
| GET | `/policies` | ListPurchasingPolicies | `PurchaseRequest`, `PurchaseOrder` |
| PUT | `/policies/{documentType}` | UpdatePurchasingPolicy | `{ requiresApproval, rowVersion }`, company-wide only |
| GET | `/requests?page&pageSize&warehouseId&status&orderable&search`, `/requests/{id}` | List/GetPurchaseRequest(s) | |
| POST, PUT | `/requests`, `/requests/{id}` | Create/UpdatePurchaseRequest | `warehouseId` (create only), `neededByDate`, `notes`, `lines[{productId, productUnitId, quantity, notes}]` |
| POST | `/requests/{id}/submit`, `/approve`, `/cancel` | … | `{ rowVersion }` |
| POST | `/requests/{id}/reject`, `/close` | … | `{ reason, rowVersion }` |
| GET | `/orders?page&pageSize&warehouseId&supplierId&status&search`, `/orders/{id}` | List/GetPurchaseOrder(s) | detail has `lines`, `documents`, `events` |
| POST, PUT | `/orders`, `/orders/{id}` | Create/UpdatePurchaseOrder | `supplierId`, `warehouseId` (create only), `expectedDeliveryDate`, `supplierReference`, `paymentTermDays`, `notes`, `lines[{productId, productUnitId, quantity, unitPrice, discountPercent, taxRate, taxTreatment, purchaseRequestLineId}]` |
| POST | `/orders/{id}/submit`, `/approve` | … | `{ rowVersion }` |
| POST | `/orders/{id}/reject`, `/close`, `/cancel` | … | `{ reason, rowVersion }` (reason optional to cancel) |
| POST | `/orders/{id}/receipts` | ReceiveGoods | **Idempotency-Key**; `{ supplierDeliveryNote, notes, lines[{purchaseOrderLineId, quantity, locationId}] }` |
| GET | `/receipts?page&pageSize&warehouseId&supplierId&purchaseOrderId&search`, `/receipts/{id}` | List/GetGoodsReceipt(s) | |
| GET | `/invoices?page&pageSize&supplierId&purchaseOrderId&status&search&dueOnOrBefore`, `/invoices/{id}` | List/GetSupplierInvoice(s) | |
| POST, PUT | `/invoices`, `/invoices/{id}` | Create/UpdateSupplierInvoice | `purchaseOrderId` (create only), `supplierInvoiceNumber`, `invoiceDate`, `dueDate`, `notes`, `lines[{purchaseOrderLineId, quantity, unitPrice, discountPercent, taxRate, taxTreatment}]` |
| POST | `/invoices/{id}/submit`, `/cancel` | … | `{ rowVersion }` |
| POST | `/invoices/{id}/post`, `/approve` | Post/ApproveSupplierInvoice | **Idempotency-Key**; `{ rowVersion }` |
| POST | `/invoices/{id}/reject` | RejectSupplierInvoice | `{ reason, rowVersion }` |
| GET | `/returns?page&pageSize&supplierId&purchaseOrderId&goodsReceiptId&status&withCreditClaim&search`, `/returns/{id}` | List/GetSupplierReturn(s) | |
| POST, PUT | `/returns`, `/returns/{id}` | Create/UpdateSupplierReturn | `goodsReceiptId` (create only), `reason`, `notes`, `lines[{goodsReceiptLineId, quantity, locationId}]` |
| POST | `/returns/{id}/submit`, `/cancel` | … | `{ rowVersion }` |
| POST | `/returns/{id}/approve` | ApproveSupplierReturn | **Idempotency-Key**; `{ rowVersion }` |
| POST | `/returns/{id}/reject` | RejectSupplierReturn | `{ reason, rowVersion }` |

## Request / response examples

Create an order (`POST /api/purchasing/orders` → `201 { "id": "911e…5d06" }`):
```json
{ "supplierId": "0c64…5b60", "warehouseId": "2456…5930",
  "lines": [ { "productId": "8b51…5967", "productUnitId": "faab…5973", "quantity": 10, "unitPrice": 20,
               "discountPercent": 10, "taxRate": 14, "taxTreatment": "Recoverable",
               "purchaseRequestLineId": "201a…5c19" } ] }
```

The order after two receipts, two invoices and a return (`GET /api/purchasing/orders/{id}`, shortened):
```json
{ "draftReference": "DPO-000001", "number": "PO-CAI-2026-000001", "status": "Received",
  "supplierCode": "SUPF32AA867", "supplierName": "شركة المورد", "warehouseCode": "MAIN", "currencyCode": "EGP",
  "requiresApproval": true, "paymentTermDays": 30,
  "grossAmount": 200.0, "discountAmount": 20.0, "netAmount": 180.0, "taxAmount": 25.2, "totalAmount": 205.2,
  "createdBy": "buyer", "submittedBy": "buyer", "approvedBy": "buyer", "rowVersion": "AAAAAAAAJzg=",
  "lines": [ { "lineNumber": 1, "unitCode": "PCS", "quantity": 10.0, "unitPrice": 20.0, "discountPercent": 10.0,
               "taxRate": 14.0, "taxTreatment": "Recoverable", "netUnitPrice": 18.0,
               "receivedQuantity": 10.0, "remainingQuantity": 0, "invoicedQuantity": 8.0, "invoiceableQuantity": 0.0,
               "returnedQuantity": 4.0, "returnedInvoicedQuantity": 2.0 } ],
  "documents": [
    { "documentType": "GoodsReceipt", "reference": "GRN-CAI-2026-000001", "status": "Posted", "amount": null },
    { "documentType": "GoodsReceipt", "reference": "GRN-CAI-2026-000002", "status": "Posted", "amount": null },
    { "documentType": "SupplierInvoice", "reference": "SIN-CAI-2026-000001", "status": "Posted", "amount": 123.12 },
    { "documentType": "SupplierInvoice", "reference": "SIN-CAI-2026-000002", "status": "Posted", "amount": 43.09 },
    { "documentType": "SupplierReturn", "reference": "SRN-CAI-2026-000001", "status": "Posted", "amount": 41.55 } ],
  "events": [ { "action": "Created", "userName": "buyer" }, { "action": "Submitted", "userName": "buyer" },
              { "action": "Approved", "userName": "buyer" }, { "action": "GoodsReceived", "note": "GRN-CAI-2026-000001" } ] }
```

Receive (`POST /api/purchasing/orders/{id}/receipts`, header `Idempotency-Key: 6f1c…`):
```json
{ "supplierDeliveryNote": "DN-77", "lines": [ { "purchaseOrderLineId": "f334…5d13", "quantity": 6, "locationId": null } ] }
```
→ `200 { "id": "8f0b…5d8b", "number": "GRN-CAI-2026-000001", "stockDocumentId": "…", "stockDocumentNumber": "PRC-CAI-2026-000001", "receivedAtUtc": "2026-10-28T21:06:41.865Z" }`.
The same key and body again answer the same receipt; the same key with another body → 409 `Purchasing.Idempotency.KeyReused`.

A posted invoice with a price variance (`GET /api/purchasing/invoices/{id}`, shortened):
```json
{ "draftReference": "DSIN-000002", "number": "SIN-CAI-2026-000002", "status": "Posted",
  "supplierInvoiceNumber": "F-1002", "invoiceDate": "2026-10-28", "dueDate": "2026-11-27", "requiresApproval": true,
  "grossAmount": 42.0, "discountAmount": 4.2, "netAmount": 37.8, "taxAmount": 5.29, "totalAmount": 43.09,
  "priceVarianceAmount": 1.8, "createdBy": "buyer", "submittedBy": "buyer", "approvedBy": "buyer", "postedBy": "buyer" }
```

A posted return line (`GET /api/purchasing/returns/{id}` → `lines[0]`):
```json
{ "quantity": 4.0, "locationCode": "DEFAULT", "receiptUnitCost": 18.0,
  "uninvoicedQuantity": 2.0, "invoicedQuantity": 2.0, "creditClaimNetAmount": 36.45, "creditClaimTaxAmount": 5.1 }
```

Policies (`GET /api/purchasing/policies`):
```json
[ { "documentType": "PurchaseRequest", "requiresApproval": true, "rowVersion": "AAAAAAAAB9Y=" },
  { "documentType": "PurchaseOrder", "requiresApproval": true, "rowVersion": "AAAAAAAAB9c=" } ]
```

## Integration test steps (manual, against the running API)
Set up: company (EGP), branch `CAI`, physical warehouses `MAIN` and `SIDE`, unit `PCS`, product (stock item, untracked,
base `PCS`). Users: a **buyer** scoped to `MAIN` with every `purchasing.*` permission except settings, plus
`inventory.stock.read`, `inventory.documents.read`, `catalog.products.read`; a **clerk** scoped to `MAIN` with
`purchasing.orders.manage`, `purchasing.documents.read`, `purchasing.suppliers.read`; an **outsider** scoped to `SIDE`
with `purchasing.documents.approve`, `purchasing.documents.read`.
1. **Supplier:** create one with tax number `200-300-400`. Another with `200300400` → 409 `TaxNumberTaken`. Typing a
   similar name shows it in the warning list.
2. **Request:** 10 PCS for `MAIN`; submit; the buyer approves it (own document). An order line of 11 from it → 422
   `ExceedsRequest`.
3. **Order:** 10 PCS at 20, 10 % discount, 14 % tax, from the request line: totals 200 / 20 / 180 / 25.20 / 205.20; the
   request becomes Ordered. Submit. The clerk's Approve → 403; the outsider's → 403 `WarehouseDenied`; the buyer
   approves: `PO-CAI-2026-000001`.
4. **Receipts:** receive 6 (retry with the same key shows the same `GRN-…`), then 5 → 422 `OverReceipt`, then 4 → order
   Received. Stock 10; valuation 180 at 18 (tax excluded). The stock document is `PurchaseReceipt` naming the receipt;
   reversing it in Inventory → 422 `ManagedByPurchasing`.
5. **Invoices:** `F-1001`, 6 at 20 −10 % +14 %: posts directly (`SIN-…`). `f 1001` again → 409 `InvoiceNumberTaken`.
   5 more → 422 `InvoiceExceedsReceipts`. `F-1002`, 2 at 21: Post → 422 `ApprovalRequired`; Submit; the buyer approves:
   variance 1.80. Valuation still 180.
6. **Return:** from the first receipt, 4 PCS; submit; the clerk can't approve (403); the buyer approves: `SRN-…` with
   stock `PRT-…`; split 2 not invoiced / 2 invoiced; credit claim 36.45 + 5.10 = 41.55. Stock 6, valuation 108 (at the
   average 18). The order stays Received with nothing left to invoice. The outsider can't open the order (403).
7. **Reconciliation** (administrator): consistent.

## Backend facts
- Receiving, posting an invoice and approving a return each run in one transaction with the stock document or the
  order's quantities; a refusal leaves nothing behind (no receipt, no stock, no number).
- Line amounts are rounded per line (half away from zero) and totals are sums of lines; previews may differ by a cent
  only if they round differently — always show the server's values after saving.
- A request line's `remainingQuantity` and an order line's `remainingQuantity` are 0 once the request or order no longer
  accepts more (closed, cancelled, fully ordered or received).
- Errors are ProblemDetails with `errorCode`; 400 has `errors[]` per field (`lines[0].quantity` …); line errors carry the
  line in `detail`.
