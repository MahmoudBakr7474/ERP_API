# Organization: company profile, reports, files and the location import

Organization's operational enhancements (2026-10-31, ADR 0022): company contact details and profile PDF, richer branch and
warehouse lists, a cross-warehouse location directory, master-data reports, server-generated XLSX/CSV/PDF and a two-step
storage location import. File conventions shared by every module are in `docs/DOCUMENTS.md`. Nothing here touches stock
quantities, valuation or reservations: those belong to Inventory.

## Company profile
`Migration17_CompanyContactDetails` adds six nullable columns to `organization.Companies` (temporal; the history table
gets them too): `CommercialRegistrationNumber` varchar(30), `CommercialRegistry` nvarchar(100), `Phone` nvarchar(30),
`Email` nvarchar(100), `AddressLine` nvarchar(200), `CityId` (FK to `organization.Cities`). No existing value changes.

- `POST`/`PUT /api/organization/company` take them as optional fields (the PUT replaces them as sent). A new city must be
  usable (it, its governorate and its country active): 422 `Organization.Company.CityNotUsable`; the current city may
  stay after it is retired. Code, base currency and every other rule of ADR 0013 are unchanged.
- `GET /api/organization/company` returns them, with `city` (names of the city, governorate and country).
- `GET /api/organization/company/profile?lang=Ar|En` — the Company Profile PDF (A4 portrait, company code as document
  number): identification, address and contact, settings. **A detail never recorded is left out**, never printed blank or
  invented; a section with nothing recorded says "Nothing recorded yet". Any signed-in user may print it, as they may
  read the company. The shared document header keeps printing the company's names and tax number (unchanged).

## Lists (additive)
| Endpoint | New query parameters | New response fields |
|---|---|---|
| `GET /branches` | `hasWarehouses`, `sort=Code\|NameAr\|NameEn`, `descending` | `warehouseCount`, `activeWarehouseCount` (only the caller's warehouses) |
| `GET /warehouses` | `type`, `allowsSelling`, `sort`, `descending` | `branchCode`, `branchNameAr`, `branchNameEn`, `branchIsActive`, `locationCount`, `activeLocationCount`, `defaultLocationCode`, `defaultLocationIsActive` |
| `GET /locations` (new, `organization.warehouses.read`) | `branchId`, `warehouseId`, `warehouseType`, `search`, `isActive`, `sort`, `descending`, paging | the location with its warehouse and branch codes, names and status |

Ties are broken by code and id, so pages never overlap. Warehouse codes repeat across branches, so warehouse lists also
order by branch code. The location directory is grouped by branch and warehouse, the default location first.

## Reports (`GET /api/organization/reports/…`, `organization.branches.read` **and** `organization.warehouses.read`)
| Route | Returns |
|---|---|
| `summary` | branches (active), warehouses (active, physical, transit), active selling warehouses, storage locations of physical warehouses (active), active non-default transit locations (one per open transfer), data-quality rows |
| `structure?isActive` | branches → warehouses with location counts and the default location code; the screen drills into locations through `/locations?warehouseId` |
| `data-quality?issue&page&pageSize` | `{ issues: Paged<…>, counts: [{ issue, count }] }` |

Every figure is counted by the database at request time; nothing is stored. A scoped user (not company-wide) sees their
warehouses, those warehouses' locations and the branches holding them; problems are judged on all the data (a branch's
warehouses outside the user's scope still count) and only records the user may see are listed.

### Data-quality issues (`OrganizationDataIssue`)
Only states the database and the domain still allow; what they already make impossible (a selling transit warehouse, two
default locations, an inactive warehouse code reused) is not reported.

| Issue | Found when |
|---|---|
| `CompanyMissingTaxNumber`, `CompanyMissingCommercialRegistration` | the company has no such number |
| `CompanyMissingAddress` | no street line or no city |
| `CompanyMissingContact` | neither phone nor e-mail |
| `MissingEnglishName` | the company, a branch, a warehouse or a location of a physical warehouse has no English name (English documents fall back to Arabic) |
| `BranchWithoutActiveWarehouse` | an active branch has no active warehouse |
| `ActiveWarehouseInInactiveBranch` | the screens refuse it; data changed by hand or before the rule can hold it |
| `NoDefaultLocation`, `InactiveDefaultLocation` | the domain creates and protects the default location; the database (filtered unique index) does not require one |
| `NoDefaultTransitWarehouse` | an active branch with active physical warehouses has 0 or 2+ active transit warehouses, so each transfer out of it must name its transit warehouse (`Inventory.StockTransfer.TransitWarehouseRequired`); `detail` is the count |

## Files (`GET /api/organization/exports/{dataset}?format=Xlsx|Csv|Pdf&lang=Ar|En&…`)
The **whole** filtered result, through the same read as the screen; over the format's limit (25 000 rows, 5 000 for PDF)
a 422 `Organization.Export.TooManyRows`. No database ids are exported.

| Dataset | Permission | Filters | PDF |
|---|---|---|---|
| `branches` | branches.read | `search`, `isActive`, `hasWarehouses`, `sort`, `descending` | Branch directory (portrait, totals row) |
| `warehouses` | warehouses.read | `branchId`, `search`, `isActive`, `type`, `allowsSelling`, `sort`, `descending` | Warehouse directory (landscape; summary by status, type, selling) |
| `locations` | warehouses.read (+ scope) | as the location directory | Storage location directory (landscape) |
| `structure` | both | `isActive`, `includeLocations` | Organizational structure (a dash per level; every printed line counts against the limit) |
| `data-quality` | both | `issue` | Organization data-quality report (landscape; counts per issue) |

XLSX/CSV carry codes and names in separate columns (branch and warehouse codes on every row to filter on); PDFs carry the
columns that fit the page. Codes are text cells; CSV is UTF-8 with BOM and formula-safe.

## Location import (`/api/organization/imports/locations/…`, `organization.warehouses.read`)
Same two-step workflow as the Catalog import (ADR 0021 decision 8): `template?lang`, `preview` (multipart `file`, `mode`),
`error-report?lang` (same form), `commit` (same form + `previewToken`). Preview changes nothing; commit plans again
against the organization as it is now (permissions and scope included), proceeds only when the outcome hashes to the
previewed token, applies the plan through the domain methods (`Warehouse.AddLocation`, `WarehouseLocation.Rename`) and
saves once. Otherwise nothing is saved: 409 `Organization.Import.ChangedSincePreview` (file, mode or any row's outcome
changed — also a second confirmation), 422 `HasErrors`, 422 `NothingToImport`, 409 `Organization.WarehouseLocation.CodeTaken`
or `Common.ConcurrencyConflict` for a change made during the save, 403 `Organization.Import.NotPermitted` without
`locations.create` or `locations.update`.

### File
One sheet `المواقع` / `Locations` (a one-sheet workbook or a CSV is read whatever its name; delimiter and BOM detected).
Headers in Arabic or English or by key; `*` required.

| Column (key) | Rule |
|---|---|
| `branchCode`* | an existing branch code (case ignored) |
| `warehouseCode`* | a warehouse of that branch — **warehouse codes repeat across branches, so both codes name it** |
| `code`* | ≤ 50, `A-Z a-z 0-9 . _ -`; unique in the warehouse (case ignored); fixed once created |
| `nameAr`* | ≤ 150 |
| `nameEn` | ≤ 150; empty in Update mode keeps the current name |

The template adds Instructions and a Lists sheet with the physical warehouses in the user's scope (branch, warehouse,
status, location count). Its data sheet is empty, so an untouched template imports nothing.

### Modes (what happens to a row whose location exists; new locations are always created)
| Mode | Existing location |
|---|---|
| `Create` | error `Organization.WarehouseLocation.CodeTaken` |
| `Update` | renamed when a name differs, else `Unchanged` |
| `Skip` | left exactly as it is |

### Rules
- Scope per row: a warehouse outside the user's scope, or a code they can't reach, reads `Organization.Warehouse.AccessDenied`
  whether or not it exists (no existence leak). Company-wide users get `Organization.Branch.NotFound` /
  `Organization.Warehouse.NotFound`.
- New locations need an active warehouse (`Organization.Warehouse.NotActive`) and `locations.create`; renames need
  `locations.update` (`Organization.Import.NotPermitted` per row).
- **Never**: a location moves (it is found by branch, warehouse and its own code), a code changes, a status changes
  (activation and deactivation stay on the screen, where Inventory's stock guard runs), the `DEFAULT` location is created
  or renamed (`Organization.Import.DefaultLocationFixed`), or a transit warehouse's locations are touched
  (`Organization.Import.TransitWarehouse`: they are opened and retired by stock transfers).
- Import-only codes (prefix `Organization.Import.`): `Required`, `TooLong`, `InvalidFormat`, `DuplicateInFile` (every
  occurrence; `totals.duplicates`), `NotPermitted`, `MissingColumn`, `NoRows`, `TransitWarehouse`, `DefaultLocationFixed`,
  `ModeRequired`. Each issue has the Excel row, the column key and the value.
- Limits: 5 MB, 5 000 rows, 40 columns, zip-bomb checks (`docs/DOCUMENTS.md`).
- Audit: temporal history with the importing user as `CreatedBy`/`UpdatedBy`, and the log entry
  `Location import by {UserId} ({Mode}): … created, … renamed, … unchanged, … skipped; file {Fingerprint}` (event 2029).

## Not done (deferred, with reasons)
- **Branch and warehouse imports.** Branches and warehouses are few and created once by a company-wide administrator;
  each one also needs roles, scopes, POS terminals, cashboxes and posting set-up that an import can't do, and a warehouse's
  type, branch and selling permission are used by Inventory, Sales and POS. The single-record screens stay the way.
- Branch addresses and contact details (not in the model; e-invoicing will decide what a branch needs).
- Importing status changes; changing a warehouse's default location; moving a warehouse between branches (ADR 0013).
