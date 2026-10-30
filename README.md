# Warehouse ERP

Modular monolith on .NET 10 / ASP.NET Core / EF Core 10.0.14 / SQL Server, with Clean Architecture inside each
module and vertical slices in the Application layer. One application, one database, one `ErpDbContext`.
Scope: one company with branches and warehouses, trading and distribution only.

* **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**: projects, dependency map, enforced rules, request flow, error contract.
* **[docs/adr/](docs/adr/)**: architecture decisions and their reasons.
* **[CLAUDE.md](CLAUDE.md)**: project guardrails. **[docs/INVENTORY_DECISIONS.md](docs/INVENTORY_DECISIONS.md)**: rules for the coming Inventory module.

## Structure

```
src/
  WarehouseERP.Api/                 composition root (error handling, logging, health, OpenAPI)
  WarehouseERP.Persistence/         ErpDbContext (IdentityDbContext) over every module's configurations, migrations
  BuildingBlocks/                   SharedKernel (Result, Error, MasterEntity), Application and Infrastructure plumbing
  Modules/Organization/             .Domain, .Application, outer project (Infrastructure + Presentation), .Contracts
  Modules/Catalog/                  same layout
  Modules/Identity/                 same layout: users, roles, permissions, scope, sessions (ADR 0009)
  Modules/Inventory/                same layout: stock documents, posting, balances, ledger, valuation, transfers, counts,
                                    reconciliation (ADR 0015)
  Modules/Purchasing/               same layout: suppliers, purchase requests and orders, goods receipts, supplier
                                    invoices and returns; stock only through Inventory's IPurchaseStockPosting (ADR 0016)
  Modules/Sales/                    same layout: customers, price list, product tax classifications, quotations, orders,
                                    deliveries, invoices, returns; reservations and stock only through Inventory (ADR 0018)
  Modules/Pos/                      same layout: terminals, cashier shifts and cash, checkout, receipts, returns and
                                    refunds; prices from Sales, stock only through Inventory's IPosStockPosting (ADR 0019)
  Modules/Accounting/               same layout: chart of accounts and posting-role mappings, fiscal years and periods,
                                    journals (manual, opening, reversal, year-end close), vouchers, customer and supplier
                                    open items and allocations, cashboxes, banks and their reconciliation, reports, and
                                    after the cutover the journals of operational documents with their queue (ADR 0020)
tests/
  WarehouseERP.UnitTests/           no database
  WarehouseERP.IntegrationTests/    SQL Server (LocalDB) + the real HTTP host
  WarehouseERP.ArchitectureTests/   dependency rules
```

Package versions: `Directory.Packages.props` (central). Build settings, analyzers (`latest-recommended`, warnings as
errors) and style: `Directory.Build.props`, `.editorconfig`.

## API

| Method | Route | Permission |
|---|---|---|
| GET | `/api/organization/company` (404 `Organization.Company.NotConfigured` until set up) | signed in |
| POST, PUT | `/api/organization/company` (POST once: first-run setup; PUT settings + `rowVersion`) | `organization.company.update` + company-wide |
| GET | `/api/organization/branches?page&pageSize&search&isActive`, `/branches/{id}` | `organization.branches.read` (company-wide: all; else branches of the user's warehouses) |
| POST | `/api/organization/branches` | `organization.branches.create` + company-wide |
| PUT, POST | `/api/organization/branches/{id}` (names + `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `organization.branches.update` + company-wide |
| POST | `/api/organization/warehouses` (creates its `DEFAULT` location) | `organization.warehouses.create` + company-wide |
| GET | `/api/organization/warehouses?page&pageSize&branchId&search&isActive`, `/warehouses/{id}` | `organization.warehouses.read` + warehouse in scope |
| PUT, POST | `/api/organization/warehouses/{id}` (names, `allowsSelling`, `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `organization.warehouses.update` + warehouse in scope |
| GET | `/api/organization/warehouses/{warehouseId}/locations?page&pageSize&search&isActive`, `/locations/{locationId}` | `organization.warehouses.read` + warehouse in scope |
| POST | `/api/organization/warehouses/{warehouseId}/locations` | `organization.locations.create` + warehouse in scope |
| PUT, POST | `/api/organization/warehouses/{warehouseId}/locations/{locationId}` (names + `rowVersion`), `/activate`, `/deactivate` | `organization.locations.update` + warehouse in scope |
| GET | `/api/organization/countries?isActive`, `/governorates?countryCode&isActive`, `/cities?governorateId&isActive` (whole lists, by Arabic name) | any signed-in user |
| POST, PUT | `/api/organization/{countries,governorates,cities}` (country: ISO `code`, fixed; governorate: `countryCode`; city: `governorateId`), `/{id}` (names + `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `organization.geography.manage` + company-wide |
| GET | `/api/catalog/units?page&pageSize&search&isActive`, `/units/{id}` | `catalog.products.read` |
| POST, PUT | `/api/catalog/units` (code fixed), `/units/{id}` (names, `allowsFraction`, `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `catalog.units.manage` |
| GET | `/api/catalog/categories?isActive` (flat list, `parentId`), `/categories/{id}` | `catalog.products.read` |
| POST, PUT | `/api/catalog/categories` (code fixed), `/categories/{id}` (names, `parentId`, `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `catalog.categories.manage` |
| POST | `/api/catalog/products` | `catalog.products.create` |
| GET | `/api/catalog/products/{id}` (with units and barcodes), `/products?page&pageSize&search&categoryId&type&isActive` (search also matches a barcode exactly) | `catalog.products.read` |
| GET | `/api/catalog/products/lookup?barcode=` (sellable item: active barcode, unit and product) | `catalog.products.read` |
| PUT, POST | `/api/catalog/products/{id}` (names, `categoryId`, `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `catalog.products.update` |
| POST | `/api/catalog/products/{id}/units` (`unitId`, `conversionFactor`), `/units/{productUnitId}/activate`, `/deactivate` | `catalog.products.update` |
| POST | `/api/catalog/products/{id}/units/{productUnitId}/barcodes`, `/products/{id}/barcodes/{barcodeId}/activate`, `/deactivate` | `catalog.products.update` |
| POST, PUT | `/api/inventory/documents` (draft: `documentType`, `warehouseId`, lines), `/documents/{id}` (draft + `rowVersion`) | `inventory.<type>.manage` (`opening`, `receipts`, `issues`, `adjustments`) + warehouse in scope |
| GET | `/api/inventory/documents?page&pageSize&warehouseId&documentType&status&search`, `/documents/{id}` (lines, history) | `inventory.documents.read` + warehouse in scope |
| POST | `/api/inventory/documents/{id}/submit`, `/post` (`Idempotency-Key` header), `/cancel`, `/reverse` | `inventory.<type>.manage` + warehouse in scope |
| POST | `/api/inventory/documents/{id}/approve` (`Idempotency-Key` header; posts it), `/reject` (reason) | `inventory.documents.approve` + warehouse in scope, the creator included |
| GET | `/api/inventory/balances`, `/availability`, `/movements?productId&warehouseId` (stock card), `/over-reservations` | `inventory.stock.read` + warehouse in scope |
| GET | `/api/inventory/valuation?productId`, `/valuation/{productId}/entries` | `inventory.valuation.read` |
| GET, PUT | `/api/inventory/document-policies`, `/document-policies/{documentType}` (`requiresApproval` + `rowVersion`) | `inventory.documents.read` / `inventory.settings.manage` + company-wide |
| POST, PUT | `/api/inventory/transfers` (draft: `sourceWarehouseId`, `destinationWarehouseId`, optional `transitWarehouseId`, lines), `/transfers/{id}` (draft + `rowVersion`) | `inventory.transfers.manage` + source in scope |
| GET | `/api/inventory/transfers?page&pageSize&warehouseId&status&search`, `/transfers/{id}` (lines, documents, history) | `inventory.documents.read` + source or destination in scope |
| GET | `/api/inventory/transfers/destinations` (every active physical warehouse, beyond the user's scope) | `inventory.transfers.manage` |
| POST | `/api/inventory/transfers/{id}/submit`, `/cancel`, `/dispatch` (`Idempotency-Key` header) | `inventory.transfers.manage` + source in scope |
| POST | `/api/inventory/transfers/{id}/approve` (`Idempotency-Key` header; dispatches it), `/reject` (reason) | `inventory.documents.approve` + source in scope, the creator included |
| POST | `/api/inventory/transfers/{id}/receive` (`Idempotency-Key` header; base quantity per line) | `inventory.transfers.receive` + destination in scope |
| POST | `/api/inventory/transfers/{id}/close` (reason; submits the loss adjustment for what never arrived) | `inventory.transfers.manage` + source or destination in scope |
| POST, PUT | `/api/inventory/counts` (`warehouseId`, `locationIds`, none for the whole warehouse), `/counts/{id}/start`, `PUT /counts/{id}/lines`, `/submit`, `/cancel` | `inventory.counts.manage` + warehouse in scope |
| GET | `/api/inventory/counts?page&pageSize&warehouseId&status`, `/counts/{id}` (lines, history) | `inventory.documents.read` + warehouse in scope |
| POST | `/api/inventory/counts/{id}/approve` (`Idempotency-Key` header; posts the differences), `/reject` (reason) | `inventory.documents.approve` + warehouse in scope, the creator included |
| GET | `/api/inventory/reconciliation` (differences only; never corrects) | `inventory.reconciliation.read` + company-wide |
| GET | `/api/inventory/reservations?page&pageSize&warehouseId&productId&includeClosed` (sales order reservations, read-only) | `inventory.stock.read` + warehouse in scope |
| GET, POST, PUT | `/api/purchasing/suppliers?page&pageSize&search&isActive&countryCode`, `/suppliers/{id}`, `/suppliers/similar?nameAr&nameEn&exceptSupplierId` (duplicate warning) / create (code fixed), update (+ `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `purchasing.suppliers.read` / `purchasing.suppliers.manage` |
| GET, PUT | `/api/purchasing/policies`, `/policies/{documentType}` (`PurchaseRequest`, `PurchaseOrder`: `requiresApproval` + `rowVersion`) | `purchasing.documents.read` / `purchasing.settings.manage` + company-wide |
| GET, POST, PUT | `/api/purchasing/requests?page&pageSize&warehouseId&status&orderable&search`, `/requests/{id}` / draft, update, `/{id}/submit`, `/cancel`, `/close` (reason) | `purchasing.documents.read` / `purchasing.requests.manage` + warehouse in scope |
| GET, POST, PUT | `/api/purchasing/orders?page&pageSize&warehouseId&supplierId&status&search`, `/orders/{id}` (lines, documents, history) / draft (lines may name `purchaseRequestLineId`), update, `/{id}/submit`, `/cancel` (reason), `/close` (reason) | `purchasing.documents.read` / `purchasing.orders.manage` + warehouse in scope |
| POST | `/api/purchasing/orders/{id}/receipts` (`Idempotency-Key` header; posts a PurchaseReceipt to stock) | `purchasing.receipts.manage` + warehouse in scope |
| GET | `/api/purchasing/receipts?page&pageSize&warehouseId&supplierId&purchaseOrderId&search`, `/receipts/{id}` | `purchasing.documents.read` + warehouse in scope |
| GET, POST, PUT | `/api/purchasing/invoices?page&pageSize&supplierId&purchaseOrderId&status&search&dueOnOrBefore`, `/invoices/{id}` / draft, update, `/{id}/submit`, `/post` (`Idempotency-Key`), `/cancel` | `purchasing.documents.read` / `purchasing.invoices.manage` + warehouse in scope |
| GET, POST, PUT | `/api/purchasing/returns?page&pageSize&supplierId&purchaseOrderId&goodsReceiptId&status&withCreditClaim&search`, `/returns/{id}` / draft, update, `/{id}/submit`, `/cancel` | `purchasing.documents.read` / `purchasing.returns.manage` + warehouse in scope |
| POST | `/api/purchasing/{requests,orders}/{id}/approve`, `/reject` (reason); `/invoices/{id}/approve`, `/returns/{id}/approve` (`Idempotency-Key`; the return posts a PurchaseReturn to stock), `/reject` | `purchasing.documents.approve` + warehouse in scope, the creator included |
| GET, POST, PUT | `/api/sales/customers?page&pageSize&search&isActive&customerType&isOnCreditHold&countryCode`, `/customers/{id}`, `/customers/similar?nameAr&nameEn&mobile&exceptCustomerId` (duplicate warning), `/customers/{id}/exposure` (partial, informational) / create (code and type fixed), update (+ `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `sales.customers.read` / `sales.customers.manage` |
| PUT | `/api/sales/customers/{id}/credit` (payment terms, credit limit, credit hold + reason, `rowVersion`) | `sales.credit.manage` |
| GET, PUT, DELETE | `/api/sales/prices?page&pageSize&productId&productUnitId` / `/prices/{productUnitId}` (`unitPrice`, 204) | `sales.documents.read` / `sales.prices.manage` |
| GET, PUT | `/api/sales/settings` (approval rules, credit hold policy, default VAT, quotation validity; + `rowVersion`) | `sales.documents.read` / `sales.settings.manage` + company-wide |
| GET, POST, PUT | `/api/sales/quotations?page&pageSize&warehouseId&customerId&status&search`, `/quotations/{id}` / draft, update, `/{id}/submit` (issues it unless approval is needed), `/decline` (reason), `/cancel`, `/convert` (→ draft order) | `sales.documents.read` / `sales.quotations.manage` + warehouse in scope |
| GET, POST, PUT | `/api/sales/orders?page&pageSize&warehouseId&customerId&status&search`, `/orders/{id}` (lines, documents, history) / draft, update, `/{id}/submit` (confirms and reserves unless approval is needed), `/cancel`, `/close` (reason), `/lines/{lineId}/reduce` (quantity, reason) | `sales.documents.read` / `sales.orders.manage` + warehouse in scope |
| GET, POST, PUT | `/api/sales/deliveries?page&pageSize&warehouseId&orderId&customerId&status&search`, `/deliveries/{id}` / draft (picking per location), update, `/{id}/cancel`, `/post` (`Idempotency-Key`; posts a SalesDelivery from the reservation) | `sales.documents.read` / `sales.deliveries.manage` + warehouse in scope |
| GET, POST, PUT | `/api/sales/invoices?page&pageSize&warehouseId&orderId&customerId&status&search`, `/invoices/{id}` / draft, update, `/{id}/cancel`, `/post` (`Idempotency-Key`) | `sales.documents.read` / `sales.invoices.manage` + warehouse in scope |
| GET, POST, PUT | `/api/sales/returns?page&pageSize&warehouseId&orderId&deliveryId&customerId&status&search`, `/returns/{id}` / draft, update, `/{id}/submit`, `/cancel` | `sales.documents.read` / `sales.returns.manage` + warehouse in scope |
| POST | `/api/sales/{quotations,orders,returns}/{id}/approve`, `/reject` (reason); the return's approve takes an `Idempotency-Key` and posts a SalesReturn to stock | `sales.documents.approve` + warehouse in scope, the creator included |
| GET, PUT, DELETE | `/api/sales/product-taxes?page&pageSize&productId&taxTreatment` / `/product-taxes/{productId}` (`taxTreatment` Standard/ZeroRated/Exempt, own `taxRate` or null = default VAT; 204) | `sales.documents.read` / `sales.prices.manage` |
| GET, PUT | `/api/pos/settings` (maximum till discount, return window; + `rowVersion`) | signed in / `pos.settings.manage` + company-wide |
| GET, POST, PUT | `/api/pos/terminals?page&pageSize&warehouseId&isActive&search`, `/terminals/{id}` / register (code and warehouse fixed), update (+ `rowVersion`), `/{id}/activate`, `/{id}/deactivate` | `pos.read` / `pos.terminals.manage` + warehouse in scope |
| GET | `/api/pos/terminals/{id}/current-shift`, `/terminals/{id}/items/lookup?barcode`, `/terminals/{id}/items?search&take`, `/api/pos/customers?search&take` | `pos.sell` + warehouse in scope |
| GET, POST | `/api/pos/shifts?page&pageSize&terminalId&warehouseId&cashierUserId&status`, `/shifts/{id}` (X/Z report) / open (float), `/{id}/cash-movements` (`pos.cash.manage`), `/{id}/close` (count, note; the cashier with `pos.sell`, anyone else forced with `pos.shifts.manage`) | `pos.read` / `pos.sell` + warehouse in scope |
| POST | `/api/pos/sales/quote` (prices a cart, records nothing); `/api/pos/sales` (`Idempotency-Key`; checkout on the caller's open shift, posts a PosSale from unreserved stock) | `pos.sell` + warehouse in scope |
| GET, POST | `/api/pos/sales?page&pageSize&terminalId&shiftId&warehouseId&customerId&cashierUserId&search&from&to`, `/sales/{id}` / `/{id}/reprint` (counted copy) | `pos.read` / `pos.receipts.reprint` |
| GET, POST | `/api/pos/returns?…`, `/returns/{id}` / create (`Idempotency-Key`; refunds and posts a PosReturn), `/{id}/reprint` | `pos.read` / `pos.returns.manage`, `pos.receipts.reprint` |
| GET, POST | `/api/pos/card-incidents?page&pageSize&terminalId&shiftId&status` / `/{id}/resolve` (note, `rowVersion`) | `pos.read` / `pos.shifts.manage` |
| GET, PUT | `/api/accounting/settings` (fiscal-year start month, accounting start date, manual-journal and voucher approval; + `rowVersion`; the start date is fixed once a journal posted or accounting is active) | signed in / `accounting.settings.manage` + company-wide |
| GET, POST | `/api/accounting/cutover` (readiness: each check with what the modules hold and what the opening balances hold), `/cutover/activate` (`rowVersion`; only when every check passes, once) | `accounting.reports.read` / `accounting.integration.manage` + company-wide |
| GET, POST | `/api/accounting/source-postings?status&sourceType&fromDate&toDate&varianceOnly&page&pageSize` (the journaling queue: pending, posted, failed with reason and next try), `/status`, `/reconciliation` (`accounting.reports.read`), `/run` (`fullScan`), `/{id}/retry` | `accounting.journals.read` / `accounting.integration.manage` + company-wide |
| GET, POST, PUT, DELETE | `/api/accounting/accounts?page&pageSize&search&type&isActive&isPosting&parentId`, `/accounts/tree`, `/accounts/{id}` / create, update (+ `rowVersion`), delete (unused only), `/{id}/activate`, `/{id}/deactivate`, `/default-chart`, `/import` (empty chart only) | `accounting.accounts.read` / `accounting.accounts.manage` + company-wide |
| GET, PUT, DELETE | `/api/accounting/account-mappings`, `/account-mappings/{role}` (`accountId`, `rowVersion`) | `accounting.accounts.read` / `accounting.accounts.manage` + company-wide |
| GET, POST | `/api/accounting/fiscal-years`, `/fiscal-years/{id}` / create (`startYear`), `/{id}/close` (`rowVersion`), `/{id}/reopen` (reason); `/api/accounting/fiscal-periods/{id}/close`, `/{id}/reopen` (reason) | `accounting.accounts.read` / `accounting.periods.manage`, `accounting.periods.reopen` + company-wide |
| GET, POST, PUT | `/api/accounting/journals?page&pageSize&status&type&fromDate&toDate&search`, `/journals/{id}` / draft, `/opening` (`accounting.opening.manage`), update, `/{id}/submit`, `/post` (`Idempotency-Key`), `/cancel`, `/reverse` | `accounting.journals.read` / `accounting.journals.manage` (opening: `accounting.opening.manage`) + company-wide |
| POST | `/api/accounting/journals/{id}/approve` (`Idempotency-Key`; posts), `/reject` (reason) | `accounting.journals.approve` + company-wide, the writer included |
| GET | `/api/accounting/reports/trial-balance?fromDate&toDate&branchId&level&includeZero`, `/account-statement?accountId&fromDate&toDate&branchId`, `/general-ledger?fromDate&toDate&accountId&branchId&journalType&page&pageSize`, `/balance-sheet?asOfDate`, `/income-statement?fromDate&toDate&branchId&compareFromDate&compareToDate`, `/aging?partyType&asOfDate&branchId`, `/party-statement?partyType&partyId&fromDate&toDate`, `/subledger-reconciliation?asOfDate` | `accounting.reports.read` + company-wide |
| POST, PUT | `/api/accounting/vouchers` (draft: `type`, cash account, party, method, `amount`, `withholdingAmount`, `taxAmount`, counter `lines`, planned `allocations`), `/vouchers/{id}` (replace a draft; + `rowVersion`); posted, approved, cancelled and reversed through `/api/accounting/journals/{id}/...` | by type: `accounting.receivables.manage` (customer receipts, refunds), `accounting.payables.manage` (supplier payments, refunds, expense bills), `accounting.treasury.manage` (cash vouchers, transfers) + company-wide |
| GET, POST | `/api/accounting/open-items?partyType&partyId&accountId&openOnly&page&pageSize`; `/api/accounting/allocations` (`debitItemId`, `creditItemId`, `amount`), `/allocations/{id}/remove`, `/allocations/auto` (`partyType`, `partyId`) | `accounting.reports.read` / `accounting.receivables.manage` or `accounting.payables.manage` by party + company-wide |
| GET, POST, PUT | `/api/accounting/cash-accounts?kind&branchId&isActive&asOfDate`, `/cash-accounts/{id}` / create, update (+ `rowVersion`; `receivesPosCash`: the branch's one cashbox closed POS shifts hand their cash in to), `/{id}/activate`, `/{id}/deactivate` (zero balance only) | `accounting.accounts.read` / `accounting.treasury.manage` + company-wide |
| GET, POST, PUT | `/api/accounting/bank-reconciliations?cashAccountId`, `/{id}` / start (`cashAccountId`, `statementDate`, `statementBalance`), `/{id}/lines` (ticked `journalLineIds`, `rowVersion`), `/{id}/complete`, `/{id}/cancel` | `accounting.treasury.manage` + company-wide |
| POST | `/api/identity/login` → HttpOnly cookie + `XSRF-TOKEN`; `/api/identity/logout` | anonymous / signed in |
| POST | `/api/identity/tokens` → bearer + refresh; `/tokens/refresh`; `/tokens/revoke` | anonymous / token |
| GET | `/api/identity/me`; POST `/api/identity/me/password`; GET `/api/identity/password-policy` | signed in |
| GET, POST | `/api/identity/users`, GET/PUT `/users/{id}`, POST `/users/{id}/activate`, `/deactivate`, `/unlock`, `/password` | `identity.users.read` / `identity.users.manage` |
| PUT | `/api/identity/users/{id}/roles` | `identity.users.manage` |
| PUT | `/api/identity/users/{id}/access` (company-wide / branches / warehouses + `revision` from GET) | `identity.access.manage` |
| GET, POST | `/api/identity/users/{id}/sessions`, `/users/{id}/sessions/revoke` | `identity.sessions.manage` |
| GET, POST, PUT, DELETE | `/api/identity/roles`, `/roles/{id}` (PUT + `version` from GET); GET `/api/identity/permissions` | `identity.roles.read` / `identity.roles.manage` |
| GET | `/api/identity/security-events?page&pageSize&userId&type&fromUtc&toUtc` (audit trail) | `identity.audit.read` |
| GET, POST | `/api/identity/sessions` (own), `/sessions/{id}/revoke` | signed in (others' need `identity.sessions.manage`) |
| GET | `/health/live`, `/health/ready`, `/api/health` | anonymous |
| GET | `/openapi/v1.json` (Development only) | anonymous |

Errors are ProblemDetails with a stable `errorCode` (see ARCHITECTURE.md). Sign-in is the API's own (ADR 0009): the
React client uses the cookie session and echoes the `XSRF-TOKEN` cookie in an `X-XSRF-TOKEN` header on every
POST/PUT/DELETE; the POS sends `Authorization: Bearer <accessToken>` and refreshes before it expires. Sign-in
endpoints answer only over HTTPS, and tokens are accepted only over HTTPS. Role, permission and scope changes apply on
the user's next request. Login, token issue, token refresh and password change each have their own limit per client
address (429 `Common.TooManyRequests` with `Retry-After`); sign-ins, refused attempts, lockouts, password, role, scope
and session changes and refused escalation attempts are recorded in `identity.SecurityEvents` (ADR 0012).

Any second use of a refresh token ends the session, so the POS keeps one refresh in flight for all its callers,
refreshes shortly before `accessTokenExpiresAtUtc` (not on every 401), never resends a refresh automatically (a
refresh that timed out means signing in again), and treats 401 `Identity.Session.RefreshTokenReused` or
`Identity.Session.Revoked` as signed out.

The API contract is [`docs/api/openapi-v1.json`](docs/api/openapi-v1.json) (the Development `/openapi/v1.json`, checked
by `HostTests`): schema names, operation ids, tags, property names, enum members and status codes are public API.
After a deliberate change, run the integration tests once with `$env:ERP_UPDATE_OPENAPI_SNAPSHOT = '1'` (the contract
test rewrites the file and then fails on purpose), review the diff, remove the variable and run them again.

## Prerequisites

- .NET 10 SDK; SQL Server 2019+ or LocalDB.
- `dotnet-ef` 10.0.14 is a local tool (`dotnet-tools.json`): run `dotnet tool restore` once, then `dotnet dotnet-ef`.
- On the development laptop use **LocalDB**, not `Server=.`: the default SQL Server instance there belongs to another live system.
- Never commit secrets. Outside Development set `Database__ConnectionString` (and, once, `Identity__Bootstrap__*`) as environment variables or in a secret store.
- HTTPS in development too: `dotnet dev-certs https --trust` once. Behind a TLS-terminating proxy, enable `ReverseProxy` with the proxy's address, or sign-in is refused and every client shares one rate limit.

## Configuration

| Section | Keys | Validated at startup |
|---|---|---|
| `Database` | `ConnectionString` (required), `CommandTimeoutSeconds` (5–600, default 30) | yes |
| `Pagination` | `DefaultPageSize` (default 20), `MaxPageSize` (default 100), default ≤ max | yes |
| `Identity:Sessions` | `WebIdleTimeout` (8 h), `WebAbsoluteLifetime` (12 h), `ApiAccessTokenLifetime` (15 min), `ApiRefreshTokenLifetime` (14 d), `ApiAbsoluteLifetime` (30 d), `RetentionAfterEnd` (30 d: ended sessions are then deleted), `CleanupInterval` (1 h) | yes (ranges, idle ≤ absolute, access ≤ refresh ≤ absolute) |
| `Identity:SignInRateLimit` | One section per operation: `Login`, `IssueApiToken`, `RefreshApiToken`, `ChangeMyPassword`, each with `PermitLimit` (60, 60, 300, 30; 1–100000) attempts per client address per `Window` (1 min, 1 s–1 h). An IPv6 address counts as its /64, so one IPv6 LAN segment is one address. Size each for the busiest address a branch shares: about twice its clients that use the operation. Read at startup: restart after a change | yes |
| `Identity:Password` | ASP.NET Core Identity password options; `RequiredLength` 10 (≥ 8), upper/lower/digit required | yes |
| `Identity:Lockout` | `MaxFailedAccessAttempts` 5 (3–20), `DefaultLockoutTimeSpan` 15 min (≥ 1 min) | yes |
| `Identity:Bootstrap` | `UserName`, `Password`, `DisplayName`: first administrator, only while no user exists; never in appsettings | yes (both or neither) |
| `ReverseProxy` | `Enabled` (false), `KnownProxies` (IP addresses), `KnownNetworks` (CIDR), `ForwardLimit` (1, 1–10): trust X-Forwarded-For/-Proto from these proxies only | yes (at least one proxy when enabled, parsable entries) |
| `Serilog` | `MinimumLevel`, overrides, properties | — |

`appsettings.Development.json` points `Database:ConnectionString` at LocalDB (Windows authentication, no secret).

## Database

```powershell
$env:ERP_SQL_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=WarehouseERP_Dev;Trusted_Connection=True;TrustServerCertificate=True'
dotnet tool restore
$p = 'src/WarehouseERP.Persistence/WarehouseERP.Persistence.csproj'

dotnet dotnet-ef migrations script --idempotent --project $p --startup-project $p -o migrations.sql   # inspect first
dotnet dotnet-ef database update --project $p --startup-project $p
dotnet dotnet-ef migrations has-pending-model-changes --project $p --startup-project $p                                  # must report none
```

The design-time factory reads `ERP_SQL_CONNECTION`. `Migration02_Identity` only adds the `identity` schema (and its
`history.*History` tables); it changes no existing table and seeds the system Administrator role.
`Migration03_IdentitySecurity` only adds `identity.SecurityEvents` (append-only audit trail, no foreign keys) and
`identity.RetiredRefreshTokens` (hashes of exchanged refresh tokens, cascading with their session).
`Migration04_Inventory` adds the `inventory` schema (13 tables and the approval-policy seed). `Migration05_InventoryTransfersCounts`
adds transfers and counts (7 tables), two nullable columns on `StockDocuments`, `IdempotencyRecords.SubjectId` (backfilled)
and the `TransferDispatch` policy row; it changes no data. `Migration06_InventorySnapshotIsolation` only turns
`ALLOW_SNAPSHOT_ISOLATION` on, outside a transaction (ADR 0015 *Consequences*). `Migration07_Purchasing` adds the
`purchasing` schema (20 tables; suppliers, contacts and addresses temporal) and, on `inventory.StockDocuments` only,
widens `CK_StockDocuments_Type` to 1–10 and adds `SourceDocumentId` (nullable, filtered unique, set exactly for
purchase receipts and returns); it changes no data (ADR 0016 *Consequences*). `Migration08_Geography` adds
`organization.Countries`, `Governorates` and `Cities` (temporal), seeds 249 countries and Egypt's 27 governorates and 271
cities, and turns a supplier address's text city, region and country into `CityId`, matching existing rows by name; it
stops, changing nothing, if one doesn't match (ADR 0017). `Migration09_InventoryReservations` adds
`inventory.StockReservations` and `StockReservationEntries` and widens the stock document type and source checks to
types 11–12 (sales delivery and return); `Migration10_Sales` adds the `sales` schema (22 tables; customers, contacts,
addresses and prices temporal) and the settings row. Neither changes data (ADR 0018 *Consequences*).
`Migration11_InventoryPos` widens the stock document type and source checks to types 13–14 (POS sale and return);
`Migration12_SalesProductTaxes` adds `sales.ProductTaxes` (temporal); `Migration13_Pos` adds the `pos` schema (14 tables;
terminals temporal) and the POS settings row. None changes data (ADR 0019 *Consequences*).
`Migration14_Accounting` adds the `accounting` schema only (11 tables; accounts, posting-role mappings and settings
temporal) and the settings row (calendar fiscal years, accounting start 2027-01-01, manual journals need approval); it
changes no existing table or data (ADR 0020 *Consequences*). `Migration15_AccountingSubledgers` adds cash accounts
(temporal), open items, allocations, voucher allocations and bank reconciliations, nullable voucher and party columns on
journals and lines, and the voucher-approval setting (off); it changes no existing data (ADR 0020 *Consequences*).
`Migration16_AccountingIntegration` adds the operational documents' accounting identities (`SourcePostings`) and scan
cursors, nullable source columns on journals, the POS-cashbox flag of cash accounts and the activation columns of the
settings (null: not active); it changes no existing data. Automatic journaling starts only when the cutover is activated
(`POST /api/accounting/cutover/activate`); the worker is configured under `Accounting:Integration` (`WorkerEnabled`,
`Interval`, `ScanOverlap`, `BatchSize`).

## Running the API

```powershell
cd src/WarehouseERP.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run
```

In Development the host validates the whole DI container at startup and serves OpenAPI. To create the first
administrator, set it for one start while the user table is empty, then remove it:

```powershell
$env:Identity__Bootstrap__UserName = 'admin'
$env:Identity__Bootstrap__Password = '<a password meeting Identity:Password>'
```

## Tests

```powershell
dotnet test WarehouseERP.slnx
```

On this laptop builds can run out of memory with parallel nodes; add `-m:1` if MSBuild reports `OutOfMemoryException`.

- `WarehouseERP.UnitTests`: Result and its HTTP mapping, the handler pipeline, paging rules, domain rules, validators, handlers against fakes.
- `WarehouseERP.IntegrationTests`: each run creates `WarehouseERP_Tests_<guid>` on LocalDB, applies the migrations and drops it. Covers constraints, temporal history and audit stamps, EF model vs migrations, unique-key and concurrency mapping, the endpoints' validation 400, business 422, conflicts 409, scope 403 and 401, sanitized 500 logged once, health, options validation, OpenAPI, DI. Identity runs on real sessions over https: cookie and bearer sign-in, identical 401s, lockout (also under parallel guesses), stale user saves refused after a counted attempt, what a failed login save leaves behind, administrative reset and unlock, logout/revocation/deactivation/password change effective on the next request, idle expiry, session cleanup, anti-forgery, refresh rotation, reuse and racing refreshes, permission removal and branch grants applied without re-login, no privilege escalation, per-operation rate limits, plain-HTTP tokens ignored, forwarded headers from trusted proxies only, the audit trail's permission, bootstrap (own database), no secrets in logs, the committed OpenAPI contract. Set `ERP_TEST_SQL_SERVER` for another instance.
- `WarehouseERP.ArchitectureTests`: layer and module rules, composition root, no cycles, entity ownership per schema.

## Constraints worth noting

1. **One DEFAULT location**: `Warehouse.Create` always adds it; a filtered unique index refuses a second one. Never deactivate the only default location.
2. **Base unit**: `Product.BaseUnitId` is canonical; `Product.Create` adds its `ProductUnit` row at factor 1. Do not change `BaseUnitId` once inventory exists.
3. **Temporal master history**: `ValidFrom/ValidTo` are SQL Server-generated; `history.*History` versions master changes. `CreatedBy/UpdatedBy` are stamped by `AuditingSaveChangesInterceptor` from `ICurrentUser`.
4. **No cascade deletes**: master data is retired with `Deactivate()`.
5. **Tax periods**: `[EffectiveFrom, EffectiveTo)`; overlapping ranges need serialized validation in the future tax commands.
6. **Category cycles**: the database refuses only a self-parent; `UpdateCategory` refuses deeper loops.
7. **Sequential GUID keys**: never assign `Guid.NewGuid()` to a key; EF Core generates SQL Server-ordered values.
8. **ExternalItemCode**: optional `ProductUnitId` must belong to the same product (composite FK).
9. **Known unique constraints** are mapped to business codes by name (`IX_Products_SKU`, `IX_Warehouses_BranchId_Code`); renaming an index means updating its repository map.
10. **Stock changes only by posting**: balances, availability, movements and cost entries are written by `Application/Shared/StockDocumentPosting` in one transaction under Inventory's locks (ADR 0015), for documents, transfers and counts alike. Never update `inventory.*` rows by hand; post an adjustment or a reversal. A running count's freeze is its active `StockCountScopes` rows.
11. **Snapshot isolation**: `Migration06` turns `ALLOW_SNAPSHOT_ISOLATION` on for the reconciliation report's consistent read; everything else keeps READ COMMITTED.
12. **Purchasing posts stock only through Inventory**: goods receipts and supplier returns call `IPurchaseStockPosting` inside their own transaction; the PRC/PRT stock documents name their source (`SourceDocumentId`) and can't be edited or reversed in Inventory. Order, invoice and return quantities are guarded by check constraints as well as the order's row lock (ADR 0016).
13. **Approvals**: anyone with the approval permission and the warehouse in scope approves, the creator included (`CLAUDE.md`, *Document approvals*).
14. **Reservations belong to Inventory**: Sales reserves, releases and consumes stock only through `IStockReservations` and `ISalesStockPosting`; `ProductWarehouseStock.ReservedQuantity` always equals what open `StockReservations` hold (the reconciliation report checks it). Delivery, invoice and return quantities are guarded by check constraints as well as the order's row lock (ADR 0018).
15. **POS sells only unreserved stock, in one transaction**: a checkout locks its key and the terminal's open shift, prices the cart through Sales (`ISalesPricing`, no VAT logic in POS) and posts a PosSale through `IPosStockPosting`; sale, payments, stock, shift totals, receipt number and idempotency record commit together. A refused checkout with card payments records a card incident afterwards and answers `Pos.Checkout.CardReversalRequired`. Payment, drawer and return arithmetic are also check constraints (ADR 0019).
16. **The ledger is computed from posted journal lines only**: every journal balances by its own lines (also a check constraint), posted journals never change (corrections are reversals or new journals), posting takes its period's lock shared and closing takes it exclusively, so nothing posts into a closed period; numbers are per journal type and fiscal year, postings idempotent (ADR 0020).

## Postponed

See `docs/adr/` for reasons: Data Protection key encryption at rest, MFA/SSO, POS device registration and offline grants (ADR 0009), OpenTelemetry exporters, the outbox/inbox (until a consumer exists, ADR 0015),
management of tax codes, rates, product tax assignments and EGS/GS1 external item codes (Sales/e-invoicing phase,
ADR 0014), batch/serial tracking (`docs/modules/Inventory/V1_DECISIONS.md`, ADR 0015 *Not done*),
foreign-currency purchasing and invoice revaluation (ADR 0016 *Not done*), post-dated cheques, bank statement import,
VAT at delivery and the revaluation of supplier price differences (ADR 0020 *Not done*), backorders,
quarantine returns, customer price lists, credit notes and Egyptian e-invoicing (ADR 0018 *Not done*; customer receipts are Accounting vouchers),
offline POS, integrated card gateways and wallets, held carts, promotions and the ETA e-Receipt (ADR 0019 *Not done*).
