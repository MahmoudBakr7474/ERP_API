# Lovable prompt — WarehouseERP Sales screens

Copy everything below the line into Lovable, in the same project as the Organization, Identity, Catalog, Inventory and
Purchasing screens. It is built from the backend's real OpenAPI contract (`docs/api/openapi-v1.json`) and an end-to-end
run against the real API; don't invent endpoints, fields or rules. The examples and the manual test steps are in
[`FRONTEND_HANDOFF.md`](FRONTEND_HANDOFF.md).

---

Add the **Sales** module to the existing WarehouseERP app. It covers:
- customers with contacts, addresses, credit terms and a duplicate warning;
- the company's sales price list;
- quotations that become sales orders;
- sales orders that reserve stock when confirmed, with approval when the settings ask for it, reducing, closing and
  cancelling;
- picking lists and deliveries from one or more locations;
- sales invoices for what was delivered;
- customer returns that bring goods back into stock and record a pending customer credit;
- the sales settings (approval rules, credit hold policy, default VAT);
- Inventory's screens recognising the two stock document types Sales posts and its reservations (section 0, do it
  first).

It is part of the existing app: same App Shell, routing, sign-in, cookie session, XSRF handling, RTK Query `baseApi`
and design system. Don't add another app, another auth provider, Supabase, mock data or any business rule the server
already enforces (prices, amounts, reservations, limits): show what the API returns and the errors it sends.

## Build on what exists (don't rebuild it)
Follow `AGENTS.md`, and above all its **UI conventions**:
- Every create and edit form opens in `FormDialog` (`src/shared/ui/FormDialog.tsx`). There are **no `/new` pages** and no
  inline edit forms. Detail pages are read-only (`Panel` + `DetailGrid` + `ReadOnlyValue`); their action buttons open
  modals or confirmations.
- An edit dialog loads its record with `refetchOnMountOrArgChange: true` and fills its form from `useLatestRecord(q)`
  (`src/shared/forms/useLatestRecord.ts`), so the form and its `rowVersion` come from the same fetch.
- Every action that changes state asks first with SweetAlert2 through `useConfirm()` / `useStatusConfirm()`
  (`src/shared/ui/confirm.ts`); `run`/`onConfirm` performs the call and toasts the result. Actions that need input
  (reject, decline, close, reduce, credit hold reason) use a `FormDialog` instead.
- Lists use `ServerTable`; other tables sit in `<Panel bodyClassName="p-0">`. Row actions are icon buttons from
  `src/shared/ui/RowActions.tsx` in an end-aligned last column. "New …" goes in `PageHeader` actions.
- Full-width pages, logical classes only (`ms`/`me`/`ps`/`pe`/`start`/`end`). Radix `DirectionProvider` handles RTL.
- Reuse, don't copy: move `Money`, `Qty`, `DateValue`, `PHistory` and `RetryNotice` from
  `src/features/purchasing/components.tsx` to `src/shared/ui/` (`PHistory` becomes `DocHistory` with a translation
  `prefix` prop: `pur.` for Purchasing, `sal.` for Sales), update Purchasing's imports, change nothing in their
  behaviour. Customer addresses use `src/features/organization/GeographyPickers.tsx` exactly as the supplier dialog does.
- RTK Query endpoints are injected into `src/shared/api/baseApi.ts` with new tag types `Customer`, `SalesPrice`,
  `SalesSettings`, `SalesQuotation`, `SalesOrder`, `SalesDelivery`, `SalesInvoice`, `SalesReturn`. Confirming,
  approving, cancelling, reducing or closing an order, posting a delivery and approving a return also invalidate
  Inventory's `StockDocument`, `Stock`, `Valuation` and `Reconciliation` tags; every action on a delivery, invoice or
  return invalidates `SalesOrder` (its quantities and linked documents change) and `Customer` (its exposure).
- Permission names go in `P`, checked with `can()`; the settings screen also uses `canCompanyWide()`
  (`src/shared/session/permissions.ts`). Every text goes in `translations.ts` (keys `sal.`); errors under
  `err.<errorCode>`.
- Code in `src/features/sales/`, pages under `src/routes/_authenticated/sales/`.
- Sidebar: one collapsible **Sales** group in `navGroups` (`src/shared/shell/AppShell.tsx`), after Purchasing:
  Customers (`sales.customers.read`), Price list, Quotations, Sales orders, Deliveries, Sales invoices, Customer
  returns (each `sales.documents.read`), Sales settings (`sales.documents.read`).
- In the Identity role editor, add the permission labels from the table at the end.

## 0. Inventory compatibility (do this first)
The backend now posts two more stock document types, created only by Sales, and keeps reservations. In the existing
Inventory code:
- `src/features/inventory/types.ts`: add `"SalesDelivery"` and `"SalesReturn"` to `documentTypes`.
- `src/features/inventory/rules.ts`: next to `purchasingOwned`, treat `SalesDelivery` and `SalesReturn` the same way
  (never offer edit, post, cancel or reverse; the API refuses with `Inventory.StockDocument.ManagedBySales`);
  `DraftDialog` must not offer them as new types.
- `src/features/inventory/RecordPages.tsx`, beside the Purchasing links: when `r.sourceDocumentId` is set, link
  `SalesDelivery` to `/sales/deliveries/{sourceDocumentId}` (label `inv.salesDelivery`) and `SalesReturn` to
  `/sales/returns/{sourceDocumentId}` (label `inv.customerReturn`), showing `externalReference` (the DN-/SR- number).
- A read-only **Reservations** page under Inventory (`/inventory/reservations`, `inventory.stock.read`):
  `GET /api/inventory/reservations?page&pageSize&warehouseId&productId&includeClosed` → paged `{ sourceNumber (the SO-
  number), sku, warehouseCode, reservedQuantity, consumedQuantity, releasedQuantity, remainingQuantity, createdAtUtc }`
  (quantities in the product's base unit). Link `sourceDocumentId` to `/sales/orders/{id}`.
- The reconciliation page (`src/features/inventory/SettingsPages.tsx`): add the section `reservationDifferences`
  (`sku`, `warehouseCode`, `reservedQuantity`, `heldByReservations`) to the list of difference sections.
- `src/shared/i18n/translations.ts`: `inv.SalesDelivery` (ar "صرف مبيعات", en "Sales delivery"), `inv.SalesReturn`
  (ar "مرتجع مبيعات", en "Sales return"), `inv.salesDelivery` (ar "إذن التسليم", en "Delivery note"),
  `inv.customerReturn` (ar "مرتجع العميل", en "Customer return"), `inv.reservations` (ar "الحجوزات", en "Reservations"),
  `inv.reservationDifferences` (ar "فروق الحجز", en "Reservation differences").
- `src/test/inventory-rules.test.ts`: one case each that a posted `SalesDelivery` and `SalesReturn` offer no action.
Nothing else in Inventory changes.

## Permissions and warehouse scope
`GET /api/identity/me` gives `permissions`, `isCompanyWide` and `warehouseIds`. Quotations, orders, deliveries, invoices
and returns belong to their warehouse: a scoped user sees and acts only on their warehouses (403
`Sales.Access.WarehouseDenied`). Customers and the price list are company-wide master data.

**Approvals:** anyone with `sales.documents.approve` and the warehouse in scope approves, **including the user who
created or submitted the document**. Never hide or disable Approve because the current user created the document.

| Action | Needs |
|---|---|
| See customers, their exposure, check duplicates | `sales.customers.read` |
| Create, edit, activate, deactivate customers | `sales.customers.manage` |
| Change payment terms, credit limit, credit hold | `sales.credit.manage` |
| Maintain the price list; type a **manual price** on a quotation or order line | `sales.prices.manage` |
| See quotations, orders, deliveries, invoices, returns, the price list and settings | `sales.documents.read` (+ warehouse in scope for documents) |
| Draft, edit, submit, decline, cancel, convert quotations | `sales.quotations.manage` + warehouse in scope |
| Draft, edit, submit, cancel, reduce, close sales orders | `sales.orders.manage` + warehouse in scope |
| Draft picking lists and post deliveries | `sales.deliveries.manage` + warehouse in scope |
| Draft, post, cancel invoices | `sales.invoices.manage` + warehouse in scope |
| Draft, submit, cancel customer returns | `sales.returns.manage` + warehouse in scope |
| Approve or reject quotations, orders and returns | `sales.documents.approve` + warehouse in scope (creator included) |
| Change the sales settings | `sales.settings.manage` + **company-wide** |

Pickers read other modules: warehouses need `organization.warehouses.read`, products and units
`catalog.products.read`, locations `organization.warehouses.read`, availability `inventory.stock.read`. Without them
show the picker disabled with a hint.

## Shared rules
- **Row versions.** Every detail response has `rowVersion`; send the last one read with every change or action
  (`{ rowVersion }`, or `{ reason, rowVersion }` for reject, decline, cancel and close). 409 `Common.ConcurrencyConflict`
  → reload and keep what the user typed. Updates and actions answer `200 { rowVersion }`; creates `201 { id }`.
- **Idempotency-Key header** on `POST /deliveries/{id}/post`, `POST /invoices/{id}/post` and
  `POST /returns/{id}/approve` (400 without it). Generate `crypto.randomUUID()` when the user confirms. If the request
  fails without an answer (network, timeout, 5xx), retry with the **same key and the same body**: the server answers
  with the first result and posts nothing twice. A new user action gets a new key. 409 `Sales.Idempotency.KeyReused` =
  the key was used for another request; 409 `Sales.Idempotency.Busy` = the first attempt is still running, retry
  shortly. Show `RetryNotice` while a retry is pending, as Purchasing does.
- **Stock errors pass through from Inventory** with Inventory's codes (`Inventory.Stock.Insufficient`,
  `Inventory.Stock.InsufficientAtLocation`, `Inventory.StockCount.ScopeFrozen`, …): reuse the existing translations.
- **Prices.** When a line's product unit is chosen, show its list price from `GET /api/sales/prices?productUnitId=`.
  Send `unitPrice: null` to take the list price (the server snapshots it). Only users with `sales.prices.manage` see an
  editable unit price field (to type a manual price); others see the list price read-only, and a unit without a list
  price shows "No price" and can't be added (the server answers 422 `Sales.Line.PriceMissing`, 403
  `Sales.Line.ManualPriceNotAllowed`). Each saved line shows `priceSource` (`PriceList`/`Manual`) and, for manual
  prices, `listUnitPrice` beside it.
- **Tax.** Each line has `taxTreatment` (`Standard`, `ZeroRated`, `Exempt`) and `taxRate`. Standard: the rate defaults
  to the settings' `defaultVatRate` (send `taxRate: null` to use it) and may be edited (above 0). Zero-rated and exempt:
  rate 0, field disabled (send `null`). Prices are before tax.
- **Quantities** up to 4 decimals, whole for a unit that can't be split, always in the line's unit (`unitCode`).
  Discounts are percentages 0–100 (4 decimals). Amounts come back rounded to the currency (`currencyCode`, the company
  currency): 2 decimals (3 for KWD/BHD/OMR/JOD/TND/LYD/IQD).
- Show amounts the server computed (`grossAmount`, `discountAmount`, `netAmount`, `taxAmount`, `totalAmount`); in an
  edit dialog you may preview line amounts — gross = qty × price, discount = gross × d %, net = gross − discount,
  tax = net × t %, each rounded half away from zero — but the saved values are the server's.
- Codes, SKUs, numbers, phones, emails and registration numbers are ASCII: show them `dir="ltr"`. Times are UTC: show
  local time. `quotationDate`, `validUntil`, `orderDate`, `requestedDeliveryDate`, `invoiceDate`, `dueDate` are dates
  (`YYYY-MM-DD`).
- Status badges: `Draft` grey, `Submitted` amber, `Issued`/`Confirmed`/`Approved` blue, `PartiallyDelivered` blue,
  `Delivered`/`Posted`/`Converted` green, `Closed` slate, `Declined`/`Cancelled` muted; an issued quotation with
  `isExpired: true` shows an extra red "Expired" badge.
- Drafts show `draftReference` (`DQT-000001`, `DSO-…`, `DDN-…`, `DINV-…`, `DSR-…`) until they have a `number`
  (`QT-CAI-2026-000001`, `SO-…`, `DN-…`, `INV-…`, `SR-…`).
- Every list pages (`page`, `pageSize` ≤ 100) and returns `{ items, page, pageSize, totalCount, totalPages }`. Keep
  filters and paging the way the Purchasing lists do and open the detail page on row click.
- For line errors the server's `detail` names the line ("Line 2: …"); show the translation, then `detail` smaller.

## Arabic / English, RTL / LTR
- Arabic is the default (RTL), English the alternative (LTR), exactly as the rest of the app: every label, button,
  status, action, enum value and error goes in `src/shared/i18n/translations.ts` with both `ar` and `en`, keys
  prefixed `sal.` (errors stay `err.<errorCode>`). No hard-coded text.
- Customer and product names show `nameAr` (with `nameEn` where the API has it, in English mode).
- Amounts and quantities use the app's number formatting for the active language; dates as the app formats them.

## Status history and audit
Every detail page of a quotation, order, delivery, invoice and return shows:
- a "who and when" block: `createdBy` + `createdAtUtc`, `submittedBy`, `approvedBy` + `approvedAtUtc` (quotations,
  orders, returns), `issuedAtUtc` (quotations), `confirmedAtUtc` (orders), `postedBy` + `postedAtUtc` (deliveries,
  invoices). The same person may appear as creator and approver: that is allowed and must be shown as it is;
- the timeline from `events` (`action`, `userName`, `atUtc`, `note`) with `DocHistory` (`prefix="sal."`). Reasons,
  approval reasons and the numbers of related documents (on `Converted`, `Delivered`, `Invoiced`, `Returned`, `Reduced`)
  are in `note`.

## 1. Customers — `/sales/customers`
List: code, Arabic name, type, country, tax number, mobile, payment terms, credit limit, "on hold" badge, active badge.
Filters: search (code, names, tax number, phone/mobile), type, active, on credit hold, country. Row actions: view, edit,
activate/deactivate (SweetAlert).

Create/edit dialog (code and type only on create, fixed afterwards): code (`^[A-Za-z0-9_-]+$`, ≤ 20, stored upper
case), type (`Individual`/`Business`), Arabic name, English name, country (from `/api/organization/countries`, default
`EG`), tax registration number, commercial registration number + registry office (business customers only; office
required with the number), phone, mobile, email, notes; a contacts table (name, job title, phone, email, primary — at
most one) and an addresses table (type Billing/Shipping/Other, line 1, line 2, country → governorate → city with
`GeographyPickers`, only `cityId` is sent; postal code; primary — at most one). Saving sends the whole lists; they replace
the old ones. Payment terms and credit are **not** in this dialog (section below).

**Duplicate warning:** when the Arabic or English name or the mobile loses focus, call
`GET /api/sales/customers/similar?nameAr=&nameEn=&mobile=&exceptCustomerId=` and, if it returns customers, show a
non-blocking warning listing them (code, names, mobile, tax number, active). The user may still save. The server checks
registrations: 409 `Sales.Customer.TaxNumberTaken` / `CommercialRegistrationTaken`, 422 `EgyptianTaxNumberInvalid` (9
digits for `EG`; dashes and spaces are ignored).

Detail page: all fields, contacts, addresses, created/updated time, and two panels:
- **Credit** — payment terms (days), credit limit (or "none"), credit hold (with reason, who and when:
  `creditChangedBy`, `creditChangedAtUtc`). Button "Change credit" (`sales.credit.manage`) opens a dialog:
  `PUT /api/sales/customers/{id}/credit` `{ paymentTermDays, creditLimit, isOnCreditHold, creditHoldReason, rowVersion }`
  (reason required while on hold).
- **Exposure** — `GET /api/sales/customers/{id}/exposure`: open orders, uninvoiced confirmed orders, of which
  delivered, invoiced, pending credits, beside the credit limit. Always show the banner from `isPartial: true`:
  "Partial and informational: customer payments are not tracked yet, so this is not the customer's balance and nothing
  is blocked by the credit limit."

## 2. Price list — `/sales/prices`
List (`GET /api/sales/prices?page&pageSize&productId&productUnitId`): SKU, product, unit, conversion factor, unit price,
updated at. Filter: product picker. With `sales.prices.manage`: "Set price" (dialog: product → unit, unit price > 0, up
to 8 decimals; `PUT /api/sales/prices/{productUnitId}` `{ unitPrice }` → 204) and a row action "Remove price"
(SweetAlert; `DELETE /api/sales/prices/{productUnitId}` → 204). Explain under the title: "One price per product unit,
before tax, in the company currency. Changing it never changes existing quotations or orders."

## 3. Quotations — `/sales/quotations`
List: reference/number, status (+ expired), customer, warehouse, date, valid until, total, created by. Filters:
warehouse, customer, status, search.

Create/edit dialog: customer (active only; `GET /api/sales/customers?isActive=true`), warehouse (selling warehouses in
scope; fixed after create), quotation date (default today), valid until (default from the settings), customer reference,
payment terms (defaults to the customer's), notes, lines: product, unit, quantity, unit price (see *Prices*), discount %,
tax treatment, tax %. Show a running line total and the document totals.

Actions by status:
- Draft → Edit, **Submit** (issues it at once unless the settings require approval: it then comes back `Submitted` with
  `approvalReasons`), Cancel.
- Submitted → Approve (issues and numbers it), Reject (reason; back to draft), Cancel.
- Issued → **Convert to order** (not when expired; opens the new draft order), Decline (reason: the customer said no),
  Cancel.
- Converted (link to `convertedOrderId`), Declined (show `declinedReason`), Cancelled → none.
A quotation reserves nothing: say so under the lines.

## 4. Sales orders — `/sales/orders`
List: reference/number, status, customer, warehouse, total, invoiced, order date, requested delivery, created by,
confirmed at. Filters: warehouse, customer, status, search (number, draft reference, customer reference).

Create/edit dialog: as the quotation, plus requested delivery date and shipping address (the customer's addresses;
`shippingAddressId`). Show, per line, the warehouse's available quantity
(`GET /api/inventory/availability?warehouseId=&productId=`, `onHandSellable − reservedQuantity`) as a hint only — the
server decides.

Detail page: header (customer, warehouse, payment terms, shipping address, source quotation link, totals, invoiced and
credited amounts, `approvalReasons` while submitted, `closedReason`); lines with ordered, delivered, closed, remaining
(still reserved), invoiced, invoiceable, returned; a **Documents** panel from `documents` (deliveries, invoices, returns
with reference, status, amount, links); history.

Actions by status:
- Draft → Edit, **Submit** ("Confirm": reserves the stock and numbers the order, unless approval is needed — then it
  comes back `Submitted` with its `approvalReasons`, each translated), Cancel. 422 `Inventory.Stock.Insufficient` → "Not
  enough unreserved stock for line N"; the order stays a draft. 422 `Sales.Customer.OnCreditHold` → the customer is on
  hold.
- Submitted → Approve (confirms and reserves; creator included), Reject (reason; back to draft), Cancel.
- Confirmed → "New delivery", **Reduce line** (row action: quantity ≤ remaining, reason;
  `POST /orders/{id}/lines/{lineId}/reduce` `{ quantity, reason, rowVersion }`; releases that much reservation; reducing
  everything is refused with `CancelInstead`), Cancel (releases everything).
- PartiallyDelivered → "New delivery", Reduce line, **Close** (reason: the rest won't be delivered; releases it),
  "New invoice", "New return".
- Delivered / Closed → "New invoice", "New return". Cancelled → none.

## 5. Deliveries — `/sales/deliveries`
List: reference/number, status, order, customer, warehouse, created, posted. Filters: warehouse, order, customer,
status, search.

**Picking list dialog**, opened from an order: one block per order line with remaining > 0; inside it one or more rows
of location (the warehouse's locations; "Default" sends `null`) and quantity, with the location's stock as a hint
(`GET /api/inventory/balances?warehouseId=&productId=&locationId=`). The rows of a line may not exceed its remaining
quantity; the same line and location may appear only once. Send only rows with a quantity:
`{ orderId, notes, lines: [{ orderLineId, quantity, locationId }] }`.

Actions: Draft → Edit, **Post** (SweetAlert, Idempotency-Key; the goods leave stock from the order's reservation),
Cancel. The answer is `{ deliveryId, number, stockDocumentId, stockDocumentNumber, costAmount, postedAtUtc }`: toast
"DN-… posted (stock SDL-…)". 422 `Inventory.Stock.InsufficientAtLocation` → not enough in that location (pick another);
`Inventory.Stock.Insufficient` → the warehouse doesn't have it any more; `Sales.Order.OverDelivery` /
`Sales.Order.NotOpen` → reload the order; `Sales.Customer.OnCreditHold` → deliveries to a held customer are blocked.

Detail: lines (product, unit, quantity, location, cost after posting, returned), the stock document link
(`/inventory/documents/{stockDocumentId}`), its returns; action "New return" when something is returnable.

## 6. Sales invoices — `/sales/invoices`
List: reference/number, status, order, customer, invoice date, due date, total. Filters: warehouse, order, customer,
status, search.

Create/edit dialog, opened from an order: invoice date (default today; not after today), customer reference, notes, one
row per order line with `invoiceableQuantity` > 0 — quantity (max invoiceable). Prices, discounts and taxes come from
the order and are shown read-only. Actions: Draft → Edit, **Post** (Idempotency-Key; numbers it, sets the due date =
invoice date + the order's payment terms), Cancel; Posted, Cancelled → none (posted invoices never change). 422
`Sales.Order.InvoiceExceedsDeliveries` → reload the order (someone invoiced meanwhile).

Detail: customer with tax number, invoice and due dates, lines and totals; a print-friendly layout (browser print) is
enough — no e-invoice submission yet.

## 7. Customer returns — `/sales/returns`
List: reference/number, status, delivery, order, customer, warehouse, credit total, created, approved. Filters:
warehouse, order, delivery, customer, status, search.

Create/edit dialog, opened from a posted delivery: reason (required), notes, one row per delivery line with something
returnable (`quantity − returnedQuantity`): quantity, condition (only **Fit for resale** is selectable; show Damaged and
Uninspected disabled with the hint "Not supported yet: needs quarantine stock"), location (a location of the
delivery's warehouse; "Default" sends `null` for its default location).

Actions: Draft → Edit, Submit, Cancel; Submitted → **Approve** (Idempotency-Key; the goods come back into stock at the
cost they left with), Reject (reason; back to draft), Cancel. Every return needs approval; its creator may approve it.

Detail after approval: per line `uninvoicedQuantity` (returned before invoicing: won't be invoiced) and
`invoicedQuantity` with `creditNetAmount` / `creditTaxAmount`; header `creditTotalAmount` labelled "Pending customer
credit (for Accounting)" — it is not a credit note and nothing is refunded. Link the stock document (`SRT-…`).

## 8. Sales settings — `/sales/settings`
`GET /api/sales/settings` shown as a form; editable only by company-wide users with `sales.settings.manage`
(`PUT /api/sales/settings` with every field + `rowVersion`); others see it read-only:
- quotations require approval (switch), orders require approval (switch);
- discount approval threshold (optional %, empty = none; a line discount *above* it needs approval);
- manual prices require approval; manual prices below the list require approval (switches);
- credit hold policy: `BlockConfirmation` (orders of a held customer can't be confirmed) or `RequireApproval`;
- credit hold blocks deliveries (switch);
- default VAT % (above 0, default 14); quotation validity days (1–365).
Explain: "Approvers may approve their own documents. Changes apply when a document is next submitted."

## Labels to translate

| Permission | Arabic | English |
|---|---|---|
| `sales.customers.read` | عرض العملاء | View customers |
| `sales.customers.manage` | إدارة العملاء | Manage customers |
| `sales.credit.manage` | إدارة ائتمان العملاء | Manage customer credit |
| `sales.prices.manage` | إدارة أسعار البيع | Manage selling prices |
| `sales.documents.read` | عرض مستندات المبيعات | View sales documents |
| `sales.quotations.manage` | إدارة عروض الأسعار | Manage quotations |
| `sales.orders.manage` | إدارة أوامر البيع | Manage sales orders |
| `sales.deliveries.manage` | تسليم البضاعة | Deliver goods |
| `sales.invoices.manage` | إدارة فواتير المبيعات | Manage sales invoices |
| `sales.returns.manage` | إدارة مرتجعات العملاء | Manage customer returns |
| `sales.documents.approve` | اعتماد مستندات المبيعات | Approve sales documents |
| `sales.settings.manage` | إعدادات المبيعات | Manage sales settings |

| Value | Arabic | English |
|---|---|---|
| `Quotation` / `Order` / `Delivery` / `Invoice` / `Return` (linked documents) | عرض سعر / أمر بيع / إذن تسليم / فاتورة مبيعات / مرتجع عميل | Quotation / Sales order / Delivery note / Sales invoice / Customer return |
| `Individual` / `Business` | فرد / شركة | Individual / Business |
| `Draft` / `Submitted` / `Issued` / `Converted` / `Declined` | مسودة / بانتظار الاعتماد / مُصدر / تحول لأمر بيع / رفضه العميل | Draft / Submitted / Issued / Converted / Declined |
| `Confirmed` / `PartiallyDelivered` / `Delivered` | مؤكد / مسلّم جزئيًا / مسلّم بالكامل | Confirmed / Partially delivered / Delivered |
| `Approved` / `Posted` / `Closed` / `Cancelled` | معتمد / مُرحَّل / مغلق / ملغي | Approved / Posted / Closed / Cancelled |
| `PriceList` / `Manual` | من قائمة الأسعار / سعر يدوي | Price list / Manual |
| `Standard` / `ZeroRated` / `Exempt` | خاضع للضريبة / خاضع بنسبة صفر / معفى | Standard rated / Zero rated / Exempt |
| `FitForResale` / `Damaged` / `Uninspected` | صالح للبيع / تالف / لم يُفحص | Fit for resale / Damaged / Not inspected |
| `BlockConfirmation` / `RequireApproval` | منع التأكيد / يحتاج اعتماد | Block confirmation / Require approval |
| `Policy` / `DiscountAboveThreshold` / `PriceBelowList` / `ManualPrice` / `CreditHold` (approval reasons) | سياسة الاعتماد / خصم أعلى من الحد / سعر أقل من القائمة / سعر يدوي / العميل موقوف ائتمانيًا | Approval policy / Discount above threshold / Price below list / Manual price / Customer on credit hold |
| `SalesDelivery` / `SalesReturn` (stock documents) | صرف مبيعات / مرتجع مبيعات | Sales delivery / Sales return |
| Actions `Created` … `Reduced` | أُنشئ، أُرسل للاعتماد، رُفض، اعتُمد، أُلغي، أُغلق، رُحّل، أُصدر، رفضه العميل، تحول لأمر بيع، تأكد، سُلّم، فُوتر، أُرجع، خُفّضت الكمية | Created, Submitted, Rejected, Approved, Cancelled, Closed, Posted, Issued, Declined, Converted, Confirmed, Delivered, Invoiced, Returned, Reduced |

## Error codes to translate

| errorCode | Status | Arabic | English |
|---|---|---|---|
| `Sales.Access.WarehouseDenied` | 403 | ليس لديك صلاحية على مستندات هذا المخزن | You can't access this warehouse's sales documents |
| `Sales.Access.CompanyWideRequired` | 403 | تغيير إعدادات المبيعات يحتاج صلاحية على كل الشركة | Changing sales settings needs company-wide access |
| `Sales.Idempotency.KeyReused` | 409 | تم استخدام مفتاح العملية لطلب آخر | This operation key was used for another request |
| `Sales.Idempotency.Busy` | 409 | العملية نفسها قيد التنفيذ، حاول بعد لحظات | The same request is still running; try again shortly |
| `Sales.Reference.CompanySetupRequired` | 422 | قم بإعداد الشركة قبل المبيعات | Set up the company first |
| `Sales.Reference.WarehouseNotFound` / `WarehouseNotActive` | 422 | المخزن غير موجود أو غير نشط | The warehouse doesn't exist or is inactive |
| `Sales.Reference.WarehouseNotSelling` | 422 | هذا المخزن لا يبيع | This warehouse doesn't sell |
| `Sales.Reference.ProductNotFound` / `ProductNotActive` | 422 | الصنف غير موجود أو غير نشط | The product doesn't exist or is inactive |
| `Sales.Reference.ProductNotStockItem` | 422 | المبيعات تقبل الأصناف المخزنية فقط | Only stock items can be sold |
| `Sales.Reference.TrackingNotSupported` | 422 | الأصناف المتتبعة بالتشغيلة أو السيريال غير مدعومة بعد | Batch- and serial-tracked products aren't supported yet |
| `Sales.Reference.ProductUnitNotFound` / `ProductUnitNotActive` | 422 | الوحدة ليست من وحدات الصنف أو غير نشطة | The unit isn't one of the product's active units |
| `Sales.Reference.LocationNotFound` / `LocationNotActive` | 422 | الموقع ليس في مخزن المستند أو غير نشط | The location isn't an active location of the document's warehouse |
| `Sales.Line.QuantityInvalid` | 422 | الكمية يجب أن تكون موجبة وبحد أقصى 4 أرقام عشرية | Quantity must be positive, at most 4 decimals |
| `Sales.Line.QuantityMustBeWhole` | 422 | هذه الوحدة لا تتجزأ، أدخل عددًا صحيحًا | This unit can't be split; enter a whole number |
| `Sales.Line.ConversionNotExact` | 422 | الكمية لا تتحول بدقة إلى الوحدة الأساسية | The quantity doesn't convert exactly to the base unit |
| `Sales.Line.PriceMissing` | 422 | لا يوجد سعر بيع لهذه الوحدة | This unit has no selling price |
| `Sales.Line.ManualPriceNotAllowed` | 403 | السعر اليدوي يحتاج صلاحية إدارة الأسعار | A manual price needs the price permission |
| `Sales.Line.UnitPriceInvalid` | 422 | سعر الوحدة غير صحيح | The unit price is invalid |
| `Sales.Line.DiscountInvalid` | 422 | الخصم بين 0 و100% | Discount must be 0–100 % |
| `Sales.Line.TaxTreatmentNotSupported` | 422 | المعاملة الضريبية غير معروفة | Unknown tax treatment |
| `Sales.Line.TaxRateInvalid` | 422 | نسبة الضريبة لا تناسب المعاملة الضريبية | The tax rate doesn't fit the tax treatment |
| `Sales.Line.TaxRateNotAllowed` | 422 | البند المعفى أو الصفري لا يحمل نسبة ضريبة | Zero-rated and exempt lines carry no tax rate |
| `Sales.Line.AmountTooLarge` | 422 | قيمة السطر كبيرة جدًا | The line amount is too large |
| `Sales.Customer.NotFound` | 404 | العميل غير موجود | The customer does not exist |
| `Sales.Customer.CodeTaken` | 409 | كود العميل مستخدم | This customer code is taken |
| `Sales.Customer.TaxNumberTaken` | 409 | رقم التسجيل الضريبي مسجل لعميل آخر | Another customer has this tax number |
| `Sales.Customer.CommercialRegistrationTaken` | 409 | السجل التجاري مسجل لعميل آخر في نفس المكتب | Another customer has this commercial registration |
| `Sales.Customer.NotActive` | 422 | العميل غير نشط ولا يقبل مستندات جديدة | The customer is inactive and takes no new documents |
| `Sales.Customer.OnCreditHold` | 422 | العميل موقوف ائتمانيًا | The customer is on credit hold |
| `Sales.Customer.TypeInvalid` | 422 | نوع العميل غير صحيح | The customer type is invalid |
| `Sales.Customer.CountryInvalid` / `CountryNotFound` | 422 | الدولة غير صحيحة | The country is invalid |
| `Sales.Customer.CityNotFound` | 422 | المدينة غير موجودة | The city does not exist |
| `Sales.Customer.AddressNotFound` | 422 | العنوان ليس من عناوين العميل | The address isn't one of the customer's |
| `Sales.Customer.TaxNumberInvalid` | 422 | رقم التسجيل الضريبي غير صحيح | The tax number is invalid |
| `Sales.Customer.EgyptianTaxNumberInvalid` | 422 | رقم التسجيل الضريبي المصري 9 أرقام | An Egyptian tax number has 9 digits |
| `Sales.Customer.CommercialRegistrationInvalid` | 422 | رقم السجل التجاري غير صحيح | The commercial registration number is invalid |
| `Sales.Customer.CommercialRegistrationForBusinessOnly` | 422 | السجل التجاري للشركات فقط | Only business customers have a commercial registration |
| `Sales.Customer.CommercialRegistryRequired` | 422 | حدد مكتب السجل التجاري | Name the commercial registry office |
| `Sales.Customer.PaymentTermsInvalid` | 422 | مدة السداد من 0 إلى 365 يومًا | Payment terms are 0–365 days |
| `Sales.Customer.CreditLimitInvalid` | 422 | حد الائتمان لا يكون سالبًا | The credit limit can't be negative |
| `Sales.Customer.CreditHoldReasonRequired` | 422 | اذكر سبب الإيقاف الائتماني | Give the credit hold's reason |
| `Sales.Customer.TooManyContacts` / `TooManyAddresses` | 422 | عدد جهات الاتصال أو العناوين أكثر من المسموح | Too many contacts or addresses |
| `Sales.Customer.SeveralPrimaryContacts` / `SeveralPrimaryAddresses` | 422 | يسمح بجهة اتصال أو عنوان رئيسي واحد فقط | Only one primary contact or address |
| `Sales.Price.NotFound` | 404 | لا يوجد سعر لهذه الوحدة | This unit has no price |
| `Sales.Price.AlreadyPriced` | 409 | تم تسعير الوحدة في نفس اللحظة، أعد المحاولة | The unit was priced meanwhile; try again |
| `Sales.Price.UnitPriceInvalid` | 422 | السعر يجب أن يكون موجبًا وبحد أقصى 8 أرقام عشرية | The price must be positive, at most 8 decimals |
| `Sales.Price.ProductUnitNotFound` / `ProductUnitNotActive` | 422 | وحدة الصنف غير موجودة أو غير نشطة | The product unit doesn't exist or is inactive |
| `Sales.Settings.DiscountThresholdInvalid` | 422 | حد الخصم من 0 إلى أقل من 100% | The discount threshold is 0 to under 100 % |
| `Sales.Settings.CreditHoldPolicyInvalid` | 422 | سياسة الإيقاف الائتماني غير صحيحة | Invalid credit hold policy |
| `Sales.Settings.DefaultVatRateInvalid` | 422 | نسبة الضريبة الافتراضية أكبر من 0 وحتى 100% | The default VAT is above 0 and up to 100 % |
| `Sales.Settings.QuotationValidityInvalid` | 422 | صلاحية العرض من 1 إلى 365 يومًا | Quotation validity is 1–365 days |
| `Sales.Quotation.NotFound` | 404 | عرض السعر غير موجود | The quotation does not exist |
| `Sales.Quotation.NotDraft` / `NotSubmitted` / `NotIssued` / `NotCancellable` | 422 | حالة عرض السعر لا تسمح بهذا الإجراء | The quotation's status doesn't allow this |
| `Sales.Quotation.Expired` | 422 | انتهت صلاحية عرض السعر | The quotation has expired |
| `Sales.Quotation.ValidityInvalid` | 422 | تاريخ الصلاحية قبل تاريخ العرض | Valid-until is before the quotation date |
| `Sales.Quotation.PaymentTermsInvalid` | 422 | مدة السداد من 0 إلى 365 يومًا | Payment terms are 0–365 days |
| `Sales.Quotation.NoLines` / `TooManyLines` | 422 | عدد السطور من 1 إلى 200 | 1 to 200 lines |
| `Sales.Order.NotFound` | 404 | أمر البيع غير موجود | The sales order does not exist |
| `Sales.Order.NotDraft` / `NotSubmitted` / `NotConfirmed` / `NotOpen` | 422 | حالة أمر البيع لا تسمح بهذا الإجراء | The order's status doesn't allow this |
| `Sales.Order.NotCancellable` | 422 | لا يمكن إلغاء أمر تم التسليم منه، أغلقه بدلًا من ذلك | An order with deliveries can't be cancelled; close it |
| `Sales.Order.NothingDeliveredToClose` | 422 | لم يُسلّم شيء، ألغِ الأمر بدلًا من إغلاقه | Nothing was delivered; cancel instead |
| `Sales.Order.NotClosable` | 422 | يُغلق الأمر المسلّم جزئيًا فقط | Only a partially delivered order can be closed |
| `Sales.Order.CancelInstead` | 422 | تخفيض كل الكمية إلغاء: ألغِ الأمر | Reducing everything is cancelling: cancel the order |
| `Sales.Order.ReductionInvalid` | 422 | التخفيض أكبر من المتبقي | More than what remains on the line |
| `Sales.Order.OverDelivery` | 422 | الكمية أكبر من المتبقي تسليمه | More than what remains to be delivered |
| `Sales.Order.InvoiceExceedsDeliveries` | 422 | الكمية أكبر من المسلّم غير المفوتر | More than what was delivered and not invoiced |
| `Sales.Order.ReturnExceedsDeliveries` | 422 | الكمية أكبر من المسلّم غير المرتجع | More than what was delivered and not returned |
| `Sales.Order.RequestedDateInvalid` | 422 | تاريخ التسليم المطلوب قبل تاريخ الأمر | The requested delivery date is before the order date |
| `Sales.Order.PaymentTermsInvalid` | 422 | مدة السداد من 0 إلى 365 يومًا | Payment terms are 0–365 days |
| `Sales.Order.NoLines` / `TooManyLines` / `DuplicateLine` / `LineNotFound` | 422 | سطور الأمر غير صحيحة | The order's lines are invalid |
| `Sales.Delivery.NotFound` | 404 | إذن التسليم غير موجود | The delivery does not exist |
| `Sales.Delivery.NotDraft` / `NotPosted` | 422 | حالة إذن التسليم لا تسمح بهذا الإجراء | The delivery's status doesn't allow this |
| `Sales.Delivery.DuplicatePick` | 422 | نفس السطر من نفس الموقع مكرر | The same line from the same location appears twice |
| `Sales.Delivery.ReturnExceedsDelivery` | 422 | الكمية أكبر من المتبقي في إذن التسليم | More than the delivery line can still return |
| `Sales.Delivery.NoLines` / `TooManyLines` / `LineNotFound` | 422 | سطور إذن التسليم غير صحيحة | The delivery's lines are invalid |
| `Sales.Invoice.NotFound` | 404 | الفاتورة غير موجودة | The invoice does not exist |
| `Sales.Invoice.NotDraft` | 422 | الفاتورة المرحلة لا تتغير | A posted invoice can't change |
| `Sales.Invoice.DateInFuture` | 422 | تاريخ الفاتورة بعد اليوم | The invoice date is after today |
| `Sales.Invoice.NoLines` / `TooManyLines` | 422 | سطور الفاتورة غير صحيحة | The invoice's lines are invalid |
| `Sales.Return.NotFound` | 404 | المرتجع غير موجود | The return does not exist |
| `Sales.Return.NotDraft` / `NotSubmitted` / `NotCancellable` | 422 | حالة المرتجع لا تسمح بهذا الإجراء | The return's status doesn't allow this |
| `Sales.Return.ConditionNotSupported` | 422 | يُقبل المرتجع الصالح للبيع فقط حاليًا | Only goods fit for resale can be returned for now |
| `Sales.Return.NoLines` / `TooManyLines` | 422 | سطور المرتجع غير صحيحة | The return's lines are invalid |
| `Inventory.StockDocument.ManagedBySales` | 422 | هذا المستند يُدار من المبيعات | This stock document is managed by Sales |
| `Inventory.Stock.InsufficientAtLocation` | 422 | الكمية غير متوفرة في هذا الموقع | Not enough stock in this location |
| `Inventory.Reservation.WarehouseNotSelling` | 422 | هذا المخزن لا يبيع | This warehouse doesn't sell |

## Backend facts (don't fake them)
- Nothing is deleted: drafts are cancelled, orders cancelled or closed, posted deliveries, invoices and returns never
  change. A delivery is corrected by a customer return.
- Confirming (or approving) an order reserves **all** its lines or none; reserved stock can't be issued, transferred or
  sold to anyone else. Cancelling, reducing and closing release exactly that order's reservation.
- Posting a delivery, posting an invoice and approving a return each run in one transaction with their stock or
  amounts; a refusal leaves nothing behind (no document number, no stock movement).
- Goods leave at the moving average cost and come back at the cost they left with. Invoices bill prices, discounts and
  taxes saved on the order; later price list changes never touch them.
- Credit limit: informational only (`isPartial`); the manual credit hold is what blocks, per the settings.
- E-invoicing (Egyptian ETA) is not implemented yet; don't show a "submit to tax authority" action.
