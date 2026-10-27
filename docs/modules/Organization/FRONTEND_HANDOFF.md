# Organization — frontend handoff

Verified 2026-10-27 on `feature/organization`: the real API, run against a throwaway LocalDB database with real
cookie sessions. The examples show the real shapes, status codes and error codes; ids and row versions are
placeholders. The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md); business rules
and their reasons are in [ADR 0013](../../adr/0013-organization-module.md).

## Run the backend
```powershell
dotnet dev-certs https --trust            # once
cd src/WarehouseERP.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run                                # https://localhost:49571 (the owner's launch profile)
```
- The database is LocalDB `WarehouseERP_Dev` (`appsettings.Development.json`). Apply the migrations first with
  `dotnet dotnet-ef database update` and `ERP_SQL_CONNECTION` pointing at it.
- There is no Organization migration: Migration01 already has every table.
- First administrator: see README *Running the API* (`Identity__Bootstrap__*`, one start, empty user table).
- OpenAPI (Development only): `https://localhost:49571/openapi/v1.json`. The committed contract is
  `docs/api/openapi-v1.json`. Organization tags: `Company`, `Branches`, `Warehouses`, `WarehouseLocations`.

## Frontend wiring
- **No CORS.** Serve the SPA same-origin: in dev, proxy `/api` to the API with Vite.
  ```ts
  server: { proxy: { '/api': { target: 'https://localhost:49571', changeOrigin: true, secure: false } } }
  ```
  If the browser drops the `Secure` session cookie on `http://localhost`, run Vite over https
  (`@vitejs/plugin-basic-ssl`).
- The frontend needs no env vars beyond the proxy target. Base URL in code: `/api`.
- **Session:**
  - `POST /api/identity/login` `{ userName, password }` → 204, sets `__Host-erp` (HttpOnly) and `XSRF-TOKEN`.
  - Fetch with `credentials: 'include'`.
  - Echo `XSRF-TOKEN` as `X-XSRF-TOKEN` on POST/PUT/DELETE. `GET /api/identity/me` renews it.
  - Any 401 = signed out.

## Endpoints
| Method | Path | Permission (+ scope) | Success | Errors |
|---|---|---|---|---|
| GET | `/api/organization/company` | signed in | 200 `CompanyResponse` | 404 NotConfigured |
| POST | `/api/organization/company` | `organization.company.update` + company-wide | 201 `{id}` | 400, 403, 409 AlreadyConfigured |
| PUT | `/api/organization/company` | `organization.company.update` + company-wide | 200 `{rowVersion}` | 400, 403, 404, 409 |
| GET | `/api/organization/branches?page&pageSize&search&isActive` | `organization.branches.read` (scoped list) | 200 paged | 400 |
| GET | `/api/organization/branches/{id}` | `organization.branches.read` (scope first) | 200 `BranchDetailsResponse` | 403, 404 |
| POST | `/api/organization/branches` | `organization.branches.create` + company-wide | 201 `{id}` | 400, 403, 409, 422 SetupRequired |
| PUT | `/api/organization/branches/{id}` | `organization.branches.update` + company-wide | 200 `{rowVersion}` | 400, 403, 404, 409 |
| POST | `/api/organization/branches/{id}/activate` | `organization.branches.update` + company-wide | 204 | 403, 404, 409 |
| POST | `/api/organization/branches/{id}/deactivate` | `organization.branches.update` + company-wide | 204 | 403, 404, 409, 422 HasActiveWarehouses |
| GET | `/api/organization/warehouses?page&pageSize&branchId&search&isActive` | `organization.warehouses.read` (scoped list) | 200 paged | 400 |
| GET | `/api/organization/warehouses/{id}` | `organization.warehouses.read` + in scope | 200 `WarehouseDetailsResponse` | 403, 404 |
| POST | `/api/organization/warehouses` | `organization.warehouses.create` + company-wide | 201 `{id}` | 400, 403, 409, 422 |
| PUT | `/api/organization/warehouses/{id}` | `organization.warehouses.update` + in scope | 200 `{rowVersion}` | 400, 403, 404, 409, 422 TransitCannotSell |
| POST | `/api/organization/warehouses/{id}/activate` | `organization.warehouses.update` + in scope | 204 | 403, 404, 409, 422 Branch.NotActive |
| POST | `/api/organization/warehouses/{id}/deactivate` | `organization.warehouses.update` + in scope | 204 | 403, 404, 409 |
| GET | `/api/organization/warehouses/{warehouseId}/locations?page&pageSize&search&isActive` | `organization.warehouses.read` + in scope | 200 paged | 400, 403, 404 |
| GET | `/api/organization/warehouses/{warehouseId}/locations/{locationId}` | `organization.warehouses.read` + in scope | 200 `WarehouseLocationResponse` | 403, 404 |
| POST | `/api/organization/warehouses/{warehouseId}/locations` | `organization.locations.create` + in scope | 201 `{id}` | 400, 403, 404, 409, 422 Warehouse.NotActive |
| PUT | `/api/organization/warehouses/{warehouseId}/locations/{locationId}` | `organization.locations.update` + in scope | 200 `{rowVersion}` | 400, 403, 404, 409 |
| POST | `.../locations/{locationId}/activate` | `organization.locations.update` + in scope | 204 | 403, 404, 409 |
| POST | `.../locations/{locationId}/deactivate` | `organization.locations.update` + in scope | 204 | 403, 404, 409, 422 DefaultMustStayActive |

"Company-wide" is `isCompanyWide` in `/me`. "Scoped list" means a company-wide user sees everything; others see the
branches and warehouses their access covers (a branch grant covers its later warehouses too).

## Request / response examples
```http
POST /api/organization/company
{ "code": "DIST", "nameAr": "شركة التوزيع", "nameEn": "Distribution Co", "baseCurrencyCode": "egp",
  "timeZoneId": "Africa/Cairo", "taxRegistrationNumber": "100-200-300" }
→ 201  Location: /api/organization/company   { "id": "…" }

GET /api/organization/company
→ 200 { "id": "…", "code": "DIST", "nameAr": "شركة التوزيع", "nameEn": "Distribution Co", "baseCurrencyCode": "EGP",
        "timeZoneId": "Africa/Cairo", "taxRegistrationNumber": "100-200-300", "rowVersion": "AAAAAAAAB9E=" }

PUT /api/organization/company
{ "nameAr": "شركة التوزيع الحديثة", "nameEn": "Modern Distribution", "timeZoneId": "Africa/Cairo",
  "taxRegistrationNumber": "100-200-300", "rowVersion": "AAAAAAAAB9E=" }
→ 200 { "rowVersion": "AAAAAAAAB+k=" }        (same rowVersion again → 409 Common.ConcurrencyConflict)

POST /api/organization/branches   { "code": "CAI", "nameAr": "فرع القاهرة", "nameEn": "Cairo" }
→ 201  Location: /api/organization/branches/{id}

GET /api/organization/branches?search=CA&isActive=true
→ 200 { "items": [ { "id": "…", "code": "CAI", "nameAr": "فرع القاهرة", "nameEn": "Cairo", "isActive": true } ],
        "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1 }

POST /api/organization/warehouses
{ "branchId": "…", "code": "WH-CAI-1", "nameAr": "مخزن القاهرة", "nameEn": null, "type": "Physical", "allowsSelling": true }
→ 201

GET /api/organization/warehouses/{id}
→ 200 { "id": "…", "branchId": "…", "code": "WH-CAI-1", "nameAr": "مخزن القاهرة", "nameEn": null, "type": "Physical",
        "allowsSelling": true, "isActive": true, "rowVersion": "…",
        "locations": [ { "id": "…", "code": "DEFAULT", "nameAr": "الموقع الافتراضي", "nameEn": "Default location",
                         "isDefault": true, "isActive": true, "rowVersion": "…" } ] }

POST /api/organization/warehouses/{id}/locations   { "code": "A.01", "nameAr": "رف أ ١", "nameEn": "Shelf A1" }
→ 201  Location: /api/organization/warehouses/{id}/locations/{locationId}

POST /api/organization/branches/{id}/deactivate   (branch still has an active warehouse)
→ 422 { "title": "…", "status": 422, "detail": "The branch still has active warehouses; deactivate them first.",
        "errorCode": "Organization.Branch.HasActiveWarehouses", "traceId": "…" }

POST /api/organization/branches   { "code": "فرع", "nameAr": "" }
→ 400 { "errorCode": "Validation.Failed", "errors": [ { "property": "code", "code": "RegularExpressionValidator", "message": "…" },
                                                     { "property": "nameAr", "code": "NotEmptyValidator", "message": "…" } ] }
```

## Integration test steps (manual, against the running API)
1. Sign in as the bootstrap administrator. `GET /company` → 404. The setup screen appears; submit it → 201. A
   second submit → 409.
2. Edit the company → 200. Edit again with the old `rowVersion` → 409, and the UI reloads.
3. Create branches CAI and ALX. Search "CA" → CAI only. A duplicate code → 409 on the field.
4. Create a warehouse in each branch; each shows a `DEFAULT` location. Edit one: a Transit warehouse keeps
   "sells" off.
5. Add location `A.01`, then `a.01` → 409. Deactivating `DEFAULT` → 422. Deactivate and reactivate `A.01` → 204.
6. Deactivate ALX → 422 (active warehouse). Deactivate its warehouse, then the branch → 204. Activating the
   warehouse → 422 until the branch is active again.
7. In Identity, create a role with `organization.branches.read`, `organization.warehouses.read` and
   `organization.locations.update`. Create a user scoped to branch CAI with it.
8. Sign in as that user:
   * Branches show CAI only; ALX details → 403.
   * Warehouses show CAI's only; ALX's locations → 403.
   * Company edit and branch create → 403.
   * Deactivating a CAI location → 204.
9. As administrator, add a warehouse to CAI. The scoped user sees it without signing in again.

Every step above passed in the verification run.
