# ADR 0022 — Organization operational enhancements: company contact details, reports and the location import

Status: accepted (2026-10-31), on `feature/organization-enhancements`. The owner's brief "Organization Module Operational
Enhancements" asked for the Catalog pattern (ADR 0021) applied to Organization: professional PDFs, XLSX/CSV, reports,
safe location import, without rebuilding the module or changing its approved rules (ADR 0013). This ADR records the
decisions the brief left to the implementation. Details: `docs/modules/Organization/REPORTS_AND_IMPORTS.md`.

## Decisions
1. **Company contact details (the only schema change).** Official documents need the issuer's commercial registration,
   address and contact (Egyptian commercial-register rules; the company profile asks for them), and the company had none.
   `Migration17_CompanyContactDetails` adds six nullable columns, sized and validated like a supplier's, the address naming
   a city of the lookups (ADR 0017). Nothing is required, nothing is defaulted, nothing is invented; the profile PDF leaves
   out what was never recorded. Branch addresses were not added: no current need.
2. **Lists carry what the screens and files need.** Branch and warehouse list responses gain counts, the branch's code and
   names and the default location (additive OpenAPI changes), so the warehouse list no longer depends on a second,
   active-only branch list for names. One read serves the screen and its export (ADR 0021 decision 7).
3. **Report permissions follow what a report lists.** Branch files need `branches.read`, warehouse and location files
   `warehouses.read` (with warehouse scope); the summary, the structure and the data-quality report show branches and
   warehouses together and need both. No new permission. Scoped users see their warehouses and the branches holding them
   (ADR 0013 decision 6), and counts count only those warehouses.
4. **Data quality reports only states the data still allows** (inactive or missing default location, active warehouse in
   an inactive branch, missing English names, company details, branches without an active warehouse, branches without a
   single active transit warehouse). It is computed on request from the Organization tables; no table, no stored figure.
5. **The location import identifies a warehouse by branch code and warehouse code** (warehouse codes are unique per
   branch only) and a location by those plus its code, so a row can never move a location. It reuses the Catalog import's
   token-bound two-step workflow and adds the safety rules bulk changes need: no status changes (deactivation stays on the
   screen with Inventory's stock guard), the `DEFAULT` location is never created or renamed, transit warehouses are
   refused (their locations belong to stock transfers), scope is checked per row without revealing existence.
6. **Shared import pieces moved to the foundation.** `ImportColumn`, `ImportColumnKind`, `ImportChoice`, header and choice
   matching (`ImportColumns`) and the template data sheet (`ImportTemplateSheet`) moved from Catalog to
   `BuildingBlocks.Infrastructure/Documents` (Catalog behaviour unchanged, its tests pass). Planners, row rules and messages
   stay in each module.
7. **Branch and warehouse imports are deferred.** They are few, need company-wide administration and follow-up set-up
   (roles, POS, accounting), and their type, branch and selling flag feed Inventory, Sales and POS.

## Consequences
- One additive migration; `WarehouseERP_Dev` gets it only after a verified backup. No existing value changes.
- OpenAPI: 14 new paths, 20 new schemas, new optional fields on the company requests, new response fields and query
  parameters on the branch and warehouse lists; nothing removed or renamed.
- The PUT of the company now replaces the contact details as sent; the web app sends them all.
