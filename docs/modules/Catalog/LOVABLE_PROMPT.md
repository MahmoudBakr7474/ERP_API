# Lovable prompt — WarehouseERP Catalog screens

Copy everything below the line into Lovable, in the same project as the Organization and Identity screens. It is built
from the backend's real OpenAPI contract (`docs/api/openapi-v1.json`) and an end-to-end run against the real API; don't
invent endpoints or fields.

---

Add the **Catalog** module to the existing WarehouseERP app. It covers:
- units of measure;
- the product category tree;
- products, with the extra units they are sold in and their barcodes;
- a barcode lookup.

## Build on what exists (don't rebuild it)
Follow `AGENTS.md`:
- Inject the endpoints into `src/shared/api/baseApi.ts`, with new tag types `Unit`, `Category` and `Product`.
- Put the permission names in `P` and check them with `can()`.
- Put every text in `translations.ts`, and errors under `err.<errorCode>`.
- Use `ServerTable`, `ConfirmDialog`, form fields and `useEditableRecord` for the `rowVersion` forms. Catalog updates
  return `{ rowVersion }` like Organization.
- Put the code in `src/features/catalog/`, and the pages under `src/routes/_authenticated/catalog/`.
- In `AppShell.tsx`, add a **Catalog** sidebar group between Organization and Administration with Products, Categories
  and Units, each shown with `catalog.products.read`.
- In the Identity role editor's permission labels, add the two new permissions (table at the end).

## Permissions
Catalog data is shared by the whole company: these permissions have **no** company-wide or branch condition.

| Action | Needs |
|---|---|
| See products, units, categories; barcode lookup | `catalog.products.read` |
| Create product | `catalog.products.create` |
| Edit product, activate/deactivate it, add/retire its units and barcodes | `catalog.products.update` |
| Create, edit, activate/deactivate units | `catalog.units.manage` |
| Create, edit, move, activate/deactivate categories | `catalog.categories.manage` |

## Shared rules
- Codes and SKUs are ASCII; show them `dir="ltr"`. They **can't be changed** after creation: show them read-only on
  edit.
- `nameAr` is required and `nameEn` optional. Show `nameAr` in Arabic and `nameEn ?? nameAr` in English. Send `null`
  for empty optional text.
- `201` → `{ id }`. A PUT → `{ rowVersion }`. Activate/deactivate → `204`.
  - A stale `rowVersion` gives 409 `Common.ConcurrencyConflict`: reload and keep the user's edits.
- Nothing is ever deleted: retire with deactivate. Confirm every deactivation.

## 1. Units — `/catalog/units`
- List: `GET /api/catalog/units?page&pageSize&search&isActive` → paged `{ id, code, nameAr, nameEn, allowsFraction,
  isActive }`.
  - Columns: code, name, a "Fractions" badge when `allowsFraction`, and status.
- Create dialog: `POST /api/catalog/units` with `{ code, nameAr, nameEn, allowsFraction }`.
  - `code` ≤20, `A–Z a–z 0–9 _ -`, unique ignoring case (409 `Catalog.Unit.CodeTaken` on the field).
  - Names ≤100.
  - `allowsFraction` is a switch: "Quantities may have decimals (kg, litre)".
- Edit dialog: `GET /api/catalog/units/{id}` → adds `rowVersion`; then `PUT /api/catalog/units/{id}` with
  `{ nameAr, nameEn, allowsFraction, rowVersion }`.
- Activate/deactivate: `POST /api/catalog/units/{id}/activate` or `/deactivate`. 422 `Catalog.Unit.InUse`: "Active
  products still use this unit".

## 2. Categories — `/catalog/categories`
- `GET /api/catalog/categories?isActive` → **one flat array** (not paged) of
  `{ id, parentId, code, nameAr, nameEn, isActive }`.
  - Draw it as a collapsible **tree** (children under `parentId`, top level `parentId: null`), with a search box that
    filters on the client and an Active / Inactive / All filter.
- Create dialog (also "Add subcategory" on a tree row): `POST /api/catalog/categories` with
  `{ code, nameAr, nameEn, parentId }`.
  - `code` ≤30, `A–Z a–z 0–9 _ -`; names ≤150.
  - `parentId` comes from a tree select of **active** categories, or none for the top level.
- Edit dialog: `GET /api/catalog/categories/{id}` (with `rowVersion`), then `PUT /api/catalog/categories/{id}` with
  `{ nameAr, nameEn, parentId, rowVersion }`.
  - Changing the parent moves the category with its subtree.
  - In the parent select, disable the category itself and everything below it. The server refuses those with 422
    `Catalog.Category.Cycle`.
- Activate/deactivate on each row:
  - 422 `Catalog.Category.HasActiveChildren`: "Deactivate its subcategories first".
  - 422 `Catalog.Category.ParentNotActive`: "Activate the parent first".

## 3. Products — `/catalog/products`
- List: `GET /api/catalog/products?page&pageSize&search&categoryId&type&isActive` → paged
  `{ id, sku, nameAr, nameEn, type, trackingMode, isActive, categoryId, baseUnitId }`.
  - `search` matches SKU, either name, or a **barcode exactly**: a scanned code finds its product.
  - Filters: category (tree select), type (`StockItem` / `NonStockItem` / `Service`) and status.
  - Show the category and base unit names by looking them up in the categories and units lists.
- **Barcode lookup** box at the top of the page: on Enter (scanners send Enter), call
  `GET /api/catalog/products/lookup?barcode=…`.
  - 200 → `{ productId, sku, nameAr, nameEn, type, trackingMode, productUnitId, unitId, unitCode, unitNameAr,
    unitNameEn, conversionFactor, isBaseUnit, barcodeId, barcode, barcodeType }`. Show a result card ("Cola 330ml —
    Carton (24 pieces)") with a link to the product.
  - 404 `Catalog.Barcode.NoSellableItem`: "No active product has this barcode". The barcode, its unit or its product
    may be inactive.
- Create page `/catalog/products/new`: `POST /api/catalog/products` with
  `{ sku, nameAr, nameEn, baseUnitId, categoryId, type, trackingMode }`.
  - `sku` ≤80, `A–Z a–z 0–9 . _ / -`, unique (409 `Catalog.Product.SkuTaken`). Names ≤250.
  - `baseUnitId`: a select of **active** units: "the unit stock is counted in".
  - `categoryId`: optional, active categories only.
  - `type`: `StockItem`, `NonStockItem` or `Service`.
  - `trackingMode`: `None`, `Batch` or `Serial`, enabled only for `StockItem` (422
    `Catalog.Product.TrackingNeedsStockItem` otherwise).
  - Warn that SKU, type, tracking and base unit can't be changed later. After 201, open the product page.
- Product page `/catalog/products/:id`: `GET /api/catalog/products/{id}` →
  `{ id, sku, nameAr, nameEn, categoryId, baseUnitId, type, trackingMode, isActive, rowVersion,
     units: [{ id, unitId, conversionFactor, isActive, unitCode, unitNameAr, unitNameEn, isBase,
               barcodes: [{ id, barcode, barcodeType, isActive }] }] }`.
  - **Details** form: `PUT /api/catalog/products/{id}` with `{ nameAr, nameEn, categoryId, rowVersion }`.
    - Always send `categoryId`: `null` removes the category.
    - The product may sit in a category that was later deactivated: keep it selectable as the current value. Choosing
      another category offers active ones only (422 `Catalog.Category.NotActive`).
    - SKU, type, tracking and base unit are read-only.
  - Activate/deactivate the product (`POST /api/catalog/products/{id}/activate` or `/deactivate`). 422
    `Catalog.Unit.NotActive` on activate: "Its base unit is inactive".
  - **Units** section, a table: unit, "1 = N base units", Base badge, status, barcodes.
    - "Add unit": `POST /api/catalog/products/{id}/units` with `{ unitId, conversionFactor }`.
      - `conversionFactor` > 0, up to 8 decimals. Example: "1 Carton = 24 Piece".
      - 409 `Catalog.ProductUnit.UnitAlreadyAdded`.
      - 422 `Catalog.ProductUnit.FactorMustBeWhole`: the base unit can't be split.
      - 422 `Catalog.Unit.NotActive`.
    - Activate/deactivate a unit row: `POST /api/catalog/products/{id}/units/{productUnitId}/activate` or
      `/deactivate`. Never offer to deactivate the base unit (422 `Catalog.ProductUnit.BaseUnitMustStayActive`).
  - **Barcodes** under each unit row.
    - "Add barcode": `POST /api/catalog/products/{id}/units/{productUnitId}/barcodes` with `{ barcode, barcodeType }`.
      - `barcodeType`: `Internal` (default), `Ean13`, `Gtin` or `Other`.
      - For `Ean13`/`Gtin`, check length and GS1 check digit on the client too: EAN-13 is 13 digits, GTIN is 8, 12, 13
        or 14. A bad value is 400 on `barcode` (`BarcodeFormatValidator`).
      - `Internal`/`Other`: printable ASCII, no spaces, ≤100.
      - 409 `Catalog.Barcode.Taken`: the value is used or was used before. Retired barcodes stay reserved.
      - 422 `Catalog.ProductUnit.NotActive` / `Catalog.Product.NotActive`.
    - Activate/deactivate a barcode: `POST /api/catalog/products/{id}/barcodes/{barcodeId}/activate` or
      `/deactivate`.
    - Show inactive barcodes struck through. A barcode can't be edited: retire it and add a new one.

## Labels and error codes to translate
New permission labels for the Identity role editor:

| Permission | Arabic | English |
|---|---|---|
| `catalog.units.manage` | إدارة وحدات القياس | Manage units of measure |
| `catalog.categories.manage` | إدارة تصنيفات الأصناف | Manage product categories |

Also update `catalog.products.read` to "عرض الأصناف والوحدات والتصنيفات / View products, units and categories" and
`catalog.products.update` to "تعديل الأصناف ووحداتها وباركوداتها / Edit products, their units and barcodes".

| errorCode | Status | Arabic | English |
|---|---|---|---|
| `Catalog.Unit.CodeTaken` | 409 | كود الوحدة مستخدم بالفعل | Another unit uses this code |
| `Catalog.Unit.NotFound` | 404 | الوحدة غير موجودة | The unit does not exist |
| `Catalog.Unit.NotActive` | 422 | الوحدة غير موجودة أو غير نشطة | The unit is missing or inactive |
| `Catalog.Unit.InUse` | 422 | أصناف نشطة تستخدم هذه الوحدة | Active products still use this unit |
| `Catalog.Category.CodeTaken` | 409 | كود التصنيف مستخدم بالفعل | Another category uses this code |
| `Catalog.Category.NotFound` | 404 | التصنيف غير موجود | The category does not exist |
| `Catalog.Category.NotActive` | 422 | التصنيف غير موجود أو غير نشط | The category is missing or inactive |
| `Catalog.Category.ParentNotActive` | 422 | التصنيف الأب غير نشط | The parent category is inactive |
| `Catalog.Category.Cycle` | 422 | لا يمكن نقل التصنيف تحت نفسه أو أحد فروعه | A category can't go under itself or its subcategories |
| `Catalog.Category.HasActiveChildren` | 422 | أوقف التصنيفات الفرعية أولاً | Deactivate the subcategories first |
| `Catalog.Product.SkuTaken` | 409 | كود الصنف (SKU) مستخدم بالفعل | Another product uses this SKU |
| `Catalog.Product.NotFound` | 404 | الصنف غير موجود | The product does not exist |
| `Catalog.Product.NotActive` | 422 | الصنف غير نشط | The product is inactive |
| `Catalog.Product.TrackingNeedsStockItem` | 422 | التتبع بالتشغيلة أو السيريال للأصناف المخزنية فقط | Only stock items can be tracked by batch or serial |
| `Catalog.ProductUnit.NotFound` | 404 | الوحدة غير موجودة لهذا الصنف | The product has no such unit |
| `Catalog.ProductUnit.UnitAlreadyAdded` | 409 | الصنف لديه هذه الوحدة بالفعل | The product already has this unit |
| `Catalog.ProductUnit.FactorMustBeWhole` | 422 | الوحدة الأساسية لا تقبل الكسور، معامل التحويل يجب أن يكون عددًا صحيحًا | The base unit can't be split; use a whole factor |
| `Catalog.ProductUnit.BaseUnitMustStayActive` | 422 | لا يمكن إيقاف الوحدة الأساسية | The base unit can't be deactivated |
| `Catalog.ProductUnit.NotActive` | 422 | وحدة الصنف غير نشطة | The product unit is inactive |
| `Catalog.Barcode.Taken` | 409 | الباركود مستخدم أو استُخدم من قبل ومحجوز | This barcode is used or reserved |
| `Catalog.Barcode.NotFound` | 404 | الباركود غير موجود | No such barcode |
| `Catalog.Barcode.NoSellableItem` | 404 | لا يوجد صنف نشط بهذا الباركود | No active product has this barcode |

Validation codes on fields: `BarcodeFormatValidator`, "الباركود لا يطابق نوعه / The barcode doesn't match its type".

## Backend facts (don't fake them)
- Units, categories and products can't be deleted, only deactivated.
- There is no endpoint to change a product unit's conversion factor or a barcode's value: retire and add.
- The product list filters by one category, not by its subcategories.
- Tax codes and e-invoicing item codes (EGS/GS1) are not part of this phase.
