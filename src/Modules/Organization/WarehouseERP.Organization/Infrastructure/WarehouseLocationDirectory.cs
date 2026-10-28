using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure;

// What other modules may learn about locations: projections, never the entities.
internal sealed class WarehouseLocationDirectory(DbContext db) : IWarehouseLocationDirectory
{
    private static readonly Expression<Func<WarehouseLocation, WarehouseLocationInfo>> ToInfo =
        x => new WarehouseLocationInfo(x.Id, x.WarehouseId, x.Code, x.NameAr, x.IsDefault, x.IsActive);

    public async Task<IReadOnlyList<WarehouseLocationInfo>> FindManyAsync(IReadOnlyCollection<Guid> locationIds, CancellationToken ct)
    {
        var ids = locationIds.Distinct().ToArray();
        return ids.Length == 0
            ? []
            : await db.Set<WarehouseLocation>().AsNoTracking().Where(x => ids.Contains(x.Id)).Select(ToInfo).ToListAsync(ct);
    }

    public Task<WarehouseLocationInfo?> FindDefaultAsync(Guid warehouseId, CancellationToken ct) =>
        db.Set<WarehouseLocation>().AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && x.IsDefault)
            .Select(ToInfo)
            .SingleOrDefaultAsync(ct);
}
