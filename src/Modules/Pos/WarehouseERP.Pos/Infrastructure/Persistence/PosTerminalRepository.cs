using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Terminals;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

internal sealed class PosTerminalRepository(DbContext db) : IPosTerminalRepository
{
    public Task<PosTerminal?> FindAsync(Guid terminalId, CancellationToken ct) =>
        db.Set<PosTerminal>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == terminalId, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => db.Set<PosTerminal>().AnyAsync(x => x.Code == code, ct);

    public async Task<PosTerminal?> GetForUpdateAsync(Guid terminalId, byte[]? expectedRowVersion, CancellationToken ct)
    {
        var terminal = await db.Set<PosTerminal>().SingleOrDefaultAsync(x => x.Id == terminalId, ct);
        if (terminal is not null && expectedRowVersion is not null)
            db.Entry(terminal).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
        return terminal;
    }

    public void Add(PosTerminal terminal) => db.Add(terminal);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PosSaves.SaveAsync(db, ct);
}
