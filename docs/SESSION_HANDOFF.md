# Session handoff — WarehouseERP

Last verified: 2026-10-30 (Accounting Milestones A, B and C complete on `feature/accounting`; POS V1 merged into `main`
in both repositories earlier the same day).
Read this first; don't re-analyse the repository.

## Where things stand
- **Accounting started (2026-10-30) on `feature/accounting`**, cut from backend `main` at `302f57a` (= `origin/main`).
  Decisions approved by the owner the same day: `docs/modules/Accounting/V1_DECISIONS.md`; how the code implements them:
  `docs/adr/0020-accounting-module.md`. **Milestones A (general ledger), B (receivables, payables, cash and banks) and C
  (ERP integration, cutover, reconciliations) are implemented and verified; D (acceptance, Lovable prompt and frontend
  handoff) remains.** Not merged, not pushed; `Migration14_Accounting`, `Migration15_AccountingSubledgers` and
  `Migration16_AccountingIntegration` are not applied to `WarehouseERP_Dev`. Details in *Accounting status* below.
- **`main` holds POS V1 (2026-10-30).** With the owner's approval after the acceptance on the dev database, the
  backend `main` was fast-forwarded from `ea122c1` to the tip of `feature/pos` (this handoff) and the frontend's `main`
  from `e9da46f` (Lovable's POS screens) to `fd44db5` (`fix/pos-integration`). No merge commits; both branches stay on
  the remotes for reference. Migrations 11–13 are applied to `WarehouseERP_Dev`. Details in *POS status* below.
- **`main` holds Sales V1 (2026-10-29).** With the owner's approval after the acceptance on the dev database, `main`
  was fast-forwarded from `60bb17c` to `0eb88af`, the tip of `feature/sales` (cut from `main` at `60bb17c`; it stays on
  the remote for reference). The frontend's `main` was fast-forwarded from `c958638` to `526a7e6` (`fix/sales-api-contract`).
  No merge commits. Commits:
  * `e3e3756` Inventory: stock reservations (`IStockReservations`), sales stock posting (`ISalesStockPosting`, types
    `SalesDelivery` = 11 / `SalesReturn` = 12), `GET /api/inventory/reservations`, reservation reconciliation,
    `Migration09_InventoryReservations`;
  * `203f484` the Sales module; `12831be` `Migration10_Sales`; `cb42d44` tests and the OpenAPI contract;
  * the docs commit: `docs/modules/Sales/*`, ADR 0018, README, ARCHITECTURE and this handoff.
- **`main` holds Identity, Organization, Catalog, Inventory and Purchasing V1 (2026-10-29).** On 2026-10-29, with the
  owner's approval after the final acceptance, it was fast-forwarded from `c791868` to the tip of `feature/purchasing`
  (Purchasing, the address lookups, tests, docs and this handoff). On 2026-10-28 it was fast-forwarded from `1a1aba1` to the tip of `feature/inventory` (Inventory code, tests, docs and this handoff).
  Earlier on 2026-10-28 it went from `383d5be` to the tip of `feature/catalog` (Catalog code `adf4a7d`, docs
  `bb536a0`), and on 2026-10-27 from `67a7473` to `4b1048d` (`feature/organization`, which includes
  `feature/identity-completion`). There are no merge commits.
- **Remote (2026-10-28):** `origin` = `https://github.com/MahmoudBakr7474/ERP_API.git` (the owner's GitHub, used to work
  from other machines and from Claude in the cloud). `main` and `feature/inventory` are pushed with upstream tracking;
  never force-push, never push `launchSettings.json`.
- **`feature/purchasing`** (cut from `main` at `c791868`) is merged into `main` (fast-forward, 2026-10-29) and stays on
  the remote for reference. **The owner accepted the Purchasing backend for this milestone on 2026-10-29**:
  * `16a15ce` global self-approval rule (`CLAUDE.md`), stale "another user approves" comments fixed, README approval
    rows corrected, `InventorySelfApprovalTests` (no Inventory behaviour change: it already allowed it since `29cbdb3`);
  * `84e8c19` Inventory: `IPurchaseStockPosting`, stock document types `PurchaseReceipt`/`PurchaseReturn`,
    `SourceDocumentId`;
  * `661e0dd` the Purchasing module; `bb7fa42` `Migration07_Purchasing`; `aff0486` tests and the OpenAPI contract;
  * the docs commit with ADR 0016, `docs/modules/Purchasing/*` and this handoff;
  * address lookups (owner request 2026-10-29, ADR 0017): `6f81a99` Organization countries/governorates/cities,
    `3e8721c` supplier addresses name a city, `48cdff5` `Migration08_Geography`, `dc31696` tests and OpenAPI, then docs.
  * acceptance: `ec91836` supplier timestamps read back as UTC, then this handoff.
- `feature/inventory` (cut from `main` at `1a1aba1`) is merged and stays on the remote for reference:
  * Milestone A: `5dfdf1a` (code), `574217c` (handoff).
  * Milestone B: `cc75d3d` (code, Migration05, tests), `171f9b1` (ADR 0015), `4c3170d` (transfer destinations lookup,
    document links, Migration06), `65031c5` (Lovable prompt and frontend handoff), `b0fc930` (handoff).
  * Acceptance: `29cbdb3` (approvers may approve their own work), `d862b15` (timestamps read back as UTC), `9e8ce50`
    (reconciliation resets its connection to READ COMMITTED), `a88a0fe` (history test flake), `3b90f85` (transfer
    scope test), `c29a6a9` (production prerequisites in ADR 0015) and this handoff.
  * Start each new module from `main` on its own `feature/...` branch. Merging into `main` needs the owner's
    approval.
- Organization's approved baseline: `fde5e28` (code, tests, ADR 0013, frontend handoff).
- Identity's approved baseline: `5498eb5` (code and tests), `816fb20` (docs), `8d4a12b` (role management needs
  company-wide scope).
- `src/WarehouseERP.Api/Properties/launchSettings.json` is the owner's untracked file: never commit or edit it.

## Accounting status
- **Owner decisions (2026-10-30, with corrections):** cutover 2027-01-01 configurable and explicitly approved before
  activation, no replay of history; editable default Egyptian chart or an imported one, explicit posting-role mappings;
  post-dated cheques deferred; manual withholding/collection amounts; revenue at posted delivery with unbilled
  receivables (invoice reclassifies, VAT at its tax point); price differences split between stock held and sold or kept
  as visible exceptions; POS per transaction, shift close settles cash only. All in `V1_DECISIONS.md` (the checklist it
  replaces is gone).
- **Milestone A (code on `feature/accounting`):** `src/Modules/Accounting` (`accounting` schema, `Migration14_Accounting`,
  11 tables, 3 temporal, settings seeded): chart of accounts (tree, rules, default chart, import into an empty chart),
  28 posting roles with explicit mappings, settings, fiscal years with 12 periods (close/reopen with reason, period locks),
  manual and opening-balance journals (approval policy, self-approval, idempotent post/approve, numbering GJ/OB/RJ/CJ per
  type and fiscal year), reversals, year-end close per branch into retained earnings and its reopen, and reports (trial
  balance, account statement, general ledger, balance sheet, income statement by branch with comparison). 30 endpoints
  under `/api/accounting`, ten permissions in `AccountingPermissions.All`, company-wide access only.
- **Verified (2026-10-30):** build 0 warnings; Unit 267/267, Architecture 129/129, Integration 289/289 on LocalDB (14
  Accounting tests, one on its own database for the default chart and the year-end close); migrations 01–14 on a fresh
  database, 14 rolled back to 13 and re-applied, `has-pending-model-changes` none (`.artifacts/accounting-migration-check.ps1`
  and its log); OpenAPI snapshot regenerated (+30 paths, +53 schemas, +6 tags, additive). Integration tests and the
  migration check ran in the app's terminal (LocalDB caveat below).
- **Milestone B (code on `feature/accounting`, ADR 0020 decisions 14–20):** party lines on control accounts (customers
  through Sales' `ICustomerDirectory.FindManyAsync`, suppliers through Purchasing's new `ISupplierDirectory`), open items
  and allocations (planned by a voucher, by hand, automatic, removable; party locks), vouchers (customer receipt RV,
  supplier payment PV, refunds CF/SF, cash receipt CR and payment CP, transfer TR, expense bill EB with input VAT, the
  supplier's terms and a unique bill number) with manual withholding, cashboxes and banks with the cash guard (posting date
  and every later day), reversal of settled journals once their allocations are removed, manual bank reconciliation,
  aging, party statements, the subledger reconciliation, the voucher-approval setting (off), three permissions
  (`receivables.manage`, `payables.manage`, `treasury.manage`; 13 in all). `Migration15_AccountingSubledgers`: 6 tables
  (cash accounts temporal), nullable voucher/party columns, no data change. +18 endpoints.
- **Verified (2026-10-30, Milestone B):** build 0 warnings; Unit 273/273, Architecture 129/129, Integration 296/296 on
  LocalDB (7 new subledger API tests); migrations 01–15 on a fresh database, 15 rolled back to 14 and re-applied,
  `has-pending-model-changes` none (`.artifacts/accounting-b-migration-check.ps1` and its log); OpenAPI snapshot
  regenerated (+18 paths, +29 schemas, +4 tags; additive — `UpdateAccountingSettingsRequest` gains the required
  `vouchersRequireApproval`, an endpoint new on this unmerged branch).
- **Milestone C (code on `feature/accounting`, ADR 0020 decisions 21–29):** read-only accounting sources in Inventory,
  Purchasing, Sales and POS (`I*AccountingSource`; no operational behaviour changed); one accounting identity per
  document (`SourcePostings`, unique by type and id) and one system journal per document (IV, PU, SL, PS; never reversed
  by hand); `SourceSync` run by `SourcePostingWorker` (`Accounting:Integration`, every minute) and on demand, discovery
  with overlapping cursors, each document in its own transaction, failures retried with backoff and by hand; posting rules
  (`SourceEntries`): Inventory's own documents, GRN → GRNI, supplier invoice (price differences under review), supplier
  return (claim), delivery → revenue against unbilled receivables, invoice (reclassifies; VAT), customer return (credit),
  POS sale/return per transaction, shift close into the branch's POS cashbox (`receivesPosCash`) with cash over/short;
  GRNI and unbilled valued per order line so they clear to zero; cutover readiness (period, roles, opening journals,
  subledgers, inventory/GRNI/unbilled/till openings vs the modules, and the cutover's start passed) and authorized
  activation (the start kept, the openings frozen); the source-to-ledger reconciliation; Sales' exposure adds the
  receivable and the invoices not journaled yet (`ICustomerReceivables`). An independent review of the diff (four
  reviewers, every finding checked by a second) found 7 real defects before commit; all fixed (ADR 0020 *Consequences*).
  Note: a reviewer ran the integration tests from the sandbox and left LocalDB stopped; `sqllocaldb start MSSQLLocalDB`
  in the app terminal restored it once its sandboxed server exited. One permission
  (`accounting.integration.manage`; 14 in all). `Migration16_AccountingIntegration`: 2 tables, nullable/defaulted
  columns, no data change. +7 endpoints.
- **Verified (2026-10-30, Milestone C):** build 0 warnings; Unit 281/281, Architecture 129/129, Integration 297/297 on
  LocalDB (one new end-to-end test on its own database); migrations 01–16 on a fresh database, 16 rolled back to 15 and
  re-applied, `has-pending-model-changes` none (`.artifacts/accounting-c-migration-check.ps1` and its log); OpenAPI
  snapshot regenerated (+7 paths, +18 schemas, +2 tags; additive).
- **Accountant items left by C:** VAT at delivery where that is the tax point, the split of supplier price differences
  between stock held and sold (they stay under review; the accountant reclassifies by journal), card settlements recorded
  by treasury vouchers. All in `V1_DECISIONS.md` section 8.
- **Next:** Milestone D: real-host E2E, the dev-database migration (after a verified backup, with the owner's
  authorization), the Lovable prompt and frontend handoff.

## POS status
- **Decisions:** approved by the owner on 2026-10-29 ("POS V1 Final Owner Approval", with corrections: product-level tax
  classification, 10% configurable till discount limit, 14-day configurable return window, damaged returns refused, card
  charge outside the SQL transaction handled explicitly): `docs/modules/POS/V1_DECISIONS.md`. How the code implements
  them: `docs/adr/0019-pos-module.md`.
- **Backend complete and verified on `feature/pos`** (commits listed by `git log main..feature/pos`):
  * Inventory (additive): stock document types `PosSale` = 13 (`PSL-`) / `PosReturn` = 14 (`PSR-`), `IPosStockPosting`
    (unreserved issue under the warehouse lock, count freezes, location balance, moving average; returns at the sold
    cost), `IStockAvailabilityLookup`, `Inventory.StockDocument.ManagedByPos`; `Migration11_InventoryPos` (checks only).
  * Sales (additive): product tax classifications (`sales.ProductTaxes`, temporal, `/api/sales/product-taxes`,
    `sales.prices.manage`; `Migration12_SalesProductTaxes`), `ISalesPricing` (list price + classification + discount
    through the same `LineAmounts`), `ICustomerDirectory`. Sales documents' behaviour unchanged.
  * Catalog (additive): `IProductCatalog.FindUnitByBarcodeAsync`, `SearchActiveAsync`, `FindUnitsOfProductsAsync`.
  * POS module `src/Modules/Pos` (`pos` schema, `Migration13_Pos`, 14 tables): terminals, shifts (one open per terminal
    and per cashier, cash in/out, close with count and mandatory note on a difference, forced close by a shift manager),
    quote, checkout (cash/card/split, idempotent, one transaction with stock, payments, shift totals and receipt number),
    card incidents for refused checkouts with card payments, sales history, counted receipt copies, returns and refunds
    (cumulative shares, per-method caps, window, fit-for-resale only), settings (10% / 14 days seeded). 22 endpoints under
    `/api/pos`; nine permissions in `PosPermissions.All`; self-approval everywhere.
- **Verified (2026-10-29):** build 0 warnings; Unit 259/259, Architecture 114/114, Integration 275/275 on LocalDB
  (24 POS tests: last units across four tills, POS vs Sales reservation race, concurrent retries of one key, concurrent
  returns, rollback leaving no number/stock/payment, card incidents, count freeze, scope, permissions, Inventory
  protection); migrations 01–13 on a fresh database, 13–11 rolled back to 10 and re-applied, `has-pending-model-changes`
  none; OpenAPI snapshot regenerated (+24 paths, +50 schemas, additive). Real-host E2E on a throwaway
  `WarehouseERP_E2E_Pos_*` database (dropped): 60/60, no 500s, no passwords in the log. Scripts in `.artifacts/`
  (git-excluded): `e2e-pos-host.ps1` + `e2e-pos.mjs`, `pos-migration-check.ps1`, `drop-db.ps1` (now also accepts
  `E2E_Pos`). SQL Server tests and scripts were run in the app's terminal (outside the sandbox), as the LocalDB caveat
  below requires; the dev API stayed healthy.
- **Contract fix after the frontend review (`2665824`, additive, OpenAPI regenerated, 4 properties):** `PosSaleResponse` gained
  `refundedCashAmount` / `refundedCardAmount` (the till proposes refunds within each method's cap);
  `PosReturnResponse` gained `taxBreakdown` and `PosReturnLineResponse` `unitPriceWithTax` (a return receipt prints like a
  sale receipt). `FRONTEND_HANDOFF.md` samples updated.
- **Dev database (2026-10-29, owner-authorised):** copy-only checksummed backup
  `D:\work\WarehouseERP\db-backups\WarehouseERP_Dev_before_Migration11_20261029_181849.bak`, `RESTORE VERIFYONLY` valid,
  test restore matched all 124 tables' row counts (copy dropped); then `dotnet ef database update` applied exactly
  Migrations 11–13 (17 new tables, only `__EFMigrationsHistory` among existing tables changed). Logs and scripts:
  `.artifacts/dev-db-backup-pos.*`, `dev-db-migrate-pos.*`. The dev API runs the `feature/pos` build (app terminal);
  readiness 200, 401 unauthenticated, HTTPS, live OpenAPI equal to the committed one (except the `servers` block).
- **Acceptance data on the dev database (labelled, keep or deactivate):** products `POS-ACC-STD` (Standard 14%, PCS 10 /
  CTN×12 110, EAN `6221234500027` / `…034`), `POS-ACC-ZERO` (ZeroRated, 5, `…041`), `POS-ACC-EXM` (Exempt, 3.50,
  `…058`), `POS-ACC-NOTAX` (unclassified on purpose, 7); receipt `RCV-CAI-2026-000007` into STORE; terminals
  `POS-ACC-T1` (cash+card) and `POS-ACC-T2` (cash) in STORE; roles `POS-ACC *` and users `posacc.*` (cashier1/2, viewer,
  supervisor, back office scoped to STORE; outsider scoped to MAIN). Their generated passwords are only in
  `.artifacts/pos-acc-users.json` (git-excluded). All acceptance shifts are closed, the count and the sales order used
  for the checks are cancelled, and the one card incident is resolved.
- **Browser acceptance (2026-10-29, `http://localhost:8082`, admin session):** product taxes page; terminal registered
  through the UI (only selling warehouses offered); shift opened with a 500 float; barcode scans of piece and carton
  (same barcode twice merges), quantities, server-quoted VAT-inclusive prices for Standard/ZeroRated/Exempt; unclassified
  item refused; line discount capped at 10%; cash sale with change (200 → 44.44); split card 20 (simulated reference
  `SIM-ACC-0001`, last 4) + cash 10 with change 2.20, card amount above the due blocked, reference fields only after the
  pre-charge check; idempotent retry (first answer dropped in the page, Retry sent the same key, one sale, one receipt);
  receipt with VAT per rate and "not a tax invoice"; copy printed as "COPY #1"; partial return 1 tea + 1 sugar refunded
  16.40 cash (card refund on a cash sale blocked) with its own receipt; cash in 50 / out 100; X report; card charged
  then stock taken by another sale → "reverse the card" with incident, cart kept, incident resolved in the UI; close
  counted 600.00 vs 600.46 with a note; Z report; English/LTR shift page. Stock and moving-average valuation checked
  for the sale (`PSL-`) and the return at the sold cost (`PSR-`). Receipts were read from the print area (the browser
  print dialog was stubbed after the first one froze the hidden pane).
- **API-only acceptance on the dev database (multi-user, `.artifacts/acc-pos-dev.mjs`, 31/31):** MAIN-scoped cashier sees
  and opens nothing in STORE (403 `Pos.Access.WarehouseDenied`); a user with `pos.read` only cannot open a shift or check
  out; one shift per terminal; no selling on another cashier's shift; discount without `pos.discounts.apply` 403;
  unclassified 422; short quote flagged and 422 `Inventory.Stock.Insufficient`; a confirmed Sales order's reservation is
  never sold (only the 2 unreserved units sell; reservation unchanged; released on cancel); a started count freezes the
  till (`Inventory.StockCount.ScopeFrozen`, nothing recorded); two tills racing for the last 13 units: exactly one sells,
  nothing negative; copies need `pos.receipts.reprint`; a supervisor's forced close needs a note; no sale on a closed
  shift. Earlier modules' screens (Sales, Purchasing, Inventory lists) and the POS back-office pages render without
  errors.
- **Frontend (`fix/pos-integration`):** Lovable built the POS screens on the web `main` (`e9da46f`); the review against the
  prompt, handoff and OpenAPI found field-name mismatches, fixed in `cdc2cc8` (API names for shift/cash/event fields,
  return receipts, per-method refund caps, terminal code rule, translations, tests) and `fd44db5` (the pre-charge check
  stops on short stock; the Z report printed nowhere because closing unmounted the view owning the printer).
  `src/routeTree.gen.ts` shows a line-ending-only change: never commit it.
- **Known gaps (not defects, not fixed):** a till refund method above its cap disables Confirm without a message; error
  `detail` texts from the API are English (the screens show the translated `err.<code>` first); page titles are English
  in every module.
- **Merged (2026-10-30)** with the owner's approval, fast-forward in both repositories (see *Where things stand*).
  Accounting was started on the owner's request on 2026-10-30 (see *Accounting status*).
- **Production prerequisites (ADR 0019):** Egyptian ETA e-Receipt (registration, EGS/GS1 codes, device registration,
  integration) where required — V1 receipts are internal commercial receipts; reviewed tax classification of every
  product sold at tills; the accountant approves cost treatment and the handling of card incidents and cash
  differences; earlier modules' prerequisites still apply.
- **Backlog (not started):** offline POS, integrated payments, wallets/InstaPay, held carts, loyalty, promotions, barcode
  scales, quarantine returns, hardware drawer/printer, manual till prices, header discounts, cash rounding, credit sales,
  GL posting.

## Sales status
- **Decisions:** approved by the owner on 2026-10-29 ("Sales V1 Final Owner Approval & Implementation", with
  corrections): `docs/modules/Sales/V1_DECISIONS.md`. How the code implements them: `docs/adr/0018-sales-module.md`.
- **Backend complete, verified, accepted and merged into `main` (2026-10-29):**
  * customers (temporal, geography addresses, unique code and registrations, duplicate warnings, credit terms,
    informational limit, manual hold), one company-wide price list per product unit, configurable sales settings
    (seeded: nothing needs approval, VAT 14 %, hold blocks confirmation and deliveries);
  * quotations → orders confirmed with every line reserved all-or-nothing through Inventory, reduce/close/cancel
    releasing exactly their reservation, deliveries picked from several locations and posted with an
    `Idempotency-Key`, partial invoices bounded by deliveries, customer returns (fit for resale only, always approved,
    back at their delivery's cost, pending customer credit);
  * 40 endpoints under `/api/sales`; twelve permissions in `SalesPermissions.All`; self-approval everywhere.
- **Schema:** `Migration09_InventoryReservations` (two `inventory` tables; type and source checks widened to 12) and
  `Migration10_Sales` (`sales` schema, 22 tables, 4 temporal, settings seed). Verified on a fresh LocalDB database:
  all ten applied, rolled back to Migration08 and re-applied, `has-pending-model-changes` none. **Applied to
  `WarehouseERP_Dev` on 2026-10-29** with the owner's authorization, after a verified copy-only backup
  (`db-backups\WarehouseERP_Dev_before_Migration09_20261029_132609.bak`: `RESTORE VERIFYONLY` valid, test-restored and
  compared, 96 tables identical): only 09–10 applied, no existing row changed, 28 tables added. The dev API (terminal tab
  "ERP dev API", same `dotnet run` command) runs the `feature/sales` build; its live OpenAPI equals the committed one.
- **E2E (2026-10-29):** the real host over HTTPS with cookie sessions on throwaway `WarehouseERP_E2E_Sales_*`
  databases (all dropped): a scoped rep approving their own order and return, a clerk (403 on approve) and an approver
  of another warehouse (403); quotation → order (reserved) → delivery from two locations with an idempotent retry →
  invoice → return (1 uninvoiced + 1 invoiced, credit 10.26) → close; discount threshold, credit hold, price and tax
  refusals, Inventory's `ManagedBySales`, reservations, valuation and reconciliation. 49/49 checks; no 500s, no
  passwords in the log. Scripts in `.artifacts/` (git-excluded): `e2e-sales-host.ps1` + `e2e-sales.mjs`,
  `migration-check.ps1`, `drop-db.ps1`, `list-dbs.ps1`.
- **Fixed during verification:** `CK_SalesQuotations_Converted` compared two predicates (invalid T-SQL) and failed the
  migration on SQL Server; rewritten before the migration was committed. Four Sales contract names collided with
  Purchasing's OpenAPI schema names (`DocumentEventResponse`, `LinkedDocumentResponse`, `InvoiceLineRequest`,
  `ReturnLineRequest`) and now carry a `Sales` prefix.
- **Frontend:** Lovable built the screens (web `main` `c958638`). Contract fixes are on the web branch
  `fix/sales-api-contract` (`526a7e6`, merged into web `main` 2026-10-29): `customerType`, the two settings flags, exposure fields,
  invoice `customerTaxNumber`, the draft order's shipping address kept on edit, cancel without an ignored reason, id-only
  list filters, return credit totals.
- **Acceptance on `WarehouseERP_Dev` (2026-10-29), signed in as `admin` in the app's browser pane, through the Vite
  proxy.** Test records are labelled `SALES-ACC*` / `SALACC*` (product `SALES-ACC-01`, location `SALSHELF` in STORE,
  customer `SALACC-C1`, roles `SALES-ACC clerk`/`outsider`, users `salacc-clerk`/`salacc-outsider`, both deactivated).
  * Browser: customer with address created and edited (registration kept), price set, quotation issued and converted,
    order confirmed (reserved 4), delivery from DEFAULT + SALSHELF posted, reservations page (consumed 3, remaining 1),
    invoice posted (20.52), return submitted and approved by its creator (credit 10.26, back at cost 12.00), order
    closed (reservation released), another order reduced then cancelled, 50 PCS refused for stock (Arabic message),
    settings saved without losing flags, draft order edit keeps its address, English/LTR pages without raw keys.
  * API only (in the signed-in session): same `Idempotency-Key` replays one delivery (stock out once), key reused 409,
    no key 400; clerk 403 on approve/settings/prices/manual price; MAIN-scoped approver 403 `WarehouseDenied` and an
    empty list; Inventory issue and receipt, Purchasing order approved and received; valuation 13 × 6 = 78;
    reconciliation consistent for the whole database.
  * Frontend checks: `tsc` clean, Vitest 98/98, `vite build` succeeds; ESLint has no new errors in Sales code (the
    translations file's prettier formatting and CRLF noise are pre-existing).
  * Remaining cosmetic items (not blocking): the quotation/order submit button reads "send for approval" even when
    nothing needs approval; English mode shows customers' Arabic names in headers; the picking dialog's stock hint
    for "Default" lists all locations unlabelled.
- **Production prerequisites (ADR 0018):** Egyptian e-invoicing (ETA) before production use; the accountant approves
  the cost and credit treatment; Inventory's and Purchasing's prerequisites still apply.
- **Backlog (not started):** backorders, reservation expiry (POS), reserving at quotation time, quarantine returns of
  damaged/uninspected goods, customer price lists and promotions, header discounts, foreign currency, services and
  non-stock lines, consolidated invoices, credit notes, refunds, customer payments, receivables, hard credit-limit
  enforcement, GL posting, batch/serial tracking.

## Purchasing status
- **Decisions:** approved by the owner on 2026-10-28 with corrections: `docs/modules/Purchasing/V1_DECISIONS.md`. How
  the code implements them: `docs/adr/0016-purchasing-module.md`. Self-approval is allowed everywhere (owner's
  permanent rule, `CLAUDE.md` *Document approvals*); the corrections' maker-checker wording is superseded by it.
- **Backend complete, verified and accepted (2026-10-29); merged into `main`:**
  * suppliers (temporal, unique code and registrations per jurisdiction, similar-name warning), optional purchase
    requests that orders draw on, purchase orders (one warehouse, base currency, line discount and recoverable VAT,
    approval policy seeded required), goods receipts posted to stock through Inventory with an `Idempotency-Key`,
    supplier invoices matched to receipts with price-variance approval and no revaluation, approved supplier returns
    that issue at the moving average and record a pending credit claim;
  * 47 endpoints under `/api/purchasing`; ten new permissions in `PurchasingPermissions.All`;
  * Inventory changed additively: `IPurchaseStockPosting`, two stock document types, `SourceDocumentId`; purchase
    stock documents can't be drafted, edited or reversed in Inventory (`ManagedByPurchasing`); other generated types
    now answer `TypeNotDrafted` instead of a 500.
- **Schema:** `Migration07_Purchasing` (purchasing schema, 20 tables; on `inventory.StockDocuments` only the type check
  1–10 and `SourceDocumentId`). **Applied to `WarehouseERP_Dev` on 2026-10-29** after a verified copy-only backup
  (`D:\work\WarehouseERP\db-backups\WarehouseERP_Dev_before_Migration07_20261029.bak`); checked: 7 migrations, 20
  purchasing tables (3 temporal), both policies seeded, the 17 existing stock documents untouched,
  `has-pending-model-changes` none.
- **Dev API:** restarted on the Purchasing build on 2026-10-29 in the app's terminal tab "ERP dev API" (`dotnet run
  --project src/WarehouseERP.Api --no-launch-profile -e ASPNETCORE_ENVIRONMENT=Development -- --urls
  https://localhost:49571 --contentRoot src/WarehouseERP.Api`); `/health/ready` 200.
- **E2E (2026-10-29):** the real host with cookie sessions on a throwaway `WarehouseERP_E2E_Purch_*` database
  (dropped): a scoped buyer approving their own request, order, invoice and return; a clerk (403 on approve) and an
  approver of another warehouse (403); request → order → partial receipts with an idempotent retry → over-receipt
  refused → invoices (exact price posts, variance needs approval, duplicate number 409, over-invoicing 422) → return
  (2 uninvoiced + 2 invoiced, claim 41.55) → reconciliation consistent. 34/34 checks; no 500s, no passwords in the log.
- **Migration08_Geography applied to `WarehouseERP_Dev` (2026-10-29)** after a backup
  (`db-backups\WarehouseERP_Dev_before_Migration08_20261029.bak`): 249 countries, 27 governorates, 271 cities, both
  existing supplier addresses converted (and their history row), 20 stock documents untouched, still temporal. The dev
  API was restarted on that build. Note: from this session's sandboxed shell LocalDB may look stopped once it idled;
  run database commands in the app's terminal (outside the sandbox).
- **Frontend (web repo `main`, pushed 2026-10-29 at `edbfea3`, synced to Lovable):** Lovable built the Purchasing
  screens (`1971216`); on top of it, fast-forwarded with the owner's approval: `32736df` property names matched to the
  API (supplierName, productName, paymentTermDays, countryCode, addressType, linked `documentType`, `withCreditClaim`)
  and `edbfea3` the address lookups (cascading searchable pickers, `/organization/geography`). The local branch
  `fix/purchasing-api-contract` is merged into it. API acceptance through the Vite proxy on the dev DB: 31/31, request
  bodies 11/11, lookups 7/7; browser checks signed in as `admin` in the app's browser pane (the owner signs in; the
  agent never types credentials).
- **Final acceptance (2026-10-29), on the dev DB through the Vite proxy and the app's browser pane:**
  * Migration08 kept existing data: the pre-Migration08 backup restored as a temporary database (dropped after) and
    compared table by table: 90 tables, none with fewer rows; the only pre-migration ids not current are one address
    and one contact of `E2ESUP01`, replaced by a supplier edit and present in the history tables.
  * Contracts: frontend types and request bodies match the API for suppliers, addresses, contacts, countries,
    governorates and cities (no field read that the API doesn't send; bodies match the OpenAPI request schemas).
  * Browser (admin): supplier created with country → governorate → city, duplicate code shown on the field, edited
    (governorate change clears the city), reloaded and persisted; lookups page, deactivated city hidden from pickers.
  * Regression smoke 25/25: supplier with address, 403s (clerk create/approve, buyer lookups, no-read user), 400/422
    (missing or unknown city, country not listed), order self-approved, partial receipt with idempotent retry (+6,
    value +600.00), invoice posted (684.00, value unchanged), return approved (claim 228.00, out at the moving
    average), valuation = balances, reconciliation consistent.
  * One bug fixed: supplier `createdAtUtc`/`updatedAtUtc` came back without "Z" (shown 3 hours early); the supplier
    mapping now reads them as UTC like every Purchasing timestamp (no schema change), with a test.
  * Caveat: running `sqlcmd`/LocalDB from the agent's sandboxed shell can start or end the shared LocalDB instance and
    cut the dev API off (503); run database commands and the integration tests in the app's terminal.
- **Production prerequisite:** the accountant approves the provisional cost treatment (receipts at the order's net
  price, invoice variances kept without revaluation, returns at the moving average). ADR 0016 *Not done* lists the
  deferred backlog (foreign currency, revaluation/landed cost, credit notes, payments/AP/GL, header discounts,
  non-recoverable tax, receiving in other units, over-receipt tolerance, price lists, amount thresholds).

## Inventory status
- **Decisions:** the owner approved V1 on 2026-10-28 with corrections: `docs/modules/Inventory/V1_DECISIONS.md`. How
  Milestones A and B implement it: `docs/adr/0015-inventory-core.md` (decisions 1–15 and 16–24).
  `docs/INVENTORY_DECISIONS.md` still binds the design.
- **Accepted for V1 and merged into `main` (2026-10-28).** The owner approved the result of the acceptance gate:
  * Frontend: the Lovable screens are on the frontend's `main` at `4a3beec`, with the review fixes from
    mahmoudtaha311/screenshot-match#1 (Approve shown to the creator, receive column, missing translations, warehouse
    and transit pickers).
  * Owner's decision (2026-10-28): anyone with `inventory.documents.approve` and the warehouse in scope approves a
    document, transfer or count, their own included; the `SelfApproval` error codes are withdrawn (ADR 0015 decision 9,
    `V1_DECISIONS.md` section 6).
  * Fixed during the screen review: Inventory timestamps came back without "Z" and showed 3 hours early (`d862b15`);
    the reconciliation report left its pooled connection at SNAPSHOT, so a later session update failed with an update
    conflict and a 500 (`9e8ce50`).
  * Frontend integration on `WarehouseERP_Dev`, through the Vite proxy: core-workflow smoke 17/17 (receipt and issue
    with an idempotent retry, transfer with a partial receipt by the STORE storekeeper, count with a variance approved
    by the approver, balances, valuation and reconciliation, 401, XSRF and scoped 403s). The frontend's calls match
    `docs/api/openapi-v1.json` (88 checks: paths, methods, query parameters, bodies, `Idempotency-Key`), and all 84
    Inventory error codes have translations.
- **Backend complete and verified (Milestones A and B).**
  * Milestone A (`5dfdf1a`): stock documents (opening, receipt, issue, adjustment, reversal) with drafts and
    submit/approve/reject/cancel; approval policy per type; posting in one `ITransactionRunner` transaction with
    warehouse `sp_getapplock`, fixed lock order, no negative stock, numbering per type + branch + fiscal year,
    `Idempotency-Key`; balances, availability, quantity-only movements and the stock card; company-wide moving
    average; over-reservation alerts; Organization's deactivation guard.
  * Milestone B (`cc75d3d`, `4c3170d`):
    - transfers through a transit warehouse: dispatch into a transit location opened per transfer (`TR-000123`),
      partial and full receipts, closing what never arrived by an approved loss adjustment, the `TransferDispatch`
      approval policy (seeded: direct), a destinations lookup beyond the sender's scope;
    - stock counts: start freezes the scope through `StockCountScopes` (warehouse applock Exclusive, snapshot, nothing
      held while counting), postings refuse frozen stock, approval re-checks the snapshot and posts a `CountAdjustment`;
    - `GET /api/inventory/reconciliation`: availability, ledger, valuation and transit differences plus stock
      positions and open transfers, from one SNAPSHOT read; it never corrects anything;
    - receipt reversal stays at the current average (documented V1 policy, pending the accountant; ADR 0015
      decision 23); no code change was needed.
- **Schema:** `Migration04_Inventory` (13 tables), `Migration05_InventoryTransfersCounts` (7 tables, links, idempotency
  subject, policy row) and `Migration06_InventorySnapshotIsolation` (`ALLOW_SNAPSHOT_ISOLATION ON`, alone because it
  can't run in a transaction). All applied to `WarehouseERP_Dev` on 2026-10-28.
- **Contracts changed additively:** Organization `ITransitLocations`, `IWarehouseListing`; OpenAPI +19 Inventory paths in
  Milestone B and new enum members (`TransferDispatch`, `TransferReceipt`, `CountAdjustment`, `Move`).
- **E2E (2026-10-28):** the real host with cookie sessions on throwaway `WarehouseERP_E2E_*` databases (dropped):
  Milestone A, then Milestone B with an approver, a source-scoped sender and a destination-scoped storekeeper (transfer,
  partial receipt, retry, loss, count with freeze, freeze at the destination, reconciliation with stock in transit).
  Every check passed, no 500s, no passwords in the log.
- **Frontend handoff (`65031c5`):** `docs/modules/Inventory/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md`, written for
  the frontend's `AGENTS.md` UI conventions. The screens were built from them and accepted (above).
- **Production prerequisites** (not development blockers; ADR 0015 *Production prerequisites*):
  1. The accountant approves the moving weighted average and receipt reversal at the current average.
  2. Batch, expiry and serial support is built before products that need tracking are processed (they are refused
     today).
  3. `ALLOW_SNAPSHOT_ISOLATION` is on and verified on the production SQL Server (`Migration06`;
     `snapshot_isolation_state_desc` = `ON`). It is `ON` on `WarehouseERP_Dev`.
- **Deferred backlog** (recorded, not started; implement only on the owner's request):
  * reservation expiry for POS (Sales order reservations exist since `e3e3756`, ADR 0018);
  * outbox integration;
  * general-ledger integration;
  * cancelling a transfer after dispatch (today: receive it or close it);
  * blind stock counting;
  * scheduled reconciliation (today: on demand, no stored history);
  * the existing frontend lint error in `src/features/catalog/rules.ts` (`no-useless-escape`).
  Other V1 limits (ADR 0015 *Not done*): transfer receipts are entered in base units; counts cover available stock
  without lots.

## Organization status
- **Backend complete and approved for V1 (2026-10-27).** The owner accepted every decision in
  `docs/adr/0013-organization-module.md` (status: accepted). The main ones:
  * one company, set up once; code and currency fixed;
  * structure changes need company-wide scope, which also tightens `POST /warehouses`;
  * codes, warehouse type and parent branch are fixed;
  * deactivation rules.
- First-time company setup needs `organization.company.update` plus company-wide access. On a fresh install only the
  bootstrap administrator has both. Concurrent setups can't both succeed (`UPDLOCK, HOLDLOCK`, `CompanySetupTests`).
- Endpoints: company (GET/POST/PUT), branches, warehouses, and locations under `/warehouses/{id}/locations`, each with
  update and activate/deactivate (README table).
- Seven new permissions in `OrganizationPermissions.All`.
- No migration. The OpenAPI contract changed additively only.
- **Frontend built and accepted (2026-10-27)** from `docs/modules/Organization/LOVABLE_PROMPT.md` and
  `FRONTEND_HANDOFF.md`.
- Not done (ADR 0013 *Not done*): changing the default location, a DB check that it stays active, moving a warehouse
  between branches.
- **Inventory integration requirement:** deactivating a warehouse or location that holds stock must be refused before
  stock operations go live. It's recorded in ADR 0013 and `docs/INVENTORY_DECISIONS.md`.

## Catalog status
- **Full-stack complete and accepted for V1 (2026-10-28); on `main`.** Backend code and tests are `adf4a7d`. The owner
  approved the scope and decisions before implementation; they are in `docs/adr/0014-catalog-module.md` (accepted).
  In short:
  * units, categories (tree), products, product units with conversion factors, barcodes, and a barcode lookup;
  * permission-only management, with no company-wide scope; new `catalog.units.manage` and
    `catalog.categories.manage`;
  * fixed after creation: SKU, type, tracking, base unit, unit/category codes, conversion factor, barcode value;
  * retired barcode values stay reserved;
  * deactivation guards: a unit in use, a category with active children, the base product unit;
  * GS1 check digit for EAN-13/GTIN only;
  * lookup returns only fully active items.
- Tax codes/rates/assignments and EGS/GS1 external codes are deferred to Sales/e-invoicing; their tables are untouched.
- No migration; `BarcodeType` moved to `Catalog.Contracts` (same `tinyint`). The OpenAPI contract changed additively
  (21 new operations).
- **Frontend built and accepted:**
  - Lovable built the screens from `docs/modules/Catalog/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md` (frontend
    `7e8e46d`).
  - They were verified in the browser against a throwaway database, then reworked in the UI refresh (`afd7656`, see
    below).
  - The owner tested and accepted them on 2026-10-28.

## Identity status
- Backend complete for the current milestone (ADR 0012, all decisions approved).
- **Lovable handoff done (2026-10-27):** `docs/modules/Identity/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md`, for the
  users, roles, security log and own password/sessions screens. They were verified against the real API on a
  throwaway database.
- Production-readiness items and the backlog are in ADR 0012. Don't work on them unless they block development.

## Frontend and next step
- The ERP frontend is the Lovable project `mahmoudtaha311/screenshot-match`, cloned to `D:\ERP System\warehouse-erp-web`.
  - Stack: TanStack Start + RTK Query; Vite on `http://localhost:8080` proxies `/api` to `https://localhost:49571`.
  - Pushes to its `main` sync back into Lovable, so never rewrite its history.
  - It has the App Shell, sign-in, the Organization screens, the Identity administration screens (`86dc682`) and the
    Catalog screens (`7e8e46d`).
  - The Identity screens were checked in the browser on 2026-10-27 against a throwaway database. Creating roles and
    users, the role 409, own-row limits, the security log, change password, my sessions and the employee view all
    behaved as specified.
- **2026-10-27: the owner tested Identity and Organization on `WarehouseERP_Dev` and accepted them.**
- **UI refresh `afd7656` (2026-10-28, owner's request; accepted). The rules are in the frontend repo's `AGENTS.md`,
  "UI conventions":**
  - Every add and edit opens in a modal (`FormDialog`). Detail pages are read-only with Edit buttons, and there are no
    `/new` pages.
  - Activate/deactivate, delete, unlock and end-session ask first with SweetAlert2.
  - Tables and pages are full width, with icon row actions.
  - The sidebar has one collapsible group per module.
  - The role editor shows translated permission labels only.
  - Edit dialogs fill from a fetch made after they open (`useLatestRecord`).
  - Radix `DirectionProvider` makes RTL correct.
  - Every module's Lovable prompt must tell Lovable to follow those conventions. Where the older
    `docs/modules/*/LOVABLE_PROMPT.md` files say otherwise (pages for new records, inline forms), `AGENTS.md` wins.
- The frontend's `main` is at `fd44db5` with the POS screens (and Sales, Purchasing, address lookups before them). On this machine the frontend is cloned to
  `D:\work\WarehouseERP\warehouse-erp-web` and the backend to `D:\work\WarehouseERP\ERP_API`.
- **Sales V1 is accepted and merged in both repositories (2026-10-29)** (see *Sales status*). **POS V1 is accepted and
  merged in both repositories (2026-10-30)** (see *POS status*). **Accounting is in progress on `feature/accounting`**
  (see *Accounting status*); the frontend has no Accounting screens yet.
- Reopen a backend decision only for a confirmed correctness, security or blocking integration problem.

## Approved conventions
- `CLAUDE.md` is the rulebook:
  * modular monolith, Clean Architecture, vertical slices `Features/<Area>/<UseCase>/` (ADR 0011);
  * `Result` with stable error codes; one save point per command;
  * cross-module access only through `*.Contracts`, enforced by the Architecture Tests.
- Every `Error` lives in `Application/Errors/<Area>Errors.cs` (an architecture test checks it).
- Master-data updates take a base64 `rowVersion` and return the new one (`UpdatedResponse`).
  Activate/deactivate answer 204.
- `docs/api/openapi-v1.json` is the API contract (checked by `HostTests`). After a deliberate change, run the tests
  once with `ERP_UPDATE_OPENAPI_SNAPSHOT=1` (that run fails on purpose), review the diff, run again without it.
- Never change Migration01–03 or existing tables. A schema change is a new migration with an explained diff.
- **Approvals (permanent, all modules):** permission + access scope + document state decide; the creator may approve
  their own document. Never add creator/approver comparisons (`CLAUDE.md`, *Document approvals*).
- Integration tests share one database with one company (`DatabaseFixture.EnsureCompanyAsync`). First-run setup has
  its own database (`CompanySetupTests`).

## Latest verified tests (2026-10-30, Accounting Milestone C, `feature/accounting`, LocalDB, `-m:1`)
| UnitTests | ArchitectureTests | IntegrationTests |
|---|---|---|
| 281/281 | 129/129 | 297/297 |

Earlier the same day: Accounting Milestone B 273 / 129 / 296; Milestone A 267 / 129 / 289.

Earlier (2026-10-29, POS V1 acceptance, `feature/pos` at `2665824`): 259 / 114 / 275.

Frontend `fix/pos-integration` (`fd44db5`): Vitest 112/112, `tsc --noEmit` clean, `vite build` succeeds, ESLint only
Prettier formatting in Lovable's files and two older fast-refresh warnings in `Receipt.tsx`. POS dev acceptance: browser
flow above, API 31/31 (`.artifacts/acc-pos-dev.out`, `acc-pos-dev-tail.out`).

Build: 0 warnings, 0 errors. `has-pending-model-changes`: none. All thirteen migrations applied to a fresh LocalDB
database, Migrations 11–13 rolled back and re-applied. POS E2E 60/60 (above). Earlier (Sales, 2026-10-29): 242 / 99 / 252,
ten migrations, Sales E2E 49/49. Earlier (Purchasing acceptance,
2026-10-29): 236 / 84 / 230; frontend (web `main` `edbfea3`): Vitest 67/67, `tsc --noEmit` clean, `vite build`
succeeds, ESLint only the older Catalog error. Earlier still (Inventory acceptance, 2026-10-28): 225 / 69 / 209. `MasterDataHistoryTests` passed 30 consecutive runs
after its fix. Frontend (`4a3beec`): Vitest 53/53, `tsc --noEmit` clean, `vite build` succeeds; ESLint reports one
older error (the Catalog backlog item above).

The real host was also checked end to end for Catalog on a throwaway LocalDB database, then dropped:
- cookie sessions;
- every Catalog endpoint and rule;
- a branch-scoped catalog manager and a read-only cashier.

The API log had no 500s and no passwords.

## Frontend facts
- Cookie session `__Host-erp` (HttpOnly). Send the `XSRF-TOKEN` cookie back as `X-XSRF-TOKEN` on POST, PUT and
  DELETE; `GET /api/identity/me` renews it.
- No CORS: same origin through a Vite proxy for `/api`. Dev API `https://localhost:49571`
  (`dotnet dev-certs https --trust` first).
- Errors are ProblemDetails with `errorCode` (and `errors[]` on 400). 409 means reload and retry.
- POS:
  * one refresh in flight;
  * refresh before `accessTokenExpiresAtUtc`;
  * never resend a refresh;
  * `Identity.Session.RefreshTokenReused` or `Identity.Session.Revoked` means signed out.
- Gaps:
  * `/me` has warehouse ids but no branch ids;
  * permissions have no display names;
  * warehouse lists carry `branchId` only.

## Working rules
- Reply in Arabic. LocalDB only, never `Server=.`.
- Deliver ERP features, one module at a time. No new architecture refactoring, multi-agent reviews or optional
  hardening unless the owner asks.
- No change to existing migrations, schemas or data; no force push, reset or merge to `main` without approval.
- Build with `-m:1` and run test projects separately. On out-of-memory run `dotnet build-server shutdown`; if every
  integration test fails at once run `sqllocaldb start MSSQLLocalDB`.
- LocalDB starts under whichever process connects first. On 2026-10-28 a session's sandboxed test runs had started it,
  and the dev API launched from the owner's terminal could not reach it (`/health/ready` 503 after an ~18 s timeout).
  Running `sqllocaldb stop MSSQLLocalDB` from the session let the API start it again under the owner's account, with
  the same databases.
- Stage commits with explicit paths, never `launchSettings.json`.
- While the owner's dev API runs from `src/WarehouseERP.Api/bin`, builds can't overwrite it: build and test with
  `--artifacts-path D:/work/WarehouseERP/ERP_API/.artifacts` (excluded in `.git/info/exclude`; inside the repo so
  `ApplicationLayoutTests` finds the solution). Git worktrees need a short path (`C:\Users\lenovo\AppData\Local\Temp\wtsa`
  worked); the scratchpad path is too long for some files.
- The frontend dev server rewrites `src/routeTree.gen.ts` with LF line endings; that line-ending-only change is never
  committed.
