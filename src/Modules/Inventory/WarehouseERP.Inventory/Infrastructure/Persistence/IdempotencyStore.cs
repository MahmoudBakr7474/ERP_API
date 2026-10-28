using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Domain;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

internal sealed class IdempotencyStore(DbContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken ct) =>
        db.Set<IdempotencyRecord>().AsNoTracking().SingleOrDefaultAsync(x => x.Key == key, ct);

    public void Add(IdempotencyRecord record) => db.Add(record);
}
