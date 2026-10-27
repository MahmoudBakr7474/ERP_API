# Session handoff — WarehouseERP

Last verified: 2026-10-27. Read this first; don't re-analyse the repository.

## Where things stand
- Branch `feature/organization`, cut from `feature/identity-completion` HEAD `a29c3b5`. Neither is merged
  (`main` = `67a7473`). Merging into `main` needs the owner's approval.
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
- **Lovable handoff done:** `docs/modules/Organization/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md`. The frontend
  isn't built yet, so the module is not full-stack complete.
- Not done (ADR 0013 *Not done*): changing the default location, a DB check that it stays active, moving a warehouse
  between branches.
- **Inventory integration requirement:** deactivating a warehouse or location that holds stock must be refused before
  stock operations go live. It's recorded in ADR 0013 and `docs/INVENTORY_DECISIONS.md`.

## Identity status
- Backend complete for the current milestone (ADR 0012, all decisions approved).
- `docs/modules/Identity/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md` are still pending.
- Production-readiness items and the backlog are in ADR 0012. Don't work on them unless they block development.

## Next: Organization frontend
- Build it in one React ERP application with a reusable App Shell and the existing Identity cookie sign-in.
- Work from `docs/modules/Organization/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md`.
- This is frontend integration work, not more backend architecture.
- Reopen a backend decision only for a confirmed correctness, security or blocking integration problem.
- Don't start Catalog or Inventory.

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
| 145/145 | 54/54 | 156/156 |

Build: 0 warnings, 0 errors. `has-pending-model-changes`: none.

The real host was also checked end to end: Identity cookie sessions, a role and a branch-scoped user, a throwaway
LocalDB database, then dropped.

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
