using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Reservations;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

internal sealed class StockReservationRepository(DbContext db) : IStockReservationRepository
{
    public Task<bool> AnyForLinesAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken ct) =>
        db.Set<StockReservation>().AnyAsync(x => sourceLineIds.Contains(x.SourceLineId), ct);

    // UPDLOCK, one row at a time in line order: a second consumer or releaser of the same reservation waits here, then
    // sees what the first left.
    public async Task<IReadOnlyList<StockReservation>> GetForUpdateAsync(IReadOnlyCollection<Guid> sourceLineIds, CancellationToken ct)
    {
        var found = new List<StockReservation>();
        foreach (var lineId in sourceLineIds.Distinct().Order())
        {
            var reservation = await db.Set<StockReservation>()
                .FromSqlInterpolated($"SELECT * FROM [inventory].[StockReservations] WITH (UPDLOCK, ROWLOCK) WHERE [SourceLineId] = {lineId}")
                .SingleOrDefaultAsync(ct);
            if (reservation is not null)
                found.Add(reservation);
        }
        return found;
    }

    public void Add(StockReservation reservation) => db.Add(reservation);
}
