# Warehouse ERP

Modular monolith on .NET 10 / ASP.NET Core / EF Core 10.0.13 / SQL Server, with Clean Architecture inside each
module and vertical slices in the Application layer. One application, one database, one `ErpDbContext`.
Scope: one company with branches and warehouses, trading and distribution only.

* **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**: projects, dependency map, enforced rules, request flow, error contract.
* **[docs/adr/](docs/adr/)**: architecture decisions and their reasons.
* **[CLAUDE.md](CLAUDE.md)**: project guardrails. **[docs/INVENTORY_DECISIONS.md](docs/INVENTORY_DECISIONS.md)**: rules for the coming Inventory module.

## Structure

```
src/
  WarehouseERP.Api/                 composition root (security, error handling, logging, health, OpenAPI)
  WarehouseERP.Persistence/         ErpDbContext over every module's configurations, migrations
  BuildingBlocks/                   SharedKernel (Result, Error, MasterEntity), Application and Infrastructure plumbing
  Modules/Organization/             .Domain, .Application, outer project (Infrastructure + Presentation), .Contracts
  Modules/Catalog/                  same layout
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
| POST | `/api/organization/warehouses` | `organization.warehouses.create` |
| GET | `/api/organization/warehouses/{id}` | `organization.warehouses.read` + warehouse in scope |
| GET | `/api/organization/warehouses?page&pageSize&branchId` | `organization.warehouses.read` (lists the scope only) |
| POST | `/api/catalog/products` | `catalog.products.create` |
| GET | `/api/catalog/products/{id}` | `catalog.products.read` |
| GET | `/api/catalog/products?page&pageSize&search` | `catalog.products.read` |
| PUT | `/api/catalog/products/{id}` (names + `rowVersion`) | `catalog.products.update` |
| GET | `/health/live`, `/health/ready`, `/api/health` | anonymous |
| GET | `/openapi/v1.json` (Development only) | anonymous |

Errors are ProblemDetails with a stable `errorCode` (see ARCHITECTURE.md). Without an identity provider configured
(`Authentication:Jwt`), every protected endpoint answers 401: authentication validates tokens from a trusted issuer
and never issues them (ADR 0005).

## Prerequisites

- .NET 10 SDK; SQL Server 2019+ or LocalDB.
- `dotnet-ef` 10.0.13 is a local tool (`dotnet-tools.json`): run `dotnet tool restore` once, then `dotnet dotnet-ef`.
- On the development laptop use **LocalDB**, not `Server=.`: the default SQL Server instance there belongs to another live system.
- Never commit secrets. Outside Development set `Database__ConnectionString` (and `Authentication__Jwt__*`) as environment variables or in a secret store.

## Configuration

| Section | Keys | Validated at startup |
|---|---|---|
| `Database` | `ConnectionString` (required), `CommandTimeoutSeconds` (5–600, default 30) | yes |
| `Pagination` | `DefaultPageSize` (default 20), `MaxPageSize` (default 100), default ≤ max | yes |
| `Authentication:Jwt` | `Authority` (https URL of the trusted issuer), `Audience`, `RequireHttpsMetadata` | yes, when present |
| `Serilog` | `MinimumLevel`, overrides, properties | — |

`appsettings.Development.json` points `Database:ConnectionString` at LocalDB (Windows authentication, no secret).

## Database

```powershell
$env:ERP_SQL_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=WarehouseERP_Dev;Trusted_Connection=True;TrustServerCertificate=True'
dotnet tool restore
$p = 'src/WarehouseERP.Persistence/WarehouseERP.Persistence.csproj'

dotnet dotnet-ef migrations script --idempotent --project $p --startup-project $p -o Migration01_OrganizationCatalog.sql   # inspect first
dotnet dotnet-ef database update --project $p --startup-project $p
dotnet dotnet-ef migrations has-pending-model-changes --project $p --startup-project $p                                  # must report none
```

The design-time factory reads `ERP_SQL_CONNECTION`.

## Running the API

```powershell
cd src/WarehouseERP.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run
```

In Development the host validates the whole DI container at startup and serves OpenAPI.

## Tests

```powershell
dotnet test WarehouseERP.slnx
```

On this laptop builds can run out of memory with parallel nodes; add `-m:1` if MSBuild reports `OutOfMemoryException`.

- `WarehouseERP.UnitTests`: Result and its HTTP mapping, the handler pipeline, paging rules, domain rules, validators, handlers against fakes.
- `WarehouseERP.IntegrationTests`: each run creates `WarehouseERP_Tests_<guid>` on LocalDB, applies the migrations and drops it. Covers constraints, temporal history and audit stamps, EF model vs Migration 01, unique-key and concurrency mapping, every endpoint (validation 400, business 422, conflicts 409, scope 403, 401), sanitized 500 logged once, health, options validation, OpenAPI, DI. Set `ERP_TEST_SQL_SERVER` for another instance.
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

See the final report in the refactoring PR and `docs/adr/` for reasons: Identity module (users, roles, permission
assignment, login), branch scope, OpenTelemetry exporters, outbox/inbox and idempotency store (with Inventory),
explicit transaction port (with Inventory), domain methods for barcodes, external codes and tax entities (with
their features), CRUD for company/branch/unit/category.
