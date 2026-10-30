# Accounting V1 — approved decisions

Status: approved by the product owner on 2026-10-30 ("Accounting V1 Final Owner Approval & Implementation"), from the
decision checklist proposed the same day, with the owner's corrections. This file replaces the checklist; where they
differ, this file wins. The permanent document self-approval rule of `CLAUDE.md` (*Document approvals*) applies
throughout. How the code implements it: [ADR 0020](../../adr/0020-accounting-module.md).

Items marked **[Accountant]** depend on Egyptian Accounting Standards (EAS) or tax law: they are V1 defaults that the
company's accountant must sign off before production. A balanced journal is not a claim of statutory compliance.

## 1. Scope and ownership
- Accounting owns the general ledger: chart of accounts, posting-role mappings, fiscal years and periods, journals,
  balances (always computed from posted lines), subledgers, cash and bank, financial statements and reconciliations.
- Operational modules keep their documents and amounts: Inventory the value of every stock movement
  (`InventoryCostEntries`), Purchasing supplier liabilities, input VAT, price variances and credit claims, Sales
  revenue, output VAT, receivables and customer credits, POS retail revenue, tenders, refunds and drawer differences.
  Accounting never duplicates their documents; each journal names its source and recognition event.
- Base currency only (EGP from Organization); foreign currency deferred.
- Company-wide users only in V1 (`accounting.*` permissions plus company-wide access); a branch is a report filter, not a
  security scope.

## 2. Milestones
A — general ledger; B — receivables, payables, cash and banks; C — ERP integration, cutover and reconciliations; D —
acceptance, frontend handoff, documentation. V1 is complete only when all four are verified. **Status (2026-10-30):** A, B
and C implemented and verified; D's development acceptance done (real-host end-to-end on a throwaway database, dev
database migrated without activation, frontend handoff written); the Lovable screens are integrated and browser-tested
(frontend `fix/accounting-integration`); the owner accepted the technical development and both repositories' `main` hold
Accounting V1 (2026-10-30). Still open: the accountant's sign-off of every **[Accountant]** item before statutory use.

## 3. Chart of accounts and mappings (A)
- Hierarchical numeric codes extending the parent's code; header accounts group, posting accounts take lines and report
  in a statement section fitting their type; control accounts (customers, suppliers) take lines only with their party;
  accounts only the system feeds (inventory, GRNI, unbilled receivables, till clearing, COGS and inventory differences)
  refuse manual journals. Temporal history; an account with lines is deactivated, never deleted.
- An editable default chart for an Egyptian trading and distribution company (~90 accounts: assets, liabilities,
  equity, revenue, expenses, inventory and COGS, GRNI and price variances, customers and suppliers, cash, banks and card
  clearing, VAT input/output and settlement, withholding and collection, inventory adjustments and losses, cash over and
  short, opening balances and retained earnings). Or the company's approved chart is imported (all or nothing, into an
  empty chart only). **[Accountant]** reviews the chart before activation.
- Every automated posting uses an explicitly mapped posting role (28 roles); no account id is hard-coded and an unmapped
  role stops the posting instead of defaulting.

## 4. Fiscal calendar (A)
- Calendar fiscal years of 12 monthly periods by default; the start month is configurable until the first year exists.
- Periods close and reopen (reopening needs `accounting.periods.reopen` and a reason, kept in the year's history).
  Nothing posts into a closed period and nothing is moved into another period silently.
- Year-end close (after every period and every earlier year is closed) posts a closing journal per account and branch
  into retained earnings; reopening the year reverses it. **[Accountant]** signs off the closing entries.

## 5. Journals (A)
- Manual journals: draft → (submit → approve) → posted; the approval requirement is a setting (seeded: required); the
  writer may approve their own journal. Opening-balance journals are always approved and dated on the cutover date.
- Every posted journal balances by its own lines (debits = credits); amounts in the currency's minor unit, refused if
  they have more decimals; no balancing line is ever added for the user. Posted journals are immutable; corrections are
  reversal journals (one per journal, on their own date) or adjusting journals. System journals are corrected at their
  source or by an adjusting journal.
- Posting and approval are idempotent (`Idempotency-Key`), numbered per journal type and fiscal year (`GJ-2027-000001`),
  and serialized with period closing.

## 6. Cutover and history (A settings, C activation)
- Accounting start date 2027-01-01 by default, configurable until a journal posts, explicitly approved before
  activation. Historical operational transactions are never replayed.
- Activation (C): select and validate the date, opening GL balances, detailed AR/AP opening items per party reconciling
  exactly with the control accounts, inventory valuation at cutover, GRNI and unresolved source balances, debits =
  credits, authorized activation; then automatic accounting for eligible events from that date only. Opening balances
  and source postings must never count the same transaction twice; source-to-GL reconciliation reports missing,
  duplicate and unmatched events.

## 7. Receivables, payables, cash and banks (B)
- Customer receipts and supplier payments (partial and full), allocations to open items, credits applied to eligible
  balances, due dates and aging, customer return credits and supplier return claims, subledgers reconciled to their
  control accounts. Opening customer and supplier balances are linked to the real parties.
- Withholding and collection under account of income tax: entered manually on receipts and payments where applicable,
  with direction-specific accounts; gross settlement, tax withheld and net cash/bank kept separately traceable; no
  automatic Egyptian rates. **[Accountant]**
- Post-dated cheque lifecycle deferred: an uncleared cheque is never bank cash; the payment method is recorded and the
  limitation documented.
- Cashboxes, bank accounts, receipts, payments, internal transfers and a practical manual bank reconciliation.
- Once AR balances and receipts exist, Sales' credit-limit exposure uses total relevant exposure without counting
  confirmed orders and posted invoices twice.
- **Implemented in B (ADR 0020, decisions 14–20):** party lines on control accounts, open items and allocations
  (planned by a voucher, by hand, automatic oldest-first, removable), customer and supplier opening balances with their
  due dates, receipts, payments and refunds with manual withholding, cash receipts and payments, transfers, expense bills
  with input VAT and the supplier's terms, cashboxes and banks with the cash guard (on the posting date and every later
  day), manual bank reconciliation, aging, party statements and the subledger reconciliation. **Moved to C**, because
  they need the source documents in the ledger first: customer return credits and supplier return claims (they come from
  Sales and Purchasing returns) and Sales' credit-limit exposure from AR balances (invoices must open AR items first).
  Cash and bank books are the cash account balances and the account statement of their ledger accounts.

## 8. ERP integration (C)
- Accounting reads posted source events through minimal read-only contracts and posts them periodically and on demand;
  each source event has a unique, durable accounting identity enforced in the database; journal creation is atomic; a
  failure stays visible and retryable in an exception queue; nothing is lost if the API restarts; operational workflows
  (POS checkout included) never fail because Accounting is unavailable; the synchronization state and exceptions are
  shown.
- **Revenue (owner correction):** revenue follows transfer of control — for ordinary trading sales the posted delivery,
  subject to the accountant's approval and documented exceptions. Delivery before invoice recognizes revenue against an
  unbilled receivable and COGS at the Inventory value; the invoice reclassifies to AR without recording revenue again.
  VAT follows its legal tax point, which may differ. **[Accountant: EAS 48, VAT]**
- **Purchases (owner correction):** GRNI at receipt; the supplier invoice settles GRNI and records the liability,
  recoverable tax and price differences. Price differences are not all COGS: their effect on inventory still held,
  inventory already sold or other approved accounts is distinguished from Inventory's own valuation; when existing
  contracts can't support the right valuation adjustment, a visible exception keeps the variance for the accountant's
  review — no potentially wrong adjustment is posted. Transfers never create revenue, purchases, COGS or company value;
  every stock journal reconciles to `InventoryCostEntries`. **[Accountant: EAS 2]**
- **POS (owner correction):** one recognition event per sale and refund, per-transaction posting preferred, traceable to
  sale, refund, payment method and shift; shift close settles cash only; card payments sit in clearing until externally
  settled; unresolved card incidents and cash differences stay visible as reconciliation exceptions. **[Accountant]**
- **Implemented in C (ADR 0020, decisions 21–29):** read-only accounting sources in Inventory, Purchasing, Sales and POS;
  one accounting identity per document (unique in the database) and one system journal per document (IV, PU, SL, PS);
  the worker and the on-demand run, the exception queue with automatic and manual retries; posting rules for Inventory's
  own documents, goods receipts, supplier invoices (price differences under review) and returns (claims), deliveries
  (revenue against unbilled receivables), invoices (reclassified to receivables, VAT at the invoice), customer returns
  (credits), POS sales and returns per transaction, shift settlement into the branch's POS cashbox with the counted
  difference, card clearing; the cutover checks and the authorized activation (section 6); the source-to-ledger
  reconciliation with missing and unmatched documents, POS card incidents and cash differences; customer return credits
  and supplier return claims as open items, and Sales' credit exposure from the receivable balance (section 7).
- **V1 choices within these decisions [Accountant]:** an operational document's accounting date is the company's local
  date it posted on; GRNI and unbilled receivables are valued per order line at the order's price, so they clear to zero;
  stock values are Inventory's cost entries rounded once per document (the reconciliation explains the rounding); price
  differences stay under review until the accountant reclassifies them by journal (Inventory does not revalue stock);
  the treasury records card settlements with a cash receipt (card clearing as counter line) and fees by journal;
  activation is possible only once the cutover's start has passed (the openings are compared with what the modules held
  at that moment), and from then on the opening balances no longer change (corrections are manual journals).

## 9. Reports
Trial balance, general ledger, account statements, balance sheet (company level) and income statement (company or
branch, with comparison) in A; aging, statements, cash and bank books and the subledger reconciliation in B; the
source-to-ledger reconciliation in C. A VAT summary is the account statements of the input and output VAT accounts in V1.
Branch figures add up to the company's; the balance sheet is company-level because transfers move no value between
branches.

## 10. Deferred
Foreign currency, budgets, consolidation, payroll, fixed-asset register, manufacturing costing, cash-flow statement, bank
statement import, ETA integration, VAT return filing, post-dated cheques, branch-scoped accounting users.
