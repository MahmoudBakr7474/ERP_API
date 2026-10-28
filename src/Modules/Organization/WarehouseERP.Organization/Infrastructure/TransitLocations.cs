using Microsoft.EntityFrameworkCore;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Infrastructure;

// Inventory opens one location per dispatched transfer in a transit warehouse and retires it when the transfer is done.
// Nothing is saved here: the location is tracked by the shared context and saved with the posting that asked for it.
internal sealed class TransitLocations(DbContext db) : ITransitLocations
{
    public async Task<Guid?> OpenAsync(Guid transitWarehouseId, string code, string nameAr, CancellationToken ct)
    {
        var warehouse = await db.Set<Warehouse>().SingleOrDefaultAsync(x => x.Id == transitWarehouseId, ct);
        if (warehouse is not { WarehouseType: WarehouseType.Transit, IsActive: true })
            return null;
        var added = warehouse.AddLocation(code, nameAr, null);
        if (added.IsFailure)
            return null;
        db.Add(added.Value); // its key is generated now, so the caller can place stock in it before saving
        return added.Value.Id;
    }

    public async Task RetireAsync(Guid transitWarehouseId, Guid locationId, CancellationToken ct)
    {
        var location = await db.Set<WarehouseLocation>()
            .SingleOrDefaultAsync(x => x.Id == locationId && x.WarehouseId == transitWarehouseId, ct);
        if (location is { IsActive: true })
            location.SetActive(false);
    }
}
