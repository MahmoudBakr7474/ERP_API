# Accounting — frontend handoff

Verified 2026-10-30 on `feature/accounting` (merged into `main` the same day) against the real API. An end-to-end run
(`.artifacts/e2e-accounting-host.ps1` + `e2e-accounting.mjs`) used a throwaway LocalDB database with all sixteen
migrations, the real host over HTTPS, real cookie sessions and anti-forgery, the accounting worker every 15 s, a
bootstrap administrator and two users created through Identity: `acc-clerk` (company-wide; read, manual journals,
reports — no approval, no integration) and `acc-branch` (every Accounting permission but limited to one warehouse). The
test cutover was set explicitly to **2026-10-01** on that database. **57 checks passed**; the API log had no errors and no
passwords; both throwaway databases were dropped. The examples below are real responses (ids shortened).

The Lovable prompt is [`LOVABLE_PROMPT.md`](LOVABLE_PROMPT.md). Business rules: [`V1_DECISIONS.md`](V1_DECISIONS.md) and
[ADR 0020](../../adr/0020-accounting-module.md).

## Where it goes
- The screens extend the existing ERP frontend (Lovable project `mahmoudtaha311/screenshot-match`, cloned to
  `D:\work\WarehouseERP\warehouse-erp-web`), following its `AGENTS.md` UI conventions; code in `src/features/accounting/`.
- Running the backend, the Vite proxy (`http://localhost:8080` → `https://localhost:49571`) and the cookie/XSRF rules are
  the same as for [Organization](../Organization/FRONTEND_HANDOFF.md).
- OpenAPI tags: `AccountingSettings`, `Accounts`, `AccountMappings`, `FiscalYears`, `Journals`, `Vouchers`, `OpenItems`,
  `CashAccounts`, `BankReconciliations`, `Cutover`, `SourcePostings`, `FinancialReports`; Sales' `Customers` exposure
  gained four properties.
- **`WarehouseERP_Dev` (2026-10-30):** Migrations 14–16 are applied (after a verified copy-only backup and test restore,
  no existing row changed); the dev API on `https://localhost:49571` runs the `feature/accounting` build and its live
  OpenAPI equals the committed one. **Accounting is not activated there**: the chart is empty, no fiscal year, the start
  date is the seeded 2027-01-01, nothing is journaled. See *Trying the screens on the dev database*.
- Every Accounting handler requires **company-wide access** on top of its permission; a branch- or warehouse-scoped user
  gets 403 `Accounting.Access.CompanyWideRequired`.
- **Self-approval:** a user holding `accounting.journals.approve` approves journals they wrote; there is no second-person
  step.

## Frontend integration notes (2026-10-30)
Lovable's screens (frontend `main` `66ddf07`) were integrated on `fix/accounting-integration` (`ecd86a4`, now the frontend `main`) and
browser-tested against the real API (dev database read-only; every workflow on a throwaway database). What a screen must
respect, learned from the fixes:
- A voucher journal's `voucher` has no `type`, `lines`, `branchId` or net amount: the type is the journal's `journalType`;
  the edit form reads the request back from the server-built `lines` (cash receipt: cash first; cash payment: cash last;
  expense bill: expense lines, then input VAT when there is tax, then the supplier) and `amount` is the settled total
  (net cash = `amount` − `withholdingAmount` for receipts and payments).
- Reversal links are `reversalOfNumber` / `reversedByNumber` (+ `reversedByStatus`); a reversal draft's update sends
  `lines: null` (`Accounting.Journal.LinesFixed` otherwise).
- The bank reconciliation list is `BankReconciliationSummaryResponse` (`cashAccountCode`, `clearedLines`, `clearedAmount`),
  not the detail.
- Activate/deactivate, delete, period reopen and allocation removal answer **204**: treat no body as success.
- Every journal and voucher line needs a `branchId` (a Guid; null fails binding); report dates are required (`fromDate` +
  `toDate`, or `asOfDate`).
- In RTK Query, never pass `refetch` of a query skipped in create mode as a post-save reload: it throws and aborts the
  success path (the dialog stays open and a second click creates a duplicate).
- Translations: `acc.role.<PostingRole>` for the 28 roles, `acc.ev.<Action>` for history events and `err.<code>` for every
  `Accounting.*` code are now in the frontend's `translations.ts`.

## Endpoints
All under `/api/accounting`. Lists that page answer `{ items, page, pageSize, totalCount, totalPages }`. "In handler" means
the endpoint only needs a signed-in user and the handler checks the permission the record's type needs.

| Method | Route | Permission | Body / notes |
|---|---|---|---|
| GET / PUT | `/settings` | signed in / `settings.manage` | `{ fiscalYearStartMonth, cutoverDate, manualJournalsRequireApproval, vouchersRequireApproval, rowVersion }` → `{ rowVersion }` |
| GET | `/cutover` | `reports.read` | readiness: `{ cutoverDate, cutoverStartUtc, isActivated, activatedAtUtc, ready, checks[], rowVersion }` |
| POST | `/cutover/activate` | `integration.manage` | `{ rowVersion }` (readiness's) → `{ rowVersion }`; once only |
| GET | `/accounts?page&pageSize&search&type&isActive&isPosting&parentId`, `/accounts/tree`, `/accounts/{id}` | `accounts.read` | |
| POST / PUT / DELETE | `/accounts`, `/accounts/{id}` | `accounts.manage` | create `{ code, nameAr, nameEn, parentId, type, section, isPosting, allowsManualPosting, partyType }`; update `{ nameAr, nameEn, section, allowsManualPosting, rowVersion }` |
| POST | `/accounts/{id}/activate`, `/deactivate`, `/accounts/default-chart`, `/accounts/import` | `accounts.manage` | import `{ accounts: ChartAccountRow[], roles: [{ role, accountCode }] }` → `{ accountsCreated, rolesMapped }` |
| GET / PUT / DELETE | `/account-mappings`, `/account-mappings/{role}` | `accounts.read` / `accounts.manage` | `{ accountId, rowVersion }` |
| GET / POST | `/fiscal-years`, `/fiscal-years/{id}` | `accounts.read` / `periods.manage` | `{ startYear }` |
| POST | `/fiscal-years/{id}/close` / `/reopen` | `periods.manage` / `periods.reopen` | `{ rowVersion }` / `{ reason, rowVersion }` |
| POST | `/fiscal-periods/{id}/close` / `/reopen` | `periods.manage` / `periods.reopen` | none → `{ periodId, openJournals, unpostedDocuments }` / `{ reason }` |
| GET | `/journals?page&pageSize&status&type&fromDate&toDate&search`, `/journals/{id}` | `journals.read` | |
| POST | `/journals`, `/journals/opening` | `journals.manage` / `opening.manage` | `{ postingDate, description, reference, lines: JournalLineRequest[] }` (opening: no date) |
| PUT | `/journals/{id}` | in handler | `{ postingDate, description, reference, lines, rowVersion }` |
| POST | `/journals/{id}/submit`, `/cancel` | in handler | `{ rowVersion }` |
| POST | `/journals/{id}/post` | in handler | `{ rowVersion }` + `Idempotency-Key` |
| POST | `/journals/{id}/approve` / `/reject` | `journals.approve` | `{ rowVersion }` + `Idempotency-Key` / `{ reason, rowVersion }` |
| POST | `/journals/{id}/reverse` | `journals.manage` | `{ postingDate, description, reference }` → 201 reversal draft |
| POST / PUT | `/vouchers`, `/vouchers/{id}` | in handler (by type) | see the prompt's section 5 |
| GET | `/open-items?page&pageSize&partyType&partyId&accountId&openOnly` | `reports.read` | |
| POST | `/allocations`, `/allocations/auto`, `/allocations/{id}/remove` | in handler (by party) | `{ debitItemId, creditItemId, amount }` / `{ partyType, partyId }` |
| GET / POST / PUT | `/cash-accounts?kind&branchId&isActive&asOfDate`, `/cash-accounts/{id}`, `/{id}/activate`, `/{id}/deactivate` | `accounts.read` / `treasury.manage` | create `{ code, name, kind, branchId, accountId, bankName, bankAccountNumber, allowsOverdraft, receivesPosCash }` |
| GET / POST / PUT | `/bank-reconciliations?cashAccountId`, `/{id}`, `/{id}/lines`, `/{id}/complete`, `/{id}/cancel` | `treasury.manage` | start `{ cashAccountId, statementDate, statementBalance }`; lines `{ journalLineIds, rowVersion }` |
| GET | `/source-postings?page&pageSize&status&sourceType&fromDate&toDate&varianceOnly`, `/source-postings/status` | `journals.read` | the queue and its status |
| GET | `/source-postings/reconciliation` | `reports.read` | source-to-ledger reconciliation |
| POST | `/source-postings/run`, `/source-postings/{id}/retry` | `integration.manage` | `{ fullScan }` / none |
| GET | `/reports/trial-balance`, `/general-ledger`, `/account-statement`, `/balance-sheet`, `/income-statement`, `/aging`, `/party-statement`, `/subledger-reconciliation` | `reports.read` | query parameters in the prompt's section 9 |
| GET | `/api/sales/customers/{id}/exposure` | `sales.customers.read` | + `receivableBalance`, `unjournaledAmount`, `totalExposure`, `availableCredit` |

`JournalLineRequest`: `{ accountId, branchId, debit, credit, description, partyType?, partyId?, dueDate? }` (one side per
line; `partyType`/`partyId` only and always on customer and supplier control accounts).

## Examples (real responses)
Readiness once every check passes (`GET /cutover`, trimmed to 3 of 9 checks):
```json
{ "cutoverDate": "2026-10-01", "cutoverStartUtc": "2026-09-30T21:00:00Z", "isActivated": false, "activatedAtUtc": null,
  "ready": true, "checks": [ { "code": "CutoverStarted", "passed": true, "expected": null, "actual": null, "message": null },
  { "code": "FiscalPeriodOpen", "passed": true, "expected": null, "actual": null, "message": null },
  { "code": "PostingRolesMapped", "passed": true, "expected": null, "actual": null, "message": null } ],
  "rowVersion": "AAAAAAAAJ6E=" }
```
A document waiting in the queue (`GET /source-postings?status=Failed`, one item):
```json
{ "id": "cac0…dd9d", "sourceType": "PosShift", "sourceId": "7259…e048", "sourceNumber": "CAI-T1-SH-2026-000001",
  "occurredAtUtc": "2026-10-30T10:30:54.741Z", "postingDate": "2026-10-30", "status": "Failed", "journalId": null,
  "journalNumber": null, "attempts": 1, "nextAttemptAtUtc": "2026-10-30T10:31:55.292Z",
  "errorCode": "Accounting.Source.PosCashboxMissing",
  "errorMessage": "The branch of shift CAI-T1-SH-2026-000001 has no active cashbox receiving POS cash: mark one, then retry.",
  "stockValue": null, "postedStockValue": null, "varianceAmount": null }
```
Queue status after everything posted (`GET /source-postings/status`, cursors trimmed):
```json
{ "isActivated": true, "activatedAtUtc": "2026-10-30T10:30:49.109Z", "cutoverDate": "2026-10-01", "workerEnabled": true,
  "workerIntervalSeconds": 15, "pending": 0, "failed": 0, "posted": 14, "nothingToPost": 0, "oldestUnsettledAtUtc": null,
  "cursors": [ { "module": "Inventory", "scannedToUtc": "2026-10-30T10:30:57.051Z", "updatedAtUtc": "2026-10-30T10:30:57.051Z" } ] }
```
Reconciliation (`GET /source-postings/reconciliation`, trimmed):
```json
{ "isActivated": true, "cutoverDate": "2026-10-01",
  "inventory": { "ledgerBalance": 648, "valuation": 648, "difference": 0, "openingDifference": 0, "roundingDifference": 0,
                 "unpostedValue": 0, "unexplained": 0 },
  "clearingAccounts": [ { "role": "GoodsReceivedNotInvoiced", "ledgerBalance": 0, "sourceValue": 0, "difference": 0 },
                        { "role": "UnbilledReceivables", "ledgerBalance": 0, "sourceValue": 0, "difference": 0 },
                        { "role": "PosTillClearing", "ledgerBalance": 0, "sourceValue": 0, "difference": 0 } ],
  "missingCount": 0, "unmatchedJournals": 0,
  "cashDifferences": [ { "shiftId": "7259…e048", "number": "CAI-T1-SH-2026-000001", "closedAtUtc": "2026-10-30T10:30:54.741Z",
                         "cashDifference": -2, "isForceClosed": false } ],
  "isReconciled": true }
```
A system journal (`GET /journals/{id}`, a sales invoice's; header and first line):
```json
{ "number": "SL-2026-000002", "journalType": "SalesJournal", "status": "Posted", "postingDate": "2026-10-30",
  "description": "فاتورة مبيعات INV-CAI-2026-000001", "reference": "INV-CAI-2026-000001", "linesFixed": true,
  "totalDebit": 684, "sourceType": "SalesInvoice", "sourceId": "14bd…dfb0", "voucher": null, "postedByName": null,
  "lines": [ { "lineNumber": 1, "accountCode": "120201", "accountNameAr": "العملاء", "branchCode": "CAI", "debit": 684,
               "credit": 0, "partyType": "Customer", "partyCode": "CUST-1", "partyNameAr": "عميل", "dueDate": "2026-10-30" } ],
  "events": [ { "action": "Created", "userName": null, "note": "INV-CAI-2026-000001" }, { "action": "Posted", "userName": null } ] }
```
(the other lines: unbilled receivables 120202 credit 600, output VAT 220201 credit 84 — revenue was recognized when the goods
were delivered.)

A supplier payment voucher (`voucher` of its journal; lines Dr supplier 200, Cr bank 190, Cr tax withheld 10):
```json
{ "cashAccountCode": "BANK-NBE", "partyType": "Supplier", "partyCode": "SUP-1", "paymentMethod": "BankTransfer",
  "amount": 200, "withholdingAmount": 10, "taxAmount": null,
  "allocations": [ { "openItemId": "740c…dd78", "documentNumber": "OB-2026-000001", "amount": 200 } ] }
```
Sales exposure once Accounting is active (`GET /api/sales/customers/{id}/exposure`):
```json
{ "isPartial": false, "creditLimit": null, "uninvoicedOrderAmount": 0, "invoicedAmount": 684, "pendingCreditAmount": 171,
  "receivableBalance": 913, "unjournaledAmount": 0, "totalExposure": 913, "availableCredit": null }
```
Report totals of the run: trial balance for October `periodDebit` = `periodCredit` = 20,984.70, `isBalanced`; balance
sheet `totalAssets` 14,009.10 = liabilities 1,519.10 + equity 12,490 (unclosed earnings included), `isBalanced`; income
statement revenue 470, cost of sales 312; customer aging 400 overdue 31–60 days, 684 not due, −171 unallocated, 913 total.

## What the end-to-end run covered
Chart (default Egyptian chart, 28 roles mapped, a child code outside its parent refused), fiscal year with 12 periods,
the controlled test cutover, readiness failing then passing, activation refused while not ready, refused to a clerk and a
branch-scoped user, done once; no opening journal after it. Opening balances with customer and supplier items and their
due dates, approved by their writer. A manual journal: unbalanced and system-account lines refused, approval required,
the clerk refused, an idempotent approval (same key → same answer, same key elsewhere → 409), its reversal (fixed lines,
once). A partial customer receipt and a partial supplier payment with tax withheld settling the opening items; a transfer,
a cash payment and the cash guard. Purchasing (receipt, invoice with a price difference, return), Sales (two deliveries,
an invoice, a return) and POS (cash and card sales, a card refund, a short shift) journaled by the run; the shift failing
without a POS cashbox and posting on retry; every document journaled once; a system journal not reversible; a full scan
finding nothing new; a closed period refusing a journal and holding a document in the queue until reopened; the worker
journaling a document by itself; the reconciliation reconciled; trial balance, balance sheet, income statement, aging and
the subledger reconciliation; the queue readable but not runnable by the clerk.

## Rules the screens must respect
- Show the server's numbers; never compute balances, totals, taxes or allocations in the browser.
- Two decimals at most; one side per line; a party on control-account lines only.
- Approve may be done by the journal's writer. Post and approve carry an `Idempotency-Key`.
- Automatic journals (`InventoryJournal`, `PurchasingJournal`, `SalesJournal`, `PosJournal`) and closing journals are read-only.
- A closed period refuses postings; the queue keeps the documents dated in it until it reopens (then Retry).
- Activation is explicit, once, by a user with `accounting.integration.manage`; the UI never triggers it by itself.

## Trying the screens on the dev database
The dev database has no Accounting data, and its start date (2027-01-01) is in the future, so readiness reports
`CutoverStarted` failing and nothing is journaled automatically — by design, nothing from the existing dev documents will
be posted unless the owner decides so.
1. Sign in as an administrator (company-wide). Accounting → Chart of accounts → "Default Egyptian chart".
2. Fiscal years → create 2027 (the year of the start date). Settings stay as they are.
3. Journals: write a manual journal dated in 2027, submit, approve (the same user may), reverse it; try an unbalanced one
   and a line on 120101 (system only) to see the errors.
4. Cash and banks: create a cashbox and a bank on new accounts under 1204; post an opening-balance journal for them (it is
   dated 2027-01-01, needs approval); post a transfer and a cash payment dated in 2027; try to overdraw the cashbox.
5. Receivables / Payables: an opening item for a dev customer and supplier, a partial receipt and payment with allocation,
   aging and party statements; the subledger reconciliation.
6. Setup and cutover: read the checklist (it shows what would block activation). **Don't activate on the dev database**
   unless the owner wants every dev document posted from the chosen start date journaled; to see the automatic journals,
   run `.artifacts/e2e-accounting-host.ps1` (a throwaway database) instead.

## Error codes
`Accounting.Access.*`: CompanyWideRequired, PermissionDenied. `Accounting.Account.*`: CodeInvalid, CodeOutsideParent,
CodeTaken, HasActiveChildren, HasBalance, HasChildren, HasLines, Mapped, NameInvalid, NotFound, ParentIsPosting,
ParentNotActive, ParentNotFound, PartyInvalid, SectionDoesNotFitType, SectionRequired, TooDeep, TypeDiffersFromParent,
TypeInvalid. `Accounting.Allocation.*`: AccountMismatch, AlreadyRemoved, AmountInvalid, Busy, ExceedsOpen, ItemNotFound,
NotFound, PartyMismatch, SidesInvalid. `Accounting.BankReconciliation.*`: BeforeLastStatement, DraftExists, LineNotFree,
LineTicked, NotBalanced, NotBank, NotDraft, NotFound. `Accounting.CashAccount.*`: AccountInUse, AccountInvalid,
AccountNotFound, BankDetailsOnCashbox, BankDetailsTooLong, BranchNotActive, BranchNotFound, CodeInvalid, CodeTaken,
HasBalance, KindInvalid, NameInvalid, NotFound, PosCashNotCashbox, PosCashboxTaken. `Accounting.Chart.*`: CodeDuplicate,
NotEmpty, ParentMissing, RoleAccountMissing. `Accounting.Company.SetupRequired`. `Accounting.Cutover.NotReady`.
`Accounting.FiscalPeriod.*`: NotClosed, NotFound, NotOpen. `Accounting.FiscalYear.*`: Busy, Closed, EarlierYearOpen,
Exists, LaterYearClosed, NotClosed, NotContiguous, NotFound, PeriodsStillOpen, ReasonRequired, StartNotMonthStart.
`Accounting.Idempotency.*`: Busy, KeyReused. `Accounting.Journal.*`: AccountNotActive, AccountNotFound, AccountNotManual,
AccountNotPosting, AlreadyReversed, ApprovalNotRequired, ApprovalRequired, BeforeCutover, BranchNotActive, BranchNotFound,
Busy, CashAccountOverdrawn, DescriptionInvalid, ItemsAllocated, LineAmountScale, LineAmountTooLarge, LineDescriptionTooLong,
LineIncomplete, LineOneSide, LinePartyIncomplete, LinesFixed, NotDraft, NotFound, NotOpen, NotPosted, NotReversible,
NotSubmitted, OpeningAfterActivation, OpeningDateMismatch, PartyNotAllowed, PartyNotFound, PartyRequired, PeriodBusy,
PeriodClosed, PeriodMissing, ReasonRequired, ReferenceTooLong, ReversalBeforeOriginal, TooFewLines, TooManyLines,
Unbalanced, VoucherManaged. `Accounting.Mapping.*`: AccountDoesNotFitRole, AccountNotActive, AccountNotFound, NotFound,
NotPostingAccount, RoleInvalid, RoleNotMapped. `Accounting.Report.*`: AccountNotFound, TooManyLines.
`Accounting.Settings.*`: AlreadyActivated, CutoverLocked, CutoverNotPeriodStart, CutoverNotStarted, StartMonthInvalid,
StartMonthLocked. `Accounting.Source.*`: Busy, DocumentNotFound, NotActive, NotFound, PosCashboxMissing, Unexpected,
UnsupportedDocument. `Accounting.Voucher.*`: AllocationExceedsOpen, AllocationInvalid, AllocationItemMismatch,
AllocationItemNotFound, AllocationsExceedAmount, AllocationsNotAllowed, AmountInvalid, BillNumberRequired,
BillNumberTaken, BranchNotActive, BranchNotFound, CashAccountNotActive, CashAccountNotFound, CashNotAllowed, CashRequired,
ChequeNumberRequired, CounterAccountInvalid, CounterCashNotAllowed, CounterCashRequired, DueBeforeBill, LineAmountInvalid,
LinesNotAllowed, LinesRequired, MethodDoesNotFitCash, MethodNotAllowed, MethodRequired, NotVoucher, PartyNotAllowed,
PartyNotFound, PartyRequired, PartyTypeMismatch, SameCashAccount, TaxInvalid, TooManyLines, WithholdingInvalid.
Plus `Common.ConcurrencyConflict`, `Common.Forbidden`, `Validation.Failed`.

## Known limits (V1)
- Base currency only; no post-dated cheque portfolio, no bank statement import or automatic matching.
- VAT is recognized at the invoice; supplier price differences stay under review until the accountant reclassifies them
  by journal; card settlements are recorded by the treasury (cash receipt with card clearing as the counter line, fees by
  journal) — all three are accountant decisions (V1 decisions, section 8).
- From an operational document to its journal there is no direct endpoint: search the journals by the document's number
  (the journal's `reference`).

## UI backlog (non-blocking, recorded at the final check, 2026-10-30)
Seen in the browser; none blocks a workflow, the server refuses or corrects each case.
- Fiscal years: period "Reopen" buttons still show while the year is closed (the server answers
  `Accounting.FiscalYear.Closed`, translated); the year-close confirmation names the year but does not explain that the
  closing journal moves the year's revenue and expenses to retained earnings.
- Reversal drafts: the fixed lines' branch pickers still look editable (changes are ignored; the update sends
  `lines: null`).
- Multi-line editors (journal and voucher lines, party pickers) reuse the same element ids on every line, so a label points
  to the first line's field (accessibility).
- The party picker lists customers and suppliers through Sales and Purchasing, so an accounting-only user without
  `sales.customers.read` / supplier read gets a 403 inside the picker.
- Printing a report prints the page as shown (sidebar and header included; filters and buttons hidden); wide tables such
  as the trial balance can be clipped on A4 — a dedicated report print layout would help.
- The general ledger shows amounts without the currency code (its paged response has none); other reports show it.
- Readiness check details and errors whose server message carries a line number show the server's English detail under
  the Arabic message; the readiness check titles and every error code are translated.
- On phones, tables scroll inside their card (no page overflow; dialogs keep Save visible) — usable but dense.
- POS (seen while scoping the print rule): `@page { size: 80mm auto }` is invalid CSS, so browsers ignore the receipt
  size and keep only the zero margin; receipts rely on the printer's paper setting, as before.
