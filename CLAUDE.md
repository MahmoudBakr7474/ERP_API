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
- Application layout = **business area + vertical slice**: `Features/<BusinessArea>/<UseCase>/` (e.g. `Catalog.Application/Features/Products/CreateProduct/`, `Features/Categories/CreateCategory/`). Each use case folder holds `Command.cs` or `Query.cs` (the request and its own DTOs), `Handler.cs` and `Validator.cs` (when input needs validation), named `<UseCase>Command|Query|Handler|Validator`, namespace = folder path. Use cases never reference each other and areas never reference each other; types shared by an area's use cases sit directly in `Features/<Area>/`, port read models in `Abstractions/`, module-wide helpers in `Shared/`. No CRUD service classes. Enforced by `FeatureConventionTests`; details in `docs/ARCHITECTURE.md` and ADR 0010. Async + `CancellationToken`.
- `Result`/`Result<T>` for *expected* domain and application errors, with stable error codes; use exceptions for truly unexpected failures. Map errors consistently to RFC 7807-style ProblemDetails at the HTTP boundary.
- FluentValidation through explicit async validation or an Application pipeline, not obsolete MVC auto-validation. Domain invariants and database constraints remain authoritative.
- Strongly typed Options and `ValidateOnStart()` for required application configuration. Do not use appsettings as a substitute for editable operational policies.
- Serilog is configured at the Host only; structured `ILogger<T>` in other assemblies. Never log credentials, tokens, connection strings or sensitive payloads.
- `IExceptionHandler` + ProblemDetails in Host; distinguish validation/conflict/authorization from 500 errors.
- CQRS as separate reads/writes; do not add an unnecessary mediator, event bus, generic repository, or microservice.
- Outbox/Inbox for reliable integration when needed. Distinguish Domain Events from Integration Events and enforce idempotency.

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
