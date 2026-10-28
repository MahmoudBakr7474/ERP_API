# Inventory — frontend handoff

Verified 2026-10-28 on `feature/inventory` against the real API. Two end-to-end runs used a throwaway LocalDB database,
the real host, real cookie sessions and anti-forgery, a company-wide approver and a warehouse-scoped storekeeper; the
databases were then dropped. The examples below are real responses (ids shortened); every call in the test steps was
checked with its expected status and error code. The API log had no 500s and no passwords.

The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md). Business rules and their reasons are in
[ADR 0015](../../adr/0015-inventory-core.md) and [`V1_DECISIONS.md`](V1_DECISIONS.md).

## Where it goes
- The screens extend the existing ERP frontend (Lovable project `mahmoudtaha311/screenshot-match`, cloned to
  `D:\ERP System\warehouse-erp-web`), following its `AGENTS.md` UI conventions (modal forms, SweetAlert confirmations,
  `ServerTable`, `RowActions`, `useLatestRecord`, one collapsible sidebar group).
- Running the backend, the Vite proxy and the cookie/XSRF rules are the same as in
  [Organization's handoff](../Organization/FRONTEND_HANDOFF.md).
- OpenAPI tags: `StockDocuments`, `Stock`, `Valuation`, `DocumentPolicies`, `StockTransfers`, `StockCounts`,
  `Reconciliation`.
- The database needs `Migration04_Inventory`, `Migration05_InventoryTransfersCounts` and
  `Migration06_InventorySnapshotIsolation` (`dotnet ef database update`, README *Database*). `WarehouseERP_Dev` has
  all three.
- The bootstrap administrator holds `"*"`. Give other roles the `inventory.*` permissions in the role editor. Anyone with
  `inventory.documents.approve` and the warehouse in scope approves, their own documents, transfers and counts included.
- Transfers need a warehouse of type **Transit** in the source branch (Organization → Warehouses → New, type
  Transit). With exactly one active transit warehouse in the branch, transfers use it without asking.

## Endpoints
| Method | Path | Permission (+ scope) | Success | Errors |
|---|---|---|---|---|
| GET | `/api/inventory/documents?page&pageSize&warehouseId&documentType&status&search` | `documents.read` | 200 paged | 400, 403 |
| GET | `/api/inventory/documents/{id}` | `documents.read` + warehouse | 200 `StockDocumentDetailsResponse` | 403, 404 |
| POST | `/api/inventory/documents` | `<type>.manage` + warehouse | 201 `{id}` | 400, 403, 422 |
| PUT | `/api/inventory/documents/{id}` | `<type>.manage` + warehouse | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/documents/{id}/submit` · `/cancel` | `<type>.manage` + warehouse | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/documents/{id}/post` (Idempotency-Key) | `<type>.manage` + warehouse | 200 `PostedDocumentResponse` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/documents/{id}/approve` (Idempotency-Key) | `documents.approve` + warehouse | 200 `PostedDocumentResponse` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/documents/{id}/reject` | `documents.approve` + warehouse | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/documents/{id}/reverse` | `<type>.manage` + warehouse | 201 `{id}` of the reversal | 400, 403, 404, 409 AlreadyReversed, 422 |
| GET | `/api/inventory/balances?page&pageSize&warehouseId&locationId&productId&includeZero` | `stock.read` | 200 paged | 400, 403 |
| GET | `/api/inventory/availability?page&pageSize&warehouseId&productId&includeZero` | `stock.read` | 200 paged | 400, 403 |
| GET | `/api/inventory/movements?productId&warehouseId&locationId&fromUtc&toUtc&page&pageSize` | `stock.read` | 200 paged | 400, 403 |
| GET | `/api/inventory/over-reservations?page&pageSize&warehouseId&productId` | `stock.read` | 200 paged | 400, 403 |
| GET | `/api/inventory/valuation?page&pageSize&productId&includeZero` | `valuation.read` | 200 paged | 400 |
| GET | `/api/inventory/valuation/{productId}/entries?page&pageSize` | `valuation.read` | 200 paged | 400 |
| GET | `/api/inventory/document-policies` | `documents.read` | 200 `DocumentPolicyResponse[]` (5 rows) | |
| PUT | `/api/inventory/document-policies/{documentType}` | `settings.manage` + company-wide | 200 `{rowVersion}` | 400, 403, 404, 409 |
| GET | `/api/inventory/transfers?page&pageSize&warehouseId&status&search` | `documents.read` | 200 paged | 400, 403 |
| GET | `/api/inventory/transfers/destinations` | `transfers.manage` | 200 `TransferDestinationResponse[]` | 403 |
| GET | `/api/inventory/transfers/{id}` | `documents.read` + source or destination | 200 `StockTransferDetailsResponse` | 403, 404 |
| POST | `/api/inventory/transfers` | `transfers.manage` + source | 201 `{id}` | 400, 403, 422 |
| PUT | `/api/inventory/transfers/{id}` | `transfers.manage` + source | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/transfers/{id}/submit` · `/cancel` | `transfers.manage` + source | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/transfers/{id}/dispatch` (Idempotency-Key) | `transfers.manage` + source | 200 `StockTransferPostedResponse` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/transfers/{id}/approve` (Idempotency-Key) | `documents.approve` + source | 200 `StockTransferPostedResponse` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/transfers/{id}/reject` | `documents.approve` + source | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/transfers/{id}/receive` (Idempotency-Key) | `transfers.receive` + destination | 200 `StockTransferPostedResponse` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/transfers/{id}/close` | `transfers.manage` + source or destination | 201 `{id}` of the loss adjustment | 400, 403, 404, 409, 422 |
| GET | `/api/inventory/counts?page&pageSize&warehouseId&status` | `documents.read` | 200 paged | 400, 403 |
| GET | `/api/inventory/counts/{id}` | `documents.read` + warehouse | 200 `StockCountDetailsResponse` | 403, 404 |
| POST | `/api/inventory/counts` | `counts.manage` + warehouse | 201 `{id}` | 400, 403, 422 |
| POST | `/api/inventory/counts/{id}/start` · `/submit` · `/cancel` | `counts.manage` + warehouse | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| PUT | `/api/inventory/counts/{id}/lines` | `counts.manage` + warehouse | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/counts/{id}/approve` (Idempotency-Key) | `documents.approve` + warehouse | 200 `StockCountApprovalResponse` | 400, 403, 404, 409, 422 |
| POST | `/api/inventory/counts/{id}/reject` | `documents.approve` + warehouse | 200 `{rowVersion}` | 400, 403, 404, 409, 422 |
| GET | `/api/inventory/reconciliation` | `reconciliation.read` + company-wide | 200 `ReconciliationReport` | 403 |

All permissions are `inventory.*`. "+ warehouse" means the warehouse must be in the user's scope: otherwise 403
(`…AccessDenied`), also for an id that doesn't exist; only company-wide users get 404.

## Request / response examples
```http
GET /api/inventory/document-policies
→ 200 [ { "documentType": "OpeningBalance", "requiresApproval": true,  "updatedAtUtc": null, "rowVersion": "AAAAAAAAB9E=" },
        { "documentType": "Receipt",        "requiresApproval": false, … },
        { "documentType": "Issue",          "requiresApproval": false, … },
        { "documentType": "Adjustment",     "requiresApproval": true,  … },
        { "documentType": "TransferDispatch", "requiresApproval": false, "updatedAtUtc": null, "rowVersion": "AAAAAAAAB9U=" } ]

POST /api/inventory/transfers
{ "sourceWarehouseId": "…MAIN…", "destinationWarehouseId": "…STORE…", "transitWarehouseId": null,
  "externalReference": "WAYBILL-7", "notes": null,
  "lines": [ { "productId": "…", "productUnitId": "…CTN (12 PCS)…", "quantity": 2 } ] }
→ 201 { "id": "…" }

GET /api/inventory/transfers/destinations
→ 200 [ { "warehouseId": "…", "code": "MAIN", "nameAr": "MAIN", "nameEn": null, "branchId": "…", "branchCode": "CAI" },
        { "warehouseId": "…", "code": "STORE", … } ]          (every active physical warehouse, whatever the scope)

POST /api/inventory/transfers/{id}/dispatch   Idempotency-Key: 3f6c…   { "rowVersion": "AAAAAAAAJyM=" }
→ 200 { "transferId": "…", "documentId": "…", "number": "TRF-CAI-2026-000001", "status": "InTransit",
        "postedAtUtc": "2026-10-28T13:27:50.8161311Z", "overReservations": [] }
   (same key + same body again → the same 200, nothing posted twice; same key + other body → 409 Inventory.Idempotency.KeyReused)
   (a scoped user without transfers.manage → 403 Common.Forbidden)

GET /api/inventory/transfers/{id}
→ 200 { "id": "…", "reference": "TR-000001", "number": "TRF-CAI-2026-000001", "status": "InTransit",
        "sourceWarehouseId": "…", "sourceWarehouseCode": "MAIN", "destinationWarehouseId": "…", "destinationWarehouseCode": "STORE",
        "transitWarehouseId": "…", "transitWarehouseCode": "TRANSIT", "transitLocationId": "…", "transitLocationCode": "TR-000001",
        "requiresApproval": false, "externalReference": "WAYBILL-7", "notes": null,
        "createdAtUtc": "2026-10-28T13:27:50.522", "createdByUserId": "…", "createdBy": "Administrator",
        "dispatchedAtUtc": "2026-10-28T13:27:50.785", "dispatchedBy": "Administrator", "approvedBy": null, "completedAtUtc": null,
        "outstandingQuantity": 24.0, "rowVersion": "AAAAAAAAJyc=",
        "lines": [ { "id": "…", "lineNumber": 1, "productId": "…", "sku": "E2E-001", "productNameAr": "عصير", "productUnitId": "…",
                     "unitCode": "CTN", "conversionFactor": 12.0, "quantity": 2.0, "baseQuantity": 24.0, "baseUnitCode": "PCS",
                     "sourceLocationId": "…", "sourceLocationCode": "DEFAULT",
                     "receivedQuantity": 0.0, "lostQuantity": 0.0, "outstandingQuantity": 24.0 } ],
        "documents": [ { "id": "…", "documentType": "TransferDispatch", "status": "Posted", "draftReference": "DR-000002",
                         "number": "TRF-CAI-2026-000001", "createdAtUtc": "…", "postedAtUtc": "…" } ],
        "events": [ { "action": "Created", "userId": "…", "userName": "Administrator", "atUtc": "…", "note": null },
                    { "action": "Dispatched", "userId": "…", "userName": "Administrator", "atUtc": "…", "note": "TRF-CAI-2026-000001" } ] }

POST /api/inventory/transfers/{id}/receive   Idempotency-Key: 9a1d…   (by the destination's storekeeper)
{ "lines": [ { "lineId": "…", "quantity": 10, "locationId": null } ], "rowVersion": "AAAAAAAAJyc=" }
→ 200 { "transferId": "…", "documentId": "…", "number": "TRR-CAI-2026-000001", "status": "PartiallyReceived", … }

POST /api/inventory/transfers/{id}/close   { "reason": "Loss", "notes": "كرتونة مفقودة", "rowVersion": "…" }
→ 201 { "id": "…loss adjustment…" }          transfer → "Closing"; receive → 422 Inventory.StockTransfer.NotInTransit
POST /api/inventory/documents/{loss}/approve  (its creator or the approver) → 200 { "number": "ADJ-CAI-2026-000001", … }
GET /api/inventory/transfers/{id}
→ "status": "Closed", "lines": [ { …, "receivedQuantity": 10.0, "lostQuantity": 14.0, "outstandingQuantity": 0.0 } ],
  "documents": [ TransferDispatch TRF-…, TransferReceipt TRR-…, Adjustment ADJ-… ],
  "events": [ Created, Dispatched, Received ("TRR-…"), ClosingRequested ("كرتونة مفقودة"), Closed ("ADJ-…") ]

GET /api/inventory/documents/{dispatchDocumentId}
→ 200 { …, "documentType": "TransferDispatch", "transferId": "…", "countId": null,
        "lines": [ { …, "direction": "Move", "fromLocationId": "…", "fromLocationCode": "DEFAULT",
                     "locationId": "…", "locationCode": "TR-000001" } ] }

POST /api/inventory/counts   { "warehouseId": "…MAIN…", "locationIds": [], "notes": "جرد سنوي" }   → 201 { "id": "…" }
POST /api/inventory/counts/{id}/start   { "rowVersion": "…" }   → 200 { "rowVersion": "AAAAAAAAJzI=" }
GET /api/inventory/counts/{id}
→ 200 { "id": "…", "reference": "CT-000001", "status": "Counting", "warehouseId": "…", "warehouseCode": "MAIN",
        "isWholeWarehouse": true, "locations": [], "notes": "جرد سنوي", "createdAtUtc": "…", "createdByUserId": "…",
        "createdBy": "Administrator", "startedAtUtc": "…", "startedBy": "Administrator", "submittedAtUtc": null,
        "approvedBy": null, "completedAtUtc": null, "adjustmentDocumentId": null, "adjustmentNumber": null,
        "rowVersion": "AAAAAAAAJzI=",
        "lines": [ { "id": "…", "productId": "…", "sku": "E2E-001", "productNameAr": "عصير", "baseUnitCode": "PCS",
                     "locationId": "…", "locationCode": "DEFAULT", "expectedQuantity": 24.0, "countedQuantity": null,
                     "variance": 0, "unitCost": null } ],
        "events": [ Created, Started ] }
POST /api/inventory/documents/{an issue in MAIN}/post
→ 422 { "status": 422, "errorCode": "Inventory.StockCount.ScopeFrozen",
        "detail": "Count CT-000001 has frozen this stock; nothing posts there until it is completed or cancelled.", … }
PUT /api/inventory/counts/{id}/lines
{ "lines": [ { "productId": "…", "locationId": "…", "countedQuantity": 22, "unitCost": null } ], "rowVersion": "AAAAAAAAJzI=" }
→ 200 { "rowVersion": "…" }
POST /api/inventory/counts/{id}/submit → 200
POST /api/inventory/counts/{id}/approve   Idempotency-Key: …   (its creator or the approver)
→ 200 { "countId": "…", "adjustmentDocumentId": "…", "adjustmentNumber": "CNT-CAI-2026-000001",
        "completedAtUtc": "2026-10-28T13:27:52.4621571Z", "overReservations": [] }
GET /api/inventory/counts?pageSize=5
→ 200 { "items": [ { "id": "…", "reference": "CT-000001", "status": "Completed", "warehouseId": "…", "warehouseCode": "MAIN",
                     "isWholeWarehouse": true, "lineCount": 1, "countedLineCount": 1, "differenceCount": 1, … } ],
        "page": 1, "pageSize": 5, "totalCount": 1, "totalPages": 1 }

GET /api/inventory/reconciliation   (company-wide approver)
→ 200 { "generatedAtUtc": "2026-10-28T13:27:52.8167535Z", "isConsistent": true,
        "availabilityDifferences": [], "ledgerDifferences": [], "valuationDifferences": [], "transferDifferences": [],
        "stockPositions": [ { "productId": "…", "sku": "E2E-001", "productNameAr": "عصير",
                              "physicalQuantity": 26.0, "inTransitQuantity": 5.0, "valuedQuantity": 31.0, "valuedValue": 310.0 } ],
        "openTransfers": [ { "transferId": "…", "number": "TRF-CAI-2026-000002", "status": "InTransit",
                             "sourceWarehouseCode": "MAIN", "destinationWarehouseCode": "STORE",
                             "dispatchedAtUtc": "…", "daysInTransit": 0, "outstandingQuantity": 5.0 } ] }
GET /api/inventory/reconciliation   (warehouse-scoped user) → 403 Inventory.Reconciliation.CompanyWideAccessRequired
```

## Integration test steps (manual, against the running API)
Set up first: company, a branch `CAI`, warehouses `MAIN` and `STORE` (physical) and `TRANSIT` (transit), units `PCS`
and `CTN`, product `E2E-001` (base `PCS`, `CTN` = 12 PCS). Users: the administrator, an approver (company-wide,
`documents.read`, `documents.approve`, `stock.read`, `valuation.read`, `reconciliation.read`), and a storekeeper scoped
to `STORE` (`stock.read`, `documents.read`, `transfers.receive`, `reconciliation.read`).
1. **Approval policies:** five rows; `TransferDispatch` doesn't need approval. Only a company-wide user can switch one.
2. **Receipt:** 48 PCS at 10 into `MAIN`, posted directly. Valuation 48 / 480. A retried post (same key) shows the same
   number.
3. **Transfer:** 2 CTN `MAIN` → `STORE`.
   - The transit warehouse fills in by itself. The storekeeper can't dispatch.
   - Dispatch: `TRF-CAI-2026-000001`. Balances: `MAIN` 24, `TRANSIT` 24 (location `TR-000001`), `STORE` 0. Valuation
     still 48 / 480.
   - The storekeeper sees the transfer and receives 10: `TRR-CAI-2026-000001`, status partly received. `TRANSIT` 14,
     `STORE` 10.
   - Reconciliation (approver): consistent; physical 34 + in transit 14 = valued 48. The storekeeper gets 403.
4. **Loss:** close the transfer with reason Loss. It is "Closing" and refuses receipts. The administrator who closed it
   approves the loss adjustment (the approver could too). The transfer is closed with 14 lost; `TRANSIT` 0; valuation 34 / 340.
5. **Count:** a whole-warehouse count of `MAIN`.
   - Start: 24 expected. An issue from `MAIN` → "frozen by count CT-000001".
   - Count 22, submit, approve as its creator (the approver could too): `CNT-CAI-2026-000001`.
   - The issue posts now. `MAIN` 21, valuation 31 / 310.
6. **In transit at the end:** dispatch 5 PCS to `STORE` and leave it. Reconciliation: consistent, physical 26 + in
   transit 5 = valued 31, one open transfer with 5 outstanding. The storekeeper's transfer list shows both transfers.
7. **Freeze and transfers:** while a count of `STORE` runs, receiving into it is refused (ScopeFrozen); after
   cancelling the count it works.
8. **Permissions and scope:** a storekeeper scoped to `MAIN` with `transfers.manage` lists destinations outside their
   scope and can send there; a user of another warehouse gets 403 on these transfers and counts.

## Backend facts
- Dispatch, receipt and count approval each run in one transaction; a refused dispatch leaves nothing behind (no
  document, no transit location).
- The stock card shows a transfer twice: out of the source location, into the transit location (and later out of
  transit, into the destination). Filter by warehouse to see one side.
- A transit location is named after its transfer (`TR-000001`) and is deactivated once the transfer is received or
  closed.
- Errors are ProblemDetails with `errorCode`; 400 also has `errors[]` per field (`lines[0].quantity` …).
