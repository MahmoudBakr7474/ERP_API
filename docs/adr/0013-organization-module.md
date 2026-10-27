# ADR 0013 — Organization module: company setup, structure changes, scope and status rules

Status: accepted (2026-10-27), on `feature/organization`. Implemented and tested. The product owner approved every
decision below for V1 on 2026-10-27.

## Context
Migration01 already had `Company`, `Branch`, `Warehouse` and `WarehouseLocation` (temporal tables) and the API could
only create, read and list warehouses. The Organization module needed company settings, branch management, warehouse
changes, warehouse locations and active/inactive rules, without a schema change.

## Decision
1. **One company, set up once.** `POST /api/organization/company` creates it on first run and answers 409
   `Organization.Company.AlreadyConfigured` afterwards. The check holds `UPDLOCK, HOLDLOCK` until the insert commits,
   so two setups at the same time cannot both succeed. `GET` answers 404 `Organization.Company.NotConfigured` until
   then, and creating a branch answers 422 `Organization.Company.SetupRequired`. Setup needs
   `organization.company.update` and company-wide access. On a fresh installation only the bootstrap administrator
   (`Identity:Bootstrap`: Administrator role, company-wide) has both.
2. **Company code and base currency are fixed after setup.** Names, time zone (IANA or Windows id known to the host)
   and tax registration number can change. Documents, balances and costs will be kept in the base currency.
3. **Any signed-in user reads the company.** Screens and documents need its name, currency and time zone; no extra
   permission is needed.
4. **Changing the structure needs company-wide access.** Company setup and settings, creating, renaming, activating
   and deactivating branches, and creating warehouses answer 403 `Organization.Company.CompanyWideAccessRequired`
   for a user scoped to branches or warehouses. **This also tightens the existing `POST /warehouses`**, which checked
   the permission but not the scope. A new warehouse would fall outside a narrower scope anyway.
5. **Warehouse-level changes follow the warehouse scope.** Updating, activating and deactivating a warehouse, and
   reading and changing its locations, need the warehouse in scope. Scope is checked before existence, as for reads
   (403 `Organization.Warehouse.AccessDenied`).
6. **Branch reads are scoped through warehouses.** A company-wide user sees every branch. Anyone else sees the
   branches that hold one of their warehouses, which includes every warehouse of a granted branch. An out-of-scope
   branch answers 403 `Organization.Branch.AccessDenied` before the existence check.
7. **Codes are fixed once created** (branch, warehouse, location). A warehouse's type and branch are fixed too. Only
   names, and a warehouse's `allowsSelling`, change. A Transit warehouse still never sells.
8. **Status rules.**
   * A branch can't be deactivated while it has active warehouses (422 `Organization.Branch.HasActiveWarehouses`).
   * A warehouse can't be activated in an inactive branch (422 `Organization.Branch.NotActive`).
   * So an inactive branch never holds an active warehouse.
   * An inactive warehouse gets no new locations (422 `Organization.Warehouse.NotActive`).
   * The `DEFAULT` location can't be deactivated (422 `Organization.WarehouseLocation.DefaultMustStayActive`).
9. **Optimistic concurrency on every update.** Company, branch, warehouse and location reads return a base64
   `rowVersion`. The PUT sends it back and gets the new one; a stale one is 409 `Common.ConcurrencyConflict`.
   Activate/deactivate take no row version (204).
10. **New permissions:**
    * `organization.company.update`
    * `organization.branches.read`, `organization.branches.create`, `organization.branches.update`
    * `organization.warehouses.update`
    * `organization.locations.create`, `organization.locations.update`

    Location reads use `organization.warehouses.read`. All are in `OrganizationPermissions.All`, so Identity roles
    can grant them.

## Consequences
* No migration: `has-pending-model-changes` reports none. The OpenAPI contract changed only additively:
  * new paths and schemas;
  * `rowVersion` on `WarehouseDetailsResponse` and `WarehouseLocationResponse`;
  * `search`/`isActive` on `ListWarehouses`;
  * 403 on `CreateWarehouse`.
* **Inventory integration requirement.** Organization does not yet know about stock, so today it can deactivate a
  warehouse or location that holds stock. Before stock operations go live, Inventory must make deactivation refuse a
  warehouse or location that holds stock. The requirement is also recorded in `docs/INVENTORY_DECISIONS.md`.

## Not done (later, if needed)
* Changing which location is a warehouse's default. The filtered unique index allows one default, and swapping it
  needs an ordered two-step save.
* A database check that the default location stays active. Today the domain enforces it.
* Moving a warehouse to another branch.
