# WarehouseERP — Project Instructions

> Read this file before making changes. This is a repository-level guardrail, not a request to rewrite the project.
> If a rule conflicts with real, verified code or an existing ADR, report the conflict and obtain a decision before changing core behavior.

## Product scope
- One company, multiple branches, warehouses and locations. **Trading and distribution only. No manufacturing module or manufacturing code.**
- Future business modules: Inventory, Purchasing, Sales, POS, Accounting. Implement only the currently requested scope.
- Backend: ASP.NET Core / EF Core 10 / SQL Server; frontend later: React + TypeScript + RTK Query.

## Architecture boundaries
- **Modular Monolith + Clean Architecture + vertical slices**. Keep Organization, Catalog and future modules independently understandable.
- Prefer the existing project layout and refactor incrementally. Never rebuild the solution merely to introduce a pattern.
- Each module owns its Domain, Application, Infrastructure and Presentation logic; cross-module access occurs only through small, explicit `*.Contracts` abstractions.
- Domain must not reference EF Core, ASP.NET Core, Infrastructure, UI or other modules' implementations.
- Application must not reference HTTP types, concrete DbContexts, Serilog or Infrastructure; use purpose-specific ports where required.
- Infrastructure implements Application ports. API Host is the composition root. Depend on `ILogger<T>` in modules.
- Keep **one `ErpDbContext` and one SQL Server database in V1**, with module-owned schema/configurations. This does NOT authorize direct cross-module entity access.
- Maintain Architecture Tests to enforce dependency rules, including new modules. Avoid circular references and shared business logic in the SharedKernel.

## Preferred application patterns
- Application code follows the **Application Layer Organization & Vertical Slice Conventions** section below. Async + `CancellationToken` everywhere.
- `Result`/`Result<T>` for *expected* domain and application errors, with stable error codes; use exceptions for truly unexpected failures. Map errors consistently to RFC 7807-style ProblemDetails at the HTTP boundary.
- FluentValidation through explicit async validation or an Application pipeline, not obsolete MVC auto-validation. Domain invariants and database constraints remain authoritative.
- Strongly typed Options and `ValidateOnStart()` for required application configuration. Do not use appsettings as a substitute for editable operational policies.
- Serilog is configured at the Host only; structured `ILogger<T>` in other assemblies. Never log credentials, tokens, connection strings or sensitive payloads.
- `IExceptionHandler` + ProblemDetails in Host; distinguish validation/conflict/authorization from 500 errors.
- CQRS as separate reads/writes; do not add an unnecessary mediator, event bus, generic repository, or microservice.
- Outbox/Inbox for reliable integration when needed. Distinguish Domain Events from Integration Events and enforce idempotency.

## Document approvals (all modules, permanent owner decision 2026-10-28)
- **Document self-approval is allowed throughout WarehouseERP. Approval authorization is determined by granted
  permissions, applicable access scopes and valid document state, not by whether the approver created the document.**
- Never compare the approver with the creator or submitter, and never add `SelfApproval`-style errors, job-title or
  role-name checks, or "administrators only" rules. A user without the approval permission still gets 403; warehouse or
  branch scope still applies.
- Whether a document *requires* approval (configurable policies) is separate from *who* may approve it. Keep approval
  states, `CreatedBy`/`SubmittedBy`/`ApprovedBy`, approval timestamps and the action history: the same user id in
  creator and approver is legitimate. Every approval is recorded under the real authenticated user.
- Sales, POS, Accounting and every future module follow this rule.

## Application Layer Organization & Vertical Slice Conventions
Permanent standard for every module (Identity, Organization, Catalog, Inventory, Purchasing, Sales, POS, Accounting).
Full rules, the "where each type belongs" table and the enforcing tests: `docs/ARCHITECTURE.md` → *Application
layer organization*; reasons and alternatives: `docs/adr/0011-application-layer-organization.md` (refines ADR 0010).

```text
<Module>.Application/
├── Features/<BusinessArea>/<UseCase>/      e.g. Features/Products/CreateProduct/
│     CreateProductCommand.cs  CreateProductHandler.cs  CreateProductValidator.cs  CreateProductResponse.cs
├── Contracts/<BusinessArea>/               models reused by several use cases (or returned by a port): ProductSummaryResponse.cs
├── Abstractions/Interfaces/<Group>/        one interface per file: Products/, Users/, Sessions/, Persistence/ ...
├── Errors/                                 <Area>Errors.cs: ProductErrors, UserErrors, SessionErrors ...
└── Shared/                                 rare helpers used by several areas
```

- **Use cases**: grouped by business area, one folder per use case holding its Command or Query, Handler, Validator and
  the models only it uses. Never global Commands/Queries/Handlers/Validators folders, never CRUD services.
- **Contracts**: a model used by one use case stays in its folder. Move it to `Application/Contracts/<Area>/` only on
  genuine reuse (several use cases, or a port returns it). No `Contracts/DTOs`, no mandatory `Requests`/`Responses`
  subfolders, no automatic `Dto` suffix: name models `...Request`/`...Response` by meaning; a name alone does not make a
  type HTTP-specific. `Application/Contracts` is internal to the module; other modules use only the
  `<Module>.Contracts` assembly.
- **No duplicate models**: commands and queries are the use-case contract. Write a Presentation request record
  (`Presentation/Contracts/<Area>/`) only when HTTP genuinely differs (route id, server-set field, lenient optional
  list); otherwise the endpoint binds the command. Standard HTTP bodies `CreatedResponse`/`UpdatedResponse` live in
  BuildingBlocks.Infrastructure.
- **Interfaces** in `Abstractions/Interfaces/<Group>/`, one per file; segregate when a consumer needs only part (e.g.
  `IUserAccounts` / `IUserCredentials` / `IUserRoleAssignments`), not into tiny pieces. Implementations live in the
  module's Infrastructure. Application never references EF Core, SQL Server or Infrastructure.
- **Errors** by business area in `Application/Errors/<Area>Errors.cs`; domain invariant errors stay in the Domain as
  `<Entity>DomainErrors`. Error codes never change. `Result`/`Error` stay in BuildingBlocks.
- **API contract**: like error codes, OpenAPI paths, schema names, operation ids, tags, JSON property names and enum
  members are public API. `docs/api/openapi-v1.json` is the committed contract (checked by `HostTests`); change it
  only deliberately (`ERP_UPDATE_OPENAPI_SNAPSHOT=1`) and review its diff. Schema names are the CLR type names
  (`OpenApiSetup` publishes a body-bound command as `...Request`), so renaming a request or response type is a
  contract change.
- **Files**: one top-level type per file, file named after it, namespace = folder (IDE0130 is an error in modules).
  Domain enums stay in Domain; helpers sit next to their owner (use case, area, or `Shared/` when several areas use them).
- **Before writing a feature**: owning module → business area → `Features/<Area>/<UseCase>/` → its types beside it →
  reuse `Contracts/<Area>` only where real → interfaces and errors by responsibility → tests → docs/ADR when a new
  decision is made. A feature that needs a different structure documents why (and, for a multi-type file, an entry in
  `ApplicationLayoutTests.MultiTypeFileExceptions`) instead of silently breaking the convention.

## Database and operational safety
- Never change Migration 01, schemas, temporal tables, indexes, or historical data just to make architecture refactoring pass.
- Keep SQL Server-specific behavior tested against SQL Server, not assumed from an in-memory provider.
- No unsafe destructive migration, DB reset, force push, history rewrite or production deployment without explicit permission.
- Master data uses SQL Server Temporal Tables. Operational actions use AuditLog; avoid duplicating history unnecessarily.
- Avoid duplicated sources of truth. Prefer exact decimal arithmetic; unit prices and average unit cost use `decimal(19,8)`.
- Review `docs/INVENTORY_DECISIONS.md` before any inventory-related design or implementation.

## Definition of done (every meaningful change)
1. Inspect actual files and understand callers before editing; summarize the planned impact.
2. Preserve existing behavior and public contracts unless a breaking change is explicitly approved.
3. Implement the feature end-to-end; no unused abstractions or pattern-only scaffolding.
4. Add focused unit and integration tests; update Architecture Tests when new projects/modules are added.
5. Run the relevant build/tests when tools are available; report exact commands, pass/fail/skipped, and blockers honestly.
6. Check the SQL model snapshot/migration diff if EF mappings changed; explain every schema change.
7. Update README/ARCHITECTURE.md/ADR only when decisions or developer workflows changed.
8. Before finishing, report files changed, reason for changes, test results and remaining risks.

## Review priorities
Correctness and data integrity > modular boundaries > maintainability > security > observability > performance optimization > pattern completeness.
Keep solutions small and readable. No speculative infrastructure or premature abstractions.
