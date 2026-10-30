# Lovable prompt — WarehouseERP Accounting screens

Copy everything below the line into Lovable, in the same project as the Organization, Identity, Catalog, Inventory,
Purchasing, Sales and POS screens. It is built from the backend's real OpenAPI contract (`docs/api/openapi-v1.json`,
55 paths under `/api/accounting`) and an end-to-end run against the real API (57/57 checks); don't invent endpoints,
fields or rules. Example payloads, the endpoint table and manual test steps are in
[`FRONTEND_HANDOFF.md`](FRONTEND_HANDOFF.md).

---

Add the **Accounting** module (general ledger, receivables and payables, cash and banks, the automatic journals of
operational documents, financial reports) to the existing WarehouseERP app. Users are accountants and finance managers;
every Accounting page is **company-wide** (a user limited to some branches or warehouses gets 403
`Accounting.Access.CompanyWideRequired` from every Accounting endpoint).

It is part of the existing app: same App Shell, routing, sign-in, cookie session, XSRF handling, RTK Query `baseApi` and
design system. Don't add another app, another auth provider, Supabase, mock data, or any accounting rule the server
enforces: never balance a journal for the user, never compute a balance, a report, a tax or an allocation in the
browser, never post anything by itself. Show what the API returns and the errors it sends. Amounts are in the company's
currency (EGP, 2 decimals): the server refuses more decimals (`Accounting.Journal.LineAmountScale`), so inputs accept at
most two.

## Build on what exists (don't rebuild it)
Follow `AGENTS.md` and its **UI conventions**:
- Create and edit forms open in `FormDialog` (`src/shared/ui/FormDialog.tsx`); no `/new` pages. Detail pages are
  read-only (`Panel` + `DetailGrid` + `ReadOnlyValue`). Edit dialogs load with `refetchOnMountOrArgChange: true` and fill
  from `useLatestRecord(q)` so the form and its `rowVersion` come from the same fetch.
- State-changing actions ask first through `useConfirm()` / `useStatusConfirm()` (`src/shared/ui/confirm.ts`); actions
  needing input (reject, reopen, reverse, allocate) use a `FormDialog`.
- Lists use `ServerTable`; row actions are icon buttons from `src/shared/ui/RowActions.tsx`. Full-width pages, logical
  classes only (`ms`/`me`/`ps`/`pe`/`start`/`end`); Radix `DirectionProvider` handles RTL. Numbers and amounts stay LTR.
- Reuse `Money`, `Qty`, `DateValue` from `src/shared/ui/DocValues.tsx`, `DocHistory` from `src/shared/ui/DocHistory.tsx`,
  `StatusBadge`, `PageHeader`, `BackLink`.
- RTK Query endpoints are injected into `src/shared/api/baseApi.ts`; add the tag types `AccountingSettings`, `Account`,
  `AccountMapping`, `FiscalYear`, `Journal`, `OpenItem`, `CashAccount`, `BankReconciliation`, `Cutover`, `SourcePosting`,
  `AccountingReport`. Posting, approving, reversing or cancelling a journal (vouchers included) also invalidates
  `OpenItem`, `CashAccount`, `AccountingReport` and `SourcePosting`; running or retrying the queue invalidates `Journal`,
  `OpenItem`, `AccountingReport`, `SourcePosting`; activation invalidates `AccountingSettings` and `Cutover`.
- **Idempotency-Key:** `POST /api/accounting/journals/{id}/post` and `/approve` need an `Idempotency-Key` header (a new
  `crypto.randomUUID()` per click, the same key when the user retries after a network error). Without it the API answers
  400 `Validation.Failed`.
- Concurrency: every update and action sends the `rowVersion` the user saw; a 409 `Common.ConcurrencyConflict` means
  "someone changed it: reload". Actions answer the new `rowVersion` (`UpdatedResponse`) or the posted journal.
- Dates are `YYYY-MM-DD` strings (`DateOnly`), timestamps are UTC ISO strings shown with `LocalTime`.
- Permission names go in `P` (`src/shared/session/permissions.ts`); Accounting pages and actions check
  `canCompanyWide(me, P.x)`. Every text goes in `src/shared/i18n/translations.ts` (keys `acc.`), errors under
  `err.<errorCode>` (Arabic and English).
- Code in `src/features/accounting/` (one file per area: `api.ts`, `types.ts`, `SetupPage.tsx`, `AccountsPages.tsx`,
  `FiscalYearsPages.tsx`, `JournalPages.tsx`, `VoucherDialogs.tsx`, `OpenItemsPage.tsx`, `TreasuryPages.tsx`,
  `IntegrationPage.tsx`, `ReportPages.tsx`); pages under `src/routes/_authenticated/accounting/`.
- Sidebar: one collapsible **Accounting / الحسابات** group in `navGroups` (`src/shared/shell/AppShell.tsx`) after POS:
  Setup and cutover (`accounting.reports.read`), Chart of accounts (`accounting.accounts.read`), Posting roles
  (`accounting.accounts.read`), Fiscal years (`accounting.accounts.read`), Journals (`accounting.journals.read`),
  Receivables (`accounting.reports.read`), Payables (`accounting.reports.read`), Cash and banks
  (`accounting.accounts.read`), Bank reconciliation (`accounting.treasury.manage`), Automatic journals
  (`accounting.journals.read`), Reports (`accounting.reports.read`).
- In the Identity role editor, add the permission labels from section 11.

## 0. Compatibility (do this first)
**Sales — customer exposure** (`src/features/sales/CustomerPages.tsx`, exposure panel; `types.ts` `Exposure`): the
response gained `receivableBalance`, `unjournaledAmount`, `totalExposure`, `availableCredit` (all `number | null`; null
while Accounting is not active). Keep the existing rows; when `isPartial` is false, hide the `sal.partialExposure` notice
and add rows: receivable balance (`sal.receivableBalance` "رصيد العميل المدين" / "Receivable balance"), waiting for
their journal (`sal.unjournaled` "فواتير لم تُقيَّد بعد" / "Not journaled yet"), total exposure (`sal.totalExposure`
"إجمالي التعرض الائتماني" / "Total exposure"), available credit (`sal.availableCredit` "الائتمان المتاح" / "Available
credit", red when negative). It stays informational: nothing is blocked by it.

**Links from operational documents (optional, read-only):** a posted goods receipt, supplier invoice or return, sales
delivery, invoice or return, POS sale, return or closed shift, or an Inventory document has one automatic journal whose
`reference` is the document's number. A "Journal" link may open `/accounting/journals?search={number}` for users with
`accounting.journals.read`. No operational page changes otherwise.

## 1. Setup and cutover (`/accounting/setup`)
**Settings panel** — `GET /api/accounting/settings` → `{ fiscalYearStartMonth, cutoverDate, manualJournalsRequireApproval,
vouchersRequireApproval, currencyCode, startMonthLocked, cutoverLocked, rowVersion, activatedAtUtc }`. Edit dialog
(`accounting.settings.manage`): start month 1–12 (disabled when `startMonthLocked`), accounting start date (a first-of-month
date picker, disabled when `cutoverLocked`), two switches → `PUT /api/accounting/settings` with all five fields +
`rowVersion`.

**Cutover readiness** — `GET /api/accounting/cutover` → `{ cutoverDate, cutoverStartUtc, isActivated, activatedAtUtc,
activatedByUserId, ready, checks: [{ code, passed, expected, actual, message }], rowVersion }`. Show a checklist with a
green/red icon per check, its translated title (`acc.check.<code>`), `expected` vs `actual` as money when present and the
server's `message` under a failed check. Codes: `CutoverStarted`, `FiscalPeriodOpen`, `PostingRolesMapped`,
`OpeningBalancesPosted`, `SubledgersReconciled`, `InventoryValuation`, `GoodsReceivedNotInvoiced`, `UnbilledReceivables`,
`PosTillCash`. The comparisons read "what the modules hold at the start" (`expected`) against "the opening balances"
(`actual`).

**Activate** (`accounting.integration.manage`, only when `ready` and not `isActivated`): a strong confirmation that says
it can't be undone, that documents posted from `cutoverStartUtc` will be journaled automatically, that the start date and
the opening balances are fixed from then on → `POST /api/accounting/cutover/activate` `{ rowVersion }` (the readiness
`rowVersion`). After activation show "Active since …" and hide the button. Never activate automatically.

**Opening balances**: a button "Opening balance journal" (`accounting.opening.manage`, hidden once activated) opens the
journal editor of section 4 in opening mode (`POST /api/accounting/journals/opening` `{ description, reference, lines }`;
no date: it is always dated on the start date; it always needs approval). After activation the API answers 422
`Accounting.Journal.OpeningAfterActivation`.

## 2. Chart of accounts (`/accounting/accounts`) and posting roles (`/accounting/mappings`)
- Tree from `GET /api/accounting/accounts/tree` (`AccountSummaryResponse[]`: `id, code, nameAr, nameEn, parentId, level,
  type, section, isPosting, allowsManualPosting, partyType, isActive`): an indented, expandable tree (code + name, type,
  section, badges "header", "system only" when `!allowsManualPosting`, "customers"/"suppliers" for `partyType`,
  inactive greyed). Search uses `GET /api/accounting/accounts?search&type&isActive&isPosting&parentId&page&pageSize`.
- Empty chart: two buttons (`accounting.accounts.manage`): "Default Egyptian chart" → `POST /accounts/default-chart` →
  `{ accountsCreated, rolesMapped }`; "Import" → a dialog that pastes or uploads CSV rows parsed in the browser into
  `{ accounts: [{ code, nameAr, nameEn, parentCode, type, section, isPosting, allowsManualPosting, partyType }], roles:
  [{ role, accountCode }] }` → `POST /accounts/import` (all or nothing; 409 `Accounting.Chart.NotEmpty` once accounts
  exist).
- Account detail `/accounting/accounts/$id` (`GET /accounts/{id}`: plus `parentCode`, `hasChildren`, `hasLines`,
  `mappedRoles`, timestamps) with a link to its account statement (section 9).
- Create (`accounting.accounts.manage`): parent picker, `code` (must extend the parent's code), names, `type` (the
  parent's), `section` (for posting accounts; filtered by type: Asset → CurrentAssets/NonCurrentAssets, Liability →
  CurrentLiabilities/NonCurrentLiabilities, Equity → Equity, Revenue → Revenue/OtherIncome, Expense →
  CostOfSales/OperatingExpenses/OtherExpenses/IncomeTax), `isPosting`, `allowsManualPosting`, `partyType` → `POST
  /accounts`. Edit: names, section, manual flag + `rowVersion` → `PUT /accounts/{id}`. Activate / deactivate
  (`POST /{id}/activate|deactivate`; deactivation needs a zero balance, no active children, no mapping); delete only an
  account nothing uses (`DELETE /{id}`).
- Posting roles page: `GET /api/accounting/account-mappings` → 28 rows `{ role, accountId, accountCode, accountNameAr,
  accountNameEn, allowedTypes, requiredParty, rowVersion }` (unmapped roles have null account). Map (account picker limited
  to active posting accounts of `allowedTypes` and the `requiredParty`) → `PUT /account-mappings/{role}` `{ accountId,
  rowVersion }`; unmap → `DELETE /account-mappings/{role}`. Translate each role (`acc.role.<Role>`).

## 3. Fiscal years (`/accounting/fiscal-years`)
- `GET /api/accounting/fiscal-years` → years `{ id, name, startDate, endDate, status, closedAtUtc, closingJournalId,
  closingJournalNumber, periods: [{ id, number, startDate, endDate, status, closedAtUtc, closedByUserId }], events,
  rowVersion }`. Create (`accounting.periods.manage`): `{ startYear }` → `POST /fiscal-years` (years follow each other).
- Periods table with close (`POST /api/accounting/fiscal-periods/{id}/close`, no body → `{ periodId, openJournals,
  unpostedDocuments }`: after closing, tell the user how many drafts and how many operational documents are dated in it)
  and reopen (`accounting.periods.reopen`, reason required → `POST /fiscal-periods/{id}/reopen` `{ reason }`).
- Year close (`POST /fiscal-years/{id}/close` `{ rowVersion }` → `{ closingJournalNumber, netProfit, … }`, after every
  period is closed) and reopen (`accounting.periods.reopen`, `{ reason, rowVersion }`). `DocHistory` from `events`.

## 4. Journals (`/accounting/journals`, `/accounting/journals/$id`)
- List `GET /api/accounting/journals?page&pageSize&status&type&fromDate&toDate&search` → `{ id, draftReference, number,
  journalType, status, postingDate, description, reference, totalDebit, lineCount, reversalOfJournalId, createdAtUtc,
  postedAtUtc }`; filters for status, type (16 `JournalType` values, translated `acc.jt.<Type>`), dates and search
  (number, description, reference).
- Detail `GET /journals/{id}` → header, `lines` (`lineNumber, accountCode, accountNameAr, branchCode, debit, credit,
  description, partyCode, partyNameAr, dueDate`), totals, `voucher` (for vouchers), `reversalOf…`/`reversedBy…` links,
  `sourceType`/`sourceId` (system journals: show "From {sourceType} {reference}"), people (`createdByName`,
  `approvedByName`, `postedByName`), `DocHistory` from `events`. Draft numbers show `draftReference` (DJ-000123) until
  posted.
- **Editor** (create `POST /journals` `{ postingDate, description, reference, lines }`, edit `PUT /journals/{id}` + `rowVersion`):
  a lines grid with account picker (posting accounts; manual journals only accounts with `allowsManualPosting`), branch
  picker (active branches), debit or credit (one side), line description, and — only when the account has a `partyType` —
  a customer or supplier picker (Sales / Purchasing search endpoints) and an optional due date. Show the running totals and
  the difference, but let the server refuse an unbalanced journal (`Accounting.Journal.Unbalanced`); never add a balancing
  line. Lines of reversals and vouchers are fixed (`linesFixed`).
- Actions by `status` (buttons hidden when the action is impossible):
  - Draft: Edit; Submit (`POST /{id}/submit` `{ rowVersion }`) when `requiresApproval`; Post (`POST /{id}/post`
    `{ rowVersion }` + Idempotency-Key) when not; Cancel (`POST /{id}/cancel`).
  - Submitted: Approve (`accounting.journals.approve`, `POST /{id}/approve` + Idempotency-Key — the writer may approve
    their own journal, there is no "different approver" rule); Reject (reason, `POST /{id}/reject` `{ reason, rowVersion }`);
    Cancel.
  - Posted: Reverse (`accounting.journals.manage`, manual, opening and voucher journals only): a dialog with
    `postingDate` (not before the original), optional description and reference → `POST /{id}/reverse` → 201 with the
    reversal draft's id; open it. System journals (types `YearEndClosing`, `InventoryJournal`, `PurchasingJournal`,
    `SalesJournal`, `PosJournal`) have no action at all.
- Which permission edits a draft depends on its type: manual `accounting.journals.manage`, opening
  `accounting.opening.manage`, vouchers `accounting.receivables.manage` / `accounting.payables.manage` /
  `accounting.treasury.manage` (section 5). The server checks; hide what the user can't do.

## 5. Vouchers (receipts, payments, refunds, cash vouchers, transfers, expense bills)
One `FormDialog` per type → `POST /api/accounting/vouchers` `{ type, postingDate, description, reference, cashAccountId,
counterCashAccountId, partyType, partyId, paymentMethod, amount, withholdingAmount, taxAmount, dueDate, branchId, lines,
allocations }` (unused fields `null`, numbers `0`, arrays `[]`), edit `PUT /vouchers/{id}` (same + `rowVersion`); then it
posts (or is submitted/approved when `vouchersRequireApproval`) through the journal actions of section 4. The server
builds the lines; the dialog never shows editable journal lines.

| Type (`acc.jt.*`) | Permission | Fields |
|---|---|---|
| `CustomerReceipt` (RV) | `accounting.receivables.manage` | cash account, customer, method, `amount` = net cash received, `withholdingAmount` (tax the customer withheld, by hand), reference (cheque/transfer no.), allocations |
| `CustomerRefund` (CF) | `accounting.receivables.manage` | cash account, customer, method, `amount`, allocations |
| `SupplierPayment` (PV) | `accounting.payables.manage` | cash account, supplier, method, `amount` = net cash paid, `withholdingAmount` (tax the company withheld), reference, allocations |
| `SupplierRefund` (SF) | `accounting.payables.manage` | cash account, supplier, method, `amount`, allocations |
| `ExpenseBill` (EB) | `accounting.payables.manage` | supplier, `reference` = the supplier's bill number (required, unique per supplier), `lines` (expense account, branch, amount), `taxAmount` (input VAT), optional `dueDate` (default: the supplier's terms) |
| `CashReceipt` (CR) | `accounting.treasury.manage` | cash account, method, `lines` (counter accounts, credited) |
| `CashPayment` (CP) | `accounting.treasury.manage` | cash account, method, `lines` (counter accounts, debited) |
| `CashTransfer` (TR) | `accounting.treasury.manage` | source `cashAccountId`, destination `counterCashAccountId`, `amount`; no method |

- Methods: a cashbox takes `Cash` only; a bank `BankTransfer`, `Cheque` (reference required) or `Card`. Post-dated cheques
  don't exist in V1: record a cheque when it has cleared.
- **Allocations** (receipts, payments, refunds): after picking the party, list its open items of the other side
  (`GET /api/accounting/open-items?partyType&partyId&openOnly=true`) with `documentNumber`, `dueDate`, `openAmount`,
  `daysOverdue`, and an amount input per item (defaulting to nothing; a "settle oldest first" helper may fill amounts in
  the dialog, but the user confirms them). Sent as `allocations: [{ openItemId, amount }]`; the server checks them again
  when the voucher posts.
- `voucher.amount` in the detail response is the party's settled total (net cash + tax withheld for receipts and
  payments; the bill's total for an expense bill), not the request's `amount`.
- The cash guard: a cashbox, or a bank without overdraft, never goes below zero on the posting date or any later day
  (`Accounting.Journal.CashAccountOverdrawn` names the account).

## 6. Receivables and payables (`/accounting/receivables`, `/accounting/payables`)
- Tabs: Open items (`GET /api/accounting/open-items?partyType=Customer|Supplier&partyId&accountId&openOnly&page&pageSize` →
  `{ id, partyCode, partyNameAr, documentNumber, journalType, reference, documentDate, dueDate, amount, openAmount,
  daysOverdue, allocations: [{ allocationId, otherItemId, amount, allocatedOn, journalId }] }`; amounts signed: customer
  invoices positive, credits negative; supplier bills negative, claims positive), Aging (section 9), Party statement.
- Buttons: new receipt/refund (receivables) or payment/refund/expense bill (payables) → section 5 dialogs.
- Allocate by hand (`accounting.receivables.manage` / `.payables.manage`): pick a debit and a credit item of the same
  party → amount → `POST /api/accounting/allocations` `{ debitItemId, creditItemId, amount }`; auto: `POST
  /allocations/auto` `{ partyType, partyId }` → `{ allocationsMade, amountAllocated }` (oldest first); remove an allocation
  (confirm) → `POST /allocations/{id}/remove`.
- Customer return credits and supplier return claims arrive here by themselves as open items (from Sales and Purchasing
  returns, once accounting is active) and are allocated like any other item.

## 7. Cash and banks (`/accounting/cash-accounts`) and bank reconciliation (`/accounting/bank-reconciliations`)
- `GET /api/accounting/cash-accounts?kind&branchId&isActive&asOfDate` → `{ id, code, name, kind, branchCode, accountCode,
  accountNameAr, bankName, bankAccountNumber, allowsOverdraft, isActive, balance, receivesPosCash, rowVersion }`. Create
  (`accounting.treasury.manage`): code, name, `kind` (Cashbox | Bank), branch, its own ledger account (an asset posting
  account, not a customer/supplier one, used by no other cash account), bank name/number and overdraft for banks only,
  `receivesPosCash` for cashboxes only ("receives the cash of closed POS shifts"; one per branch: 409
  `Accounting.CashAccount.PosCashboxTaken`) → `POST /cash-accounts`; edit `PUT /cash-accounts/{id}` `{ name, bankName,
  bankAccountNumber, allowsOverdraft, rowVersion, receivesPosCash }`; activate/deactivate (zero balance).
- Each row links to its ledger account's statement (the cash or bank book) and offers cash receipt/payment/transfer.
- Bank reconciliation (`accounting.treasury.manage`): list `GET /bank-reconciliations?cashAccountId`; start `POST
  /bank-reconciliations` `{ cashAccountId, statementDate, statementBalance }`; detail `GET /{id}` → `{ statementBalance,
  openingClearedBalance, clearedAmount, difference, ledgerBalance, lines: [{ journalLineId, journalNumber, postingDate,
  description, reference, amount, isTicked }], status, rowVersion }`; ticking sends the whole ticked set `PUT /{id}/lines`
  `{ journalLineIds, rowVersion }`; complete (only when `difference` is 0) `POST /{id}/complete`; cancel `POST /{id}/cancel`.

## 8. Automatic journals (`/accounting/integration`)
- Status card from `GET /api/accounting/source-postings/status` → `{ isActivated, activatedAtUtc, cutoverDate,
  workerEnabled, workerIntervalSeconds, pending, failed, posted, nothingToPost, oldestUnsettledAtUtc, cursors: [{ module,
  scannedToUtc }] }` ("Journals are written every N seconds" when the worker is on).
- Queue table `GET /source-postings?status&sourceType&fromDate&toDate&varianceOnly&page&pageSize` → `{ id, sourceType,
  sourceNumber, occurredAtUtc, postingDate, status, journalId, journalNumber, attempts, nextAttemptAtUtc, errorCode,
  errorMessage, stockValue, postedStockValue, varianceAmount }`. Default filter: `Failed`. Show the translated
  `err.<errorCode>` and the server's `errorMessage`, the next automatic try, a link to the journal when posted.
- Actions (`accounting.integration.manage`): "Run now" `POST /source-postings/run` `{ fullScan: false }` → `{ discovered,
  posted, nothingToPost, failed, moreDue }` (a "full scan from the start" checkbox sends `fullScan: true`); per row "Retry"
  `POST /source-postings/{id}/retry` (after the cause is fixed: a role mapped, a period reopened, a POS cashbox marked).
- "Price differences" tab: `varianceOnly=true` — supplier price or cost differences kept under review (the accountant
  reclassifies them by manual journal from the "purchase price differences under review" account).
- **Reconciliation** panel `GET /source-postings/reconciliation` (`accounting.reports.read`) → `inventory` (ledger
  balance, valuation, difference, opening and rounding differences, unposted value, unexplained), `clearingAccounts`
  (`GoodsReceivedNotInvoiced`, `UnbilledReceivables`, `PosTillClearing`: ledger vs module, difference), counts by type and
  status, `missingCount` + `missing` documents, `unmatchedJournals`, `openCardIncidents`, `cashDifferences` (POS shifts
  short or over), `isReconciled` (a big green/amber badge). Everything is read-only.

## 9. Reports (`/accounting/reports/...`, `accounting.reports.read`)
All read-only, with a date range or "as of" picker, branch filter where the API has one, print and CSV export of what is
shown (no recalculation):
- Trial balance `GET /reports/trial-balance?fromDate&toDate&branchId&level&includeZero` → rows (code, name, level,
  opening/period/closing debit and credit) and totals + `isBalanced`.
- General ledger `GET /reports/general-ledger?fromDate&toDate&accountId&branchId&journalType&page&pageSize` (paged).
- Account statement `GET /reports/account-statement?accountId&fromDate&toDate&branchId` → opening, lines with running
  `balance`, closing (a header account includes its posting accounts).
- Balance sheet `GET /reports/balance-sheet?asOfDate` → `lines` grouped by `section` + totals, `unclosedEarnings`,
  `isBalanced`.
- Income statement `GET /reports/income-statement?fromDate&toDate&branchId&compareFromDate&compareToDate` → `lines` and
  `totals` (`revenue, costOfSales, grossProfit, operatingExpenses, operatingProfit, otherIncome, otherExpenses,
  profitBeforeTax, incomeTax, netProfit`), optional comparison column.
- Aging `GET /reports/aging?partyType&asOfDate&branchId` → per party `notDue, days1To30, days31To60, days61To90,
  over90Days, unallocated, total` and totals.
- Party statement `GET /reports/party-statement?partyType&partyId&fromDate&toDate`.
- Subledger reconciliation `GET /reports/subledger-reconciliation?asOfDate` → per control account: ledger balance, open
  items, lines without party, difference, `isReconciled`.

## 10. Rules the screens must respect
- Nothing posts into a closed period (`Accounting.Journal.PeriodClosed`) or before the start date
  (`Accounting.Journal.BeforeCutover`); a posted journal never changes: corrections are reversals or new journals.
- Self-approval is allowed everywhere (CLAUDE.md, *Document approvals*): never hide Approve because the user wrote it.
- Control accounts (customers, suppliers) need a party on every line (`Accounting.Journal.PartyRequired`); other accounts
  refuse one. System-only accounts refuse manual journals (`Accounting.Journal.AccountNotManual`).
- A journal whose items are allocated can be reversed only after its allocations are removed
  (`Accounting.Journal.ItemsAllocated`).
- Automatic journals are never edited or reversed by hand; their documents are corrected in their own module.

## 11. Permissions (labels for the role editor; `P` keys)
| Permission | `P` key | Arabic | English |
|---|---|---|---|
| `accounting.accounts.read` | `accAccountsRead` | عرض دليل الحسابات والسنوات المالية | View the chart of accounts and fiscal years |
| `accounting.accounts.manage` | `accAccountsManage` | إدارة دليل الحسابات وربط الأدوار | Manage accounts and posting roles |
| `accounting.periods.manage` | `accPeriodsManage` | إدارة السنوات والفترات وإقفالها | Manage and close fiscal years and periods |
| `accounting.periods.reopen` | `accPeriodsReopen` | إعادة فتح الفترات والسنوات | Reopen periods and years |
| `accounting.journals.read` | `accJournalsRead` | عرض القيود | View journals |
| `accounting.journals.manage` | `accJournalsManage` | إنشاء القيود اليدوية وعكسها | Write and reverse manual journals |
| `accounting.journals.approve` | `accJournalsApprove` | اعتماد القيود | Approve journals |
| `accounting.opening.manage` | `accOpeningManage` | قيد الأرصدة الافتتاحية | Opening balances |
| `accounting.reports.read` | `accReportsRead` | التقارير المالية | Financial reports |
| `accounting.settings.manage` | `accSettingsManage` | إعدادات المحاسبة | Accounting settings |
| `accounting.receivables.manage` | `accReceivablesManage` | المقبوضات وتسويات العملاء | Customer receipts and allocations |
| `accounting.payables.manage` | `accPayablesManage` | المدفوعات وفواتير المصروفات وتسويات الموردين | Supplier payments, expense bills and allocations |
| `accounting.treasury.manage` | `accTreasuryManage` | الخزائن والبنوك والتسوية البنكية | Cash, banks and bank reconciliation |
| `accounting.integration.manage` | `accIntegrationManage` | تفعيل المحاسبة الآلية وتشغيلها | Activate and run automatic accounting |

## 12. Error codes (translate each under `err.<code>`, Arabic and English)
Translate every `Accounting.*` code listed in `FRONTEND_HANDOFF.md` (*Error codes*); show `detail` from the ProblemDetails
when a code is unknown. The ones users meet most: `Accounting.Access.CompanyWideRequired`, `Accounting.Journal.Unbalanced`,
`Accounting.Journal.PeriodClosed`, `Accounting.Journal.BeforeCutover`, `Accounting.Journal.PartyRequired`,
`Accounting.Journal.AccountNotManual`, `Accounting.Journal.ApprovalRequired`, `Accounting.Journal.CashAccountOverdrawn`,
`Accounting.Journal.ItemsAllocated`, `Accounting.Journal.NotReversible`, `Accounting.Journal.OpeningAfterActivation`,
`Accounting.Voucher.BillNumberTaken`, `Accounting.Voucher.AllocationExceedsOpen`, `Accounting.Mapping.RoleNotMapped`,
`Accounting.Cutover.NotReady`, `Accounting.Settings.AlreadyActivated`, `Accounting.Settings.CutoverLocked`,
`Accounting.Source.PosCashboxMissing`, `Accounting.Source.NotActive`, `Accounting.Idempotency.KeyReused`,
`Common.ConcurrencyConflict`.

## 13. Tests (Vitest)
- `accounting-rules.test.ts`: which journal actions a status, type and permission offer (system journals none; Approve
  shown to the writer; Reverse only for posted manual, opening and voucher journals); voucher dialog fields per type
  (section 5 table); a cashbox offers only `Cash`.
- `accounting-api.test.ts`: post and approve send an `Idempotency-Key`, retries reuse it; every action sends `rowVersion`;
  activation sends the readiness `rowVersion`.
- `accounting-readiness.test.tsx`: the checklist renders every check code, shows expected vs actual and hides Activate
  unless `ready`.
- `exposure.test.tsx`: the Sales exposure panel shows the receivable rows only when `isPartial` is false.
