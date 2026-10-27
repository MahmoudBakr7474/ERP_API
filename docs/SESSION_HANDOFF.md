# Session handoff — WarehouseERP

Last verified: 2026-10-27. Read this first; don't re-analyse the repository.

## Where things stand
- **`main` holds Identity and Organization (2026-10-27).** With the owner's approval it was fast-forwarded from
  `67a7473` to `4b1048d` (`feature/organization`, which includes `feature/identity-completion`). There is no merge
  commit and no remote.
- Start new modules from `main` on their own `feature/...` branch. Merging into `main` needs the owner's approval.
- **Current branch: `feature/catalog`**, cut from `main` at `383d5be`. Ancestry was checked on 2026-10-27: it holds
  every approved Identity and Organization commit (including `63ec342`). Not merged.
- Organization's approved baseline: `fde5e28` (code, tests, ADR 0013, frontend handoff).
- Identity's approved baseline: `5498eb5` (code and tests), `816fb20` (docs), `8d4a12b` (role management needs
  company-wide scope).
- `src/WarehouseERP.Api/Properties/launchSettings.json` is the owner's untracked file: never commit or edit it.

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
- **Backend V1 complete (2026-10-27), on `feature/catalog`: code and tests `adf4a7d`.** The owner approved the scope and decisions before
  implementation; they are in `docs/adr/0014-catalog-module.md` (accepted). In short:
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
- **Lovable handoff done:** `docs/modules/Catalog/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md`. The frontend isn't
  built yet.

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
  - It has the App Shell, sign-in, the Organization screens and, since `86dc682`, the Identity administration screens.
  - The Identity screens were checked in the browser on 2026-10-27 against a throwaway database. Creating roles and
    users, the role 409, own-row limits, the security log, change password, my sessions and the employee view all
    behaved as specified.
- **2026-10-27: the owner tested both modules on `WarehouseERP_Dev` and accepted them.** Identity and Organization
  are full-stack complete for V1.
- Next: the owner reviews Catalog, then pastes `docs/modules/Catalog/LOVABLE_PROMPT.md` into the same Lovable project.
  Merging `feature/catalog` into `main` needs approval. Don't start Inventory.
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
- Integration tests share one database with one company (`DatabaseFixture.EnsureCompanyAsync`). First-run setup has
  its own database (`CompanySetupTests`).

## Latest verified tests (2026-10-27, LocalDB, `-m:1`, each project separately)
| UnitTests | ArchitectureTests | IntegrationTests |
|---|---|---|
| 186/186 | 54/54 | 171/171 |

Build: 0 warnings, 0 errors. `has-pending-model-changes`: none.

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
- Stage commits with explicit paths, never `launchSettings.json`.
