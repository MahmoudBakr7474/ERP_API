# Session handoff — WarehouseERP

Last verified: 2026-10-27. Read this first; don't re-analyse the repository.

## Where things stand
- Branch `feature/identity-completion`, not merged (`main` = `67a7473`). HEAD is the docs commit on top of `8d4a12b`
  (`git log -1`). Approved baseline: `5498eb5` (Identity implementation and tests), `816fb20` (docs), `8d4a12b`
  (role management needs company-wide scope).
- `src/WarehouseERP.Api/Properties/launchSettings.json` is the owner's untracked file: never commit or edit it.
- Merging into `main` needs the owner's approval.

## Identity status
- **Backend complete for the current milestone.** `docs/adr/0012-identity-hardening.md` records the design and every
  owner decision (all approved 2026-10-27).
- **Not full-stack complete:** `docs/modules/Identity/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md` are still pending.
- Before production (ADR 0012, *Production readiness*): an independent administrative recovery procedure, Data
  Protection key encryption, `identity.SecurityEvents` retention, the deployment's `ReverseProxy` and rate limits.
- Low-priority backlog: ADR 0012, *Backlog*. Don't work on it unless it blocks normal development.

## Approved conventions
- `CLAUDE.md` is the rulebook: modular monolith, Clean Architecture, vertical slices `Features/<Area>/<UseCase>/`
  (ADR 0011), `Result` with stable error codes, one save point per command, cross-module access only through
  `*.Contracts`, enforced by the Architecture Tests.
- `docs/api/openapi-v1.json` is the API contract (checked by `HostTests`). After a deliberate change, run the tests
  once with `ERP_UPDATE_OPENAPI_SNAPSHOT=1` (that run fails on purpose), review the diff, run again without it.
- Never change Migration01–03 or existing tables. A schema change is a new migration with an explained diff.

## Latest verified tests (2026-10-27, LocalDB, `-m:1`, each project separately)
| UnitTests | ArchitectureTests | IntegrationTests |
|---|---|---|
| 118/118 | 54/54 | 126/126 |

Build: 0 warnings, 0 errors. `has-pending-model-changes`: none.

## Next module: Organization
**First task: the Branch read API.** There is no branch endpoint today, and the Identity access screens need branch
names. No migration: `Branch` already exists (`Code`, `NameAr`, `NameEn`, company, warehouses).
- `GET /api/organization/branches` (paged, `search` on code and names) and `GET /api/organization/branches/{id}`.
- A new permission `organization.branches.read` in `OrganizationPermissions.All`, so Identity can grant it.
- Scope as for warehouses: a company-wide caller sees every branch, anyone else only the branches of warehouses they
  can reach. An out-of-scope branch answers 403 `Organization.Branch.AccessDenied` before the existence check, as
  `GetWarehouseHandler` does.
- Slices `Features/Branches/ListBranches` and `Features/Branches/GetBranch` in Organization.Application, a read store
  in Infrastructure, endpoints beside `WarehouseEndpoints`.
- Unit and integration tests (scope, 401/403/404, paging), the OpenAPI snapshot regenerated, the README endpoint
  table updated.

## Frontend facts (for the pending Lovable files)
- Cookie session `__Host-erp` (HttpOnly). Send the `XSRF-TOKEN` cookie back as `X-XSRF-TOKEN` on POST, PUT and
  DELETE; `GET /api/identity/me` renews it. No CORS: same origin through a Vite proxy for `/api`. Dev API
  `https://localhost:49571` (`dotnet dev-certs https --trust` first).
- Errors are ProblemDetails with `errorCode` (and `errors[]` on 400). 409 means reload and retry.
- POS: one refresh in flight, refresh before `accessTokenExpiresAtUtc`, never resend a refresh;
  `Identity.Session.RefreshTokenReused` or `Identity.Session.Revoked` means signed out.
- Gaps: no branch endpoint (Organization's first task), access returns ids only, permissions have no display names.

## Working rules
- Reply in Arabic. LocalDB only, never `Server=.`.
- Deliver ERP features, one module at a time. No new architecture refactoring, multi-agent reviews or optional
  hardening unless the owner asks.
- No change to existing migrations, schemas or data; no force push, reset or merge to `main` without approval.
- Build with `-m:1` and run test projects separately. On out-of-memory run `dotnet build-server shutdown`; if every
  integration test fails at once run `sqllocaldb start MSSQLLocalDB`.
- Stage commits with explicit paths, never `launchSettings.json`.
