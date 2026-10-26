# ADR 0001 — Modular monolith: four projects per module, one ErpDbContext

Status: accepted (2026-10-26)

## Context
Organization and Catalog exist; Inventory, Purchasing, Sales, POS and Accounting will follow. One developer.
Rules: Domain free of EF Core/ASP.NET Core; Application free of HTTP, concrete DbContexts and Serilog; modules meet
only through Contracts; one database and one `ErpDbContext` in V1. `CLAUDE.md` asks to refactor the existing
layout incrementally instead of rebuilding it.

## Decision
Each module has four projects:

| Project | Holds | Boundary enforced by |
|---|---|---|
| `WarehouseERP.<M>.Domain` | entities, invariants, domain errors | compiler (no EF/ASP.NET reference) |
| `WarehouseERP.<M>.Application` | use cases (vertical slices), ports | compiler (no EF/ASP.NET/Serilog reference) |
| `WarehouseERP.<M>` (existing project) | `Infrastructure/` + `Presentation/` + DI | architecture test (Presentation never uses EF Core/Infrastructure/Domain) |
| `WarehouseERP.<M>.Contracts` | read interfaces, enums, permission names | compiler (no references) |

`ErpDbContext` lives in `WarehouseERP.Persistence`, applies every module's configurations and is registered as
`DbContext`; module repositories depend on `DbContext` and never reference `Persistence`. Each entity lives in its
module's schema. Cross-module foreign keys are configured in `ErpDbContext`, the only place that sees every module.

## Alternatives
* Five projects per module (separate Presentation): compiler-enforces one more edge but adds a project per module
  (35+ projects with the planned modules) for an edge a test enforces well.
* Folders only in one project per module: Domain and Application boundaries would rest on tests alone.
* DbContext per module: every cross-module posting (sale = stock + accounting) would need shared connections and
  transactions; rejected for V1.

## Consequences
Two new projects per module. The architecture tests discover modules from the host, so a new module is checked
without editing them. A single DbContext makes cross-module entity access *technically* possible through
`Set<T>()`; the compiler prevents it (no project reference to another module's Domain) and the ownership tests
catch a misconfigured entity.
