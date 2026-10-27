# Lovable prompt — WarehouseERP Organization screens

Copy everything below the line into Lovable. It is built from the backend's real OpenAPI contract
(`docs/api/openapi-v1.json`) and an end-to-end run against the real API; don't invent endpoints or fields.

---

Build the **Organization** screens of WarehouseERP, a trading and distribution ERP. They manage the company, its branches,
its warehouses and the storage locations inside each warehouse.

## Stack and ground rules
- React + TypeScript + Vite, Redux Toolkit with **RTK Query** for every API call. No Supabase, no mock data, no local
  fake backend: every screen talks to the real ASP.NET Core API under `/api` (same origin through the Vite dev proxy).
- **Bilingual UI: Arabic (RTL, default) and English (LTR)** with a language switch. Set `dir` on `<html>`. Use logical
  CSS (`margin-inline-start`, etc.).
- Every entity has `nameAr` (required) and `nameEn` (optional). Show `nameAr` in Arabic and `nameEn ?? nameAr` in
  English.
- Send `null` for an empty optional text field. Codes are ASCII; show them `dir="ltr"`.

## Authentication (cookie session)
If the app has no sign-in yet, add a minimal one:
- `POST /api/identity/login` with `{ userName, password }` answers **204** and sets an HttpOnly session cookie plus an
  `XSRF-TOKEN` cookie. Send every request with `credentials: 'include'`.
- `GET /api/identity/me` → `{ userId, userName, displayName, email, roles: string[], permissions: string[],
  isCompanyWide: boolean, warehouseIds: string[] }`. Call it at startup and after login. It also renews `XSRF-TOKEN`.
- On every POST, PUT and DELETE, read the `XSRF-TOKEN` cookie and send it as the `X-XSRF-TOKEN` header. Do it in the
  RTK Query `baseQuery` `prepareHeaders`.
- `POST /api/identity/logout` signs out. Any 401 means signed out: clear state and go to the login page.

## Permissions and scope
Hide or disable actions with `/me`. `permissions` may contain `"*"`, which means every permission.

| Action | Needs |
|---|---|
| See company | signed in |
| Set up / edit company | `organization.company.update` **and** `isCompanyWide` |
| See branches | `organization.branches.read` |
| Create branch | `organization.branches.create` **and** `isCompanyWide` |
| Rename / activate / deactivate branch | `organization.branches.update` **and** `isCompanyWide` |
| See warehouses and their locations | `organization.warehouses.read` |
| Create warehouse | `organization.warehouses.create` **and** `isCompanyWide` |
| Edit / activate / deactivate warehouse | `organization.warehouses.update` |
| Add location | `organization.locations.create` |
| Rename / activate / deactivate location | `organization.locations.update` |

Lists come back already scoped to the user's access, so never filter them on the client. A 403 is still possible;
show the server's message.

## Shared API shapes
- List: `{ items: T[], page, pageSize, totalCount, totalPages }`. Query `page` (from 1) and `pageSize` (1–100,
  default 20). Use server-side paging and search, debounced 300 ms.
- `201 Created` → `{ id }`. A successful PUT → `{ rowVersion }`. Activate/deactivate → `204` with no body.
- **Concurrency:** every detail read returns `rowVersion` (base64). Send it back unchanged in the PUT, then keep the
  `rowVersion` from the response. **409** `Common.ConcurrencyConflict` means someone else changed the record: show
  "This record was changed by someone else", reload it, and keep the user's edits visible to re-apply.
- **Errors** are ProblemDetails: `{ status, title, detail, errorCode, errors?: [{ property, code, message }], traceId }`.
  - Map `errors[].property` (camelCase, e.g. `nameAr`) to the form field.
  - Branch on `errorCode`, never on the message; show `detail` as a toast for the others.
  - Add Arabic/English texts for the codes in the table at the end.

## 1. Company settings — `/settings/company`
- `GET /api/organization/company` →
  `{ id, code, nameAr, nameEn, baseCurrencyCode, timeZoneId, taxRegistrationNumber, rowVersion }`.
- **404 `Organization.Company.NotConfigured`** means first run.
  - A user with the setup permission gets a **Company setup** form for `POST /api/organization/company` with
    `{ code, nameAr, nameEn, baseCurrencyCode, timeZoneId, taxRegistrationNumber }`.
  - Everyone else gets "The company has not been set up yet".
  - After 201, reload. 409 `Organization.Company.AlreadyConfigured` means someone else just did it: reload.
- Setup form rules:
  - `code`: ≤20 characters, `A–Z a–z 0–9 _ -`.
  - `nameAr`: required, ≤200 characters; `nameEn` ≤200.
  - `baseCurrencyCode`: 3 letters (offer EGP, SAR, AED, USD, EUR plus free entry).
  - `timeZoneId`: IANA id, default `Africa/Cairo` (offer a searchable list).
  - `taxRegistrationNumber`: optional, ≤50, ASCII.
  - Warn that **code and base currency can't be changed later**.
- Edit form: `PUT /api/organization/company` with `{ nameAr, nameEn, timeZoneId, taxRegistrationNumber, rowVersion }`.
  Show code and base currency read-only.
- Show the company name in the app header for every signed-in user. A 404 there is not an error.

## 2. Branches — `/organization/branches`
- List: `GET /api/organization/branches?page&pageSize&search&isActive`.
  - Items are `{ id, code, nameAr, nameEn, isActive }`.
  - `search` matches the code or either name. Status filter: All / Active / Inactive (`isActive` true/false/omitted).
  - Columns: code, name, status badge, actions.
- Create dialog: `POST /api/organization/branches` with `{ code, nameAr, nameEn }`.
  - `code` ≤30, `A–Z a–z 0–9 _ -`; names ≤150.
  - 409 `Organization.Branch.CodeTaken` goes on the code field.
  - 422 `Organization.Company.SetupRequired` links to company setup.
- Details/edit page `/organization/branches/:id`:
  - `GET /api/organization/branches/{id}` → `{ id, companyId, code, nameAr, nameEn, isActive, rowVersion }`.
  - Edit names with `PUT /api/organization/branches/{id}` and `{ nameAr, nameEn, rowVersion }`. The code is
    read-only.
  - Show this branch's warehouses: `GET /api/organization/warehouses?branchId={id}`.
- Activate/deactivate: `POST /api/organization/branches/{id}/activate` or `/deactivate`, after a confirm dialog.
  - 422 `Organization.Branch.HasActiveWarehouses`: "Deactivate this branch's warehouses first". Link to its
    warehouses.
- 403 `Organization.Branch.AccessDenied` on the details page: "You don't have access to this branch".

## 3. Warehouses — `/organization/warehouses`
- List: `GET /api/organization/warehouses?page&pageSize&branchId&search&isActive`.
  - Items are `{ id, branchId, code, nameAr, nameEn, type, allowsSelling, isActive }`.
  - `type` is `"Physical"` or `"Transit"`.
  - Filters: branch (a select fed by `GET /api/organization/branches?isActive=true&pageSize=100`), search, status.
  - Show the branch name by looking `branchId` up in the branches you loaded. Show a "Transit" badge and a "Sells"
    badge.
- Create dialog: `POST /api/organization/warehouses` with
  `{ branchId, code, nameAr, nameEn, type, allowsSelling }`.
  - `code` ≤30, `A–Z a–z 0–9 _ -`.
  - When `type` is Transit, force `allowsSelling` to false and disable the switch.
  - The server creates a `DEFAULT` location automatically; say so in the success toast.
  - Errors: 409 `Organization.Warehouse.CodeTaken` (code field); 422 `Organization.Branch.NotActive` (branch
    field); 422 `Organization.Warehouse.TransitCannotSell`.
- Details page `/organization/warehouses/:id`:
  - `GET /api/organization/warehouses/{id}` →
    `{ id, branchId, code, nameAr, nameEn, type, allowsSelling, isActive, rowVersion, locations: Location[] }`.
  - Header: code, names, type, branch, status.
  - Edit form: `PUT /api/organization/warehouses/{id}` with `{ nameAr, nameEn, allowsSelling, rowVersion }`. Code,
    type and branch are read-only; for Transit, `allowsSelling` stays off.
  - Activate/deactivate: `POST /api/organization/warehouses/{id}/activate` or `/deactivate`. 422
    `Organization.Branch.NotActive` on activate: "Activate the branch first".
- 403 `Organization.Warehouse.AccessDenied`: "You don't have access to this warehouse".

## 4. Warehouse locations — a tab on the warehouse details page
- List: `GET /api/organization/warehouses/{warehouseId}/locations?page&pageSize&search&isActive`.
  - Items (`Location`) are `{ id, code, nameAr, nameEn, isDefault, isActive, rowVersion }`.
  - The default location comes first. Mark it with a "Default" badge and never offer to deactivate it.
- Add dialog: `POST /api/organization/warehouses/{warehouseId}/locations` with `{ code, nameAr, nameEn }`.
  - `code` ≤50, `A–Z a–z 0–9 _ . -` (e.g. `A.01-02`).
  - 409 `Organization.WarehouseLocation.CodeTaken` (codes are unique per warehouse, case-insensitive; `DEFAULT` is
    taken).
  - 422 `Organization.Warehouse.NotActive`: "Activate the warehouse first".
- Edit dialog: `GET .../locations/{locationId}` for a fresh `rowVersion`, then
  `PUT /api/organization/warehouses/{warehouseId}/locations/{locationId}` with `{ nameAr, nameEn, rowVersion }`. The
  code is read-only.
- Activate/deactivate: `POST .../locations/{locationId}/activate` or `/deactivate`. 422
  `Organization.WarehouseLocation.DefaultMustStayActive` if it is ever hit.

## Error codes to translate
| errorCode | Status | Arabic | English |
|---|---|---|---|
| `Organization.Company.NotConfigured` | 404 | لم يتم إعداد بيانات الشركة بعد | The company has not been set up yet |
| `Organization.Company.AlreadyConfigured` | 409 | تم إعداد الشركة بالفعل | The company is already set up |
| `Organization.Company.SetupRequired` | 422 | يجب إعداد الشركة قبل إنشاء الفروع | Set up the company before creating branches |
| `Organization.Company.CompanyWideAccessRequired` | 403 | هذا الإجراء يتطلب صلاحية على مستوى الشركة | This needs company-wide access |
| `Organization.Branch.CodeTaken` | 409 | كود الفرع مستخدم بالفعل | Another branch uses this code |
| `Organization.Branch.NotFound` | 404 | الفرع غير موجود | The branch does not exist |
| `Organization.Branch.AccessDenied` | 403 | ليس لديك صلاحية على هذا الفرع | You don't have access to this branch |
| `Organization.Branch.NotActive` | 422 | الفرع غير موجود أو غير نشط | The branch is missing or inactive |
| `Organization.Branch.HasActiveWarehouses` | 422 | أوقف مخازن الفرع أولاً | Deactivate the branch's warehouses first |
| `Organization.Warehouse.CodeTaken` | 409 | كود المخزن مستخدم في هذا الفرع | Another warehouse in this branch uses this code |
| `Organization.Warehouse.NotFound` | 404 | المخزن غير موجود | The warehouse does not exist |
| `Organization.Warehouse.AccessDenied` | 403 | ليس لديك صلاحية على هذا المخزن | You don't have access to this warehouse |
| `Organization.Warehouse.TransitCannotSell` | 422 | مخزن الترانزيت لا يمكنه البيع | A transit warehouse can't sell |
| `Organization.Warehouse.NotActive` | 422 | المخزن غير نشط | The warehouse is inactive |
| `Organization.WarehouseLocation.CodeTaken` | 409 | كود الموقع مستخدم في هذا المخزن | Another location in this warehouse uses this code |
| `Organization.WarehouseLocation.NotFound` | 404 | الموقع غير موجود | The location does not exist |
| `Organization.WarehouseLocation.DefaultMustStayActive` | 422 | لا يمكن إيقاف الموقع الافتراضي | The default location can't be deactivated |
| `Common.ConcurrencyConflict` | 409 | تم تعديل السجل من مستخدم آخر، أعد التحميل | Changed by someone else, reload |
| `Common.Forbidden` | 403 | ليس لديك صلاحية | You don't have permission |
| `Common.Unauthorized` | 401 | انتهت الجلسة | Signed out |
| `Validation.Failed` | 400 | راجع الحقول المظللة | Check the highlighted fields |

## Backend gaps (don't fake them)
- Warehouse lists return `branchId` only. Resolve the branch name from the branches list.
- `/me` has `warehouseIds` but no branch ids. Rely on the scoped lists instead of computing access.
- There is no endpoint to change which location is the default, and none to move a warehouse to another branch.
- Codes can't be changed after creation (by design).
