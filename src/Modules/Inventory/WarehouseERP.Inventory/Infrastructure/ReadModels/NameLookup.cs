using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Inventory.Infrastructure.ReadModels;

// Display names from Catalog, Organization and Identity for one page of a read model. Rows never lose their product,
// warehouse or location (no hard deletes), so "?" only shows if something is badly wrong.
internal sealed class NameLookup(
    IReadOnlyDictionary<Guid, ProductInfo> products,
    IReadOnlyDictionary<Guid, ProductUnitInfo> baseUnits,
    IReadOnlyDictionary<Guid, ProductUnitInfo> productUnits,
    IReadOnlyDictionary<Guid, WarehouseInfo> warehouses,
    IReadOnlyDictionary<Guid, WarehouseLocationInfo> locations,
    IReadOnlyDictionary<Guid, string> users)
{
    private const string Unknown = "?";

    public string Sku(Guid productId) => products.TryGetValue(productId, out var p) ? p.Sku : Unknown;

    public string ProductName(Guid productId) => products.TryGetValue(productId, out var p) ? p.NameAr : Unknown;

    public string BaseUnitCode(Guid productId) => baseUnits.TryGetValue(productId, out var u) ? u.UnitCode : Unknown;

    public string UnitCode(Guid productUnitId) => productUnits.TryGetValue(productUnitId, out var u) ? u.UnitCode : Unknown;

    public string WarehouseCode(Guid warehouseId) => warehouses.TryGetValue(warehouseId, out var w) ? w.Code : Unknown;

    public string LocationCode(Guid locationId) => locations.TryGetValue(locationId, out var l) ? l.Code : Unknown;

    public string? UserName(Guid? userId) => userId is { } id && users.TryGetValue(id, out var name) ? name : null;
}
