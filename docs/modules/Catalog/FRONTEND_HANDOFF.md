# Catalog — frontend handoff

Verified 2026-10-27 on `feature/catalog` against the real API. The run used a throwaway LocalDB database, real cookie
sessions and anti-forgery, and the throwaway database was then dropped. The examples show the real shapes, status
codes and error codes; ids and row versions are placeholders.

The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md). Business rules and their reasons are in
[ADR 0014](../../adr/0014-catalog-module.md).

## Where it goes
- The screens extend the existing ERP frontend (Lovable project `mahmoudtaha311/screenshot-match`, cloned to
  `D:\ERP System\warehouse-erp-web`). They reuse the App Shell, sign-in, base API, `can()` and `useEditableRecord`.
- Running the backend, the Vite proxy and the cookie/XSRF rules are the same as in
  [Organization's handoff](../Organization/FRONTEND_HANDOFF.md).
- OpenAPI tags: `Units`, `Categories`, `Products`, `ProductUnits`, `ProductBarcodes`.
- No migration: Migration01 already has every Catalog table.
- The bootstrap administrator holds `"*"`. Grant `catalog.units.manage` and `catalog.categories.manage` to other roles
  in the Identity role editor.

## Endpoints
| Method | Path | Permission | Success | Errors |
|---|---|---|---|---|
| GET | `/api/catalog/units?page&pageSize&search&isActive` | `catalog.products.read` | 200 paged | 400 |
| GET | `/api/catalog/units/{id}` | `catalog.products.read` | 200 `UnitDetailsResponse` | 404 |
| POST | `/api/catalog/units` | `catalog.units.manage` | 201 `{id}` | 400, 409 |
| PUT | `/api/catalog/units/{id}` | `catalog.units.manage` | 200 `{rowVersion}` | 400, 404, 409 |
| POST | `/api/catalog/units/{id}/activate` · `/deactivate` | `catalog.units.manage` | 204 | 404, 409, 422 InUse |
| GET | `/api/catalog/categories?isActive` | `catalog.products.read` | 200 `CategoryResponse[]` (flat, unpaged) | |
| GET | `/api/catalog/categories/{id}` | `catalog.products.read` | 200 `CategoryDetailsResponse` | 404 |
| POST | `/api/catalog/categories` | `catalog.categories.manage` | 201 `{id}` | 400, 409, 422 ParentNotActive |
| PUT | `/api/catalog/categories/{id}` | `catalog.categories.manage` | 200 `{rowVersion}` | 400, 404, 409, 422 Cycle / ParentNotActive |
| POST | `/api/catalog/categories/{id}/activate` · `/deactivate` | `catalog.categories.manage` | 204 | 404, 409, 422 ParentNotActive / HasActiveChildren |
| GET | `/api/catalog/products?page&pageSize&search&categoryId&type&isActive` | `catalog.products.read` | 200 paged | 400 |
| GET | `/api/catalog/products/{id}` | `catalog.products.read` | 200 `ProductDetailsResponse` | 404 |
| GET | `/api/catalog/products/lookup?barcode=` | `catalog.products.read` | 200 `ProductLookupResponse` | 400, 404 NoSellableItem |
| POST | `/api/catalog/products` | `catalog.products.create` | 201 `{id}` | 400, 409 SkuTaken, 422 |
| PUT | `/api/catalog/products/{id}` | `catalog.products.update` | 200 `{rowVersion}` | 400, 404, 409, 422 Category.NotActive |
| POST | `/api/catalog/products/{id}/activate` · `/deactivate` | `catalog.products.update` | 204 | 404, 409, 422 Unit.NotActive |
| POST | `/api/catalog/products/{id}/units` | `catalog.products.update` | 201 `{id}` | 400, 404, 409 UnitAlreadyAdded, 422 |
| POST | `/api/catalog/products/{id}/units/{productUnitId}/activate` · `/deactivate` | `catalog.products.update` | 204 | 404, 409, 422 |
| POST | `/api/catalog/products/{id}/units/{productUnitId}/barcodes` | `catalog.products.update` | 201 `{id}` | 400, 404, 409 Barcode.Taken, 422 |
| POST | `/api/catalog/products/{id}/barcodes/{barcodeId}/activate` · `/deactivate` | `catalog.products.update` | 204 | 404, 409, 422 |

None of these needs company-wide access: a branch-scoped user with the permission manages the catalog (verified).

## Request / response examples
```http
POST /api/catalog/units   { "code": "PCS", "nameAr": "قطعة", "nameEn": "Piece", "allowsFraction": false }
→ 201 { "id": "…" }            ("pcs" again → 409 Catalog.Unit.CodeTaken)

GET /api/catalog/units/{id}
→ 200 { "id": "…", "code": "KG", "nameAr": "كيلو", "nameEn": null, "allowsFraction": true, "isActive": true,
        "rowVersion": "AAAAAAAAB9U=" }

PUT /api/catalog/units/{id}   { "nameAr": "كيلوجرام", "nameEn": "Kilogram", "allowsFraction": true, "rowVersion": "AAAAAAAAB9U=" }
→ 200 { "rowVersion": "…" }    (same rowVersion again → 409 Common.ConcurrencyConflict)

POST /api/catalog/categories   { "code": "DRINKS", "nameAr": "مشروبات", "nameEn": null, "parentId": "…FOOD…" }
→ 201 { "id": "…" }

GET /api/catalog/categories
→ 200 [ { "id": "…", "parentId": null, "code": "FOOD", "nameAr": "أغذية", "nameEn": "Food", "isActive": true },
        { "id": "…", "parentId": "…FOOD…", "code": "DRINKS", "nameAr": "مشروبات", "nameEn": null, "isActive": true }, … ]

PUT /api/catalog/categories/{FOOD}   { "nameAr": "أغذية", "parentId": "…JUICE (below FOOD)…", "rowVersion": "…" }
→ 422 Catalog.Category.Cycle
POST /api/catalog/categories/{FOOD}/deactivate   (DRINKS still active) → 422 Catalog.Category.HasActiveChildren

POST /api/catalog/products
{ "sku": "COLA-330", "nameAr": "كولا", "nameEn": "Cola 330ml", "baseUnitId": "…PCS…", "categoryId": "…DRINKS…",
  "type": "StockItem", "trackingMode": "None" }
→ 201 { "id": "…" }

POST /api/catalog/products/{id}/units   { "unitId": "…CTN…", "conversionFactor": 24 }   → 201 { "id": "…" }
POST /api/catalog/products/{id}/units   { "unitId": "…KG…", "conversionFactor": 0.5 }   → 422 Catalog.ProductUnit.FactorMustBeWhole
POST /api/catalog/products/{id}/units/{baseProductUnitId}/deactivate                → 422 Catalog.ProductUnit.BaseUnitMustStayActive

GET /api/catalog/products/{id}
→ 200 { "id": "…", "sku": "COLA-330", "nameAr": "كولا", "nameEn": "Cola 330ml", "categoryId": "…", "baseUnitId": "…PCS…",
        "type": "StockItem", "trackingMode": "None", "isActive": true, "rowVersion": "AAAAAAAAB9o=",
        "units": [
          { "id": "…", "unitId": "…PCS…", "conversionFactor": 1.0, "isActive": true, "unitCode": "PCS", "unitNameAr": "قطعة",
            "unitNameEn": "Piece", "isBase": true, "barcodes": [ { "id": "…", "barcode": "6221234567891", "barcodeType": "Ean13", "isActive": true } ] },
          { "id": "…", "unitId": "…CTN…", "conversionFactor": 24.0, "isActive": true, "unitCode": "CTN", "unitNameAr": "كرتونة",
            "unitNameEn": "Carton", "isBase": false, "barcodes": [ … ] } ] }

POST /api/catalog/products/{id}/units/{productUnitId}/barcodes   { "barcode": "6221234567891", "barcodeType": "Ean13" }  → 201
POST …/barcodes   { "barcode": "6221234567892", "barcodeType": "Ean13" }
→ 400 { "errorCode": "Validation.Failed", "errors": [ { "property": "barcode", "code": "BarcodeFormatValidator", "message": "An EAN-13 barcode is 13 digits with a valid check digit." } ] }
POST …/barcodes   { "barcode": "COLA-CTN/24", "barcodeType": "Internal" }   → 201   (no GS1 rule)
POST …/barcodes   (same value on any product, even after it was deactivated)   → 409 Catalog.Barcode.Taken

GET /api/catalog/products/lookup?barcode=00012345600012
→ 200 { "productId": "…", "sku": "COLA-330", "nameAr": "كولا", "nameEn": "Cola 330ml", "type": "StockItem", "trackingMode": "None",
        "productUnitId": "…", "unitId": "…CTN…", "unitCode": "CTN", "unitNameAr": "كرتونة", "unitNameEn": "Carton",
        "conversionFactor": 24.0, "isBaseUnit": false, "barcodeId": "…", "barcode": "00012345600012", "barcodeType": "Gtin" }
GET /api/catalog/products/lookup?barcode=…   (barcode, its unit, or the product inactive) → 404 Catalog.Barcode.NoSellableItem

POST /api/catalog/units/{CTN}/deactivate   (an active product unit uses it) → 422 Catalog.Unit.InUse
GET /api/catalog/products?search=6221234567891   → the product that carries this barcode
```

## Integration test steps (manual, against the running API)
1. **Units:**
   - Create PCS (no fractions), CTN, and KG (fractions). `pcs` again → 409 on the code field.
   - Edit KG twice from the same page load: the second save → 409, and the page reloads.
2. **Categories:**
   - Create FOOD > DRINKS > JUICE. The tree shows the nesting.
   - Moving FOOD under JUICE is not offered (the server would answer 422 Cycle).
   - Deactivating FOOD → 422 until its subcategories are inactive.
3. **Product:**
   - Create COLA-330: base unit PCS, category DRINKS, StockItem. Tracking is disabled for a Service.
   - Add the unit CTN = 24 PCS. Adding KG with 0.5 → 422 FactorMustBeWhole.
   - Deactivating the PCS row isn't offered.
4. **Barcodes:**
   - Add EAN-13 `6221234567891` on PCS; `6221234567892` → field error.
   - Add GTIN-14 `00012345600012` and internal `COLA-CTN/24` on CTN.
   - The same EAN on another product → 409.
5. **Lookup:**
   - Scan `00012345600012`: Carton (24 Piece).
   - Deactivate that barcode → "No active product…". Reactivate it.
   - Deactivate the CTN unit → not found. Reactivate it.
   - Deactivate the product → not found. Reactivate it.
6. **Search and filters:**
   - Products search `6221234567891` → COLA-330.
   - Filter by category FOOD and type StockItem.
7. **Units in use:** deactivating CTN → 422 InUse while COLA-330 uses it.
8. **Permissions:**
   - A role with only `catalog.products.read` sees the Catalog screens read-only and can scan; every change button is
     hidden (403 if forced).
   - A branch-scoped user with the manage permissions can create units, categories and products.

Every call in these steps was checked in the verification run, with the expected status and error code. The API log
had no 500s and no passwords.
