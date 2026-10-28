# Lovable prompt — WarehouseERP Inventory screens

Copy everything below the line into Lovable, in the same project as the Organization, Identity and Catalog screens. It
is built from the backend's real OpenAPI contract (`docs/api/openapi-v1.json`) and an end-to-end run against the real
API; don't invent endpoints, fields or rules. The examples and the manual test steps are in
[`FRONTEND_HANDOFF.md`](FRONTEND_HANDOFF.md).

---

Add the **Inventory** module to the existing WarehouseERP app. It covers:
- stock documents: opening stock, goods receipt, goods issue, adjustments and reversals, with drafts and approval;
- transfers between warehouses through a transit warehouse: dispatch, partial receipt, closing what never arrived;
- stock counts that freeze their stock while people count, and post the differences on approval;
- stock balances, availability, the stock card, over-reservation alerts;
- the company-wide valuation (moving average) and its history;
- the reconciliation report;
- the approval policy per document type.

## Build on what exists (don't rebuild it)
Follow `AGENTS.md`, and above all its **UI conventions**:
- Every create and edit form opens in `FormDialog` (`src/shared/ui/FormDialog.tsx`). There are **no `/new` pages** and no
  inline edit forms. Detail pages are read-only (`Panel` + `DetailGrid` + `ReadOnlyValue`); their action buttons open
  modals or confirmations.
- An edit dialog loads its record with `refetchOnMountOrArgChange: true` and fills its form from `useLatestRecord(q)`
  (`src/shared/forms/useLatestRecord.ts`), so the form and its `rowVersion` always come from the same fetch.
- Every action that changes state asks first with SweetAlert2 through `useConfirm()` / `useStatusConfirm()` from
  `src/shared/ui/confirm.ts`; their `run`/`onConfirm` performs the call and toasts the result. Never open one from
  inside an open `FormDialog`. Actions that need input (reject reason, receipt quantities, close reason, counted
  quantities) use a `FormDialog` instead.
- Lists use `ServerTable`; other tables sit in `<Panel bodyClassName="p-0">`. Row actions are icon buttons from
  `src/shared/ui/RowActions.tsx` in an end-aligned last column.
- The primary "New …" button goes in `PageHeader` actions; toolbar filters are compact with `Field labelHidden`.
- Full width pages, logical classes only (`ms`/`me`/`ps`/`pe`/`start`/`end`).
- RTK Query endpoints are injected into `src/shared/api/baseApi.ts` with new tag types `StockDocument`, `StockTransfer`,
  `StockCount`, `Stock`, `Valuation`, `DocumentPolicy`, `Reconciliation`. Any posting (post, approve, dispatch, receive,
  count approval, loss approval) invalidates `StockDocument`, `StockTransfer`, `StockCount`, `Stock`, `Valuation` and
  `Reconciliation`.
- Permission names go in `P`, checked with `can()`; company-wide-only screens also use `canCompanyWide()`
  (`src/shared/session/permissions.ts`). Every text goes in `translations.ts`; errors under `err.<errorCode>`.
- Code in `src/features/inventory/`, pages under `src/routes/_authenticated/inventory/`.
- Sidebar: one collapsible **Inventory** group in `navGroups` (`src/shared/shell/AppShell.tsx`), after Catalog, with
  these sub-items (each shown only with its permission):
  Stock documents (`inventory.documents.read`), Transfers (`inventory.documents.read`), Stock counts
  (`inventory.documents.read`), Stock (`inventory.stock.read`), Stock card (`inventory.stock.read`), Valuation
  (`inventory.valuation.read`), Reconciliation (`inventory.reconciliation.read` + company-wide), Approval policies
  (`inventory.documents.read`).
- In the Identity role editor, add the permission labels from the table at the end.

## Permissions and warehouse scope
`GET /api/identity/me` already gives `permissions`, `isCompanyWide` and `warehouseIds`. A scoped user sees and changes
only documents, transfers and counts of their warehouses; the server answers 403 otherwise, so hide what can't be done.

| Action | Needs |
|---|---|
| See documents, transfers, counts and their history | `inventory.documents.read` + the warehouse in scope (a transfer: its source or destination) |
| Draft, edit, submit, post, cancel, reverse a document | `inventory.<type>.manage` for its type: `opening` (OpeningBalance), `receipts` (Receipt), `issues` (Issue), `adjustments` (Adjustment) |
| Approve or reject a document, a transfer, a count | `inventory.documents.approve`; **never the user who created it** |
| Draft, edit, submit, cancel, dispatch a transfer | `inventory.transfers.manage` + the source warehouse in scope |
| Close a transfer (write off what never arrived) | `inventory.transfers.manage` + source or destination in scope |
| Receive a transfer | `inventory.transfers.receive` + the destination warehouse in scope |
| Create, start, enter, submit, cancel a count | `inventory.counts.manage` + the warehouse in scope |
| Balances, availability, stock card, over-reservations | `inventory.stock.read` |
| Valuation and its entries | `inventory.valuation.read` |
| Reconciliation report | `inventory.reconciliation.read` + **company-wide** |
| Change an approval policy | `inventory.settings.manage` + **company-wide** |

Compare `createdByUserId` with `me.userId` to hide Approve on the user's own document, transfer or count (the server
answers 422 `…SelfApproval`).

The pickers read other modules: warehouses and locations need `organization.warehouses.read`, products and their
units `catalog.products.read`. Inventory roles that create documents, transfers or counts need those too; without
them show the picker disabled with a hint instead of failing.

## Shared rules
- **Row versions.** Every detail response has `rowVersion`. Send the one the screen last read with every change or
  action. 409 `Common.ConcurrencyConflict` → reload the record and keep what the user typed. Updates and simple actions
  answer `200 { rowVersion }`; creates answer `201 { id }`.
- **Idempotency-Key header** on: document `post` and `approve`; transfer `dispatch`, `approve` and `receive`; count
  `approve`. Generate `crypto.randomUUID()` when the user confirms. If the request fails without an answer (network,
  timeout, 5xx), retry with the **same key and the same body** (same `rowVersion`): the server answers with the first
  result and posts nothing twice. A new user action gets a new key. 409 `Inventory.Idempotency.KeyReused` = the key was
  used for a different request.
- 409 `Inventory.Stock.Busy`: another operation is changing this stock; offer "Try again".
- **Quantities** are decimals up to 4 places, whole for a unit that can't be split; base quantities are in the product's
  base unit (`baseUnitCode`). The server refuses a quantity that doesn't convert exactly; show its message on the line.
  Unit costs up to 8 decimals; values 4 decimals, in the company currency.
- Codes, SKUs and numbers are ASCII: show them `dir="ltr"`. Show `productNameAr` (the API has no English product name
  here). Times are UTC: show them in local time.
- Warehouses for pickers: `GET /api/organization/warehouses?isActive=true&pageSize=100` (only the user's scope),
  `type` `Physical` or `Transit` (filter on the client). Locations:
  `GET /api/organization/warehouses/{warehouseId}/locations?isActive=true&pageSize=100`; the `DEFAULT` location is used
  when a line names none, so let "Default" send `null`. Products: `GET /api/catalog/products?search=&type=StockItem&isActive=true`
  (a scanned barcode in `search` finds its product); its units from `GET /api/catalog/products/{id}` (`units`, active
  ones, label `unitCode` and "= {conversionFactor} {base unit}").
- Status badges: `Draft` grey, `Submitted` amber, `Posted`/`Received`/`Completed` green, `Cancelled` muted,
  `InTransit`/`PartiallyReceived`/`Counting` blue, `Closing` amber, `Closed` slate.
- Every list has paging (`page`, `pageSize` ≤ 100) and returns `{ items, page, pageSize, totalCount, totalPages }`.

## 1. Stock documents — `/inventory/documents`
- List: `GET /api/inventory/documents?page&pageSize&warehouseId&documentType&status&search` →
  `{ id, draftReference, number, documentType, status, warehouseId, warehouseCode, requiresApproval,
  reversalOfDocumentId, externalReference, lineCount, createdAtUtc, createdByUserId, createdBy, postedAtUtc }`.
  - Columns: reference (`number ?? draftReference`), type, warehouse, status, external reference, lines, created by/at,
    posted at. `search` matches the number, the draft reference (`DR-000123`) or the external reference.
  - Filters: warehouse, type (all eight types, see translations), status.
- **New document** (`FormDialog`, wide): `POST /api/inventory/documents` with
  `{ documentType, warehouseId, adjustmentReason, externalReference, notes, lines: [{ productId, productUnitId,
  quantity, locationId, unitCost, direction }] }` → `201 { id }`, then open its page.
  - `documentType`: `OpeningBalance`, `Receipt`, `Issue` or `Adjustment`, each offered only with its manage permission.
    The other types are created by the system.
  - `warehouseId`: an active **physical** warehouse in scope (transit warehouses take stock only through transfers).
  - `adjustmentReason` only for `Adjustment`: `Damage`, `Loss`, `Expiry`, `Found`, `Correction`, `Other`; otherwise
    `null`.
  - `externalReference` ≤100, `notes` ≤500, 1–500 lines.
  - Line: product, unit (default the base unit), `quantity` > 0, location (default `null`), `unitCost` (cost of one
    entered unit: **required** for `OpeningBalance` and `Receipt`, optional for an `Adjustment` increase, **never** for
    an issue or a decrease), `direction` only for `Adjustment` (`In` = increase, `Out` = decrease; `null` otherwise).
- **Document page** `/inventory/documents/$id`: `GET /api/inventory/documents/{id}` →
  `{ id, draftReference, number, documentType, status, warehouseId, warehouseCode, branchId, requiresApproval,
  reversalOfDocumentId, reversedByDocumentId, transferId, countId, adjustmentReason, externalReference, notes,
  createdAtUtc, createdByUserId, createdBy, postedAtUtc, postedBy, approvedBy, rowVersion,
  lines: [{ id, lineNumber, productId, sku, productNameAr, productUnitId, unitId, unitCode, conversionFactor, quantity,
  baseQuantity, baseUnitCode, direction, unitCost, locationId, locationCode, fromLocationId, fromLocationCode }],
  events: [{ action, userId, userName, atUtc, note }] }`.
  - Header, lines table, and the history as a timeline (`note` holds a rejection reason or a number).
  - A `Move` line (transfer documents) goes from `fromLocationCode` to `locationCode`.
  - Links: `reversalOfDocumentId` ("Reverses …"), `reversedByDocumentId` ("Reversed by …"), `transferId` (to the
    transfer), `countId` (to the count).
  - Actions by status (and permission):
    - `Draft`: **Edit** (dialog, `PUT /api/inventory/documents/{id}` with `{ adjustmentReason, externalReference, notes,
      lines, rowVersion }`; not for a `Reversal` or a document with `transferId`/`countId`), **Post** when
      `requiresApproval` is false (`POST …/post`, Idempotency-Key, `{ rowVersion }`), **Submit** when it is true
      (`POST …/submit`), **Cancel** (`POST …/cancel`).
    - `Submitted`: **Approve** (`POST …/approve`, Idempotency-Key; it posts), **Reject** (dialog: reason ≤500, `POST
      …/reject` with `{ reason, rowVersion }`, back to draft), **Cancel**.
    - `Posted`: **Reverse** (confirm, optional notes ≤500: `POST …/reverse` with `{ notes }` → `201 { id }` of a new
      `Reversal` already submitted; open it). Not offered for a `Reversal`, for a document already reversed
      (`reversedByDocumentId`), for `TransferDispatch`, `TransferReceipt`, `CountAdjustment`, or a document with
      `transferId`.
  - Post and approve answer `{ id, number, postedAtUtc, overReservations: [...] }`. When `overReservations` is not
    empty (an authorized loss left a warehouse with less sellable stock than reserved), show a warning listing them.
  - A posting into stock frozen by a count answers 422 `Inventory.StockCount.ScopeFrozen`; its message names the count.

## 2. Transfers — `/inventory/transfers`
A transfer moves stock from a physical warehouse to another (any branch) through a transit warehouse. Dispatch takes it
out of the source into a location opened for this transfer in the transit warehouse; receipts move it on to the
destination, in parts or at once; what never arrives is written off by an approved loss adjustment. The company's
quantity and value don't change while stock is in transit.
- List: `GET /api/inventory/transfers?page&pageSize&warehouseId&status&search` →
  `{ id, reference, number, status, sourceWarehouseId, sourceWarehouseCode, destinationWarehouseId,
  destinationWarehouseCode, transitWarehouseId, transitWarehouseCode, requiresApproval, lineCount, outstandingQuantity,
  createdAtUtc, createdByUserId, createdBy, dispatchedAtUtc, completedAtUtc }`.
  - Columns: reference (`number ?? reference`), from → to, status, in transit (`outstandingQuantity`, base units),
    created, dispatched, completed. `warehouseId` matches the source or the destination. Status filter (eight
    statuses). A scoped user sees transfers leaving or reaching their warehouses: show "Incoming" and "Outgoing" quick
    filters by comparing with `me.warehouseIds`.
- **New transfer** (`FormDialog`, wide): `POST /api/inventory/transfers` with
  `{ sourceWarehouseId, destinationWarehouseId, transitWarehouseId, externalReference, notes,
  lines: [{ productId, productUnitId, quantity, sourceLocationId }] }` → `201 { id }`, then open it.
  - Source: an active physical warehouse in scope.
  - Destination: from `GET /api/inventory/transfers/destinations` → `[{ warehouseId, code, nameAr, nameEn, branchId,
    branchCode }]`: every active physical warehouse, **also outside the user's scope** (label `branchCode / code — name`).
    Not the source.
  - Transit warehouse: optional. Leave it empty to use the source branch's only active transit warehouse; if the server
    answers 422 `Inventory.StockTransfer.TransitWarehouseRequired`, show a select of transit warehouses.
  - Lines (1–500): product, unit (any active unit), quantity, source location (default `null` = DEFAULT).
- **Transfer page** `/inventory/transfers/$id`: `GET /api/inventory/transfers/{id}` →
  `{ id, reference, number, status, sourceWarehouseId, sourceWarehouseCode, destinationWarehouseId,
  destinationWarehouseCode, transitWarehouseId, transitWarehouseCode, transitLocationId, transitLocationCode,
  requiresApproval, externalReference, notes, createdAtUtc, createdByUserId, createdBy, dispatchedAtUtc, dispatchedBy,
  approvedBy, completedAtUtc, outstandingQuantity, rowVersion,
  lines: [{ id, lineNumber, productId, sku, productNameAr, productUnitId, unitCode, conversionFactor, quantity,
  baseQuantity, baseUnitCode, sourceLocationId, sourceLocationCode, receivedQuantity, lostQuantity,
  outstandingQuantity }],
  documents: [{ id, documentType, status, draftReference, number, createdAtUtc, postedAtUtc }],
  events: [{ action, userId, userName, atUtc, note }] }`.
  - A progress header (Draft → Dispatched → Received/Closed), the lines with sent / received / lost / still in transit
    (base units), the documents it produced (links to the document page), the history.
  - Actions by status:
    - `Draft` (source side): **Edit** (dialog: `PUT /api/inventory/transfers/{id}` with `{ transitWarehouseId,
      externalReference, notes, lines, rowVersion }`; source and destination are fixed), **Dispatch** when
      `requiresApproval` is false (`POST …/dispatch`, Idempotency-Key, `{ rowVersion }`), **Submit** when it is true
      (`POST …/submit`), **Cancel** (`POST …/cancel`).
    - `Submitted`: **Approve** (`POST …/approve`, Idempotency-Key; it dispatches; not the creator), **Reject** (dialog:
      reason, `POST …/reject` with `{ reason, rowVersion }`), **Cancel** (source side).
    - `InTransit` / `PartiallyReceived`: **Receive** (destination side, dialog below) and **Close** (dialog below).
    - `Closing`: show "Waiting for the loss adjustment to be approved" with a link to the `Adjustment` in `documents`
      whose status is `Submitted`. Approving it closes the transfer; cancelling it goes back to receiving.
    - `Received`, `Closed`, `Cancelled`: read only.
  - Dispatch, approve and receive answer `{ transferId, documentId, number, status, postedAtUtc, overReservations }`:
    toast the number (`TRF-…` for a dispatch, `TRR-…` for a receipt).
- **Receive dialog**: one row per line with something outstanding: product, still in transit, a quantity input
  (**base units**, 0 to `outstandingQuantity`, whole when the base unit can't be split) and a destination location
  (default `null` = DEFAULT). "Receive all" fills every row. Send only rows with a quantity > 0:
  `POST /api/inventory/transfers/{id}/receive` with `{ lines: [{ lineId, quantity, locationId }], rowVersion }` and an
  Idempotency-Key. 422 `Inventory.StockTransfer.ReceiptExceedsOutstanding` names the line.
- **Close dialog** (when something will never arrive): reason (`Loss`, `Damage`, `Expiry`, `Other`…) and notes ≤500,
  explaining that everything still in transit is written off once someone approves the loss:
  `POST /api/inventory/transfers/{id}/close` with `{ reason, notes, rowVersion }` → `201 { id }` of the loss adjustment
  (an `Adjustment` in the transit warehouse, already submitted). Show it in the transfer's documents.

## 3. Stock counts — `/inventory/counts`
Starting a count **freezes** its scope (the whole warehouse, or the chosen locations) and records what the system
expects there. Until the count is approved or cancelled nothing posts into or out of that stock: documents,
transfer dispatches and receipts are refused with `Inventory.StockCount.ScopeFrozen`.
- List: `GET /api/inventory/counts?page&pageSize&warehouseId&status` → `{ id, reference, status, warehouseId,
  warehouseCode, isWholeWarehouse, lineCount, countedLineCount, differenceCount, createdAtUtc, createdByUserId,
  createdBy, startedAtUtc, completedAtUtc }`. Columns: reference (`CT-000123`), warehouse, scope (whole / N
  locations), status, counted `countedLineCount/lineCount`, differences, created, started, completed.
- **New count** (`FormDialog`): `POST /api/inventory/counts` with `{ warehouseId, locationIds, notes }` → `201 { id }`.
  Warehouse: active physical in scope. Scope: "Whole warehouse" (send `locationIds: []`) or a multi-select of its
  active locations (at most 200). Notes ≤500.
- **Count page** `/inventory/counts/$id`: `GET /api/inventory/counts/{id}` → `{ id, reference, status, warehouseId,
  warehouseCode, isWholeWarehouse, locations: [{ locationId, locationCode }], notes, createdAtUtc, createdByUserId,
  createdBy, startedAtUtc, startedBy, submittedAtUtc, approvedBy, completedAtUtc, adjustmentDocumentId,
  adjustmentNumber, rowVersion, lines: [{ id, productId, sku, productNameAr, baseUnitCode, locationId, locationCode,
  expectedQuantity, countedQuantity, variance, unitCost }], events: [...] }`.
  - While `Counting`, show a "Stock frozen" banner. The lines table (location, SKU, product, base unit, expected,
    counted, difference) has a counted-quantity input per line, a search box, and "Show differences only".
  - Actions by status:
    - `Draft`: **Start** (confirm: "This freezes the stock of {scope}; nothing posts there until the count is approved
      or cancelled." → `POST …/start` with `{ rowVersion }`), **Cancel**.
    - `Counting`: **Save counts** (`PUT /api/inventory/counts/{id}/lines` with `{ lines: [{ productId, locationId,
      countedQuantity, unitCost }], rowVersion }` — send the lines the user changed; quantities in base units, whole
      when the base unit can't be split; a later save replaces a line's quantity), **Add found item** (dialog: product,
      a location of the count's scope, counted quantity, and a unit cost; the cost is required when the product was
      never valued, otherwise optional), **Submit** (all lines must have a counted quantity: 422
      `Inventory.StockCount.NotFullyCounted`), **Cancel** (releases the freeze, nothing posts).
    - `Submitted`: **Approve** (not the creator; confirm showing the number of differences; `POST …/approve` with
      `{ rowVersion }` and an Idempotency-Key → `{ countId, adjustmentDocumentId, adjustmentNumber, completedAtUtc,
      overReservations }`), **Reject** (dialog: reason, back to `Counting`, still frozen), **Cancel**.
    - `Completed`: read only, with a link to the `CountAdjustment` (`adjustmentDocumentId`, `CNT-…`); no link when
      there was no difference.
  - Approving posts shortages at the average cost and surpluses at their unit cost (or the average) and releases the
    freeze. 409 `Inventory.StockCount.StockChangedDuringCount` means stock in the frozen scope changed outside the
    system: cancel the count and start a new one. 422 `Inventory.Valuation.UnitCostRequired` means a surplus of a
    product that was never valued has no unit cost: reject the count back to counting and enter the cost.

## 4. Stock — `/inventory/stock`
Three tabs:
- **Balances**: `GET /api/inventory/balances?page&pageSize&warehouseId&locationId&productId&includeZero` →
  `{ productId, sku, productNameAr, baseUnitCode, warehouseId, warehouseCode, locationId, locationCode, stockStatus,
  quantity, updatedAtUtc }`. Filters: warehouse (also transit warehouses: stock in transit), location, product,
  "include zero".
- **Availability** (physical warehouses): `GET /api/inventory/availability?page&pageSize&warehouseId&productId&includeZero`
  → `{ productId, sku, productNameAr, baseUnitCode, warehouseId, warehouseCode, onHandSellable, reservedQuantity,
  available }`.
- **Over-reservations**: `GET /api/inventory/over-reservations?page&pageSize&warehouseId&productId` →
  `{ productId, sku, warehouseId, warehouseCode, documentId, documentNumber, onHandSellable, reservedQuantity,
  detectedAtUtc }` (link to the document).

## 5. Stock card — `/inventory/stock-card`
`GET /api/inventory/movements?productId&warehouseId&locationId&fromUtc&toUtc&page&pageSize` → `{ id, postedAtUtc,
documentId, documentNumber, documentType, locationId, locationCode, quantity, balanceAfter }`. Product is required
(picker), warehouse and location optional, a date range. Show in (+) and out (−) quantities, the running
`balanceAfter`, and link the document number.

## 6. Valuation — `/inventory/valuation`
- `GET /api/inventory/valuation?page&pageSize&productId&includeZero` → `{ productId, sku, productNameAr, baseUnitCode,
  totalQuantity, totalValue, averageUnitCost, updatedAtUtc }` (company-wide; the quantity includes stock in transit).
- A row opens its history: `GET /api/inventory/valuation/{productId}/entries?page&pageSize` → `{ id, postedAtUtc,
  documentId, documentNumber, documentType, quantityDelta, valueDelta, unitCost, quantityAfter, valueAfter,
  averageCostAfter }`.
- A note on the page: "Moving weighted average, pending the accountant's approval; no general-ledger posting."

## 7. Reconciliation — `/inventory/reconciliation`
Company-wide users with `inventory.reconciliation.read` only. `GET /api/inventory/reconciliation` (on demand, a
"Run again" button) → `{ generatedAtUtc, isConsistent, availabilityDifferences, ledgerDifferences,
valuationDifferences, transferDifferences, stockPositions, openTransfers }`.
- A green "Inventory is consistent" or red "{n} differences found" banner with `generatedAtUtc`. It only reports:
  nothing is corrected.
- One section per difference list, shown only when not empty:
  - availability `{ productId, sku, warehouseId, warehouseCode, sellableBalance, onHandSellable }`;
  - ledger `{ productId, sku, warehouseId, warehouseCode, locationId, locationCode, movementsQuantity,
    balanceQuantity }`;
  - valuation `{ productId, sku, stockQuantity, valuedQuantity, entriesQuantity, valuedValue, entriesValue }`;
  - transfers `{ transferId, number, productId, sku, outstandingQuantity, transitBalance }` (link to the transfer).
- **Stock positions**: `{ productId, sku, productNameAr, physicalQuantity, inTransitQuantity, valuedQuantity,
  valuedValue }` (physical + in transit = valued).
- **Open transfers**: `{ transferId, number, status, sourceWarehouseCode, destinationWarehouseCode, dispatchedAtUtc,
  daysInTransit, outstandingQuantity }`, longest in transit first.

## 8. Approval policies — `/inventory/settings/approval`
`GET /api/inventory/document-policies` → `[{ documentType, requiresApproval, updatedAtUtc, rowVersion }]` for
`OpeningBalance`, `Receipt`, `Issue`, `Adjustment`, `TransferDispatch`. A switch per row, enabled only for company-wide
users with `inventory.settings.manage`: `PUT /api/inventory/document-policies/{documentType}` with
`{ requiresApproval, rowVersion }` → `{ rowVersion }`. Explain: "Applies to documents and transfers created from now
on. Reversals always need approval."

## Labels to translate

| Permission | Arabic | English |
|---|---|---|
| `inventory.stock.read` | عرض الأرصدة وكارت الصنف | View stock and the stock card |
| `inventory.documents.read` | عرض المستندات والتحويلات والجرد | View documents, transfers and counts |
| `inventory.opening.manage` | إدارة أرصدة أول المدة | Manage opening stock |
| `inventory.receipts.manage` | إدارة أذون الاستلام | Manage goods receipts |
| `inventory.issues.manage` | إدارة أذون الصرف | Manage goods issues |
| `inventory.adjustments.manage` | إدارة التسويات | Manage adjustments |
| `inventory.documents.approve` | اعتماد المستندات والتحويلات والجرد | Approve documents, transfers and counts |
| `inventory.valuation.read` | عرض تقييم المخزون | View inventory valuation |
| `inventory.settings.manage` | إعدادات اعتماد المخزون | Manage inventory approval settings |
| `inventory.transfers.manage` | إعداد وإرسال التحويلات | Prepare and dispatch transfers |
| `inventory.transfers.receive` | استلام التحويلات | Receive transfers |
| `inventory.counts.manage` | إدارة الجرد | Manage stock counts |
| `inventory.reconciliation.read` | عرض تقرير المطابقة | View the reconciliation report |

| Value | Arabic | English |
|---|---|---|
| `OpeningBalance` | رصيد أول المدة | Opening stock |
| `Receipt` | إذن استلام | Goods receipt |
| `Issue` | إذن صرف | Goods issue |
| `Adjustment` | تسوية | Adjustment |
| `Reversal` | عكس مستند | Reversal |
| `TransferDispatch` | إرسال تحويل | Transfer dispatch |
| `TransferReceipt` | استلام تحويل | Transfer receipt |
| `CountAdjustment` | تسوية جرد | Count adjustment |
| status `Draft` / `Submitted` / `Posted` / `Cancelled` | مسودة / بانتظار الاعتماد / مُرحّل / ملغي | Draft / Awaiting approval / Posted / Cancelled |
| transfer `InTransit` / `PartiallyReceived` / `Closing` / `Received` / `Closed` | في الطريق / مستلم جزئيًا / قيد الإقفال / مستلم / مُقفل | In transit / Partly received / Closing / Received / Closed |
| count `Counting` / `Completed` | جاري الجرد / مكتمل | Counting / Completed |
| direction `In` / `Out` / `Move` | وارد / منصرف / نقل | In / Out / Move |
| reason `Damage` / `Loss` / `Expiry` / `Found` / `Correction` / `Other` | تالف / فاقد / منتهي الصلاحية / زيادة / تصحيح / أخرى | Damage / Loss / Expiry / Found / Correction / Other |
| history `Created` / `Submitted` / `Rejected` / `Approved` / `Posted` / `Cancelled` | إنشاء / إرسال للاعتماد / رفض / اعتماد / ترحيل / إلغاء | Created / Submitted / Rejected / Approved / Posted / Cancelled |
| history `Dispatched` / `Received` / `ClosingRequested` / `ClosingAborted` / `Closed` | إرسال / استلام / طلب إقفال / إلغاء الإقفال / إقفال | Dispatched / Received / Closing requested / Closing withdrawn / Closed |
| history `Started` / `Completed` | بدء الجرد / اكتمال | Started / Completed |

## Error codes to translate

| errorCode | Status | Arabic | English |
|---|---|---|---|
| `Inventory.Warehouse.AccessDenied` | 403 | ليس لديك صلاحية على هذا المخزن | You can't access this warehouse |
| `Inventory.Stock.Busy` | 409 | المخزون يُعدَّل الآن، حاول مرة أخرى | This stock is being changed; try again |
| `Inventory.Stock.Insufficient` | 422 | الرصيد المتاح في المخزن لا يكفي | Not enough available stock in this warehouse |
| `Inventory.Stock.InsufficientAtLocation` | 422 | الرصيد في هذا الموقع لا يكفي | Not enough stock in this location |
| `Inventory.Idempotency.KeyReused` | 409 | تم استخدام مفتاح العملية لطلب آخر | This operation key was used for another request |
| `Inventory.Valuation.UnitCostRequired` | 422 | الصنف ليس له تكلفة بعد، أدخل تكلفة الوحدة | The product has no cost yet; enter a unit cost |
| `Inventory.DocumentPolicy.NotFound` | 404 | لا توجد سياسة اعتماد لهذا النوع | This type has no approval policy |
| `Inventory.DocumentPolicy.CompanyWideAccessRequired` | 403 | تغيير سياسات الاعتماد يحتاج صلاحية على كل الشركة | Changing approval policies needs company-wide access |
| `Inventory.Reconciliation.CompanyWideAccessRequired` | 403 | تقرير المطابقة يحتاج صلاحية على كل الشركة | The reconciliation report needs company-wide access |
| `Inventory.StockDocument.NotFound` | 404 | المستند غير موجود | The document does not exist |
| `Inventory.StockDocument.AccessDenied` | 403 | ليس لديك صلاحية على مخزن هذا المستند | You can't access this document's warehouse |
| `Inventory.StockDocument.PermissionDenied` | 403 | ليس لديك صلاحية على هذا النوع من المستندات | You can't manage documents of this type |
| `Inventory.StockDocument.ReversalNotDrafted` | 422 | العكس يُنشأ من المستند المُرحّل نفسه | A reversal is created from the posted document |
| `Inventory.StockDocument.WarehouseNotFound` | 422 | المخزن غير موجود | The warehouse does not exist |
| `Inventory.StockDocument.WarehouseNotActive` | 422 | المخزن غير نشط | The warehouse is inactive |
| `Inventory.StockDocument.WarehouseIsTransit` | 422 | مخزن الترانزيت يستقبل البضاعة عن طريق التحويلات فقط | A transit warehouse takes stock only through transfers |
| `Inventory.StockDocument.OpeningClosed` | 422 | رصيد أول المدة يُدخل قبل ترحيل أي مستند آخر في المخزن | Opening stock comes before any other posting in the warehouse |
| `Inventory.StockDocument.AlreadyReversed` | 409 | هذا المستند له عكس بالفعل | This document already has a reversal |
| `Inventory.StockDocument.CompanySetupRequired` | 422 | أعد بيانات الشركة أولًا | Set up the company first |
| `Inventory.StockDocument.ProductNotFound` | 422 | الصنف غير موجود | The product does not exist |
| `Inventory.StockDocument.ProductNotStockItem` | 422 | الأصناف المخزنية فقط لها رصيد | Only stock items are kept in stock |
| `Inventory.StockDocument.TrackingNotSupported` | 422 | الأصناف المتتبعة بالتشغيلة أو السيريال غير مدعومة بعد | Batch- and serial-tracked products are not supported yet |
| `Inventory.StockDocument.ProductNotActive` | 422 | الصنف غير نشط، يمكن صرفه فقط | The product is inactive; it can only go out |
| `Inventory.StockDocument.ProductUnitNotFound` | 422 | الوحدة ليست من وحدات الصنف | The unit is not one of the product's units |
| `Inventory.StockDocument.ProductUnitNotActive` | 422 | الوحدة غير نشطة | The unit is inactive |
| `Inventory.StockDocument.LocationNotFound` | 422 | الموقع ليس في مخزن المستند | The location is not in the document's warehouse |
| `Inventory.StockDocument.LocationNotActive` | 422 | الموقع غير نشط | The location is inactive |
| `Inventory.StockDocument.DirectionRequired` | 422 | حدد هل التسوية زيادة أم نقص | Say whether the adjustment adds or removes stock |
| `Inventory.StockDocument.NotDraft` | 422 | المسودة فقط يمكن تعديلها أو إرسالها أو ترحيلها مباشرة | Only a draft can be changed, submitted or posted directly |
| `Inventory.StockDocument.NotSubmitted` | 422 | المستند ليس بانتظار الاعتماد | Only a submitted document can be approved or rejected |
| `Inventory.StockDocument.NotOpen` | 422 | المسودة أو المستند بانتظار الاعتماد فقط يمكن إلغاؤه | Only a draft or submitted document can be cancelled |
| `Inventory.StockDocument.NotPosted` | 422 | المستند المُرحّل فقط يمكن عكسه | Only a posted document can be reversed |
| `Inventory.StockDocument.ApprovalRequired` | 422 | هذا المستند يحتاج اعتمادًا: أرسله للاعتماد | This document needs approval: submit it |
| `Inventory.StockDocument.ApprovalNotRequired` | 422 | هذا المستند لا يحتاج اعتمادًا: رحّله مباشرة | This document needs no approval: post it |
| `Inventory.StockDocument.SelfApproval` | 422 | لا يمكنك اعتماد مستند أنشأته | You can't approve a document you created |
| `Inventory.StockDocument.NoLines` | 422 | المستند يحتاج سطرًا واحدًا على الأقل | A document needs at least one line |
| `Inventory.StockDocument.TooManyLines` | 422 | الحد الأقصى 500 سطر | At most 500 lines |
| `Inventory.StockDocument.ReasonRequired` | 422 | التسوية تحتاج سببًا | An adjustment needs a reason |
| `Inventory.StockDocument.ReasonOnlyForAdjustments` | 422 | السبب للتسويات فقط | Only adjustments carry a reason |
| `Inventory.StockDocument.ReversalLinesFixed` | 422 | سطور العكس لا تتغير | A reversal's lines can't change |
| `Inventory.StockDocument.CannotReverseReversal` | 422 | لا يمكن عكس مستند عكس | A reversal can't be reversed |
| `Inventory.StockDocument.LinkedLinesFixed` | 422 | هذا المستند تابع لتحويل أو جرد ولا يمكن تعديله | This document belongs to a transfer or a count; it can't change |
| `Inventory.StockDocument.NotReversible` | 422 | مستندات التحويل والجرد لا تُعكس؛ استخدم تسوية أو تحويلًا جديدًا | Transfer and count documents aren't reversed; post an adjustment or a new transfer |
| `Inventory.StockDocument.WrongDirection` | 422 | هذا النوع لا يحرك المخزون في هذا الاتجاه | This document type can't move stock that way |
| `Inventory.StockDocument.UnitCostRequired` | 422 | رصيد أول المدة والاستلام يحتاجان تكلفة الوحدة | Opening stock and receipts need a unit cost |
| `Inventory.StockDocument.UnitCostNotAllowed` | 422 | المنصرف يُقيَّم بمتوسط التكلفة؛ لا تدخل تكلفة | Stock going out is valued at the average; enter no cost |
| `Inventory.StockDocument.QuantityMustBeWhole` | 422 | هذه الوحدة لا تقبل الكسور | This unit can't be split; enter a whole quantity |
| `Inventory.StockDocument.ConversionNotExact` | 422 | الكمية لا تتحول للوحدة الأساسية بدقة | The quantity doesn't convert exactly to the base unit |
| `Inventory.StockDocument.UnitCostTooLarge` | 422 | تكلفة الوحدة أو قيمة السطر كبيرة جدًا | The unit cost or the line's value is too large |
| `Inventory.StockDocument.QuantityTooLarge` | 422 | الكمية كبيرة جدًا | The quantity is too large |
| `Inventory.StockTransfer.NotFound` | 404 | التحويل غير موجود | The transfer does not exist |
| `Inventory.StockTransfer.AccessDenied` | 403 | ليس لديك صلاحية على مخازن هذا التحويل | You can't access this transfer's warehouses |
| `Inventory.StockTransfer.PermissionDenied` | 403 | ليس لديك صلاحية لهذا الإجراء على التحويلات | You can't do this with transfers |
| `Inventory.StockTransfer.SourceNotUsable` | 422 | مخزن الإرسال يجب أن يكون مخزنًا فعليًا نشطًا | The source must be an active physical warehouse |
| `Inventory.StockTransfer.DestinationNotUsable` | 422 | مخزن الاستلام يجب أن يكون مخزنًا فعليًا نشطًا | The destination must be an active physical warehouse |
| `Inventory.StockTransfer.TransitWarehouseRequired` | 422 | اختر مخزن الترانزيت: فرع الإرسال ليس له مخزن ترانزيت نشط واحد | Choose the transit warehouse: the source branch has no single active one |
| `Inventory.StockTransfer.TransitWarehouseInvalid` | 422 | مخزن الترانزيت يجب أن يكون نشطًا ومن نوع ترانزيت | The transit warehouse must be an active transit warehouse |
| `Inventory.StockTransfer.SameWarehouse` | 422 | مخزن الإرسال ومخزن الاستلام لا يكونان نفس المخزن | Source and destination must differ |
| `Inventory.StockTransfer.NoLines` | 422 | التحويل يحتاج سطرًا واحدًا على الأقل | A transfer needs at least one line |
| `Inventory.StockTransfer.TooManyLines` | 422 | الحد الأقصى 500 سطر | At most 500 lines |
| `Inventory.StockTransfer.NotDraft` | 422 | المسودة فقط يمكن تعديلها أو إرسالها | Only a draft transfer can be changed, submitted or dispatched directly |
| `Inventory.StockTransfer.NotSubmitted` | 422 | التحويل ليس بانتظار الاعتماد | Only a submitted transfer can be approved or rejected |
| `Inventory.StockTransfer.NotOpen` | 422 | لا يمكن إلغاء تحويل تم إرساله؛ استلمه أو أقفله | A dispatched transfer can't be cancelled; receive or close it |
| `Inventory.StockTransfer.ApprovalRequired` | 422 | هذا التحويل يحتاج اعتمادًا: أرسله للاعتماد | This transfer needs approval: submit it |
| `Inventory.StockTransfer.ApprovalNotRequired` | 422 | هذا التحويل لا يحتاج اعتمادًا: أرسله مباشرة | This transfer needs no approval: dispatch it |
| `Inventory.StockTransfer.SelfApproval` | 422 | لا يمكنك اعتماد تحويل أنشأته | You can't approve a transfer you created |
| `Inventory.StockTransfer.NotInTransit` | 422 | التحويل ليس في الطريق أو قيد الإقفال | The transfer isn't in transit (or is being closed) |
| `Inventory.StockTransfer.NothingOutstanding` | 422 | لا يوجد شيء متبقٍ في الطريق | Nothing is still in transit |
| `Inventory.StockTransfer.NothingToReceive` | 422 | حدد السطور والكميات المستلمة | Say which lines arrived and how much |
| `Inventory.StockTransfer.UnknownLine` | 422 | السطر ليس من سطور هذا التحويل | That line is not part of this transfer |
| `Inventory.StockTransfer.ReceiptExceedsOutstanding` | 422 | الكمية المستلمة أكبر من المتبقي في الطريق | More is received than is still in transit |
| `Inventory.StockTransfer.QuantityMustBeWhole` | 422 | الوحدة الأساسية لا تقبل الكسور | The base unit can't be split; receive a whole quantity |
| `Inventory.StockCount.NotFound` | 404 | الجرد غير موجود | The count does not exist |
| `Inventory.StockCount.AccessDenied` | 403 | ليس لديك صلاحية على مخزن هذا الجرد | You can't access this count's warehouse |
| `Inventory.StockCount.ScopeOverlaps` | 409 | جرد آخر يجمّد جزءًا من هذا النطاق | Another count already freezes part of this scope |
| `Inventory.StockCount.StockChangedDuringCount` | 409 | الرصيد تغيّر أثناء الجرد؛ ألغِ الجرد وابدأ جردًا جديدًا | Stock changed during the count; cancel it and start a new one |
| `Inventory.StockCount.ScopeFrozen` | 422 | هذا المخزون مجمّد بسبب جرد جارٍ | This stock is frozen by a running count |
| `Inventory.StockCount.LocationNotFound` | 422 | الموقع ليس موقعًا نشطًا في مخزن الجرد | The location is not an active location of the count's warehouse |
| `Inventory.StockCount.TooManyLocations` | 422 | الجرد للمخزن كله أو 200 موقع بحد أقصى | A count covers the whole warehouse or at most 200 locations |
| `Inventory.StockCount.NotDraft` | 422 | الجرد بدأ بالفعل | The count has already started |
| `Inventory.StockCount.NotCounting` | 422 | الكميات تُدخل والجرد يُرسل أثناء الجرد فقط | Counts are entered and submitted only while counting |
| `Inventory.StockCount.NotSubmitted` | 422 | الجرد ليس بانتظار الاعتماد | Only a submitted count can be approved or rejected |
| `Inventory.StockCount.NotOpen` | 422 | الجرد مكتمل أو ملغي | The count is completed or cancelled |
| `Inventory.StockCount.NotFullyCounted` | 422 | أدخل الكمية المجرودة لكل السطور أولًا | Enter a counted quantity for every line first |
| `Inventory.StockCount.SelfApproval` | 422 | لا يمكنك اعتماد جرد أنشأته | You can't approve a count you created |
| `Inventory.StockCount.OutOfScope` | 422 | الموقع ليس ضمن نطاق هذا الجرد | That location is not part of this count |
| `Inventory.StockCount.QuantityMustBeWhole` | 422 | الوحدة الأساسية لا تقبل الكسور | The base unit can't be split; count a whole quantity |
| `Organization.Warehouse.HoldsStock` | 422 | المخزن به رصيد؛ انقله أو سوّه أولًا | The warehouse still holds stock |
| `Organization.Warehouse.HasOpenInventoryDocuments` | 422 | مستندات أو تحويلات أو جرد مفتوح ما زال يحتاج هذا المخزن | Open documents, transfers or a running count still need this warehouse |
| `Organization.Warehouse.InventoryBusy` | 409 | المخزون يُعدَّل الآن، حاول مرة أخرى | Stock here is being changed; try again |
| `Organization.WarehouseLocation.HoldsStock` | 422 | الموقع به رصيد؛ انقله أو سوّه أولًا | The location still holds stock |
| `Organization.WarehouseLocation.HasOpenInventoryDocuments` | 422 | مستندات أو تحويلات أو جرد مفتوح ما زال يحتاج هذا الموقع | Open documents, transfers or a running count still need this location |

For errors that name a line, a count or a product, the server's `detail` carries it (for example "Count CT-000001 has
frozen this stock…"): show the translated text, then `detail` in smaller type.

## Backend facts (don't fake them)
- Nothing is deleted: drafts are cancelled, posted documents are reversed (or corrected by an adjustment), transfers
  are closed, counts are cancelled.
- Posted documents never change. Documents produced by a transfer or a count can't be edited or reversed.
- Final numbers (`RCV-<branch>-2026-000001`, `TRF-…`, `TRR-…`, `CNT-…`) exist only once posted; drafts show
  `DR-000123`, transfers `TR-000123`, counts `CT-000123`.
- A dispatched transfer can't be cancelled. Receipts are entered in base units.
- Transit warehouses can't be counted and don't take documents. Stock in transit is shown under the transit warehouse
  in balances, in its own location named after the transfer.
- Batch, expiry and serial tracking aren't supported: tracked products are refused. Reservations don't exist yet
  (`reservedQuantity` is 0).
