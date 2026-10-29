using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

internal sealed class PosReturnRepository(DbContext db) : IPosReturnRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid returnId, CancellationToken ct) =>
        db.Set<PosReturn>().AsNoTracking().Where(x => x.Id == returnId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    public Task<PosReturn?> GetForUpdateAsync(Guid returnId, CancellationToken ct) =>
        db.Set<PosReturn>().SingleOrDefaultAsync(x => x.Id == returnId, ct);

    public void Add(PosReturn posReturn) => db.Add(posReturn);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PosSaves.SaveAsync(db, ct);
}
