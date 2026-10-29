using Microsoft.EntityFrameworkCore;
using WarehouseERP.BuildingBlocks.Infrastructure.Persistence;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

// The save point every POS repository shares (one DbContext, one SaveChanges per use case; Inventory's stock document,
// posted inside the same transaction, saves with it). A known unique-index violation comes back as its business conflict;
// anything else is rethrown.
internal static class PosSaves
{
    // Unique indexes whose violation is an expected business conflict. Names come from Migration13.
    private static readonly Dictionary<string, Error> KnownUniqueConstraints = new(StringComparer.Ordinal)
    {
        ["UX_PosTerminals_Code"] = TerminalErrors.CodeTaken,
        ["UX_PosShifts_OpenTerminal"] = ShiftErrors.TerminalBusy,
        ["UX_PosShifts_OpenCashier"] = ShiftErrors.CashierBusy,
    };

    public static Task<Result> SaveAsync(DbContext db, CancellationToken ct) => DbFailures.SaveAsync(db, KnownUniqueConstraints, ct);
}
