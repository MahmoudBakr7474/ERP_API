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
    WarehouseERP.<Module>.Domain/           entities, value objects, enums, invariants, <Entity>DomainErrors
    WarehouseERP.<Module>.Application/      Features/<Area>/<UseCase>/, Contracts/<Area>/, Abstractions/Interfaces/<Group>/,
                                            Errors/, Shared/ (see "Application layer organization")
    WarehouseERP.<Module>/                  Infrastructure/ (Persistence/Configurations/, repositories and read stores,
                                            contract implementations) + Presentation/ (endpoints, Contracts/<Area>/
                                            HTTP-only requests) + <Module>Module (DI)
    WarehouseERP.<Module>.Contracts/        what other modules may use: read interfaces, enums, permission names
tests/
  WarehouseERP.UnitTests/                   Result, mapping, pipeline, domain rules, validators, handlers (fakes)
  WarehouseERP.IntegrationTests/            SQL Server (LocalDB) + the real HTTP host
  WarehouseERP.ArchitectureTests/           the dependency rules below
```

Modules today: **Organization** (company, branch, warehouse, location, and the address lookups country → governorate →
city, ADR 0017), **Catalog** (category, unit, product,
product unit, barcode, external item code, tax code/rate/assignment), **Identity** (users, roles and their
permissions, user scope, sessions; authenticates every request and provides `ICurrentUser` to all modules, ADR 0009)
**Inventory** (stock documents and their posting, balances, availability, the movement ledger, the
company-wide valuation, transfers through transit warehouses, stock counts and the reconciliation report, ADR 0015)
**Purchasing** (suppliers, purchase requests and orders, goods receipts, supplier invoices and returns; it moves
stock only through Inventory's `IPurchaseStockPosting`, ADR 0016) and **Sales** (customers, the price list,
quotations, orders, deliveries, invoices and customer returns; it reserves and moves stock only through Inventory's
`IStockReservations` and `ISalesStockPosting`, ADR 0018) and **POS** (terminals, cashier shifts and their cash, checkout,
receipts, returns and refunds; it prices through Sales' `ISalesPricing`, names customers through `ICustomerDirectory` and
moves only unreserved stock through Inventory's `IPosStockPosting`, ADR 0019).

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
| Every entity lives in its module's schema (`organization.*`, `catalog.*`, `identity.*`, `inventory.*`) | `Every_entity_lives_in_its_modules_schema` |
| Only `WarehouseERP.Identity` and `Persistence` reference ASP.NET Core Identity | `Only_identity_and_persistence_reference_aspnet_core_identity` |
| Contracts and the shared kernel reference nothing | `Contracts_are_plain_types_with_no_references`, `Shared_kernel_references_no_project_and_no_framework_package` |
| Only the host and Persistence reference module outer projects; nothing references the host | `Only_the_host_and_persistence_reference_module_outer_assemblies`, `Nothing_references_the_host` |
| No circular references | `Project_assemblies_form_no_cycle` |
| New modules are discovered from the host and checked automatically | `Host_wires_the_known_modules` (update the expected list when adding one) |
| Application features live in `Features/<Area>` or `Features/<Area>/<UseCase>`, nothing deeper or directly under `Features` | `Feature_types_live_in_a_business_area_or_one_of_its_use_cases` |
| Each handler and validator sits in its own use case folder, named `<UseCase>Handler`/`<UseCase>Validator`, with its `<UseCase>Command`/`<UseCase>Query` beside it | `Each_handler_validator_and_request_sits_in_its_own_use_case_folder` |
| Use cases don't reference other use cases, areas don't reference other areas, area-shared types don't reference use cases (checked on the IL, async/lambda code included) | `Use_cases_and_areas_do_not_reference_each_other` |
| Application errors live in `Errors/` in `<Area>Errors` classes; domain errors in `<Entity>DomainErrors` | `Application_errors_are_grouped_by_business_area_in_Errors`, `Domain_errors_are_named_after_their_entity` |
| Application interfaces live in `Abstractions/Interfaces/<Group>/` | `Application_interfaces_live_in_Abstractions_Interfaces_grouped_by_responsibility` |
| Reused application contracts live in `Contracts/<Area>/`, one level, no `DTOs`/`Requests`/`Responses` folders | `Reused_application_contracts_are_grouped_by_business_area` |
| HTTP-only request records live in `Presentation/Contracts/<Area>/` | `Http_request_records_live_in_Presentation_Contracts_by_area` |
| One top-level type per module source file, named after it (documented exceptions only) | `Every_module_source_file_declares_one_top_level_type_named_like_the_file` |
| Namespaces match folders in every module | IDE0130 as a build error (`.editorconfig`, `[src/Modules/**.cs]`) |

## A request, end to end

```
HTTP ─► ForwardedHeaders (only with ReverseProxy enabled) ─► HSTS (outside Development) ─► CorrelationIdMiddleware
    ─► Serilog request logging ─► ExceptionHandler ─► StatusCodePages ─► RateLimiter (credential endpoints)
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
  one database transaction). Known unique-index violations become business errors there (ADR 0004). A use case that
  must also take locks or run raw SQL in the same unit (an Inventory posting, a deactivation that asks Inventory) runs
  inside `ITransactionRunner`, which retries the whole unit on a deadlock (ADR 0015).
* Handlers see the user through `ICurrentUser`; warehouse scope is checked in the handler, permissions by
  endpoint policies (ADR 0005). The Identity module authenticates every request against its sessions (ADR 0009).
* Security-sensitive Identity changes add a row to `identity.SecurityEvents` in the same save (ADR 0012). Two writes
  are deliberately outside the save point: a session's last-seen time, and the failed-attempt count, lockout or reset
  of a checked password. They are atomic `UPDATE`s that commit first; the count's also renews the user's
  `ConcurrencyStamp`, so a user row loaded before it can't be saved over it. ADR 0012 describes what a login leaves
  behind when its save fails.

## Application layer organization (ADR 0010, ADR 0011)

Business area first, then one vertical slice per use case; a few folders for what is genuinely shared:

```text
WarehouseERP.Identity.Application/
├── Features/                         Authentication/, Account/, Users/, Roles/, Permissions/, Sessions/, SecurityEvents/
│   ├── Authentication/
│   │   ├── Login/                    LoginCommand.cs, LoginHandler.cs, LoginValidator.cs, LoginResponse.cs
│   │   ├── IssueApiToken/  RefreshApiToken/  Logout/
│   │   └── ApiTokenIssuer.cs, Credentials.cs, IssuedPair.cs    (shared by this area's use cases only)
│   ├── Account/GetMe/                GetMeQuery.cs, GetMeHandler.cs, MeResponse.cs
│   └── Users/CreateUser/ ...  Users/AdministratorGuard.cs
├── Contracts/                        reused by several use cases or returned by a port
│   ├── Users/                        UserDetailsResponse.cs, UserSummaryResponse.cs, UserRoleResponse.cs
│   ├── Authentication/               ApiTokensResponse.cs (IssueApiToken + RefreshApiToken)
│   └── Roles/  Sessions/  UserAccess/  SecurityEvents/
├── Abstractions/Interfaces/          one interface (or port model) per file
│   ├── Users/                        IUserAccounts, IUserReadStore, UserAccount, NewUserAccount
│   ├── Authentication/               IUserCredentials, IPasswordPolicy, LoginCheck, LoginOutcome
│   ├── Roles/                        IRoleRepository, IUserRoleAssignments
│   ├── SecurityEvents/               ISecurityEventLog (write, in the use case's save), ISecurityEventReadStore, SecurityEventFilter
│   └── Sessions/  UserAccess/  Persistence/ (IIdentityPersistence)
├── Errors/                           AuthenticationErrors, UserErrors, RoleErrors, PermissionErrors, UserAccessErrors, SessionErrors
├── Shared/                           IdentityRules, PrivilegeGuard, SecurityAudit, SessionRevocation (used by several areas)
└── IdentitySessionOptions.cs         the module's options
```

Organization: `Features/{Companies,Branches,Warehouses,WarehouseLocations}`, `Errors/{Company,Branch,Warehouse,WarehouseLocation,RowVersion}Errors`.
Catalog: `Features/{Units,Categories,Products,ProductUnits,ProductBarcodes}`, `Contracts/{Units,Categories,Products}`,
`Abstractions/Interfaces/{Units,Categories,Products}`, `Errors/{Unit,Category,Product,ProductUnit,ProductBarcode,RowVersion}Errors`.
Inventory: `Features/{StockDocuments,Stock,Valuation,DocumentPolicies,Transfers,Counts,Reconciliation}` (the posting
transaction is `Shared/StockDocumentPosting`, used by documents, transfers and counts),
`Abstractions/Interfaces/{StockDocuments,Posting,Stock,Valuation,DocumentPolicies,Transfers,Counts,Reconciliation}`,
`Errors/{StockDocument,Stock,Idempotency,Valuation,DocumentPolicy,StockTransfer,StockCount,Reconciliation,RowVersion}Errors`.
Purchasing: `Features/{Suppliers,PurchasingPolicies,PurchaseRequests,PurchaseOrders,GoodsReceipts,SupplierInvoices,SupplierReturns}`
(numbering, references, idempotency and scope in `Shared/`), `Abstractions/Interfaces/{Suppliers,DocumentPolicies,PurchaseRequests,
PurchaseOrders,GoodsReceipts,SupplierInvoices,SupplierReturns,Posting}`, `Errors/{Supplier,PurchaseRequest,PurchaseOrder,GoodsReceipt,
SupplierInvoice,SupplierReturn,DocumentPolicy,Reference,Access,StockPosting,Idempotency,RowVersion}Errors`.
Sales: `Features/{Customers,Prices,Settings,Quotations,Orders,Deliveries,Invoices,Returns}` (numbering, references, line
pricing, idempotency and scope in `Shared/`), `Abstractions/Interfaces/{Customers,Prices,Settings,Quotations,Orders,
Deliveries,Invoices,Returns,Posting}`, `Errors/{Customer,Price,Quotation,Order,Delivery,Invoice,Return,Reference,Access,
Stock,Idempotency,RowVersion}Errors`; product tax classifications in `Features/ProductTaxes`, `ISalesPricing` in
`Features/Prices/SalesPricing`.
POS: `Features/{Settings,Terminals,Shifts,Items,Customers,Sales,Returns,CardIncidents}` (cart pricing in `Features/Sales`,
item descriptions in `Features/Items`; references, till access, numbering, idempotency, request rules in `Shared/`),
`Abstractions/Interfaces/{Settings,Terminals,Shifts,Sales,Returns,CardIncidents,Posting}`,
`Errors/{Access,Terminal,Shift,Sale,Item,Return,CardIncident,Idempotency,RowVersion,Reference,Pricing,Stock}Errors`.

### Where each type belongs

| Type | Place | Name |
|---|---|---|
| Entity, value object, domain enum | `<Module>.Domain/` | `Product`, `WarehouseType` |
| Domain invariant error | `<Module>.Domain/` | `<Entity>DomainErrors` (`ProductDomainErrors.TrackingNeedsStockItem`) |
| Command / Query | `Application/Features/<Area>/<UseCase>/` | `<UseCase>Command` / `<UseCase>Query` |
| Handler / Validator | same folder | `<UseCase>Handler` / `<UseCase>Validator` |
| Model only one use case returns or takes | same folder | `<UseCase>Response` or a meaningful name (`MeResponse`, `RoleDetailsResponse`) |
| Model reused by several use cases or returned by a port | `Application/Contracts/<Area>/` | `...Response` / `...Request` by meaning, never an automatic `Dto` suffix |
| Helper used by one use case / one area / several areas | its use case folder / `Features/<Area>/` / `Shared/` | meaningful (`RowVersions`, `Credentials`, `PrivilegeGuard`) |
| Interface (port) and the models in its signature | `Application/Abstractions/Interfaces/<Group>/` | `IProductRepository`, `IUserReadStore`, `LoginCheck` |
| Application error | `Application/Errors/` | `<Area>Errors` (`UserErrors.NotFound`); codes are stable |
| Options class | Application root (module-wide) | `IdentitySessionOptions` |
| HTTP request that differs from the command | `<Module>/Presentation/Contracts/<Area>/` | `UpdateUserRequest`; identical ones are not written, the endpoint binds the command |
| Standard HTTP bodies | `BuildingBlocks.Infrastructure/Http/` | `CreatedResponse`, `UpdatedResponse` |
| Endpoints | `<Module>/Presentation/` | `<Area>Endpoints` |
| Repository / read store / port implementation | `<Module>/Infrastructure/Persistence/` (or `Security/`, etc. by concern) | `ProductRepository`, `UserReadStore`, `UserCredentials` |
| EF Core configuration | `<Module>/Infrastructure/Persistence/Configurations/` | `<Entity>Configuration` |
| What other modules may use | `<Module>.Contracts` assembly | `IWarehouseDirectory`, `CatalogPermissions`; never `Application/Contracts` |

Rules worth remembering:
* A use case never references another use case; an area never references another area; area-level types never
  reference a use case.
* A model moves out of its use case only on genuine reuse; there is no `Contracts/DTOs` folder and no mandatory
  `Requests`/`Responses` level. `Request`/`Response` describe a role, not HTTP.
* One top-level type per file, namespace = folder. A justified exception is documented in
  `ApplicationLayoutTests.MultiTypeFileExceptions` and in the PR.

## Adding a feature

1. Find the owning module and business area; create `Application/Features/<Area>/<UseCase>/` with
   `<UseCase>Command.cs` (or `Query.cs`), `<UseCase>Handler.cs`, `<UseCase>Validator.cs` and any model only it uses.
2. A port in `Application/Abstractions/Interfaces/<Group>/` if the handler needs data it can't get yet; implement it
   in `<Module>/Infrastructure`. Writes save once, through the repository method that is the save point. Errors go to
   `Application/Errors/<Area>Errors.cs`; a model reused by several use cases goes to `Application/Contracts/<Area>/`.
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
| 401 | no valid session (or a token over plain HTTP); sign-in refused (same code for every reason); refresh token ended | `Common.Unauthorized`, `Identity.Login.Failed`, `Identity.Session.Revoked`, `Identity.Session.RefreshTokenReused` |
| 403 | missing permission, out-of-scope warehouse, anti-forgery token missing, granting or managing beyond own rights or scope, plain HTTP sign-in | `Common.Forbidden`, `Organization.Warehouse.AccessDenied`, `Identity.Session.CsrfTokenInvalid`, `Identity.Access.ExceedsOwnPermissions`, `Identity.Access.ExceedsOwnScope`, `Identity.Session.HttpsRequired` |
| 404 | the resource in the URL does not exist | `Organization.Warehouse.NotFound`, `Catalog.Product.NotFound` |
| 409 | taken key, stale row version / role `version` / scope `revision`, a user changed since it was loaded (reload and retry) | `Catalog.Product.SkuTaken`, `Organization.Warehouse.CodeTaken`, `Common.ConcurrencyConflict` |
| 422 | a business rule, or a referenced entity missing/inactive | `Organization.Warehouse.TransitCannotSell`, `Organization.Branch.NotActive`, `Identity.Password.LockedOut`, `Identity.User.LastAdministrator` |
| 429 | too many attempts at one credential operation from one client address (`Retry-After` header) | `Common.TooManyRequests` |
| 500 | anything unexpected | `Common.Unexpected` |

Lists return `{ items, page, pageSize, totalCount, totalPages }`; enums travel as names; numbers only as JSON numbers;
Identity times carry `Z`; updates send back the `rowVersion` (or `version`/`revision`) they read.

**The contract is public API.** OpenAPI is served at `/openapi/v1.json` in Development and committed as
`docs/api/openapi-v1.json`; `HostTests.OpenApi_document_matches_the_committed_contract` fails on any difference.
Paths, schema names, operation ids, tags, JSON property names, enum members, status codes and error codes change only
deliberately: regenerate the copy (`ERP_UPDATE_OPENAPI_SNAPSHOT=1`; that run fails on purpose) and review its diff
like code. Schema names are the CLR type names, so renaming any request or response type is a contract change.
`OpenApiSetup` publishes a command bound from a body as `...Request`, fails the document when two types claim one
name, shows query parameters (including `[AsParameters]` queries) in camelCase, and declares the ProblemDetails
extensions (`errorCode`, `traceId`, `correlationId`, `errors[]`).
