using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

// The save point every Accounting repository shares (one DbContext, one SaveChanges per use case). A known unique-index
// violation comes back as its business conflict; anything else is rethrown.
internal static class AccountingSaves
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration14.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UX_Accounts_Code"] = AccountErrors.CodeTaken,
        ["UX_FiscalYears_StartDate"] = FiscalYearErrors.Exists,
        ["UX_FiscalPeriods_StartDate"] = FiscalYearErrors.Exists,
        ["UX_JournalEntries_Reversal"] = JournalErrors.AlreadyReversed,
        ["PK_AccountMappings"] = CommonErrors.ConcurrencyConflict, // the same role mapped twice at once
    };

    public static Task<Result> SaveAsync(DbContext db, CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
