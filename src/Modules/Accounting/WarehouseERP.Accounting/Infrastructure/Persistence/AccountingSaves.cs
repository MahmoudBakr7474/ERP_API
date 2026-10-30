using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

// The save point every Accounting repository shares (one DbContext, one SaveChanges per use case). A known unique-index
// violation comes back as its business conflict; anything else is rethrown.
internal static class AccountingSaves
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migrations 14, 15 and 16.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UX_Accounts_Code"] = AccountErrors.CodeTaken,
        ["UX_FiscalYears_StartDate"] = FiscalYearErrors.Exists,
        ["UX_FiscalPeriods_StartDate"] = FiscalYearErrors.Exists,
        ["UX_JournalEntries_Reversal"] = JournalErrors.AlreadyReversed,
        ["PK_AccountMappings"] = CommonErrors.ConcurrencyConflict, // the same role mapped twice at once
        ["UX_JournalEntries_BillNumber"] = VoucherErrors.BillNumberTaken,
        ["UX_CashAccounts_Code"] = CashAccountErrors.CodeTaken,
        ["UX_CashAccounts_Account"] = CashAccountErrors.AccountInUse,
        ["UX_BankReconciliations_Draft"] = BankReconciliationErrors.DraftExists,
        ["UX_BankReconciliationLines_Line"] = BankReconciliationErrors.LineTicked,
        ["UX_CashAccounts_PosCash"] = CashAccountErrors.PosCashboxTaken,
        ["UX_SourcePostings_Source"] = CommonErrors.ConcurrencyConflict, // a document found by two runs at once
        ["UX_JournalEntries_Source"] = SourcePostingErrors.Busy, // one document journaled by two runs at once
    };

    public static Task<Result> SaveAsync(DbContext db, CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
