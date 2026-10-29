# Lovable prompt — WarehouseERP POS screens

Copy everything below the line into Lovable, in the same project as the Organization, Identity, Catalog, Inventory,
Purchasing and Sales screens. It is built from the backend's real OpenAPI contract (`docs/api/openapi-v1.json`) and an
end-to-end run against the real API (60/60 checks); don't invent endpoints, fields or rules. Example payloads and the
manual test steps are in [`FRONTEND_HANDOFF.md`](FRONTEND_HANDOFF.md).

---

Add the **POS (point of sale)** module to the existing WarehouseERP app. It has two parts:
- a **till screen** for cashiers: pick a terminal, open a shift with a float, scan or search items, build a cart, take
  cash / card / split payments, print the receipt, take goods back and refund them, cash in / cash out, close the shift
  with a cash count;
- **back-office pages**: terminals, shifts (X/Z reports, force-close), sales history with receipt copies, returns, card
  incidents and the POS settings.

Plus two small changes elsewhere (section 0): Inventory must recognise the POS stock document types, and Sales gets a
**Product taxes** page, because a product can't be sold at a till until it is classified for VAT.

It is part of the existing app: same App Shell, routing, sign-in, cookie session, XSRF handling, RTK Query `baseApi` and
design system. Don't add another app, another auth provider, Supabase, mock data, local price or VAT calculation, or any
business rule the server enforces (prices, VAT, discount limits, stock, payment arithmetic, refund limits): show what the
API returns and the errors it sends. The server prices everything; the till only displays.

## Build on what exists (don't rebuild it)
Follow `AGENTS.md` and its **UI conventions**:
- Create and edit forms open in `FormDialog` (`src/shared/ui/FormDialog.tsx`); no `/new` pages. Detail pages are
  read-only (`Panel` + `DetailGrid` + `ReadOnlyValue`). Edit dialogs load with `refetchOnMountOrArgChange: true` and fill
  from `useLatestRecord(q)` so the form and its `rowVersion` come from the same fetch.
- State-changing actions ask first through `useConfirm()` / `useStatusConfirm()` (`src/shared/ui/confirm.ts`); actions
  needing input (cash in/out, close shift, resolve incident, force-close) use a `FormDialog`.
- Lists use `ServerTable`; row actions are icon buttons from `src/shared/ui/RowActions.tsx`. Full-width pages, logical
  classes only (`ms`/`me`/`ps`/`pe`/`start`/`end`); Radix `DirectionProvider` handles RTL.
- Reuse `Money`, `Qty`, `DateValue` from `src/shared/ui/DocValues.tsx` and `DocHistory` from `src/shared/ui/DocHistory.tsx`.
- RTK Query endpoints are injected into `src/shared/api/baseApi.ts` with tag types `PosTerminal`, `PosShift`, `PosSale`,
  `PosReturn`, `PosCardIncident`, `PosSettings`, `ProductTax`. A checkout or a return also invalidates `PosShift` and
  Inventory's `Stock`, `StockDocument`, `Valuation`, `Reconciliation` tags.
- Permission names go in `P` (`src/shared/session/permissions.ts`), checked with `can()`; settings use `canCompanyWide()`.
  Every text goes in `src/shared/i18n/translations.ts` (keys `pos.`), errors under `err.<errorCode>`.
- Code in `src/features/pos/`; pages under `src/routes/_authenticated/pos/`.
- Sidebar: one collapsible **POS** group in `navGroups` (`src/shared/shell/AppShell.tsx`) after Sales: Till (`pos.sell`),
  Sales history, Returns, Shifts, Terminals, Card incidents (each `pos.read`), POS settings (`pos.read`). Under the Sales
  group add **Product taxes** (`sales.documents.read`).
- In the Identity role editor, add the permission labels from the table at the end.

## 0. Compatibility (do this first)
**Inventory.** The backend posts two more stock document types, created only by POS:
- `src/features/inventory/types.ts`: add `"PosSale"` and `"PosReturn"` to `documentTypes`.
- `src/features/inventory/rules.ts`: add `posOwned = (type) => type === "PosSale" || type === "PosReturn"` and treat it like
  `salesOwned` (no edit, post, cancel or reverse; the API answers `Inventory.StockDocument.ManagedByPos`); `DraftDialog`
  never offers them.
- `src/features/inventory/RecordPages.tsx`, beside the Sales links: when `r.sourceDocumentId` is set, link `PosSale` to
  `/pos/sales/{sourceDocumentId}` (label `inv.posSale`) and `PosReturn` to `/pos/returns/{sourceDocumentId}` (label
  `inv.posReturn`), showing `externalReference` (the receipt or return number).
- Translations: `inv.PosSale` (ar "بيع نقطة بيع", en "POS sale"), `inv.PosReturn` (ar "مرتجع نقطة بيع", en "POS return"),
  `inv.posSale` (ar "إيصال البيع", en "Sales receipt"), `inv.posReturn` (ar "إيصال المرتجع", en "Return receipt").
- `src/test/inventory-rules.test.ts`: a posted `PosSale` and `PosReturn` offer no action.

**Sales — Product taxes page** (`/sales/product-taxes`, `src/features/sales/ProductTaxesPage.tsx`, next to `PricesPage`):
- `GET /api/sales/product-taxes?page&pageSize&productId&taxTreatment` → paged `{ id, productId, sku, productName,
  taxTreatment, taxRate, createdAtUtc, updatedAtUtc }` (`taxRate` null = the default VAT of the sales settings).
- "Classify product" / row edit (`sales.prices.manage`): a `FormDialog` with a product picker (the existing Catalog product
  search), `taxTreatment` = `Standard` | `ZeroRated` | `Exempt`, and, only for `Standard`, an optional own rate
  (empty = default VAT, show the default from `GET /api/sales/settings` `defaultVatRate` as the hint) →
  `PUT /api/sales/product-taxes/{productId}` `{ taxTreatment, taxRate }` → 204. Zero-rated and exempt send `taxRate: null`.
- Row "remove" (`sales.prices.manage`, confirm) → `DELETE /api/sales/product-taxes/{productId}` → 204. Explain in the
  confirmation that the till then refuses the product.
- Labels: `sal.taxStandard` "خاضع للضريبة" / "Standard-rated", `sal.taxZeroRated` "خاضع بنسبة صفر" / "Zero-rated",
  `sal.taxExempt` "معفى" / "Exempt", `sal.productTaxes` "التصنيف الضريبي للأصناف" / "Product taxes".

## 1. The till (`/pos/till`, `pos.sell`) — one full-screen page, fast, keyboard-first
Layout (RTL-aware): a header with terminal, shift number, cashier and buttons (X report, cash in/out, returns, close
shift); the cart table in the middle; on the side the scan input, totals and the big **Pay** button.

**Terminal.** On first visit, pick a terminal from `GET /api/pos/terminals?isActive=true&pageSize=100` (the cashier's
warehouses only) and remember its id in `localStorage` (`pos.terminalId`), with a "change terminal" action. Show its
`code`, `name`, `warehouseCode`; cash/card buttons follow `acceptsCash` / `acceptsCard`.

**Shift.** `GET /api/pos/terminals/{id}/current-shift` → the caller's open shift there, or 404 `Pos.Shift.NotFound`. With
none, show only an **Open shift** `FormDialog`: `openingFloat` (≥ 0) → `POST /api/pos/shifts` `{ terminalId,
openingFloat }` → 201 `{ id }`. 409 `Pos.Shift.TerminalBusy` (someone else's shift is open on this till: show the cashier's
name from the terminal's `openShiftCashierName`) and 409 `Pos.Shift.CashierBusy` (you have a shift on another till).

**Scanning.** A scan input with permanent focus: Enter → `GET /api/pos/terminals/{id}/items/lookup?barcode=…` →
`PosItemResponse` `{ productId, sku, productName, productUnitId, unitCode, unitName, conversionFactor, unitPrice,
unitPriceWithTax, taxTreatment, taxRate, availableQuantity, canSell, problemCode }`. 404 `Pos.Item.NotFound` → beep-style
toast. If `canSell` is false show `err.<problemCode>` and don't add it. Otherwise add 1 (or +1 to the same product unit's
line). **Search** (F2 or a button): `GET /api/pos/terminals/{id}/items?search=…&take=20` → every active unit of each
matching product; pick one row to add.

**Cart** (client state only, in the till page; nothing is reserved): rows show SKU, name, unit (a select of that
product's units from the search endpoint when the cashier wants another unit), quantity (editable; whole numbers when
`unitAllowsFraction` is false), unit price with VAT, discount %, line total. The discount field shows only with
`can(P.posDiscountsApply)` and is capped in the input at the settings' `maxLineDiscountPercent` (`GET /api/pos/settings`);
the server checks again. Customer: default walk-in with optional name and phone; or "Registered customer" →
`GET /api/pos/customers?search=…` → `{ id, code, nameAr, nameEn, customerType, taxRegistrationNumber, mobile }`.

**Totals** come from the server: after each cart change (debounced ~300 ms) `POST /api/pos/sales/quote` `{ terminalId,
lines: [{ productId, productUnitId, quantity, discountPercent }] }` → `{ currencyCode, currencyDecimals,
maxLineDiscountPercent, lines[{ …, unitPriceWithTax, netAmount, taxAmount, totalAmount, availableBaseQuantity, stockShort
}], grossAmount, discountAmount, netAmount, taxAmount, totalAmount, stockShort }`. Show a warning on lines with
`stockShort` (a hint only). A 422 from the quote (for example `Sales.Line.PriceMissing`, `Sales.ProductTax.NotClassified`,
`Pos.Sale.DiscountAboveMax`) is shown on the cart and disables Pay.

**Pay** (F9) opens a `FormDialog` with the total due:
- Cash tendered (one field), and any number of card lines: amount + **authorization/reference number (required)** +
  optional **last four digits** (exactly 4 digits; never ask for the full number or CVV).
- Live display: card total, cash needed (total − cards), change (cash tendered − cash needed). Disable Confirm while
  cards exceed the total or the payments don't cover it.
- **Card flow:** call the quote again just before the cashier charges the card terminal; only if it succeeds, tell the
  cashier to charge the card terminal, then enter the reference and confirm.
- **Idempotency:** create one `crypto.randomUUID()` key when Confirm is first pressed for this cart and keep it until a
  definitive answer; send it as the `Idempotency-Key` header. On a timeout, network error or 5xx, **retry with the same
  key** (offer a "Retry" button; never a new key for the same cart) — the server returns the first receipt if it was
  recorded. On 409 `Pos.Idempotency.Busy`, wait a second and retry with the same key.
- `POST /api/pos/sales` `{ terminalId, customerId, customerName, customerPhone, notes, lines, payments: [{ method:
  "Cash"|"Card", amount, reference, cardLast4 }] }` → 200 `PosSaleResponse` (the receipt). For cash, `amount` is what the
  customer handed over.
- 422 **`Pos.Checkout.CardReversalRequired`**: the sale was NOT recorded but the card was charged. Show a blocking red
  dialog with the server's `detail` (reason + references) telling the cashier to reverse the charge on the card terminal
  and that a card incident was recorded; keep the cart so the cashier can fix it and retry **with the same key**.
- Other 422s (stock, discount, shift) → show `err.<errorCode>`, keep the cart and the key only for a pure retry; if the
  cashier changes the cart, make a new key.
- On 200: print the receipt (section 2), clear the cart and the key, refresh the shift.

**Cash in / out** (`pos.cash.manage`): fetch the shift first (`GET /api/pos/shifts/{id}`), then `FormDialog` type
(`CashIn` | `CashOut`), amount, reason (required) → `POST /api/pos/shifts/{id}/cash-movements` `{ movementType, amount,
reason, rowVersion }` → `{ rowVersion }`. 409 `Common.ConcurrencyConflict` (a sale changed the shift) → reload and retry.

**X report** button: `GET /api/pos/shifts/{id}` → `PosShiftResponse` (section 4), printable.

**Close shift**: fetch the shift, then `FormDialog` with counted cash and a note. Show the expected drawer and the
difference live; the note is required when the difference isn't 0 → `POST /api/pos/shifts/{id}/close` `{ countedCash,
note, rowVersion }` → 200 the closed shift (Z report): print it, then go back to "Open shift".

## 2. Receipt (80 mm, browser print)
A `Receipt` component used by the till, the sales detail page and reprints, printed with `window.print()` and a print
stylesheet (80 mm width, monospace numbers, RTL for Arabic). From `PosSaleResponse`:
- `receiptHeader`, `companyName`, `branchName`, `terminalCode`, `number`, `soldAtUtc` shown in `timeZoneId`, cashier
  (`cashierName`), `shiftNumber`, customer (`customerName`, `customerPhone`, `customerTaxNumber`).
- Lines: name, unit, quantity × `unitPriceWithTax`, discount % when > 0, line `totalAmount`.
- Totals: `grossAmount`, `discountAmount`, `netAmount`, VAT summary from `taxBreakdown` (treatment, rate, net, VAT),
  `taxAmount`, `totalAmount`.
- Payments: each `payments[]` line (method, amount, card reference / ****last4), cash tendered and change.
- Returns made against it (`returns[]`: number, date, amount).
- Footer: `receiptFooter`, and always **"إيصال بيع — ليس فاتورة ضريبية" / "Sales receipt — not a tax invoice"**
  (`isTaxInvoice` is false).
- When `isCopy` is true print a large **"نسخة / COPY"** mark and `copyNumber`. Only print from the checkout or return
  response (original) or from a reprint response (copy); never print from a plain GET.

## 3. Returns at the till (`pos.returns.manage`)
"Return" button → find the sale: `GET /api/pos/sales?search=<receipt number>` (or scan the receipt number) →
`GET /api/pos/sales/{id}`. Show its lines with `returnableQuantity`; the cashier enters quantities to take back and a
condition per line: only **FitForResale** ("صالح لإعادة البيع") is enabled — show Damaged / Uninspected disabled with the
note that they're not accepted yet. Reason is required.
Refunds: the amount due is computed by the server; compute a preview as the selected quantity's share of each line's
total and send refunds that **equal** it: by default back the way the sale was paid (cash up to `cashReceivedAmount` of
the sale, card up to `cardAmount`, minus what earlier returns refunded); a card refund needs the card terminal's
reference. With a new `Idempotency-Key`: `POST /api/pos/returns` `{ terminalId, saleId, reason, notes, lines: [{
saleLineId, quantity, condition }], refunds: [{ method, amount, reference, cardLast4 }] }` → 200 `PosReturnResponse`; print
it (same receipt layout, title "إيصال مرتجع / Return receipt", with `saleNumber`). On `Pos.Refund.Mismatch` the server's
detail says the exact amount: show it and let the cashier adjust. Other errors: `Pos.Return.ExceedsSale`,
`Pos.Return.WindowClosed`, `Pos.Return.OtherWarehouse`, `Pos.Refund.CashExceedsPaid`, `Pos.Refund.CardExceedsPaid`,
`Pos.Shift.DrawerShort`.

## 4. Back-office pages (`pos.read` unless noted; every list is limited to the user's warehouses by the server)
- **Terminals** (`/pos/terminals`): `GET /api/pos/terminals?page&pageSize&warehouseId&isActive&search` → `{ id, code,
  name, branchCode, warehouseCode, sellingLocationCode, acceptsCash, acceptsCard, isActive, openShiftNumber,
  openShiftCashierName, rowVersion }`. "New terminal" (`pos.terminals.manage`): code (fixed after creation), name,
  warehouse (selling warehouses of the user's scope), selling location (that warehouse's locations, default = DEFAULT),
  accepts cash / card, receipt header and footer → `POST /api/pos/terminals` → 201. Edit (code and warehouse read-only) →
  `PUT /api/pos/terminals/{id}` `{ name, sellingLocationId, acceptsCash, acceptsCard, receiptHeader, receiptFooter,
  rowVersion }`. Activate/deactivate → `POST /api/pos/terminals/{id}/activate|deactivate` → 204
  (`Pos.Terminal.HasOpenShift`).
- **Shifts** (`/pos/shifts`): `GET /api/pos/shifts?page&pageSize&terminalId&warehouseId&cashierUserId&status` → summary
  rows; detail `/pos/shifts/$id` → `PosShiftResponse`: opening float, cash sales (net), cash refunds, cash in, cash out,
  **expected cash**, counted cash, **difference** (red when negative), card sales and card refunds (labelled "outside the
  drawer"), sales/refund totals and counts, open card incidents, cash movements table, history (`events`). "Close shift"
  for an open shift: the cashier closes their own; others need `pos.shifts.manage` (forced close, note required) — same
  close call as the till. Print X/Z report.
- **Sales history** (`/pos/sales`): `GET /api/pos/sales?page&pageSize&terminalId&shiftId&warehouseId&customerId&
  cashierUserId&search&from&to` (dates `yyyy-MM-dd`, company calendar) → `{ number, terminalCode, warehouseCode,
  cashierName, customerName, soldAtUtc, lineCount, totalAmount, cashReceivedAmount, cardAmount, refundedAmount,
  returnCount }`. Detail `/pos/sales/$id` shows the receipt on screen; "Print copy" (`pos.receipts.reprint`, confirm) →
  `POST /api/pos/sales/{id}/reprint` → copy response → print. Link to the stock document
  (`/inventory/documents/{stockDocumentId}`, Inventory permission) and to its returns.
- **Returns** (`/pos/returns`): `GET /api/pos/returns?page&pageSize&terminalId&shiftId&saleId&warehouseId&search&from&to`;
  detail `/pos/returns/$id`, "Print copy" → `POST /api/pos/returns/{id}/reprint`.
- **Card incidents** (`/pos/card-incidents`): `GET /api/pos/card-incidents?page&pageSize&terminalId&shiftId&status` →
  `{ terminalCode, warehouseCode, cashierName, cardAmount, references, errorCode, errorMessage, status (ReversalRequired |
  SaleRecorded | Resolved), attempts, occurredAtUtc, saleId, resolvedByName, resolutionNote, rowVersion }`. Default filter
  `ReversalRequired`, highlighted. "Resolve" (`pos.shifts.manage`): `FormDialog` note (required, e.g. "reversed on the
  card terminal, ref …") → `POST /api/pos/card-incidents/{id}/resolve` `{ note, rowVersion }`.
- **POS settings** (`/pos/settings`): `GET /api/pos/settings` → `{ maxLineDiscountPercent, returnWindowDays, rowVersion }`;
  edit with `canCompanyWide(P.posSettingsManage)` → `PUT /api/pos/settings` `{ maxLineDiscountPercent (0–100),
  returnWindowDays (0–365), rowVersion }`.

## 5. Permissions (labels for the role editor; `P` keys)
| Permission | ar | en |
|---|---|---|
| `pos.read` | عرض نقاط البيع والورديات والمبيعات | View POS terminals, shifts and sales |
| `pos.sell` | البيع على نقطة البيع | Sell at a till |
| `pos.discounts.apply` | منح خصم على الأصناف | Give line discounts |
| `pos.cash.manage` | إيداع وسحب نقدية الدرج | Cash in / cash out |
| `pos.returns.manage` | المرتجعات واسترداد المبالغ | Returns and refunds |
| `pos.receipts.reprint` | طباعة نسخة من الإيصال | Print receipt copies |
| `pos.shifts.manage` | إدارة الورديات وحوادث البطاقات | Manage shifts and card incidents |
| `pos.terminals.manage` | إدارة نقاط البيع | Manage terminals |
| `pos.settings.manage` | إعدادات نقاط البيع | POS settings |

## 6. Error codes (translate each under `err.<code>`, Arabic and English)
`Pos.Access.WarehouseDenied`, `Pos.Access.NotShiftCashier`, `Pos.Access.PermissionDenied`, `Pos.Access.CompanyWideRequired`,
`Pos.Terminal.NotFound`, `Pos.Terminal.CodeTaken`, `Pos.Terminal.CodeInvalid`, `Pos.Terminal.NameInvalid`,
`Pos.Terminal.NoPaymentMethod`, `Pos.Terminal.ReceiptTextTooLong`, `Pos.Terminal.NotActive`, `Pos.Terminal.HasOpenShift`,
`Pos.Terminal.WarehouseNotFound`, `Pos.Terminal.WarehouseNotActive`, `Pos.Terminal.WarehouseNotSelling`,
`Pos.Terminal.LocationNotFound`, `Pos.Terminal.LocationNotActive`, `Pos.Shift.NotFound`, `Pos.Shift.NoOpenShift`,
`Pos.Shift.TerminalBusy`, `Pos.Shift.CashierBusy`, `Pos.Shift.NotOpen`, `Pos.Shift.AmountInvalid`,
`Pos.Shift.ReasonRequired`, `Pos.Shift.DrawerShort`, `Pos.Shift.DifferenceNoteRequired`, `Pos.Shift.ForceCloseNoteRequired`,
`Pos.Shift.NoteTooLong`, `Pos.Shift.CashMovementTypeInvalid`, `Pos.Item.NotFound`, `Pos.Sale.NotFound`, `Pos.Sale.NoLines`,
`Pos.Sale.TooManyLines`, `Pos.Sale.ProductNotFound`, `Pos.Sale.ProductNotStockItem`, `Pos.Sale.TrackingNotSupported`,
`Pos.Sale.ProductNotActive`, `Pos.Sale.ProductUnitNotFound`, `Pos.Sale.ProductUnitNotActive`, `Pos.Sale.QuantityInvalid`,
`Pos.Sale.QuantityMustBeWhole`, `Pos.Sale.ConversionNotExact`, `Pos.Sale.DiscountNotAllowed`, `Pos.Sale.DiscountAboveMax`,
`Pos.Sale.CustomerNotFound`, `Pos.Sale.CustomerNotActive`, `Pos.Sale.CustomerTooLong`, `Pos.Sale.NotesTooLong`,
`Pos.Sale.TotalTooLarge`, `Pos.Payment.Missing`, `Pos.Payment.TooMany`, `Pos.Payment.OneCashOnly`,
`Pos.Payment.MethodInvalid`, `Pos.Payment.MethodNotAccepted`, `Pos.Payment.AmountInvalid`,
`Pos.Payment.CardReferenceRequired`, `Pos.Payment.CardLast4Invalid`, `Pos.Payment.CardExceedsTotal`,
`Pos.Payment.Underpaid`, `Pos.Payment.CashNotNeeded`, `Pos.Checkout.CardReversalRequired`, `Pos.Idempotency.KeyReused`,
`Pos.Idempotency.Busy`, `Pos.Return.NotFound`, `Pos.Return.SaleNotFound`, `Pos.Return.NoLines`, `Pos.Return.TooManyLines`,
`Pos.Return.ReasonRequired`, `Pos.Return.SaleLineNotFound`, `Pos.Return.QuantityInvalid`, `Pos.Return.ExceedsSale`,
`Pos.Return.ConditionNotSupported`, `Pos.Return.OtherWarehouse`, `Pos.Return.WindowClosed`, `Pos.Refund.Mismatch`,
`Pos.Refund.CashExceedsPaid`, `Pos.Refund.CardExceedsPaid`, `Pos.CardIncident.NotFound`, `Pos.CardIncident.NotOpen`,
`Pos.CardIncident.NoteRequired`, `Pos.Settings.MaxDiscountInvalid`, `Pos.Settings.ReturnWindowInvalid`,
`Pos.Reference.CompanySetupRequired`, `Sales.ProductTax.NotClassified`, `Sales.ProductTax.NotFound`,
`Sales.ProductTax.ProductNotFound`, `Sales.ProductTax.ProductNotActive`, `Sales.ProductTax.AlreadyClassified`,
`Sales.ProductTax.RateInvalid`, `Sales.ProductTax.TreatmentInvalid`, `Inventory.StockDocument.ManagedByPos`,
`Inventory.StockDocument.WarehouseNotSelling`, `Inventory.StockDocument.SoldLineNotFound`,
`Inventory.StockDocument.ReturnExceedsSale`. Inventory's existing `Inventory.Stock.Insufficient`,
`Inventory.Stock.InsufficientAtLocation` and `Inventory.StockCount.ScopeFrozen` (a stock count is running on the selling
location) also reach the till.

## 7. Tests (Vitest)
- Pay dialog math: card total, cash needed, change; Confirm disabled when cards exceed the total or payments are short.
- The idempotency key survives a network error and a 5xx and is reused on retry; a changed cart makes a new key.
- `CardReversalRequired` keeps the cart and shows the blocking dialog.
- Receipt shows "COPY" only when `isCopy`, the not-a-tax-invoice line always, and the VAT summary rows.
- Inventory rules: `PosSale`/`PosReturn` offer no action.
