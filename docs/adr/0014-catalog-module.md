# ADR 0014 — Catalog V1: units, categories, products, product units, barcodes and lookup

Status: accepted (2026-10-27), on `feature/catalog`. The product owner approved the scope and decisions 1–6 before
implementation; the rules in 7–9 come from the owner's additional business rules. Implemented and tested.

## Context
Migration01 already had the Catalog tables (temporal, `catalog` schema): units, categories, products, product units,
barcodes, tax codes and rates, product tax assignments and EGS/GS1 external item codes. The API could only create,
read, list and rename products, and there was no way to create a unit, so no product could be created through the
API on a fresh database.

## Decision
1. **Scope.** Units, categories, products, product units, product barcodes and a barcode lookup.
   * Tax codes, tax rates, product tax assignments and EGS/GS1 external item codes wait for the Sales/e-invoicing
     phase.
   * Their tables and constraints stay as Migration01 made them.
2. **Permission-based management, no scope.** Catalog data is shared by the whole company, so these permissions carry
   no branch or warehouse scope, and a branch-scoped user who holds them manages the catalog.
   * `catalog.products.read`: products, units and categories, and the barcode lookup.
   * `catalog.products.create`: new products.
   * `catalog.products.update`: a product's names, category and status, and its units and barcodes.
   * `catalog.units.manage` and `catalog.categories.manage` are new.
3. **Fixed after creation:**
   * a product's SKU, type, tracking mode and base unit;
   * a unit's code and a category's code;
   * a product unit's conversion factor;
   * a barcode's value (and type).

   Quantities and history rely on them; a mistake is retired and replaced. What changes:
   * names;
   * a unit's `allowsFraction`;
   * a category's parent;
   * a product's category;
   * active status.
4. **Barcode values are never reused.** The unique index (`IX_ProductBarcodes_Barcode`) covers retired barcodes too, so
   a deactivated value stays reserved (409 `Catalog.Barcode.Taken`). No schema change.
5. **Units start empty.** They are created through the API and the frontend; no seed data or data migration.
6. **Categories.**
   * A category can't be deactivated while it has active subcategories (422 `Catalog.Category.HasActiveChildren`).
   * It can't be reactivated, created or moved under an inactive parent (422 `Catalog.Category.ParentNotActive`). So
     an inactive category never holds an active one.
   * Products may stay in a retired category. A new product, or a product moved to another category, can't target an
     inactive one (422 `Catalog.Category.NotActive`).
   * Moving a category under itself or anywhere below itself is refused (422 `Catalog.Category.Cycle`). The handler
     walks the ancestors; the database still refuses a self-parent.
7. **Units in use.** A unit can't be deactivated while an active product keeps stock in it, or an active product unit
   of an active product uses it (422 `Catalog.Unit.InUse`). Accordingly:
   * a product comes back only while its base unit is active;
   * a product unit comes back only while its unit is active (422 `Catalog.Unit.NotActive`).
8. **Product units and barcodes.**
   * A product gets each unit once (409 `Catalog.ProductUnit.UnitAlreadyAdded`), with a conversion factor in base
     units. The factor is > 0 and fits `decimal(18, 8)`.
   * When the base unit can't be split (`allowsFraction: false`) the factor must be whole (422
     `Catalog.ProductUnit.FactorMustBeWhole`). Half a piece can't be counted.
   * The base unit's product unit can't be deactivated (422 `Catalog.ProductUnit.BaseUnitMustStayActive`).
   * Inactive products and inactive product units take no new barcodes (422 `Catalog.Product.NotActive`,
     `Catalog.ProductUnit.NotActive`). A barcode is reactivated only while its product unit is active.
   * **GS1 validation** applies only to `Ean13` (13 digits) and `Gtin` (8, 12, 13 or 14 digits), with the GS1 mod-10
     check digit. `Internal` and `Other` barcodes are the company's own: printable ASCII without spaces, up to 100
     characters, no GS1 rule. A bad value is 400 `Validation.Failed` on `barcode` (`BarcodeFormatValidator`).
9. **Barcode lookup** (`GET /api/catalog/products/lookup?barcode=`).
   * It returns the sellable item: the product, the product unit the barcode is on, the unit, the conversion factor
     and whether it is the base unit.
   * It does so only when the barcode, its product unit, the unit and the product are all active. Otherwise it
     answers 404 `Catalog.Barcode.NoSellableItem`.
   * The product list's `search` also matches a barcode exactly.
10. **No hard delete.** Every Catalog change is an insert, an update or an activate/deactivate; temporal history keeps
    the past versions.
11. **Concurrency.**
    * Unit, category and product updates take the base64 `rowVersion` and return the new one (stale: 409
      `Common.ConcurrencyConflict`).
    * Activate/deactivate and adding units or barcodes take none (204 / 201).
    * Unique-key races map to their business codes: `IX_Units_Code`, `IX_Categories_Code`,
      `IX_ProductUnits_ProductId_UnitId` and `IX_ProductBarcodes_Barcode`.

## Consequences
* **No migration.** `has-pending-model-changes` reports none. `BarcodeType` moved from Catalog.Domain to
  Catalog.Contracts (beside `ProductType` and `TrackingMode`) so HTTP requests can name it. It is stored as `tinyint`
  as before.
* **The OpenAPI contract changed additively.**
  * 21 new operations (tags `Units`, `Categories`, `ProductUnits`, `ProductBarcodes`, and `Products` for
    lookup/activate/deactivate) and new schemas.
  * `categoryId`, `type` and `isActive` filters on `ListProducts`.
  * `categoryId` and `baseUnitId` on `ProductSummaryResponse`.
  * unit code/names, `isBase` and `barcodes` on `ProductUnitResponse`.
* **`PUT /api/catalog/products/{id}` now carries an optional `categoryId`.** Leaving it out takes the product out of
  its category: the PUT replaces every editable field. No client used Catalog yet.
* The category list is one unpaged, flat list with `parentId`. Categories are few, and a client draws the tree.
* **Residual risk:** two category moves at the same instant, each valid alone, could form a loop. Category edits are
  rare administrative actions; if needed later, the move can take an application lock.

## Not done (later, if needed)
* Tax codes, rates, product tax assignments, EGS/GS1 external codes (Sales/e-invoicing phase).
* Generating internal barcodes automatically.
* Filtering the product list by a category's whole subtree.
* A `IProductCatalog` barcode lookup for other modules (POS and Sales will add it when they need it).
