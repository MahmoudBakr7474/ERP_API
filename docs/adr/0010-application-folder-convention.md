# ADR 0010 — Application layout: business area, then one vertical slice per use case

Status: accepted (2026-10-26). Refines the feature-folder layout of ADR 0002. Extended by ADR 0011 (contracts on
reuse, interfaces and errors by area, one type per file): use case files are now named after their type
(`CreateProductCommand.cs`, not `Command.cs`) and `ApiTokens` became `Contracts/Authentication/ApiTokensResponse`.

## Context
Use cases sat directly under `Application/Features/<UseCase>/`. Identity already has 24 of them and Catalog,
Organization and the coming modules will each have dozens; a flat list hides which use cases belong to which business
entity, and invites "service" classes that gather an entity's CRUD.

## Decision
* Every module's Application layer groups use cases by **business area** (an entity or a functional area), then one
  folder per **use case**: `Features/<BusinessArea>/<UseCase>/`, namespace = folder path. Example:
  `Catalog.Application/Features/Products/CreateProduct/`, `Features/Categories/CreateCategory/`.
* A use case folder holds `Command.cs` or `Query.cs` (the request plus DTOs only it returns), `Handler.cs`, and
  `Validator.cs` when the input needs validation; types are named `<UseCase>Command|Query|Handler|Validator`.
* A use case never references another use case; an area never references another area. Types shared by the use
  cases of one area sit directly in `Features/<Area>/` and must not depend on any of its use cases. Read models a port
  returns stay with the port in `Abstractions/`; module-wide helpers live in `Shared/`.
* No CRUD service classes; no new abstraction is introduced for the layout.
* The layout is enforced by `FeatureConventionTests` (namespace depth, handler/validator/request placement and names,
  and a dependency check read from the compiled IL with Mono.Cecil so async state machines and lambdas are covered).

Areas applied to the existing code (files moved, behaviour unchanged): Organization `Warehouses`; Catalog `Products`;
Identity `Authentication`, `Account`, `Users`, `Roles`, `Permissions`, `Sessions`. The two token types shared by
`IssueApiToken` and `RefreshApiToken` moved to `Features/Authentication/ApiTokens.cs`, and the Identity helper
`Sessions` was renamed `SessionRevocation` (its name clashed with the `Sessions` area namespace).

## Consequences
* Namespaces changed; type names, database schema, endpoints and JSON contracts did not (checked by comparing the
  OpenAPI document before and after, and `has-pending-model-changes`).
* A new module or feature follows the same layout from its first commit; the architecture tests pick it up.
* When two use cases want the same logic, the choice is explicit: an area-level type, a port, or a `Shared/` helper,
  never a call from one handler into another.
