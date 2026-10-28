# Session handoff — WarehouseERP

Last verified: 2026-10-29 (Purchasing V1 backend complete on `feature/purchasing`, waiting for the owner's review;
not merged). Read this first; don't re-analyse the repository.

## Where things stand
- **`main` holds Identity, Organization, Catalog and Inventory V1 (2026-10-28).** With the owner's approval it was
  fast-forwarded from `1a1aba1` to the tip of `feature/inventory` (Inventory code, tests, docs and this handoff).
  Earlier on 2026-10-28 it went from `383d5be` to the tip of `feature/catalog` (Catalog code `adf4a7d`, docs
  `bb536a0`), and on 2026-10-27 from `67a7473` to `4b1048d` (`feature/organization`, which includes
  `feature/identity-completion`). There are no merge commits.
- **Remote (2026-10-28):** `origin` = `https://github.com/MahmoudBakr7474/ERP_API.git` (the owner's GitHub, used to work
  from other machines and from Claude in the cloud). `main` and `feature/inventory` are pushed with upstream tracking;
  never force-push, never push `launchSettings.json`.
- **Current branch: `feature/purchasing`** (cut from `main` at `c791868`, local only, not pushed, not merged):
  * `16a15ce` global self-approval rule (`CLAUDE.md`), stale "another user approves" comments fixed, README approval
    rows corrected, `InventorySelfApprovalTests` (no Inventory behaviour change: it already allowed it since `29cbdb3`);
  * `84e8c19` Inventory: `IPurchaseStockPosting`, stock document types `PurchaseReceipt`/`PurchaseReturn`,
    `SourceDocumentId`;
  * `661e0dd` the Purchasing module; `bb7fa42` `Migration07_Purchasing`; `aff0486` tests and the OpenAPI contract;
  * the docs commit with ADR 0016, `docs/modules/Purchasing/*` and this handoff.
  Merging into `main` needs the owner's approval.
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

## Purchasing status
- **Decisions:** approved by the owner on 2026-10-28 with corrections: `docs/modules/Purchasing/V1_DECISIONS.md`. How
  the code implements them: `docs/adr/0016-purchasing-module.md`. Self-approval is allowed everywhere (owner's
  permanent rule, `CLAUDE.md` *Document approvals*); the corrections' maker-checker wording is superseded by it.
- **Backend complete and verified (2026-10-29), waiting for the owner's review:**
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
  1–10 and `SourceDocumentId`). Applied to throwaway databases only; **not yet applied to `WarehouseERP_Dev`** (run
  `dotnet dotnet-ef database update`, README *Database*, before the frontend work).
- **E2E (2026-10-29):** the real host with cookie sessions on a throwaway `WarehouseERP_E2E_Purch_*` database
  (dropped): a scoped buyer approving their own request, order, invoice and return; a clerk (403 on approve) and an
  approver of another warehouse (403); request → order → partial receipts with an idempotent retry → over-receipt
  refused → invoices (exact price posts, variance needs approval, duplicate number 409, over-invoicing 422) → return
  (2 uninvoiced + 2 invoiced, claim 41.55) → reconciliation consistent. 34/34 checks; no 500s, no passwords in the log.
- **Frontend handoff:** `docs/modules/Purchasing/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md` (screens not built yet).
  Inventory's screens also need labels for `PurchaseReceipt`/`PurchaseReturn` and a link from `sourceDocumentId`.
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
  * stock reservations for Sales/POS;
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
- The frontend's `main` is at `4a3beec` with the accepted Inventory screens. On this machine the frontend is cloned to
  `D:\work\WarehouseERP\warehouse-erp-web` and the backend to `D:\work\WarehouseERP\ERP_API`.
- **Next step: the owner reviews Purchasing on `feature/purchasing`.** Then, on approval: apply `Migration07` to
  `WarehouseERP_Dev`, merge into `main` (owner's approval), build the Purchasing screens from the Lovable prompt.
  Don't start Sales, POS or Accounting without the owner's request.
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

## Latest verified tests (2026-10-29, Purchasing on `feature/purchasing`, LocalDB, `-m:1`)
| UnitTests | ArchitectureTests | IntegrationTests |
|---|---|---|
| 236/236 | 84/84 | 221/221 |

Build: 0 warnings, 0 errors. `has-pending-model-changes`: none. All seven migrations applied to a fresh LocalDB
database. Earlier (Inventory acceptance, 2026-10-28): 225 / 69 / 209. `MasterDataHistoryTests` passed 30 consecutive runs
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
