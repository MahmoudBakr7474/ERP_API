using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure;

// What other modules may learn about a warehouse: a projection, never the entity.
internal sealed class WarehouseDirectory(DbContext db) : IWarehouseDirectory
{
    public Task<WarehouseInfo?> FindAsync(Guid warehouseId, CancellationToken ct) =>
        db.Set<Warehouse>().AsNoTracking()
            .Where(x => x.Id == warehouseId)
            .Select(x => new WarehouseInfo(x.Id, x.BranchId, x.Code, x.WarehouseType, x.AllowsSelling, x.IsActive))
            .SingleOrDefaultAsync(ct);
}
