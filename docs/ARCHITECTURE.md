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
    WarehouseERP.<Module>.Application/      Features/<BusinessArea>/<UseCase>/{Command|Query, Handler, Validator},
                                            Abstractions/ (ports), Shared/, errors
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
| Application features live in `Features/<Area>` or `Features/<Area>/<UseCase>`, nothing deeper or directly under `Features` | `Feature_types_live_in_a_business_area_or_one_of_its_use_cases` |
| Each handler and validator sits in its own use case folder, named `<UseCase>Handler`/`<UseCase>Validator`, with its `<UseCase>Command`/`<UseCase>Query` beside it | `Each_handler_validator_and_request_sits_in_its_own_use_case_folder` |
| Use cases don't reference other use cases, areas don't reference other areas, area-shared types don't reference use cases (checked on the IL, async/lambda code included) | `Use_cases_and_areas_do_not_reference_each_other` |

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

## Application folder convention (ADR 0010)

Business area first, then one vertical slice per use case:

```
WarehouseERP.Catalog.Application/
  Abstractions/                 ports and the read models they return (IProductRepository, ProductDetails, ...)
  Shared/                       module-wide helpers used by several areas (rare)
  Features/
    Products/                   business area: an entity or a functional area, plural noun or area name
      CreateProduct/            one use case = one folder = one namespace
        Command.cs              CreateProductCommand (+ response DTOs only this use case returns)
        Handler.cs              CreateProductHandler
        Validator.cs            CreateProductValidator (when the input needs validation)
      GetProduct/  ListProducts/  UpdateProduct/
    Categories/CreateCategory/  Units/CreateUnit/  ...
```

* Areas today: Organization `Warehouses`; Catalog `Products`; Identity `Authentication`, `Account` (the signed-in
  user's own profile and password), `Users`, `Roles`, `Permissions`, `Sessions`.
* A use case never references another use case, and an area never references another area. What several use cases
  of one area share sits directly in `Features/<Area>/` (e.g. `Identity…Features/Authentication/ApiTokens.cs`, used by
  `IssueApiToken` and `RefreshApiToken`); what several areas share is a port in `Abstractions/` or a helper in
  `Shared/`. There are no service classes that gather an entity's CRUD.
* Presentation mirrors the areas: one endpoints file per area is the norm (`Presentation/<Area>Endpoints.cs`).

## Adding a feature

1. `Application/Features/<Area>/<UseCase>/Command.cs` (or `Query.cs`), `Handler.cs`, `Validator.cs`; a new area is
   just a new folder.
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
