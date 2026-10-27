# Warehouse ERP

Modular monolith on .NET 10 / ASP.NET Core / EF Core 10.0.14 / SQL Server, with Clean Architecture inside each
module and vertical slices in the Application layer. One application, one database, one `ErpDbContext`.
Scope: one company with branches and warehouses, trading and distribution only.

* **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**: projects, dependency map, enforced rules, request flow, error contract.
* **[docs/adr/](docs/adr/)**: architecture decisions and their reasons.
* **[CLAUDE.md](CLAUDE.md)**: project guardrails. **[docs/INVENTORY_DECISIONS.md](docs/INVENTORY_DECISIONS.md)**: rules for the coming Inventory module.

## Structure

```
src/
  WarehouseERP.Api/                 composition root (error handling, logging, health, OpenAPI)
  WarehouseERP.Persistence/         ErpDbContext (IdentityDbContext) over every module's configurations, migrations
  BuildingBlocks/                   SharedKernel (Result, Error, MasterEntity), Application and Infrastructure plumbing
  Modules/Organization/             .Domain, .Application, outer project (Infrastructure + Presentation), .Contracts
  Modules/Catalog/                  same layout
  Modules/Identity/                 same layout: users, roles, permissions, scope, sessions (ADR 0009)
tests/
  WarehouseERP.UnitTests/           no database
  WarehouseERP.IntegrationTests/    SQL Server (LocalDB) + the real HTTP host
  WarehouseERP.ArchitectureTests/   dependency rules
```

Package versions: `Directory.Packages.props` (central). Build settings, analyzers (`latest-recommended`, warnings as
errors) and style: `Directory.Build.props`, `.editorconfig`.

## API

| Method | Route | Permission |
|---|---|---|
| GET | `/api/organization/company` (404 `Organization.Company.NotConfigured` until set up) | signed in |
| POST, PUT | `/api/organization/company` (POST once: first-run setup; PUT settings + `rowVersion`) | `organization.company.update` + company-wide |
| GET | `/api/organization/branches?page&pageSize&search&isActive`, `/branches/{id}` | `organization.branches.read` (company-wide: all; else branches of the user's warehouses) |
| POST | `/api/organization/branches` | `organization.branches.create` + company-wide |
| PUT, POST | `/api/organization/branches/{id}` (names + `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `organization.branches.update` + company-wide |
| POST | `/api/organization/warehouses` (creates its `DEFAULT` location) | `organization.warehouses.create` + company-wide |
| GET | `/api/organization/warehouses?page&pageSize&branchId&search&isActive`, `/warehouses/{id}` | `organization.warehouses.read` + warehouse in scope |
| PUT, POST | `/api/organization/warehouses/{id}` (names, `allowsSelling`, `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `organization.warehouses.update` + warehouse in scope |
| GET | `/api/organization/warehouses/{warehouseId}/locations?page&pageSize&search&isActive`, `/locations/{locationId}` | `organization.warehouses.read` + warehouse in scope |
| POST | `/api/organization/warehouses/{warehouseId}/locations` | `organization.locations.create` + warehouse in scope |
| PUT, POST | `/api/organization/warehouses/{warehouseId}/locations/{locationId}` (names + `rowVersion`), `/activate`, `/deactivate` | `organization.locations.update` + warehouse in scope |
| POST | `/api/catalog/products` | `catalog.products.create` |
| GET | `/api/catalog/products/{id}` | `catalog.products.read` |
| GET | `/api/catalog/products?page&pageSize&search` | `catalog.products.read` |
| PUT | `/api/catalog/products/{id}` (names + `rowVersion`) | `catalog.products.update` |
| POST | `/api/identity/login` → HttpOnly cookie + `XSRF-TOKEN`; `/api/identity/logout` | anonymous / signed in |
| POST | `/api/identity/tokens` → bearer + refresh; `/tokens/refresh`; `/tokens/revoke` | anonymous / token |
| GET | `/api/identity/me`; POST `/api/identity/me/password`; GET `/api/identity/password-policy` | signed in |
| GET, POST | `/api/identity/users`, GET/PUT `/users/{id}`, POST `/users/{id}/activate`, `/deactivate`, `/unlock`, `/password` | `identity.users.read` / `identity.users.manage` |
| PUT | `/api/identity/users/{id}/roles` | `identity.users.manage` |
| PUT | `/api/identity/users/{id}/access` (company-wide / branches / warehouses + `revision` from GET) | `identity.access.manage` |
| GET, POST | `/api/identity/users/{id}/sessions`, `/users/{id}/sessions/revoke` | `identity.sessions.manage` |
| GET, POST, PUT, DELETE | `/api/identity/roles`, `/roles/{id}` (PUT + `version` from GET); GET `/api/identity/permissions` | `identity.roles.read` / `identity.roles.manage` |
| GET | `/api/identity/security-events?page&pageSize&userId&type&fromUtc&toUtc` (audit trail) | `identity.audit.read` |
| GET, POST | `/api/identity/sessions` (own), `/sessions/{id}/revoke` | signed in (others' need `identity.sessions.manage`) |
| GET | `/health/live`, `/health/ready`, `/api/health` | anonymous |
| GET | `/openapi/v1.json` (Development only) | anonymous |

Errors are ProblemDetails with a stable `errorCode` (see ARCHITECTURE.md). Sign-in is the API's own (ADR 0009): the
React client uses the cookie session and echoes the `XSRF-TOKEN` cookie in an `X-XSRF-TOKEN` header on every
POST/PUT/DELETE; the POS sends `Authorization: Bearer <accessToken>` and refreshes before it expires. Sign-in
endpoints answer only over HTTPS, and tokens are accepted only over HTTPS. Role, permission and scope changes apply on
the user's next request. Login, token issue, token refresh and password change each have their own limit per client
address (429 `Common.TooManyRequests` with `Retry-After`); sign-ins, refused attempts, lockouts, password, role, scope
and session changes and refused escalation attempts are recorded in `identity.SecurityEvents` (ADR 0012).

Any second use of a refresh token ends the session, so the POS keeps one refresh in flight for all its callers,
refreshes shortly before `accessTokenExpiresAtUtc` (not on every 401), never resends a refresh automatically (a
refresh that timed out means signing in again), and treats 401 `Identity.Session.RefreshTokenReused` or
`Identity.Session.Revoked` as signed out.

The API contract is [`docs/api/openapi-v1.json`](docs/api/openapi-v1.json) (the Development `/openapi/v1.json`, checked
by `HostTests`): schema names, operation ids, tags, property names, enum members and status codes are public API.
After a deliberate change, run the integration tests once with `$env:ERP_UPDATE_OPENAPI_SNAPSHOT = '1'` (the contract
test rewrites the file and then fails on purpose), review the diff, remove the variable and run them again.

## Prerequisites

- .NET 10 SDK; SQL Server 2019+ or LocalDB.
- `dotnet-ef` 10.0.14 is a local tool (`dotnet-tools.json`): run `dotnet tool restore` once, then `dotnet dotnet-ef`.
- On the development laptop use **LocalDB**, not `Server=.`: the default SQL Server instance there belongs to another live system.
- Never commit secrets. Outside Development set `Database__ConnectionString` (and, once, `Identity__Bootstrap__*`) as environment variables or in a secret store.
- HTTPS in development too: `dotnet dev-certs https --trust` once. Behind a TLS-terminating proxy, enable `ReverseProxy` with the proxy's address, or sign-in is refused and every client shares one rate limit.

## Configuration

| Section | Keys | Validated at startup |
|---|---|---|
| `Database` | `ConnectionString` (required), `CommandTimeoutSeconds` (5–600, default 30) | yes |
| `Pagination` | `DefaultPageSize` (default 20), `MaxPageSize` (default 100), default ≤ max | yes |
| `Identity:Sessions` | `WebIdleTimeout` (8 h), `WebAbsoluteLifetime` (12 h), `ApiAccessTokenLifetime` (15 min), `ApiRefreshTokenLifetime` (14 d), `ApiAbsoluteLifetime` (30 d), `RetentionAfterEnd` (30 d: ended sessions are then deleted), `CleanupInterval` (1 h) | yes (ranges, idle ≤ absolute, access ≤ refresh ≤ absolute) |
| `Identity:SignInRateLimit` | One section per operation: `Login`, `IssueApiToken`, `RefreshApiToken`, `ChangeMyPassword`, each with `PermitLimit` (60, 60, 300, 30; 1–100000) attempts per client address per `Window` (1 min, 1 s–1 h). An IPv6 address counts as its /64, so one IPv6 LAN segment is one address. Size each for the busiest address a branch shares: about twice its clients that use the operation. Read at startup: restart after a change | yes |
| `Identity:Password` | ASP.NET Core Identity password options; `RequiredLength` 10 (≥ 8), upper/lower/digit required | yes |
| `Identity:Lockout` | `MaxFailedAccessAttempts` 5 (3–20), `DefaultLockoutTimeSpan` 15 min (≥ 1 min) | yes |
| `Identity:Bootstrap` | `UserName`, `Password`, `DisplayName`: first administrator, only while no user exists; never in appsettings | yes (both or neither) |
| `ReverseProxy` | `Enabled` (false), `KnownProxies` (IP addresses), `KnownNetworks` (CIDR), `ForwardLimit` (1, 1–10): trust X-Forwarded-For/-Proto from these proxies only | yes (at least one proxy when enabled, parsable entries) |
| `Serilog` | `MinimumLevel`, overrides, properties | — |

`appsettings.Development.json` points `Database:ConnectionString` at LocalDB (Windows authentication, no secret).

## Database

```powershell
$env:ERP_SQL_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=WarehouseERP_Dev;Trusted_Connection=True;TrustServerCertificate=True'
dotnet tool restore
$p = 'src/WarehouseERP.Persistence/WarehouseERP.Persistence.csproj'

dotnet dotnet-ef migrations script --idempotent --project $p --startup-project $p -o migrations.sql   # inspect first
dotnet dotnet-ef database update --project $p --startup-project $p
dotnet dotnet-ef migrations has-pending-model-changes --project $p --startup-project $p                                  # must report none
```

The design-time factory reads `ERP_SQL_CONNECTION`. `Migration02_Identity` only adds the `identity` schema (and its
`history.*History` tables); it changes no existing table and seeds the system Administrator role.
`Migration03_IdentitySecurity` only adds `identity.SecurityEvents` (append-only audit trail, no foreign keys) and
`identity.RetiredRefreshTokens` (hashes of exchanged refresh tokens, cascading with their session).

## Running the API

```powershell
cd src/WarehouseERP.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run
```

In Development the host validates the whole DI container at startup and serves OpenAPI. To create the first
administrator, set it for one start while the user table is empty, then remove it:

```powershell
$env:Identity__Bootstrap__UserName = 'admin'
$env:Identity__Bootstrap__Password = '<a password meeting Identity:Password>'
```

## Tests

```powershell
dotnet test WarehouseERP.slnx
```

On this laptop builds can run out of memory with parallel nodes; add `-m:1` if MSBuild reports `OutOfMemoryException`.

- `WarehouseERP.UnitTests`: Result and its HTTP mapping, the handler pipeline, paging rules, domain rules, validators, handlers against fakes.
- `WarehouseERP.IntegrationTests`: each run creates `WarehouseERP_Tests_<guid>` on LocalDB, applies the migrations and drops it. Covers constraints, temporal history and audit stamps, EF model vs migrations, unique-key and concurrency mapping, the endpoints' validation 400, business 422, conflicts 409, scope 403 and 401, sanitized 500 logged once, health, options validation, OpenAPI, DI. Identity runs on real sessions over https: cookie and bearer sign-in, identical 401s, lockout (also under parallel guesses), stale user saves refused after a counted attempt, what a failed login save leaves behind, administrative reset and unlock, logout/revocation/deactivation/password change effective on the next request, idle expiry, session cleanup, anti-forgery, refresh rotation, reuse and racing refreshes, permission removal and branch grants applied without re-login, no privilege escalation, per-operation rate limits, plain-HTTP tokens ignored, forwarded headers from trusted proxies only, the audit trail's permission, bootstrap (own database), no secrets in logs, the committed OpenAPI contract. Set `ERP_TEST_SQL_SERVER` for another instance.
- `WarehouseERP.ArchitectureTests`: layer and module rules, composition root, no cycles, entity ownership per schema.

## Constraints worth noting

1. **One DEFAULT location**: `Warehouse.Create` always adds it; a filtered unique index refuses a second one. Never deactivate the only default location.
2. **Base unit**: `Product.BaseUnitId` is canonical; `Product.Create` adds its `ProductUnit` row at factor 1. Do not change `BaseUnitId` once inventory exists.
3. **Temporal master history**: `ValidFrom/ValidTo` are SQL Server-generated; `history.*History` versions master changes. `CreatedBy/UpdatedBy` are stamped by `AuditingSaveChangesInterceptor` from `ICurrentUser`.
4. **No cascade deletes**: master data is retired with `Deactivate()`.
5. **Tax periods**: `[EffectiveFrom, EffectiveTo)`; overlapping ranges need serialized validation in the future tax commands.
6. **Category cycles**: the database refuses only a self-parent; deeper loops are for the category commands.
7. **Sequential GUID keys**: never assign `Guid.NewGuid()` to a key; EF Core generates SQL Server-ordered values.
8. **ExternalItemCode**: optional `ProductUnitId` must belong to the same product (composite FK).
9. **Known unique constraints** are mapped to business codes by name (`IX_Products_SKU`, `IX_Warehouses_BranchId_Code`); renaming an index means updating its repository map.

## Postponed

See `docs/adr/` for reasons: Data Protection key encryption at rest, MFA/SSO, POS device registration and offline grants (ADR 0009), OpenTelemetry exporters, outbox/inbox and idempotency store (with Inventory),
explicit transaction port (with Inventory), domain methods for barcodes, external codes and tax entities (with
their features), CRUD for company/branch/unit/category.
