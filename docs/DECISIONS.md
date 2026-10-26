# Locked design decisions — 2026-10-26

- **Business**: one company, many branches and warehouses; **trading and distribution only**. No manufacturing module/entities.
- **Application**: ASP.NET Core / React, Modular Monolith, single SQL Server database, single `ErpDbContext`, per-module schemas.
- **Module layout**: one project per module plus a `.Contracts` project (`src/Modules/<Name>/`). A module references only the shared kernel and Contracts; it owns its entity configurations and uses `DbContext.Set<T>()`. `Persistence` composes every module into `ErpDbContext` and holds cross-module foreign keys. `WarehouseERP.ArchitectureTests` enforces the references.
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
