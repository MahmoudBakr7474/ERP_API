using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Shifts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

internal sealed class PosShiftRepository(DbContext db) : IPosShiftRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid shiftId, CancellationToken ct) =>
        db.Set<PosShift>().AsNoTracking().Where(x => x.Id == shiftId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: inside a transaction, a second writer of the same shift (a checkout, a return, a cash movement, the close)
    // waits here, then sees the outcome.
    public async Task<PosShift?> GetForUpdateAsync(Guid shiftId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var shift = await db.Set<PosShift>()
            .FromSqlInterpolated($"SELECT * FROM [pos].[PosShifts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {shiftId}")
            .SingleOrDefaultAsync(ct);
        if (shift is not null && expectedRowVersion is not null)
            db.Entry(shift).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return shift;
    }

    public Task<PosShift?> GetOpenForUpdateAsync(Guid terminalId, CancellationToken ct) =>
        db.Set<PosShift>()
            .FromSqlInterpolated($"SELECT * FROM [pos].[PosShifts] WITH (UPDLOCK, ROWLOCK) WHERE [TerminalId] = {terminalId} AND [Status] = 1")
            .SingleOrDefaultAsync(ct);

    public Task<bool> HasOpenAsync(Guid terminalId, CancellationToken ct) =>
        db.Set<PosShift>().AnyAsync(x => x.TerminalId == terminalId && x.Status == PosShiftStatus.Open, ct);

    public void Add(PosShift shift) => db.Add(shift);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PosSaves.SaveAsync(db, ct);
}
