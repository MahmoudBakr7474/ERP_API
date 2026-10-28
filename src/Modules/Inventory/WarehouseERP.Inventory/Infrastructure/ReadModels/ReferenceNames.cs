using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

// Loads the names a page needs through the other modules' contracts: one call per kind, never per row.
internal sealed class ReferenceNames(
    IProductCatalog products, IWarehouseDirectory warehouses, IWarehouseLocationDirectory locations, IUserDirectory users)
{
    public async Task<NameLookup> LoadAsync(
        IEnumerable<Guid>? productIds = null, IEnumerable<Guid>? productUnitIds = null, IEnumerable<Guid>? warehouseIds = null,
        IEnumerable<Guid>? locationIds = null, IEnumerable<Guid?>? userIds = null, CancellationToken ct = default)
    {
        var productList = Distinct(productIds);
        var unitList = Distinct(productUnitIds);
        var warehouseList = Distinct(warehouseIds);
        var locationList = Distinct(locationIds);
        var userList = (userIds ?? []).OfType<Guid>().Distinct().ToList();

        return new NameLookup(
            productList.Count == 0 ? [] : (await products.FindManyAsync(productList, ct)).ToDictionary(p => p.Id),
            productList.Count == 0 ? [] : (await products.FindBaseUnitsAsync(productList, ct)).ToDictionary(u => u.ProductId),
            unitList.Count == 0 ? [] : (await products.FindUnitsAsync(unitList, ct)).ToDictionary(u => u.Id),
            warehouseList.Count == 0 ? [] : (await warehouses.FindManyAsync(warehouseList, ct)).ToDictionary(w => w.Id),
            locationList.Count == 0 ? [] : (await locations.FindManyAsync(locationList, ct)).ToDictionary(l => l.Id),
            userList.Count == 0 ? new Dictionary<Guid, string>() : await users.GetDisplayNamesAsync(userList, ct));
    }

    private static List<Guid> Distinct(IEnumerable<Guid>? ids) => (ids ?? []).Distinct().ToList();
}
