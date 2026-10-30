# Catalog: reports, files and imports

Catalog's operational enhancements (2026-10-30, ADR 0021): reports, server-generated XLSX/CSV/PDF, product sheets and a
two-step master-data import. File conventions shared by every module are in `docs/DOCUMENTS.md`. Nothing here touches
stock quantities, valuation or costs: those belong to Inventory and Accounting.

## Reports (`GET /api/catalog/reports/…`, `catalog.products.read`)
| Route | Returns |
|---|---|
| `summary` | product counts (total, active, inactive, by type), categories, units, active barcodes, and for **active** products: without barcode, without category, with unit issues |
| `categories?isActive` | every category in tree order (`level`, `parentCode`) with its active and inactive products; a last row with `id: null` counts products in no category |
| `data-quality?issue&search&isActive&page&pageSize` | `{ issues: Paged<…>, counts: [{ issue, count }] }`; counts use the same status and search filters |
| `unit-conversions?search&categoryId&type&isActive&unitId&page&pageSize` | product units ordered by SKU then factor, with the base unit and active barcode count |

Data-quality issues (`CatalogDataIssue`): `NoBarcode` (stock and non-stock items with no active barcode on an active
unit; services are not expected to carry one), `NoCategory`, `InactiveCategory`, `InactiveBaseUnit`,
`InactiveUnitInUse` (an active non-base product unit on a retired unit), `FractionalFactorOnWholeUnit` (a fractional
factor whose base unit no longer allows fractions; detail `1 HALF = 0.5 KG`), `MissingEnglishName`. The summary and the
list share the same predicates (`CatalogReportReadStore`).

The product list itself gained `sort=Sku|NameAr|NameEn|CreatedAt` and `descending` (ties by SKU, so pages never overlap).

## Files (`catalog.products.read`)
`GET /api/catalog/exports/{dataset}?format=Xlsx|Csv|Pdf&lang=Ar|En&…` — the **whole** filtered result, not the screen's
page; over the format's limit (25 000 rows, 5 000 for PDF) a 422 `Catalog.Export.TooManyRows`.

| Dataset | Filters | PDF title (A4) |
|---|---|---|
| `products` | the product list's: `search`, `categoryId`, `type`, `isActive`, `sort`, `descending` | Product master list (landscape) |
| `product-units` | `search`, `categoryId`, `type`, `isActive` (product), `unitId` | Unit and conversion reference (landscape) |
| `barcodes` | `search` (also part of the barcode), `categoryId`, `type`, `isActive` (product), `barcodeType`, `barcodeIsActive` | Product barcodes (landscape) |
| `categories` | `isActive` | Category list and product counts (tree shown with a dash per level; totals row) |
| `units` | `isActive` | Units of measure (with products using each) |
| `data-quality` | `issue`, `search`, `isActive` | Catalog data-quality report (counts per issue) |

XLSX/CSV carry analysis columns (codes and names apart, both product names, creation time in company time); PDFs carry
the columns that fit a page. No database ids are exported. `GET /api/catalog/products/{id}/sheet?lang` is the product
specification sheet (PDF, portrait): master data, every unit with `1 BOX = 12 PCS`, every barcode; the SKU is the
document number.

## Import
### Workflow
1. `GET /api/catalog/imports/template?lang=Ar|En` — a workbook: Instructions (rules and an example of every sheet),
   Lists (choice labels for the drop-downs, the active units and categories), then the five data sheets with header
   comments, text-formatted code columns and drop-downs. Data sheets are empty, so an untouched template imports nothing.
2. `POST /api/catalog/imports/preview` (multipart: `file`, `mode`, and `sheet` for a CSV) — plans every row against the
   current catalog and **changes nothing**. Response: `previewToken`, `canCommit`, `totals` and per-sheet counts
   (rows, valid, invalid, duplicates, create, update, unchanged, skip), every row's action, and up to 1 000 issues
   (`issueCount`, `issuesTruncated`).
3. `POST /api/catalog/imports/error-report?lang` (same form) — every issue as an XLSX to fix the file from.
4. `POST /api/catalog/imports/commit` (same form + `previewToken`) — plans again against the catalog **as it is now**;
   only when the outcome is exactly the previewed one (same token) and has no issue does it apply the plan through the
   domain methods and save everything in **one transaction**. Otherwise nothing is saved:
   409 `Catalog.Import.ChangedSincePreview` (the file, the mode or any row's outcome changed — also what a second
   confirmation of the same file gets, since its rows now exist), 422 `Catalog.Import.HasErrors`,
   422 `Catalog.Import.NothingToImport`, 409 for a unique key taken or a product changed during the save, 403
   `Catalog.Import.NotPermitted` without any Catalog write permission.

### Sheets, in the order they are applied
Headers are matched in Arabic or English (or by the key), ignoring case, spaces and the `*` marker; sheet names in either
language. Choices accept the API name or either label (`صنف مخزني`, `Stock item`, `StockItem`).

| Sheet (Ar / En) | Columns (key) — * required |
|---|---|
| الوحدات / Units | `code`* (≤ 20, `A-Z a-z 0-9 _ -`), `nameAr`* (≤ 100), `nameEn`, `allowsFraction`* (Yes/No) |
| التصنيفات / Categories | `code`* (≤ 30), `nameAr`* (≤ 150), `nameEn`, `parentCode` (existing, or another row of the sheet) |
| الأصناف / Products | `sku`* (≤ 80, also `. /`), `nameAr`* (≤ 250), `nameEn`, `baseUnitCode`*, `categoryCode`, `type`*, `trackingMode` (empty = None) |
| وحدات الأصناف / ProductUnits | `sku`*, `unitCode`*, `conversionFactor`* (base units per unit; > 0, ≤ 8 decimals, dot) |
| الباركود / Barcodes | `sku`*, `unitCode`*, `barcode`*, `barcodeType`* |

A row may refer to a record created by an earlier sheet of the same file. A CSV holds one sheet, named by the `sheet`
field. Codes compare without regard to case, as the database does.

### Modes (what happens to a row whose key exists; new rows are always created)
| Mode | Existing key |
|---|---|
| `Create` | error (`…CodeTaken`, `Catalog.Product.SkuTaken`, `Catalog.ProductUnit.UnitAlreadyAdded`, `Catalog.Barcode.Taken`) |
| `Update` | updated where an editable field differs (unit names and fraction setting, category names, product names and category); otherwise `Unchanged`. Fixed fields must match or the row fails with `Catalog.Import.FixedField`: base unit, type, tracking, a category's parent (moves are done on the Categories screen), a factor, a barcode's type. An empty optional cell keeps the current value. |
| `Skip` | left exactly as it is |

An import never deletes, deactivates or reactivates anything, never changes ids, and never touches stock.

### Rules (same as the single-record endpoints, ADR 0014)
Inactive units and categories can't be newly used (`Catalog.Unit.NotActive`, `Catalog.Category.NotActive`; a product may
stay in a retired category); a parent must be active (`Catalog.Category.ParentNotActive`) and loops are refused
(`Catalog.Category.Cycle`); only stock items are tracked; a factor is whole when the base unit allows no fractions
(`Catalog.ProductUnit.FactorMustBeWhole`) and the base unit's own row must say 1 (`Catalog.Import.BaseUnitFactor`); an
inactive product or product unit takes no new unit or barcode; barcodes follow the GS1 check digit for EAN-13/GTIN
(`Catalog.Import.BarcodeInvalid`) and are never reused (`Catalog.Barcode.Taken`, also when used by another product).
Import-only codes: `Required`, `TooLong`, `InvalidFormat`, `InvalidValue`, `InvalidNumber`, `DuplicateInFile` (every
occurrence), `ReferenceInvalid` (refers to a row with errors), `MissingColumn`, `NoRows`, `DuplicateSheet`,
`NotPermitted` (prefix `Catalog.Import.`). Each issue has the sheet, the Excel row number, the column key and the value.

### Permissions
Endpoints need `catalog.products.read`; preview and commit then need at least one Catalog write permission, and each
row the permission of what it changes: units `catalog.units.manage`, categories `catalog.categories.manage`, new
products (with their units and barcodes) `catalog.products.create`, changes to existing products (names, category,
added units or barcodes) `catalog.products.update`.

### Audit
Every created or changed row keeps its history in the temporal tables with the importing user as `CreatedBy`/`UpdatedBy`;
the commit logs `Catalog import by {UserId} ({Mode}): … created, … updated, … unchanged, … skipped; file {Fingerprint}`
(event 3050). No new table.

### Limits
5 MB per file, 5 000 rows per sheet, 40 columns, cells cut past 1 000 characters, XLSX archives checked for inflation
before opening (`docs/DOCUMENTS.md`).

## Frontend
`/catalog/import` (template, file, mode, preview with counts, issues and rows, error report, confirmation, result),
`/catalog/reports` (overview with drill-down to the filtered product list, data quality, unit conversions, files), export
menus on the product, category and unit lists, the specification sheet on the product page, and a sort control on the
product list. See the web repository's `AGENTS.md`.

## Not done (next small steps)
- Barcode label sheets (needs a barcode renderer, e.g. ZXing.Net, Apache-2.0).
- Importing status changes (activate/deactivate) and category moves.
- A background job for exports beyond the row limits (not needed at today's catalog size).
