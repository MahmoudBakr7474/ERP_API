# ADR 0020 — Accounting V1: general ledger, subledgers, treasury and ERP integration

Status: accepted (2026-10-30), on `feature/accounting`. The product owner approved the scope and decisions in
[`docs/modules/Accounting/V1_DECISIONS.md`](../modules/Accounting/V1_DECISIONS.md), including the global approval rule
in `CLAUDE.md` (*Document approvals*). This ADR records how each milestone implements them; **Milestone A (general
ledger) is implemented**, B (receivables, payables, cash and banks), C (ERP integration and cutover) and D (acceptance)
follow on the same branch and extend this ADR.

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
   only accounts open to manual postings; opening balances may use system accounts; control accounts wait for their
   party lines (Milestone B, `PartyLinesNotSupported`). Drafts copy the approval policy (opening balances always need
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

## Not done yet (later milestones)
- B: party lines, AR/AP open items and allocations, opening customer and supplier balances, receipts, payments,
  withholding amounts, cashboxes, bank accounts, transfers, aging, bank reconciliation, Sales exposure from AR balances.
- C: read-only source contracts, the source-posting step with its unique source identity, worker and exception queue,
  posting rules (deliveries with unbilled receivables, invoices, GRNI and price differences, POS per transaction, shift
  settlement, card clearing), cutover activation and reconciliations.

## Production prerequisites
The accountant approves the chart and mappings, the closing treatment, and every **[Accountant]** item of the V1
decisions before Accounting is used for statutory reporting; the earlier modules' prerequisites still apply.
