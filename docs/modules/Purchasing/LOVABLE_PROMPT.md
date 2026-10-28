# Lovable prompt — WarehouseERP Purchasing screens

Copy everything below the line into Lovable, in the same project as the Organization, Identity, Catalog and Inventory
screens. It is built from the backend's real OpenAPI contract (`docs/api/openapi-v1.json`) and an end-to-end run
against the real API; don't invent endpoints, fields or rules. The examples and the manual test steps are in
[`FRONTEND_HANDOFF.md`](FRONTEND_HANDOFF.md).

---

Add the **Purchasing** module to the existing WarehouseERP app. It covers:
- suppliers with contacts and addresses, and a duplicate-name warning;
- purchase requests (optional) that orders draw on;
- purchase orders with discounts and tax, approval, partial receipts, closing and cancelling;
- goods receipts that put the goods into stock;
- supplier invoices matched to the order's receipts, with price-variance approval;
- supplier returns that take goods out of stock and record a pending credit claim;
- the approval policy of requests and orders;
- Inventory's screens recognising the two stock document types Purchasing posts (section 0, do it first).

It is part of the existing app: same App Shell, routing, sign-in, cookie session, XSRF handling, RTK Query `baseApi`
and design system. Don't add another app, another auth provider, Supabase, mock data or any business rule the server
already enforces (amounts, matching, limits): show what the API returns and the errors it sends.

## Build on what exists (don't rebuild it)
Follow `AGENTS.md`, and above all its **UI conventions**:
- Every create and edit form opens in `FormDialog` (`src/shared/ui/FormDialog.tsx`). There are **no `/new` pages** and no
  inline edit forms. Detail pages are read-only (`Panel` + `DetailGrid` + `ReadOnlyValue`); their action buttons open
  modals or confirmations.
- An edit dialog loads its record with `refetchOnMountOrArgChange: true` and fills its form from `useLatestRecord(q)`
  (`src/shared/forms/useLatestRecord.ts`), so the form and its `rowVersion` come from the same fetch.
- Every action that changes state asks first with SweetAlert2 through `useConfirm()` / `useStatusConfirm()`
  (`src/shared/ui/confirm.ts`); `run`/`onConfirm` performs the call and toasts the result. Actions that need input
  (reject reason, close reason, receipt quantities) use a `FormDialog` instead.
- Lists use `ServerTable`; other tables sit in `<Panel bodyClassName="p-0">`. Row actions are icon buttons from
  `src/shared/ui/RowActions.tsx` in an end-aligned last column. "New …" goes in `PageHeader` actions.
- Full-width pages, logical classes only (`ms`/`me`/`ps`/`pe`/`start`/`end`). Radix `DirectionProvider` handles RTL.
- RTK Query endpoints are injected into `src/shared/api/baseApi.ts` with new tag types `Supplier`, `PurchaseRequest`,
  `PurchaseOrder`, `GoodsReceipt`, `SupplierInvoice`, `SupplierReturn`, `PurchasingPolicy`. Receiving and approving a
  return also invalidate Inventory's `StockDocument`, `Stock`, `Valuation` and `Reconciliation` tags; every action on an
  order, invoice, receipt or return invalidates `PurchaseOrder` (its quantities and linked documents change).
- Permission names go in `P`, checked with `can()`; the policy screen also uses `canCompanyWide()`
  (`src/shared/session/permissions.ts`). Every text goes in `translations.ts`; errors under `err.<errorCode>`.
- Code in `src/features/purchasing/`, pages under `src/routes/_authenticated/purchasing/`.
- Sidebar: one collapsible **Purchasing** group in `navGroups` (`src/shared/shell/AppShell.tsx`), after Inventory:
  Suppliers (`purchasing.suppliers.read`), Purchase requests, Purchase orders, Goods receipts, Supplier invoices,
  Supplier returns (each `purchasing.documents.read`), Approval policies (`purchasing.documents.read`).
- In the Identity role editor, add the permission labels from the table at the end.
- Inventory's document screens must recognise `PurchaseReceipt`, `PurchaseReturn` and `sourceDocumentId`: section 0.

## 0. Inventory compatibility (do this first)
The backend now posts two more stock document types, created only by Purchasing. In the existing Inventory code:
- `src/features/inventory/types.ts`: add `"PurchaseReceipt"` and `"PurchaseReturn"` to `documentTypes`, and
  `sourceDocumentId?: string | null` to `InventoryRecord` (the document details return it).
- `src/shared/i18n/translations.ts`: `inv.PurchaseReceipt` (ar "استلام مشتريات", en "Purchase receipt"),
  `inv.PurchaseReturn` (ar "مرتجع مشتريات", en "Purchase return"), `inv.goodsReceipt` (ar "إذن الاستلام", en "Goods
  receipt"), `inv.supplierReturn` (ar "مرتجع المورد", en "Supplier return").
- `src/features/inventory/rules.ts`: add both types to the list that never offers `reverse`, and never offer
  `reverse` when `r.sourceDocumentId` is set. (They are always Posted and `managePermissions` has no entry for them,
  so edit/post/cancel are already hidden; keep it that way. `DraftDialog` must not offer them as new types.)
- `src/features/inventory/RecordPages.tsx`, next to the `transferId` / `countId` links: when `r.sourceDocumentId` is
  set, link `PurchaseReceipt` to `/purchasing/receipts/{sourceDocumentId}` (label `inv.goodsReceipt`) and
  `PurchaseReturn` to `/purchasing/returns/{sourceDocumentId}` (label `inv.supplierReturn`), showing
  `externalReference` (the GRN-/SRN- number) beside it.
- `src/test/inventory-rules.test.ts`: one case each that a posted `PurchaseReceipt` offers no action.
- The documents list filter already maps `documentTypes`, so the new types appear there by themselves.
Nothing else in Inventory changes.

## Permissions and warehouse scope
`GET /api/identity/me` gives `permissions`, `isCompanyWide` and `warehouseIds`. Requests, orders, receipts, invoices and
returns belong to their order's warehouse: a scoped user sees and acts only on their warehouses (403 otherwise). Suppliers
are company-wide master data.

**Approvals:** anyone with `purchasing.documents.approve` and the warehouse in scope approves, **including the user who
created or submitted the document**. Never hide or disable Approve because the current user created the document.

| Action | Needs |
|---|---|
| See suppliers, check similar names | `purchasing.suppliers.read` |
| Create, edit, activate, deactivate suppliers | `purchasing.suppliers.manage` |
| See requests, orders, receipts, invoices, returns, policies | `purchasing.documents.read` + warehouse in scope |
| Draft, edit, submit, cancel, close a purchase request | `purchasing.requests.manage` + warehouse in scope |
| Draft, edit, submit, cancel, close a purchase order | `purchasing.orders.manage` + warehouse in scope |
| Receive goods against an order | `purchasing.receipts.manage` + warehouse in scope |
| Enter, edit, submit, post, cancel a supplier invoice | `purchasing.invoices.manage` + warehouse in scope |
| Draft, edit, submit, cancel a supplier return | `purchasing.returns.manage` + warehouse in scope |
| Approve or reject a request, order, invoice or return | `purchasing.documents.approve` + warehouse in scope (creator included) |
| Change whether requests or orders need approval | `purchasing.settings.manage` + **company-wide** |

Pickers read other modules: warehouses need `organization.warehouses.read`, products and units `catalog.products.read`,
receipt locations `organization.warehouses.read`. Without them show the picker disabled with a hint.

## Shared rules
- **Row versions.** Every detail response has `rowVersion`; send the last one read with every change or action
  (`{ rowVersion }`, or `{ reason, rowVersion }` for reject/close/cancel). 409 `Common.ConcurrencyConflict` → reload and
  keep what the user typed. Updates and actions answer `200 { rowVersion }`; creates `201 { id }`.
- **Idempotency-Key header** on: `POST /orders/{id}/receipts`, `POST /invoices/{id}/post`, `POST /invoices/{id}/approve`,
  `POST /returns/{id}/approve`. Generate `crypto.randomUUID()` when the user confirms. If the request fails without an
  answer (network, timeout, 5xx), retry with the **same key and the same body**: the server answers with the first result
  and posts nothing twice. A new user action gets a new key. 409 `Purchasing.Idempotency.KeyReused` = the key was used for
  another request; 409 `Purchasing.Idempotency.Busy` = the first attempt is still running, retry shortly.
- **Stock errors pass through from Inventory** with Inventory's codes (for example `Inventory.Stock.Insufficient`,
  `Inventory.StockCount.ScopeFrozen`, `Inventory.StockDocument.ProductNotActive`): reuse the existing translations.
- **Quantities** up to 4 decimals, whole for a unit that can't be split, always in the line's unit (`unitCode`). Prices up
  to 8 decimals; discount and tax are percentages 0–100 (4 decimals); amounts come back rounded to the currency
  (`currencyCode`, the company currency): show them with 2 decimals (3 for KWD/BHD/OMR/JOD/TND/LYD/IQD).
- **Tax:** each line has `taxRate` and `taxTreatment`. Only `Recoverable` is supported; don't offer `NonRecoverable`
  (the server refuses it with `Purchasing.Line.NonRecoverableTaxNotSupported`). Prices are before tax.
- Show amounts the server computed (`grossAmount`, `discountAmount`, `netAmount`, `taxAmount`, `totalAmount`); in an
  edit dialog you may preview line amounts with the same formula — gross = qty × price, discount = gross × d %,
  net = gross − discount, tax = net × t %, each rounded half away from zero — but the saved values are the server's.
- Codes, SKUs, numbers and registration numbers are ASCII: show them `dir="ltr"`. Times are UTC: show local time.
  `invoiceDate`, `dueDate`, `expectedDeliveryDate`, `neededByDate` are dates (`YYYY-MM-DD`), no time zone.
- Status badges: `Draft` grey, `Submitted` amber, `Approved` blue, `PartiallyReceived` blue, `Received`/`Posted`/`Ordered`
  green, `Closed` slate, `Cancelled` muted.
- Drafts show `draftReference` (`DPR-000001`, `DPO-…`, `DSIN-…`, `DSRN-…`) until they have a `number`
  (`PR-CAI-2026-000001`, `PO-…`, `GRN-…`, `SIN-…`, `SRN-…`).
- Every list pages (`page`, `pageSize` ≤ 100) and returns `{ items, page, pageSize, totalCount, totalPages }`. Lists use
  `ServerTable` with the filters named in each section (search box + selects), keep filters and paging the way the
  Inventory lists do, and open the detail page on row click.
- For line errors the server's `detail` names the line ("Line 2: …"); show the translation, then `detail` smaller.

## Arabic / English, RTL / LTR
- Arabic is the default (RTL), English the alternative (LTR), exactly as the rest of the app: every label, button,
  status, action, enum value and error goes in `src/shared/i18n/translations.ts` with both `ar` and `en`, keys
  prefixed `pur.` (errors stay `err.<errorCode>`). No hard-coded text.
- Logical classes only (`ms`/`me`/`ps`/`pe`/`start`/`end`); tables, dialogs and row actions mirror in RTL.
- Codes, SKUs, document numbers, registration numbers, invoice numbers, emails and phones are shown `dir="ltr"`.
  Product and supplier names show `nameAr` (with `nameEn` where the API has it, in English mode).
- Amounts and quantities use the app's number formatting for the active language; dates as the app formats them.

## Status history and audit
Every detail page of a request, order, invoice and return shows:
- a "who and when" block: `createdBy` + `createdAtUtc`, `submittedBy`, `approvedBy` (+ `approvedAtUtc` on orders),
  `postedBy` + `postedAtUtc` on invoices, `postedAtUtc` on returns. The same person may appear as creator and approver:
  that is allowed and must be shown as it is;
- the timeline from `events` (`action`, `userName`, `atUtc`, `note`): reuse Inventory's `History` component — move it
  from `src/features/inventory/components.tsx` to `src/shared/ui/History.tsx` with a `prefix` prop for the
  translation key (`inv.` for Inventory, `pur.` for Purchasing) and update Inventory's import; don't copy it. A
  rejection's or closure's reason, and the GRN/SIN/SRN number on an order's `GoodsReceived`/`Invoiced`/`Returned`
  events, are in `note`.
Goods receipts have no events: show `receivedBy` and `receivedAtUtc`.

## 1. Suppliers — `/purchasing/suppliers`
List: code, Arabic name, English name, country, tax number, phone, payment terms, active badge. Filters: search (code,
names, tax number), active, country. Row actions: view, edit, activate/deactivate (SweetAlert).

Create/edit dialog (code only on create, fixed afterwards): code (`^[A-Za-z0-9_-]+$`, ≤ 20, stored upper case),
Arabic name, English name, country (ISO 2 letters, default `EG`), tax registration number, commercial registration
number + registry office (office required with the number), payment terms days (0–365), phone, email, notes; a
contacts table (name, job title, phone, email, primary — at most one) and an addresses table (type
Billing/Shipping/Other, line 1, line 2, city, region, postal code, country, primary — at most one). Saving sends the
whole lists; they replace the old ones.

**Duplicate warning:** when the Arabic or English name loses focus, call
`GET /api/purchasing/suppliers/similar?nameAr=&nameEn=&exceptSupplierId=` and, if it returns suppliers, show a
non-blocking warning listing them (code, names, tax number, active). The user may still save. Registration numbers
are checked by the server: 409 `TaxNumberTaken` / `CommercialRegistrationTaken`, 422 `EgyptianTaxNumberInvalid` (9 digits
for `EG`; dashes and spaces are ignored).

Detail page: all fields, contacts, addresses, created/updated time.

## 2. Purchase requests — `/purchasing/requests`
List: reference/number, status, warehouse, needed by, lines, created, created by. Filters: warehouse, status, search,
"orderable" (approved with something left: `orderable=true`).

Create/edit dialog: warehouse (physical, in scope; fixed after create), needed-by date, notes, lines (product, unit,
quantity, notes). Detail: lines with `quantity`, `orderedQuantity`, `remainingQuantity`; history (`events`: action,
user, time, note).

Actions by status: Draft → Edit, Submit, Cancel; Submitted → Approve, Reject (reason), Cancel; Approved → Close (reason),
Cancel (only while nothing is ordered); Ordered/Closed/Cancelled → none. Submitting a request whose policy needs no
approval approves it at once (it comes back `Approved` with a number).

## 3. Purchase orders — `/purchasing/orders`
List: reference/number, status, supplier, warehouse, total, expected delivery, created by, approved at. Filters:
warehouse, supplier, status, search (number, draft reference, supplier reference).

Create/edit dialog: supplier (active suppliers only; `GET /api/purchasing/suppliers?isActive=true`), warehouse (fixed
after create), expected delivery date, supplier reference, payment terms (defaults to the supplier's), notes, lines:
product, unit, quantity, unit price, discount %, tax %, and optionally "from request" — a picker of orderable request
lines of the same warehouse (`GET /api/purchasing/requests?warehouseId=&orderable=true`, then the request's lines with
`remainingQuantity`); picking one fills product and unit (both fixed for that line) and sends `purchaseRequestLineId`.
Show a running line total and the order totals.

Detail page: header with supplier, warehouse, totals, payment terms; lines with ordered, received, remaining, invoiced,
invoiceable, returned; a **Documents** panel from `documents` (receipts, invoices, returns with reference, status,
amount, linking to their pages); history.

Actions by status:
- Draft → Edit, Submit, Cancel. Submit with no approval needed approves at once.
- Submitted → Approve, Reject (reason), Cancel.
- Approved → **Receive goods**, Cancel (only before any receipt), "New invoice", nothing else.
- PartiallyReceived → **Receive goods**, Close (reason: the rest won't come), "New invoice", "New return" (from a receipt).
- Received / Closed → "New invoice", "New return". Cancelled → none.

**Receive goods dialog** (`POST /api/purchasing/orders/{id}/receipts`, Idempotency-Key): supplier delivery note, notes,
one row per order line with remaining > 0: quantity (default 0, max `remainingQuantity`), location (the warehouse's
locations; "Default" sends `null`). Send only rows with a quantity. The answer is `{ id, number, stockDocumentId,
stockDocumentNumber, receivedAtUtc }`: toast "GRN-… posted (stock PRC-…)" and open the receipt. 422 `OverReceipt` /
`NotReceivable` → reload the order (someone else received meanwhile).

## 4. Goods receipts — `/purchasing/receipts`
List: number, order number, supplier, warehouse, stock document number, delivery note, received at, received by.
Filters: warehouse, supplier, order, search. Read-only detail: lines (product, unit, quantity, unit cost, location,
returned, returnable), the stock document (link to `/inventory/documents/{stockDocumentId}`), its returns. Action:
"New return" when something is returnable.

## 5. Supplier invoices — `/purchasing/invoices`
List: reference/number, status, supplier, order, supplier invoice number, invoice date, due date, total, price
variance, "needs approval" badge. Filters: supplier, order, status, search, "due by" (`dueOnOrBefore`, posted only).

Create/edit dialog, opened from an order (or with an order picker): supplier invoice number (as printed), invoice date
(not after today), due date (optional; defaults to invoice date + the order's payment terms), notes, lines: one row per
order line with `invoiceableQuantity` > 0 — quantity (max invoiceable), unit price (prefilled with the order's), discount
%, tax %. Show per line the order's net unit price and the variance when the net price differs, and a banner "Prices
differ from the order: it will need approval" when any does.

Actions by status: Draft → Edit, **Post** (when `requiresApproval` is false; Idempotency-Key) or **Submit** (when true),
Cancel; Submitted → Approve (Idempotency-Key; posts it), Reject (reason), Cancel; Posted, Cancelled → none (posted
invoices never change). 409 `InvoiceNumberTaken` → the supplier already has that number (case, spaces and dashes don't
matter). 422 `InvoiceExceedsReceipts` → reload the order.

## 6. Supplier returns — `/purchasing/returns`
List: reference/number, status, receipt, order, supplier, warehouse, reason, credit claim, created, posted. Filters:
supplier, order, receipt, status, "with credit claim", search.

Create/edit dialog, opened from a receipt: reason (required), notes, one row per receipt line with `returnableQuantity`
> 0: quantity (max returnable), location (default: where it was received). Actions: Draft → Edit, Submit, Cancel;
Submitted → **Approve** (Idempotency-Key; the goods leave stock), Reject (reason), Cancel. Every return needs
approval; its creator may approve it. 422 `Inventory.Stock.Insufficient` → the goods are no longer there.

Detail after posting: per line `uninvoicedQuantity` (returned before invoicing: won't be invoiced) and
`invoicedQuantity` with `creditClaimNetAmount` / `creditClaimTaxAmount`; header `creditClaimTotalAmount` labelled
"Pending supplier credit (for Accounting)" — it is not a posted credit note. Link the stock document (`PRT-…`).

## 7. Approval policies — `/purchasing/settings/approval`
Two rows from `GET /api/purchasing/policies`: purchase requests and purchase orders, each a switch "needs approval".
Only company-wide users with `purchasing.settings.manage` may switch (`PUT /api/purchasing/policies/{documentType}`
`{ requiresApproval, rowVersion }`); others see it read-only. Explain: "Changes apply to documents created afterwards;
approvers may approve their own documents."

## Labels to translate

| Permission | Arabic | English |
|---|---|---|
| `purchasing.suppliers.read` | عرض الموردين | View suppliers |
| `purchasing.suppliers.manage` | إدارة الموردين | Manage suppliers |
| `purchasing.documents.read` | عرض مستندات المشتريات | View purchasing documents |
| `purchasing.requests.manage` | إدارة طلبات الشراء | Manage purchase requests |
| `purchasing.orders.manage` | إدارة أوامر الشراء | Manage purchase orders |
| `purchasing.receipts.manage` | استلام البضاعة | Receive goods |
| `purchasing.invoices.manage` | إدارة فواتير الموردين | Manage supplier invoices |
| `purchasing.returns.manage` | إدارة مرتجعات الموردين | Manage supplier returns |
| `purchasing.documents.approve` | اعتماد مستندات المشتريات | Approve purchasing documents |
| `purchasing.settings.manage` | إعدادات اعتماد المشتريات | Manage purchasing approval settings |

| Value | Arabic | English |
|---|---|---|
| `PurchaseRequest` | طلب شراء | Purchase request |
| `PurchaseOrder` | أمر شراء | Purchase order |
| `GoodsReceipt` | إذن استلام | Goods receipt |
| `SupplierInvoice` | فاتورة مورد | Supplier invoice |
| `SupplierReturn` | مرتجع لمورد | Supplier return |
| `PurchaseReceipt` (stock document) | استلام مشتريات | Purchase receipt |
| `PurchaseReturn` (stock document) | مرتجع مشتريات | Purchase return |
| `Draft` / `Submitted` / `Approved` | مسودة / بانتظار الاعتماد / معتمد | Draft / Submitted / Approved |
| `PartiallyReceived` / `Received` | مستلم جزئيًا / مستلم بالكامل | Partially received / Received |
| `Ordered` / `Closed` / `Posted` / `Cancelled` | تم الطلب / مغلق / مُرحَّل / ملغي | Ordered / Closed / Posted / Cancelled |
| `Recoverable` | ضريبة قابلة للاسترداد | Recoverable tax |
| `Billing` / `Shipping` / `Other` | فواتير / شحن / أخرى | Billing / Shipping / Other |
| Actions `Created` … `Returned` | أُنشئ، أُرسل للاعتماد، رُفض، اعتُمد، أُلغي، أُغلق، رُحّل، استُلمت بضاعة، فُوتر، أُرجع | Created, Submitted, Rejected, Approved, Cancelled, Closed, Posted, Goods received, Invoiced, Returned |

## Error codes to translate

| errorCode | Status | Arabic | English |
|---|---|---|---|
| `Purchasing.Access.WarehouseDenied` | 403 | ليس لديك صلاحية على مستندات هذا المخزن | You can't access this warehouse's purchasing documents |
| `Purchasing.Access.CompanyWideRequired` | 403 | تغيير سياسات الاعتماد يحتاج صلاحية على كل الشركة | Changing approval policies needs company-wide access |
| `Purchasing.DocumentPolicy.NotFound` | 404 | لا توجد سياسة اعتماد لهذا النوع | This type has no approval policy |
| `Purchasing.Idempotency.KeyReused` | 409 | تم استخدام مفتاح العملية لطلب آخر | This operation key was used for another request |
| `Purchasing.Idempotency.Busy` | 409 | العملية نفسها قيد التنفيذ، حاول بعد لحظات | The same request is still running; try again shortly |
| `Purchasing.Reference.CompanySetupRequired` | 422 | قم بإعداد الشركة قبل المشتريات | Set up the company first |
| `Purchasing.Reference.WarehouseNotFound` | 422 | المخزن غير موجود | The warehouse does not exist |
| `Purchasing.Reference.WarehouseNotActive` | 422 | المخزن غير نشط | The warehouse is inactive |
| `Purchasing.Reference.WarehouseIsTransit` | 422 | الشراء يكون لمخزن فعلي لا لمخزن عبور | Buy into a physical warehouse, not a transit one |
| `Purchasing.Reference.ProductNotFound` | 422 | الصنف غير موجود | The product does not exist |
| `Purchasing.Reference.ProductNotStockItem` | 422 | المشتريات تقبل الأصناف المخزنية فقط | Only stock items can be bought |
| `Purchasing.Reference.TrackingNotSupported` | 422 | الأصناف المتتبعة بالتشغيلة أو السيريال غير مدعومة بعد | Batch- and serial-tracked products aren't supported yet |
| `Purchasing.Reference.ProductNotActive` | 422 | الصنف غير نشط | The product is inactive |
| `Purchasing.Reference.ProductUnitNotFound` | 422 | الوحدة ليست من وحدات الصنف | The unit isn't one of the product's units |
| `Purchasing.Reference.ProductUnitNotActive` | 422 | الوحدة غير نشطة | The unit is inactive |
| `Purchasing.Reference.LocationNotFound` | 422 | الموقع ليس في مخزن الأمر | The location isn't in the order's warehouse |
| `Purchasing.Reference.LocationNotActive` | 422 | الموقع غير نشط | The location is inactive |
| `Purchasing.Line.QuantityInvalid` | 422 | الكمية يجب أن تكون موجبة وبحد أقصى 4 أرقام عشرية | Quantity must be positive, at most 4 decimals |
| `Purchasing.Line.QuantityMustBeWhole` | 422 | هذه الوحدة لا تتجزأ، أدخل عددًا صحيحًا | This unit can't be split; enter a whole number |
| `Purchasing.Line.ConversionNotExact` | 422 | الكمية لا تتحول بدقة إلى الوحدة الأساسية | The quantity doesn't convert exactly to the base unit |
| `Purchasing.Line.UnitPriceInvalid` | 422 | سعر الوحدة غير صحيح | The unit price is invalid |
| `Purchasing.Line.DiscountInvalid` | 422 | الخصم بين 0 و100% | Discount must be 0–100 % |
| `Purchasing.Line.TaxRateInvalid` | 422 | نسبة الضريبة بين 0 و100% | Tax rate must be 0–100 % |
| `Purchasing.Line.NonRecoverableTaxNotSupported` | 422 | الضريبة غير القابلة للاسترداد غير مدعومة حاليًا | Non-recoverable tax isn't supported yet |
| `Purchasing.Line.AmountTooLarge` | 422 | قيمة السطر كبيرة جدًا | The line amount is too large |
| `Purchasing.Supplier.NotFound` | 404 | المورد غير موجود | The supplier does not exist |
| `Purchasing.Supplier.CodeTaken` | 409 | كود المورد مستخدم | This supplier code is taken |
| `Purchasing.Supplier.TaxNumberTaken` | 409 | رقم التسجيل الضريبي مسجل لمورد آخر | Another supplier has this tax number |
| `Purchasing.Supplier.CommercialRegistrationTaken` | 409 | السجل التجاري مسجل لمورد آخر في نفس المكتب | Another supplier has this commercial registration |
| `Purchasing.Supplier.NotActive` | 422 | المورد غير نشط ولا يقبل أوامر جديدة | The supplier is inactive and takes no new orders |
| `Purchasing.Supplier.CountryInvalid` | 422 | الدولة رمز من حرفين | The country must be a two-letter code |
| `Purchasing.Supplier.TaxNumberInvalid` | 422 | رقم التسجيل الضريبي غير صحيح | The tax number is invalid |
| `Purchasing.Supplier.EgyptianTaxNumberInvalid` | 422 | رقم التسجيل الضريبي المصري 9 أرقام | An Egyptian tax number has 9 digits |
| `Purchasing.Supplier.CommercialRegistrationInvalid` | 422 | رقم السجل التجاري غير صحيح | The commercial registration number is invalid |
| `Purchasing.Supplier.CommercialRegistryRequired` | 422 | حدد مكتب السجل التجاري | Name the commercial registry office |
| `Purchasing.Supplier.PaymentTermsInvalid` | 422 | مدة السداد من 0 إلى 365 يومًا | Payment terms are 0–365 days |
| `Purchasing.Supplier.TooManyContacts` / `TooManyAddresses` | 422 | عدد جهات الاتصال أو العناوين أكثر من المسموح | Too many contacts or addresses |
| `Purchasing.Supplier.SeveralPrimaryContacts` / `SeveralPrimaryAddresses` | 422 | يسمح بجهة اتصال أو عنوان رئيسي واحد فقط | Only one primary contact or address |
| `Purchasing.PurchaseRequest.NotFound` | 404 | طلب الشراء غير موجود | The purchase request does not exist |
| `Purchasing.PurchaseRequest.NotDraft` / `NotSubmitted` | 422 | حالة الطلب لا تسمح بهذا الإجراء | The request's status doesn't allow this |
| `Purchasing.PurchaseRequest.ApprovalRequired` / `ApprovalNotRequired` | 422 | سياسة الاعتماد لا تسمح بهذا الإجراء | The approval policy doesn't allow this |
| `Purchasing.PurchaseRequest.NotCancellable` | 422 | لا يمكن إلغاء طلب تم الشراء منه، أغلقه بدلًا من ذلك | A request already ordered from can't be cancelled; close it |
| `Purchasing.PurchaseRequest.NotClosable` | 422 | يُغلق الطلب المعتمد غير المكتمل فقط | Only an approved, not fully ordered request can be closed |
| `Purchasing.PurchaseRequest.NoLines` / `TooManyLines` | 422 | عدد السطور غير صحيح (1 إلى 200) | 1 to 200 lines |
| `Purchasing.PurchaseRequest.LineNotFound` | 422 | سطر طلب الشراء غير موجود | The request line does not exist |
| `Purchasing.PurchaseRequest.NotOrderable` | 422 | طلب الشراء غير معتمد أو مغلق أو مكتمل | The request isn't approved, or is closed or fully ordered |
| `Purchasing.PurchaseRequest.ExceedsRequest` | 422 | الكمية أكبر من المتبقي في طلب الشراء | More than what remains on the request |
| `Purchasing.PurchaseRequest.WarehouseMismatch` | 422 | طلب الشراء لمخزن آخر | The request is for another warehouse |
| `Purchasing.PurchaseRequest.ProductMismatch` | 422 | اطلب نفس صنف ووحدة سطر الطلب | Order the request line's product and unit |
| `Purchasing.PurchaseOrder.NotFound` | 404 | أمر الشراء غير موجود | The purchase order does not exist |
| `Purchasing.PurchaseOrder.NotDraft` / `NotSubmitted` / `NotApproved` | 422 | حالة الأمر لا تسمح بهذا الإجراء | The order's status doesn't allow this |
| `Purchasing.PurchaseOrder.ApprovalRequired` / `ApprovalNotRequired` | 422 | سياسة الاعتماد لا تسمح بهذا الإجراء | The approval policy doesn't allow this |
| `Purchasing.PurchaseOrder.NotCancellable` | 422 | لا يمكن إلغاء أمر تم الاستلام عليه، أغلقه بدلًا من ذلك | An order with receipts can't be cancelled; close it |
| `Purchasing.PurchaseOrder.NothingReceivedToClose` | 422 | لم يُستلم شيء، ألغِ الأمر بدلًا من إغلاقه | Nothing was received; cancel instead |
| `Purchasing.PurchaseOrder.NotClosable` | 422 | يُغلق الأمر المستلم جزئيًا فقط | Only a partially received order can be closed |
| `Purchasing.PurchaseOrder.NotReceivable` | 422 | لا يمكن الاستلام على هذا الأمر الآن | Goods can't be received against this order now |
| `Purchasing.PurchaseOrder.OverReceipt` | 422 | الكمية أكبر من المتبقي استلامه | More than what remains to be received |
| `Purchasing.PurchaseOrder.InvoiceExceedsReceipts` | 422 | الكمية أكبر من المستلم غير المفوتر | More than what was received and not invoiced |
| `Purchasing.PurchaseOrder.ReturnExceedsReceipts` | 422 | الكمية أكبر من المستلم غير المرتجع | More than what was received and not returned |
| `Purchasing.PurchaseOrder.NoLines` / `TooManyLines` / `DuplicateLine` | 422 | سطور الأمر غير صحيحة | The order's lines are invalid |
| `Purchasing.PurchaseOrder.LineNotFound` | 422 | السطر ليس من سطور الأمر | Not a line of the order |
| `Purchasing.PurchaseOrder.PaymentTermsInvalid` | 422 | مدة السداد من 0 إلى 365 يومًا | Payment terms are 0–365 days |
| `Purchasing.PurchaseOrder.RequestLineRepeated` | 422 | سطر طلب الشراء مكرر في الأمر | A request line appears twice |
| `Purchasing.GoodsReceipt.NotFound` | 404 | إذن الاستلام غير موجود | The goods receipt does not exist |
| `Purchasing.SupplierInvoice.NotFound` | 404 | فاتورة المورد غير موجودة | The supplier invoice does not exist |
| `Purchasing.SupplierInvoice.InvoiceNumberTaken` | 409 | للمورد فاتورة بنفس الرقم | The supplier already has an invoice with this number |
| `Purchasing.SupplierInvoice.InvoiceNumberInvalid` | 422 | رقم فاتورة المورد غير صحيح | The supplier invoice number is invalid |
| `Purchasing.SupplierInvoice.InvoiceDateInFuture` | 422 | تاريخ الفاتورة بعد اليوم | The invoice date is after today |
| `Purchasing.SupplierInvoice.DueBeforeInvoice` | 422 | تاريخ الاستحقاق قبل تاريخ الفاتورة | The due date is before the invoice date |
| `Purchasing.SupplierInvoice.OrderNotApproved` | 422 | الفاتورة تُسجل على أمر معتمد فقط | Invoices are recorded against approved orders only |
| `Purchasing.SupplierInvoice.ApprovalRequired` | 422 | الأسعار تختلف عن الأمر: أرسلها للاعتماد | Prices differ from the order: submit for approval |
| `Purchasing.SupplierInvoice.ApprovalNotRequired` | 422 | الأسعار مطابقة للأمر: رحّلها مباشرة | Prices match the order: post it directly |
| `Purchasing.SupplierInvoice.NotDraft` / `NotSubmitted` / `NotOpen` | 422 | حالة الفاتورة لا تسمح بهذا الإجراء | The invoice's status doesn't allow this |
| `Purchasing.SupplierInvoice.NoLines` / `TooManyLines` / `DuplicateLine` / `OrderLineNotFound` | 422 | سطور الفاتورة غير صحيحة | The invoice's lines are invalid |
| `Purchasing.SupplierReturn.NotFound` | 404 | مرتجع المورد غير موجود | The supplier return does not exist |
| `Purchasing.SupplierReturn.ReceiptNotFound` | 422 | إذن الاستلام غير موجود | The goods receipt does not exist |
| `Purchasing.SupplierReturn.ExceedsReceipt` | 422 | الكمية أكبر من المتبقي في إذن الاستلام | More than what the receipt line can still return |
| `Purchasing.SupplierReturn.NotDraft` / `NotSubmitted` / `NotOpen` | 422 | حالة المرتجع لا تسمح بهذا الإجراء | The return's status doesn't allow this |
| `Purchasing.SupplierReturn.NoLines` / `TooManyLines` / `DuplicateLine` / `ReceiptLineNotFound` | 422 | سطور المرتجع غير صحيحة | The return's lines are invalid |
| `Inventory.StockDocument.ManagedByPurchasing` | 422 | هذا المستند يُدار من المشتريات | This stock document is managed by Purchasing |
| `Inventory.StockDocument.TypeNotDrafted` | 422 | هذا النوع يُنشأ من التحويل أو الجرد فقط | This type comes from a transfer or count only |
| `Inventory.StockDocument.SourceAlreadyPosted` | 409 | تم ترحيل هذا المستند للمخزون من قبل | Already posted to stock |

## Backend facts (don't fake them)
- Nothing is deleted: drafts are cancelled, orders are cancelled or closed, posted receipts, invoices and returns never
  change. A receipt is corrected by a supplier return, not by reversing its stock document.
- Receiving, posting an invoice and approving a return each run in one transaction with their stock or liability;
  a refusal leaves nothing behind.
- Goods enter stock at the order's net unit price (after discount, before tax). An invoice at another price records a
  price variance; stock is **not** revalued. Returned goods leave stock at the moving average.
- An order becomes Received by itself; returns never reopen it. Over-receipt is never accepted.
- Foreign currency, header discounts, non-recoverable tax, payments and supplier balances don't exist yet.
