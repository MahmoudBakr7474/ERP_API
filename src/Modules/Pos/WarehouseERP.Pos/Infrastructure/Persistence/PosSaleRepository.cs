using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

internal sealed class PosSaleRepository(DbContext db) : IPosSaleRepository
{
    public Task<Guid?> FindWarehouseAsync(Guid saleId, CancellationToken ct) =>
        db.Set<PosSale>().AsNoTracking().Where(x => x.Id == saleId).Select(x => (Guid?)x.WarehouseId).SingleOrDefaultAsync(ct);

    // UPDLOCK: two returns (or a return and a reprint) of one sale wait for each other here.
    public Task<PosSale?> GetForUpdateAsync(Guid saleId, CancellationToken ct) =>
        db.Set<PosSale>()
            .FromSqlInterpolated($"SELECT * FROM [pos].[PosSales] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {saleId}")
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .AsSplitQuery()
            .SingleOrDefaultAsync(ct);

    public void Add(PosSale sale) => db.Add(sale);

    public Task<Result> SaveChangesAsync(CancellationToken ct) => PosSaves.SaveAsync(db, ct);
}
