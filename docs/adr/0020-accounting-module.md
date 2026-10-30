# ADR 0020 — Accounting V1: general ledger, subledgers, treasury and ERP integration

Status: accepted (2026-10-30), on `feature/accounting`. The product owner approved the scope and decisions in
[`docs/modules/Accounting/V1_DECISIONS.md`](../modules/Accounting/V1_DECISIONS.md), including the global approval rule
in `CLAUDE.md` (*Document approvals*). This ADR records how each milestone implements them; **Milestones A (general
ledger), B (receivables, payables, cash and banks) and C (ERP integration and cutover) are implemented, and D
(development acceptance and frontend handoff) is done** (section *Milestone D*). The accountant's and the statutory
sign-off are separate and still open (*Production prerequisites*).

## Context
Every operational module keeps immutable posted documents with their amounts (Inventory's cost entries, supplier and
sales invoices, returns, POS sales and shifts), but there was no ledger, no fiscal calendar, no balances and no financial
statements, and no module exposed its documents for accounting. Accounting had to own the ledger without duplicating
those documents, keep posted entries immutable and balanced, refuse closed periods, survive retries and concurrent
closes, and leave the cutover from the existing operational history explicit.

## Decision — Milestone A
1. **Module and schema.** `src/Modules/Accounting` with the usual four projects, schema `accounting`, created by
   `Migration14_Accounting` (Migrations 01–13 unchanged). Other modules only through their Contracts: Organization
   (`ICompanyDirectory` for the base currency, `IBranchDirectory` for line branches) and Identity (`IUserDirectory` for
   names). The branch foreign key of journal lines lives in `ErpDbContext`. Architecture tests know the module.
2. **Chart of accounts** (`Accounts`, temporal). Numeric codes, unique, a child's code extending its parent's and longer,
   same type as the parent, at most 8 levels; header accounts group and never take lines; posting accounts need a
   statement section fitting their type (checked by the domain and by check constraints); `AllowsManualPosting` is false
   for accounts only the system feeds; `PartyType` marks customer (asset) and supplier (liability) control accounts.
   Code, parent, type, kind and party are fixed; names, section and the manual flag change with a row version.
   Deactivation waits for inactive children, no mapping and a zero posted balance; deletion only for an account nothing
   uses (no children, no line, no mapping).
3. **Default chart and import.** `POST /accounts/default-chart` builds the default Egyptian trading chart
   (`Features/Accounts/DefaultChart`, ~90 accounts) and maps all 28 posting roles; `POST /accounts/import` takes the
   company's chart as rows with parent codes and optional role mappings. Both run the same `ChartBuilder` (parents first,
   every row through `Account.Create`), all or nothing, and only into an empty chart (409 `Accounting.Chart.NotEmpty`).
4. **Posting roles** (`AccountMappings`, temporal, one row per role). `PostingRoleRules` lists the account types each
   role accepts and the party it requires; a mapping needs an active posting account that fits. Nothing in code names an
   account: the year-end close reads `RetainedEarnings` and refuses (`Accounting.Mapping.RoleNotMapped`) when unmapped.
5. **Settings** (`AccountingSettings`, temporal, one row seeded): fiscal-year start month (1, fixed once a year
   exists), cutover date (2027-01-01, first day of a month, fixed once a journal has posted), manual-journal approval
   (required). Company-wide `accounting.settings.manage` changes them.
6. **Fiscal years** (`FiscalYears` → `FiscalPeriods`, `FiscalYearEvents`). Twelve monthly periods from the start month;
   the first year anywhere, every later one the day after the last (`NotContiguous`), unique start dates for years and
   periods. A period closes (`accounting.periods.manage`) and reopens (`accounting.periods.reopen`, reason) while its year
   is open; each step is an event.
7. **Locks.** `IAccountingLocks` takes transaction-owned `sp_getapplock`s: request key → fiscal year → period. Every
   posting takes its period's lock **shared** and then reads the period's state again; closing or reopening takes the
   year's lock and the period's lock **exclusively**. A posting is therefore either committed before a close or refused
   after it (`Accounting.Journal.PeriodClosed`); nothing is moved to another period.
8. **Journals** (`JournalEntries` → `JournalLines`, `JournalEvents`). Types `Manual` (GJ), `OpeningBalance` (OB),
   `Reversal` (RJ), `YearEndClosing` (CJ). Every journal balances by its own lines (domain and
   `CK_JournalEntries_Balanced`), each line has an amount on exactly one side (`CK_JournalLines_OneSide`) in the
   currency's minor unit (more decimals are refused, never rounded), a posting account and a branch. Manual journals use
   only accounts open to manual postings; opening balances may use system accounts; a control-account line names its
   party (Milestone B, decision 14). Drafts copy the approval policy (opening balances always need
   approval); `submit` → `approve` posts, `post` posts a draft that needs none, `reject` (reason) and `cancel` before
   posting. The writer may approve their own journal: permission, company-wide access and state decide.
9. **Posting** is one `ITransactionRunner` transaction (`Features/Journals/JournalPosting`): the `Idempotency-Key` lock and
   record (SHA-256 of operation, journal, row version, user; a retry answers the first outcome, another request with the
   key is 409 `KeyReused`), the journal at the version the client read, the date rules (on or after the cutover;
   opening balances exactly on it), the line rules again, then `Shared/LedgerPosting`: the period of the date, its
   shared lock, its state re-read, and the next number from `DocumentNumberSequences` (locked row per type and fiscal
   year; a rollback gives it back). `CK_JournalEntries_Posted` ties the posted state to its number, year, period, sequence
   and time; a posted journal never changes.
10. **Reversal.** `POST /journals/{id}/reverse` drafts a `Reversal` of a posted manual or opening journal: the lines
    turned around, fixed (`LinesFixed`), dated on or after the original, posting like a manual journal. One reversal that
    isn't cancelled per journal (`UX_JournalEntries_Reversal`, filtered); its posting adds a `Reversed` event to the
    original, which stays as it was. Reversals and closing journals are never reversed by hand.
11. **Year-end close** (`POST /fiscal-years/{id}/close`, row version): under the year's lock, every period closed and every
    earlier year closed; one generated `YearEndClosing` journal on the last day zeroes each revenue and expense account
    **per branch** against the `RetainedEarnings` account (also per branch, so branch figures stay whole), posted into
    the closed last period and numbered CJ; nothing with a balance → no journal. Reopening the year (reason, latest closed
    year first) posts a generated reversal of that journal on the same date and leaves the periods closed.
12. **Reports** read posted lines only (`ILedgerReadStore`; no stored balances): trial balance (opening before the range,
    period movement, closing; headers sum their posting accounts; totals over posting accounts; optional level, branch
    and zero rows), account statement (opening, running balance, header accounts with their posting accounts, refused
    above 5000 lines), paged general ledger, balance sheet as of a date (company level; unclosed revenue and expenses shown
    in equity so it balances before and after a close), income statement over a range (closing journals excluded,
    optional branch and comparison period). Company-wide `accounting.reports.read`.
13. **Access.** Ten permissions in `AccountingPermissions.All`; every handler also requires company-wide access
    (`Accounting.Access.CompanyWideRequired`). Editing, submitting, posting and cancelling a journal need the manage
    permission of its type (`journals.manage` or `opening.manage`), checked in the handler because the type is in the
    journal.

## Decision — Milestone B
14. **Party lines.** A line on a control account names a party of the account's kind (customer on an asset control
    account, supplier on a liability one) that exists in Sales' `ICustomerDirectory` or Purchasing's new
    `ISupplierDirectory` (read-only, `FindManyAsync`/`SearchActiveAsync`); any other line names none
    (`Accounting.Journal.PartyRequired`, `PartyNotAllowed`, `PartyNotFound`). A party line may carry a due date (its posting
    date otherwise). Parties are kept by type and id with no foreign key, because a line names one of two tables; the
    posting checks them. Manual journals, opening balances (customer and supplier opening balances with their original due
    dates) and reversals all accept party lines.
15. **Open items.** Posting opens one `OpenItem` per party line (`UX_OpenItems_Line`): party, account, branch, document
    and due dates, amount signed debit minus credit (receivables positive, payables negative), open amount
    (`CK_OpenItems_Open`). An `Allocation` settles a debit item against a credit item of the same party and account,
    within what both have open, dated the later of the two items; it is removed (never deleted) to undo it. By hand
    (`POST /allocations`), automatically oldest-first per party and account (`/allocations/auto`), planned by a voucher
    (below) or made by a reversal. Allocation needs `receivables.manage` or `payables.manage` for the party type and takes
    the party's lock (`accounting/party/{type}/{id}`), so concurrent allocations and postings of one party queue.
16. **Vouchers** are journals of types 5–12 (`CustomerReceipt` RV, `SupplierPayment` PV, `CustomerRefund` CF,
    `SupplierRefund` SF, `CashReceipt` CR, `CashPayment` CP, `CashTransfer` TR, `ExpenseBill` EB) whose lines are built
    from a header (`POST/PUT /api/accounting/vouchers`, `VoucherLines.Build`) and fixed (`LinesFixed`; the journal PUT
    answers `VoucherManaged`). Each type has one shape, so it balances by construction:
    - receipt: Dr cash (net), Dr withholding tax receivable (amount withheld, entered by hand), Cr customer (gross);
    - payment: Dr supplier (gross), Cr cash (net), Cr withholding tax payable;
    - refunds: the party against cash; cash receipts and payments: cash against counter lines (manual, non-control,
      non-treasury posting accounts); transfer: destination against source, each in its own branch;
    - expense bill: Dr expense lines, Dr input VAT, Cr supplier (total) due on the bill's due date or, by default, the
      supplier's payment terms; the supplier's bill number is required and unique per supplier among bills that aren't
      cancelled (`UX_JournalEntries_BillNumber`).
    The withholding and VAT accounts come from the `WithholdingTaxReceivable`/`WithholdingTaxPayable`/`InputVat` roles,
    the control accounts from `AccountsReceivable`/`AccountsPayable`; gross, withheld and net stay on the header
    (`VoucherAmount`, `WithholdingAmount`, `TaxAmount`). A cashbox moves only cash, a bank only transfers, cards and
    cheques; a cheque needs its number and is recorded only once cleared (no post-dated cheque portfolio in V1). A
    receipt, payment or refund may plan allocations (`VoucherAllocations`), checked when drafted and settled from the
    voucher's own item when it posts. Vouchers post, are approved (`VouchersRequireApproval`, default off), cancelled and
    reversed like any journal; each type needs its manage permission (`JournalAccess.PermissionFor`).
17. **Cashboxes and banks** (`CashAccounts`, temporal): code, name, kind, branch, one ledger posting asset account each
    (`UX_CashAccounts_Account`), bank details and overdraft only for banks. The balance is computed from posted lines.
    Deactivation needs a zero balance. **Cash guard:** posting locks every cash account it touches (after the parties) and
    refuses (`Accounting.Journal.CashAccountOverdrawn`) when a cashbox, or a bank without overdraft, would end its
    posting date or any later day below zero — for vouchers and hand journals alike, so a back-dated payment cannot
    overdraw a past day.
18. **Reversal of settled journals.** A journal whose items are allocated is reversed only once those allocations are
    removed (`ItemsAllocated`, checked when the reversal is drafted and again, under the party locks, when it posts);
    the reversal's items then settle the original's items line by line, so neither stays open.
19. **Bank reconciliation** (`BankReconciliations` → `BankReconciliationLines`, `accounting.treasury.manage`): a draft per
    bank at a time (`UX_BankReconciliations_Draft`), dated no earlier than the last completed statement, lists the bank account's
    posted lines up to the statement date that no other reconciliation holds; ticking replaces the ticked set; it
    completes only when statement balance = earlier cleared balance + ticked lines. A line is cleared by one
    reconciliation only (`UX_BankReconciliationLines_Line`; cancelling a draft frees its lines).
20. **Reports.** Aging per party type as of a date (by due date: not due, 1–30, 31–60, 61–90, over 90; the other side's
    items as unallocated; allocations dated after the date ignored; optional branch), party statement (opening, lines,
    running balance) and the subledger reconciliation (per control account: ledger balance, open items, what is still
    open, party-less lines; reconciled when they agree). Open items are listed with their allocations
    (`GET /open-items`).

## Decision — Milestone C
21. **Read-only source contracts.** Each operational module reports its posted documents and the amounts it owns through
    one new contract, implemented over its own tables without locks and changing none of its behaviour:
    Inventory `IInventoryAccountingSource` (its own documents — opening stock, receipts and issues without a business
    document, adjustments, their reversals, count adjustments —, any stock document's exact value from its cost entries,
    the valuation before a moment), Purchasing `IPurchasingAccountingSource` (goods receipts, posted supplier invoices and
    returns, goods received not invoiced before a moment), Sales `ISalesAccountingSource` (posted deliveries and invoices,
    approved returns, goods delivered not invoiced before a moment) and POS `IPosAccountingSource` (sales, returns, closed
    shifts' cash, the tills' cash before a moment, open card incidents, cash differences). Transfers move no value and are
    not reported. Accounting never writes to them, so no operational workflow (checkout included) waits on Accounting.
22. **Accounting identity and queue.** `SourcePostings` holds one row per operational document (`UX_SourcePostings_Source`
    on type and id): pending, posted (with its journal), failed (error code and message, attempts, next try after 1, 2,
    4 … minutes, at most an hour) or nothing to post. Its journal is one of four new system types — `InventoryJournal` IV,
    `PurchasingJournal` PU, `SalesJournal` SL, `PosJournal` PS — naming the document (`SourceType`, `SourceId`,
    `UX_JournalEntries_Source`, `CK_JournalEntries_Source`), written and posted by the system with fixed lines and no
    user; it is never edited or reversed by hand (`NotReversible`): a document is corrected at its source or by another
    journal. The documents themselves and their identities are the queue, so nothing is lost on a restart (no outbox is
    needed: ADR 0015's rule — none until a consumer exists — still holds for other modules).
23. **The run.** `SourceSync` runs every `Accounting:Integration:Interval` (one minute by default; `SourcePostingWorker`,
    `WorkerEnabled`) and on demand (`POST /source-postings/run`, `fullScan` to look again from the cutover). Discovery is
    one transaction under the scan lock: each module lists what it posted since its cursor less `ScanOverlap` (ten
    minutes, for documents that commit a moment after their timestamp; `SourceCursors`), and documents posted from the
    cutover's start without an identity get one. Then each due document posts in its own transaction (`SourcePoster`): its
    identity's lock, the record read again, its rule, every role resolved to its mapped account (an unmapped role stops it),
    the usual line checks (a branch closed since may still receive it), the party and cash locks with the cash guard
    (`Shared/PostingGuard`, now also used by `JournalPosting`), the period of its date locked shared and found open, its
    number; the journal, its parties' open items and the posted record commit together. A failure rolls that back and is
    recorded in a second transaction under the same lock; an unexpected exception is recorded as
    `Accounting.Source.Unexpected` and logged, so one document never blocks the others; a document another run holds
    (`Busy`) is left to that run. `POST /source-postings/{id}/retry` tries one now (after the cause is fixed). Closing a
    period reports the documents dated in it still waiting (`unpostedDocuments`), as it reports unposted drafts; a journal's
    detail names its source document (`sourceType`, `sourceId`).
24. **Dates.** A document's accounting date is the company's local date of the moment it posted in its module (delivery,
    invoice, receipt and return posting, a return's approval, a sale, a shift's close; never before the start date); due
    dates come from the documents. Documents stamped before the cutover's start (local midnight of the accounting start
    date, kept in the settings at activation as `CutoverStartUtc`, so a later time-zone change moves nothing) never get an
    identity: the opening balances hold them.
25. **Posting rules** (`Features/SourcePostings/SourceEntries`, pure functions of what the modules report):
    - Inventory's own documents: inventory against opening-balance equity (opening stock), unmatched receipts (receipts
      without a supplier), internal use (issues), gains (found), losses (damage, loss, expiry), adjustments (correction,
      other) or count differences; a reversal uses the reversed document's account the other way.
    - Goods receipt: inventory against goods received not invoiced. Supplier invoice: GRNI cleared at the received value,
      input VAT, the supplier's payable (its due date); the difference from the invoice's net is kept on
      `PurchaseVarianceUnderReview` and recorded as the posting's `VarianceAmount`. Inventory does not revalue posted stock,
      so splitting a difference between stock held and stock sold is the accountant's review: the default chart opens that
      account to manual journals for the reclassification (V1 decision 8). Supplier return: stock out at Inventory's value,
      GRNI cleared for goods returned before invoicing, a claim on the supplier (a debit open item with its VAT) for
      invoiced goods, the rest under review.
    - Sales delivery: revenue against unbilled receivables when goods are delivered, and their cost. Sales invoice: the
      receivable (due date) and output VAT, unbilled receivables cleared without recognizing revenue again (only a rounding
      difference between the invoice's net and the delivered value reaches revenue). Customer return: a sales return with
      its VAT credited to the customer (a credit open item) for invoiced goods, unbilled receivables cleared for goods
      returned before invoicing, and the goods back at their cost.
    - POS sale and return: retail revenue or returns and VAT against the till's cash clearing and card clearing, and the
      goods' cost. A closed shift hands its takings (its sales' cash less its refunds' cash) in to the branch's POS
      cashbox (`CashAccount.ReceivesPosCash`, one active cashbox per branch, `UX_CashAccounts_PosCash`), with the counted
      difference on cash over and short; without such a cashbox the shift fails visibly (`PosCashboxMissing`) until one is
      marked. Card clearing waits for the acquirer: the treasury records the settlement with a cash receipt (clearing as
      counter line) and the fees by journal.
    - VAT follows the invoice (the only document with tax amounts) **[Accountant: tax point]**.
26. **Clearing values and rounding.** Goods received not invoiced and unbilled receivables are valued per order line: its
    uninvoiced quantity (received or delivered, less invoiced, less returned before invoicing) at the order's net unit
    price (Purchasing) or priced like an invoice line (Sales, `LineAmounts`), rounded once for the whole quantity
    (`UninvoicedReceipts`, `UnbilledDeliveries`). A document's amount is the change it made, from the quantity every earlier
    document of those lines left (posting order: timestamp, then id) to what it left, so a line's clearing value returns
    exactly to zero whatever the splits. Stock values are Inventory's cost entries rounded once per document (per direction
    for an Inventory document moving both ways) to the currency's minor unit; a goods receipt puts its GRNI value on both
    sides. Each posting keeps the exact stock value and the posted one, so the difference with Inventory is explained to the
    last decimal. A module document and its stock document are stamped a moment apart in one transaction; where they fall on
    two sides of the cutover's start, the valuation at the cutover counts that stock on its document's side
    (`Shared/CutoverBoundary`, from Inventory's `GetPostingsAsync`).
27. **Cutover activation** (`GET /cutover`, `POST /cutover/activate`, `accounting.integration.manage`, company-wide): the
    cutover's start has passed by the scan overlap (`CutoverStarted`: the openings are compared with what the modules hold
    at that moment, so activation can't come earlier); the cutover's period exists and is open; every role the rules use is
    mapped; no opening-balance journal is a draft or
    submitted; customers' and suppliers' items reconcile with their control accounts at the cutover; and the opening
    balances of inventory, GRNI, unbilled receivables and the tills' cash clearing equal what the modules hold at the
    cutover's start (Inventory's valuation rounded to the minor unit; open shifts' takings). Activation from the settings'
    version the user read, only when every check passes, is recorded (`ActivatedAtUtc`, `ActivatedByUserId`,
    `CutoverStartUtc`), never undone, and fixes the accounting start date and the openings: no opening-balance journal is
    drafted, posted or reversed after it (`Accounting.Journal.OpeningAfterActivation`; a correction is a manual journal).
    The openings the checks read include posted reversals of opening journals made before activation. Opening balances and
    automatic journals therefore never count a document twice.
28. **Reconciliation** (`GET /source-postings/reconciliation`, `accounting.reports.read`): the inventory account against
    Inventory's valuation, the difference split into the opening's rounding, the journals' rounding and the value not
    journaled yet; anything else (an opening that differs from the valuation by more than its rounding, a journal that
    doesn't match its document) is `Unexplained`; GRNI, unbilled receivables and the tills' cash against their modules;
    counts by type and status; documents posted since the cutover without an identity (a full scan); journals and
    identities that don't point at each other; open card incidents and shifts' cash differences. The queue, its status
    and cursors are `GET /source-postings` (filters, `varianceOnly`) and `/source-postings/status` (`accounting.journals.read`).
29. **Return credits, claims and credit exposure.** A customer return's credit and a supplier return's claim are open items
    of their party, allocated like any other (by hand, automatically, by vouchers). Once accounting is active, Sales'
    customer exposure adds `ReceivableBalance` (Accounting's `ICustomerReceivables`: posted control-account lines of the
    customer, opening balances included), `UnjournaledAmount` (Sales' invoices less credits posted since the cutover whose
    journal hasn't posted yet: Accounting names the ones already journaled), `TotalExposure` (the three, plus confirmed
    orders not invoiced: an invoiced order is counted once, in the receivable or waiting for its journal) and
    `AvailableCredit`; it stays informational (`IsPartial` false).

## Consequences
- `Migration14_Accounting` creates the `accounting` schema: 11 tables (`Accounts`, `AccountMappings` and
  `AccountingSettings` temporal with `history.*History`), check constraints and indexes, the settings seed, and the
  foreign key from `JournalLines.BranchId` to `organization.Branches`. Its `Down` drops exactly that; no existing table or
  data changes. Verified on a fresh LocalDB database: 01–14 applied, 14 rolled back and re-applied,
  `has-pending-model-changes` none.
- OpenAPI grows by 30 paths under `/api/accounting` (tags `AccountingSettings`, `Accounts`, `AccountMappings`,
  `FiscalYears`, `Journals`, `FinancialReports`) and 53 schemas; nothing existing changed.
- Tests: unit tests of the chart, role, journal, reversal, fiscal-year and settings rules; SQL Server integration tests
  through the real HTTP pipeline (approval with self-approval, refusals of unbalanced and misdirected lines, closed
  periods and reopening, a close racing a posting, idempotent retries, reversals, reports agreeing and branches adding up,
  opening balances, access, settings locks, chart rules, mappings and their history, database guards) and a year-end
  test on its own database (default chart, import refusals, contiguous years, close per branch into retained earnings,
  reports after the close, reopen and close again).
- `Migration15_AccountingSubledgers` adds `CashAccounts` (temporal), `OpenItems`, `Allocations`, `VoucherAllocations`,
  `BankReconciliations` and `BankReconciliationLines`; nullable voucher columns on `JournalEntries` and party columns on
  `JournalLines` with their check constraints (`CK_JournalEntries_Type` widened to 1–12, `CK_JournalEntries_Party`,
  `CK_JournalEntries_Voucher`, `CK_JournalLines_Party`); `AccountingSettings.VouchersRequireApproval` (default false);
  branch foreign keys of open items and cash accounts in `ErpDbContext`. Existing rows are untouched (every new column is
  nullable or defaulted). Verified on a fresh LocalDB database: 01–15 applied, 15 rolled back to 14 and re-applied,
  `has-pending-model-changes` none.
- Sales and Purchasing gained one read-only contract method each (`ICustomerDirectory.FindManyAsync`, new
  `ISupplierDirectory`); nothing else in them changed.
- OpenAPI grows by 18 paths (tags `Vouchers`, `OpenItems`, `CashAccounts`, `BankReconciliations`, three reports under
  `FinancialReports`) and 29 schemas; `JournalType` gains eight members and journal, line and settings models gain
  properties. Nothing was removed or renamed.
- Three permissions (`accounting.receivables.manage`, `accounting.payables.manage`, `accounting.treasury.manage`); 13 in
  all.
- Tests: unit tests of each voucher shape and its refusals, planned allocations, number prefixes and the reconciliation
  arithmetic; SQL Server integration tests of receipts with tax withheld settling invoices, manual and automatic
  allocation and removal, expense bills with VAT and supplier terms, duplicate bill numbers, payments with tax withheld,
  the cash guard on the date and every later day, transfers between branches, cash account rules, reversal of a settled
  receipt, party opening balances, aging, party statements, the subledger reconciliation, bank reconciliation end to end
  and the permission of each voucher type.

- `Migration16_AccountingIntegration` adds `SourcePostings` and `SourceCursors`; nullable `SourceType`/`SourceId` on
  `JournalEntries` with `CK_JournalEntries_Source` and `UX_JournalEntries_Source` (`CK_JournalEntries_Type` widened to
  1–16); `CashAccounts.ReceivesPosCash` (default false) with `CK_CashAccounts_PosCash` and `UX_CashAccounts_PosCash`;
  `AccountingSettings.ActivatedAtUtc`/`ActivatedByUserId`/`CutoverStartUtc` (null). Existing rows are untouched. Verified
  on a fresh LocalDB database: 01–16 applied, 16 rolled back to 15 and re-applied, `has-pending-model-changes` none.
- Inventory, Purchasing, Sales and POS each gained one read-only contract (21); Sales' exposure reads
  `ICustomerReceivables` (Sales.Application references Accounting.Contracts); no operational behaviour changed.
- An independent review of the milestone's diff (four reviewers, each finding checked by a second one) found, before
  commit, activation possible before the cutover's start, the start recomputed from the current time zone, openings
  still changeable after activation (and reversed openings ignored by the checks), a failure recorded over a run holding
  the document, exposure missing invoices not journaled yet, and stock stamped across the cutover's start; all are fixed
  as described above.
- OpenAPI grows by 7 paths (tags `Cutover`, `SourcePostings`) and 18 schemas; `JournalType` gains four members, and the
  settings, cash account, journal, period-close and customer exposure models gain properties (`receivesPosCash` optional
  in requests). Nothing was removed or renamed.
- One permission, `accounting.integration.manage`; 14 in all. Options `Accounting:Integration` (worker on, one minute,
  ten-minute overlap, 500 per run), validated on start.
- Tests: unit tests of every posting rule (balance, roles, signs, parties, rounding), the per-order-line clearing values,
  the retry schedule, automatic journals and activation; a SQL Server test on its own database through the real HTTP host:
  documents before the cutover kept out, a cutover not started yet refused, readiness failing and then passing with the
  opening balances, activation once, the start date and the openings locked, ten documents of every kind journaled
  exactly once (each journal's lines checked), Sales' exposure the same before and after journaling, a shift
  failing without a POS cashbox and posting on retry, credits and claims as open items, Sales' exposure from the
  receivable, idempotent runs (worker and full scan), the reconciliation with nothing unexplained, and the permissions.

## Milestone D — development acceptance (2026-10-30)
- `feature/accounting` pushed to `origin`; after the owner's acceptance it was fast-forwarded into `main` with the
  frontend screens (2026-10-30; technical development acceptance only, see *Production prerequisites*).
- `WarehouseERP_Dev`: state verified (01–13 applied, 14–16 pending); copy-only checksummed backup
  `db-backups\WarehouseERP_Dev_before_Migration14_20261030_131814.bak`, `RESTORE VERIFYONLY` valid, test-restored and
  compared (141 tables, identical row counts, copy dropped); exactly 14–16 applied (`database update
  Migration16_AccountingIntegration`): 23 new tables, no existing table's row count changed, accounting not activated
  (start date 2027-01-01, no account, journal or source posting). Scripts and logs: `.artifacts/dev-db-backup-accounting.*`,
  `dev-db-migrate-accounting.*`.
- The dev API runs the branch build (`https://localhost:49571`): readiness 200, unauthenticated 401, live OpenAPI equal to
  the committed contract (servers excluded).
- Real-host end-to-end on a throwaway database with an explicitly controlled test cutover (2026-10-01; the dev database
  was not activated and none of its documents were journaled): 57/57 checks over every Accounting workflow, the worker
  included (`.artifacts/e2e-accounting-host.ps1`, `e2e-accounting.mjs`); no server error; databases dropped.
- `docs/modules/Accounting/LOVABLE_PROMPT.md` and `FRONTEND_HANDOFF.md` written from the contract and the run.

## Not done yet
- Accountant decisions that change rules: VAT at delivery where that is the tax point; a valuation adjustment of price
  differences once Inventory can revalue (until then they stay under review); a dedicated card-settlement voucher.
- Later than V1: post-dated cheque portfolio, statement import and automatic bank matching, multi-currency vouchers.

## Production prerequisites
The accountant approves the chart and mappings, the closing treatment, and every **[Accountant]** item of the V1
decisions before Accounting is used for statutory reporting; the earlier modules' prerequisites still apply.
