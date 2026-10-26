# Locked design decisions — 2026-10-26

- **Business**: one company, many branches and warehouses; **trading and distribution only**. No manufacturing module/entities.
- **Application**: ASP.NET Core / React, Modular Monolith, single SQL Server database, single `ErpDbContext`, per-module schemas.
- **Module layout**: four projects per module (`.Domain`, `.Application`, the outer project with Infrastructure + Presentation, `.Contracts`); modules meet only through Contracts; `Persistence` composes every module into `ErpDbContext` and holds cross-module foreign keys. Details and enforcement: `ARCHITECTURE.md`, ADR 0001.
- **Application patterns**: Result + stable error codes (ADR 0003), handlers with validation/logging decorators and no mediator (ADR 0002), use cases laid out as `Features/<BusinessArea>/<UseCase>/` vertical slices, contracts moved to `Application/Contracts/<Area>` only on reuse, interfaces and errors by business area (ADR 0010, ADR 0011), repository write methods as save points with known-constraint mapping (ADR 0004), `ICurrentUser` with permission policies and warehouse scope (ADR 0005), Serilog in the host with one log per failure (ADR 0006), validated options (ADR 0007), outbox/inbox/idempotency designed for Inventory (ADR 0008).
- **Identity & Access** (ADR 0009): server-side sessions (HttpOnly cookie + anti-forgery for React, bearer + rotating refresh token for POS/API, no external JWT); global roles carry permissions, per-user scope is company-wide / branches (covering later warehouses) / warehouses; Identity owns scope, Organization owns structure, cross-module FKs in Persistence; `ErpDbContext` derives from `IdentityDbContext` with tables in `identity`; first administrator from `Identity:Bootstrap` while no user exists. Nobody grants or manages more than they hold.
- **Master audit**: SQL Server temporal tables for all master data; operational business events go to `AuditLog` in a later migration.
- **Location freezing**: active StockCountScope only; no WarehouseLocation.IsFrozen.
- **Base unit**: Product.BaseUnitId only; matching ProductUnit row with factor 1, no IsBaseUnit.
- **Valuation**: cost flows stored in InventoryCostEntry only, not StockMovement. Unit-price precision decimal(19,8).
- **Reserve/stock**: ProductWarehouseStock holds OnHandSellable and ReservedQuantity. No SQL CHECK Reserved <= OnHand. Conditional reserve/direct issue update protects availability; damage, loss and counting adjustment may cause OverReserved alert.
- **Sellability**: Transit warehouses excluded unconditionally; reconciliation checks current stock summaries against sellable rows only, using a consistent database snapshot.
- **Stock count**: Begin takes per-warehouse `sp_getapplock` Exclusive, all postings take Shared using transaction-owned locks, and scope state persists after transaction commit.
- **Transfers**: Transit location per transfer; close-with-discrepancy posts a loss/damage adjustment from its Transit location.
- **Document approval**: policy per DocumentType; externally generated docs may post directly from Draft subject to permissions and validation.
- **Costing**: company-level moving average pending accountant validation, issue decreases quantity and value (average unit cost unchanged unless rounding).
- **V1 period**: posting at current business date; offline POS processes later with sync exception handling.
- **Migration 01**: only Organization and Catalog tables; migration generated locally with dotnet ef, not manually fabricated.
