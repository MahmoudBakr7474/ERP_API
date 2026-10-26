# WarehouseERP — Architecture

Modular monolith with Clean Architecture inside each module and vertical slices in the Application layer.
One process, one SQL Server database, one `ErpDbContext`. Scope: one company, trading and distribution only.
Decisions and their reasons are in [`adr/`](adr/); the rules below are enforced by `tests/WarehouseERP.ArchitectureTests`.

## Projects

```
src/
  WarehouseERP.Api/                         composition root: Program.cs, error handling, logging, health
  WarehouseERP.Persistence/                 ErpDbContext (IdentityDbContext over all modules' configurations), migrations,
                                            cross-module foreign keys, DatabaseOptions, Data Protection key store
  BuildingBlocks/
    WarehouseERP.SharedKernel/              Result, Error, ErrorType, CommonErrors, MasterEntity, IAuditable, Text (no references)
    WarehouseERP.BuildingBlocks.Application/ handler contracts, validation + logging decorators, ICurrentUser, claim
                                            names, PermissionDefinition, paging
    WarehouseERP.BuildingBlocks.Infrastructure/ temporal mapping, auditing interceptor, DbFailures, handler
                                            registration, Result -> ProblemDetails, permission policies
  Modules/<Module>/
    WarehouseERP.<Module>.Domain/           entities, invariants, domain errors
    WarehouseERP.<Module>.Application/      Features/<UseCase>/{Command|Query, Handler, Validator}, ports, errors
    WarehouseERP.<Module>/                  Infrastructure/ (EF configurations, port implementations, contract
                                            implementations) + Presentation/ (endpoints) + <Module>Module (DI)
    WarehouseERP.<Module>.Contracts/        what other modules may use: read interfaces, enums, permission names
tests/
  WarehouseERP.UnitTests/                   Result, mapping, pipeline, domain rules, validators, handlers (fakes)
  WarehouseERP.IntegrationTests/            SQL Server (LocalDB) + the real HTTP host
  WarehouseERP.ArchitectureTests/           the dependency rules below
```

Modules today: **Organization** (company, branch, warehouse, location), **Catalog** (category, unit, product,
product unit, barcode, external item code, tax code/rate/assignment) and **Identity** (users, roles and their
permissions, user scope, sessions; authenticates every request and provides `ICurrentUser` to all modules, ADR 0009).

## Dependency map

```
                       ┌──────────────────────── WarehouseERP.Api ────────────────────────┐
                       │                               │                                   │
                       ▼                               ▼                                   ▼
          WarehouseERP.Persistence ──────────► WarehouseERP.<Module>  (Infrastructure + Presentation)
                       │                               │
                       │                               ▼
                       │                  WarehouseERP.<Module>.Application ──► any *.Contracts
                       │                               │
                       ▼                               ▼
     BuildingBlocks.Infrastructure ──► BuildingBlocks.Application        WarehouseERP.<Module>.Domain
                                                       │                       │          │
                                                       ▼                       ▼          ▼
                                               WarehouseERP.SharedKernel ◄─────┘   <Module>.Contracts
```

Arrows point inward. No cycles (checked). The module's outer project never references `Persistence` or `Api`.

## Rules and the tests that enforce them

| Rule | Test |
|---|---|
| Domain references only the shared kernel and its own Contracts; no EF Core, ASP.NET Core, FluentValidation, Serilog | `Domain_uses_only_the_shared_kernel_and_its_own_contracts` |
| Application depends on its Domain, Contracts (any module), BuildingBlocks.Application, SharedKernel; never EF Core, ASP.NET Core, SqlClient, Serilog | `Application_depends_inward_and_never_on_ef_core_http_or_serilog` |
| A module's outer project reaches no other module's Domain/Application/outer project, nor `Persistence`/`Api` | `Infrastructure_and_presentation_reach_no_other_module_nor_the_composition_root` |
| Presentation never touches EF Core, Infrastructure or Domain types (goes through Application) | `Presentation_goes_through_the_application_and_never_touches_ef_core` |
| A module configures only its own entities (Identity also maps ASP.NET Core Identity's row types) | `Infrastructure_configures_only_its_own_modules_entities` |
| Every entity lives in its module's schema (`organization.*`, `catalog.*`, `identity.*`) | `Every_entity_lives_in_its_modules_schema` |
| Only `WarehouseERP.Identity` and `Persistence` reference ASP.NET Core Identity | `Only_identity_and_persistence_reference_aspnet_core_identity` |
| Contracts and the shared kernel reference nothing | `Contracts_are_plain_types_with_no_references`, `Shared_kernel_references_no_project_and_no_framework_package` |
| Only the host and Persistence reference module outer projects; nothing references the host | `Only_the_host_and_persistence_reference_module_outer_assemblies`, `Nothing_references_the_host` |
| No circular references | `Project_assemblies_form_no_cycle` |
| New modules are discovered from the host and checked automatically | `Host_wires_the_known_modules` (update the expected list when adding one) |

## A request, end to end

```
HTTP ─► CorrelationIdMiddleware ─► Serilog request logging ─► ExceptionHandler ─► StatusCodePages
    ─► Authentication (Identity session: cookie or bearer, access resolved for this request)
    ─► anti-forgery check (cookie sessions, unsafe methods) ─► Authorization (permission policy) ─► endpoint
        maps request DTO -> command/query (explicit, never an entity)
        ─► ICommandHandler/IQueryHandler  =  Logging decorator ─► Validation decorator ─► feature handler
                                                                   (FluentValidation)   (domain + ports)
        ◄─ Result<T> ─► 2xx  |  Error ─► ProblemDetails { errorCode, errors?, traceId, correlationId }
```

* Expected failures are `Result` errors with stable codes (ADR 0003). Unexpected exceptions reach
  `GlobalExceptionHandler`: logged once, answered as `Common.Unexpected` (500) without details.
* Each command handler owns its transaction: one save point (a repository write method → one `SaveChanges`,
  one database transaction). Known unique-index violations become business errors there (ADR 0004).
* Handlers see the user through `ICurrentUser`; warehouse scope is checked in the handler, permissions by
  endpoint policies (ADR 0005). The Identity module authenticates every request against its sessions (ADR 0009).

## Adding a feature

1. `Application/Features/<UseCase>/Command.cs` (or `Query.cs`), `Handler.cs`, `Validator.cs`.
2. A port in `Application/Abstractions` if the handler needs data it can't get yet; implement it in
   `<Module>/Infrastructure`. Writes save once, through the repository method that is the save point.
3. Register in `<Module>Module.Add<Module>Module()` with `AddCommandHandler`/`AddQueryHandler` (this wraps the
   handler in the pipeline), map the endpoint in `Presentation/`, name its permission in Contracts.
4. Unit-test the handler with fakes, integration-test the endpoint. No migration unless the schema must change.

## Adding a module

`src/Modules/<Name>/` with `WarehouseERP.<Name>.Domain`, `.Application`, `.Contracts` and the outer
`WarehouseERP.<Name>` project; reference the outer project from `Api` and `Persistence`; call
`Add<Name>Module()`/`Map<Name>Endpoints()` in `Program.cs`, apply its configurations in `ErpDbContext`, register its
permissions in `Add<Name>Module()` with `services.AddPermissions("<Name>", <Name>Permissions.All)` (policies and the
role permission catalog come from there), add it to `Host_wires_the_known_modules`. The boundary tests pick it up.
Inventory must follow `docs/INVENTORY_DECISIONS.md`.

## Error contract (for the React client)

| Status | When | `errorCode` examples |
|---|---|---|
| 400 | input shape (`errors[]` lists `{ property, code, message }`, property in camelCase) | `Validation.Failed`, `Common.BadRequest` |
| 401 | no valid session; sign-in refused (same code for every reason); refresh token ended | `Common.Unauthorized`, `Identity.Login.Failed`, `Identity.Session.Revoked`, `Identity.Session.RefreshTokenReused` |
| 403 | missing permission, out-of-scope warehouse, anti-forgery token missing, granting beyond own rights, plain HTTP sign-in | `Common.Forbidden`, `Organization.Warehouse.AccessDenied`, `Identity.Session.CsrfTokenInvalid`, `Identity.Access.ExceedsOwnPermissions`, `Identity.Session.HttpsRequired` |
| 404 | the resource in the URL does not exist | `Organization.Warehouse.NotFound`, `Catalog.Product.NotFound` |
| 409 | taken key, stale row version | `Catalog.Product.SkuTaken`, `Organization.Warehouse.CodeTaken`, `Common.ConcurrencyConflict` |
| 422 | a business rule, or a referenced entity missing/inactive | `Organization.Warehouse.TransitCannotSell`, `Organization.Branch.NotActive` |
| 500 | anything unexpected | `Common.Unexpected` |

Lists return `{ items, page, pageSize, totalCount, totalPages }`; enums travel as names; updates send back the
`rowVersion` they read. OpenAPI: `/openapi/v1.json` in Development.
